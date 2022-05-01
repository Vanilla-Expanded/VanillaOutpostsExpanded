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

namespace VOE
{
    [StaticConstructorOnStartup]
    public class Outpost_Defensive : Outpost
    {
        private static readonly Func<IncidentWorker_RaidEnemy, IncidentParms, bool> generateFaction;

        [PostToSetings("Outposts.Settings.DoIntercept", PostToSetingsAttribute.DrawMode.Checkbox, true)]
        public bool DoIntercept = true;

        [PostToSetings("Outposts.Settings.NeedPods", PostToSetingsAttribute.DrawMode.Checkbox, true)]
        public bool NeedPods = true;

        static Outpost_Defensive()
        {
            if (OutpostsMod.Outposts.Any(outpost => typeof(Outpost_Defensive).IsAssignableFrom(outpost.worldObjectClass)))
                OutpostsMod.Harm.Patch(AccessTools.Method(typeof(IncidentWorker_RaidEnemy), "TryExecuteWorker"),
                    new HarmonyMethod(typeof(Outpost_Defensive), nameof(UpdateRaidTarget)));
            generateFaction = AccessTools.MethodDelegate<Func<IncidentWorker_RaidEnemy, IncidentParms, bool>>(AccessTools.Method(
                typeof(IncidentWorker_RaidEnemy),
                "TryResolveRaidFaction"));
        }

        public static string CanSpawnOnWith(int tile, List<Pawn> pawns) =>
            pawns.Where(p => p.RaceProps.Humanlike).Any(p => p.WorkTagIsDisabled(WorkTags.Violent) || p.equipment.Primary is null)
                ? "Outposts.MustBeArmed".Translate()
                : null;

        public static string RequirementsString(int tile, List<Pawn> pawns) =>
            "Outposts.MustBeArmed".Translate().Requirement(
                pawns.Where(p => p.RaceProps.Humanlike).All(p => !p.WorkTagIsDisabled(WorkTags.Violent) && p.equipment.Primary is not null));

        public static bool UpdateRaidTarget(IncidentParms parms, IncidentWorker_RaidEnemy __instance)
        {
            var defense = Find.WorldObjects.AllWorldObjects.OfType<Outpost_Defensive>().Where(outpost => outpost.PawnCount > 1).InRandomOrder()
                .FirstOrDefault(d => d.DoIntercept && Rand.Chance(0.25f));
            if (defense == null) return true;
            if (parms.target is not Map targetMap) return true;
            if (!TileFinder.TryFindPassableTileWithTraversalDistance(targetMap.Tile, 2, 5, out var tile,
                t => !Find.WorldObjects.AnyMapParentAt(t) && Find.WorldGrid.ApproxDistanceInTiles(defense.Tile, t) <= 7f)) tile = defense.Tile;
            LongEventHandler.QueueLongEvent(() =>
            {
                var map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, new IntVec3(75, 1, 75), DefDatabase<WorldObjectDef>.GetNamed("VOE_AmbushedRaid"));
                parms.target = map;
                parms.points = StorytellerUtility.DefaultThreatPointsNow(map);
                generateFaction(__instance, parms);
                if (map.Parent is AmbushedRaid ambushedRaid) ambushedRaid.DefensiveOutpost = defense;
                var pawns = defense.AllPawns.InRandomOrder().Skip(1).ToList();
                var defaultPawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, parms);
                defaultPawnGroupMakerParms.generateFightersOnly = true;
                defaultPawnGroupMakerParms.dontUseSingleUseRocketLaunchers = true;
                var enemies = PawnGroupMakerUtility.GeneratePawns(defaultPawnGroupMakerParms).ToList();
                MultipleCaravansCellFinder.FindStartingCellsFor2Groups(map, out var playerSpot, out var enemySpot);
                foreach (var pawn in pawns) GenSpawn.Spawn(defense.RemovePawn(pawn), CellFinder.RandomSpawnCellForPawnNear(playerSpot, map), map, Rot4.Random);
                foreach (var enemy in enemies) GenSpawn.Spawn(enemy, CellFinder.RandomSpawnCellForPawnNear(enemySpot, map), map, Rot4.Random);
                var lordJob = new LordJob_AssaultColony(parms.faction, true, false);
                LordMaker.MakeNewLord(parms.faction, lordJob, map, enemies);
                var letterLabel = "Outposts.Letters.Intercept.Label".Translate();
                var letterText = "Outposts.Letters.Intercept.Text".Translate(targetMap.Parent.LabelCap, defense.Name);
                PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(enemies, ref letterLabel, ref letterText,
                    "LetterRelatedPawnsGroupGeneric".Translate(Faction.OfPlayer.def.pawnsPlural), true);
                Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.PositiveEvent,
                    new LookTargets(new List<GlobalTargetInfo> {defense, map.Parent}));
                Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
            }, "GeneratingMapForNewEncounter", false, null, false);
            return false;
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
                                pods.Tile = Tile;
                                pods.SetFaction(Faction);
                                pods.destinationTile = target.Tile;
                                pods.arrivalAction = new TransportPodsArrivalAction_LandInSpecificCell(parent, localTarget.Cell, false);
                                foreach (var pawn in AllPawns.InRandomOrder().Skip(1).ToList())
                                {
                                    var info = new ActiveDropPodInfo
                                    {
                                        SingleContainedThing = RemovePawn(pawn),
                                        leaveSlag = false,
                                        openDelay = 30
                                    };
                                    pods.AddPod(info, false);
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
                disabled = ReinforcementsDisabled(out var reason),
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

            if (PawnCount < 2)
            {
                reason = "Outposts.Commands.Deploy.Only1".Translate();
                return true;
            }

            reason = "";
            return false;
        }
    }

    public class AmbushedRaid : MapParent
    {
        public Outpost_Defensive DefensiveOutpost;

        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
        {
            if (!Map.mapPawns.FreeColonists.Any())
            {
                Find.LetterStack.ReceiveLetter("Outposts.Defensive.Lost.Label".Translate(), "Outposts.Defensive.Lost.Desc".Translate(),
                    LetterDefOf.NeutralEvent);
                alsoRemoveWorldObject = true;
                return true;
            }

            if (Map.mapPawns.AllPawns.Where(p => p.RaceProps.Humanlike).All(p => p.Faction is {IsPlayer: true}))
            {
                Find.LetterStack.ReceiveLetter("Outposts.Defensive.Won.Label".Translate(), "Outposts.Defensive.Won.Desc".Translate(),
                    LetterDefOf.PositiveEvent);
                foreach (var pawn in Map.mapPawns.AllPawns.Where(p => p.RaceProps.Humanlike).ToList())
                {
                    pawn.DeSpawn();
                    DefensiveOutpost.AddPawn(pawn);
                }

                alsoRemoveWorldObject = true;
                return true;
            }

            alsoRemoveWorldObject = false;
            return false;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_References.Look(ref DefensiveOutpost, "defensiveOutpost");
        }
    }

    [StaticConstructorOnStartup]
    public static class TexDefensive
    {
        public static readonly Texture2D DeployTex = ContentFinder<Texture2D>.Get("UI/DeployDefensiveGarrison");
    }
}