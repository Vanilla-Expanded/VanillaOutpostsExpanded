using System.Collections.Generic;
using Outposts;
using RimWorld;
using Verse;

namespace VOE
{
    public class Outpost_Drilling : Outpost
    {
        private bool ready;
        private int workDone;

        [PostToSetings("Outposts.Settings.WorkToDrill", PostToSetingsAttribute.DrawMode.Time, 7 * GenDate.TicksPerDay, GenDate.TicksPerDay, GenDate.TicksPerYear)]
        public int WorkToDrill = 7 * 60000;

        public override void PostMake()
        {
            base.PostMake();
            workDone = 0;
        }

        public override IEnumerable<Thing> ProducedThings() => ready ? new List<Thing>() : base.ProducedThings();

        public override void Tick()
        {
            base.Tick();
            if (!ready && !Packing) workDone += TotalSkill(SkillDefOf.Construction);
            if (workDone >= WorkToDrill) ready = true;
        }

        public override string ProductionString() => ready
            ? "Outposts.Drilling".Translate(((float) workDone / WorkToDrill).ToStringPercent(),
                ((WorkToDrill - workDone) / TotalSkill(SkillDefOf.Construction)).ToStringTicksToPeriodVerbose())
            : base.ProductionString();

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref workDone, "workUntilReady");
        }
    }
}