using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Numerics;
using System.Collections.Specialized;
using System.Collections.ObjectModel;

using MathNet.Numerics.LinearAlgebra.Double;

using Nt_ManifoldRing;

using Ninject;
using Ninject.Parameters;

using System.Diagnostics;
using NativeDaphne;
using Gene = NativeDaphne.Nt_Gene;

namespace Daphne
{
    public abstract class SimulationBase : EntityModelBase, IDynamic
    {
        public SimulationBase()
        {
            cellManager = new CellManager();
        }

        /// <summary>
        /// accessor for the simulation's run status
        /// </summary>
        public byte RunStatus
        {
            get { return runStatus; }
            set
            {
                runStatus = value;
                OnPropertyChanged("RunStatus");
            }
        }

        public void ClearFlag(byte flag)
        {
            simFlags &= (byte)~flag;
        }

        public bool CheckFlag(byte flag)
        {
            return (simFlags & flag) != 0;
        }

        public void SetFlag(byte flag)
        {
            simFlags |= flag;
        }

        /// <summary>
        /// for multiple cell simulations, find a non-overlapping initial configuration
        /// </summary>
        public virtual void Burn_inStep()
        {
        }

        /// <summary>
        /// indicates whether burn in needs to run, only should ever return true for multiple cell simulations
        /// </summary>
        /// <returns>true when burn in needs to run a step</returns>
        public virtual bool Burn_inActive()
        {
            return false;
        }

        /// <summary>
        /// finish burn in
        /// </summary>
        public virtual void Burn_inCleanup()
        {
        }
        public virtual void reset()
        {
            RunStatus = RUNSTAT_OFF;
            accumulatedTime = 0.0;
            renderCount = sampleCount = 0;
        }

        public void restart()
        {
            lock (this)
            {
                reset();
                RunStatus = RUNSTAT_READY;
            }
        }

        public double AccumulatedTime
        {
            get { return accumulatedTime; }
        }

        /// <summary>
        /// calculate and return the progress of the simulation
        /// </summary>
        /// <returns>integer indicating the percent of progress</returns>
        public int GetProgressPercent()
        {
            int percent = (accumulatedTime == 0) ? 0 : (int)(100 * accumulatedTime / duration);

            if (RunStatus == RUNSTAT_RUN)
            {
                if (percent >= 100)
                {
                    percent = 99;
                }
            }
            else if (RunStatus == RUNSTAT_FINISHED)
            {
                if (percent > 0)
                {
                    percent = 100;
                }
            }
            else if (percent > 100)
            {
                percent = 100;
            }

            return percent;
        }

        private void LoadTransitionDriverElements(ConfigTransitionDriver config_td, Dictionary<string, MolecularPopulation> population, ITransitionDriver behavior)
        {
            foreach (ConfigTransitionDriverRow row in config_td.DriverElements)
            {
                foreach (ConfigTransitionDriverElement config_tde in row.elements)
                {
                    if (config_tde.GetType() == typeof(ConfigMolTransitionDriverElement))
                    {

                        if (population.ContainsKey(((ConfigMolTransitionDriverElement)config_tde).driver_mol_guid_ref) == true)
                        {
                            MolTransitionDriverElement tde = new MolTransitionDriverElement();
                            tde.Alpha = ((ConfigMolTransitionDriverElement)config_tde).Alpha;
                            tde.Beta = ((ConfigMolTransitionDriverElement)config_tde).Beta;
                            tde.DriverPop = population[((ConfigMolTransitionDriverElement)config_tde).driver_mol_guid_ref];
                            behavior.AddDriverElement(config_tde.CurrentState, config_tde.DestState, tde);
                        }
                    }
                    else if (config_tde.GetType() == typeof(ConfigDistrTransitionDriverElement))
                    {
                        DistrTransitionDriverElement tde = new DistrTransitionDriverElement();
                        if (((ConfigDistrTransitionDriverElement)config_tde).Distr.DistributionType == ParameterDistributionType.CONSTANT)
                        {
                            tde.distr = new DiracDeltaParameterDistribution();
                            ((DiracDeltaParameterDistribution)tde.distr).ConstValue = ((ConfigDistrTransitionDriverElement)config_tde).Distr.ConstValue;
                        }
                        else
                        {
                            tde.distr = ((ConfigDistrTransitionDriverElement)config_tde).Distr.ParamDistr.Clone();
                        }
                        behavior.AddDriverElement(config_tde.CurrentState, config_tde.DestState, tde);
                    }
                }
            }
        }

        public void LoadTransitionScheme(ConfigTransitionScheme cds, Cell cell, ITransitionScheme trans_scheme)
        {
            ConfigTransitionScheme config_Scheme = cds;
            ConfigTransitionDriver config_td = config_Scheme.Driver;

            trans_scheme.Initialize(config_Scheme.activationRows.Count, config_Scheme.genes.Count);
            LoadTransitionDriverElements(config_td, cell.Cytosol.Populations, trans_scheme.Behavior);

            // Load state names
            for (int j = 0; j < trans_scheme.nStates; j++)
            {
                trans_scheme.AddState(j, config_Scheme.Driver.states[j]);
            }

            // Epigenetic information
            for (int ii = 0; ii < trans_scheme.nGenes; ii++)
            {
                trans_scheme.AddGene(ii, config_Scheme.genes[ii]);

                for (int j = 0; j < trans_scheme.nStates; j++)
                {
                    trans_scheme.AddActivity(j, ii, config_Scheme.activationRows[j].activations[ii]);
                }
            }
        }

        private void addCellMolpops(ConfigCompartment[] configComp, Compartment[] simComp)
        {
            MoleculeLocation[] molLoc = new MoleculeLocation[] { MoleculeLocation.Bulk, MoleculeLocation.Boundary};

            for (int comp = 0; comp < 2; comp++)
            {
                addCompartmentMolpops(simComp[comp], configComp[comp], molLoc[comp]);
            }
        }

        public void prepareCellInstantiation(CellPopulation cp,
                                             ConfigCompartment[] configComp,
                                             List<ConfigReaction>[] bulk_reacs,
                                             ref List<ConfigReaction> boundary_reacs,
                                             ref List<ConfigReaction> transcription_reacs)
        {
            // if this is a new cell population, add it
            dataBasket.AddPopulation(cp.cellpopulation_id);

            configComp[0] = cp.Cell.cytosol;
            configComp[1] = cp.Cell.membrane;

            bulk_reacs[0] = ProtocolHandle.GetReactions(configComp[0], false);
            bulk_reacs[1] = ProtocolHandle.GetReactions(configComp[1], false);
            boundary_reacs = ProtocolHandle.GetReactions(configComp[0], true);
            transcription_reacs = ProtocolHandle.GetTranscriptionReactions(configComp[0]);
            //need to figure out how to set the label
            if (cp.renderLabel == null)
            {
                cp.renderLabel = cp.Cell.entity_guid;
            }

            // Force all cell distributed parameters to reinitialize.
            // Otherwise we won't get reproducible results for the same global seed.
            cp.Cell.ResetDistributedParameters();
        }

        /// <summary>
        /// instantiate a cell based on given values
        /// </summary>
        /// <param name="cp">the population this cell is part of</param>
        /// <param name="configComp">the two cell compartments (cytosol, membrane)</param>
        /// <param name="bulk_reacs">bulk reactions (cytosol, membrane)</param>
        /// <param name="boundary_reacs">boundary reactions</param>
        /// <param name="transcription_reacs">transcription reactions</param>
        /// <param name="report">when true, prepare boundary reaction report</param>
        public Cell instantiateCell(CellPopulation cp,
                                    ConfigCompartment[] configComp,
                                    List<ConfigReaction>[] bulk_reacs,
                                    List<ConfigReaction> boundary_reacs,
                                    List<ConfigReaction> transcription_reacs,
                                    bool report)
        {
            bool[] result = null;

            // only report on demand
            if (report == true)
            {
                prepareBoundaryReactionReport(boundary_reacs.Count, ref result);
            }

            Cell simCell = SimulationModule.kernel.Get<Cell>(new ConstructorArgument("radius", cp.Cell.CellRadius));

            simCell.renderLabel = cp.Cell.renderLabel ?? cp.Cell.entity_guid;
            Compartment[] simComp = new Compartment[2];

            simComp[0] = simCell.Cytosol;
            simComp[1] = simCell.PlasmaMembrane;

            // cell population id
            simCell.Population_id = cp.cellpopulation_id;

            // modify molpop information before setting
            addCellMolpops(configComp, simComp);

            // cell genes
            foreach (ConfigGene cg in cp.Cell.genes)
            {
                //ConfigGene cg = protocol.entity_repository.genes_dict[s];
                double geneActivationLevel = cg.ActivationLevel;
                Gene gene = SimulationModule.kernel.Get<Gene>(new ConstructorArgument("name", cg.Name),
                                                              new ConstructorArgument("copyNumber", cg.CopyNumber),
                                                              new ConstructorArgument("actLevel", geneActivationLevel));

                simCell.AddGene(cg.entity_guid, gene);
            }

            //CELL REACTIONS
            AddCompartmentBulkReactions(simCell.Cytosol, ProtocolHandle.entity_repository, bulk_reacs[0]);
            AddCompartmentBulkReactions(simCell.PlasmaMembrane, ProtocolHandle.entity_repository, bulk_reacs[1]);
            // membrane has no boundary
            AddCompartmentBoundaryReactions(simCell.Cytosol, simCell.PlasmaMembrane, ProtocolHandle.entity_repository, boundary_reacs, result);
            AddCellTranscriptionReactions(simCell, ProtocolHandle.entity_repository, transcription_reacs);

            double nextValue;
            int nextIntValue;

            if (simCell.Cytosol.Populations.ContainsKey(cp.Cell.locomotor_mol_guid_ref) == true)
            {
                MolecularPopulation driver = simCell.Cytosol.Populations[cp.Cell.locomotor_mol_guid_ref];
                simCell.Locomotor = new Locomotor(driver, cp.Cell.TransductionConstant.Sample());
                simCell.IsChemotactic = true;
            }
            else
            {
                simCell.IsChemotactic = false;
            }

            nextValue = cp.Cell.Sigma.Sample();
            if (nextValue > 0)
            {
                simCell.IsStochastic = true;
                simCell.StochLocomotor = new StochLocomotor(nextValue);
            }
            else
            {
                simCell.IsStochastic = false;
            }
            if (simCell.IsChemotactic || simCell.IsStochastic)
            {
                simCell.IsMotile = true;
                simCell.DragCoefficient = cp.Cell.DragCoefficient.Sample();
            }
            else
            {
                simCell.IsMotile = false;
            }

            //TRANSITIONS
            // death behavior
            if (cp.Cell.death_driver != null)
            {
                nextIntValue = (int)cp.Cell.death_driver.CurrentState.Sample();
                if (nextIntValue >= 0 && nextIntValue < cp.Cell.death_driver.states.Count())
                {
                    simCell.DeathBehavior.CurrentState = nextIntValue;
                }
                // else, the default is 0

                // set the alive flag
                simCell.Alive = simCell.DeathBehavior.CurrentState == 0;

                ConfigTransitionDriver config_td = cp.Cell.death_driver;

                LoadTransitionDriverElements(config_td, simCell.Cytosol.Populations, simCell.DeathBehavior);
                simCell.DeathBehavior.InitializeState();
            }

            // Division 
            if (cp.Cell.div_scheme != null)
            {
                LoadTransitionScheme(cp.Cell.div_scheme, simCell, simCell.Divider);

                // from distribution
                nextIntValue = (int)cp.Cell.div_scheme.Driver.CurrentState.Sample();
                if (nextIntValue >= 0 && nextIntValue < cp.Cell.div_scheme.Driver.states.Count())
                {
                    simCell.Divider.Behavior.CurrentState = nextIntValue;
                }
                // else, the default is 0

                simCell.Divider.Behavior.InitializeState();
                // Set cell division scheme state
                simCell.DividerState = simCell.Divider.CurrentState = simCell.Divider.Behavior.CurrentState;
                // Set gene activity levels now that the current state is set
                simCell.SetGeneActivities(simCell.Divider);
            }

            // Differentiation
            if (cp.Cell.diff_scheme != null)
            {
                LoadTransitionScheme(cp.Cell.diff_scheme, simCell, simCell.Differentiator);

                // from distribution
                nextIntValue = (int)cp.Cell.diff_scheme.Driver.CurrentState.Sample();
                if (nextIntValue >= 0 && nextIntValue < cp.Cell.diff_scheme.Driver.states.Count())
                {
                    simCell.Differentiator.Behavior.CurrentState = nextIntValue;
                }
                // else, the default is 0

                simCell.Differentiator.Behavior.InitializeState();
                // Set cell differentiation state
                simCell.DifferentiationState = simCell.Differentiator.CurrentState = simCell.Differentiator.Behavior.CurrentState;
                // Set gene activity levels now that the current state is set
                simCell.SetGeneActivities(simCell.Differentiator);
            }

            //parameters for objects not created in the middle layer
            simCell.Sigma = simCell.StochLocomotor != null ? simCell.StochLocomotor.Sigma : 0.0;
            simCell.TransductionConstant = simCell.Locomotor != null ? simCell.Locomotor.TransductionConstant : 0;

            if (report == true && result != null)
            {
                // report if a reaction could not be inserted
                boundaryReactionReport(boundary_reacs, result, "population " + cp.cellpopulation_id + ", cell " + cp.Cell.CellName);
            }

            return simCell;
        }

        /// <summary>
        /// adds a list of molpops to the compartment
        /// </summary>
        /// <param name="simComp">the compartment</param>
        /// <param name="molpops">the list of molpops</param>
        private void addCompartmentMolpops(Compartment simComp, ObservableCollection<ConfigMolecularPopulation> molpops, MoleculeLocation molLoc)
        {
            foreach (ConfigMolecularPopulation cmp in molpops)
            {
                // avoid duplicates
                if (simComp.Populations.ContainsKey(cmp.molecule.entity_guid) == true)
                {
                    continue;
                }

                // cytosol and ecm reaction complexes may contain boundary molecules
                if (cmp.molecule.molecule_location != molLoc)
                {
                    continue;
                }

                Molecule mol = SimulationModule.kernel.Get<Molecule>(new ConstructorArgument("name", cmp.molecule.Name),
                                                                     new ConstructorArgument("mw", cmp.molecule.MolecularWeight),
                                                                     new ConstructorArgument("effRad", cmp.molecule.EffectiveRadius),
                                                                     new ConstructorArgument("diffCoeff", cmp.molecule.DiffusionCoefficient));

                if (cmp.mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
                {
                    MolPopGaussian mpgg = (MolPopGaussian)cmp.mp_distribution;

                    if (mpgg.gauss_spec == null || mpgg.gauss_spec.box_spec == null)
                    {
                        // Should never reach here... pop up notice
                        MessageBoxResult tmp = MessageBox.Show("Problem: Invalid Gaussian or box spec...");
                        return;
                    }

                    BoxSpecification box = mpgg.gauss_spec.box_spec;

                    double[] sigma = new double[] { box.x_scale / 2, box.y_scale / 2, box.z_scale / 2 };

                    // compute rotation matrix from box data
                    // R is vtk's rotation matrix (transpose of traditional definition)
                    double[,] R = new double[3, 3];
                    for (int i = 0; i < 3; i++)
                    {
                        R[i, 0] = box.transform_matrix[i][0] / box.getScale((byte)0);
                        R[i, 1] = box.transform_matrix[i][1] / box.getScale((byte)1);
                        R[i, 2] = box.transform_matrix[i][2] / box.getScale((byte)2);
                    }

                    // Rotate diagonal covariance (Sigma) matrix: D[i,j] = delta_ij sigma[i]^(-2)
                    // Given vtk format where R is the transpose of the usual definition of the rotation matrix:  
                    //      S = R' D R
                    //      S is the rotated covariance matrix
                    //      R is vtk's rotation matrix (tranpose of traditional definition)
                    //      R' is the transpose of R

                    // T = R D
                    double[,] S = new double[3, 3];
                    double[,] T = new double[3, 3];
                    for (int i = 0; i < 3; i++)
                    {
                        T[i, 0] = R[i, 0] / (sigma[0] * sigma[0]);
                        T[i, 1] = R[i, 1] / (sigma[1] * sigma[1]);
                        T[i, 2] = R[i, 2] / (sigma[2] * sigma[2]);
                    }

                    // S = T R'
                    for (int i = 0; i < 3; i++)
                    {
                        for (int j = 0; j < 3; j++)
                        {
                            for (int k = 0; k < 3; k++)
                            {
                                S[i, j] += T[i, k] * R[j, k];
                            }
                        }
                    }

                    // Gaussian distribution parameters: coordinates of center, standard deviations (sigma), and peak concentrtation
                    // box x,y,z_scale parameters are 2*sigma
                    double[] initArray = new double[] { box.x_trans, box.y_trans, box.z_trans,
                                                        S[0,0], S[1,0], S[2,0],
                                                        S[0,1], S[1,1], S[2,1],
                                                        S[0,2], S[1,2], S[2,2],
                                                        mpgg.peak_concentration };

                    simComp.AddMolecularPopulation(mol, cmp.molecule.entity_guid, "gauss", initArray);
                }
                else if (cmp.mp_distribution.mp_distribution_type == MolPopDistributionType.Homogeneous)
                {
                    MolPopHomogeneousLevel mphl = (MolPopHomogeneousLevel)cmp.mp_distribution;
                    simComp.AddMolecularPopulation(mol, cmp.molecule.entity_guid, "const", new double[] { mphl.concentration });
                }
                else if (cmp.mp_distribution.mp_distribution_type == MolPopDistributionType.Explicit)
                {
                    MolPopExplicit mpc = (MolPopExplicit)cmp.mp_distribution;
                    simComp.AddMolecularPopulation(mol, cmp.molecule.entity_guid, "explicit", mpc.conc);
                }
                else if (cmp.mp_distribution.mp_distribution_type == MolPopDistributionType.Linear)
                {
                    MolPopLinear mpl = cmp.mp_distribution as MolPopLinear;
                    double c1 = mpl.boundaryCondition[0].concVal;
                    double c2 = mpl.boundaryCondition[1].concVal;
                    double x2;

                    x2 = linearDistributionCase(mpl.dim);
                    simComp.AddMolecularPopulation(mol, cmp.molecule.entity_guid, "linear", new double[] {       
                                c1, 
                                c2,
                                mpl.x1, 
                                x2, 
                                mpl.dim});
                }
                else
                {
                    throw new Exception("Molecular population distribution type not implemented.");
                }
            }
        }

        /// <summary>
        /// add molpops for a whole compartment, includes reaction complexes
        /// specify whether bulk or boundary molecules are allowed
        /// </summary>
        /// <param name="simComp">the simlation compartment</param>
        /// <param name="configComp">the config compartment that describes the simulation compartment</param>
        protected void addCompartmentMolpops(Compartment simComp, ConfigCompartment configComp, MoleculeLocation molLoc)
        {
            addCompartmentMolpops(simComp, configComp.molpops, molLoc);
            foreach (ConfigReactionComplex rc in configComp.reaction_complexes)
            {
                addCompartmentMolpops(simComp, rc.molpops, molLoc);
            }
        }

        public virtual void Load(Protocol protocol, bool completeReset, int repetition)
        {
            ProtocolHandle = protocol;

            duration = protocol.scenario.time_config.duration;
            sampleStep = protocol.scenario.time_config.sampling_interval;
            renderStep = protocol.scenario.time_config.rendering_interval;
            integratorStep = protocol.scenario.time_config.integrator_step;
            // make sure the simulation does not start to run immediately
            RunStatus = RUNSTAT_OFF;

            // exit if no reset required
            if (completeReset == false)
            {
                return;
            }

            if (repetition < 2)
            {
                Rand.ReseedAll(protocol.sim_params.globalRandomSeed);
            }

            // executes the ninject bindings; call this after the config is initialized with valid values
            SimulationModule.kernel = new StandardKernel(new SimulationModule(protocol.scenario));

            // create a factory container: shared factories reside here; not all instances of a class
            // need their own factory
            SimulationModule.kernel.Get<FactoryContainer>();

            cellManager.DeadDict.Clear();
        }

        public CollisionManager CollisionManager
        {
            get { return collisionManager; }
        }

        public ReporterBase Reporter
        {
            get
            {
                return reporter;
            }
            set
            {
                reporter = value;
            }
        }

        public HDF5FileBase HDF5FileHandle
        {
            get
            {
                return hdf5file;
            }
        }

        protected void prepareBoundaryReactionReport(int size, ref bool[] result)
        {
            result = new bool[size];
            for (int i = 0; i < size; i++)
            {
                result[i] = false;
            }
        }

        protected void boundaryReactionReport(List<ConfigReaction> boundary_reacs, bool[] result, string id)
        {
            // should always be so, but for safety have this check
            if (boundary_reacs.Count == result.Length)
            {
                bool allPresent = true;

                for (int i = 0; i < result.Length; i++)
                {
                    if (result[i] == false)
                    {
                        allPresent = false;
                        break;
                    }
                }
                if (allPresent == false)
                {
                    // configure the message box to be displayed
                    string messageBoxText = "Not all boundary reactions could be added into " + id + ":";
                    string caption = "Boundary reaction warning";
                    MessageBoxButton button = MessageBoxButton.OK;
                    MessageBoxImage icon = MessageBoxImage.Warning;

                    for (int i = 0; i < result.Length; i++)
                    {
                        if (result[i] == false)
                        {
                            messageBoxText += "\n" + boundary_reacs[i].TotalReactionString;
                        }
                    }
                    // display message box
                    MessageBox.Show(messageBoxText, caption, button, icon);
                }
            }
        }

        /// <summary>
        /// adds boundary reactions to the compartment
        /// </summary>
        /// <param name="comp">the compartment</param>
        /// <param name="boundary">its boundary compartment</param>
        /// <param name="er">the entity repository</param>
        /// <param name="config_reacs">the boundary reactions</param>
        /// <param name="result">result array, null when no reporting needed</param>
        public static void AddCompartmentBoundaryReactions(Compartment comp, Compartment boundary, EntityRepository er, List<ConfigReaction> config_reacs, bool[] result)
        {
            // NOTES: 
            // ConfigCreator.PredefinedReactionsCreator() and ProtocolToolWindow.btnSave_Click() 
            // add bulk molecules then boundary molecules to the reactant, product, and modifier lists.
            // 
            // When comp is ECS then ECS boundary reactions may not apply to some cell types. 
            // The continue statements below catch these cases.

            for (int i = 0; i < config_reacs.Count; i++)
            {
                ConfigReaction cr = config_reacs[i];

                if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.BoundaryAssociation)
                {
                    if (!boundary.Populations.ContainsKey(cr.reactants_molecule_guid_ref[1]) || !boundary.Populations.ContainsKey(cr.products_molecule_guid_ref[0]))
                    {
                        continue;
                    }
                    comp.AddBoundaryReaction(boundary.Interior.Id, new BoundaryAssociation(boundary.Populations[cr.reactants_molecule_guid_ref[1]],
                                                                                           comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                                           boundary.Populations[cr.products_molecule_guid_ref[0]], cr.rate_const));
                    if (result != null)
                    {
                        result[i] = true;
                    }
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.BoundaryDissociation)
                {
                    if (!boundary.Populations.ContainsKey(cr.reactants_molecule_guid_ref[0]) || !boundary.Populations.ContainsKey(cr.products_molecule_guid_ref[1]))
                    {
                        continue;
                    }
                    comp.AddBoundaryReaction(boundary.Interior.Id, new BoundaryDissociation(boundary.Populations[cr.products_molecule_guid_ref[1]],
                                                                                            comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                                            boundary.Populations[cr.reactants_molecule_guid_ref[0]], cr.rate_const));
                    if (result != null)
                    {
                        result[i] = true;
                    }
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedBoundaryActivation)
                {
                    if (!boundary.Populations.ContainsKey(cr.modifiers_molecule_guid_ref[0]))
                    {
                        continue;
                    }
                    comp.AddBoundaryReaction(boundary.Interior.Id, new CatalyzedBoundaryActivation(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                                                   comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                                                   boundary.Populations[cr.modifiers_molecule_guid_ref[0]], cr.rate_const));
                    if (result != null)
                    {
                        result[i] = true;
                    }
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.BoundaryTransportTo)
                {
                    if (!boundary.Populations.ContainsKey(cr.products_molecule_guid_ref[0]))
                    {
                        continue;
                    }
                    comp.AddBoundaryReaction(boundary.Interior.Id, new BoundaryTransportTo(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                                           boundary.Populations[cr.products_molecule_guid_ref[0]], cr.rate_const));
                    if (result != null)
                    {
                        result[i] = true;
                    }
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.BoundaryTransportFrom)
                {
                    if (!boundary.Populations.ContainsKey(cr.reactants_molecule_guid_ref[0]))
                    {
                        continue;
                    }
                    comp.AddBoundaryReaction(boundary.Interior.Id, new BoundaryTransportFrom(boundary.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                                             comp.Populations[cr.products_molecule_guid_ref[0]], cr.rate_const));
                    if (result != null)
                    {
                        result[i] = true;
                    }
                }
            }
        }

        public static void AddCompartmentBulkReactions(Compartment comp, EntityRepository er, List<ConfigReaction> config_reacs)
        {
            foreach (ConfigReaction cr in config_reacs)
            {
                if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Association)
                {
                    comp.AddBulkReaction(new Association(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                           comp.Populations[cr.reactants_molecule_guid_ref[1]],
                                                           comp.Populations[cr.products_molecule_guid_ref[0]],
                                                           cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Dissociation)
                {
                    comp.AddBulkReaction(new Dissociation(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                            comp.Populations[cr.products_molecule_guid_ref[0]],
                                                            comp.Populations[cr.products_molecule_guid_ref[1]],
                                                            cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Annihilation)
                {
                    comp.AddBulkReaction(new Annihilation(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                            cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Dimerization)
                {
                    comp.AddBulkReaction(new Dimerization(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                            comp.Populations[cr.products_molecule_guid_ref[0]],
                                                            cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.DimerDissociation)
                {
                    comp.AddBulkReaction(new DimerDissociation(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                 comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                 cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Transformation)
                {
                    comp.AddBulkReaction(new Transformation(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                              comp.Populations[cr.products_molecule_guid_ref[0]],
                                                              cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.AutocatalyticTransformation)
                {
                    comp.AddBulkReaction(new AutocatalyticTransformation(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                           comp.Populations[cr.reactants_molecule_guid_ref[1]],
                                                                           comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                           cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedAnnihilation)
                {
                    comp.AddBulkReaction(new CatalyzedAnnihilation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                     comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                     cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedAssociation)
                {
                    comp.AddBulkReaction(new CatalyzedAssociation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                    comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                    comp.Populations[cr.reactants_molecule_guid_ref[1]],
                                                                    comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                    cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedCreation)
                {
                    comp.AddBulkReaction(new CatalyzedCreation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                 comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                 cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedDimerization)
                {
                    comp.AddBulkReaction(new CatalyzedDimerization(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                     comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                     comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                     cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedDimerDissociation)
                {
                    comp.AddBulkReaction(new CatalyzedDimerDissociation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                          comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                          comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                          cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedDissociation)
                {
                    comp.AddBulkReaction(new CatalyzedDissociation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                     comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                     comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                     comp.Populations[cr.products_molecule_guid_ref[1]],
                                                                     cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedTransformation)
                {
                    comp.AddBulkReaction(new CatalyzedTransformation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                       comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                       comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                       cr.rate_const));
                }
            }
        }

        public static void AddCellTranscriptionReactions(Cell cell, EntityRepository er, List<ConfigReaction> config_reacs)
        {
            foreach (ConfigReaction cr in config_reacs)
            {
                var r = new Transcription(cell.Genes[cr.modifiers_molecule_guid_ref[0]],
                                                                cell.Cytosol.Populations[cr.products_molecule_guid_ref[0]],
                                                                cr.rate_const);

                cell.Cytosol.AddBulkReaction(r);
            }

        }

        public static void AddCell(Cell c)
        {
            // in order to add the cell membrane to the ecs
            if (dataBasket.Environment == null)
            {
                throw new Exception("Need to create the ECS before adding cells.");
            }

            dataBasket.AddCell(c);

            // no cell rotation currently
            Transform t = new Transform(false);

            dataBasket.Environment.Comp.AddBoundary(c.Population_id, c.PlasmaMembrane.Interior.Id, c.PlasmaMembrane);

            // set translation by reference: when the cell moves then the transform gets updated automatically
            t.setTranslationByReference(c.SpatialState.X);
            dataBasket.Environment.Comp.AddBoundaryTransform(c.PlasmaMembrane.Interior.Id, t);
            //dataBasket.Environment.Comp.BoundaryTransforms.Add(c.PlasmaMembrane.Interior.Id, t);
        }

        public void RemoveCell(Cell c)
        {
            // remove the ecs boundary concs and fluxes that involve this cell
            foreach (MolecularPopulation mp in dataBasket.Environment.Comp.Populations.Values)
            {
                mp.RemoveBoundaryFluxConc(c.PlasmaMembrane.Interior.Id);
            }

            // remove the boundary reactions involving this cell
            //handled both comp and baseComp
            //delayed removal to RemoveCellBoudnaryReacitons(Cell c)
            //dataBasket.Environment.Comp.RemoveBoundaryReactions(c.PlasmaMembrane.Interior.Id);

            // remove the cell's membrane from the ecs boundary
            dataBasket.Environment.Comp.RemoveBoundary(c.PlasmaMembrane.Interior.Id);
            dataBasket.Environment.Comp.RemoveBoundaryTransform(c.PlasmaMembrane.Interior.Id);

            // remove the actual cell - moved to databasket.RemoveCell
            //dataBasket.Cells.Remove(c.Cell_id);
            //dataBasket.Populations[c.Population_id].RemoveCell(c.Cell_id);
        }

        //rmeove boundary reactions associated with this cell
        public void RemoveCellBoudnaryReacitons(Cell c)
        {
            dataBasket.Environment.Comp.RemoveBoundaryReactions(c.PlasmaMembrane.Interior.Id);
        }



        public virtual void RunForward()
        {
            if (RunStatus == RUNSTAT_RUN)
            {
                // render and sample the initial state
                if (renderCount == 0 && sampleCount == 0)
                {
                    SetFlag((byte)(SIMFLAG_RENDER | SIMFLAG_SAMPLE));
                    renderCount++;
                    sampleCount++;
                    return;
                }
                // clear all flags
                ClearFlag(SIMFLAG_ALL);

                // find the maximum allowable step (time until the next event occurs)
                double dt;

                // render to happen next
                if (renderCount * renderStep < sampleCount * sampleStep)
                {
                    SetFlag(SIMFLAG_RENDER);
                    dt = renderCount * renderStep - accumulatedTime;
                    renderCount++;
                }
                // sample to happen next
                else if (renderCount * renderStep > sampleCount * sampleStep)
                {
                    SetFlag(SIMFLAG_SAMPLE);
                    dt = sampleCount * sampleStep - accumulatedTime;
                    sampleCount++;
                }
                // both to happen simultaneously
                else
                {
                    SetFlag((byte)(SIMFLAG_RENDER | SIMFLAG_SAMPLE));
                    dt = renderCount * renderStep - accumulatedTime;
                    renderCount++;
                    sampleCount++;
                }
                // take the step
                Step(dt);

                // render and sample the final state upon completion
                if (RunStatus == RUNSTAT_FINISHED)
                {
                    // only increment the counts if they were not incremented during the same call already
                    if (CheckFlag(SIMFLAG_RENDER) == false)
                    {
                        renderCount++;
                    }
                    if (CheckFlag(SIMFLAG_SAMPLE) == false)
                    {
                        sampleCount++;
                    }
                    SetFlag((byte)(SIMFLAG_RENDER | SIMFLAG_SAMPLE));
                }
            }
        }

        public IFrameData FrameData
        {
            get { return frameData; }
        }

        public int FrameNumber
        {
            get { return renderCount; }
        }

        public abstract void Step(double dt);
        protected abstract int linearDistributionCase(int dim);

        /// <summary>
        /// constants used to set the run status
        /// </summary>
        public const byte RUNSTAT_OFF = 0,
                          RUNSTAT_READY = 1,
                          RUNSTAT_RUN = 2,
                          RUNSTAT_PAUSE = 3,
                          RUNSTAT_ABORT = 4,
                          RUNSTAT_FINISHED = 5;
        /// <summary>
        /// simulation actions
        /// </summary>
        public static byte SIMFLAG_RENDER = 1 << 0,
                           SIMFLAG_SAMPLE = 1 << 1,
                           SIMFLAG_ALL = 0xFF;

        public static DataBasket dataBasket;
        public static Protocol ProtocolHandle;

        public static CellManager cellManager;

        protected CollisionManager collisionManager;

        protected byte runStatus;
        protected byte simFlags;
        protected double accumulatedTime, duration, renderStep, sampleStep;
        protected int renderCount, sampleCount;
        protected double integratorStep;
        protected ReporterBase reporter;
        protected HDF5FileBase hdf5file;
        protected IFrameData frameData;
    }

    public class TissueSimulation : SimulationBase
    {
        // have a local pointer of the correct type for use within this class
        private TissueScenario scenarioHandle;
        private ConfigECSEnvironment envHandle;
        // burn in variables
        private double mu_max, alpha, f_max;
        private int burn_in_iter;
        public TissueSimulation()
        {
            dataBasket = new DataBasket(this);
            //integratorStep = 0.001;
            reporter = new TissueSimulationReporter(this);
            hdf5file = new TissueSimulationHDF5File(this);
            frameData = new TissueSimulationFrameData((TissueSimulationHDF5File)hdf5file);
            reset();
        }

        public override void reset()
        {
            base.reset();
            mu_max = 1.0;
            alpha = 2.0;
            burn_in_iter = 0;
        }
        protected override int linearDistributionCase(int dim)
        {
            switch (dim)
            {
                case 0:
                    return envHandle.extent_x;
                case 1:
                    return envHandle.extent_y;
                case 2:
                    return envHandle.extent_z;
                default:
                    return envHandle.extent_x;
            }
        }

        /// <summary>
        /// get the starting id based on the population size
        /// </summary>
        /// <param name="size">population size</param>
        /// <returns></returns>
        private ulong getStartingID(int size)
        {
            double x = Math.Ceiling(Math.Log(size) / Math.Log(2));
            return (ulong)Math.Pow(2, x) + 1;
        }

        public override void Load(Protocol protocol, bool completeReset, int repetition)
        {
            scenarioHandle = (TissueScenario)protocol.scenario;
            envHandle = (ConfigECSEnvironment)protocol.scenario.environment;

            // call the base
            base.Load(protocol, completeReset, repetition);

            // exit if no reset required
            if (completeReset == false)
            {
                return;
            }

            if (dataBasket.Environment != null)
            {
                dataBasket.Environment.Dispose();
            }
            //INSTANTIATE EXTRA CELLULAR MEDIUM
            dataBasket.Environment = SimulationModule.kernel.Get<ECSEnvironment>();

            // clear the databasket dictionaries
            dataBasket.Clear();

            //remove any cells in the cell dictionary
            cellManager.Clear();
            // set up the collision manager
            //MathNet.Numerics.LinearAlgebra.Vector box = new MathNet.Numerics.LinearAlgebra.Vector(3);
            DenseVector box = new DenseVector(3);

            box[0] = envHandle.extent_x;
            box[1] = envHandle.extent_y;
            box[2] = envHandle.extent_z;
            collisionManager = SimulationModule.kernel.Get<CollisionManager>(new ConstructorArgument("gridSize", box), new ConstructorArgument("gridStep", 2 * Cell.defaultRadius));

            // cells
            double[] extent = new double[] { dataBasket.Environment.Comp.Interior.Extent(0), 
                                             dataBasket.Environment.Comp.Interior.Extent(1), 
                                             dataBasket.Environment.Comp.Interior.Extent(2) };

            // ADD CELLS            
            // convenience arrays to reduce code length
            ConfigCompartment[] configComp = new ConfigCompartment[2];
            List<ConfigReaction>[] bulk_reacs = new List<ConfigReaction>[2];
            List<ConfigReaction> boundary_reacs = new List<ConfigReaction>();
            List<ConfigReaction> transcription_reacs = new List<ConfigReaction>();
            ulong idStart, idCount = 0;
            int cellNumber = 0;

            // total number of cells
            foreach (CellPopulation cp in scenarioHandle.cellpopulations)
            {
                cellNumber += cp.number;
            }
            idStart = getStartingID(cellNumber);

            // INSTANTIATE CELLS AND ADD THEIR MOLECULAR POPULATIONS
            foreach (CellPopulation cp in scenarioHandle.cellpopulations)
            {
                prepareCellInstantiation(cp, configComp, bulk_reacs, ref boundary_reacs, ref transcription_reacs);

                for (int i = 0; i < cp.number; i++)
                {
                    Cell c = instantiateCell(cp, configComp, bulk_reacs, boundary_reacs, transcription_reacs, i == 0);
                    int cell_id = cp.CellStates[i].Cell_id;
                    string lineage_id = cp.CellStates[i].Lineage_id;

                    // the safe id must be larger than the largest one in use
                    // if the cell id is legitimate (> 0), use it
                    c.Cell_id = DataBasket.GenerateSafeCellId(cell_id);

                    // assign lineage
                    if (lineage_id != "")
                    {
                        c.Lineage_id = BigInteger.Parse(lineage_id);
                    }
                    else
                    {
                        c.Lineage_id = idCount + idStart;
                        idCount++;
                    }

                    // state
                    c.SetCellState(cp.CellStates[i]);

                    // add the cell
                    AddCell(c);
                }
            }

            // ADD ECS MOLECULAR POPULATIONS
            addCompartmentMolpops(dataBasket.Environment.Comp, scenarioHandle.environment.comp, MoleculeLocation.Bulk);

            // ECS molpops boundary conditions
            if (SimulationBase.dataBasket.Environment is ECSEnvironment)
            {
                foreach (ConfigMolecularPopulation cmp in scenarioHandle.environment.comp.molpops)
                {
                    if (cmp.mp_distribution.GetType() == typeof(MolPopLinear))
                    {
                        MolPopLinear mpl = cmp.mp_distribution as MolPopLinear;

                        foreach (BoundaryCondition bc in mpl.boundaryCondition)
                        //foreach (BoundaryCondition bc in cmp.boundaryCondition)
                        {
                            int face = ((ECSEnvironment)SimulationBase.dataBasket.Environment).Sides[bc.boundary.ToString()];

                            if (!dataBasket.Environment.Comp.Populations[cmp.molecule.entity_guid].boundaryCondition.ContainsKey(face))
                            {
                                dataBasket.Environment.Comp.Populations[cmp.molecule.entity_guid].boundaryCondition.Add(face, bc.boundaryType);

                                if (bc.boundaryType == MolBoundaryType.Dirichlet)
                                {
                                    dataBasket.Environment.Comp.Populations[cmp.molecule.entity_guid].NaturalBoundaryConcs[face].Initialize("const", new double[] { bc.concVal });
                                }
                                else
                                {
                                    dataBasket.Environment.Comp.Populations[cmp.molecule.entity_guid].NaturalBoundaryFluxes[face].Initialize("const", new double[] { bc.concVal });
                                }
                            }
                        }
                    }
                }
            }

            //// ADD ECS REACTIONS
            List<ConfigReaction> reacs = new List<ConfigReaction>();
            bool[] result = null;

            reacs = protocol.GetReactions(scenarioHandle.environment.comp, false);
            AddCompartmentBulkReactions(dataBasket.Environment.Comp, protocol.entity_repository, reacs);
            reacs = protocol.GetReactions(scenarioHandle.environment.comp, true);

            prepareBoundaryReactionReport(reacs.Count, ref result);
            foreach (KeyValuePair<int, Cell> kvp in dataBasket.Cells)
            {
                AddCompartmentBoundaryReactions(dataBasket.Environment.Comp, kvp.Value.PlasmaMembrane, protocol.entity_repository, reacs, result);
            }
            // report if a reaction could not be inserted
            boundaryReactionReport(reacs, result, "the ECS");

            // general parameters
            Pair.Phi1 = protocol.sim_params.phi1;
            Pair.Phi2 = protocol.sim_params.phi2;

            // distribution governing time-to-removal for apoptotic cells
            cellManager.Phagocytosis = protocol.sim_params.Phagocytosis.Clone();

            //set up some middle layer parameters
            cellManager.InitializeNtCellManger();
            dataBasket.Environment.Comp.InitilizeBase();

        }

        // Tom's burn in algorithm
        // 1.	Zero cell forces.
        // 2.	Identify overlapping cell pairs.
        // 3.	Compute the total force F exerted on each cell due to collisions using the same methods as in the simulation.
        // 4.	Update cell positions according to:
        // a.	X += µ * F * dt ; where dt is the usual time step and µ is a parameter.
        // 5.	Compute Fmax, the largest force among all the cells.
        // 6.	Repeat steps 1-5 until Fmax < α, where α is a parameter.
        // 7.	Zero cell forces and start the normal simulation.
        // Empirically determine good values for µ and α. Try µ=0.1 and α=1 to start.

        /// <summary>
        /// find a non-overlapping starting configuration
        /// </summary>
        public override void Burn_inStep()
        {
            double mu = mu_max;

            ClearFlag(SIMFLAG_ALL);
            // render every 500 integration steps to show the progress
            if(burn_in_iter % 500 == 0)
            {
                SetFlag((byte)SIMFLAG_RENDER);
            }
            burn_in_iter++;
            // 1., zero forces
            cellManager.ResetCellForces();
            // 2. + 3., handle collisions, find and accumulate the forces per cell
            if (collisionManager != null)
            {
                collisionManager.Step(integratorStep);
            }
            // add in the boundary force; has no effect for toroidal boundary condition
            foreach (Cell c in dataBasket.Cells.Values)
            {
                c.BoundaryForce();
            }
            // 5., find the maximum force and associated mu values.
            double tmu = collisionManager.nt_collisionManager.GetBurnInMuValue(integratorStep);

            mu = tmu < mu_max ? tmu : mu_max;

            if (mu > 0)
            {
                //cell movement
                cellManager.Burn_inStep(integratorStep, mu);
            }
        }

        /// <summary>
        /// indicates whether burn in needs to run
        /// </summary>
        /// <returns>true when burn in needs to run a step</returns>
        public override bool Burn_inActive()
        {
            return f_max >= alpha || burn_in_iter == 0;
        }

        /// <summary>
        /// finish burn in
        /// </summary>
        public override void Burn_inCleanup()
        {
            // 7., zero forces
            cellManager.ResetCellForces();
        }

        public override void Step(double dt)
        {
            double t = 0, localStep;

            while (t < dt)
            {
                localStep = Math.Min(integratorStep, dt - t);

                //dataBasket.Environment.Comp.Step(localStep);
                dataBasket.Environment.Step(localStep);
                
                // zero all cell forces; needs to happen first
                //handled in the middle layer
                //cellManager.ResetCellForces();

                // handle collisions
                if (collisionManager != null)
                {
                    collisionManager.Step(localStep);
                }
                // cell movement
                cellManager.Step(localStep);
                t += localStep;
            }
            accumulatedTime += dt;

            if (accumulatedTime >= duration)
            {
                RunStatus = RUNSTAT_FINISHED;
            }
        }
    }

    public class VatReactionComplex : SimulationBase
    {
        //List of times that will be graphed on x-axis. There is only one times list no matter how many molecules
        private List<double> listTimes;
        public List<double> ListTimes
        {
            get
            {
                return listTimes;
            }
            set
            {
                listTimes = value;
            }
        }

        //This dict is used by chart view to plot the points and draw the graph
        private Dictionary<string, List<double>> dictGraphConcs;
        public Dictionary<string, List<double>> DictGraphConcs
        {
            get
            {
                return dictGraphConcs;
            }
            set
            {
                dictGraphConcs = value;
            }
        }

        public bool generateReport;

        public VatReactionComplex()
        {
            dataBasket = new DataBasket(this);
            reporter = new VatReactionComplexReporter(this);
            hdf5file = new VatReactionComplexHDF5File(this);
            reset();
            listTimes = new List<double>();
            dictGraphConcs = new Dictionary<string, List<double>>();
            generateReport = false;
        }

        public override void Load(Protocol protocol, bool completeReset, int repetition)
        {
            scenarioHandle = (VatReactionComplexScenario)protocol.scenario;
            envHandle = (ConfigPointEnvironment)protocol.scenario.environment;

            base.Load(protocol, completeReset, repetition);

            // exit if no reset required
            if (completeReset == true)
            {
                // instantiate the environment
                dataBasket.Environment = SimulationModule.kernel.Get<PointEnvironment>();

                // clear the databasket dictionaries
                dataBasket.Clear();
            }
            else
            {
                dataBasket.Environment.Comp.Populations.Clear();
                dataBasket.Environment.Comp.BulkReactions.Clear();
            }

            List<ConfigReaction> reacs = new List<ConfigReaction>();
            reacs = protocol.GetReactions(scenarioHandle.environment.comp, false);
            addCompartmentMolpops(dataBasket.Environment.Comp, scenarioHandle.environment.comp, MoleculeLocation.Bulk);
            AddCompartmentBulkReactions(dataBasket.Environment.Comp, protocol.entity_repository, reacs);
        }

        public override void Step(double dt)
        {
            double t = 0, localStep;

            while (t < dt)
            {
                localStep = Math.Min(integratorStep, dt - t);
                //dataBasket.Environment.Comp.Step(localStep);
                dataBasket.Environment.Step(localStep);
                t += localStep;
            }
            accumulatedTime += dt;
            if (accumulatedTime >= duration)
            {
                RunStatus = RUNSTAT_FINISHED;
            }
        }

        public override void RunForward()
        {
                base.RunForward();
        }

        protected override int linearDistributionCase(int dim)
        {
            return 0;
        }

        private ConfigPointEnvironment envHandle;
        private VatReactionComplexScenario scenarioHandle;
    }
}
