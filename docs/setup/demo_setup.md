# EkoVen Demo Setup Guide

## Overview
EkoVen is an EV for physically challenged with battery management system that uses ML to predict battery life and optimize performance. This demo showcases the core functionality.

## Prerequisites
- MATLAB R2021b or later
- .NET 6.0 SDK
- Visual Studio 2022 or VS Code
- Git

## Quick Start

1) Clone the repository:

git clone https://github.com/GautamPataskar/Ekoven.git
cd Ekoven

2) Set up MATLAB components:

cd simulation/matlab

Add the project folders to MATLAB path
addpath('bms_models')
addpath('tests')

3) run MATLAB tests

cd tests
run battery_tests.m

4) build and test ML components

cd ../../cloud/src/EkoVen.ML
dotnet build
dotnet test ../../tests/EkoVen.ML.Tests




## Demo Features
1. Battery State Optimization
   - Thermal management
   - Charging optimization
   - Safety monitoring

2. ML-based Prediction
   - Remaining life estimation
   - Performance optimization
   - Real-time monitoring

## Expected Results
- MATLAB tests will demonstrate battery optimization algorithms
- ML tests will show prediction capabilities
- You should see test results and optimization metrics in the console

## Troubleshooting
1. MATLAB Issues:
   - Ensure all paths are properly added
   - Check MATLAB version compatibility
   - Verify all required toolboxes are installed

2. .NET Issues:
   - Verify .NET 6.0 SDK installation
   - Run `dotnet --version` to confirm
   - Try cleaning solution: `dotnet clean`

## Contributing
Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## License
This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.

## Contact:
Gautam Pataskar - [@GautamPataskar](https://github.com/GautamPataskar)

Project Link: [https://github.com/GautamPataskar/Ekoven](https://github.com/GautamPataskar/Ekoven)


