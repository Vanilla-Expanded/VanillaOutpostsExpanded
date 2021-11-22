using System.Collections.Generic;
using Outposts;
using RimWorld;
using Verse;

namespace VOE
{
    public class Outpost_Science : Outpost
    {
        public override int TicksPerProduction => -1;
        public override ThingDef ProvidedFood => ThingDefOf.MealFine;

        public override void Tick()
        {
            base.Tick();
            if (Find.ResearchManager.currentProj != null && !Packing)
                foreach (var pawn in AllPawns)
                    Find.ResearchManager.ResearchPerformed(pawn.skills.GetSkill(SkillDefOf.Intellectual).Level, pawn);
        }

        public static string CanSpawnOnWith(int tile, List<Pawn> pawns)
        {
            return CheckSkill(pawns, SkillDefOf.Intellectual, 30);
        }
    }
}