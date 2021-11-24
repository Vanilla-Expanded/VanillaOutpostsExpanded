using System.Collections.Generic;
using Outposts;
using RimWorld;
using Verse;

namespace VOE
{
    public class Outpost_Scavenging : Outpost
    {
        public override IEnumerable<Thing> ProducedThings() => ThingSetMakerDefOf.Reward_ItemsStandard.root.Generate();

        public override string ProductionString() => "Outposts.WillProduce.0".Translate(TimeTillProduction);
    }
}