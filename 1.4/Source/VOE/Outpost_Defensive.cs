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
        [PostToSetings("Outposts.Settings.InterceptDifficultyMultiplier", PostToSetingsAttribute.DrawMode.Slider, 1f,0.1f,3f, "Outposts.Settings.InterceptDifficultyMultiplierTooltip")]
        public float InterceptDifficultyMultiplier = 1f;

        [PostToSetings("Outposts.Settings.NeedPods", PostToSetingsAttribute.DrawMode.Checkbox, true)]
        public bool NeedPods = true;
        //Adding this to balance things a bit. I assume they get their own steel, but fuel could be a bit hard to get.
        [PostToSetings("Outposts.Settings.NeedFuel", PostToSetingsAttribute.DrawMode.Checkbox, false)]
        public bool NeedFuel = false;
        [PostToSetings("Outposts.Settings.NeedFuelAmount", PostToSetingsAttribute.DrawMode.IntSlider,100,min:1, max:500)]
        public int FuelAmount = 100;
        

        public static bool DoRaid = false;

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
        public override float ResolveRaidPoints(IncidentParms parms, float rangeMin = 0.35F, float rangeMax = 0.45F) //Defense gets turrets sandbags and is where combat pawns go
        {
            return base.ResolveRaidPoints(parms, rangeMin, rangeMax);
        }

        public static string RequirementsString(int tile, List<Pawn> pawns) =>
            "Outposts.MustBeArmed".Translate().Requirement(
                pawns.Where(p => p.RaceProps.Humanlike).All(p => !p.WorkTagIsDisabled(WorkTags.Violent) && p.equipment.Primary is not null));
        

        public static bool UpdateRaidTarget(IncidentParms parms, IncidentWorker_RaidEnemy __instance)
        {
            var defense = Find.WorldObjects.AllWorldObjects.OfType<Outpost_Defensive>().Where(outpost => outpost.PawnCount > 1).InRandomOrder()
                .FirstOrDefault(d => d.DoIntercept && Rand.Chance(0.25f) && !DoRaid);
            if (defense == null) return true;
            if (parms.target is not Map targetMap) return true;
            if(!generateFaction(__instance, parms)) return true;
            defense.CanIntercept(parms, __instance);
            return false;
        }
        //To make the intercept raids as strong as I think they should be. I figured it also wouldnt be fair to throw ppls pawns to a meat grinder unprepared. 
        //So made it a player choice cause there is just no way to estimate it on my side
        public void CanIntercept(IncidentParms parms, IncidentWorker_RaidEnemy raid)
        {
            float parmPoints = parms.points;//storing this because I want to generate with estimated points then set it back
            parms.points = ResolveRaidPoints(parms, 0.4f, 0.50f)*InterceptDifficultyMultiplier;       
            var groupParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, parms, true);
            groupParms.generateFightersOnly = true;
            groupParms.dontUseSingleUseRocketLaunchers = true;
            //I dont want to generate the pawns just to pass to world if declined confusing things so just using example. Hence text will say estimate
            var enemies = PawnGroupMakerUtility.GeneratePawnKindsExample(groupParms).ToList();
            if (!enemies.Any()) { return; }
            CameraJumper.TryJumpAndSelect(this);
            DiaNode nodeRoot = new DiaNode("Outposts.Intercept.CanIntercept".Translate(Name, parms.faction.Name, parms.target.ChangeType<Map>().Parent.LabelCap, PawnUtility.PawnKindsToLineList(enemies, "  - ")));
            var intercept = new DiaOption("Outposts.Intercept.InterceptRaid".Translate());
            intercept.action = delegate () { StartIntercept(parms, raid); };
            intercept.resolveTree = true;
            nodeRoot.options.Add(intercept);
            var decline = new DiaOption("Outposts.Intercept.DontIntercept".Translate());
            decline.action = delegate () {
                parms.points = parmPoints;
                DeclineIntercept(parms, raid);
            };
            decline.resolveTree = true;
            nodeRoot.options.Add(decline);
            var title = "Outposts.Intercept.Title".Translate(parms.faction.Name);
            Find.WindowStack.Add(new Dialog_NodeTreeWithFactionInfo(nodeRoot, parms.faction, true, false, title));
        }
        private void StartIntercept(IncidentParms parms, IncidentWorker_RaidEnemy raid)
        {
            var targetMap = parms.target as Map;
            if (!TileFinder.TryFindPassableTileWithTraversalDistance(targetMap.Tile, 2, 5, out var tile,
            t => !Find.WorldObjects.AnyMapParentAt(t) && Find.WorldGrid.ApproxDistanceInTiles(Tile, t) <= 7f)) tile = Tile;
            LongEventHandler.QueueLongEvent(() =>
            {
                var map = GetOrGenerateMapUtility.GetOrGenerateMap(tile, new IntVec3(75, 1, 75), DefDatabase<WorldObjectDef>.GetNamed("VOE_AmbushedRaid"));
                parms.target = map;
                
                var defaultPawnGroupMakerParms = IncidentParmsUtility.GetDefaultPawnGroupMakerParms(PawnGroupKindDefOf.Combat, parms, true);
                if (map.Parent is AmbushedRaid ambushedRaid)
                {
                    ambushedRaid.DefensiveOutpost = this;
                    ambushedRaid.raidFaction = parms.faction;
                    ambushedRaid.raidPoints = parms.points;
                }
                defaultPawnGroupMakerParms.generateFightersOnly = true;
                defaultPawnGroupMakerParms.dontUseSingleUseRocketLaunchers = true;
                //Came across a minor bug here.Certian modded factions wont pass these validators. (VFEI insectoids is one but also only sometimes...?). It will eventually generate just produces 2 errors. Fixing it is pretty complicated and it doesnt actually hurt anything so dont think its a big deal
                var enemies = PawnGroupMakerUtility.GeneratePawns(defaultPawnGroupMakerParms).ToList();
                MultipleCaravansCellFinder.FindStartingCellsFor2Groups(map, out var playerSpot, out var enemySpot);
                var pawns = AllPawns.Where(x => !x.IsPrisonerOfColony).InRandomOrder().Skip(1).ToList();
                foreach (var pawn in pawns) GenSpawn.Spawn(RemovePawn(pawn), CellFinder.RandomSpawnCellForPawnNear(playerSpot, map), map, Rot4.Random);
                foreach (var enemy in enemies) GenSpawn.Spawn(enemy, CellFinder.RandomSpawnCellForPawnNear(enemySpot, map), map, Rot4.Random);
                var lordJob = new LordJob_AssaultColony(parms.faction, true, false);
                LordMaker.MakeNewLord(parms.faction, lordJob, map, enemies);
                var letterLabel = "Outposts.Letters.Intercept.Label".Translate();
                var letterText = "Outposts.Letters.Intercept.Text".Translate(targetMap.Parent.LabelCap, Name);
                PawnRelationUtility.Notify_PawnsSeenByPlayer_Letter(enemies, ref letterLabel, ref letterText,
                    "LetterRelatedPawnsGroupGeneric".Translate(Faction.OfPlayer.def.pawnsPlural), true);
                Find.LetterStack.ReceiveLetter(letterLabel, letterText, LetterDefOf.PositiveEvent,
                    new LookTargets(new List<GlobalTargetInfo> { this, map.Parent }));
                Find.TickManager.Notify_GeneratedPotentiallyHostileMap();
            }, "GeneratingMapForNewEncounter", false, null, false);
        }
        private void DeclineIntercept(IncidentParms parms, IncidentWorker_RaidEnemy raid)
        {
            DoRaid = true;
            raid.TryExecute(parms);            
            DoRaid = false;            
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
                                if (NeedFuel)
                                {
                                    foreach(var fuel in TakeItems(ThingDefOf.Chemfuel, FuelAmount)) fuel.Destroy();                                    
                                }
                                //Trainability is because my poor drop camels landing with the melee pawns on the enemey. Poor things :'(. But now I also want to droppod a bunch of nasty genetic creations to save me
                                //orderyby to leave a human pawn rather then random which can be animals as pretty sure that'd cause issues
                                foreach (var pawn in AllPawns.Where(x=>x.RaceProps.trainability != TrainabilityDefOf.None).OrderByDescending(x=>x.IsColonist).Skip(1).ToList())
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
            if (NeedFuel)
            {
                if (Things.Sum(delegate(Thing x)
                {
                    if (x.def == ThingDefOf.Chemfuel)
                    {
                        return x.stackCount;
                    }
                    return 0;
                }) < FuelAmount)
                {
                    reason = "Outposts.Commands.Deploy.NotEnoughFuel".Translate();
                    return true;
                }
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
        public Faction raidFaction;
        public float raidPoints;
        public override bool ShouldRemoveMapNow(out bool alsoRemoveWorldObject)
        {
            if (!Map.mapPawns.FreeColonists.Any())
            {
                Find.LetterStack.ReceiveLetter("Outposts.Defensive.Lost.Label".Translate(), "Outposts.Defensive.Lost.Desc".Translate(),
                    LetterDefOf.NeutralEvent);
                alsoRemoveWorldObject = true;
                return true;
            }
            var pawns = Map.mapPawns.AllPawns.ListFullCopy();
            if (!pawns.Any(p => p.Faction == raidFaction && !p.Downed))
            {
                
                List<Pawn> mapPawns = Map.PlayerPawnsForStoryteller.ToList();
                foreach (var pawn in mapPawns)
                {
                    pawn.DeSpawn();
                    DefensiveOutpost.AddPawn(pawn);
                }
                DefensiveOutpost.AddLoot(raidFaction, raidPoints,Map,out var loot);
                Find.LetterStack.ReceiveLetter("Outposts.Defensive.Won.Label".Translate(), "Outposts.Defensive.Won.Desc".Translate(loot),
                    LetterDefOf.PositiveEvent);
                raidFaction = null;
                raidPoints = 0;
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
            Scribe_References.Look(ref raidFaction, "raidFaction");
            Scribe_Values.Look(ref raidPoints, "raidPoints");
        }
    }

    [StaticConstructorOnStartup]
    public static class TexDefensive
    {
        public static readonly Texture2D DeployTex = ContentFinder<Texture2D>.Get("UI/DeployDefensiveGarrison");
    }
}