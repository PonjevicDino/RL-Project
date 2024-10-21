# Hill Climb Racing
This repository was created for the semesterproject in the second semester of the Interactive Media master at the University of Applied Sciences Media Department in Hagenberg.

Additional Settings: 
- 20 Physics frames per Second
- Agent Desicion Period: 15 Physics Frames = 1 Agent Frames
The agent currently works with the following principles:
- If agent dies: Reset Car Position of Agent to start instead of reloading scene (would reset ML-Agent) -> Unloading & Reloading Terrain (Resetting Gamemanager manually)
- Agent can use the following:
  - Fuel state
  - Fuel consumption
  - Current Velocity (normalized)
  - Velocity history (last 5 Physics frames)
  - Current Rotation
  - Trajectory (Landing point calculation: Terrain-Angle, Distance to Landing Point)
  - Coins (Distance to Coin, Angle to next Coin)
- Rewards:
  - for each 10 meters: Reward of 10 * Speedbonusfactor
  - based on fuel state: (1 - CurrentFuel)/10000
  - based on collected coins: CollectedCoinsValue/100
  - based on Acceleration: if hold for 3 Agent-Frames
- Punishment:
  - Driving backward: 0.05 per Physics Frame
- Episode End Conditons:
  - Fuel Empty
  - Head Hit (Neck broken)
  - Underneath Terrain (Error)
  - Reaches Goal (9900) -> Reward = 1000

This project is ... Work in Progress!

Changes compared to original: Procedural Terrain Generation with Customizable Fourier-Transformation, Main Menu Changes, Moon Map, ML-Agent added
Project adjusted from original source: https://github.com/choijoohee213/HillClimbRacing
[![Video Label](http://img.youtube.com/vi/TMuYO-Zndus/0.jpg)](https://www.youtube.com/watch?v=TMuYO-Zndus)
