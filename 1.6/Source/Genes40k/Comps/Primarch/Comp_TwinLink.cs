using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Genes40k;

[StaticConstructorOnStartup]
public class Comp_TwinLink : CompAbilityEffect
{
    public new CompProperties_AbilityTwinLink Props => (CompProperties_AbilityTwinLink)props;

    public override bool ShouldHideGizmo
    {
        get
        {
            var casterGene = parent.pawn.TwinGene();
            if (casterGene == null)
            {
                return true;
            }

            return casterGene.Twin != null;
        }
    }

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        var caster = parent.pawn;
        var targetPawn = target.Pawn;
        
        var casterGene = caster.TwinGene();
        var targetGene = targetPawn.TwinGene();

        if (casterGene == null || targetGene == null)
        {
            return;
        }
        
        casterGene.SetTwin(targetPawn);
        targetGene.SetTwin(caster);

        base.Apply(target, dest);
    }
    
    public override bool Valid(LocalTargetInfo target, bool throwMessages = false)
    {
        if (!base.Valid(target, throwMessages))
        {
            return false;
        }
        
        var targetPawn = target.Pawn;

        var targetGene = targetPawn.TwinGene();

        if (targetGene == null)
        {
            return false;
        }
        
        return targetGene.Twin == null;
    }
    
    public override string ExtraLabelMouseAttachment(LocalTargetInfo target)
    {
        var targetPawn = target.Pawn;
        if (targetPawn == null)
        {
            return base.ExtraLabelMouseAttachment(target);
        }
        
        var targetGene = targetPawn.TwinGene();
        
        if (targetGene == null)
        {
            return "BEWH.MankindsFinest.Ability.DoesNotHaveAlphariusGene".Translate();
        }
        
        if (targetGene.Twin != null)
        {
            return "BEWH.MankindsFinest.Ability.HaveTwin".Translate();
        }

        return base.ExtraLabelMouseAttachment(target);
    }
}