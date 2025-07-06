using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Outposts;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace VOE;

public class Outpost_Mining : Outpost_ChooseResult
{
    [PostToSetings("Outposts.Settings.Production", PostToSetingsAttribute.DrawMode.Percentage, 1f, 0.01f, 5f)]
    public float ProductionMultiplier = 1f;

    public OutpostExtension_Mining Mining => Ext as OutpostExtension_Mining;

    public override IEnumerable<ResultOption> GetExtraOptions() =>
        (from rock in Find.World.NaturalRockTypesIn(Tile)
            let product = rock?.building?.mineableThing?.butcherProducts?.FirstOrDefault()
            let block = product?.thingDef
            where block is not null
            select new ResultOption
            {
                Thing = block,
                AmountPerPawn = GetAmountPerPawnFor(rock, product.count)
            }).Concat(from resource in Mining.Resources
            let ore = OutpostExtension_Mining.GetOre(resource.Def)
            where ore is not null
            select new ResultOption
            {
                Thing = resource.Def,
                AmountPerPawn = GetAmountPerPawnFor(ore),
                MinSkills = new List<AmountBySkill>
                {
                    new()
                    {
                        Count = resource.MinMiningSkill,
                        Skill = SkillDefOf.Mining
                    }
                }
            });

    public int GetAmountPerPawnFor(ThingDef ore, float additionalMult = 1f)
    {
        var pickStrikesNeeded = ore.BaseMaxHitPoints / (ore.building.isNaturalRock ? 80 : 40);
        const float ticksPerPickHit = 100f / 0.64f;
        var ticksPerBlock = pickStrikesNeeded * ticksPerPickHit;
        var blocksPerDay = GenDate.TicksPerDay / 2f / ticksPerBlock;
        return Mathf.RoundToInt(blocksPerDay * ore.building.mineableYield * ProductionMultiplier * additionalMult * 15 / 50);
    }

    public static string CanSpawnOnWith(PlanetTile tile, List<Pawn> pawns) =>
        Find.WorldGrid[tile].hilliness == Hilliness.Flat ? "Outposts.MustBeMade.Hill".Translate() : null;

    public static string RequirementsString(PlanetTile tile, List<Pawn> pawns) =>
        "Outposts.MustBeMade.Hill".Translate().Requirement(Find.WorldGrid[tile].hilliness != Hilliness.Flat);
}

public class OutpostExtension_Mining : OutpostExtension_Choose
{
    private static readonly Dictionary<ThingDef, ThingDef> resourcesToOre = new();
    public List<Resource> Resources;

    public static ThingDef GetOre(ThingDef resource)
    {
        if (resourcesToOre.TryGetValue(resource, out var ore)) return ore;
        ore = DefDatabase<ThingDef>.AllDefs.FirstOrDefault(t => t is { building: { mineableThing: var mt } } && mt == resource);
        resourcesToOre.Add(resource, ore);
        return ore;
    }
}

public class Resource
{
    public ThingDef Def;
    public int MinMiningSkill;

    public void LoadDataFromXmlCustom(XmlNode xmlRoot)
    {
        if (xmlRoot.ChildNodes.Count != 1)
        {
            Log.Error("Misconfigured Resource: " + xmlRoot.OuterXml);
            return;
        }

        DirectXmlCrossRefLoader.RegisterObjectWantsCrossRef(this, "Def", xmlRoot.Name);
        MinMiningSkill = ParseHelper.FromString<int>(xmlRoot.FirstChild.Value);
    }
}
