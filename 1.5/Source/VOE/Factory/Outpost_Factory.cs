using System.Collections.Generic;
using System.Linq;
using ItemProcessor;
using Outposts;
using Verse;

namespace FactoryOutpost
{
    [StaticConstructorOnStartup]
    public class Outpost_Factory : Outpost
    {
        public static List<ThingDef> AllFactories;

        private CombinationDef chosenCombination;
        private ThingDef chosenFactory;

        [PostToSetings("Outposts.Settings.Production", PostToSetingsAttribute.DrawMode.Percentage, 1f, 0.01f, 5f)]
        public float ProductionMultiplier = 1f;

        static Outpost_Factory()
        {
            var designation = DefDatabase<DesignationCategoryDef>.GetNamed("VFE_Factory");
            AllFactories = DefDatabase<ThingDef>.AllDefs
                .Where(td => td.designationCategory == designation && typeof(Building_ItemProcessor).IsAssignableFrom(td.thingClass))
                .Where(fact => DefDatabase<CombinationDef>.AllDefs.Any(comb => comb.building == fact.defName)).ToList();
        }

        protected ThingDef ResultDef => ThingDef.Named(chosenCombination.result);

        public IEnumerable<CombinationDef> AllCombinations => DefDatabase<CombinationDef>.AllDefs.Where(comb => comb.building == chosenFactory.defName)
            .GroupBy(comb => ThingDef.Named(comb.result)).Select(combs => combs.MaxBy(comb => comb.yield));

        public override string ProductionString() =>
            "Outposts.WillProduce.1".Translate(ProductionAmount(), ResultDef.label, TimeTillProduction);

        public override IEnumerable<Thing> ProducedThings() => ResultDef.Make(ProductionAmount());

        private int ProductionAmount()
        {
            return (int)(ProductionMultiplier * chosenCombination.yield * PawnCount * 5);
        }

        public override void PostAdd()
        {
            base.PostAdd();
            chosenFactory = AllFactories.First();
            chosenCombination = AllCombinations.First();
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Defs.Look(ref chosenCombination, "chosenCombinations");
            Scribe_Defs.Look(ref chosenFactory, "chosenFactory");
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            return base.GetGizmos().Concat(new[]
            {
                new Command_Action
                {
                    action = () => Find.WindowStack.Add(new FloatMenu(AllFactories.Select(fact => new FloatMenuOption(fact.LabelCap, () =>
                    {
                        chosenFactory = fact;
                        chosenCombination = AllCombinations.First();
                    })).ToList())),
                    defaultLabel = "Outposts.Commands.Factory.Label".Translate(),
                    defaultDesc = "Outposts.Commands.Factory.Desc".Translate(),
                    icon = chosenFactory.uiIcon
                },
                new Command_Action
                {
                    action = () => Find.WindowStack.Add(new FloatMenu(AllCombinations.Select(comb =>
                        new FloatMenuOption($"{ThingDef.Named(comb.result).label} x{comb.yield * PawnCount * 15}", () => chosenCombination = comb)).ToList())),
                    defaultLabel = "Outposts.Commands.Comb.Label".Translate(),
                    defaultDesc = "Outposts.Commands.Comb.Desc".Translate(),
                    icon = ResultDef.uiIcon
                }
            });
        }
    }
}