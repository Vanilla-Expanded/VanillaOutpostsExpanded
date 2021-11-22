using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Outposts;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace VOE
{
    [StaticConstructorOnStartup]
    public class Outpost_Defensive : Outpost
    {
        private static readonly Texture2D DeployTex = ContentFinder<Texture2D>.Get("UI/DeployDefensiveGarrison");
        private static readonly ResearchProjectDef DropPods = ResearchProjectDef.Named("TransportPod");

        static Outpost_Defensive()
        {
            if (OutpostsMod.Outposts.Any(outpost => typeof(Outpost_Defensive).IsAssignableFrom(outpost.worldObjectClass)))
                OutpostsMod.Harm.Patch(AccessTools.Method(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker"),
                    new HarmonyMethod(typeof(Outpost_Defensive), nameof(UpdateRaidTarget)));
        }

        public static void UpdateRaidTarget(IncidentParms parms)
        {
            var defense = Find.WorldObjects.AllWorldObjects.OfType<Outpost_Defensive>().InRandomOrder().FirstOrDefault(d => Rand.Chance(0.25f));
            if (defense == null) return;
            if (!(parms.target is Map targetMap)) return;
            if (!TileFinder.TryFindPassableTileWithTraversalDistance(targetMap.Tile, 2, 5, out var tile,
                t => !Find.WorldObjects.AnyMapParentAt(t))) tile = defense.Tile;
            var map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, new IntVec3(75, 75, 75), WorldObjectDefOf.Ambush);
            Find.LetterStack.ReceiveLetter("Outposts.Letters.Intercept.Label".Translate(),
                "Outposts.Letters.Intercept.Text".Translate(targetMap.Parent.LabelCap, defense.Name),
                LetterDefOf.PositiveEvent, new LookTargets(new List<GlobalTargetInfo> {defense, map.Parent}));
            parms.points = (targetMap.wealthWatcher.WealthTotal - defense.AllPawns.Sum(p => p.MarketValue)) / 2f;
            parms.target = map;
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
                            Find.Targeter.BeginTargeting(TargetingParameters.ForDropPodsDestination(), localTarget =>
                            {
                                var pods = (TravelingTransportPods) WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.TravelingTransportPods);
                                foreach (var pawn in AllPawns)
                                {
                                    var info = new ActiveDropPodInfo
                                    {
                                        SingleContainedThing = pawn,
                                        leaveSlag = false,
                                        openDelay = 30
                                    };
                                    pods.AddPod(info, false);
                                }

                                pods.Tile = Tile;
                                pods.SetFaction(Faction);
                                pods.destinationTile = target.Tile;
                                pods.arrivalAction = new TransportPodsArrivalAction_LandInSpecificCell(parent, localTarget.Cell, false);
                                Find.WorldObjects.Add(pods);
                            });
                            return true;
                        }

                        return false;
                    }, false, null, false, null, null,
                    target => Find.WorldGrid.ApproxDistanceInTiles(target.Tile, Tile) <= Range && target.HasWorldObject && target.WorldObject is MapParent parent &&
                              parent.HasMap && parent.Map.mapPawns.AnyFreeColonistSpawned),
                defaultLabel = "Outposts.Commands.Deploy.Label".Translate(),
                defaultDesc = "Outposts.Commands.Deploy.Desc".Translate(),
                disabled = !DropPods.IsFinished,
                disabledReason = "Outposts.Commands.Deploy.Disabled".Translate(),
                icon = DeployTex
            });
        }
    }
}