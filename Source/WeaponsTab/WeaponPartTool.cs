using UnityEngine;
using Verse;
using RimWorld;

namespace WeaponStats
{
    public class WeaponPartTool
    {
        public string label { get; set; }
        public float power { get; set; }
        public float cooldownTime { get; set; }
        public float chanceFactor { get; set; }

        public void fillFromTool(Tool tl, bool ce = false)
        {
            label = tl.label;
            power = tl.power;
            cooldownTime = tl.cooldownTime;
            chanceFactor = tl.chanceFactor;
        }
    }
}