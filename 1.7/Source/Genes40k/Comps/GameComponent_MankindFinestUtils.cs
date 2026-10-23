using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Genes40k;

public class GameComponent_MankindFinestUtils : GameComponent
{
    private const int CheckInterval = 500;
    private int currentTick;
    private bool useNewRandomChapter = true;
        
    private Genes40kModSettings modSettings = null;
    private Genes40kModSettings ModSettings => modSettings ??= LoadedModManager.GetMod<Genes40kMod>().GetSettings<Genes40kModSettings>();

    [Obsolete]
    private bool legacyArmourHitPointsScaled;

    private ChapterColourDef currentChapterColour;
    public ChapterColourDef CurrentChapterColour
    {
        get
        {
            if (useNewRandomChapter || currentChapterColour == null || !Genes40kUtils.NpcChapterColourDefs.Contains(currentChapterColour))
            {
                useNewRandomChapter = false;
                currentChapterColour = GetRandomChapterForRaid();
            }

            return currentChapterColour;
        }
    }

    private HashSet<Pawn> serfOathDeclined = new HashSet<Pawn>();

    public bool HasDeclinedSerfOath(Pawn pawn) => serfOathDeclined.Contains(pawn);

    public void Notify_SerfOathDeclined(Pawn pawn) => serfOathDeclined.Add(pawn);

    public GameComponent_MankindFinestUtils(Game game) { }

    public override void StartedNewGame()
    {
        base.StartedNewGame();
        legacyArmourHitPointsScaled = true;
    }

    public override void LoadedGame()
    {
        base.LoadedGame();

        SerfUtility.GrantOathAbilityToChaplains();

        if (legacyArmourHitPointsScaled)
        {
            return;
        }

        ScaleLegacyArmourHitPoints();
        legacyArmourHitPointsScaled = true;
    }

    /// <summary>
    /// Power armour max hit points were raised tenfold, but saves keep the old absolute hit points. Scales them once when
    /// a save from before the change is loaded, so existing armour keeps its condition instead of showing as nearly destroyed.
    /// </summary>
    [Obsolete]
    private static void ScaleLegacyArmourHitPoints()
    {
        var things = new HashSet<Thing>();
        var buffer = new List<Thing>();

        foreach (var map in Find.Maps)
        {
            ThingOwnerUtility.GetAllThingsRecursively(map, ThingRequest.ForGroup(ThingRequestGroup.Apparel), buffer);
            things.UnionWith(buffer);
        }

        if (Find.World != null)
        {
            ThingOwnerUtility.GetAllThingsRecursively(Find.World, buffer);
            things.UnionWith(buffer);

            foreach (var pawn in Find.WorldPawns.AllPawnsAliveOrDead)
            {
                ThingOwnerUtility.GetAllThingsRecursively(pawn, buffer);
                things.UnionWith(buffer);
            }
        }

        var scaled = 0;

        foreach (var thing in things)
        {
            if (thing is not Apparel || !thing.def.useHitPoints)
            {
                continue;
            }

            var defMod = thing.def.GetModExtension<DefModExtension_LegacyArmourHitPoints>();

            if (defMod == null)
            {
                continue;
            }

            thing.HitPoints = Math.Min((int)Math.Round(thing.HitPoints * defMod.hitPointsFactor), thing.MaxHitPoints);
            scaled++;
        }

        if (scaled > 0)
        {
            Log.Message("[Mankind's Finest] Scaled the hit points of " + scaled + " pieces of power armour from an older save.");
        }
    }
    
    public override void GameComponentTick()
    {
        if (currentTick != CheckInterval)
        {
            currentTick++;
            return;
        }

        currentTick = 0;
        useNewRandomChapter = true;
    }
    
    private ChapterColourDef GetRandomChapterForRaid()
    {   
        var chapterColours = Genes40kUtils.NpcChapterColourDefs.ToList();

        if (ModSettings.CurrentlySelectedPreset != null && ModSettings.CurrentlySelectedPreset != ModSettings.CustomPreset)
        {
            chapterColours.Remove(ModSettings.CurrentlySelectedPreset);
        }

        if (currentChapterColour != null && chapterColours.Contains(currentChapterColour))
        {
            chapterColours.Remove(currentChapterColour);
        }

        if (chapterColours.TryRandomElement(out var chapterColour))
        {
            return chapterColour;
        }

        return Genes40kUtils.NpcChapterColourDefs.Contains(currentChapterColour) ? currentChapterColour : Genes40kUtils.NpcChapterColourDefs.FirstOrDefault();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref currentTick, "currentTick");
        Scribe_Values.Look(ref useNewRandomChapter, "useNewRandomChapter", true);
        Scribe_Defs.Look(ref currentChapterColour, "currentChapterColour");
        Scribe_Values.Look(ref legacyArmourHitPointsScaled, "legacyArmourHitPointsScaled", false);

        if (Scribe.mode == LoadSaveMode.Saving)
        {
            serfOathDeclined.RemoveWhere(pawn => pawn == null || pawn.Dead || pawn.Destroyed);
        }

        Scribe_Collections.Look(ref serfOathDeclined, "serfOathDeclined", LookMode.Reference);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            serfOathDeclined ??= new HashSet<Pawn>();
            serfOathDeclined.RemoveWhere(pawn => pawn == null);
        }
    }
}