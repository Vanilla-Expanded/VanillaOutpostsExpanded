using System.Collections.Generic;
using Outposts;
using RimWorld;
using Verse;

namespace VOE
{
    public class Outpost_Drilling : Outpost
    {
        private int workUntilReady;
        protected virtual int WorkNeeded => 7 * 60000;

        public override void PostMake()
        {
            base.PostMake();
            workUntilReady = WorkNeeded;
        }

        public override IEnumerable<Thing> ProducedThings() => workUntilReady > 0 ? new List<Thing>() : base.ProducedThings();

        public override void Tick()
        {
            base.Tick();
            if (workUntilReady > 0 && !Packing) workUntilReady -= TotalSkill(SkillDefOf.Construction);
        }

        public override string ProductionString() => workUntilReady > 0
            ? "Outposts.Drilling".Translate(((float) workUntilReady / WorkNeeded).ToStringPercent(),
                (workUntilReady / TotalSkill(SkillDefOf.Construction)).ToStringTicksToPeriodVerbose())
            : base.ProductionString();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workUntilReady, "workUntilReady");
        }
    }
}