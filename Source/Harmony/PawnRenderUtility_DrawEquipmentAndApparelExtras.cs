using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using Verse;
// ReSharper disable InconsistentNaming
// ReSharper disable UnusedType.Global

namespace DualWield.Harmony
{


    [HarmonyPatch(typeof(UIRoot_Entry), "Init")]
    public static class UIRoot_Entry_Init_IncompatibleModifications_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (AccessTools.TypeByName("Tacticowl.ModSettings_Tacticowl") != null) 
                CreateHereticalModificationsDialog();
        }

        private static void CreateHereticalModificationsDialog()
        {
            const string text = @"Dual Wield - Continued

Incompatible version of Run and Gun detected, please use Meme Goddess' version.";
            Find.WindowStack.Add(new Dialog_MessageBox(text));
        }
    }

    [HarmonyPatch]
    public static class PawnRenderUtility_DrawEquipmentAndApparelExtras
    {

        [HarmonyTargetMethods]
        public static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Method(typeof(PawnRenderUtility),
                nameof(PawnRenderUtility.DrawEquipmentAndApparelExtras));
        }

        private static readonly MethodInfo MI_DrawEquipmentAiming =
            AccessTools.Method(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAiming),
                new[] { typeof(ThingWithComps), typeof(Vector3), typeof(float) });

        private static readonly MethodInfo MI_PrepareOffhandDraw =
            AccessTools.Method(
                typeof(PawnRenderUtility_DrawEquipmentAndApparelExtras),
                nameof(PrepareOffhandDraw),
                new[]
                {
                    typeof(Pawn),
                    typeof(ThingWithComps),
                    typeof(Vector3),
                    typeof(float),
                    typeof(ThingWithComps).MakeByRefType(),
                    typeof(Vector3).MakeByRefType(),
                    typeof(float).MakeByRefType(),
                });

        private static readonly MethodInfo MI_PrepareMainhandDraw =
            AccessTools.Method(
                typeof(PawnRenderUtility_DrawEquipmentAndApparelExtras),
                nameof(PrepareMainHand),
                new[]
                {
                    typeof(Pawn),
                    typeof(ThingWithComps),
                    typeof(Vector3),
                    typeof(float),
                    typeof(ThingWithComps).MakeByRefType(),
                    typeof(Vector3).MakeByRefType(),
                    typeof(float).MakeByRefType(),
                });

        [ThreadStatic] private static Pawn _ctxPawn;


        public static bool Prefix(Pawn pawn, Vector3 drawPos, Rot4 facing, PawnRenderFlags flags)
        {
            _ctxPawn = pawn;
            return true;
        }

        public static IEnumerable<CodeInstruction> Transpiler(MethodBase __originalMethod, ILGenerator il,
            IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            var pawnArgIndex = FindPawnArgumentIndex(__originalMethod);
            if (pawnArgIndex < 0)
            {
                Log.Error("Couldn't find Pawn while trying to init Dual Wield, sad ;(");
                return codes;
            }

            var pawn =
                AccessTools.Field(typeof(PawnRenderUtility_DrawEquipmentAndApparelExtras), nameof(_ctxPawn));

            // OG
            var lb_eq = il.DeclareLocal(typeof(ThingWithComps));
            var lb_drawLoc = il.DeclareLocal(typeof(Vector3));
            var lb_aimAngle = il.DeclareLocal(typeof(float));

            // Main Hand Refs
            var lb_mainEq = il.DeclareLocal(typeof(ThingWithComps));
            var lb_mainPos = il.DeclareLocal(typeof(Vector3));
            var lb_mainAngle = il.DeclareLocal(typeof(float));

            // Offhand Refs
            var lb_offEq = il.DeclareLocal(typeof(ThingWithComps));
            var lb_offPos = il.DeclareLocal(typeof(Vector3));
            var lb_offAngle = il.DeclareLocal(typeof(float));

            for (int i = 0; i < codes.Count; i++)
            {
                var ci = codes[i];

                if (ci.opcode != OpCodes.Call || !(ci.operand is MethodInfo mi) ||
                    mi != MI_DrawEquipmentAiming) continue;

                // Patch draw location for Main hand
                var injectedBefore = new List<CodeInstruction>
                {
                    new CodeInstruction(OpCodes.Stloc, lb_aimAngle),
                    new CodeInstruction(OpCodes.Stloc, lb_drawLoc),
                    new CodeInstruction(OpCodes.Stloc, lb_eq),

                    LoadArg(pawnArgIndex),
                    new CodeInstruction(OpCodes.Ldloc, lb_eq),
                    new CodeInstruction(OpCodes.Ldloc, lb_drawLoc),
                    new CodeInstruction(OpCodes.Ldloc, lb_aimAngle),

                    new CodeInstruction(OpCodes.Ldloca_S, lb_mainEq),
                    new CodeInstruction(OpCodes.Ldloca_S, lb_mainPos),
                    new CodeInstruction(OpCodes.Ldloca_S, lb_mainAngle),
                    new CodeInstruction(OpCodes.Call, MI_PrepareMainhandDraw),

                    new CodeInstruction(OpCodes.Ldloc, lb_mainEq),
                    new CodeInstruction(OpCodes.Ldloc, lb_mainPos),
                    new CodeInstruction(OpCodes.Ldloc, lb_mainAngle),
                };

                codes.InsertRange(i, injectedBefore);
                i += injectedBefore.Count;

                // Optionally draw offhand, if they have one
                var skipLabel = il.DefineLabel();
                var originalNext = (i + 1 < codes.Count) ? codes[i + 1] : null;
                var preservedNextLabels =
                    originalNext != null ? new List<Label>(originalNext.labels) : new List<Label>();
                if (originalNext != null) originalNext.labels.Clear();

                var injectedAfter = new List<CodeInstruction>
                {
                    LoadArg(pawnArgIndex),
                    new CodeInstruction(OpCodes.Ldloc, lb_eq),
                    new CodeInstruction(OpCodes.Ldloc, lb_drawLoc),
                    new CodeInstruction(OpCodes.Ldloc, lb_aimAngle),

                    new CodeInstruction(OpCodes.Ldloca_S, lb_offEq),
                    new CodeInstruction(OpCodes.Ldloca_S, lb_offPos),
                    new CodeInstruction(OpCodes.Ldloca_S, lb_offAngle),

                    new CodeInstruction(OpCodes.Call, MI_PrepareOffhandDraw),
                    new CodeInstruction(OpCodes.Brfalse_S, skipLabel),

                    new CodeInstruction(OpCodes.Ldloc, lb_offEq),
                    new CodeInstruction(OpCodes.Ldloc, lb_offPos),
                    new CodeInstruction(OpCodes.Ldloc, lb_offAngle),
                    new CodeInstruction(OpCodes.Call, MI_DrawEquipmentAiming),
                };

                var skipNop = new CodeInstruction(OpCodes.Nop);
                skipNop.labels.Add(skipLabel);
                foreach (var lab in preservedNextLabels) skipNop.labels.Add(lab);

                codes.InsertRange(i + 1, injectedAfter);
                codes.Insert(i + 1 + injectedAfter.Count, skipNop);
                i += injectedAfter.Count + 1;
            }

            return codes;
        }
        public static bool PrepareOffhandDraw(
            Pawn pawn,
            ThingWithComps mainEq,
            Vector3 drawLoc,
            float aimAngle,
            out ThingWithComps offEq,
            out Vector3 offDrawPos,
            out float offDrawAngle)
        {
            offEq = null;
            offDrawPos = default;
            offDrawAngle = default;

            if (pawn?.equipment == null) return false;

            if (!pawn.equipment.TryGetOffHandEquipment(out var offHandEquip) || offHandEquip == null)
                return false;

            if (offHandEquip == pawn.equipment.Primary)
                return false;

            var mainHandAngle = aimAngle;
            var offHandAngle = aimAngle;

            var mainStance = pawn.stances?.curStance as Stance_Busy;

            Stance_Busy offHandStance = null;
            var offHandStances = pawn.GetStancesOffHand();
            if (offHandStances != null)
                offHandStance = offHandStances.curStance as Stance_Busy;

            var focusTarget = LocalTargetInfo.Invalid;
            if (mainStance != null && !mainStance.neverAimWeapon)
                focusTarget = mainStance.focusTarg;
            else if (offHandStance != null && !offHandStance.neverAimWeapon)
                focusTarget = offHandStance.focusTarg;

            var mainHandAiming = CurrentlyAiming(mainStance);
            var offHandAiming = CurrentlyAiming(offHandStance);

            Vector3 offsetMainHand = default;
            Vector3 offsetOffHand = default;

            SetAnglesAndOffsets(mainEq, offHandEquip, aimAngle, pawn,
                ref offsetMainHand, ref offsetOffHand,
                ref mainHandAngle, ref offHandAngle,
                mainHandAiming, offHandAiming);

            if ((offHandAiming || mainHandAiming) && focusTarget != null)
            {
                offHandAngle = GetAimingRotation(pawn, focusTarget);

                // Make sure offhand renders “on top”
                offsetOffHand.y += 0.1f;
            }

            offEq = offHandEquip;
            offDrawPos = drawLoc + offsetOffHand;
            offDrawAngle = offHandAngle;
            return true;
        }

        public static void PrepareMainHand(Pawn pawn,
            ThingWithComps eq,
            Vector3 drawLoc,
            float aimAngle,
            out ThingWithComps mainEq,
            out Vector3 mainDrawPos,
            out float mainDrawAngle)
        {
            mainEq = eq;
            mainDrawPos = drawLoc;
            mainDrawAngle = aimAngle;

            var mainHandAngle = aimAngle;
            var offHandAngle = aimAngle;

            Vector3 offsetMainHand = default;
            Vector3 offsetOffHand = default;
            var mainStance = pawn.stances?.curStance as Stance_Busy;

            Stance_Busy offHandStance = null;
            var offHandStances = pawn.GetStancesOffHand();
            if (offHandStances != null)
                offHandStance = offHandStances.curStance as Stance_Busy;
            var mainHandAiming = CurrentlyAiming(mainStance);
            var offHandAiming = CurrentlyAiming(offHandStance);
            if (!pawn.equipment.TryGetOffHandEquipment(out var offHandEquip) || offHandEquip == null)
                return;
            SetAnglesAndOffsets(eq, offHandEquip, aimAngle, pawn,
                ref offsetMainHand, ref offsetOffHand,
                ref mainHandAngle, ref offHandAngle,
                mainHandAiming, offHandAiming);

            mainEq = eq;
            mainDrawPos = drawLoc + offsetMainHand;
            mainDrawAngle = mainHandAngle;
        }

        private static int FindPawnArgumentIndex(MethodBase original)
        {
            var parameters = original.GetParameters();

            var baseIndex = original.IsStatic ? 0 : 1;

            for (var p = 0; p < parameters.Length; p++)
            {
                if (parameters[p].ParameterType == typeof(Pawn))
                    return baseIndex + p;
            }

            return -1;
        }

        private static CodeInstruction LoadArg(int index)
        {
            // Use the short forms where possible
            switch (index)
            {
                case 0:
                    return new CodeInstruction(OpCodes.Ldarg_0);
                case 1:
                    return new CodeInstruction(OpCodes.Ldarg_1);
                case 2:
                    return new CodeInstruction(OpCodes.Ldarg_2);
                case 3:
                    return new CodeInstruction(OpCodes.Ldarg_3);
                default:
                    return new CodeInstruction(OpCodes.Ldarg_S, (byte)index);
            }
        }

        private static void SetAnglesAndOffsets(Thing eq, ThingWithComps offHandEquip, float aimAngle, Pawn pawn,
            ref Vector3 offsetMainHand, ref Vector3 offsetOffHand, ref float mainHandAngle, ref float offHandAngle,
            bool mainHandAiming, bool offHandAiming)
        {
            var offHandIsMelee = IsMeleeWeapon(offHandEquip);
            var mainHandIsMelee = IsMeleeWeapon(pawn.equipment.Primary);
            var meleeAngleFlipped = DualWield.Settings.MeleeMirrored
                ? 360 - DualWield.Settings.MeleeAngle
                : DualWield.Settings.MeleeAngle;
            var rangedAngleFlipped = DualWield.Settings.RangedMirrored
                ? 360 - DualWield.Settings.RangedAngle
                : DualWield.Settings.RangedAngle;

            if (pawn.Rotation == Rot4.East)
            {
                offsetOffHand.y = -1f;
                offsetOffHand.z = 0.1f;
            }
            else if (pawn.Rotation == Rot4.West)
            {
                offsetMainHand.y = -1f;
                //zOffsetMain = 0.25f;
                offsetOffHand.z = -0.1f;
            }
            else if (pawn.Rotation == Rot4.North)
            {
                if (!mainHandAiming && !offHandAiming)
                {
                    offsetMainHand.x =
                        mainHandIsMelee ? DualWield.Settings.MeleeXOffset : DualWield.Settings.RangedXOffset;
                    offsetOffHand.x = offHandIsMelee
                        ? -DualWield.Settings.MeleeXOffset
                        : -DualWield.Settings.RangedXOffset;
                    offsetMainHand.z =
                        mainHandIsMelee ? DualWield.Settings.MeleeZOffset : DualWield.Settings.RangedZOffset;
                    offsetOffHand.z = offHandIsMelee
                        ? -DualWield.Settings.MeleeZOffset
                        : -DualWield.Settings.RangedZOffset;
                    offHandAngle = offHandIsMelee ? DualWield.Settings.MeleeAngle : DualWield.Settings.RangedAngle;
                    mainHandAngle = mainHandIsMelee ? meleeAngleFlipped : rangedAngleFlipped;

                }
                else
                {
                    offsetOffHand.x = -0.1f;
                }
            }
            else
            {
                if (!mainHandAiming && !offHandAiming)
                {
                    offsetMainHand.y = 1f;
                    offsetMainHand.x = mainHandIsMelee
                        ? -DualWield.Settings.MeleeXOffset
                        : -DualWield.Settings.RangedXOffset;
                    offsetOffHand.x =
                        offHandIsMelee ? DualWield.Settings.MeleeXOffset : DualWield.Settings.RangedXOffset;
                    offsetMainHand.z = mainHandIsMelee
                        ? -DualWield.Settings.MeleeZOffset
                        : -DualWield.Settings.RangedZOffset;
                    offsetOffHand.z =
                        offHandIsMelee ? DualWield.Settings.MeleeZOffset : DualWield.Settings.RangedZOffset;
                    offHandAngle = offHandIsMelee ? meleeAngleFlipped : rangedAngleFlipped;
                    mainHandAngle = mainHandIsMelee ? DualWield.Settings.MeleeAngle : DualWield.Settings.RangedAngle;
                }
                else
                {
                    offsetOffHand.x = 0.1f;
                }
            }

            if (pawn.Rotation.IsHorizontal) return;

            if (DualWield.Settings.CustomRotations.TryGetValue((offHandEquip.def.defName), out var offHandValue))
            {
                offHandAngle += pawn.Rotation == Rot4.North
                    ? offHandValue.extraRotation
                    : -offHandValue.extraRotation;
                //offHandAngle %= 360;
            }

            if (DualWield.Settings.CustomRotations.TryGetValue((eq.def.defName), out var mainHandValue))
            {
                mainHandAngle += pawn.Rotation == Rot4.North
                    ? -mainHandValue.extraRotation
                    : mainHandValue.extraRotation;
                //mainHandAngle %= 360;
            }
        }


        private static float GetAimingRotation(Pawn pawn, LocalTargetInfo focusTarg)
        {
            var pos = focusTarg.HasThing ? focusTarg.Thing.DrawPos : focusTarg.Cell.ToVector3Shifted();

            var angle = 0f;
            if ((pos - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
            {
                angle = (pos - pawn.DrawPos).AngleFlat();
            }

            return angle;
        }

        private static bool CurrentlyAiming(Stance_Busy stance)
        {
            return stance != null && !stance.neverAimWeapon && stance.focusTarg.IsValid;
        }

        private static bool IsMeleeWeapon(ThingWithComps eq)
        {
            if (!(eq?.TryGetComp<CompEquippable>() is CompEquippable ceq)) return false;

            return ceq.PrimaryVerb.IsMeleeAttack;
        }
    }
}
