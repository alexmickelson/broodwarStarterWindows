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
                return settings;
            }
        }
        catch
        {
            // Use defaults if file not found or invalid
        }
        return new BuildSetting();
    }

    public int FirstProbesToPylon { get; set; } = 5;
    public int ForgeProbeThreshold { get; set; } = 12;
    public int ChangeToEarlyGameProbes { get; set; } = 15;

    public int EarlyCannonThreshold { get; set; } = 4;
    public int EarlyGatewayThreshold { get; set; } = 2;
    public int EarlyGamePylons { get; set; } = 3;
}