using System.Collections.Generic;
using System.Linq;
using Outposts;
using RimWorld;
using UnityEngine;
using Verse;

namespace VOE;

public class Outpost_Farming : Outpost_ChooseResult
{
    [PostToSetings("Outposts.Settings.Production", PostToSetingsAttribute.DrawMode.Percentage, 1f, 0.01f, 5f)]
    public float ProductionMultiplier = 1f;

    public override IEnumerable<ResultOption> GetExtraOptions()
    {
        return DefDatabase<ThingDef>.AllDefs
           .Where(d => d.category == ThingCategory.Plant && d.plant?.harvestedThingDef is not null && d.plant.sowTags.Contains("Ground"))
           .GroupBy(plant => plant.plant.harvestedThingDef)
           .Select(plants => plants.MaxBy(plant => plant.plant.harvestYield))
           .Select(plant => new ResultOption
            {
                Thing = plant.plant.harvestedThingDef,
                AmountsPerSkills = new List<AmountBySkill>
                {
                    new()
                    {
                        Count = Mathf.FloorToInt(ProductionMultiplier * 45 / plant.plant.growDays * plant.plant.harvestYield),
                        Skill = SkillDefOf.Plants
                    }
                },
                MinSkills = new List<AmountBySkill>
                {
                    new()
                    {
                        Skill = SkillDefOf.Plants,
                        Count = plant.plant.sowMinSkill * PawnCount
                    }
                }
            });
    }
}
