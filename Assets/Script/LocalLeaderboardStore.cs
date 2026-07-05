using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class LocalLeaderboardEntry
{
    public string playerId;
    public int score;
    public int pressureFullCount;
    public long achievedAtUtcTicks;
}

public static class LocalLeaderboardStore
{
    [Serializable]
    private sealed class LeaderboardFile
    {
        public List<LocalLeaderboardEntry> entries = new List<LocalLeaderboardEntry>();
    }

    private const int MaxEntries = 10;
    private const string FileName = "dog_leaderboard.json";

    public static IReadOnlyList<LocalLeaderboardEntry> AddEntry(
        string playerId,
        int score,
        int pressureFullCount)
    {
        LeaderboardFile leaderboard = LoadFile();
        leaderboard.entries.Add(new LocalLeaderboardEntry
        {
            playerId = string.IsNullOrWhiteSpace(playerId) ? "未命名" : playerId.Trim(),
            score = score,
            pressureFullCount = Mathf.Max(0, pressureFullCount),
            achievedAtUtcTicks = DateTime.UtcNow.Ticks
        });

        SortAndTrim(leaderboard.entries);
        SaveFile(leaderboard);
        return new List<LocalLeaderboardEntry>(leaderboard.entries);
    }

    public static IReadOnlyList<LocalLeaderboardEntry> LoadTopEntries()
    {
        LeaderboardFile leaderboard = LoadFile();
        SortAndTrim(leaderboard.entries);
        return new List<LocalLeaderboardEntry>(leaderboard.entries);
    }

    private static LeaderboardFile LoadFile()
    {
        string path = GetFilePath();

        if (!File.Exists(path))
        {
            return new LeaderboardFile();
        }

        try
        {
            string json = File.ReadAllText(path);
            LeaderboardFile leaderboard = JsonUtility.FromJson<LeaderboardFile>(json);

            if (leaderboard == null)
            {
                return new LeaderboardFile();
            }

            if (leaderboard.entries == null)
            {
                leaderboard.entries = new List<LocalLeaderboardEntry>();
            }

            leaderboard.entries.RemoveAll(entry => entry == null);
            return leaderboard;
        }
        catch (Exception exception)
        {
            Debug.LogError($"读取本地排行榜失败：{exception.Message}");
            return new LeaderboardFile();
        }
    }

    private static void SaveFile(LeaderboardFile leaderboard)
    {
        try
        {
            string json = JsonUtility.ToJson(leaderboard, true);
            File.WriteAllText(GetFilePath(), json);
        }
        catch (Exception exception)
        {
            Debug.LogError($"保存本地排行榜失败：{exception.Message}");
        }
    }

    private static void SortAndTrim(List<LocalLeaderboardEntry> entries)
    {
        entries.Sort(CompareEntries);

        if (entries.Count > MaxEntries)
        {
            entries.RemoveRange(MaxEntries, entries.Count - MaxEntries);
        }
    }

    private static int CompareEntries(LocalLeaderboardEntry left, LocalLeaderboardEntry right)
    {
        int scoreComparison = right.score.CompareTo(left.score);

        if (scoreComparison != 0)
        {
            return scoreComparison;
        }

        return left.achievedAtUtcTicks.CompareTo(right.achievedAtUtcTicks);
    }

    private static string GetFilePath()
    {
        return Path.Combine(Application.persistentDataPath, FileName);
    }
}
