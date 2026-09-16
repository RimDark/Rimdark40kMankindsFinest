using System;
using System.Collections.Generic;
using System.Linq;
using Core40k;
using RimWorld;
using VEF.Abilities;
using Verse;

namespace Genes40k;

/// <summary>
/// Gene class of runtime-generated chapter genes. Executes the random gene/trait grants of the merged traits the way
/// Core40k.Gene_AddRandomGeneAndOrTraitByWeight does (rolled once in PostMake, applied in PostAdd, undone in PostRemove),
/// so no helper gene is needed on the pawn. Inherits Gene_InduceFear so a chapter whose traits carry
/// DefModExtension_GeneInducedFear gets the Night Lords effect; without that extension the base class does nothing.
/// </summary>
public class Gene_CustomChapter : Gene_InduceFear, ITwinGene
{
    private List<GeneDef> chosenGenes = [];
    private Dictionary<TraitDef, int> chosenTraits = new();
    private Pawn twin;

    public Pawn Twin => twin;

    public bool TwinCapable => Generated?.twinLinked == true;

    public void SetTwin(Pawn twinPawn)
    {
        twin = twinPawn;
    }

    private DefModExtension_CustomChapterGenerated Generated => def.GetModExtension<DefModExtension_CustomChapterGenerated>();

    public CustomChapterGene Chapter => GameComponent_CustomChapterGenes.Instance?.GetByGeneDef(def);

    public override void PostMake()
    {
        base.PostMake();

        var generated = Generated;

        if (generated == null || pawn == null)
        {
            return;
        }

        foreach (var grant in generated.geneGrants.Where(grant => grant != null))
        {
            SelectGenes(grant);
        }

        foreach (var grant in generated.traitGrants.Where(grant => grant != null))
        {
            SelectTraits(grant);
        }

        RunWorkers(worker => worker.PostMake(this, pawn));
    }

    public override void Tick()
    {
        base.Tick();

        var workerTraits = Generated?.workerTraits;

        if (workerTraits.NullOrEmpty() || pawn == null)
        {
            return;
        }

        foreach (var trait in workerTraits)
        {
            var worker = trait?.worker;

            if (worker != null && worker.tickInterval > 0 && pawn.IsHashIntervalTick(worker.tickInterval))
            {
                worker.Tick(this, pawn);
            }
        }
    }

    public override void Notify_PawnDied(DamageInfo? dinfo, Hediff culprit = null)
    {
        base.Notify_PawnDied(dinfo, culprit);
        RunWorkers(worker => worker.Notify_PawnDied(this, pawn, dinfo, culprit));
    }

    public override void Notify_IngestedThing(Thing thing, int numTaken)
    {
        base.Notify_IngestedThing(thing, numTaken);
        RunWorkers(worker => worker.Notify_IngestedThing(this, pawn, thing, numTaken));
    }

    public override void Notify_NewColony()
    {
        base.Notify_NewColony();
        RunWorkers(worker => worker.Notify_NewColony(this, pawn));
    }

    public override void Reset()
    {
        base.Reset();
        RunWorkers(worker => worker.Reset(this, pawn));
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        var workerTraits = Generated?.workerTraits;

        if (workerTraits.NullOrEmpty() || pawn == null)
        {
            yield break;
        }

        foreach (var gizmo in workerTraits.Where(trait => trait?.worker != null).SelectMany(trait => trait.worker.GetGizmos(this, pawn)))
        {
            yield return gizmo;
        }
    }

    public override IEnumerable<StatDrawEntry> SpecialDisplayStats()
    {
        var workerTraits = Generated?.workerTraits;

        if (workerTraits.NullOrEmpty() || pawn == null)
        {
            yield break;
        }

        foreach (var entry in workerTraits.Where(trait => trait?.worker != null).SelectMany(trait => trait.worker.SpecialDisplayStats(this, pawn)))
        {
            yield return entry;
        }
    }

    /// <summary>
    /// Runs an action on every worker of the traits this chapter was built from.
    /// </summary>
    private void RunWorkers(Action<ChapterTraitWorker> action)
    {
        var workerTraits = Generated?.workerTraits;

        if (workerTraits.NullOrEmpty() || pawn == null)
        {
            return;
        }

        foreach (var trait in workerTraits.Where(trait => trait?.worker != null))
        {
            action(trait.worker);
        }
    }

    public override void PostAdd()
    {
        base.PostAdd();
        ApplyChosen();
        GiveVefAbilities();
        RunWorkers(worker => worker.PostAdd(this, pawn));
    }

    private IEnumerable<VEF.Abilities.AbilityDef> VefAbilityDefs => Generated?.vefAbilityGrants?
        .Where(grant => grant != null && !grant.abilityDefs.NullOrEmpty())
        .SelectMany(grant => grant.abilityDefs)
        .Where(abilityDef => abilityDef != null)
        .Distinct() ?? [];

    private void GiveVefAbilities()
    {
        var comp = pawn?.GetComp<CompAbilities>();

        if (comp == null)
        {
            return;
        }

        foreach (var abilityDef in VefAbilityDefs)
        {
            comp.GiveAbility(abilityDef);
        }
    }

    private void RemoveVefAbilities()
    {
        var comp = pawn?.GetComp<CompAbilities>();

        if (comp?.LearnedAbilities == null)
        {
            return;
        }

        var toRemove = VefAbilityDefs.ToList();

        for (var i = comp.LearnedAbilities.Count - 1; i >= 0; i--)
        {
            if (toRemove.Contains(comp.LearnedAbilities[i].def))
            {
                comp.LearnedAbilities.RemoveAt(i);
            }
        }
    }

    private void ApplyChosen()
    {
        if (pawn?.genes == null)
        {
            return;
        }

        foreach (var geneDef in chosenGenes.Where(geneDef => !pawn.genes.HasActiveGene(geneDef)))
        {
            pawn.genes.AddGene(geneDef, true);
        }

        if (pawn.story?.traits == null)
        {
            return;
        }

        foreach (var pair in chosenTraits.Where(pair => !pawn.story.traits.HasTrait(pair.Key, pair.Value)))
        {
            pawn.story.traits.GainTrait(new Trait(pair.Key, pair.Value));
        }
    }

    public override void PostRemove()
    {
        base.PostRemove();
        RunWorkers(worker => worker.PostRemove(this, pawn));
        RemoveVefAbilities();
        RemoveChosen();
    }

    private void RemoveChosen()
    {
        if (pawn?.genes == null)
        {
            return;
        }

        foreach (var geneDef in chosenGenes.Distinct().ToList())
        {
            var gene = pawn.genes.GetGene(geneDef);

            if (gene != null)
            {
                pawn.genes.RemoveGene(gene);
            }
        }

        if (pawn.story?.traits == null)
        {
            return;
        }

        foreach (var traitDef in chosenTraits.Keys)
        {
            var trait = pawn.story.traits.GetTrait(traitDef);

            if (trait != null)
            {
                pawn.story.traits.RemoveTrait(trait);
            }
        }
    }

    private void SelectGenes(DefModExtension_AddRandomGeneByWeight grant)
    {
        if (grant.possibleGenesToGive.NullOrEmpty() || Rand.RangeInclusive(1, 100) > grant.chanceToGrantGene)
        {
            return;
        }

        var possible = grant.possibleGenesToGive.Where(pair => !pawn.genes.HasActiveGene(pair.Key) && !chosenGenes.Contains(pair.Key)).ToList();

        if (possible.NullOrEmpty() || (grant.skipIfAnyAlreadyExistsOnPawn && possible.Count < grant.possibleGenesToGive.Count))
        {
            return;
        }

        var amount = Math.Min(grant.amountToGive.RandomInRange, possible.Count);

        if (amount >= possible.Count)
        {
            chosenGenes.AddRange(possible.Select(pair => pair.Key));
            return;
        }

        var selection = new WeightedSelection<GeneDef>();

        foreach (var pair in possible)
        {
            selection.AddEntry(pair.Key, pair.Value);
        }

        for (var i = 0; i < amount; i++)
        {
            var result = selection.GetRandomUnique();

            if (result != null && !chosenGenes.Contains(result))
            {
                chosenGenes.Add(result);
            }
        }
    }

    private void SelectTraits(DefModExtension_AddRandomTraitByWeight grant)
    {
        if (grant.possibleTraitsToGive.NullOrEmpty() || pawn.story?.traits == null || Rand.RangeInclusive(1, 100) > grant.chanceToGrantTrait)
        {
            return;
        }

        var possible = grant.possibleTraitsToGive.Where(data => data.traitDef != null && !pawn.story.traits.HasTrait(data.traitDef, data.degree) && !chosenTraits.ContainsKey(data.traitDef)).ToList();

        if (possible.NullOrEmpty())
        {
            return;
        }

        var amount = Math.Min(grant.amountToGive, possible.Count);

        if (amount >= possible.Count)
        {
            foreach (var data in possible.Where(data => !chosenTraits.ContainsKey(data.traitDef)))
            {
                chosenTraits.Add(data.traitDef, data.degree);
            }

            return;
        }

        var selection = new WeightedSelection<WeightedTraitData>();

        foreach (var data in possible)
        {
            selection.AddEntry(data, data.weight);
        }

        for (var i = 0; i < amount; i++)
        {
            var result = selection.GetRandomUnique();

            if (result?.traitDef != null && !chosenTraits.ContainsKey(result.traitDef))
            {
                chosenTraits.Add(result.traitDef, result.degree);
            }
        }
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref chosenGenes, "chosenGenes", LookMode.Def);
        Scribe_Collections.Look(ref chosenTraits, "chosenTraits", LookMode.Def, LookMode.Value);
        Scribe_References.Look(ref twin, "twin");

        if (Scribe.mode != LoadSaveMode.PostLoadInit)
        {
            return;
        }

        chosenGenes ??= [];
        chosenGenes.RemoveAll(geneDef => geneDef == null);
        chosenTraits ??= new Dictionary<TraitDef, int>();
    }
}
