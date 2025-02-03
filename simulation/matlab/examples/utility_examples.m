% simulation/matlab/examples/utility_examples.m

%% Example 1: Basic Data Processing
% Initialize processor
processor = DataProcessor();

% Generate sample data
t = linspace(0, 100, 1000)';
voltage = 3.7 + 0.1*sin(t/10) + 0.05*randn(size(t));

% Process voltage data
processed = processor.processData(voltage, 'voltage');

% Calculate statistics
stats = processor.calculateStatistics(voltage);

% Display results
fprintf('Voltage Statistics:\n');
fprintf('Mean: %.2f V\n', stats.mean);
fprintf('Std Dev: %.2f V\n', stats.std);
fprintf('Quality Score: %.2f\n', processed.quality.snr);

%% Example 2: Advanced Data Processing
% Process multiple data types
current = 10 + 2*sin(t/5) + 0.5*randn(size(t));
temperature = 25 + 5*sin(t/20) + 0.2*randn(size(t));

% Apply moving average filter
smoothedVoltage = processor.applyMovingAverage(voltage, 20);
smoothedCurrent = processor.applyMovingAverage(current, 20);

% Plot original vs smoothed data
figure;
subplot(2,1,1);
plot(t, voltage, 'b', t, smoothedVoltage, 'r');
title('Voltage: Original vs Smoothed');
legend('Original', 'Smoothed');

subplot(2,1,2);
plot(t, current, 'b', t, smoothedCurrent, 'r');
title('Current: Original vs Smoothed');
legend('Original', 'Smoothed');

%% Example 3: Basic Visualization
% Initialize visualizer
visualizer = Visualizer();

% Create data structure
data = struct(...
    'time', t, ...
    'voltage', voltage, ...
    'current', current, ...
    'temperature', temperature, ...
    'soc', 80 - 0.2*t + randn(size(t)), ...
    'efficiency', 95 + 2*sin(t/15) + 0.1*randn(size(t)) ...
);

% Plot real-time data
visualizer.plotRealTime(data, 'all');

%% Example 4: Performance Visualization
% Add performance metrics
data.capacityLoss = 0.1*t + 0.2*randn(size(t));
data.impedanceIncrease = 0.05*t + 0.1*randn(size(t));
data.health = 100 - 0.1*t + 0.5*randn(size(t));

% Plot performance metrics
visualizer.plotPerformance(data, 'overview');

% Export plot
visualizer.exportPlot('performance', 'png');

%% Example 5: Integration Example
% Combine processing and visualization
% Process raw data
processedData = struct();
processedData.voltage = processor.processData(voltage, 'voltage');
processedData.current = processor.processData(current, 'current');
processedData.temperature = processor.processData(temperature, 'temperature');

% Create time series
timeVector = datetime('now') + seconds(t);
processedData.time = timeVector;

% Visualize processed data
visualizer.plotRealTime(processedData, 'all');

% Add analysis results
fprintf('\nData Analysis Results:\n');
fprintf('Voltage Quality: %.2f\n', processedData.voltage.quality.snr);
fprintf('Current Quality: %.2f\n', processedData.current.quality.snr);
fprintf('Temperature Quality: %.2f\n', processedData.temperature.quality.snr);

%% Example 6: Real-time Simulation
% Simulate real-time data processing and visualization
fprintf('\nStarting real-time simulation...\n');

figure('Name', 'Real-time Battery Monitoring');
for i = 1:100
    % Generate new data point
    newData = struct(...
        'time', datetime('now'), ...
        'voltage', 3.7 + 0.1*sin(i/10) + 0.05*randn(), ...
        'current', 10 + 2*sin(i/5) + 0.5*randn(), ...
        'temperature', 25 + 5*sin(i/20) + 0.2*randn() ...
    );
    
    % Process data
    processed = struct();
    processed.voltage = processor.processData(newData.voltage, 'voltage');
    processed.current = processor.processData(newData.current, 'current');
    processed.temperature = processor.processData(newData.temperature, 'temperature');
    processed.time = newData.time;
    
    % Update visualization
    visualizer.plotRealTime(processed, 'all');
    
    % Add small delay to simulate real-time
    pause(0.1);
end

fprintf('Simulation complete.\n');