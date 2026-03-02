using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class CPHInline
{
    private static string EscapeJson(string s)
    {
        return (s ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
    }

    public bool Execute()
    {
        string idsRaw = CPH.GetGlobalVar<string>("battleParticipantIds", true) ?? string.Empty;
        List<string> ids = idsRaw
            .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var participants = new List<(string Name, int Health, string Id)>();
        foreach (string id in ids)
        {
            string name = CPH.GetGlobalVar<string>($"battle_{id}_name", true) ?? string.Empty;
            int health = CPH.GetGlobalVar<int?>($"battle_{id}_health", true) ?? 0;
            if (!string.IsNullOrWhiteSpace(name) && health > 0)
                participants.Add((name, health, id));
        }

        if (participants.Count < 2)
        {
            CPH.SendYouTubeMessage("Need at least 2 clocked-out fighters to start the battle.");
            CPH.LogInfo($"[BattleTrigger] Not enough participants. count={participants.Count}");
            return false;
        }

        StringBuilder sb = new StringBuilder();
        sb.Append("{\"event\":\"battle_load\",\"participants\":[");
        for (int i = 0; i < participants.Count; i++)
        {
            if (i > 0) sb.Append(',');
            sb.Append("{\"userName\":\"").Append(EscapeJson(participants[i].Name)).Append("\",\"health\":").Append(participants[i].Health).Append('}');
        }
        sb.Append("]}");

        CPH.WebsocketBroadcastJson(sb.ToString());
        CPH.WebsocketBroadcastJson("{\"event\":\"battle_begin\"}");

        foreach (var p in participants)
        {
            CPH.UnsetGlobalVar($"battle_{p.Id}_name", true);
            CPH.UnsetGlobalVar($"battle_{p.Id}_health", true);
        }
        CPH.UnsetGlobalVar("battleParticipantIds", true);

        CPH.SendYouTubeMessage($"⚔ Clock-Out Battle Royale starting with {participants.Count} fighters!");
        CPH.LogInfo($"[BattleTrigger] Started battle with {participants.Count} participants.");
        return true;
    }
}
