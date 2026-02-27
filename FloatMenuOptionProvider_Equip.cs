using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;

namespace DualWield
{
    public class FloatMenuOptionProvider_Equip : FloatMenuOptionProvider
    {
        protected override bool Drafted => true;
        protected override bool Undrafted => true;
        protected override bool Multiselect => false;
        protected override bool AppliesInt(FloatMenuContext context) => context.FirstSelectedPawn.equipment != null;
        protected override FloatMenuOption GetSingleOptionFor(Thing clickedThing, FloatMenuContext context)
        {
            if (!clickedThing.def.IsWeapon)
                return null;

            if (clickedThing.def.IsWeapon && context.FirstSelectedPawn.story.DisabledWorkTagsBackstoryAndTraits.HasFlag(WorkTags.Violent))
                return new FloatMenuOption("CannotEquip".Translate(clickedThing.LabelShort) + " " + "DW_AsOffHand".Translate() + " (" + "IsIncapableOfViolenceLower".Translate(context.FirstSelectedPawn.LabelShort, context.FirstSelectedPawn) + ")", null);
            if (!context.FirstSelectedPawn.CanReach(clickedThing, PathEndMode.ClosestTouch, Danger.Deadly))
                return new FloatMenuOption("CannotEquip".Translate(clickedThing.LabelShort) + " " + "DW_AsOffHand".Translate() + " (" + "NoPath".Translate() + ")", null);
            if (!context.FirstSelectedPawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                return new FloatMenuOption("CannotEquip".Translate(clickedThing.LabelShort) + " " + "DW_AsOffHand".Translate() + " (" + "Incapable".Translate() + ")", null);
            if (clickedThing.IsBurning())
                return new FloatMenuOption("CannotEquip".Translate(clickedThing.LabelShort) + " " + "DW_AsOffHand".Translate() + " (" + "BurningLower".Translate() + ")", null);
            if (context.FirstSelectedPawn.HasMissingArmOrHand())
                return new FloatMenuOption("CannotEquip".Translate(clickedThing.LabelShort) + " " + "DW_AsOffHand".Translate() + " (" + "DW_MissArmOrHand".Translate() + ")", null);
            if (context.FirstSelectedPawn.equipment != null && context.FirstSelectedPawn.equipment.Primary != null && context.FirstSelectedPawn.equipment.Primary.def.IsTwoHand())
                return new FloatMenuOption("CannotEquip".Translate(clickedThing.LabelShort) + " " + "DW_AsOffHand".Translate() + " (" + "DW_WieldingTwoHanded".Translate() + ")", null);
            if (clickedThing.def.IsTwoHand())
                return new FloatMenuOption("CannotEquip".Translate(clickedThing.LabelShort) + " " + "DW_AsOffHand".Translate() + " (" + "DW_NoTwoHandedInOffHand".Translate() + ")", null);
            if (!clickedThing.def.CanBeOffHand())
                return new FloatMenuOption("CannotEquip".Translate(clickedThing.LabelShort) + " " + "DW_AsOffHand".Translate() + " (" + "DW_CannotBeOffHand".Translate() + ")", null);

            var text = "DW_EquipOffHand".Translate(clickedThing.LabelShort);
            if (clickedThing.def.IsRangedWeapon && context.FirstSelectedPawn.story != null && context.FirstSelectedPawn.story.traits.HasTrait(TraitDefOf.Brawler))
                text += " " + "EquipWarningBrawler".Translate();

            return FloatMenuUtility.DecoratePrioritizedTask(new FloatMenuOption(text, delegate
            {
                FleckMaker.Static(clickedThing.DrawPos, clickedThing.Map, FleckDefOf.FeedbackEquip);
                clickedThing.SetForbidden(false);
                context.FirstSelectedPawn.jobs.TryTakeOrderedJob(new Job(DW_DefOff.DW_EquipOffhand, clickedThing), JobTag.Misc);
                PlayerKnowledgeDatabase.KnowledgeDemonstrated(ConceptDefOf.EquippingWeapons, KnowledgeAmount.Total);
            }, MenuOptionPriority.High), context.FirstSelectedPawn, clickedThing);
        }
    }
}
