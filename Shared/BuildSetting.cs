using BWAPI.NET;
using System.Text.Json;
using System.IO;

namespace Shared;


public class BuildSetting
{
    public static BuildSetting GetSettings()
    {
        try
        {
            
            string json = File.ReadAllText("BuildSettings.json");
            var settings = JsonSerializer.Deserialize<BuildSetting>(json);
            if (settings != null)
            {
                Console.WriteLine("Build settings loaded from file.");
                return settings;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error reading build settings: {ex.Message}");
            // Use defaults if file not found or invalid
        }
        Console.WriteLine("Using default build settings.");
        return new BuildSetting();
    }

    public int InitialFirstProbes { get; set; } = 5;
    public int InitialProbesBeforeForge { get; set; } = 12;
    public int InitialProbesToChangeState { get; set; } = 15;

    public int EarlyCannonThreshold { get; set; } = 4;
    public int EarlyGatewayThreshold { get; set; } = 2;
    public int EarlyGamePylons { get; set; } = 3;
    public int EarlyGameProbes { get; set; } = 20;
}