% simulation/matlab/bms_models/StateEstimator.m
classdef StateEstimator < handle
    properties (Access = private)
        voltageModel
        socHistory
        kalmanFilter
        modelParams
    end
    
    methods
        function obj = StateEstimator()
            % Initialize state estimator
            obj.initializeKalmanFilter();
            obj.initializeVoltageModel();
            obj.socHistory = zeros(1, 100);
            obj.modelParams = struct(...
                'capacity', 100,      % Ah
                'nominalVoltage', 3.7,% V
                'internalR', 0.1,     % Ohm
                'tempCoeff', 0.001    % V/°C
            );
        end
        
        function state = validateState(obj, inputState)
            % Validate and clean input state
            required = {'voltage', 'current', 'temperature', 'soc'};
            
            % Check required fields
            for i = 1:length(required)
                if ~isfield(inputState, required{i})
                    error('Missing required field: %s', required{i});
                end
            end
            
            % Validate ranges
            state = inputState;
            state.voltage = max(2.5, min(4.2, state.voltage));
            state.current = max(-100, min(100, state.current));
            state.temperature = max(0, min(60, state.temperature));
            state.soc = max(0, min(100, state.soc));
        end
        
        function state = estimate(obj, measurements)
            try
                % Apply Kalman filter to measurements
                filtered = obj.applyKalmanFilter(measurements);
                
                % Estimate SOC using multiple methods
                socCC = obj.coulombCounting(filtered.current);
                socOCV = obj.socFromOCV(filtered.voltage, filtered.temperature);
                
                % Fuse SOC estimates
                soc = obj.fuseSOCEstimates(socCC, socOCV);
                
                % Update SOC history
                obj.updateSOCHistory(soc);
                
                % Compile complete state
                state = struct(...
                    'voltage', filtered.voltage, ...
                    'current', filtered.current, ...
                    'temperature', filtered.temperature, ...
                    'soc', soc, ...
                    'health', obj.estimateHealth(filtered), ...
                    'resistance', obj.estimateResistance(filtered), ...
                    'timestamp', datetime('now') ...
                );
                
            catch ME
                warning('State estimation error: %s', ME.message);
                state = obj.getDefaultState();
            end
        end
    end
    
    methods (Access = private)
        function initializeKalmanFilter(obj)
            % Initialize Kalman filter for state estimation
            obj.kalmanFilter = struct(...
                'A', eye(3),      % State transition matrix
                'P', eye(3)*0.1,  % Error covariance
                'Q', eye(3)*0.01, % Process noise
                'R', eye(3)*0.1   % Measurement noise
            );
        end
        
        function initializeVoltageModel(obj)
            % Initialize battery voltage model parameters
            obj.voltageModel = struct(...
                'ocvTable', [0,2.5; 25,3.2; 50,3.7; 75,4.0; 100,4.2], ...
                'tempCoeff', -0.001  % Temperature coefficient V/°C
            );
        end
        
        function filtered = applyKalmanFilter(obj, measurements)
            % Apply Kalman filter to raw measurements
            z = [measurements.voltage; measurements.current; measurements.temperature];
            
            % Prediction
            x_pred = obj.kalmanFilter.A * z;
            P_pred = obj.kalmanFilter.A * obj.kalmanFilter.P * obj.kalmanFilter.A' + obj.kalmanFilter.Q;
            
            % Update
            K = P_pred * inv(P_pred + obj.kalmanFilter.R);
            x = x_pred + K * (z - x_pred);
            obj.kalmanFilter.P = (eye(3) - K) * P_pred;
            
            filtered = struct(...
                'voltage', x(1), ...
                'current', x(2), ...
                'temperature', x(3) ...
            );
        end
        
% Continuing StateEstimator.m with remaining methods:

    methods (Access = private)
        function soc = coulombCounting(obj, current)
            % Coulomb counting method for SOC estimation
            lastSOC = obj.socHistory(end);
            dt = 0.1; % Sample time in hours
            
            % Calculate SOC change
            dSOC = (current * dt) / obj.modelParams.capacity * 100;
            soc = lastSOC - dSOC;
            
            % Apply bounds
            soc = min(100, max(0, soc));
        end
        
        function soc = socFromOCV(obj, voltage, temperature)
            % Estimate SOC from Open Circuit Voltage
            % Compensate for temperature
            compensatedV = voltage + (temperature - 25) * obj.modelParams.tempCoeff;
            
            % Interpolate SOC from OCV table
            soc = interp1(obj.voltageModel.ocvTable(:,2), ...
                         obj.voltageModel.ocvTable(:,1), ...
                         compensatedV, 'linear', 'extrap');
            
            % Apply bounds
            soc = min(100, max(0, soc));
        end
        
        function soc = fuseSOCEstimates(obj, socCC, socOCV)
            % Fusion of different SOC estimates using weighted average
            % Weights depend on SOC range (CC more reliable in middle range)
            if socCC > 20 && socCC < 80
                w_cc = 0.8;
            else
                w_cc = 0.3;
            end
            w_ocv = 1 - w_cc;
            
            % Weighted average
            soc = w_cc * socCC + w_ocv * socOCV;
            
            % Validate bounds
            soc = min(100, max(0, soc));
        end
        
        function updateSOCHistory(obj, soc)
            % Update SOC history buffer
            obj.socHistory = [obj.socHistory(2:end), soc];
        end
        
        function health = estimateHealth(obj, filtered)
            % Estimate battery health based on voltage and internal resistance
            try
                % Calculate voltage efficiency
                voltageHealth = filtered.voltage / obj.modelParams.nominalVoltage;
                
                % Calculate resistance health
                resistance = obj.estimateResistance(filtered);
                resistanceHealth = obj.modelParams.internalR / resistance;
                
                % Combine health indicators
                health = struct(...
                    'soh', min(100, (voltageHealth + resistanceHealth) * 50), ... % State of Health
                    'voltageHealth', voltageHealth * 100, ...
                    'resistanceHealth', resistanceHealth * 100, ...
                    'timestamp', datetime('now') ...
                );
            catch
                health = struct(...
                    'soh', 100, ...
                    'voltageHealth', 100, ...
                    'resistanceHealth', 100, ...
                    'timestamp', datetime('now') ...
                );
            end
        end
        
        function resistance = estimateResistance(obj, filtered)
            % Estimate internal resistance using voltage and current
            try
                if abs(filtered.current) > 1 % Ensure sufficient current for calculation
                    % R = ΔV/I
                    deltaV = filtered.voltage - obj.modelParams.nominalVoltage;
                    resistance = abs(deltaV / filtered.current);
                    
                    % Apply temperature compensation
                    tempFactor = 1 + 0.003 * (filtered.temperature - 25); % 0.3% per °C
                    resistance = resistance * tempFactor;
                    
                    % Bound resistance estimates
                    resistance = min(1.0, max(0.01, resistance));
                else
                    resistance = obj.modelParams.internalR;
                end
            catch
                resistance = obj.modelParams.internalR;
            end
        end
        
        function state = getDefaultState(obj)
            % Return safe default state when estimation fails
            state = struct(...
                'voltage', obj.modelParams.nominalVoltage, ...
                'current', 0, ...
                'temperature', 25, ...
                'soc', 50, ...
                'health', struct(...
                    'soh', 100, ...
                    'voltageHealth', 100, ...
                    'resistanceHealth', 100, ...
                    'timestamp', datetime('now') ...
                ), ...
                'resistance', obj.modelParams.internalR, ...
                'timestamp', datetime('now') ...
            );
        end
        
        function validateMeasurements(~, measurements)
            % Validate measurement data
            required = {'voltage', 'current', 'temperature'};
            for i = 1:length(required)
                if ~isfield(measurements, required{i})
                    error('BMS:StateEstimator:MissingData', ...
                          'Missing required measurement: %s', required{i});
                end
            end
            
            % Validate ranges
            if measurements.voltage < 2.0 || measurements.voltage > 4.5
                error('BMS:StateEstimator:InvalidVoltage', ...
                      'Voltage out of valid range');
            end
            if abs(measurements.current) > 150
                error('BMS:StateEstimator:InvalidCurrent', ...
                      'Current out of valid range');
            end
            if measurements.temperature < -20 || measurements.temperature > 70
                error('BMS:StateEstimator:InvalidTemperature', ...
                      'Temperature out of valid range');
            end
        end
    end
    
    methods (Static)
        function params = getDefaultModelParams()
            % Return default model parameters
            params = struct(...
                'capacity', 100, ...      % Nominal capacity in Ah
                'nominalVoltage', 3.7, ...% Nominal voltage in V
                'internalR', 0.1, ...     % Nominal internal resistance in Ohm
                'tempCoeff', 0.001, ...   % Temperature coefficient in V/°C
                'socTable', [0,2.5; 25,3.2; 50,3.7; 75,4.0; 100,4.2] ... % SOC-OCV table
            );
        end
    end
end