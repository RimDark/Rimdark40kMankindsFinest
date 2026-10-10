using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Genes40k;

public class GameComponent_LivingSaint : GameComponent
{
    private List<Pawn> livingSaints = new();
    public List<Pawn> LivingSaints => livingSaints;
        
    private Genes40kModSettings modSettings;
        
    public int LivingSaintsCount => livingSaints.Count;

    public GameComponent_LivingSaint(Game game)
    {
        modSettings = LoadedModManager.GetMod<Genes40kMod>().GetSettings<Genes40kModSettings>();
    }

    public void TrySpawnSaint(IncidentCategoryDef categoryDef, IIncidentTarget target)
    {
        if (livingSaints.Count <= 0)
        {
            return;
        }

        if (!Enumerable.Any(livingSaints, p => p is { Dead: true }))
        {
            return;
        }
            
        int chance;
        if (categoryDef == IncidentCategoryDefOf.ThreatBig)
        {
            chance = modSettings.livingSaintBigThreat;
        }
        else if (categoryDef == IncidentCategoryDefOf.ThreatSmall)
        {
            chance = modSettings.livingSaintSmallThreat;
        }
        else
        {
            return;
        }
        if (Prefs.DevMode && DebugSettings.godMode)
        {
            chance = 200;
        }
        if (Rand.Chance(chance / 100f))
        {
            SpawnSaint(target as Map);
        }
    }

    private void SpawnSaint(Map map)
    {
        if (map == null)
        {
            return;
        }

        livingSaints.RemoveAll(saint => saint == null || saint.Discarded);

        var deadSaints = livingSaints.Where(saint => saint.Dead).ToList();

        if (!deadSaints.Any() || !TryFindArrivalCell(map, out var cell))
        {
            return;
        }

        var toSpawn = deadSaints.RandomElement();

        if (!ResurrectionUtility.TryResurrect(toSpawn, new ResurrectionParams { dontSpawn = true, removeDiedThoughts = false }))
        {
            return;
        }

        GenSpawn.Spawn(toSpawn, cell, map);

        var letter = LetterMaker.MakeLetter("BEWH.MankindsFinest.LivingSaint.LivingSaintReturn".Translate(), "BEWH.MankindsFinest.LivingSaint.LivingSaintReturnMessage".Translate(toSpawn), Genes40kDefOf.BEWH_GoldenPositive, toSpawn);
        Find.LetterStack.ReceiveLetter(letter);
    }

    /// <summary>
    /// Finds a free cell next to a random colonist on the threatened map, preferring colonists who are still standing.
    /// </summary>
    private static bool TryFindArrivalCell(Map map, out IntVec3 cell)
    {
        cell = IntVec3.Invalid;

        var colonists = map.mapPawns.FreeColonistsSpawned;

        if (colonists.NullOrEmpty())
        {
            return false;
        }

        if (!colonists.Where(colonist => !colonist.Downed).TryRandomElement(out var anchor))
        {
            anchor = colonists.RandomElement();
        }

        return CellFinder.TryFindRandomSpawnCellForPawnNear(anchor.Position, map, out cell);
    }

    public void AddSaintToSpawnable(Pawn pawn)
    {
        if (livingSaints.Contains(pawn))
        {
            return;
        }
            
        livingSaints.Add(pawn);
    }
        
    public void RemoveSaintFromSpawnable(Pawn pawn)
    {
        if (!livingSaints.Contains(pawn))
        {
            return;
        }
            
        livingSaints.Remove(pawn);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref livingSaints, "livingSaints", LookMode.Reference);

        if (Scribe.mode != LoadSaveMode.PostLoadInit)
        {
            return;
        }

        livingSaints ??= new List<Pawn>();
        livingSaints.RemoveAll(saint => saint == null);
    }

    public override void LoadedGame()
    {
        base.LoadedGame();
        KeepDeadSaintsInWorld();
    }

    /// <summary>
    /// Saints that died before dead saints were kept as world pawns could still be discarded by world pawn cleanup.
    /// </summary>
    private void KeepDeadSaintsInWorld()
    {
        foreach (var saint in livingSaints)
        {
            if (saint is not { Dead: true, Discarded: false } || saint.Spawned || saint.Corpse is { } corpse && (corpse.Spawned || corpse.ParentHolder != null))
            {
                continue;
            }

            if (Find.WorldPawns.Contains(saint))
            {
                Find.WorldPawns.ForcefullyKeptPawns.Add(saint);
                continue;
            }

            Find.WorldPawns.PassToWorld(saint, PawnDiscardDecideMode.KeepForever);
        }
    }
}