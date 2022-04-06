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

        public static IEnumerable<Thing> Weapons() => listWeapons;

        public static IEnumerable<Thing> Apparels() => listApparels;

        public static IEnumerable<Thing> Corpses() => listCorpses;

        public static void Update()
        {
            if (NeedUpdate)
            {
                var lister = Find.CurrentMap?.listerThings;
                if (lister == null)
                {
                    listWeapons = new List<Thing>();
                    listApparels = new List<Thing>();
                    listCorpses = new List<Thing>();
                }
                else
                {
                    listWeapons = lister.AllThings.Where(thing => thing.def.IsWeapon).ToList();
                    listApparels = lister.AllThings.Where(thing => thing.def.IsApparel).ToList();
                    listCorpses = lister.AllThings.Where(thing => thing.def.IsCorpse).ToList();
                }
                latestUpdateTick = Find.TickManager.TicksGame;
            }
        }

        private static bool NeedUpdate => Math.Abs(Find.TickManager.TicksGame - latestUpdateTick) >= 60;

        private static int latestUpdateTick;
        private static List<Thing> listWeapons, listApparels, listCorpses;
    }
}
