using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Ninject;
using Ninject.Modules;

using ManifoldRing;

namespace Daphne
{
    /// <summary>
    /// to be removed!
    /// this is a fake config only; all this should be part of the SimConfig in some way
    /// </summary>
    public class FakeConfig
    {
        // start at > 0 as zero seems to be the default for metadata when a property is not present
        public enum SimStates { Linear = 1, Cubic, Tiny, Large, OneD, TwoD, ThreeD };

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

    public class SimulationModule : NinjectModule
    {
        public static IKernel kernel;

        public override void Load()
        {
            // bindings for interpolators
            Bind<Interpolator>().To<Trilinear2D>().WhenAnyAncestorMatches(ctx => ctx.Binding.Metadata.Get<FakeConfig.SimStates>("Interpolation") == FakeConfig.SimStates.Linear && ctx.Binding.Metadata.Get<FakeConfig.SimStates>("Dimension") == FakeConfig.SimStates.TwoD);
            Bind<Interpolator>().To<Tricubic2D>().WhenAnyAncestorMatches(ctx => ctx.Binding.Metadata.Get<FakeConfig.SimStates>("Interpolation") == FakeConfig.SimStates.Cubic && ctx.Binding.Metadata.Get<FakeConfig.SimStates>("Dimension") == FakeConfig.SimStates.TwoD);
            Bind<Interpolator>().To<Trilinear3D>().WhenAnyAncestorMatches(ctx => ctx.Binding.Metadata.Get<FakeConfig.SimStates>("Interpolation") == FakeConfig.SimStates.Linear && ctx.Binding.Metadata.Get<FakeConfig.SimStates>("Dimension") == FakeConfig.SimStates.ThreeD);
            Bind<Interpolator>().To<Tricubic3D>().WhenAnyAncestorMatches(ctx => ctx.Binding.Metadata.Get<FakeConfig.SimStates>("Interpolation") == FakeConfig.SimStates.Cubic && ctx.Binding.Metadata.Get<FakeConfig.SimStates>("Dimension") == FakeConfig.SimStates.ThreeD);

            // bindings for manifolds
            Bind<Manifold>().To<TinyBall>().WhenParentNamed("Cytosol").WithConstructorArgument("radius", FakeConfig.radius);
            Bind<Manifold>().To<TinySphere>().WhenParentNamed("Membrane").WithConstructorArgument("radius", FakeConfig.radius);
            Bind<InterpolatedRectangularPrism>().ToSelf().WithMetadata("Dimension", FakeConfig.SimStates.ThreeD).WithMetadata("Interpolation", FakeConfig.simInterpolate);
            Bind<InterpolatedRectangle>().ToSelf().WithMetadata("Dimension", FakeConfig.SimStates.TwoD).WithMetadata("Interpolation", FakeConfig.simInterpolate);

            // bindings for compartment
            Bind<Compartment>().ToSelf().WhenMemberHas<Cytosol>().Named("Cytosol");
            Bind<Compartment>().ToSelf().WhenMemberHas<Membrane>().Named("Membrane");
            Bind<Compartment>().ToSelf();

            // bindings for entities
            Bind<Cell>().ToSelf().WithConstructorArgument("radius", FakeConfig.radius).WithMetadata("Size", FakeConfig.simCellSize);
            Bind<ExtraCellularSpace>().ToSelf().WithConstructorArgument("numGridPts", FakeConfig.numGridPts).WithConstructorArgument("gridStep", FakeConfig.gridStep);
        }
    }
}
