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
            CPH.LogInfo($"[YT ClockGout] Skipped non-YouTube platform: {platform}");
            return false;
        }

        string userId = args.ContainsKey("userId") ? args["userId"]?.ToString() ?? "" : "";
        string userName = args.ContainsKey("user") ? args["user"]?.ToString() ?? "Viewer" : "Viewer";

        if (string.IsNullOrWhiteSpace(userId))
        {
            CPH.LogInfo("[YT ClockGout] Missing userId; aborting.");
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
        string lockKey = $"yt_{userId}_lastClockOutDate";

        string lastClockOutDate = CPH.GetGlobalVar<string>(lockKey, true) ?? "";
        if (lastClockOutDate == today)
        {
            CPH.SendYouTubeMessage($"{userName}, you already clocked gout today 🕒");
            return true;
        }

        CPH.SetGlobalVar(lockKey, today, true);

        RegisterBattleParticipant(userId, userName, totalCheckIns);

        string clockoutJson = $"{{\"event\":\"clockout\",\"userName\":\"{EscapeJson(userName)}\",\"health\":{totalCheckIns}}}";
        CPH.WebsocketBroadcastJson(clockoutJson);

        string ordinal = ToOrdinal(totalCheckIns);
        string message;

        if (IsMilestone(totalCheckIns))
        {
            message = $"🎉 {userName}, your {ordinal} visit to the studio — that's a milestone! WPIG1651 salutes you 🫡";
        }
        else
        {
            string[] goutTips = new string[]
            {
                "Daily Check: Consistency in hydration matters more than chugging once.",
                "Portion Control: Smaller servings reduce purine load.",
                "Protein Balance: Rotate protein sources through the week.",
                "Lentil Logic: Plant proteins are generally safer than red meat.",
                "Turkey Tip: Lean poultry in moderation is usually tolerated.",
                "Avoid Organ Meats: They're extremely high in purines.",
                "Bone Broth Caution: Concentrated broths can spike uric acid.",
                "Weight Stability: Big weight swings increase flare risk.",
                "Stay Cool: Heat and dehydration worsen inflammation.",
                "Foot Elevation: Elevate swollen joints during flares.",
                "Recovery Rule: Rest inflamed joints temporarily.",
                "Compression Check: Light support socks may reduce swelling.",
                "Evening Water: Hydrate before bed if prone to night flares.",
                "Moderate Seafood: Shellfish deserves special caution.",
                "Watch Energy Drinks: Sugar + dehydration combo.",
                "Coffee Count: Moderate is helpful — excess isn't.",
                "Avoid Sugary Smoothies: Liquid sugar hits fast.",
                "Strength Training: Muscle mass helps metabolic balance.",
                "Avoid Extreme Keto: Rapid ketosis may raise uric acid.",
                "Check Medications: Some diuretics affect uric acid.",
                "Fruit Focus: Whole fruit > fruit juice.",
                "Inflammation Guard: Omega-3 rich foods may help.",
                "Stay Active: Light movement even during mild soreness.",
                "Limit Gravy: Concentrated meat drippings are purine-heavy.",
                "Holiday Strategy: Plan indulgences carefully.",
                "Hydration Timer: Set reminders if you forget to drink.",
                "Don't Ignore Flares: Early treatment shortens duration.",
                "Consult Regularly: Monitor levels with your clinician.",
                "Balanced Breakfast: Avoid starting the day with sugar spikes.",
                "Meal Timing: Regular meals stabilize metabolism.",
                "Avoid Crash Fasting: Sudden restriction increases uric acid.",
                "Keep It Boring: Simple diets are easier to maintain.",
                "Shoes Matter: Protect the big toe joint.",
                "Reduce Soda: Even diet soda can affect habits.",
                "Eat Mindfully: Slower eating reduces overconsumption.",
                "Avoid Late Alcohol: Evening drinks increase overnight risk.",
                "Lean Cooking: Grill, bake, or steam instead of frying.",
                "Veggie Volume: Fill half your plate with vegetables.",
                "Avoid Overeating: Large meals strain metabolism.",
                "Hydrate During Exercise: Replace fluids lost through sweat.",
                "Avoid Sweet Sauces: Teriyaki, BBQ, and glazes hide sugar.",
                "Track Uric Acid Trends: Patterns beat guesswork.",
                "Gradual Change: Sustainable beats extreme.",
                "Know Family History: Genetics increase awareness.",
                "Plan Social Events: Eat beforehand to avoid overeating.",
                "Use Ice Early: Faster swelling control.",
                "Limit Processed Snacks: Jerky and meat sticks are risky.",
                "Stick With Routine: Gout prefers chaos — don't give it any.",
                "Prioritize Recovery: Stress management reduces flare likelihood.",
                "Stay Informed: Knowledge reduces fear and confusion."
            };

            string randomTip = goutTips[new Random().Next(goutTips.Length)];
            message = $"{userName} clocked gout — visit #{ordinal} since June 3rd. 🦶 {randomTip}";
        }

        if (message.Length > 200) message = message.Substring(0, 197) + "...";

        CPH.LogInfo($"[YT ClockGout] OK userId={userId} user={userName} total={totalCheckIns} registeredForBattle=true");
        CPH.SendYouTubeMessage(message);
        return true;
    }
}
