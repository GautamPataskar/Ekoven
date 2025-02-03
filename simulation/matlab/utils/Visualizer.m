% simulation/matlab/utils/Visualizer.m
classdef Visualizer < handle
    properties (Access = private)
        config
        figures
        colorMap
        plotStyles
        lastUpdate
    end
    
    methods
        function obj = Visualizer()
            % Initialize visualizer
            obj.initializeConfig();
            obj.figures = containers.Map();
            obj.initializeColorMap();
            obj.initializePlotStyles();
            obj.lastUpdate = datetime('now');
        end
        
        function plotPerformance(obj, data, plotType)
            try
                % Create or get figure
                fig = obj.getFigure('performance');
                
                switch plotType
                    case 'efficiency'
                        obj.plotEfficiency(fig, data);
                    case 'thermal'
                        obj.plotThermal(fig, data);
                    case 'degradation'
                        obj.plotDegradation(fig, data);
                    case 'overview'
                        obj.plotOverview(fig, data);
                    otherwise
                        error('Invalid plot type');
                end
                
                % Update figure
                drawnow;
                obj.lastUpdate = datetime('now');
                
            catch ME
                warning('Visualization error: %s', ME.message);
            end
        end
        
        function plotRealTime(obj, data, plotType)
            try
                fig = obj.getFigure('realtime');
                
                % Clear previous plots if update interval exceeded
                if seconds(datetime('now') - obj.lastUpdate) > obj.config.updateInterval
                    clf(fig);
                end
                
                % Plot real-time data
                switch plotType
                    case 'voltage'
                        obj.plotVoltage(fig, data);
                    case 'current'
                        obj.plotCurrent(fig, data);
                    case 'temperature'
                        obj.plotTemperature(fig, data);
                    case 'all'
                        obj.plotAllMetrics(fig, data);
                end
                
                % Update display
                drawnow;
                obj.lastUpdate = datetime('now');
                
            catch ME
                warning('Real-time visualization error: %s', ME.message);
            end
        end
        
        function exportPlot(obj, figName, format)
            if nargin < 3
                format = 'png';
            end
            
            try
                if obj.figures.isKey(figName)
                    fig = obj.figures(figName);
                    filename = sprintf('%s_%s.%s', figName, ...
                        datestr(now, 'yyyymmdd_HHMMSS'), format);
                    saveas(fig, filename, format);
                else
                    error('Figure not found: %s', figName);
                end
            catch ME
                warning('Export error: %s', ME.message);
            end
        end
    end
    
    methods (Access = private)
        function initializeConfig(obj)
            obj.config = struct(...
                'figureSize', [800 600], ...
                'fontSize', 12, ...
                'lineWidth', 1.5, ...
                'markerSize', 6, ...
                'updateInterval', 1, ... % seconds
                'maxDataPoints', 1000, ...
                'gridAlpha', 0.3, ...
                'defaultColor', [0 0.4470 0.7410] ...
            );
        end
        
        function initializeColorMap(obj)
            obj.colorMap = containers.Map();
            obj.colorMap('voltage') = [0.8500 0.3250 0.0980];
            obj.colorMap('current') = [0.9290 0.6940 0.1250];
            obj.colorMap('temperature') = [0.4940 0.1840 0.5560];
            obj.colorMap('efficiency') = [0.4660 0.6740 0.1880];
            obj.colorMap('degradation') = [0.6350 0.0780 0.1840];
        end
        
        function initializePlotStyles(obj)
            obj.plotStyles = struct(...
                'normal', struct('LineStyle', '-', 'LineWidth', obj.config.lineWidth), ...
                'warning', struct('LineStyle', '--', 'LineWidth', obj.config.lineWidth), ...
                'error', struct('LineStyle', ':', 'LineWidth', obj.config.lineWidth*1.5) ...
            );
        end
        
        function fig = getFigure(obj, name)
            if ~obj.figures.isKey(name)
                fig = figure('Name', name, ...
                           'NumberTitle', 'off', ...
                           'Position', [100 100 obj.config.figureSize]);
                obj.figures(name) = fig;
            else
                fig = obj.figures(name);
                figure(fig); % Make current figure
            end
        end
        
        % Individual plotting methods...
        function plotEfficiency(obj, fig, data)
            subplot(2,2,1);
            plot(data.time, data.efficiency, ...
                 'Color', obj.colorMap('efficiency'), ...
                 obj.plotStyles.normal);
            title('Efficiency Over Time');
            xlabel('Time');
            ylabel('Efficiency (%)');
            grid on;
            alpha(obj.config.gridAlpha);
        end
        
        function plotThermal(obj, fig, data)
            subplot(2,2,2);
            plot(data.time, data.temperature, ...
                 'Color', obj.colorMap('temperature'), ...
                 obj.plotStyles.normal);
            title('Thermal Performance');
            xlabel('Time');
            ylabel('Temperature (°C)');
            grid on;
            alpha(obj.config.gridAlpha);
        end
        

    methods (Access = private)
        function plotDegradation(obj, fig, data)
            subplot(2,2,3);
            
            % Plot capacity loss
            yyaxis left
            plot(data.time, data.capacityLoss, ...
                 'Color', obj.colorMap('degradation'), ...
                 obj.plotStyles.normal);
            ylabel('Capacity Loss (%)');
            
            % Plot impedance increase on secondary axis
            yyaxis right
            plot(data.time, data.impedanceIncrease, '--', ...
                 'Color', obj.colorMap('degradation'), ...
                 obj.plotStyles.normal);
            ylabel('Impedance Increase (%)');
            
            title('Battery Degradation');
            xlabel('Time');
            grid on;
            alpha(obj.config.gridAlpha);
            legend('Capacity Loss', 'Impedance Increase', 'Location', 'northwest');
        end
        
        function plotOverview(obj, fig, data)
            subplot(2,2,4);
            
            % Create a multi-parameter overview plot
            parameters = {'SOC', 'Efficiency', 'Temperature', 'Health'};
            values = [data.soc, data.efficiency, ...
                     data.temperature/100, data.health]; % Normalize temperature
            
            % Create radar chart
            angles = linspace(0, 2*pi, length(parameters)+1);
            values = [values values(1)]; % Close the polygon
            
            polarplot(angles, values, 'LineWidth', obj.config.lineWidth, ...
                     'Color', obj.config.defaultColor);
            thetaticks(0:360/length(parameters):360);
            thetaticklabels(parameters);
            
            title('System Overview');
            rlim([0 1]);
        end
        
        function plotVoltage(obj, fig, data)
            subplot(3,1,1);
            
            % Plot voltage data
            plot(data.time, data.voltage, ...
                 'Color', obj.colorMap('voltage'), ...
                 obj.plotStyles.normal);
            
            % Add safety limits
            hold on;
            yline(4.2, '--r', 'Max Voltage');
            yline(2.5, '--r', 'Min Voltage');
            hold off;
            
            title('Battery Voltage');
            xlabel('Time');
            ylabel('Voltage (V)');
            grid on;
            alpha(obj.config.gridAlpha);
            
            % Add dynamic annotations
            if ~isempty(data.voltage)
                text(data.time(end), data.voltage(end), ...
                     sprintf(' %.2fV', data.voltage(end)), ...
                     'VerticalAlignment', 'bottom');
            end
        end
        
        function plotCurrent(obj, fig, data)
            subplot(3,1,2);
            
            % Plot current data
            plot(data.time, data.current, ...
                 'Color', obj.colorMap('current'), ...
                 obj.plotStyles.normal);
            
            % Add charging/discharging indicators
            hold on;
            charging = data.current > 0;
            discharging = data.current < 0;
            
            if any(charging)
                plot(data.time(charging), data.current(charging), 'g.', ...
                     'MarkerSize', obj.config.markerSize);
            end
            if any(discharging)
                plot(data.time(discharging), data.current(discharging), 'r.', ...
                     'MarkerSize', obj.config.markerSize);
            end
            hold off;
            
            title('Battery Current');
            xlabel('Time');
            ylabel('Current (A)');
            grid on;
            alpha(obj.config.gridAlpha);
            legend('Current', 'Charging', 'Discharging', 'Location', 'best');
        end
        
        function plotTemperature(obj, fig, data)
            subplot(3,1,3);
            
            % Plot temperature data with thermal zones
            temp = data.temperature;
            time = data.time;
            
            % Create colored thermal zones
            hold on;
            fill([time(1) time(end) time(end) time(1)], ...
                 [45 45 60 60], 'r', 'FaceAlpha', 0.1, 'EdgeColor', 'none');
            fill([time(1) time(end) time(end) time(1)], ...
                 [35 35 45 45], 'y', 'FaceAlpha', 0.1, 'EdgeColor', 'none');
            fill([time(1) time(end) time(end) time(1)], ...
                 [15 15 35 35], 'g', 'FaceAlpha', 0.1, 'EdgeColor', 'none');
            
            % Plot temperature line
            plot(time, temp, 'Color', obj.colorMap('temperature'), ...
                 'LineWidth', obj.config.lineWidth);
            hold off;
            
            title('Battery Temperature');
            xlabel('Time');
            ylabel('Temperature (°C)');
            ylim([0 60]);
            grid on;
            alpha(obj.config.gridAlpha);
            
            % Add zone labels
            text(time(end), 50, 'Danger', 'Color', 'r', ...
                 'HorizontalAlignment', 'right');
            text(time(end), 40, 'Warning', 'Color', [0.8 0.8 0]);
            text(time(end), 25, 'Optimal', 'Color', 'g');
        end
        
        function plotAllMetrics(obj, fig, data)
            % Clear figure
            clf(fig);
            
            % Create subplot grid
            subplot(2,2,1);
            obj.plotVoltage(fig, data);
            
            subplot(2,2,2);
            obj.plotCurrent(fig, data);
            
            subplot(2,2,3);
            obj.plotTemperature(fig, data);
            
            subplot(2,2,4);
            obj.plotOverview(fig, data);
            
            % Adjust layout
            sgtitle('Battery Management System - Real-time Metrics');
            set(fig, 'Color', 'white');
            set(findall(fig,'-property','FontSize'), 'FontSize', obj.config.fontSize);
        end
        
        function plotHealthIndicators(obj, fig, data)
            % Create new figure for health indicators
            figure(fig);
            clf;
            
            % SOH Plot
            subplot(2,2,1);
            obj.plotSOH(data);
            
            % Capacity Retention Plot
            subplot(2,2,2);
            obj.plotCapacityRetention(data);
            
            % Impedance Growth Plot
            subplot(2,2,3);
            obj.plotImpedanceGrowth(data);
            
            % Cycle Life Plot
            subplot(2,2,4);
            obj.plotCycleLife(data);
            
            % Adjust layout
            sgtitle('Battery Health Indicators');
            set(fig, 'Color', 'white');
        end
        
        function plotSOH(obj, data)
            bar(data.soh, 'FaceColor', obj.config.defaultColor);
            title('State of Health');
            ylabel('SOH (%)');
            ylim([0 100]);
            grid on;
        end
        
        function plotCapacityRetention(obj, data)
            plot(data.time, data.capacityRetention, ...
                 'Color', obj.colorMap('degradation'), ...
                 obj.plotStyles.normal);
            title('Capacity Retention');
            ylabel('Retention (%)');
            grid on;
        end
        
        function plotImpedanceGrowth(obj, data)
            plot(data.time, data.impedanceGrowth, ...
                 'Color', obj.colorMap('degradation'), ...
                 obj.plotStyles.normal);
            title('Impedance Growth');
            ylabel('Growth (%)');
            grid on;
        end
        
        function plotCycleLife(obj, data)
            plot(data.cycles, data.cycleCapacity, ...
                 'Color', obj.colorMap('efficiency'), ...
                 obj.plotStyles.normal);
            title('Cycle Life');
            xlabel('Cycle Number');
            ylabel('Capacity (%)');
            grid on;
        end
    end
end


    
