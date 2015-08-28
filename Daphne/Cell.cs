using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Numerics;

using MathNet.Numerics.LinearAlgebra.Double;
using Ninject;
using Ninject.Parameters;
using NativeDaphne;
using System.Diagnostics;
using Newtonsoft.Json;
using Nt_ManifoldRing;
using Gene = NativeDaphne.Nt_Gene;

namespace Daphne
{
    public class Cytosol : Attribute { }
    public class Membrane : Attribute { }

    /// <summary>
    /// The basic representation of a biological cell. 
    /// </summary>
    public class Cell : Nt_Cell, IDynamic
    {
        /// <summary>
        /// the cell's behaviors (death, division, differentiation)
        /// </summary>
        private ITransitionDriver deathBehavior;
        private ITransitionScheme differentiator, divider;

        private StochLocomotor stochLocomotor; 


        /// <summary>
        /// info for rendering
        /// </summary>
        public string renderLabel;
        public int generation;

        public Cell(double radius) :base (radius)
        {
            if (radius <= 0)
            {
                throw new Exception("Cell radius must be greater than zero.");
            }
            isMotile = true;
            isChemotactic = true;
            isStochastic = true;
            alive = true;
            cytokinetic = false;
            this.radius = radius;
            genes = new Dictionary<string, Gene>();
            exiting = false;

            spatialState = new Nt_CellSpatialState();
            spatialState.X = new Nt_Darray(Nt_CellSpatialState.SingleDim);
            spatialState.V = new Nt_Darray(Nt_CellSpatialState.SingleDim);
            spatialState.F = new Nt_Darray(Nt_CellSpatialState.SingleDim);
        }

        [Inject]
        [Cytosol]
        public void InjectCytosol(Compartment c)
        {
            Cytosol = c;
            Cytosol.Interior.Initialize(new double[] { radius });
            base.BaseCytosol = c.BaseComp;
            if (PlasmaMembrane != null)
            {
                initBoundary();
            }
        }

        [Inject]
        [Membrane]
        public void InjectMembrane(Compartment c)
        {
            PlasmaMembrane = c;
            PlasmaMembrane.Interior.Initialize(new double[] { radius });
            base.BasePlasmaMembrane = c.BaseComp;
            if (Cytosol != null)
            {
                initBoundary();
            }
        }

        [Inject]
        public void InjectDeathBehavior(ITransitionDriver behavior)
        {
            deathBehavior = behavior;
        }

        public ITransitionDriver DeathBehavior
        {
            get { return deathBehavior; }
        }

        [Inject]
        public void InjectDifferentiator(ITransitionScheme diff)
        {
            differentiator = diff;
        }

        public ITransitionScheme Differentiator
        {
            get { return differentiator; }
        }

        [Inject]
        public void InjectDivider(ITransitionScheme div)
        {
            divider = div;
        }

        public ITransitionScheme Divider
        {
            get { return divider; }
        }


        private void initBoundary()
        {
            // boundary and position
            Cytosol.AddBoundary(PlasmaMembrane.Interior.Id, PlasmaMembrane);
            Cytosol.BoundaryTransforms.Add(PlasmaMembrane.Interior.Id, new Transform(false));
        }

        public void setSpatialState(double[] s)
        {
            if (s.Length != Nt_CellSpatialState.Dim)
            {
                throw new Exception("Cell state length implausible.");
            }

            int i;

            // position
            for (i = 0; i < Nt_CellSpatialState.SingleDim; i++)
            {
                spatialState.X[i] = s[i];
            }
            // velocity
            for (i = 0; i < Nt_CellSpatialState.SingleDim; i++)
            {
                spatialState.V[i] = s[i + Nt_CellSpatialState.SingleDim];
            }
            // force
            for (i = 0; i < Nt_CellSpatialState.SingleDim; i++)
            {
                spatialState.F[i] = s[i + 2 * Nt_CellSpatialState.SingleDim];
            }
        }

        /// <summary>
        /// set the state as used in rendering; does not set the state for simulation purposes
        /// </summary>
        /// <param name="state">the state</param>
        public void SetCellState(CellState state)
        {
            // cell id
            if (state.Cell_id > -1)
            {
                Cell_id = state.Cell_id;
            }
            // lineage id
            if (state.Lineage_id != "")
            {
                Lineage_id = BigInteger.Parse(state.Lineage_id);
            }
            // spatial
            setSpatialState(state.spState);
            // generation
            generation = state.CellGeneration;
            // behaviors
            if (state.cbState.deathDriverState != -1)
            {
                Alive = state.cbState.deathDriverState == 0;
                DeathBehavior.CurrentState = state.cbState.deathDriverState;
                if (state.cbState.deathDistrState != null)
                {
                    Dictionary<int, TransitionDriverElement> drivers = DeathBehavior.Drivers[DeathBehavior.CurrentState];
                    ((DistrTransitionDriverElement)drivers[1]).Restore(state.cbState.deathDistrState);
                }
            }
            if (state.cbState.divisionDriverState != -1)
            {
                DividerState = Divider.CurrentState = Divider.Behavior.CurrentState = state.cbState.divisionDriverState;

                if (state.cbState.divisionDistrState.Count > 0)
                {
                    Dictionary<int, TransitionDriverElement> drivers = Divider.Behavior.Drivers[DividerState];
                    foreach (KeyValuePair<int, double[]> kvp in state.cbState.divisionDistrState)
                    {
                        ((DistrTransitionDriverElement)drivers[kvp.Key]).Restore(kvp.Value);
                    }
                }
            }
            if (state.cbState.differentiationDriverState != -1)
            {
                DifferentiationState = Differentiator.CurrentState = Differentiator.Behavior.CurrentState = state.cbState.differentiationDriverState;

                if (state.cbState.differentiationDistrState.Count > 0)
                {
                    Dictionary<int, TransitionDriverElement> drivers = Differentiator.Behavior.Drivers[DifferentiationState];
                    foreach (KeyValuePair<int, double[]> kvp in state.cbState.differentiationDistrState)
                    {
                        ((DistrTransitionDriverElement)drivers[kvp.Key]).Restore(kvp.Value);
                    }
                }
            }
            // genes
            SetGeneActivities(state.cgState.geneDict);
            // molecules
            SetMolPopConcentrations(state.cmState.molPopDict);
            // dead cell removal
            if (state.cbState.removalDistrState != null)
            {
                SimulationBase.cellManager.DeadDict.Add(Cell_id, state.cbState.removalDistrState);
            }
 
        }

        public void setSpatialState(Nt_CellSpatialState s)
        {
            int i;

            // position
            for (i = 0; i < Nt_CellSpatialState.SingleDim; i++)
            {
                spatialState.X[i] = s.X[i];
            }
            // velocity
            for (i = 0; i < Nt_CellSpatialState.SingleDim; i++)
            {
                spatialState.V[i] = s.V[i];
            }
            // force
            for (i = 0; i < Nt_CellSpatialState.SingleDim; i++)
            {
                spatialState.F[i] = s.F[i];
            }
        }
        
        //public static System.IO.StreamWriter debug_writer = null;
        /// <summary>
        /// Drives the cell's dynamics through time-step dt. The dynamics is applied in-place: the
        /// cell's state is changed directly through this method.
        /// </summary>
        /// <param name="dt">Time interval.</param>
        public void Step(double dt) 
        {
            
            //Cell's reactions/molpop/updateboundaries are handled by
            //cellpopulation.step(). only the transition schemes are handled here.

            // death
            deathBehavior.Step(dt);
            if (deathBehavior.TransitionOccurred == true && deathBehavior.CurrentState == 1)
            {
                alive = false;
                cytokinetic = false;
            }

            // division
            Divider.Step(dt);
            if (Divider.TransitionOccurred == true)
            {
                Divider.TransitionOccurred = false;
                if (Divider.CurrentState == Divider.Behavior.FinalState)
                {
                    cytokinetic = true;
                    Divider.CurrentState = Divider.Behavior.CurrentState = 0;
                    Divider.PreviousState = Divider.Behavior.PreviousState = Divider.Behavior.FinalState;
                    // changing state, so we need to reinitialize the driver for this state
                    Divider.Behavior.InitializeState();
                }
                // Epigentic changes
                SetGeneActivities(Divider);
                DividerState = Divider.CurrentState;
            }

            // Differentiation
            Differentiator.Step(dt);
            if (Differentiator.TransitionOccurred == true)
            {
                // Epigentic changes
                SetGeneActivities(Differentiator);
                Differentiator.TransitionOccurred = false;
                DifferentiationState = Differentiator.CurrentState;
            }
            
            //moved to middle layer
            //if (alive && isMotile && (!exiting))
            //{
            //    this.EnforceBC();
            //}
        }

        public void SetGeneActivities(ITransitionScheme scheme)
        {
            // Set gene activity levels based on current differentiation state
            for (int i = 0; i < scheme.gene_id.Length; i++)
            {
                // Negative activation means leave the gene as is
                if (scheme.activity[scheme.CurrentState, i] >= 0)
                {
                    Genes[scheme.gene_id[i]].ActivationLevel = scheme.activity[scheme.CurrentState, i];
                }
            }
        }

        /// <summary>
        /// save gene activity from saved values.
        /// </summary>
        /// <param name="geneDict"></param>
        public void SetGeneActivities(Dictionary<String, double> geneDict)
        {
            foreach (var kvp in geneDict)
            {
                Genes[kvp.Key].ActivationLevel = kvp.Value;
            }
        }

        /// <summary>
        /// set molecular population concentrations from saved values
        /// </summary>
        /// <param name="molPopDict">saved values in a dictionary</param>
        public void SetMolPopConcentrations(Dictionary<string, double[]> molPopDict)
        {
            // cytosol
            foreach (KeyValuePair<string, MolecularPopulation> kvp in Cytosol.Populations)
            {
                if (molPopDict.ContainsKey(kvp.Key) == false)
                {
                    continue;
                }
                kvp.Value.Initialize("explicit", molPopDict[kvp.Key]);
            }
            // membrane
            foreach (KeyValuePair<string, MolecularPopulation> kvp in PlasmaMembrane.Populations)
            {
                if (molPopDict.ContainsKey(kvp.Key) == false)
                {
                    continue;
                }
                kvp.Value.Initialize("explicit", molPopDict[kvp.Key]);
            }
        }
        /// <summary>
        /// Returns the force the cell applies to the environment.
        /// </summary>
        public double[] Force(double[] position)
        {
            return Locomotor.Force(position);
        }

        /// <summary>
        /// Carries out cell division. In addition to returning a daughter cell, the cell's own state is reset as appropriate
        /// </summary>
        /// <returns>A new daughter cell.</returns>
        public Cell Divide()
        {
            // the daughter is constructed on the same blueprint as the mother
            Cell daughter = null;

            // set the mother's new post-division state
            cytokinetic = false;
            
            // force reset
            spatialState.F[0] = spatialState.F[1] = spatialState.F[2] = 0;
            // velocity reset
            spatialState.V[0] = spatialState.V[1] = spatialState.V[2] = 0;
            // halve the chemistry
            foreach (MolecularPopulation mp in Cytosol.Populations.Values)
            {
                mp.Conc.Multiply(0.5);
            }
            foreach (MolecularPopulation mp in PlasmaMembrane.Populations.Values)
            {
                mp.Conc.Multiply(0.5);
            }

            // create daughter
            daughter = SimulationModule.kernel.Get<Cell>(new ConstructorArgument("radius", radius));
            // generate new cell id, pass -1 to use safeCellId++
            daughter.Cell_id = DataBasket.GenerateSafeCellId(-1);
            // same population id
            daughter.Population_id = Population_id;
            daughter.renderLabel = renderLabel;
            this.generation++;
            daughter.generation = generation;
            // lineage ids
            this.Lineage_id *= 2;
            daughter.Lineage_id = this.Lineage_id + 1;
            // same state
            daughter.setSpatialState(spatialState);
            // but offset the daughter randomly
            double[] delta = Rand.RandomDirection(daughter.spatialState.X.Length).Multiply(radius).ToArray();

            for (int i = 0; i < delta.Length; i++)
            {
                this.spatialState.X[i] -= delta[i];
                daughter.spatialState.X[i] += delta[i];
            }

            daughter.cytokinetic = false;

            // the daughter's state dependent on the mother's pre-division state

            // cytosol molpops
            foreach (KeyValuePair<string, MolecularPopulation> kvp in Cytosol.Populations)
            {
                MolecularPopulation newMP = SimulationModule.kernel.Get<MolecularPopulation>(new ConstructorArgument("mol", kvp.Value.Molecule), new ConstructorArgument("moleculeKey", kvp.Key), new ConstructorArgument("comp", daughter.Cytosol));

                newMP.Initialize("explicit", kvp.Value.CopyArray());
                daughter.Cytosol.AddMolecularPopulation(kvp.Key, newMP);
            }
            // membrane molpops
            foreach (KeyValuePair<string, MolecularPopulation> kvp in PlasmaMembrane.Populations)
            {
                MolecularPopulation newMP = SimulationModule.kernel.Get<MolecularPopulation>(new ConstructorArgument("mol", kvp.Value.Molecule), new ConstructorArgument("moleculeKey", kvp.Key), new ConstructorArgument("comp", daughter.PlasmaMembrane));

                newMP.Initialize("explicit", kvp.Value.CopyArray());
                daughter.PlasmaMembrane.AddMolecularPopulation(kvp.Key, newMP);
            }
            // genes
            foreach (KeyValuePair<string, Gene> kvp in Genes)
            {
                Gene newGene = SimulationModule.kernel.Get<Gene>(new ConstructorArgument("name", kvp.Value.Name), new ConstructorArgument("copyNumber", kvp.Value.CopyNumber), new ConstructorArgument("actLevel", kvp.Value.ActivationLevel));
                daughter.Genes.Add(kvp.Key, newGene);
            }

            // reactions
            ConfigCompartment[] configComp = new ConfigCompartment[2];
            List<ConfigReaction>[] bulk_reacs = new List<ConfigReaction>[2];
            List<ConfigReaction> boundary_reacs = new List<ConfigReaction>();
            List<ConfigReaction> transcription_reacs = new List<ConfigReaction>();

            if (SimulationBase.ProtocolHandle.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == true)
            {
                // only the TissueScenario has cell populations
                configComp[0] = ((TissueScenario)SimulationBase.ProtocolHandle.scenario).cellpopulation_dict[daughter.Population_id].Cell.cytosol;
                configComp[1] = ((TissueScenario)SimulationBase.ProtocolHandle.scenario).cellpopulation_dict[daughter.Population_id].Cell.membrane;
            }
            else
            {
                // for now
                throw new NotImplementedException();
            }

            bulk_reacs[0] = SimulationBase.ProtocolHandle.GetReactions(configComp[0], false);
            bulk_reacs[1] = SimulationBase.ProtocolHandle.GetReactions(configComp[1], false);
            boundary_reacs = SimulationBase.ProtocolHandle.GetReactions(configComp[0], true);
            transcription_reacs = SimulationBase.ProtocolHandle.GetTranscriptionReactions(configComp[0]);

            // cytosol bulk reactions
            SimulationBase.AddCompartmentBulkReactions(daughter.Cytosol, SimulationBase.ProtocolHandle.entity_repository, bulk_reacs[0]);
            // membrane bulk reactions
            SimulationBase.AddCompartmentBulkReactions(daughter.PlasmaMembrane, SimulationBase.ProtocolHandle.entity_repository, bulk_reacs[1]);

            // boundary reactions
            // all daughter cells will have the molecules as long as the mother had them,
            // and if the latter didn't we issued a warning there already

            SimulationBase.AddCompartmentBoundaryReactions(daughter.Cytosol, daughter.PlasmaMembrane, SimulationBase.ProtocolHandle.entity_repository, boundary_reacs, null);
            // transcription reactions
            SimulationBase.AddCellTranscriptionReactions(daughter, SimulationBase.ProtocolHandle.entity_repository, transcription_reacs);

            /* this needs to be added after adding the cell*/
            /*
            ((ECSEnvironment)SimulationBase.dataBasket.Environment).Comp.AddBoundary(daughter.Population_id, daughter.PlasmaMembrane.Interior.Id, daughter.PlasmaMembrane);
            // add the cell's membrane to the ecs boundary
            ((ECSEnvironment)SimulationBase.dataBasket.Environment).AddBoundaryManifold(daughter.PlasmaMembrane.Interior);
            // add ECS boundary reactions, where applicable
            List<ConfigReaction> reacs = SimulationBase.ProtocolHandle.GetReactions(SimulationBase.ProtocolHandle.scenario.environment.comp, true);
            SimulationBase.AddCompartmentBoundaryReactions(SimulationBase.dataBasket.Environment.Comp, daughter.PlasmaMembrane, SimulationBase.ProtocolHandle.entity_repository, reacs, null);
            */
             // behaviors
           

            // behaviors

            // locomotion
            daughter.IsMotile = isMotile;
            daughter.IsChemotactic = isChemotactic;
            if (IsChemotactic == true)
            {
                MolecularPopulation driver = daughter.Cytosol.Populations[Locomotor.Driver.MoleculeKey];

                daughter.Locomotor = new Locomotor(driver, Locomotor.TransductionConstant);
            }

            // stochastic locomotion
            daughter.IsStochastic = isStochastic;
            if (isStochastic == true)
            {
                daughter.StochLocomotor = new StochLocomotor(StochLocomotor.Sigma);
            }

            daughter.DragCoefficient = DragCoefficient;

            // death
            LoadTransitionDriverElements(daughter, daughter.DeathBehavior, DeathBehavior);
            daughter.DeathBehavior.CurrentState = DeathBehavior.CurrentState;
            daughter.DeathBehavior.InitializeState();

            // division
            if (Divider.nStates > 1)
            {
                // The daughter cell will start at state 0 of the cell cycle (as does the mother). 
                // For distribution-driven transitions, choose the time-to-next event in the IntializeState method.
                daughter.Divider.Initialize(Divider.nStates, Divider.nGenes);
                LoadTransitionDriverElements(daughter, daughter.Divider.Behavior, Divider.Behavior);
                Array.Copy(Divider.State, daughter.Divider.State, Divider.State.Length);
                Array.Copy(Divider.gene_id, daughter.Divider.gene_id, Divider.gene_id.Length);
                Array.Copy(Divider.activity, daughter.Divider.activity, Divider.activity.Length);
                daughter.DividerState = daughter.Divider.CurrentState = daughter.Divider.Behavior.CurrentState = Divider.CurrentState;
                daughter.SetGeneActivities(daughter.Divider);
                daughter.Divider.Behavior.InitializeState();
            }

            // differentiation
            if (Differentiator.nStates > 1)
            {
                // The daughter cell will start in the same differentiation state as the mother. 
                // For distribution-driven transitions, the daughter will be assigned the same clock and time-to-next event values as the mother.
                // So, no need to run InitializeState.
                daughter.Differentiator.Initialize(Differentiator.nStates, Differentiator.nGenes);
                LoadTransitionDriverElements(daughter, daughter.Differentiator.Behavior, Differentiator.Behavior);
                Array.Copy(Differentiator.State, daughter.Differentiator.State, Differentiator.State.Length);
                Array.Copy(Differentiator.gene_id, daughter.Differentiator.gene_id, Differentiator.gene_id.Length);
                Array.Copy(Differentiator.activity, daughter.Differentiator.activity, Differentiator.activity.Length);
                daughter.DifferentiationState = daughter.Differentiator.CurrentState = daughter.Differentiator.Behavior.CurrentState = Differentiator.CurrentState;
                daughter.SetGeneActivities(daughter.Differentiator);
            }

            return daughter;
        }

        private void LoadTransitionDriverElements(Cell daughter, ITransitionDriver daughter_behavior, ITransitionDriver behavior)
        {
            foreach (KeyValuePair<int, Dictionary<int, TransitionDriverElement>> kvp_outer in behavior.Drivers)
            {
                foreach (KeyValuePair<int, TransitionDriverElement> kvp_inner in kvp_outer.Value)
                {
                    if (kvp_inner.Value.GetType() == typeof(MolTransitionDriverElement))
                    {
                        MolTransitionDriverElement tde = new MolTransitionDriverElement();
                        tde.Alpha = ((MolTransitionDriverElement)kvp_inner.Value).Alpha;
                        tde.Beta = ((MolTransitionDriverElement)kvp_inner.Value).Beta;
                        tde.DriverPop = daughter.Cytosol.Populations[((MolTransitionDriverElement)kvp_inner.Value).DriverPop.MoleculeKey];
                        // add it to the daughter
                        daughter_behavior.AddDriverElement(kvp_outer.Key, kvp_inner.Key, tde);
                    }
                    else
                    {
                        DistrTransitionDriverElement tde = new DistrTransitionDriverElement();
                        tde.clock = ((DistrTransitionDriverElement)kvp_inner.Value).clock;
                        tde.timeToNextEvent = ((DistrTransitionDriverElement)kvp_inner.Value).timeToNextEvent;
                        tde.distr = ((DistrTransitionDriverElement)kvp_inner.Value).distr.Clone();
                        // add it to the daughter
                        daughter_behavior.AddDriverElement(kvp_outer.Key, kvp_inner.Key, tde);
                    }
                }
            }
        }

        public int DifferentiationState, DividerState;

        private Locomotor locomotor;

        public Locomotor Locomotor 
        {
            get
            {
                return locomotor;
            }
            set
            {
                locomotor = value;
                if (locomotor != null)
                {
                    this.Driver = locomotor.Driver;
                    this.TransductionConstant = locomotor.TransductionConstant;
                }
                else
                {
                    this.Driver = null;
                    this.TransductionConstant = 0.0;
                }
            }
        }

        public BigInteger Lineage_id { get; set; }

        public Compartment Cytosol { get; private set; }
        public Compartment PlasmaMembrane { get; private set; }

        public StochLocomotor StochLocomotor 
        {
            get
            {
                return stochLocomotor;
            }
            set
            {
                stochLocomotor = value;
                if (stochLocomotor != null)
                {
                    Sigma = stochLocomotor.Sigma;
                }
                else
                {
                    Sigma = 0;
                }
            }
        } 

        /// <summary>
        /// set force to zero
        /// </summary>
        public void resetForce()
        {
            spatialState.F[0] = spatialState.F[1] = spatialState.F[2] = 0;
        }

        /// <summary>
        /// calculate the boundary force when approaching the environment walls
        /// </summary>
        /// <param name="normal">direction of the force</param>
        /// <param name="dist">distance of the cell to the wall</param>
        private void applyBoundaryForce(Vector normal, double dist)
        {
            if (dist != 0.0)
            {
                double force = Pair.Phi1 * (1.0 / dist - 1.0 / radius);
                addForce(normal.Multiply(force).ToArray());
            }
        }

        /// <summary>
        /// boundary forces
        /// </summary>
        public void BoundaryForce()
        {
            // boundary force
            if (SimulationBase.dataBasket.Environment is ECSEnvironment && ((ECSEnvironment)SimulationBase.dataBasket.Environment).toroidal == false)
            {
                double dist = 0.0;

                // X
                // left
                if ((dist = SpatialState.X[0]) < radius)
                {
                    applyBoundaryForce(new DenseVector(new double[] { 1, 0, 0 }), dist);
                }
                // right
                else if ((dist = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(0) - SpatialState.X[0]) < radius)
                {
                    applyBoundaryForce(new DenseVector(new double[] { -1, 0, 0 }), dist);
                }

                // Y
                // bottom
                if ((dist = SpatialState.X[1]) < radius)
                {
                    applyBoundaryForce(new DenseVector(new double[] { 0, 1, 0 }), dist);
                }
                // top
                else if ((dist = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(1) - SpatialState.X[1]) < radius)
                {
                    applyBoundaryForce(new DenseVector(new double[] { 0, -1, 0 }), dist);
                }

                // Z
                // far
                if ((dist = SpatialState.X[2]) < radius)
                {
                    applyBoundaryForce(new DenseVector(new double[] { 0, 0, 1 }), dist);
                }
                // near
                else if ((dist = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(2) - SpatialState.X[2]) < radius)
                {
                    applyBoundaryForce(new DenseVector(new double[] { 0, 0, -1 }), dist);
                }
            }
        }

        /// <summary>
        /// enforce boundary condition
        /// </summary>
        public void EnforceBC()
        {
            // toroidal boundary conditions, wrap around
            // NOTE: this assumes the environment has a lower bound of (0, 0, 0)
            if (SimulationBase.dataBasket.Environment is ECSEnvironment && ((ECSEnvironment)SimulationBase.dataBasket.Environment).toroidal == true)
            {
                for (int i = 0; i < SimulationBase.dataBasket.Environment.Comp.Interior.Dim; i++)
                {
                    // displace the cell such that it wraps around
                    if (SpatialState.X[i] < 0.0)
                    {
                        // use a small fudge factor to displace the cell just back into the grid
                        SpatialState.X[i] = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(i) - SafetySlab;
                    }
                    else if (SpatialState.X[i] > SimulationBase.dataBasket.Environment.Comp.Interior.Extent(i))
                    {
                        SpatialState.X[i] = 0.0;
                    }
                }
            }
            else
            {
                for (int i = 0; i < SimulationBase.dataBasket.Environment.Comp.Interior.Dim; i++)
                {
                    // detect out of bounds cells
                    if (SpatialState.X[i] < 0.0 || SpatialState.X[i] > SimulationBase.dataBasket.Environment.Comp.Interior.Extent(i))
                    {
                        // cell exits
                        exiting = true;
                    }
                }
            }
        }
    }
    /// <summary>
    /// data structure for single cell track data
    /// </summary>
    public class CellTrackData : ReporterData
    {
        public List<double> Times { get; set; }
        public List<double[]> Positions { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public CellTrackData(int key)
        {
            Times = new List<double>();
            Positions = new List<double[]>();
        }

        /// <summary>
        /// do a selection sort to make sure the data is sorted by the time
        /// </summary>
        public void Sort()
        {
            int minloc;
            double dtmp, min;

            for (int i = 0; i < Times.Count - 1; i++)
            {
                // assume min is in starting position
                min = Times[i];
                minloc = i;
                // find the minimum's location
                for (int j = i + 1; j < Times.Count; j++)
                {
                    if (Times[j] < min)
                    {
                        min = Times[j];
                        minloc = j;
                    }
                }
                // swap if needed
                if (minloc != i)
                {
                    // times
                    dtmp = Times[i];
                    Times[i] = Times[minloc];
                    Times[minloc] = dtmp;
                    // position
                    for (int j = 0; j < 3; j++)
                    {
                        dtmp = Positions[i][j];
                        Positions[i][j] = Positions[minloc][j];
                        Positions[minloc][j] = dtmp;
                    }
                }
            }
        }
    }
}



