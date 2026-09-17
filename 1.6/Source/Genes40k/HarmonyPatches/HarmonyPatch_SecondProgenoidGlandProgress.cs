using System.Text;
using HarmonyLib;
using Verse;

namespace Genes40k;

[HarmonyPatch(typeof(Corpse), "GetInspectString")]
public class SecondProgenoidGlandProgress
{
    public static void Postfix(ref string __result, Corpse __instance)
    {
        if (__instance.InnerPawn?.genes?.GetGene(Genes40kDefOf.BEWH_ProgenoidGlands) is not Gene_ProgenoidGlands { Active: true } progenoidGlands)
        {
            return;
        }
            
        string line = progenoidGlands.SecondProgenoidGlandHarvested
            ? "BEWH.MankindsFinest.SpaceMarine.SecondGeneseedsHarvested".Translate()
            : "BEWH.MankindsFinest.SpaceMarine.SecondGeneseedsHarvestable".Translate();

        __result = __result.NullOrEmpty()
            ? line
            : __result.TrimEndNewlines() + "\n" + line;
    }
}