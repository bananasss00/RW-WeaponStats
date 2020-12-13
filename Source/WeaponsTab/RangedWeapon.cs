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
        public float dpsaTouch { get; set; }
        public float dpsaShort { get; set; }
        public float dpsaMedium { get; set; }
        public float dpsaLong { get; set; }

        public int burstShotCount { get; set; }

        public int ticksBetweenBurstShots { get; set; }
        public float ceSightsEfficiency { get; set; }
        public float ceShotSpread { get; set; }
        public float ceSwayFactor { get; set; }
        public float ceBulk { get; set; }
        
        public float ceMagazineCapacity { get; set; }

        public RangedWeapon() : base()
        {
            accuracyTouch = 0f;
            accuracyShort = 0f;
            accuracyMedium = 0f;
            accuracyLong = 0f;
            minRange = 0f;
            maxRange = 0f;
            warmup = 0f;
        }

        private float getDps()
        {
            float burstDamage = this.damage * this.burstShotCount;
            float warmupTicks = (this.cooldown + this.warmup) * TPS;
            float burstTicks = this.burstShotCount * this.ticksBetweenBurstShots;
            float totalTime = (warmupTicks + burstTicks) / TPS;

            return (float) Math.Round(burstDamage / totalTime, 2);
        }

        private float getDpsA()
        {
            float sumAccuracy = 0;
            var numAccuracy = 0;
            dpsaTouch = 0;
            dpsaShort = 0;
            dpsaMedium = 0;
            dpsaLong = 0;
            if (accuracyTouch > 0)
            {
                dpsaTouch = (float) Math.Round(dps * accuracyTouch / 100, 1);
                sumAccuracy += accuracyTouch;
                numAccuracy++;
            }

            if (accuracyShort > 0)
            {
                dpsaShort = (float) Math.Round(dps * accuracyShort / 100, 1);
                sumAccuracy += accuracyShort;
                numAccuracy++;
            }

            if (accuracyMedium > 0)
            {
                dpsaMedium = (float) Math.Round(dps * accuracyMedium / 100, 1);
                sumAccuracy += accuracyMedium;
                numAccuracy++;
            }

            if (accuracyLong > 0)
            {
                dpsaLong = (float) Math.Round(dps * accuracyLong / 100, 1);
                sumAccuracy += accuracyLong;
                numAccuracy++;
            }

            return numAccuracy > 0 ? (float) Math.Round(dps * (sumAccuracy / numAccuracy) / 100, 1) : 0;
        }

        public string getAccuracyStr()
        {
            StringBuilder sb = new StringBuilder();
            if (minRange > RNG_TOUCH || maxRange < RNG_TOUCH)
            {
                sb.Append(" - /");
            }
            else
            {
                sb.Append(" ").Append(Math.Round(accuracyTouch, 1).ToString()).Append(" /");
            }

            if (minRange > RNG_SHORT || maxRange < RNG_SHORT)
            {
                sb.Append(" - /");
            }
            else
            {
                sb.Append(" ").Append(Math.Round(accuracyShort, 1).ToString()).Append(" /");
            }

            if (minRange > RNG_MEDIUM || maxRange < RNG_MEDIUM)
            {
                sb.Append(" - /");
            }
            else
            {
                sb.Append(" ").Append(Math.Round(accuracyMedium, 1).ToString()).Append(" /");
            }

            if (minRange > RNG_LONG || maxRange < RNG_LONG)
            {
                sb.Append(" -");
            }
            else
            {
                sb.Append(" ").Append(Math.Round(accuracyLong, 1).ToString());
            }

            return sb.ToString();
        }

        public new void fillFromThing(Thing th, bool ce = false)
        {
            base.fillFromThing(th);

            if (th.def != null && th.def.Verbs != null)
            {
                try
                {
                    if (ce)
                    {
                        ceSightsEfficiency = th.GetStatValue(StatDef.Named("SightsEfficiency"));
                        ceShotSpread = th.GetStatValue(StatDef.Named("ShotSpread"));
                        ceSwayFactor = th.GetStatValue(StatDef.Named("SwayFactor"));
                        ceBulk = th.GetStatValue(StatDef.Named("Bulk"));
                        ceMagazineCapacity = th.GetStatValue(StatDef.Named("MagazineCapacity"));
                        cooldown = th.GetStatValue(StatDefOf.RangedWeapon_Cooldown);

                        foreach (VerbProperties vp in th.def.Verbs)
                        {
                            if (ce && (vp.verbClass.FullName == "CombatExtended.Verb_ShootCE" || vp.verbClass.FullName == "CombatExtended.Verb_ShootCEOneUse"))
                            {
                                hasCeVerb = true;
                                warmup = vp.warmupTime;
                                maxRange = vp.range;
                            }
                        }
                    }
                    else
                    {
                        foreach (VerbProperties vp in th.def.Verbs)
                        {
                            if (vp.ToString().StartsWith("VerbProperties"))
                            {
                                warmup = vp.warmupTime;
                                maxRange = vp.range;
                                minRange = vp.minRange;
                                damage = vp.defaultProjectile.projectile.GetDamageAmount(th);
                                damageType = vp.defaultProjectile.projectile.damageDef.label;
                                armorPenetration = vp.defaultProjectile.projectile.GetArmorPenetration(th);
                                if (vp.burstShotCount > 0)
                                {
                                    this.burstShotCount = vp.burstShotCount;
                                }
                                else
                                {
                                    this.burstShotCount = 1;
                                }

                                if (vp.ticksBetweenBurstShots > 0)
                                {
                                    this.ticksBetweenBurstShots = vp.ticksBetweenBurstShots;
                                }
                                else
                                {
                                    this.ticksBetweenBurstShots = 10;
                                }
                            }
                        }
                    }
                }
                catch (System.NullReferenceException e)
                {
                    this.exceptions.Add(e);
                }
            }
            
            try
            {
                if (minRange <= RNG_TOUCH && maxRange >= RNG_TOUCH)
                {
                    accuracyTouch = (float) Math.Round(th.GetStatValue(StatDefOf.AccuracyTouch) * 100, 2);
                }

                if (minRange <= RNG_SHORT && maxRange >= RNG_SHORT)
                {
                    accuracyShort = (float) Math.Round(th.GetStatValue(StatDefOf.AccuracyShort) * 100, 2);
                }

                if (minRange <= RNG_MEDIUM && maxRange >= RNG_MEDIUM)
                {
                    accuracyMedium = (float) Math.Round(th.GetStatValue(StatDefOf.AccuracyMedium) * 100, 2);
                }

                if (minRange <= RNG_LONG && maxRange >= RNG_LONG)
                {
                    accuracyLong = (float) Math.Round(th.GetStatValue(StatDefOf.AccuracyLong) * 100, 2);
                }

                cooldown = th.GetStatValue(StatDefOf.RangedWeapon_Cooldown);
                mass = th.GetStatValue(StatDefOf.Mass);
            }
            catch (System.NullReferenceException e)
            {
                this.exceptions.Add(e);
            }

            this.dps = this.getDps();
            this.dpsa = this.getDpsA();
        }
    }
}