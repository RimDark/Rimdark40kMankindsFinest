using System.Collections.Generic;
using Verse;

namespace Genes40k;

/// <summary>
/// Shapes the aspirants sent by RoyalTitlePermitWorker_LevyAspirants. Pawn kind and count come from the permit's royalAid.
/// </summary>
public class DefModExtension_AspirantLevy : DefModExtension
{
    //Biological age with Biotech, and without it (no children, so the youngest teenagers)
    public FloatRange ageRange = new(10f, 13.9f);
    public FloatRange ageRangeNoBiotech = new(13f, 13.9f);

    //Generated passions are replaced by exactly this many, of which majorPassionCount are burning
    public IntRange passionCount = new(1, 1);
    public IntRange majorPassionCount = new(0, 0);
    //First passion (burning if any are) goes to Shooting or Melee
    public bool combatPassion = false;
    //Passionate skills are raised to at least this level
    public IntRange passionSkillLevel = new(0, 0);

    //badTraits are removed, then goodTraits are added until goodTraitCount of them are present
    public List<TraitRequirement> badTraits = new();
    public List<TraitRequirement> goodTraits = new();
    public IntRange goodTraitCount = new(0, 0);
    public int maxTraits = 3;

    //Rerolls aspirants whose backstory or traits disable any work
    public bool noIncapabilities = false;
}
