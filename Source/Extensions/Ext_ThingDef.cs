using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace DualWield
{
    public static class Ext_ThingDef
    {
        public static bool CanBeOffHand(this ThingDef td)
        {
            return td.IsWeapon && Base.Settings.dualWieldSelection != null && Base.Settings.dualWieldSelection.TryGetValue(td.defName, out Settings.Record value) && value.isSelected; 
        }
        public static bool IsTwoHand(this ThingDef td)
        {
            return td.IsWeapon && Base.Settings.twoHandSelection != null && Base.Settings.twoHandSelection.TryGetValue(td.defName, out Settings.Record value) && value.isSelected;
        }
    }
}
