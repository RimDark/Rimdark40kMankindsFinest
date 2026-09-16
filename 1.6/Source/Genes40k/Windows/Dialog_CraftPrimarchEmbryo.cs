using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace Genes40k;

/// <summary>
/// Picker opened by the gene manipulation table: one column each for the primarch ascendance vial, the human embryo
/// and the crafter, a summary of the primarch that would result, and an accept that pins a Bill_PrimarchEmbryo to the
/// chosen ingredients.
/// </summary>
public class Dialog_CraftPrimarchEmbryo : Window
{
    private class Candidate
    {
        public Thing thing;
        public string blockReason;
        public bool Usable => blockReason == null;
    }

    private readonly Map map;
    private readonly Building_GeneTable geneTable;
    private readonly RecipeDef recipe = Genes40kDefOf.BEWH_MakePrimarchEmbryo;

    private readonly List<Candidate> vialCandidates = new();
    private readonly List<Candidate> embryoCandidates = new();
    private readonly List<Candidate> pawnCandidates = new();

    private GeneseedVial chosenGeneseedVial;
    private HumanEmbryo chosenEmbryo;
    private Pawn chosenPawn;

    private Vector2 vialScrollPosition;
    private Vector2 embryoScrollPosition;
    private Vector2 pawnScrollPosition;

    private int lastRefreshFrame = -1;

    private const int RefreshFrames = 120;
    private const float RowHeight = 44f;
    private const float IconSize = 32f;
    private const float HeaderHeight = 24f;
    private const float Gap = 8f;
    private const float ResultHeight = 156f;
    private const float InspectButtonHeight = 30f;
    private const float SkillWidth = 62f;
    private const float GeneCountWidth = 62f;

    private static readonly Vector2 ButSize = new(150f, 38f);
    private static readonly Color BlockedTextColor = new(0.5f, 0.5f, 0.5f, 1f);
    private static readonly Color BlockedOverlayColor = new(0f, 0f, 0f, 0.5f);
    private static readonly Color SkillMetColor = new(0.45f, 0.9f, 0.45f, 1f);

    public override Vector2 InitialSize => new(Mathf.Min(UI.screenWidth, 900f), Mathf.Min(UI.screenHeight - 4f, 680f));

    public Dialog_CraftPrimarchEmbryo(Map map, Building_GeneTable geneTable)
    {
        this.map = map;
        this.geneTable = geneTable;
        forcePause = true;
        absorbInputAroundWindow = true;
        doCloseX = true;
        closeOnClickedOutside = false;
    }

    public override void PreOpen()
    {
        base.PreOpen();
        RefreshCandidates();
    }

    public override void DoWindowContents(Rect inRect)
    {
        if (Time.frameCount - lastRefreshFrame >= RefreshFrames)
        {
            RefreshCandidates();
        }

        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 35f), "BEWH.MankindsFinest.GeneManupulationTable.CraftPrimarchEmbryo".Translate());
        Text.Font = GameFont.Small;

        GUI.color = ColoredText.SubtleGrayColor;
        Widgets.Label(new Rect(inRect.x, inRect.y + 36f, inRect.width, Text.LineHeight), "BEWH.MankindsFinest.GeneManupulationTable.CraftPrimarchEmbryoSubtitle".Translate());
        GUI.color = Color.white;

        var contentTop = inRect.y + 36f + Text.LineHeight + 6f;
        var footerTop = inRect.yMax - ButSize.y;
        var columnsRect = new Rect(inRect.x, contentTop, inRect.width, footerTop - Gap - ResultHeight - Gap - contentTop);
        var columnWidth = (columnsRect.width - Gap * 2f) / 3f;

        DrawVialColumn(new Rect(columnsRect.x, columnsRect.y, columnWidth, columnsRect.height));
        DrawEmbryoColumn(new Rect(columnsRect.x + columnWidth + Gap, columnsRect.y, columnWidth, columnsRect.height));
        DrawPawnColumn(new Rect(columnsRect.xMax - columnWidth, columnsRect.y, columnWidth, columnsRect.height));

        DrawResult(new Rect(inRect.x, columnsRect.yMax + Gap, inRect.width, ResultHeight));

        if (Widgets.ButtonText(new Rect(inRect.x, footerTop, ButSize.x, ButSize.y), "Close".Translate()))
        {
            Close();
        }

        var acceptRect = new Rect(inRect.xMax - ButSize.x, footerTop, ButSize.x, ButSize.y);
        var missing = MissingSelectionReason();

        if (!missing.NullOrEmpty())
        {
            TooltipHandler.TipRegion(acceptRect, missing);
        }

        if (Widgets.ButtonText(acceptRect, "Accept".Translate(), true, true, missing.NullOrEmpty()) && missing.NullOrEmpty())
        {
            StartCraft();
        }
    }

    private Rect DrawColumnFrame(Rect rect, string header, bool reserveInspectButton)
    {
        Widgets.DrawMenuSection(rect);

        var headerRect = new Rect(rect.x, rect.y + 4f, rect.width, HeaderHeight);
        Text.Anchor = TextAnchor.MiddleCenter;
        Text.Font = GameFont.Tiny;
        Widgets.Label(headerRect, header);
        Text.Font = GameFont.Small;
        Text.Anchor = TextAnchor.UpperLeft;

        var bottomInset = reserveInspectButton ? InspectButtonHeight + 6f : 4f;

        return new Rect(rect.x + 4f, headerRect.yMax + 2f, rect.width - 8f, rect.yMax - headerRect.yMax - 2f - bottomInset);
    }

    private static void DrawNoneAvailable(Rect rect)
    {
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = ColoredText.SubtleGrayColor;
        Widgets.Label(rect, "BEWH.MankindsFinest.GeneManupulationTable.NoneToSelect".Translate());
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;
    }

    private static bool DrawRowBase(Rect rect, Candidate candidate, bool selected, float rightWidth, out Rect iconRect, out Rect labelRect)
    {
        if (selected)
        {
            Widgets.DrawHighlightSelected(rect);
        }
        else if (Mouse.IsOver(rect))
        {
            Widgets.DrawHighlight(rect);
        }

        iconRect = new Rect(rect.x + 4f, rect.y + (rect.height - IconSize) / 2f, IconSize, IconSize);
        labelRect = new Rect(iconRect.xMax + 6f, rect.y, rect.width - IconSize - 14f - rightWidth, rect.height);

        return candidate.Usable && Widgets.ButtonInvisible(rect);
    }

    private static string RowTooltip(string label, Candidate candidate)
    {
        if (candidate.Usable)
        {
            return label + "\n\n" + "BEWH.MankindsFinest.ImplantGeneseed.ClickToSelect".Translate().Colorize(ColoredText.SubtleGrayColor);
        }

        return label + "\n\n" + candidate.blockReason.Colorize(ColorLibrary.RedReadable);
    }

    private void DrawVialColumn(Rect rect)
    {
        var listRect = DrawColumnFrame(rect, "BEWH.MankindsFinest.GeneManupulationTable.PrimarchGeneseedVialSelection".Translate(), true);

        if (vialCandidates.Count == 0)
        {
            DrawNoneAvailable(listRect);
            return;
        }

        var viewRect = new Rect(0f, 0f, listRect.width - 16f, vialCandidates.Count * RowHeight);
        Widgets.BeginScrollView(listRect, ref vialScrollPosition, viewRect);
        var curY = 0f;

        foreach (var candidate in vialCandidates)
        {
            DrawVialRow(new Rect(0f, curY, viewRect.width, RowHeight), candidate);
            curY += RowHeight;
        }

        Widgets.EndScrollView();

        var inspectRect = new Rect(rect.x + 4f, rect.yMax - InspectButtonHeight - 2f, rect.width - 8f, InspectButtonHeight);

        if (Widgets.ButtonText(inspectRect, "InspectGenes".Translate(), true, true, chosenGeneseedVial != null) && chosenGeneseedVial != null)
        {
            Genes40kUtils.InspectGeneseedVialGenes(chosenGeneseedVial);
        }
    }

    private void DrawVialRow(Rect rect, Candidate candidate)
    {
        var vial = (GeneseedVial)candidate.thing;
        var selected = chosenGeneseedVial == vial;
        var clicked = DrawRowBase(rect, candidate, selected, 0f, out var iconRect, out var labelRect);

        Widgets.ThingIcon(iconRect, vial);

        if (vial.extraGeneFromMaterial != null)
        {
            var geneIconRect = new Rect(iconRect.xMax + 2f, iconRect.y, IconSize, IconSize);
            GUI.DrawTexture(geneIconRect, vial.extraGeneFromMaterial.Icon);
            labelRect.xMin = geneIconRect.xMax + 6f;
        }

        if (!candidate.Usable)
        {
            Widgets.DrawBoxSolid(new Rect(iconRect.x, iconRect.y, labelRect.xMin - iconRect.x - 6f, IconSize), BlockedOverlayColor);
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.color = candidate.Usable ? Color.white : BlockedTextColor;
        Widgets.Label(labelRect, Genes40kUtils.PrimarchLineLabel(vial).Truncate(labelRect.width));
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;

        TooltipHandler.TipRegion(rect, () => RowTooltip(vial.LabelCap, candidate), vial.thingIDNumber);

        if (clicked)
        {
            chosenGeneseedVial = selected ? null : vial;
        }
    }

    private void DrawEmbryoColumn(Rect rect)
    {
        var listRect = DrawColumnFrame(rect, "BEWH.MankindsFinest.GeneManupulationTable.HumanEmbryoSelection".Translate(), true);

        if (embryoCandidates.Count == 0)
        {
            DrawNoneAvailable(listRect);
            return;
        }

        var viewRect = new Rect(0f, 0f, listRect.width - 16f, embryoCandidates.Count * RowHeight);
        Widgets.BeginScrollView(listRect, ref embryoScrollPosition, viewRect);
        var curY = 0f;

        foreach (var candidate in embryoCandidates)
        {
            DrawEmbryoRow(new Rect(0f, curY, viewRect.width, RowHeight), candidate);
            curY += RowHeight;
        }

        Widgets.EndScrollView();

        var inspectRect = new Rect(rect.x + 4f, rect.yMax - InspectButtonHeight - 2f, rect.width - 8f, InspectButtonHeight);

        if (Widgets.ButtonText(inspectRect, "InspectGenes".Translate(), true, true, chosenEmbryo != null) && chosenEmbryo != null)
        {
            Find.WindowStack.Add(new Dialog_ViewGenesEmbryo(chosenEmbryo));
        }
    }

    private void DrawEmbryoRow(Rect rect, Candidate candidate)
    {
        var embryo = (HumanEmbryo)candidate.thing;
        var selected = chosenEmbryo == embryo;
        var clicked = DrawRowBase(rect, candidate, selected, GeneCountWidth, out var iconRect, out var labelRect);

        Widgets.ThingIcon(iconRect, embryo);

        if (!candidate.Usable)
        {
            Widgets.DrawBoxSolid(iconRect, BlockedOverlayColor);
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.color = candidate.Usable ? Color.white : BlockedTextColor;
        Widgets.Label(labelRect, ParentsLabel(embryo).Truncate(labelRect.width));

        var geneCount = embryo.GeneSet?.GenesListForReading?.Count ?? 0;
        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(new Rect(labelRect.xMax, rect.y, GeneCountWidth - 4f, rect.height), "BEWH.MankindsFinest.GeneManupulationTable.GeneCount".Translate(geneCount));
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;

        TooltipHandler.TipRegion(rect, () => RowTooltip(embryo.LabelCap + "\n" + ParentsLabel(embryo), candidate), embryo.thingIDNumber);

        if (clicked)
        {
            chosenEmbryo = selected ? null : embryo;
        }
    }

    private void DrawPawnColumn(Rect rect)
    {
        var listRect = DrawColumnFrame(rect, "BEWH.MankindsFinest.GeneManupulationTable.PawnSelection".Translate(), false);

        if (pawnCandidates.Count == 0)
        {
            DrawNoneAvailable(listRect);
            return;
        }

        var viewRect = new Rect(0f, 0f, listRect.width - 16f, pawnCandidates.Count * RowHeight);
        Widgets.BeginScrollView(listRect, ref pawnScrollPosition, viewRect);
        var curY = 0f;

        foreach (var candidate in pawnCandidates)
        {
            DrawPawnRow(new Rect(0f, curY, viewRect.width, RowHeight), candidate);
            curY += RowHeight;
        }

        Widgets.EndScrollView();
    }

    private void DrawPawnRow(Rect rect, Candidate candidate)
    {
        var pawn = (Pawn)candidate.thing;
        var selected = chosenPawn == pawn;
        var clicked = DrawRowBase(rect, candidate, selected, SkillWidth, out var iconRect, out var labelRect);

        Widgets.ThingIcon(iconRect, pawn);

        if (!candidate.Usable)
        {
            Widgets.DrawBoxSolid(iconRect, BlockedOverlayColor);
        }

        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.color = candidate.Usable ? Color.white : BlockedTextColor;
        Widgets.Label(labelRect, pawn.LabelShortCap.Truncate(labelRect.width));
        GUI.color = Color.white;

        Text.Anchor = TextAnchor.MiddleRight;
        Widgets.Label(new Rect(labelRect.xMax, rect.y, SkillWidth - 4f, rect.height), SkillLevelsLabel(pawn));
        Text.Anchor = TextAnchor.UpperLeft;

        TooltipHandler.TipRegion(rect, () => RowTooltip(pawn.LabelCap + "\n" + SkillRequirementsText(), candidate), pawn.thingIDNumber);

        if (clicked)
        {
            chosenPawn = selected ? null : pawn;
        }
    }

    private void DrawResult(Rect rect)
    {
        Widgets.DrawMenuSection(rect);
        var inner = rect.ContractedBy(8f);

        if (chosenGeneseedVial == null)
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = ColoredText.SubtleGrayColor;
            Widgets.Label(inner, "BEWH.MankindsFinest.GeneManupulationTable.NothingSelected".Translate());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            return;
        }

        var iconRect = new Rect(inner.x, inner.y, 48f, 48f);
        var icon = chosenGeneseedVial.iconDef != null ? chosenGeneseedVial.iconDef.Icon : Genes40kDefOf.BEWH_Primarch.Icon;
        GUI.color = XenotypeDef.IconColor;
        GUI.DrawTexture(iconRect, icon);
        GUI.color = Color.white;

        var textX = iconRect.xMax + 10f;
        var textWidth = inner.xMax - textX - ButSize.x - 8f;
        var curY = inner.y;

        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(textX, curY, textWidth, 32f), Genes40kUtils.PrimarchLineLabel(chosenGeneseedVial));
        Text.Font = GameFont.Small;
        curY += 32f;

        GUI.color = ColoredText.SubtleGrayColor;
        var xenotype = chosenGeneseedVial.xenotype ?? Genes40kDefOf.BEWH_Primarch;
        Widgets.Label(new Rect(textX, curY, textWidth, Text.LineHeight), xenotype.LabelCap);
        GUI.color = Color.white;
        curY += Text.LineHeight + 4f;

        if (chosenEmbryo != null)
        {
            Widgets.Label(new Rect(textX, curY, textWidth, Text.LineHeight), "BEWH.MankindsFinest.GeneManupulationTable.ResultParents".Translate(ParentsLabel(chosenEmbryo)));
            curY += Text.LineHeight;
        }

        Widgets.Label(new Rect(textX, curY, textWidth, Text.LineHeight), CraftWorkLine());
        curY += Text.LineHeight;

        Widgets.Label(new Rect(textX, curY, textWidth, Text.LineHeight), GestationLine());

        if (chosenEmbryo == null)
        {
            return;
        }

        var inspectRect = new Rect(inner.xMax - ButSize.x, inner.yMax - InspectButtonHeight, ButSize.x, InspectButtonHeight);

        if (Widgets.ButtonText(inspectRect, "BEWH.MankindsFinest.GeneManupulationTable.InspectResultGenes".Translate()))
        {
            Genes40kUtils.InspectProspectivePrimarchGenes(chosenGeneseedVial, chosenEmbryo);
        }
    }

    private string CraftWorkLine()
    {
        var workAmount = recipe.WorkAmountTotal(null);

        if (chosenPawn == null || recipe.workSpeedStat == null)
        {
            return "BEWH.MankindsFinest.GeneManupulationTable.ResultCraftWork".Translate(workAmount.ToStringWorkAmount());
        }

        var speed = chosenPawn.GetStatValue(recipe.workSpeedStat);

        if (speed <= 0f)
        {
            return "BEWH.MankindsFinest.GeneManupulationTable.ResultCraftWork".Translate(workAmount.ToStringWorkAmount());
        }

        return "BEWH.MankindsFinest.GeneManupulationTable.ResultCraftWorkBy".Translate(Mathf.RoundToInt(workAmount / speed).ToStringTicksToPeriod(), chosenPawn.LabelShortCap);
    }

    private string GestationLine()
    {
        var genes = new List<GeneDef>();

        if (chosenGeneseedVial.GeneSet != null)
        {
            genes.AddRange(chosenGeneseedVial.GeneSet.GenesListForReading);
        }

        if (chosenGeneseedVial.extraGeneFromMaterial != null)
        {
            genes.Add(chosenGeneseedVial.extraGeneFromMaterial);
        }

        var ticks = CustomChapterGeneUtility.GestationTicksFor(genes, Building_PrimarchGrowthVat.EmbryoGestationTicks);
        string line = "BEWH.MankindsFinest.GeneManupulationTable.ResultGestation".Translate(ticks.ToStringTicksToPeriod());
        var offset = ticks - Building_PrimarchGrowthVat.EmbryoGestationTicks;

        if (offset == 0)
        {
            return line;
        }

        string offsetText = "BEWH.MankindsFinest.GeneManupulationTable.ResultGestationOffset".Translate((offset > 0 ? "+" : "-") + Mathf.Abs(offset).ToStringTicksToPeriod());

        return line + " " + offsetText.Colorize(offset > 0 ? ColorLibrary.RedReadable : SkillMetColor);
    }

    private static string ParentsLabel(HumanEmbryo embryo)
    {
        var mother = embryo.Mother != null ? embryo.Mother.LabelShortCap.ToString() : "BEWH.MankindsFinest.CommonKeywords.Unknown".Translate().ToString();
        string father = "BEWH.MankindsFinest.CommonKeywords.Emperor".Translate();

        return "BEWH.MankindsFinest.GeneManupulationTable.EmbryoParents".Translate(mother, father);
    }

    private string SkillLevelsLabel(Pawn pawn)
    {
        if (recipe.skillRequirements.NullOrEmpty())
        {
            return string.Empty;
        }

        var parts = new List<string>();

        foreach (var skillRequirement in recipe.skillRequirements)
        {
            var level = pawn.skills?.GetSkill(skillRequirement.skill)?.Level ?? 0;
            parts.Add(level.ToString().Colorize(level >= skillRequirement.minLevel ? SkillMetColor : ColorLibrary.RedReadable));
        }

        return string.Join(" ", parts);
    }

    private string SkillRequirementsText()
    {
        if (recipe.skillRequirements.NullOrEmpty())
        {
            return string.Empty;
        }

        return string.Join(" ", recipe.skillRequirements.Select(skillRequirement => skillRequirement.skill.label + ": " + skillRequirement.minLevel)).Trim();
    }

    private string MissingSelectionReason()
    {
        if (!recipe.AvailableNow)
        {
            return "BEWH.MankindsFinest.GeneManupulationTable.ResearchMissing".Translate(recipe.researchPrerequisite != null ? recipe.researchPrerequisite.LabelCap : recipe.LabelCap);
        }

        if (!geneTable.CurrentlyUsableForBills())
        {
            return "BEWH.MankindsFinest.GeneManupulationTable.BlockedTableUnusable".Translate();
        }

        if (chosenGeneseedVial == null)
        {
            return "BEWH.MankindsFinest.GeneManupulationTable.NeedVial".Translate();
        }

        if (chosenEmbryo == null)
        {
            return "BEWH.MankindsFinest.GeneManupulationTable.NeedEmbryo".Translate();
        }

        if (chosenPawn == null)
        {
            return "BEWH.MankindsFinest.GeneManupulationTable.NeedCrafter".Translate();
        }

        return null;
    }

    private void RefreshCandidates()
    {
        lastRefreshFrame = Time.frameCount;

        pawnCandidates.Clear();

        foreach (var pawn in map.mapPawns.FreeColonistsSpawned)
        {
            if (pawn == null)
            {
                continue;
            }

            pawnCandidates.Add(new Candidate
            {
                thing = pawn,
                blockReason = PawnBlockReason(pawn)
            });
        }

        var usablePawns = pawnCandidates.Where(candidate => candidate.Usable).Select(candidate => (Pawn)candidate.thing).ToList();

        vialCandidates.Clear();

        foreach (var thing in map.listerThings.ThingsOfDef(Genes40kDefOf.BEWH_GeneseedVialPrimarch))
        {
            if (thing is not GeneseedVial vial)
            {
                continue;
            }

            vialCandidates.Add(new Candidate
            {
                thing = vial,
                blockReason = IngredientBlockReason(vial, usablePawns)
            });
        }

        embryoCandidates.Clear();

        foreach (var thing in map.listerThings.ThingsOfDef(ThingDefOf.HumanEmbryo))
        {
            if (thing is not HumanEmbryo embryo)
            {
                continue;
            }

            embryoCandidates.Add(new Candidate
            {
                thing = embryo,
                blockReason = IngredientBlockReason(embryo, usablePawns)
            });
        }

        pawnCandidates.SortBy(candidate => candidate.Usable ? 0 : 1);
        vialCandidates.SortBy(candidate => candidate.Usable ? 0 : 1);
        embryoCandidates.SortBy(candidate => candidate.Usable ? 0 : 1);

        if (!vialCandidates.Any(candidate => candidate.Usable && candidate.thing == chosenGeneseedVial))
        {
            chosenGeneseedVial = null;
        }

        if (!embryoCandidates.Any(candidate => candidate.Usable && candidate.thing == chosenEmbryo))
        {
            chosenEmbryo = null;
        }

        if (!pawnCandidates.Any(candidate => candidate.Usable && candidate.thing == chosenPawn))
        {
            chosenPawn = null;
        }
    }

    private string IngredientBlockReason(Thing thing, List<Pawn> usablePawns)
    {
        if (thing.IsForbidden(Faction.OfPlayer))
        {
            return "BEWH.MankindsFinest.GeneManupulationTable.BlockedForbidden".Translate();
        }

        if (geneTable.billStack.Bills.OfType<Bill_PrimarchEmbryo>().Any(bill => bill.pinnedVial == thing || bill.pinnedEmbryo == thing))
        {
            return "BEWH.MankindsFinest.GeneManupulationTable.BlockedAlreadyPinned".Translate();
        }

        if (map.reservationManager.IsReservedByAnyoneOf(thing, Faction.OfPlayer))
        {
            return "BEWH.MankindsFinest.GeneManupulationTable.BlockedReserved".Translate();
        }

        if (usablePawns.Count > 0 && !usablePawns.Any(pawn => pawn.CanReach(thing, PathEndMode.ClosestTouch, Danger.Deadly)))
        {
            return "BEWH.MankindsFinest.GeneManupulationTable.BlockedUnreachable".Translate();
        }

        return null;
    }

    private string PawnBlockReason(Pawn pawn)
    {
        if (pawn.Dead || pawn.Downed || !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
        {
            return "BEWH.MankindsFinest.GeneManupulationTable.BlockedPawnIncapable".Translate();
        }

        if (pawn.InMentalState)
        {
            return "BEWH.MankindsFinest.GeneManupulationTable.BlockedPawnMentalState".Translate();
        }

        if (pawn.Drafted)
        {
            return "BEWH.MankindsFinest.GeneManupulationTable.BlockedPawnDrafted".Translate();
        }

        if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Research) || pawn.workSettings is not { EverWork: true } || !pawn.workSettings.WorkIsActive(WorkTypeDefOf.Research))
        {
            return "BEWH.MankindsFinest.GeneManupulationTable.BlockedPawnWorkDisabled".Translate(WorkTypeDefOf.Research.labelShort);
        }

        if (!recipe.PawnSatisfiesSkillRequirements(pawn))
        {
            return "BEWH.MankindsFinest.GeneManupulationTable.NotSkilledEnoughPrimarchEmbryo".Translate(pawn, SkillRequirementsText());
        }

        if (!pawn.CanReach(geneTable, PathEndMode.InteractionCell, Danger.Deadly))
        {
            return "BEWH.MankindsFinest.GeneManupulationTable.BlockedPawnCannotReach".Translate();
        }

        return null;
    }

    private void StartCraft()
    {
        var bill = new Bill_PrimarchEmbryo(recipe, chosenGeneseedVial, chosenEmbryo)
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1
        };

        geneTable.billStack.AddBill(bill);

        var ingredients = new List<ThingCount>
        {
            new(chosenGeneseedVial, 1),
            new(chosenEmbryo, 1)
        };

        var job = WorkGiver_DoBill.TryStartNewDoBillJob(chosenPawn, bill, geneTable, ingredients, out _);

        if (job != null)
        {
            chosenPawn.jobs.TryTakeOrderedJob(job);
        }
        else
        {
            Messages.Message("BEWH.MankindsFinest.GeneManupulationTable.CouldNotStartNow".Translate(chosenPawn.LabelShortCap), geneTable, MessageTypeDefOf.CautionInput, false);
        }

        Close();
    }
}
