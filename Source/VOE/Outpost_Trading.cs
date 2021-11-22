using System.Collections.Generic;
using Outposts;
using RimWorld;
using Verse;

namespace VOE
{
    public class Outpost_Trading : Outpost
    {
        private int socialSkill;
        public override ThingDef ProvidedFood => ThingDefOf.MealFine;

        public override IEnumerable<Thing> ProducedThings() => MakeThings(ThingDefOf.Silver, socialSkill * 30);

        public override void RecachePawnTraits()
        {
            socialSkill = TotalSkill(SkillDefOf.Social);
        }

        public override string GetInspectString() =>
            base.GetInspectString() + "\n" + "Outposts.TotalSkill".Translate(SkillDefOf.Social.skillLabel, socialSkill) + "\n" +
            "Outposts.WillProduce.1".Translate(socialSkill * 30, ThingDefOf.Silver.label, ticksTillProduction.ToStringTicksToPeriodVerbose());

        public static string CanSpawnOnWith(int tile, List<Pawn> pawns) => CheckSkill(pawns, SkillDefOf.Social, 10);
    }
}