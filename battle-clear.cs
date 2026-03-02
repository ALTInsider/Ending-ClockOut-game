using System;
using System.Collections.Generic;
using System.Linq;

public class CPHInline
{
    public bool Execute()
    {
        string idsRaw = CPH.GetGlobalVar<string>("battleParticipantIds", true) ?? string.Empty;
        List<string> ids = idsRaw
            .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (string id in ids)
        {
            CPH.UnsetGlobalVar($"battle_{id}_name", true);
            CPH.UnsetGlobalVar($"battle_{id}_health", true);
        }

        CPH.UnsetGlobalVar("battleParticipantIds", true);

        CPH.LogInfo($"[BattleClear] Cleared battle queue. participantsRemoved={ids.Count}");
        CPH.SendYouTubeMessage("🧹 Battle queue cleared for next stream.");
        return true;
    }
}
