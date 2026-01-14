

## First Steps

Clone this repository. Make sure to give your personal repository a cool name to represent your bot.

Run the `setup.ps1` script. This will download and extract a working version of StarCraft Broodwar with the required BWAPI extensions. Run the `Web` project and click start game. A game should automatically start and you should see the words "Hello Bot" on the screen.


## Links and resources 

- [BWAPI Wiki](https://bwapi.github.io/)
- [BWAPI.NET](https://github.com/acoto87/bwapi.net) C# library this repo uses (some starting code available here)
- [Broodwar Bot Community Wiki](https://www.starcraftai.com/wiki/Main_Page)
- [C# map analysis library](https://github.com/acoto87/bwem.net)
- [Student Starcraft AI Tournament and Ladder](https://sscaitournament.com/)
- [Basil Bot Ladder](https://www.basil-ladder.net)

## Zero-to-hero getting started videos

Dave Churchill made two youtube videos as part of this "AI for Video Games" college course. These are good breakdowns of the Broodwar game. The most important parts are the second half of each video. You can also check out [his git repo](https://github.com/davechurchill/STARTcraftle) and read that code for ideas

### Dave Churchill Intro to StarCraft

Explanation of starcraft for somebody who has never played a Real Time Strategy game before.

- [00:00](https://www.youtube.com/watch?v=czhNqUxmLks) - Introduction / Links
- [02:13](https://www.youtube.com/watch?v=czhNqUxmLks&t=133s) - What is Starcraft / RTS?
- [05:23](https://www.youtube.com/watch?v=czhNqUxmLks&t=323s) - Basic RTS Strategies
- [08:31](https://www.youtube.com/watch?v=czhNqUxmLks&t=511s) - Starcraft Races
- [10:28](https://www.youtube.com/watch?v=czhNqUxmLks&t=628s) - Terran Overview
- [13:06](https://www.youtube.com/watch?v=czhNqUxmLks&t=786s) - Protoss Overview
- [15:26](https://www.youtube.com/watch?v=czhNqUxmLks&t=926s) - Zerg Overview

Basic game mechanics

- [18:37](https://www.youtube.com/watch?v=czhNqUxmLks&t=1117s) - Example Game Scenario
- [20:55](https://www.youtube.com/watch?v=czhNqUxmLks&t=1255s) - What is a Build Order?
- [23:19](https://www.youtube.com/watch?v=czhNqUxmLks&t=1399s) - Starcraft Game Play Demo / Tutorial
- [36:48](https://www.youtube.com/watch?v=czhNqUxmLks&t=2208s) - Build Order Considerations

Talking about BroodWar API (BWAPI) - he uses the C++ version, all the classes, functions, and data types are the same on the C# version

- [38:32](https://www.youtube.com/watch?v=czhNqUxmLks&t=2312s) - BWAPI Introduction
- [41:35](https://www.youtube.com/watch?v=czhNqUxmLks&t=2495s) - STARTcraft Github Project (starter code project)

Start here if you already know about StarCraft. Goes over how to control units with code.

- [44:08](https://www.youtube.com/watch?v=czhNqUxmLks&t=2648s) - Starcraft Unit Commands in BWAPI
- [49:34](https://www.youtube.com/watch?v=czhNqUxmLks&t=2974s) - Starcraft Unit Properties
- [52:53](https://www.youtube.com/watch?v=czhNqUxmLks&t=3173s) - BWAPI Important Classes

Getting your economy started

- [59:40](https://www.youtube.com/watch?v=czhNqUxmLks&t=3580s) - BWAPI Resource Gathering Code

GAmeplay overview of building an army and attacking

- [1:07:49](https://www.youtube.com/watch?v=czhNqUxmLks&t=4069s) - Starcraft Army Composition
- [1:08:23](https://www.youtube.com/watch?v=czhNqUxmLks&t=4103s) - Starcraft Tech Tree
- [1:10:42](https://www.youtube.com/watch?v=czhNqUxmLks&t=4242s) - Starcraft Maps: Chokepoints, Expansions, and Islands
- [1:15:18](https://www.youtube.com/watch?v=czhNqUxmLks&t=4518s) - Fog of War + Invisible Units
- [1:17:09](https://www.youtube.com/watch?v=czhNqUxmLks&t=4629s) - Starcraft Base Progression

Learning more about starcraft maps on a technical level

- [1:19:43](https://www.youtube.com/watch?v=czhNqUxmLks&t=4783s) - Starcraft Grid / Positioning Systems
- [1:31:26](https://www.youtube.com/watch?v=czhNqUxmLks&t=5486s) - Map Analysis Libraries
- [1:32:32](https://www.youtube.com/watch?v=czhNqUxmLks&t=5552s) - BWAPI Example Scouting Code

Bot architecture ideas

- [1:37:42](https://www.youtube.com/watch?v=czhNqUxmLks&t=5862s) - Starcraft AI Combat Note
- [1:39:49](https://www.youtube.com/watch?v=czhNqUxmLks&t=5989s) - Starcraft AI Bot Logic Flow
- [1:40:11](https://www.youtube.com/watch?v=czhNqUxmLks&t=6011s) - STARTcraft Demo

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
