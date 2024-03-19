﻿using System.Collections.Generic;
using Outposts;
using RimWorld;
using Verse;

namespace VOE;

public class Outpost_Drilling : Outpost
{
    [PostToSetings("Outposts.Settings.WorkToDrill", PostToSetingsAttribute.DrawMode.Time, 7 * GenDate.TicksPerDay, GenDate.TicksPerDay, GenDate.TicksPerYear)]
    public int WorkToDrill = 7 * 60000;

    private int workDone;

    private bool Ready => workDone >= WorkToDrill * 20;


    public override void PostMake()
    {
        base.PostMake();
        workDone = 0;
    }

    public override IEnumerable<Thing> ProducedThings() => Ready ? base.ProducedThings() : new List<Thing>();

    public override void Tick()
    {
        base.Tick();
        if (!Ready && !Packing) workDone += TotalSkill(SkillDefOf.Construction);
    }

    public override string ProductionString() =>
        Ready
            ? base.ProductionString()
            : "Outposts.Drilling".Translate(((float)workDone / (WorkToDrill * 20)).ToStringPercent(),
                ((WorkToDrill * 20 - workDone) / TotalSkill(SkillDefOf.Construction)).ToStringTicksToPeriodVerbose().Colorize(ColoredText.DateTimeColor));

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref workDone, "workUntilReady");
    }
}
