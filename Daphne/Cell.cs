using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MathNet.Numerics.LinearAlgebra;
using ManifoldRing;
using Ninject;
using Ninject.Parameters;

namespace Daphne
{
    public struct CellSpatialState
    {
        public double[] X;
        public double[] V;
        public double[] F;

        public static int Dim = 9;
    }

    public class Gene
    {
        public string Name { get; private set; }
        public int CopyNumber { get; private set; }
        // Activation level may be adjusted depending on cell state 
        public double ActivationLevel { get; set; }

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

        public Cell(double radius)
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

            Cell_id = SafeCell_id++;
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

        public void setState(double[] s)
        {
            if(s.Length != CellSpatialState.Dim)
            {
                throw new Exception("Cell state length implausible.");
            }
            spatialState.X = new double[] { s[0], s[1], s[2] };
            spatialState.V = new double[] { s[3], s[4], s[5] };
            spatialState.F = new double[] { s[6], s[7], s[8] };
        }

        public void setState(CellSpatialState s)
        {
            spatialState.X = new double[] { s.X[0], s.X[1], s.X[2] };
            spatialState.V = new double[] { s.V[0], s.V[1], s.V[2] };
            spatialState.F = new double[] { s.F[0], s.F[1], s.F[2] };
        }
        
        /// <summary>
        /// Drives the cell's dynamics through time-step dt. The dynamics is applied in-place: the
        /// cell's state is changed directly through this method.
        /// </summary>
        /// <param name="dt">Time interval.</param>
        public void Step(double dt) 
        {
            // we are using the simplest kind of integrator here. It should be made more sophisticated at some point.
            Cytosol.Step(dt);
            PlasmaMembrane.Step(dt);

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
                    Divider.CurrentState = 0;
                    Divider.Behavior.CurrentState = 0;
                    Divider.PreviousState = Divider.Behavior.FinalState;
                    Divider.Behavior.PreviousState = Divider.Behavior.FinalState;
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
            daughter = SimulationModule.kernel.Get<Cell>(new ConstructorArgument("radius", radius));
            // same population id
            daughter.Population_id = Population_id;
            daughter.renderLabel = renderLabel;
            this.generation++;
            daughter.generation = generation;
            // same state
            daughter.setState(spatialState);
            // but offset the daughter randomly
            double[] delta = radius * Rand.RandomDirection(daughter.spatialState.X.Length);

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
            SimulationBase.AddCompartmentBoundaryReactions(daughter.Cytosol, daughter.PlasmaMembrane, SimulationBase.ProtocolHandle.entity_repository, boundary_reacs);
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
            foreach(KeyValuePair<int, Dictionary<int, TransitionDriverElement>> kvp_outer in DeathBehavior.Drivers)
            {
                foreach (KeyValuePair<int, TransitionDriverElement> kvp_inner in kvp_outer.Value)
                {
                    TransitionDriverElement tde = new TransitionDriverElement();

                    tde.DriverPop = daughter.Cytosol.Populations[kvp_inner.Value.DriverPop.MoleculeKey];
                    tde.Alpha = kvp_inner.Value.Alpha;
                    tde.Beta = kvp_inner.Value.Beta;
                    // add it to the daughter
                    daughter.DeathBehavior.AddDriverElement(kvp_outer.Key, kvp_inner.Key, tde);
                }
            }

            // division
            if (Divider.nStates > 1)
            {
                daughter.Divider.Initialize(Divider.nStates, Divider.nGenes);
                foreach (KeyValuePair<int, Dictionary<int, TransitionDriverElement>> kvp_outer in Divider.Behavior.Drivers)
                {
                    foreach (KeyValuePair<int, TransitionDriverElement> kvp_inner in kvp_outer.Value)
                    {
                        TransitionDriverElement tde = new TransitionDriverElement();

                        tde.DriverPop = daughter.Cytosol.Populations[kvp_inner.Value.DriverPop.MoleculeKey];
                        tde.Alpha = kvp_inner.Value.Alpha;
                        tde.Beta = kvp_inner.Value.Beta;
                        // add it to the daughter
                        daughter.Divider.Behavior.AddDriverElement(kvp_outer.Key, kvp_inner.Key, tde);
                    }
                }
                Array.Copy(Divider.State, daughter.Divider.State, Divider.State.Length);
                Array.Copy(Divider.gene_id, daughter.Divider.gene_id, Divider.gene_id.Length);
                Array.Copy(Divider.activity, daughter.Divider.activity, Divider.activity.Length);
                daughter.DividerState = Divider.CurrentState;
                daughter.SetGeneActivities(Divider);
            }


            // differentiation
            if (Differentiator.nStates > 1)
            {
                daughter.Differentiator.Initialize(Differentiator.nStates, Differentiator.nGenes);
                foreach (KeyValuePair<int, Dictionary<int, TransitionDriverElement>> kvp_outer in Differentiator.Behavior.Drivers)
                {
                    foreach (KeyValuePair<int, TransitionDriverElement> kvp_inner in kvp_outer.Value)
                    {
                        TransitionDriverElement tde = new TransitionDriverElement();

                        tde.DriverPop = daughter.Cytosol.Populations[kvp_inner.Value.DriverPop.MoleculeKey];
                        tde.Alpha = kvp_inner.Value.Alpha;
                        tde.Beta = kvp_inner.Value.Beta;
                        // add it to the daughter
                        daughter.Differentiator.Behavior.AddDriverElement(kvp_outer.Key, kvp_inner.Key, tde);
                    }
                }
                Array.Copy(Differentiator.State, daughter.Differentiator.State, Differentiator.State.Length);
                Array.Copy(Differentiator.gene_id, daughter.Differentiator.gene_id, Differentiator.gene_id.Length);
                Array.Copy(Differentiator.activity, daughter.Differentiator.activity, Differentiator.activity.Length);
                daughter.DifferentiationState = Differentiator.CurrentState;
                daughter.SetGeneActivities(Differentiator);
            }

            return daughter;
        }

        public int DifferentiationState, DividerState;

        public Locomotor Locomotor { get; set; }
        public Compartment Cytosol { get; private set; }
        public Compartment PlasmaMembrane { get; private set; }
        //public Differentiator Differentiator { get; private set; }
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
            get { return gridIndex; }
        }

        public bool Exiting
        {
            get { return exiting; }
            set { exiting = value; }
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

                addForce(normal * force);
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
                    applyBoundaryForce(new double[] { 1, 0, 0 }, dist);
                }
                // right
                else if ((dist = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(0) - SpatialState.X[0]) < radius)
                {
                    applyBoundaryForce(new double[] { -1, 0, 0 }, dist);
                }

                // Y
                // bottom
                if ((dist = SpatialState.X[1]) < radius)
                {
                    applyBoundaryForce(new double[] { 0, 1, 0 }, dist);
                }
                // top
                else if ((dist = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(1) - SpatialState.X[1]) < radius)
                {
                    applyBoundaryForce(new double[] { 0, -1, 0 }, dist);
                }

                // Z
                // far
                if ((dist = SpatialState.X[2]) < radius)
                {
                    applyBoundaryForce(new double[] { 0, 0, 1 }, dist);
                }
                // near
                else if ((dist = SimulationBase.dataBasket.Environment.Comp.Interior.Extent(2) - SpatialState.X[2]) < radius)
                {
                    applyBoundaryForce(new double[] { 0, 0, -1 }, dist);
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



