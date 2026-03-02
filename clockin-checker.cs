using System;

public class CPHInline
{
    private static bool IsMilestone(int n)
    {
        if (n == 1 || n == 5 || n == 10 || n == 25 || n == 50) return true;
        if (n >= 100 && n % 50 == 0) return true;
        return false;
    }

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

    public bool Execute()
    {
        string cmd = (args.ContainsKey("command") ? args["command"]?.ToString() : "")
            ?.TrimStart('!').ToLowerInvariant() ?? "";
        if (cmd != "clockin") return false;

        string platform = (args.ContainsKey("platform") ? args["platform"]?.ToString() : "youtube")
            ?.ToLowerInvariant() ?? "youtube";
        if (platform != "youtube")
        {
            CPH.LogInfo($"[YT ClockIn] Skipped non-YouTube platform: {platform}");
            return false;
        }

        string user = args["user"].ToString();
        string userId = args["userId"].ToString();
        string pfpUrl = args.ContainsKey("userProfileUrl") ? args["userProfileUrl"]?.ToString() ?? "" : "";

        DateTime now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTime.UtcNow, "Eastern Standard Time");
        string today = now.ToString("yyyy-MM-dd");
        string dailyKey = $"yt_{userId}_lastYTClockInDate";

        string last = CPH.GetGlobalVar<string>(dailyKey, true) ?? "";
        if (last == today)
        {
            CPH.SendYouTubeMessage($"{user}, you've already clocked in today ✅");
            return false;
        }

        int totalCheckIns = CPH.GetYouTubeUserVarById<int?>(userId, "totalCheckIns", true) ?? 0;
        totalCheckIns++;

        CPH.SetYouTubeUserVarById(userId, "totalCheckIns", totalCheckIns, true);
        CPH.SetGlobalVar(dailyKey, today, true);

        string ordinal = ToOrdinal(totalCheckIns);
        string message = (totalCheckIns == 1)
            ? $"Wuut? That was {user}'s first ever clock-in! Welcome to the studio🙏"
            : $"{user}, that's your {ordinal} time stopping by the studio since June 3rd, 2025. WPIG1651 salutes you🫡";

        CPH.LogInfo($"[YT ClockIn] Sending message for {userId} ({user}) total={totalCheckIns}");
        CPH.SendYouTubeMessage(message);

        if (IsMilestone(totalCheckIns))
        {
            string safeUser = user.Replace("\\", "\\\\").Replace("\"", "\\\"");
            string safePfpUrl = pfpUrl.Replace("\\", "\\\\").Replace("\"", "\\\"");
            string json = $"{{\"event\":\"clockin_milestone\",\"userName\":\"{safeUser}\",\"userProfileUrl\":\"{safePfpUrl}\",\"checkIns\":{totalCheckIns}}}";
            CPH.WebsocketBroadcastJson(json);
            CPH.LogInfo($"[YT ClockIn] Milestone triggered for {user} at #{totalCheckIns}");
        }

        return true;
    }
}
