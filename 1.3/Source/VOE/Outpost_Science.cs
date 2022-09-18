using Outposts;
using RimWorld;
using Verse;

namespace VOE
{
    public class Outpost_Science : Outpost
    {
        [PostToSetings("Outposts.Settings.ResearchRate", PostToSetingsAttribute.DrawMode.Percentage, 0.25f, 0.0001f, 10f)]
        public float ResearchRate = 1f;

        public override void Tick()
        {
            base.Tick();
            if (Find.ResearchManager.currentProj == null || Packing) return;
            foreach (var pawn in CapablePawns)
            {
                if (Find.ResearchManager.currentProj == null || Packing || GenLocalDate.HourInteger(Tile) >= 16 || GenLocalDate.HourInteger(Tile) <= 8) continue;
                Find.ResearchManager.ResearchPerformed(pawn.GetStatValue(StatDefOf.ResearchSpeed) * ResearchRate * OutpostsMod.Settings.ProductionMultiplier / 5f, pawn);
            }
        }
    }
}