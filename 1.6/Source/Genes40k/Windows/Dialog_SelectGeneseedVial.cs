using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Genes40k;

/// <summary>
/// Picker opened by the Astartes gene-seed implantation surgery: one tab per vial type (Firstborn, Primaris), each listing
/// every vial kind once with the vials on the map, this patient's failure chance, and for kinds without vials what is
/// needed to gestate or unlock them. Accepting creates a Bill_GeneseedImplant pinned to the chosen kind.
/// </summary>
public class Dialog_SelectGeneseedVial : Window
{
    private readonly Pawn pawn;
    private readonly RecipeDef recipe;
    private readonly BodyPartRecord part;

    private List<GeneseedVialKind> kinds;
    private List<ThingDef> tabVialDefs;
    private ThingDef currentTab;
    private GeneseedVialKind selected;

    private Vector2 scrollPosition;
    private float scrollHeight;

    private const float RowHeight = 44f;
    private const float IconSize = 32f;
    private const float CountWidth = 50f;
    private const float ChanceWidth = 90f;
    private static readonly Vector2 ButSize = new(150f, 38f);

    private static readonly Color LockedTextColor = new(0.5f, 0.5f, 0.5f, 1f);
    private static readonly Color GoodChanceColor = new(0.45f, 0.9f, 0.45f, 1f);
    private static readonly Color MediumChanceColor = new(0.95f, 0.85f, 0.35f, 1f);
    private static readonly Color LockedOverlayColor = new(0f, 0f, 0f, 0.5f);

    public override Vector2 InitialSize => new(620f, 640f);

    public Dialog_SelectGeneseedVial(Pawn pawn, RecipeDef recipe, BodyPartRecord part)
    {
        this.pawn = pawn;
        this.recipe = recipe;
        this.part = part;
        forcePause = true;
        absorbInputAroundWindow = true;
        doCloseX = true;
        closeOnAccept = false;
    }

    public override void PreOpen()
    {
        base.PreOpen();
        kinds = GeneseedVialKindUtility.KindsFor(pawn.MapHeld);
        tabVialDefs = GeneseedVialKindUtility.PickerVialDefs.Where(vialDef => kinds.Any(kind => kind.vialDef == vialDef)).ToList();
        currentTab = tabVialDefs.FirstOrDefault(vialDef => kinds.Any(kind => kind.vialDef == vialDef && kind.Available)) ?? tabVialDefs.FirstOrDefault();
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 35f), "BEWH.MankindsFinest.ImplantGeneseed.SelectVial".Translate(pawn.LabelShortCap));
        Text.Font = GameFont.Small;

        GUI.color = ColoredText.SubtleGrayColor;
        Widgets.Label(new Rect(inRect.x, inRect.y + 36f, inRect.width, Text.LineHeight), "BEWH.MankindsFinest.ImplantGeneseed.SelectVialDesc".Translate());
        GUI.color = Color.white;

        var listTop = inRect.y + 36f + Text.LineHeight + 6f + TabDrawer.TabHeight;
        var listRect = new Rect(inRect.x, listTop, inRect.width, inRect.yMax - listTop - ButSize.y - 8f);

        var tabs = tabVialDefs.Select(vialDef => new TabRecord(TabLabel(vialDef), delegate
        {
            currentTab = vialDef;
            selected = null;
            scrollPosition = Vector2.zero;
        }, currentTab == vialDef)).ToList();

        Widgets.DrawMenuSection(listRect);

        if (tabs.Any())
        {
            TabDrawer.DrawTabs(listRect, tabs);
        }

        var outRect = listRect.ContractedBy(4f);
        var viewRect = new Rect(0f, 0f, outRect.width - 16f, scrollHeight);

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        var curY = 0f;

        foreach (var kind in kinds.Where(kind => kind.vialDef == currentTab))
        {
            DrawRow(kind, new Rect(0f, curY, viewRect.width, RowHeight));
            curY += RowHeight;
        }

        if (Event.current.type == EventType.Layout)
        {
            scrollHeight = curY;
        }

        Widgets.EndScrollView();

        var buttonsRect = new Rect(inRect.x, inRect.yMax - ButSize.y, inRect.width, ButSize.y);

        if (Widgets.ButtonText(new Rect(buttonsRect.x, buttonsRect.y, ButSize.x, ButSize.y), "Close".Translate()))
        {
            Close();
        }

        var canAccept = selected is { Available: true };

        if (Widgets.ButtonText(new Rect(buttonsRect.xMax - ButSize.x, buttonsRect.y, ButSize.x, ButSize.y), "Accept".Translate(), true, true, canAccept) && canAccept)
        {
            GeneseedVialKindUtility.CreateBill(pawn, recipe, part, selected);
            Close();
        }
    }

    private static string TabLabel(ThingDef vialDef)
    {
        var xenotype = vialDef.GetModExtension<DefModExtension_GeneseedVial>()?.xenotype;
        return xenotype != null ? xenotype.LabelCap : vialDef.LabelCap;
    }

    private void DrawRow(GeneseedVialKind kind, Rect rect)
    {
        if (selected == kind)
        {
            Widgets.DrawHighlightSelected(rect);
        }
        else if (Mouse.IsOver(rect))
        {
            Widgets.DrawHighlight(rect);
        }

        var iconRect = new Rect(rect.x + 4f, rect.y + (rect.height - IconSize) / 2f, IconSize, IconSize);
        var sample = kind.vials.FirstOrDefault();

        if (sample != null)
        {
            Widgets.ThingIcon(iconRect, sample);
        }
        else
        {
            Widgets.ThingIcon(iconRect, kind.vialDef);
        }

        var geneIconRect = new Rect(iconRect.xMax + 4f, iconRect.y, IconSize, IconSize);

        if (kind.chapterGene != null)
        {
            GUI.DrawTexture(geneIconRect, kind.chapterGene.Icon);
        }

        if (!kind.Available)
        {
            Widgets.DrawBoxSolid(new Rect(iconRect.x, iconRect.y, geneIconRect.xMax - iconRect.x, IconSize), LockedOverlayColor);
        }

        var labelRect = new Rect(geneIconRect.xMax + 8f, rect.y, rect.width - geneIconRect.xMax - 8f - ChanceWidth - CountWidth - 8f, rect.height);
        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.color = kind.Available ? Color.white : LockedTextColor;
        Widgets.Label(labelRect, kind.Label.Truncate(labelRect.width));

        var countRect = new Rect(labelRect.xMax, rect.y, CountWidth, rect.height);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(countRect, kind.Available ? "x" + kind.vials.Count : "-");

        var chanceRect = new Rect(countRect.xMax, rect.y, ChanceWidth - 4f, rect.height);
        Text.Anchor = TextAnchor.MiddleRight;
        var failChance = Genes40kUtils.GetGeneseedImplantationFailChance(pawn, kind.vialDef, kind.chapterGene);
        GUI.color = kind.Available ? ChanceColor(failChance) : LockedTextColor;
        Widgets.Label(chanceRect, "BEWH.MankindsFinest.ImplantGeneseed.FailChanceShort".Translate(failChance));
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;

        var breakdownRect = new Rect(countRect.x, rect.y, rect.xMax - countRect.x, rect.height);

        if (Mouse.IsOver(breakdownRect))
        {
            TooltipHandler.TipRegion(breakdownRect, () => Genes40kUtils.GetGeneseedImplantationFailChanceDesc(pawn, kind.vialDef, kind.chapterGene, false, false, false), kind.GetHashCode() ^ 0x5f3a);
        }
        else
        {
            var rowRect = new Rect(rect.x, rect.y, countRect.x - rect.x, rect.height);
            TooltipHandler.TipRegion(rowRect, () => RowTooltip(kind), kind.GetHashCode());
        }

        if (Widgets.ButtonInvisible(rect) && kind.Available)
        {
            selected = kind;
        }
    }

    private string RowTooltip(GeneseedVialKind kind)
    {
        string text = "BEWH.MankindsFinest.GeneseedVial.ImplantGeneseedDesc".Translate(pawn, kind.Label);

        if (kind.Available)
        {
            text += "\n\n" + "BEWH.MankindsFinest.ImplantGeneseed.AvailableCount".Translate(kind.vials.Count);
        }
        else if (kind.Locked)
        {
            var missing = new List<string>();
            missing.AddRange(kind.missingResearch.Select(research => "BEWH.MankindsFinest.CustomChapter.RequiresResearch".Translate(research.LabelCap).ToString()));

            if (kind.missingMaterial && kind.chapterMaterial != null)
            {
                var materialName = kind.chapterMaterial.GetModExtension<DefModExtension_ChapterMaterial>()?.shownMaterialName;
                missing.Add("BEWH.MankindsFinest.CustomChapter.RequiresMaterial".Translate(materialName.NullOrEmpty() ? kind.chapterMaterial.label : materialName));
            }

            text += "\n\n" + ("BEWH.MankindsFinest.ImplantGeneseed.LockedKind".Translate() + "\n" + missing.ToLineList("  - ")).Colorize(ColorLibrary.RedReadable);
        }
        else
        {
            var matrixLabel = kind.matrixDef?.label ?? string.Empty;
            string gestateHint = kind.customChapter != null
                ? (kind.customChapter.Kind == CustomGeneKind.Custodes ? "BEWH.MankindsFinest.ImplantGeneseed.GestateHintCustodes" : "BEWH.MankindsFinest.ImplantGeneseed.GestateHintCustom").Translate(matrixLabel, kind.customChapter.name)
                : kind.chapterMaterial != null
                    ? "BEWH.MankindsFinest.ImplantGeneseed.GestateHintMaterial".Translate(matrixLabel, kind.chapterMaterial.label)
                    : "BEWH.MankindsFinest.ImplantGeneseed.GestateHint".Translate(matrixLabel);
            text += "\n\n" + gestateHint.Colorize(ColoredText.SubtleGrayColor);
        }

        var requiredSkill = GeneseedVialKindUtility.RequiredMedicineSkill(kind.vialDef);

        if (requiredSkill > 0)
        {
            text += "\n\n" + "BEWH.MankindsFinest.ImplantGeneseed.RequiresSkill".Translate(requiredSkill);
        }

        if (kind.Available)
        {
            text += "\n\n" + "BEWH.MankindsFinest.ImplantGeneseed.ClickToSelect".Translate().Colorize(ColoredText.SubtleGrayColor);
        }

        return text;
    }

    private static Color ChanceColor(int failChance)
    {
        if (failChance <= 15)
        {
            return GoodChanceColor;
        }

        return failChance <= 40 ? MediumChanceColor : ColorLibrary.RedReadable;
    }
}
