namespace Web.Services;

public record GamePreferences(
    string Map = "maps/(2)Boxer.scm",
    string PlayerRace = "Protoss",
    bool AllowUserControl = false,
    int HumanPlayerSlots = 0,
    int ComputerPlayerSlots = 1,
    string[] ComputerRaces = null!,
    string[] PlayerSlots = null!,
    string AutoMenu = "SINGLE_PLAYER"
) { }

public enum Race
{
    Terran,
    Protoss,
    Zerg,
    Random,
}
