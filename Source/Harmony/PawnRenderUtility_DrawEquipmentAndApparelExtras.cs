using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using DualWield.Settings;
using HarmonyLib;
using UnityEngine;
using Verse;

namespace DualWield.Harmony
{ 


    [HarmonyPatch(typeof(UIRoot_Entry), "Init")]
    public static class UIRoot_Entry_Init_IncompatibleModifications_Patch
    {
        [HarmonyPostfix]
        public static void Postfix()
        {
            if (AccessTools.TypeByName("Tacticowl.ModSettings_Tacticowl") != null)
            {
                UIRoot_Entry_Init_IncompatibleModifications_Patch.CreateHereticalModificationsDialog();
            }
        }
        public static void CreateHereticalModificationsDialog()
        {
            string text = "Dual Wield";
            text += "\n\n";
            text += "Incompatible version of Run and Gun detected, please use either the one linked on this mods page, or Roolo's.";
            Find.WindowStack.Add(new Dialog_MessageBox(text, null, null, null, null, null, false, null, null, WindowLayer.Dialog));
            UIRoot_Entry_Init_IncompatibleModifications_Patch.dialogDone = true;
        }
        private static bool dialogDone;
    }


    //[HarmonyPatch(typeof(PawnRenderUtility))]
    //[HarmonyPatch(nameof(PawnRenderUtility.DrawEquipmentAndApparelExtras))]
    public class PawnRenderUtility_DrawEquipmentAndApparelExtras
    {

        [ThreadStatic] private static Pawn _ctxPawn;
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo drawCarriedWeapon = AccessTools.Method(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawCarriedWeapon));
            MethodInfo drawCarriedWeaponAfter = AccessTools.Method(typeof(PawnRenderUtility_DrawEquipmentAndApparelExtras), nameof(DrawCarriedOffhand));

            MethodInfo drawEquipmentAiming = AccessTools.Method(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAiming));
            MethodInfo drawEquipmentAimingModified = AccessTools.Method(typeof(PawnRenderUtility_DrawEquipmentAndApparelExtras), nameof(DrawEquipmentModified));
            var instructionsList = new List<CodeInstruction>(instructions);
            for (int i = 0; i < instructionsList.Count; i++)
            {
                CodeInstruction instruction = instructionsList[i];
                if (instruction.OperandIs(drawEquipmentAiming))
                {
                        if (drawEquipmentAimingModified != null)
                        {
                            yield return new CodeInstruction(OpCodes.Ldarg_0);
                            instruction = new CodeInstruction(OpCodes.Call, drawEquipmentAimingModified);
                        }
                }

                if (instruction.Calls(drawCarriedWeapon))
                {
                    yield return new CodeInstruction(OpCodes.Call, drawCarriedWeaponAfter);
                    continue;
                }

                yield return instruction;
            }
        }

        public static bool Prefix(Pawn pawn, Vector3 drawPos, Rot4 facing, PawnRenderFlags flags)
        {
            _ctxPawn = pawn;
            return true;
        }

        public static void DrawCarriedOffhand(
            ThingWithComps weapon,
            Vector3 drawPos,
            Rot4 facing,
            float equipmentDrawDistanceFactor)
        {
            float drawDistanceFactor = _ctxPawn.ageTracker.CurLifeStage.equipmentDrawDistanceFactor;

            //PawnRenderUtility.DrawCarriedWeapon(offHandEquip, drawPos, facing, drawDistanceFactor);
            int aimAngle = 143;
            switch (facing.AsInt)
            {
                case 0:
                    drawPos += new Vector3(0.0f, 0.0f, -0.11f) * drawDistanceFactor;
                    break;
                case 1:
                    drawPos += new Vector3(0.22f, 0.0f, -0.22f) * drawDistanceFactor;
                    break;
                case 2:
                    drawPos += new Vector3(0.0f, 0.0f, -0.22f) * drawDistanceFactor;
                    break;
                case 3:
                    drawPos += new Vector3(-0.22f, 0.0f, -0.22f) * drawDistanceFactor;
                    aimAngle = 217;
                    break;
            }
            DrawEquipmentModified(_ctxPawn.equipment.Primary, drawPos, aimAngle, _ctxPawn);
        }

        public static bool ShouldPatch(Thing eq, Vector3 drawLoc, float aimAngle, Pawn pawn)
        {
            ThingWithComps offHandEquip = null;
            if (pawn.equipment == null)
            {
                return false;
            }
            if (pawn.equipment.TryGetOffHandEquipment(out ThingWithComps result))
            {
                offHandEquip = result;
            }
            if (offHandEquip == null)
            {
                PawnRenderUtility.DrawEquipmentAiming(eq, drawLoc, aimAngle);
                return false;
            }

            return true;
        }

        public static void DrawEquipmentModified(Thing eq, Vector3 drawLoc, float aimAngle, Pawn pawn)
        {
            ThingWithComps offHandEquip = null;
            if (pawn.equipment == null)
            {
                PawnRenderUtility.DrawEquipmentAiming(eq, drawLoc, aimAngle);
                return;
            }
            if (pawn.equipment.TryGetOffHandEquipment(out ThingWithComps result))
            {
                offHandEquip = result;
            }
            if (offHandEquip == null)
            {
                PawnRenderUtility.DrawEquipmentAiming(eq, drawLoc, aimAngle);
                return;
            }
            float mainHandAngle = aimAngle;
            float offHandAngle = aimAngle;
            Stance_Busy mainStance = pawn.stances.curStance as Stance_Busy;
            Stance_Busy offHandStance = null;
            if (pawn.GetStancesOffHand() != null)
            {
                var test = pawn.GetStancesOffHand().curStance;
                offHandStance = pawn.GetStancesOffHand().curStance as Stance_Busy;
            }
            LocalTargetInfo focusTarg = null;
            if (mainStance != null && !mainStance.neverAimWeapon)
            {
                focusTarg = mainStance.focusTarg;
            }
            else if (offHandStance != null && !offHandStance.neverAimWeapon)
            {
                focusTarg = offHandStance.focusTarg;
            }

            bool mainHandAiming = CurrentlyAiming(mainStance);
            bool offHandAiming = CurrentlyAiming(offHandStance);

            Vector3 offsetMainHand = new Vector3();
            Vector3 offsetOffHand = new Vector3();
            //bool currentlyAiming = (mainStance != null && !mainStance.neverAimWeapon && mainStance.focusTarg.IsValid) || stancesOffHand.curStance is Stance_Busy ohs && !ohs.neverAimWeapon && ohs.focusTarg.IsValid;
            //When wielding offhand weapon, facing south, and not aiming, draw differently 

            SetAnglesAndOffsets(eq, offHandEquip, aimAngle, pawn, ref offsetMainHand, ref offsetOffHand, ref mainHandAngle, ref offHandAngle, mainHandAiming, offHandAiming);

            if (offHandEquip != pawn.equipment.Primary)
            {
                //drawLoc += offsetMainHand;
                //aimAngle = mainHandAngle;
                //__instance.DrawEquipmentAiming(eq, drawLoc + offsetMainHand, mainHandAngle);
                PawnRenderUtility.DrawEquipmentAiming(eq, drawLoc + offsetMainHand, mainHandAngle);
            }
            if ((offHandAiming || mainHandAiming) && focusTarg != null)
            {
                offHandAngle = GetAimingRotation(pawn, focusTarg);
                offsetOffHand.y += 0.1f;
                Vector3 adjustedDrawPos = pawn.DrawPos + new Vector3(0f, 0f, 0.4f).RotatedBy(offHandAngle) + offsetOffHand;
                PawnRenderUtility.DrawEquipmentAiming(offHandEquip, adjustedDrawPos, offHandAngle);
            }
            else
            {
                PawnRenderUtility.DrawEquipmentAiming(offHandEquip, drawLoc + offsetOffHand, offHandAngle);
            }
        }

        private static void SetAnglesAndOffsets(Thing eq, ThingWithComps offHandEquip, float aimAngle, Pawn pawn, ref Vector3 offsetMainHand, ref Vector3 offsetOffHand, ref float mainHandAngle, ref float offHandAngle, bool mainHandAiming, bool offHandAiming)
        {
            bool offHandIsMelee = IsMeleeWeapon(offHandEquip);
            bool mainHandIsMelee = IsMeleeWeapon(pawn.equipment.Primary);
            float meleeAngleFlipped = DualWield.Settings.MeleeMirrored ? 360 - DualWield.Settings.MeleeAngle : DualWield.Settings.MeleeAngle;
            float rangedAngleFlipped = DualWield.Settings.RangedMirrored ? 360 - DualWield.Settings.RangedAngle : DualWield.Settings.RangedAngle;

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
                    offsetMainHand.x = mainHandIsMelee ? DualWield.Settings.MeleeXOffset : DualWield.Settings.RangedXOffset;
                    offsetOffHand.x = offHandIsMelee ? -DualWield.Settings.MeleeXOffset : -DualWield.Settings.RangedXOffset;
                    offsetMainHand.z = mainHandIsMelee ? DualWield.Settings.MeleeZOffset : DualWield.Settings.RangedZOffset;
                    offsetOffHand.z = offHandIsMelee ? -DualWield.Settings.MeleeZOffset : -DualWield.Settings.RangedZOffset;
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
                    offsetMainHand.x = mainHandIsMelee ? -DualWield.Settings.MeleeXOffset : -DualWield.Settings.RangedXOffset;
                    offsetOffHand.x = offHandIsMelee ? DualWield.Settings.MeleeXOffset : DualWield.Settings.RangedXOffset;
                    offsetMainHand.z = mainHandIsMelee ? -DualWield.Settings.MeleeZOffset : -DualWield.Settings.RangedZOffset;
                    offsetOffHand.z = offHandIsMelee ? DualWield.Settings.MeleeZOffset : DualWield.Settings.RangedZOffset;
                    offHandAngle = offHandIsMelee ? meleeAngleFlipped : rangedAngleFlipped;
                    mainHandAngle = mainHandIsMelee ? DualWield.Settings.MeleeAngle : DualWield.Settings.RangedAngle;
                }
                else
                {
                    offsetOffHand.x = 0.1f;
                }
            }
            if (!pawn.Rotation.IsHorizontal)
            {
                if (DualWield.Settings.CustomRotations.TryGetValue((offHandEquip.def.defName), out Record offHandValue))
                {
                    offHandAngle += pawn.Rotation == Rot4.North ? offHandValue.extraRotation : -offHandValue.extraRotation;
                    //offHandAngle %= 360;
                }
                if (DualWield.Settings.CustomRotations.TryGetValue((eq.def.defName), out Record mainHandValue))
                {
                    mainHandAngle += pawn.Rotation == Rot4.North ? -mainHandValue.extraRotation : mainHandValue.extraRotation;
                    //mainHandAngle %= 360;
                }
            }
        }

        private static float GetAimingRotation(Pawn pawn, LocalTargetInfo focusTarg)
        {
            Vector3 a;
            if (focusTarg.HasThing)
            {
                a = focusTarg.Thing.DrawPos;
            }
            else
            {
                a = focusTarg.Cell.ToVector3Shifted();
            }
            float num = 0f;
            if ((a - pawn.DrawPos).MagnitudeHorizontalSquared() > 0.001f)
            {
                num = (a - pawn.DrawPos).AngleFlat();
            }

            return num;
        }
        private static bool CurrentlyAiming(Stance_Busy stance)
        {
            return (stance != null && !stance.neverAimWeapon && stance.focusTarg.IsValid);
        }
        private static bool IsMeleeWeapon(ThingWithComps eq)
        {
            if (eq == null)
            {
                return false;
            }
            if (eq.TryGetComp<CompEquippable>() is CompEquippable ceq)
            {
                if (ceq.PrimaryVerb.IsMeleeAttack)
                {
                    return true;
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(PawnRenderUtility))]
    [HarmonyPatch(nameof(PawnRenderUtility.DrawEquipmentAndApparelExtras))]
    public static class Patch_DualWield_DrawEquipmentAiming_Transpiler
    {
        private static readonly MethodInfo MI_DrawEquipmentAiming =
            AccessTools.Method(typeof(PawnRenderUtility), nameof(PawnRenderUtility.DrawEquipmentAiming),
                new[] { typeof(ThingWithComps), typeof(Vector3), typeof(float) });

        private static readonly MethodInfo MI_ShouldDoOffhandWork =
            AccessTools.Method(typeof(Patch_DualWield_DrawEquipmentAiming_Transpiler), nameof(ShouldDoOffhandWork));

        private static readonly MethodInfo MI_PrepareOffhandDraw =
            AccessTools.Method(
                typeof(Patch_DualWield_DrawEquipmentAiming_Transpiler),
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
                typeof(Patch_DualWield_DrawEquipmentAiming_Transpiler),
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

        private static readonly MethodInfo MI_AdjustMainHandDrawLoc =
            AccessTools.Method(
                typeof(Patch_DualWield_DrawEquipmentAiming_Transpiler),
                nameof(AdjustMainHandDrawLoc),
                new[] { typeof(Pawn), typeof(Vector3) });

        /// <summary>
        /// Return true if this pawn should do offhand rendering work at this point.
        /// Keep this fast: it’s executed in the render path.
        /// </summary>
        public static bool ShouldDoOffhandWork(Pawn pawn)
        {
            if (pawn == null) return false;
            if (pawn.equipment == null) return false;

            // Your mod's API:
            // return pawn.equipment.TryGetOffHandEquipment(out _);
            // If you want to avoid out var allocation, just do the out.
            return pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offhand) && offhand != null;
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

            // Bail out safely
            if (pawn == null) return false;
            if (pawn.equipment == null) return false;

            if (!pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip) || offHandEquip == null)
                return false;

            // Avoid accidental double-draw of the same instance
            if (offHandEquip == pawn.equipment.Primary)
                return false;

            float mainHandAngle = aimAngle;
            float offHandAngle = aimAngle;

            Stance_Busy mainStance = pawn.stances?.curStance as Stance_Busy;

            Stance_Busy offHandStance = null;
            var offHandStances = pawn.GetStancesOffHand();
            if (offHandStances != null)
                offHandStance = offHandStances.curStance as Stance_Busy;

            LocalTargetInfo focusTarg = LocalTargetInfo.Invalid;
            if (mainStance != null && !mainStance.neverAimWeapon)
                focusTarg = mainStance.focusTarg;
            else if (offHandStance != null && !offHandStance.neverAimWeapon)
                focusTarg = offHandStance.focusTarg;

            bool mainHandAiming = CurrentlyAiming(mainStance);
            bool offHandAiming = CurrentlyAiming(offHandStance);

            Vector3 offsetMainHand = default;
            Vector3 offsetOffHand = default;

            // Your existing helper (unchanged)
            SetAnglesAndOffsets(mainEq, offHandEquip, aimAngle, pawn,
                ref offsetMainHand, ref offsetOffHand,
                ref mainHandAngle, ref offHandAngle,
                mainHandAiming, offHandAiming);

            // Compute the final draw params for the offhand (NO drawing here)
            if ((offHandAiming || mainHandAiming) && focusTarg != null)
            {
                offHandAngle = GetAimingRotation(pawn, focusTarg);

                // Make sure offhand renders “on top”
                offsetOffHand.y += 0.1f;

                Vector3 adjustedDrawPos = pawn.DrawPos
                    + new Vector3(0f, 0f, 0.4f).RotatedBy(offHandAngle)
                    + offsetOffHand;

                offEq = offHandEquip;
                offDrawPos = adjustedDrawPos;
                offDrawAngle = offHandAngle;
                return true;
            }
            else
            {
                offEq = offHandEquip; 
                offDrawPos = drawLoc + offsetOffHand;
                offDrawAngle = offHandAngle;
                return true;
            }
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

            float mainHandAngle = aimAngle;
            float offHandAngle = aimAngle;

            Vector3 offsetMainHand = default;
            Vector3 offsetOffHand = default;
            Stance_Busy mainStance = pawn.stances?.curStance as Stance_Busy;

            Stance_Busy offHandStance = null;
            var offHandStances = pawn.GetStancesOffHand();
            if (offHandStances != null)
                offHandStance = offHandStances.curStance as Stance_Busy;
            bool mainHandAiming = CurrentlyAiming(mainStance);
            bool offHandAiming = CurrentlyAiming(offHandStance);
            if (!pawn.equipment.TryGetOffHandEquipment(out ThingWithComps offHandEquip) || offHandEquip == null)
                return;
            SetAnglesAndOffsets(eq, offHandEquip, aimAngle, pawn,
                ref offsetMainHand, ref offsetOffHand,
                ref mainHandAngle, ref offHandAngle,
                mainHandAiming, offHandAiming);

            mainEq = eq;
            mainDrawPos = drawLoc + offsetMainHand;
            mainDrawAngle = mainHandAngle;
        }

        public static Vector3 AdjustMainHandDrawLoc(Pawn pawn, Vector3 drawPos)
        {
            if (pawn?.ageTracker?.CurLifeStage == null)
                return drawPos;

            float drawDistanceFactor = pawn.ageTracker.CurLifeStage.equipmentDrawDistanceFactor;

            switch (pawn.Rotation.AsInt)
            {
                case 0:
                    drawPos += new Vector3(0.0f, 0.0f, -0.11f) * drawDistanceFactor;
                    break;
                case 1:
                    drawPos += new Vector3(0.22f, 0.0f, -0.22f) * drawDistanceFactor;
                    break;
                case 2:
                    drawPos += new Vector3(0.0f, 0.0f, -0.22f) * drawDistanceFactor;
                    break;
                case 3:
                    drawPos += new Vector3(-0.22f, 0.0f, -0.22f) * drawDistanceFactor;
                    break;
            }

            return drawPos;
        }


        public static IEnumerable<CodeInstruction> Transpiler(MethodBase __originalMethod, ILGenerator il,
            IEnumerable<CodeInstruction> instructions)
        {
            var codes = new List<CodeInstruction>(instructions);

            int pawnArgIndex = FindPawnArgumentIndex(__originalMethod);
            if (pawnArgIndex < 0)
            {
                Log.Warning(
                    $"[YourMod] Could not find Pawn parameter in {__originalMethod.DeclaringType?.FullName}.{__originalMethod.Name}. Patch skipped.");
                return codes;
            }

            // Locals to store original call args
            LocalBuilder lb_eq = il.DeclareLocal(typeof(ThingWithComps));
            LocalBuilder lb_drawLoc = il.DeclareLocal(typeof(Vector3));
            LocalBuilder lb_aimAngle = il.DeclareLocal(typeof(float));

            LocalBuilder lb_drawLocRaw = il.DeclareLocal(typeof(Vector3));   // original drawLoc as it existed on the stack
            LocalBuilder lb_drawLocMain = il.DeclareLocal(typeof(Vector3));  // adjusted drawLoc for the original call only

            LocalBuilder lb_mainEq = il.DeclareLocal(typeof(ThingWithComps));
            LocalBuilder lb_mainPos = il.DeclareLocal(typeof(Vector3));
            LocalBuilder lb_mainAngle = il.DeclareLocal(typeof(float));

            // Locals for offhand draw outputs
            LocalBuilder lb_offEq = il.DeclareLocal(typeof(ThingWithComps));
            LocalBuilder lb_offPos = il.DeclareLocal(typeof(Vector3));
            LocalBuilder lb_offAngle = il.DeclareLocal(typeof(float));

            for (int i = 0; i < codes.Count; i++)
            {
                var ci = codes[i];

                if (ci.opcode == OpCodes.Call && ci.operand is MethodInfo mi && mi == MI_DrawEquipmentAiming)
                {

                    // BEFORE call: stash args then reload them so the original call remains in place
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

                    // The original call remains at codes[i]
                    var skipLabel = il.DefineLabel();
                    var originalNext = (i + 1 < codes.Count) ? codes[i + 1] : null;
                    var preservedNextLabels = originalNext != null ? new List<Label>(originalNext.labels) : new List<Label>();
                    if (originalNext != null) originalNext.labels.Clear();
                    // AFTER call: call PrepareOffhandDraw(...) -> if true, emit second DrawEquipmentAiming call
                    var injectedAfter = new List<CodeInstruction>
                    {
                        // bool PrepareOffhandDraw(pawn, eq, drawLoc, aimAngle, out offEq, out offPos, out offAngle)
                        LoadArg(pawnArgIndex),
                        new CodeInstruction(OpCodes.Ldloc, lb_eq),
                        new CodeInstruction(OpCodes.Ldloc, lb_drawLoc),
                        new CodeInstruction(OpCodes.Ldloc, lb_aimAngle),

                        new CodeInstruction(OpCodes.Ldloca_S, lb_offEq),
                        new CodeInstruction(OpCodes.Ldloca_S, lb_offPos),
                        new CodeInstruction(OpCodes.Ldloca_S, lb_offAngle),

                        new CodeInstruction(OpCodes.Call, MI_PrepareOffhandDraw),
                        new CodeInstruction(OpCodes.Brfalse_S, skipLabel),

                        // *** SECOND CALL PRESENT IN IL ***
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

                    // If you only want to patch the first call site, uncomment:
                    // break;
                }
            }

            return codes;
        }

        private static int FindPawnArgumentIndex(MethodBase original)
        {
            var parameters = original.GetParameters();

            // For instance methods: arg0 is "this", real params start at 1
            // For static methods: arg0 is first param
            int baseIndex = original.IsStatic ? 0 : 1;

            for (int p = 0; p < parameters.Length; p++)
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

        // --------------------------------------------------------------------
        // The helpers below are placeholders — you already have equivalents.
        // Keep/replace them with your existing implementations.
        // --------------------------------------------------------------------

        private static bool CurrentlyAiming(Stance_Busy stance)
        {
            if (stance == null) return false;
            if (stance.neverAimWeapon) return false;
            return stance.focusTarg.IsValid;
        }

        private static float GetAimingRotation(Pawn pawn, LocalTargetInfo target)
        {
            // Replace with your real method; this is a very rough placeholder.
            Vector3 a = pawn.DrawPos;
            Vector3 b = target.Cell.ToVector3Shifted();
            Vector3 delta = b - a;
            return delta.AngleFlat();
        }

        private static void SetAnglesAndOffsets(
            ThingWithComps mainEq, ThingWithComps offEq, float aimAngle, Pawn pawn,
            ref Vector3 offsetMain, ref Vector3 offsetOff,
            ref float mainAngle, ref float offAngle,
            bool mainAiming, bool offAiming)
        {
            // Replace with your real implementation.
            // This placeholder merely separates the weapons a bit.
            offsetMain = new Vector3(0.05f, 0f, 0f);
            offsetOff = new Vector3(-0.05f, 0f, 0f);
            mainAngle = aimAngle;
            offAngle = aimAngle;
        }
    }
}
