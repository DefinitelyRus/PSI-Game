using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using Godot;
using Game;
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
    private static bool _gameStartRecorded = false;

    public static List<TelemetryData> Data { get; private set; } = [];

    public static JsonSerializerOptions JsonOptions { get; private set; } = new() {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static void SaveData(string key, Level? level, Variant value) {
        TelemetryData data = new(key, level, value);
        Data.Add(data);
    }

        public static void SaveData(string key, Level? level, Godot.Collections.Dictionary dict) {
            SaveData(key, level, (Variant) dict);
        }

    public static void ExportData() {
		// Save to the user's desktop folder.
		string fileName = AIDirector.Instance != null ? AIDirector.Instance.Mode switch {
			AIDirector.SpawnMode.Dynamic => "PSI_Data_Dynamic",
			AIDirector.SpawnMode.Static => "PSI_Data_Static",
			_ => "PSI_Data_Unknown",
		} : "PSI_Data_Unknown";

    string desktopPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
    string homePath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
    string exportDir = System.IO.Directory.Exists(desktopPath) ? desktopPath : (!string.IsNullOrEmpty(homePath) ? homePath : ".");
    try { if (!System.IO.Directory.Exists(exportDir)) System.IO.Directory.CreateDirectory(exportDir); } catch {}
    string filePath = System.IO.Path.Combine(exportDir, fileName + ".json");

        // Build JSON-serializable payload
        List<Dictionary<string, object?>> exportData = [];

        foreach (TelemetryData datum in Data) {
            exportData.Add(datum.ToDictionary());
        }

        try {
            string jsonString = JsonSerializer.Serialize(exportData, JsonOptions);
            System.IO.File.WriteAllText(filePath, jsonString);
            long size = jsonString.Length;
            Log.Me($"Telemetry exported. Entries={exportData.Count} SizeChars={size} Path={filePath}");
        }
        
        catch (Exception ex) {
            Log.Err($"Failed to export telemetry: {ex.Message}");
        }
    }

    public static void TestWriteSample() {
    SaveData("test_entry", null, (Variant)123);
        ExportData();
    }

    public static void ClearData() {
        Data.Clear();
    }

    public static void RecordLevelCompletion(Level level) {
        if (level == null) return;
    double completionTime = level.LevelTimeLimit - GameManager.TimeRemaining;
    completionTime = Math.Round(completionTime, 2);
        Godot.Collections.Array unitsArr = [];
        foreach (StandardCharacter c in Commander.GetAllUnits()) {
            if (c == null || !c.IsAlive) continue;
            Godot.Collections.Dictionary unitDict = new() {
                {"unitInstanceID", c.InstanceID},
                {"health", Math.Round(c.Health, 2)},
                {"maxHealth", Math.Round(c.CurrentMaxHealth, 2)}
            };
            Godot.Collections.Array upgradesArr = [];
            if (c.UpgradeManager != null) {
                foreach (StandardItem item in c.UpgradeManager.Items) {
                    if (item is UpgradeItem up) {
                        upgradesArr.Add(new Godot.Collections.Dictionary {
                            {"upgradeInstanceID", up.InstanceID},
                            {"isEquipped", up.IsEquipped}
                        });
                    }
                }
            }
            unitDict["upgrades"] = upgradesArr;
            unitsArr.Add(unitDict);
        }
        Godot.Collections.Array completedObjectivesArr = [];
        int requiredCompleted = AIDirector.RequiredObjectivesCompleted;
        int optionalCompleted = AIDirector.OptionalObjectivesCompleted;
        completedObjectivesArr.Add(new Godot.Collections.Dictionary {{"requiredObjectivesCompleted", requiredCompleted}});
        completedObjectivesArr.Add(new Godot.Collections.Dictionary {{"optionalObjectivesCompleted", optionalCompleted}});
        Godot.Collections.Dictionary payload = new() {
            {"levelIndex", level != null ? (Variant) level.LevelIndex : new Variant()},
            {"completionTime", completionTime},
            {"requiredObjectivesCompleted", requiredCompleted},
            {"requiredObjectivesTotal", AIDirector.RequiredObjectivesTotal},
            {"optionalObjectivesCompleted", optionalCompleted},
            {"optionalObjectivesTotal", AIDirector.OptionalObjectivesTotal},
            {"units", unitsArr},
            {"objectives", completedObjectivesArr}
        };
        SaveData("level_complete", level, payload);
        ExportData();
    }

    public static void RecordPanelInteraction(StandardPanel panel, StandardCharacter unit, Godot.Collections.Dictionary extra) {
        if (panel == null || unit == null) return;
        float ux = (float)Math.Round(unit.GlobalPosition.X, 2);
        float uy = (float)Math.Round(unit.GlobalPosition.Y, 2);
        float px = (float)Math.Round(panel.GlobalPosition.X, 2);
        float py = (float)Math.Round(panel.GlobalPosition.Y, 2);
        Godot.Collections.Dictionary payload = new() {
            {"panelInstanceID", panel.PropID},
            {"panelType", panel.PanelType.ToString()},
            {"unitInstanceID", unit.InstanceID},
            {"unitPosition", new Godot.Collections.Dictionary {{"x", ux}, {"y", uy}}},
            {"panelPosition", new Godot.Collections.Dictionary {{"x", px}, {"y", py}}},
            {"timestamp", DateTime.Now.ToString("MM/dd/yy HH:mm:ss")}
        };
        foreach (var kv in extra) payload[kv.Key.ToString()] = kv.Value;
        Level? level = SceneLoader.Instance.LoadedScene as Level;
        SaveData("panel_interact", level, payload);
    }

    public static void RecordInventoryEvent(StandardCharacter unit, UpgradeItem item, string eventType, int currentPower, int maxPower, int usedSlots, int maxSlots) {
        if (unit == null || item == null) return;
        float ux = (float)Math.Round(unit.GlobalPosition.X, 2);
        float uy = (float)Math.Round(unit.GlobalPosition.Y, 2);
        Godot.Collections.Dictionary payload = new() {
            {"eventType", eventType},
            {"unitInstanceID", unit.InstanceID},
            {"itemInstanceID", item.InstanceID},
            {"unitPosition", new Godot.Collections.Dictionary {{"x", ux}, {"y", uy}}},
            {"currentPower", currentPower},
            {"maxPower", maxPower},
            {"usedSlots", usedSlots},
            {"maxSlots", maxSlots},
            {"timestamp", DateTime.Now.ToString("MM/dd/yy HH:mm:ss")}
        };
        Level? level = SceneLoader.Instance.LoadedScene as Level;
        SaveData("inventory_event", level, payload);
    }

    public static void RecordGameStart() {
        if (_gameStartRecorded) return;
        string gameVersion = string.Empty;
        try {
            if (ProjectSettings.HasSetting("application/config/version")) {
                Variant v = ProjectSettings.GetSetting("application/config/version");
                gameVersion = v.ToString();
            }
        } catch {}
        if (string.IsNullOrEmpty(gameVersion)) {
            try {
                var info = Engine.GetVersionInfo();
                if (info != null && info.Count > 0) {
                    string major = info.ContainsKey("major") ? info["major"].ToString() : "";
                    string minor = info.ContainsKey("minor") ? info["minor"].ToString() : "";
                    string patch = info.ContainsKey("patch") ? info["patch"].ToString() : "";
                    gameVersion = $"{major}.{minor}.{patch}".Trim('.');
                }
            } catch {}
        }
        string mode = AIDirector.Instance != null ? AIDirector.Instance.Mode.ToString() : "";
    string cpu = GetCpuName();
    double ramGb = GetRamGb();
    string gpu = GetGpuName();
        Godot.Collections.Dictionary payload = new() {
            {"gameVersion", string.IsNullOrEmpty(gameVersion) ? "" : gameVersion},
            {"aiDirectorMode", mode},
            {"cpu", cpu},
            {"ramGB", Math.Round(ramGb, 2)},
            {"gpu", gpu},
            {"timestamp", DateTime.Now.ToString("MM/dd/yy HH:mm:ss")}
        };
        SaveData("game_start", null, payload);
        _gameStartRecorded = true;
    }

    private static string GetCpuName() {
        try {
            string os = OS.GetName();
            if (os == "Windows") {
                string? env = System.Environment.GetEnvironmentVariable("PROCESSOR_IDENTIFIER");
                if (!string.IsNullOrEmpty(env)) return env;
            }
            if (os == "Linux") {
                try {
                    foreach (string line in System.IO.File.ReadLines("/proc/cpuinfo")) {
                        if (line.StartsWith("model name")) {
                            int idx = line.IndexOf(':');
                            if (idx >= 0) return line[(idx + 1)..].Trim();
                        }
                        if (line.StartsWith("Hardware")) {
                            int idx = line.IndexOf(':');
                            if (idx >= 0) return line[(idx + 1)..].Trim();
                        }
                    }
                } catch {}
            }
        } catch {}
        return string.Empty;
    }

    private static double GetRamGb() {
        try {
            string os = OS.GetName();
            if (os == "Linux") {
                try {
                    foreach (string line in System.IO.File.ReadLines("/proc/meminfo")) {
                        if (line.StartsWith("MemTotal:")) {
                            string[] parts = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            foreach (string p in parts) {
                                if (long.TryParse(p, out long kB)) return kB / 1048576d;
                            }
                        }
                    }
                } catch {}
            }
            return 0d;
        } catch { return 0d; }
    }

    private static string GetGpuName() {
        try {
            return RenderingServer.GetVideoAdapterName();
        } catch { return string.Empty; }
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
        // Sanitize level timestamp: only emit when level is valid and timestamp is finite and non-negative
        object? levelTsOut = null;
        if (LevelID >= 0) {
            double ts = LevelTimeStamp;
            if (!double.IsNaN(ts) && !double.IsInfinity(ts) && ts >= 0 && ts < double.MaxValue/2) {
                levelTsOut = Math.Round(ts, 2);
            }
        }

        return new Dictionary<string, object?> {
            { "DataID", DataID },
            { "Value", NormalizeVariant(Value) },
            { "LevelID", LevelID >= 0 ? LevelID : null },
            { "LevelTimeStamp", levelTsOut },
            { "Timestamp", Timestamp.ToString("MM/dd/yy HH:mm:ss") }
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
                return Math.Round((double)v, 2);
            case Variant.Type.String:
                    return NormalizeStringPossiblyJson((string)v);
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
                List<object?> list = [];
                foreach (Variant item in arr) list.Add(NormalizeVariant(item));
                return list;
            }
            case Variant.Type.Dictionary: {
                var dict = (Godot.Collections.Dictionary)v;
                Dictionary<string, object?> outDict = [];
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

        private static object? NormalizeStringPossiblyJson(string s) {
            string trimmed = s.Trim();
            if (!(trimmed.StartsWith('{') || trimmed.StartsWith('['))) return s;
            try {
                using JsonDocument doc = JsonDocument.Parse(trimmed);
                return ConvertJsonElement(doc.RootElement);
            }
            catch {
                return s;
            }
        }

        private static object? ConvertJsonElement(JsonElement e) {
            switch (e.ValueKind) {
                case JsonValueKind.Object: {
                    Dictionary<string, object?> obj = [];
                    foreach (JsonProperty p in e.EnumerateObject()) obj[p.Name] = ConvertJsonElement(p.Value);
                    return obj;
                }
                case JsonValueKind.Array: {
                    List<object?> list = [];
                    foreach (JsonElement item in e.EnumerateArray()) list.Add(ConvertJsonElement(item));
                    return list;
                }
                case JsonValueKind.String:
                    return e.GetString();
                case JsonValueKind.Number:
                    if (e.TryGetInt64(out long l)) return l;
                    if (e.TryGetDouble(out double d)) return d;
                    return e.ToString();
                case JsonValueKind.True:
                    return true;
                case JsonValueKind.False:
                    return false;
                case JsonValueKind.Null:
                    return null;
                default:
                    return e.ToString();
            }
        }
}