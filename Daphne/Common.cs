using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Ninject;
using Ninject.Modules;
using Ninject.Extensions.Factory;

using ManifoldRing;

namespace Daphne
{
    /// <summary>
    /// to be removed!
    /// this is a fake config only; all this should be part of the SimConfig in some way
    /// </summary>
    public class FakeConfig
    {
        static FakeConfig()
        {
            simInterpolate = SimStates.Linear;
            simCellSize = SimStates.Tiny;
            radius = 5.0;
            gridStep = 50;
            numGridPts = new int[] { 21, 21, 21 };
        }

        // these are static for simplicity only; they more than likely should be instance members in SimConfig
        public static SimStates simInterpolate { get; set; }
        public static SimStates simCellSize { get; set; }
        public static double radius { get; set; }
        public static double gridStep { get; set; }
        public static int[] numGridPts { get; set; }
    }

    public class CustomInstanceProvider : StandardInstanceProvider
    {
        protected override string GetName(System.Reflection.MethodInfo methodInfo, object[] arguments)
        {
            return (string)arguments[0];
        }

        protected override Ninject.Parameters.ConstructorArgument[] GetConstructorArguments(System.Reflection.MethodInfo methodInfo, object[] arguments)
        {
            return base.GetConstructorArguments(methodInfo, arguments).Skip(1).ToArray();
        }
    }

    public class SimulationModule : NinjectModule
    {
        public static IKernel kernel;
        private Scenario scenario;

        public SimulationModule(Scenario scenario)
            : base()
        {
            // hack to get this to work for now
            if (scenario == null)
            {
                scenario = new Scenario();
                scenario.simInterpolate = FakeConfig.simInterpolate;
                scenario.simCellSize = FakeConfig.simCellSize;
                scenario.NumGridPts = FakeConfig.numGridPts;
                scenario.GridStep = FakeConfig.gridStep;
                scenario.CellRadius = FakeConfig.radius;
            }
            // end hack
            this.scenario = scenario;
        }

        public override void Load()
        {
            // bindings for interpolators
            Bind<Interpolator>().To<Trilinear2D>().WhenAnyAncestorMatches(ctx => ctx.Binding.Metadata.Get<SimStates>("Interpolation") == SimStates.Linear && ctx.Binding.Metadata.Get<SimStates>("Dimension") == SimStates.TwoD);
            Bind<Interpolator>().To<Tricubic2D>().WhenAnyAncestorMatches(ctx => ctx.Binding.Metadata.Get<SimStates>("Interpolation") == SimStates.Cubic && ctx.Binding.Metadata.Get<SimStates>("Dimension") == SimStates.TwoD);
            Bind<Interpolator>().To<Trilinear3D>().WhenAnyAncestorMatches(ctx => ctx.Binding.Metadata.Get<SimStates>("Interpolation") == SimStates.Linear && ctx.Binding.Metadata.Get<SimStates>("Dimension") == SimStates.ThreeD);
            Bind<Interpolator>().To<Tricubic3D>().WhenAnyAncestorMatches(ctx => ctx.Binding.Metadata.Get<SimStates>("Interpolation") == SimStates.Cubic && ctx.Binding.Metadata.Get<SimStates>("Dimension") == SimStates.ThreeD);

            // bindings for manifolds
            Bind<Manifold>().To<TinyBall>().WhenParentNamed("Cytosol").WithConstructorArgument("radius", scenario.CellRadius);
            Bind<Manifold>().To<TinySphere>().WhenParentNamed("Membrane").WithConstructorArgument("radius", scenario.CellRadius);
            Bind<InterpolatedRectangularPrism>().ToSelf().WithMetadata("Dimension", SimStates.ThreeD).WithMetadata("Interpolation", scenario.simInterpolate);
            Bind<InterpolatedRectangle>().ToSelf().WithMetadata("Dimension", SimStates.TwoD).WithMetadata("Interpolation", scenario.simInterpolate);

            // bindings for compartment
            Bind<Compartment>().ToSelf().WhenMemberHas<Cytosol>().Named("Cytosol");
            Bind<Compartment>().ToSelf().WhenMemberHas<Membrane>().Named("Membrane");
            Bind<Compartment>().ToSelf();

            // bindings for molecular populations
            Bind<IFieldInitializer>().To<ConstFieldInitializer>().Named("const");
            Bind<IFieldInitializer>().To<GaussianFieldInitializer>().Named("gauss");
            Bind<ScalarField>().ToSelf();
            Bind<MolecularPopulation>().ToSelf();
            Bind<IFieldInitializerFactory>().ToFactory(() => new CustomInstanceProvider());


            // bindings for simulation entities
            Bind<Cell>().ToSelf().WithConstructorArgument("radius", scenario.CellRadius).WithMetadata("Size", scenario.simCellSize);
            Bind<ExtraCellularSpace>().ToSelf().WithConstructorArgument("numGridPts", scenario.NumGridPts).WithConstructorArgument("gridStep", scenario.GridStep);
        }
    }
}
