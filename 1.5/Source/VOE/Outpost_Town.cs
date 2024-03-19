﻿using System.Collections.Generic;
using System.Linq;
using Outposts;
using RimWorld;
using Verse;

namespace VOE;

public class Outpost_Town : Outpost
{
    [PostToSetings("Outposts.Settings.Chance", PostToSetingsAttribute.DrawMode.Percentage, 1f, 0.01f, 1f)]
    public float Chance = 1f;

    public override void Produce()
    {
        var newPawns = new List<Pawn>();
        foreach (var pawn in CapablePawns)
            if (Rand.Chance(pawn.skills.GetSkill(SkillDefOf.Social).Level * Chance / 100f))
            {
                var newPawn = PawnGenerator.GeneratePawn(pawn.kindDef, pawn.Faction);
                newPawn.SetFaction(pawn.Faction, pawn);
                newPawns.Add(newPawn);
            }

        if (newPawns.Any())
            Find.LetterStack.ReceiveLetter("Outposts.Letters.Recruit.Label".Translate(Name),
                "Outposts.Letters.Recruit.Desc".Translate(Name, newPawns.Select(pawn => pawn.NameFullColored.ToString()).ToLineList("  - ")),
                LetterDefOf.PositiveEvent,
                new LookTargets(Gen.YieldSingle(this)));

        foreach (var pawn in newPawns) AddPawn(pawn);
    }

    public override string ProductionString() => "Outposts.WillProduce.Pawns".Translate(TimeTillProduction).RawText;

    public static string CanSpawnOnWith(int tile, List<Pawn> pawns) =>
        Find.WorldObjects.Settlements.Count(s => Find.WorldGrid.ApproxDistanceInTiles(s.Tile, tile) < 10) < 3
            ? "Outposts.NearbySettlements".Translate(3, 10)
            : null;

    public static string RequirementsString(int tile, List<Pawn> pawns) =>
        "Outposts.NearbySettlements".Translate(3, 10)
           .Requirement(
                Find.WorldObjects.Settlements.Count(s => Find.WorldGrid.ApproxDistanceInTiles(s.Tile, tile) < 10) >= 3);
}
