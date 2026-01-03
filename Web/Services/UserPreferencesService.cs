using System.Text.Json;

namespace Web.Services;

public class UserPreferencesService
{
    private readonly string _preferencesFilePath = Path.Combine(
        Directory.GetCurrentDirectory(),
        "userGamePreference.json"
    );

    public GamePreferences? LoadPreferences()
    {
        if (!File.Exists(_preferencesFilePath))
        {
            Console.WriteLine("No saved preferences found, using defaults");
            return null;
        }

        var json = File.ReadAllText(_preferencesFilePath);
        var preferences = JsonSerializer.Deserialize<GamePreferences>(json);
        Console.WriteLine($"Loaded preferences from {_preferencesFilePath}");
        return preferences;
    }

    public void SavePreferences(GamePreferences preferences)
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        var json = JsonSerializer.Serialize(preferences, options);
        File.WriteAllText(_preferencesFilePath, json);
        Console.WriteLine($"Saved preferences to {_preferencesFilePath}");
    }
}
