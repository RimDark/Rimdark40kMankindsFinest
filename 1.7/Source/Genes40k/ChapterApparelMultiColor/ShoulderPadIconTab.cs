using System.Collections.Generic;
using ColourPicker;
using Core40k;
using UnityEngine;
using Verse;

namespace Genes40k;

public class ShoulderPadIconTab : CustomizerTabDrawer
{
    private List<ShoulderIconDef> rightShoulderIcons = [];
    private List<ShoulderIconDef> leftShoulderIcons = [];

    private CompChapterColorWithShoulderDecoration chapterColorComp = null;
    private CompDecorative decorativeComp = null;

    private const int RowAmount = 6;

    private float listScrollViewHeight = 0f;

    public override IEnumerable<CompGraphicParent> Comps
    {
        get
        {
            if (chapterColorComp != null)
            {
                yield return chapterColorComp;
            }
            if (decorativeComp != null)
            {
                yield return decorativeComp;
            }
        }
    }

    public override void Setup(Pawn pawn)
    {
        chapterColorComp = null;
        decorativeComp = null;
        leftShoulderIcons.Clear();
        rightShoulderIcons.Clear();

        if (pawn.apparel != null)
        {
            foreach (var apparel in pawn.apparel.WornApparel)
            {
                var comp = apparel.GetComp<CompChapterColorWithShoulderDecoration>();
                if (comp == null)
                {
                    continue;
                }

                chapterColorComp = comp;
                decorativeComp = comp.DecorativeComp;
                break;
            }
        }

        if (chapterColorComp == null)
        {
            return;
        }

        //Catch up on any rank change first so opening the dialog never counts as an edit.
        chapterColorComp.SyncRankIcon(pawn);
        chapterColorComp.BeginEditing();
        decorativeComp?.SetOriginals();
        chapterColorComp.SetOriginals();

        var allShoulderIcons = DefDatabase<ShoulderIconDef>.AllDefsListForReading;
        foreach (var shoulderIcon in allShoulderIcons)
        {
            if (!shoulderIcon.MeetsRequirements(pawn))
            {
                continue;
            }

            if (shoulderIcon.leftShoulder)
            {
                leftShoulderIcons.Add(shoulderIcon);
            }
            if (shoulderIcon.rightShoulder)
            {
                rightShoulderIcons.Add(shoulderIcon);
            }
        }

        rightShoulderIcons.SortBy(def => def.sortOrder);
        leftShoulderIcons.SortBy(def => def.sortOrder);
    }

    private bool IsSelected(ShoulderIconDef def, bool chapterSlot)
    {
        var fitted = chapterSlot ? chapterColorComp.ChapterIcon : chapterColorComp.RankIcon;
        if (!def.setsNull)
        {
            return fitted == def;
        }

        if (def == Genes40kDefOf.BEWH_FollowRankUp)
        {
            return !chapterSlot && chapterColorComp.FollowRank;
        }

        return fitted == null && (chapterSlot || !chapterColorComp.FollowRank);
    }

    private void DrawColourBox(ref float curY, Vector2 position, float width, bool chapterSlot)
    {
        var fitted = chapterSlot ? chapterColorComp.ChapterIcon : chapterColorComp.RankIcon;
        if (fitted is not { colourable: true })
        {
            return;
        }

        var colourRect = new Rect(position, new Vector2(width, 50f)).ContractedBy(5);
        Widgets.DrawMenuSection(colourRect);
        colourRect = colourRect.ContractedBy(1);

        Widgets.DrawRectFast(colourRect, chapterColorComp.SlotColour(chapterSlot));
        if (Widgets.ButtonInvisible(colourRect))
        {
            Find.WindowStack.Add(new Dialog_ColourPicker(chapterColorComp.SlotColour(chapterSlot), newColour =>
            {
                chapterColorComp.SetSlotColour(chapterSlot, newColour);
            }));
        }

        curY = colourRect.yMax;
    }

    private void DrawIcon(Rect iconRect, Vector2 position, Vector2 smallIconSize, ShoulderIconDef def, bool chapterSlot)
    {
        if (IsSelected(def, chapterSlot))
        {
            Widgets.DrawStrongHighlight(iconRect.ExpandedBy(3f));
        }

        var color = Mouse.IsOver(iconRect) ? GenUI.MouseoverColor : Color.white;
        GUI.color = color;
        GUI.DrawTexture(iconRect, Command.BGTexShrunk);
        GUI.color = Color.white;
        GUI.DrawTexture(iconRect, def.Icon);

        if (def.colourable)
        {
            var paintableIconRect = new Rect(new Vector2(position.x + 7f, position.y + 5f), smallIconSize);
            GUI.DrawTexture(paintableIconRect, Genes40kUtils.PaintableIcon.Texture);
        }

        TooltipHandler.TipRegion(iconRect, def.label);

        if (Widgets.ButtonInvisible(iconRect))
        {
            if (chapterSlot)
            {
                chapterColorComp.SetChapterIcon(def);
            }
            else
            {
                chapterColorComp.SetRankIcon(def);
            }
        }
    }
        
    public override void DrawTab(Rect rect, Pawn pawn, ref Vector2 apparelColorScrollPosition)
    {            
        if (chapterColorComp == null || decorativeComp == null)
        {
            return;
        }
            
        GUI.BeginGroup(rect);
        var outRect = new Rect(0f, 0f, rect.width, rect.height);
        var viewRect = new Rect(0f, 0f, rect.width - 16f, listScrollViewHeight);
        Widgets.BeginScrollView(outRect, ref apparelColorScrollPosition, viewRect);
        
        //Flip shoulder pad icon locations
        var flipRect = new Rect(viewRect.x, viewRect.y, viewRect.width, 30f);
        flipRect.width /= 3;
        flipRect.x += flipRect.width;
        if (Widgets.ButtonText(flipRect, "BEWH.MankindsFinest.ShoulderIcon.Flip".Translate()))
        {
            chapterColorComp.SwapShoulderIcons();
        }

        var curY = flipRect.yMax + 5f;
        
        //Left shoulder icon title
        var nameRect = new Rect(viewRect.x, curY, viewRect.width, 30f);
        nameRect.width /= 2;
        nameRect.x += nameRect.width / 2;
        Widgets.DrawMenuSection(nameRect);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(nameRect, "BEWH.MankindsFinest.ShoulderIcon.LeftShoulder".Translate());
        Text.Anchor = TextAnchor.UpperLeft;
                
        //Reset left shoulder icon to default
        var resetChapterIconRect = new Rect(viewRect.x, curY, viewRect.width, 30f);
        resetChapterIconRect.width /= 5;
        resetChapterIconRect.x = nameRect.xMin - resetChapterIconRect.width - nameRect.width/20;
        if (Widgets.ButtonText(resetChapterIconRect, "BEWH.MankindsFinest.ShoulderIcon.ResetToDefault".Translate()))
        {
            var settings = Genes40kUtils.ModSettings;
            chapterColorComp.SetChapterIcon(settings.CurrentlySelectedPreset?.relatedChapterIcon);
            if (settings.chapterShoulderIconColor != null && chapterColorComp.ChapterIcon is { colourable: true })
            {
                chapterColorComp.SetSlotColour(true, settings.chapterShoulderIconColor.Value);
            }
        }
            
        var iconSize = new Vector2(viewRect.width/RowAmount, viewRect.width/RowAmount);
        var smallIconSize = new Vector2(iconSize.x / 4, iconSize.y / 4);
        var position = new Vector2(viewRect.x, resetChapterIconRect.yMax);
            
        var curX = position.x;
        curY = position.y;
        
        //Left icon colour selection if possible.
        DrawColourBox(ref curY, position, viewRect.width, chapterSlot: true);
            
        //Left icon selection
        for (var i = 0; i < leftShoulderIcons.Count; i++)
        {
            position = new Vector2(curX, curY);
            var iconRect = new Rect(position, iconSize);
                
            curX += iconRect.width;

            if (i != 0 && (i+1) % RowAmount == 0)
            {
                curY += iconRect.height;
                curX = viewRect.position.x;
            }
            else if (i == leftShoulderIcons.Count - 1)
            {
                curY += iconRect.height;
            }
                
            iconRect = iconRect.ContractedBy(5f);

            DrawIcon(iconRect, position, smallIconSize, leftShoulderIcons[i], chapterSlot: true);
        }

        curY += 34f;

        //Right Shoulder title
        var nameRect2 = new Rect(viewRect.x, curY, viewRect.width, 30f);
        nameRect2.width /= 2;
        nameRect2.x += nameRect2.width / 2;
            
        Widgets.DrawMenuSection(nameRect2);
        Text.Anchor = TextAnchor.MiddleCenter;
        Widgets.Label(nameRect2, "BEWH.MankindsFinest.ShoulderIcon.RightShoulder".Translate());
        Text.Anchor = TextAnchor.UpperLeft;
            
        //Reset right Shoulder to default
        var resetRankIconRect = new Rect(viewRect.x, curY, viewRect.width, 30f);
        resetRankIconRect.width /= 5;
        resetRankIconRect.x = nameRect.xMin - resetRankIconRect.width - nameRect.width/20;
        if (Widgets.ButtonText(resetRankIconRect, "BEWH.MankindsFinest.ShoulderIcon.ResetToDefault".Translate()))
        {
            chapterColorComp.SetRankIcon(Genes40kDefOf.BEWH_FollowRankUp);
        }

        position = new Vector2(viewRect.x, resetRankIconRect.yMax);
            
        curX = position.x;
        curY = position.y;
            
        //Right icon colour selection if possible.
        DrawColourBox(ref curY, position, viewRect.width, chapterSlot: false);
                
        //Right Shoulder Icons
        for (var i = 0; i < rightShoulderIcons.Count; i++)
        {
            position = new Vector2(curX, curY);
            var iconRect = new Rect(position, iconSize);
                
            curX += iconRect.width;

            if (i != 0 && (i+1) % RowAmount == 0)
            {
                curY += iconRect.height;
                curX = viewRect.position.x;
            }
                
            iconRect = iconRect.ContractedBy(5f);

            DrawIcon(iconRect, position, smallIconSize, rightShoulderIcons[i], chapterSlot: false);
        }

        listScrollViewHeight = position.y + iconSize.y + 10f;
            
        Widgets.EndScrollView();
        GUI.EndGroup();
    }
    
    public override void OnClose(Pawn pawn, bool closeOnCancel, bool closeOnClickedOutside)
    {
        OnReset(pawn);
        chapterColorComp?.EndEditing();
    }
    
    public override void OnReset(Pawn pawn)
    {
        if (pawn.apparel == null)
        {
            return;
        }

        foreach (var apparel in pawn.apparel.WornApparel)
        {
            var comp = apparel.GetComp<CompChapterColorWithShoulderDecoration>();
            if (comp == null)
            {
                continue;
            }

            //The chapter comp first: the rank slot state has to be back before the entries are.
            comp.Reset();
            comp.DecorativeComp?.Reset();
        }
    }
}
