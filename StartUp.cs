using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;

namespace SlaveRebellionsImproved
{

    [StaticConstructorOnStartup]
    class StartUp
    {
        static StartUp()
        {
            var harmony = new Harmony("SlaveRebellionsImproved.patch");
            harmony.PatchAll();
        }
    }

}
