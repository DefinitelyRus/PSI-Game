using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
namespace CommonScripts;

public partial class DataManager : Node {

    #region Instance Members

    #region Godot Callbacks

    public override void _EnterTree() {
        if (Instance != null) {
            Log.Err("Multiple instances of DataManager detected! There should only be one instance in the scene tree.");
            QueueFree();
            return;
        }

        Instance = this;
    }

    #endregion

    #endregion

    #region Static Members

    public static DataManager Instance { get; private set; } = null!;

    public static List<TelemetryData> Data { get; private set; } = [];

    public static JsonSerializerOptions JsonOptions { get; private set; } = new() {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static void SaveData(string key, Level? level, Variant value) {
        TelemetryData data = new(key, level, value);
        Data.Add(data);
    }

    public static void ExportData() {
		// Save to the user's desktop folder.
		string fileName = AIDirector.Instance != null ? AIDirector.Instance.Mode switch {
			AIDirector.SpawnMode.Dynamic => "PSI_Data_Dynamic",
			AIDirector.SpawnMode.Static => "PSI_Data_Static",
			_ => "PSI_Data_Unknown",
		} : "PSI_Data_Unknown";

		string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string filePath = System.IO.Path.Combine(desktopPath, fileName + ".json");

        // Build JSON-serializable payload
        List<Dictionary<string, object?>> exportData = [];

        foreach (TelemetryData datum in Data) {
            exportData.Add(datum.ToDictionary());
        }

        try {
            string jsonString = JsonSerializer.Serialize(exportData, JsonOptions);
            System.IO.File.WriteAllText(filePath, jsonString);
            Log.Me($"Telemetry exported to: {filePath}");
        }
        
        catch (Exception ex) {
            Log.Err($"Failed to export telemetry: {ex.Message}");
        }
    }

    public static void ClearData() {
        Data.Clear();
    }

    #endregion

}

public class TelemetryData {
    public string DataID { get; set; }
    public Variant Value { get; set; }
    public int LevelID { get; set; } = -1;
    public double LevelTimeStamp { get; set; } = -1;
    public DateTime Timestamp { get; set; } = DateTime.Now;

    public TelemetryData(string dataID, Level? level, Variant value) {
        DataID = dataID;
        Value = value;

        if (level != null) {
            LevelID = (int) level.LevelIndex;
            LevelTimeStamp = level?.LevelTimeLimit - GameManager.TimeRemaining ?? -1;
        }

        Timestamp = DateTime.Now;
    }

    public Dictionary<string, object?> ToDictionary() {
        return new Dictionary<string, object?> {
            { "DataID", DataID },
            { "Value", NormalizeVariant(Value) },
            { "LevelID", LevelID },
            { "LevelTimeStamp", LevelTimeStamp },
            { "Timestamp", Timestamp.ToString("o") }
        };
    }

    private static object? NormalizeVariant(Variant v) {
        switch (v.VariantType) {
            case Variant.Type.Nil:
                return null;
            case Variant.Type.Bool:
                return (bool)v;
            case Variant.Type.Int:
                // Godot Variant int is 64-bit; JSON will handle number
                return (long)v;
            case Variant.Type.Float:
                return (double)v;
            case Variant.Type.String:
                return (string)v;
            case Variant.Type.Vector2: {
                Vector2 val = (Vector2)v;
                return new { x = val.X, y = val.Y };
            }
            case Variant.Type.Vector3: {
                Vector3 val = (Vector3)v;
                return new { x = val.X, y = val.Y, z = val.Z };
            }
            case Variant.Type.Color: {
                Color c = (Color)v;
                return new { r = c.R, g = c.G, b = c.B, a = c.A };
            }
            case Variant.Type.Array: {
                var arr = (Godot.Collections.Array)v;
                List<object?> list = new();
                foreach (Variant item in arr) list.Add(NormalizeVariant(item));
                return list;
            }
            case Variant.Type.Dictionary: {
                var dict = (Godot.Collections.Dictionary)v;
                Dictionary<string, object?> outDict = new();
                foreach (var kv in dict) {
                    string key = kv.Key.ToString();
                    Variant val = (Variant)kv.Value;
                    outDict[key] = NormalizeVariant(val);
                }
                return outDict;
            }
            default:
                // Fallback: string representation to avoid breaking serialization
                return v.ToString();
        }
    }
}