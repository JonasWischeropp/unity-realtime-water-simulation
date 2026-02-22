# Realtime Water Simulation
This Unity package implements an iterative shallow water equation solver to simulate water flow in realtime.

> [!IMPORTANT]
> WIP: Parts of the simulation and API might not be fully stable yet!

TODO: Insert demo video here

## Usage
The package provides three main components:
| Component         | Function |
|-|-|
| Water Simulator   | simulates water in an area |
| Water Manipulator | used to add/remove water |
| Water Floater     | lets rigidbodys interact with the water |

The [sample scene](#sample-scene) is probably the best documentation and should explain everything.

### Sample Scene
For `SampleScene.unity` to work, the free asset pack [Rock and Boulders 2 by Manufactura K4](https://assetstore.unity.com/packages/3d/props/exterior/rock-and-boulders-2-6947) has to be imported and converted to URP.

## Setup
Installation using the Package Manager:
1. Click on the `+` in the `Package Manager` window
2. Chose `Add package from git URL...`
3. Insert the following URL `https://github.com/JonasWischeropp/unity-realtime-water-simulation.git`  
A specific [release](https://github.com/JonasWischeropp/unity-realtime-water-simulation/releases) version can be specified by appending `#<version>` (e.g. `...lation.git#0.0.0`).
4. Press the `Add`-Button

## Credit
- Large parts of the numeric calculations are based on "A fast second-order shallow water scheme on two-dimensional structured grids over abrupt topography" ([Buttinger-Kreuzhuber et al.](https://doi.org/10.1016/j.advwatres.2019.03.010.))
- The water shader uses a lot of techniques from tutorials by [Ben Cloward](https://www.youtube.com/@BenCloward)
