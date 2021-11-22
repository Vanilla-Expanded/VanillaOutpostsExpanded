using System.Collections.Generic;
using System.Linq;
using Outposts;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace VOE
{
    [StaticConstructorOnStartup]
    public class Outpost_Artillery : Outpost
    {
        private static readonly Texture2D FireTex = ContentFinder<Texture2D>.Get("UI/ArtilleryFireMission");
        private readonly Queue<Pair<int, Pawn>> fireTimes = new();
        private int averageSkill;
        private int cooldownTicksLeft;
        private GlobalTargetInfo currentTarget;
        public virtual IntRange TicksBetweenShots => new(60, 360);

        public virtual int CooldownTicks => 60000;

        public override void Tick()
        {
            base.Tick();
            if (cooldownTicksLeft > 0) cooldownTicksLeft--;
            if (fireTimes.Count > 0 && fireTimes.Peek().First <= Find.TickManager.TicksGame)
            {
                var pawn = fireTimes.Dequeue().Second;
                var proj = (Projectile) GenSpawn.Spawn(ThingDef.Named("Bullet_Shell_HighExplosive"),
                    CellFinder.RandomEdgeCell(Find.WorldGrid.GetRotFromTo(currentTarget.Tile, Tile), currentTarget.Map), currentTarget.Map);
                var radius = (20 - averageSkill) / 5;
                var local = currentTarget.HasThing ? new LocalTargetInfo(currentTarget.Thing) : new LocalTargetInfo(currentTarget.Cell);
                if (radius > 0.5)
                {
                    var num = VerbUtility.CalculateAdjustedForcedMiss(radius, currentTarget.Cell);
                    if (num > 0.5f)
                    {
                        var max = GenRadial.NumCellsInRadius(num);
                        var num2 = Rand.Range(0, max);
                        if (num2 > 0)
                        {
                            var c = currentTarget.Cell + GenRadial.RadialPattern[num2];
                            var projectileHitFlags = ProjectileHitFlags.NonTargetWorld & ~ProjectileHitFlags.NonTargetPawns;
                            if (Rand.Chance(0.5f)) projectileHitFlags = ProjectileHitFlags.All;

                            proj.Launch(pawn, c, local, projectileHitFlags);
                        }
                    }
                }
                else
                    proj.Launch(pawn, local, local, ProjectileHitFlags.IntendedTarget, true);
            }
        }

        public virtual void Fire()
        {
            var curTime = Find.TickManager.TicksGame;
            foreach (var pawn in AllPawns)
            {
                curTime += TicksBetweenShots.RandomInRange;
                fireTimes.Enqueue(new Pair<int, Pawn>(curTime, pawn));
            }

            cooldownTicksLeft = CooldownTicks;
        }

        public override void RecachePawnTraits()
        {
            averageSkill = (int) AllPawns.Select(p => p.skills.GetSkill(SkillDefOf.Shooting).Level).Concat(AllPawns.Select(p => p.skills.GetSkill(SkillDefOf.Intellectual).Level))
                .Average();
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            return base.GetGizmos().Append(new Command_Action
            {
                action = () => Find.WorldTargeter.BeginTargeting(target =>
                    {
                        if (target.HasWorldObject && target.WorldObject is MapParent parent && parent.HasMap)
                        {
                            CameraJumper.TryJump(parent.Map.Center, parent.Map);
                            Find.Targeter.BeginTargeting(new TargetingParameters
                            {
                                canTargetBuildings = true,
                                canTargetPawns = true,
                                canTargetLocations = true
                            }, localTarget =>
                            {
                                currentTarget = localTarget.ToGlobalTargetInfo(parent.Map);
                                Fire();
                            });
                            return true;
                        }

                        return false;
                    }, false, null, false, null, null,
                    target => Find.WorldGrid.ApproxDistanceInTiles(target.Tile, Tile) <= Range && target.HasWorldObject && target.WorldObject is MapParent parent &&
                              parent.HasMap && parent.Map.mapPawns.AnyFreeColonistSpawned),
                defaultLabel = "Outposts.Commands.Fire.Label".Translate(),
                defaultDesc = "Outposts.Commands.Fire.Desc".Translate(),
                disabled = cooldownTicksLeft > 0,
                disabledReason = "Outposts.Commands.Disabled.Cooldown".Translate(),
                icon = FireTex
            });
        }

        public override string GetInspectString() => base.GetInspectString() +
                                                     (cooldownTicksLeft > 0 ? "\n" + "Outposts.Cooldown".Translate(cooldownTicksLeft.ToStringTicksToPeriodVerbose()).RawText : "");
    }
}