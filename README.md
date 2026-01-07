# Realtime Water Simulation
This unity package implements a iterative shallow water equation solver to simulate water flow in realtime.

TODO: Insert demo video here

## Usage
The package provides three main components:
- WaterSimulator
- WaterSimulatorManipulator
- WaterSimulatroFloater
<!-- TODO continue explanation -->

## Setup
Installation using the Package Manager:
1. Click on the `+` in the `Package Manager` window
2. Chose `Add package from git URL...`
3. Insert the following URL `https://github.com/JonasWischeropp/unity-realtime-water-simulation.git`  
A specific [release](https://github.com/JonasWischeropp/unity-realtime-water-simulation/releases) version can be specified by appending `#<version>` (e.g. `...lation.git#0.0.0`).
4. Press the `Add`-Button

# TODO
- check license of water normal texture
- remove test files
- write README
- document/prevent wrong layer (depth camera) setup
- default values

## Shader
- "Default" refraction of water (at the moment not breaking at surface)
- Shadow
- Refraction always to the right
