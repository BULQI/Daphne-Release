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
            gridStep = 50;
            numGridPts = new int[] { 21, 21, 21 };
        }

        // these are static for simplicity only; they more than likely should be instance members in SimConfig
        public static SimStates simInterpolate { get; set; }
        public static SimStates simCellSize { get; set; }
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

        private enum MetadataValues { OneD = 1, TwoD, ThreeD, Membrane, Cytosol, ECS };

        public SimulationModule(Scenario scenario)
            : base()
        {
            // hack to get this to work for now
            if (scenario == null)
            {
                scenario = new Scenario();
                scenario.simInterpolate = FakeConfig.simInterpolate;
                scenario.simCellSize = FakeConfig.simCellSize;
                scenario.environment.NumGridPts = FakeConfig.numGridPts;
                scenario.environment.gridstep = FakeConfig.gridStep;
            }
            // end hack
            this.scenario = scenario;
        }

        public override void Load()
        {
            // bindings for interpolators
            Bind<Interpolator>().To<Trilinear2D>().WhenAnyAncestorMatches(ctx => ctx.Binding.Metadata.Get<SimStates>("Interpolation") == SimStates.Linear && ctx.Binding.Metadata.Get<MetadataValues>("Dimension") == MetadataValues.TwoD);
            Bind<Interpolator>().To<Tricubic2D>().WhenAnyAncestorMatches(ctx => ctx.Binding.Metadata.Get<SimStates>("Interpolation") == SimStates.Cubic && ctx.Binding.Metadata.Get<MetadataValues>("Dimension") == MetadataValues.TwoD);
            Bind<Interpolator>().To<Trilinear3D>().WhenAnyAncestorMatches(ctx => ctx.Binding.Metadata.Get<SimStates>("Interpolation") == SimStates.Linear && ctx.Binding.Metadata.Get<MetadataValues>("Dimension") == MetadataValues.ThreeD);
            Bind<Interpolator>().To<Tricubic3D>().WhenAnyAncestorMatches(ctx => ctx.Binding.Metadata.Get<SimStates>("Interpolation") == SimStates.Cubic && ctx.Binding.Metadata.Get<MetadataValues>("Dimension") == MetadataValues.ThreeD);

            // bindings for manifolds
            Bind<Manifold>().To<TinyBall>().WhenAnyAncestorMatches(ctx => ctx.Binding.Metadata.Get<SimStates>("Size") == SimStates.Tiny && ctx.Binding.Metadata.Get<MetadataValues>("Component") == MetadataValues.Cytosol);
            Bind<Manifold>().To<TinySphere>().WhenAnyAncestorMatches(ctx => ctx.Binding.Metadata.Get<SimStates>("Size") == SimStates.Tiny && ctx.Binding.Metadata.Get<MetadataValues>("Component") == MetadataValues.Membrane);
            // NOTE: have Ball/Sphere definitions here in addition when size == Large
            Bind<InterpolatedRectangularPrism>().ToSelf().WithMetadata("Dimension", MetadataValues.ThreeD).WithMetadata("Interpolation", scenario.simInterpolate);
            Bind<InterpolatedRectangle>().ToSelf().WithMetadata("Dimension", MetadataValues.TwoD).WithMetadata("Interpolation", scenario.simInterpolate);

            // bindings for compartment
            Bind<Compartment>().ToSelf().WhenMemberHas<Cytosol>().WithMetadata("Component", MetadataValues.Cytosol).WithMetadata("Size", scenario.simCellSize);
            Bind<Compartment>().ToSelf().WhenMemberHas<Membrane>().WithMetadata("Component", MetadataValues.Membrane).WithMetadata("Size", scenario.simCellSize);
            Bind<Compartment>().ToSelf();

            // bindings for molecular populations
            Bind<IFieldInitializer>().To<ConstFieldInitializer>().Named("const");
            Bind<IFieldInitializer>().To<GaussianFieldInitializer>().Named("gauss");
            Bind<ScalarField>().ToSelf();
            Bind<MolecularPopulation>().ToSelf();
            Bind<IFieldInitializerFactory>().ToFactory(() => new CustomInstanceProvider());

            // bindings for simulation entities
            Bind<Cell>().ToSelf();
            Bind<ExtraCellularSpace>().ToSelf().WithConstructorArgument("numGridPts", scenario.environment.NumGridPts).WithConstructorArgument("gridStep", scenario.environment.gridstep);
        }
    }
}
