using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;

using Newtonsoft.Json;

namespace Daphne
{
    public class ProtocolCreators
    {
        /// <summary>
        /// Populates the DaphneStore with the given protocol's entity repository
        /// Call this after blank scenario is created to copy entities from blank scenario
        /// But after this, to generate other scenarios, use DaphneStore
        /// </summary>
        /// <param name="daphneStore"></param>
        public static void CreateDaphneAndUserStores(Level daphneStore, Level userStore)
        {
            //Create DaphneStore
            LoadDefaultGlobalParameters(daphneStore);
            daphneStore.SerializeToFile();

            //Clone UserStore from DaphneStore
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(daphneStore.entity_repository, Newtonsoft.Json.Formatting.Indented, Settings);
            userStore.entity_repository = JsonConvert.DeserializeObject<EntityRepository>(jsonSpec, Settings);
            userStore.SerializeToFile();
        }

        public static void LoadDefaultGlobalParameters(Level store)
        {
            // genes
            PredefinedGenesCreator(store);

            // molecules
            PredefinedMoleculesCreator(store);

            // differentiation schemes
            PredefinedDiffSchemesCreator(store);

            // template reactions
            PredefinedReactionTemplatesCreator(store);

            //code to create reactions
            PredefinedReactionsCreator(store);

            //cells
            PredefinedCellsCreator(store);

            //reaction complexes
            PredefinedReactionComplexesCreator(store);
        }

        /// <summary>
        /// Use UserStore for loading entity_repository into the given protocol.
        /// This way every protocol will have the same entity guids.
        /// </summary>
        /// <param name="protocol"></param>
        public static void LoadEntitiesFromUserStore(Protocol protocol)
        {
            if (protocol == null)
                return;

            Level store = new Level("Config\\daphne_userstore.json", "Config\\temp_userstore.json");
            store = store.Deserialize();

            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(store.entity_repository, Newtonsoft.Json.Formatting.Indented, Settings);
            protocol.entity_repository = JsonConvert.DeserializeObject<EntityRepository>(jsonSpec, Settings);
            protocol.InitializeStorageClasses();
        }

        //------------------------------------------------

        private static void LoadLigandReceptorEntities(Protocol protocol)
        {
            //Load from User Store so open it
            Level userstore = new Level("Config\\daphne_userstore.json", "Config\\temp_userstore.json");
            userstore = userstore.Deserialize();

            //MOLECULES
            string[] type = new string[1] { "CXCL13" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = null;
                foreach (ConfigMolecule mol in userstore.entity_repository.molecules)
                {
                    if (mol.Name == type[i] && mol.molecule_location == MoleculeLocation.Bulk)
                    {
                        configMolecule = mol;
                        break;
                    }
                }
                if (configMolecule != null)
                {
                    ConfigMolecule newmol = configMolecule.Clone(null);
                    protocol.repositoryPush(newmol, Level.PushStatus.PUSH_CREATE_ITEM);
                }
            }

            type = new string[2] { "CXCR5|", "CXCL13:CXCR5|" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = null;
                foreach (ConfigMolecule mol in userstore.entity_repository.molecules)
                {
                    if (mol.Name == type[i] && mol.molecule_location == MoleculeLocation.Boundary)
                    {
                        configMolecule = mol;
                        break;
                    }
                }
                if (configMolecule != null)
                {
                    ConfigMolecule newmol = configMolecule.Clone(null);
                    protocol.repositoryPush(newmol, Level.PushStatus.PUSH_CREATE_ITEM);
                }
            }

            //REACTION TEMPLATES
            AddReactionTemplate(userstore, ReactionType.BoundaryAssociation, protocol);
            AddReactionTemplate(userstore, ReactionType.BoundaryDissociation, protocol);

            //CELLS
            type = new string[1] { "Leukocyte_staticReceptor" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigCell configCell = findCell(type[i], userstore);
                if (configCell != null)
                {
                    ConfigCell newcell = configCell.Clone(true);
                    protocol.repositoryPush(newcell, Level.PushStatus.PUSH_CREATE_ITEM);
                }
            }

            //EXTERNAL REACTIONS - I.E. IN EXTRACELLULAR SPACE
            type = new string[2] {"CXCL13 + CXCR5| -> CXCL13:CXCR5|",
                                  "CXCL13:CXCR5| -> CXCL13 + CXCR5|"};
            ConfigReaction reac;
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], userstore);
                if (reac != null)
                {
                    ConfigReaction newReac = reac.Clone(true);
                    protocol.repositoryPush(newReac, Level.PushStatus.PUSH_CREATE_ITEM);
                }
            }

        }

        private static void LoadDriverLocomotionEntities(Protocol protocol)
        {
            //Load from User Store so open it
            Level userstore = new Level("Config\\daphne_userstore.json", "Config\\temp_userstore.json");
            userstore = userstore.Deserialize();

            //GENES
            string[] type = new string[1] { "gApop" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigGene configEntity = null;
                foreach (ConfigGene ent in userstore.entity_repository.genes)
                {
                    if (ent.Name == type[i])
                    {
                        configEntity = ent;
                        break;
                    }
                }
                if (configEntity != null)
                {
                    ConfigGene newentity = configEntity.Clone(null);
                    protocol.repositoryPush(newentity, Level.PushStatus.PUSH_CREATE_ITEM);
                }
            }

            //MOLECULES
            type = new string[4] { "CXCL13", "A", "A*", "sApop" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = null;
                foreach (ConfigMolecule mol in userstore.entity_repository.molecules)
                {
                    if (mol.Name == type[i] && mol.molecule_location == MoleculeLocation.Bulk)
                    {
                        configMolecule = mol;
                        break;
                    }
                }
                if (configMolecule != null)
                {
                    ConfigMolecule newmol = configMolecule.Clone(null);
                    protocol.repositoryPush(newmol, Level.PushStatus.PUSH_CREATE_ITEM);
                }
            }

            type = new string[2] { "CXCR5|", "CXCL13:CXCR5|" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = null;
                foreach (ConfigMolecule mol in userstore.entity_repository.molecules)
                {
                    if (mol.Name == type[i] && mol.molecule_location == MoleculeLocation.Boundary)
                    {
                        configMolecule = mol;
                        break;
                    }
                }
                if (configMolecule != null)
                {
                    ConfigMolecule newmol = configMolecule.Clone(null);
                    protocol.repositoryPush(newmol, Level.PushStatus.PUSH_CREATE_ITEM);
                }
            }

            //CELLS
            type = new string[1] { "Leukocyte_staticReceptor_motile" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigCell configCell = findCell(type[i], userstore);
                if (configCell != null)
                {
                    ConfigCell newcell = configCell.Clone(true);
                    protocol.repositoryPush(newcell, Level.PushStatus.PUSH_CREATE_ITEM);
                }
            }

            //REACTION TEMPLATES
            AddReactionTemplate(userstore, ReactionType.BoundaryAssociation, protocol);
            AddReactionTemplate(userstore, ReactionType.BoundaryDissociation, protocol);
            AddReactionTemplate(userstore, ReactionType.Transformation, protocol);
            AddReactionTemplate(userstore, ReactionType.CatalyzedBoundaryActivation, protocol);
            AddReactionTemplate(userstore, ReactionType.Transcription, protocol);
            AddReactionTemplate(userstore, ReactionType.Annihilation, protocol);

            //REACTIONS
            type = new string[6] {  "CXCL13 + CXCR5| -> CXCL13:CXCR5|",
                                    "CXCL13:CXCR5| -> CXCL13 + CXCR5|",
                                    "A + CXCL13:CXCR5| -> A* + CXCL13:CXCR5|",
                                    "A* -> A",
                                    "gApop -> sApop + gApop",
                                    "sApop ->"};

            ConfigReaction reac;
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], userstore);
                if (reac != null)
                {
                    ConfigReaction newReac = reac.Clone(true);
                    protocol.repositoryPush(newReac, Level.PushStatus.PUSH_CREATE_ITEM);
                }
            }

        }

        //-------------------------------------------------


        /// <summary>
        /// Helper method for creating a reaction template from the given store, 
        /// to the given protocol, given a reaction type.
        /// </summary>
        /// <param name="store"></param>
        /// <param name="type"></param>
        /// <param name="protocol"></param>
        private static void AddReactionTemplate(Level store, ReactionType type, Protocol protocol)
        {
            ConfigReactionTemplate crtUser = null;
            foreach (ConfigReactionTemplate crt in store.entity_repository.reaction_templates)
            {
                if (crt.reac_type == type)
                {
                    crtUser = crt;
                    break;
                }
            }
            if (crtUser != null)
            {
                ConfigReactionTemplate crtnew = crtUser.Clone(null);
                protocol.repositoryPush(crtnew, Level.PushStatus.PUSH_CREATE_ITEM);
            }
        }

        

        
        public static void CreateLigandReceptorProtocol(Protocol protocol)
        {
            if (protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                throw new InvalidCastException();
            }

            ConfigECSEnvironment envHandle = (ConfigECSEnvironment)protocol.scenario.environment;

            // Experiment
            protocol.experiment_name = "Ligand Receptor Scenario";
            protocol.experiment_description = "CXCL13 binding to membrane-bound CXCR5. Uniform CXCL13.";
            protocol.scenario.time_config.duration = 15;
            protocol.scenario.time_config.rendering_interval = protocol.scenario.time_config.duration / 10;
            protocol.scenario.time_config.sampling_interval = protocol.scenario.time_config.duration / 100;

            envHandle.extent_x = 200;
            envHandle.extent_y = 200;
            envHandle.extent_z = 200;
            envHandle.gridstep = 10;

            // Global Paramters
            //LoadEntitiesFromUserStore(protocol);
            LoadLigandReceptorEntities(protocol);

            // ECM MOLECULES

            // Choose uniform CXCL13 distribution
            //
            // Fraction of bound receptor = [CXCL13] / ( [CXCL13] + Kd )
            // Binding Affinity Kd = kReverse/kForward 
            //
            // The FASEB Journal vol. 26 no. 12 4841-4854.  doi: 10.1096/fj.12-208876
            // Kd ~ 50.5 nM for CXCL13:CXCR5| = (50.5e-9)*(1e-18)*(6.022e23) = 0.0304 molecule/um^3
            //
            // Arbitrarily, choose CXCL13 concentration that give equilibrium receptor occupancy of 0.5
            // [CXCL13] = Kd = 0.0304 molecule/um^3
            //
            double CXCL13conc = 50.5e-9 * 1e-18 * 6.022e23;
            double[] conc = new double[1] { CXCL13conc };
            string[] type = new string[1] { "CXCL13" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = protocol.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, protocol)];
                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.ECM_MP);
                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;
                    configMolPop.mp_dist_name = "Uniform";
                    configMolPop.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    configMolPop.mp_render_blending_weight = 2.0;
                    configMolPop.mp_render_on = true;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    configMolPop.mp_distribution = hl;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.NONE;
                    ((ReportECM)configMolPop.report_mp).mean = true;

                    protocol.scenario.environment.comp.molpops.Add(configMolPop);
                }
            }

            // Add cell type
            ConfigCell configCell = findCell("Leukocyte_staticReceptor", protocol);
            //protocol.entity_repository.cells_dict.Add(configCell.entity_guid, configCell);

            // Add cell population
            // Add cell population
            CellPopulation cellPop = new CellPopulation();
            cellPop.Cell = configCell.Clone(true);
            cellPop.cellpopulation_name = configCell.CellName;
            cellPop.number = 1;
            double[] extents = new double[3] { envHandle.extent_x, envHandle.extent_y, envHandle.extent_z };
            double minDisSquared = 2 * protocol.entity_repository.cells_dict[cellPop.Cell.entity_guid].CellRadius;
            minDisSquared *= minDisSquared;
            cellPop.cellPopDist = new CellPopSpecific(extents, minDisSquared, cellPop);
            cellPop.CellStates[0] = new CellState(envHandle.extent_x / 2, envHandle.extent_y / 2, envHandle.extent_z / 2);
            cellPop.cellpopulation_color = System.Windows.Media.Color.FromScRgb(1.0f, 1.0f, 0.5f, 0.0f);
            ((TissueScenario)protocol.scenario).cellpopulations.Add(cellPop);

            // Cell reporting
            cellPop.report_xvf.position = false;
            cellPop.report_xvf.velocity = false;
            cellPop.report_xvf.force = false;
            foreach (ConfigMolecularPopulation cmp in configCell.membrane.molpops)
            {
                // Mean only
                cmp.report_mp.mp_extended = ExtendedReport.LEAN;
            }
            foreach (ConfigMolecularPopulation mpECM in protocol.scenario.environment.comp.molpops)
            {
                ReportECM reportECM = new ReportECM();
                reportECM.molpop_guid_ref = mpECM.molpop_guid;
                reportECM.mp_extended = ExtendedReport.LEAN;
                cellPop.ecm_probe.Add(reportECM);
                //cellPop.ecm_probe_dict.Add(mpECM.molpop_guid, reportECM);
            }

            protocol.reporter_file_name = "lig-rec_test";

            //EXTERNAL REACTIONS - I.E. IN EXTRACELLULAR SPACE
            type = new string[2] {"CXCL13 + CXCR5| -> CXCL13:CXCR5|",
                                  "CXCL13:CXCR5| -> CXCL13 + CXCR5|"};
            ConfigReaction reac;
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], protocol);
                if (reac != null)
                {
                    protocol.scenario.environment.comp.Reactions.Add(reac.Clone(true));
                }
            }
        }



        
        /// <summary>
        /// 
        /// </summary>
        public static void CreateDriverLocomotionProtocol(Protocol protocol)
        {
            if (protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                throw new InvalidCastException();
            }

            ConfigECSEnvironment envHandle = (ConfigECSEnvironment)protocol.scenario.environment;

            // Experiment
            protocol.experiment_name = "Cell locomotion with driver molecule.";
            protocol.experiment_description = "Cell moves in the direction of the CXCL13 linear gradient (right to left) maintained by Dirichlet BCs. Cytosol molecule A* drives locomotion.";
            envHandle.extent_x = 200;
            envHandle.extent_y = 200;
            envHandle.extent_z = 200;
            envHandle.gridstep = 10;

            protocol.scenario.time_config.duration = 30;
            protocol.scenario.time_config.rendering_interval = protocol.scenario.time_config.duration / 100;
            protocol.scenario.time_config.sampling_interval = protocol.scenario.time_config.duration / 100;

            // Global Paramters
            //LoadEntitiesFromUserStore(protocol);
            LoadDriverLocomotionEntities(protocol);

            // ECS

            // Choose uniform CXCL13 distribution
            //
            // Fraction of bound receptor = [CXCL13] / ( [CXCL13] + Kd )
            // Binding Affinity Kd = kReverse/kForward 
            //
            // The FASEB Journal vol. 26 no. 12 4841-4854.  doi: 10.1096/fj.12-208876
            // Kd ~ 50.5 nM for CXCL13:CXCR5| = (50.5e-9)*(1e-18)*(6.022e23) = 0.0304 molecule/um^3
            //
            // Arbitrarily, choose CXCL13 concentration that give equilibrium receptor occupancy of 0.5
            // [CXCL13] = Kd = 0.0304 molecule/um^3
            //
            double CXCL13conc = 50.5e-9 * 1e-18 * 6.022e23;
            double[] conc = new double[1] { CXCL13conc };
            string[] type = new string[1] { "CXCL13" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = protocol.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, protocol)];
                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.ECM_MP);
                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;

                    MolPopLinear molpoplin = new MolPopLinear();
                    molpoplin.boundary_face = BoundaryFace.X;
                    molpoplin.dim = 0;
                    molpoplin.x1 = 0;
                    molpoplin.boundaryCondition = new List<BoundaryCondition>();
                    BoundaryCondition bc = new BoundaryCondition(MolBoundaryType.Dirichlet, Boundary.left, 5 * CXCL13conc);
                    molpoplin.boundaryCondition.Add(bc);
                    bc = new BoundaryCondition(MolBoundaryType.Dirichlet, Boundary.right, 0.0);
                    molpoplin.boundaryCondition.Add(bc);
                    configMolPop.mp_distribution = molpoplin;
                    configMolPop.mp_dist_name = "Linear";

                    // graphics colors etc
                    configMolPop.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    configMolPop.mp_render_blending_weight = 2.0;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.NONE;
                    ((ReportECM)configMolPop.report_mp).mean = false;

                    protocol.scenario.environment.comp.molpops.Add(configMolPop);
                }
            }

            // Add cell
            //This code will add the cell and the predefined ConfigCell already has the molecules needed
            ConfigCell configCell = findCell("Leukocyte_staticReceptor_motile", protocol);
            //protocol.entity_repository.cells_dict.Add(configCell.entity_guid, configCell);

            // Add cell population
            CellPopulation cellPop = new CellPopulation();
            cellPop.Cell = configCell.Clone(true);
            cellPop.cellpopulation_name = configCell.CellName;
            cellPop.number = 1;
            double[] extents = new double[3] { envHandle.extent_x, envHandle.extent_y, envHandle.extent_z };
            double minDisSquared = 2 * protocol.entity_repository.cells_dict[cellPop.Cell.entity_guid].CellRadius;
            minDisSquared *= minDisSquared;
            cellPop.cellPopDist = new CellPopSpecific(extents, minDisSquared, cellPop);
            // Don't start the cell on a lattice point, until gradient interpolation method improves.
            cellPop.CellStates[0] = new CellState(envHandle.extent_x - 2 * configCell.CellRadius - envHandle.gridstep / 2,
                                                  envHandle.extent_y / 2 - envHandle.gridstep / 2,
                                                  envHandle.extent_z / 2 - envHandle.gridstep / 2);
            cellPop.cellpopulation_color = System.Windows.Media.Color.FromScRgb(1.0f, 1.0f, 0.5f, 0.0f);
            ((TissueScenario)protocol.scenario).cellpopulations.Add(cellPop);
            cellPop.report_xvf.position = true;
            cellPop.report_xvf.velocity = true;
            cellPop.report_xvf.force = true;

            foreach (ConfigMolecularPopulation cmp in configCell.membrane.molpops)
            {
                // Mean only
                cmp.report_mp.mp_extended = ExtendedReport.COMPLETE;
            }
            foreach (ConfigMolecularPopulation cmp in configCell.cytosol.molpops)
            {
                // Mean only
                cmp.report_mp.mp_extended = ExtendedReport.COMPLETE;
            }
            foreach (ConfigMolecularPopulation mpECM in protocol.scenario.environment.comp.molpops)
            {
                ReportECM reportECM = new ReportECM();
                reportECM.molpop_guid_ref = mpECM.molpop_guid;
                reportECM.mp_extended = ExtendedReport.COMPLETE;
                cellPop.ecm_probe.Add(reportECM);
                //cellPop.ecm_probe_dict.Add(mpECM.molpop_guid, reportECM);
            }

            protocol.reporter_file_name = "Loco_test";

            //EXTERNAL REACTIONS - I.E. IN EXTRACELLULAR SPACE
            type = new string[2] {"CXCL13 + CXCR5| -> CXCL13:CXCR5|",
                                  "CXCL13:CXCR5| -> CXCL13 + CXCR5|"};
            ConfigReaction reac;
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], protocol);
                if (reac != null)
                {
                    protocol.scenario.environment.comp.Reactions.Add(reac.Clone(true));
                }
            }
        }

        private static void LoadDiffusionEntities(Protocol protocol)
        {
            //Load from User Store so open it
            Level userstore = new Level("Config\\daphne_userstore.json", "Config\\temp_userstore.json");
            userstore = userstore.Deserialize();

            //MOLECULES
            string[] type = new string[1] { "CXCL13" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = null;
                foreach (ConfigMolecule mol in userstore.entity_repository.molecules)
                {
                    if (mol.Name == type[i] && mol.molecule_location == MoleculeLocation.Bulk)
                    {
                        configMolecule = mol;
                        break;
                    }
                }
                if (configMolecule != null)
                {
                    ConfigMolecule newmol = configMolecule.Clone(null);
                    protocol.repositoryPush(newmol, Level.PushStatus.PUSH_CREATE_ITEM);
                }
            }

        }


        /// <summary>
        /// New default scenario for first pass of Daphne
        /// </summary>
        public static void CreateDiffusionProtocol(Protocol protocol)
        {
            if (protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                throw new InvalidCastException();
            }

            ConfigECSEnvironment envHandle = (ConfigECSEnvironment)protocol.scenario.environment;

            // Experiment
            protocol.experiment_name = "Diffusion Scenario";
            protocol.experiment_description = "CXCL13 diffusion in the ECM. No cells. Initial distribution is Gaussian. No flux BCs.";
            protocol.scenario.time_config.duration = 2.0;
            protocol.scenario.time_config.rendering_interval = 0.2;
            protocol.scenario.time_config.sampling_interval = 0.2;

            envHandle.extent_x = 200;
            envHandle.extent_y = 200;
            envHandle.extent_z = 200;
            envHandle.gridstep = 10;

            // Global Paramters
            //LoadEntitiesFromUserStore(protocol);
            LoadDiffusionEntities(protocol);

            // Gaussian Distrtibution
            // Gaussian distribution parameters: coordinates of center, standard deviations (sigma), and peak concentrtation
            // box x,y,z_scale parameters are 2*sigma
            GaussianSpecification gaussSpec = new GaussianSpecification();
            BoxSpecification box = new BoxSpecification();
            box.x_trans = envHandle.extent_x / 2;
            box.y_trans = envHandle.extent_y / 2;
            box.z_trans = envHandle.extent_z / 2;
            box.x_scale = envHandle.extent_x / 2;
            box.y_scale = envHandle.extent_y / 4;
            box.z_scale = envHandle.extent_z / 5;
            gaussSpec.box_spec = box;
            //gg.gaussian_spec_name = "gaussian";
            gaussSpec.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            // Rotate the box by 45 degrees about the box's y-axis.
            double theta = Math.PI / 4,
                   cos = Math.Cos(theta),
                   sin = Math.Cos(theta);
            double[][] trans_matrix = new double[4][];
            trans_matrix[0] = new double[4] { box.x_scale * cos, 0, box.z_scale * sin, box.x_trans };
            trans_matrix[1] = new double[4] { 0, box.y_scale, 0, box.y_trans };
            trans_matrix[2] = new double[4] { -box.x_scale * sin, 0, box.z_scale * cos, box.z_trans };
            trans_matrix[3] = new double[4] { 0, 0, 0, 1 };
            box.SetMatrix(trans_matrix);

            var query =
                from mol in protocol.entity_repository.molecules
                where mol.Name == "CXCL13"
                select mol;

            ConfigMolecularPopulation configMolPop = null;
            // ecs
            foreach (ConfigMolecule cm in query)
            {
                configMolPop = new ConfigMolecularPopulation(ReportType.ECM_MP);
                configMolPop.molecule = cm.Clone(null);
                configMolPop.Name = cm.Name;
                configMolPop.mp_dist_name = "Gaussian";
                configMolPop.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                configMolPop.mp_render_blending_weight = 2.0;

                MolPopGaussian molPopGaussian = new MolPopGaussian();
                molPopGaussian.peak_concentration = 10;
                molPopGaussian.gauss_spec = gaussSpec;
                configMolPop.mp_distribution = molPopGaussian;

                // Reporting
                configMolPop.report_mp.mp_extended = ExtendedReport.COMPLETE;
                ReportECM r = configMolPop.report_mp as ReportECM;
                r.mean = true;

                protocol.reporter_file_name = "Diffusion_test";

                protocol.scenario.environment.comp.molpops.Add(configMolPop);
            }
        }

        /// <summary>
        /// New default scenario for first pass of Daphne
        /// </summary>
        public static void CreateBlankProtocol(Protocol protocol)
        {
            if (protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                throw new InvalidCastException();
            }

            // Experiment
            protocol.experiment_name = "Blank Tissue Simulation Scenario";
            protocol.experiment_description = "Libraries only.";
            protocol.scenario.time_config.duration = 100;
            protocol.scenario.time_config.rendering_interval = 1.0;
            protocol.scenario.time_config.sampling_interval = 100;

            // Global Paramters
            //LoadEntitiesFromUserStore(protocol);

        }

        public static void CreateBlankVatReactionComplexProtocol(Protocol protocol)
        {
            if (protocol.CheckScenarioType(Protocol.ScenarioType.VAT_REACTION_COMPLEX) == false)
            {
                throw new InvalidCastException();
            }

            ConfigPointEnvironment envHandle = (ConfigPointEnvironment)protocol.scenario.environment;

            // Experiment
            protocol.experiment_name = "Blank Vat Reaction Complex Scenario";
            protocol.experiment_description = "...";
            protocol.scenario.time_config.duration = 2.0;
            protocol.scenario.time_config.rendering_interval = 0.2;
            protocol.scenario.time_config.sampling_interval = 0.2;
        }

        private static void LoadVatReactionComplexEntities(Protocol protocol)
        {
            //Load from User Store so open it
            Level userstore = new Level("Config\\daphne_userstore.json", "Config\\temp_userstore.json");
            userstore = userstore.Deserialize();

            // RC
            string[] type = new string[1] { "Ligand/Receptor" };

            for (int i = 0; i < type.Length; i++)
            {
                foreach (ConfigReactionComplex ent in userstore.entity_repository.reaction_complexes)
                {
                    if (ent.Name == type[i])
                    {
                        protocol.repositoryPush(ent.Clone(true), Level.PushStatus.PUSH_CREATE_ITEM);
                        break;
                    }
                }
            }
        }

        public static void CreateVatReactionComplexProtocol(Protocol protocol)
        {
            if (protocol.CheckScenarioType(Protocol.ScenarioType.VAT_REACTION_COMPLEX) == false)
            {
                throw new InvalidCastException();
            }

            ConfigPointEnvironment envHandle = (ConfigPointEnvironment)protocol.scenario.environment;

            // Experiment
            protocol.experiment_name = "Vat Reaction Complex Scenario";
            protocol.experiment_description = "...";
            protocol.scenario.time_config.duration = 2.0;
            protocol.scenario.time_config.rendering_interval = 0.2;
            protocol.scenario.time_config.sampling_interval = 0.2;

            LoadVatReactionComplexEntities(protocol);

            // add the reaction complex
            string[] type = new string[1] { "Ligand/Receptor" };

            for (int i = 0; i < type.Length; i++)
            {
                foreach (ConfigReactionComplex ent in protocol.entity_repository.reaction_complexes)
                {
                    if (ent.Name == type[i])
                    {
                        envHandle.comp.reaction_complexes.Add(ent.Clone(true));
                        break;
                    }
                }
            }
        }

        private static void PredefinedCellsCreator(Level store)
        {
            // Generic death transition driver - move to PredefinedTransitionDriversCreator() ?
            // Cell cytoplasm must contain sApop molecular population
            ConfigTransitionDriver config_td = new ConfigTransitionDriver();
            config_td.Name = "generic apoptosis";
            string[] stateName = new string[] { "alive", "dead" };
            string[,] signal = new string[,] { { "", "sApop" }, { "", "" } };
            double[,] alpha = new double[,] { { 0, 0 }, { 0, 0 } };
            double[,] beta = new double[,] { { 0, 0.002}, { 0, 0 } };
            LoadConfigTransitionDriverElements(config_td, signal, alpha, beta, stateName, store);
            config_td.CurrentState = 0;
            config_td.StateName = config_td.states[config_td.CurrentState];
            store.entity_repository.transition_drivers.Add(config_td);
            store.entity_repository.transition_drivers_dict.Add(config_td.entity_guid, config_td);

            // generic division driver
            config_td = new ConfigTransitionDriver();
            config_td.Name = "generic division";
            stateName = new string[] { "quiescent", "mitotic" };
            signal = new string[,] { { "", "sDiv" }, { "", "" } };
            alpha = new double[,] { { 0, 0 }, { 0, 0 } };
            beta = new double[,] { { 0, 0.002 }, { 0, 0 } };
            LoadConfigTransitionDriverElements(config_td, signal, alpha, beta, stateName, store);
            config_td.CurrentState = 0;
            config_td.StateName = config_td.states[config_td.CurrentState];
            store.entity_repository.transition_drivers.Add(config_td);
            store.entity_repository.transition_drivers_dict.Add(config_td.entity_guid, config_td);

            ConfigCell gc;
            double[] conc;
            //ReactionType[] reacType;
            string[] type;
            ConfigMolecularPopulation gmp;
            ConfigMolecule cm;
            ConfigReaction reac;

            // concentration of CXCR5 on B cells
            // Barroso2012:  13,851 per cell = 44 um^{-2} for cells with 5 um radius
            double cxcr5Conc_5umRadius = 44;

            //
            // Leukocyte_staticReceptor
            // Leukocyte with fixed number of receptor molecules and no locomotion
            //
            gc = new ConfigCell();
            gc.CellName = "Leukocyte_staticReceptor";
            gc.CellRadius = 5.0;

            //MOLECULES IN MEMBRANE
            conc = new double[2] { cxcr5Conc_5umRadius, 0 };
            type = new string[2] { "CXCR5|", "CXCL13:CXCR5|" };
            float[,] colors = new float[2,4]{ {0.3f, 0.9f, 0.1f, 0.1f},
                                              {0.3f, 0.2f, 0.9f, 0.1f} };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    gmp.mp_dist_name = "Uniform";
                    //gmp.mp_color = System.Windows.Media.Color.FromScRgb(colors[i,0], colors[i,1], colors[i,2], colors[i,3]);
                    gmp.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.membrane.molpops.Add(gmp);
                }
            }

            //MOLECULES IN Cytosol
#if LEUKOCYTE_HAS_DEATH // remove to reenable death as a default behavior for leukocytes
            conc = new double[1] { 0 };
            type = new string[1] { "sApop" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = sc.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, sc)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule_guid_ref = cm.molecule_guid;
                    gmp.Name = cm.Name;

                    gmp.mp_dist_name = "Uniform";
                    gmp.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.cytosol.molpops.Add(gmp);
                }
            }

            // Add genes
            type = new string[1] { "gApop" };
            for (int i = 0; i < type.Length; i++)
            {
                gc.genes.Add(findGene(type[i], sc));
            }

            //
            // ToDo: Add gene transcription for sApop

            // Add death driver
            // Cell cytoplasm must contain sApop molecular population
            gc.death_driver_guid = findTransitionDriverGuid("generic apoptosis", sc);
#endif

            gc.DragCoefficient = 1.0;
            store.entity_repository.cells.Add(gc);

            //////////////////////////////////////////////
            // Leukocyte_staticReceptor_motile
            // Leukocyte with fixed number of receptor molecules with locomotion driven by A*.
            //
            gc = new ConfigCell();
            gc.CellName = "Leukocyte_staticReceptor_motile";
            gc.CellRadius = 5.0;

            //MOLECULES IN MEMBRANE
            conc = new double[2] { cxcr5Conc_5umRadius, 0 };
            type = new string[2] { "CXCR5|", "CXCL13:CXCR5|" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    gmp.mp_dist_name = "Uniform";
                    gmp.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.membrane.molpops.Add(gmp);
                }
            }

            //MOLECULES IN Cytosol
            conc = new double[3] { 250, 0, 0 };
            type = new string[3] { "A", "A*", "sApop" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    gmp.mp_dist_name = "Uniform";
                    gmp.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.cytosol.molpops.Add(gmp);
                }
            }
            gc.locomotor_mol_guid_ref = findMoleculeGuid("A*", MoleculeLocation.Bulk, store);

            // Add genes
            type = new string[1] { "gApop" };
            for (int i = 0; i < type.Length; i++)
            {
                gc.genes.Add(findGene(type[i], store));
            }

            // Reactions in Cytosol
            type = new string[3] {"A + CXCL13:CXCR5| -> A* + CXCL13:CXCR5|",
                                          "A* -> A", "gApop -> sApop + gApop" };
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], store);
                if (reac != null)
                {
                    gc.cytosol.Reactions.Add(reac.Clone(true));
                }
            }

            gc.DragCoefficient = 1.0;
            gc.TransductionConstant = 100;

            store.entity_repository.cells.Add(gc);

            /////////////////////////////////////////////
            // Leukocyte_dynamicReceptor_motile
            // Leukocyte with dynamic concentration of receptor molecules with locomotion driven by A*.
            //
            gc = new ConfigCell();
            gc.CellName = "Leukocyte_dynamicReceptor_motile";
            gc.CellRadius = 5.0;

            //MOLECULES IN MEMBRANE
            conc = new double[2] { cxcr5Conc_5umRadius, 0 };
            type = new string[2] { "CXCR5|", "CXCL13:CXCR5|" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    gmp.mp_dist_name = "Uniform";
                    gmp.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.membrane.molpops.Add(gmp);
                }
            }

            //MOLECULES IN Cytosol
            conc = new double[5] { 250, 0, 0, 0, 0 };
            type = new string[5] { "A", "A*", "CXCR5", "CXCL13:CXCR5", "sApop" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    gmp.mp_dist_name = "Uniform";
                    gmp.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.cytosol.molpops.Add(gmp);
                }
            }
            gc.locomotor_mol_guid_ref = findMoleculeGuid("A*", MoleculeLocation.Bulk, store);

            // Add genes
            type = new string[2] { "gApop", "gCXCR5" };
            for (int i = 0; i < type.Length; i++)
            {
                gc.genes.Add(findGene(type[i], store));
            }

            // Reactions in Cytosol
            type = new string[9] {"A + CXCL13:CXCR5| -> A* + CXCL13:CXCR5|",
                                  "A* -> A",
                                  "gCXCR5 -> CXCR5 + gCXCR5",
                                  "CXCR5 -> CXCR5|",
                                  "CXCR5| -> CXCR5",
                                  "CXCR5 ->",
                                  "CXCL13:CXCR5| -> CXCL13:CXCR5", 
                                  "CXCL13:CXCR5 ->",
                                  "gApop -> sApop + gApop"
                                };
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], store);
                if (reac != null)
                {
                    gc.cytosol.Reactions.Add(reac.Clone(true));
                }
            }

            gc.DragCoefficient = 1.0;
            gc.TransductionConstant = 1e2;

            store.entity_repository.cells.Add(gc);

            ///////////////////////////////////
            // B cell
            // B cell with differentiation states: 
            //      Naive, Activated, Short-lived plasmacyte, Long-lived plasmacyte, Centroblast, Centrocyte, Memory

            gc = new ConfigCell();
            gc.CellName = "B";
            gc.CellRadius = 5.0;

            //MOLECULES IN MEMBRANE
            conc = new double[4] { cxcr5Conc_5umRadius, 0, 0, 0};
            type = new string[4] { "CXCR5|", "CXCL13:CXCR5|", "CXCR4|", "CXCL12:CXCR4|" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    gmp.mp_dist_name = "Uniform";
                    gmp.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.membrane.molpops.Add(gmp);
                }
            }

            //MOLECULES IN Cytosol
            conc = new double[19] { 250,  0,     0,       0,       0,       0,        0,      0,       0,     0,        0,
                                    0,          0,      0,    0,    0,      0,      0,      0 };
            type = new string[19] { "A", "A*", "sDif1", "sDif2", "sDif3", "sDif4", "sDif5", "sDif6", "sDif7", "sApop", "sDiv",
                                    "CXCR4", "CXCR5", "IgH", "IgL", "IgS", "AID", "BL1", "MHCII" };

            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    gmp.mp_dist_name = "Uniform";
                    gmp.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.cytosol.molpops.Add(gmp);
                }
            }
            gc.locomotor_mol_guid_ref = findMoleculeGuid("A*", MoleculeLocation.Bulk, store);

            // Genes
            type = new string[17] { "gCXCR4", "gCXCR5", "gIgH", "gIgL", "gIgS", "gAID", "gBL1", "gMHCII", "gApop",
                                    "gDif1", "gDif2", "gDif3", "gDif4", "gDif5", "gDif6", "gDif7", "gDiv" };
            for (int i = 0; i < type.Length; i++)
            {
                gc.genes.Add(findGene(type[i], store));
            }

            // Reactions in Cytosol
            type = new string[34] {"A + CXCL13:CXCR5| -> A* + CXCL13:CXCR5|", "A* -> A",
                                    "gCXCR4 -> CXCR4 + gCXCR4", "gCXCR5 -> CXCR5 + gCXCR5", 
                                    "gIgH -> IgH + gIgH", "gIgL -> IgL + gIgL", "gIgS -> IgS + gIgS", 
                                    "gAID -> AID + gAID", "gBL1 -> BL1 + gBL1", "gMHCII -> MHCII + gMHCII", "gApop -> sApop + gApop",
                                    "gDif1 -> sDif1 + gDif1", "gDif2 -> sDif2 + gDif2", "gDif3 -> sDif3 + gDif3", "gDif4 -> sDif4 + gDif4",
                                    "gDif5 -> sDif5 + gDif5", "gDif6 -> sDif6 + gDif6", "gDif7 -> sDif7 + gDif7", "gDiv -> sDiv + gDiv", "sApop ->",
                                    "sDif1 ->", "sDif2 ->", "sDif3 ->", "sDif4 ->", "sDif5 ->", "sDif6 ->", "sDif7 ->",
                                    "IgH ->", "IgL ->", "IgS ->", "AID ->", "BL1 ->", "MHCII ->", "sDiv ->"
                                  };
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], store);
                if (reac != null)
                {
                    gc.cytosol.Reactions.Add(reac.Clone(true));
                }
            }

            gc.DragCoefficient = 1.0;
            gc.TransductionConstant = 100;

            // Add differentiatior
            // Assumes all genes and signal molecules are present
            string diff_scheme_guid = findDiffSchemeGuid("B cell 7 state", store);

            if (store.entity_repository.diff_schemes_dict.ContainsKey(diff_scheme_guid) == true)
            {
                gc.diff_scheme = store.entity_repository.diff_schemes_dict[diff_scheme_guid].Clone(true);
            }

            // Add apoptosis
            string death_driver_guid = findTransitionDriverGuid("generic apoptosis", store);

            if (store.entity_repository.transition_drivers_dict.ContainsKey(death_driver_guid) == true)
            {
                gc.death_driver = store.entity_repository.transition_drivers_dict[death_driver_guid].Clone(true);
            }

            // add division

            store.entity_repository.cells.Add(gc);

            ///////////////////////////////////
            // GC B cell
            // B cell with differentiation states: 
            //      Centroblast, Centrocyte, Memory, and Long-lived plasmacyte


            gc = new ConfigCell();
            gc.CellName = "GC B";
            gc.CellRadius = 5.0;

            //MOLECULES IN MEMBRANE
            conc = new double[4] { cxcr5Conc_5umRadius, 0, 0, 0 };
            type = new string[4] { "CXCR5|", "CXCL13:CXCR5|", "CXCR4|", "CXCL12:CXCR4|" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    gmp.mp_dist_name = "Uniform";
                    gmp.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.membrane.molpops.Add(gmp);
                }
            }

            //MOLECULES IN Cytosol
            conc = new double[16] { 250,  0,     0,       0,       0,       0,       0,       0,
                                    0,          0,      0,    0,    0,      0,      0,      0 };
            type = new string[16] { "A", "A*", "sDif4", "sDif5", "sDif6", "sDif7", "sApop", "sDiv",
                                    "CXCR4", "CXCR5", "IgH", "IgL", "IgS", "AID", "BL1", "MHCII" };

            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    gmp.mp_dist_name = "Uniform";
                    gmp.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.cytosol.molpops.Add(gmp);
                }
            }
            gc.locomotor_mol_guid_ref = findMoleculeGuid("A*", MoleculeLocation.Bulk, store);

            // Genes
            type = new string[14] { "gCXCR4", "gCXCR5", "gIgH", "gIgL", "gIgS", "gAID", "gBL1", "gMHCII", "gApop",
                                    "gDif4", "gDif5", "gDif6", "gDif7", "gDiv" };
            for (int i = 0; i < type.Length; i++)
            {
                gc.genes.Add(findGene(type[i], store));
            }

            // Reactions in Cytosol
            type = new string[28] {"A + CXCL13:CXCR5| -> A* + CXCL13:CXCR5|", "A* -> A",
                                    "gCXCR4 -> CXCR4 + gCXCR4", "gCXCR5 -> CXCR5 + gCXCR5", 
                                    "gIgH -> IgH + gIgH", "gIgL -> IgL + gIgL", "gIgS -> IgS + gIgS", 
                                    "gAID -> AID + gAID", "gBL1 -> BL1 + gBL1", "gMHCII -> MHCII + gMHCII", "gApop -> sApop + gApop",
                                    "gDif4 -> sDif4 + gDif4", "gDif5 -> sDif5 + gDif5", "gDif6 -> sDif6 + gDif6", "gDif7 -> sDif7 + gDif7", "gDiv -> sDiv + gDiv",
                                    "sApop ->", "sDif4 ->", "sDif5 ->", "sDif6 ->", "sDif7 ->", "sDiv ->",
                                    "IgH ->", "IgL ->", "IgS ->", "AID ->", "BL1 ->", "MHCII ->"
                                  };
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], store);
                if (reac != null)
                {
                    gc.cytosol.Reactions.Add(reac.Clone(true));
                }
            }

            gc.DragCoefficient = 1.0;
            gc.TransductionConstant = 100;

            // Add differentiator
            // Assumes all genes and signal molecules are present
            diff_scheme_guid = findDiffSchemeGuid("GC B cell", store);
            if (store.entity_repository.diff_schemes_dict.ContainsKey(diff_scheme_guid) == true)
            {
                gc.diff_scheme = store.entity_repository.diff_schemes_dict[diff_scheme_guid].Clone(true);
            }

            // Add apoptosis
            death_driver_guid = findTransitionDriverGuid("generic apoptosis", store);
            if (store.entity_repository.transition_drivers_dict.ContainsKey(death_driver_guid) == true)
            {
                gc.death_driver = store.entity_repository.transition_drivers_dict[death_driver_guid].Clone(true);
            }

            // add division
            //div_driver changed to div_scheme now, need to add div_scheme for this part?

            store.entity_repository.cells.Add(gc);

            ////////////////////////
            // Stromal CXCL12-secreting
            //
            gc = new ConfigCell();
            gc.CellName = "Stromal CXCL12-secreting";
            gc.CellRadius = 5.0;

            //MOLECULES IN MEMBRANE
            conc = new double[1] { 0 };
            type = new string[1] { "CXCL12|" };
            //colors = new float[2, 4]{ {0.3f, 0.9f, 0.1f, 0.1f},
            //                                  {0.3f, 0.2f, 0.9f, 0.1f} };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    gmp.mp_dist_name = "Uniform";
                    //gmp.mp_color = System.Windows.Media.Color.FromScRgb(colors[i,0], colors[i,1], colors[i,2], colors[i,3]);
                    gmp.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.membrane.molpops.Add(gmp);
                }
            }

            //MOLECULES IN Cytosol
            conc = new double[1] { 0 };
            type = new string[1] { "CXCL12" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    gmp.mp_dist_name = "Uniform";
                    gmp.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.cytosol.molpops.Add(gmp);
                }
            }

            // Add genes
            type = new string[1] { "gCXCL12" };
            for (int i = 0; i < type.Length; i++)
            {
                gc.genes.Add(findGene(type[i], store));
            }

            // Reactions in Cytosol
            type = new string[2] {"gCXCL12 -> CXCL12 + gCXCL12", "CXCL12 -> CXCL12|" };
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], store);
                if (reac != null)
                {
                    gc.cytosol.Reactions.Add(reac.Clone(true));
                }
            }

            gc.DragCoefficient = 1.0;
            gc.TransductionConstant = 0.0;
            store.entity_repository.cells.Add(gc);

            ////////////////////////////////
            // Stromal CXCL13-secreting
            //
            gc = new ConfigCell();
            gc.CellName = "Stromal CXCL13-secreting";
            gc.CellRadius = 5.0;

            //MOLECULES IN MEMBRANE
            conc = new double[1] { 0 };
            type = new string[1] { "CXCL13|" };
            colors = new float[2, 4]{ {0.3f, 0.9f, 0.1f, 0.1f},
                                              {0.3f, 0.2f, 0.9f, 0.1f} };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    gmp.mp_dist_name = "Uniform";
                    //gmp.mp_color = System.Windows.Media.Color.FromScRgb(colors[i,0], colors[i,1], colors[i,2], colors[i,3]);
                    gmp.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.membrane.molpops.Add(gmp);
                }
            }

            //MOLECULES IN Cytosol
            conc = new double[1] { 0 };
            type = new string[1] { "CXCL13" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    gmp.mp_dist_name = "Uniform";
                    gmp.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.cytosol.molpops.Add(gmp);
                }
            }

            // Add genes
            type = new string[1] { "gCXCL13" };
            for (int i = 0; i < type.Length; i++)
            {
                gc.genes.Add(findGene(type[i], store));
            }

            // Reactions in Cytosol
            type = new string[2] { "gCXCL13 -> CXCL13 + gCXCL13", "CXCL13 -> CXCL13|" };
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], store);
                if (reac != null)
                {
                    gc.cytosol.Reactions.Add(reac.Clone(true));
                }
            } 
            
            gc.DragCoefficient = 1.0;
            gc.TransductionConstant = 0.0;
            store.entity_repository.cells.Add(gc);

        }

        private static void PredefinedDiffSchemesCreator(Level store)
        {
            // These can be reused to create other differentiation schemes
            ConfigTransitionDriver driver;
            ConfigDiffScheme diffScheme;
            ConfigActivationRow actRow;
            string[] stateNames, geneNames;
            double[,] activations, alpha, beta;
            string[,] signal;

            ////////////////////////////
            // B cell differentiatior 
            ////////////////////////////

            stateNames = new string[] { "naive", "activated", "short-lived plasmacyte", "centroblast", "centrocyte", "long-lived plasmacyte", "memory" };
            geneNames = new string[]             { "gCXCR4", "gCXCR5", "gIgH", "gIgL", "gIgS", "gAID", "gBL1", "gMHCII" };
            activations = new double[,]          { { 0,        1,       1,      1,      0,       0,      0,      0 },  // naive
                                                   { 0,        1,       1,      1,      0,       0,      1,      1 },  // activated
                                                   { 0,        1,       1,      1,      1,       0,      0,      0 },  // slplasmacyte
                                                   { 1,        0,       0,      0,      0,       1,      0,      0 },  // centroblast
                                                   { 0,        1,       1,      1,      0,       0,      1,      1 },  // centrocyte
                                                   { 0,        0,       1,      1,      1,       0,      0,      0 },  // llplasmacyte
                                                   { 0,        0,       1,      1,      0,       0,      1,      1 },  // memory
                                                };
            //                                     naive    act       slp      cb       cc    llp     memory
            signal = new string[,]          {     { "",   "sDif1",    "",     "",      "",    "",       ""      },  // naive
                                                   { "",     "",     "sDif2", "sDif3",  "",    "",       ""      },  // activated
                                                   { "",     "",       "",      "",     "",    "",       ""      },  // slplasmacyte
                                                   { "",     "",       "",      "",   "sDif4", "",       ""      },  // centroblast
                                                   { "",     "",       "",   "sDif5",   "",   "sDif6",  "sDif7"  },  // centrocyte
                                                   { "",     "",       "",      "",     "",    "",       ""     },  // llplasmacyte
                                                   { "",     "",       "",      "",     "",    "",       ""     },  // memory
                                                };
            //  no spontaneous transitions        naive    act       slp      cb       cc    llp     memory
            alpha = new double[,]          {       { 0,     0,        0,       0,      0,    0,       0   },  // naive
                                                   { 0,     0,        0,       0,      0,    0,       0   },  // activated
                                                   { 0,     0,        0,       0,      0,    0,       0   },  // slplasmacyte
                                                   { 0,     0,        0,       0,      0,    0,       0   },  // centroblast
                                                   { 0,     0,        0,       0,      0,    0,       0   },  // centrocyte
                                                   { 0,     0,        0,       0,      0,    0,       0   },  // llplasmacyte
                                                   { 0,     0,        0,       0,      0,    0,       0   },  // memory
                                                };
            //  need better values here             naive    act       slp      cb       cc    llp     memory
            beta = new double[,]           {       { 0,     1,        0,       0,      0,    0,       0   },  // naive
                                                   { 0,     0,        1,       1,      0,    0,       0   },  // activated
                                                   { 0,     0,        0,       0,      0,    0,       0   },  // slplasmacyte
                                                   { 0,     0,        0,       0,      1,    0,       0   },  // centroblast
                                                   { 0,     0,        0,       1,      0,    1,       1   },  // centrocyte
                                                   { 0,     0,        0,       0,      0,    0,       0   },  // llplasmacyte
                                                   { 0,     0,        0,       0,      0,    0,       0   },  // memory
                                                };

            diffScheme = new ConfigDiffScheme();
            diffScheme.Name = "B cell 7 state";
            driver = new ConfigTransitionDriver();
            driver.Name = "B cell 7 state driver";
            driver.CurrentState = 0;
            driver.StateName = stateNames[driver.CurrentState];

            // Attach transition driver to differentiation scheme
            diffScheme.Driver = driver;

            // Add genes
            diffScheme.genes = new ObservableCollection<string>();
            for (int j = 0; j < activations.GetLength(1); j++)
            {
                diffScheme.genes.Add(findGeneGuid(geneNames[j], store));
            }

            // Add epigenetic map of genes and activations
            diffScheme.activationRows = new ObservableCollection<ConfigActivationRow>();
            for (int i = 0; i < activations.GetLength(0); i++)
            {
                actRow = new ConfigActivationRow();
                for (int j = 0; j < activations.GetLength(1); j++)
                {
                    actRow.activations.Add(activations[i, j]);
                }
                diffScheme.activationRows.Add(actRow);
            }

            // Add DriverElements to TransitionDriver
            LoadConfigTransitionDriverElements(driver, signal, alpha, beta, stateNames, store);

            // Add to Entity Repository
            store.entity_repository.transition_drivers.Add(driver);
            store.entity_repository.transition_drivers_dict.Add(driver.entity_guid, driver);
            store.entity_repository.diff_schemes.Add(diffScheme);
            store.entity_repository.diff_schemes_dict.Add(diffScheme.entity_guid, diffScheme);

            ////////////////////////////
            // GC B cell differentiatior 
            ////////////////////////////

            stateNames = new string[] {  "centroblast", "centrocyte", "long-lived plasmacyte", "memory" };
            geneNames = new string[] {           "gCXCR4", "gCXCR5", "gIgH", "gIgL", "gIgS", "gAID", "gBL1", "gMHCII" };
            activations = new double[,]          { { 1,        0,       0,      0,      0,       1,      0,      0 },  // centroblast
                                                   { 0,        1,       1,      1,      0,       0,      1,      1 },  // centrocyte
                                                   { 0,        0,       1,      1,      1,       0,      0,      0 },  // llplasmacyte
                                                   { 0,        0,       1,      1,      0,       0,      1,      1 },  // memory
                                                };
            //                                           cb       cc     llp     memory
            signal = new string[,]          {      {      "",   "sDif4", "",       ""      },  // centroblast
                                                   {  "sDif5",   "",   "sDif6",  "sDif7"  },  // centrocyte
                                                   {      "",     "",    "",       ""     },  // llplasmacyte
                                                   {      "",     "",    "",       ""     },  // memory
                                                };
            //  no spontaneous transitions          cb       cc    llp     memory
            alpha = new double[,]          {       { 0,      0,    0,       0   },  // centroblast
                                                   { 0,      0,    0,       0   },  // centrocyte
                                                   { 0,      0,    0,       0   },  // llplasmacyte
                                                   { 0,      0,    0,       0   },  // memory
                                                };
            //  need better values here             cb       cc    llp     memory
            beta = new double[,]           {       { 0,      1,    0,       0   },  // centroblast
                                                   { 0.5,    0,   1.0,     0.25   },  // centrocyte
                                                   { 0,      0,    0,       0   },  // llplasmacyte
                                                   { 0,      0,    0,       0   },  // memory
                                                };

            diffScheme = new ConfigDiffScheme();
            diffScheme.Name = "GC B cell";
            driver = new ConfigTransitionDriver();
            driver.Name = "GC B Cell driver";
            driver.CurrentState = 0;
            driver.StateName = stateNames[driver.CurrentState];

            // Attach transition driver to differentiation scheme
            diffScheme.Driver = driver;

            // Add genes
            diffScheme.genes = new ObservableCollection<string>();
            for (int j = 0; j < activations.GetLength(1); j++)
            {
                diffScheme.genes.Add(findGeneGuid(geneNames[j], store));
            }

            // Add epigenetic map of genes and activations
            diffScheme.activationRows = new ObservableCollection<ConfigActivationRow>();
            for (int i = 0; i < activations.GetLength(0); i++)
            {
                actRow = new ConfigActivationRow();
                for (int j = 0; j < activations.GetLength(1); j++)
                {
                    actRow.activations.Add(activations[i, j]);
                }
                diffScheme.activationRows.Add(actRow);
            }

            // Add DriverElements to TransitionDriver
            LoadConfigTransitionDriverElements(driver, signal, alpha, beta, stateNames, store);

            // Add to Entity Repository
            store.entity_repository.transition_drivers.Add(driver);
            store.entity_repository.transition_drivers_dict.Add(driver.entity_guid, driver);
            store.entity_repository.diff_schemes.Add(diffScheme);
            store.entity_repository.diff_schemes_dict.Add(diffScheme.entity_guid, diffScheme);
        }

        private static void PredefinedGenesCreator(Level store)
        {
            ConfigGene gene;

            gene = new ConfigGene("gCXCR5", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);

            gene = new ConfigGene("gCXCR4", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);

            gene = new ConfigGene("gCXCL12", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);

            gene = new ConfigGene("gCXCL13", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);

            gene = new ConfigGene("gIgH", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);
           
            gene = new ConfigGene("gIgL", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);

            gene = new ConfigGene("gIgS", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);

            gene = new ConfigGene("gAID", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);

            gene = new ConfigGene("gBL1", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);

            gene = new ConfigGene("gMHCII", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);

            gene = new ConfigGene("gApop", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);

            gene = new ConfigGene("gDiv", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);

            gene = new ConfigGene("gResc1", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);

            gene = new ConfigGene("gResc2", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);

            // generic genes for differentiation signal molecules
            for (int i = 1; i < 8; i++)
            {
                gene = new ConfigGene("gDif" + i, 2, 1.0);
                store.entity_repository.genes.Add(gene);
                store.entity_repository.genes_dict.Add(gene.entity_guid, gene);
            }
        }

        private static void PredefinedMoleculesCreator(Level store)
        {
            // Molecular weight in kDa
            // Effective radius in micrometers (um)
            // Diffusion coefficient in um^{2} / min
            ConfigMolecule cm;

            //
            // The following are generally intended as ECS molecules
            //

            // Wang2011, CXCL12:  MWt = 7.96 kDa, D = 4.5e3
            double MWt_CXCL12 = 7.96;
            cm = new ConfigMolecule("CXCL12", MWt_CXCL12, 1.0, 4.5e3);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            // Use CXCL13 values for CXCL13, for now
            cm = new ConfigMolecule("CXCL13", MWt_CXCL12, 1.0, 4.5e3);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            //
            // Membrane bound
            // 

            // Set the diffusion coefficients very low, for now.
            double membraneDiffCoeff = 1e-7;

            // Marchese2001, CXCR4:  MWt = 43 kDa
            double MWt_CXCR4 = 43;
            cm = new ConfigMolecule("CXCR4|", MWt_CXCR4, 1.0, membraneDiffCoeff);
            cm.molecule_location = MoleculeLocation.Boundary;
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            // Use CXCR4 value for CXCR5
            cm = new ConfigMolecule("CXCR5|", MWt_CXCR4, 1.0, membraneDiffCoeff);
            cm.molecule_location = MoleculeLocation.Boundary;
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            // Add values for CXCL12 and CXCR4
            cm = new ConfigMolecule("CXCL12:CXCR4|", MWt_CXCL12 + MWt_CXCR4, 1.0, membraneDiffCoeff);
            cm.molecule_location = MoleculeLocation.Boundary;
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);
            
            // Use CXCL12:CXCR4 values, for now
            cm = new ConfigMolecule("CXCL13:CXCR5|", MWt_CXCL12 + MWt_CXCR4, 1.0, membraneDiffCoeff);
            cm.molecule_location = MoleculeLocation.Boundary;
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            // Molecules for secretion
            cm = new ConfigMolecule("CXCL12|", MWt_CXCL12, 1.0, membraneDiffCoeff);
            cm.molecule_location = MoleculeLocation.Boundary;
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("CXCL13|", MWt_CXCL12, 1.0, membraneDiffCoeff);
            cm.molecule_location = MoleculeLocation.Boundary;
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);


            //
            // The following are generally intended as cytosol molecules 
            //

            // Bionumbers: diffusion coefficient for cytoplasmic proteins ~ 300 - 900
            // Francis1997: diffusion coefficient for signalling molecules ~ 600 - 6000
            // Use mean of common range 600 - 900
            double cytoDiffCoeff = 750;

            cm = new ConfigMolecule("CXCR5", MWt_CXCR4, 1.0, cytoDiffCoeff);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("CXCR4", MWt_CXCR4, 1.0, cytoDiffCoeff);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("CXCL13:CXCR5", MWt_CXCR4 + MWt_CXCL12, 1.0, cytoDiffCoeff);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("CXCL12:CXCR4", MWt_CXCR4 + MWt_CXCL12, 1.0, cytoDiffCoeff);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            // These are pseudo cytoplasmic molecules
            //
            // Set diffusion coefficient for A to same as other cytoplasmic proteins
            cm = new ConfigMolecule("A", 1.0, 1.0, cytoDiffCoeff);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);
            // Make the diffusion coefficient for A* much less than A
            // A* encompasses the tubulin structure and polarity of the cell 
            // If the diffusion coefficient is too large, the polarity will not be maintained and the cell will not move
            double f = 1e-2;
            cm = new ConfigMolecule("A*", 1.0, 1.0, f * cytoDiffCoeff);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            // Signaling (pseudo) molecules

            // generic cell division
            cm = new ConfigMolecule("sDiv", 1.0, 1.0, 1.0);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            // generic cell differentiation
            for (int i = 1; i < 8; i++)
            {
                cm = new ConfigMolecule("sDif" + i, 1.0, 1.0, 1.0);
                store.entity_repository.molecules.Add(cm);
                store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);
            }

            // generic cell apoptosis
            cm = new ConfigMolecule("sApop", 1.0, 1.0, 1.0);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            // generic t cell rescue
            cm = new ConfigMolecule("sResc1", 1.0, 1.0, 1.0);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("sResc2", 1.0, 1.0, 1.0);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("IgL", 1.0, 1.0, 1.0);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("IgS", 1.0, 1.0, 1.0);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("IgH", 1.0, 1.0, 1.0);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("AID", 1.0, 1.0, 1.0);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("BL1", 1.0, 1.0, 1.0);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("MHCII", 1.0, 1.0, 1.0);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

        }

        //Following function needs to be called only once
        private static void PredefinedReactionTemplatesCreator(Level store)
        {
            //Test code to read in json containing object "PredefinedReactions"
            //string readText = File.ReadAllText("TESTER.TXT");
            //entity_repository.reactions = JsonConvert.DeserializeObject<ObservableCollection<ConfigReaction>>(readText);
            ConfigReactionTemplate crt;

            //Annihilation
            // a ->
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            //products
            //crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.Annihilation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);
            
            //Association
            // a + b -> c
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.Association;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);

            // Dimerization
            // 2a -> b
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(2);
            //products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.Dimerization;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);

            // Dimer Dissociation
            // b -> 2a
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(2);
            // type
            crt.reac_type = ReactionType.DimerDissociation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);

            //Dissociation
            // c -> a + b
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(1);
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.Dissociation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);

            // Transformation
            // a -> b
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            // products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.Transformation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);

            // CatalyzedBoundaryActivation
            // a + E -> E + b
            crt = new ConfigReactionTemplate();
            // modifiers
            crt.modifiers_stoichiometric_const.Add(1);
            // reactants
             crt.reactants_stoichiometric_const.Add(1);
            // products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.CatalyzedBoundaryActivation;
            crt.isBoundary = true;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);

            //BoundaryAssociation
            // a + B -> C
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.BoundaryAssociation;
            crt.isBoundary = true;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);

            //BoundaryDissociation
            // C -> a + B
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(1);
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.BoundaryDissociation;
            crt.isBoundary = true;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);

            // BoundaryTransportFrom
            // A -> a
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            // products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.BoundaryTransportFrom;
            crt.isBoundary = true;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);

            // BoundaryTransportTo
            // a -> A
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            // products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.BoundaryTransportTo;
            crt.isBoundary = true;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);

            // Autocatalytic Transformation
            // Not a catalyzed reaction in the strict sense needed in the Config class
            // a + e -> 2e 
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(2);
            // type
            crt.reac_type = ReactionType.AutocatalyticTransformation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);

            // Catalyzed Annihilation
            // a + e -> e
            crt = new ConfigReactionTemplate();
            // modifiers
            crt.modifiers_stoichiometric_const.Add(1);
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            //products
            // type
            crt.reac_type = ReactionType.CatalyzedAnnihilation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);

            // Catalyzed Association
            // a + b + e -> c + e
            crt = new ConfigReactionTemplate();
            // modifiers
            crt.modifiers_stoichiometric_const.Add(1);
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.CatalyzedAssociation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);

            // Catalyzed Creation
            // e -> e + a
            crt = new ConfigReactionTemplate();
            // modifiers
            crt.modifiers_stoichiometric_const.Add(1);
            // reactants
            //products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.CatalyzedCreation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);

            // Catalyzed Dimerization
            // e + 2a -> b + e
            crt = new ConfigReactionTemplate();
            // modifiers
            crt.modifiers_stoichiometric_const.Add(1);
            // reactants
            crt.reactants_stoichiometric_const.Add(2);
            //products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.CatalyzedDimerization;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);

            // Catalyzed Dimer Dissociation
            // e + b -> 2a + e
            crt = new ConfigReactionTemplate();
            // modifiers
            crt.modifiers_stoichiometric_const.Add(1);
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(2);
            // type
            crt.reac_type = ReactionType.CatalyzedDimerDissociation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);

            // Catalyzed Dissociation
            // c + e -> a + b + e
            crt = new ConfigReactionTemplate();
            // modifiers
            crt.modifiers_stoichiometric_const.Add(1);
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(1);
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.CatalyzedDissociation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);

            // Catalyzed Transformation
            // a + e -> b + e
            crt = new ConfigReactionTemplate();
            // modifiers
            crt.modifiers_stoichiometric_const.Add(1);
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.CatalyzedTransformation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);

            // Transcription
            // gA -> A + gA
            crt = new ConfigReactionTemplate();
            // modifiers
            crt.modifiers_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.Transcription;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            store.entity_repository.reaction_templates.Add(crt);
            store.entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);        
        }

        // given a transition driver name, find its guid
        public static string findTransitionDriverGuid(string name, Level store)
        {
            foreach (ConfigTransitionDriver s in store.entity_repository.transition_drivers)
            {
                if (s.Name == name)
                {
                    return s.entity_guid;
                }
            }
            return "";
        }

        // given a diff scheme name, find its guid
        public static string findDiffSchemeGuid(string name, Level store)
        {
            foreach (ConfigDiffScheme s in store.entity_repository.diff_schemes)
            {
                if (s.Name == name)
                {
                    return s.entity_guid;
                }
            }
            return "";
        }

        // given a gene name, find its guid
        public static string findGeneGuid(string name, Level store)
        {
            foreach (ConfigGene gene in store.entity_repository.genes)
            {
                if (gene.Name == name)
                {
                    return gene.entity_guid;
                }
            }
            return "";
        }

        // given a gene name, find its gene
        public static ConfigGene findGene(string name, Level store)
        {
            foreach (ConfigGene gene in store.entity_repository.genes)
            {
                if (gene.Name == name)
                {
                    return gene;
                }
            }
            return null;
        }

        // given a molecule name and location, find its guid
        public static string findMoleculeGuid(string name, MoleculeLocation ml, Level store)
        {
            foreach (ConfigMolecule cm in store.entity_repository.molecules)
            {
                if (cm.Name == name && cm.molecule_location == ml)
                {
                    return cm.entity_guid;
                }
            }
            return "";
        }

        // given a cell type name like BCell, find the ConfigCell object
        public static ConfigCell findCell(string name, Level level)
        {
            foreach (ConfigCell cc in level.entity_repository.cells)
            {
                if (cc.CellName == name)
                {
                    return cc;
                }
            }
            return null;
        }

        //Following function needs to be called only once
        private static void PredefinedReactionsCreator(Level store)
        {
            // NOTE: Whenever there are two or more reactants (or products or modifiers) in a boundary reaction,
            // the bulk molecules must be added first, then the boundary molecules.

            double kr, kf;

            // These values are waiting for more informed choices:
            //
            // Default degradation rate for ECS proteins  (min)
            // Arbitraritly, assume half life lambda=7 min, then rate constant k=ln(2)/lambda=0.6931/7
            double ecsDefaultDegradRate = 0.1; 
            //
            // Default degradation rate for cytoplasm proteins  (min)
            // Arbitraritly, assume half life lambda=7 min, then rate constant k=ln(2)/lambda=0.6931/7
            double cytoDefaultDegradRate = 0.1;

            ConfigReaction cr = new ConfigReaction();

            // Annihiliation: CXCR5 -> 
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, store));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            // Annihiliation: CXCL13:CXCR5 -> 
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, store));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            // Annihiliation: CXCR4 -> 
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR4", MoleculeLocation.Bulk, store));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            // Annihiliation: CXCL12:CXCR4 -> 
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants 
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL12:CXCR4", MoleculeLocation.Bulk, store));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            // Annihiliation: CXCL12 -> 
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL12", MoleculeLocation.Bulk, store));
            cr.rate_const = ecsDefaultDegradRate;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            // Annihiliation: CXCL13 -> 
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, store));
            cr.rate_const = ecsDefaultDegradRate;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);


            ////////////////////////////////////////////////////////////////////////////////////
            // CXCL13 + CXCR5| <-> CXCL13:CXCR5|
            //
            // Barroso, Munoz, et al.
            // EBI2 regulates CXCL13-mediated responses by heterodimerization with CXCR5
            // The FASEB Journal vol. 26 no. 12 4841-485
            // CXCL13/CXCR5 binding affinity:  KD = 5.05e-8 M  = (50.5e-9)*(1e-18)*(6.022e23) molec/um^3 = 0.0304 molec/um^3
            //
            // KD = k_reverse / k_forward
            // Use k_reverse from CXCL12/CXCR4| binding/unbinding.
            // Vega2011 k_off = 8.24e-3/s = 0.494/min
            // Use same for CXCL13:CXCR5| unbinding k_reverse = 0.494 min^{-1}
            // k_forward = k_reverse / 0.0304 um^3/(molec-min) = 1.625 um^3/(molec-min)
            //
            kr = 0.494;
            kf = 16.25;
            //
            // BoundaryAssociation: CXCL13 + CXCR5| -> CXCL13:CXCR5|
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.BoundaryAssociation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, store));
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5|", MoleculeLocation.Boundary, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL13:CXCR5|", MoleculeLocation.Boundary, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            //
            // BoundaryDissociation:  CXCL13:CXCR5| ->  CXCR5| + CXCL13
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.BoundaryDissociation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13:CXCR5|", MoleculeLocation.Boundary, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, store));
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5|", MoleculeLocation.Boundary, store));
            cr.rate_const = kr;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            //////////////////////////////////////////////////////////////////////////////////
            

            /////////////////////////////////////////////////////////////////////////////
            // CXCL13:CXCR5| + A -> CXCL13:CXCR5| + A*
            // A* -> A
            // CXCL12:CXCR4| + A -> CXCL12:CXCR4| + A*
            //
            // The A* gradient drives the locomotion force.
            // The rate constant has units min^{-1}, similar to the units for off rate for receptor/ligand binding,
            // but this reaction encompasses many processes, including tubulin network growth. Therefore, we expect
            // this pseudo-reaction to occur at a slower rate, and we will choose a rate constant (activation rate) 
            // that is significantly less than simple off rates.
            // 
            // For simple unbinding k_off ~ 0.5 min^{-1} (CXCL12:CXCR4, Vega2011), so arbitrarily, choose a reduction factor 
            double f1 = 1e-2;
            // Then  k_activation ~ 0.5 * f = 5e-3 min^{-1}
            //
            // Choose a slower deactivation (A* -> A) rate   k_deactivation = k_activation / 100;
            double f2 = 1e-1;
            // 
            kf = 0.5 * f1;
            kr = kf * f2;
            //
            // CatalyzedBoundaryActivation: CXCL13:CXCR5| + A -> CXCL13:CXCR5| + A*
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.CatalyzedBoundaryActivation);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("CXCL13:CXCR5|", MoleculeLocation.Boundary, store));
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("A", MoleculeLocation.Bulk, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("A*", MoleculeLocation.Bulk, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            //
            // Transformation: A* -> A
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transformation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("A*", MoleculeLocation.Bulk, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("A", MoleculeLocation.Bulk, store));
            cr.rate_const = kr;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            //
            // CatalyzedBoundaryActivation: CXCL12:CXCR4| + A -> CXCL12:CXCR4| + A*
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.CatalyzedBoundaryActivation);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("CXCL12:CXCR4|", MoleculeLocation.Boundary, store));
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("A", MoleculeLocation.Bulk, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("A*", MoleculeLocation.Bulk, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            ////////////////////////////////////////////////////////////////

            ///////////////////////////////////////////////////////////////////////////////////
            // CXCL12 + CXCR4| -> CXCL12:CXCR4|
            //
            ////// Crump, M. P., Gong, J. H., Loetscher, et al., (1997) EMBO J. 16, 6996–7007.
            ////// CXCL12/CXCR4 binding affinity:  KD = 3.6e-9 M  = (3.6e-9)*(1e-18)*(6.022e23) molec/um^3 = 2.2e-3 molec/um^3
            //////
            ////// KD = k_reverse / k_forward
            ////// Arbitrarily, choose k_reverse = 1/0.1   min^{-1} = 10 min^{-1}
            ////// k_forward = k_reverse / 2.2e-3 um^3/(molec-min) = 455 um^3/(molec-min)
            //////
            ///////////////////////////////////////////////////////////////////////////////////
            // Vega et al., Technical Advance: Surface plasmon resonance-based analysis of CXCL12 binding 
            // using immobilized lentiviral particles. Journal of Leukocyte Biology Vol 90, 1-10, 2011. 
            // 
            // CXCL12/CXCR4 binding affinity:  KD = 2.09e-2 molec/um^3
            // k_reverse (k_off) = 0.494/min
            //
            // KD = k_reverse / k_forward
            // k_forward = k_reverse / KD = 0.494 / 2.09e-2  um^3/(molec-min) = 23.6 um^3/(molec-min)
            //
            kr = 0.494;
            kf = 23.6;
            //
            // BoundaryAssociation: CXCL12 + CXCR4| -> CXCL12:CXCR4|
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.BoundaryAssociation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL12", MoleculeLocation.Bulk, store));
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR4|", MoleculeLocation.Boundary, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL12:CXCR4|", MoleculeLocation.Boundary, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            //
            // BoundaryDissociation:  CXCL12:CXCR4| ->  CXCR4| + CXCL12
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.BoundaryDissociation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL12:CXCR4|", MoleculeLocation.Boundary, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL12", MoleculeLocation.Bulk, store));
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR4|", MoleculeLocation.Boundary, store));
            cr.rate_const = kr;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            ///////////////////////////////////////////////////////////////////////////////////////////////////

            // Internalization of bound receptor
            // 
            // Barroso R, Munoz LM, Barrondo S, et al., EBI2 regulates CXCL13-mediated responses by 
            // heterodimerization with CXCR5. The FASEB Journal Vol. 26, pp 4841-4854, December 2012.
            // Fit of fig. S3b:
            //          k = 0.010 min(-1)
            double k1_CXCL13_CXCR5 = 0.010;
            //
            // BoundaryTransportFrom: CXCL13:CXCR5| -> CXCL13:CXCR5
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.BoundaryTransportFrom);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13:CXCR5|", MoleculeLocation.Boundary, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL13:CXCR5", MoleculeLocation.Bulk, store));
            cr.rate_const = k1_CXCL13_CXCR5;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            //
            // Hesselgesser et al.,  Identification and Characterization of the CXCR4 Chemokine, J Immunol 1998; 160:877-883.
            // Fit of curve on pg 880, SDF-1alpha(CXCL12):CXCR4 internalization:
            //          k = 0.027 min(-1)
            double k1_CXCL12_CXCR4 = 0.027;
            //
            // BoundaryTransportFrom: CXCL12:CXCR4| -> CXCL12:CXCR4
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.BoundaryTransportFrom);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL12:CXCR4|", MoleculeLocation.Boundary, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL12:CXCR4", MoleculeLocation.Bulk, store));
            cr.rate_const = k1_CXCL12_CXCR4;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            //
            // Assume that internalization of the unactivated receptor is slower than internalization of bound(activated) receptor
            // Arbitrarily choose a factor of 100
            f2 = 1e-2;
            //
            // BoundaryTransportFrom: CXCR5| -> CXCR5
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.BoundaryTransportFrom);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5|", MoleculeLocation.Boundary, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, store));
            cr.rate_const = f2 * k1_CXCL13_CXCR5;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            //
            // BoundaryTransportFrom: CXCR4| -> CXCR4
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.BoundaryTransportFrom);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR4|", MoleculeLocation.Boundary, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR4", MoleculeLocation.Bulk, store));
            cr.rate_const = f2 * k1_CXCL12_CXCR4;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            //
            // Secretion reactions
            //
            string[] mol_name =        { "CXCL12", "CXCL13" };
            double[] k_cytosol_to_pm =  {   1.0,      1.0 };
            double[] k_pm_to_ecs =      {   1.0,      1.0 };
            // BoundaryTransportTo: cytosol to plasma membrane
            for (int i = 0; i < mol_name.Length; i++ )
            {
                cr = new ConfigReaction();
                cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.BoundaryTransportTo);
                // reactants
                cr.reactants_molecule_guid_ref.Add(findMoleculeGuid(mol_name[i], MoleculeLocation.Bulk, store));
                // products
                cr.products_molecule_guid_ref.Add(findMoleculeGuid(mol_name[i] + "|", MoleculeLocation.Boundary, store));
                cr.rate_const = k_cytosol_to_pm[i];
                cr.GetTotalReactionString(store.entity_repository);
                store.entity_repository.reactions.Add(cr);
            }
            // BoundaryTransportFrom: plasma membrane to ECS
            for (int i = 0; i < mol_name.Length; i++)
            {
                cr = new ConfigReaction();
                cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.BoundaryTransportFrom);
                // reactants
                cr.reactants_molecule_guid_ref.Add(findMoleculeGuid(mol_name[i] + "|", MoleculeLocation.Boundary, store));
                // products
                cr.products_molecule_guid_ref.Add(findMoleculeGuid(mol_name[i], MoleculeLocation.Bulk, store));
                cr.rate_const = k_pm_to_ecs[i];
                cr.GetTotalReactionString(store.entity_repository);
                store.entity_repository.reactions.Add(cr);
            }

            //
            // These next reactions are in need of more informed reaction rates
            //

            // BoundaryTransportTo: CXCR5 -> CXCR5|
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.BoundaryTransportTo);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5|", MoleculeLocation.Boundary, store));
            cr.rate_const = 1.0;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            // BoundaryTransportTo: CXCR4 -> CXCR4|
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.BoundaryTransportTo);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR4", MoleculeLocation.Bulk, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR4|", MoleculeLocation.Boundary, store));
            cr.rate_const = 1.0;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            // Transcription

            // Schwanhausser et al, Nature 2011 (473), 337-342
            // average mRNAs per hour:  ~ 2
            // average proteins per mRNA per hour: ~ 100
            // Default: 2 gene copies per cell
            kf = 2 * 100.0 / (2*60);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gCXCR4", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR4", MoleculeLocation.Bulk, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gCXCR5", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gCXCL12", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL12", MoleculeLocation.Bulk, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gCXCL13", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gIgH", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("IgH", MoleculeLocation.Bulk, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gIgL", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("IgL", MoleculeLocation.Bulk, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gIgS", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("IgS", MoleculeLocation.Bulk, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gAID", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("AID", MoleculeLocation.Bulk, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gBL1", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("BL1", MoleculeLocation.Bulk, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gMHCII", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("MHCII", MoleculeLocation.Bulk, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gApop", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("sApop", MoleculeLocation.Bulk, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gDiv", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("sDiv", MoleculeLocation.Bulk, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gResc1", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("sResc1", MoleculeLocation.Bulk, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gResc2", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("sResc2", MoleculeLocation.Bulk, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            // generic transcription for differentiation signaling genes
            for (int i = 1; i < 8; i++)
            {
                cr = new ConfigReaction();
                cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
                // modifiers
                cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gDif" + i, store));
                // products
                cr.products_molecule_guid_ref.Add(findMoleculeGuid("sDif" + i, MoleculeLocation.Bulk, store));
                cr.rate_const = kf;
                cr.GetTotalReactionString(store.entity_repository);
                store.entity_repository.reactions.Add(cr); 
            }

            // degradation of signalling molecules 

            for (int i = 1; i < 8; i++)
            {
                cr = new ConfigReaction();
                cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
                // reactants
                cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("sDif" + i, MoleculeLocation.Bulk, store));
                cr.rate_const = cytoDefaultDegradRate;
                cr.GetTotalReactionString(store.entity_repository);
                store.entity_repository.reactions.Add(cr);
            }

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("sDiv", MoleculeLocation.Bulk, store));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("sApop", MoleculeLocation.Bulk, store));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("sResc1", MoleculeLocation.Bulk, store));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("sResc2", MoleculeLocation.Bulk, store));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("IgH", MoleculeLocation.Bulk, store));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("IgL", MoleculeLocation.Bulk, store));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("IgS", MoleculeLocation.Bulk, store));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("AID", MoleculeLocation.Bulk, store));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("BL1", MoleculeLocation.Bulk, store));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("MHCII", MoleculeLocation.Bulk, store));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            ///////////////////////////////////////////////
            // NOTE: These reactions may not be biologically meaningful for bulk molecules, 
            // but are used for reaction complexes
            //
            // Association: CXCR5 + CXCL13 -> CXCL13:CXCR5
            kr = 0.494;
            kf = 16.25;
            //
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Association);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, store));
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL13:CXCR5", MoleculeLocation.Bulk, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            // Dissociation: CXCL13:CXCR5 -> CXCR5 + CXCL13
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Dissociation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13:CXCR5", MoleculeLocation.Bulk, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, store));
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, store));
            cr.rate_const = kr;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            ////////////////////////////////////////////////////

        }

        private static void PredefinedReactionComplexesCreator(Level store)
        {
            ConfigReactionComplex crc = new ConfigReactionComplex("Ligand/Receptor");

            //MOLECULES
            double[] conc = new double[3] { 0.0304, 1, 0 };
            string[] type = new string[3] { "CXCL13", "CXCR5", "CXCL13:CXCR5" };

            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];

                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.CELL_MP);

                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;
                    configMolPop.mp_dist_name = "Uniform";
                    configMolPop.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    configMolPop.mp_render_blending_weight = 2.0;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();

                    hl.concentration = conc[i];
                    configMolPop.mp_distribution = hl;
                    crc.molpops.Add(configMolPop);
                }
            }

            //REACTIONS

            //string guid = findReactionGuid(ReactionType.Association, sc);

            // Reaction strings
            type = new string[2] { "CXCL13:CXCR5 -> CXCL13 + CXCR5", "CXCL13 + CXCR5 -> CXCL13:CXCR5"}; 
            
            for (int i = 0; i < type.Length; i++)
            {
                ConfigReaction reac = findReaction(type[i], store);

                if (reac != null)
                {
#if OLD_RC
                    ConfigReactionGuidRatePair grp = new ConfigReactionGuidRatePair();
                    grp.entity_guid = reac.entity_guid;
                    grp.OriginalRate = reac.rate_const;
                    grp.ReactionComplexRate = reac.rate_const;
#endif

                    crc.reactions.Add(reac.Clone(true));
#if OLD_RC
                    crc.ReactionRates.Add(grp);
#endif
                }
            }

            store.entity_repository.reaction_complexes.Add(crc);
        }

        // given a string description of a reaction, return the ConfigReaction that matches
        public static ConfigReaction findReaction(string rt, Level store)
        {
            foreach (ConfigReaction cr in store.entity_repository.reactions)
            {
                if (cr.TotalReactionString == rt) //cr.reaction_template_guid_ref == template_guid)
                {
                    return cr;
                }
            }
            return null;
        }

        // given a reaction guid, return the ConfigReaction 
        public static ConfigReaction findReactionByGuid(string guid, Protocol protocol)
        {
            foreach (ConfigReaction cr in protocol.entity_repository.reactions)
            {
                if (cr.entity_guid == guid) 
                {
                    return cr;
                }
            }
            return null;
        }

        /// <summary>
        /// Add ConfigTransitionDriverElements to a ConfigTransitionDriver.
        /// Null transition elements are included to facilitate use in the GUI.
        /// </summary>
        /// <param name="driver">The ConfigTransitionDriver</param>
        /// <param name="signal">Square array of driver molecule names</param>
        /// <param name="alpha">Square array of Alpha values</param>
        /// <param name="beta">Square array of Beta values</param>
        /// <param name="stateName"></param>
        public static void LoadConfigTransitionDriverElements(ConfigTransitionDriver driver, string[,] signal, double[,] alpha, double[,] beta,string[] stateName, Level store)
        {
            ConfigTransitionDriverRow row;
            driver.DriverElements = new ObservableCollection<ConfigTransitionDriverRow>();
            driver.states = new ObservableCollection<string>();
            for (int i = 0; i < signal.GetLength(0); i++)
            {
                row = new ConfigTransitionDriverRow();
                row.elements = new ObservableCollection<ConfigTransitionDriverElement>();
                driver.states.Add(stateName[i]);
                for (int j = 0; j < signal.GetLength(1); j++)
                {
                    ConfigTransitionDriverElement driverElement = new ConfigTransitionDriverElement();

                    driverElement.CurrentState = i;
                    driverElement.DestState = j;
                    driverElement.CurrentStateName = stateName[i];
                    driverElement.DestStateName = stateName[j];

                    if (signal[i, j] != "")
                    {
                        driverElement.Alpha = alpha[i, j];
                        driverElement.Beta = beta[i, j];
                        driverElement.driver_mol_guid_ref = findMoleculeGuid(signal[i, j], MoleculeLocation.Bulk, store);
                    }
                    row.elements.Add(driverElement);
                }
                driver.DriverElements.Add(row);
            }
        }
    }
}
