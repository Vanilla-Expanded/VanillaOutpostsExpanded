using System.Collections.Generic;
using System.Linq;
using Outposts;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace VOE
{
    public class Outpost_Mining : Outpost
    {
        private const string STONES = "StonePlaceHolder";

        public static readonly Dictionary<ThingDef, int> MinLevels = new()
        {
            {new ThingDef {defName = STONES}, 10},
            {ThingDefOf.Steel, 15},
            {ThingDefOf.Jade, 20},
            {ThingDefOf.Uranium, 25},
            {ThingDefOf.Silver, 30},
            {ThingDefOf.Gold, 35},
            {ThingDefOf.Plasteel, 40},
            {ThingDefOf.ComponentIndustrial, 50}
        };

        private ThingDef choice;
        private int miningSkill;

        public override void PostAdd()
        {
            base.PostAdd();
            if (choice == null) choice = Find.World.NaturalRockTypesIn(Tile).Select(rock => rock.building.mineableThing.butcherProducts[0].thingDef).RandomElement();
        }

        public override IEnumerable<Thing> ProducedThings() => MakeThings(choice, 750);

        public override void RecachePawnTraits()
        {
            miningSkill = TotalSkill(SkillDefOf.Mining);
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref choice, "choice");
        }

        public override string GetInspectString() =>
            base.GetInspectString() + "\n" + "Outposts.TotalSkill".Translate(SkillDefOf.Mining.skillLabel, miningSkill) + "\n" +
            "Outposts.WillProduce.1".Translate(750, choice.label, ticksTillProduction.ToStringTicksToPeriodVerbose());

        public override IEnumerable<Gizmo> GetGizmos()
        {
            return base.GetGizmos().Append(new Command_Action
            {
                action = () => Find.WindowStack.Add(new FloatMenu(MinLevels.SelectMany(kv =>
                {
                    if (kv.Key.defName == STONES)
                        return Find.World.NaturalRockTypesIn(Tile).Select(rock => rock.building.mineableThing.butcherProducts[0].thingDef)
                            .Select(rock => new FloatMenuOption(rock.LabelCap, () => choice = rock));
                    return Gen.YieldSingle(kv.Value <= miningSkill
                        ? new FloatMenuOption(kv.Key.LabelCap, () => choice = kv.Key)
                        : new FloatMenuOption(kv.Key.LabelCap + " - " + "Outposts.SkillTooLow".Translate(kv.Value), null));
                }).ToList())),
                defaultLabel = "Outposts.Commands.Mining.Label".Translate(choice.label),
                defaultDesc = "Outposts.Commands.Mining.Desc".Translate(),
                icon = choice.uiIcon
            });
        }


        public static string CanSpawnOnWith(int tile, List<Pawn> pawns) => Find.WorldGrid[tile].hilliness == Hilliness.Flat ? "Outposts.MustBeMade.Hill".Translate() : null;
    }
}