using HarmonyLib;
using RimWorld;
using Verse;

namespace Genes40k;

//Scenarios with a fixed pawn kind list (Lost Strike Force) fill the extra choice slots with the scenario's
//leftBehindPawnKind, but vanilla forces those pawns to Baseliner. Let a kind with its own xenotype set use it,
//the same way vanilla already does for the listed kinds.
[HarmonyPatch(typeof(StartingPawnUtility), "DefaultStartingPawnRequest", MethodType.Getter)]
public static class StartingPawnKindXenotype
{
    public static void Postfix(ref PawnGenerationRequest __result)
    {
        if (!ModsConfig.BiotechActive || Find.GameInitData?.startingPawnKind == null)
        {
            return;
        }

        if (__result.ForcedXenotype == XenotypeDefOf.Baseliner && __result.KindDef?.xenotypeSet != null)
        {
            __result.ForcedXenotype = null;
        }
    }
}
