using BWAPI.NET;

namespace Shared;

// library from https://www.nuget.org/packages/BWAPI.NET

public enum GameState
{
    IntialGame,
    EarlyGame,
    MidGame,
    LateGame,
}

public class MyStarcraftBot : DefaultBWListener
{
    private BWClient? _bwClient = null;
    public Game? Game => _bwClient?.Game;

    public bool IsRunning { get; private set; } = false;
    public bool InGame { get; private set; } = false;
    public int? GameSpeedToSet { get; set; } = null;

    public event Action? StatusChanged;

    private GameState currentGameState = GameState.IntialGame;
    private bool controlledByHuman = false;
    private MapTools mapTools = new MapTools();
    private int buildId = -1;
    private int textLine = 10;
    BuildSetting buildSetting = new BuildSetting();
    private TilePosition nextBuildLocation = new TilePosition(0, 0);

    DefenseNode currentDefenseNode = new DefenseNode(new TilePosition(0,0));

    #region start
    public void Connect()
    {
        _bwClient = new BWClient(this);
        var _ = Task.Run(() => _bwClient.StartGame());
        IsRunning = true;
        StatusChanged?.Invoke();
    }

    public void Disconnect()
    {
        if (_bwClient != null)
        {
            (_bwClient as IDisposable)?.Dispose();
        }
        _bwClient = null;
        IsRunning = false;
        InGame = false;
        StatusChanged?.Invoke();
    }

    // Bot Callbacks below
    public override void OnStart()
    {
        InGame = true;
        StatusChanged?.Invoke();
        Game?.EnableFlag(Flag.UserInput); // let human control too

        currentGameState = GameState.IntialGame;
        buildSetting = BuildSetting.GetSettings();
    }

    public override void OnEnd(bool isWinner)
    {
        InGame = false;
        StatusChanged?.Invoke();
    }
    #endregion

    public override void OnFrame()
    {
        if (Game == null)
            return;
        if (GameSpeedToSet != null)
        {
            Game.SetLocalSpeed(GameSpeedToSet.Value);
            GameSpeedToSet = null;
        }
        if (!mapTools.IsInitialized)
        {
            mapTools.Initialize(Game);
            currentDefenseNode = 
                mapTools.GetDefenseNodeAtTilePosition(
                mapTools.GetBuildLocationTowardsBaseAccess(
                    Game.Self().GetStartLocation()
                ));

        }
        textLine = 10;

        CheckBuilder();

        DrawDebugInfo();

        if (controlledByHuman)
            return;


        if (Game.GetFrameCount() % 3 == 0)
        {
            BackToWork();
        }

        if (currentGameState == GameState.IntialGame)
        {
            InitialGameLogic();
        }

        else if (currentGameState == GameState.EarlyGame)
        {
            EarlyGameLogic();
        }


    }

    private void BackToWork()
    {
        if (Game == null)
            return;

        var idleWorkers = Game.Self().GetUnits()
            .Where(u => u.GetUnitType().IsWorker() && u.IsIdle());
        foreach (var worker in idleWorkers)
        {
            if (worker.IsCarryingGas() || worker.IsCarryingMinerals())
            {
                worker.ReturnCargo();
            }
            var mineralPatch = Game.GetMinerals()
                .OrderBy(m => m.GetDistance(worker))
                .FirstOrDefault();
            if (mineralPatch != null)
            {
                worker.Gather(mineralPatch);
            }
        }
    }
    private void CheckBuilder()
    {
        if (Game == null)
            return;

        if (buildId != -1)
        {
            var builder = Tools.GetBuilder(Game, buildId);
            if (builder == null)
            {
                buildId = -1;
            }
        }
        if (buildId == -1)
        {
            var newBuilder = Game.Self().GetUnits()
                .FirstOrDefault(u => u.GetUnitType().IsWorker());
            if (newBuilder != null)
            {
                buildId = newBuilder.GetID();
            }
        }
    }

    #region logging

    private void LogToScreen(string message)
    {
        if (Game == null)
            return;

        Game.DrawTextScreen(10, textLine, message);
        textLine += 15;
    }
    private void DrawDebugInfo()
    {
        if (Game == null)
            return;

        mapTools.DrawGrid();

        var myUnits = Game.Self().GetUnits();
        foreach (var unit in myUnits)
        {
            if (unit.IsCompleted())
                Game.DrawTextMap(unit.GetPosition().X + 20, unit.GetPosition().Y,
                    $"{unit.GetID()}-{unit.GetHitPoints()}");
            else 
                Game.DrawTextMap(unit.GetPosition().X + 20, unit.GetPosition().Y + 10,
                    $"{unit.GetID()}-{unit.GetRemainingBuildTime()}");
        }

        LogToScreen($"Frame: {Game.GetFrameCount()}");
        LogToScreen($"Controlled by human: {controlledByHuman}");
        LogToScreen($"Game State: {currentGameState}");
        if (buildId != -1)
        {
            var builder = Game.Self().GetUnits()
                .FirstOrDefault(u => u.GetID() == buildId);
            LogToScreen($"Build Id: {buildId}; IsConstructing: {builder?.IsConstructing()}");
        }
        var buildTemp = nextBuildLocation.ToPosition();
        Game.DrawTextMap(buildTemp.X, buildTemp.Y, "Build Location");
        Game.DrawBoxMap(buildTemp.X, buildTemp.Y, buildTemp.X + 32, buildTemp.Y + 32,
            Color.Red, false);

        // var buildings = Game.Self().GetUnits().Where(u => u.GetUnitType().IsBuilding());
        // foreach (var building in buildings)
        // {
        //     LogToScreen($"{building.IsCompleted()}");
        // }
    }
    #endregion

    private void InitialGameLogic()
    {
        if (Game == null) return;
        LogToScreen("Initial Game Logic");

        var nexus = Game.Self().GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Nexus);
        if (nexus == null)
        {
            Console.WriteLine("No Nexus found!");
            return;
        }

        var builder = Tools.GetBuilder(Game, buildId);
        if (builder == null)
        {
            Console.WriteLine("No builder found!");
            return;
        }

        var probeCount = Tools.GetUnitCount(Game, UnitType.Protoss_Probe, true);
        LogToScreen($"Probes: {probeCount}");
        var forge = Tools.GetUnits(Game, UnitType.Protoss_Forge, true)
            .FirstOrDefault();
        var pylonsBuiltOrInProgress = Tools.GetUnits(Game, UnitType.Protoss_Pylon, true);
        LogToScreen($"Pylons: {pylonsBuiltOrInProgress.Count()}");

        if (probeCount < buildSetting.InitialFirstProbes)
        {
            LogToScreen($"Training Probes - Initial - Target: {buildSetting.InitialFirstProbes}");
            Tools.BuildProbe(Game, nexus);
        }
        else if (!pylonsBuiltOrInProgress.Any())
        {
            LogToScreen("Building Pylon - Initial");
            if (!builder.IsConstructing() && Tools.CanAfford(Game, UnitType.Protoss_Pylon))
            {
                nextBuildLocation = Tools.BuildPylon(Game, builder, currentDefenseNode);
            }
            else
            {
                LogToScreen($"Constructing: {builder.IsConstructing()}; CanAfford: {Tools.CanAfford(Game, UnitType.Protoss_Pylon)}");
            }
        }
        else if (probeCount < buildSetting.InitialProbesBeforeForge)
        {
            LogToScreen($"Training Probes - Before Forge - Target: {buildSetting.InitialProbesBeforeForge}");
            Tools.BuildProbe(Game, nexus);
        }
        else if (forge == null)
        {
            LogToScreen("Building Forge - Initial");
            if (pylonsBuiltOrInProgress.Any(p => p.IsCompleted()) &&
                !builder.IsConstructing() && Tools.CanAfford(Game, UnitType.Protoss_Forge))
            {
                var buildLocation = Tools.GetBuildLocationByPylon(Game, UnitType.Protoss_Forge,
                    pylonsBuiltOrInProgress.First(p => p.IsCompleted()));
                nextBuildLocation = buildLocation;
                builder.Build(UnitType.Protoss_Forge, buildLocation);
            }
        }
        else if (probeCount < buildSetting.InitialProbesToChangeState)
        {
            LogToScreen($"Training Probes - After Forge - Target: {buildSetting.InitialProbesToChangeState}");
            Tools.BuildProbe(Game, nexus);
        }
        else if (forge.IsCompleted() &&
            probeCount >= buildSetting.InitialProbesToChangeState)
        {
            currentGameState = GameState.EarlyGame;
        }
    }

    private void EarlyGameLogic()
    {
        if (Game == null) return;
        LogToScreen("EarlyGameLogic");

        var nexus = Game.Self().GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Nexus);
        if (nexus == null)
        {
            Console.WriteLine("No Nexus found!");
            return;
        }

        var builder = Tools.GetBuilder(Game, buildId);
        if (builder == null)
        {
            Console.WriteLine("No builder found!");
            return;
        }

        var probeCount = Tools.GetUnitCount(Game, UnitType.Protoss_Probe, true);
        LogToScreen($"Probes: {probeCount}");

        var pylonsCompleted = Tools.GetUnits(Game, UnitType.Protoss_Pylon);
        LogToScreen($"Pylons Completed: {pylonsCompleted.Count()}");

        var pylonsTotal = Tools.GetUnits(Game, UnitType.Protoss_Pylon, true);
        LogToScreen($"Pylons Total: {pylonsTotal.Count()}");

        var cannons = Tools.GetUnits(Game, UnitType.Protoss_Photon_Cannon, true);
        LogToScreen($"Cannons: {cannons.Count()}");

        var gateways = Tools.GetUnits(Game, UnitType.Protoss_Gateway, true);
        LogToScreen($"Gateways: {gateways.Count()}");

        if (cannons.Count() < buildSetting.EarlyCannonThreshold)
        {
            LogToScreen("Building Cannons - Early Game");
            if (!builder.IsConstructing() && Tools.CanAfford(Game, UnitType.Protoss_Photon_Cannon))
            {
                var buildLocation = Tools.GetBuildLocationByPylon(Game,
                    UnitType.Protoss_Photon_Cannon, pylonsCompleted.First(), 0);
                nextBuildLocation = buildLocation;
                builder.Build(UnitType.Protoss_Photon_Cannon, buildLocation);
            }
        }
        else if (probeCount < buildSetting.EarlyGameProbes)
        {
            LogToScreen($"Training Probes - Early Game - Target: {buildSetting.EarlyGameProbes}");
            Tools.BuildProbe(Game, nexus);
        }
        else if (gateways.Count() < buildSetting.EarlyGatewayThreshold)
        {
            LogToScreen("Building Gateways - Early Game");
            
            if (!builder.IsConstructing() && Tools.CanAfford(Game, UnitType.Protoss_Gateway))
            {
                var buildLocation = Tools.GetBuildLocationByPylon(Game,
                    UnitType.Protoss_Gateway, pylonsCompleted.First(), 1);
                nextBuildLocation = buildLocation;
                builder.Build(UnitType.Protoss_Gateway, buildLocation);
            }
        }
        else if (pylonsTotal.Count()< cannons.Count()/4)
        {
            LogToScreen("Building Pylon - Early Game");
            if (!builder.IsConstructing() && Tools.CanAfford(Game, UnitType.Protoss_Pylon))
            {
                currentDefenseNode = mapTools.GetEmptyDefenseNodeNextToPopulatedGrid();
                nextBuildLocation = Tools.BuildPylon(Game, builder, currentDefenseNode);
            }
            else
            {
                LogToScreen($"Constructing: {builder.IsConstructing()}; CanAfford: {Tools.CanAfford(Game, UnitType.Protoss_Pylon)}");
            }
        }

        else
        {
            LogToScreen("Build cannons.");
            if (!builder.IsConstructing() && Tools.CanAfford(Game, UnitType.Protoss_Photon_Cannon))
            {
                var buildLocation = Tools.GetBuildLocationByPylonInNode(Game,
                    UnitType.Protoss_Photon_Cannon, currentDefenseNode, 0);
                nextBuildLocation = buildLocation;
                builder.Build(UnitType.Protoss_Photon_Cannon, buildLocation);
            }
        }

    }

    #region other

    public override void OnUnitComplete(Unit unit) { }

    public override void OnUnitDestroy(Unit unit) { }

    public override void OnUnitMorph(Unit unit) { }

    public override void OnSendText(string text)
    {

        if (text.Contains("/control"))
        {
            controlledByHuman = !controlledByHuman;
        }
        if (text.Contains("/dud"))
        {
            currentDefenseNode.IsDud = true;
        }

    }

    public override void OnReceiveText(Player player, string text) { }

    public override void OnPlayerLeft(Player player) { }

    public override void OnNukeDetect(Position target) { }

    public override void OnUnitEvade(Unit unit) { }

    public override void OnUnitShow(Unit unit) { }

    public override void OnUnitHide(Unit unit) { }

    public override void OnUnitCreate(Unit unit) { }

    public override void OnUnitRenegade(Unit unit) { }

    public override void OnSaveGame(string gameName) { }

    public override void OnUnitDiscover(Unit unit) { }

    #endregion


}
