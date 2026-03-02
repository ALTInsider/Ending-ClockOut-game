using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class CPHInline
{
    private static string ToOrdinal(int n)
    {
        int abs = Math.Abs(n);
        int lastTwo = abs % 100;
        if (lastTwo >= 11 && lastTwo <= 13) return $"{n}th";
        switch (abs % 10)
        {
            case 1: return $"{n}st";
            case 2: return $"{n}nd";
            case 3: return $"{n}rd";
            default: return $"{n}th";
        }
    }

    private static bool IsMilestone(int n)
    {
        if (n == 1 || n == 5 || n == 10 || n == 15 || n == 20 || n == 25 || n == 50 || n == 100) return true;
        if (n > 100 && n % 25 == 0) return true;
        return false;
    }

    private static string EscapeJson(string s)
    {
        return (s ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    private static void RegisterBattleParticipant(string userId, string userName, int health)
    {
        string idsRaw = CPH.GetGlobalVar<string>("battleParticipantIds", true) ?? string.Empty;
        List<string> ids = idsRaw
            .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (!ids.Contains(userId, StringComparer.Ordinal))
            ids.Add(userId);

        CPH.SetGlobalVar($"battle_{userId}_name", userName, true);
        CPH.SetGlobalVar($"battle_{userId}_health", health, true);
        CPH.SetGlobalVar("battleParticipantIds", string.Join("|", ids), true);
    }

    public bool Execute()
    {
        string platform = (args.ContainsKey("platform") ? args["platform"]?.ToString() : "youtube")
            ?.ToLowerInvariant() ?? "youtube";
        if (platform != "youtube")
        {
            CPH.LogInfo($"[YT ClockOut] Skipped non-YouTube platform: {platform}");
            return false;
        }

        string userId = args.ContainsKey("userId") ? args["userId"]?.ToString() ?? "" : "";
        string userName = args.ContainsKey("user") ? args["user"]?.ToString() ?? "Viewer" : "Viewer";

        if (string.IsNullOrWhiteSpace(userId))
        {
            CPH.LogInfo("[YT ClockOut] Missing userId; aborting.");
            return false;
        }

        int totalCheckIns = CPH.GetYouTubeUserVarById<int?>(userId, "totalCheckIns", true) ?? 0;
        if (totalCheckIns <= 0)
        {
            CPH.SendYouTubeMessage($"{userName}, you haven't clocked in yet. Nice try 🕒");
            return true;
        }

        DateTime now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Eastern Standard Time");
        string today = now.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string time = now.ToString("h:mm tt", CultureInfo.InvariantCulture);
        string lockKey = $"yt_{userId}_lastClockOutDate";

        string lastClockOutDate = CPH.GetGlobalVar<string>(lockKey, true) ?? "";
        if (lastClockOutDate == today)
        {
            CPH.SendYouTubeMessage($"{userName}, you already clocked out today 🕒");
            return true;
        }

        CPH.SetGlobalVar(lockKey, today, true);

        RegisterBattleParticipant(userId, userName, totalCheckIns);

        string clockoutJson = $"{{\"event\":\"clockout\",\"userName\":\"{EscapeJson(userName)}\",\"health\":{totalCheckIns}}}";
        CPH.WebsocketBroadcastJson(clockoutJson);

        string ordinal = ToOrdinal(totalCheckIns);
        string message = $"{userName} clocked out at {time} ET. Visit #{ordinal} since June 3rd, 2025. What a legend.";

        if (IsMilestone(totalCheckIns))
            message += $" 🎉 That's visit #{ordinal} — a milestone! WPIG1651 salutes you 🫡";

        if (message.Length > 200) message = message.Substring(0, 197) + "...";

        CPH.LogInfo($"[YT ClockOut] OK userId={userId} user={userName} total={totalCheckIns} registeredForBattle=true");
        CPH.SendYouTubeMessage(message);
        return true;
    }
}
