using RimWorld;
using RimWorld.Planet;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Verse;

namespace Genes40k;

public class GameComponent_Perpetual : GameComponent
{
    private Dictionary<Pawn ,int> perpetuals = new ();
    public Dictionary<Pawn ,int> Perpetuals => perpetuals;

    private List<Pawn> perpetualsKeysWorkingList;
    private List<int> perpetualsValuesWorkingList;
        
    private const int CheckInterval = 4000;
    private int currentTick;

    private bool legacyFormatLoaded;
    private List<string> legacyReferenceIds;
    private List<int> legacyReferenceTicks;
    private List<Pawn> legacyDeepPawns;
    private List<int> legacyDeepTicks;

    public GameComponent_Perpetual(Game game)
    {
    }

    public override void GameComponentTick()
    {
        if (perpetuals.Count > 0)
        {
            List<Pawn> unrecoverable = null;

            foreach (var trackedPawn in perpetuals.Keys)
            {
                if (trackedPawn.Destroyed || trackedPawn.Discarded)
                {
                    unrecoverable ??= new List<Pawn>();
                    unrecoverable.Add(trackedPawn);
                    continue;
                }

                KeepPawnForResurrection(trackedPawn);
            }

            if (unrecoverable != null)
            {
                foreach (var lostPawn in unrecoverable)
                {
                    perpetuals.Remove(lostPawn);
                }
            }
        }

        if (currentTick != CheckInterval)
        {
            currentTick++;  
            return;
        }

        currentTick = 0;
            
        var removeAfterResurrection = new List<Pawn>();
            
        foreach (var perpetual in perpetuals.Where(perpetual => Find.TickManager.TicksGame >= perpetual.Value))
        {
            if (perpetual.Key.genes?.GetFirstGeneOfType<Gene_Perpetual>() == null)
            {
                removeAfterResurrection.Add(perpetual.Key);
                continue;
            }
            if (perpetual.Key.Dead)
            {
                ResurrectionUtility.TryResurrect(perpetual.Key);
            }
                
            if (!perpetual.Key.Spawned && perpetual.Key.Corpse is { Spawned: false } or null)
            {
                var map = GetMapToSpawnIn(perpetual.Key);
                CellFinder.TryFindRandomCell(map, cell => cell.Walkable(map), out var cell2);
                var pawn = GenSpawn.Spawn(perpetual.Key, cell2, map);
                    
                var letter = LetterMaker.MakeLetter("BEWH.MankindsFinest.Perpetual.PerpetualReturn".Translate(), "BEWH.MankindsFinest.Perpetual.PerpetualReturnMessage".Translate(pawn), Genes40kDefOf.BEWH_GoldenPositive, pawn);
                Find.LetterStack.ReceiveLetter(letter);
            }
            removeAfterResurrection.Add(perpetual.Key);
        }

        foreach (var pawn in removeAfterResurrection)
        {
            RemovePerpetual(pawn);
        }
    }

    private static Map GetMapToSpawnIn(Pawn pawn)
    {
        if (pawn.Map != null)
        {
            return pawn.Map;
        }

        if (pawn.Corpse?.Map != null)
        {
            return pawn.Corpse.Map;
        }
            
        var map = Find.AnyPlayerHomeMap;
        if (map != null)
        {
            return map;
        }
            
        return Find.CurrentMap ?? Find.Maps.First();
    }
        
    public void AddPerpetual(Pawn pawn, int resurrectIn)
    {
        if (!perpetuals.ContainsKey(pawn))
        {
            perpetuals.Add(pawn, resurrectIn);
        }

        KeepPawnForResurrection(pawn);
    }

    private static void KeepPawnForResurrection(Pawn pawn)
    {
        if (pawn == null || pawn.Spawned || pawn.Discarded)
        {
            return;
        }

        if (Find.WorldPawns.Contains(pawn))
        {
            Find.WorldPawns.ForcefullyKeptPawns.Add(pawn);
            return;
        }

        Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
    }
        
    public void RemovePerpetual(Pawn pawn)
    {
        perpetuals.Remove(pawn);
    }

    public override void ExposeData()
    {
        base.ExposeData();

        if (Scribe.mode == LoadSaveMode.LoadingVars)
        {
            legacyFormatLoaded = TryLoadLegacyPerpetuals();
        }

        if (!legacyFormatLoaded)
        {
            Scribe_Collections.Look(ref perpetuals, "perpetuals", LookMode.Reference, LookMode.Value, ref perpetualsKeysWorkingList, ref perpetualsValuesWorkingList);
        }

        Scribe_Values.Look(ref currentTick, "currentTick");

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            perpetuals ??= new Dictionary<Pawn, int>();
        }
    }

    public override void LoadedGame()
    {
        base.LoadedGame();

        if (!legacyFormatLoaded)
        {
            return;
        }

        legacyFormatLoaded = false;
        RestoreLegacyPerpetuals();
    }

    /// <summary>
    /// Older saves stored every tracked pawn in full inside this component. Pawns that also exist elsewhere in the save
    /// are remembered by ID and looked up once the game has loaded; pawns only stored here are loaded from that copy.
    /// </summary>
    private bool TryLoadLegacyPerpetuals()
    {
        var perpetualsNode = Scribe.loader.curXmlParent?["perpetuals"];
        var keysNode = perpetualsNode?["keys"];

        if (keysNode == null)
        {
            return false;
        }

        var keyEntries = keysNode.ChildNodes.OfType<XmlElement>().ToList();

        if (keyEntries.Count == 0 || !keyEntries[0].ChildNodes.OfType<XmlElement>().Any())
        {
            return false;
        }

        var valueEntries = perpetualsNode["values"]?.ChildNodes.OfType<XmlElement>().ToList() ?? new List<XmlElement>();

        legacyReferenceIds = new List<string>();
        legacyReferenceTicks = new List<int>();
        legacyDeepPawns = new List<Pawn>();
        legacyDeepTicks = new List<int>();

        for (var i = 0; i < keyEntries.Count; i++)
        {
            var entry = keyEntries[i];
            var thingId = entry["id"]?.InnerText;

            if (thingId.NullOrEmpty() || i >= valueEntries.Count || !int.TryParse(valueEntries[i].InnerText, out var resurrectTick))
            {
                Log.Warning("[Mankind's Finest] Skipped an unreadable perpetual entry from an older save.");
                continue;
            }

            if (ExistsElsewhereInSave(entry, thingId))
            {
                legacyReferenceIds.Add(thingId);
                legacyReferenceTicks.Add(resurrectTick);
                continue;
            }

            var pawn = ScribeExtractor.SaveableFromNode<Pawn>(entry, null);

            if (pawn == null)
            {
                continue;
            }

            legacyDeepPawns.Add(pawn);
            legacyDeepTicks.Add(resurrectTick);
        }

        return true;
    }

    private static bool ExistsElsewhereInSave(XmlElement entry, string thingId)
    {
        var matches = entry.OwnerDocument?.SelectNodes("//id[text()='" + thingId + "']");

        if (matches == null)
        {
            return false;
        }

        foreach (XmlNode match in matches)
        {
            var insideEntry = false;

            for (var node = match.ParentNode; node != null; node = node.ParentNode)
            {
                if (node != entry)
                {
                    continue;
                }

                insideEntry = true;
                break;
            }

            if (!insideEntry)
            {
                return true;
            }
        }

        return false;
    }

    private void RestoreLegacyPerpetuals()
    {
        if (!legacyReferenceIds.NullOrEmpty())
        {
            var pawnsById = FindPawnsByThingId(new HashSet<string>(legacyReferenceIds));

            for (var i = 0; i < legacyReferenceIds.Count; i++)
            {
                if (pawnsById.TryGetValue(legacyReferenceIds[i], out var pawn) && !pawn.Discarded)
                {
                    AddPerpetual(pawn, legacyReferenceTicks[i]);
                    continue;
                }

                Log.Warning("[Mankind's Finest] Could not find perpetual " + legacyReferenceIds[i] + " from an older save, they will not resurrect.");
            }
        }

        if (!legacyDeepPawns.NullOrEmpty())
        {
            for (var i = 0; i < legacyDeepPawns.Count; i++)
            {
                var pawn = legacyDeepPawns[i];

                if (pawn == null || pawn.Discarded)
                {
                    continue;
                }

                AddPerpetual(pawn, legacyDeepTicks[i]);
            }
        }

        legacyReferenceIds = null;
        legacyReferenceTicks = null;
        legacyDeepPawns = null;
        legacyDeepTicks = null;
    }

    private static Dictionary<string, Pawn> FindPawnsByThingId(HashSet<string> thingIds)
    {
        var found = new Dictionary<string, Pawn>();

        foreach (var pawn in PawnsFinder.AllMapsWorldAndTemporary_AliveOrDead)
        {
            TryAddFound(pawn);
        }

        var corpses = new List<Corpse>();

        foreach (var map in Find.Maps)
        {
            corpses.Clear();
            ThingOwnerUtility.GetAllThingsRecursively(map, ThingRequest.ForGroup(ThingRequestGroup.Corpse), corpses);

            foreach (var corpse in corpses)
            {
                TryAddFound(corpse.InnerPawn);
            }
        }

        return found;

        void TryAddFound(Pawn pawn)
        {
            if (pawn == null || !thingIds.Contains(pawn.ThingID) || found.ContainsKey(pawn.ThingID))
            {
                return;
            }

            found.Add(pawn.ThingID, pawn);
        }
    }
}