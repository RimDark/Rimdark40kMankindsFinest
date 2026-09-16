using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using Verse;

namespace Genes40k;

public static class DebugActions
{
    [DebugAction("RimDark", "Get dead perpetual info", false, false, true, false, false,0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = -1000)]
    private static void DeadPerpetualInfo()
    {
        var gameComponentPerpetual = Current.Game.GetComponent<GameComponent_Perpetual>();
        Log.Message("Current dead perpetual amount: " + gameComponentPerpetual.Perpetuals?.Count);
        var currentTime = Current.Game.tickManager.TicksGame;
        foreach (var perpetualDict in gameComponentPerpetual.Perpetuals)
        {
            Log.Message("Perpetual: " + perpetualDict.Key);
            Log.Message("Time left: " + (perpetualDict.Value - currentTime));
        }
    }
    
    [DebugAction("RimDark", "Unlock all chapter trait materials", false, false, true, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing, displayPriority = -1000)]
    private static void UnlockAllChapterTraitMaterials()
    {
        var gameComp = GameComponent_UnlockedMaterials.Instance;
        if (gameComp == null)
        {
            return;
        }

        foreach (var trait in CustomChapterGeneUtility.AllTraits)
        {
            var requiredMaterials = trait.requiredAnyMaterial;
            if (requiredMaterials.NullOrEmpty())
            {
                continue;
            }

            foreach (var material in requiredMaterials.Where(material => !gameComp.HasMaterial(material)))
            {
                gameComp.UnlockMaterial(material);
            }
        }
    }

    [DebugAction("RimDark", "Spawn custom chapter gene-seed vial", false, false, true, false, false, 0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = -1000)]
    private static void SpawnCustomChapterVial()
    {
        var chapters = GameComponent_CustomChapterGenes.Instance?.Chapters;
        if (chapters.NullOrEmpty())
        {
            Messages.Message("No custom chapter gene-seeds exist.", MessageTypeDefOf.RejectInput, false);
            return;
        }

        var options = new List<DebugMenuOption>();
        foreach (var chapter in chapters)
        {
            switch (chapter.Kind)
            {
                case CustomGeneKind.Primarch:
                    options.Add(new DebugMenuOption(chapter.name + " (Primarch)", DebugMenuOptionMode.Tool, delegate
                    {
                        SpawnVialFor(chapter, Genes40kDefOf.BEWH_GeneseedVialPrimarch);
                    }));
                    break;
                case CustomGeneKind.Custodes:
                    options.Add(new DebugMenuOption(chapter.name + " (Custodes)", DebugMenuOptionMode.Tool, delegate
                    {
                        SpawnVialFor(chapter, Genes40kDefOf.BEWH_GeneseedVialCustodes);
                    }));
                    break;
                default:
                    options.Add(new DebugMenuOption(chapter.name + " (Firstborn)", DebugMenuOptionMode.Tool, delegate
                    {
                        SpawnVialFor(chapter, Genes40kDefOf.BEWH_GeneseedVialFirstborn);
                    }));
                    options.Add(new DebugMenuOption(chapter.name + " (Primaris)", DebugMenuOptionMode.Tool, delegate
                    {
                        SpawnVialFor(chapter, Genes40kDefOf.BEWH_GeneseedVialPrimaris);
                    }));
                    break;
            }
        }

        Find.WindowStack.Add(new Dialog_DebugOptionListLister(options));
    }

    private static void SpawnVialFor(CustomChapterGene chapter, ThingDef vialDef)
    {
        var map = Find.CurrentMap;
        var cell = UI.MouseCell();
        if (map == null || !cell.InBounds(map))
        {
            return;
        }

        var vial = (GeneseedVial)ThingMaker.MakeThing(vialDef);
        vial.extraGeneFromMaterial = chapter.GeneDef;
        vial.newGeneseedVialTexture = chapter.Template.vialTexturePath;
        chapter.locked = true;
        GenSpawn.Spawn(vial, cell, map);
    }

    [DebugAction("RimDark", "Get dead living saint info", false, false, true, false, false,0, false, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap, displayPriority = -1000)]
    private static void DeadLivingSaintInfo()
    {
        var gameComponentLivingSaint = Current.Game.GetComponent<GameComponent_LivingSaint>();
        Log.Message("Current dead living saint amount: " + gameComponentLivingSaint.LivingSaintsCount);
        foreach (var livingSaint in gameComponentLivingSaint.LivingSaints)
        {
            Log.Message("Living Saint: " + livingSaint);
        }
    }
}