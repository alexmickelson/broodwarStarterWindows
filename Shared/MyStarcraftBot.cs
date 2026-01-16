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

    // Bot state
    private Position? _baseLocation = null;
    private Unit? _mainBase = null;
    private bool _defendPhaseComplete = false;
    private bool _attackPhaseStarted = false;
    private List<Unit> _carrierFleet = new List<Unit>();
    private Unit? _observer = null;
    private Position? _attackTarget = null;
    private int _lastScanFrame = 0;
    private string _currentlyBuilding = "";
    private string _savingFor = "";
    private bool _halted = false;
    private string _logFilePath = "";
    private object _logLock = new object();
    private int _nextPylonAngle = 0; // Track angle for next pylon placement
    private int _targetCannonCount = 6; // How many cannons we want
    private int _targetStargateCount = 2; // How many stargates we want
    private int _lastBuildFrame = 0; // Track last build command to prevent spam
    private int _carrierPatrolAngle = 0; // Track angle for carrier defensive patrol
    private int _lastPowerCheckFrame = 0; // Track when we last checked for unpowered buildings
    private Dictionary<int, int> _probeRetreatTimes = new Dictionary<int, int>(); // Track when probes retreated
    private HashSet<int> _retreatingProbes = new HashSet<int>(); // Track which probes are currently retreating
    private Position? _expansionLocation = null; // Track where our expansion is
    private int _lastScoutTime = 0; // Track when we last sent carriers to scout
    private Position? _scoutTarget = null; // Current scout destination

    public void Connect()
    {
        _bwClient = new BWClient(this);
        var _ = Task.Run(() => _bwClient.StartGame());
        IsRunning = true;
        StatusChanged?.Invoke();
        
        // Initialize log file
        var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        // Go up from bin/Debug/net9.0 to the solution root
        var solutionRoot = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..");
        var logsDir = Path.Combine(solutionRoot, "logs");
        Directory.CreateDirectory(logsDir);
        _logFilePath = Path.Combine(logsDir, $"ProtossBot_{timestamp}.log");
        Console.WriteLine($"Logging to {_logFilePath}");
        Log($"Bot started at {DateTime.Now}");
    }

    private void Log(string message)
    {
        try
        {
            lock (_logLock)
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                var logMessage = $"[{timestamp}] {message}";
                File.AppendAllText(_logFilePath, logMessage + Environment.NewLine);
            }
        }
        catch
        {
            // Ignore logging errors to prevent crashes
        }
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
        
        // Set game speed faster
        Game?.SetLocalSpeed(10);
        
        Log("Game started, searching for base...");
        
        // Find our starting base
        if (Game != null)
        {
            var self = Game.Self();
            if (self != null)
            {
                Game.SendText("Protoss Bot initialized");
                
                // Find our nexus
                foreach (var unit in self.GetUnits())
                {
                    if (unit.GetUnitType() == UnitType.Protoss_Nexus)
                    {
                        _mainBase = unit;
                        _baseLocation = unit.GetPosition();
                        Log($"Base found at position {_baseLocation}");
                        break;
                    }
                }
            }
        }
    }

    public override void OnEnd(bool isWinner)
    {
        InGame = false;
        StatusChanged?.Invoke();
    }

    public override void OnFrame()
    {
        if (Game == null)
            return;
        if (GameSpeedToSet != null)
        {
            Game.SetLocalSpeed(GameSpeedToSet.Value);
            GameSpeedToSet = null;
        }

        var self = Game.Self();
        if (self == null || _baseLocation == null)
            return;

        // Display status
        DisplayStatus(self);

        // If halted, don't do any automation
        if (_halted)
            return;

        // Manage workers
        ManageWorkers(self);

        // Manage supply
        ManageSupply(self);

        // Check for unpowered buildings periodically
        if (Game.GetFrameCount() - _lastPowerCheckFrame >= 50)
        {
            CheckUnpoweredBuildings(self);
            _lastPowerCheckFrame = Game.GetFrameCount();
        }

        // Build defense structures - ALWAYS maintain cannons, even in late game
        BuildDefenses(self);

        // Tech to carriers
        BuildTechStructures(self);

        // Train carriers and observer
        TrainFleet(self);

        // Make carriers patrol defensively before attack phase
        if (!_attackPhaseStarted && _carrierFleet.Count > 0)
        {
            DefensiveCarrierPatrol(self);
        }

        // Research upgrades only after we have carriers with interceptors
        var carriersReady = _carrierFleet.Count >= 4 && _carrierFleet.All(c => c.GetInterceptorCount() >= 4);
        if (carriersReady)
        {
            ResearchUpgrades(self);
        }

        // Attack phase
        if (!_attackPhaseStarted && _carrierFleet.Count >= 6 && _observer != null)
        {
            _attackPhaseStarted = true;
            Game.SendText("Beginning attack phase!");
            Log($"Attack phase started with {_carrierFleet.Count} carriers");
        }

        if (_attackPhaseStarted)
        {
            AttackWithFleet(self);
        }

        // Expand with new nexus once we have 3+ carriers for defense
        if (_carrierFleet.Count >= 3)
        {
            BuildExpansion(self);
            DefendExpansion(self);
        }
    }

    private void DisplayStatus(Player self)
    {
        if (Game == null)
            return;

        var minerals = self.Minerals();
        var gas = self.Gas();
        var supply = $"{self.SupplyUsed() / 2}/{self.SupplyTotal() / 2}";

        // Determine what's being saved for
        DetermineBuildGoal(self, minerals, gas);

        Game.DrawTextScreen(10, 10, $"Resources: {minerals} minerals, {gas} gas");
        Game.DrawTextScreen(10, 25, $"Supply: {supply}");
        Game.DrawTextScreen(10, 40, $"Carriers: {_carrierFleet.Count}/8");
        
        // Display upgrades
        var airWeapons = self.GetUpgradeLevel(UpgradeType.Protoss_Air_Weapons);
        var airArmor = self.GetUpgradeLevel(UpgradeType.Protoss_Air_Armor);
        var shields = self.GetUpgradeLevel(UpgradeType.Protoss_Plasma_Shields);
        var carrierCap = self.GetUpgradeLevel(UpgradeType.Carrier_Capacity);
        Game.DrawTextScreen(10, 55, $"Upgrades: Wpn {airWeapons}/3, Armor {airArmor}/3, Shields {shields}/3, Cap {carrierCap}/1");
        
        if (!string.IsNullOrEmpty(_currentlyBuilding))
        {
            Game.DrawTextScreen(10, 75, $"Building: {_currentlyBuilding}");
        }
        
        if (!string.IsNullOrEmpty(_savingFor))
        {
            Game.DrawTextScreen(10, 90, $"Saving for: {_savingFor}");
        }

        if (_attackPhaseStarted)
        {
            Game.DrawTextScreen(10, 110, "Status: ATTACK MODE");
        }
        else if (_defendPhaseComplete)
        {
            Game.DrawTextScreen(10, 110, "Status: Teching to Carriers");
        }
        else
        {
            Game.DrawTextScreen(10, 110, "Status: Building Defenses");
        }

        if (_halted)
        {
            Game.DrawTextScreen(10, 125, "BOT HALTED - Full manual control");
        }
    }

    private void DetermineBuildGoal(Player self, int minerals, int gas)
    {
        // Check what's currently under construction
        var inProgress = self.GetUnits().Where(u => !u.IsCompleted()).ToList();
        if (inProgress.Count > 0)
        {
            var buildingNames = inProgress.Select(u => u.GetUnitType().ToString().Replace("Protoss_", "").Replace("_", " ")).Take(3);
            _currentlyBuilding = string.Join(", ", buildingNames);
        }
        else
        {
            _currentlyBuilding = "Nothing";
        }

        // Determine next goal
        var hasAssimilator = self.GetUnits().Any(u => u.GetUnitType() == UnitType.Protoss_Assimilator);
        var hasForge = self.GetUnits().Any(u => u.GetUnitType() == UnitType.Protoss_Forge);
        var completedCannons = self.GetUnits().Count(u => u.GetUnitType() == UnitType.Protoss_Photon_Cannon && u.IsCompleted());
        var hasGateway = self.GetUnits().Any(u => u.GetUnitType() == UnitType.Protoss_Gateway);
        var hasCyberCore = self.GetUnits().Any(u => u.GetUnitType() == UnitType.Protoss_Cybernetics_Core);
        var stargateCount = self.GetUnits().Count(u => u.GetUnitType() == UnitType.Protoss_Stargate);
        var hasFleetBeacon = self.GetUnits().Any(u => u.GetUnitType() == UnitType.Protoss_Fleet_Beacon);
        var hasRobotics = self.GetUnits().Any(u => u.GetUnitType() == UnitType.Protoss_Robotics_Facility);
        var hasObservatory = self.GetUnits().Any(u => u.GetUnitType() == UnitType.Protoss_Observatory);

        if (!hasAssimilator && minerals >= 100)
        {
            _savingFor = "Assimilator (100 min)";
        }
        else if (!hasForge && minerals >= 150)
        {
            _savingFor = "Forge (150 min)";
        }
        else if (hasForge && completedCannons < 6 && minerals >= 150)
        {
            _savingFor = $"Photon Cannon {completedCannons + 1}/6 (150 min)";
        }
        else if (!hasGateway && minerals >= 150)
        {
            _savingFor = "Gateway (150 min)";
        }
        else if (hasGateway && !hasCyberCore && minerals >= 200)
        {
            _savingFor = "Cybernetics Core (200 min)";
        }
        else if (hasCyberCore && stargateCount < 1 && minerals < 150)
        {
            _savingFor = "Stargate 1/2 (150 min, 150 gas)";
        }
        else if (stargateCount >= 1 && !hasFleetBeacon && (minerals < 300 || gas < 200))
        {
            _savingFor = "Fleet Beacon (300 min, 200 gas)";
        }
        else if (hasCyberCore && stargateCount < 2 && minerals < 150)
        {
            _savingFor = "Stargate 2/2 (150 min, 150 gas)";
        }
        else if (hasCyberCore && !hasRobotics && minerals < 200)
        {
            _savingFor = "Robotics Facility (200 min, 100 gas)";
        }
        else if (hasRobotics && !hasObservatory && (minerals < 50 || gas < 100))
        {
            _savingFor = "Observatory (50 min, 100 gas)";
        }
        else if (hasFleetBeacon && _carrierFleet.Count < 8 && (minerals < 350 || gas < 250))
        {
            _savingFor = $"Carrier {_carrierFleet.Count + 1}/8 (350 min, 250 gas)";
        }
        else if (hasRobotics && _observer == null && (minerals < 75 || gas < 100))
        {
            _savingFor = "Observer (75 min, 100 gas)";
        }
        else if (self.SupplyUsed() / 2 + 4 >= self.SupplyTotal() / 2)
        {
            _savingFor = "Pylon (100 min)";
        }
        else
        {
            _savingFor = "Carrier Interceptors";
        }
    }

    private void ManageWorkers(Player self)
    {
        var probes = self.GetUnits().Where(u => u.GetUnitType() == UnitType.Protoss_Probe).ToList();
        var nexuses = self.GetUnits().Where(u => u.GetUnitType() == UnitType.Protoss_Nexus).ToList();

        // Build probes until we have 16 per base (2 per mineral patch + 3 for gas)
        var targetProbes = nexuses.Count * 16;
        if (probes.Count < targetProbes)
        {
            foreach (var nexus in nexuses)
            {
                if (nexus.IsIdle() && nexus.IsCompleted())
                {
                    nexus.Train(UnitType.Protoss_Probe);
                }
            }
        }

        // Check for danger and retreat probes if needed
        CheckProbesSafety(self, probes);

        // Determine if we're low on minerals (allows distant gathering)
        var isLowOnMinerals = self.Minerals() < 300;
        var safeDistanceFromBase = isLowOnMinerals ? 1000 : 400; // ~31 tiles when low, ~12 tiles otherwise

        // Assign workers to mine
        foreach (var probe in probes)
        {
            // Skip probes that are constructing or retreating
            if (probe.IsConstructing() || _retreatingProbes.Contains(probe.GetID()))
                continue;

            // Check if probe can retry after retreat cooldown (5 seconds)
            if (_probeRetreatTimes.ContainsKey(probe.GetID()))
            {
                var framesSinceRetreat = Game!.GetFrameCount() - _probeRetreatTimes[probe.GetID()];
                if (framesSinceRetreat < 24 * 5) // 5 second cooldown
                    continue;
                else
                    _probeRetreatTimes.Remove(probe.GetID()); // Cooldown expired
            }

            if (probe.IsIdle())
            {
                // Find nearest nexus to this probe
                var nearestNexus = nexuses.Where(n => n.IsCompleted()).OrderBy(n => n.GetDistance(probe.GetPosition())).FirstOrDefault();
                if (nearestNexus != null)
                {
                    // Find minerals within safe distance (or further if we're desperate)
                    var availableMinerals = Game!.GetMinerals()
                        .Where(m => m.GetDistance(nearestNexus.GetPosition()) < safeDistanceFromBase)
                        .OrderBy(m => m.GetDistance(probe.GetPosition()))
                        .ToList();

                    // If low on minerals and no safe minerals found, try ANY minerals
                    if (availableMinerals.Count == 0 && isLowOnMinerals)
                    {
                        availableMinerals = Game.GetMinerals()
                            .OrderBy(m => m.GetDistance(probe.GetPosition()))
                            .ToList();
                        
                        if (availableMinerals.Count > 0)
                        {
                            Log($"Probe venturing to distant minerals (low resources)");
                        }
                    }
                    
                    var targetMineral = availableMinerals.FirstOrDefault();
                    if (targetMineral != null)
                    {
                        probe.Gather(targetMineral);
                    }
                }
            }
        }
    }

    private void CheckProbesSafety(Player self, List<Unit> probes)
    {
        if (Game == null)
            return;

        var cannons = self.GetUnits()
            .Where(u => u.GetUnitType() == UnitType.Protoss_Photon_Cannon && u.IsCompleted())
            .ToList();

        if (cannons.Count == 0)
            return; // No safe retreat point

        foreach (var probe in probes)
        {
            if (probe.IsConstructing())
                continue;

            // Check for nearby enemy military units
            var nearbyEnemies = Game.GetAllUnits()
                .Where(u => u.GetPlayer() != null && u.GetPlayer() != self && !u.GetPlayer().IsNeutral())
                .Where(u => u.GetUnitType().CanAttack())
                .Where(u => u.GetDistance(probe.GetPosition()) < 200) // Within ~6 tiles
                .ToList();

            if (nearbyEnemies.Count > 0)
            {
                // Find nearest cannon for safety
                var nearestCannon = cannons.OrderBy(c => c.GetDistance(probe.GetPosition())).FirstOrDefault();
                if (nearestCannon != null)
                {
                    var probeId = probe.GetID();
                    if (!_retreatingProbes.Contains(probeId))
                    {
                        _retreatingProbes.Add(probeId);
                        _probeRetreatTimes[probeId] = Game.GetFrameCount();
                        Log($"Probe retreating from danger to nearest cannon");
                    }

                    // Move to cannon and stop retreating when close enough
                    if (probe.GetDistance(nearestCannon.GetPosition()) < 150) // Within cannon range
                    {
                        _retreatingProbes.Remove(probeId);
                        // Stay put until cooldown expires
                    }
                    else
                    {
                        probe.Move(nearestCannon.GetPosition());
                    }
                }
            }
        }
    }

    private void CheckUnpoweredBuildings(Player self)
    {
        if (Game == null)
            return;

        // Find any unpowered buildings
        var unpoweredBuilding = self.GetUnits()
            .Where(u => u.GetUnitType().IsBuilding())
            .Where(u => u.GetUnitType().RequiresPsi())
            .Where(u => !u.IsPowered() && u.IsCompleted())
            .FirstOrDefault();

        if (unpoweredBuilding != null && self.Minerals() >= 100)
        {
            // Throttle to prevent spam
            if (Game.GetFrameCount() - _lastBuildFrame < 24)
                return;

            var probe = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Probe && !u.IsConstructing());
            if (probe == null)
                return;

            // Try to build a pylon near the unpowered building
            var buildingPos = unpoweredBuilding.GetTilePosition();
            var buildTile = Game.GetBuildLocation(UnitType.Protoss_Pylon, buildingPos, 8);
            
            if (Game.CanBuildHere(buildTile, UnitType.Protoss_Pylon, probe))
            {
                probe.Build(UnitType.Protoss_Pylon, buildTile);
                _lastBuildFrame = Game.GetFrameCount();
                Log($"Building pylon near unpowered {unpoweredBuilding.GetUnitType()}");
            }
        }
    }

    private void ManageSupply(Player self)
    {
        var supplyUsed = self.SupplyUsed() / 2;
        var supplyTotal = self.SupplyTotal() / 2;
        var pylons = self.GetUnits().Where(u => u.GetUnitType() == UnitType.Protoss_Pylon).ToList();
        var pylonsInProgress = pylons.Count(p => !p.IsCompleted());

        // Build pylons when needed
        if (supplyTotal < 200 && supplyUsed + 4 >= supplyTotal && pylonsInProgress == 0)
        {
            BuildPylon(self);
        }
    }

    private void BuildExpansion(Player self)
    {
        if (Game == null)
            return;

        var nexuses = self.GetUnits().Where(u => u.GetUnitType() == UnitType.Protoss_Nexus).ToList();
        var nexusesInProgress = nexuses.Count(n => !n.IsCompleted());

        // Only build one expansion at a time
        if (nexusesInProgress > 0)
            return;

        // Don't expand too much
        if (nexuses.Count >= 3)
            return;

        // Find a new mineral patch far from existing bases
        var existingNexusPositions = nexuses.Select(n => n.GetPosition()).ToList();
        var expansionMinerals = Game.GetMinerals()
            .Where(m => existingNexusPositions.All(pos => m.GetDistance(pos) > 500)) // Far from existing bases
            .OrderBy(m => m.GetDistance(_baseLocation!.Value)) // Prefer closer expansions first
            .FirstOrDefault();

        if (expansionMinerals != null && self.Minerals() >= 400)
        {
            var probe = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Probe && !u.IsConstructing());
            if (probe != null)
            {
                // Try to build near the expansion minerals
                var buildPos = new TilePosition(expansionMinerals.GetTilePosition().X - 2, expansionMinerals.GetTilePosition().Y - 1);
                if (Game.CanBuildHere(buildPos, UnitType.Protoss_Nexus, probe))
                {
                    probe.Build(UnitType.Protoss_Nexus, buildPos);
                    _expansionLocation = new Position(buildPos.X * 32, buildPos.Y * 32);
                    Log($"Building expansion nexus at new mineral location");
                }
            }
        }
    }

    private void DefendExpansion(Player self)
    {
        if (Game == null || _expansionLocation == null)
            return;

        var expansionNexus = self.GetUnits()
            .FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Nexus && 
                                 u.GetPosition().GetDistance(_expansionLocation.Value) < 200);

        if (expansionNexus == null)
            return; // No expansion yet

        // Build pylon at expansion if needed
        var expansionPylons = self.GetUnits()
            .Where(u => u.GetUnitType() == UnitType.Protoss_Pylon && 
                       u.GetPosition().GetDistance(_expansionLocation.Value) < 400)
            .ToList();

        if (expansionPylons.Count == 0 && self.Minerals() >= 100)
        {
            if (Game.GetFrameCount() - _lastBuildFrame < 24)
                return;

            var probe = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Probe && !u.IsConstructing());
            if (probe != null)
            {
                var pylonPos = new TilePosition(_expansionLocation.Value.X / 32 + 3, _expansionLocation.Value.Y / 32 + 2);
                if (Game.CanBuildHere(pylonPos, UnitType.Protoss_Pylon, probe))
                {
                    probe.Build(UnitType.Protoss_Pylon, pylonPos);
                    _lastBuildFrame = Game.GetFrameCount();
                    Log("Building pylon at expansion");
                }
            }
        }

        // Build cannons at expansion (3 total)
        var expansionCannons = self.GetUnits()
            .Where(u => u.GetUnitType() == UnitType.Protoss_Photon_Cannon && 
                       u.GetPosition().GetDistance(_expansionLocation.Value) < 400)
            .ToList();

        var forge = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Forge);
        var expansionPylonCompleted = expansionPylons.Any(p => p.IsCompleted());

        if (forge != null && forge.IsCompleted() && expansionPylonCompleted && 
            expansionCannons.Count < 3 && self.Minerals() >= 150)
        {
            if (BuildBuilding(self, UnitType.Protoss_Photon_Cannon))
            {
                Log($"Building expansion cannon {expansionCannons.Count + 1}/3");
            }
        }

        // Build assimilator at expansion
        var expansionAssimilator = self.GetUnits()
            .FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Assimilator && 
                                u.GetPosition().GetDistance(_expansionLocation.Value) < 400);

        if (expansionNexus.IsCompleted() && expansionAssimilator == null && self.Minerals() >= 100)
        {
            if (Game.GetFrameCount() - _lastBuildFrame < 24)
                return;

            var nearbyGeyser = Game.GetGeysers()
                .Where(g => g.GetDistance(_expansionLocation.Value) < 400)
                .OrderBy(g => g.GetDistance(_expansionLocation.Value))
                .FirstOrDefault();

            if (nearbyGeyser != null)
            {
                var probe = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Probe && !u.IsConstructing());
                if (probe != null)
                {
                    probe.Build(UnitType.Protoss_Assimilator, nearbyGeyser.GetTilePosition());
                    _lastBuildFrame = Game.GetFrameCount();
                    Log("Building assimilator at expansion");
                }
            }
        }
    }

    private bool ShouldReserveForExpansion(Player self)
    {
        // Reserve resources when we have carriers but haven't expanded yet
        if (_carrierFleet.Count < 3)
            return false;

        var nexuses = self.GetUnits().Where(u => u.GetUnitType() == UnitType.Protoss_Nexus).ToList();
        var nexusesInProgress = nexuses.Count(n => !n.IsCompleted());

        // Don't reserve if we're already expanding or have enough bases
        if (nexusesInProgress > 0 || nexuses.Count >= 3)
            return false;

        // Reserve when we're approaching expansion threshold (need 400 nexus + 100 assimilator + 100 pylon + 450 cannons = 1050 total)
        return self.Minerals() >= 400 && self.Minerals() < 1200;
    }

    private void BuildPylon(Player self)
    {
        if (Game == null || _baseLocation == null)
            return;

        // Throttle build commands - don't try to build more than once per second
        if (Game.GetFrameCount() - _lastBuildFrame < 24)
            return;

        var probe = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Probe && !u.IsCarryingMinerals() && !u.IsCarryingGas() && !u.IsConstructing());
        if (probe == null)
            probe = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Probe && !u.IsConstructing());

        if (probe == null)
            return;

        // Try to place pylons in a ring around the base
        // Place them at varying distances and angles to spread coverage
        var existingPylons = self.GetUnits().Where(u => u.GetUnitType() == UnitType.Protoss_Pylon).Count();
        
        // Alternate between closer and farther rings
        var distance = (existingPylons % 2 == 0) ? 12 : 18; // tiles from base
        
        // Try multiple angles to find a good spot
        for (int attempt = 0; attempt < 12; attempt++)
        {
            var angle = (_nextPylonAngle + attempt * 30) % 360;
            var radians = angle * Math.PI / 180.0;
            
            var targetX = (int)(_baseLocation.Value.X + Math.Cos(radians) * distance * 32);
            var targetY = (int)(_baseLocation.Value.Y + Math.Sin(radians) * distance * 32);
            var targetPos = new Position(targetX, targetY);
            var targetTile = new TilePosition(targetX / 32, targetY / 32);
            
            // Check if this is a valid build location
            if (Game.CanBuildHere(targetTile, UnitType.Protoss_Pylon, probe))
            {
                probe.Build(UnitType.Protoss_Pylon, targetTile);
                _nextPylonAngle = (angle + 45) % 360; // Increment for next pylon
                _lastBuildFrame = Game.GetFrameCount();
                Log($"Building pylon at angle {angle}° distance {distance} tiles");
                return;
            }
        }
        
        // Fallback to default building logic if ring placement fails
        var buildTile = Game.GetBuildLocation(UnitType.Protoss_Pylon, probe.GetTilePosition(), 64);
        probe.Build(UnitType.Protoss_Pylon, buildTile);
        _lastBuildFrame = Game.GetFrameCount();
        Log($"Building pylon using fallback location");
    }

    private void BuildDefenses(Player self)
    {
        var forge = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Forge);
        var cannons = self.GetUnits().Where(u => u.GetUnitType() == UnitType.Protoss_Photon_Cannon).ToList();
        var completedCannons = cannons.Count(c => c.IsCompleted());

        // Rebuild forge if destroyed - HIGH PRIORITY
        if (forge == null && self.Minerals() >= 150)
        {
            if (BuildBuilding(self, UnitType.Protoss_Forge))
            {
                Log("Rebuilding destroyed Forge");
            }
            return;
        }

        // Build/rebuild cannons to reach target count - ALWAYS maintain defenses
        if (forge != null && forge.IsCompleted() && completedCannons < _targetCannonCount)
        {
            var cannonsInProgress = cannons.Count(c => !c.IsCompleted());
            // Allow up to 3 cannons building at once if we're way below target
            var maxSimultaneous = (completedCannons < 3) ? 3 : 2;
            if (cannonsInProgress < maxSimultaneous && self.Minerals() >= 150)
            {
                if (BuildBuilding(self, UnitType.Protoss_Photon_Cannon))
                {
                    Log($"Building cannon {completedCannons + 1}/{_targetCannonCount}");
                }
            }
        }

        // Mark defend phase complete only after we have all initial cannons
        if (completedCannons >= 6 && !_defendPhaseComplete)
        {
            _defendPhaseComplete = true;
        }
    }

    private void BuildTechStructures(Player self)
    {
        var assimilator = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Assimilator);
        var cyberCore = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Cybernetics_Core);
        var gateway = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Gateway);
        var stargates = self.GetUnits().Where(u => u.GetUnitType() == UnitType.Protoss_Stargate).ToList();
        var fleetBeacon = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Fleet_Beacon);
        var roboticsFacility = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Robotics_Facility);
        var observatory = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Observatory);

        // Build/rebuild assimilator on geyser
        if (assimilator == null && self.Minerals() >= 100)
        {
            // Throttle to prevent spam
            if (Game!.GetFrameCount() - _lastBuildFrame < 24)
                return;
                
            var geyser = Game.GetGeysers().OrderBy(g => g.GetDistance(_baseLocation!.Value)).FirstOrDefault();
            if (geyser != null)
            {
                var probe = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Probe && !u.IsConstructing());
                if (probe != null)
                {
                    probe.Build(UnitType.Protoss_Assimilator, geyser.GetTilePosition());
                    _lastBuildFrame = Game.GetFrameCount();
                    Log("Building/rebuilding Assimilator");
                }
            }
        }

        // Assign workers to gas
        if (assimilator != null && assimilator.IsCompleted())
        {
            var workersOnGas = self.GetUnits().Count(u => u.GetUnitType() == UnitType.Protoss_Probe && u.IsGatheringGas());
            if (workersOnGas < 3)
            {
                var probe = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Probe && u.IsGatheringMinerals());
                if (probe != null)
                {
                    probe.Gather(assimilator);
                }
            }
        }

        // Build/rebuild gateway (required for cyber core)
        if (gateway == null && self.Minerals() >= 150)
        {
            if (BuildBuilding(self, UnitType.Protoss_Gateway))
            {
                Log("Building/rebuilding Gateway");
            }
        }

        // Build/rebuild cybernetics core (required for stargate)
        if (gateway != null && gateway.IsCompleted() && cyberCore == null && self.Minerals() >= 200)
        {
            if (BuildBuilding(self, UnitType.Protoss_Cybernetics_Core))
            {
                Log("Building/rebuilding Cybernetics Core");
            }
        }

        // Build/rebuild stargates (2 total)
        var completedStargates = stargates.Count(s => s.IsCompleted());
        if (cyberCore != null && cyberCore.IsCompleted() && completedStargates < _targetStargateCount && self.Minerals() >= 150 && self.Gas() >= 150)
        {
            var stargatesInProgress = stargates.Count(s => !s.IsCompleted());
            if (stargatesInProgress == 0)
            {
                if (BuildBuilding(self, UnitType.Protoss_Stargate))
                {
                    Log($"Building/rebuilding Stargate ({completedStargates + 1}/{_targetStargateCount})");
                }
            }
        }

        // Build/rebuild fleet beacon (for carriers)
        if (stargates.Count > 0 && stargates[0].IsCompleted() && fleetBeacon == null && self.Minerals() >= 300 && self.Gas() >= 200)
        {
            if (BuildBuilding(self, UnitType.Protoss_Fleet_Beacon))
            {
                Log("Building/rebuilding Fleet Beacon");
            }
        }

        // Build/rebuild robotics facility (for observer)
        if (cyberCore != null && cyberCore.IsCompleted() && roboticsFacility == null && self.Minerals() >= 200 && self.Gas() >= 100)
        {
            if (BuildBuilding(self, UnitType.Protoss_Robotics_Facility))
            {
                Log("Building/rebuilding Robotics Facility");
            }
        }

        // Build/rebuild observatory (for observer upgrades)
        if (roboticsFacility != null && roboticsFacility.IsCompleted() && observatory == null && self.Minerals() >= 50 && self.Gas() >= 100)
        {
            if (BuildBuilding(self, UnitType.Protoss_Observatory))
            {
                Log("Building/rebuilding Observatory");
            }
        }
    }

    private void DefensiveCarrierPatrol(Player self)
    {
        if (Game == null || _baseLocation == null)
            return;

        var nexuses = self.GetUnits().Where(u => u.GetUnitType() == UnitType.Protoss_Nexus).ToList();
        var basePositions = nexuses.Select(n => n.GetPosition()).ToList();

        // Calculate carrier group center to keep them together
        if (_carrierFleet.Count > 0)
        {
            var avgX = _carrierFleet.Average(c => c.GetPosition().X);
            var avgY = _carrierFleet.Average(c => c.GetPosition().Y);
            var groupCenter = new Position((int)avgX, (int)avgY);

            // Keep observer with carrier group
            if (_observer != null && _observer.Exists())
            {
                if (_observer.GetDistance(groupCenter) > 150)
                {
                    _observer.Move(groupCenter);
                }
            }
        }

        // Priority 1: Defend against enemies attacking our bases
        foreach (var basePos in basePositions)
        {
            var enemiesNearBase = Game.GetAllUnits()
                .Where(u => u.GetPlayer() != null && u.GetPlayer() != self && !u.GetPlayer().IsNeutral())
                .Where(u => u.GetDistance(basePos) < 600) // Within ~19 tiles of base
                .Where(u => u.GetUnitType().CanAttack() || u.GetUnitType().IsBuilding())
                .ToList();

            if (enemiesNearBase.Count > 0)
            {
                var target = enemiesNearBase
                    .OrderByDescending(e => e.GetUnitType().CanAttack() ? 1 : 0) // Prioritize attacking units
                    .ThenBy(e => e.GetDistance(basePos))
                    .First();

                Log($"Carriers defending base from {target.GetUnitType()}");
                
                // All carriers attack together
                foreach (var carrier in _carrierFleet)
                {
                    if (carrier.Exists())
                    {
                        carrier.Attack(target.GetPosition());
                    }
                }
                return; // All carriers respond to threat
            }
        }

        // Priority 2: Clear enemy buildings near our bases
        foreach (var basePos in basePositions)
        {
            var enemyBuildingsNearBase = Game.GetAllUnits()
                .Where(u => u.GetPlayer() != null && u.GetPlayer() != self && !u.GetPlayer().IsNeutral())
                .Where(u => u.GetUnitType().IsBuilding())
                .Where(u => u.GetDistance(basePos) < 1000) // Within ~31 tiles
                .ToList();

            if (enemyBuildingsNearBase.Count > 0)
            {
                var closestBuilding = enemyBuildingsNearBase.OrderBy(b => b.GetDistance(basePos)).First();
                Log($"Carriers clearing enemy {closestBuilding.GetUnitType()} near base");
                
                // All carriers attack together
                foreach (var carrier in _carrierFleet)
                {
                    if (carrier.Exists())
                    {
                        carrier.Attack(closestBuilding.GetPosition());
                    }
                }
                return;
            }
        }

        // Priority 3: Occasionally scout for expansion (only if no expansion exists and enough carriers)
        var hasExpansion = nexuses.Count > 1 || _expansionLocation != null;
        var canScout = _carrierFleet.Count >= 3;
        var shouldScout = (Game.GetFrameCount() - _lastScoutTime) > (24 * 60); // Every 60 seconds

        if (!hasExpansion && canScout && shouldScout)
        {
            // Find new scout target if we don't have one
            if (_scoutTarget == null)
            {
                var currentBases = nexuses.Select(n => n.GetPosition()).ToList();
                var potentialExpansion = Game.GetMinerals()
                    .Where(m => currentBases.All(b => m.GetDistance(b) > 500)) // Far from current bases
                    .OrderBy(m => m.GetDistance(_baseLocation.Value)) // Closer expansions first
                    .FirstOrDefault();

                if (potentialExpansion != null)
                {
                    _scoutTarget = potentialExpansion.GetPosition();
                    _lastScoutTime = Game.GetFrameCount();
                    Log("Carriers scouting for expansion location");
                }
            }

            // Move entire carrier group to scout target
            if (_scoutTarget != null)
            {
                var groupCenter = new Position(
                    (int)_carrierFleet.Average(c => c.GetPosition().X),
                    (int)_carrierFleet.Average(c => c.GetPosition().Y)
                );

                // If we've reached the scout target, clear it and return to base
                if (groupCenter.GetDistance(_scoutTarget.Value) < 300)
                {
                    _scoutTarget = null;
                }
                else
                {
                    // All carriers move together to scout
                    foreach (var carrier in _carrierFleet)
                    {
                        if (carrier.Exists())
                        {
                            carrier.Move(_scoutTarget.Value);
                        }
                    }
                    return;
                }
            }
        }

        // Priority 4: Default patrol around main base (stay close)
        foreach (var carrier in _carrierFleet)
        {
            if (!carrier.Exists())
                continue;

            var distanceFromBase = carrier.GetDistance(_baseLocation.Value);
            
            // If carrier is too far from base (more than 20 tiles), bring it back
            if (distanceFromBase > 640)
            {
                carrier.Move(_baseLocation.Value);
            }
            // Only command idle carriers or those close to their patrol point
            else if (carrier.IsIdle() || (carrier.GetTargetPosition() != null && carrier.GetDistance(carrier.GetTargetPosition()!) < 50))
            {
                // Calculate patrol point in a circle around base (radius ~12 tiles - closer to base)
                var radius = 384; // pixels (12 tiles)
                var radians = _carrierPatrolAngle * Math.PI / 180.0;
                var patrolX = (int)(_baseLocation.Value.X + Math.Cos(radians) * radius);
                var patrolY = (int)(_baseLocation.Value.Y + Math.Sin(radians) * radius);
                var patrolPos = new Position(patrolX, patrolY);

                carrier.Move(patrolPos);
            }
        }

        // Slowly rotate the patrol angle every ~2 seconds
        if (Game.GetFrameCount() % 48 == 0)
        {
            _carrierPatrolAngle = (_carrierPatrolAngle + 15) % 360;
        }
    }

    private void TrainFleet(Player self)
    {
        var stargates = self.GetUnits().Where(u => u.GetUnitType() == UnitType.Protoss_Stargate && u.IsCompleted()).ToList();
        var fleetBeacon = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Fleet_Beacon);
        var roboticsFacility = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Robotics_Facility);

        // Update carrier list
        _carrierFleet = self.GetUnits().Where(u => u.GetUnitType() == UnitType.Protoss_Carrier && u.IsCompleted()).ToList();

        // Build carriers from all stargates
        if (fleetBeacon != null && fleetBeacon.IsCompleted() && _carrierFleet.Count < 8)
        {
            // Don't build carriers if we're saving for expansion
            if (!ShouldReserveForExpansion(self))
            {
                foreach (var stargate in stargates)
                {
                    if (stargate.IsIdle() && self.Minerals() >= 350 && self.Gas() >= 250)
                    {
                        stargate.Train(UnitType.Protoss_Carrier);
                        Log($"Training carrier {_carrierFleet.Count + 1}/8");
                    }
                }
            }
        }

        // Build observer
        if (roboticsFacility != null && roboticsFacility.IsCompleted())
        {
            if (_observer == null || !_observer.Exists())
            {
                if (roboticsFacility.IsIdle() && self.Minerals() >= 75 && self.Gas() >= 100)
                {
                    roboticsFacility.Train(UnitType.Protoss_Observer);
                    _observer = null; // Will be set in OnUnitComplete
                }
            }
        }

        // Make carriers build interceptors
        foreach (var carrier in _carrierFleet)
        {
            if (!carrier.Exists())
                continue;
                
            var maxInterceptors = self.GetUpgradeLevel(UpgradeType.Carrier_Capacity) > 0 ? 8 : 4;
            
            // Only train if not already training and below max capacity
            if (carrier.GetInterceptorCount() < maxInterceptors && !carrier.IsTraining())
            {
                // Check if we have resources for interceptors (25 minerals each)
                if (self.Minerals() >= 25)
                {
                    carrier.Train(UnitType.Protoss_Interceptor);
                }
            }
        }
    }

    private void ResearchUpgrades(Player self)
    {
        // Don't research if we're saving for expansion
        if (ShouldReserveForExpansion(self))
            return;

        var cyberCore = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Cybernetics_Core && u.IsCompleted());
        var fleetBeacon = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Fleet_Beacon && u.IsCompleted());
        var forge = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Forge && u.IsCompleted());

        // Research Carrier Capacity first (very important - increases interceptors from 4 to 8)
        if (fleetBeacon != null && !fleetBeacon.IsUpgrading())
        {
            var hasCarrierCapacity = self.GetUpgradeLevel(UpgradeType.Carrier_Capacity) > 0;
            if (!hasCarrierCapacity && self.Minerals() >= 100 && self.Gas() >= 100)
            {
                fleetBeacon.Upgrade(UpgradeType.Carrier_Capacity);
                return;
            }
        }

        // Research Air Weapons at Cybernetics Core
        if (cyberCore != null && !cyberCore.IsUpgrading())
        {
            var airWeaponsLevel = self.GetUpgradeLevel(UpgradeType.Protoss_Air_Weapons);
            if (airWeaponsLevel == 0 && self.Minerals() >= 100 && self.Gas() >= 100)
            {
                cyberCore.Upgrade(UpgradeType.Protoss_Air_Weapons);
                return;
            }
            else if (airWeaponsLevel == 1 && self.Minerals() >= 175 && self.Gas() >= 175)
            {
                cyberCore.Upgrade(UpgradeType.Protoss_Air_Weapons);
                return;
            }
            else if (airWeaponsLevel == 2 && self.Minerals() >= 250 && self.Gas() >= 250)
            {
                cyberCore.Upgrade(UpgradeType.Protoss_Air_Weapons);
                return;
            }
        }

        // Research Air Armor at Cybernetics Core
        if (cyberCore != null && !cyberCore.IsUpgrading())
        {
            var airArmorLevel = self.GetUpgradeLevel(UpgradeType.Protoss_Air_Armor);
            if (airArmorLevel == 0 && self.Minerals() >= 150 && self.Gas() >= 150)
            {
                cyberCore.Upgrade(UpgradeType.Protoss_Air_Armor);
                return;
            }
            else if (airArmorLevel == 1 && self.Minerals() >= 225 && self.Gas() >= 225)
            {
                cyberCore.Upgrade(UpgradeType.Protoss_Air_Armor);
                return;
            }
            else if (airArmorLevel == 2 && self.Minerals() >= 300 && self.Gas() >= 300)
            {
                cyberCore.Upgrade(UpgradeType.Protoss_Air_Armor);
                return;
            }
        }

        // Research Shields at Forge
        if (forge != null && !forge.IsUpgrading())
        {
            var shieldsLevel = self.GetUpgradeLevel(UpgradeType.Protoss_Plasma_Shields);
            if (shieldsLevel == 0 && self.Minerals() >= 200 && self.Gas() >= 200)
            {
                forge.Upgrade(UpgradeType.Protoss_Plasma_Shields);
                return;
            }
            else if (shieldsLevel == 1 && self.Minerals() >= 300 && self.Gas() >= 300)
            {
                forge.Upgrade(UpgradeType.Protoss_Plasma_Shields);
                return;
            }
            else if (shieldsLevel == 2 && self.Minerals() >= 400 && self.Gas() >= 400)
            {
                forge.Upgrade(UpgradeType.Protoss_Plasma_Shields);
                return;
            }
        }
    }

    private void AttackWithFleet(Player self)
    {
        if (_carrierFleet.Count == 0 || Game == null)
            return;

        // Keep fleet together - find center position
        var avgX = _carrierFleet.Average(c => c.GetPosition().X);
        var avgY = _carrierFleet.Average(c => c.GetPosition().Y);
        var fleetCenter = new Position((int)avgX, (int)avgY);

        // Scan for enemies periodically
        if (Game.GetFrameCount() - _lastScanFrame > 24 * 5) // Every 5 seconds
        {
            _lastScanFrame = Game.GetFrameCount();
            
            // Find enemy units
            var enemyUnits = Game.GetAllUnits()
                .Where(u => u.GetPlayer() != null && u.GetPlayer() != self && !u.GetPlayer().IsNeutral())
                .Where(u => u.Exists() && u.GetHitPoints() > 0)
                .ToList();

            if (enemyUnits.Count > 0)
            {
                // Find closest enemy to our fleet
                var closestEnemy = enemyUnits.OrderBy(e => e.GetDistance(fleetCenter)).FirstOrDefault();
                if (closestEnemy != null)
                {
                    var enemyPos = closestEnemy.GetPosition();
                    if (enemyPos != null)
                    {
                        _attackTarget = enemyPos;
                    }
                }
            }
            else
            {
                // No enemies found, explore the map
                if (_attackTarget == null || fleetCenter.GetDistance(_attackTarget.Value) < 100)
                {
                    // Pick a random location on the map
                    var mapWidth = Game.MapWidth() * 32;
                    var mapHeight = Game.MapHeight() * 32;
                    var random = new Random();
                    _attackTarget = new Position(random.Next(0, mapWidth), random.Next(0, mapHeight));
                }
            }
        }

        // Move carriers to attack target
        if (_attackTarget != null)
        {
            var target = _attackTarget.Value;
            foreach (var carrier in _carrierFleet)
            {
                // Find enemies in weapon range (Carrier range is 8*32 = 256 pixels)
                var carrierRange = 256;
                var nearbyEnemies = Game.GetAllUnits()
                    .Where(u => u.GetPlayer() != null && u.GetPlayer() != self && !u.GetPlayer().IsNeutral())
                    .Where(u => u.Exists() && u.GetHitPoints() > 0)
                    .Where(u => u.GetDistance(carrier.GetPosition()) <= carrierRange)
                    .ToList();

                if (nearbyEnemies.Count > 0)
                {
                    // Attack the closest enemy in range - this sends interceptors without moving the carrier
                    var closestEnemy = nearbyEnemies.OrderBy(e => e.GetDistance(carrier.GetPosition())).First();
                    if (!carrier.IsAttacking() || carrier.GetTarget() != closestEnemy)
                    {
                        carrier.Attack(closestEnemy);
                    }
                }
                else
                {
                    // No enemies in range, move towards target but maintain distance
                    var distanceToTarget = carrier.GetDistance(target);
                    
                    // Stay at max range - if we're too far, move closer, if too close, back up
                    if (distanceToTarget > carrierRange + 100)
                    {
                        // Too far, move closer
                        carrier.Move(target);
                    }
                    else if (distanceToTarget < carrierRange - 50)
                    {
                        // Too close, back away
                        var carrierPos = carrier.GetPosition();
                        var dx = carrierPos.X - target.X;
                        var dy = carrierPos.Y - target.Y;
                        var distance = Math.Sqrt(dx * dx + dy * dy);
                        if (distance > 0)
                        {
                            var retreatX = carrierPos.X + (int)(dx / distance * 100);
                            var retreatY = carrierPos.Y + (int)(dy / distance * 100);
                            carrier.Move(new Position(retreatX, retreatY));
                        }
                    }
                    else if (carrier.IsIdle())
                    {
                        // In good position, hold position
                        carrier.HoldPosition();
                    }
                }
            }
        }

        // Observer follows fleet
        if (_observer != null && _observer.Exists())
        {
            if (_observer.GetDistance(fleetCenter) > 200)
            {
                _observer.Move(fleetCenter);
            }
        }
    }

    private bool BuildBuilding(Player self, UnitType buildingType)
    {
        if (Game == null || _baseLocation == null)
            return false;

        // Throttle build commands - don't try to build more than once per second
        if (Game.GetFrameCount() - _lastBuildFrame < 24)
            return false;

        var probe = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Probe && !u.IsCarryingMinerals() && !u.IsCarryingGas() && !u.IsConstructing());
        if (probe == null)
            probe = self.GetUnits().FirstOrDefault(u => u.GetUnitType() == UnitType.Protoss_Probe && !u.IsConstructing());

        if (probe == null)
            return false;

        // Find a build location near the base
        var buildTile = Game.GetBuildLocation(buildingType, probe.GetTilePosition(), 64);
        probe.Build(buildingType, buildTile);
        _lastBuildFrame = Game.GetFrameCount();
        return true;
    }

    public override void OnUnitComplete(Unit unit) 
    { 
        var unitType = unit.GetUnitType();
        
        // Don't log resource completion
        if (unitType.ToString().StartsWith("Resource_"))
            return;
        
        // Only log our own units, not opponents'
        if (Game != null && unit.GetPlayer() != Game.Self())
            return;
            
        Log($"Unit completed: {unitType}");
        
        if (unitType == UnitType.Protoss_Observer)
        {
            _observer = unit;
            Log("Observer ready for scouting");
        }
        else if (unitType == UnitType.Protoss_Carrier)
        {
            Log($"Carrier completed. Fleet size: {_carrierFleet.Count + 1}");
        }
    }

    public override void OnUnitDestroy(Unit unit) 
    { 
        var unitType = unit.GetUnitType();
        
        // Don't log resource destruction
        if (unitType.ToString().StartsWith("Resource_"))
            return;
        
        // Determine if it's our unit or enemy unit
        var isOurs = Game != null && unit.GetPlayer() == Game.Self();
        var ownerTag = isOurs ? "[OURS]" : "[ENEMY]";
        
        Log($"{ownerTag} Unit destroyed: {unitType}");
        
        if (isOurs)
        {
            // Log critical building destruction
            if (unitType == UnitType.Protoss_Forge ||
                unitType == UnitType.Protoss_Gateway ||
                unitType == UnitType.Protoss_Cybernetics_Core ||
                unitType == UnitType.Protoss_Stargate ||
                unitType == UnitType.Protoss_Fleet_Beacon ||
                unitType == UnitType.Protoss_Robotics_Facility ||
                unitType == UnitType.Protoss_Observatory ||
                unitType == UnitType.Protoss_Assimilator)
            {
                Log($"CRITICAL: {unitType} destroyed! Will rebuild.");
            }
            
            // Remove destroyed carriers from fleet
            if (unitType == UnitType.Protoss_Carrier)
            {
                _carrierFleet.Remove(unit);
                Log($"Carrier destroyed! Fleet size now: {_carrierFleet.Count}");
            }
            
            // Reset observer if destroyed
            if (unit == _observer)
            {
                _observer = null;
                Log("Observer destroyed!");
            }
        }
    }

    public override void OnUnitMorph(Unit unit) { }

    public override void OnSendText(string text) 
    {
        if (text == "/halt")
        {
            _halted = !_halted;
            Log($"Bot {(_halted ? "halted" : "resumed")}");
            if (Game != null)
            {
                Game.SendText(_halted ? "Bot halted - manual control enabled" : "Bot resumed - automation enabled");
            }
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
}
