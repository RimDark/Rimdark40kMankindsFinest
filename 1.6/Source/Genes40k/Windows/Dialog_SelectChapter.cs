using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace Genes40k;

/// <summary>
/// Picker opened by the chapter application surgery: every legion chapter and every player-made design listed once,
/// with locked entries greyed out. Accepting creates a Bill_ApplyChapter pinned to the chosen chapter.
/// </summary>
public class Dialog_SelectChapter : Window
{
    private readonly Pawn pawn;
    private readonly RecipeDef recipe;
    private readonly BodyPartRecord part;

    private List<ChapterChoice> choices;
    private ChapterChoice selected;

    private Vector2 scrollPosition;
    private float scrollHeight;

    private const float RowHeight = 44f;
    private const float IconSize = 32f;
    private static readonly Vector2 ButSize = new(150f, 38f);

    private static readonly Color LockedTextColor = new(0.5f, 0.5f, 0.5f, 1f);
    private static readonly Color LockedOverlayColor = new(0f, 0f, 0f, 0.5f);

    public override Vector2 InitialSize => new(560f, 640f);

    public Dialog_SelectChapter(Pawn pawn, RecipeDef recipe, BodyPartRecord part)
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
        choices = ChapterChoiceUtility.Choices();
    }

    public override void DoWindowContents(Rect inRect)
    {
        Text.Font = GameFont.Medium;
        Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 35f), "BEWH.MankindsFinest.ApplyChapter.SelectChapter".Translate(pawn.LabelShortCap));
        Text.Font = GameFont.Small;

        GUI.color = ColoredText.SubtleGrayColor;
        Widgets.Label(new Rect(inRect.x, inRect.y + 36f, inRect.width, Text.LineHeight), "BEWH.MankindsFinest.ApplyChapter.SelectChapterDesc".Translate());
        GUI.color = Color.white;

        var listTop = inRect.y + 36f + Text.LineHeight + 6f;
        var listRect = new Rect(inRect.x, listTop, inRect.width, inRect.yMax - listTop - ButSize.y - 8f);
        Widgets.DrawMenuSection(listRect);

        var outRect = listRect.ContractedBy(4f);
        var viewRect = new Rect(0f, 0f, outRect.width - 16f, scrollHeight);

        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);
        var curY = 0f;

        foreach (var choice in choices)
        {
            DrawRow(choice, new Rect(0f, curY, viewRect.width, RowHeight));
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
            ChapterChoiceUtility.CreateBill(pawn, recipe, part, selected);
            Close();
        }
    }

    private void DrawRow(ChapterChoice choice, Rect rect)
    {
        if (selected == choice)
        {
            Widgets.DrawHighlightSelected(rect);
        }
        else if (Mouse.IsOver(rect))
        {
            Widgets.DrawHighlight(rect);
        }

        var iconRect = new Rect(rect.x + 4f, rect.y + (rect.height - IconSize) / 2f, IconSize, IconSize);

        if (choice.chapterGene != null)
        {
            GUI.DrawTexture(iconRect, choice.chapterGene.Icon);
        }

        if (!choice.Available)
        {
            Widgets.DrawBoxSolid(iconRect, LockedOverlayColor);
        }

        var labelRect = new Rect(iconRect.xMax + 8f, rect.y, rect.width - iconRect.xMax - 12f, rect.height);
        Text.Anchor = TextAnchor.MiddleLeft;
        GUI.color = choice.Available ? Color.white : LockedTextColor;
        Widgets.Label(labelRect, choice.Label.Truncate(labelRect.width));
        GUI.color = Color.white;
        Text.Anchor = TextAnchor.UpperLeft;

        TooltipHandler.TipRegion(rect, () => RowTooltip(choice), choice.GetHashCode());

        if (Widgets.ButtonInvisible(rect) && choice.Available)
        {
            selected = choice;
        }
    }

    private string RowTooltip(ChapterChoice choice)
    {
        var text = choice.chapterGene?.description ?? string.Empty;

        if (!choice.Available && choice.chapterMaterial != null)
        {
            var reason = "BEWH.MankindsFinest.CustomChapter.RequiresMaterial".Translate(ChapterChoiceUtility.MaterialLabel(choice.chapterMaterial));
            text += (text.NullOrEmpty() ? string.Empty : "\n\n") + ("BEWH.MankindsFinest.ApplyChapter.LockedChapter".Translate() + "\n  - " + reason).Colorize(ColorLibrary.RedReadable);
        }
        else if (choice.Available)
        {
            text += (text.NullOrEmpty() ? string.Empty : "\n\n") + "BEWH.MankindsFinest.ImplantGeneseed.ClickToSelect".Translate().Colorize(ColoredText.SubtleGrayColor);
        }

        return text;
    }
}
