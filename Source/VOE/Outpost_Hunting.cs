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
                                Count = (int) (opt.Thing.GetStatValueAbstract(StatDefOf.LeatherAmount) * opt.AmountsPerSkills[0].Count / 2f)
                            },
                            new()
                            {
                                Skill = SkillDefOf.Animals,
                                Count = (int) (opt.Thing.GetStatValueAbstract(StatDefOf.LeatherAmount) * opt.AmountsPerSkills[0].Count / 2f)
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
                                Count = (int) (opt.Thing.GetStatValueAbstract(StatDefOf.MeatAmount) * opt.AmountsPerSkills[0].Count / 2f)
                            },
                            new()
                            {
                                Skill = SkillDefOf.Animals,
                                Count = (int) (opt.Thing.GetStatValueAbstract(StatDefOf.MeatAmount) * opt.AmountsPerSkills[0].Count / 2f)
                            }
                        }
                    }
                };
            }
        }

        public override IEnumerable<ResultOption> GetExtraOptions()
        {
            var biome = Find.WorldGrid[Tile].biome;
            return biome.AllWildAnimals.OrderByDescending(biome.CommonalityOfAnimal).Take(5).Select(pkd => new ResultOption
            {
                Thing = pkd.race,
                AmountsPerSkills = new List<AmountBySkill>
                {
                    new()
                    {
                        Skill = SkillDefOf.Shooting,
                        Count = Mathf.CeilToInt(biome.CommonalityOfAnimal(pkd) * 2f)
                    }
                }
            });
        }
    }
}