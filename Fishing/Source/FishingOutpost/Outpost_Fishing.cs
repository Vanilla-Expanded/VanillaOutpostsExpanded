using System.Collections.Generic;
using System.Linq;
using Outposts;
using VCE_Fishing;
using Verse;

namespace FishingOutpost
{
    public class Outpost_Fishing : Outpost
    {
        private List<FishDef> possibleFish;

        public override IEnumerable<Thing> ProducedThings()
        {
            var items = new List<Thing>();
            foreach (var pawn in AllPawns)
                for (var i = 0; i < 15; i++)
                {
                    var result = possibleFish.RandomElementByWeight(fish => fish.commonality);
                    var fishingSkill = pawn.skills.AverageOfRelevantSkillsFor(DefDatabase<WorkTypeDef>.GetNamed("VCEF_Fishing"));
                    var amount = result.baseFishingYield;
                    var num = fishingSkill < 6f
                        ? (int) (amount - (6f - fishingSkill))
                        : amount + (int) ((fishingSkill - 6f) / 2.0f);

                    items.AddRange(result.thingDef.Make(num));
                }

            return items;
        }

        public override void RecachePawnTraits()
        {
            base.RecachePawnTraits();
            possibleFish = DefDatabase<FishDef>.AllDefs
                .Where(fish => fish.allowedBiomes.Any(biome =>
                    DefDatabase<BiomeTempDef>.AllDefs.Any(bt => bt.biomeTempLabel == biome && bt.biomes.Any(b => b == Find.WorldGrid[Tile].biome.defName)))).ToList();
        }

        public override string ProductionString() => "Outposts.WillProduce.0".Translate(TimeTillProduction);

        public static string CanSpawnOnWith(int tile, List<Pawn> pawns) => Find.World.CoastDirectionAt(tile) == Rot4.Invalid && Find.WorldGrid[tile].Rivers.NullOrEmpty()
            ? "Outposts.MustBeOnCoastOrRiver".Translate()
            : null;

        public static string RequirementsString(int tile, List<Pawn> pawns) => "Outposts.MustBeOnCoastOrRiver".Translate()
            .Requirement(Find.World.CoastDirectionAt(tile) != Rot4.Invalid || (Find.WorldGrid[tile].Rivers?.Any() ?? false));
    }
}