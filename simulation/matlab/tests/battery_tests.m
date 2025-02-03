% simulation/matlab/tests/battery_tests.m
function tests = battery_tests
    tests = functiontests(localfunctions);
end

function testSetup(testCase)
    % Create test data
    testCase.TestData.capacity = 100;
    testCase.TestData.currentState = struct(...
        'soc', 75, ...
        'temperature', 25, ...
        'voltage', 3.7, ...
        'current', 2.0 ...
    );
end

function testBatteryOptimization(testCase)
    % Test basic optimization
    optimizer = BatteryOptimizer(testCase.TestData.capacity);
    [current, thermal] = optimizer.optimize(testCase.TestData.currentState);
    
    % Verify outputs
    verifyGreaterThan(testCase, current, 0);
    verifyLessThan(testCase, current, 100);
    verifyGreaterThan(testCase, thermal.targetTemp, 15);
    verifyLessThan(testCase, thermal.targetTemp, 45);
end

function testSafetyLimits(testCase)
    % Test safety limits
    optimizer = BatteryOptimizer(testCase.TestData.capacity);
    
    % Test high temperature scenario
    highTempState = testCase.TestData.currentState;
    highTempState.temperature = 40;
    [current, ~] = optimizer.optimize(highTempState);
    
    % Verify current reduction at high temperature
    verifyLessThan(testCase, current, 50);
end