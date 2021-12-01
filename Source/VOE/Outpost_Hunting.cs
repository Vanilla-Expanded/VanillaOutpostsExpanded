using System;
using System.Collections.Generic;
using System.Linq;
using Outposts;
using RimWorld;
using Verse;

namespace VOE
{
    public class Outpost_Hunting : Outpost
    {
        private int animalsSkill;
        private ThingDef leather;
        private ThingDef meat;
        private int shootingSkill;

        public override IEnumerable<Thing> ProducedThings() => MakeThings(meat, shootingSkill * 10).Concat(MakeThings(leather, shootingSkill * 5));

        public override void RecachePawnTraits()
        {
            base.RecachePawnTraits();
            animalsSkill = TotalSkill(SkillDefOf.Animals);
            shootingSkill = TotalSkill(SkillDefOf.Shooting);
            var leathers = animalsSkill >= 75
                ? DefDatabase<ThingDef>.AllDefs.Where(d => d.IsLeather).ToList()
                : Find.WorldGrid[Tile].biome.AllWildAnimals.Select(pkd => pkd.RaceProps.leatherDef).ToList();
            // foreach (var thingDef in leathers) Log.Message($"{thingDef.label}: ${thingDef.BaseMarketValue}");
            leather = leathers.MinBy(td => Math.Abs(animalsSkill - td.BaseMarketValue * 20));
            meat = DefDatabase<PawnKindDef>.AllDefs.First(pkd => pkd.RaceProps.leatherDef == leather).race.race.meatDef;
        }

        public override string ProductionString() => "Outposts.WillProduce.2".Translate(shootingSkill * 10, meat.label, 100,
            leather.label, TimeTillProduction);
    }
}