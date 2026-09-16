using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using UnityEngine;
using VEF.Genes;
using Verse;
using Verse.Sound;

namespace Genes40k;

[StaticConstructorOnStartup]
public class Building_PrimarchGrowthVat : Building, IStoreSettingsParent, IThingHolder
{
    private static readonly Texture2D CancelIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
    private static readonly Texture2D StartIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/BEWH_PrimarchVatStart");
    private static readonly Texture2D EjectEmbryoIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/BEWH_EjectPrimarchEmbryo");
    private static readonly Texture2D InsertEmbryoIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/BEWH_InsertPrimarchEmbryo");
    
    [Unsaved(false)]
    private DefModExtension_PrimarchVatTexture cachedDefModTexture;
    private DefModExtension_PrimarchVatTexture DefModTexture => cachedDefModTexture ??= def.GetModExtension<DefModExtension_PrimarchVatTexture>();
    [Unsaved(false)]
    private Graphic fetusEarlyStageGraphic;
    private Graphic FetusEarlyStage => fetusEarlyStageGraphic ??= GraphicDatabase.Get<Graphic_Single>(DefModTexture.earlyFetusTexture, ShaderDatabase.Cutout, DefModTexture.earlyFetusSize, Color.white);
    [Unsaved(false)]
    private Graphic fetusLateStageGraphic;
    private Graphic FetusLateStage => fetusLateStageGraphic ??= GraphicDatabase.Get<Graphic_Single>(DefModTexture.lateFetusTexture, ShaderDatabase.Cutout, DefModTexture.lateFetusSize, Color.white);
    private Graphic cylinderGraphic;
    private Graphic topGraphic;
    
    [Unsaved(false)]
    private CompPowerTrader cachedPowerComp;
    private CompPowerTrader PowerTraderComp => cachedPowerComp ??= this.TryGetComp<CompPowerTrader>();
    public bool PowerOn => PowerTraderComp.PowerOn;
    
    
    [Unsaved(false)]
    private Sustainer sustainerWorking;
    private Mote workingMote;
    
    
    private int startTick = -1;
    public const int EmbryoGestationTicks = 600000;
    private int gestationTicksTotal = EmbryoGestationTicks;
    private int EmbryoGestationTicksRemaining => startTick - Find.TickManager.TicksGame;
    private int EmbryoLateStageGraphicTicksRemaining => gestationTicksTotal / 2;
    private float EmbryoGestationPct => 1f - Mathf.Clamp01((float)EmbryoGestationTicksRemaining / gestationTicksTotal);

    /// <summary>
    /// The gestation this embryo needs: the base time adjusted by the complexity of any custom primarch design it carries.
    /// </summary>
    private static int GestationTicksFor(PrimarchEmbryo embryo)
    {
        return CustomChapterGeneUtility.GestationTicksFor(embryo?.PrimarchGenes?.GenesListForReading, EmbryoGestationTicks);
    }

    
    private const float FetusMinSize = 0.4f;
    private const float FetusMaxSize = 0.95f;
    
    
    public bool StorageTabVisible => true;
    private StorageSettings allowedNutritionSettings;
    public ThingOwner nutritionContainer;
    
    private float containedNutrition;
    private const float NutritionBuffer = 20f;
    private const float NutritioConsumedPerDayByEmbryo = 6f;
    private float NutritionConsumedPerDay
    {
        get
        {
            var consumedNutritionPerDay = containedEmbryo != null ? NutritioConsumedPerDayByEmbryo : 0f;

            if (BiostarvationSeverityPercent <= 0f)
            {
                return consumedNutritionPerDay;
            }
                
            var biostarvationMultiplier = 1.1f;
            consumedNutritionPerDay *= biostarvationMultiplier;
            return consumedNutritionPerDay;
        }
    }
    private float NutritionStored => containedNutrition + nutritionContainer.Sum(thing => thing.stackCount * thing.GetStatValue(StatDefOf.Nutrition));
    public float NutritionNeeded => NutritionBuffer - NutritionStored;

    
    private float embryoStarvation;
    private float BiostarvationDailyOffset
    {
        get
        {
            if (!Working)
            {
                return 0f;
            }
            if (!PowerOn || containedNutrition <= 0f)
            {
                return 0.5f;
            }
            return -0.1f;
        }
    }
    private float BiostarvationSeverityPercent => containedEmbryo != null ? embryoStarvation : 0f;
    
    public bool Working => startTick >= 0;

    public PrimarchEmbryo selectedEmbryo;
    private PrimarchEmbryo containedEmbryo;
    public PrimarchEmbryo ContainedEmbryo => containedEmbryo;

    public Building_PrimarchGrowthVat()
    {
        nutritionContainer = new ThingOwner<Thing>(this);
        selectedEmbryo = null;
        containedEmbryo = null;
    }

    public override void PostMake()
    {
        base.PostMake();
        allowedNutritionSettings = new StorageSettings(this);
        if (def.building.defaultStorageSettings != null)
        {
            allowedNutritionSettings.CopyFrom(def.building.defaultStorageSettings);
        }
    }

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        if (respawningAfterLoad && containedEmbryo != null)
        {
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                var color = EmbryoColor();
                fetusEarlyStageGraphic = FetusEarlyStage.GetColoredVersion(ShaderDatabase.Cutout, color, color);
                fetusLateStageGraphic = FetusLateStage.GetColoredVersion(ShaderDatabase.Cutout, color, color);
            });
        }
    }
    
    protected override void Tick()
    {
        base.Tick();
        if (this.IsHashIntervalTick(250))
        {
            PowerTraderComp.PowerOutput = Working ? 0f - PowerComp.Props.PowerConsumption : 0f - PowerComp.Props.idlePowerDraw;

            // FIX: drop a selection that can no longer be hauled (destroyed, despawned,
            // moved off-map, or a phantom duplicate restored from an old save). Left in
            // place it made WorkGiver_CarryPrimarchEmbryoToVat hand out a job that failed
            // instantly, every tick, to every hauler and hauler drone on the map.
            if (!Working && containedEmbryo == null && selectedEmbryo != null
                && (selectedEmbryo.Destroyed || !selectedEmbryo.Spawned || selectedEmbryo.MapHeld != Map))
            {
                selectedEmbryo = null;
            }
        }

        ThingDef thingDef = null;
            
        if (Working)
        {
            thingDef = def.building.gestatorFormingMote.GetForRotation(Rotation);
            if (containedEmbryo != null)
            {
                if (EmbryoGestationTicksRemaining <= 0)
                {
                    Finish();
                    return;
                }
                embryoStarvation = Mathf.Clamp01(embryoStarvation + BiostarvationDailyOffset / 60000f);
            }
            
            if (BiostarvationSeverityPercent >= 1f)
            {
                Fail();
                return;
            }
            
            if (sustainerWorking == null || sustainerWorking.Ended)
            {
                sustainerWorking = SoundDefOf.GrowthVat_Working.TrySpawnSustainer(SoundInfo.InMap(this, MaintenanceType.PerTick));
            }
            else
            {
                sustainerWorking.Maintain();
            }
            
            containedNutrition = Mathf.Clamp(containedNutrition - NutritionConsumedPerDay / 60000f, 0f, 2.14748365E+09f);
            if (containedNutrition <= 0f)
            {
                TryAbsorbNutritiousThing();
            }
        }

        if (thingDef == null)
        {
            return;
        }
            
        if (workingMote == null || workingMote.Destroyed || workingMote.def != thingDef)
        {
            workingMote = MoteMaker.MakeAttachedOverlay(this, thingDef, Vector3.zero);
        }

        workingMote.yOffset = -4.4f;
        workingMote.Maintain();
    }

    public void InsertEmbryo(PrimarchEmbryo embryo)
    {
        if (embryo == null || embryo.Destroyed || containedEmbryo != null)
        {
            return;
        }

        // FIX: the old code did holdingOwner.TryDrop(...Near) then DeSpawn().
        // If the drop failed (no free cell, hauler standing in a doorway, etc.) the
        // embryo stayed inside the hauler's carry tracker while this field also
        // pointed at it - two owners for one Thing, which is a second way to get the
        // duplicate-ID error on the next load. Take it out of whatever holds it
        // instead of routing it through the floor. A null holdingOwner would also
        // have thrown here.
        if (embryo.holdingOwner != null)
        {
            embryo.holdingOwner.Remove(embryo);
        }
        else if (embryo.Spawned)
        {
            embryo.DeSpawn();
        }

        containedEmbryo = embryo;
        selectedEmbryo = null;
    }
    
    private void Finish()
    {
        // FIX: EmbryoBirth() could throw, and the exception skipped DestroyEmbryo()
        // and OnStop(). startTick and containedEmbryo were left untouched, Thing.DoTick
        // swallowed the error, and the next tick found the gestation finished all over
        // again - one newborn per tick, forever, until the player noticed. The vat now
        // always shuts itself down, whatever the birth does.
        try
        {
            EmbryoBirth();
        }
        catch (Exception ex)
        {
            Log.Error($"[Mankind's Finest] Primarch birth failed in {this}: {ex}");
        }
        finally
        {
            DestroyEmbryo();
            OnStop();
        }
    }

    private void Fail()
    {
        DestroyEmbryo(biostarvation: true);
        OnStop();
    }
    
    private void OnStop()
    {
        selectedEmbryo = null;
        containedEmbryo = null;
        startTick = -1;
        embryoStarvation = 0f;
        sustainerWorking = null;
    }
    
    private void EmbryoBirth()
    {
        if (containedEmbryo == null || Map == null || startTick > Find.TickManager.TicksGame)
        {
            return;
        }

        var geneDef = containedEmbryo.PrimarchGenes.GenesListForReading.FirstOrDefault(g => g.HasModExtension<DefModExtension_PrimarchVatExtras>());
        var childAmount = geneDef == null ? 1 : geneDef.GetModExtension<DefModExtension_PrimarchVatExtras>().childAmount;

        var children = new List<Pawn>();

        for (var i = 0; i < childAmount; i++)
        {
            var child = GenerateNewbornPrimarch();
            if (child == null)
            {
                Log.Error($"[Mankind's Finest] {this} failed to generate a primarch newborn.");
                continue;
            }

            children.Add(child);
        }

        ConnectTwins(children);

        foreach (var child in children)
        {
            PlaceNewborn(child);
        }

        if (children.Count == 0)
        {
            return;
        }

        SendBirthLetter(children);
        ApplyNaturalBirthRolls(children);
    }

    // These two are Harmony postfixes on ApplyBirthOutcome, so they used to fire on vat
    // births for free. Now that the vat generates its own pawn they have to be invoked
    // by hand, or vat-born primarchs would silently lose the chance to be born a
    // perpetual/psyker/pariah. They are run after the pawn is spawned because both send
    // a letter pointing at it, and after the primarch genes are applied so that their
    // exclusion-tag checks actually see those genes.
    private void ApplyNaturalBirthRolls(List<Pawn> children)
    {
        var embryoMother = containedEmbryo?.Mother;

        foreach (var child in children)
        {
            Thing born = child;
            NaturalBirthPerpetual.Postfix(ref born, embryoMother);
            NaturalBirthPsykerPariah.Postfix(ref born, embryoMother);
        }
    }

    // FIX: this used to call PregnancyUtility.ApplyBirthOutcome and immediately
    // dereference whatever came back. That method is patched by several mods, and at
    // least one of them (Big and Small) runs a prefix that spawns the babies itself
    // and then returns false WITHOUT assigning __result - so the call handed back null
    // and the next line threw a NullReferenceException. The vat never needed the
    // vanilla childbirth ritual for anything, so it now builds the newborn itself and
    // no other mod's birth patch sits between the vat and its primarch.
    private Pawn GenerateNewbornPrimarch()
    {
        var embryoMother = containedEmbryo.Mother;
        var faction = Faction.OfPlayer;

        var lastName = (embryoMother?.Name as NameTriple)?.Last;

        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            faction?.def?.basicMemberKind ?? PawnKindDefOf.Colonist,
            faction,
            forceGenerateNewPawn: true,
            allowDowned: true,
            canGeneratePawnRelations: false,
            colonistRelationChanceFactor: 0f,
            allowFood: false,
            allowAddictions: false,
            fixedGender: ForcedGenderFromGeneDefs(),
            fixedLastName: lastName,
            developmentalStages: DevelopmentalStage.Newborn));

        if (pawn?.genes == null)
        {
            return pawn;
        }

        ApplyEmbryoGenes(pawn);
        ApplyForcedGender(pawn);

        if (embryoMother != null)
        {
            pawn.relations.AddDirectRelation(PawnRelationDefOf.Parent, embryoMother);
        }

        // FIX: the old code read Faction.OfPlayer.ideos.PrimaryIdeo unguarded, which is
        // a NullReferenceException on its own for anyone playing without Ideology.
        if (ModsConfig.IdeologyActive && pawn.ideo != null)
        {
            var ideo = embryoMother?.Ideo ?? faction?.ideos?.PrimaryIdeo;
            if (ideo != null)
            {
                pawn.ideo.SetIdeo(ideo);
            }
        }

        if (embryoStarvation > 0f)
        {
            var hediff = HediffMaker.MakeHediff(HediffDefOf.BioStarvation, pawn);
            hediff.Severity = Mathf.Lerp(0f, HediffDefOf.BioStarvation.maxSeverity, embryoStarvation);
            pawn.health.AddHediff(hediff);
        }

        return pawn;
    }

    private void ApplyEmbryoGenes(Pawn pawn)
    {
        foreach (var gene in pawn.genes.GenesListForReading.ToList())
        {
            pawn.genes.RemoveGene(gene);
        }

        if (containedEmbryo.birthGenes != null)
        {
            foreach (var birthGene in containedEmbryo.birthGenes.GenesListForReading)
            {
                pawn.genes.AddGene(birthGene, false);
            }
        }

        foreach (var primarchGene in containedEmbryo.PrimarchGenes.GenesListForReading)
        {
            pawn.genes.AddGene(primarchGene, true);
        }

        pawn.genes.SetXenotypeDirect(Genes40kDefOf.BEWH_Primarch);
    }

    // Read off the gene defs before generation so the pawn is built with the right
    // gender in the first place - the name and portrait are picked from it.
    private Gender? ForcedGenderFromGeneDefs()
    {
        var geneDefs = containedEmbryo.PrimarchGenes.GenesListForReading
            .Concat(containedEmbryo.birthGenes?.GenesListForReading ?? Enumerable.Empty<GeneDef>());

        foreach (var defMod in geneDefs.Select(g => g?.GetModExtension<GeneExtension>()))
        {
            if (defMod == null)
            {
                continue;
            }
            if (defMod.forceFemale)
            {
                return Gender.Female;
            }
            if (defMod.forceMale)
            {
                return Gender.Male;
            }
        }

        if (!Genes40kUtils.ModSettings.allowFemalePrimarchBirths)
        {
            return Gender.Male;
        }

        return null;
    }

    // Re-checked once the genes are actually on the pawn, because only then is it
    // known which of them ended up active.
    private static void ApplyForcedGender(Pawn pawn)
    {
        var forcedGenderGene = pawn.genes.GenesListForReading.FirstOrDefault(gene =>
        {
            if (!gene.Active)
            {
                return false;
            }

            var defMod = gene.def?.GetModExtension<GeneExtension>();
            return defMod != null && (defMod.forceFemale || defMod.forceMale);
        });

        if (forcedGenderGene != null)
        {
            var defMod = forcedGenderGene.def.GetModExtension<GeneExtension>();
            pawn.gender = defMod.forceFemale ? Gender.Female : Gender.Male;
            return;
        }

        if (!Genes40kUtils.ModSettings.allowFemalePrimarchBirths)
        {
            pawn.gender = Gender.Male;
        }
    }

    private static void ConnectTwins(List<Pawn> children)
    {
        Pawn firstTwin = null;

        foreach (var child in children.Where(c => c.TwinGene() != null))
        {
            if (firstTwin == null)
            {
                firstTwin = child;
                continue;
            }

            firstTwin.TwinGene().SetTwin(child);
            child.TwinGene().SetTwin(firstTwin);

            firstTwin.relations.AddDirectRelation(PawnRelationDefOf.Sibling, child);
            child.relations.AddDirectRelation(PawnRelationDefOf.Sibling, firstTwin);

            firstTwin = null;
        }
    }

    private void PlaceNewborn(Pawn child)
    {
        var cell = InteractionCell;

        if (!cell.IsValid || !cell.Standable(Map))
        {
            cell = CellFinder.StandableCellNear(Position, Map, 3f);
        }

        if (!cell.IsValid)
        {
            cell = Position;
        }

        GenSpawn.Spawn(child, cell, Map);
    }

    private void SendBirthLetter(List<Pawn> children)
    {
        var names = children.Select(child => child.LabelShortCap).ToCommaList(useAnd: true);

        Find.LetterStack.ReceiveLetter(
            "BEWH.MankindsFinest.PrimarchGrowthVat.PrimarchBorn".Translate(),
            "BEWH.MankindsFinest.PrimarchGrowthVat.PrimarchBornDesc".Translate(names),
            LetterDefOf.PositiveEvent,
            new LookTargets(children.Cast<Thing>()));
    }

    private void DestroyEmbryo(bool biostarvation = false)
    {
        if (startTick < 0 || containedEmbryo == null)
        {
            return;
        }
            
        if (startTick > Find.TickManager.TicksGame)
        {
            Messages.Message(biostarvation
                    ? "EmbryoEjectedFromGrowthVatBiostarvation".Translate(containedEmbryo.Label)
                    : "EmbryoEjectedFromGrowthVat".Translate(containedEmbryo.Label), this, MessageTypeDefOf.NegativeEvent);
        }

        if (!containedEmbryo.Destroyed)
        {
            containedEmbryo.Destroy();
        }
        containedEmbryo = null;
    }
    
    private Color EmbryoColor()
    {
        var result = PawnSkinColors.GetSkinColor(0.5f);
        if (containedEmbryo?.GeneSet == null)
        {
            return result;
        }
            
        foreach (var item in containedEmbryo.GeneSet.GenesListForReading)
        {
            if (item.skinColorOverride.HasValue)
            {
                return item.skinColorOverride.Value;
            }
            if (item.skinColorBase.HasValue)
            {
                result = item.skinColorBase.Value;
            }
        }
        return result;
    }
    
    private List<PrimarchEmbryo> AvailableEmbryo()
    {
        var embryos = new List<PrimarchEmbryo>();
        if (Map?.listerThings == null)
        {
            return embryos;
        }

        // FIX: OfType instead of Cast (a hard cast throws if anything else ever ends
        // up under this def), and skip embryos that are gone or forbidden so the
        // player cannot select one that no hauler is allowed to fetch.
        foreach (var thing in Map.listerThings.ThingsOfDef(Genes40kDefOf.BEWH_PrimarchEmbryo))
        {
            if (thing is not PrimarchEmbryo embryo || embryo.Destroyed || !embryo.Spawned)
            {
                continue;
            }

            if (embryo.IsForbidden(Faction.OfPlayer))
            {
                continue;
            }

            embryos.Add(embryo);
        }

        return embryos;
    }
    
    private void TryAbsorbNutritiousThing()
    {
        foreach (var thing in nutritionContainer)
        {
            if (thing.def != Genes40kDefOf.BEWH_RawGestationalSlurry)
            {
                continue;
            }
            var statValue = thing.GetStatValue(StatDefOf.Nutrition);
            if (statValue <= 0f)
            {
                continue;
            }
                    
            containedNutrition += statValue;
            // FIX: was SplitOff(1).DeSpawn(). SplitOff returns an unspawned Thing, so
            // DeSpawn logged "Tried to despawn ... when it's unspawned" every time and
            // leaked the split-off stack. Vanilla Building_GrowthVat uses Destroy().
            thing.SplitOff(1).Destroy();
                
            break;
        }
    }
    
    protected override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        base.DrawAt(drawLoc, flip);
        if (Working && containedEmbryo != null)
        {
            var loc = drawLoc + def.building.formingMechPerRotationOffset[Rotation.AsInt];
            loc.y += 1f / 52f;
            loc.z += Mathf.PingPong(Find.TickManager.TicksGame * def.building.formingMechBobSpeed, def.building.formingMechYBobDistance);
                
            var texture = DefModTexture;
            var early = EmbryoGestationTicksRemaining > EmbryoLateStageGraphicTicksRemaining;
            var fetusGraphic = early ? FetusEarlyStage : FetusLateStage;
            var fetusSize = (early ? texture.earlyFetusSize : texture.lateFetusSize) * Mathf.Lerp(FetusMinSize, FetusMaxSize, EmbryoGestationPct);
            loc += early ? texture.earlyFetusOffset : texture.lateFetusOffset;

            //Scaled through the matrix so the GraphicDatabase-owned graphic is never mutated.
            var fetusMatrix = Matrix4x4.TRS(loc, Quaternion.identity, new Vector3(fetusSize.x, 1f, fetusSize.y));
            Graphics.DrawMesh(MeshPool.plane10, fetusMatrix, fetusGraphic.MatSingle, 0);
        }

        topGraphic ??= def.building.mechGestatorTopGraphic.GraphicColoredFor(this);
        cylinderGraphic ??= def.building.mechGestatorCylinderGraphic.GraphicColoredFor(this);
            
        var loc2 = new Vector3(drawLoc.x, AltitudeLayer.BuildingBelowTop.AltitudeFor(), drawLoc.z);
        cylinderGraphic.Draw(loc2, Rotation, this);
            
        var loc3 = new Vector3(drawLoc.x, AltitudeLayer.BuildingOnTop.AltitudeFor(), drawLoc.z);
        topGraphic.Draw(loc3, Rotation, this);
    }
    
    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        if (!Working)
        {
            //STARTS MACHINE
            var command_Action1 = new Command_Action
            {
                defaultLabel = "BEWH.MankindsFinest.PrimarchGrowthVat.StartPrimarchGrowth".Translate(),
                defaultDesc = "BEWH.MankindsFinest.PrimarchGrowthVat.StartPrimarchGrowthDesc".Translate(),
                icon = StartIcon,
                activateSound = SoundDefOf.Designate_Cancel,
                action = delegate
                {
                    var window = Dialog_MessageBox.CreateConfirmation("BEWH.MankindsFinest.PrimarchGrowthVat.PrimarchVatStartConfirmation".Translate(), Action, destructive: true);
                    Find.WindowStack.Add(window);
                    return;

                    void Action()
                    {
                        gestationTicksTotal = GestationTicksFor(containedEmbryo);
                        startTick = Find.TickManager.TicksGame + gestationTicksTotal;
                        selectedEmbryo = null;
                    }
                }
            };
            yield return command_Action1;
            if (containedEmbryo == null)
            {
                command_Action1.Disable("BEWH.MankindsFinest.PrimarchGrowthVat.ContainsNoEmbryo".Translate().CapitalizeFirst());
            }
            else if (!PowerOn)
            {
                command_Action1.Disable("NoPower".Translate().CapitalizeFirst());
            }
            
            if (containedEmbryo != null)
            {
                //EJECT PRIMARCH EMBRYO
                var command_Action2 = new Command_Action
                {
                    defaultLabel = "BEWH.MankindsFinest.PrimarchGrowthVat.EjectPrimarchEmbryo".Translate(),
                    defaultDesc = "BEWH.MankindsFinest.PrimarchGrowthVat.EjectPrimarchEmbryoDesc".Translate(),
                    icon = EjectEmbryoIcon,
                    activateSound = SoundDefOf.Designate_Cancel,
                    action = delegate
                    {
                        // FIX: only clear the field if the embryo actually made it out,
                        // otherwise the ejected embryo is lost for good.
                        if (!GenPlace.TryPlaceThing(containedEmbryo, InteractionCell, Map, ThingPlaceMode.Near))
                        {
                            Messages.Message("NoEmptyPlaceLower".Translate().CapitalizeFirst(),
                                this, MessageTypeDefOf.RejectInput, historical: false);
                            return;
                        }

                        containedEmbryo = null;
                        OnStop();
                    }
                };
                yield return command_Action2;
            }
        }
        
        if(containedEmbryo == null)
        {
            //START HAUL JOB
            if (selectedEmbryo == null)
            {
                var embryos = AvailableEmbryo();
                var command_Action3 = new Command_Action
                {
                    defaultLabel = "ImplantEmbryo".Translate() + "...",
                    defaultDesc = "InsertEmbryoGrowthVatDesc".Translate(EmbryoGestationTicks.ToStringTicksToPeriod()).Resolve(),
                    icon = InsertEmbryoIcon,
                    action = delegate
                    {
                        var list = new List<FloatMenuOption>();
                        foreach (var embryo in embryos)
                        {
                            var embryoName = "BEWH.MankindsFinest.PrimarchGrowthVat.PrimarchMother".Translate(embryo.Mother.Name.ToStringFull);
                            embryoName += "\n";
                            embryoName += "BEWH.MankindsFinest.PrimarchGrowthVat.PrimarchFather".Translate("BEWH.MankindsFinest.CommonKeywords.Emperor".Translate());

                            var primarchChapterGenes = embryo.PrimarchGenes.GenesListForReading.Where(gene => gene.HasModExtension<DefModExtension_PrimarchMaterial>()).ToList();
                            if (primarchChapterGenes.Any())
                            {
                                embryoName += "\n";
                                embryoName += "BEWH.MankindsFinest.PrimarchGrowthVat.PrimarchLine".Translate(CustomChapterGeneUtility.ChapterLabelOf(primarchChapterGenes.First()).CapitalizeFirst());
                            }

                            var embryoGestation = GestationTicksFor(embryo);

                            if (embryoGestation != EmbryoGestationTicks)
                            {
                                embryoName += "\n";
                                embryoName += "BEWH.MankindsFinest.PrimarchGrowthVat.GestationTime".Translate(embryoGestation.ToStringTicksToPeriod());
                            }
                            list.Add(new FloatMenuOption(embryoName, delegate
                            {
                                selectedEmbryo = embryo;
                            }, embryo, Color.white));
                        }
                        Find.WindowStack.Add(new FloatMenu(list));
                    }
                };
                if (embryos.NullOrEmpty())
                {
                    command_Action3.Disable("ImplantNoEmbryos".Translate().CapitalizeFirst());
                }
                else if (!PowerOn)
                {
                    command_Action3.Disable("NoPower".Translate().CapitalizeFirst());
                }
                yield return command_Action3;
            }
            else //CANCEL HAUL JOB
            {
                var command_Action4 = new Command_Action
                {
                    defaultLabel = "CommandCancelLoad".Translate(),
                    defaultDesc = "CommandCancelLoadDesc".Translate(),
                    icon = CancelIcon,
                    activateSound = SoundDefOf.Designate_Cancel,
                    action = delegate
                    {
                        selectedEmbryo = null;
                    }
                };
                yield return command_Action4;
            }
        }
        
        if (!DebugSettings.ShowDevGizmos)
        {
            yield break;
        }

        foreach (var debugGizmo in DebugGizmo())
        {
            yield return debugGizmo;
        }
    }

    private IEnumerable<Gizmo> DebugGizmo()
    {
        if (containedEmbryo != null)
        {
            yield return new Command_Action
            {
                defaultLabel = "TEST INSPECT",
                action = delegate
                {
                    Genes40kUtils.InspectPrimarchEmbryoGenes(containedEmbryo);
                }
            };
        }
        
        //DEV: FILL NUTRITION
        yield return new Command_Action
        {
            defaultLabel = "DEV: Fill nutrition",
            action = delegate
            {
                containedNutrition = NutritionBuffer;
            }
        };
            
        //DEV: EMPTY NUTRITION
        yield return new Command_Action
        {
            defaultLabel = "DEV: Empty nutrition",
            action = delegate
            {
                containedNutrition = 0f;
                nutritionContainer.Clear();
            }
        };

        if (!Working)
        {
            yield break;
        }
                        
        //DEV: ALMOST FINISH BIRTH
        yield return new Command_Action
        {
            defaultLabel = "DEV: Embryo almost done",
            action = delegate
            {
                startTick = Find.TickManager.TicksGame + 500;
            }
        };
            
        //DEV: DECREASE TIME REMAINING BY 12H
        yield return new Command_Action
        {
            defaultLabel = "DEV: decrease time by 12h",
            action = delegate
            {
                startTick -= 30000;
            }
        };
    }
    
    public override void DrawExtraSelectionOverlays()
    {
        base.DrawExtraSelectionOverlays();
        if (selectedEmbryo != null && selectedEmbryo.Map == Map)
        {
            GenDraw.DrawLineBetween(this.TrueCenter(), selectedEmbryo.TrueCenter());
        }
    }

    public override string GetInspectString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append(base.GetInspectString());
        if (Working)
        {
            if (containedEmbryo != null)
            {
                stringBuilder.Append("\n");
                if (EmbryoGestationTicksRemaining > 60000)
                {
                    stringBuilder.AppendTagged("EmbryoTimeUntilBirth".Translate() + ": " + EmbryoGestationTicksRemaining.ToStringTicksToDays().Colorize(ColoredText.DateTimeColor));
                }
                else
                {
                    stringBuilder.AppendTagged("EmbryoTimeUntilBirth".Translate() + ": " + EmbryoGestationTicksRemaining.ToStringTicksToPeriod(allowYears: false).Colorize(ColoredText.DateTimeColor));
                }
            }
            
            if (BiostarvationSeverityPercent > 0f)
            {
                var text = BiostarvationDailyOffset >= 0f ? "+" : string.Empty;
                stringBuilder.Append("\n");
                stringBuilder.Append($"{"Biostarvation".Translate()}: {BiostarvationSeverityPercent.ToStringPercent()} ({"PerDay".Translate(text + BiostarvationDailyOffset.ToStringPercent())})");
            }
        }


        if (!PowerTraderComp.Off)
        {
            stringBuilder.Append("\n");
        }
        stringBuilder.Append("Nutrition".Translate()).Append(": ").Append(NutritionStored.ToStringByStyle(ToStringStyle.FloatMaxOne));
        if (Working)
        {
            stringBuilder.Append(" (-").Append("PerDay".Translate(NutritionConsumedPerDay.ToString("F1"))).Append(")");
        }
            
        return stringBuilder.ToString();
    }
    
    public bool CanAcceptNutrition(Thing thing)
    {
        return allowedNutritionSettings.AllowedToAccept(thing);
    }

    public StorageSettings GetStoreSettings()
    {
        return allowedNutritionSettings;
    }

    public StorageSettings GetParentStoreSettings()
    {
        return def.building.fixedStorageSettings;
    }

    public void Notify_SettingsChanged()
    {
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        EjectContentsOnRemoval();
        base.DeSpawn(mode);
    }

    private void EjectContentsOnRemoval()
    {
        if (!Spawned)
        {
            return;
        }

        nutritionContainer?.TryDropAll(Position, Map, ThingPlaceMode.Near);

        if (containedEmbryo is { Destroyed: false })
        {
            GenPlace.TryPlaceThing(containedEmbryo, Position, Map, ThingPlaceMode.Near);
        }

        containedEmbryo = null;
    }

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, GetDirectlyHeldThings());
    }

    public ThingOwner GetDirectlyHeldThings() => nutritionContainer;
    
    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_References.Look(ref selectedEmbryo, "selectedEmbryo");
        Scribe_Deep.Look(ref containedEmbryo, "containedEmbryo");
        Scribe_Values.Look(ref embryoStarvation, "embryoStarvation", 0f);
        Scribe_Values.Look(ref containedNutrition, "containedNutrition", 0f);
        Scribe_Deep.Look(ref allowedNutritionSettings, "allowedNutritionSettings", this);
        Scribe_Deep.Look(ref nutritionContainer, "nutritionContainer", this);
        Scribe_Values.Look(ref startTick, "startTick", -1);
        Scribe_Values.Look(ref gestationTicksTotal, "gestationTicksTotal", EmbryoGestationTicks);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            if (selectedEmbryo is { Destroyed: true })
            {
                selectedEmbryo = null;
            }
            
            if (containedEmbryo != null)
            {
                selectedEmbryo = null;
            }
        }

        if (allowedNutritionSettings != null)
        {
            return;
        }
            
        allowedNutritionSettings = new StorageSettings(this);
            
        if (def.building.defaultStorageSettings != null)
        {
            allowedNutritionSettings.CopyFrom(def.building.defaultStorageSettings);
        }
    }
}