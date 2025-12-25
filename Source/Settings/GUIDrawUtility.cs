using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;
using Verse.Sound;
using HarmonyLib;

namespace DualWield.Settings
{
    [StaticConstructorOnStartup]
    public class GUIDrawUtility
    {
        private const float TextMargin = 20f;
        private const float BottomMargin = 2f;
        private static readonly Color iconMouseOverColor = new Color(0.6f, 0.6f, 0.4f, 1f);
        private static readonly Color disabledColor = new Color(0.7f,0.7f,0.7f,0.2f);
        private static readonly Color notSelectedColor = new Color(0.5f, 0, 0, 0.1f);


        private static Color background = new Color(0.5f, 0, 0, 0.1f);
        private static Color selectedBackground = new Color(0f, 0.5f, 0, 0.1f);
        private const float IconSize = 32f;
        private const float IconGap = 1f;
        private static Texture2D disabledTex;

        static GUIDrawUtility()
        {
            disabledTex = ContentFinder<Texture2D>.Get("UI/ExclamationMark", true);
        }

        private static void DrawBackground(Rect rect, Color background)
        {
            Color save = GUI.color;
            GUI.color = background;
            GUI.DrawTexture(rect, TexUI.FastFillTex);
            GUI.color = save;
        }
        private static void DrawLabel(string labelText, Rect textRect, float offset)
        {
            var labelHeight = Text.CalcHeight(labelText, textRect.width);
            labelHeight -= 2f;
            var labelRect = new Rect(textRect.x, textRect.yMin - labelHeight + offset, textRect.width, labelHeight);
            GUI.DrawTexture(labelRect, TexUI.GrayTextBG);
            GUI.color = Color.white;
            Text.Anchor = TextAnchor.UpperCenter;
            Widgets.Label(labelRect, labelText);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.color = Color.white;
        }
        private static Color GetColor(ThingDef thingDef)
        {
            var stuff = GenStuff.DefaultStuffFor(thingDef);
            if (stuff != null)
                return thingDef.GetColorForStuff(stuff);

            return thingDef.graphicData?.color ?? Color.white;
        }

        private static bool DrawTileForThingDef(ThingDef thingDef, KeyValuePair<String, Record> kv, Rect contentRect, Vector2 iconOffset, int buttonID, bool disabled, string disabledReason = "")
        {
            if(thingDef == null)
                return false;

            var iconRect = new Rect(contentRect.x + iconOffset.x, contentRect.y + iconOffset.y, IconSize, IconSize);
            MouseoverSounds.DoRegion(iconRect, SoundDefOf.Mouseover_Command);
            var save = GUI.color;

            if (Mouse.IsOver(iconRect))
                GUI.color = iconMouseOverColor;
            else if (disabled)
                GUI.color = disabledColor;
            else if (kv.Value.isSelected == true)
                GUI.color = selectedBackground;
            else
                GUI.color = notSelectedColor;

            GUI.DrawTexture(iconRect, TexUI.FastFillTex);
            GUI.color = save;

            TooltipHandler.TipRegion(iconRect, disabled ? disabledReason : thingDef.label);

            var color = GetColor(thingDef);
            var resolvedIcon = GenerateIcon(thingDef, color);
            GUI.color = color;
            GUI.DrawTexture(iconRect, resolvedIcon);
            if (disabled) 
                GUI.DrawTexture(iconRect, disabledTex);
            
            GUI.color = Color.white;

            if (Widgets.ButtonInvisible(iconRect, true))
            {
                Event.current.button = buttonID;
                return true;
            }

            return false;

        }

        private static Dictionary<(ThingDef, Color), Texture> _textureCache = new Dictionary<(ThingDef, Color), Texture>();
        private static Texture GenerateIcon(ThingDef thingDef, Color color)
        {
            Graphic g2 = null;

            if(_textureCache.TryGetValue((thingDef, color), out var cachedTexture))
                return cachedTexture;

            if (thingDef.graphicData != null && thingDef.graphicData.Graphic != null)
            {
                Graphic g = thingDef.graphicData.Graphic;
                g2 = thingDef.graphicData.Graphic.GetColoredVersion(g.Shader, color, color);
            }
            Texture resolvedIcon;
            if (!thingDef.uiIconPath.NullOrEmpty())
            {
                resolvedIcon = thingDef.uiIcon;
            }
            else if (g2 != null)
            {
                resolvedIcon = g2.MatSingle.mainTexture;
            }
            else
            {
                resolvedIcon = new Texture2D(0, 0);
            }
            _textureCache.Add((thingDef, color), resolvedIcon);
            return resolvedIcon;
        }

        private static Color GetPixel(Texture2D tex, float x, float y)
        {
            Color pix;
            int x1 = (int)Mathf.Floor(x);
            int y1 = (int)Mathf.Floor(y);

            if (x1 > tex.width || x1 < 0 ||
               y1 > tex.height || y1 < 0)
            {
                pix = Color.clear;
            }
            else
            {
                pix = tex.GetPixel(x1, y1);
            }

            return pix;
        }

        private static float Rot_x(float angle, float x, float y)
        {
            float cos = Mathf.Cos(angle / 180.0f * Mathf.PI);
            float sin = Mathf.Sin(angle / 180.0f * Mathf.PI);
            return (x * cos + y * (-sin));
        }
        private static float Rot_y(float angle, float x, float y)
        {
            float cos = Mathf.Cos(angle / 180.0f * Mathf.PI);
            float sin = Mathf.Sin(angle / 180.0f * Mathf.PI);
            return (x * sin + y * cos);
        }

        public static float CustomDrawer_MatchingThingDefs_active(Rect wholeRect, Dictionary<string, Record> setting, Dictionary<string, Record> defaults, List<ThingDef> allThingDefs, string yesText = "", string noText = "", Dictionary<string, Record> disabledThingDefs = null,string disabledReason = "")
        {
            //TODO: refactor this mess, remove redundant and quircky things.
            if (setting == null)
            {
                setting = new Dictionary<string, Record>();
                foreach (KeyValuePair<string, Record> kv in defaults)
                {
                    setting.Add(kv.Key, kv.Value);
                }
            }
            int iconsPerRow = (int)((wholeRect.width / 2) / (IconGap + IconSize));
            var highestIndex = setting.GroupBy(x => x.Value.isSelected).Max(y => y.Count());
            var rows = (int)Math.Ceiling(highestIndex / (float)iconsPerRow);
            var maxHeight = (rows * IconSize) + (rows * IconGap) + TextMargin + BottomMargin;
            Rect leftRect = new Rect(wholeRect);
            leftRect.width /= 2;
            leftRect.height = maxHeight;
            leftRect.position = new Vector2(leftRect.position.x, leftRect.position.y);
            Rect rightRect = new Rect(wholeRect);
            rightRect.width = rightRect.width / 2;
            leftRect.height = maxHeight;
            rightRect.position = new Vector2(rightRect.position.x + leftRect.width, rightRect.position.y);
            DrawBackground(new Rect(wholeRect.x, wholeRect.y, wholeRect.width, Math.Max(leftRect.height, rightRect.height)), background);


            GUI.color = Color.white;

            DrawLabel(yesText, leftRect, TextMargin);
            DrawLabel(noText, rightRect, TextMargin);

            leftRect.position = new Vector2(leftRect.position.x, leftRect.position.y + TextMargin);
            rightRect.position = new Vector2(rightRect.position.x, rightRect.position.y + TextMargin);

            bool change = false;
            int indexLeft = 0;
            int indexRight = 0;
            foreach (KeyValuePair<String, Record> item in setting)
            {
                Rect rect = item.Value.isSelected ? leftRect : rightRect;
                int index = item.Value.isSelected ? indexLeft : indexRight;
                leftRect.height = IconSize;
                rightRect.height = IconSize;

                if (item.Value.isSelected)
                {
                    indexLeft++;
                }
                else
                {
                    indexRight++;
                }
                int column = index % iconsPerRow;
                int row = index / iconsPerRow;
                ThingDef thingDef = allThingDefs.FirstOrDefault((ThingDef td) => td.defName == item.Key);
                bool disabled = false;
                if(disabledThingDefs != null)
                {
                    disabled = disabledThingDefs.TryGetValue(item.Key, out Record value) && value.isSelected && item.Value.isSelected;
                }

                bool interacted = DrawTileForThingDef(thingDef, item, rect, new Vector2(IconSize * column + column * IconGap, IconSize * row + row * IconGap), index, disabled, disabledReason);
                if (interacted)
                {
                    change = true;
                    item.Value.isSelected = !item.Value.isSelected;
                }
            }
            return maxHeight;
        }

        public static float CustomDrawer_MatchingThingDefs_dialog(Rect wholeRect, Dictionary<string, Record> setting, Dictionary<string, Record> defaults, List<ThingDef> allThingDefs, string yesText = "")
        {
            //TODO: refactor this mess, remove redundant and quircky things.

            float rowHeight = 20f;
            if (setting == null)
            {
                setting = new Dictionary<string, Record>();
                foreach (KeyValuePair<string, Record> kv in defaults)
                {
                    setting.Add(kv.Key, kv.Value);
                }
            }
            Rect rect = new Rect(wholeRect);
            rect.width = rect.width;
            rect.height = wholeRect.height - TextMargin + BottomMargin;
            rect.position = new Vector2(rect.position.x, rect.position.y);
            int iconsPerRow = (int)(rect.width / (IconGap + IconSize));
            var wastedWidth = rect.width - (iconsPerRow * (IconGap + IconSize));
            rect = new Rect(rect.x, rect.y, rect.width - wastedWidth, rect.height);
            var rowEstimate = (int)Math.Ceiling(setting.Count / (float)iconsPerRow);
            var backgroundHeight = (rowEstimate * IconSize) + (rowEstimate * IconGap) + TextMargin;

            DrawBackground(new Rect(rect.position, new Vector2(rect.width, backgroundHeight)), background);


            GUI.color = Color.white;

            DrawLabel(yesText, rect, TextMargin);

            rect.position = new Vector2(rect.position.x, rect.position.y + TextMargin);

            bool change = false;
            int index = 0;
            foreach (KeyValuePair<String, Record> item in setting)
            {
                rect.height = IconSize;
                int column = index % iconsPerRow;
                int row = index / iconsPerRow;
                ThingDef thingDef = allThingDefs.FirstOrDefault((ThingDef td) => td.defName == item.Key);
                bool interacted = DrawTileForThingDef(thingDef, item, rect, new Vector2(IconSize * column + column * IconGap, IconSize * row + row * IconGap), index, false);
                if (interacted)
                {
                    change = true;
                    Func<int, string> textGetter = ((int x) => "DW_Setting_CustomRotations_SetRotation".Translate(x));
                    Dialog_Slider window = new Dialog_Slider(textGetter, 0, 360, delegate (int value)
                    {
                        item.Value.extraRotation = value;
                        item.Value.isSelected = item.Value.extraRotation > 0;
                    }, item.Value.extraRotation);
                    Find.WindowStack.Add(window);
                }
                index++;
            }
            int rows = index/iconsPerRow + 1;
            return (rows * IconSize) + (rows * IconGap) + TextMargin;
        }
        

    }
}



