using System.Collections.Generic;
using System.Linq;
using ItemProcessor;
using Outposts;
using RimWorld.Planet;
using Verse;

namespace FactoryOutpost
{
    [StaticConstructorOnStartup]
    public class Outpost_Factory : Outpost
    {
        public static List<ThingDef> AllFactories;

        private CombinationDef chosenCombination;
        private ThingDef chosenFactory;

        static Outpost_Factory()
        {
            var designation = DefDatabase<DesignationCategoryDef>.GetNamed("VFE_Factory");
            AllFactories = DefDatabase<ThingDef>.AllDefs.Where(td => td.designationCategory == designation && typeof(Building_ItemProcessor).IsAssignableFrom(td.thingClass))
                .ToList();
        }

        protected ThingDef ResultDef => ThingDef.Named(chosenCombination.result);

        public IEnumerable<CombinationDef> AllCombinations => DefDatabase<CombinationDef>.AllDefs.Where(comb => comb.building == chosenFactory.defName);

        public override string ProductionString() => "Outposts.WillProduce.1".Translate(chosenCombination.yield * PawnCount * 15, ResultDef.label, TimeTillProduction);

        public override IEnumerable<Thing> ProducedThings() => MakeThings(ResultDef, chosenCombination.yield * PawnCount * 15);

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
                    action = () => Find.WindowStack.Add(new FloatMenu(AllFactories.Where(fact => DefDatabase<CombinationDef>.AllDefs.Any(comb => comb.building == fact.defName))
                        .Select(fact => new FloatMenuOption(fact.LabelCap, () =>
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

        public static string CanSpawnOnWith(int tile, List<Pawn> pawns)
        {
            return !CaravanInventoryUtility.HasThings(Find.WorldObjects.Caravans.First(c => c.Tile == tile), ThingDef.Named("VFE_ComponentMechanoid"), 20)
                ? "Outposts.MustHaveInCaravan".Translate(20, ThingDef.Named("VFE_ComponentMechanoid").label)
                : null;
        }
    }
}