using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Outposts;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;
using Verse.AI.Group;

namespace VOE;

[StaticConstructorOnStartup]
public class Outpost_Defensive : Outpost
{
    private static readonly Func<IncidentWorker_RaidEnemy, IncidentParms, bool> generateFaction;

    public static bool DoRaid;

    [PostToSetings("Outposts.Settings.DoIntercept", PostToSetingsAttribute.DrawMode.Checkbox, true)]
    public bool DoIntercept = true;

    [PostToSetings("Outposts.Settings.NeedFuelAmount", PostToSetingsAttribute.DrawMode.IntSlider, 100, 1, 500)]
    public int FuelAmount = 100;

    [PostToSetings("Outposts.Settings.InterceptDifficultyMultiplier", PostToSetingsAttribute.DrawMode.Slider, 1f, 0.1f, 3f,
        "Outposts.Settings.InterceptDifficultyMultiplierTooltip")]
    public float InterceptDifficultyMultiplier = 1f;

    //Adding this to balance things a bit. I assume they get their own steel, but fuel could be a bit hard to get.
    [PostToSetings("Outposts.Settings.NeedFuel", PostToSetingsAttribute.DrawMode.Checkbox, false)]
    public bool NeedFuel = false;

    [PostToSetings("Outposts.Settings.NeedPods", PostToSetingsAttribute.DrawMode.Checkbox, true)]
    public bool NeedPods = true;

    static Outpost_Defensive()
    {
        if (OutpostsMod.Outposts.Any(outpost => typeof(Outpost_Defensive).IsAssignableFrom(outpost.worldObjectClass)))
            OutpostsMod.Harm.Patch(AccessTools.Method(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker"),
                new(typeof(Outpost_Defensive), nameof(InterceptRaid)));
        /*generateFaction = AccessTools.MethodDelegate<Func<IncidentWorker_RaidEnemy, IncidentParms, bool>>(AccessTools.Method(
            typeof(IncidentWorker_RaidEnemy),
            "TryResolveRaidFaction"));*/
    }

    public static string CanSpawnOnWith(int tile, List<Pawn> pawns) =>
        pawns.Where(p => p.RaceProps.Humanlike).Any(p => p.WorkTagIsDisabled(WorkTags.Violent) || p.equipment.Primary is null)
            ? "Outposts.MustBeArmed".Translate()
            : null;

    public static string RequirementsString(int tile, List<Pawn> pawns) =>
        "Outposts.MustBeArmed".Translate()
           .Requirement(
                pawns.Where(p => p.RaceProps.Humanlike).All(p => !p.WorkTagIsDisabled(WorkTags.Violent) && p.equipment.Primary is not null));


    public static bool InterceptRaid(IncidentParms parms, IncidentWorker_RaidEnemy __instance)
    {
        if (!DoRaid) return true;
        var defenses = Find.WorldObjects.AllWorldObjects.OfType<Outpost_Defensive>()
           .Where(outpost => outpost.PawnCount > 0 && outpost.DoIntercept);
        var combatSkills = defenses.Sum(x => (x.TotalSkill(SkillDefOf.Shooting) + x.TotalSkill(SkillDefOf.Melee)) * x.InterceptDifficultyMultiplier);
        parms.points *= (1 - GetRaidSizeReductionFactor(combatSkills));
        DoRaid = true;
        __instance.TryExecute(parms);
        DoRaid = false;
        return false;
    }

    public static float GetRaidSizeReductionFactor(float totalSkills)
    {
        float reductionFactor = (totalSkills / 200f) * 0.5f;
        return Mathf.Min(reductionFactor, 0.5f);
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
                            Find.Targeter.BeginTargeting(TargetingParameters.ForDropPodsDestination(), localTarget =>
                            {
                                var pods = (TravellingTransporters)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.TravellingTransporters);
                                pods.Tile = Tile;
                                pods.SetFaction(Faction);
                                pods.destinationTile = target.Tile;
                                pods.arrivalAction = new TransportersArrivalAction_LandInSpecificCell(parent, localTarget.Cell, Rot4.South, false);
                                if (NeedFuel)
                                    foreach (var fuel in TakeItems(ThingDefOf.Chemfuel, FuelAmount))
                                        fuel.Destroy();
                                //Trainability is because my poor drop camels landing with the melee pawns on the enemey. Poor things :'(. But now I also want to droppod a bunch of nasty genetic creations to save me
                                //orderyby to leave a human pawn rather then random which can be animals as pretty sure that'd cause issues
                                foreach (var pawn in AllPawns.Where(x => x.RaceProps.trainability != TrainabilityDefOf.None)
                                            .OrderByDescending(x => x.IsColonist)
                                            .Skip(1)
                                            .ToList())
                                {
                                    var info = new ActiveTransporterInfo
                                    {
                                        SingleContainedThing = RemovePawn(pawn),
                                        leaveSlag = false,
                                        openDelay = 30
                                    };
                                    pods.AddTransporter(info, false);
                                }

                                Find.WorldObjects.Add(pods);
                            });
                            return true;
                        }

                        return false;
                    }, false, null, false, null, null,
                    target => Find.WorldGrid.ApproxDistanceInTiles(target.Tile, Tile) <= Range && target.HasWorldObject &&
                              target.WorldObject is MapParent parent &&
                              parent.HasMap && parent.Map.mapPawns.AnyFreeColonistSpawned),
                defaultLabel = "Outposts.Commands.Deploy.Label".Translate(),
                defaultDesc = "Outposts.Commands.Deploy.Desc".Translate(),
                Disabled = ReinforcementsDisabled(out var reason),
                disabledReason = reason,
                icon = TexDefensive.DeployTex
            });
    }

    private bool ReinforcementsDisabled(out string reason)
    {
        if (NeedPods && !Outposts_DefOf.TransportPod.IsFinished)
        {
            reason = "Outposts.Commands.Deploy.Disabled".Translate();
            return true;
        }

        if (NeedFuel)
            if (Things.Sum(delegate(Thing x)
                {
                    if (x.def == ThingDefOf.Chemfuel) return x.stackCount;
                    return 0;
                }) < FuelAmount)
            {
                reason = "Outposts.Commands.Deploy.NotEnoughFuel".Translate();
                return true;
            }

        if (PawnCount < 2)
        {
            reason = "Outposts.Commands.Deploy.Only1".Translate();
            return true;
        }

        reason = "";
        return false;
    }
}

[StaticConstructorOnStartup]
public static class TexDefensive
{
    public static readonly Texture2D DeployTex = ContentFinder<Texture2D>.Get("UI/DeployDefensiveGarrison");
}
