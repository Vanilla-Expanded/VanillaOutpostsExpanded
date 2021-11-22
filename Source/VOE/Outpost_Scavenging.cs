using System.Collections.Generic;
using Outposts;
using RimWorld;
using Verse;

namespace VOE
{
    public class Outpost_Scavenging : Outpost
    {
        public override ThingDef ProvidedFood => ThingDefOf.Pemmican;

        public override IEnumerable<Thing> ProducedThings()
        {
            return ThingSetMakerDefOf.Reward_ItemsStandard.root.Generate();
        }

        public override string GetInspectString()
        {
            return base.GetInspectString() + "\n" + "Outposts.WillProduce.0".Translate(ticksTillProduction.ToStringTicksToPeriodVerbose());
        }
    }
}