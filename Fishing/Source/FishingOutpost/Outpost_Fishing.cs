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
        private int animalsSkill;
        private ThingDef currentFish;
        private List<ThingDef> possibleFish;

        public override IEnumerable<Thing> ProducedThings()
        {
            var items = MakeThings(currentFish, animalsSkill * 10);
            currentFish = possibleFish.RandomElement();
            return items;
        }

        public override void RecachePawnTraits()
        {
            animalsSkill = TotalSkill(SkillDefOf.Animals);
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

        public override string GetInspectString()
        {
            return base.GetInspectString() + "\n" + "Outposts.TotalSkill".Translate(SkillDefOf.Animals.skillLabel, animalsSkill) +
                   (Packing ? "" : " \n" + "Outposts.WillProduce.1".Translate(animalsSkill * 10, currentFish.label, TimeTillProduction).ToString());
        }

        public static string CanSpawnOnWith(int tile, List<Pawn> pawns)
        {
            if (Find.World.CoastDirectionAt(tile) == Rot4.Invalid) return "Outposts.MustBeOnCoast".Translate();

            return CheckSkill(pawns, SkillDefOf.Animals, 10);
        }
    }
}