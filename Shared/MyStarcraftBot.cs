using BWAPI.NET;

namespace Shared;

// library from https://www.nuget.org/packages/BWAPI.NET

public enum GameState
{
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

    private GameState currentGameState = GameState.EarlyGame;
    private bool controlledByHuman = false;
    private MapTools mapTools = new MapTools();
    private int buildId = -1;
    private int textLine = 10;

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

        currentGameState = GameState.EarlyGame;
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
        }
        textLine = 10;

        if (buildId == -1)
        {
            var probes = Game.Self().GetUnits().Where(u => u.GetUnitType() == UnitType.Protoss_Probe);
            if (probes.Any())
            {
                buildId = probes.First().GetID();
            }
        }

        DrawDebugInfo();

        if (controlledByHuman)
            return;


        if (Game.GetFrameCount() % 3 == 0)
        {
            BackToWork();
        }

        if (currentGameState == GameState.EarlyGame)
        {
            // Ramp up probes
            var nexus = Game.Self().GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Nexus);
            var probeCount = Game.Self().GetUnits().Count(u => u.GetUnitType() == UnitType.Protoss_Probe);
            if (nexus != null && probeCount < 12 && !nexus.IsTraining()
                && UnitType.Protoss_Probe.MineralPrice() <= Game.Self().Minerals())
            {
                nexus.Train(UnitType.Protoss_Probe);
                return;
            }

            var pylonsBuiltOrInProgress = Game.Self().GetUnits()
                .Where(u => u.GetUnitType() == UnitType.Protoss_Pylon
                && (u.IsCompleted() || u.IsBeingConstructed()));
            var builder = Game.Self().GetUnits()
                .FirstOrDefault(u => u.GetID() == buildId);
            if (!pylonsBuiltOrInProgress.Any() && probeCount >= 8 &&
                builder != null && !builder.IsConstructing())
            {
                // Build first pylon
                if (builder != null && !builder.IsConstructing()
                    && UnitType.Protoss_Pylon.MineralPrice() <= Game.Self().Minerals())
                {
                    var location = mapTools.GetBuildLocationTowardsBaseAccess(
                        Game.Self().GetStartLocation());
                    var buildLocation = Game.GetBuildLocation(
                        UnitType.Protoss_Pylon, location, 5, false);
                    builder.Build(UnitType.Protoss_Pylon, buildLocation);
                    return;
                }
            }
            var forge = Game.Self().GetUnits()
                .FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Forge);
            if (pylonsBuiltOrInProgress.Any() &&
                pylonsBuiltOrInProgress.First().IsCompleted() &&
                probeCount >= 12 && forge == null &&
                builder != null && !builder.IsConstructing())
            {
                if (builder != null && !builder.IsConstructing()
                    && UnitType.Protoss_Forge.MineralPrice() <= Game.Self().Minerals())
                {
                    var location = pylonsBuiltOrInProgress.First().GetTilePosition();
                    var buildLocation = Game.GetBuildLocation(
                        UnitType.Protoss_Forge, location, 4, false);
                    builder.Build(UnitType.Protoss_Forge, buildLocation);
                    return;
                }
            }
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

    private void DrawDebugInfo()
    {
        if (Game == null)
            return;

        var myUnits = Game.Self().GetUnits();
        foreach (var unit in myUnits)
        {
            Game.DrawTextMap(unit.GetPosition().X + 20, unit.GetPosition().Y,
                $"{unit.GetID()}-{unit.GetHitPoints()}");
        }

        Game.DrawTextScreen(10, textLine, $"Frame: {Game.GetFrameCount()}");
        Game.DrawTextScreen(10, textLine + 15, $"Controlled by human: {controlledByHuman}");
        Game.DrawTextScreen(10, textLine + 30, $"Game State: {currentGameState}");
        textLine += 45;
        if (buildId != -1)
        {
            var builder = Game.Self().GetUnits()
                .FirstOrDefault(u => u.GetID() == buildId);
            Game.DrawTextScreen(10, textLine, $"Build Id: {buildId}; IsConstructing: {builder?.IsConstructing()}");
            textLine += 15;
        }

        // var buildings = Game.Self().GetUnits().Where(u => u.GetUnitType().IsBuilding());
        // foreach (var building in buildings)
        // {
        //     Game.DrawTextScreen(10, textLine, $"{building.IsCompleted()}");
        //     textLine += 15;
        // }
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
