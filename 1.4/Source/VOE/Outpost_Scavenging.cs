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
        public override IEnumerable<Thing> ProducedThings() => ThingSetMakerDefOf.Reward_ItemsStandard.root.Generate();
        public override string ProductionString() => "Outposts.WillProduce.0".Translate(TimeTillProduction).RawText;
    }
}