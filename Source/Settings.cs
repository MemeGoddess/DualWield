using DualWield.Settings;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace DualWield
{
    public class DWSettings : ModSettings
    {
        private Color SelectedColor = new Color(0.5f, 1f, 0.5f, 1f);


        private bool settingsGroup_Drawing = true;
        private bool settingsGroup_Secondary = true;
        private bool settingsGroup_TwoHanded = true;
        private bool settingsGroup_Penalties = true;

        public float staticCooldownPOffHand = 20;
        public float staticCooldownPMainHand = 10;
        public float staticAccPOffHand = 10;
        public float staticAccPMainHand = 10;
        public float dynamicCooldownP = 5;
        public float dynamicAccP = 0.5f;

        public float meleeAngle = 270;
        public float rangedAngle = 135f;
        public float meleeXOffset = 0.4f;
        public float rangedXOffset = 0.1f;
        public float meleeZOffset = 0f;
        public float rangedZOffset = 0f;

        public bool meleeMirrored = true;
        public bool rangedMirrored = true;

        public float NPCDualWieldChance = 40f;

        public Dictionary<string, Record> dualWieldSelection = new Dictionary<string, Record>();
        public Dictionary<string, Record> twoHandSelection = new Dictionary<string, Record>();
        public Dictionary<string, Record> customRotations = new Dictionary<string, Record>();

        private string meleeAngleBuffer;
        private string rangedAngleBuffer;
        private string meleeXOffsetBuffer;
        private string rangedXOffsetBuffer;
        private string meleeZOffsetBuffer;
        private string rangedZOffsetBuffer;

        private string staticCooldownPOffHandBuffer;
        private string staticCooldownPMainHandBuffer;
        private string staticAccPOffHandBuffer;
        private string staticAccPMainHandBuffer;
        private string dynamicCooldownPBuffer;
        private string dynamicAccPBuffer;
        private string npcDualWieldChanceBuffer;

        private List<ThingDef> allWeapons = new List<ThingDef>();

        private static Vector2 scroll = Vector2.zero;
        private static float maxHeight = 600;
        public void DoWindowContents(Rect rect)
        {

            if(allWeapons == null || !allWeapons.Any())
                allWeapons = GetAllWeapons();
            
            var minY = 30;
            var columns = 2;
            var leftRect = new Rect(rect.x, 0, (rect.width - 16f) / columns, maxHeight);
            var rightRect = new Rect(leftRect.xMax, 0, (rect.width - 16f) / columns, maxHeight);


            var header = new Listing_Standard();
            header.Begin(rect);
            header.GapLine(5f);
            header.End();

            var scrollRect = new Rect(new Vector2(rect.x, rect.y + 15f), new Vector2(rect.width, rect.height - 15f));
            Widgets.BeginScrollView(scrollRect, ref scroll, new Rect(0f, 0f, rect.width - 16f, maxHeight));
            //GUIDrawUtility.DrawBackground(new Rect(0f, 10f, rect.width - 16f, maxHeight), Color.magenta);

            #region Left
            var left = new Listing_Standard();
            left.Begin(leftRect);
            left.verticalSpacing = 5f;
            maxHeight = 0;

            var leftHeight = 0f;
            leftHeight += left.Button("DW_SettingsGroup_Drawing_Title".Translate(), ref settingsGroup_Drawing);

            if (settingsGroup_Drawing)
            {
                leftHeight += left.Label("DW_Setting_Note_Drawing".Translate()).height;
                leftHeight += left.verticalSpacing;

                leftHeight += left.TextNumeric("DW_Setting_MeleeAngle_Title".Translate(), "DW_Setting_MeleeAngle_Description".Translate(), 
                    ref meleeAngle, ref meleeAngleBuffer, max: 360f);
                leftHeight += left.TextNumeric("DW_Setting_RangedAngle_Title".Translate(), "DW_Setting_RangedAngle_Description".Translate(), 
                    ref rangedAngle, ref rangedAngleBuffer, max: 360f);

                leftHeight += left.TextNumeric("DW_Setting_MeleeXOffset_Title".Translate(), "DW_Setting_MeleeXOffset_Description".Translate(), 
                        ref meleeXOffset, ref meleeXOffsetBuffer, -2f, 2f);
                leftHeight += left.TextNumeric("DW_Setting_RangedXOffset_Title".Translate(), "DW_Setting_RangedXOffset_Description".Translate(),
                    ref rangedXOffset, ref rangedXOffsetBuffer, -2f, 2f);

                leftHeight += left.TextNumeric("DW_Setting_MeleeZOffset_Title".Translate(), "DW_Setting_MeleeZOffset_Description".Translate(),
                    ref meleeZOffset, ref meleeZOffsetBuffer, -2f, 2f);
                leftHeight += left.TextNumeric("DW_Setting_RangedZOffset_Title".Translate(), "DW_Setting_RangedZOffset_Description".Translate(),
                    ref rangedZOffset, ref rangedZOffsetBuffer, -2f, 2f);

                left.CheckboxLabeled("DW_Setting_MeleeMirrored_Title".Translate(), ref meleeMirrored, "DW_Setting_MeleeMirrored_Description".Translate(), labelPct:0.6f, height: Text.LineHeight);
                leftHeight += Text.LineHeight;
                leftHeight += left.verticalSpacing;

                left.CheckboxLabeled("DW_Setting_RangedMirrored_Title".Translate(), ref rangedMirrored, "DW_Setting_RangedMirrored_Description".Translate(), labelPct:0.6f, height: Text.LineHeight);
                leftHeight += Text.LineHeight;
                leftHeight += left.verticalSpacing;

                if (customRotations.Count < allWeapons.Count)
                    AddMissingWeaponsForRotationSelection(allWeapons);
                if (customRotations.Count > allWeapons.Count)
                {
                    RemoveDeprecatedRecords(allWeapons, customRotations);
                }

                var actualHeight = GUIDrawUtility.CustomDrawer_MatchingThingDefs_dialog(left.GetRect(1), customRotations,
                    GetRotationDefaults(allWeapons), allWeapons, "DW_Setting_CustomRotations_Header".Translate());
                left.Gap(actualHeight + left.verticalSpacing);
                leftHeight += actualHeight;
                leftHeight += left.verticalSpacing;
            }

            leftHeight += left.Button("DW_SettingsGroup_Penalties_Title".Translate(), ref settingsGroup_Penalties);
            
            if (settingsGroup_Penalties)
            {
                leftHeight += left.TextNumeric("DW_Setting_StaticCooldownPenOffHand_Title".Translate(), "DW_Setting_StaticCooldownPenOffHand_Description".Translate(), 
                    ref staticCooldownPOffHand, ref staticCooldownPOffHandBuffer, max: 500f);
                leftHeight += left.TextNumeric("DW_Setting_StaticCooldownPMainHand_Title".Translate(), "DW_Setting_StaticCooldownPMainHand_Description".Translate(),
                    ref staticCooldownPMainHand, ref staticCooldownPMainHandBuffer, max: 500f);

                leftHeight += left.TextNumeric("DW_Setting_StaticAccPOffHand_Title".Translate(), "DW_Setting_StaticAccPOffHand_Description".Translate(),
                    ref staticAccPOffHand, ref staticAccPOffHandBuffer, max: 500f);
                leftHeight += left.TextNumeric("DW_Setting_StaticAccPMainHand_Title".Translate(), "DW_Setting_StaticAccPMainHand_Description".Translate(),
                    ref staticAccPMainHand, ref staticAccPMainHandBuffer, max: 500f);

                leftHeight += left.TextNumeric("DW_Setting_DynamicCooldownP_Title".Translate(), "DW_Setting_DynamicCooldownP_Description".Translate(),
                    ref dynamicCooldownP, ref dynamicCooldownPBuffer, max: 100f);
                leftHeight += left.TextNumeric("DW_Setting_DynamicAccP_Title".Translate(), "DW_Setting_DynamicAccP_Description".Translate(),
                    ref dynamicAccP, ref dynamicAccPBuffer, max: 10f);
            }

            left.End();
            #endregion

            #region Right

            var rightHeight = 0f;
            var right = new Listing_Standard();
            right.verticalSpacing = left.verticalSpacing;
            right.Begin(rightRect);

            rightHeight += right.Button("DW_SettingsGroup_Drawing_Title".Translate(), ref settingsGroup_Secondary);

            if (settingsGroup_Secondary)
            {
                if (dualWieldSelection.Count < allWeapons.Count)
                    AddMissingWeaponsForDualWieldSelection(allWeapons);
                if (dualWieldSelection.Count > allWeapons.Count)
                {
                    RemoveDeprecatedRecords(allWeapons, dualWieldSelection);
                }

                var actualHeight = GUIDrawUtility.CustomDrawer_MatchingThingDefs_active(right.GetRect(1), dualWieldSelection, GetDualWieldDefaults(allWeapons), allWeapons, "DW_Setting_DualWield_OK".Translate(), "DW_Setting_DualWield_NOK".Translate(), twoHandSelection, "DW_Setting_DualWield_DisabledReason".Translate());
                right.Gap(actualHeight + right.verticalSpacing);
                rightHeight += actualHeight;
                rightHeight += right.verticalSpacing;
            }

            rightHeight += right.Button("DW_SettingsGroup_TwoHandSelection_Title".Translate(),
                ref settingsGroup_TwoHanded);

            if (settingsGroup_TwoHanded)
            {
                if (twoHandSelection.Count < allWeapons.Count)
                {
                    AddMissingWeaponsForTwoHandSelection(allWeapons);
                }
                if (twoHandSelection.Count > allWeapons.Count)
                {
                    RemoveDeprecatedRecords(allWeapons, twoHandSelection);
                }

                var actualHeight = GUIDrawUtility.CustomDrawer_MatchingThingDefs_active(right.GetRect(1), twoHandSelection, GetTwoHandDefaults(allWeapons), allWeapons, "DW_Setting_TwoHanded_OK".Translate(), "DW_Setting_TwoHanded_NOK".Translate(),  dualWieldSelection, "DW_Setting_TwoHand_DisabledReason".Translate());
                right.Gap(actualHeight + right.verticalSpacing);
                rightHeight += actualHeight;
                rightHeight += right.verticalSpacing;
            }

            rightHeight += right.TextNumeric("DW_Setting_NPCDualWieldChance_Title".Translate(),
                "DW_Setting_NPCDualWieldChance_Description".Translate(), ref NPCDualWieldChance,
                ref npcDualWieldChanceBuffer, max: 100f);

            right.End();

            Widgets.EndScrollView();
            maxHeight = Math.Max(leftHeight, rightHeight);
            #endregion
        }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref settingsGroup_Drawing, nameof(settingsGroup_Drawing), true);
            Scribe_Values.Look(ref settingsGroup_Secondary, nameof(settingsGroup_Secondary), true);
            Scribe_Values.Look(ref settingsGroup_TwoHanded, nameof(settingsGroup_TwoHanded), true);
            Scribe_Values.Look(ref settingsGroup_Penalties, nameof(settingsGroup_Penalties), true);

            Scribe_Values.Look(ref staticCooldownPOffHand, nameof(staticCooldownPOffHand), 20f);
            Scribe_Values.Look(ref staticCooldownPMainHand, nameof(staticCooldownPMainHand), 10f);
            Scribe_Values.Look(ref staticAccPOffHand, nameof(staticAccPOffHand), 10f);
            Scribe_Values.Look(ref staticAccPMainHand, nameof(staticAccPMainHand), 10f);
            Scribe_Values.Look(ref dynamicCooldownP, nameof(dynamicCooldownP), 5f);
            Scribe_Values.Look(ref dynamicAccP, nameof(dynamicAccP), 0.5f);

            Scribe_Values.Look(ref meleeAngle, nameof(meleeAngle), 270f);
            Scribe_Values.Look(ref rangedAngle, nameof(rangedAngle), 135f);
            Scribe_Values.Look(ref meleeXOffset, nameof(meleeXOffset), 0.4f);
            Scribe_Values.Look(ref rangedXOffset, nameof(rangedXOffset), 0.1f);
            Scribe_Values.Look(ref meleeZOffset, nameof(meleeZOffset), 0f);
            Scribe_Values.Look(ref rangedZOffset, nameof(rangedZOffset), 0f);

            Scribe_Values.Look(ref meleeMirrored, nameof(meleeMirrored), true);
            Scribe_Values.Look(ref rangedMirrored, nameof(rangedMirrored), true);

            Scribe_Values.Look(ref NPCDualWieldChance, nameof(NPCDualWieldChance), 40f);

            Scribe_Collections.Look(ref dualWieldSelection, nameof(dualWieldSelection), LookMode.Value);
            Scribe_Collections.Look(ref twoHandSelection, nameof(twoHandSelection), LookMode.Value);
            Scribe_Collections.Look(ref customRotations, nameof(customRotations), LookMode.Value);

            if (Scribe.mode != LoadSaveMode.PostLoadInit) return;

            if (dualWieldSelection == null) dualWieldSelection = new Dictionary<string, Record>();
            if (twoHandSelection == null) twoHandSelection = new Dictionary<string, Record>();
            if (customRotations == null) customRotations = new Dictionary<string, Record>();
        }

        private static void RemoveDeprecatedRecords(List<ThingDef> allWeapons, Dictionary<string, Record> dict)
        {
            List<string> shouldRemove = new List<string>();
            foreach (string key in from string defName in dict.Keys where !allWeapons.Exists((ThingDef td) => td.defName == defName) select defName)
            {
                shouldRemove.Add(key);
            }
            foreach (string key in shouldRemove)
            {
                dict.Remove(key);
            }
        }

        private void AddMissingWeaponsForRotationSelection(List<ThingDef> allWeapons)
        {
            foreach (ThingDef weapon in from td in allWeapons where !customRotations.ContainsKey(td.defName) select td)
            {
                SetRotationDefault(customRotations, weapon);
            }
        }
        private void AddMissingWeaponsForDualWieldSelection(List<ThingDef> allWeapons)
        {
            foreach (ThingDef weapon in from td in allWeapons where !dualWieldSelection.ContainsKey(td.defName) select td)
            {
                SetDualWieldDefault(dualWieldSelection, weapon);
            }
        }

        private void AddMissingWeaponsForTwoHandSelection(List<ThingDef> allWeapons)
        {
            foreach (ThingDef weapon in from td in allWeapons where !twoHandSelection.ContainsKey(td.defName) select td)
            {
                SetTwoHandDefault(twoHandSelection, weapon);
            }
        }

        private static Dictionary<string, Record> GetRotationDefaults(List<ThingDef> allWeapons)
        {
            Dictionary<string, Record> dict = new Dictionary<string, Record>();
            foreach (ThingDef td in allWeapons)
            {
                SetRotationDefault(dict, td);
            }
            return dict;
        }

        private static void SetRotationDefault(Dictionary<string, Record> dict, ThingDef td)
        {
            Record record = new Record(false, td.label);
            if (td.GetModExtension<DefModextension_CustomRotation>() is DefModextension_CustomRotation modExt)
            {
                record.extraRotation = modExt.extraRotation;
                record.isSelected = true;
            }
            dict.Add(td.defName, record);
        }

        private static Dictionary<string, Record> GetDualWieldDefaults(List<ThingDef> allWeapons)
        {
            Dictionary<string, Record> dict = new Dictionary<string, Record>();
            foreach (ThingDef td in allWeapons)
            {
                SetDualWieldDefault(dict, td);
            }
            return dict;
        }

        private static void SetDualWieldDefault(Dictionary<string, Record> dict, ThingDef td)
        {
            if (td.GetModExtension<DefModextension_DefaultSettings>() is DefModextension_DefaultSettings modExt)
            {
                dict.Add(td.defName, new Record(modExt.dualWield, td.label));
            }
            else if (td.defName.Contains("Bow_") || td.defName.Contains("Blowgun") || td.GetStatValueAbstract(StatDefOf.Mass) > 3f || (td.IsMeleeWeapon && td.GetStatValueAbstract(StatDefOf.Mass) > 1.5f))
            {
                dict.Add(td.defName, new Record(false, td.label));
            }
            else
            {
                dict.Add(td.defName, new Record(true, td.label));
            }
        }

        private static Dictionary<string, Record> GetTwoHandDefaults(List<ThingDef> allWeapons)
        {
            Dictionary<string, Record> dict = new Dictionary<string, Record>();
            foreach (ThingDef td in allWeapons)
            {
                SetTwoHandDefault(dict, td);
            }
            return dict;
        }

        private static void SetTwoHandDefault(Dictionary<string, Record> dict, ThingDef td)
        {
            if (td.GetModExtension<DefModextension_DefaultSettings>() is DefModextension_DefaultSettings modExt)
            {
                dict.Add(td.defName, new Record(modExt.twoHand, td.label));
            }
            else if (td.defName.Contains("Bow") || td.defName.Contains("Shotgun") || td.GetStatValueAbstract(StatDefOf.Mass) > 3f)
            {
                dict.Add(td.defName, new Record(true, td.label));
            }
            else
            {
                dict.Add(td.defName, new Record(false, td.label));
            }
        }

        private static List<ThingDef> GetAllWeapons()
        {
            List<ThingDef> allWeapons = new List<ThingDef>();

            Predicate<ThingDef> isWeapon = (ThingDef td) => td.equipmentType == EquipmentType.Primary && !td.destroyOnDrop;
            foreach (ThingDef thingDef in from td in DefDatabase<ThingDef>.AllDefs
                     where isWeapon(td)
                     select td)
            {
                allWeapons.Add(thingDef);
            }
            return allWeapons;
        }


    }

    public static class SettingsExtensions
    {
        private static Color SelectedColor = new Color(0.5f, 1f, 0.5f, 1f);
        public static float TextNumeric<T>(this Listing_Standard listing,
            string label,
            string description,
            ref T val,
            ref string buffer,
            float min = 0.0f,
            float max = 1E+09f)
            where T : struct
        {
            var rect = listing.GetRect(Text.LineHeight);
            if (!listing.BoundingRectCached.HasValue || rect.Overlaps(listing.BoundingRectCached.Value))
            {
                var rect1 = rect.LeftPart(0.6f).Rounded();
                var rect2 = rect.RightPart(0.4f).Rounded();
                var anchor = (int)Verse.Text.Anchor;
                Verse.Text.Anchor = TextAnchor.MiddleLeft;
                Widgets.Label(rect1, label);
                Verse.Text.Anchor = (TextAnchor)anchor;
                ref var local1 = ref val;
                ref var local2 = ref buffer;
                var min1 = (double)min;
                var max1 = (double)max;
                Widgets.TextFieldNumeric<T>(rect2, ref local1, ref local2, (float)min1, (float)max1);
                TooltipHandler.TipRegion(rect1, () => description, description.GetHashCode());
            }
            listing.Gap(listing.verticalSpacing);
            return Text.LineHeight + listing.verticalSpacing;
        }

        public static float Button(this Listing_Standard listing, string label, ref bool active)
        {
            var original = GUI.color;
            GUI.color = active ? SelectedColor : original;
            if (listing.ButtonText(label))
                active = !active;
            GUI.color = original;
            return 30f + listing.verticalSpacing;
        }
    }
}
