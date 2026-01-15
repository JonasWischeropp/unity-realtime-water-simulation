# Realtime Water Simulation
This unity package implements an iterative shallow water equation solver to simulate water flow in realtime.

TODO: Insert demo video here

## Usage
The package provides three main components:
- WaterSimulator
- WaterSimulatorManipulator
- WaterSimulatroFloater
<!-- TODO continue explanation -->

### Sample Scene
For `SampleScene.unity` to work, the free asset pack [Rock and Boulders 2 by Manufactura K4](https://assetstore.unity.com/packages/3d/props/exterior/rock-and-boulders-2-6947) has to be imported into the project and converted to URP.

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
- default values

## Shader
- "Default" refraction of water (at the moment not breaking at surface)
- Shadow
- Refraction always to the right
