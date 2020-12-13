using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;
using RimWorld;

namespace WeaponStats
{
    abstract public class Weapon
    {
        protected const int RNG_TOUCH = 3;
        protected const int RNG_SHORT = 12;
        protected const int RNG_MEDIUM = 25;
        protected const int RNG_LONG = 40;
        protected const int TPS = 60;

        public bool visible = true;

        public string label { get; set; }

        public IntVec3 position { get; set; }

        public int hp { get; set; }

        public float damage { get; set; }

        public float armorPenetration { get; set; }

        public float dps { get; set; }
        public float dpsa { get; set; }

        public float marketValue { get; set; }

        public float mass { get; set; }

        public float cooldown { get; set; }

        public string damageType { get; set; }

        public Thing thing { get; set; }

        public string quality { get; set; }

        public int qualityNum { get; set; }

        public string pawn { get; set; }

        public string pawnType { get; set; }

        public bool craftable { get; set; }

        public Building_WorkTable cratablePos { get; set; }

        public bool inStorage { get; set; }

        public IntVec3 storagePos { get; set; }

        public string stuff { get; set; }

        public bool hasCeVerb = false;
        public bool isCeAmmoUser = false;
        public bool hasCeFireModes = false;

        public List<Exception> exceptions;

        public Weapon()
        {
            exceptions = new List<Exception>();
            label = "<unknown weapon>";
            position = new IntVec3();
            hp = 100;
            damage = 0f;
            dps = 0f;
            marketValue = 0f;
            mass = 0f;
            cooldown = 0f;
            damageType = "";
            thing = null;
            quality = "normal";
            pawn = null;
        }

        private int getQualityNum(string label)
        {
            switch (label)
            {
                case "awful":
                    return 1;
                case "shoddy":
                    return 2;
                case "poor":
                    return 3;
                case "normal":
                    return 4;
                case "good":
                    return 5;
                case "superior":
                    return 6;
                case "excellent":
                    return 7;
                case "masterwork":
                    return 8;
                case "legendary":
                    return 9;
            }

            return 0;
        }

        public string getHpStr()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(hp.ToString()).Append("%");
            return sb.ToString();
        }

        public void fillFromThing(Thing th, bool ce = false)
        {
            try
            {
                if (th != null)
                {
                    thing = th;
                    marketValue = th.MarketValue;
                    position = th.Position;
                    if (th.def != null)
                    {
                        label = th.def.label;
                    }

                    hp = 100 * th.HitPoints / th.MaxHitPoints;
                    marketValue = th.MarketValue;
                    QualityCategory qc;
                    bool hasQuality = th.TryGetQuality(out qc);
                    if (hasQuality)
                    {
                        quality = qc.GetLabel();
                    }
                    else
                    {
                        quality = "normal";
                    }

                    qualityNum = getQualityNum(quality);
                    if (th.Stuff != null)
                    {
                        stuff = thing.Stuff.label;
                    }
                    else
                    {
                        stuff = "";
                    }
                }
            }
            catch (System.NullReferenceException e)
            {
                this.exceptions.Add(e);
            }
        }
    }
}