using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace PlayerLevels;

[DataContract]
public sealed class LevelRecord
{
    [DataMember(Name = "level")] public int Level { get; set; } = 1;
    [DataMember(Name = "experience")] public int Experience { get; set; }
    [DataMember(Name = "total_points")] public long TotalPoints { get; set; }
    // Only used to migrate the old players.json data.
    [DataMember(Name = "multiplier")] public int Multiplier { get; set; } = 1;
}

[DataContract]
public sealed class LevelStore
{
    [DataMember(Name = "players")]
    public Dictionary<string, LevelRecord> Players { get; set; } =
        new(StringComparer.OrdinalIgnoreCase);

    public Dictionary<string, int> Multipliers { get; } =
        new(StringComparer.OrdinalIgnoreCase);

    public static int Requirement(int level) => level <= 10 ? 10 :
        level <= 50 ? 50 : level <= 100 ? 100 : level <= 200 ? 200 : 300;

    public LevelRecord Get(string userId)
    {
        if (!Players.TryGetValue(userId, out var value))
            Players[userId] = value = new LevelRecord();
        if (value.Level < 1) value.Level = 1;
        return value;
    }

    public int GetMultiplier(string userId) =>
        Multipliers.TryGetValue(userId, out int value) ? value : 1;

    public void SetMultiplier(string userId, int multiplier)
    {
        if (multiplier <= 1) Multipliers.Remove(userId);
        else Multipliers[userId] = multiplier;
    }

    public static LevelStore Load(string levelsPath, string multipliersPath)
    {
        string legacyPath = Path.Combine(Path.GetDirectoryName(levelsPath)
            ?? throw new InvalidDataException("等级存档路径无效"), "players.json");
        LevelStore? legacy = null;
        if ((!File.Exists(levelsPath) || !File.Exists(multipliersPath)) && File.Exists(legacyPath))
            legacy = LoadLegacy(legacyPath);

        var result = new LevelStore();
        if (File.Exists(levelsPath))
            result.ReadLevels(levelsPath);
        else if (legacy != null)
            result.Players = legacy.Players;

        if (File.Exists(multipliersPath))
            result.ReadMultipliers(multipliersPath);
        else if (legacy != null)
            foreach (var pair in legacy.Players)
                if (pair.Value.Multiplier > 1)
                    result.Multipliers[pair.Key] = pair.Value.Multiplier;

        // The old JSON stays untouched as an upgrade backup. TXT is authoritative.
        if (!File.Exists(levelsPath)) result.SaveLevels(levelsPath);
        if (!File.Exists(multipliersPath)) result.SaveMultipliers(multipliersPath);
        return result;
    }

    private static LevelStore LoadLegacy(string path)
    {
        using var stream = File.OpenRead(path);
        var serializer = new DataContractJsonSerializer(typeof(LevelStore));
        var result = serializer.ReadObject(stream) as LevelStore
            ?? throw new InvalidDataException("旧版 players.json 格式错误");
        result.Players ??= new Dictionary<string, LevelRecord>();
        result.Players = new Dictionary<string, LevelRecord>(result.Players, StringComparer.OrdinalIgnoreCase);
        return result;
    }

    private void ReadLevels(string path)
    {
        int lineNumber = 0;
        foreach (var line in File.ReadLines(path, Encoding.UTF8))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
            string[] columns = line.Split('|');
            if (columns.Length != 4 || !ValidId(columns[0].Trim()) ||
                !int.TryParse(columns[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int level) || level < 1 ||
                !int.TryParse(columns[2].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int experience) || experience < 0 ||
                !long.TryParse(columns[3].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out long total) || total < 0)
                throw new InvalidDataException($"players.txt 第 {lineNumber} 行无效，格式：SteamID | 等级 | 当前经验 | 累计积分");
            string id = columns[0].Trim();
            if (Players.ContainsKey(id)) throw new InvalidDataException($"players.txt 第 {lineNumber} 行 SteamID 重复：{id}");
            while (experience >= Requirement(level) && level < int.MaxValue)
            {
                experience -= Requirement(level);
                level++;
            }
            Players[id] = new LevelRecord { Level = level, Experience = experience, TotalPoints = total };
        }
    }

    private void ReadMultipliers(string path)
    {
        int lineNumber = 0;
        foreach (var line in File.ReadLines(path, Encoding.UTF8))
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
            string[] columns = line.Split('|');
            if (columns.Length != 2 || !ValidId(columns[0].Trim()) ||
                !int.TryParse(columns[1].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int multiplier) || multiplier < 1)
                throw new InvalidDataException($"multipliers.txt 第 {lineNumber} 行无效，格式：SteamID | 倍数");
            string id = columns[0].Trim();
            if (Multipliers.ContainsKey(id)) throw new InvalidDataException($"multipliers.txt 第 {lineNumber} 行 SteamID 重复：{id}");
            if (multiplier > 1) Multipliers[id] = multiplier;
        }
    }

    private static bool ValidId(string id) => id.Length > 0 && id.IndexOfAny(new[] { '\r', '\n', '|', '#' }) < 0;

    public void SaveLevels(string path)
    {
        var lines = new List<string> { "# SteamID | 等级 | 当前经验 | 累计积分", "# 示例：76561198000000000@steam | 12 | 20 | 130" };
        lines.AddRange(Players.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase).Select(pair =>
            $"{pair.Key} | {pair.Value.Level} | {pair.Value.Experience} | {pair.Value.TotalPoints}"));
        WriteAtomic(path, lines);
    }

    public void SaveMultipliers(string path)
    {
        var lines = new List<string> { "# SteamID | 经验倍数（未列出的玩家默认为 1 倍）", "# 示例：76561198000000000@steam | 2" };
        lines.AddRange(Multipliers.OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => $"{pair.Key} | {pair.Value}"));
        WriteAtomic(path, lines);
    }

    private static void WriteAtomic(string path, IEnumerable<string> lines)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)
            ?? throw new InvalidDataException("等级存档路径无效"));
        string temp = path + ".tmp";
        File.WriteAllLines(temp, lines, new UTF8Encoding(true));
        if (File.Exists(path)) File.Replace(temp, path, null);
        else File.Move(temp, path);
    }
}
