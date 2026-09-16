using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace Genes40k;

/// <summary>
/// Lists the colony's designed gene-seeds, one tab per line (chapter, primarch, Custodes), with their budget and contents;
/// opens the matching editor for new, unlocked or locked (view only) designs.
/// </summary>
public class Dialog_CustomChapterGeneList : Window
{
    private Vector2 scrollPosition;
    private float scrollHeight;
    private CustomChapterGeneTemplateDef currentTemplate;
    private bool showAdHoc;

    private const float RowHeight = 58f;
    private const float ButtonWidth = 90f;
    private static readonly Vector2 ButSize = new(220f, 38f);

    private static GameComponent_CustomChapterGenes GameComp => GameComponent_CustomChapterGenes.Instance;

    public override Vector2 InitialSize => new(760f, 600f);

    public Dialog_CustomChapterGeneList(CustomChapterGeneTemplateDef template = null)
    {
        forcePause = true;
        absorbInputAroundWindow = true;
        doCloseX = true;
        closeOnClickedOutside = true;
        currentTemplate = template ?? CustomChapterGeneUtility.TemplatesInOrder.FirstOrDefault() ?? CustomChapterGeneUtility.Tuning;
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 35f), "BEWH.MankindsFinest.CustomChapter.ListHeader".Translate());
        Text.Font = GameFont.Small;

        var listTop = inRect.y + 40f + TabDrawer.TabHeight;
        var listRect = new Rect(inRect.x, listTop, inRect.width, inRect.yMax - listTop - ButSize.y - 8f);

        var tabs = CustomChapterGeneUtility.TemplatesInOrder.Select(template => new TabRecord(CustomChapterGeneUtility.KindLabel(template), delegate
        {
            currentTemplate = template;
            scrollPosition = Vector2.zero;
        }, currentTemplate == template)).ToList();

        Widgets.DrawMenuSection(listRect);

        if (tabs.Any())
        {
            TabDrawer.DrawTabs(listRect, tabs);
        }

        var outRect = listRect.ContractedBy(4f);

        if (currentTemplate.kind == CustomGeneKind.Custodes)
        {
            var toggleRect = new Rect(outRect.x, outRect.y, outRect.width, Text.LineHeight + 4f);
            Widgets.CheckboxLabeled(toggleRect, "BEWH.MankindsFinest.CustomCustodes.ShowUnnamed".Translate(), ref showAdHoc);
            outRect.yMin += toggleRect.height + 4f;
        }

        var viewRect = new Rect(0f, 0f, outRect.width - 16f, scrollHeight);

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        var curY = 0f;
        var chapters = GameComp?.DesignsOf(currentTemplate.kind, showAdHoc);

        if (chapters.NullOrEmpty())
        {
            Text.Anchor = TextAnchor.MiddleCenter;
            GUI.color = ColoredText.SubtleGrayColor;
            Widgets.Label(new Rect(0f, 0f, viewRect.width, RowHeight), "BEWH.MankindsFinest.CustomChapter.NoChapters".Translate());
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperLeft;
            curY += RowHeight;
        }
        else
        {
            CustomChapterGene toDelete = null;

            for (var i = 0; i < chapters.Count; i++)
            {
                if (DrawRow(chapters[i], new Rect(0f, curY, viewRect.width, RowHeight), i % 2 == 1))
                {
                    toDelete = chapters[i];
                }

                curY += RowHeight;
            }

            if (toDelete != null)
            {
                var chapter = toDelete;
                Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation("BEWH.MankindsFinest.CustomChapter.ConfirmDelete".Translate(chapter.name), delegate
                {
                    GameComp.Remove(chapter);
                }, true));
            }
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

        if (Widgets.ButtonText(new Rect(buttonsRect.xMax - ButSize.x, buttonsRect.y, ButSize.x, ButSize.y), NewButtonLabel(currentTemplate)))
        {
            OpenEditor(currentTemplate, null, false);
        }
    }

    private static string NewButtonLabel(CustomChapterGeneTemplateDef template)
    {
        return template.kind switch
        {
            CustomGeneKind.Primarch => "BEWH.MankindsFinest.CustomPrimarch.NewDesign".Translate(),
            CustomGeneKind.Custodes => "BEWH.MankindsFinest.CustomCustodes.NewPreset".Translate(),
            _ => "BEWH.MankindsFinest.CustomChapter.NewChapter".Translate()
        };
    }

    public static void OpenEditor(CustomChapterGeneTemplateDef template, CustomChapterGene chapter, bool readOnly)
    {
        if (template.kind == CustomGeneKind.Custodes)
        {
            Find.WindowStack.Add(new Dialog_CustodesCustomization(chapter, readOnly));
        }
        else
        {
            Find.WindowStack.Add(new Dialog_CreateChapterGene(template, chapter, readOnly));
        }
    }

    private static string ContentsLine(CustomChapterGene chapter)
    {
        if (chapter.Kind == CustomGeneKind.Custodes)
        {
            return chapter.disciplines.Where(entry => entry?.def != null && entry.level > 0).Select(entry => entry.def.label.CapitalizeFirst() + " " + entry.level).ToCommaList();
        }

        return chapter.traits.Select(trait => trait.label).ToCommaList().CapitalizeFirst();
    }

    private static string ContentsTooltip(CustomChapterGene chapter)
    {
        if (chapter.Kind == CustomGeneKind.Custodes)
        {
            return chapter.disciplines.Where(entry => entry?.def != null && entry.level > 0).Select(entry => entry.def.label.CapitalizeFirst() + " " + entry.level).ToLineList("  - ");
        }

        var lines = chapter.traits.Select(trait => trait.LabelCap.ToString()).ToLineList("  - ");
        var relatedPrimarch = chapter.RelatedPrimarchGene;

        if (chapter.Kind == CustomGeneKind.Chapter && relatedPrimarch != null)
        {
            lines += "\n" + "BEWH.MankindsFinest.CustomChapter.RelatedPrimarch".Translate() + ": " + CustomChapterGeneUtility.ChapterLabelOf(relatedPrimarch).CapitalizeFirst();
        }

        return lines;
    }

    private static bool DrawRow(CustomChapterGene chapter, Rect rect, bool alternate)
    {
        if (alternate)
        {
            Widgets.DrawLightHighlight(rect);
        }

        Widgets.DrawHighlightIfMouseover(rect);
        var requestDelete = false;

        var iconRect = new Rect(rect.x + 4f, rect.y + (rect.height - 40f) / 2f, 40f, 40f);

        if (chapter.flagIconDef != null)
        {
            GUI.DrawTexture(iconRect, chapter.flagIconDef.Icon);
        }

        var textRect = new Rect(iconRect.xMax + 8f, rect.y + 4f, rect.width - iconRect.width - 16f - ButtonWidth * 2f - 12f, rect.height - 8f);
        var budget = chapter.Budget;
        string title = chapter.name;

        if (chapter.locked)
        {
            title += " " + ("(" + "BEWH.MankindsFinest.CustomChapter.LockedShort".Translate() + ")").Colorize(ColoredText.SubtleGrayColor);
        }

        Widgets.Label(new Rect(textRect.x, textRect.y, textRect.width, Text.LineHeight), title);
        var budgetText = chapter.Kind == CustomGeneKind.Custodes
            ? budget + " / " + CustomChapterGeneUtility.DisciplinePoints(chapter.Template)
            : budget.ToStringWithSign();
        string details = CustomChapterGeneUtility.BudgetLabel(chapter.Template) + ": " + budgetText + "  |  " + ContentsLine(chapter);
        GUI.color = ColoredText.SubtleGrayColor;
        Widgets.Label(new Rect(textRect.x, textRect.y + Text.LineHeight, textRect.width, Text.LineHeight), details.Truncate(textRect.width));
        GUI.color = Color.white;

        var tooltip = ContentsTooltip(chapter) + "\n\n" + CustomChapterGeneUtility.BudgetEffectDescription(chapter.Template, budget);
        TooltipHandler.TipRegion(textRect, tooltip);

        var editRect = new Rect(rect.xMax - ButtonWidth * 2f - 8f, rect.y + (rect.height - 30f) / 2f, ButtonWidth, 30f);
        var editLabel = chapter.locked ? "BEWH.MankindsFinest.CustomChapter.View".Translate() : "BEWH.MankindsFinest.CustomChapter.Edit".Translate();

        if (Widgets.ButtonText(editRect, editLabel))
        {
            OpenEditor(chapter.Template, chapter, chapter.locked && chapter.Kind != CustomGeneKind.Custodes);
        }

        var deleteRect = new Rect(rect.xMax - ButtonWidth - 4f, editRect.y, ButtonWidth, 30f);

        if (chapter.locked)
        {
            GUI.color = ColoredText.SubtleGrayColor;
            Widgets.ButtonText(deleteRect, "BEWH.MankindsFinest.CustomChapter.Delete".Translate(), true, false, false);
            GUI.color = Color.white;
            TooltipHandler.TipRegion(deleteRect, "BEWH.MankindsFinest.CustomChapter.Locked".Translate(chapter.name));
        }
        else if (Widgets.ButtonText(deleteRect, "BEWH.MankindsFinest.CustomChapter.Delete".Translate()))
        {
            requestDelete = true;
        }

        return requestDelete;
    }
}
