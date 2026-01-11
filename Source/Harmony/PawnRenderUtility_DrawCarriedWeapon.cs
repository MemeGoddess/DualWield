using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace DualWield.Harmony
{
    [HarmonyPatch(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawCarriedWeapon))]
    [HarmonyPriority(Priority.High)]
    public static class PawnRenderUtility_DrawCarriedWeapon
    {
        public static bool PatchApplied = false;
        private static readonly MethodInfo miDrawEquipmentAiming = AccessTools.Method(
            typeof(PawnRenderUtility),
            nameof(PawnRenderUtility.DrawEquipmentAiming),
            new[] { typeof(Thing), typeof(Vector3), typeof(float) }
        );
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
        {
            // No point copying over the patches
            var helperType = typeof(PawnRenderUtility_DrawEquipmentAndApparelExtras);

            var fiCtxPawn = AccessTools.Field(helperType, nameof(PawnRenderUtility_DrawEquipmentAndApparelExtras._ctxPawn));
            var miPrepareMainHand = AccessTools.Method(helperType, nameof(PawnRenderUtility_DrawEquipmentAndApparelExtras.PrepareMainHand));
            var miPrepareOffhandDraw = AccessTools.Method(helperType, nameof(PawnRenderUtility_DrawEquipmentAndApparelExtras.PrepareOffhandDraw));

            if (miDrawEquipmentAiming == null) throw new MissingMethodException("PawnRenderUtility.DrawEquipmentAiming(Thing,Vector3,float) not found.");
            if (fiCtxPawn == null) throw new MissingFieldException(helperType.FullName, nameof(PawnRenderUtility_DrawEquipmentAndApparelExtras._ctxPawn));
            if (miPrepareMainHand == null) throw new MissingMethodException($"{helperType.FullName}.{nameof(PawnRenderUtility_DrawEquipmentAndApparelExtras.PrepareMainHand)} not found.");
            if (miPrepareOffhandDraw == null) throw new MissingMethodException($"{helperType.FullName}.{nameof(PawnRenderUtility_DrawEquipmentAndApparelExtras.PrepareOffhandDraw)} not found.");

            var code = instructions.ToList();
            if (!TryFindFirstLocalAssigned(code, out int aimAngleLocalIndex))
                throw new InvalidOperationException("Could not locate aimAngle local (expected pattern: ldc.i4(.s) 143; stloc.*).");

            var mainEqLocal = il.DeclareLocal(typeof(ThingWithComps));
            var mainDrawPosLocal = il.DeclareLocal(typeof(Vector3));
            var mainDrawAngleLocal = il.DeclareLocal(typeof(float));

            var offEqLocal = il.DeclareLocal(typeof(Thing));
            var offDrawPosLocal = il.DeclareLocal(typeof(Vector3));
            var offDrawAngleLocal = il.DeclareLocal(typeof(float));


            var matcher = new CodeMatcher(code, il);
            matcher.MatchStartForward(
                new CodeMatch(OpCodes.Ldarg_0),
                new CodeMatch(OpCodes.Ldarg_1),
                new CodeMatch(ci => ci.IsLdloc()),
                new CodeMatch(OpCodes.Conv_R4)
            );

            if (!matcher.IsValid)
                throw new InvalidOperationException("Could not find call to PawnRenderUtility.DrawEquipmentAiming inside DrawCarriedWeapon.");

            var labels = matcher.Instructions().SelectMany(x => x.labels).ToList();
            matcher.RemoveInstructions(4);


            var injectedBefore = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldsfld, fiCtxPawn) { labels = labels },
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                EmitLdloc(aimAngleLocalIndex),
                new CodeInstruction(OpCodes.Conv_R4),
                new CodeInstruction(OpCodes.Ldloca_S, mainEqLocal),
                new CodeInstruction(OpCodes.Ldloca_S, mainDrawPosLocal),
                new CodeInstruction(OpCodes.Ldloca_S, mainDrawAngleLocal),
                new CodeInstruction(OpCodes.Call, miPrepareMainHand),

                new CodeInstruction(OpCodes.Ldloc, mainEqLocal),
                new CodeInstruction(OpCodes.Castclass, typeof(ThingWithComps)),
                new CodeInstruction(OpCodes.Ldloc, mainDrawPosLocal),
                new CodeInstruction(OpCodes.Ldloc, mainDrawAngleLocal)
            };

            var skipOffhandLabel = il.DefineLabel();
            var injectedAfter = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldsfld, fiCtxPawn),
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Ldarg_1),
                EmitLdloc(aimAngleLocalIndex),
                new CodeInstruction(OpCodes.Conv_R4),
                new CodeInstruction(OpCodes.Ldloca_S, offEqLocal),
                new CodeInstruction(OpCodes.Ldloca_S, offDrawPosLocal),
                new CodeInstruction(OpCodes.Ldloca_S, offDrawAngleLocal),
                new CodeInstruction(OpCodes.Call, miPrepareOffhandDraw),
                new CodeInstruction(OpCodes.Brfalse_S, skipOffhandLabel),
                new CodeInstruction(OpCodes.Ldloc, offEqLocal),
                new CodeInstruction(OpCodes.Castclass, typeof(ThingWithComps)),
                new CodeInstruction(OpCodes.Ldloc, offDrawPosLocal),
                new CodeInstruction(OpCodes.Ldloc, offDrawAngleLocal),
                new CodeInstruction(OpCodes.Call, miDrawEquipmentAiming),
                new CodeInstruction(OpCodes.Nop)
                {
                    labels = new List<Label> { skipOffhandLabel }
                }
            };

            matcher.Insert(injectedBefore);
            while (matcher.IsValid && !matcher.Instruction.Calls(miDrawEquipmentAiming))
                matcher.Advance();
            if (matcher.IsInvalid)
            {
                Log.Error("Unable to patch drafted weapon drawing, this will cause offhand weapons to not appear when standing still.");
                return code;
            }
            matcher.InsertAfter(injectedAfter);

            PatchApplied = true;
            return matcher.Instructions();
        }

        private static bool TryFindFirstLocalAssigned(List<CodeInstruction> code, out int localIndex)
        {
            localIndex = -1;

            for (int i = 0; i < code.Count - 1; i++)
            {
                if (TryGetStlocIndex(code[i + 1], out localIndex))
                    return true;

                if (i + 2 < code.Count && code[i + 1].opcode == OpCodes.Nop && TryGetStlocIndex(code[i + 2], out localIndex))
                    return true;
            }

            return false;
        }

        private static bool TryGetStlocIndex(CodeInstruction ci, out int index)
        {
            index = -1;

            if (ci.opcode == OpCodes.Stloc_0) { index = 0; return true; }
            if (ci.opcode == OpCodes.Stloc_1) { index = 1; return true; }
            if (ci.opcode == OpCodes.Stloc_2) { index = 2; return true; }
            if (ci.opcode == OpCodes.Stloc_3) { index = 3; return true; }

            if (ci.opcode == OpCodes.Stloc_S && ci.operand is LocalBuilder lbS) { index = lbS.LocalIndex; return true; }
            if (ci.opcode == OpCodes.Stloc && ci.operand is LocalBuilder lb) { index = lb.LocalIndex; return true; }

            return false;
        }

        private static CodeInstruction EmitLdloc(int localIndex)
        {
            switch (localIndex)
            {
                case 0:
                    return new CodeInstruction(OpCodes.Ldloc_0);
                case 1:
                    return new CodeInstruction(OpCodes.Ldloc_1);
                case 2:
                    return new CodeInstruction(OpCodes.Ldloc_2);
                case 3:
                    return new CodeInstruction(OpCodes.Ldloc_3);
                default:
                    return new CodeInstruction(OpCodes.Ldloc, localIndex);
            }
        }
    }
}