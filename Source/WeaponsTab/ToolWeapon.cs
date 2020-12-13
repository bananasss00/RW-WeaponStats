using Verse;
using RimWorld;

namespace WeaponStats
{
    public class ToolWeapon : Weapon
    {
        public new void fillFromThing(Thing th, bool ce = false)
        {
            base.fillFromThing(th);
            try
            {
                mass = th.GetStatValue(StatDefOf.Mass);
            }
            catch (System.NullReferenceException e)
            {
                this.exceptions.Add(e);
            }
        }
    }
}