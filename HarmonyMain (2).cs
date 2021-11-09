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
using Verse.Sound;
using static SlaveRebellionsImproved.SettingsClass;
using System.Reflection.Emit;

namespace SlaveRebellionsImproved
{
    public class HarmonyMain
    {
        [HarmonyPatch(typeof(SlaveRebellionUtility), "StartSlaveRebellion", new Type[] { typeof(Pawn), typeof(string), typeof(string), typeof(LetterDef), typeof(LookTargets), typeof(bool) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Normal })]

        class StartSlaveRebellionPatch
        {
            private enum SlaveRebellionType
            {
                GrandRebellion,
                LocalRebellion,
                SingleRebellion
            }
            private static bool OriginalCanParticipateInSlaveRebellion(Pawn pawn)
            {
                if (!pawn.Downed && pawn.Spawned && pawn.IsSlave && !pawn.InMentalState && pawn.Awake())
                {
                    return !SlaveRebellionUtility.IsRebelling(pawn);
                }
                return false;
            }
            private static bool CanApplyWeaponFactor(Pawn pawn)
            {
                ThingWithComps primary = pawn.equipment.Primary;
                if (primary == null || !primary.def.IsWeapon || !SlaveRebellionUtility.WeaponUsableInRebellion(primary))
                {
                    return GoodWeaponInSameRoom(pawn);
                }
                return true;
            }
            private static bool GoodWeaponInSameRoom(Pawn pawn)
            {
                Room room = pawn.GetRoom();
                if (room == null || room.PsychologicallyOutdoors)
                {
                    return false;
                }
                ThingRequest thingReq = ThingRequest.ForGroup(ThingRequestGroup.Weapon);
                return GenClosest.ClosestThingReachable(pawn.Position, pawn.Map, thingReq, PathEndMode.Touch, TraverseParms.For(pawn), 6.9f, (Thing t) => EquipmentUtility.CanEquip(t, pawn) && SlaveRebellionUtility.WeaponUsableInRebellion(t) && t.GetRoom() == room) != null;
            }

            private static float LoyaltyLevelSupression => LoadedModManager.GetMod<SlaveMod>().GetSettings<SlaveSettings>().rebellionLoyal;


            [HarmonyPatch(typeof(SlaveRebellionUtility))]
            [HarmonyPatch("StartSlaveRebellion")]
            [HarmonyPatch(new Type[] { typeof(Pawn), typeof(string), typeof(string), typeof(LetterDef), typeof(LookTargets), typeof(bool) }, new ArgumentType[] { ArgumentType.Normal, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Out, ArgumentType.Normal })]

            public static bool Prefix(ref bool __result, List<Pawn> ___rebellingSlaves, List<Pawn> ___allPossibleRebellingSlaves, Pawn initiator, out string letterText, out string letterLabel, out LetterDef letterDef, out LookTargets lookTargets, bool forceAggressive = false)
            {
                letterText = null;
                letterLabel = null;
                letterDef = null;
                lookTargets = null;
                if (!ModLister.CheckIdeology("Slave rebellion"))
                {
                    __result = false;
                    return false;
                }
                ___rebellingSlaves.Clear();
                ___rebellingSlaves.Add(initiator);
                ___allPossibleRebellingSlaves.Clear();
                List<Pawn> slavesOfColonySpawned = initiator.Map.mapPawns.SlavesOfColonySpawned;
                for (int i = 0; i < slavesOfColonySpawned.Count; i++)
                {
                    Pawn pawn = slavesOfColonySpawned[i];
                    if (pawn != initiator && SlaveRebellionUtility.CanParticipateInSlaveRebellion(pawn))
                    {
                        ___allPossibleRebellingSlaves.Add(pawn);
                    }
                }
                List<Pawn> allReallyPossibleRebellingSlaves = new List<Pawn> { };
                for (int i = 0; i < slavesOfColonySpawned.Count; i++)
                {
                    Pawn pawn = slavesOfColonySpawned[i];
                    if (pawn != initiator && OriginalCanParticipateInSlaveRebellion(pawn))
                    {
                        allReallyPossibleRebellingSlaves.Add(pawn);
                    }
                }
                // Rebelling type
                SlaveRebellionType slaveRebellionType;
                int rebellingslaves = ___allPossibleRebellingSlaves.Count;
                int allslaves = slavesOfColonySpawned.Count;
                if (rebellingslaves > allslaves * 0.5)
                {
                    slaveRebellionType = SlaveRebellionType.GrandRebellion;
                }
                else
                {
                    slaveRebellionType = SlaveRebellionType.LocalRebellion;
                }
                // Rebelling type
                switch (slaveRebellionType)
                {
                    case SlaveRebellionType.GrandRebellion:
                        {
                            for (int k = 0; k < allReallyPossibleRebellingSlaves.Count; k++)
                            {
                                Need_Suppression need_Suppression = allReallyPossibleRebellingSlaves[k].needs.TryGetNeed<Need_Suppression>();
                                if (need_Suppression == null)
                                {
                                    continue;
                                }
                                if (need_Suppression.CurLevelPercentage < LoyaltyLevelSupression)
                                {
                                    ___rebellingSlaves.Add(allReallyPossibleRebellingSlaves[k]);
                                }
                            }
                            break;
                        }
                    case SlaveRebellionType.LocalRebellion:
                        {
                            for (int j = 0; j < ___allPossibleRebellingSlaves.Count; j++)
                            {
                                Pawn pawn2 = ___allPossibleRebellingSlaves[j];
                                if (!(initiator.Position.DistanceTo(pawn2.Position) > 25f))
                                {
                                    ___rebellingSlaves.Add(pawn2);
                                }
                            }
                            break;
                        }
                }
                if (___rebellingSlaves.Count == 1)
                {
                    slaveRebellionType = SlaveRebellionType.SingleRebellion;
                }
                else if (___rebellingSlaves.Count > rebellingslaves * 0.5)
                {
                    slaveRebellionType = SlaveRebellionType.GrandRebellion;
                }
                if (!RCellFinder.TryFindRandomExitSpot(initiator, out var spot, TraverseMode.PassDoors))
                {
                    __result = false;
                    return false;
                }
                if (!PrisonBreakUtility.TryFindGroupUpLoc(___rebellingSlaves, spot, out var groupUpLoc))
                {
                    __result = false;
                    return false;
                }
                bool flag = false;
                if (forceAggressive)
                {
                    flag = true;
                }
                else
                {
                    if (slaveRebellionType == SlaveRebellionType.SingleRebellion)
                    {
                        if (initiator.Map.mapPawns.FreeColonistsSpawnedCount < 3)
                        {
                            flag = (CanApplyWeaponFactor(initiator));
                        }
                    }
                    else
                    {
                        int i = 0;
                        for (int j = 0; j < ___rebellingSlaves.Count; j++)
                        {
                            i += CanApplyWeaponFactor(___rebellingSlaves[j]) ? 1 : 0;
                        }
                        if (i > initiator.Map.mapPawns.FreeColonistsSpawnedCount * 0.4)
                        {
                            flag = true;
                        }
                    }
                }

                switch (slaveRebellionType)
                {
                    case SlaveRebellionType.GrandRebellion:
                        if (flag)
                        {
                            letterLabel = "LetterLabelGrandSlaveRebellion".Translate();
                            letterText = "LetterGrandSlaveRebellion".Translate(GenLabel.ThingsLabel(___rebellingSlaves));
                        }
                        else
                        {
                            letterLabel = "LetterLabelGrandSlaveEscape".Translate();
                            letterText = "LetterGrandSlaveEscape".Translate(GenLabel.ThingsLabel(___rebellingSlaves));
                        }
                        break;
                    case SlaveRebellionType.LocalRebellion:
                        if (flag)
                        {
                            letterLabel = "LetterLabelLocalSlaveRebellion".Translate();
                            letterText = "LetterLocalSlaveRebellion".Translate(initiator, GenLabel.ThingsLabel(___rebellingSlaves));
                        }
                        else
                        {
                            letterLabel = "LetterLabelLocalSlaveEscape".Translate();
                            letterText = "LetterLocalSlaveEscape".Translate(initiator, GenLabel.ThingsLabel(___rebellingSlaves));
                        }
                        break;
                    case SlaveRebellionType.SingleRebellion:
                        if (flag)
                        {
                            letterLabel = "LetterLabelSingleSlaveRebellion".Translate() + (": " + initiator.LabelShort);
                            letterText = "LetterSingleSlaveRebellion".Translate(initiator);
                        }
                        else
                        {
                            letterLabel = "LetterLabelSingleSlaveEscape".Translate() + (": " + initiator.LabelShort);
                            letterText = "LetterSingleSlaveEscape".Translate(initiator);
                        }
                        break;
                    default:
                        Log.Error($"Unkown slave rebellion type {slaveRebellionType}");
                        break;
                }
                letterText += "\n\n" + "SlaveRebellionSuppressionExplanation".Translate();
                lookTargets = new LookTargets(___rebellingSlaves);
                letterDef = LetterDefOf.ThreatBig;
                int sapperThingID = -1;
                if (Rand.Value < 0.5f)
                {
                    sapperThingID = initiator.thingIDNumber;
                }
                for (int l = 0; l < ___rebellingSlaves.Count; l++)
                {
                    ___rebellingSlaves[l].GetLord()?.Notify_PawnLost(___rebellingSlaves[l], PawnLostCondition.ForcedToJoinOtherLord);
                }
                if (!flag)
                {
                    LordMaker.MakeNewLord(___rebellingSlaves[0].Faction, new LordJob_SlaveEscape(groupUpLoc, spot), initiator.Map, ___rebellingSlaves);
                }
                else
                {
                    LordMaker.MakeNewLord(___rebellingSlaves[0].Faction, new LordJob_SlaveRebellion(groupUpLoc, spot, sapperThingID, !flag), initiator.Map, ___rebellingSlaves);
                }

                for (int m = 0; m < ___rebellingSlaves.Count; m++)
                {
                    if (!___rebellingSlaves[m].Awake())
                    {
                        RestUtility.WakeUp(___rebellingSlaves[m]);
                    }
                    ___rebellingSlaves[m].drafter.Drafted = false;
                    if (___rebellingSlaves[m].CurJob != null)
                    {
                        ___rebellingSlaves[m].jobs.EndCurrentJob(JobCondition.InterruptForced);
                    }
                    ___rebellingSlaves[m].Map.attackTargetsCache.UpdateTarget(___rebellingSlaves[m]);
                    if (___rebellingSlaves[m].carryTracker.CarriedThing != null)
                    {
                        ___rebellingSlaves[m].carryTracker.TryDropCarriedThing(___rebellingSlaves[m].Position, ThingPlaceMode.Near, out var _);
                    }
                }
                ___rebellingSlaves.Clear();
                __result = true;
                return false;
            }
        }
        [HarmonyPatch(typeof(SlaveRebellionUtility), "CanParticipateInSlaveRebellion")]

        public static class CanParticipateInSlaveRebellionPatch
        {
            private static float FearedLevelSupression => LoadedModManager.GetMod<SlaveMod>().GetSettings<SlaveSettings>().rebellionMin;
            public static void Postfix(ref bool __result, Pawn pawn)
            {
                Need_Suppression need_Suppression = pawn.needs.TryGetNeed<Need_Suppression>();
                if (need_Suppression == null)
                {
                    return;
                }
                if (need_Suppression.CurLevelPercentage > FearedLevelSupression)
                {
                    __result = false;
                }
            }
        }

        [HarmonyPatch(typeof(SlaveRebellionUtility), "IsRebelling")]

        public static class IsRebellingPatch
        {
            private static readonly MethodInfo lordJob = AccessTools.PropertyGetter(typeof(Lord), nameof(Lord.LordJob));
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator il)
            {
                var codes = new List<CodeInstruction>(instructions);
                Label label = il.DefineLabel();
                codes[codes.Count - 1].labels.Add(label);
                for (var i = 0; i < codes.Count; i++)
                {
                    if (i < codes.Count - 3 && codes[i].opcode == OpCodes.Ldnull)
                    {
                        yield return new CodeInstruction(OpCodes.Ldnull);
                        yield return new CodeInstruction(OpCodes.Cgt_Un);
                        yield return new CodeInstruction(OpCodes.Dup);
                        yield return new CodeInstruction(OpCodes.Brtrue_S, label);
                        yield return new CodeInstruction(OpCodes.Pop);
                        yield return new CodeInstruction(OpCodes.Ldloc_0);
                        yield return new CodeInstruction(OpCodes.Callvirt, lordJob);
                        yield return new CodeInstruction(OpCodes.Isinst, typeof(LordJob_SlaveEscape));
                    }
                    yield return codes[i];
                }
            }
            /*
            public static void Postfix(ref bool __result, Pawn pawn)
            {
                
                Lord lord = pawn.GetLord();
                if (lord != null && lord.LordJob is LordJob_SlaveEscape)
                {
                    __result = true;
                }
                
            }
            */
        }
    }
}
