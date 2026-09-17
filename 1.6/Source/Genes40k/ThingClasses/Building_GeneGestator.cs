using RimWorld;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Genes40k;

[StaticConstructorOnStartup]
public class Building_GeneGestator : Building
{
    public ThingDef selectedMatrix = null;
    public Thing containedMatrix = null;

    public ThingDef selectedMaterial = null;
    public int selectedCustomChapterId = -1;
    public List<CustodesDisciplineLevel> selectedDisciplines = [];

    private CustomChapterGene SelectedCustomChapter => selectedCustomChapterId < 0 ? null : GameComponent_CustomChapterGenes.Instance?.Get(selectedCustomChapterId);

    private DefModExtension_GeneMatrix ContainedMatrixMod => containedMatrix?.def.GetModExtension<DefModExtension_GeneMatrix>();

    private bool HasDisciplinePattern => selectedDisciplines.Any(entry => entry?.def != null && entry.level > 0);

    private bool doWork = false;
    private float totalTime = 0;
    private float TotalTimeAdjusted => totalTime * (Mathf.Max(Genes40kUtils.ModSettings.matrixGestationTimeFactor, 1)/100f);
    private float progressInt = 0;
    public bool InProgress => TotalTimeAdjusted - progressInt > 0;
    public bool Finished => TotalTimeAdjusted - progressInt <= 0 && containedMatrix != null;
    
    [Unsaved(false)]
    private Effecter progressBar;

    private static readonly Texture2D CancelIcon = ContentFinder<Texture2D>.Get("UI/Designators/Cancel");
    private static readonly Texture2D EjectIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/BEWH_EjectMatrixIcon");
    private static readonly Texture2D StartIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/BEWH_GestationStartIcon");
    private static readonly Texture2D EmptyChapterMaterialIcon = ContentFinder<Texture2D>.Get("Things/Item/ChapterMaterial/BEWH_ChapterMaterial_None");
    private static readonly Texture2D EmptyPrimarchMaterialIcon = ContentFinder<Texture2D>.Get("Things/Item/PrimarchMaterial/BEWH_PrimarchMaterial_None");
    private static readonly Texture2D MatrixSelectionTex = ContentFinder<Texture2D>.Get("Things/Item/GeneMatrix/BEWH_GeneMatrix_Empty");
    private static readonly Texture2D ConfigurePatternIcon = ContentFinder<Texture2D>.Get("UI/Gizmos/BEWH_CogIcon");

    public bool PowerOn => PowerTraderComp.PowerOn;
    [Unsaved(false)]
    private CompPowerTrader cachedPowerComp;
    private CompPowerTrader PowerTraderComp => cachedPowerComp ??= this.TryGetComp<CompPowerTrader>();
    private GameComponent_UnlockedMaterials GameComp => GameComponent_UnlockedMaterials.Instance;
    public void AddGeneMatrix(Thing geneMatrix)
    {
        var singleGeneMatrix = geneMatrix.stackCount > 1 ? geneMatrix.SplitOff(1) : geneMatrix;

        if (singleGeneMatrix.Spawned)
        {
            singleGeneMatrix.DeSpawn();
        }
        singleGeneMatrix.holdingOwner?.Remove(singleGeneMatrix);

        containedMatrix = singleGeneMatrix;
            
        totalTime = containedMatrix.def.GetModExtension<DefModExtension_GeneMatrix>().ticksToGestate;
        progressInt = 0;
        selectedMatrix = null;
    }

    private List<ThingDef> AvailableGestatables()
    {
        var defMod = def.GetModExtension<DefModExtension_GeneGestator>();
        var result = new List<ThingDef>();
        if (defMod.gestatableThings.NullOrEmpty())
        {
            return result;
        }

        result.AddRange(defMod.gestatableThings.Where(thing => thing.GetModExtension<DefModExtension_GeneMatrix>().researchNeeded.IsFinished));
        return result;
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        if (progressBar != null)
        {
            progressBar.Cleanup();
            progressBar = null;
        }

        TryDropContainedMatrix();
        
        base.DeSpawn(mode);
    }

    protected override void Tick()
    {
        base.Tick();
        if (!doWork)
        {
            return;
        }
            
        if (PowerOn)
        {
            if (Finished)
            {
                Finish();
            }
            else
            {
                TickEffects();
                progressInt++;
            }
        }
        else
        {
            if (progressBar == null)
            {
                return;
            }
                
            progressBar.Cleanup();
            progressBar = null;
        }
    }

    private void TickEffects()
    {
        progressBar ??= EffecterDefOf.ProgressBarAlwaysVisible.Spawn();
        progressBar.EffectTick(new TargetInfo(this.TrueCenter().ToIntVec3(), Map), TargetInfo.Invalid);
        var mote = ((SubEffecter_ProgressBar)progressBar.children[0]).mote;
        if (mote == null) return;
            
        mote.progress = Mathf.Clamp01(progressInt / TotalTimeAdjusted);
        mote.offsetZ = Rotation == Rot4.North ? 0.5f : -0.5f;
    }

    private void Finish()
    {
        var geneseedVial = (GeneseedVial)ThingMaker.MakeThing(containedMatrix.def.GetModExtension<DefModExtension_GeneMatrix>().makesGeneVial);
        
        if (selectedMaterial != null)
        {
            var defMod = selectedMaterial.GetModExtension<DefModExtension_GeneFromMaterial>();
            geneseedVial.extraGeneFromMaterial = defMod.addedGene;
            geneseedVial.newGeneseedVialTexture = defMod.newGeneseedVialTexture;
        }
        else if (SelectedCustomChapter != null)
        {
            var customChapter = SelectedCustomChapter;
            geneseedVial.extraGeneFromMaterial = customChapter.GeneDef;
            geneseedVial.newGeneseedVialTexture = customChapter.Template.vialTexturePath;
            customChapter.locked = true;
        }
        else if (HasDisciplinePattern && GameComponent_CustomChapterGenes.Instance != null)
        {
            var design = GameComponent_CustomChapterGenes.Instance.GetOrCreateCustodesDesign(selectedDisciplines);
            geneseedVial.extraGeneFromMaterial = design.GeneDef;
            geneseedVial.newGeneseedVialTexture = design.Template.vialTexturePath;
            design.locked = true;
        }
        
        GenSpawn.Spawn(geneseedVial, InteractionCell, Map);

        var message = geneseedVial.def.defName == Genes40kDefOf.BEWH_GeneseedVialPrimarch.defName ? "BEWH.MankindsFinest.GeneGestator.GestatePrimarchFinishMessage" : "BEWH.MankindsFinest.GeneGestator.GestateFinishMessage";
        
        var letter = LetterMaker.MakeLetter("BEWH.MankindsFinest.GeneGestator.GestateFinishLetter".Translate(), message.Translate(containedMatrix.def.label, geneseedVial.Label), LetterDefOf.PositiveEvent, geneseedVial);
        
        Find.LetterStack.ReceiveLetter(letter);
        Reset();
    }

    private void Reset()
    {
        totalTime = 0;
        progressInt = 0;
        if (progressBar != null)
        {
            progressBar.Cleanup();
            progressBar = null;
        }
        doWork = false;
        selectedMatrix = null;
        if (containedMatrix is { Destroyed: false })
        {
            containedMatrix.Destroy();
        }
        containedMatrix = null;
        selectedMaterial = null;
        selectedCustomChapterId = -1;
        selectedDisciplines = [];
    }

    private void TryDropContainedMatrix()
    {
        if (containedMatrix == null)
        {
            return;
        }

        if (!containedMatrix.Destroyed && Spawned)
        {
            GenPlace.TryPlaceThing(containedMatrix, InteractionCell, Map, ThingPlaceMode.Near);
        }

        containedMatrix = null;
    }
    public override string GetInspectString()
    {
        var stringBuilder = new StringBuilder();
        stringBuilder.Append(base.GetInspectString());
        stringBuilder.AppendLineIfNotEmpty();

        stringBuilder.Append(containedMatrix == null ? "BEWH.MankindsFinest.GeneGestator.ContainsNoGeneMatrix".Translate() : "BEWH.MankindsFinest.GeneGestator.ContainsGeneMatrix".Translate(containedMatrix.Label));

        if (selectedMaterial != null)
        {
            stringBuilder.AppendLineIfNotEmpty();
            stringBuilder.Append("BEWH.MankindsFinest.GeneGestator.ContainsExtraMaterial".Translate(selectedMaterial.label));
        }
        else if (SelectedCustomChapter != null)
        {
            stringBuilder.AppendLineIfNotEmpty();
            stringBuilder.Append("BEWH.MankindsFinest.GeneGestator.ContainsExtraMaterial".Translate(SelectedCustomChapter.name));
        }
        else if (HasDisciplinePattern)
        {
            stringBuilder.AppendLineIfNotEmpty();
            stringBuilder.Append("BEWH.MankindsFinest.GeneGestator.ContainsPattern".Translate(CustomChapterGeneUtility.PatternCode(selectedDisciplines)));
        }

        if (!doWork)
        {
            return stringBuilder.ToString();
        }
            
        stringBuilder.AppendLineIfNotEmpty();
        if (Finished)
        {
            stringBuilder.Append("BEWH.MankindsFinest.GeneGestator.GeneVialFinished".Translate());
        }
        else
        {
            var divider = 60000f;
            string timeDenoter = "LetterDay".Translate();

            var timeLeft = TotalTimeAdjusted - progressInt;

            if (timeLeft < 60000)
            {
                divider = 2500f;
                timeDenoter = "LetterHour".Translate();
            }
            stringBuilder.Append("BEWH.MankindsFinest.GeneGestator.GeneVialFinishesIn".Translate(Math.Round(timeLeft / divider, 2), timeDenoter));
        }

        return stringBuilder.ToString();
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        foreach (var gizmo in base.GetGizmos())
        {
            yield return gizmo;
        }

        if (!doWork)
        {
            if (containedMatrix != null)
            { 
                //SELECT PRIMARCH OR CHAPTER MATERIAL
                if (CanUseAnyMaterial())
                {
                    var availableMaterial = new List<ThingDef>();
                    var availableCustomChapters = new List<CustomChapterGene>();
                    string geneticType;

                    Texture2D emptyMaterialIcon = null;
                    if (ContainedMatrixMod.canUsePrimarchMaterial)
                    {
                        availableMaterial.AddRange(GameComp.UnlockedPrimarchMaterial);
                        if (GameComponent_CustomChapterGenes.Instance != null)
                        {
                            availableCustomChapters.AddRange(GameComponent_CustomChapterGenes.Instance.DesignsOf(CustomGeneKind.Primarch));
                        }
                        geneticType = "BEWH.MankindsFinest.CommonKeywords.Primarch".Translate();
                        emptyMaterialIcon = EmptyPrimarchMaterialIcon;
                    }
                    else
                    {
                        availableMaterial.AddRange(GameComp.UnlockedChapterMaterial);
                        if (GameComponent_CustomChapterGenes.Instance != null)
                        {
                            availableCustomChapters.AddRange(GameComponent_CustomChapterGenes.Instance.DesignsOf(CustomGeneKind.Chapter));
                        }
                        geneticType = "BEWH.MankindsFinest.CommonKeywords.Chapter".Translate();
                        emptyMaterialIcon = EmptyChapterMaterialIcon;
                    }

                    var materialIcon = emptyMaterialIcon;
                    if (selectedMaterial != null)
                    {
                        materialIcon = selectedMaterial.GetModExtension<DefModExtension_GeneFromMaterial>().addedGene.Icon;
                    }
                    else if (SelectedCustomChapter != null)
                    {
                        materialIcon = SelectedCustomChapter.GeneDef.Icon;
                    }
                    
                    var command_Action10 = new Command_Action
                    {
                        defaultLabel = "BEWH.MankindsFinest.GeneGestator.SelectXMaterial".Translate(geneticType),
                        defaultDesc = "BEWH.MankindsFinest.GeneGestator.SelectXMaterialDesc".Translate(geneticType),
                        icon = materialIcon,
                        action = delegate
                        {
                            var list = new List<FloatMenuOption>();
                            foreach (var material in availableMaterial)
                            {
                                if (selectedMaterial == material)
                                {
                                    continue;
                                }
                                list.Add(new FloatMenuOption(material.LabelCap, delegate
                                {
                                    selectedMaterial = material;
                                    selectedCustomChapterId = -1;
                                }));
                            }
                            foreach (var customChapter in availableCustomChapters)
                            {
                                if (selectedCustomChapterId == customChapter.id)
                                {
                                    continue;
                                }
                                var optionKey = customChapter.Kind == CustomGeneKind.Primarch ? "BEWH.MankindsFinest.CustomPrimarch.GestatorOption" : "BEWH.MankindsFinest.CustomChapter.GestatorOption";
                                list.Add(new FloatMenuOption(optionKey.Translate(customChapter.name), delegate
                                {
                                    selectedMaterial = null;
                                    selectedCustomChapterId = customChapter.id;
                                }));
                            }
                            if (selectedMaterial != null || SelectedCustomChapter != null)
                            {
                                list.Add(new FloatMenuOption("NoneBrackets".Translate(), delegate
                                {
                                    selectedMaterial = null;
                                    selectedCustomChapterId = -1;
                                }));
                            }
                            if (!list.Any())
                            {
                                list.Add(new FloatMenuOption("BEWH.MankindsFinest.GeneGestator.NoAvailableMaterial".Translate(), null));
                            }
                            Find.WindowStack.Add(new FloatMenu(list));
                        }
                    };

                    if (!Map.listerBuildings.ColonistsHaveBuilding(Genes40kDefOf.BEWH_SangprimusPortum))
                    {
                        command_Action10.Disabled = true;
                        command_Action10.disabledReason = "BEWH.MankindsFinest.GeneGestator.NoSangprimus".Translate();
                    }
                    else if (!Map.listerBuildings.ColonistsHaveBuildingWithPowerOn(Genes40kDefOf.BEWH_SangprimusPortum))
                    {
                        command_Action10.Disabled = true;
                        command_Action10.disabledReason = "BEWH.MankindsFinest.GeneGestator.NoPoweredSangprimus".Translate();
                    }

                    yield return command_Action10;
                }

                //CONFIGURE CUSTODES PATTERN
                if (ContainedMatrixMod.usesDisciplineCustomization)
                {
                    var configureCommand = new Command_Action
                    {
                        defaultLabel = "BEWH.MankindsFinest.CustomCustodes.ConfigureGizmo".Translate(),
                        defaultDesc = "BEWH.MankindsFinest.CustomCustodes.ConfigureGizmoDesc".Translate(),
                        icon = ConfigurePatternIcon,
                        action = delegate
                        {
                            Find.WindowStack.Add(new Dialog_CustodesCustomization(this));
                        }
                    };

                    if (!Map.listerBuildings.ColonistsHaveBuilding(Genes40kDefOf.BEWH_SangprimusPortum))
                    {
                        configureCommand.Disable("BEWH.MankindsFinest.GeneGestator.NoSangprimus".Translate());
                    }
                    else if (!Map.listerBuildings.ColonistsHaveBuildingWithPowerOn(Genes40kDefOf.BEWH_SangprimusPortum))
                    {
                        configureCommand.Disable("BEWH.MankindsFinest.GeneGestator.NoPoweredSangprimus".Translate());
                    }

                    yield return configureCommand;
                }
                
                //STARTS MACHINE
                var command_Action2 = new Command_Action
                {
                    defaultLabel = "BEWH.MankindsFinest.GeneGestator.StartGeneGestating".Translate(),
                    defaultDesc = "BEWH.MankindsFinest.GeneGestator.StartGeneGestatingDesc".Translate(),
                    icon = StartIcon,
                    activateSound = SoundDefOf.Designate_Cancel,
                    action = delegate
                    {
                        if (selectedMaterial == null && SelectedCustomChapter == null && !HasDisciplinePattern && CanConfigureMatrix())
                        {
                            var availableMaterialAmount = 0;
                            
                            if (ContainedMatrixMod.usesDisciplineCustomization)
                            {
                                availableMaterialAmount += 1;
                            }
                            else if (ContainedMatrixMod.canUsePrimarchMaterial)
                            {
                                availableMaterialAmount += GameComp.UnlockedPrimarchMaterial.Count;
                                availableMaterialAmount += GameComponent_CustomChapterGenes.Instance?.DesignsOf(CustomGeneKind.Primarch).Count ?? 0;
                            }
                            else
                            {
                                availableMaterialAmount += GameComp.UnlockedChapterMaterial.Count;
                                availableMaterialAmount += GameComponent_CustomChapterGenes.Instance?.DesignsOf(CustomGeneKind.Chapter).Count ?? 0;
                            }

                            if (availableMaterialAmount > 0)
                            {
                                var confirmationText = "BEWH.MankindsFinest.GeneGestator.NoGeneMaterialSelectedProceed".Translate();
                                Find.WindowStack.Add(new Dialog_MessageBox(confirmationText, "Yes".Translate(), delegate
                                {
                                    doWork = true;
                                }, "No".Translate()));
                            }
                            else
                            {
                                doWork = true;
                            }
                        }
                        else
                        {
                            doWork = true;
                        }
                    }
                };
                yield return command_Action2;

                //EJECT LOADED MATRIX
                var command_Action3 = new Command_Action
                {
                    defaultLabel = "BEWH.MankindsFinest.GeneGestator.EjectGeneMatrix".Translate(containedMatrix.Label),
                    defaultDesc = "BEWH.MankindsFinest.GeneGestator.EjectGeneMatrixDesc".Translate(containedMatrix.Label),
                    icon = EjectIcon,
                    activateSound = SoundDefOf.Designate_Cancel,
                    action = delegate
                    {
                        TryDropContainedMatrix();
                        Reset();
                    }
                };
                yield return command_Action3;
            }
            else
            {
                //SELECTS MATRIX TO LOAD AND START HAUL JOB
                if (selectedMatrix == null)
                {
                    var command_Action4 = new Command_Action
                    {

                        defaultLabel = "BEWH.MankindsFinest.GeneGestator.SelectMatrix".Translate() + "...",
                        defaultDesc = "BEWH.MankindsFinest.GeneGestator.SelectMatrixDesc".Translate(), 
                        icon = MatrixSelectionTex
                    };

                    var gestatablesAvailable = new List<ThingDef>();
                    gestatablesAvailable.AddRange(AvailableGestatables());
                    command_Action4.action = delegate
                    {
                        var list = new List<FloatMenuOption>();
                        foreach (var matrix in gestatablesAvailable)
                        {
                            var text = matrix.label;
                            var floatMenu = new FloatMenuOption(text, delegate
                            {
                                selectedMatrix = matrix;
                                if (containedMatrix == null)
                                { 
                                    return;
                                }
                                
                                TryDropContainedMatrix();
                            });

                            var doesNotHaveMatrix = Map.listerThings.ThingsOfDef(matrix).NullOrEmpty();
                            
                            if (doesNotHaveMatrix)
                            {
                                floatMenu.Disabled = true;
                            }

                            list.Add(floatMenu);
                        }
                        if (!list.Any())
                        {
                            list.Add(new FloatMenuOption("BEWH.MankindsFinest.GeneGestator.NoAvailableMaterial".Translate(), null));
                        } 
                        Find.WindowStack.Add(new FloatMenu(list));
                    };
                    if (!PowerOn)
                    {
                        command_Action4.Disable("NoPower".Translate().CapitalizeFirst());
                    }
                    if (gestatablesAvailable.NullOrEmpty()) 
                    {
                        command_Action4.Disable("BEWH.MankindsFinest.GeneGestator.MissingGestateResearch".Translate().CapitalizeFirst());
                    }
                    yield return command_Action4;
                }
                //STOPS HAUL JOB
                else
                {
                    var command_Action3 = new Command_Action
                    {
                        defaultLabel = "CommandCancelLoad".Translate(),
                        defaultDesc = "CommandCancelLoadDesc".Translate(),
                        icon = CancelIcon,
                        activateSound = SoundDefOf.Designate_Cancel,
                        action = delegate
                        {
                            selectedMatrix = null;
                        }
                    };
                    yield return command_Action3;
                }
            }
        }

        yield return Genes40kUtils.ViewSangprimusPortumGizmo(Map);

        if (!DebugSettings.ShowDevGizmos)
        {
            yield break;
        }
        
        if (doWork)
        {
            //DEV: ALMOST FINISH
            var command_Action1 = new Command_Action
            {
                defaultLabel = "DEV: ALMOST FINISH",
                icon = CancelIcon,
                activateSound = SoundDefOf.Designate_Cancel,
                action = delegate
                {
                    progressInt = totalTime - 250;
                }
            };
            yield return command_Action1;
        }
    }

    private bool CanUseAnyMaterial()
    {
        return ContainedMatrixMod.canUsePrimarchMaterial || ContainedMatrixMod.canUseChapterMaterial;
    }

    private bool CanConfigureMatrix()
    {
        return CanUseAnyMaterial() || ContainedMatrixMod.usesDisciplineCustomization;
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref totalTime, "totalTime", 0f);
        Scribe_Values.Look(ref progressInt, "progress", 0f);
        Scribe_Values.Look(ref doWork, "doWork");
        Scribe_Defs.Look(ref selectedMatrix, "selectedMatrix");
        Scribe_Defs.Look(ref selectedMaterial, "selectedMaterial");
        Scribe_Values.Look(ref selectedCustomChapterId, "selectedCustomChapterId", -1);
        Scribe_Collections.Look(ref selectedDisciplines, "selectedDisciplines", LookMode.Deep);

        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            selectedDisciplines ??= [];
            selectedDisciplines.RemoveAll(entry => entry?.def == null);
        }
        Scribe_Deep.Look(ref containedMatrix, "containedMatrix");
    }
}