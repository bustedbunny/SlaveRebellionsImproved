using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using Verse.AI;

namespace SlaveRebellionsImproved
{
    [RimWorld.DefOf]
    public class DefOfClass
    {
        public static DutyDef SlaveEscapeNoViolence;
        static DefOfClass()
        {
            DefOfHelper.EnsureInitializedInCtor(typeof(DefOfClass));
        }

    }
}
