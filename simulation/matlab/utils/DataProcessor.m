% simulation/matlab/utils/DataProcessor.m
classdef DataProcessor < handle
    properties (Access = private)
        config
        dataBuffer
        filterParams
        preprocessors
        validators
    end
    
    methods
        function obj = DataProcessor()
            % Initialize data processor
            obj.initializeConfig();
            obj.dataBuffer = containers.Map();
            obj.initializeFilters();
            obj.initializePreprocessors();
            obj.initializeValidators();
        end
        
        function processed = processData(obj, rawData, dataType)
            try
                % Validate input data
                obj.validateInput(rawData, dataType);
                
                % Preprocess data
                preprocessed = obj.preprocess(rawData, dataType);
                
                % Apply filters
                filtered = obj.applyFilters(preprocessed, dataType);
                
                % Post-process and validate
                processed = obj.postprocess(filtered, dataType);
                
                % Update data buffer
                obj.updateBuffer(processed, dataType);
                
            catch ME
                warning('Data processing error: %s', ME.message);
                processed = obj.getLastValidData(dataType);
            end
        end
        
        function stats = calculateStatistics(obj, data, statType)
            % Calculate various statistics on the data
            stats = struct(...
                'mean', mean(data), ...
                'std', std(data), ...
                'min', min(data), ...
                'max', max(data), ...
                'median', median(data) ...
            );
            
            if nargin > 2 && strcmp(statType, 'advanced')
                stats.skewness = skewness(data);
                stats.kurtosis = kurtosis(data);
                stats.percentiles = prctile(data, [5 25 75 95]);
            end
        end
        
        function filtered = applyMovingAverage(obj, data, windowSize)
            if nargin < 3
                windowSize = obj.config.defaultWindowSize;
            end
            filtered = movmean(data, windowSize);
        end
    end
    
    methods (Access = private)
        function initializeConfig(obj)
            obj.config = struct(...
                'defaultWindowSize', 10, ...
                'maxBufferSize', 1000, ...
                'sampleRate', 10, ... % Hz
                'filterOrder', 4, ...
                'cutoffFreq', 2 ... % Hz
            );
        end
        
        function initializeFilters(obj)
            % Initialize digital filters
            fs = obj.config.sampleRate;
            fc = obj.config.cutoffFreq;
            order = obj.config.filterOrder;
            
            [b, a] = butter(order, fc/(fs/2), 'low');
            obj.filterParams = struct(...
                'lowpass', struct('b', b, 'a', a), ...
                'median', struct('windowSize', 5), ...
                'kalman', struct('Q', 0.1, 'R', 1) ...
            );
        end
        
        function initializePreprocessors(obj)
            obj.preprocessors = containers.Map();
            
            % Voltage preprocessor
            obj.preprocessors('voltage') = @(x) obj.preprocessVoltage(x);
            
            % Current preprocessor
            obj.preprocessors('current') = @(x) obj.preprocessCurrent(x);
            
            % Temperature preprocessor
            obj.preprocessors('temperature') = @(x) obj.preprocessTemperature(x);
        end
        
        function initializeValidators(obj)
            obj.validators = containers.Map();
            
            % Voltage validator
            obj.validators('voltage') = @(x) x >= 2.0 && x <= 4.5;
            
            % Current validator
            obj.validators('current') = @(x) x >= -150 && x <= 150;
            
            % Temperature validator
            obj.validators('temperature') = @(x) x >= -20 && x <= 60;
        end
        
        function validateInput(obj, data, dataType)
            if ~obj.validators.isKey(dataType)
                error('Invalid data type: %s', dataType);
            end
            
            validator = obj.validators(dataType);
            if ~validator(data)
                error('Data validation failed for type: %s', dataType);
            end
        end
        
        function preprocessed = preprocess(obj, data, dataType)
            if ~obj.preprocessors.isKey(dataType)
                preprocessed = data;
                return;
            end
            
            preprocessor = obj.preprocessors(dataType);
            preprocessed = preprocessor(data);
        end
        
        function filtered = applyFilters(obj, data, dataType)
            % Apply appropriate filters based on data type
            switch dataType
                case 'voltage'
                    filtered = filtfilt(obj.filterParams.lowpass.b, ...
                                     obj.filterParams.lowpass.a, data);
                case 'current'
                    filtered = medfilt1(data, obj.filterParams.median.windowSize);
                case 'temperature'
                    filtered = obj.applyKalmanFilter(data);
                otherwise
                    filtered = data;
            end
        end
        
        function filtered = applyKalmanFilter(obj, data)
            % Simple Kalman filter implementation
            Q = obj.filterParams.kalman.Q;
            R = obj.filterParams.kalman.R;
            
            x = data(1);
            P = 1;
            filtered = zeros(size(data));
            
            for i = 1:length(data)
                % Predict
                P = P + Q;
                
                % Update
                K = P / (P + R);
                x = x + K * (data(i) - x);
                P = (1 - K) * P;
                
                filtered(i) = x;
            end
        end
        
        function processed = postprocess(obj, data, dataType)
            % Apply any necessary post-processing
            processed = data;
            
            % Add metadata
            processed = struct(...
                'value', processed, ...
                'timestamp', datetime('now'), ...
                'type', dataType, ...
                'quality', obj.assessDataQuality(data) ...
            );
        end
        
        function quality = assessDataQuality(obj, data)
            % Assess data quality metrics
            quality = struct(...
                'snr', obj.calculateSNR(data), ...
                'completeness', obj.checkCompleteness(data), ...
                'reliability', obj.checkReliability(data) ...
            );
        end
        
        function updateBuffer(obj, data, dataType)
            if ~obj.dataBuffer.isKey(dataType)
                obj.dataBuffer(dataType) = [];
            end
            
            buffer = obj.dataBuffer(dataType);
            buffer = [buffer; data];
            
            % Maintain buffer size
            if length(buffer) > obj.config.maxBufferSize
                buffer = buffer(end-obj.config.maxBufferSize+1:end);
            end
            
            obj.dataBuffer(dataType) = buffer;
        end
    end
end