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

        public static string CanSpawnOnWith(int tile, List<Pawn> pawns) =>
            pawns.Where(p => p.RaceProps.Humanlike).Any(p => p.WorkTagIsDisabled(WorkTags.Violent) || p.equipment.Primary is null)
                ? "Outposts.MustBeArmed".Translate()
                : null;

        public static string RequirementsString(int tile, List<Pawn> pawns) =>
            Requirement("Outposts.MustBeArmed".Translate(),
                pawns.Where(p => p.RaceProps.Humanlike).All(p => !p.WorkTagIsDisabled(WorkTags.Violent) && p.equipment.Primary is not null));

        public static void UpdateRaidTarget(IncidentParms parms)
        {
            var defense = Find.WorldObjects.AllWorldObjects.OfType<Outpost_Defensive>().Where(outpost => outpost.PawnCount > 0).InRandomOrder()
                .FirstOrDefault(_ => Rand.Chance(0.25f));
            if (defense == null) return;
            if (parms.target is not Map targetMap) return;
            if (!TileFinder.TryFindPassableTileWithTraversalDistance(targetMap.Tile, 2, 5, out var tile,
                t => !Find.WorldObjects.AnyMapParentAt(t))) tile = defense.Tile;
            var map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, new IntVec3(75, 1, 75), DefDatabase<WorldObjectDef>.GetNamed("VOE_AmbushedRaid"));
            var ambushedRaid = (AmbushedRaid) map.Parent;
            ambushedRaid.DefensiveOutpost = defense;
            var pawns = defense.AllPawns.ToList();
            foreach (var pawn in pawns)
            {
                GenPlace.TryPlaceThing(defense.RemovePawn(pawn), map.Center, map, ThingPlaceMode.Near);
                pawn.drafter.Drafted = true;
            }

            Find.LetterStack.ReceiveLetter("Outposts.Letters.Intercept.Label".Translate(),
                "Outposts.Letters.Intercept.Text".Translate(targetMap.Parent.LabelCap, defense.Name),
                LetterDefOf.PositiveEvent, new LookTargets(new List<GlobalTargetInfo> {defense, map.Parent}));
            parms.points *= pawns.Sum(p => p.MarketValue) / targetMap.wealthWatcher.WealthTotal;
            parms.target = map;
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            return base.GetGizmos().Append(new Command_Action
            {
                action = () => Find.WorldTargeter.BeginTargeting(target =>
                    {
                        if (target.HasWorldObject && target.WorldObject is MapParent {HasMap: true} parent)
                        {
                            CameraJumper.TryJump(parent.Map.Center, parent.Map);
                            Find.Targeter.BeginTargeting(TargetingParameters.ForDropPodsDestination(), localTarget =>
                            {
                                var pods = (TravelingTransportPods) WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.TravelingTransportPods);
                                foreach (var pawn in AllPawns.InRandomOrder().Skip(1))
                                {
                                    var info = new ActiveDropPodInfo
                                    {
                                        SingleContainedThing = RemovePawn(pawn),
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

    public class AmbushedRaid : MapParent
    {
        public Outpost_Defensive DefensiveOutpost;

        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
        {
            if (!Map.mapPawns.FreeColonists.Any())
            {
                Find.LetterStack.ReceiveLetter("Outposts.Defensive.Lost.Label".Translate(), "Outposts.Defensive.Lost.Desc".Translate(), LetterDefOf.NeutralEvent);
                alsoRemoveWorldObject = true;
                return true;
            }

            if (Map.mapPawns.AllPawns.Where(p => p.RaceProps.Humanlike).All(p => p.Faction is {IsPlayer: true}))
            {
                Find.LetterStack.ReceiveLetter("Outposts.Defensive.Won.Label".Translate(), "Outposts.Defensive.Won.Desc".Translate(), LetterDefOf.PositiveEvent);
                foreach (var pawn in Map.mapPawns.AllPawns.Where(p => p.RaceProps.Humanlike))
                {
                    pawn.DeSpawn();
                    Find.WorldPawns.PassToWorld(pawn, PawnDiscardDecideMode.KeepForever);
                    DefensiveOutpost.AddPawn(pawn);
                }

                alsoRemoveWorldObject = true;
                return true;
            }

            alsoRemoveWorldObject = false;
            return false;
        }
    }
}