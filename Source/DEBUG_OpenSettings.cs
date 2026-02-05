using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LudeonTK;
using RimWorld;
using Verse;

namespace DualWield
{
#if DEBUG
    [StaticConstructorOnStartup]
    internal class DEBUG_OpenSettings
    {
        [DebugAction("DualWield", "Open Dual Wield settings")]
        public static void OpenSettings()
        {
            Find.WindowStack.Add(new Dialog_ModSettings(DualWield.Instance));
        }

    }
#endif
}
