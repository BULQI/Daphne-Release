using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections.Specialized;
using System.Collections.ObjectModel;


using MathNet.Numerics.LinearAlgebra;

using ManifoldRing;

using Ninject;
using Ninject.Parameters;

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

        protected void clearFlag(byte flag)
        {
            simFlags &= (byte)~flag;
        }

        public bool CheckFlag(byte flag)
        {
            return (simFlags & flag) != 0;
        }

        protected void setFlag(byte flag)
        {
            simFlags |= flag;
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
                    if (population.ContainsKey(config_tde.driver_mol_guid_ref) == true)
                    {
                        TransitionDriverElement tde = new TransitionDriverElement();
                        tde.Alpha = config_tde.Alpha;
                        tde.Beta = config_tde.Beta;
                        tde.DriverPop = population[config_tde.driver_mol_guid_ref];
                        behavior.AddDriverElement(config_tde.CurrentState, config_tde.DestState, tde);
                    }
                }
            }
        }

        private void addCellMolpops(CellState cellState, ConfigCompartment[] configComp, Compartment[] simComp)
        {
            for (int comp = 0; comp < 2; comp++)
            {
                foreach (ConfigMolecularPopulation cmp in configComp[comp].molpops)
                {
                    //config_comp's distribution changed. may need to keep 
                    //it for not customized cell later(?)

                    if (cellState.cmState.molPopDict.ContainsKey(cmp.molecule.entity_guid) == false)
                    {
                        continue;
                    }

                    MolPopExplicit mp_explicit = new MolPopExplicit();

                    mp_explicit.conc = cellState.cmState.molPopDict[cmp.molecule.entity_guid];
                    cmp.mp_distribution = mp_explicit;
                }
                addCompartmentMolpops(simComp[comp], configComp[comp]);
            }
        }

        protected void instantiateCell(Protocol protocol, ConfigCell cell, int cellpop_id,
                                       ConfigCompartment[] configComp, CellState cellState,
                                       List<ConfigReaction>[] bulk_reacs,
                                       List<ConfigReaction> boundary_reacs,
                                       List<ConfigReaction> transcription_reacs)
        {
            Cell simCell = SimulationModule.kernel.Get<Cell>(new ConstructorArgument("radius", cell.CellRadius));
            Compartment[] simComp = new Compartment[2];

            simComp[0] = simCell.Cytosol;
            simComp[1] = simCell.PlasmaMembrane;

            // cell population id
            simCell.Population_id = cellpop_id;
            // state
            simCell.setState(cellState.spState);

            // modify molpop information before setting
            addCellMolpops(cellState, configComp, simComp);

            // cell genes
            foreach (ConfigGene cg in cell.genes)
            {
                //ConfigGene cg = protocol.entity_repository.genes_dict[s];
                double geneActivationLevel = cg.ActivationLevel;

                if (cellState.cgState.geneDict.ContainsKey(cg.entity_guid) == true)
                {
                    geneActivationLevel = cellState.cgState.geneDict[cg.entity_guid];
                }

                Gene gene = SimulationModule.kernel.Get<Gene>(new ConstructorArgument("name", cg.Name),
                                             new ConstructorArgument("copyNumber", cg.CopyNumber),
                                             new ConstructorArgument("actLevel", geneActivationLevel));
                simCell.AddGene(cg.entity_guid, gene);
            }

            //CELL REACTIONS
            AddCompartmentBulkReactions(simCell.Cytosol, protocol.entity_repository, bulk_reacs[0]);
            AddCompartmentBulkReactions(simCell.PlasmaMembrane, protocol.entity_repository, bulk_reacs[1]);
            // membrane; no boundary
            AddCompartmentBoundaryReactions(simCell.Cytosol, simCell.PlasmaMembrane, protocol.entity_repository, boundary_reacs);
            AddCellTranscriptionReactions(simCell, protocol.entity_repository, transcription_reacs);

            // locomotion - merged from release-dev
            if (simCell.Cytosol.Populations.ContainsKey(cell.locomotor_mol_guid_ref) == true)
            {
                MolecularPopulation driver = simCell.Cytosol.Populations[cell.locomotor_mol_guid_ref];

                simCell.Locomotor = new Locomotor(driver, cell.TransductionConstant);
                simCell.IsChemotactic = true;
            }
            else
            {
                simCell.IsChemotactic = false;
            }
            if (cell.Sigma > 0)
            {
                simCell.IsStochastic = true;
                simCell.StochLocomotor = new StochLocomotor(cell.Sigma);
            }
            else
            {
                simCell.IsStochastic = false;
            }
            if (simCell.IsChemotactic || simCell.IsStochastic)
            {
                simCell.IsMotile = true;
                simCell.DragCoefficient = cell.DragCoefficient;
            }
            else
            {
                simCell.IsMotile = false;
            }

            //TRANSITION DRIVERS
            // death behavior
            if (cell.death_driver != null)
            {
                if (cellState.cbState.deathDriveState != -1)
                {
                    simCell.DeathBehavior.CurrentState = cellState.cbState.deathDriveState;
                }
                ConfigTransitionDriver config_td = cell.death_driver;
                LoadTransitionDriverElements(config_td, simCell.Cytosol.Populations, simCell.DeathBehavior);
            }

            // Division before differentiation
            if (cell.div_scheme != null)
            {
                ConfigDiffScheme config_divScheme = cell.div_scheme;
                ConfigTransitionDriver config_td = config_divScheme.Driver;

                simCell.Divider.Initialize(config_divScheme.activationRows.Count, config_divScheme.genes.Count);
                LoadTransitionDriverElements(config_td, simCell.Cytosol.Populations, simCell.Divider.Behavior);

                // Epigenetic information
                for (int ii = 0; ii < simCell.Divider.nGenes; ii++)
                {
                    simCell.Divider.AddGene(ii, config_divScheme.genes[ii]);

                    for (int j = 0; j < simCell.Divider.nStates; j++)
                    {
                        simCell.Divider.AddActivity(j, ii, config_divScheme.activationRows[j].activations[ii]);
                        simCell.Divider.AddState(j, config_divScheme.Driver.states[j]);
                    }
                }

                // Set cell state and corresponding gene activity levels
                simCell.DividerState = simCell.Divider.CurrentState;
                simCell.SetGeneActivities(simCell.Divider);
            }

            // Differentiation
            if (cell.diff_scheme != null)
            {
                ConfigDiffScheme config_diffScheme = cell.diff_scheme;
                ConfigTransitionDriver config_td = config_diffScheme.Driver;

                simCell.Differentiator.Initialize(config_diffScheme.activationRows.Count, config_diffScheme.genes.Count);
                LoadTransitionDriverElements(config_td, simCell.Cytosol.Populations, simCell.Differentiator.Behavior);

                // Epigenetic information
                for (int ii = 0; ii < simCell.Differentiator.nGenes; ii++)
                {
                    simCell.Differentiator.AddGene(ii, config_diffScheme.genes[ii]);

                    for (int j = 0; j < simCell.Differentiator.nStates; j++)
                    {
                        simCell.Differentiator.AddActivity(j, ii, config_diffScheme.activationRows[j].activations[ii]);
                        simCell.Differentiator.AddState(j, config_diffScheme.Driver.states[j]);
                    }
                }
                // Set cell state and corresponding gene activity levels
                if (cellState.cbState.differentiationDriverState != -1)
                {
                    simCell.Differentiator.CurrentState = cellState.cbState.differentiationDriverState;
                }
                simCell.DifferentiationState = simCell.Differentiator.CurrentState;
                //saving from saved states
                if (cellState.cgState.geneDict.Count > 0)
                {
                    simCell.SetGeneActivities(cellState.cgState.geneDict);
                }
                else
                {
                    simCell.SetGeneActivities(simCell.Differentiator);
                }
            }

            // add the cell
            AddCell(simCell);
        }

        /// <summary>
        /// adds a list of molpops to the compartment
        /// </summary>
        /// <param name="simComp">the compartment</param>
        /// <param name="molpops">the list of molpops</param>
        private void addCompartmentMolpops(Compartment simComp, ObservableCollection<ConfigMolecularPopulation> molpops)
        {
            foreach (ConfigMolecularPopulation cmp in molpops)
            {
                // avoid duplicates
                if (simComp.Populations.ContainsKey(cmp.molecule.entity_guid) == true)
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
        /// </summary>
        /// <param name="simComp">the simlation compartment</param>
        /// <param name="configComp">the config compartment that describes the simulation compartment</param>
        protected void addCompartmentMolpops(Compartment simComp, ConfigCompartment configComp)
        {
            addCompartmentMolpops(simComp, configComp.molpops);
            foreach (ConfigReactionComplex rc in configComp.reaction_complexes)
            {
                addCompartmentMolpops(simComp, rc.molpops);
            }
        }

        public virtual void Load(Protocol protocol, bool completeReset)
        {
            ProtocolHandle = protocol;

            duration = protocol.scenario.time_config.duration;
            sampleStep = protocol.scenario.time_config.sampling_interval;
            renderStep = protocol.scenario.time_config.rendering_interval;
            // make sure the simulation does not start to run immediately
            RunStatus = RUNSTAT_OFF;

            // exit if no reset required
            if (completeReset == false)
            {
                return;
            }

            // executes the ninject bindings; call this after the config is initialized with valid values
            SimulationModule.kernel = new StandardKernel(new SimulationModule(protocol.scenario));

            // create a factory container: shared factories reside here; not all instances of a class
            // need their own factory
            SimulationModule.kernel.Get<FactoryContainer>();
        }

        public CollisionManager CollisionManager
        {
            get { return collisionManager; }
        }

        public ReporterBase Reporter
        {
            get { return reporter; }
        }

        public static void AddCompartmentBoundaryReactions(Compartment comp, Compartment boundary, EntityRepository er, List<ConfigReaction> config_reacs)
        {
            //foreach (string rcguid in configComp.reaction_complexes_guid_ref)
            //{
            //    ConfigReactionComplex crc = er.reaction_complexes_dict[rcguid];
            //    foreach (string rguid in crc.reactions_guid_ref)
            //    {
            //        if (reac_guids.Contains(rguid) == false)
            //        {
            //            reac_guids.Add(rguid);
            //        }
            //    }
            //}

            // NOTES: 
            // ConfigCreator.PredefinedReactionsCreator() and ProtocolToolWindow.btnSave_Click() 
            // add bulk molecules then boundary molecules to the reactant, product, and modifier lists.
            // 
            // When comp is ECS then ECS boundary reactions may not apply to some cell types. 
            // The continue statements below catch these cases.

            foreach (ConfigReaction cr in config_reacs)
            {
                if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.BoundaryAssociation)
                {
                    if (!boundary.Populations.ContainsKey(cr.reactants_molecule_guid_ref[1]) || !boundary.Populations.ContainsKey(cr.products_molecule_guid_ref[0]))
                    {
                        continue;
                    }
                    comp.AddBoundaryReaction(boundary.Interior.Id, new BoundaryAssociation(boundary.Populations[cr.reactants_molecule_guid_ref[1]],
                                                                                           comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                                           boundary.Populations[cr.products_molecule_guid_ref[0]], cr.rate_const));
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
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.BoundaryTransportTo)
                {
                    if (!boundary.Populations.ContainsKey(cr.products_molecule_guid_ref[0]))
                    {
                        continue;
                    }
                    comp.AddBoundaryReaction(boundary.Interior.Id, new BoundaryTransportTo(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                                           boundary.Populations[cr.products_molecule_guid_ref[0]], cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.BoundaryTransportFrom)
                {
                    if (!boundary.Populations.ContainsKey(cr.reactants_molecule_guid_ref[0]))
                    {
                        continue;
                    }
                    comp.AddBoundaryReaction(boundary.Interior.Id, new BoundaryTransportFrom(boundary.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                                             comp.Populations[cr.products_molecule_guid_ref[0]], cr.rate_const));
                }
            }
            return;
        }

        public static void AddCompartmentBulkReactions(Compartment comp, EntityRepository er, List<ConfigReaction> config_reacs)
        {
            foreach (ConfigReaction cr in config_reacs)
            {
                if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Association)
                {
                    comp.BulkReactions.Add(new Association(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                           comp.Populations[cr.reactants_molecule_guid_ref[1]],
                                                           comp.Populations[cr.products_molecule_guid_ref[0]],
                                                           cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Dissociation)
                {
                    comp.BulkReactions.Add(new Dissociation(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                            comp.Populations[cr.products_molecule_guid_ref[0]],
                                                            comp.Populations[cr.products_molecule_guid_ref[1]],
                                                            cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Annihilation)
                {
                    comp.BulkReactions.Add(new Annihilation(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                            cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Dimerization)
                {
                    comp.BulkReactions.Add(new Dimerization(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                            comp.Populations[cr.products_molecule_guid_ref[0]],
                                                            cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.DimerDissociation)
                {
                    comp.BulkReactions.Add(new DimerDissociation(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                 comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                 cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Transformation)
                {
                    comp.BulkReactions.Add(new Transformation(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                              comp.Populations[cr.products_molecule_guid_ref[0]],
                                                              cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.AutocatalyticTransformation)
                {
                    comp.BulkReactions.Add(new AutocatalyticTransformation(comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                           comp.Populations[cr.reactants_molecule_guid_ref[1]],
                                                                           comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                           cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedAnnihilation)
                {
                    comp.BulkReactions.Add(new CatalyzedAnnihilation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                     comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                     cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedAssociation)
                {
                    comp.BulkReactions.Add(new CatalyzedAssociation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                    comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                    comp.Populations[cr.reactants_molecule_guid_ref[1]],
                                                                    comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                    cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedCreation)
                {
                    comp.BulkReactions.Add(new CatalyzedCreation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                 comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                 cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedDimerization)
                {
                    comp.BulkReactions.Add(new CatalyzedDimerization(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                     comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                     comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                     cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedDimerDissociation)
                {
                    comp.BulkReactions.Add(new CatalyzedDimerDissociation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                          comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                          comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                          cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedDissociation)
                {
                    comp.BulkReactions.Add(new CatalyzedDissociation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
                                                                     comp.Populations[cr.reactants_molecule_guid_ref[0]],
                                                                     comp.Populations[cr.products_molecule_guid_ref[0]],
                                                                     comp.Populations[cr.products_molecule_guid_ref[1]],
                                                                     cr.rate_const));
                }
                else if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.CatalyzedTransformation)
                {
                    comp.BulkReactions.Add(new CatalyzedTransformation(comp.Populations[cr.modifiers_molecule_guid_ref[0]],
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
                cell.Cytosol.BulkReactions.Add(new Transcription(cell.Genes[cr.modifiers_molecule_guid_ref[0]],
                                                                cell.Cytosol.Populations[cr.products_molecule_guid_ref[0]],
                                                                cr.rate_const));
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

            dataBasket.Environment.Comp.Boundaries.Add(c.PlasmaMembrane.Interior.Id, c.PlasmaMembrane);
            // set translation by reference: when the cell moves then the transform gets updated automatically
            t.setTranslationByReference(c.SpatialState.X);
            dataBasket.Environment.Comp.BoundaryTransforms.Add(c.PlasmaMembrane.Interior.Id, t);
        }

        public void RemoveCell(Cell c)
        {
            // remove the boundary reactions involving this cell
            dataBasket.Environment.Comp.BoundaryReactions.Remove(c.PlasmaMembrane.Interior.Id);

            // remove the ecs boundary concs and fluxes that involve this cell
            foreach (MolecularPopulation mp in dataBasket.Environment.Comp.Populations.Values)
            {
                mp.BoundaryConcs.Remove(c.PlasmaMembrane.Interior.Id);
                mp.BoundaryFluxes.Remove(c.PlasmaMembrane.Interior.Id);
            }

            // remove the cell's membrane from the ecs boundary
            dataBasket.Environment.Comp.Boundaries.Remove(c.PlasmaMembrane.Interior.Id);
            dataBasket.Environment.Comp.BoundaryTransforms.Remove(c.PlasmaMembrane.Interior.Id);

            // remove the actual cell
            dataBasket.Cells.Remove(c.Cell_id);
        }

        public virtual void RunForward()
        {
            if (RunStatus == RUNSTAT_RUN)
            {
                // render and sample the initial state
                if (renderCount == 0 && sampleCount == 0)
                {
                    setFlag((byte)(SIMFLAG_RENDER | SIMFLAG_SAMPLE));
                    renderCount++;
                    sampleCount++;
                    return;
                }
                // clear all flags
                clearFlag(SIMFLAG_ALL);

                // find the maximum allowable step (time until the next event occurs)
                double dt;

                // render to happen next
                if (renderCount * renderStep < sampleCount * sampleStep)
                {
                    setFlag(SIMFLAG_RENDER);
                    dt = renderCount * renderStep - accumulatedTime;
                    renderCount++;
                }
                // sample to happen next
                else if (renderCount * renderStep > sampleCount * sampleStep)
                {
                    setFlag(SIMFLAG_SAMPLE);
                    dt = sampleCount * sampleStep - accumulatedTime;
                    sampleCount++;
                }
                // both to happen simultaneously
                else
                {
                    setFlag((byte)(SIMFLAG_RENDER | SIMFLAG_SAMPLE));
                    dt = renderCount * renderStep - accumulatedTime;
                    renderCount++;
                    sampleCount++;
                }
                // take the step
                Step(dt);

                // render and sample the final state upon completion
                if (RunStatus == RUNSTAT_FINISHED)
                {
                    setFlag((byte)(SIMFLAG_RENDER | SIMFLAG_SAMPLE));
                }
            }
        }

        public abstract void Step(double dt);
        protected abstract int linearDistributionCase(int dim);

        /// <summary>
        /// constants used to set the run status
        /// </summary>
        public static byte RUNSTAT_OFF = 0,
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

        protected CellManager cellManager;
        protected CollisionManager collisionManager;

        protected byte runStatus;
        protected byte simFlags;
        protected double accumulatedTime, duration, renderStep, sampleStep;
        protected int renderCount, sampleCount;
        protected double integratorStep;
        protected ReporterBase reporter;
    }

    public class TissueSimulation : SimulationBase
    {
        // have a local pointer of the correct type for use within this class
        private TissueScenario scenarioHandle;
        private ConfigECSEnvironment envHandle;

        public TissueSimulation()
        {
            dataBasket = new DataBasket(this);
            integratorStep = 0.001;
            reporter = new TissueSimulationReporter();
            reset();
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

        public override void Load(Protocol protocol, bool completeReset)
        {
            if (protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                throw new InvalidCastException();
            }
            scenarioHandle = (TissueScenario)protocol.scenario;
            envHandle = (ConfigECSEnvironment)protocol.scenario.environment;

            // call the base
            base.Load(protocol, completeReset);

            // exit if no reset required
            if (completeReset == false)
            {
                return;
            }
#if OLD_RC
            if (is_reaction_complex == true)
            {
                scenarioHandle = protocol.rc_scenario;
            }
#endif

            //INSTANTIATE EXTRA CELLULAR MEDIUM
            dataBasket.Environment = SimulationModule.kernel.Get<ECSEnvironment>();

            // clear the databasket dictionaries
            dataBasket.Clear();

            // set up the collision manager
            MathNet.Numerics.LinearAlgebra.Vector box = new MathNet.Numerics.LinearAlgebra.Vector(3);

            box[0] = envHandle.extent_x;
            box[1] = envHandle.extent_y;
            box[2] = envHandle.extent_z;
            collisionManager = SimulationModule.kernel.Get<CollisionManager>(new ConstructorArgument("gridSize", box), new ConstructorArgument("gridStep", 2 * Cell.defaultRadius));

            // cells
            double[] extent = new double[] { dataBasket.Environment.Comp.Interior.Extent(0), 
                                             dataBasket.Environment.Comp.Interior.Extent(1), 
                                             dataBasket.Environment.Comp.Interior.Extent(2) };

            // ADD CELLS            
            double[] state = new double[CellSpatialState.Dim];
            // convenience arrays to reduce code length
            ConfigCompartment[] configComp = new ConfigCompartment[2];
            List<ConfigReaction>[] bulk_reacs = new List<ConfigReaction>[2];
            List<ConfigReaction> boundary_reacs = new List<ConfigReaction>();
            List<ConfigReaction> transcription_reacs = new List<ConfigReaction>();

            // INSTANTIATE CELLS AND ADD THEIR MOLECULAR POPULATIONS
            foreach (CellPopulation cp in scenarioHandle.cellpopulations)
            {
                // if this is a new cell population, add it
                dataBasket.AddPopulation(cp.cellpopulation_id);

                configComp[0] = cp.Cell.cytosol;
                configComp[1] = cp.Cell.membrane;

                bulk_reacs[0] = protocol.GetReactions(configComp[0], false);
                bulk_reacs[1] = protocol.GetReactions(configComp[1], false);
                boundary_reacs = protocol.GetReactions(configComp[0], true);
                transcription_reacs = protocol.GetTranscriptionReactions(configComp[0]);
                
                for (int i = 0; i < cp.number; i++)
                {
                    instantiateCell(protocol, cp.Cell, cp.cellpopulation_id, configComp,
                                    cp.CellStates[i], bulk_reacs, boundary_reacs, transcription_reacs);
                }
            }

            // ADD ECS MOLECULAR POPULATIONS
            addCompartmentMolpops(dataBasket.Environment.Comp, scenarioHandle.environment.comp);

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

            reacs = protocol.GetReactions(scenarioHandle.environment.comp, false);
            AddCompartmentBulkReactions(dataBasket.Environment.Comp, protocol.entity_repository, reacs);
            reacs = protocol.GetReactions(scenarioHandle.environment.comp, true);
            foreach (KeyValuePair<int, Cell> kvp in dataBasket.Cells)
            {
                AddCompartmentBoundaryReactions(dataBasket.Environment.Comp, kvp.Value.PlasmaMembrane, protocol.entity_repository, reacs);
            }

            // general parameters
            Pair.Phi1 = protocol.sim_params.phi1;
            Pair.Phi2 = protocol.sim_params.phi2;

            cellManager.deathTimeConstant = 1.0 / protocol.sim_params.deathConstant;
            cellManager.deathOrder = (int)protocol.sim_params.deathOrder;
            cellManager.deathFactor = (cellManager.deathOrder + 1) * cellManager.deathTimeConstant;
        }

        public override void Step(double dt)
        {
            double t = 0, localStep;

            while (t < dt)
            {
                localStep = Math.Min(integratorStep, dt - t);
                dataBasket.Environment.Comp.Step(localStep);
                // zero all cell forces; needs to happen first
                cellManager.ResetCellForces();
                // handle collisions
                if (collisionManager != null)
                {
                    collisionManager.Step(localStep);
                }
                // cell force reset happens in cell manager at the end of the cell update
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
        // variables used in graphing
        public int MaxTime { get; set; }

        private double dmaxtime;
        public double dMaxTime
        {
            get
            {
                return dmaxtime;
            }
            set
            {
                if (dmaxtime != value)
                {
                    dmaxtime = value;
                    OnPropertyChanged("dMaxTime");
                }
            }
        }

        private double dinittime;
        public double dInitialTime
        {
            get
            {
                return dinittime;
            }
            set
            {
                if (dinittime != value)
                {
                    dinittime = value;
                    OnPropertyChanged("dInitialTime");
                }
            }
        }

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

        //save the original concentrations
        private Dictionary<string, double> dictOriginalConcs;

        //Initial concentrations - user can change initial concentrations of molecules
        private Dictionary<string, double> dictInitialConcs;

        //for wpf binding
        private ObservableCollection<MolConcInfo> _initConcs;
        public ObservableCollection<MolConcInfo> initConcs
        {
            get
            {
                return _initConcs;
            }
            set
            {
                _initConcs = value;
            }
        }

        //Convenience dictionary of initial concs and mol info
        public Dictionary<string, MolConcInfo> initConcsDict { get; set; }


        public VatReactionComplex()
        {
            dataBasket = new DataBasket(this);
            integratorStep = 0.001;
            reporter = new VatReactionComplexReporter();
            reset();
            listTimes = new List<double>();
            dictGraphConcs = new Dictionary<string, List<double>>();
            dictOriginalConcs = new Dictionary<string, double>();
            dictInitialConcs = new Dictionary<string, double>();
            initConcs = new ObservableCollection<MolConcInfo>();
            initConcsDict = new Dictionary<string, MolConcInfo>();

            initConcs.CollectionChanged += new NotifyCollectionChangedEventHandler(initConcs_CollectionChanged);
        }

        private void initConcs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //if (e.Action == NotifyCollectionChangedAction.Add)
            //{
            //    foreach (var nn in e.NewItems)
            //    {
            //    }
            //}            
            OnPropertyChanged("initConcs");
        }

        //Save the original concs in a temp array in case user wants to discard the changes
        public void SaveOriginalConcs()
        {
            Compartment comp = SimulationBase.dataBasket.Environment.Comp;

            if (comp == null)
            {
                return;
            }

            dictOriginalConcs.Clear();
            foreach (KeyValuePair<string, MolecularPopulation> kvp in comp.Populations)
            {
                string molguid = kvp.Key;
                double conc = kvp.Value.Conc.Value(new double[] { 0.0, 0.0, 0.0 });

                dictOriginalConcs[molguid] = conc;
            }
        }

        //Restores original concs
        //If user made changes by dragging initial concs and wants to discard the changes, do that here
        //by copying the original concs back to mol pops
        public void RestoreOriginalConcs()
        {
            foreach (KeyValuePair<string, double> kvp in dictOriginalConcs)
            {
                dictInitialConcs[kvp.Key] = kvp.Value;
            }
            OnPropertyChanged("initConcs");
        }

        public void OverwriteOriginalConcs()
        {
            Compartment comp = SimulationBase.dataBasket.Environment.Comp;
            ConfigReactionComplex crc = envHandle.comp.reaction_complexes.First();
            double[] initArray = new double[1];

            if (comp == null || crc == null)
            {
                return;
            }

            dictOriginalConcs.Clear();
            //Copy current (may have changed) initial concs to Originals dict
            foreach (KeyValuePair<string, double> kvp in dictInitialConcs)
            {
                dictOriginalConcs[kvp.Key] = kvp.Value;

                //Now overwrite the concs in Protocoluration
                ConfigMolecularPopulation mol_pop = (ConfigMolecularPopulation)(crc.molpops.First());
                MolPopHomogeneousLevel homo = (MolPopHomogeneousLevel)mol_pop.mp_distribution;

                homo.concentration = kvp.Value;
            }
        }

        //This method updates the conc of the given molecule
        public void EditConc(string moleculeKey, double conc)
        {
            dictInitialConcs[moleculeKey] = conc;
            initConcsDict[moleculeKey].conc = conc;
            OnPropertyChanged("initConcs");
        }

        //Save the initial concs. If user drags graph, use dictInitialConcs to update the initial concs
        public void SaveInitialConcs()
        {
            Compartment comp = SimulationBase.dataBasket.Environment.Comp;

            if (comp == null)
            {
                return;
            }

            dictInitialConcs.Clear();
            initConcs.Clear();
            initConcsDict.Clear();
            foreach (KeyValuePair<string, MolecularPopulation> kvp in comp.Populations)
            {
                string molguid = kvp.Key;
                //double conc = 0.0;
                double conc = comp.Populations[molguid].Conc.Value(new double[] { 0.0, 0.0, 0.0 });

                dictInitialConcs[molguid] = conc;

                MolConcInfo mci = new MolConcInfo(molguid, conc, ProtocolHandle);

                initConcs.Add(mci);
                initConcsDict.Add(molguid, mci);
            }
        }

        public override void Load(Protocol protocol, bool completeReset)
        {
            if (protocol.CheckScenarioType(Protocol.ScenarioType.VAT_REACTION_COMPLEX) == false)
            {
                throw new InvalidCastException();
            }
            scenarioHandle = (VatReactionComplexScenario)protocol.scenario;
            envHandle = (ConfigPointEnvironment)protocol.scenario.environment;

            // call the base
            base.Load(protocol, completeReset);

            // exit if no reset required
            if (completeReset == false)
            {
                return;
            }

            // instantiate the environment
            dataBasket.Environment = SimulationModule.kernel.Get<PointEnvironment>();

            // clear the databasket dictionaries
            dataBasket.Clear();

            List<ConfigReaction> reacs = new List<ConfigReaction>();

            reacs = protocol.GetReactions(scenarioHandle.environment.comp, false);
            addCompartmentMolpops(dataBasket.Environment.Comp, scenarioHandle.environment.comp);
            AddCompartmentBulkReactions(dataBasket.Environment.Comp, protocol.entity_repository, reacs);
        }

        public override void reset()
        {
            base.reset();

            double minVal = 1e7;

            dInitialTime = 1 / minVal;
            dMaxTime = 2 * dInitialTime;
            MaxTime = (int)dMaxTime;

            if (SimulationBase.dataBasket.Environment == null || SimulationBase.dataBasket.Environment.Comp == null)
            {
                return;
            }

            SaveOriginalConcs();
            SaveInitialConcs();

            Compartment comp = SimulationBase.dataBasket.Environment.Comp;
            double[] initArray = new double[1];
            ScalarField sf = SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", comp.Interior));

            foreach (KeyValuePair<string, MolecularPopulation> kvp in comp.Populations)
            {
                string molguid = kvp.Key;
                double conc = dictInitialConcs[molguid];

                initArray[0] = conc;
                sf.Initialize("const", initArray);
                comp.Populations[molguid].Conc *= 0;
                comp.Populations[molguid].Conc += sf;
            }
        }

        public override void Step(double dt)
        {
            double t = 0, localStep;

            while (t < dt)
            {
                localStep = Math.Min(integratorStep, dt - t);
                dataBasket.Environment.Comp.Step(localStep);
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
            // no rendering in the vat rc
            clearFlag(SIMFLAG_RENDER);
        }

        protected override int linearDistributionCase(int dim)
        {
            return 0;
        }

        private ConfigPointEnvironment envHandle;
        private VatReactionComplexScenario scenarioHandle;
    }
}
