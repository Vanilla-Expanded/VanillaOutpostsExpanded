using Outposts;
using RimWorld;
using Verse;

namespace VOE
{
    public class Outpost_Science : Outpost
    {
        public override void Tick()
        {
            base.Tick();
            if (Find.ResearchManager.currentProj == null || Packing) return;
            foreach (var pawn in AllPawns)
                if (Find.ResearchManager.currentProj is not null)
                    Find.ResearchManager.ResearchPerformed(pawn.skills.GetSkill(SkillDefOf.Intellectual).Level, pawn);
        }
    }
}