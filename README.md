

## First Steps

Clone this repository. Make sure to give your personal repository a cool name to represent your bot.

Run the `setup.ps1` script. This will download and extract a working version of StarCraft Broodwar with the required BWAPI extensions. Run the `Web` project and click start game. A game should automatically start and you should see the words "Hello Bot" on the screen.


## Important links and resources 

- [BWAPI Wiki](https://bwapi.github.io/)
- [BWAPI.NET](https://github.com/acoto87/bwapi.net) C# library this repo uses (some starting code available here)
- [Broodwar Bot Community Wiki](https://www.starcraftai.com/wiki/Main_Page)

## Zero-to-hero getting started videos

Dave Churchill made two youtube videos as part of this "AI for Video Games" college course. These are good breakdowns of the Broodwar game. The most important parts are the second half of each video. You can also check out [his git repo](https://github.com/davechurchill/STARTcraftle) and read that code for ideas

### Dave Churchill Intro to StarCraft

Explanation of starcraft for somebody who has never played a Real Time Strategy game before.

- [00:00 - Introduction / Links](https://www.youtube.com/watch?v=czhNqUxmLks)
- [02:13 - What is Starcraft / RTS?](https://www.youtube.com/watch?v=czhNqUxmLks&t=133s)
- [05:23 - Basic RTS Strategies](https://www.youtube.com/watch?v=czhNqUxmLks&t=323s)
- [08:31 - Starcraft Races](https://www.youtube.com/watch?v=czhNqUxmLks&t=511s)
- [10:28 - Terran Overview](https://www.youtube.com/watch?v=czhNqUxmLks&t=628s)
- [13:06 - Protoss Overview](https://www.youtube.com/watch?v=czhNqUxmLks&t=786s)
- [15:26 - Zerg Overview](https://www.youtube.com/watch?v=czhNqUxmLks&t=926s)

Basic game mechanics

- [18:37 - Example Game Scenario](https://www.youtube.com/watch?v=czhNqUxmLks&t=1117s)
- [20:55 - What is a Build Order?](https://www.youtube.com/watch?v=czhNqUxmLks&t=1255s)
- [23:19 - Starcraft Game Play Demo / Tutorial](https://www.youtube.com/watch?v=czhNqUxmLks&t=1399s)
- [36:48 - Build Order Considerations](https://www.youtube.com/watch?v=czhNqUxmLks&t=2208s)

Talking about BroodWar API (BWAPI) - he uses the C++ version, all the classes, functions, and data types are the same on the C# version

- [38:32 - BWAPI Introduction](https://www.youtube.com/watch?v=czhNqUxmLks&t=2312s)
- [41:35 - STARTcraft Github Project (starter code project)](https://www.youtube.com/watch?v=czhNqUxmLks&t=2495s)

Start here if you already know about StarCraft. Goes over how to control units with code.

- [44:08 - Starcraft Unit Commands in BWAPI](https://www.youtube.com/watch?v=czhNqUxmLks&t=2648s)
- [49:34 - Starcraft Unit Properties](https://www.youtube.com/watch?v=czhNqUxmLks&t=2974s)
- [52:53 - BWAPI Important Classes](https://www.youtube.com/watch?v=czhNqUxmLks&t=3173s)

Getting your economy started

- [59:40 - BWAPI Resource Gathering Code](https://www.youtube.com/watch?v=czhNqUxmLks&t=3580s)

GAmeplay overview of building an army and attacking

- [1:07:49 - Starcraft Army Composition](https://www.youtube.com/watch?v=czhNqUxmLks&t=4069s)
- [1:08:23 - Starcraft Tech Tree](https://www.youtube.com/watch?v=czhNqUxmLks&t=4103s)
- [1:10:42 - Starcraft Maps: Chokepoints, Expansions, and Islands](https://www.youtube.com/watch?v=czhNqUxmLks&t=4242s)
- [1:15:18 - Fog of War + Invisible Units](https://www.youtube.com/watch?v=czhNqUxmLks&t=4518s)
- [1:17:09 - Starcraft Base Progression](https://www.youtube.com/watch?v=czhNqUxmLks&t=4629s)

Learning more about starcraft maps on a technical level

- [1:19:43 - Starcraft Grid / Positioning Systems](https://www.youtube.com/watch?v=czhNqUxmLks&t=4783s)
- [1:31:26 - Map Analysis Libraries](https://www.youtube.com/watch?v=czhNqUxmLks&t=5486s)
- [1:32:32 - BWAPI Example Scouting Code](https://www.youtube.com/watch?v=czhNqUxmLks&t=5552s)

Bot architecture ideas

- [1:37:42 - Starcraft AI Combat Note](https://www.youtube.com/watch?v=czhNqUxmLks&t=5862s)
- [1:39:49 - Starcraft AI Bot Logic Flow](https://www.youtube.com/watch?v=czhNqUxmLks&t=5989s)
- [1:40:11 - STARTcraft Demo](https://www.youtube.com/watch?v=czhNqUxmLks&t=6011s)

### Dave Churchill Broodwar AI Programming Tutorial

Going over his starter repo
- [00:00](https://www.youtube.com/watch?v=FEEkO6__GKw&t=0s) — Introduction  
- [02:39](https://www.youtube.com/watch?v=FEEkO6__GKw&t=159s) — STARTcraft GitHub Project  
- [04:20](https://www.youtube.com/watch?v=FEEkO6__GKw&t=260s) — StarterBot Setup and Run  
- [08:12](https://www.youtube.com/watch?v=FEEkO6__GKw&t=492s) — Compiling StarterBot in Visual Studio  
- [09:03](https://www.youtube.com/watch?v=FEEkO6__GKw&t=543s) — How Starcraft Bots Work (Client Architecture / DLL Injection)  

How to configure BWAPI

- [15:29](https://www.youtube.com/watch?v=FEEkO6__GKw&t=929s) — BWAPI Settings  

Going over BWAPI events

- [27:50](https://www.youtube.com/watch?v=FEEkO6__GKw&t=1670s) — `main.cpp` (Connecting to Starcraft and BWAPI Events)  
- [37:26](https://www.youtube.com/watch?v=FEEkO6__GKw&t=2246s) — StarterBot Class Architecture, First Lines of Code  
- [38:41](https://www.youtube.com/watch?v=FEEkO6__GKw&t=2321s) — `onStart()` (Game Speed / Options)  
- [44:52](https://www.youtube.com/watch?v=FEEkO6__GKw&t=2692s) — `onEnd()` (Printing Who Won the Game)  
- [46:03](https://www.youtube.com/watch?v=FEEkO6__GKw&t=2763s) — `onUnitEvents()` (Triggered Event Functions)  
- [50:31](https://www.youtube.com/watch?v=FEEkO6__GKw&t=3031s) — `onFrame()` (Main Game Loop)  

Code to start gathering resources and putting information on the StarCraft game for debugging and feedback

- [53:53](https://www.youtube.com/watch?v=FEEkO6__GKw&t=3233s) — Sending Workers to Minerals (Unit, Game, Player Classes)
- [1:04:54](https://www.youtube.com/watch?v=FEEkO6__GKw&t=3894s) — Printing Unit IDs / Fog of War  
- [1:10:39](https://www.youtube.com/watch?v=FEEkO6__GKw&t=4239s) — Drawing Shapes on the Map  
- [1:15:04](https://www.youtube.com/watch?v=FEEkO6__GKw&t=4504s) — Actually Sending Workers to Minerals  
- [1:18:18](https://www.youtube.com/watch?v=FEEkO6__GKw&t=4698s) — Training Additional Workers  
- [1:26:50](https://www.youtube.com/watch?v=FEEkO6__GKw&t=5210s) — `onUnitCreate()` Event Example  

Building buildings and going over some pitfalls to avoid

- [1:28:53](https://www.youtube.com/watch?v=FEEkO6__GKw&t=5333s) — Constructing Buildings  
- [1:38:28](https://www.youtube.com/watch?v=FEEkO6__GKw&t=5908s) — Edge Cases: Units in Progress  
- [1:44:46](https://www.youtube.com/watch?v=FEEkO6__GKw&t=6286s) — Preventing Duplicate Unit Commands  

Drawing debug map information

- [1:50:35](https://www.youtube.com/watch?v=FEEkO6__GKw&t=6635s) — Map Tools  
- [1:53:48](https://www.youtube.com/watch?v=FEEkO6__GKw&t=6828s) — Sending a Unit to Scout  
- [2:01:31](https://www.youtube.com/watch?v=FEEkO6__GKw&t=7291s) — UAlbertaBot GitHub / Outro  
