using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using SlaveRebellionsImproved;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace SlaveRebellionsImproved
{
    class LordJob_SlaveEscape : LordJob
    {
        private IntVec3 exitPoint;
        private IntVec3 groupUpLoc;

        public LordJob_SlaveEscape()
        {
        }
        public LordJob_SlaveEscape(IntVec3 groupUpLoc, IntVec3 exitPoint)
        {
            this.groupUpLoc = groupUpLoc;
            this.exitPoint = exitPoint;
        }

        public override StateGraph CreateGraph()
        {
            StateGraph stateGraph = new StateGraph();
            if (!ModLister.CheckIdeology("Slave rebellion"))
            {
                return stateGraph;
            }
            LordToil_Travel lordToil_Travel = new LordToil_Travel(groupUpLoc)
            {
                maxDanger = Danger.Deadly,
                useAvoidGrid = true
            };
            stateGraph.StartingToil = lordToil_Travel;
            LordToil_SlavesEscapeNoViolence lordToil_SlavesEscapeNoViolence = new LordToil_SlavesEscapeNoViolence(exitPoint);
            stateGraph.AddToil(lordToil_SlavesEscapeNoViolence);
            LordToil_ExitMap lordToil_ExitMap = new LordToil_ExitMap(LocomotionUrgency.Jog, canDig: true);
            stateGraph.AddToil(lordToil_ExitMap);
            LordToil_ExitMap lordToil_ExitMap2 = new LordToil_ExitMap(LocomotionUrgency.Jog)
            {
                useAvoidGrid = true
            };
            stateGraph.AddToil(lordToil_ExitMap2);
            Transition transition = new Transition(lordToil_Travel, lordToil_SlavesEscapeNoViolence);
            transition.AddTrigger(new Trigger_Memo("TravelArrived"));
            stateGraph.AddTransition(transition);

            Transition transition1 = new Transition(lordToil_SlavesEscapeNoViolence, lordToil_ExitMap);
            transition1.AddSource(lordToil_ExitMap2);
            transition1.AddTrigger(new Trigger_PawnCannotReachMapEdge());
            stateGraph.AddTransition(transition1);

            Transition transition2 = new Transition(lordToil_ExitMap, lordToil_ExitMap2);
            transition2.AddSource(lordToil_SlavesEscapeNoViolence);
            transition2.AddTrigger(new Trigger_PawnCanReachMapEdge());
            stateGraph.AddTransition(transition2);

            return stateGraph;
        }
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref groupUpLoc, "groupUpLoc");
        }
        public override void Notify_PawnAdded(Pawn p)
        {
            ReachabilityUtility.ClearCacheFor(p);
        }
        public override void Notify_PawnLost(Pawn p, PawnLostCondition condition)
        {
            ReachabilityUtility.ClearCacheFor(p);
        }
        public override bool CanOpenAnyDoor(Pawn p)
        {
            return true;
        }
        public override bool ValidateAttackTarget(Pawn searcher, Thing target)
        {
            if (!(target is Pawn pawn))
            {
                return true;
            }
            MentalStateDef mentalStateDef = pawn.MentalStateDef;
            if (mentalStateDef == null)
            {
                return true;
            }
            return !mentalStateDef.escapingPrisonersIgnore;
        }

    }
}
