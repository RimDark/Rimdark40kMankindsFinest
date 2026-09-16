using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Genes40k;

[StaticConstructorOnStartup]
public class Comp_TwinTeleport : CompAbilityEffect
{
    public new CompProperties_AbilityTwinTeleport Props => (CompProperties_AbilityTwinTeleport)props;

    public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
    {
        var caster = parent.pawn;

        var twinGene = caster.TwinGene();

        if (twinGene?.Twin == null)
        {
            return;
        }

        var twin = twinGene.Twin;
            
        if (twin.Map != null && twin.Position.IsValid)
        {
            caster.teleporting = true;
            caster.ExitMap(allowedToJoinOrCreateCaravan: false, Rot4.Invalid);
            caster.teleporting = false;
                
            GenSpawn.Spawn(caster, twin.Position, twin.Map);
        }

        var caravan = twin.GetCaravan();

        if (caravan?.Faction is { IsPlayer: true })
        {
            caravan.AddPawn(caster, addCarriedPawnToWorldPawnsIfAny: true);
            caster.ExitMap(allowedToJoinOrCreateCaravan: false, Rot4.Invalid);
        }
        base.Apply(target, dest);
    }
}