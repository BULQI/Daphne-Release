/*
Copyright (C) 2019 Kepler Laboratory of Quantitative Immunology

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY 
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Ninject;
using Ninject.Modules;
using Ninject.Extensions.Factory;

using Nt_ManifoldRing;
using Gene = NativeDaphne.Nt_Gene;
namespace Daphne
{
    /// <summary>
    /// Ninject needs this for handling factories (resolving names)
    /// </summary>
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
        private ScenarioBase scenario;

        private enum MetadataValues { OneD = 1, TwoD, ThreeD, Membrane, Cytosol, ECS };

        public SimulationModule(ScenarioBase scenario)
            : base()
        {
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
            Bind<IFieldInitializer>().To<LinearFieldInitializer>().Named("linear");
            Bind<IFieldInitializer>().To<GaussianFieldInitializer>().Named("gauss");
            Bind<IFieldInitializer>().To<ExplicitFieldInitializer>().Named("explicit"); 
            Bind<ScalarField>().ToSelf();
            Bind<MolecularPopulation>().ToSelf();
            Bind<IFieldInitializerFactory>().ToFactory(() => new CustomInstanceProvider());

            // chemistry
            Bind<Molecule>().ToSelf();

            // same transition for all
            Bind<ITransitionDriver>().To<TransitionDriver>();

            // same scheme for all differentiator and divider
            Bind<ITransitionScheme>().To<TransitionScheme>();


            Bind<Gene>().ToSelf();

            // bindings for simulation entities
            Bind<Cell>().ToSelf();
            if (scenario.environment is ConfigECSEnvironment)
            {
                Bind<ECSEnvironment>().ToSelf().WithConstructorArgument("numGridPts", ((ConfigECSEnvironment)scenario.environment).NumGridPts).
                                                WithConstructorArgument("gridStep", ((ConfigECSEnvironment)scenario.environment).gridstep).
                                                WithConstructorArgument("toroidal", ((ConfigECSEnvironment)scenario.environment).toroidal);
            }
            else if (scenario.environment is ConfigPointEnvironment)
            {
                Bind<PointEnvironment>().ToSelf();
            }
            else if (scenario.environment is ConfigRectEnvironment)
            {
                Bind<RectEnvironment>().ToSelf().WithConstructorArgument("numGridPts", ((ConfigRectEnvironment)scenario.environment).NumGridPts).
                                                 WithConstructorArgument("gridStep", ((ConfigRectEnvironment)scenario.environment).gridstep).
                                                 WithConstructorArgument("toroidal", ((ConfigRectEnvironment)scenario.environment).toroidal);
            }
            else
            {
                throw new NotImplementedException();
            }
            Bind<CollisionManager>().ToSelf();

            // factory container
            Bind<FactoryContainer>().ToSelf();
        }
    }
}
