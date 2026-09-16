using RimWorld;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Genes40k;

[StaticConstructorOnStartup]
public class Building_SangprimusPortum : Building, IThingHolder
{
    private ThingOwner innerContainer;

    private GameComponent_UnlockedMaterials GameComp => GameComponent_UnlockedMaterials.Instance;
    private bool gameCompChangeDone = false;
    
    public Building_SangprimusPortum()
    {
        innerContainer = new ThingOwner<Thing>(this);
    }

    public bool CanAcceptMaterial(Thing thing)
    {
        return !GameComp.HasMaterial(thing.def);
    }

    public void AddMaterial(Thing thing)
    {
        GameComp.UnlockMaterial(thing.def);
        thing.Destroy();
    }
    
    private static readonly Texture2D DesignChapterGeneIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/ViewGenes");
    private static readonly Texture2D ChapterGeneListIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/BEWH_CogIcon");

    [Unsaved(false)]
    private CompPowerTrader cachedPowerComp;
    private CompPowerTrader PowerTraderComp => cachedPowerComp ??= this.TryGetComp<CompPowerTrader>();
    private bool PowerOn => PowerTraderComp == null || PowerTraderComp.PowerOn;

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        if (Faction != Faction.OfPlayer)
        {
            yield break;
        }

        var designCommand = new Command_Action
        {
            defaultLabel = "BEWH.MankindsFinest.CustomChapter.DesignGizmo".Translate(),
            defaultDesc = "BEWH.MankindsFinest.CustomChapter.DesignGizmoDesc".Translate(),
            icon = DesignChapterGeneIcon,
            action = delegate
            {
                Find.WindowStack.Add(new Dialog_CreateChapterGene());
            }
        };

        var listCommand = new Command_Action
        {
            defaultLabel = "BEWH.MankindsFinest.CustomChapter.ListGizmo".Translate(),
            defaultDesc = "BEWH.MankindsFinest.CustomChapter.ListGizmoDesc".Translate(),
            icon = ChapterGeneListIcon,
            action = delegate
            {
                Find.WindowStack.Add(new Dialog_CustomChapterGeneList());
            }
        };

        if (!PowerOn)
        {
            designCommand.Disable("NoPower".Translate().CapitalizeFirst());
            listCommand.Disable("NoPower".Translate().CapitalizeFirst());
        }

        yield return designCommand;
        yield return listCommand;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public ThingOwner GetDirectlyHeldThings() => innerContainer;

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Deep.Look(ref innerContainer, "innerContainer", this);
        Scribe_Values.Look(ref gameCompChangeDone, "gameCompChangeDone");
        
        if (Scribe.mode == LoadSaveMode.PostLoadInit && !gameCompChangeDone)
        {
            if (innerContainer != null)
            {
                foreach (var thing in innerContainer)
                {
                    GameComp.UnlockMaterial(thing.def); 
                }
            }
            innerContainer = new ThingOwner<Thing>(this);
            gameCompChangeDone = true;
        }
    }
}