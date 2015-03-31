using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MathNet.Numerics.LinearAlgebra.Double;
using ManifoldRing;
using Ninject;
using Ninject.Parameters;
using NativeDaphne;

using System.Diagnostics;
using Newtonsoft.Json;


namespace Daphne
{
    public struct CellSpatialState
    {
        public Nt_Darray X;
        public Nt_Darray V;
        public Nt_Darray F;

        public static int SingleDim = 3, Dim = 3 * SingleDim;
    }

    public class Gene
    {
        public Nt_Gene nt_gene; //unmanged instance
        public string Name { get; private set; }
        public int CopyNumber { get; private set; }
        // Activation level may be adjusted depending on cell state 
        public double _activationLevel;
        public double ActivationLevel 
        {
            get
            {
                return _activationLevel;
            }
            set
            {
                _activationLevel = value;
                if (nt_gene != null)
                {
                    nt_gene.SetActivationLevel(value);
                }
            }
        }

        public Gene(string name, int copyNumber, double actLevel)
        {
            Name = name;
            CopyNumber = copyNumber;
            ActivationLevel = actLevel;
        }
    }

    public class Cytosol : Attribute { }
    public class Membrane : Attribute { }

    /// <summary>
    /// The basic representation of a biological cell. 
    /// </summary>
    public class Cell : IDynamic
    {
        /// <summary>
        /// A flag that signals to the cell manager whether the cell is alive or dead.
        /// </summary>
        private bool alive;
        /// <summary>
        /// A flag that signals to the cell manager whether the cell is ready to divide. 
        /// </summary>
        private bool cytokinetic;
        /// <summary>
        /// a flag that signals that the cell is motile
        /// </summary>
        private bool isMotile = true;
        /// <summary>
        /// a flag that signals that the cell responds to chemokine gradients
        /// </summary>
        private bool isChemotactic = true;
        /// <summary>
        /// a flag that signals that the cell is subject to stochastic forces
        /// </summary>
        private bool isStochastic = true;
        /// <summary>
        /// A flag that signals to the cell manager whether the cell is exiting the simulation space.
        /// </summary>
        private bool exiting;

        /// <summary>
        /// The radius of the cell
        /// </summary>
        private double radius;

        /// <summary>
        /// the cell's behaviors (death, division, differentiation)
        /// </summary>
        private ITransitionDriver deathBehavior;
        private ITransitionScheme differentiator, divider;


        /// <summary>
        /// info for rendering
        /// </summary>
        public string renderLabel;
        public int generation;

        public Nt_Cell ntCell;

        /// <summary>
        /// the genes in a cell
        /// NOTE: should these be in the cytoplasm
        /// </summary>
        //public List<Gene> genes;
        private Dictionary<string, Gene> genes;
        public Dictionary<string, Gene> Genes
        {
            get { return genes; }
        }
        public void AddGene(string gene_guid, Gene gene)
        {
            genes.Add(gene_guid, gene);
        }

        public Cell(double radius, int id)
        {
            if (radius <= 0)
            {
                throw new Exception("Cell radius must be greater than zero.");
            }
            alive = true;
            cytokinetic = false;
            this.radius = radius;
            genes = new Dictionary<string, Gene>();
            exiting = false;

            spatialState.X = new Nt_Darray(CellSpatialState.SingleDim);
            spatialState.V = new Nt_Darray(CellSpatialState.SingleDim);
            spatialState.F = new Nt_Darray(CellSpatialState.SingleDim);
            
            if (id > -1)
            {
                Cell_id = id;
                if (id >= SafeCell_id)
                {
                    SafeCell_id = id + 1;
                }
            }
            else
            {
                Cell_id = SafeCell_id++;
            }

            //C++/Cli instance
            ntCell = new Nt_Cell(Cell_id, Radius);
            ntCell.spatialState = new Nt_CellSpatialState(SpatialState.X, SpatialState.V, SpatialState.F);
            Nt_Cell.SafeCell_id = SafeCell_id;
        }

        [Inject]
        [Cytosol]
        public void InjectCytosol(Compartment c)
        {
            Cytosol = c;
            Cytosol.Interior.Initialize(new double[] { radius });
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
            Cytosol.Boundaries.Add(PlasmaMembrane.Interior.Id, PlasmaMembrane);
            Cytosol.BoundaryTransforms.Add(PlasmaMembrane.Interior.Id, new Transform(false));
        }

        public void setSpatialState(double[] s)
        {
            if(s.Length != CellSpatialState.Dim)
            {
                throw new Exception("Cell state length implausible.");
            }

            int i;

            // position
            for (i = 0; i < CellSpatialState.SingleDim; i++)
            {
                spatialState.X[i] = s[i];
            }
            // velocity
            for (i = 0; i < CellSpatialState.SingleDim; i++)
            {
                spatialState.V[i] = s[i + CellSpatialState.SingleDim];
            }
            // force
            for (i = 0; i < CellSpatialState.SingleDim; i++)
            {
                spatialState.F[i] = s[i + 2 * CellSpatialState.SingleDim];
            }
        }

        /// <summary>
        /// set the state as used in rendering; does not set the state for simulation purposes
        /// </summary>
        /// <param name="state"></param>
        public void SetStateForVCR(CellState state)
        {
            // spatial
            setSpatialState(state.spState);
            // generation
            generation = state.CellGeneration;
            // behaviors
            Alive = state.cbState.deathDriverState == 0;
            DividerState = state.cbState.divisionDriverState;
            DifferentiationState = state.cbState.differentiationDriverState;
            // genes
            SetGeneActivities(state.cgState.geneDict);
        }

        public void setSpatialState(CellSpatialState s)
        {
            int i;

            // position
            for (i = 0; i < CellSpatialState.SingleDim; i++)
            {
                spatialState.X[i] = s.X[i];
            }
            // velocity
            for (i = 0; i < CellSpatialState.SingleDim; i++)
            {
                spatialState.V[i] = s.V[i];
            }
            // force
            for (i = 0; i < CellSpatialState.SingleDim; i++)
            {
                spatialState.F[i] = s.F[i];
            }
        }
        
        //public static int iteration_count = 0;
        //public static System.IO.StreamWriter debug_writer = null;
        /// <summary>
        /// Drives the cell's dynamics through time-step dt. The dynamics is applied in-place: the
        /// cell's state is changed directly through this method.
        /// </summary>
        /// <param name="dt">Time interval.</param>
        public void Step(double dt) 
        {
            // we are using the simplest kind of integrator here. It should be made more sophisticated at some point.
            //Cytosol.Step(dt);
 
            //apply cytosol/membrane boundary flux - specific to cytosol/Membrane
            //foreach (KeyValuePair<string, MolecularPopulation> kvp in Cytosol.Populations)
            //{
            //    MolecularPopulation molpop = kvp.Value;
            //    ScalarField conc = molpop.Conc;
            //    foreach (KeyValuePair<int, ScalarField> item in molpop.BoundaryFluxes)
            //    {
            //        conc.DiffusionFluxTerm(item.Value, molpop.Comp.BoundaryTransforms[item.Key], dt);
            //        item.Value.reset(0);
            //    }
            //}

            //update cytosol/membrane boundary
            //foreach (KeyValuePair<string, MolecularPopulation> molpop in Cytosol.Populations)
            //{
            //    molpop.Value.UpdateCytosolMembraneBoundary();
            //}


            //PlasmaMembrane.Step(dt);

            // step the cell behaviors
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

            if (alive && isMotile && (!exiting))
            {
             this.EnforceBC();
            }
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
            daughter = SimulationModule.kernel.Get<Cell>(new ConstructorArgument("radius", radius), new ConstructorArgument("id", -1));
            // same population id
            daughter.Population_id = Population_id;
            daughter.renderLabel = renderLabel;
            this.generation++;
            daughter.generation = generation;
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
                daughter.Cytosol.Populations.Add(kvp.Key, newMP);
            }
            // membrane molpops
            foreach (KeyValuePair<string, MolecularPopulation> kvp in PlasmaMembrane.Populations)
            {
                MolecularPopulation newMP = SimulationModule.kernel.Get<MolecularPopulation>(new ConstructorArgument("mol", kvp.Value.Molecule), new ConstructorArgument("moleculeKey", kvp.Key), new ConstructorArgument("comp", daughter.PlasmaMembrane));

                newMP.Initialize("explicit", kvp.Value.CopyArray());
                daughter.PlasmaMembrane.Populations.Add(kvp.Key, newMP);
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

            string cell_guid;

            if (SimulationBase.ProtocolHandle.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == true)
            {
                // only the TissueScenario has cell populations
                cell_guid = ((TissueScenario)SimulationBase.ProtocolHandle.scenario).GetCellPopulation(daughter.Population_id).Cell.entity_guid;
            }
            else
            {
                // for now
                throw new NotImplementedException();
            }

            configComp[0] = SimulationBase.ProtocolHandle.entity_repository.cells_dict[cell_guid].cytosol;
            configComp[1] = SimulationBase.ProtocolHandle.entity_repository.cells_dict[cell_guid].membrane;

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
                daughter.Divider.Initialize(Divider.nStates, Divider.nGenes);
                LoadTransitionDriverElements(daughter, daughter.Divider.Behavior, Divider.Behavior);
                Array.Copy(Divider.State, daughter.Divider.State, Divider.State.Length);
                Array.Copy(Divider.gene_id, daughter.Divider.gene_id, Divider.gene_id.Length);
                Array.Copy(Divider.activity, daughter.Divider.activity, Divider.activity.Length);
                daughter.DividerState = daughter.Divider.CurrentState = Divider.CurrentState;
                daughter.SetGeneActivities(daughter.Divider);
                daughter.Divider.Behavior.InitializeState();
            }

            // differentiation
            if (Differentiator.nStates > 1)
            {
                daughter.Differentiator.Initialize(Differentiator.nStates, Differentiator.nGenes);
                LoadTransitionDriverElements(daughter, daughter.Differentiator.Behavior, Differentiator.Behavior);
                Array.Copy(Differentiator.State, daughter.Differentiator.State, Differentiator.State.Length);
                Array.Copy(Differentiator.gene_id, daughter.Differentiator.gene_id, Differentiator.gene_id.Length);
                Array.Copy(Differentiator.activity, daughter.Differentiator.activity, Differentiator.activity.Length);
                daughter.DifferentiationState = daughter.Differentiator.CurrentState = Differentiator.CurrentState;
                daughter.SetGeneActivities(daughter.Differentiator);
                daughter.Differentiator.Behavior.InitializeState();
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
                        tde.distr = ((DistrTransitionDriverElement)kvp_inner.Value).distr.Clone();
                        // add it to the daughter
                        daughter_behavior.AddDriverElement(kvp_outer.Key, kvp_inner.Key, tde);
                    }
                }
            }
        }

        public int DifferentiationState, DividerState;

        public Locomotor Locomotor { get; set; }
        public Compartment Cytosol { get; private set; }
        public Compartment PlasmaMembrane { get; private set; }
        private CellSpatialState spatialState;
        public double DragCoefficient { get; set; }
        public StochLocomotor StochLocomotor { get; set; } 

        public int Cell_id { get; private set; }
        public static int SafeCell_id = 0;
        public int Population_id { get; set; }
        protected int[] gridIndex = { -1, -1, -1 };
        public static double defaultRadius = 5.0;

        public CellSpatialState SpatialState
        {
            get { return spatialState; }
            set { spatialState = value; }
        }

        public bool IsMotile
        {
            get { return isMotile; }
            set { isMotile = value; }
        }
        public bool IsChemotactic
        {
            get { return isChemotactic; }
            set { isChemotactic = value; }

        }
        public bool IsStochastic
        {
            get { return isStochastic; }
            set { isStochastic = value; }

        }
        public bool Alive
        {
            get { return alive; }
            set { alive = value; }
        }

        public bool Cytokinetic
        {
            get { return cytokinetic; }
            set { cytokinetic = value; }
        }

        public double Radius
        {
            get { return radius; }
        }

        /// <summary>
        /// retrieve the cell's grid index
        /// </summary>
        public int[] GridIndex
        {
            get 
            {
                return gridIndex; 
            }
        }

        public bool Exiting
        {
            get { return exiting; }
            set 
            { 
                exiting = value; 
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
        /// accumulate the force vector
        /// </summary>
        /// <param name="f"></param>
        public void addForce(double[] f)
        {
            spatialState.F[0] += f[0];
            spatialState.F[1] += f[1];
            spatialState.F[2] += f[2];
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
                    double safetySlab = 1e-3;

                    // displace the cell such that it wraps around
                    if (SpatialState.X[i] < 0.0)
                    {
                        // use a small fudge factor to displace the cell just back into the grid
                        SpatialState.X[i] = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(i) - safetySlab;
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
}



