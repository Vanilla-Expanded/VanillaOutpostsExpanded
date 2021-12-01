using System.Collections.Generic;
using System.Linq;
using Outposts;
using RimWorld;
using VCE_Fishing;
using Verse;

namespace FishingOutpost
{
    public class Outpost_Fishing : Outpost
    {
        private ThingDef currentFish;
        private List<ThingDef> possibleFish;

        public override IEnumerable<Thing> ProducedThings()
        {
            var items = MakeThings(currentFish, TotalSkill(SkillDefOf.Animals) * 10);
            currentFish = possibleFish.RandomElement();
            return items;
        }

        public override void PostAdd()
        {
            base.PostAdd();
            possibleFish = DefDatabase<FishDef>.AllDefs
                .Where(fish => fish.allowedBiomes.Any(biome =>
                    DefDatabase<BiomeTempDef>.AllDefs.Any(bt => bt.biomeTempLabel == biome && bt.biomes.Any(b => b == Find.WorldGrid[Tile].biome.defName))))
                .Select(fish => fish.thingDef).ToList();
            currentFish = possibleFish.RandomElement();
        }

        public override string ProductionString() => "Outposts.WillProduce.1".Translate(TotalSkill(SkillDefOf.Animals) * 10, currentFish.label, TimeTillProduction);

        public static string CanSpawnOnWith(int tile, List<Pawn> pawns) => Find.World.CoastDirectionAt(tile) == Rot4.Invalid ? "Outposts.MustBeOnCoast".Translate() : null;
    }
}