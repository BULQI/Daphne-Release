using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

using MathNet.Numerics.LinearAlgebra;

using ManifoldRing;

using Ninject;
using Ninject.Parameters;

namespace Daphne
{
    public class Simulation : EntityModelBase, IDynamic
    {
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
                           SIMFLAG_ALL    = 0xFF;

        public static DataBasket dataBasket;
        public static SimConfiguration SimConfigHandle;

        private byte runStatus;
        private byte simFlags;
        private double accumulatedTime, duration, renderStep, sampleStep;
        private int renderCount, sampleCount;
        private const double integratorStep = 0.001;


        public Simulation()
        {
            cellManager = new CellManager();
            dataBasket = new DataBasket(this);
            reset();
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

        private void clearFlag(byte flag)
        {
            simFlags &= (byte)~flag;
        }

        public bool CheckFlag(byte flag)
        {
            return (simFlags & flag) != 0;
        }

        private void setFlag(byte flag)
        {
            simFlags |= flag;
        }

        public void reset()
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

        public static void AddCell(Cell c)
        {
            // in order to add the cell membrane to the ecs
            if (dataBasket.ECS == null)
            {
                throw new Exception("Need to create the ECS before adding cells.");
            }

            dataBasket.AddCell(c);

            // no cell rotation currently
            Transform t = new Transform(false);

            dataBasket.ECS.Space.Boundaries.Add(c.PlasmaMembrane.Interior.Id, c.PlasmaMembrane);
            // set translation by reference: when the cell moves then the transform gets updated automatically
            t.setTranslationByReference(c.State.X);
            dataBasket.ECS.Space.BoundaryTransforms.Add(c.PlasmaMembrane.Interior.Id, t);
        }

        public void RemoveCell(Cell c)
        {
            // remove the boundary reactions involving this cell
            dataBasket.ECS.Space.BoundaryReactions.Remove(c.PlasmaMembrane.Interior.Id);

            // remove the ecs boundary concs and fluxes that involve this cell
            foreach (MolecularPopulation mp in dataBasket.ECS.Space.Populations.Values)
            {
                mp.BoundaryConcs.Remove(c.PlasmaMembrane.Interior.Id);
                mp.BoundaryFluxes.Remove(c.PlasmaMembrane.Interior.Id);
            }

            // remove the cell's membrane from the ecs boundary
            dataBasket.ECS.Space.Boundaries.Remove(c.PlasmaMembrane.Interior.Id);
            dataBasket.ECS.Space.BoundaryTransforms.Remove(c.PlasmaMembrane.Interior.Id);

            // remove the actual cell
            dataBasket.Cells.Remove(c.Cell_id);
        }

        private void addCompartmentMolpops(Compartment simComp, ConfigCompartment configComp, SimConfiguration sc)
        {
            foreach (ConfigMolecularPopulation cmp in configComp.molpops)
            {
                if (cmp.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
                {
                    MolPopGaussian mpgg = (MolPopGaussian)cmp.mpInfo.mp_distribution;

                    // find the box associated with this gaussian
                    int box_id = -1;

                    for (int j = 0; j < sc.entity_repository.box_specifications.Count; j++)
                    {
                        if (sc.entity_repository.box_specifications[j].box_guid == mpgg.gaussgrad_gauss_spec_guid_ref)
                        {
                            box_id = j;
                            break;
                        }
                    }
                    if (box_id == -1)
                    {
                        // Should never reach here... pop up notice
                        MessageBoxResult tmp = MessageBox.Show("Problem: Box spec for that gaussian spec can't be found...");
                        return;
                    }

                    BoxSpecification box = sc.entity_repository.box_specifications[box_id];

                    double[] sigma = new double[] { box.x_scale / 2, box.y_scale / 2, box.z_scale / 2 };

                    // compute rotation matrix from box data
                    // R is vtk's rotation matrix (tranpose of traditional definition)
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
                                S[i, j] += T[i, k] * R[j,k];
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

                    simComp.AddMolecularPopulation(cmp.molecule_guid_ref, "gauss", initArray);
                }
                else if (cmp.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Homogeneous)
                {
                    MolPopHomogeneousLevel mphl = (MolPopHomogeneousLevel)cmp.mpInfo.mp_distribution;

                    simComp.AddMolecularPopulation(cmp.molecule_guid_ref, "const", new double[] { mphl.concentration });
                }
                else if (cmp.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Explicit)
                {
                	MolPopExplicit mpc = (MolPopExplicit)cmp.mpInfo.mp_distribution;
                    simComp.AddMolecularPopulation(cmp.molecule_guid_ref, "explicit", mpc.conc);
                }
                else if (cmp.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Linear)
                {
                    MolPopLinear mpl = cmp.mpInfo.mp_distribution as MolPopLinear;

                    double c1 = mpl.boundaryCondition[0].concVal;
                    double c2 = mpl.boundaryCondition[1].concVal;
                    double x2;
                    switch (mpl.dim)
                    {
                        case 0:
                            x2 = sc.scenario.environment.extent_x;
                            break;
                        case 1:
                            x2 = sc.scenario.environment.extent_y;
                            break;
                        case 2:
                            x2 = sc.scenario.environment.extent_z;
                            break;
                        default:
                            x2 = sc.scenario.environment.extent_x; 
                            break;
                    }
                         
                    simComp.AddMolecularPopulation(cmp.molecule_guid_ref, "linear", new double[] {       
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
            // ConfigCreator.PredefinedReactionsCreator() and SimConfigToolWindow.btnSave_Click() 
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
            foreach(ConfigReaction cr in config_reacs)
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
                cell.Cytosol.BulkReactions.Add(new Transcription(cell.Genes[cr.reactants_molecule_guid_ref[0]],
                                                                cell.Cytosol.Populations[cr.products_molecule_guid_ref[0]],
                                                                cr.rate_const));
            }

        }

        public void Load(SimConfiguration sc, bool completeReset, bool is_reaction_complex = false)
        {
            Scenario scenario = sc.scenario;

            SimConfigHandle = sc;

            if (is_reaction_complex == true)
            {
                scenario = sc.rc_scenario;
            }

            duration = scenario.time_config.duration;
            sampleStep = scenario.time_config.sampling_interval;
            renderStep = scenario.time_config.rendering_interval;
            // make sure the simulation does not start to run immediately
            RunStatus = RUNSTAT_OFF;

            // exit if no reset required
            if (completeReset == false)
            {
                return;
            }

            // executes the ninject bindings; call this after the config is initialized with valid values
            SimulationModule.kernel = new StandardKernel(new SimulationModule(scenario));

            // create a factory container: shared factories reside here; not all instances of a class
            // need their own factory
            SimulationModule.kernel.Get<FactoryContainer>();

            //INSTANTIATE EXTRA CELLULAR MEDIUM
            dataBasket.ECS = SimulationModule.kernel.Get<ExtraCellularSpace>();

            // clear the databasket dictionaries
            dataBasket.Clear();

            // molecules
            foreach (ConfigMolecule cm in sc.entity_repository.molecules)
            {
                Molecule mol = SimulationModule.kernel.Get<Molecule>(new ConstructorArgument("name", cm.Name),
                                                                     new ConstructorArgument("mw", cm.MolecularWeight),
                                                                     new ConstructorArgument("effRad", cm.EffectiveRadius),
                                                                     new ConstructorArgument("diffCoeff", cm.DiffusionCoefficient));

                dataBasket.Molecules.Add(cm.molecule_guid, mol);
            }

            // genes
            foreach (ConfigGene cg in sc.entity_repository.genes)
            {
                Gene gene = SimulationModule.kernel.Get<Gene>(new ConstructorArgument("name", cg.Name),
                                             new ConstructorArgument("copyNumber", cg.CopyNumber),
                                             new ConstructorArgument("actLevel", cg.ActivationLevel));


                dataBasket.Genes.Add(cg.gene_guid, gene);
            }

            // set up the collision manager
            MathNet.Numerics.LinearAlgebra.Vector box = new MathNet.Numerics.LinearAlgebra.Vector(3);
            
            box[0] = sc.scenario.environment.extent_x;
            box[1] = sc.scenario.environment.extent_y;
            box[2] = sc.scenario.environment.extent_z;
            collisionManager = SimulationModule.kernel.Get<CollisionManager>(new ConstructorArgument("gridSize", box), new ConstructorArgument("gridStep", 2 * Cell.defaultRadius));

            // cells
            double[] extent = new double[] { dataBasket.ECS.Space.Interior.Extent(0), 
                                             dataBasket.ECS.Space.Interior.Extent(1), 
                                             dataBasket.ECS.Space.Interior.Extent(2) };

            // ADD CELLS            
            double[] state = new double[SpatialState.Dim];
            // convenience arrays to save code length
            ConfigCompartment[] configComp = new ConfigCompartment[2];
            Compartment[] simComp = new Compartment[2];
            List<ConfigReaction>[] bulk_reacs = new List<ConfigReaction>[2];
            List<ConfigReaction> boundary_reacs = new List<ConfigReaction>();
            List<ConfigReaction> transcription_reacs = new List<ConfigReaction>();

            // INSTANTIATE CELLS AND ADD THEIR MOLECULAR POPULATIONS
            foreach (CellPopulation cp in scenario.cellpopulations)
            {
                // if this is a new cell population, add it
                dataBasket.AddPopulation(cp.cellpopulation_id);

                configComp[0] = sc.entity_repository.cells_dict[cp.cell_guid_ref].cytosol;
                configComp[1] = sc.entity_repository.cells_dict[cp.cell_guid_ref].membrane;

                bulk_reacs[0] = sc.GetReactions(configComp[0], false);
                bulk_reacs[1] = sc.GetReactions(configComp[1], false);
                boundary_reacs = sc.GetReactions(configComp[0], true);
                transcription_reacs = sc.GetTranscriptionReactions(configComp[0]);
                
                for (int i = 0; i < cp.number; i++)
                {
                    Cell cell = SimulationModule.kernel.Get<Cell>(new ConstructorArgument("radius", sc.entity_repository.cells_dict[cp.cell_guid_ref].CellRadius));

                    // cell population id
                    cell.Population_id = cp.cellpopulation_id;
                    cell.setState(cp.cellPopDist.CellStates[i].ConfigState);

                    simComp[0] = cell.Cytosol;
                    simComp[1] = cell.PlasmaMembrane;

                    //modify molpop information before setting
                    for (int comp = 0; comp < 2; comp++)
                    {
                        foreach (ConfigMolecularPopulation cmp in configComp[comp].molpops)
                        {
                            //config_comp's distribution changed. may need to keep 
                            //it for not customized cell later(?)

                            // if (!cp.cell_list[i].configMolPop.ContainsKey(cmp.molecule_guid_ref)) continue;
                            if (!cp.cellPopDist.CellStates[i].configMolPop.ContainsKey(cmp.molecule_guid_ref)) continue;

                            MolPopExplicit mp_explicit = new MolPopExplicit();
                            // mp_explicit.conc = cp.cell_list[i].configMolPop[cmp.molecule_guid_ref];
                            mp_explicit.conc = cp.cellPopDist.CellStates[i].configMolPop[cmp.molecule_guid_ref];
                            cmp.mpInfo.mp_distribution = mp_explicit;            
                        }
                        addCompartmentMolpops(simComp[comp], configComp[comp], sc);
                    }

                    // cell genes
                    foreach (string s in sc.entity_repository.cells_dict[cp.cell_guid_ref].genes_guid_ref)
                    {
                        ConfigGene cg = sc.entity_repository.genes_dict[s];

                        Gene gene = SimulationModule.kernel.Get<Gene>(new ConstructorArgument("name", cg.Name),
                                                     new ConstructorArgument("copyNumber", cg.CopyNumber),
                                                     new ConstructorArgument("actLevel", cg.ActivationLevel));
                        cell.AddGene(cg.gene_guid, gene);
                    }

                    //CELL REACTIONS
                    AddCompartmentBulkReactions(cell.Cytosol, sc.entity_repository, bulk_reacs[0]);
                    AddCompartmentBulkReactions(cell.PlasmaMembrane, sc.entity_repository, bulk_reacs[1]);
                    // membrane; no boundary
                    AddCompartmentBoundaryReactions(cell.Cytosol, cell.PlasmaMembrane, sc.entity_repository, boundary_reacs);
                    AddCellTranscriptionReactions(cell, sc.entity_repository, transcription_reacs);
                    
                    // locomotion
                    if (sc.entity_repository.cells_dict[cp.cell_guid_ref].locomotor_mol_guid_ref != "" && sc.entity_repository.cells_dict[cp.cell_guid_ref].locomotor_mol_guid_ref != null)
                    {
                        MolecularPopulation driver = cell.Cytosol.Populations[sc.entity_repository.cells_dict[cp.cell_guid_ref].locomotor_mol_guid_ref];

                        cell.Locomotor = new Locomotor(driver, sc.entity_repository.cells_dict[cp.cell_guid_ref].TransductionConstant);
                        cell.IsMotile = true;
                        cell.DragCoefficient = sc.entity_repository.cells_dict[cp.cell_guid_ref].DragCoefficient;
                    }
                    else
                    {
                        cell.IsMotile = false;
                    }


                    //TRANSITION DRIVERS
                    // death behavior
#if GUID_REF_BASED
                    if (sc.entity_repository.cells_dict[cp.cell_guid_ref].death_driver_guid_ref != "")
                    {
                        string death_driver_guid = sc.entity_repository.cells_dict[cp.cell_guid_ref].death_driver_guid_ref;
                        ConfigTransitionDriver config_td = sc.entity_repository.transition_drivers_dict[death_driver_guid];
                        LoadTransitionDriverElements(config_td, cell.Cytosol.Populations, cell.DeathBehavior);
                    }
#else
                    if (sc.entity_repository.cells_dict[cp.cell_guid_ref].death_driver != null)
                    {
                        ConfigTransitionDriver config_td = sc.entity_repository.cells_dict[cp.cell_guid_ref].death_driver;

                        LoadTransitionDriverElements(config_td, cell.Cytosol.Populations, cell.DeathBehavior);
                    }
#endif

                    // Differentiation
#if GUID_REF_BASED
                    if (sc.entity_repository.cells_dict[cp.cell_guid_ref].diff_scheme_guid_ref != "")
                    {
                        string diff_scheme_guid = sc.entity_repository.cells_dict[cp.cell_guid_ref].diff_scheme_guid_ref;
                        ConfigDiffScheme config_diffScheme = sc.entity_repository.diff_schemes_dict[diff_scheme_guid];
                        ConfigTransitionDriver config_td = sc.entity_repository.transition_drivers_dict[config_diffScheme.Driver.driver_guid];
                        cell.Differentiator.Initialize(config_diffScheme.activationRows.Count, config_diffScheme.genes.Count);

                        LoadTransitionDriverElements(config_td, cell.Cytosol.Populations, ((Differentiator)cell.Differentiator).DiffBehavior);

                        // Epigenetic information
                        for (int ii = 0; ii < cell.Differentiator.nGenes; ii++)
                        {
                            cell.Differentiator.AddGene(ii, config_diffScheme.genes[ii]);

                            for (int j = 0; j < cell.Differentiator.nStates; j++)
                            {
                                cell.Differentiator.AddActivity(j, ii, config_diffScheme.activationRows[j].activations[ii]);
                            }
                        }
                    }
#else
                    if (sc.entity_repository.cells_dict[cp.cell_guid_ref].diff_scheme != null)
                    {
                        ConfigDiffScheme config_diffScheme = sc.entity_repository.cells_dict[cp.cell_guid_ref].diff_scheme;
                        ConfigTransitionDriver config_td = config_diffScheme.Driver;

                        cell.Differentiator.Initialize(config_diffScheme.activationRows.Count, config_diffScheme.genes.Count);
                        LoadTransitionDriverElements(config_td, cell.Cytosol.Populations, ((Differentiator)cell.Differentiator).DiffBehavior);

                        // Epigenetic information
                        for (int ii = 0; ii < cell.Differentiator.nGenes; ii++)
                        {
                            cell.Differentiator.AddGene(ii, config_diffScheme.genes[ii]);

                            for (int j = 0; j < cell.Differentiator.nStates; j++)
                            {
                                cell.Differentiator.AddActivity(j, ii, config_diffScheme.activationRows[j].activations[ii]);
                                cell.Differentiator.AddState(j, config_diffScheme.Driver.states[j]);
                            }
                        }
                        // Set cell state and corresponding gene activity levels
                        cell.DifferentiationState = cell.Differentiator.CurrentState;
                        cell.SetGeneActivities();
                    }
#endif

                    // division behavior
#if GUID_REF_BASED
                    if (sc.entity_repository.cells_dict[cp.cell_guid_ref].div_driver_guid_ref != "")
                    {
                        string div_driver_guid = sc.entity_repository.cells_dict[cp.cell_guid_ref].div_driver_guid_ref;
                        ConfigTransitionDriver config_td = sc.entity_repository.transition_drivers_dict[div_driver_guid];
                        LoadTransitionDriverElements(config_td, cell.Cytosol.Populations, cell.DivisionBehavior);
                    }
#else
                    if (sc.entity_repository.cells_dict[cp.cell_guid_ref].div_driver != null)
                    {
                        ConfigTransitionDriver config_td = sc.entity_repository.cells_dict[cp.cell_guid_ref].div_driver;

                        LoadTransitionDriverElements(config_td, cell.Cytosol.Populations, cell.DivisionBehavior);
                    }
#endif

                    AddCell(cell);
                }
            }

            // ADD ECS MOLECULAR POPULATIONS
            addCompartmentMolpops(dataBasket.ECS.Space, scenario.environment.ecs, sc);
            // NOTE: This boolean isn't used anywhere. Do we envision a need for it?
            // Default: set diffusing
            foreach (MolecularPopulation mp in dataBasket.ECS.Space.Populations.Values)
            {
                mp.IsDiffusing = true;
            }

            // ECS molpops boundary conditions
            foreach (ConfigMolecularPopulation cmp in scenario.environment.ecs.molpops)
            {
                if (cmp.mpInfo.mp_distribution.GetType() == typeof(MolPopLinear)) {
                    MolPopLinear mpl = cmp.mpInfo.mp_distribution as MolPopLinear;
                    foreach (BoundaryCondition bc in mpl.boundaryCondition)
                    //foreach (BoundaryCondition bc in cmp.boundaryCondition)
                    {
                        int face = Simulation.dataBasket.ECS.Sides[bc.boundary.ToString()];

                        if (!dataBasket.ECS.Space.Populations[cmp.molecule_guid_ref].boundaryCondition.ContainsKey(face))
                        {
                            dataBasket.ECS.Space.Populations[cmp.molecule_guid_ref].boundaryCondition.Add(face, bc.boundaryType);

                            if (bc.boundaryType == MolBoundaryType.Dirichlet)
                            {
                                dataBasket.ECS.Space.Populations[cmp.molecule_guid_ref].NaturalBoundaryConcs[face].Initialize("const", new double[] { bc.concVal });
                            }
                            else
                            {
                                dataBasket.ECS.Space.Populations[cmp.molecule_guid_ref].NaturalBoundaryFluxes[face].Initialize("const", new double[] { bc.concVal });
                            }
                        }
                    }
                }
            }

            //// ADD ECS REACTIONS
            List<ConfigReaction> reacs = new List<ConfigReaction>();
            reacs = sc.GetReactions(scenario.environment.ecs, false);
            AddCompartmentBulkReactions(dataBasket.ECS.Space, sc.entity_repository,reacs);
            reacs = sc.GetReactions(scenario.environment.ecs, true);
            foreach (KeyValuePair<int, Cell> kvp in dataBasket.Cells)
            {
                AddCompartmentBoundaryReactions(dataBasket.ECS.Space, kvp.Value.PlasmaMembrane, sc.entity_repository, reacs);
            }

            // general parameters
            Pair.Phi1 = sc.sim_params.phi1;
            Pair.Phi2 = sc.sim_params.phi2;
        }

        public void LoadTransitionDriverElements(ConfigTransitionDriver config_td, Dictionary<string,MolecularPopulation> population, ITransitionDriver behavior)
        {
            foreach (ConfigTransitionDriverRow row in config_td.DriverElements)
            {
                foreach (ConfigTransitionDriverElement config_tde in row.elements)
                {
                    if ((config_tde.driver_mol_guid_ref != null) && (config_tde.driver_mol_guid_ref != ""))
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
        
        public void RunForward()
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

        public void Step(double dt)
        {
            double t = 0, localStep;

            while (t < dt)
            {
                localStep = Math.Min(integratorStep, dt - t);
                dataBasket.ECS.Space.Step(localStep);
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

        public CollisionManager CollisionManager
        {
            get { return collisionManager; }
        }

        private CellManager cellManager;
        private CollisionManager collisionManager;
    }
}
