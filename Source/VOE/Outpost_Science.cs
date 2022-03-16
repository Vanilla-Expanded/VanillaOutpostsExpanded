using Outposts;
using RimWorld;
using Verse;

namespace VOE
{
    public class Outpost_Science : Outpost
    {
        [PostToSetings("Outposts.Settings.ResearchRate", PostToSetingsAttribute.DrawMode.Percentage, 0.25f, 0.0001f, 10f)]
        public float ResearchRate = 0.25f;

        public override void Tick()
        {
            base.Tick();
            if (Find.ResearchManager.currentProj == null || Packing) return;
            foreach (var pawn in CapablePawns)
            {
                if (Find.ResearchManager.currentProj == null || Packing || GenLocalDate.HourInteger(Tile) >= 23 || GenLocalDate.HourInteger(Tile) <= 5) continue;
                Find.ResearchManager.ResearchPerformed(pawn.skills.GetSkill(SkillDefOf.Intellectual).Level * ResearchRate * OutpostsMod.Settings.ProductionMultiplier, pawn);
            }
        }
    }
}