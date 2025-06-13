using Outposts;
using RimWorld;

namespace VOE;

public class Outpost_Encampment : Outpost
{
    public override void Tick()
    {
        base.Tick();
        if (Packing) return;
        foreach (var pawn in AllPawns)
        {
            if (pawn.needs.food != null) pawn.needs.food.CurLevel += Need_Food.BaseFoodFallPerTick;
            if (pawn.needs.rest != null) pawn.needs.rest.CurLevel += Need_Rest.BaseRestGainPerTick;
            if (pawn.health != null)
            {
                if (pawn.health.HasHediffsNeedingTend())
                    foreach (var hediff in pawn.health.hediffSet.GetHediffsTendable())
                        hediff.Tended(1f, 1f);
                pawn.health.HealthTick();
            }
        }
    }
}
