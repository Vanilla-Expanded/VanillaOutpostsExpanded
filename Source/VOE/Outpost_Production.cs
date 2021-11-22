using System.Collections.Generic;
using System.Linq;
using Outposts;
using RimWorld;
using UnityEngine;
using Verse;

namespace VOE
{
    public class Outpost_Production : Outpost
    {
        private ThingDef choice = ThingDefOf.ComponentIndustrial;
        private int craftingSkill;

        public override IEnumerable<Thing> ProducedThings() =>
            MakeThings(choice, choice == ThingDefOf.ComponentIndustrial ? craftingSkill * 10 : Mathf.RoundToInt(craftingSkill * 2.5f));

        public override void RecachePawnTraits()
        {
            craftingSkill = TotalSkill(SkillDefOf.Crafting);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            return base.GetGizmos().Append(new Command_Action
            {
                action = () => Find.WindowStack.Add(new FloatMenu(new List<FloatMenuOption>
                {
                    new(ThingDefOf.ComponentIndustrial.LabelCap + $" x{craftingSkill * 10}", () => choice = ThingDefOf.ComponentIndustrial),
                    craftingSkill < 50
                        ? new FloatMenuOption(ThingDefOf.ComponentSpacer.LabelCap + " - " + "Outposts.SkillTooLow".Translate(50), null)
                        : new FloatMenuOption(
                            ThingDefOf.ComponentSpacer.LabelCap + $" x{Mathf.RoundToInt(craftingSkill * 2.5f)}",
                            () => choice = ThingDefOf.ComponentSpacer)
                })),
                defaultLabel = "Outposts.Command.Comp.Label".Translate(),
                defaultDesc = "Outposts.Command.Comp.Desc".Translate(),
                icon = choice.uiIcon
            });
        }

        public override string GetInspectString() =>
            base.GetInspectString() + "\n" + "Outposts.TotalSkill".Translate(SkillDefOf.Crafting.skillLabel, craftingSkill) + "\n" + "Outposts.WillProduce.1".Translate(
                choice == ThingDefOf.ComponentIndustrial ? craftingSkill * 10 : Mathf.RoundToInt(craftingSkill * 2.5f), choice.label,
                ticksTillProduction.ToStringTicksToPeriodVerbose());

        public static string CanSpawnOnWith(int tile, List<Pawn> pawns) => CheckSkill(pawns, SkillDefOf.Crafting, 30);
    }
}