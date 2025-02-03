% simulation/matlab/bms_models/SafetyMonitor.m

classdef SafetyMonitor < handle
    properties (Access = private)
        % Safety thresholds with hysteresis
        voltageThresholds
        currentThresholds
        temperatureThresholds
        socThresholds
        
        % Fault counters and history
        faultCounters
        alarmHistory
        
        % Operating states
        operatingState
        lastCheckTime
        
        % Configuration
        config
        
        % Data logging
        logger
    end
    
    methods
        function obj = SafetyMonitor()
            % Initialize safety monitor with default configurations
            obj.initializeThresholds();
            obj.initializeFaultCounters();
            obj.initializeConfig();
            obj.operatingState = 'NORMAL';
            obj.lastCheckTime = datetime('now');
            obj.alarmHistory = [];
            obj.setupLogger();
        end
        
        function [safetyStatus, alarms] = checkSafety(obj, measurements)
            % Main safety check method
            try
                % Input validation
                obj.validateMeasurements(measurements);
                
                % Update check time
                currentTime = datetime('now');
                deltaT = seconds(currentTime - obj.lastCheckTime);
                obj.lastCheckTime = currentTime;
                
                % Perform individual safety checks
                voltageStatus = obj.checkVoltage(measurements.voltage);
                currentStatus = obj.checkCurrent(measurements.current);
                tempStatus = obj.checkTemperature(measurements.temperature);
                socStatus = obj.checkSOC(measurements.soc);
                
                % Check rate of change
                rateStatus = obj.checkRatesOfChange(measurements, deltaT);
                
                % Compile alarms
                alarms = obj.compileAlarms([voltageStatus, currentStatus, ...
                                         tempStatus, socStatus, rateStatus]);
                
                % Update fault counters and determine system status
                safetyStatus = obj.updateSystemStatus(alarms);
                
                % Log results
                obj.logSafetyCheck(measurements, safetyStatus, alarms);
                
            catch ME
                obj.handleError(ME);
                [safetyStatus, alarms] = obj.getFailSafeStatus();
            end
        end
        
        function status = getOperatingState(obj)
            status = obj.operatingState;
        end
        
        function history = getAlarmHistory(obj)
            history = obj.alarmHistory;
        end
    end
    
    methods (Access = private)
        function initializeThresholds(obj)
            % Initialize safety thresholds with hysteresis
            obj.voltageThresholds = struct(...
                'absoluteMin', 2.5, ...
                'absoluteMax', 4.2, ...
                'warningLowV', 2.8, ...
                'warningHighV', 4.1, ...
                'criticalLowV', 2.6, ...
                'criticalHighV', 4.15, ...
                'hysteresis', 0.1 ...
            );
            
            obj.currentThresholds = struct(...
                'absoluteMin', -100, ... % A
                'absoluteMax', 100, ...  % A
                'warningCurrent', 80, ...
                'criticalCurrent', 90, ...
                'hysteresis', 5 ...
            );
            
            obj.temperatureThresholds = struct(...
                'absoluteMin', 0, ...    % °C
                'absoluteMax', 45, ...   % °C
                'warningLowT', 5, ...
                'warningHighT', 40, ...
                'criticalLowT', 2, ...
                'criticalHighT', 43, ...
                'hysteresis', 2 ...
            );
            
            obj.socThresholds = struct(...
                'warningLow', 10, ...    % %
                'warningHigh', 90, ...
                'criticalLow', 5, ...
                'criticalHigh', 95, ...
                'hysteresis', 2 ...
            );
        end
        
        function initializeFaultCounters(obj)
            obj.faultCounters = struct(...
                'voltage', 0, ...
                'current', 0, ...
                'temperature', 0, ...
                'soc', 0, ...
                'rateOfChange', 0, ...
                'consecutive', 0, ...
                'resetTime', datetime('now') ...
            );
        end
        
        function initializeConfig(obj)
            obj.config = struct(...
                'maxConsecutiveFaults', 3, ...
                'faultCounterResetTime', hours(1), ...
                'maxRateVoltage', 0.1, ...    % V/s
                'maxRateCurrent', 10, ...     % A/s
                'maxRateTemperature', 1, ...  % °C/s
                'logInterval', seconds(1) ...
            );
        end
        
        function setupLogger(obj)
            % Setup data logging
            logFile = sprintf('safety_log_%s.txt', ...
                            datestr(now, 'yyyymmdd_HHMMSS'));
            obj.logger = fopen(logFile, 'w');
            fprintf(obj.logger, 'Timestamp,State,Alarms\n');
        end
        
        function validateMeasurements(obj, measurements)
            required = {'voltage', 'current', 'temperature', 'soc'};
            for i = 1:length(required)
                if ~isfield(measurements, required{i})
                    error('SafetyMonitor:MissingData', ...
                          'Missing required measurement: %s', required{i});
                end
            end
            
            % Validate data types and ranges
            validateattributes(measurements.voltage, {'numeric'}, ...
                {'finite', 'scalar', '>=', 0, '<=', 5});
            validateattributes(measurements.current, {'numeric'}, ...
                {'finite', 'scalar', '>=', -150, '<=', 150});
            validateattributes(measurements.temperature, {'numeric'}, ...
                {'finite', 'scalar', '>=', -20, '<=', 60});
            validateattributes(measurements.soc, {'numeric'}, ...
                {'finite', 'scalar', '>=', 0, '<=', 100});
        end
        
        function status = checkVoltage(obj, voltage)
            status = struct('parameter', 'voltage', 'level', 'NORMAL', ...
                          'message', '', 'value', voltage);
            
            % Check absolute limits
            if voltage <= obj.voltageThresholds.absoluteMin || ...
               voltage >= obj.voltageThresholds.absoluteMax
                status.level = 'CRITICAL';
                status.message = sprintf('Voltage (%.2fV) outside absolute limits', voltage);
                obj.faultCounters.voltage = obj.faultCounters.voltage + 1;
                return;
            end
            
            % Check critical limits with hysteresis
            if (voltage <= obj.voltageThresholds.criticalLowV && ...
                obj.operatingState ~= 'NORMAL') || ...
               (voltage >= obj.voltageThresholds.criticalHighV && ...
                obj.operatingState ~= 'NORMAL')
                status.level = 'WARNING';
                status.message = sprintf('Voltage (%.2fV) at critical level', voltage);
                obj.faultCounters.voltage = obj.faultCounters.voltage + 1;
                return;
            end
            
            % Check warning limits
            if voltage <= obj.voltageThresholds.warningLowV || ...
               voltage >= obj.voltageThresholds.warningHighV
                status.level = 'WARNING';
                status.message = sprintf('Voltage (%.2fV) approaching limits', voltage);
            end
        end
        
        % Similar implementations for current, temperature, and SOC checks...
        
        function status = checkRatesOfChange(obj, measurements, deltaT)
            status = struct('parameter', 'rate', 'level', 'NORMAL', ...
                          'message', '', 'value', 0);
            
            if deltaT > 0
                % Calculate rates of change
                voltageRate = abs(measurements.voltage) / deltaT;
                currentRate = abs(measurements.current) / deltaT;
                tempRate = abs(measurements.temperature) / deltaT;
                
                % Check against maximum allowed rates
                if voltageRate > obj.config.maxRateVoltage || ...
                   currentRate > obj.config.maxRateCurrent || ...
                   tempRate > obj.config.maxRateTemperature
                    status.level = 'WARNING';
                    status.message = 'Rapid parameter change detected';
                    obj.faultCounters.rateOfChange = obj.faultCounters.rateOfChange + 1;
                end
            end
        end
        
        function alarms = compileAlarms(obj, statusArray)
            alarms = {};
            for i = 1:length(statusArray)
                if ~strcmp(statusArray(i).level, 'NORMAL')
                    alarms{end+1} = statusArray(i);
                end
            end
        end
        
        function status = updateSystemStatus(obj, alarms)
            % Reset fault counters if enough time has passed
            if datetime('now') - obj.faultCounters.resetTime > ...
               obj.config.faultCounterResetTime
                obj.initializeFaultCounters();
            end
            
            % Update consecutive fault counter
            if ~isempty(alarms)
                obj.faultCounters.consecutive = obj.faultCounters.consecutive + 1;
            else
                obj.faultCounters.consecutive = 0;
            end
            
            % Determine system status
            if obj.faultCounters.consecutive >= obj.config.maxConsecutiveFaults
                status = 'EMERGENCY_SHUTDOWN';
                obj.operatingState = 'EMERGENCY';
            elseif any(strcmp({alarms.level}, 'CRITICAL'))
                status = 'CRITICAL';
                obj.operatingState = 'RESTRICTED';
            elseif any(strcmp({alarms.level}, 'WARNING'))
                status = 'WARNING';
                obj.operatingState = 'CAUTIOUS';
            else
                status = 'NORMAL';
                if strcmp(obj.operatingState, 'NORMAL')
                    obj.operatingState = 'NORMAL';
                end
            end
        end
        
        function logSafetyCheck(obj, measurements, status, alarms)
            % Log safety check results
            timestamp = datetime('now');
            alarmStr = '';
            for i = 1:length(alarms)
                alarmStr = [alarmStr, alarms{i}.message, '; '];
            end
            
            fprintf(obj.logger, '%s,%s,%s\n', ...
                    datestr(timestamp), status, alarmStr);
        end
        
        function handleError(obj, error)
            % Log error and set fail-safe state
            warning('SafetyMonitor:Error', ...
                    'Safety monitoring error: %s', error.message);
            obj.operatingState = 'FAIL_SAFE';
            
            % Log error
            fprintf(obj.logger, '%s,ERROR,%s\n', ...
                    datestr(datetime('now')), error.message);
        end
        
        function [status, alarms] = getFailSafeStatus(obj)
            status = 'FAIL_SAFE';
            alarms = {struct('parameter', 'system', ...
                           'level', 'CRITICAL', ...
                           'message', 'System entering fail-safe mode', ...
                           'value', NaN)};
        end
    end
    
    methods (Access = protected)
        function delete(obj)
            % Cleanup when object is destroyed
            if ~isempty(obj.logger) && obj.logger ~= 0
                fclose(obj.logger);
            end
        end
    end
end