using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace SlaveRebellionsImproved
{
    public class LordToil_SlavesEscapeNoViolence : LordToil_Travel
    {
        public override IntVec3 FlagLoc => Data.dest;
        private LordToilData_Travel Data => (LordToilData_Travel)data;
        public override bool AllowSatisfyLongNeeds => false;
        protected override float AllArrivedCheckRadius => 20f;

        public LordToil_SlavesEscapeNoViolence(IntVec3 dest)
                : base(dest)
        {

        }

        public override void UpdateAllDuties()
        {
            LordToilData_Travel lordToilData_Travel = Data;
            for (int i = 0; i< lord.ownedPawns.Count; i++)
            {
                Pawn pawn = lord.ownedPawns[i];
                pawn.mindState.duty = new PawnDuty(DefOfClass.SlaveEscapeNoViolence, lordToilData_Travel.dest);
            }
        }

        public override void LordToilTick()
        {
            base.LordToilTick();
            for (int i = 0; i < lord.ownedPawns.Count; i++)
            {
                lord.ownedPawns[i].guilt.Notify_Guilty();
            }
        }


    }
}
