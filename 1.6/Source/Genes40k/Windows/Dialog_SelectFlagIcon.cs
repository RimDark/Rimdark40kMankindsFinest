using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Genes40k;

/// <summary>
/// Grid picker over the chapter flag icons (FlagIconDef), used to give a custom chapter gene its icon.
/// </summary>
public class Dialog_SelectFlagIcon : Window
{
    private Vector2 scrollPosition;
    private float scrollHeight;

    private FlagIconDef selected;
    private readonly Action<FlagIconDef> iconSelector;

    private const float IconSize = 48f;
    private const float IconGap = 6f;

    private static readonly Color OutlineColorSelected = new(1f, 1f, 0.7f, 1f);
    private static readonly Color OutlineColorUnselected = new(1f, 1f, 1f, 0.1f);

    private static List<FlagIconDef> flagIcons;
    private static List<FlagIconDef> FlagIcons => flagIcons ??= DefDatabase<FlagIconDef>.AllDefsListForReading.Where(flagIcon => !flagIcon.setsNull).OrderBy(flagIcon => flagIcon.sortOrder).ToList();

    public override Vector2 InitialSize => new(8 * (IconSize + IconGap) + IconGap + Margin * 2f + 16f, 420f);

    public Dialog_SelectFlagIcon(FlagIconDef selected, Action<FlagIconDef> iconSelector)
    {
        this.selected = selected;
        this.iconSelector = iconSelector;
        closeOnClickedOutside = true;
    }

    public override void DoWindowContents(Rect inRect)
    {
        var rect = inRect;
        Text.Font = GameFont.Medium;
        Widgets.Label(rect, "BEWH.MankindsFinest.CustomChapter.SelectIcon".Translate());
        Text.Font = GameFont.Small;
        rect.yMin += 39f;
        rect.yMax -= CloseButSize.y + 4f;

        var outRect = rect;
        outRect.yMax -= 4f;
        var viewRect = new Rect(outRect.x, outRect.y, outRect.width - 16f, scrollHeight);
        Widgets.DrawLightHighlight(viewRect);
        Widgets.BeginScrollView(outRect, ref scrollPosition, viewRect);

        var curX = outRect.x + IconGap;
        var curY = outRect.y + IconGap;

        foreach (var flagIcon in FlagIcons)
        {
            if (curX + IconSize + IconGap > viewRect.width)
            {
                curX = outRect.x + IconGap;
                curY += IconSize + IconGap;
            }

            var iconRect = new Rect(curX, curY, IconSize, IconSize);
            Widgets.DrawHighlight(iconRect);

            if (selected == flagIcon)
            {
                GUI.color = OutlineColorSelected;
                Widgets.DrawHighlight(iconRect);
                Widgets.DrawBox(iconRect.ExpandedBy(2f), 2);
            }
            else
            {
                GUI.color = OutlineColorUnselected;
                Widgets.DrawBox(iconRect);
            }

            GUI.color = Color.white;

            if (Widgets.ButtonImage(iconRect, flagIcon.Icon))
            {
                selected = flagIcon;
            }

            TooltipHandler.TipRegion(iconRect, flagIcon.LabelCap);
            curX += IconSize + IconGap;
        }

        if (Event.current.type == EventType.Layout)
        {
            scrollHeight = curY + IconSize + IconGap - outRect.y;
        }

        Widgets.EndScrollView();

        if (Widgets.ButtonText(new Rect((inRect.width - CloseButSize.x) / 2f, rect.yMax, CloseButSize.x, CloseButSize.y), "Accept".Translate()))
        {
            Close();
        }
    }

    public override void PreClose()
    {
        base.PreClose();
        iconSelector(selected);
    }
}
