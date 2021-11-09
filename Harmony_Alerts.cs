using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.AI;
using Verse.AI.Group;
using RimWorld;
using UnityEngine;

namespace SlaveRebellionsImproved
{
    class Harmony_Alerts
    {
        [HarmonyPatch(typeof(Alert_SlavesUnsuppressed), "GetExplanation")]

        public static class Alert_SlavesUnsuppressedPatch
        {
            public static bool Prefix(ref TaggedString __result, Alert_SlavesUnsuppressed __instance)
            {
                if (__instance.Targets.Count > 1)
                {
                    string text = "";
                    foreach (Pawn slave in __instance.Targets)
                    {
                        text = String.Concat(text, "\n" + slave.NameShortColored.Colorize(ColoredText.NameColor));
                    }
                    __result = "NewSlavesUnsuppressedDesc".Translate(text);
                    return false;
                }
                return true;
            }
        }

        [HarmonyPatch(typeof(Alert_SlaveRebellionLikely), "GetReport")]

        public static class Alert_SlaveRebellionLikelyPatch
        {

            private static bool MTBMeetsRebelliousThreshold(float mtb)
            {
                if (15f > mtb)
                {
                    return mtb > 0f;
                }
                return false;
            }
            public static bool Prefix(ref AlertReport __result)
            {
                Map currentMap = Find.CurrentMap;
                if (!ModsConfig.IdeologyActive || currentMap == null)
                {
                    __result = false;
                    return false;
                }
                List<Pawn> slaves = new List<Pawn> { };
                foreach (Pawn slave in currentMap.mapPawns.SlavesOfColonySpawned)
                {
                    if (MTBMeetsRebelliousThreshold(SlaveRebellionUtility.InitiateSlaveRebellionMtbDays(slave)))
                    {
                        slaves.Add(slave);
                    }
                    
                }
                if (slaves.Count < 1)
                {
                    __result = false;
                    return false;
                }
                __result = AlertReport.CulpritsAre(slaves);
                return false;
            }

        }
    }
}
