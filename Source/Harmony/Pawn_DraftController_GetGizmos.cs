using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace DualWield.Harmony
{
    [HarmonyPatch(typeof(Pawn_DraftController), "GetGizmos", MethodType.Enumerator)]
    public class Pawn_DraftController_GetGizmos
    {
        public static IEnumerable<CodeInstruction> Transpiler(MethodBase __originalMethod, ILGenerator il,
            IEnumerable<CodeInstruction> instructions)
        {
            var codes = instructions.ToList();

            var draftController = typeof(Pawn_DraftController);
            var pawn = AccessTools.Field(draftController, "pawn");
            var equipment = AccessTools.Field(typeof(Pawn), "equipment");


            var getPrimary = AccessTools.PropertyGetter(typeof(Pawn_EquipmentTracker), "Primary");
            var thingDefField = AccessTools.Field(typeof(Thing), "def");
            var isRangedGetter = AccessTools.PropertyGetter(typeof(ThingDef), "IsRangedWeapon");

            var lb_offhandWeapon = il.DeclareLocal(typeof(ThingWithComps));

            var getOffhand = AccessTools.Method(typeof(Ext_Pawn_EquipmentTracker),
                nameof(Ext_Pawn_EquipmentTracker.TryGetOffHandEquipment));

            var matcher = new CodeMatcher(codes, il);
            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Ldfld, pawn),
                new CodeMatch(OpCodes.Ldfld, equipment),
                new CodeMatch(OpCodes.Callvirt, getPrimary),
                new CodeMatch(OpCodes.Brfalse),
                new CodeMatch(OpCodes.Ldloc_1),
                new CodeMatch(OpCodes.Ldfld, pawn),
                new CodeMatch(OpCodes.Ldfld, equipment),
                new CodeMatch(OpCodes.Callvirt, getPrimary),
                new CodeMatch(OpCodes.Ldfld, thingDefField),
                new CodeMatch(OpCodes.Callvirt, isRangedGetter),
                new CodeMatch(OpCodes.Brfalse)
                );

            if (matcher.IsInvalid)
            {
                Log.Error(@"Unable to patch GetGizmos, could not match instructions.
This will mean that if your pawn has a main-hand melee + offhand ranged, the 'Fire At Will' button will not show.");
                return codes;
            }

            var primaryNullCheckSkip = matcher.InstructionAt(4);
            var primaryRangedCheckSkip = matcher.InstructionAt(11);

            var primarySkipLabel = il.DefineLabel();
            var entryPoint = il.DefineLabel();

            var offhandRangedCheck = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldloc_1) { labels = new List<Label>() { primarySkipLabel }},
                new CodeInstruction(OpCodes.Ldfld, pawn),
                new CodeInstruction(OpCodes.Ldfld, equipment),
                new CodeInstruction(OpCodes.Ldloca_S, lb_offhandWeapon),
                new CodeInstruction(OpCodes.Call, getOffhand),
                new CodeInstruction(OpCodes.Brfalse_S, primaryNullCheckSkip.operand),

                new CodeInstruction(OpCodes.Ldloc, lb_offhandWeapon),
                new CodeInstruction(OpCodes.Ldfld, thingDefField),
                new CodeInstruction(OpCodes.Callvirt, isRangedGetter),
                new CodeInstruction(OpCodes.Brfalse_S, primaryRangedCheckSkip.operand),

                new CodeInstruction(OpCodes.Nop) { labels = new List<Label>() { entryPoint }}
            };

            matcher.Advance(4);

            if (matcher.Instruction != primaryNullCheckSkip)
                throw new Exception("Unexpected instruction");

            matcher.Set(OpCodes.Brfalse_S, primarySkipLabel);

            matcher.Advance(7);

            if (matcher.Instruction != primaryRangedCheckSkip)
                throw new Exception("Unexpected instruction");

            matcher.Set(OpCodes.Brtrue_S, entryPoint);

            matcher.InsertAfter(offhandRangedCheck);

            return matcher.Instructions();
        }
    }
}
