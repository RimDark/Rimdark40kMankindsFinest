using RimWorld;
using System.Collections.Generic;
using Verse;

namespace Genes40k;

public class DefModExtension_GeneseedVial : DefModExtension
{
    public XenotypeDef xenotype = null;
    public XenotypeIconDef xenotypeIcon = null;

    public bool overrideXenotypeGenesGiven = false;
    public List<GeneDef> overridenAddedGenes = new();

    public HediffDef appliesHediff = null;

    public int minAgeImplant = 0;
    public int maxAgeImplant = 0;
    public int baseFailureChance = 0;
    public int failureChancePerAgePast = 0;
    public int failChanceCap = 0;

    public bool usesImplantPicker = false;
    public int implantWorkAmount = 0;
    public int implantMedicineSkill = 0;
}