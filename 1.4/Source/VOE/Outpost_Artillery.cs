using System.Collections.Generic;
using System.Linq;
using Outposts;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace VOE;

[StaticConstructorOnStartup]
public class Outpost_Artillery : Outpost
{
    private static readonly Texture2D FireTex = ContentFinder<Texture2D>.Get("UI/ArtilleryFireMission");

    [PostToSetings("Outposts.Settings.Cooldown", PostToSetingsAttribute.DrawMode.Time, 60000, GenDate.TicksPerHour, GenDate.TicksPerDay * 7)]
    public int CooldownTicks = 60000;

    private int cooldownTicksLeft;

    public override void Tick()
    {
        base.Tick();
        if (cooldownTicksLeft > 0) cooldownTicksLeft--;
    }

    public virtual void Fire(GlobalTargetInfo target)
    {
        var shot = (TravellingArtilleryStrike)WorldObjectMaker.MakeWorldObject(DefDatabase<WorldObjectDef>.GetNamed("VOE_TravellingArtilleryStrike"));
        shot.Fire(this, target);
        cooldownTicksLeft = Mathf.RoundToInt(CooldownTicks * OutpostsMod.Settings.TimeMultiplier);
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Values.Look(ref cooldownTicksLeft, "cooldown");
    }

    public override IEnumerable<Gizmo> GetGizmos()
    {
        return base.GetGizmos()
           .Append(new Command_Action
            {
                action = () => Find.WorldTargeter.BeginTargeting(target =>
                    {
                        if (target.HasWorldObject && target.WorldObject is MapParent { HasMap: true } parent)
                        {
                            CameraJumper.TryJump(parent.Map.Center, parent.Map);
                            Find.Targeter.BeginTargeting(new TargetingParameters
                            {
                                canTargetBuildings = true,
                                canTargetPawns = true,
                                canTargetLocations = true
                            }, localTarget => { Fire(localTarget.ToGlobalTargetInfo(parent.Map)); });
                            return true;
                        }

                        return false;
                    }, false, null, false, null, null,
                    target => Find.WorldGrid.ApproxDistanceInTiles(target.Tile, Tile) <= Range && target.HasWorldObject
                                                                                               && target.WorldObject is MapParent { HasMap: true } parent &&
                                                                                                  parent.Map.mapPawns.AnyFreeColonistSpawned),
                defaultLabel = "Outposts.Commands.Fire.Label".Translate(),
                defaultDesc = "Outposts.Commands.Fire.Desc".Translate(),
                disabled = cooldownTicksLeft > 0,
                disabledReason = "Outposts.Commands.Disabled.Cooldown".Translate(),
                icon = FireTex
            });
    }

    public override string GetInspectString() =>
        base.GetInspectString() +
        (cooldownTicksLeft > 0 ? "Outposts.Cooldown".Translate(cooldownTicksLeft.ToStringTicksToPeriodVerbose()).RawText : "").Line();
}

public class TravellingArtilleryStrike : WorldObject
{
    private const float TravelSpeedPerShellSpeed = 0.00025f / 300;
    private int averageSkill;
    private int destinationTile;
    private int initialTile;
    private int numShots;
    private List<Pawn> pawns;
    private GlobalTargetInfo target;
    private float travelledPct;
    public override Vector3 DrawPos => Vector3.Slerp(Start, End, travelledPct);

    public override string Label => numShots > 1 ? base.Label + $" x{numShots}" : base.Label;

    private Vector3 Start => Find.WorldGrid.GetTileCenter(initialTile);
    private Vector3 End => Find.WorldGrid.GetTileCenter(destinationTile);

    private float TravelledPctStepPerTick
    {
        get
        {
            if (Start == End)
                return 1;

            var sphericalDist = GenMath.SphericalDistance(Start.normalized, End.normalized);
            if (sphericalDist == 0)
                return 1;

            return ThingDef.Named("Bullet_Shell_HighExplosive").projectile.speed * TravelSpeedPerShellSpeed / sphericalDist;
        }
    }

    public void Fire(Outpost from, GlobalTargetInfo to)
    {
        pawns = from.CapablePawns.ToList();
        averageSkill = (int)pawns.Select(p => p.skills.GetSkill(SkillDefOf.Shooting).Level)
           .Concat(pawns.Select(p => p.skills.GetSkill(SkillDefOf.Intellectual).Level))
           .Average();
        numShots = pawns.Count;
        initialTile = from.Tile;
        destinationTile = to.Tile;
        Tile = destinationTile;
        target = to;
        Find.WorldObjects.Add(this);
    }

    public override void Tick()
    {
        base.Tick();
        travelledPct += TravelledPctStepPerTick;
        if (travelledPct >= 1)
        {
            travelledPct = 1;
            Arrived();
        }
    }

    private void Arrived()
    {
        foreach (var pawn in pawns)
        {
            var proj = (Projectile)GenSpawn.Spawn(ThingDef.Named("Bullet_Shell_HighExplosive"),
                CellFinder.RandomEdgeCell(Find.WorldGrid.GetRotFromTo(target.Tile, Tile), target.Map), target.Map);
            var radius = (20 - averageSkill) / 5;
            var local = target.HasThing ? new LocalTargetInfo(target.Thing) : new LocalTargetInfo(target.Cell);
            if (radius > 0.5)
            {
                var num = VerbUtility.CalculateAdjustedForcedMiss(radius, target.Cell);
                if (num > 0.5f)
                {
                    var max = GenRadial.NumCellsInRadius(num);
                    var num2 = Rand.Range(0, max);
                    if (num2 > 0)
                    {
                        var c = target.Cell + GenRadial.RadialPattern[num2];
                        var projectileHitFlags = ProjectileHitFlags.NonTargetWorld & ~ProjectileHitFlags.NonTargetPawns;
                        if (Rand.Chance(0.5f)) projectileHitFlags = ProjectileHitFlags.All;

                        proj.Launch(pawn, c, local, projectileHitFlags);
                    }
                }
            }
            else
                proj.Launch(pawn, local, local, ProjectileHitFlags.IntendedTarget, true);
        }

        Find.WorldObjects.Remove(this);
    }

    public override void ExposeData()
    {
        Scribe_Values.Look(ref initialTile, "initialTile");
        Scribe_Values.Look(ref destinationTile, "destinationTile");
        Scribe_Values.Look(ref travelledPct, "travelledPct");
        Scribe_Collections.Look(ref pawns, "pawns", LookMode.Reference);
        Scribe_TargetInfo.Look(ref target, "target");
        Scribe_Values.Look(ref averageSkill, "averageSkill");
        Scribe_Values.Look(ref numShots, "numShots");
        base.ExposeData();
    }
}
