using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace WeaponStats
{
	public class RangedWeapon : Weapon
	{
		/* RANGED:
			 * th.def.statBases = MaxHitPoints, Flammability, DeteriorationRate, Beauty, SellPriceFactor, WorkToMake, Mass,
			 * 	AccuracyTouch, AccuracyShort, AccuracyMedium, AccuracyLong, RangedWeapon_Cooldown
			 * th.def.weaponTags = [Gun]
			 *
		*/
		public float accuracyTouch { get; set; }

		public float accuracyShort { get; set; }

		public float accuracyMedium { get; set; }

		public float accuracyLong { get; set; }

		public float minRange { get; set; }

		public float maxRange { get; set; }

		public float warmup { get; set; }

		public int burstShotCount { get; set; }

		public int ticksBetweenBurstShots { get; set; }

		public RangedWeapon () : base ()
		{
			accuracyTouch = 0f;
			accuracyShort = 0f;
			accuracyMedium = 0f;
			accuracyLong = 0f;
			minRange = 0f;
			maxRange = 0f;
			warmup = 0f;
		}

		private float getDps ()
		{
			float burstDamage = this.damage * this.burstShotCount;
			float warmupTicks = (this.cooldown + this.warmup) * TPS;
			float burstTicks = this.burstShotCount * this.ticksBetweenBurstShots;
			float totalTime = (warmupTicks + burstTicks) / TPS;

			return (float)Math.Round (burstDamage / totalTime, 2);
		}

		public string getAccuracyStr ()
		{
			StringBuilder sb = new StringBuilder ();
			if (minRange > RNG_TOUCH || maxRange < RNG_TOUCH) {
				sb.Append (" - /");
			} else {
				sb.Append (" ").Append (Math.Round (accuracyTouch, 1).ToString ()).Append (" /");
			}
			if (minRange > RNG_SHORT || maxRange < RNG_SHORT) {
				sb.Append (" - /");
			} else {
				sb.Append (" ").Append (Math.Round (accuracyShort, 1).ToString ()).Append (" /");
			}
			if (minRange > RNG_MEDIUM || maxRange < RNG_MEDIUM) {
				sb.Append (" - /");
			} else {
				sb.Append (" ").Append (Math.Round (accuracyMedium, 1).ToString ()).Append (" /");
			}
			if (minRange > RNG_LONG || maxRange < RNG_LONG) {
				sb.Append (" -");
			} else {
				sb.Append (" ").Append (Math.Round (accuracyLong, 1).ToString ());
			}
			return sb.ToString ();
		}

		public new void fillFromThing (Thing th)
		{
			base.fillFromThing (th);
			try {
				accuracyTouch = th.GetStatValue (StatDefOf.AccuracyTouch) * 100;
				accuracyShort = th.GetStatValue (StatDefOf.AccuracyShort) * 100;
				accuracyMedium = th.GetStatValue (StatDefOf.AccuracyMedium) * 100;
				accuracyLong = th.GetStatValue (StatDefOf.AccuracyLong) * 100;
				cooldown = th.GetStatValue (StatDefOf.RangedWeapon_Cooldown);
				mass = th.GetStatValue (StatDefOf.Mass);
			} catch (System.NullReferenceException e) {
				this.exceptions.Add (e);
			}

			if (th.def != null && th.def.Verbs != null) {
				try {
					foreach (VerbProperties vp in th.def.Verbs) {
						if (vp.ToString ().StartsWith ("VerbProperties")) {
							warmup = vp.warmupTime;
							maxRange = vp.range;
							minRange = vp.minRange;
							damage = vp.defaultProjectile.projectile.GetDamageAmount (th);
							damageType = vp.defaultProjectile.projectile.damageDef.label;
							if (vp.burstShotCount > 0) {
								this.burstShotCount = vp.burstShotCount;
							} else {
								this.burstShotCount = 1;
							}
							if (vp.ticksBetweenBurstShots > 0) {
								this.ticksBetweenBurstShots = vp.ticksBetweenBurstShots;
							} else {
								this.ticksBetweenBurstShots = 10;
							}
						}
					}
				} catch (System.NullReferenceException e) {
					this.exceptions.Add (e);
				}
			}

			this.dps = this.getDps ();
		}
	}
}
