% simulation/matlab/bms_models/ThermalController.m
classdef ThermalController < handle
    properties (Access = private)
        tempHistory
        safetyLimits
        controlParams
    end
    
    methods
        function obj = ThermalController()
            % Initialize thermal controller with default parameters
            obj.tempHistory = zeros(1, 100); % Rolling temperature history
            obj.safetyLimits = struct(...
                'maxTemp', 45, ...
                'minTemp', 15, ...
                'maxFanSpeed', 100, ...
                'minFanSpeed', 0, ...
                'optimalTemp', 25 ...
            );
            obj.controlParams = struct(...
                'kp', 2.5,    % Proportional gain
                'ki', 0.5,    % Integral gain
                'kd', 1.0,    % Derivative gain
                'dt', 0.1     % Sample time
            );
        end
        
        function control = getControlParams(obj, temperature, current, soc)
            try
                % Update temperature history
                obj.updateTempHistory(temperature);
                
                % Calculate rate of temperature change
                tempRate = obj.calculateTempRate();
                
                % Predict future temperature
                predictedTemp = obj.predictTemperature(temperature, current, soc);
                
                % Calculate PID control output
                fanSpeed = obj.calculatePIDControl(temperature, predictedTemp);
                
                % Package control parameters
                control = struct(...
                    'fanSpeed', fanSpeed, ...
                    'targetTemp', obj.safetyLimits.optimalTemp, ...
                    'predictedTemp', predictedTemp, ...
                    'tempRate', tempRate ...
                );
                
            catch ME
                warning('Thermal control error: %s', ME.message);
                control = obj.getFailSafeControl();
            end
        end
    end
    
    methods (Access = private)
        function updateTempHistory(obj, newTemp)
            obj.tempHistory = [obj.tempHistory(2:end), newTemp];
        end
        
        function rate = calculateTempRate(obj)
            % Calculate temperature change rate using recent history
            recent = obj.tempHistory(end-9:end);
            rate = mean(diff(recent)) / obj.controlParams.dt;
        end
        
        function predTemp = predictTemperature(obj, temp, current, soc)
            % Simple thermal model for temperature prediction
            % Higher current or SOC leads to more heat generation
            heatGeneration = 0.01 * current^2 + 0.005 * soc;
            coolingEffect = -0.1 * (temp - obj.safetyLimits.optimalTemp);
            
            predTemp = temp + (heatGeneration + coolingEffect) * obj.controlParams.dt;
        end
        
        function fanSpeed = calculatePIDControl(obj, currentTemp, predictedTemp)
            % PID control implementation
            error = currentTemp - obj.safetyLimits.optimalTemp;
            errorRate = (predictedTemp - currentTemp) / obj.controlParams.dt;
            
            % Calculate PID terms
            pTerm = obj.controlParams.kp * error;
            iTerm = obj.controlParams.ki * sum(obj.tempHistory - obj.safetyLimits.optimalTemp) * obj.controlParams.dt;
            dTerm = obj.controlParams.kd * errorRate;
            
            % Calculate fan speed
            fanSpeed = pTerm + iTerm + dTerm;
            
            % Ensure within limits
            fanSpeed = min(max(fanSpeed, obj.safetyLimits.minFanSpeed), obj.safetyLimits.maxFanSpeed);
            
            % Additional rules for extreme conditions
            if currentTemp >= obj.safetyLimits.maxTemp
                fanSpeed = obj.safetyLimits.maxFanSpeed;
            elseif currentTemp <= obj.safetyLimits.minTemp
                fanSpeed = obj.safetyLimits.minFanSpeed;
            end
        end
        
        function control = getFailSafeControl(obj)
            % Return fail-safe control parameters
            control = struct(...
                'fanSpeed', obj.safetyLimits.maxFanSpeed, ...
                'targetTemp', obj.safetyLimits.optimalTemp, ...
                'predictedTemp', NaN, ...
                'tempRate', 0 ...
            );
        end
    end
end