using System.Collections.Generic;
using System.Linq;
using Outposts;
using RimWorld;
using UnityEngine;
using Verse;

namespace VOE
{
    public class Outpost_Hunting : Outpost_ChooseResult
    {
        [PostToSetings("Outposts.Settings.Animals", PostToSetingsAttribute.DrawMode.Percentage, 1f, 0.01f, 2f)]
        public float Animals = 1f;

        [PostToSetings("Outposts.Settings.Leather", PostToSetingsAttribute.DrawMode.Percentage, 0.5f, 0.01f, 2f)]
        public float Leather = 0.5f;

        [PostToSetings("Outposts.Settings.Meat", PostToSetingsAttribute.DrawMode.Percentage, 0.5f, 0.01f, 2f)]
        public float Meat = 0.5f;

        [PostToSetings("Outposts.Settings.Production", PostToSetingsAttribute.DrawMode.Percentage, 0.5f, 0.01f, 5f)]
        public float ProductionMultiplier = 0.5f;

        [PostToSetings("Outposts.Settings.Shooting", PostToSetingsAttribute.DrawMode.Percentage, 0.5f, 0.01f, 2f)]
        public float Shooting = 1f;

        public override List<ResultOption> ResultOptions
        {
            get
            {
                var opt = base.ResultOptions.FirstOrDefault();
                if (opt?.Thing is null) return new List<ResultOption>();
                return new List<ResultOption>
                {
                    new()
                    {
                        Thing = opt.Thing.race.leatherDef ?? ThingDefOf.Leather_Plain,

                        AmountsPerSkills = new List<AmountBySkill>
                        {
                            new()
                            {
                                Skill = SkillDefOf.Shooting,
                                Count = (int) (ProductionMultiplier * Shooting * Leather * opt.Thing.GetStatValueAbstract(StatDefOf.LeatherAmount) * opt.AmountsPerSkills[0].Count)
                            },
                            new()
                            {
                                Skill = SkillDefOf.Animals,
                                Count = (int) (ProductionMultiplier * Animals * Leather * opt.Thing.GetStatValueAbstract(StatDefOf.LeatherAmount) * opt.AmountsPerSkills[0].Count)
                            }
                        }
                    },
                    new()
                    {
                        Thing = opt.Thing.race.meatDef ?? ThingDefOf.Cow.race.meatDef ?? ThingDefOf.Meat_Human,
                        AmountsPerSkills = new List<AmountBySkill>
                        {
                            new()
                            {
                                Skill = SkillDefOf.Shooting,
                                Count = (int) (ProductionMultiplier * Shooting * Meat * opt.Thing.GetStatValueAbstract(StatDefOf.MeatAmount) * opt.AmountsPerSkills[0].Count)
                            },
                            new()
                            {
                                Skill = SkillDefOf.Animals,
                                Count = (int) (ProductionMultiplier * Animals * Meat * opt.Thing.GetStatValueAbstract(StatDefOf.MeatAmount) * opt.AmountsPerSkills[0].Count)
                            }
                        }
                    }
                };
            }
        }

        public override IEnumerable<ResultOption> GetExtraOptions()
        {
            var biome = Find.WorldGrid[Tile].biome;
            return biome.AllWildAnimals.Where(pkd => pkd.RaceProps.baseBodySize >= 2f).OrderByDescending(biome.CommonalityOfAnimal).Take(5).Select(pkd => new ResultOption
            {
                Thing = pkd.race,
                AmountsPerSkills = new List<AmountBySkill>
                {
                    new()
                    {
                        Skill = SkillDefOf.Shooting,
                        Count = Mathf.CeilToInt(biome.CommonalityOfAnimal(pkd) / 15f)
                    }
                }
            });
        }
    }
}