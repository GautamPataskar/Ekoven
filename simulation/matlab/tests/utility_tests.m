% simulation/matlab/tests/utility_tests.m

classdef UtilityTests < matlab.unittest.TestCase
    properties
        dataProcessor
        visualizer
        testData
    end
    
    methods (TestMethodSetup)
        function setupTest(testCase)
            % Initialize test environment
            testCase.dataProcessor = DataProcessor();
            testCase.visualizer = Visualizer();
            testCase.testData = testCase.generateTestData();
        end
    end
    
    methods (Test)
        function testDataProcessing(testCase)
            % Test data processing functionality
            rawData = testCase.testData.voltage;
            
            % Test voltage processing
            processed = testCase.dataProcessor.processData(rawData, 'voltage');
            testCase.verifyClass(processed, 'struct');
            testCase.verifyField(processed, 'value');
            testCase.verifyField(processed, 'quality');
            
            % Test filtering
            filtered = testCase.dataProcessor.applyMovingAverage(rawData);
            testCase.verifySize(filtered, size(rawData));
            
            % Test statistics
            stats = testCase.dataProcessor.calculateStatistics(rawData);
            testCase.verifyField(stats, 'mean');
            testCase.verifyField(stats, 'std');
        end
        
        function testVisualization(testCase)
            % Test visualization functionality
            data = testCase.testData;
            
            % Test performance plotting
            testCase.visualizer.plotPerformance(data, 'efficiency');
            testCase.verifyTrue(isvalid(gcf));
            
            % Test real-time plotting
            testCase.visualizer.plotRealTime(data, 'voltage');
            testCase.verifyTrue(isvalid(gcf));
            
            % Test plot export
            testCase.visualizer.exportPlot('performance', 'png');
            testCase.verifyTrue(exist('performance_*.png', 'file') > 0);
        end
    end
    
    methods (Access = private)
        function data = generateTestData(~)
            % Generate test data
            t = linspace(0, 100, 1000)';
            
            data = struct(...
                'time', t, ...
                'voltage', 3.7 + 0.1*sin(t/10) + 0.05*randn(size(t)), ...
                'current', 10 + 2*sin(t/5) + 0.5*randn(size(t)), ...
                'temperature', 25 + 5*sin(t/20) + 0.2*randn(size(t)), ...
                'soc', 80 - 0.2*t + 1*randn(size(t)), ...
                'efficiency', 95 + 2*sin(t/15) + 0.1*randn(size(t)), ...
                'health', 100 - 0.1*t + 0.5*randn(size(t)), ...
                'capacityLoss', 0.1*t + 0.2*randn(size(t)), ...
                'impedanceIncrease', 0.05*t + 0.1*randn(size(t)) ...
            );
        end
    end
end