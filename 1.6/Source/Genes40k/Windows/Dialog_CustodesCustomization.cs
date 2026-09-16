using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace Genes40k;

/// <summary>
/// Slider editor for a Custodes ascension pattern: spend the discipline point pool across the CustodesDisciplineDefs.
/// Opened from the gene gestator to configure the pattern for the contained matrix (with load/save-as-preset), or from
/// the design list to create, edit or view a named preset.
/// </summary>
[StaticConstructorOnStartup]
public class Dialog_CustodesCustomization : Window
{
    private readonly Building_GeneGestator gestator;
    private readonly CustomChapterGene editing;
    private readonly bool readOnly;
    private readonly CustomChapterGeneTemplateDef template;

    private readonly Dictionary<CustodesDisciplineDef, int> levels = new();
    private string presetName = string.Empty;
    private FlagIconDef flagIconDef;

    private Vector2 scrollPosition;
    private float scrollHeight;

    private static readonly Regex ValidSymbolRegex = new("^[\\p{L}0-9 '\\-]*$");
    private static readonly Vector2 ButSize = new(150f, 38f);
    private const float HeaderHeight = 35f;
    private const float RowHeight = 48f;
    private const float IconSize = 36f;
    private const float LabelWidth = 150f;
    private const float LevelWidth = 52f;
    private const float CostWidth = 60f;
    private const float StepButtonSize = 24f;
    private const int MaxNameLength = 40;

    private static readonly Color SpentColor = new(0.45f, 0.9f, 0.45f, 1f);

    private static GameComponent_CustomChapterGenes GameComp => GameComponent_CustomChapterGenes.Instance;

    public override Vector2 InitialSize => new(Mathf.Min(UI.screenWidth, 820f), Mathf.Min(UI.screenHeight - 4f, 700f));

    private bool GestatorMode => gestator != null;

    private Dialog_CustodesCustomization()
    {
        template = CustomChapterGeneUtility.TemplateFor(CustomGeneKind.Custodes);
        flagIconDef = Genes40kDefOf.BEWH_FlagAquila;
        forcePause = true;
        absorbInputAroundWindow = true;
        doCloseX = true;
        closeOnAccept = false;

        foreach (var discipline in CustomChapterGeneUtility.AllDisciplines)
        {
            levels[discipline] = 0;
        }
    }

    public Dialog_CustodesCustomization(Building_GeneGestator gestator) : this()
    {
        this.gestator = gestator;
        Load(gestator.selectedDisciplines);
    }

    public Dialog_CustodesCustomization(CustomChapterGene preset, bool readOnly = false) : this()
    {
        editing = preset;
        this.readOnly = readOnly;

        if (preset == null)
        {
            return;
        }

        presetName = preset.name;
        flagIconDef = preset.flagIconDef ?? Genes40kDefOf.BEWH_FlagAquila;
        Load(preset.disciplines);
    }

    private void Load(List<CustodesDisciplineLevel> source)
    {
        foreach (var key in levels.Keys.ToList())
        {
            levels[key] = 0;
        }

        foreach (var entry in source.Where(entry => entry?.def != null))
        {
            levels[entry.def] = Mathf.Clamp(entry.level, 0, entry.def.maxLevel);
        }
    }

    private List<CustodesDisciplineLevel> CurrentLevels => levels.Where(pair => pair.Value > 0).Select(pair => new CustodesDisciplineLevel(pair.Key, pair.Value)).ToList();

    private int TotalPoints => CustomChapterGeneUtility.DisciplinePoints(template);

    private int SpentPoints => CustomChapterGeneUtility.PointsSpent(CurrentLevels, template);

    private bool LevelsLocked => readOnly || (editing is { locked: true });

    private string Header
    {
        get
        {
            if (GestatorMode)
            {
                return "BEWH.MankindsFinest.CustomCustodes.ConfigureHeader".Translate();
            }

            if (readOnly)
            {
                return "BEWH.MankindsFinest.CustomCustodes.ViewHeader".Translate(presetName);
            }

            return editing == null ? "BEWH.MankindsFinest.CustomCustodes.CreateHeader".Translate() : "BEWH.MankindsFinest.CustomCustodes.EditHeader".Translate();
        }
    }

    public override void DoWindowContents(Rect inRect)
    {
        var rect = inRect;
        rect.yMax -= ButSize.y + 4f;

        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, HeaderHeight), Header);
        Text.Font = GameFont.Small;
        rect.yMin += HeaderHeight + 4f;

        GUI.color = ColoredText.SubtleGrayColor;
        Widgets.Label(new Rect(rect.x, rect.y, rect.width, Text.LineHeight), "BEWH.MankindsFinest.CustomCustodes.ConfigureDesc".Translate(template.softCapLevel, template.softCapCostPerLevel));
        GUI.color = Color.white;
        rect.yMin += Text.LineHeight + 6f;

        var footerHeight = Text.LineHeight + 8f + 30f + 8f;
        var footerRect = new Rect(rect.x, rect.yMax - footerHeight, rect.width, footerHeight);
        rect.yMax -= footerHeight + 8f;

        DrawDisciplines(rect);
        DrawFooter(footerRect);

        var buttonsRect = inRect;
        buttonsRect.yMin = buttonsRect.yMax - ButSize.y;
        DoBottomButtons(buttonsRect);
    }

    private void DrawDisciplines(Rect rect)
    {
        Widgets.DrawMenuSection(rect);
        var outRect = rect.ContractedBy(4f);
        var viewRect = new Rect(0f, 0f, outRect.width - 16f, scrollHeight);

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        var curY = 0f;
        var alternate = false;

        foreach (var discipline in CustomChapterGeneUtility.AllDisciplines)
        {
            DrawRow(discipline, new Rect(0f, curY, viewRect.width, RowHeight), alternate);
            curY += RowHeight;
            alternate = !alternate;
        }

        if (Event.current.type == EventType.Layout)
        {
            scrollHeight = curY;
        }

        Widgets.EndScrollView();
    }

    private void DrawRow(CustodesDisciplineDef discipline, Rect rect, bool alternate)
    {
        if (alternate)
        {
            Widgets.DrawLightHighlight(rect);
        }

        Widgets.DrawHighlightIfMouseover(rect);

        var level = levels[discipline];
        var iconRect = new Rect(rect.x + 4f, rect.y + (rect.height - IconSize) / 2f, IconSize, IconSize);
        GUI.DrawTexture(iconRect, discipline.Icon);

        var labelRect = new Rect(iconRect.xMax + 8f, rect.y, LabelWidth, rect.height);
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(labelRect, discipline.LabelCap);
        Text.Anchor = TextAnchor.UpperLeft;

        var costRect = new Rect(rect.xMax - CostWidth - 4f, rect.y, CostWidth, rect.height);
        var levelRect = new Rect(costRect.x - LevelWidth - 4f, rect.y, LevelWidth, rect.height);
        var plusRect = new Rect(levelRect.x - StepButtonSize - 4f, rect.y + (rect.height - StepButtonSize) / 2f, StepButtonSize, StepButtonSize);
        var minusRect = new Rect(plusRect.x - StepButtonSize - 2f, plusRect.y, StepButtonSize, StepButtonSize);
        var sliderRect = new Rect(labelRect.xMax + 8f, rect.y + (rect.height - 24f) / 2f, minusRect.x - labelRect.xMax - 16f, 24f);

        var newLevel = level;

        if (LevelsLocked)
        {
            Widgets.HorizontalSlider(sliderRect, level, 0f, discipline.maxLevel, false, null, null, null, 1f);
        }
        else
        {
            newLevel = Mathf.RoundToInt(Widgets.HorizontalSlider(sliderRect, level, 0f, discipline.maxLevel, false, null, null, null, 1f));

            if (Widgets.ButtonText(minusRect, "-"))
            {
                newLevel = level - 1;
            }

            if (Widgets.ButtonText(plusRect, "+"))
            {
                newLevel = level + 1;
            }
        }

        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(levelRect, level + " / " + discipline.maxLevel);
        var cost = CustomChapterGeneUtility.PointCost(discipline, level, template);
        GUI.color = cost > 0 ? SpentColor : ColoredText.SubtleGrayColor;
        Widgets.Label(costRect, "BEWH.MankindsFinest.CustomCustodes.CostShort".Translate(cost));
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;

        TooltipHandler.TipRegion(rect, () => RowTooltip(discipline, level), discipline.GetHashCode());

        if (newLevel == level)
        {
            return;
        }

        SetLevel(discipline, newLevel);
    }

    private void SetLevel(CustodesDisciplineDef discipline, int wanted)
    {
        wanted = Mathf.Clamp(wanted, 0, discipline.maxLevel);
        var current = levels[discipline];

        if (wanted == current)
        {
            return;
        }

        var affordable = MaxAffordable(discipline);

        if (wanted > affordable)
        {
            wanted = affordable;

            if (wanted == current)
            {
                Messages.Message("BEWH.MankindsFinest.CustomCustodes.NoPointsLeft".Translate(), MessageTypeDefOf.RejectInput, false);
                return;
            }
        }

        (wanted > current ? SoundDefOf.Tick_High : SoundDefOf.Tick_Low).PlayOneShotOnCamera();
        levels[discipline] = wanted;
    }

    private int MaxAffordable(CustodesDisciplineDef discipline)
    {
        var current = levels[discipline];
        var spentElsewhere = SpentPoints - CustomChapterGeneUtility.PointCost(discipline, current, template);
        var best = 0;

        for (var level = 0; level <= discipline.maxLevel; level++)
        {
            if (spentElsewhere + CustomChapterGeneUtility.PointCost(discipline, level, template) <= TotalPoints)
            {
                best = level;
            }
        }

        return best;
    }

    private string RowTooltip(CustodesDisciplineDef discipline, int level)
    {
        var text = discipline.LabelCap.Colorize(ColoredText.TipSectionTitleColor) + "\n" + discipline.description;

        if (level > 0)
        {
            var effects = discipline.ToTraitDef(level).AsGeneDef().DescriptionFull;
            var index = effects.IndexOf(discipline.description, System.StringComparison.Ordinal);

            if (index >= 0)
            {
                effects = effects.Remove(index, discipline.description.Length).Trim();
            }

            if (!effects.NullOrEmpty())
            {
                text += "\n\n" + effects;
            }
        }

        var nextTier = discipline.NextTierAfter(level);

        if (nextTier != null && !nextTier.customEffectDescriptions.NullOrEmpty())
        {
            text += "\n\n" + "BEWH.MankindsFinest.CustomCustodes.NextTier".Translate(nextTier.minLevel).Colorize(ColoredText.SubtleGrayColor) + "\n" + nextTier.customEffectDescriptions.ToLineList("  - ").Colorize(ColoredText.SubtleGrayColor);
        }

        if (level < discipline.maxLevel)
        {
            var nextCost = CustomChapterGeneUtility.PointCost(discipline, level + 1, template) - CustomChapterGeneUtility.PointCost(discipline, level, template);
            text += "\n\n" + "BEWH.MankindsFinest.CustomCustodes.NextLevelCost".Translate(nextCost).Colorize(ColoredText.SubtleGrayColor);
        }

        return text;
    }

    private void DrawFooter(Rect rect)
    {
        Widgets.DrawMenuSection(rect);
        rect = rect.ContractedBy(4f);

        var spent = SpentPoints;
        var total = TotalPoints;
        var iconRect = new Rect(rect.x, rect.y, 22f, 22f);
        GUI.DrawTexture(iconRect, GeneUtility.METTex.Texture);

        var pointsText = "BEWH.MankindsFinest.CustomCustodes.PointsSpent".Translate(spent, total);

        if (spent > total)
        {
            pointsText = pointsText.Colorize(ColorLibrary.RedReadable);
        }

        Widgets.Label(new Rect(iconRect.xMax + 4f, rect.y, rect.width * 0.5f, Text.LineHeight), pointsText);
        TooltipHandler.TipRegion(new Rect(rect.x, rect.y, rect.width * 0.5f, Text.LineHeight), CustomChapterGeneUtility.BudgetDescription(template));

        var nameY = rect.y + Text.LineHeight + 8f;
        var nameLabel = "BEWH.MankindsFinest.CustomCustodes.PresetName".Translate() + ":";
        var nameLabelWidth = Text.CalcSize(nameLabel).x;
        Text.Anchor = TextAnchor.MiddleLeft;
        Widgets.Label(new Rect(rect.x, nameY, nameLabelWidth, 30f), nameLabel);
        Text.Anchor = TextAnchor.UpperLeft;

        var fieldRect = new Rect(rect.x + nameLabelWidth + 8f, nameY + 3f, Mathf.Min(260f, rect.width * 0.35f), 24f);

        if (readOnly)
        {
            Widgets.Label(fieldRect, presetName);
        }
        else
        {
            presetName = Widgets.TextField(fieldRect, presetName, MaxNameLength, ValidSymbolRegex);
        }

        var iconButtonRect = new Rect(fieldRect.xMax + 8f, nameY, 30f, 30f);
        Widgets.DrawHighlight(iconButtonRect);

        if (Widgets.ButtonImage(iconButtonRect, flagIconDef.Icon) && !readOnly)
        {
            Find.WindowStack.Add(new Dialog_SelectFlagIcon(flagIconDef, icon => flagIconDef = icon));
        }

        TooltipHandler.TipRegion(iconButtonRect, "BEWH.MankindsFinest.CustomChapter.SelectIconDesc".Translate());

        if (!GestatorMode)
        {
            return;
        }

        var saveRect = new Rect(rect.xMax - 170f, nameY, 170f, 30f);

        if (Widgets.ButtonText(saveRect, "BEWH.MankindsFinest.CustomCustodes.SaveAsPreset".Translate()))
        {
            SaveAsPreset();
        }

        var loadRect = new Rect(saveRect.x - 170f - 8f, nameY, 170f, 30f);

        if (Widgets.ButtonText(loadRect, "BEWH.MankindsFinest.CustomCustodes.LoadPreset".Translate()))
        {
            OpenLoadMenu();
        }
    }

    private void OpenLoadMenu()
    {
        var presets = GameComp?.DesignsOf(CustomGeneKind.Custodes) ?? new List<CustomChapterGene>();
        var options = new List<FloatMenuOption>();

        foreach (var preset in presets)
        {
            options.Add(new FloatMenuOption(preset.name, delegate
            {
                Load(preset.disciplines);
                presetName = preset.name;
                flagIconDef = preset.flagIconDef ?? Genes40kDefOf.BEWH_FlagAquila;
            }));
        }

        if (!options.Any())
        {
            options.Add(new FloatMenuOption("BEWH.MankindsFinest.CustomCustodes.NoPresets".Translate(), null));
        }

        Find.WindowStack.Add(new FloatMenu(options));
    }

    private bool ValidateLevels()
    {
        if (!CurrentLevels.Any())
        {
            Messages.Message("BEWH.MankindsFinest.CustomCustodes.NoLevels".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        if (SpentPoints > TotalPoints)
        {
            Messages.Message("BEWH.MankindsFinest.CustomCustodes.TooManyPoints".Translate(SpentPoints, TotalPoints), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        return true;
    }

    private bool ValidateName(CustomChapterGene except)
    {
        var trimmed = presetName?.Trim();

        if (trimmed.NullOrEmpty())
        {
            Messages.Message("XenotypeNameCannotBeEmpty".Translate(), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        if (GameComp.NameTaken(trimmed, except))
        {
            Messages.Message("BEWH.MankindsFinest.CustomChapter.NameTaken".Translate(trimmed), MessageTypeDefOf.RejectInput, false);
            return false;
        }

        return true;
    }

    private void SaveAsPreset()
    {
        if (GameComp == null || !ValidateLevels())
        {
            return;
        }

        var design = GameComp.GetOrCreateCustodesDesign(CurrentLevels);

        if (!ValidateName(design))
        {
            return;
        }

        design.name = presetName.Trim();
        design.flagIconDef = flagIconDef;
        design.adHoc = false;
        design.RefreshGeneDef();
        Messages.Message("BEWH.MankindsFinest.CustomCustodes.PresetSaved".Translate(design.name), MessageTypeDefOf.TaskCompletion, false);
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

        var acceptLabel = GestatorMode ? "Accept".Translate() : "BEWH.MankindsFinest.CustomChapter.Save".Translate();

        if (!Widgets.ButtonText(new Rect(rect.xMax - ButSize.x, rect.y, ButSize.x, ButSize.y), acceptLabel))
        {
            return;
        }

        if (GestatorMode)
        {
            AcceptForGestator();
        }
        else
        {
            SavePreset();
        }
    }

    private void AcceptForGestator()
    {
        if (!ValidateLevels())
        {
            return;
        }

        gestator.selectedDisciplines = CurrentLevels;
        gestator.selectedMaterial = null;
        gestator.selectedCustomChapterId = -1;
        Close();
    }

    private void SavePreset()
    {
        if (GameComp == null || !ValidateName(editing))
        {
            return;
        }

        if (editing != null)
        {
            editing.name = presetName.Trim();
            editing.flagIconDef = flagIconDef;

            if (!editing.locked)
            {
                if (!ValidateLevels())
                {
                    return;
                }

                editing.disciplines = CurrentLevels;
            }

            editing.adHoc = false;
            editing.RefreshGeneDef();
        }
        else
        {
            if (!ValidateLevels())
            {
                return;
            }

            if (GameComp.DesignsOf(CustomGeneKind.Custodes, true).Count >= template.maxCustomChapters)
            {
                Messages.Message("BEWH.MankindsFinest.CustomChapter.TooManyChapters".Translate(template.maxCustomChapters), MessageTypeDefOf.RejectInput, false);
                return;
            }

            GameComp.Add(template, presetName.Trim(), flagIconDef, null, CurrentLevels);
        }

        Messages.Message("BEWH.MankindsFinest.CustomCustodes.PresetSaved".Translate(presetName.Trim()), MessageTypeDefOf.TaskCompletion, false);
        Close();
    }
}
