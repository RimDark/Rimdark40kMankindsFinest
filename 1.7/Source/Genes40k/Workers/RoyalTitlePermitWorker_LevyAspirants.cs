using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Genes40k;

/// <summary>
/// Drop-pods baseline aspirants of implantation age who join the colony for good. Quality is set by DefModExtension_AspirantLevy.
/// </summary>
public class RoyalTitlePermitWorker_LevyAspirants : RoyalTitlePermitWorker_Targeted
{
    private const int MaxGenerationAttempts = 20;

    private Faction faction;

    public override IEnumerable<FloatMenuOption> GetRoyalAidOptions(Map map, Pawn pawn, Faction faction)
    {
        if (AidDisabled_NewTemp(map, pawn, faction, out var reason, temperatureMatters: false))
        {
            yield return new FloatMenuOption(def.LabelCap + ": " + reason, null);
            yield break;
        }
        Action action = null;
        var count = def.royalAid.pawnCount;
        var countKey = count == 1 ? "BEWH.MankindsFinest.Permit.AspirantCountOne" : "BEWH.MankindsFinest.Permit.AspirantCount";
        string description = def.LabelCap + " (" + countKey.Translate(count) + "): ";
        if (FillAidOption(pawn, faction, ref description, out var free))
        {
            action = () =>
            {
                BeginCallAspirants(pawn, faction, map, free);
            };
        }
        yield return new FloatMenuOption(description, action, faction.def.FactionIcon, faction.Color);
    }

    private void BeginCallAspirants(Pawn caller, Faction faction, Map map, bool free)
    {
        targetingParameters = new TargetingParameters
        {
            canTargetLocations = true,
            canTargetBuildings = false,
            canTargetPawns = false,
        };
        this.caller = caller;
        this.map = map;
        this.faction = faction;
        this.free = free;
        var rangeActual = RangeClamped;
        targetingParameters.validator = target =>
        {
            if (rangeActual > 0f && target.Cell.DistanceTo(caller.Position) > rangeActual)
            {
                return false;
            }
            if (target.Cell.Fogged(map) || !DropCellFinder.CanPhysicallyDropInto(target.Cell, map, canRoofPunch: true))
            {
                return false;
            }
            return target.Cell.GetEdifice(map) == null && !target.Cell.Impassable(map);
        };
        Find.Targeter.BeginTargeting(this);
    }

    public override void OrderForceTarget(LocalTargetInfo target)
    {
        CallAspirants(target.Cell);
    }

    private void CallAspirants(IntVec3 cell)
    {
        var levy = def.GetModExtension<DefModExtension_AspirantLevy>() ?? new DefModExtension_AspirantLevy();

        var aspirants = new List<Pawn>();
        for (var i = 0; i < def.royalAid.pawnCount; i++)
        {
            aspirants.Add(GenerateAspirant(levy));
        }

        DropPodUtility.DropThingsNear(cell, map, aspirants, forbid: false, allowFogged: false, faction: faction);

        var entries = aspirants.Select(aspirant => "  - " + "BEWH.MankindsFinest.Permit.AspirantEntry".Translate(aspirant.NameShortColored, aspirant.ageTracker.AgeBiologicalYears, PassionsDesc(aspirant)).Resolve());
        Find.LetterStack.ReceiveLetter("BEWH.MankindsFinest.Permit.AspirantsArrivedLabel".Translate(), "BEWH.MankindsFinest.Permit.AspirantsArrivedText".Translate(faction.Name, entries.ToLineList()), LetterDefOf.PositiveEvent, new LookTargets(cell, map));

        caller.royalty.GetPermit(def, faction).Notify_Used();
        if (!free)
        {
            caller.royalty.TryRemoveFavor(faction, def.royalAid.favorCost);
        }
    }

    private Pawn GenerateAspirant(DefModExtension_AspirantLevy levy)
    {
        var biotech = ModsConfig.BiotechActive;
        Pawn aspirant = null;
        for (var attempt = 0; attempt < MaxGenerationAttempts; attempt++)
        {
            if (aspirant != null)
            {
                Find.WorldPawns.PassToWorld(aspirant, PawnDiscardDecideMode.Discard);
            }

            //forceGenerateNewPawn: redressing an existing player-faction world pawn would skip the age range and forced xenotype
            var request = new PawnGenerationRequest(def.royalAid.pawnKindDef, Faction.OfPlayer, forceGenerateNewPawn: true, canGeneratePawnRelations: false, mustBeCapableOfViolence: true, forceAddFreeWarmLayerIfNeeded: true, allowAddictions: false, forbidAnyTitle: true, forcedXenotype: biotech ? XenotypeDefOf.Baseliner : null, developmentalStages: biotech ? DevelopmentalStage.Child | DevelopmentalStage.Adult : DevelopmentalStage.Adult, biologicalAgeRange: biotech ? levy.ageRange : levy.ageRangeNoBiotech);
            aspirant = PawnGenerator.GeneratePawn(request);

            AdjustTraits(aspirant, levy);
            AdjustPassions(aspirant, levy);

            if (!levy.noIncapabilities || aspirant.story.DisabledWorkTagsBackstoryAndTraits == WorkTags.None)
            {
                break;
            }
        }

        var disabled = aspirant.story.DisabledWorkTagsBackstoryAndTraits;
        if (levy.noIncapabilities && disabled != WorkTags.None)
        {
            Log.Warning($"[Mankind's Finest] {def.defName}: no aspirant without incapabilities in {MaxGenerationAttempts} attempts, keeping {aspirant.LabelShort} (incapable of {disabled.LabelTranslated()}).");
        }
        return aspirant;
    }

    private static void AdjustTraits(Pawn pawn, DefModExtension_AspirantLevy levy)
    {
        var traits = pawn.story.traits;
        foreach (var trait in traits.allTraits.ToList())
        {
            if (CanRemove(trait) && levy.badTraits.Any(bad => bad.Matches(trait)))
            {
                traits.RemoveTrait(trait);
            }
        }

        var wanted = levy.goodTraitCount.RandomInRange;
        var have = traits.allTraits.Count(trait => levy.goodTraits.Any(good => good.Matches(trait)));
        while (have < wanted)
        {
            if (!levy.goodTraits.Where(good => CanGain(pawn, good.def)).TryRandomElement(out var pick))
            {
                break;
            }

            //Make room by dropping a trait that isn't one of the good ones
            if (traits.allTraits.Count >= levy.maxTraits)
            {
                var spare = traits.allTraits.Where(trait => CanRemove(trait) && !levy.goodTraits.Any(good => good.Matches(trait))).RandomElementWithFallback();
                if (spare == null)
                {
                    break;
                }
                traits.RemoveTrait(spare);
            }
            traits.GainTrait(new Trait(pick.def, pick.degree ?? PawnGenerator.RandomTraitDegree(pick.def)));
            have++;
        }
    }

    private static bool CanRemove(Trait trait)
    {
        if (trait.ScenForced || trait.sourceGene != null)
        {
            return false;
        }
        return trait.def != TraitDefOf.Gay && trait.def != TraitDefOf.Bisexual && trait.def != TraitDefOf.Asexual;
    }

    private static bool CanGain(Pawn pawn, TraitDef traitDef)
    {
        var traits = pawn.story.traits;
        if (traits.HasTrait(traitDef) || traits.allTraits.Any(trait => trait.def.ConflictsWith(traitDef)))
        {
            return false;
        }
        if (pawn.kindDef.disallowedTraits.NotNullAndContains(traitDef))
        {
            return false;
        }
        return (pawn.story.DisabledWorkTagsBackstoryAndTraits & traitDef.requiredWorkTags) == WorkTags.None;
    }

    private static void AdjustPassions(Pawn pawn, DefModExtension_AspirantLevy levy)
    {
        var skills = pawn.skills.skills;
        foreach (var skill in skills)
        {
            skill.passion = Passion.None;
        }

        var open = skills.Where(skill => !skill.TotallyDisabled && !pawn.story.traits.allTraits.Any(trait => trait.def.ConflictsWithPassion(skill.def))).ToList();
        var total = levy.passionCount.RandomInRange;
        var majors = Math.Min(levy.majorPassionCount.RandomInRange, total);

        var picked = new List<SkillRecord>();
        if (levy.combatPassion && total > 0 && open.Where(skill => skill.def == SkillDefOf.Shooting || skill.def == SkillDefOf.Melee).TryRandomElement(out var combat))
        {
            picked.Add(combat);
            open.Remove(combat);
        }
        picked.AddRange(open.InRandomOrder().Take(total - picked.Count));

        for (var i = 0; i < picked.Count; i++)
        {
            var skill = picked[i];
            skill.passion = i < majors ? Passion.Major : Passion.Minor;
            //Level reads levelInt + Aptitude but writes levelInt, so take the aptitude back out when raising it
            var floor = levy.passionSkillLevel.RandomInRange;
            if (skill.Level < floor)
            {
                skill.Level = floor - skill.Aptitude;
            }
        }
    }

    private static string PassionsDesc(Pawn pawn)
    {
        var passions = pawn.skills.skills.Where(skill => skill.passion != Passion.None).OrderByDescending(skill => skill.passion).ToList();
        if (passions.NullOrEmpty())
        {
            return "BEWH.MankindsFinest.Permit.AspirantNoPassions".Translate();
        }
        return passions.Select(skill => skill.passion == Passion.Major ? "BEWH.MankindsFinest.Permit.AspirantMajorPassion".Translate(skill.def.LabelCap).Resolve() : skill.def.LabelCap.Resolve()).ToCommaList();
    }
}
