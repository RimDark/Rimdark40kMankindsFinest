using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Genes40k;

/// <summary>
/// Editor for a player-designed chapter or primarch gene-seed: pick traits within the line's budget, name it and give it
/// an icon. Opened from the Sangprimus Portum or the design list for a new design (with a tab per line), or to edit/view
/// an existing one.
/// </summary>
[StaticConstructorOnStartup]
public class Dialog_CreateChapterGene : Window
{
    private readonly CustomChapterGene editing;
    private readonly bool readOnly;
    private CustomChapterGeneTemplateDef template;

    private string chapterName = string.Empty;
    private FlagIconDef flagIconDef;
    private GeneDef relatedPrimarchGene;
    private readonly List<ChapterTraitDef> selected = [];

    private Vector2 scrollPosition;
    private float scrollHeight;
    private readonly QuickSearchWidget quickSearchWidget = new();

    private static readonly Regex ValidSymbolRegex = new("^[\\p{L}0-9 '\\-]*$");
    private static readonly Vector2 ButSize = new(150f, 38f);
    private const float HeaderHeight = 35f;
    private const float CardGap = 4f;
    private const float StabilityColumnWidth = 30f;
    private const int MaxNameLength = 40;

    private static readonly Color OutlineColorSelected = new(1f, 1f, 0.7f, 1f);
    private static readonly Color PositiveStabilityColor = new(0.45f, 0.9f, 0.45f, 1f);
    private static readonly Color LockedOverlayColor = new(0f, 0f, 0f, 0.55f);

    private static GameComponent_CustomChapterGenes GameComp => GameComponent_CustomChapterGenes.Instance;
    private CustomChapterGeneTemplateDef Tuning => template;

    private static List<CustomChapterGeneTemplateDef> TraitTemplates => CustomChapterGeneUtility.TemplatesInOrder.Where(candidate => candidate.kind != CustomGeneKind.Custodes).ToList();

    private static float CardWidth => GeneCreationDialogBase.GeneSize.x + StabilityColumnWidth + 8f;
    private static float CardHeight => GeneCreationDialogBase.GeneSize.y + 8f;

    public override Vector2 InitialSize => new(Mathf.Min(UI.screenWidth, 1000f), Mathf.Min(UI.screenHeight - 4f, 820f));

    public Dialog_CreateChapterGene(CustomChapterGeneTemplateDef template = null, CustomChapterGene chapter = null, bool readOnly = false)
    {
        editing = chapter;
        this.readOnly = readOnly;
        this.template = chapter?.Template ?? template ?? CustomChapterGeneUtility.Tuning;
        forcePause = true;
        absorbInputAroundWindow = true;
        doCloseX = true;
        closeOnAccept = false;
        flagIconDef = Genes40kDefOf.BEWH_FlagAquila;

        if (chapter == null)
        {
            return;
        }

        chapterName = chapter.name;
        flagIconDef = chapter.flagIconDef ?? Genes40kDefOf.BEWH_FlagAquila;
        relatedPrimarchGene = chapter.RelatedPrimarchGene;
        selected.AddRange(chapter.traits);
    }

    private bool IsPrimarch => template.kind == CustomGeneKind.Primarch;

    private int Stability => CustomChapterGeneUtility.StabilityOf(selected);

    private string Header
    {
        get
        {
            var prefix = IsPrimarch ? "BEWH.MankindsFinest.CustomPrimarch." : "BEWH.MankindsFinest.CustomChapter.";

            if (readOnly)
            {
                return (prefix + "ViewHeader").Translate(chapterName);
            }

            return editing == null ? (prefix + "CreateHeader").Translate() : (prefix + "EditHeader").Translate();
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        var rect = inRect;
        rect.yMax -= ButSize.y + 4f;

        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(rect.x, rect.y, rect.width - 320f, HeaderHeight), Header);
        Text.Font = GameFont.Small;
        quickSearchWidget.OnGUI(new Rect(rect.xMax - 300f, rect.y + 5f, 300f, 24f));
        rect.yMin += HeaderHeight + 4f;

        if (editing == null && !readOnly && TraitTemplates.Count > 1)
        {
            var tabsRect = new Rect(rect.x, rect.y + TabDrawer.TabHeight, rect.width, 0f);
            var tabs = TraitTemplates.Select(candidate => new TabRecord(CustomChapterGeneUtility.KindLabel(candidate), delegate
            {
                if (template == candidate)
                {
                    return;
                }

                template = candidate;
                selected.Clear();
                relatedPrimarchGene = null;
                scrollPosition = Vector2.zero;
            }, template == candidate)).ToList();
            TabDrawer.DrawTabs(tabsRect, tabs);
            rect.yMin += TabDrawer.TabHeight + 4f;
        }

        var footerHeight = Text.LineHeight * 2f + 8f + 30f + 8f;
        var footerRect = new Rect(rect.x, rect.yMax - footerHeight, rect.width, footerHeight);
        rect.yMax -= footerHeight + 8f;

        var curY = rect.y;
        DrawSelectedSection(rect, ref curY);
        curY += 10f;
        DrawTraitsSection(new Rect(rect.x, curY, rect.width, rect.yMax - curY));
        DrawFooter(footerRect);

        var buttonsRect = inRect;
        buttonsRect.yMin = buttonsRect.yMax - ButSize.y;
        DoBottomButtons(buttonsRect);
    }

    private void DrawSelectedSection(Rect rect, ref float curY)
    {
        var label = "BEWH.MankindsFinest.CustomChapter.SelectedTraits".Translate(selected.Count, CustomChapterGeneUtility.MaxTraits(Tuning));
        Widgets.Label(new Rect(rect.x, curY, rect.width, Text.LineHeight), label);

        if (!readOnly)
        {
            Text.Anchor = TextAnchor.UpperRight;
            GUI.color = ColoredText.SubtleGrayColor;
            Widgets.Label(new Rect(rect.x, curY, rect.width, Text.LineHeight), "ClickToAddOrRemove".Translate());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }

        curY += Text.LineHeight + 3f;

        var sectionRect = new Rect(rect.x, curY, rect.width, CardHeight + 8f);
        Widgets.DrawRectFast(sectionRect, Widgets.MenuSectionBGFillColor);

        if (!selected.Any())
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = ColoredText.SubtleGrayColor;
            Widgets.Label(sectionRect, "(" + "NoneLower".Translate() + ")");
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
        }
        else
        {
            var curX = rect.x + 4f;
            ChapterTraitDef toRemove = null;

            foreach (var trait in selected)
            {
                if (DrawTraitCard(trait, new Rect(curX, curY + 4f, CardWidth, CardHeight), true))
                {
                    toRemove = trait;
                }

                curX += CardWidth + CardGap;
            }

            if (toRemove != null && !readOnly)
            {
                SoundDefOf.Tick_Low.PlayOneShotOnCamera();
                selected.Remove(toRemove);
            }
        }

        curY = sectionRect.yMax;
    }

    private void DrawTraitsSection(Rect rect)
    {
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, Text.LineHeight), (IsPrimarch ? "BEWH.MankindsFinest.CustomPrimarch.AvailableTraits" : "BEWH.MankindsFinest.CustomChapter.AvailableTraits").Translate());
        var outRect = new Rect(rect.x, rect.y + Text.LineHeight + 3f, rect.width, rect.height - Text.LineHeight - 3f);
        var viewRect = new Rect(0f, 0f, outRect.width - 16f, scrollHeight);

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        var curY = 0f;

        var defaultTraits = new List<ChapterTraitDef>();
        var unlockedTraits = new List<ChapterTraitDef>();
        var lockedTraits = new List<ChapterTraitDef>();

        foreach (var trait in CustomChapterGeneUtility.TraitsFor(template.kind))
        {
            if (quickSearchWidget.filter.Active && !quickSearchWidget.filter.Matches(trait.label))
            {
                continue;
            }

            if (!CustomChapterGeneUtility.TraitUnlocked(trait, out _))
            {
                lockedTraits.Add(trait);
            }
            else if (CustomChapterGeneUtility.RequirementDescription(trait) != null)
            {
                unlockedTraits.Add(trait);
            }
            else
            {
                defaultTraits.Add(trait);
            }
        }

        DrawTraitGroup(viewRect, defaultTraits, "BEWH.MankindsFinest.CustomChapter.GroupDefault".Translate(), ref curY);
        DrawTraitGroup(viewRect, unlockedTraits, "BEWH.MankindsFinest.CustomChapter.GroupUnlocked".Translate(), ref curY);
        DrawTraitGroup(viewRect, lockedTraits, "BEWH.MankindsFinest.CustomChapter.GroupLocked".Translate(), ref curY);

        if (Event.current.type == EventType.Layout)
        {
            scrollHeight = curY;
        }

        Widgets.EndScrollView();
    }

    private void DrawTraitGroup(Rect viewRect, List<ChapterTraitDef> traits, string label, ref float curY)
    {
        if (!traits.Any())
        {
            return;
        }

        var labelRect = new Rect(4f, curY, viewRect.width - 8f, Text.LineHeight);
        Widgets.Label(labelRect, label);
        curY += Text.LineHeight;
        GUI.color = Color.grey;
        Widgets.DrawLineHorizontal(4f, curY, viewRect.width - 8f);
        GUI.color = Color.white;
        curY += 6f;

        var curX = 4f;
        ChapterTraitDef clicked = null;

        foreach (var trait in traits)
        {
            if (curX + CardWidth > viewRect.width - 4f)
            {
                curX = 4f;
                curY += CardHeight + CardGap;
            }

            if (DrawTraitCard(trait, new Rect(curX, curY, CardWidth, CardHeight), false))
            {
                clicked = trait;
            }

            curX += CardWidth + CardGap;
        }

        curY += CardHeight + 14f;

        if (clicked != null && !readOnly)
        {
            ToggleTrait(clicked);
        }
    }

    private bool DrawTraitCard(ChapterTraitDef trait, Rect rect, bool selectedSection)
    {
        var isSelected = !selectedSection && selected.Contains(trait);
        var unlocked = CustomChapterGeneUtility.TraitUnlocked(trait, out var lockedReason);
        var conflict = selectedSection || isSelected ? null : selected.FirstOrDefault(other => other.ConflictsWith(trait));

        Widgets.DrawOptionBackground(rect, isSelected);

        if (isSelected)
        {
            GUI.color = OutlineColorSelected;
            Widgets.DrawBox(rect);
            GUI.color = Color.white;
        }

        var stability = trait.stabilityOffset;
        var stabilityRect = new Rect(rect.x + 4f, rect.y, StabilityColumnWidth, rect.height);
        Text.Anchor = TextAnchor.MiddleCenter;
        GUI.color = StabilityColor(stability);
        Widgets.Label(stabilityRect, stability.ToStringWithSign());
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;

        var geneRect = new Rect(stabilityRect.xMax, rect.y + 4f, GeneCreationDialogBase.GeneSize.x, GeneCreationDialogBase.GeneSize.y);
        GeneUIUtility.DrawGeneDef(trait.AsGeneDef(), geneRect, GeneType.Xenogene, () => CardTooltip(trait, unlocked, lockedReason, conflict, stability), false, false);

        if (!unlocked || conflict != null)
        {
            Widgets.DrawBoxSolid(geneRect, LockedOverlayColor);
        }

        if (Mouse.IsOver(rect))
        {
            Widgets.DrawHighlight(rect);
        }

        return Widgets.ButtonInvisible(rect);
    }

    private string CardTooltip(ChapterTraitDef trait, bool unlocked, string lockedReason, ChapterTraitDef conflict, int stability)
    {
        var text = (CustomChapterGeneUtility.BudgetLabel(Tuning) + ": " + stability.ToStringWithSign()).Colorize(ColoredText.TipSectionTitleColor);
        var requirement = CustomChapterGeneUtility.RequirementDescription(trait);

        if (!requirement.NullOrEmpty())
        {
            text += "\n" + (unlocked ? requirement.Colorize(ColoredText.SubtleGrayColor) : lockedReason.Colorize(ColorLibrary.RedReadable));
        }

        if (readOnly)
        {
            return text;
        }

        if (conflict != null)
        {
            text += "\n\n" + "BEWH.MankindsFinest.CustomChapter.TraitConflict".Translate(trait.LabelCap, conflict.LabelCap).Colorize(ColorLibrary.RedReadable);
            return text;
        }

        text += "\n\n" + (selected.Contains(trait) ? "ClickToRemove" : "ClickToAdd").Translate().Colorize(ColoredText.SubtleGrayColor);
        return text;
    }

    private void ToggleTrait(ChapterTraitDef trait)
    {
        if (selected.Contains(trait))
        {
            SoundDefOf.Tick_Low.PlayOneShotOnCamera();
            selected.Remove(trait);
            return;
        }

        if (!CustomChapterGeneUtility.TraitUnlocked(trait, out var lockedReason))
        {
            Messages.Message(lockedReason, MessageTypeDefOf.RejectInput, false);
            return;
        }

        if (selected.Count >= CustomChapterGeneUtility.MaxTraits(Tuning))
        {
            Messages.Message("BEWH.MankindsFinest.CustomChapter.TooManyTraits".Translate(CustomChapterGeneUtility.MaxTraits(Tuning)), MessageTypeDefOf.RejectInput, false);
            return;
        }

        var conflict = selected.FirstOrDefault(other => other.ConflictsWith(trait));

        if (conflict != null)
        {
            Messages.Message("BEWH.MankindsFinest.CustomChapter.TraitConflict".Translate(trait.LabelCap, conflict.LabelCap), MessageTypeDefOf.RejectInput, false);
            return;
        }

        SoundDefOf.Tick_High.PlayOneShotOnCamera();
        selected.Add(trait);
    }

    private void DrawFooter(Rect rect)
    {
        Widgets.DrawMenuSection(rect);
        rect = rect.ContractedBy(4f);

        var stability = Stability;
        var range = Tuning.stabilityRange;
        var iconRect = new Rect(rect.x, rect.y, 22f, 22f);
        GUI.DrawTexture(iconRect, GeneUtility.METTex.Texture);

        var stabilityText = CustomChapterGeneUtility.BudgetLabel(Tuning) + ": " + stability.ToStringWithSign().Colorize(StabilityColor(stability));

        if (stability < range.min || stability > range.max)
        {
            stabilityText += " (" + "BEWH.MankindsFinest.CustomChapter.StabilityRange".Translate(range.min, range.max) + ")";
            stabilityText = stabilityText.Colorize(ColorLibrary.RedReadable);
        }

        var stabilityRect = new Rect(iconRect.xMax + 4f, rect.y, rect.width * 0.5f, Text.LineHeight);
        Widgets.Label(stabilityRect, stabilityText);
        TooltipHandler.TipRegion(new Rect(rect.x, rect.y, rect.width * 0.5f, Text.LineHeight), CustomChapterGeneUtility.BudgetDescription(Tuning));
        Widgets.Label(new Rect(rect.x, rect.y + Text.LineHeight, rect.width, Text.LineHeight), CustomChapterGeneUtility.BudgetEffectDescription(Tuning, stability));

        var nameY = rect.y + Text.LineHeight * 2f + 8f;
        var nameLabel = (IsPrimarch ? "BEWH.MankindsFinest.CustomPrimarch.DesignName" : "BEWH.MankindsFinest.CustomChapter.ChapterName").Translate() + ":";
        var nameLabelWidth = Text.CalcSize(nameLabel).x;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(new Rect(rect.x, nameY, nameLabelWidth, 30f), nameLabel);
        Text.Anchor = TextAnchor.UpperLeft;

        var fieldRect = new Rect(rect.x + nameLabelWidth + 8f, nameY + 3f, Mathf.Min(300f, rect.width * 0.4f), 24f);

        if (readOnly)
        {
            Widgets.Label(fieldRect, chapterName);
        }
        else
        {
            chapterName = Widgets.TextField(fieldRect, chapterName, MaxNameLength, ValidSymbolRegex);
        }

        var iconButtonRect = new Rect(fieldRect.xMax + 8f, nameY, 30f, 30f);
        Widgets.DrawHighlight(iconButtonRect);

        if (Widgets.ButtonImage(iconButtonRect, flagIconDef.Icon) && !readOnly)
        {
            Find.WindowStack.Add(new Dialog_SelectFlagIcon(flagIconDef, icon => flagIconDef = icon));
        }

        TooltipHandler.TipRegion(iconButtonRect, "BEWH.MankindsFinest.CustomChapter.SelectIconDesc".Translate());

        if (template.kind != CustomGeneKind.Chapter)
        {
            return;
        }

        var relatedLabel = "BEWH.MankindsFinest.CustomChapter.RelatedPrimarch".Translate() + ": " + (relatedPrimarchGene == null ? "NoneLower".Translate().ToString() : CustomChapterGeneUtility.ChapterLabelOf(relatedPrimarchGene).CapitalizeFirst());
        var relatedRect = new Rect(iconButtonRect.xMax + 12f, nameY, Mathf.Min(300f, rect.xMax - iconButtonRect.xMax - 12f), 30f);

        if (Widgets.ButtonText(relatedRect, relatedLabel.Truncate(relatedRect.width - 16f)) && !readOnly)
        {
            OpenRelatedPrimarchMenu();
        }

        TooltipHandler.TipRegion(relatedRect, "BEWH.MankindsFinest.CustomChapter.RelatedPrimarchDesc".Translate());
    }

    private void OpenRelatedPrimarchMenu()
    {
        var options = new List<FloatMenuOption>
        {
            new("NoneLower".Translate().CapitalizeFirst(), delegate { relatedPrimarchGene = null; })
        };

        foreach (var geneDef in CustomChapterGeneUtility.ShippedPrimarchGenes)
        {
            var candidate = geneDef;
            options.Add(new FloatMenuOption(candidate.LabelCap, delegate { relatedPrimarchGene = candidate; }));
        }

        foreach (var design in GameComp?.DesignsOf(CustomGeneKind.Primarch) ?? new List<CustomChapterGene>())
        {
            var candidate = design;
            options.Add(new FloatMenuOption("BEWH.MankindsFinest.CustomPrimarch.GestatorOption".Translate(candidate.name), delegate { relatedPrimarchGene = candidate.GeneDef; }));
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }

    private static Color StabilityColor(int stability)
    {
        if (stability < 0)
        {
            return ColorLibrary.RedReadable;
        }

        return stability > 0 ? PositiveStabilityColor : Color.white;
    }

    private void DoBottomButtons(Rect rect)
    {
        if (Widgets.ButtonText(new Rect(rect.x, rect.y, ButSize.x, ButSize.y), "Close".Translate()))
        {
            Close();
        }

        if (readOnly)
        {
            return;
        }

        if (Widgets.ButtonText(new Rect(rect.xMax - ButSize.x, rect.y, ButSize.x, ButSize.y), "BEWH.MankindsFinest.CustomChapter.Save".Translate()) && CanAccept())
        {
            Accept();
        }
    }

    private bool CanAccept()
    {
        if (chapterName.NullOrEmpty() || chapterName.Trim().Length == 0)
        {
            Messages.Message("XenotypeNameCannotBeEmpty".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        if (GameComp.NameTaken(chapterName.Trim(), editing))
        {
            Messages.Message("BEWH.MankindsFinest.CustomChapter.NameTaken".Translate(chapterName.Trim()), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        if (editing == null && GameComp.DesignsOf(template.kind, true).Count >= Tuning.maxCustomChapters)
        {
            Messages.Message("BEWH.MankindsFinest.CustomChapter.TooManyChapters".Translate(Tuning.maxCustomChapters), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        if (selected.Count == 0)
        {
            Messages.Message("BEWH.MankindsFinest.CustomChapter.NoTraits".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        if (selected.Count > CustomChapterGeneUtility.MaxTraits(Tuning))
        {
            Messages.Message("BEWH.MankindsFinest.CustomChapter.TooManyTraits".Translate(CustomChapterGeneUtility.MaxTraits(Tuning)), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        var stability = Stability;

        if (stability < Tuning.stabilityRange.min || stability > Tuning.stabilityRange.max)
        {
            Messages.Message((IsPrimarch ? "BEWH.MankindsFinest.CustomPrimarch.ComplexityOutOfRange" : "BEWH.MankindsFinest.CustomChapter.StabilityOutOfRange").Translate(stability.ToStringWithSign(), Tuning.stabilityRange.min, Tuning.stabilityRange.max), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        foreach (var trait in selected)
        {
            if (!CustomChapterGeneUtility.TraitUnlocked(trait, out var lockedReason))
            {
                Messages.Message(trait.LabelCap + ": " + lockedReason, MessageTypeDefOf.RejectInput, false);
                return false;
            }

            if (selected.Any(other => other != trait && other.ConflictsWith(trait)))
            {
                Messages.Message("BEWH.MankindsFinest.CustomChapter.TraitConflict".Translate(trait.LabelCap, selected.First(other => other != trait && other.ConflictsWith(trait)).LabelCap), MessageTypeDefOf.RejectInput, false);
                return false;
            }
        }

        return true;
    }

    private void Accept()
    {
        var trimmedName = chapterName.Trim();

        if (editing != null)
        {
            if (editing.locked)
            {
                Messages.Message("BEWH.MankindsFinest.CustomChapter.Locked".Translate(editing.name), MessageTypeDefOf.RejectInput, false);
                Close();
                return;
            }

            editing.name = trimmedName;
            editing.flagIconDef = flagIconDef;
            editing.traits = selected.ToList();
            editing.RelatedPrimarchGene = template.kind == CustomGeneKind.Chapter ? relatedPrimarchGene : null;
            editing.RefreshGeneDef();
        }
        else
        {
            GameComp.Add(template, trimmedName, flagIconDef, selected, null, template.kind == CustomGeneKind.Chapter ? relatedPrimarchGene : null);
        }

        Messages.Message((IsPrimarch ? "BEWH.MankindsFinest.CustomPrimarch.Saved" : "BEWH.MankindsFinest.CustomChapter.Saved").Translate(trimmedName), MessageTypeDefOf.TaskCompletion, false);
        Close();
    }
}
