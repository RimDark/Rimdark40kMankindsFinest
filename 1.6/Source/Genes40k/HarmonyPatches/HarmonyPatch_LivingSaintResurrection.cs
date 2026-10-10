using HarmonyLib;
using RimWorld;
using Verse;

namespace Genes40k;

[HarmonyPatch(typeof(IncidentWorker), "TryExecute")]
public class LivingSaintResurrection
{
    public static void Postfix(IncidentWorker __instance, bool __result, IncidentParms parms)
    {
        if (!__result || !Genes40kUtils.ModSettings.livingSaintSystem)
        {
            return;
        }
            
        var gameComponent = Current.Game.GetComponent<GameComponent_LivingSaint>();
        gameComponent?.TrySpawnSaint(__instance.def.category, parms?.target);
    }    
}