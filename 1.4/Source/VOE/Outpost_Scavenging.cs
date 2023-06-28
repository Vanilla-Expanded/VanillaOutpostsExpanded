using System.Collections.Generic;
using Outposts;
using RimWorld;
using UnityEngine;
using Verse;

namespace VOE
{
    public class Outpost_Scavenging : Outpost
    {
        public override int TicksPerProduction => Mathf.Max(base.TicksPerProduction - PawnCount * 600000, 600000);
        public override IEnumerable<Thing> ProducedThings()
        {
            ThingSetMakerParams parms = default(ThingSetMakerParams);
            parms.totalMarketValueRange = new FloatRange(750f, 2000f);
            return ThingSetMakerDefOf.Reward_ItemsStandard.root.Generate(parms);
        }
        public override string ProductionString() => "Outposts.WillProduce.0".Translate(TimeTillProduction).RawText;
    }
}