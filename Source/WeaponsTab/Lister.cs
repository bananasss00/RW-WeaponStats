using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace WeaponStats
{
    public static class Lister
    {
        // Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.Weapon)
        // Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.Apparel)
        // Find.CurrentMap.listerThings.ThingsInGroup(ThingRequestGroup.Corpse)
        // thing => thing.def.IsWeapon && thing.def.tradeability.TraderCanSell()

        // In HSK ThingsInGroup return 0 elements
        // def.IsWeapon & def.IsApparel work fine for vanilla and HSK

        public static IEnumerable<Thing> Weapons(bool onMap = true)
        {
            var lister = onMap ? Find.CurrentMap.listerThings : All;
            return lister.AllThings.Where(thing => thing.def.IsWeapon);
            //return lister.AllThings.Where(d => d.def.IsWeapon && (d.def.tradeability.TraderCanSell() || (d.def.weaponTags != null && d.def.weaponTags.Contains("TurretGun"))));
        }

        public static IEnumerable<Thing> Apparels(bool onMap = true)
        {
            var lister = onMap ? Find.CurrentMap.listerThings : All;
            return lister.AllThings.Where(thing => thing.def.IsApparel);
        }

        public static IEnumerable<Thing> Corpses()
        {
            var lister = Find.CurrentMap.listerThings;
            return lister.AllThings.Where(thing => thing.def.IsCorpse);
        }

        public static ListerThings All
        {
            get
            {
                if (listerFakeAll == null)
                {
                    listerFakeAll = new ListerThings(ListerThingsUse.Global);
                    foreach (var d in DefDatabase<ThingDef>.AllDefsListForReading.Where(d =>
                        d.IsWeapon || d.IsApparel || d.IsCorpse))
                    {
                        Thing thing = ThingMaker.MakeThing(d, d.MadeFromStuff ? GenStuff.DefaultStuffFor(d) : null);
                        listerFakeAll.Add(thing);
                    }
                }

                return listerFakeAll;
            }
        }

        private static ListerThings listerFakeAll = null;
    }
}
