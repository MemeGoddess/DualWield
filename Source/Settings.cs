using DualWield.Settings;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace DualWield
{
    public class DWSettings : ModSettings
    {
        private Color _selectedColor = new Color(0.5f, 1f, 0.5f, 1f);

        private bool _settingsGroupDrawing = true;
        private bool _settingsGroupSecondary = true;
        private bool _settingsGroupTwoHanded = true;
        private bool _settingsGroupPenalties = true;

        public float StaticCooldownPOffHand = 20;
        public float StaticCooldownPMainHand = 10;
        public float StaticAccPOffHand = 10;
        public float StaticAccPMainHand = 10;
        public float DynamicCooldownP = 5;
        public float DynamicAccP = 0.5f;

        public float MeleeAngle = 270;
        public float RangedAngle = 135f;
        public float MeleeXOffset = 0.4f;
        public float RangedXOffset = 0.1f;
        public float MeleeZOffset = 0f;
        public float RangedZOffset = 0f;

        public bool MeleeMirrored = true;
        public bool RangedMirrored = true;

        public float NpcDualWieldChance = 40f;

        public Dictionary<string, Record> DualWieldSelection = new Dictionary<string, Record>();
        public Dictionary<string, Record> TwoHandSelection = new Dictionary<string, Record>();
        public Dictionary<string, Record> CustomRotations = new Dictionary<string, Record>();

        private string _meleeAngleBuffer;
        private string _rangedAngleBuffer;
        private string _meleeXOffsetBuffer;
        private string _rangedXOffsetBuffer;
        private string _meleeZOffsetBuffer;
        private string _rangedZOffsetBuffer;

        private string _staticCooldownPOffHandBuffer;
        private string _staticCooldownPMainHandBuffer;
        private string _staticAccPOffHandBuffer;
        private string _staticAccPMainHandBuffer;
        private string _dynamicCooldownPBuffer;
        private string _dynamicAccPBuffer;
        private string _npcDualWieldChanceBuffer;

        private List<ThingDef> _allWeapons = new List<ThingDef>();

        private static Vector2 _scroll = Vector2.zero;
        private static float _maxHeight = 600;

        public void Init()
        {
            _allWeapons = GetAllWeapons();
            if (_allWeapons.Count == 0)
                throw new Exception("[Dual Wield] Found 0 weapons");

            AddMissingWeaponsForRotationSelection(_allWeapons);
            RemoveDeprecatedRecords(_allWeapons, CustomRotations);

            AddMissingWeaponsForDualWieldSelection(_allWeapons);
            RemoveDeprecatedRecords(_allWeapons, DualWieldSelection);

            AddMissingWeaponsForTwoHandSelection(_allWeapons);
            RemoveDeprecatedRecords(_allWeapons, TwoHandSelection);

            GUIDrawUtility.EnsureThingDefLookup(_allWeapons);
        }

        private static long lastTicks = 0;
        public void DoWindowContents(Rect rect)
        {
            var stopwatch = Stopwatch.StartNew();
            if(_allWeapons == null || !_allWeapons.Any())
                _allWeapons = GetAllWeapons();
            
            var columns = 2;
            var leftRect = new Rect(rect.x, 0, (rect.width - 16f) / columns, _maxHeight);
            var rightRect = new Rect(leftRect.xMax, 0, (rect.width - 16f) / columns, _maxHeight);


            var header = new Listing_Standard();
            header.Begin(rect);
            header.GapLine(5f);
            header.End();

            var scrollRect = new Rect(new Vector2(rect.x, rect.y + 15f), new Vector2(rect.width, rect.height - 15f));
            Widgets.BeginScrollView(scrollRect, ref _scroll, new Rect(0f, 0f, rect.width - 16f, _maxHeight));
            _maxHeight = 0;

            #region Left
            var left = new Listing_Standard();
            left.Begin(leftRect);
            left.verticalSpacing = 5f;

            var leftHeight = 0f;
            leftHeight += left.Button("DW_SettingsGroup_Drawing_Title".Translate() + $" {lastTicks}ts", ref _settingsGroupDrawing);

            if (_settingsGroupDrawing)
            {
                leftHeight += left.Label("DW_Setting_Note_Drawing".Translate()).height;
                leftHeight += left.verticalSpacing;

                leftHeight += left.TextNumeric("DW_Setting_MeleeAngle_Title".Translate(), "DW_Setting_MeleeAngle_Description".Translate(), 
                    ref MeleeAngle, ref _meleeAngleBuffer, max: 360f);
                leftHeight += left.TextNumeric("DW_Setting_RangedAngle_Title".Translate(), "DW_Setting_RangedAngle_Description".Translate(), 
                    ref RangedAngle, ref _rangedAngleBuffer, max: 360f);

                leftHeight += left.TextNumeric("DW_Setting_MeleeXOffset_Title".Translate(), "DW_Setting_MeleeXOffset_Description".Translate(), 
                        ref MeleeXOffset, ref _meleeXOffsetBuffer, -2f, 2f);
                leftHeight += left.TextNumeric("DW_Setting_RangedXOffset_Title".Translate(), "DW_Setting_RangedXOffset_Description".Translate(),
                    ref RangedXOffset, ref _rangedXOffsetBuffer, -2f, 2f);

                leftHeight += left.TextNumeric("DW_Setting_MeleeZOffset_Title".Translate(), "DW_Setting_MeleeZOffset_Description".Translate(),
                    ref MeleeZOffset, ref _meleeZOffsetBuffer, -2f, 2f);
                leftHeight += left.TextNumeric("DW_Setting_RangedZOffset_Title".Translate(), "DW_Setting_RangedZOffset_Description".Translate(),
                    ref RangedZOffset, ref _rangedZOffsetBuffer, -2f, 2f);

                left.CheckboxLabeled("DW_Setting_MeleeMirrored_Title".Translate(), ref MeleeMirrored, "DW_Setting_MeleeMirrored_Description".Translate(), labelPct:0.6f, height: Text.LineHeight);
                leftHeight += Text.LineHeight;
                leftHeight += left.verticalSpacing;

                left.CheckboxLabeled("DW_Setting_RangedMirrored_Title".Translate(), ref RangedMirrored, "DW_Setting_RangedMirrored_Description".Translate(), labelPct:0.6f, height: Text.LineHeight);
                leftHeight += Text.LineHeight;
                leftHeight += left.verticalSpacing;

                var rotationRect = left.GetRect(Math.Max(0, scrollRect.height + _scroll.y - leftHeight));
                var actualHeight = GUIDrawUtility.CustomDrawer_MatchingThingDefs_dialog(
                    rotationRect, CustomRotations, GetRotationDefaults(_allWeapons), _allWeapons,
                    "DW_Setting_CustomRotations_Header".Translate(),
                    _scroll.y > leftHeight ? _scroll.y - leftHeight : 0);

                left.Gap(actualHeight - rotationRect.height);
                leftHeight += actualHeight;
                leftHeight += left.verticalSpacing;
            }

            leftHeight += left.Button("DW_SettingsGroup_Penalties_Title".Translate(), ref _settingsGroupPenalties);
            
            if (_settingsGroupPenalties)
            {
                leftHeight += left.TextNumeric("DW_Setting_StaticCooldownPenOffHand_Title".Translate(), "DW_Setting_StaticCooldownPenOffHand_Description".Translate(), 
                    ref StaticCooldownPOffHand, ref _staticCooldownPOffHandBuffer, max: 500f);
                leftHeight += left.TextNumeric("DW_Setting_StaticCooldownPMainHand_Title".Translate(), "DW_Setting_StaticCooldownPMainHand_Description".Translate(),
                    ref StaticCooldownPMainHand, ref _staticCooldownPMainHandBuffer, max: 500f);

                leftHeight += left.TextNumeric("DW_Setting_StaticAccPOffHand_Title".Translate(), "DW_Setting_StaticAccPOffHand_Description".Translate(),
                    ref StaticAccPOffHand, ref _staticAccPOffHandBuffer, max: 500f);
                leftHeight += left.TextNumeric("DW_Setting_StaticAccPMainHand_Title".Translate(), "DW_Setting_StaticAccPMainHand_Description".Translate(),
                    ref StaticAccPMainHand, ref _staticAccPMainHandBuffer, max: 500f);

                leftHeight += left.TextNumeric("DW_Setting_DynamicCooldownP_Title".Translate(), "DW_Setting_DynamicCooldownP_Description".Translate(),
                    ref DynamicCooldownP, ref _dynamicCooldownPBuffer, max: 100f);
                leftHeight += left.TextNumeric("DW_Setting_DynamicAccP_Title".Translate(), "DW_Setting_DynamicAccP_Description".Translate(),
                    ref DynamicAccP, ref _dynamicAccPBuffer, max: 10f);
            }

            left.End();
            #endregion

            #region Right

            var rightHeight = 0f;
            var right = new Listing_Standard();
            right.verticalSpacing = left.verticalSpacing;
            right.Begin(rightRect);

            rightHeight += right.Button("DW_SettingsGroup_DualWieldSelection_Title".Translate(), ref _settingsGroupSecondary);

            if (_settingsGroupSecondary)
            {
                var secondaryRect = right.GetRect(Math.Max(0, scrollRect.height + _scroll.y - rightHeight));
                var actualHeight = GUIDrawUtility.CustomDrawer_MatchingThingDefs_active(secondaryRect,
                    DualWieldSelection, GetDualWieldDefaults(_allWeapons), _allWeapons,
                    "DW_Setting_DualWield_OK".Translate(), "DW_Setting_DualWield_NOK".Translate(), TwoHandSelection,
                    "DW_Setting_DualWield_DisabledReason".Translate(), _scroll.y > rightHeight ? _scroll.y - rightHeight : 0);

                right.Gap(actualHeight - secondaryRect.height);
                rightHeight += actualHeight;
                rightHeight += right.verticalSpacing;
            }

            rightHeight += right.Button("DW_SettingsGroup_TwoHandSelection_Title".Translate(),
                ref _settingsGroupTwoHanded);

            if (_settingsGroupTwoHanded)
            {
                var twoHandedRect = right.GetRect(Math.Max(0, scrollRect.height + _scroll.y - rightHeight));
                var actualHeight = GUIDrawUtility.CustomDrawer_MatchingThingDefs_active(twoHandedRect,
                    TwoHandSelection, GetTwoHandDefaults(_allWeapons), _allWeapons,
                    "DW_Setting_TwoHanded_OK".Translate(), "DW_Setting_TwoHanded_NOK".Translate(), DualWieldSelection,
                    "DW_Setting_TwoHand_DisabledReason".Translate(),
                    _scroll.y > rightHeight ? _scroll.y - rightHeight : 0);

                right.Gap(actualHeight - twoHandedRect.height);
                rightHeight += actualHeight;
                rightHeight += right.verticalSpacing;
            }

            rightHeight += right.TextNumeric("DW_Setting_NPCDualWieldChance_Title".Translate(),
                "DW_Setting_NPCDualWieldChance_Description".Translate(), ref NpcDualWieldChance,
                ref _npcDualWieldChanceBuffer, max: 100f);

            right.End();

            Widgets.EndScrollView();
            _maxHeight = Math.Max(leftHeight, rightHeight);
            #endregion

            stopwatch.Stop();
            lastTicks = stopwatch.ElapsedTicks;
        }
        public override void ExposeData()
        {
            Scribe_Values.Look(ref _settingsGroupDrawing, nameof(_settingsGroupDrawing), true);
            Scribe_Values.Look(ref _settingsGroupSecondary, nameof(_settingsGroupSecondary), true);
            Scribe_Values.Look(ref _settingsGroupTwoHanded, nameof(_settingsGroupTwoHanded), true);
            Scribe_Values.Look(ref _settingsGroupPenalties, nameof(_settingsGroupPenalties), true);

            Scribe_Values.Look(ref StaticCooldownPOffHand, nameof(StaticCooldownPOffHand), 20f);
            Scribe_Values.Look(ref StaticCooldownPMainHand, nameof(StaticCooldownPMainHand), 10f);
            Scribe_Values.Look(ref StaticAccPOffHand, nameof(StaticAccPOffHand), 10f);
            Scribe_Values.Look(ref StaticAccPMainHand, nameof(StaticAccPMainHand), 10f);
            Scribe_Values.Look(ref DynamicCooldownP, nameof(DynamicCooldownP), 5f);
            Scribe_Values.Look(ref DynamicAccP, nameof(DynamicAccP), 0.5f);

            Scribe_Values.Look(ref MeleeAngle, nameof(MeleeAngle), 270f);
            Scribe_Values.Look(ref RangedAngle, nameof(RangedAngle), 135f);
            Scribe_Values.Look(ref MeleeXOffset, nameof(MeleeXOffset), 0.4f);
            Scribe_Values.Look(ref RangedXOffset, nameof(RangedXOffset), 0.1f);
            Scribe_Values.Look(ref MeleeZOffset, nameof(MeleeZOffset), 0f);
            Scribe_Values.Look(ref RangedZOffset, nameof(RangedZOffset), 0f);

            Scribe_Values.Look(ref MeleeMirrored, nameof(MeleeMirrored), true);
            Scribe_Values.Look(ref RangedMirrored, nameof(RangedMirrored), true);

            Scribe_Values.Look(ref NpcDualWieldChance, nameof(NpcDualWieldChance), 40f);

            Scribe_Collections.Look(ref DualWieldSelection, nameof(DualWieldSelection), LookMode.Value);
            Scribe_Collections.Look(ref TwoHandSelection, nameof(TwoHandSelection), LookMode.Value);
            Scribe_Collections.Look(ref CustomRotations, nameof(CustomRotations), LookMode.Value);

            if (Scribe.mode != LoadSaveMode.PostLoadInit) return;

            if (DualWieldSelection == null) DualWieldSelection = new Dictionary<string, Record>();
            if (TwoHandSelection == null) TwoHandSelection = new Dictionary<string, Record>();
            if (CustomRotations == null) CustomRotations = new Dictionary<string, Record>();
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
            foreach (ThingDef weapon in from td in allWeapons where !CustomRotations.ContainsKey(td.defName) select td)
            {
                SetRotationDefault(CustomRotations, weapon);
            }
        }

        private void AddMissingWeaponsForDualWieldSelection(List<ThingDef> allWeapons)
        {
            foreach (ThingDef weapon in from td in allWeapons where !DualWieldSelection.ContainsKey(td.defName) select td)
            {
                SetDualWieldDefault(DualWieldSelection, weapon);
            }
        }

        private void AddMissingWeaponsForTwoHandSelection(List<ThingDef> allWeapons)
        {
            foreach (ThingDef weapon in from td in allWeapons where !TwoHandSelection.ContainsKey(td.defName) select td)
            {
                SetTwoHandDefault(TwoHandSelection, weapon);
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
