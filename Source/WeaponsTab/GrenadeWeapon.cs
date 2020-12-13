using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace WeaponStats
{
	public class GrenadeWeapon : Weapon
	{
		public float minRange { get; set; }

		public float maxRange { get; set; }

		public float warmup { get; set; }

		public int explosionDelay { get; set; }

		public float explosionRadius { get; set; }

		public GrenadeWeapon () : base ()
		{
			minRange = 0f;
			maxRange = 0f;
			warmup = 0f;
			explosionDelay = 0;
			explosionRadius = 0f;
		}

		public new void fillFromThing (Thing th, bool ce = false)
		{
			base.fillFromThing (th);
			try {
				cooldown = th.GetStatValue (StatDefOf.RangedWeapon_Cooldown);
				mass = th.GetStatValue (StatDefOf.Mass);
			} catch (System.NullReferenceException e) {
				this.exceptions.Add (e);
			}
			try {
				if (th.def != null && th.def.Verbs != null) {
					foreach (VerbProperties vp in th.def.Verbs) {
						if (vp.ToString ().StartsWith ("VerbProperties")) {
							warmup = vp.warmupTime;
							maxRange = vp.range;
							minRange = vp.minRange;
							damage = vp.defaultProjectile.projectile.GetDamageAmount (th);
							damageType = vp.defaultProjectile.projectile.damageDef.label;
							explosionDelay = vp.defaultProjectile.projectile.explosionDelay;
							explosionRadius = vp.defaultProjectile.projectile.explosionRadius;
							armorPenetration = vp.defaultProjectile.projectile.GetArmorPenetration(th);
						}
					}
				}
			} catch (System.NullReferenceException e) {
				this.exceptions.Add (e);
			}
		}
	}
}

