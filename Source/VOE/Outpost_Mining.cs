using System.Collections.Generic;
using System.Linq;
using Outposts;
using RimWorld.Planet;
using Verse;

namespace VOE
{
    public class Outpost_Mining : Outpost_ChooseResult
    {
        public override IEnumerable<ResultOption> GetExtraOptions()
        {
            return Find.World.NaturalRockTypesIn(Tile).Select(rock => rock?.building?.mineableThing?.butcherProducts.FirstOrDefault()?.thingDef).Where(x => x is not null)
                .Select(rock => new ResultOption
                {
                    Thing = rock,
                    BaseAmount = 750
                });
        }

        public static string CanSpawnOnWith(int tile, List<Pawn> pawns) => Find.WorldGrid[tile].hilliness == Hilliness.Flat ? "Outposts.MustBeMade.Hill".Translate() : null;

        public static string RequirementsString(int tile, List<Pawn> pawns) =>
            Requirement("Outposts.MustBeMade.Hill".Translate(), Find.WorldGrid[tile].hilliness != Hilliness.Flat);
    }
}