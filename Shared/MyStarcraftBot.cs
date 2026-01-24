using BWAPI.NET;

namespace Shared;

// library from https://www.nuget.org/packages/BWAPI.NET

public class MyStarcraftBot : DefaultBWListener
{
    private BWClient? _bwClient = null;
    public Game? Game => _bwClient?.Game;

    public bool IsRunning { get; private set; } = false;
    public bool InGame { get; private set; } = false;
    public int? GameSpeedToSet { get; set; } = null;

    public event Action? StatusChanged;

    private int _builderProbeId = -1;
    private UnitType _currentBuildType = UnitType.None;
    private TilePosition _lastBuildLocation = new TilePosition(0, 0);

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
    }

    public override void OnEnd(bool isWinner)
    {
        InGame = false;
        StatusChanged?.Invoke();
    }

    private int _horizontalOffset = 10;
    public void LogToScreen(string message)
    {
        Game?.DrawTextScreen(10, _horizontalOffset, message);
        _horizontalOffset += 10;
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
        _horizontalOffset = 10;

        if (Game.GetFrameCount() % 3 == 0)
            GetBackToWork();

        LogToScreen($"Builder {_builderProbeId}, CurrentBuild {_currentBuildType}, BuildLocation {_lastBuildLocation.X}, {_lastBuildLocation.Y}");
        LabelThings();

        if (Game.GetFrameCount() % 5 == 0)
            MakeEnoughWorkers(7);

        if (Game.GetFrameCount() % 7 == 0)
            BuildStartingPylon();

        if (Game.GetFrameCount() % 11 == 0)
            BuildForgeIfNeeded();

        if (Game.GetFrameCount() % 2 == 0)
            BuildCannonOrPylon();

        if (Game.GetFrameCount() % 11 == 1)
        {
            var builder = GetBuilder();
            if (_currentBuildType != UnitType.None && builder != null && !builder.IsConstructing())
            {
                var buildingsBeingBuilt = Game.Self().GetUnits()
                    .Where(u => u.GetUnitType() == _currentBuildType && !u.IsCompleted());
                if (!buildingsBeingBuilt.Any(b => b.GetUnitType() == _currentBuildType))
                {
                    LogToScreen("Builder is not constructing, resetting build type");
                    _currentBuildType = UnitType.None;
                }

            }
        }

    }

    #region Logging
    private void LabelThings()
    {
        if (Game == null)
            throw new InvalidOperationException("Game is not initialized");
        Game.DrawTextMap(
            _lastBuildLocation.ToPosition().X,
            _lastBuildLocation.ToPosition().Y, "Build Location");

        if (_builderProbeId != -1)
        {
            var builder = Game.Self().GetUnits()
                .FirstOrDefault(u => u.GetID() == _builderProbeId);
            LogToScreen($"Action: {builder!.GetLastCommand()}; Target: {builder.GetLastCommand().GetTargetPosition().X}, {builder.GetLastCommand().GetTargetPosition().Y}; Frame: {builder.GetLastCommandFrame()}; IsConstructing: {builder.IsConstructing()}");
        }

        foreach (var unit in Game.GetAllUnits())
        {
            if (unit.GetPlayer().IsEnemy(Game.Self()))
            {
                Game.DrawTextMap(
                    unit.GetPosition().X,
                    unit.GetPosition().Y - 10,
                    $"Enemy {unit.GetUnitType()}");
            }
            else if (unit.GetPlayer() == Game.Self())
            {
                Game.DrawTextMap(
                    unit.GetPosition().X,
                    unit.GetPosition().Y - 10,
                    $"{unit.GetID()}");
            }

        }

    }

    #endregion

    #region CallBacks
    public override void OnUnitComplete(Unit unit) { }

    public override void OnUnitDestroy(Unit unit) { }

    public override void OnUnitMorph(Unit unit) { }

    public override void OnSendText(string text) { }

    public override void OnReceiveText(Player player, string text) { }

    public override void OnPlayerLeft(Player player) { }

    public override void OnNukeDetect(Position target) { }

    public override void OnUnitEvade(Unit unit) { }

    public override void OnUnitShow(Unit unit) { }

    public override void OnUnitHide(Unit unit) { }

    public override void OnUnitCreate(Unit unit)
    {
        if (unit.GetUnitType().IsBuilding() && unit.GetUnitType() == _currentBuildType)
        {
            _currentBuildType = UnitType.None;
        }
    }

    public override void OnUnitRenegade(Unit unit) { }

    public override void OnSaveGame(string gameName) { }

    public override void OnUnitDiscover(Unit unit) { }
    #endregion

    public void GetBackToWork()
    {
        if (Game == null)
            throw new InvalidOperationException("Game is not initialized");

        var idleWorkers = Game.Self().GetUnits()
            .Where(u => u.GetUnitType().IsWorker() && u.IsIdle());
        foreach (var worker in idleWorkers)
        {
            if (worker.IsCarryingGas() || worker.IsCarryingMinerals())
            {
                worker.ReturnCargo();
                continue;
            }

            var closestMineral = Game.GetAllUnits()
                .Where(u => u.GetUnitType().IsMineralField())
                .OrderBy(m => m.GetDistance(worker))
                .FirstOrDefault();
            if (closestMineral != null)
            {
                worker.Gather(closestMineral);
            }
        }
    }
    private void MakeEnoughWorkers(int desiredWorkerCount)
    {
        if (Game == null)
            throw new InvalidOperationException("Game is not initialized");

        var currentWorkers = Game.Self().GetUnits()
            .Where(u => u.GetUnitType().IsWorker()).ToList();
        if (currentWorkers.Count() < desiredWorkerCount && Game.Self().Minerals() >= 50)
        {
            var nexi = Game.Self().GetUnits()
                .Where(u => u.GetUnitType() == UnitType.Protoss_Nexus);
            if (nexi.Any(n => n.IsIdle()))
            {
                nexi.First(n => n.IsIdle()).Train(UnitType.Protoss_Probe);
            }
        }
    }

    private void BuildStartingPylon()
    {
        if (Game == null)
            throw new InvalidOperationException("Game is not initialized");

        var pylons = Game.Self().GetUnits()
            .Where(u => u.GetUnitType() == UnitType.Protoss_Pylon);
        if (!pylons.Any() && Game.Self().Minerals() >= 100 && _currentBuildType == UnitType.None)
        {
            var builder = GetBuilder();
            if (builder != null)
            {
                var location = Game.GetBuildLocation(
                    UnitType.Protoss_Pylon,
                    Game.Self().GetStartLocation(), 64, false);
                builder.Build(UnitType.Protoss_Pylon, location);
                _currentBuildType = UnitType.Protoss_Pylon;
                _lastBuildLocation = location;
                //Console.WriteLine("Building starting Pylon, " +Game.GetFrameCount());
            }
        }
    }


    private Unit GetBuilder()
    {
        if (Game == null)
            throw new InvalidOperationException("Game is not initialized");

        var builder = Game.Self().GetUnits()
            .FirstOrDefault(u => u.GetID() == _builderProbeId);
        if (_builderProbeId == -1 || builder == null)
        {
            builder = Game.Self().GetUnits()
                .FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Probe);
            if (builder != null)
            {
                _builderProbeId = builder.GetID();
            }
        }
        if (builder == null)
        {
            _builderProbeId = -1;
            throw new InvalidOperationException("No builder probe found");
        }
        return builder;
    }
    public void BuildForgeIfNeeded()
    {
        if (Game == null)
            throw new InvalidOperationException("Game is not initialized");
        if (HasForge())
            return;
        LogToScreen("Build Forge Check");

        if (Game.Self().Minerals() >= 150 && _currentBuildType == UnitType.None)
        {
            LogToScreen("Building Forge");
            var builder = GetBuilder();
            if (builder != null)
            {
                LogToScreen("Found builder for Forge");
                var buildPosition = PositionNearPylon();
                var location = Game.GetBuildLocation(
                    UnitType.Protoss_Forge,
                    buildPosition, 5, false);
                if (location.X < 0 || location.Y < 0 ||
                    location.X >= Game.MapWidth() || location.Y >= Game.MapHeight())
                {
                    LogToScreen("No valid location for Forge");
                    return;
                }
                Console.WriteLine("Building Forge at " + location.X + ", " + location.Y);
                builder.Build(UnitType.Protoss_Forge, location);
                _currentBuildType = UnitType.Protoss_Forge;
                _lastBuildLocation = location;
            }
            else
            {
                LogToScreen("No builder found for Forge");
            }
        }
    }

    public TilePosition PositionNearPylon()
    {
        if (Game == null)
            throw new InvalidOperationException("Game is not initialized");

        var pylon = Game.Self().GetUnits()
            .FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Pylon);
        if (pylon != null)
        {
            var pylonTilePos = pylon.GetTilePosition();
            return new TilePosition(pylonTilePos.X, pylonTilePos.Y);
        }
        else
        {
            return Game.Self().GetStartLocation();
        }
    }

    private void BuildCannonOrPylon()
    {
        if (Game == null)
            throw new InvalidOperationException("Game is not initialized");
        if (!HasForge()) return;
        if (Game.Self().Minerals() < 150) return;

        var pylons = Game.Self().GetUnits()
            .Where(u => u.GetUnitType() == UnitType.Protoss_Pylon && u.IsCompleted());

        foreach (var pylon in pylons)
        {
            var cannonsbyPylon = Game.Self().GetUnits()
                .Where(c => c.GetUnitType() == UnitType.Protoss_Photon_Cannon)
                .Where(c => c.GetDistance(pylon) < 4 * 32);
            // Console.WriteLine($"Pylon at {pylon.GetTilePosition().X}, {pylon.GetTilePosition().Y} has {cannonsbyPylon.Count()} Cannons");
            if (cannonsbyPylon.Count() < 5)
            {
                LogToScreen("Pylon has less than 5 Cannons");

                var builder = GetBuilder();
                if (builder != null)
                {
                    LogToScreen("Found builder for Pylon");
                    var pylonTilePos = pylon.GetTilePosition();
                    var buildPosition = new TilePosition(pylonTilePos.X, pylonTilePos.Y);
                    var location = Game.GetBuildLocation(
                        UnitType.Protoss_Photon_Cannon,
                        buildPosition, 64, false);
                    builder.Build(UnitType.Protoss_Photon_Cannon, location);
                    _currentBuildType = UnitType.Protoss_Photon_Cannon;
                    _lastBuildLocation = location;
                    return;
                }
                else
                {
                    LogToScreen("No builder found for Pylon");
                }
            }
        }
        LogToScreen("No Pylon needs more Cannons");
        if (Game.Self().Minerals() >= 100 && _currentBuildType == UnitType.None)
        {
            Console.WriteLine("Building extra Pylon");
            var location = FindNewPylonLocation();
            var buildLocation = Game.GetBuildLocation(
                UnitType.Protoss_Pylon,
                location, 64, false);
            var builder = GetBuilder();
            if (builder != null)
            {
                Console.WriteLine("Building extra Pylon");
                builder.Build(UnitType.Protoss_Pylon, buildLocation);
                _currentBuildType = UnitType.Protoss_Pylon;
                _lastBuildLocation = buildLocation;
            }
        }


    }

    public TilePosition FindNewPylonLocation()
    {
        if (Game == null)
            throw new InvalidOperationException("Game is not initialized");

        var startLocation = Game.Self().GetStartLocation();
        var enenmyStartLocation = Game.GetStartLocations()
            .FirstOrDefault(loc => loc != startLocation);

        var location = GetPositionToward(startLocation, enenmyStartLocation, 5);
        var pylonsbyLocation = Game.Self().GetUnits()
            .Where(u => u.GetUnitType() == UnitType.Protoss_Pylon &&
                u.GetDistance(location.ToPosition()) < 4 * 32);
        while (pylonsbyLocation.Any())
        {
            location = GetPositionToward(location, enenmyStartLocation, 5);
            pylonsbyLocation = Game.Self().GetUnits()
                .Where(u => u.GetUnitType() == UnitType.Protoss_Pylon &&
                    u.GetDistance(location.ToPosition()) < 4 * 32);
        }
        return location;
    }

    public static TilePosition GetPositionToward(TilePosition from, TilePosition to, int distanceInTiles)
    {
        var direction = to - from;
        var length = Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y);
        var unitDirection = new TilePosition(
            (int)(direction.X / length),
            (int)(direction.Y / length));
        var newPos = new TilePosition(
            (int)(from.X + unitDirection.X * distanceInTiles),
            (int)(from.Y + unitDirection.Y * distanceInTiles));
        return newPos;
    }

    private bool HasForge()
    {
        var forges = Game!.Self().GetUnits()
            .Where(u => u.GetUnitType() == UnitType.Protoss_Forge);
        return forges.Any(f => f.IsCompleted());
    }
}
