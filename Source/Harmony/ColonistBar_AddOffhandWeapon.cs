using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace DualWield.Harmony
{
    [HarmonyPatch(typeof(ColonistBar), nameof(ColonistBar.ColonistBarOnGUI))]
    [HarmonyPriority(Priority.HigherThanNormal)]
    public class ColonistBar_AddOffhandWeapon
    {
        private static MethodInfo JobInBar;
        static MethodInfo showWeaponsUnderPortraitMode =
            AccessTools.PropertyGetter(typeof(Prefs), nameof(Prefs.ShowWeaponsUnderPortraitMode));
        static MethodInfo drafted = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.Drafted));
        static FieldInfo def = AccessTools.Field(typeof(Thing), nameof(Thing.def));
        static MethodInfo isWeapon = AccessTools.PropertyGetter(typeof(ThingDef), nameof(ThingDef.IsWeapon));
        static MethodInfo posY = AccessTools.PropertyGetter(typeof(Rect), nameof(Rect.y));
        static MethodInfo posX = AccessTools.PropertyGetter(typeof(Rect), nameof(Rect.x));
        static MethodInfo sizeHeight = AccessTools.PropertyGetter(typeof(Rect), nameof(Rect.height));
        static MethodInfo drawIcon = AccessTools.Method(typeof(Widgets), nameof(Widgets.ThingIcon), new[] { typeof(Rect), typeof(Thing), typeof(float), typeof(Rot4?), typeof(bool), typeof(float), typeof(bool) });

        private static MethodInfo tryGetOffhand = AccessTools.Method(typeof(Ext_Pawn_EquipmentTracker),
            nameof(Ext_Pawn_EquipmentTracker.TryGetOffHandEquipment));
        static FieldInfo pawn = AccessTools.Field(typeof(ColonistBar.Entry), nameof(ColonistBar.Entry.pawn));
        static FieldInfo equipment = AccessTools.Field(typeof(Pawn), nameof(Pawn.equipment));

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            showWeaponsUnderPortraitMode =
                AccessTools.PropertyGetter(typeof(Prefs), nameof(Prefs.ShowWeaponsUnderPortraitMode));
            drafted = AccessTools.PropertyGetter(typeof(Pawn), nameof(Pawn.Drafted));
            isWeapon = AccessTools.PropertyGetter(typeof(ThingDef), nameof(ThingDef.IsWeapon));
            posY = AccessTools.PropertyGetter(typeof(Rect), nameof(Rect.y));
            sizeHeight = AccessTools.PropertyGetter(typeof(Rect), nameof(Rect.height));
            drawIcon = AccessTools.Method(typeof(Widgets), nameof(Widgets.ThingIcon), new []{typeof(Rect), typeof(Thing), typeof(float), typeof(Rot4?), typeof(bool), typeof(float), typeof(bool)});
            pawn = AccessTools.Field(typeof(ColonistBar.Entry), nameof(ColonistBar.Entry.pawn));

            if (AppDomain.CurrentDomain.GetAssemblies().Any(x =>
                    x.GetName().Name.Equals("JobInBar", StringComparison.CurrentCultureIgnoreCase)))
            {
                JobInBar = AccessTools.Method("Patch_ColonistBar_OnGUI_OffsetEquipped:GetOffsetFor",
                    new[] { typeof(Pawn) });
                if (JobInBar == null) 
                    Log.Warning("JobInBar mod was found, but could not fetch the GetOffsetFor method. Dual Wield will not be able to offset the offhand weapon when job is shown.");
            }

            var matcher = new CodeMatcher(instructions);

            matcher.MatchStartForward(new CodeMatch(x =>
                x.IsLdloc() && x.operand is LocalBuilder lb && lb.LocalType == typeof(Rect)));
            if (matcher.IsInvalid)
                throw new Exception("Unable to find rect to draw on Colonist bar");

            var rectLb = matcher.Instruction.Clone().operand as LocalBuilder;

            matcher.MatchStartForward(new CodeMatch(x =>
                x.IsStloc() && (x.operand is LocalBuilder lb) && lb.LocalType == typeof(ColonistBar.Entry)));
            if (matcher.IsInvalid)
                throw new Exception("Unable to find current pawn in Colonist bar");

            var entryIndex = matcher.Instruction.Clone().operand as LocalBuilder;

            matcher.End();
            matcher.MatchEndBackwards(new CodeMatch(OpCodes.Call, showWeaponsUnderPortraitMode));
            if (matcher.IsInvalid)
                throw new Exception("Unable to find Portrait instruction for showing weapons in Colonist bar");

            matcher.MatchStartForward(new CodeMatch(OpCodes.Callvirt, drafted));
            if (matcher.IsInvalid)
                throw new Exception("Unable to find Draft instruction for showing weapons in Colonist bar");

            matcher.MatchStartForward(new CodeMatch(OpCodes.Callvirt, isWeapon));
            if (matcher.IsInvalid)
                throw new Exception("Unable to find entrypoint for Rect");

            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Ldloca_S, rectLb),
                new CodeMatch(OpCodes.Call, posX),
                new CodeMatch(OpCodes.Ldloca_S, rectLb),
                new CodeMatch(OpCodes.Call, posY),
                new CodeMatch(OpCodes.Ldloca_S, rectLb),
                new CodeMatch(OpCodes.Call, sizeHeight)
            );
            

            if (matcher.IsInvalid)
                throw new Exception("Unable to find rect being set up in Colonist bar");
            var rectStart = matcher.Pos;


            matcher.MatchStartForward(new CodeMatch(OpCodes.Call, drawIcon));
            if (matcher.IsInvalid)
                throw new Exception("Unable to find DrawIcon instruction for showing weapons in Colonist bar");

            var drawEnd = matcher.Pos;

            var createRect = matcher.InstructionsInRange(rectStart, drawEnd);
            var skipLabel = il.DefineLabel();
            var existingSkipCode = matcher.InstructionAt(1);
            existingSkipCode.labels.Add(skipLabel);
            var offHand = il.DeclareLocal(typeof(ThingWithComps));

            matcher.InsertAfterAndAdvance(new CodeInstruction(OpCodes.Ldloca_S, entryIndex),
                new CodeInstruction(OpCodes.Ldfld, pawn),
                new CodeInstruction(OpCodes.Ldfld, equipment),
                new CodeInstruction(OpCodes.Ldloca, offHand),
                new CodeInstruction(OpCodes.Call, tryGetOffhand),
                new CodeInstruction(OpCodes.Brfalse, skipLabel),

                //JobInBar compat - It's looking for isWeapon + 6 to add offset
                new CodeInstruction(OpCodes.Ldloc, offHand),
                new CodeInstruction(OpCodes.Ldfld, def),
                new CodeInstruction(OpCodes.Callvirt, isWeapon),
                new CodeInstruction(OpCodes.Brfalse, skipLabel)
            );

            matcher.InsertAfter(createRect);
            matcher.Advance(drawEnd - matcher.Pos);
            matcher.MatchStartForward(new CodeMatch(OpCodes.Call, sizeHeight));
            matcher.MatchStartForward(new CodeMatch(OpCodes.Add));
            matcher.InsertAfterAndAdvance(
                new CodeInstruction(OpCodes.Ldc_R4, 5f),
                new CodeInstruction(OpCodes.Ldloc, offHand),
                new CodeInstruction(OpCodes.Ldfld, def),
                new CodeInstruction(OpCodes.Ldfld, AccessTools.Field(typeof(ThingDef), nameof(ThingDef.uiIconScale))),
                new CodeInstruction(OpCodes.Mul),
                new CodeInstruction(OpCodes.Add)
            );

            matcher.MatchStartForward(new CodeMatch(x =>
                x.IsLdloc() && x.operand is LocalBuilder lb && lb.LocalType == typeof(ThingWithComps)));
            matcher.SetInstructionAndAdvance(new CodeInstruction(OpCodes.Ldloc, offHand));

            return matcher.Instructions();
        }
    }
}
