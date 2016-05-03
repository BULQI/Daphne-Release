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
            daphneStore.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor;
            daphneStore.SerializeToFile();

            //Clone UserStore from DaphneStore
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(daphneStore.entity_repository, Newtonsoft.Json.Formatting.Indented, Settings);
            userStore.entity_repository = JsonConvert.DeserializeObject<EntityRepository>(jsonSpec, Settings);
            userStore.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor;
            userStore.SerializeToFile();
            //update renderSkin
            RenderSkin sk = new RenderSkin("default_skin", userStore.entity_repository);
            sk.FileName ="Config\\RenderSkin\\default_skin.json";
            sk.SerializeToFile();
        }

        public static void LoadDefaultGlobalParameters(Level store)
        {
            // genes
            PredefinedGenesCreator(store);

            // molecules
            PredefinedMoleculesCreator(store);

            // differentiation schemes
            PredefinedTransitionSchemesCreator(store);

            // template reactions
            PredefinedReactionTemplatesCreator(store);

            //code to create reactions
            PredefinedReactionsCreator(store);

            //reaction complexes
            PredefinedReactionComplexesCreator(store);

            //cells
            PredefinedCellsCreator(store);
        }

 
        //------------------------------------------------
        // Accessory methods for building protocols

        /// <summary>
        /// Use UserStore for loading entity_repository into the given protocol.
        /// This way every protocol will have the same entity guids.
        /// </summary>
        /// <param name="protocol"></param>
        public static void LoadEntitiesFromUserStore(Protocol protocol)
        {
            if (protocol == null)
                return;

            Level store = new Level("Config\\Stores\\userstore.json", "Config\\Stores\\temp_userstore.json");
            store = store.Deserialize();

            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(store.entity_repository, Newtonsoft.Json.Formatting.Indented, Settings);
            protocol.entity_repository = JsonConvert.DeserializeObject<EntityRepository>(jsonSpec, Settings);
            protocol.InitializeStorageClasses();
        }
        
        private static int LoadProtocolGenes(Protocol protocol, string[] geneName, Level userstore)
        {
            int itemsLoaded = 0;

            for (int i = 0; i < geneName.Length; i++)
            {
                ConfigGene configGene = null;
                string guid = findGeneGuid(geneName[i], userstore);


                if (guid != "")
                {
                    configGene = userstore.entity_repository.genes_dict[guid];
                    if (configGene != null)
                    {
                        ConfigGene newgene = configGene.Clone(null);
                        protocol.repositoryPush(newgene, Level.PushStatus.PUSH_CREATE_ITEM);
                        itemsLoaded++;
                    }
                }
            }

            return itemsLoaded;
        }

        private static int LoadProtocolMolecules(Protocol protocol, string[] moleculeName, MoleculeLocation molLoc, Level userstore)
        {
            int itemsLoaded = 0;

            for (int i = 0; i < moleculeName.Length; i++)
            {
                ConfigMolecule configMolecule = null;
                string guid = findMoleculeGuid(moleculeName[i], molLoc, userstore);

                if (guid != "")
                {
                    configMolecule = userstore.entity_repository.molecules_dict[guid];
                    if (configMolecule != null)
                    {
                        ConfigMolecule newmol = configMolecule.Clone(null);
                        protocol.repositoryPush(newmol, Level.PushStatus.PUSH_CREATE_ITEM);
                        itemsLoaded++;
                    }
                }
            }

            return itemsLoaded;
        }

        private static int LoadProtocolReactions(Protocol protocol, string[] totalReactionString, Level userstore)
        {
            int itemsLoaded = 0;

            for (int i = 0; i < totalReactionString.Length; i++)
            {
                // Check whether this reaction has already been added to the protocol ER. If not, add it.
                if (findReaction(totalReactionString[i], protocol) == null)
                {
                    ConfigReaction configReaction = findReaction(totalReactionString[i], userstore);
                    if (configReaction != null)
                    {
                        ConfigReaction newReac = configReaction.Clone(true);
                        protocol.repositoryPush(newReac, Level.PushStatus.PUSH_CREATE_ITEM, userstore, true);
                        itemsLoaded++;
                    }
                }
            }

            return itemsLoaded;
        }

        private static int LoadProtocolReactionTemplates(Protocol protocol, Level userstore)
        {
            int itemsLoaded = 0;

            //Load all reaction templates instead of just the ones associated with reactions in a protocol
            foreach (ConfigReactionTemplate crt in userstore.entity_repository.reaction_templates)
            {
                ConfigReactionTemplate copycrt = crt.Clone(true);
                protocol.repositoryPush(copycrt, Level.PushStatus.PUSH_CREATE_ITEM);
                itemsLoaded++;
            }
            
            return itemsLoaded;
        }

        private static int LoadProtocolCells(Protocol protocol, string[] cellName, Level userstore)
        {
            int itemsLoaded = 0;

            for (int i = 0; i < cellName.Length; i++)
            {
                ConfigCell configCell = findCell(cellName[i], userstore);
                if (configCell != null)
                {
                    ConfigCell newcell = configCell.Clone(true);
                    protocol.repositoryPush(newcell, Level.PushStatus.PUSH_CREATE_ITEM, userstore, true);
                    itemsLoaded++;
                }
            }

            return itemsLoaded;
        }

        private static int LoadProtocolRCs(Protocol protocol, string[] RCName, Level userstore)
        {
            int itemsLoaded = 0;

            for (int i = 0; i < RCName.Length; i++)
            {
                foreach (ConfigReactionComplex ent in userstore.entity_repository.reaction_complexes)
                {
                    if (ent.Name == RCName[i])
                    {
                        protocol.repositoryPush(ent.Clone(true), Level.PushStatus.PUSH_CREATE_ITEM, userstore, true);
                        itemsLoaded++;
                        break;
                    }
                }
            }

            return itemsLoaded;
        }
        
        //-------------------------------------------------


        /// <summary>
        /// Helper method for creating a reaction template from the given store, 
        /// to the given protocol, given a reaction type.
        /// </summary>
        /// <param name="store"></param>
        /// <param name="type"></param>
        /// <param name="protocol"></param>
        //private static void AddReactionTemplate(Level store, ReactionType type, Protocol protocol)
        //{
        //    ConfigReactionTemplate crtUser = null;
        //    foreach (ConfigReactionTemplate crt in store.entity_repository.reaction_templates)
        //    {
        //        if (crt.reac_type == type)
        //        {
        //            crtUser = crt;
        //            break;
        //        }
        //    }
        //    if (crtUser != null)
        //    {
        //        ConfigReactionTemplate crtnew = crtUser.Clone(null);
        //        protocol.repositoryPush(crtnew, Level.PushStatus.PUSH_CREATE_ITEM);
        //    }
        //}

        public static void CreateLigandReceptorProtocol(Protocol protocol)
        {
            if (protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                throw new InvalidCastException();
            }

            protocol.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor;

            protocol.InitializeStorageClasses();

            //Load needed entities from User Store
            Level userstore = new Level("Config\\Stores\\userstore.json", "Config\\Stores\\temp_userstore.json");
            userstore = userstore.Deserialize();

            // Load reaction templates from userstore
            LoadProtocolReactionTemplates(protocol, userstore);

            ConfigECSEnvironment envHandle = (ConfigECSEnvironment)protocol.scenario.environment;

            // Experiment
            protocol.experiment_name = "CXCR4 and CXCR5 receptor homeostasis";
            string descr;
            descr = string.Format("{0}\n", "CXCR4 and CXCR5 receptor production and homeostasis reactions.");
            descr = string.Format("{0}{1}\n", descr, "Modify the concentrations of CXCL12 and CXCL13 in the extracellular medium to explore the effect of receptor binding. ");

            protocol.experiment_description = descr;        
            protocol.scenario.time_config.duration = 500;
            protocol.scenario.time_config.rendering_interval = protocol.scenario.time_config.duration / 10;
            protocol.scenario.time_config.sampling_interval = protocol.scenario.time_config.duration / 100;
            protocol.scenario.time_config.integrator_step = 0.001;

            envHandle.extent_x = 200;
            envHandle.extent_y = 200;
            envHandle.extent_z = 200;
            envHandle.gridstep = 10;

            // Get entities from User Store

            //ECS REACTION COMPLEXES - recursive
            // CXCL13 and CXCL12 binding/unbinding with CXCR5 and CXCR4
            string[] ecsReacComplex = new string[] { "GC B cell chemotaxis: ECM reactions" };
            int itemsLoaded = LoadProtocolRCs(protocol, ecsReacComplex, userstore);
            if (itemsLoaded != ecsReacComplex.Length)
            {
                System.Windows.MessageBox.Show("Unable to load all protocol reaction complexes.");
            }

            //Cytosol REACTION COMPLEXES - recursive
            string[] cytosolReacComplex = new string[] { "CXCR5 receptor production and recycling", 
                                                        "CXCR4 receptor production and recycling" };
            itemsLoaded = LoadProtocolRCs(protocol, cytosolReacComplex, userstore);
            if (itemsLoaded != cytosolReacComplex.Length)
            {
                System.Windows.MessageBox.Show("Unable to load all protocol reaction complexes.");
            }

            // Create a new cell, just for this simulation

            ConfigCell configCell = new ConfigCell();
            configCell.CellName = "Receptor homeostasis";
            configCell.CellRadius = 5.0;
            configCell.description = "Receptor homeostasis.";

            //MOLECULES IN MEMBRANE
            double[] conc = new double[] { 0, 0, 0, 0 };
            string[] type = new string[] { "CXCR5|", "CXCL13:CXCR5|", "CXCR4|", "CXCL12:CXCR4|" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule cm = protocol.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, protocol)];
                if (cm != null)
                {
                    ConfigMolecularPopulation gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gmp.report_mp.mp_extended = ExtendedReport.LEAN;
                    configCell.membrane.molpops.Add(gmp);
                }
            }

            //MOLECULES IN Cytosol
            type = new string[] { "CXCR4", "CXCR5", "CXCL12:CXCR4", "CXCL13:CXCR5" };
            conc = new double[type.Count()];
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule cm = protocol.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, protocol)];
                if (cm != null)
                {
                    ConfigMolecularPopulation gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gmp.report_mp.mp_extended = ExtendedReport.LEAN;
                    configCell.cytosol.molpops.Add(gmp);
                }
            }

            // Genes
            type = new string[] { "gCXCR4", "gCXCR5" };
            for (int i = 0; i < type.Length; i++)
            {
                configCell.genes.Add(findGene(type[i], protocol));
            }


            // Add CYTOSOL REACTION COMPLEXES 
            for (int i = 0; i < cytosolReacComplex.Length; i++)
            {
                ConfigReactionComplex crc = findReactionComplexByName(cytosolReacComplex[i], protocol);
                if (crc != null)
                {
                    configCell.cytosol.reaction_complexes.Add(crc.Clone(true));
                }
            }

            // Turn off stochastic motion
            configCell.Sigma.ConstValue = 0.0;


            // ECM

            // ECM MOLECULES

            conc = new double[] {0, 0};
            type = new string[] { "CXCL13", "CXCL12" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = protocol.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, protocol)];
                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.ECM_MP);
                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    configMolPop.mp_distribution = hl;
                    // Turn off diffusion so it will run faster
                    configMolPop.molecule.DiffusionCoefficient = 0.0;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.NONE;
                    ((ReportECM)configMolPop.report_mp).mean = false;

                    protocol.scenario.environment.comp.molpops.Add(configMolPop);

                    //rendering
                    ((TissueScenario)protocol.scenario).popOptions.AddRenderOptions(configMolPop.renderLabel, configMolPop.Name, false);
                }
            }

            // Add ECM REACTION COMPLEXES 
            for (int i = 0; i < ecsReacComplex.Length; i++)
            {
                ConfigReactionComplex crc = findReactionComplexByName(ecsReacComplex[i], protocol);
                if (crc != null)
                {
                    protocol.scenario.environment.comp.reaction_complexes.Add(crc.Clone(true));
                }
            }

            // Add cell population
            CellPopulation cellPop = new CellPopulation();
            cellPop.Cell = configCell.Clone(true);
            cellPop.cellpopulation_name = configCell.CellName;
            cellPop.number = 1;
            double[] extents = new double[3] { envHandle.extent_x, envHandle.extent_y, envHandle.extent_z };
            double minDisSquared = 2 * configCell.CellRadius;
            minDisSquared *= minDisSquared;
            cellPop.cellPopDist = new CellPopSpecific(extents, minDisSquared, cellPop);
            cellPop.cellPopDist.Initialize();
            cellPop.CellStates[0] = new CellState(envHandle.extent_x / 2, envHandle.extent_y / 2, envHandle.extent_z / 2);
            ((TissueScenario)protocol.scenario).cellpopulations.Add(cellPop);
            //rendering
            ((TissueScenario)protocol.scenario).popOptions.AddRenderOptions(cellPop.renderLabel, cellPop.cellpopulation_name, true);

            // Output all reactions to a report file
            protocol.scenario.reactionsReport = true;

            // Add cell reactions to the description
            descr = string.Format("{0}\n{1}\n", descr, "Cell reactions: ");
            descr = string.Format("{0}\n{1}", descr, "CXCR4 (CXCR5) molecules are produced (gene transcription) and degraded in the cytosol. ");
            descr = string.Format("{0}{1}", descr, "Cytosolic CXCR4 (CXCR5) is transported to the plasma membrane where it becomes CXCR4| (CXCR5|) receptor. ");
            descr = string.Format("{0}{1}\n", descr, "CXCR4| (CXCR5|) receptor and CXCL12:CXCR4| (CXCL13:CXCR5|) receptor complex are internalized and degraded. ");

            foreach (ConfigReactionComplex crc in configCell.cytosol.reaction_complexes)
            {
                descr = string.Format("{0}\n{1}\n", descr, crc.Name);
                foreach (ConfigReaction cr in crc.reactions)
                {
                    descr = string.Format("{0}rate = {1}\t{2}\n", descr, cr.rate_const.ToString(), cr.TotalReactionString);
                }
            }
            // Add ECM reactions to the description
            descr = string.Format("{0}\n{1}\n", descr, "ECM reactions: ");
            foreach (ConfigReactionComplex crc in protocol.scenario.environment.comp.reaction_complexes)
            {
                descr = string.Format("{0}\n{1}\n", descr, crc.Name);
                foreach (ConfigReaction cr in crc.reactions)
                {
                    descr = string.Format("{0}rate = {1}\t{2}\n", descr, cr.rate_const.ToString(), cr.TotalReactionString);
                }
            }
            // Update the protocol description
            protocol.experiment_description = descr;        

            protocol.reporter_file_name = "receptor_homeostasis";

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

            protocol.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor;

            protocol.InitializeStorageClasses();

            //Load needed entities from User Store
            Level userstore = new Level("Config\\Stores\\userstore.json", "Config\\Stores\\temp_userstore.json");
            userstore = userstore.Deserialize();

            // Load reaction templates from userstore
            LoadProtocolReactionTemplates(protocol, userstore);

            ConfigECSEnvironment envHandle = (ConfigECSEnvironment)protocol.scenario.environment;

            // Experiment
            protocol.experiment_name = "Chemotactic and stochastic cell movement";
            string descr = "";
            descr = string.Format("{0}", "The cell moves in the direction of increasing CXCL13 concentration (right to left).");
            descr = string.Format("{0}\n{1}", descr, "The chemotactic force is proportional to the gradient of molecule A* in the cytosol.");
            descr = string.Format("{0}\n{1}", descr, "A* is produced when molecule A is activated by bound chemokine receptor (the CXCL13:CXCR5| membrane molecule).");
            descr = string.Format("{0}\n{1}", descr, "The CXCL13 gradient in the extracellular medium causes a gradient in the bound chemokine receptor, which causes a gradient of the A* molecule in the cytosol. "); 
            protocol.experiment_description = descr;
            envHandle.extent_x = 200;
            envHandle.extent_y = 200;
            envHandle.extent_z = 200;
            envHandle.gridstep = 10;

            protocol.scenario.time_config.duration = 200;
            protocol.scenario.time_config.rendering_interval = protocol.scenario.time_config.duration / 100;
            protocol.scenario.time_config.sampling_interval = protocol.scenario.time_config.duration / 100;
            protocol.scenario.time_config.integrator_step = 0.001;

            //ECM REACTIONS - recursive
            string[] item = new string[] {  "CXCL13 + CXCR5| -> CXCL13:CXCR5|",
                                            "CXCL13:CXCR5| -> CXCL13 + CXCR5|" };
            int itemsLoaded = LoadProtocolReactions(protocol, item, userstore);
            if (itemsLoaded != item.Length)
            {
                System.Windows.MessageBox.Show("Unable to load all protocol reactions.");
            } 

            //CELLS - recursive
            item = new string[1] { "chemotactic with static receptor" };
            itemsLoaded = LoadProtocolCells(protocol, item, userstore);
            if (itemsLoaded != item.Length)
            {
                System.Windows.MessageBox.Show("Unable to load all protocol cells.");
            } 

            // ECS

            // Linear CXCL13 distribution
            //
            double CXCL13conc = 0.2;
            double[] conc = new double[1] { CXCL13conc };
            item = new string[1] { "CXCL13" };
            for (int i = 0; i < item.Length; i++)
            {
                ConfigMolecule configMolecule = protocol.entity_repository.molecules_dict[findMoleculeGuid(item[i], MoleculeLocation.Bulk, protocol)];

                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.ECM_MP);

                    // set the diffusion coefficient to zero for the locomotion protocol only
                    configMolecule.DiffusionCoefficient = 0.0;
                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;

                    MolPopLinear molpoplin = new MolPopLinear();
                    molpoplin.boundary_face = BoundaryFace.X;
                    molpoplin.dim = 0;
                    molpoplin.x1 = 0;
                    molpoplin.boundaryCondition = new List<BoundaryCondition>();
                    BoundaryCondition bc = new BoundaryCondition(MolBoundaryType.Dirichlet, Boundary.left, CXCL13conc);
                    molpoplin.boundaryCondition.Add(bc);
                    bc = new BoundaryCondition(MolBoundaryType.Dirichlet, Boundary.right, 0.0);
                    molpoplin.boundaryCondition.Add(bc);
                    configMolPop.mp_distribution = molpoplin;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.NONE;
                    ((ReportECM)configMolPop.report_mp).mean = false;

                    //Rendering
                    ((TissueScenario)protocol.scenario).popOptions.AddRenderOptions(configMolPop.renderLabel, configMolPop.Name, false);

                    protocol.scenario.environment.comp.molpops.Add(configMolPop);
                }
            }

            // Add cell
            //This code will add the cell and the predefined ConfigCell already has the molecules needed
            ConfigCell configCell = findCell("chemotactic with static receptor", protocol);
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
            cellPop.cellPopDist.Initialize();
            // Don't start the cell on a lattice point, until gradient interpolation method improves.
            cellPop.CellStates[0] = new CellState(envHandle.extent_x - 2 * configCell.CellRadius - envHandle.gridstep / 2,
                                                  envHandle.extent_y / 2 - envHandle.gridstep / 2,
                                                  envHandle.extent_z / 2 - envHandle.gridstep / 2);
            ((TissueScenario)protocol.scenario).cellpopulations.Add(cellPop);
            cellPop.report_xvf.position = true;
            cellPop.report_xvf.velocity = true;
            cellPop.report_xvf.force = true;

            //rendering
            ((TissueScenario)protocol.scenario).popOptions.AddRenderOptions(cellPop.renderLabel, cellPop.cellpopulation_name, true);

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

            protocol.reporter_file_name = "Loco_test";

            //EXTERNAL REACTIONS - I.E. IN EXTRACELLULAR SPACE
            item = new string[2] {"CXCL13 + CXCR5| -> CXCL13:CXCR5|",
                                  "CXCL13:CXCR5| -> CXCL13 + CXCR5|"};
            ConfigReaction reac;
            for (int i = 0; i < item.Length; i++)
            {
                reac = findReaction(item[i], protocol);
                if (reac != null)
                {
                    protocol.scenario.environment.comp.Reactions.Add(reac.Clone(true));
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

            protocol.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor;

            protocol.InitializeStorageClasses();

            //Load needed entities from User Store 
            Level userstore = new Level("Config\\Stores\\userstore.json", "Config\\Stores\\temp_userstore.json");
            userstore = userstore.Deserialize();

            // Load reaction templates from userstore
            LoadProtocolReactionTemplates(protocol, userstore);

            ConfigECSEnvironment envHandle = (ConfigECSEnvironment)protocol.scenario.environment;

            // Experiment
            protocol.experiment_name = "Diffusion Scenario";
            protocol.experiment_description = "CXCL13 diffusion in the ECM. No cells. Initial distribution is Gaussian. No flux BCs.";
            protocol.scenario.time_config.duration = 2.0;
            protocol.scenario.time_config.rendering_interval = 0.2;
            protocol.scenario.time_config.sampling_interval = 0.2;
            protocol.scenario.time_config.integrator_step = 0.001;

            envHandle.extent_x = 200;
            envHandle.extent_y = 200;
            envHandle.extent_z = 200;
            envHandle.gridstep = 10;
            
            //MOLECULES
            string[] item = new string[1] { "CXCL13" };
            int itemsLoaded = LoadProtocolMolecules(protocol, item, MoleculeLocation.Bulk, userstore);
            if (itemsLoaded != item.Length)
            {
                System.Windows.MessageBox.Show("Unable to load all protocol bulk molecules.");
            }

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

                //rendering
                ((TissueScenario)protocol.scenario).popOptions.AddRenderOptions(configMolPop.renderLabel, configMolPop.Name, false);
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

            protocol.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor;

            protocol.InitializeStorageClasses();

            // Load reaction templates from userstore
            Level userstore = new Level("Config\\Stores\\userstore.json", "Config\\Stores\\temp_userstore.json");
            userstore = userstore.Deserialize();
            LoadProtocolReactionTemplates(protocol, userstore);

            // Experiment
            protocol.experiment_name = "Blank Tissue Simulation";
            protocol.experiment_description = "";
            protocol.scenario.time_config.duration = 100;
            protocol.scenario.time_config.rendering_interval = 1.0;
            protocol.scenario.time_config.sampling_interval = 100;
            protocol.scenario.time_config.integrator_step = 0.001;
        }

        public static void CreateVatRC_Blank_Protocol(Protocol protocol)
        {
            if (protocol.CheckScenarioType(Protocol.ScenarioType.VAT_REACTION_COMPLEX) == false)
            {
                throw new InvalidCastException();
            }

            protocol.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor;

            protocol.InitializeStorageClasses();

            Level userstore = new Level("Config\\Stores\\userstore.json", "Config\\Stores\\temp_userstore.json");
            userstore = userstore.Deserialize();

            // Load reaction templates from userstore
            LoadProtocolReactionTemplates(protocol, userstore);

            ConfigPointEnvironment envHandle = (ConfigPointEnvironment)protocol.scenario.environment;

            // Experiment
            protocol.experiment_name = "Blank Vat RC";
            protocol.experiment_description = "...";
            protocol.scenario.time_config.duration = 2.0;
            protocol.scenario.time_config.rendering_interval = 0.2;
            protocol.scenario.time_config.sampling_interval = 0.2;
            protocol.scenario.time_config.integrator_step = 0.001;
        }

        public static void CreateVatRC_LigandReceptor_Protocol(Protocol protocol)
        {
            if (protocol.CheckScenarioType(Protocol.ScenarioType.VAT_REACTION_COMPLEX) == false)
            {
                throw new InvalidCastException();
            }

            protocol.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor;

            protocol.InitializeStorageClasses();

            Level userstore = new Level("Config\\Stores\\userstore.json", "Config\\Stores\\temp_userstore.json");
            userstore = userstore.Deserialize();

            // Load reaction templates from userstore
            LoadProtocolReactionTemplates(protocol, userstore);

            ConfigPointEnvironment envHandle = (ConfigPointEnvironment)protocol.scenario.environment;

            // Experiment
            protocol.experiment_description = "chemotactic receptor/ligand binding";
            protocol.scenario.time_config.duration = 1.0;
            protocol.scenario.time_config.rendering_interval = 0.02;
            protocol.scenario.time_config.sampling_interval = 0.02;
            protocol.scenario.time_config.integrator_step = 0.001;

            // bulk molecules
            string[] item = new string[] { "CXCL13", "CXCR5", "CXCL13:CXCR5", "CXCL12", "CXCR4", "CXCL12:CXCR4" };
            int itemsLoaded = LoadProtocolMolecules(protocol, item, MoleculeLocation.Bulk, userstore);
            if (itemsLoaded != item.Length)
            {
                System.Windows.MessageBox.Show("Unable to load all protocol bulk molecules.");
            }

            // reactions
            item = new string[] {"CXCL13 + CXCR5 -> CXCL13:CXCR5",
                                  "CXCL13:CXCR5 -> CXCL13 + CXCR5",
                                  "CXCL12 + CXCR4 -> CXCL12:CXCR4",
                                  "CXCL12:CXCR4 -> CXCL12 + CXCR4" };
            itemsLoaded = LoadProtocolReactions(protocol, item, userstore);
            if (itemsLoaded != item.Length)
            {
                System.Windows.MessageBox.Show("Unable to load all protocol reactions.");
            }

            // RC
            item = new string[] { "CXCL13/CXCR5 binding", "CXCL12/CXCR4 binding" };
            itemsLoaded = LoadProtocolRCs(protocol, item, userstore);
            if (itemsLoaded != item.Count())
            {
                System.Windows.MessageBox.Show("Unable to load all reaction complexes.");
            }

            //This adds RC to envHandle.comp
            for (int i = 0; i < item.Length; i++)
            {
                foreach (ConfigReactionComplex ent in protocol.entity_repository.reaction_complexes)
                {
                    if (ent.Name == item[i])
                    {
                        envHandle.comp.reaction_complexes.Add(ent.Clone(true));

                        //add mol pop display options
                        foreach (ConfigMolecularPopulation molpop in ent.molpops)
                        {
                            ((VatReactionComplexScenario)protocol.scenario).popOptions.AddRenderOptions(molpop.renderLabel, molpop.Name, false);
                        }
                        foreach (RenderPop rp in ((VatReactionComplexScenario)protocol.scenario).popOptions.molPopOptions)
                        {
                            rp.renderOn = true;
                        }
                        
                        break;
                    }
                }
            }

            protocol.reporter_file_name = "Vat_LigandReceptor";
        }

        //public static void CreateVatRC_LigandReceptor_Protocol(Protocol protocol)
        //{
        //    if (protocol.CheckScenarioType(Protocol.ScenarioType.VAT_REACTION_COMPLEX) == false)
        //    {
        //        throw new InvalidCastException();
        //    }

        //    ConfigPointEnvironment envHandle = (ConfigPointEnvironment)protocol.scenario.environment;

        //    // Experiment
        //    protocol.experiment_description = "CXCL13/CXCR5 binding";
        //    protocol.scenario.time_config.duration = 1.0;
        //    protocol.scenario.time_config.rendering_interval = 0.02;
        //    protocol.scenario.time_config.sampling_interval = 0.02;
        //    protocol.scenario.time_config.integrator_step = 0.001;

        //    //Load needed entities from User Store
        //    Level userstore = new Level("Config\\Stores\\userstore.json", "Config\\Stores\\temp_userstore.json");
        //    userstore = userstore.Deserialize();

        //    // bulk molecules
        //    string[] item = new string[3] { "CXCL13", "CXCR5", "CXCL13:CXCR5" };
        //    int itemsLoaded = LoadProtocolMolecules(protocol, item, MoleculeLocation.Bulk, userstore);
        //    if (itemsLoaded != item.Length)
        //    {
        //        System.Windows.MessageBox.Show("Unable to load all protocol bulk molecules.");
        //    }

        //    // reactions
        //    item = new string[2] {"CXCL13 + CXCR5 -> CXCL13:CXCR5",
        //                          "CXCL13:CXCR5 -> CXCL13 + CXCR5"};
        //    itemsLoaded = LoadProtocolReactionsAndTemplates(protocol, item, userstore);
        //    if (itemsLoaded != item.Length)
        //    {
        //        System.Windows.MessageBox.Show("Unable to load all protocol reactions.");
        //    }

        //    // RC
        //    item = new string[1] { "Ligand/Receptor" };
        //    itemsLoaded = LoadProtocolRCs(protocol, item, userstore);
            
        //    //This adds RC to envHandle.comp
        //    for (int i = 0; i < item.Length; i++)
        //    {
        //        foreach (ConfigReactionComplex ent in protocol.entity_repository.reaction_complexes)
        //        {
        //            if (ent.Name == item[i])
        //            {
        //                envHandle.comp.reaction_complexes.Add(ent.Clone(true));
        //                break;
        //            }
        //        }
        //    }

        //    protocol.reporter_file_name = "Vat";
        //}

        public static void CreateVatRC_TwoSiteAbBinding_Protocol(Protocol protocol)
        {
            if (protocol.CheckScenarioType(Protocol.ScenarioType.VAT_REACTION_COMPLEX) == false)
            {
                throw new InvalidCastException();
            }

            protocol.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor;

            protocol.InitializeStorageClasses();

            //Load needed entities from User Store
            Level userstore = new Level("Config\\Stores\\userstore.json", "Config\\Stores\\temp_userstore.json");
            userstore = userstore.Deserialize();

            // Load reaction templates
            LoadProtocolReactionTemplates(protocol, userstore);

            ConfigPointEnvironment envHandle = (ConfigPointEnvironment)protocol.scenario.environment;

            // Experiment
            protocol.experiment_name = "SPR Vat RC";
            protocol.experiment_description = "Simulation of two-state receptor binding to ligand.";
            protocol.scenario.time_config.duration = 100.0;
            protocol.scenario.time_config.rendering_interval = 2.0;
            protocol.scenario.time_config.sampling_interval = 1.0;
            protocol.scenario.time_config.integrator_step = 0.001;

            ConfigReactionComplex configRC = new ConfigReactionComplex("TwoSiteAbBinding");

            // Create new molecules and add to the protocol ER
            // Don't want to make these more permanent by adding to the user store
            //

            string[] item = new string[] { "R1", "R2", "L", "C1", "C2" };
            foreach (string s in item)
            {
                ConfigMolecule cm = new ConfigMolecule(s, 1.0, 1.0, 1.0);
                protocol.entity_repository.molecules.Add(cm);
            }

            // Create new reactions and add to the protocol ER
            // Don't want to make these more permanent by adding to the user store
            //

            // Association
            // R1 + L -> C1
            ConfigReaction cr = new ConfigReaction();
            cr.reaction_template_guid_ref = protocol.findReactionTemplateGuid(ReactionType.Association);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("R1", MoleculeLocation.Bulk, protocol));
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("L", MoleculeLocation.Bulk, protocol));
            // product
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("C1", MoleculeLocation.Bulk, protocol));
            cr.rate_const = 0.1;
            cr.GetTotalReactionString(protocol.entity_repository);
            protocol.entity_repository.reactions.Add(cr);

            // Association
            // R2 + L -> C2
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = protocol.findReactionTemplateGuid(ReactionType.Association);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("R2", MoleculeLocation.Bulk, protocol));
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("L", MoleculeLocation.Bulk, protocol));
            // product
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("C2", MoleculeLocation.Bulk, protocol));
            cr.rate_const = 0.1;
            cr.GetTotalReactionString(protocol.entity_repository);
            protocol.entity_repository.reactions.Add(cr);

            // Dissociation
            // C1 -> R1 + L
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = protocol.findReactionTemplateGuid(ReactionType.Dissociation);
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("R1", MoleculeLocation.Bulk, protocol));
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("L", MoleculeLocation.Bulk, protocol));
            // reactant
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("C1", MoleculeLocation.Bulk, protocol));
            cr.rate_const = 0.001;
            cr.GetTotalReactionString(protocol.entity_repository);
            protocol.entity_repository.reactions.Add(cr);

            // Dissociation
            // C2 -> R2 + L
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = protocol.findReactionTemplateGuid(ReactionType.Dissociation);
            // product
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("R2", MoleculeLocation.Bulk, protocol));
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("L", MoleculeLocation.Bulk, protocol));
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("C2", MoleculeLocation.Bulk, protocol));
            cr.rate_const = 0.01;
            cr.GetTotalReactionString(protocol.entity_repository);
            protocol.entity_repository.reactions.Add(cr);

            // Transformation
            // R1 -> R2
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = protocol.findReactionTemplateGuid(ReactionType.Transformation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("R1", MoleculeLocation.Bulk, protocol));
            // product
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("R2", MoleculeLocation.Bulk, protocol));
            cr.rate_const = 0.001;
            cr.GetTotalReactionString(protocol.entity_repository);
            protocol.entity_repository.reactions.Add(cr);

            // Transformation
            // R2 -> R1
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = protocol.findReactionTemplateGuid(ReactionType.Transformation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("R2", MoleculeLocation.Bulk, protocol));
            // product
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("R1", MoleculeLocation.Bulk, protocol));
            cr.rate_const = 0.001;
            cr.GetTotalReactionString(protocol.entity_repository);
            protocol.entity_repository.reactions.Add(cr);

            // Transformation
            // C1 -> C2
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = protocol.findReactionTemplateGuid(ReactionType.Transformation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("C1", MoleculeLocation.Bulk, protocol));
            // product
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("C2", MoleculeLocation.Bulk, protocol));
            cr.rate_const = 0.001;
            cr.GetTotalReactionString(protocol.entity_repository);
            protocol.entity_repository.reactions.Add(cr);

            // Transformation
            // C2 -> C1
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = protocol.findReactionTemplateGuid(ReactionType.Transformation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("C2", MoleculeLocation.Bulk, protocol));
            // product
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("C1", MoleculeLocation.Bulk, protocol));
            cr.rate_const = 0.001;
            cr.GetTotalReactionString(protocol.entity_repository);
            protocol.entity_repository.reactions.Add(cr);

            protocol.InitializeStorageClasses();
            
            // Add ER items to config reaction complex
            //

            // Push all ER reactions to Reaction Complex entity
            foreach (ConfigReaction configReac in protocol.entity_repository.reactions)
            {
                configRC.reactions.Add(configReac);
            }

            // Push all ER molecules to the Reaction Complex molecules dictionary
            foreach (KeyValuePair<string,ConfigMolecule> kvp in protocol.entity_repository.molecules_dict)
            {
                configRC.molecules_dict.Add(kvp.Key, kvp.Value);
            }

            // Create molecular populations and add to Reaction Complex
            double[] conc = new double[] { 1, 1, 1, 0, 0 };
            item = new string[] { "R1", "R2", "L", "C1", "C2" };

            for (int i = 0; i < item.Length; i++)
            {
                //ConfigMolecule configMolecule = protocol.entity_repository.molecules_dict[findMoleculeGuid(item[i], MoleculeLocation.Bulk, protocol)];
                ConfigMolecule configMolecule = configRC.molecules_dict[findMoleculeGuid(item[i], MoleculeLocation.Bulk, protocol)];

                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.CELL_MP);

                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    configMolPop.mp_distribution = hl;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.NONE;

                    configRC.molpops.Add(configMolPop);

                    //add mol pop display options
                    ((VatReactionComplexScenario)protocol.scenario).popOptions.AddRenderOptions(configMolPop.renderLabel, configMolPop.Name, false);                    
                }
            }

            foreach (RenderPop rp in ((VatReactionComplexScenario)protocol.scenario).popOptions.molPopOptions)
            {
                rp.renderOn = true;
            }

            // Add items to config compartment
            //

            envHandle.comp.reaction_complexes.Add(configRC);
            //envHandle.comp.reaction_complexes_dict.Add(configRC.entity_guid, configRC);

            protocol.reporter_file_name = "Vat_2site_ab_binding";

        }


        private static void PredefinedCellsCreator(Level store)
        {
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

            /////////////////////////////
            // stochastic locomotion
            /////////////////////////////

            gc = new ConfigCell();
            gc.CellName = "stochastic locomotion";
            gc.CellRadius = 5.0;

            gc.description = string.Format("{0}{1}", gc.description, "This cell has a 10 um diameter and undergoes stochastic locomotion.  ");

            gc.description = string.Format("{0}{1}\n", gc.description, "The cell has the following behaviors: ");
            gc.description = string.Format("{0}{1}\n", gc.description, "Differentiation states: none");
            gc.description = string.Format("{0}{1}\n", gc.description, "Division (cell cycle) states: no cell division");
            gc.description = string.Format("{0}{1}\n", gc.description, "Death: none");

            gc.DragCoefficient = new DistributedParameter(1.0);
            gc.TransductionConstant = new DistributedParameter(100);
            gc.Sigma = new DistributedParameter(4.0);

            store.entity_repository.cells.Add(gc);

            /////////////////////////////////////////////////////////////////////////////////////
            // chemotactic with static receptor
            //////////////////////////////////////////////////////////////////////////////////////

            gc = new ConfigCell();
            gc.CellName = "chemotactic with static receptor";
            gc.CellRadius = 5.0;

            gc.description = string.Format("{0}{1}\n", gc.description, "This cell has a 10 um diameter and undergoes stochastic locomotion.  ");

            gc.description = string.Format("{0}{1}", gc.description, "The plasma membrane has 14,000 CXCR4 and CXCR5 receptors (Barroso2012), ");
            gc.description = string.Format("{0}{1}\n", gc.description, "corresponding to a concentration of 44 molec/um^2 for each. ");

            gc.description = string.Format("{0}{1}", gc.description, "The cell moves chemotactically in the presence of CXCL12 or CXCL13 gradients. ");
            gc.description = string.Format("{0}{1}", gc.description, "The (pseudo) molecule A* is the chemotaxis 'driver': the chemotactic force is proportional to the gradient of [A*], ");
            gc.description = string.Format("{0}{1}\n", gc.description, "which is induced by gradients in bound chemokine receptors (CXCL12:CXCR4| and CXCL13:CXCR5|). ");

            gc.description = string.Format("{0}{1}\n", gc.description, "The cell has the following behaviors: ");
            gc.description = string.Format("{0}{1}\n", gc.description, "Differentiation states: none");
            gc.description = string.Format("{0}{1}\n", gc.description, "Division (cell cycle) states: no cell division");
            gc.description = string.Format("{0}{1}\n", gc.description, "Death: none");

            //MOLECULES IN MEMBRANE
            conc = new double[] { cxcr5Conc_5umRadius, 0, cxcr5Conc_5umRadius, 0 };
            type = new string[] { "CXCR5|", "CXCL13:CXCR5|", "CXCR4|", "CXCL12:CXCR4|" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.membrane.molpops.Add(gmp);
                }
            }

            //MOLECULES IN Cytosol
            conc = new double[] { 250, 0, };
            type = new string[] { "A", "A*" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.cytosol.molpops.Add(gmp);
                }
            }
            gc.locomotor_mol_guid_ref = findMoleculeGuid("A*", MoleculeLocation.Bulk, store);

            // Reactions in Cytosol
            type = new string[] { "A + CXCL13:CXCR5| -> A* + CXCL13:CXCR5|", "A + CXCL12:CXCR4| -> A* + CXCL12:CXCR4|", "A* -> A" };
                                          //"A* -> A", "gApop -> sApop + gApop" };
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], store);
                if (reac != null)
                {
                    gc.cytosol.Reactions.Add(reac.Clone(true));
                }
            }

            gc.DragCoefficient = new DistributedParameter(1.0);
            gc.TransductionConstant = new DistributedParameter(100.0);
            gc.Sigma = new DistributedParameter(4.0);

            store.entity_repository.cells.Add(gc);

            /////////////////////////////////////////////////////////////////////////////////////////////
            // chemotactic with receptor homeostasis
            //////////////////////////////////////////////////////////////////////////////////////////////

            gc = new ConfigCell();
            gc.CellName = "chemotactic with receptor homeostasis";
            gc.CellRadius = 5.0;

            gc.description = string.Format("{0}{1}\n", gc.description, "This cell has a 10 um diameter and undergoes stochastic locomotion.  ");

            gc.description = string.Format("{0}{1}", gc.description, "The plasma membrane has CXCR4 and CXCR5 receptors which are homeostatically maintained ");
            gc.description = string.Format("{0}{1}", gc.description, "through gene transcription, transport to the plasma membrane, internalization, and degrdation. ");
            gc.description = string.Format("{0}{1}", gc.description, "CXCR4 and CXCR5 production rates are based on data from de Guinoa et al. (2011). ");
            gc.description = string.Format("{0}{1}\n", gc.description, "CXCR4 and CXCR5 receptor interanlization rates are based on data from Barrosso et al. (2012) and Hesselgesser et al. (1998). ");

            gc.description = string.Format("{0}{1}", gc.description, "The cell moves chemotactically in the presence of CXCL12 or CXCL13 gradients. ");
            gc.description = string.Format("{0}{1}", gc.description, "The (pseudo) molecule A* is the chemotaxis 'driver': the chemotactic force is proportional to the gradient of [A*], ");
            gc.description = string.Format("{0}{1}\n", gc.description, "which is induced by gradients in bound chemokine receptors (CXCL12:CXCR4| and CXCL13:CXCR5|). ");

            gc.description = string.Format("{0}{1}\n", gc.description, "The cell has the following behaviors: ");
            gc.description = string.Format("{0}{1}\n", gc.description, "Differentiation states: none");
            gc.description = string.Format("{0}{1}\n", gc.description, "Division (cell cycle) states: no cell division");
            gc.description = string.Format("{0}{1}\n", gc.description, "Death: none");

            //MOLECULES IN MEMBRANE
            conc = new double[] { cxcr5Conc_5umRadius, 0, cxcr5Conc_5umRadius, 0 };
            type = new string[] { "CXCR5|", "CXCL13:CXCR5|", "CXCR4|", "CXCL12:CXCR4|" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.membrane.molpops.Add(gmp);
                }
            }

            //MOLECULES IN Cytosol
            conc = new double[] { 250,  0,     0,        0,              0,          0};
            type = new string[] { "A", "A*", "CXCR5", "CXCL13:CXCR5", "CXCR4", "CXCL12:CXCR4" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.cytosol.molpops.Add(gmp);
                }
            }
            gc.locomotor_mol_guid_ref = findMoleculeGuid("A*", MoleculeLocation.Bulk, store);

            // Add genes
            type = new string[] { "gCXCR4", "gCXCR5" };
            for (int i = 0; i < type.Length; i++)
            {
                gc.genes.Add(findGene(type[i], store));
            }

            // Reactions in Cytosol
            type = new string[] {"A + CXCL13:CXCR5| -> A* + CXCL13:CXCR5|", 
                                  "A + CXCL12:CXCR4| -> A* + CXCL12:CXCR4|",
                                  "A* -> A",
                                  "gCXCR5 -> CXCR5 + gCXCR5",
                                  "CXCR5 -> CXCR5|",
                                  "CXCR5| -> CXCR5",
                                  "CXCR5 ->",
                                  "CXCL13:CXCR5| -> CXCL13:CXCR5", 
                                  "CXCL13:CXCR5 ->",
                                   "gCXCR4 -> CXCR4 + gCXCR4",
                                  "CXCR4 -> CXCR4|",
                                  "CXCR4| -> CXCR4",
                                  "CXCR4 ->",
                                  "CXCL12:CXCR4| -> CXCL12:CXCR4", 
                                  "CXCL12:CXCR4 ->",
                                };
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], store);
                if (reac != null)
                {
                    gc.cytosol.Reactions.Add(reac.Clone(true));
                }
            }

            gc.DragCoefficient = new DistributedParameter(1.0);
            gc.TransductionConstant = new DistributedParameter(100.0);
            gc.Sigma = new DistributedParameter(4.0);

            store.entity_repository.cells.Add(gc);

            /////////////////////////////////////////
            // centroblast-centrocyte recycling
            ////////////////////////////////////////

            gc = new ConfigCell();
            gc.CellName = "centroblast-centrocyte recycling";
            gc.CellRadius = 5.0;

            gc.description = string.Format("{0}{1}\n", gc.description, "This cell has a 10 um diameter and undergoes stochastic locomotion.  ");

            gc.description = string.Format("{0}{1}", gc.description, "The plasma membrane has CXCR4 and CXCR5 receptors which are homeostatically maintained ");
            gc.description = string.Format("{0}{1}", gc.description, "through gene transcription, transport to the plasma membrane, internalization, and degrdation. ");
            gc.description = string.Format("{0}{1}", gc.description, "CXCR4 and CXCR5 production rates are based on data from de Guinoa et al. (2011). ");
            gc.description = string.Format("{0}{1}\n", gc.description, "CXCR4 and CXCR5 receptor interanlization rates are based on data from Barrosso et al. (2012) and Hesselgesser et al. (1998). ");

            gc.description = string.Format("{0}{1}", gc.description, "The cell moves chemotactically in the presence of CXCL12 or CXCL13 gradients. ");
            gc.description = string.Format("{0}{1}", gc.description, "The (pseudo) molecule A* is the chemotaxis 'driver': the chemotactic force is proportional to the gradient of [A*], ");
            gc.description = string.Format("{0}{1}\n", gc.description, "which is induced by gradients in bound chemokine receptors (CXCL12:CXCR4| and CXCL13:CXCR5|). ");

            gc.description = string.Format("{0}{1}\n", gc.description, "The cell has the following behaviors: ");
            gc.description = string.Format("{0}{1}\n", gc.description, "Differentiation states: centroblast, centrocyte");
            gc.description = string.Format("{0}{1}\n", gc.description, "Division (cell cycle) states: no cell division");
            gc.description = string.Format("{0}{1}\n", gc.description, "Death: none");

            //MOLECULES IN MEMBRANE
            conc = new double[] { 0, 0, 0, 0 };
            type = new string[] { "CXCR4|", "CXCR5|", "CXCL12:CXCR4|", "CXCL13:CXCR5|" };

            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.membrane.molpops.Add(gmp);
                }
            }

            //MOLECULES IN Cytosol
            conc = new double[] { 250, 0, 0, 0, 0, 0 };
            type = new string[] { "A", "A*", "CXCR4", "CXCR5", "CXCL12:CXCR4", "CXCL13:CXCR5" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.cytosol.molpops.Add(gmp);
                }
            }

            // Add genes
            type = new string[] { "gCXCR4", "gCXCR5" };
            for (int i = 0; i < type.Length; i++)
            {
                gc.genes.Add(findGene(type[i], store));
            }

            // Reactions in Cytosol
            type = new string[] { "A + CXCL12:CXCR4| -> A* + CXCL12:CXCR4|", "A + CXCL13:CXCR5| -> A* + CXCL13:CXCR5|", "A* -> A", 
                                  "gCXCR4 -> CXCR4 + gCXCR4", 
                                  "CXCR4 ->", "CXCR4 -> CXCR4|", 
                                  "CXCR4| -> CXCR4",
                                  "CXCL12:CXCR4| -> CXCL12:CXCR4",  
                                  "CXCL12:CXCR4 ->",
                                  "gCXCR5 -> CXCR5 + gCXCR5", 
                                  "CXCR5 ->", "CXCR5 -> CXCR5|", 
                                  "CXCR5| -> CXCR5",
                                  "CXCL13:CXCR5| -> CXCL13:CXCR5",  
                                  "CXCL13:CXCR5 ->" };
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], store);
                if (reac != null)
                {
                    gc.cytosol.Reactions.Add(reac.Clone(true));
                }
            }

            string guid = findDiffSchemeGuid("cycling cb-cc diff scheme", store);
            if (store.entity_repository.diff_schemes_dict.ContainsKey(guid) == true)
            {
                gc.diff_scheme = store.entity_repository.diff_schemes_dict[guid].Clone(true);
            }

            gc.locomotor_mol_guid_ref = findMoleculeGuid("A*", MoleculeLocation.Bulk, store);
            gc.DragCoefficient = new DistributedParameter(1.0);
            gc.TransductionConstant = new DistributedParameter(100.0);
            gc.Sigma = new DistributedParameter(4.0);

            store.entity_repository.cells.Add(gc);


            //////////////////////////////////////////////////////////////////////////////////
            // simple Germinal Center B cell
            // 
            // states: activated, pre-centroblast, centroblast, centrocyte, rescued, apoptotic       
            //////////////////////////////////////////////////////////////////////////////////
    
            gc = new ConfigCell();
            gc.CellName = "simple Germinal Center B cell";
            gc.CellRadius = 5.0;

            gc.description = string.Format("{0}{1}\n", gc.description, "This cell has a 10 um diameter and undergoes stochastic locomotion.  ");

            gc.description = string.Format("{0}{1}", gc.description, "The plasma membrane has CXCR4 and CXCR5 receptors which are homeostatically maintained ");
            gc.description = string.Format("{0}{1}", gc.description, "through gene transcription, transport to the plasma membrane, internalization, and degrdation. ");
            gc.description = string.Format("{0}{1}", gc.description, "CXCR4 and CXCR5 production rates are based on data from de Guinoa et al. (2011). ");
            gc.description = string.Format("{0}{1}\n", gc.description, "CXCR4 and CXCR5 receptor interanlization rates are based on data from Barrosso et al. (2012) and Hesselgesser et al. (1998). ");

            gc.description = string.Format("{0}{1}", gc.description, "The cell moves chemotactically in the presence of CXCL12 or CXCL13 gradients. ");
            gc.description = string.Format("{0}{1}", gc.description, "The (pseudo) molecule A* is the chemotaxis 'driver': the chemotactic force is proportional to the gradient of [A*], ");
            gc.description = string.Format("{0}{1}\n", gc.description, "which is induced by gradients in bound chemokine receptors (CXCL12:CXCR4| and CXCL13:CXCR5|). ");

            gc.description = string.Format("{0}{1}\n", gc.description, "The cell has the following behaviors: ");
            gc.description = string.Format("{0}{1}\n", gc.description, "Differentiation states: activated, pre-centroblast, centroblast, centrocyte, rescued, apoptotic");
            gc.description = string.Format("{0}{1}\n", gc.description, "Division (cell cycle) states: G0, G1, S, G2-M, cytokinetic");
            gc.description = string.Format("{0}{1}\n", gc.description, "Death (after reaching apoptotic state)");

            //MOLECULES IN MEMBRANE
            conc = new double[4] { 0, 0, 0, 0 };
            type = new string[4] { "CXCR5|", "CXCL13:CXCR5|", "CXCR4|", "CXCL12:CXCR4|" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.membrane.molpops.Add(gmp);
                }
            }

            //MOLECULES IN Cytosol
            type = new string[] {   "A", "A*", 
                                    "CXCR4", "CXCR5", "CXCL12:CXCR4", "CXCL13:CXCR5",
                                    "W", "Wp", "E1", "E2", "W:E1", "Wp:E2",
                                    "sDif1", "sApop" };
            conc = new double[type.Count()];
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.cytosol.molpops.Add(gmp);
                }
            }

            // Genes
            type = new string[] { "gCXCR4", "gCXCR5", "gW", "gE1", "gE2", "gDif1", "gA", "gApop" };
            for (int i = 0; i < type.Length; i++)
            {
                gc.genes.Add(findGene(type[i], store));
            }

            // Reactions in Cytosol
            type = new string[] { "gApop -> sApop + gApop", "sApop ->" };
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], store);
                if (reac != null)
                {
                    gc.cytosol.Reactions.Add(reac.Clone(true));
                }
            }

            // CYTOSOL REACTION COMPLEXES 
            type = new string[] {   "CXCR5 receptor production and recycling", 
                                    "CXCR4 receptor production and recycling", 
                                    "GC B cell chemotaxis: cytosol reactions",
                                    "Goldbeter-Koshland switch reactions",
                                    "Goldbeter-Koshland molecule homeostasis"
                                };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigReactionComplex crc = findReactionComplexByName(type[i], store);
                if (crc != null)
                {
                    gc.cytosol.reaction_complexes.Add(crc.Clone(true));
                }
            }

            // CELL MOTILITY
            gc.locomotor_mol_guid_ref = findMoleculeGuid("A*", MoleculeLocation.Bulk, store);
            gc.DragCoefficient = new DistributedParameter(1.0);
            gc.TransductionConstant = new DistributedParameter(100.0);
            gc.Sigma = new DistributedParameter(4.0);

            // DIFFERENTIATOR
            guid = findDiffSchemeGuid("GC B cell differentiation scheme", store);
            if (store.entity_repository.diff_schemes_dict.ContainsKey(guid) == true)
            {
                gc.diff_scheme = store.entity_repository.diff_schemes_dict[guid].Clone(true);
            }

            // DIVISION
            guid = findDiffSchemeGuid("GC B cell division scheme", store);
            if (store.entity_repository.diff_schemes_dict.ContainsKey(guid) == true)
            {
                gc.div_scheme = store.entity_repository.diff_schemes_dict[guid].Clone(true);
            }

            // DEATH DRIVER
            guid = findTransitionDriverGuid("GC B cell apoptosis", store);
            if (store.entity_repository.transition_drivers_dict.ContainsKey(guid) == true)
            {
                gc.death_driver = store.entity_repository.transition_drivers_dict[guid].Clone(true);
            }

            store.entity_repository.cells.Add(gc);


            ////////////////////////////////
            // simple germinal center T cell
            ///////////////////////////////

            gc = new ConfigCell();
            gc.CellName = "simple germinal center T cell";
            gc.CellRadius = 5.0;


            gc.description = string.Format("{0}{1}\n", gc.description, "This cell has a 10 um diameter and undergoes stochastic locomotion.  ");

            gc.description = string.Format("{0}{1}", gc.description, "The plasma membrane has fixed numbers of T cell receptor (TCR) and CXCR5 receptors.  ");
            gc.description = string.Format("{0}{1}\n", gc.description, "Chemotactic receptor (CXCR5) and T cell receptor (TCR) total molecular concentrations are fixed. ");

            gc.description = string.Format("{0}{1}", gc.description, "The cell moves chemotactically in the presence of CXCL13 gradients. ");
            gc.description = string.Format("{0}{1}", gc.description, "The (pseudo) molecule A* is the chemotaxis 'driver': the chemotactic force is proportional to the gradient of [A*], ");
            gc.description = string.Format("{0}{1}\n", gc.description, "which is induced by gradients in bound chemokine receptor CXCL13:CXCR5|. ");

            gc.description = string.Format("{0}{1}\n", gc.description, "The cell has the following behaviors: ");
            gc.description = string.Format("{0}{1}\n", gc.description, "Differentiation states: none");
            gc.description = string.Format("{0}{1}\n", gc.description, "Division (cell cycle) states: no cell division");
            gc.description = string.Format("{0}{1}\n", gc.description, "Death: none");


            gc.description = gc.description + "TCR density based on data from Hessengesser 2013.";

            //MOLECULES IN MEMBRANE
            conc = new double[] { 50, 0, 300 };
            type = new string[] { "CXCR5|", "CXCL13:CXCR5|", "TCR|" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.membrane.molpops.Add(gmp);
                }
            }

            //MOLECULES IN Cytosol
            conc = new double[] { 250, 0 };
            type = new string[] { "A", "A*" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mp_distribution = hl;
                    gc.cytosol.molpops.Add(gmp);
                }
            }
            gc.locomotor_mol_guid_ref = findMoleculeGuid("A*", MoleculeLocation.Bulk, store);

            // Reactions in Cytosol
            type = new string[] { "A + CXCL13:CXCR5| -> A* + CXCL13:CXCR5|", "A* -> A" };
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], store);
                if (reac != null)
                {
                    gc.cytosol.Reactions.Add(reac.Clone(true));
                }
            }

            gc.DragCoefficient = new DistributedParameter(1.0);
            gc.TransductionConstant = new DistributedParameter(100.0);
            gc.Sigma = new DistributedParameter(4.0);

            store.entity_repository.cells.Add(gc);

            ////////////////////////////////////
            // CXCL12-secreting
            ////////////////////////////////////

            gc = new ConfigCell();
            gc.CellName = "CXCL12-secreting";
            gc.CellRadius = 5.0;

            gc.description = string.Format("{0}{1}\n", gc.description, "This cell has a 10 um diameter, no locomotion, and secretes CXCL12. ");

            gc.description = string.Format("{0}{1}\n", gc.description, "The cell has the following behaviors: ");
            gc.description = string.Format("{0}{1}\n", gc.description, "Differentiation states: none");
            gc.description = string.Format("{0}{1}\n", gc.description, "Division (cell cycle) states: no cell division");
            gc.description = string.Format("{0}{1}\n", gc.description, "Death: none");

            //MOLECULES IN MEMBRANE
            conc = new double[1] { 0 };
            type = new string[1] { "CXCL12|" };

            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

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

            gc.DragCoefficient = new DistributedParameter(1.0);
            gc.TransductionConstant = new DistributedParameter(0.0);
            gc.Sigma = new DistributedParameter(0.0);

            store.entity_repository.cells.Add(gc);

            ////////////////////////////////
            // CXCL13-secreting
            ///////////////////////////////

            gc = new ConfigCell();
            gc.CellName = "CXCL13-secreting";
            gc.CellRadius = 5.0;

            gc.description = string.Format("{0}{1}\n", gc.description, "This cell has a 10 um diameter, no locomotion, and secretes CXCL13. ");

            gc.description = string.Format("{0}{1}\n", gc.description, "The cell has the following behaviors: ");
            gc.description = string.Format("{0}{1}\n", gc.description, "Differentiation states: none");
            gc.description = string.Format("{0}{1}\n", gc.description, "Division (cell cycle) states: no cell division");
            gc.description = string.Format("{0}{1}\n", gc.description, "Death: none");

            //MOLECULES IN MEMBRANE
            conc = new double[1] { 0 };
            type = new string[1] { "CXCL13|" };

            for (int i = 0; i < type.Length; i++)
            {
                cm = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, store)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    gmp.molecule = cm.Clone(null);
                    gmp.Name = cm.Name;

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

            gc.DragCoefficient = new DistributedParameter(1.0);
            gc.TransductionConstant = new DistributedParameter(0.0);
            gc.Sigma = new DistributedParameter(0.0);

            store.entity_repository.cells.Add(gc);

        }

        private static void PredefinedTransitionSchemesCreator(Level store)
        {
            // Generic death transition driver - move to PredefinedTransitionDriversCreator() ?
            // Cell cytoplasm must contain sApop molecular population
            ConfigTransitionDriver config_td = new ConfigTransitionDriver();
            config_td.Name = "generic apoptosis";
            string[] stateName = new string[] { "alive", "dead" };
            string[,] signal = new string[,] { { "", "sApop" }, { "", "" } };
            double[,] alpha = new double[,] { { 0, 0 }, { 0, 0 } };
            double[,] beta = new double[,] { { 0, 0.002 }, { 0, 0 } };
            LoadConfigTransitionDriverElements(config_td, signal, alpha, beta, stateName, store);
            config_td.StateName = config_td.states[(int)config_td.CurrentState.ConstValue];
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
            config_td.StateName = config_td.states[(int)config_td.CurrentState.ConstValue];
            store.entity_repository.transition_drivers.Add(config_td);
            store.entity_repository.transition_drivers_dict.Add(config_td.entity_guid, config_td);

            // These can be reused to create other differentiation schemes
            ConfigTransitionDriver driver;
            ConfigTransitionScheme diffScheme;
            ConfigActivationRow actRow;
            string[] stateNames, geneNames;
            double[,] activations; //, alpha, beta;
            //string[,] signal;

            //////////////////////////////
            //// B cell differentiatior 
            //////////////////////////////

            //stateNames = new string[] { "naive", "activated", "short-lived plasmacyte", "centroblast", "centrocyte", "long-lived plasmacyte", "memory" };
            //geneNames = new string[]             { "gCXCR4", "gCXCR5", "gIgH", "gIgL", "gIgS", "gAID", "gBL1", "gMHCII" };
            //activations = new double[,]          { { 0,        1,       1,      1,      0,       0,      0,      0 },  // naive
            //                                       { 0,        1,       1,      1,      0,       0,      1,      1 },  // activated
            //                                       { 0,        1,       1,      1,      1,       0,      0,      0 },  // slplasmacyte
            //                                       { 1,        0,       0,      0,      0,       1,      0,      0 },  // centroblast
            //                                       { 0,        1,       1,      1,      0,       0,      1,      1 },  // centrocyte
            //                                       { 0,        0,       1,      1,      1,       0,      0,      0 },  // llplasmacyte
            //                                       { 0,        0,       1,      1,      0,       0,      1,      1 },  // memory
            //                                    };
            ////                                     naive    act       slp      cb       cc    llp     memory
            //signal = new string[,]          {     { "",   "sDif1",    "",     "",      "",    "",       ""      },  // naive
            //                                       { "",     "",     "sDif2", "sDif3",  "",    "",       ""      },  // activated
            //                                       { "",     "",       "",      "",     "",    "",       ""      },  // slplasmacyte
            //                                       { "",     "",       "",      "",   "sDif4", "",       ""      },  // centroblast
            //                                       { "",     "",       "",   "sDif5",   "",   "sDif6",  "sDif7"  },  // centrocyte
            //                                       { "",     "",       "",      "",     "",    "",       ""     },  // llplasmacyte
            //                                       { "",     "",       "",      "",     "",    "",       ""     },  // memory
            //                                    };
            ////  no spontaneous transitions        naive    act       slp      cb       cc    llp     memory
            //alpha = new double[,]          {       { 0,     0,        0,       0,      0,    0,       0   },  // naive
            //                                       { 0,     0,        0,       0,      0,    0,       0   },  // activated
            //                                       { 0,     0,        0,       0,      0,    0,       0   },  // slplasmacyte
            //                                       { 0,     0,        0,       0,      0,    0,       0   },  // centroblast
            //                                       { 0,     0,        0,       0,      0,    0,       0   },  // centrocyte
            //                                       { 0,     0,        0,       0,      0,    0,       0   },  // llplasmacyte
            //                                       { 0,     0,        0,       0,      0,    0,       0   },  // memory
            //                                    };
            ////  need better values here             naive    act       slp      cb       cc    llp     memory
            //beta = new double[,]           {       { 0,     1,        0,       0,      0,    0,       0   },  // naive
            //                                       { 0,     0,        1,       1,      0,    0,       0   },  // activated
            //                                       { 0,     0,        0,       0,      0,    0,       0   },  // slplasmacyte
            //                                       { 0,     0,        0,       0,      1,    0,       0   },  // centroblast
            //                                       { 0,     0,        0,       1,      0,    1,       1   },  // centrocyte
            //                                       { 0,     0,        0,       0,      0,    0,       0   },  // llplasmacyte
            //                                       { 0,     0,        0,       0,      0,    0,       0   },  // memory
            //                                    };

            //diffScheme = new ConfigTransitionScheme();
            //diffScheme.Name = "B cell 7 state";
            //driver = new ConfigTransitionDriver();
            //driver.Name = "B cell 7 state driver";
            //driver.CurrentState = new DistributedParameter(0);
            //driver.StateName = stateNames[(int)driver.CurrentState.Sample()];

            //// Attach transition driver to differentiation scheme
            //diffScheme.Driver = driver;

            //// Add genes
            //diffScheme.genes = new ObservableCollection<string>();
            //for (int j = 0; j < activations.GetLength(1); j++)
            //{
            //    diffScheme.genes.Add(findGeneGuid(geneNames[j], store));
            //}

            //// Add epigenetic map of genes and activations
            //diffScheme.activationRows = new ObservableCollection<ConfigActivationRow>();
            //for (int i = 0; i < activations.GetLength(0); i++)
            //{
            //    actRow = new ConfigActivationRow();
            //    for (int j = 0; j < activations.GetLength(1); j++)
            //    {
            //        actRow.activations.Add(activations[i, j]);
            //    }
            //    diffScheme.activationRows.Add(actRow);
            //}

            //// Add DriverElements to TransitionDriver
            //LoadConfigTransitionDriverElements(driver, signal, alpha, beta, stateNames, store);

            //// Add to Entity Repository
            //store.entity_repository.transition_drivers.Add(driver);
            //store.entity_repository.transition_drivers_dict.Add(driver.entity_guid, driver);
            //store.entity_repository.diff_schemes.Add(diffScheme);
            //store.entity_repository.diff_schemes_dict.Add(diffScheme.entity_guid, diffScheme);


            ////////////////////////////
            // GC B cell death
            ////////////////////////////
            
            config_td = new ConfigTransitionDriver();
            config_td.Name = "GC B cell apoptosis";
            stateName = new string[] { "alive", "dead" };
            signal = new string[2, 2];
            alpha = new double[2, 2];
            beta = new double[2, 2];
            // sApop equil value = 0.8
            // mean transition time = 1/(beta * equil_value)
            beta[0, 1] = 1.0;
            signal[0, 1] = "sApop";
            LoadConfigTransitionDriverElements(config_td, signal, alpha, beta, stateName, store);
            config_td.StateName = config_td.states[0];
            store.entity_repository.transition_drivers.Add(config_td.Clone(true));
            store.entity_repository.transition_drivers_dict.Add(config_td.entity_guid, config_td.Clone(true));

            ////////////////////////////
            // GC B cell differentiatior 
            ////////////////////////////

            diffScheme = new ConfigTransitionScheme();
            diffScheme.Name = "GC B cell differentiation scheme";

            stateNames = new string[] {  "activated", "pre-centroblast", "centroblast", "centrocyte", "rescued", "apoptotic" };
            geneNames = new string[] {             "gCXCR4", "gCXCR5", "gE1", "gW", "gE2",  "gDif1", "gApop", "gA" };
            activations = new double[,]          { { 0,        0,  6.4e-3,      0,      0,       0,     0 ,   0 },  // activated
                                                   { 0,        0,       0,      1,      1,       0,     0 ,   1 },  // pre-centroblast            
                                                   { 1,        0,       0,      0,      0,       0,     0 ,   0 },  // centroblast
                                                   { 0,        1,       0,      0,      0,       1,     0 ,   0 },  // centrocyte
                                                   { 0,        0,    3e-3,      0,      0,       0,     0 ,   0 },  // rescued
                                                   { 0,        0,       0,      0,      0,       0,     1 ,   0 },  // apoptotic
                                                };

            // Driver
            driver = new ConfigTransitionDriver();
            driver.Name = "GC B cell differentiation driver";
            driver.CurrentState = new DistributedParameter(0);
            driver.StateName = stateNames[0];
            // Molecule driver elements
            signal = new string[stateNames.Count(), stateNames.Count()];
            alpha = new double[stateNames.Count(), stateNames.Count()];
            beta = new double[stateNames.Count(), stateNames.Count()];
            // centroblast to centrocyte transition driven by molecule W
            signal[2, 3] = "W";
            beta[2, 3] = 3e-3;
            // Add DriverElements to TransitionDriver
            LoadConfigTransitionDriverElements(driver, signal, alpha, beta, stateNames, store);

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

            // Add distribution driver elements

            // activated to pre-centroblast 
            ConfigDistrTransitionDriverElement distrTdE1 = new ConfigDistrTransitionDriverElement();
            distrTdE1.Distr = new DistributedParameter();
            distrTdE1.Distr.DistributionType = ParameterDistributionType.WEIBULL;
            distrTdE1.Distr.ParamDistr = new WeibullParameterDistribution();
            ((WeibullParameterDistribution)distrTdE1.Distr.ParamDistr).Scale = 1577;
            ((WeibullParameterDistribution)distrTdE1.Distr.ParamDistr).Shape = 10.0;
            distrTdE1.CurrentState = 0;
            distrTdE1.DestState = 1;
            distrTdE1.CurrentStateName = stateNames[0];
            distrTdE1.DestStateName = stateNames[1];
            diffScheme.Driver.DriverElements[0].elements[1] = distrTdE1;

            // pre-centroblast to centroblast 
            ConfigDistrTransitionDriverElement distrTdE2 = new ConfigDistrTransitionDriverElement();
            distrTdE2.Distr = new DistributedParameter();
            distrTdE2.Distr.DistributionType = ParameterDistributionType.CONSTANT;
            distrTdE2.Distr.ParamDistr = new DiracDeltaParameterDistribution();
            ((ConfigDistrTransitionDriverElement)distrTdE2).Distr.ConstValue = ((DiracDeltaParameterDistribution)distrTdE2.Distr.ParamDistr).ConstValue = 10;
            //((DiracDeltaParameterDistribution)distrTdE2.Distr.ParamDistr).ConstValue = 10;
            distrTdE2.CurrentState = 1;
            distrTdE2.DestState = 2;
            distrTdE2.CurrentStateName = stateNames[1];
            distrTdE2.DestStateName = stateNames[2];
            diffScheme.Driver.DriverElements[1].elements[2] = distrTdE2;

            // centrocyte to rescued 
            // mean time of 6 hours
            ConfigDistrTransitionDriverElement distrTdE3 = new ConfigDistrTransitionDriverElement();
            distrTdE3.Distr = new DistributedParameter();
            distrTdE3.Distr.DistributionType = ParameterDistributionType.GAMMA;
            distrTdE3.Distr.ParamDistr = new GammaParameterDistribution();
            ((GammaParameterDistribution)distrTdE3.Distr.ParamDistr).Rate = 1.0 / (360.0 / 50.0);  // = 0.139
            ((GammaParameterDistribution)distrTdE3.Distr.ParamDistr).Shape = 50.0;
            distrTdE3.CurrentState = 3;
            distrTdE3.DestState = 4;
            distrTdE3.CurrentStateName = stateNames[3];
            distrTdE3.DestStateName = stateNames[4];
            diffScheme.Driver.DriverElements[3].elements[4] = distrTdE3;

            // centrocyte to apoptotic 
            // mean time of 5.4 hours
            ConfigDistrTransitionDriverElement distrTdE4 = new ConfigDistrTransitionDriverElement();
            distrTdE4.Distr = new DistributedParameter();
            distrTdE4.Distr.DistributionType = ParameterDistributionType.GAMMA;
            distrTdE4.Distr.ParamDistr = new GammaParameterDistribution();
            ((GammaParameterDistribution)distrTdE4.Distr.ParamDistr).Rate = 1.0 / (325.0 / 50.0);  // = 0.154
            ((GammaParameterDistribution)distrTdE4.Distr.ParamDistr).Shape = 50.0;
            distrTdE4.CurrentState = 3;
            distrTdE4.DestState = 5;
            distrTdE4.CurrentStateName = stateNames[3];
            distrTdE4.DestStateName = stateNames[5];
            diffScheme.Driver.DriverElements[3].elements[5] = distrTdE4;

            // rescued to centroblast
            // mean time of 6 hours
            ConfigDistrTransitionDriverElement distrTdE6 = new ConfigDistrTransitionDriverElement();
            distrTdE6.Distr = new DistributedParameter();
            distrTdE6.Distr.DistributionType = ParameterDistributionType.CONSTANT;
            distrTdE6.Distr.ParamDistr = new DiracDeltaParameterDistribution();
            ((ConfigDistrTransitionDriverElement)distrTdE6).Distr.ConstValue = ((DiracDeltaParameterDistribution)distrTdE6.Distr.ParamDistr).ConstValue = 10;
            //((DiracDeltaParameterDistribution)distrTdE6.Distr.ParamDistr).ConstValue = 10;
            distrTdE6.CurrentState = 4;
            distrTdE6.DestState = 2;
            distrTdE6.CurrentStateName = stateNames[4];
            distrTdE6.DestStateName = stateNames[2];
            diffScheme.Driver.DriverElements[4].elements[2] = distrTdE6;

            // Add to Entity Repository
            store.entity_repository.transition_drivers.Add(driver.Clone(true));
            store.entity_repository.transition_drivers_dict.Add(driver.entity_guid, driver.Clone(true));
            store.entity_repository.diff_schemes.Add(diffScheme.Clone(true));
            store.entity_repository.diff_schemes_dict.Add(diffScheme.entity_guid, diffScheme.Clone(true));

            ////////////////////////////
            // GC B cell division
            ////////////////////////////

            diffScheme = new ConfigTransitionScheme();
            diffScheme.Name = "GC B cell division scheme";

            stateNames = new string[] { "G0", "G1", "S", "G2-M", "cytokinetic" };
            geneNames = new string[]               { "gW", "gE2", "gA" };
            activations = new double[,]          { { 0,     0,   0 },  // G0
                                                   { 0,     0,   0 },   // G1            
                                                   { 1,     1,   1 },  // S
                                                   { 0,     0,   0  },  // G2-M
                                                   { 0,     0,   0  },  // cytokinetic
                                                };

            // Driver
            driver = new ConfigTransitionDriver();
            driver.Name = "GC B cell division driver";
            driver.CurrentState = new DistributedParameter(0);
            driver.StateName = stateNames[0];
            // Molecule driver elements
            signal = new string[stateNames.Count(), stateNames.Count()];
            alpha = new double[stateNames.Count(), stateNames.Count()];
            beta = new double[stateNames.Count(), stateNames.Count()];
            // G0 to G1 transition driven by molecule Wp
            signal[0, 1] = "Wp";
            beta[0, 1] = 0.1;
            // Add DriverElements to TransitionDriver
            LoadConfigTransitionDriverElements(driver, signal, alpha, beta, stateNames, store);

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

            // Add distribution driver elements
            // approximate 6 hr cell cycle
            // G1 to S:  mean 97 min = 1.6 hr
            distrTdE1 = new ConfigDistrTransitionDriverElement();
            distrTdE1.Distr = new DistributedParameter();
            distrTdE1.Distr.DistributionType = ParameterDistributionType.GAMMA;
            distrTdE1.Distr.ParamDistr = new GammaParameterDistribution();
            ((GammaParameterDistribution)distrTdE1.Distr.ParamDistr).Rate = 0.514;
            ((GammaParameterDistribution)distrTdE1.Distr.ParamDistr).Shape = 50;
            distrTdE1.CurrentState = 1;
            distrTdE1.DestState = 2;
            distrTdE1.CurrentStateName = stateNames[1];
            distrTdE1.DestStateName = stateNames[2];
            diffScheme.Driver.DriverElements[1].elements[2] = distrTdE1;

            // S to G2-M:  10 min
            distrTdE2.Distr = new DistributedParameter();
            distrTdE2.Distr.DistributionType = ParameterDistributionType.CONSTANT;
            distrTdE2.Distr.ParamDistr = new DiracDeltaParameterDistribution();
            ((ConfigDistrTransitionDriverElement)distrTdE2).Distr.ConstValue = ((DiracDeltaParameterDistribution)distrTdE2.Distr.ParamDistr).ConstValue = 10;
            distrTdE2.CurrentState = 2;
            distrTdE2.DestState = 3;
            distrTdE2.CurrentStateName = stateNames[2];
            distrTdE2.DestStateName = stateNames[3];
            diffScheme.Driver.DriverElements[2].elements[3] = distrTdE2;

            // G2-M to cytokinetic:  mean 263 min = 4.4 hr
            distrTdE3 = new ConfigDistrTransitionDriverElement();
            distrTdE3.Distr = new DistributedParameter();
            distrTdE3.Distr.DistributionType = ParameterDistributionType.GAMMA;
            distrTdE3.Distr.ParamDistr = new GammaParameterDistribution();
            ((GammaParameterDistribution)distrTdE3.Distr.ParamDistr).Rate = 0.190;
            ((GammaParameterDistribution)distrTdE3.Distr.ParamDistr).Shape = 50;
            distrTdE3.CurrentState = 3;
            distrTdE3.DestState = 4;
            distrTdE3.CurrentStateName = stateNames[3];
            distrTdE3.DestStateName = stateNames[4];
            diffScheme.Driver.DriverElements[3].elements[4] = distrTdE3;

            // Add to Entity Repository
            store.entity_repository.transition_drivers.Add(driver.Clone(true));
            store.entity_repository.transition_drivers_dict.Add(driver.entity_guid, driver.Clone(true));
            store.entity_repository.diff_schemes.Add(diffScheme.Clone(true));
            store.entity_repository.diff_schemes_dict.Add(diffScheme.entity_guid, diffScheme.Clone(true));


            ////////////////////////////////////////
            // cycling cb-cc cell differentiatior 
            ////////////////////////////////////////

            stateNames = new string[] { "centroblast", "centrocyte" };
            geneNames = new string[] { "gCXCR4", "gCXCR5" };
            activations = new double[,]  { { 1,     0  },  // centroblast
                                           { 0,     1 },   // centrocyte
                                         };
            
            // centrocyte/centroblast recycling transition
            //      These numbers are more closely aligned to centroblasts as rescued centrocytes.
            //      mean of 417 min = 7 hours: 1 hour to get from DZ to LZ and 6 hours in the LZ
            // centroblast/centrocyte transition 
            //      mean of 775 min = 13 hr
            //
            diffScheme = new ConfigTransitionScheme();
            diffScheme.Name = "cycling cb-cc diff scheme";
            driver = new ConfigTransitionDriver();
            driver.Name = "cycling cb-cc driver";
            driver.CurrentState = new DistributedParameter(0);
            driver.StateName = stateNames[0];

            // Distribute starting cell population between the two states (centroblast and centrocyte)
            driver.CurrentState.DistributionType = ParameterDistributionType.CATEGORICAL;
            CategoricalParameterDistribution cat_dist = new CategoricalParameterDistribution();
            CategoricalDistrItem cdi1 = new CategoricalDistrItem(0, 0.5);
            CategoricalDistrItem cdi2 = new CategoricalDistrItem(1, 0.5);
            cat_dist.ProbMass.Add(cdi1);
            cat_dist.ProbMass.Add(cdi2);
            driver.CurrentState.ParamDistr = cat_dist;

            // Attach transition driver to differentiation scheme
            diffScheme.Driver = driver;

            // Add states
            diffScheme.Driver.states = new ObservableCollection<string>();
            for (int j = 0; j < stateNames.Count(); j++)
            {
                diffScheme.Driver.AddStateNamePlot(stateNames[j], false);
            }

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

            //// Add DriverElements to TransitionDriver
            // Create driver with default, empty molecule-driven transition driver elements
            for (int i = 0; i < diffScheme.Driver.states.Count; i++)
            {
                ConfigTransitionDriverRow row = new ConfigTransitionDriverRow();

                for (int j = 0; j < diffScheme.Driver.states.Count; j++)
                {
                    row.elements.Add(new ConfigMolTransitionDriverElement());
                }
                diffScheme.Driver.DriverElements.Add(row);
            }

            // Distribution driver elements
            distrTdE1 = new ConfigDistrTransitionDriverElement();
            distrTdE1.Distr = new DistributedParameter();
            distrTdE1.Distr.DistributionType = ParameterDistributionType.WEIBULL;
            distrTdE1.Distr.ParamDistr = new WeibullParameterDistribution();
            // scale=775, shape=6:  mean = 719 min = 12 hr
            ((WeibullParameterDistribution)distrTdE1.Distr.ParamDistr).Scale = 775;  
            ((WeibullParameterDistribution)distrTdE1.Distr.ParamDistr).Shape = 6.0;
            diffScheme.Driver.DriverElements[0].elements[1] = distrTdE1;
            distrTdE1.CurrentState = 0;
            distrTdE1.DestState = 1;

            distrTdE2 = new ConfigDistrTransitionDriverElement();
            distrTdE2.Distr = new DistributedParameter();
            // scale=450, shape=6:  mean = 417 min = 7 hr
            distrTdE2.Distr.DistributionType = ParameterDistributionType.WEIBULL;
            distrTdE2.Distr.ParamDistr = new WeibullParameterDistribution();
            ((WeibullParameterDistribution)distrTdE2.Distr.ParamDistr).Scale = 450;
            ((WeibullParameterDistribution)distrTdE2.Distr.ParamDistr).Shape = 6.0;
            diffScheme.Driver.DriverElements[1].elements[0] = distrTdE2;
            distrTdE2.CurrentState = 1;
            distrTdE2.DestState = 0;

            // Add to Entity Repository
            store.entity_repository.transition_drivers.Add(driver.Clone(true));
            store.entity_repository.transition_drivers_dict.Add(driver.entity_guid, driver.Clone(true));
            store.entity_repository.diff_schemes.Add(diffScheme.Clone(true));
            store.entity_repository.diff_schemes_dict.Add(diffScheme.entity_guid, diffScheme.Clone(true));
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

            // Goldbeter-Koshland system 
            gene = new ConfigGene("gW", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);
            gene = new ConfigGene("gE1", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);
            gene = new ConfigGene("gE2", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);

            // Production of pseudo-molecule A. Needed after cell division.
            gene = new ConfigGene("gA", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);

            // Housekeeping enzyme gene
            gene = new ConfigGene("gH", 2, 1.0);
            store.entity_repository.genes.Add(gene);
            store.entity_repository.genes_dict.Add(gene.entity_guid, gene);


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
            cm.description = "Molecular weight and diffusion coefficient based on data from Wang 2011. ";
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
            cm.description = "Molecular weight based on data from Marchese 2001. ";
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

            // T cell receptor
            cm = new ConfigMolecule("TCR|", 1.0, 1.0, membraneDiffCoeff);
            cm.molecule_location = MoleculeLocation.Boundary;
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            // Membrane-bound MHCII
            cm = new ConfigMolecule("MHCII|", 1.0, 1.0, membraneDiffCoeff);
            cm.molecule_location = MoleculeLocation.Boundary;
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            // IL21 receptor
            cm = new ConfigMolecule("IL21R|", 1.0, 1.0, membraneDiffCoeff);
            cm.molecule_location = MoleculeLocation.Boundary;
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            // Membrane-bound IL21 - needed for secretion of IL21
            cm = new ConfigMolecule("IL21|", 1.0, 1.0, membraneDiffCoeff);
            cm.molecule_location = MoleculeLocation.Boundary;
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("CD4|", 1.0, 1.0, membraneDiffCoeff);
            cm.molecule_location = MoleculeLocation.Boundary;
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("CXCR4|:CXCR4|", 1.0, 1.0, membraneDiffCoeff);
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
            // If the diffusion coefficient is too large, the polarity will not be maintained and the cell will not move.
            // double f = 1e-2;
            // 9-11-2013 gmk: reducing by another factor of 10 (A* diffusion coefficient = 0.75)
            double f = 1e-3;
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

            // Cytosolic MHCII
            cm = new ConfigMolecule("MHCII", 1.0, 1.0, 1.0);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            // IL21
            cm = new ConfigMolecule("IL21R", 1.0, 1.0, 1.0);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("IRF4", 1.0, 1.0, 1.0);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("IRF8", 1.0, 1.0, 1.0);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("BCL6", 1.0, 1.0, 1.0);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("Blimp1", 1.0, 1.0, 1.0);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("CD4", 1.0, 1.0, 1.0);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            // molecules for Goldbeter-Koshland system of reactions
            cm = new ConfigMolecule("W", 1.0, 1.0, 1e-7);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);
            
            cm = new ConfigMolecule("Wp", 1.0, 1.0, 1e-7);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("E1", 1.0, 1.0, 1e-7);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("E2", 1.0, 1.0, 1e-7);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm); 
            store.entity_repository.molecules.Add(cm);

            cm = new ConfigMolecule("W:E1", 1.0, 1.0, 1e-7);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            cm = new ConfigMolecule("Wp:E2", 1.0, 1.0, 1e-7);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

            // housekeeping molecule
            cm = new ConfigMolecule("H", 1.0, 1.0, 1e-7);
            store.entity_repository.molecules.Add(cm);
            store.entity_repository.molecules_dict.Add(cm.entity_guid, cm);

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
            //// Default degradation rate for cytoplasm proteins  (min)
            //// Arbitraritly, assume half life lambda=7 min, then rate constant k=ln(2)/lambda=0.6931/7
            //double cytoDefaultDegradRate = 0.1;
            // This seems to work better in the simulations so far
            double cytoDefaultDegradRate = 1.0;
            ConfigReaction cr = new ConfigReaction();

            // Annihiliation: CXCR5 -> 
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, store));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            // Annihiliation: CXCL13 -> 
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, store));
            cr.rate_const = cytoDefaultDegradRate;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            // Annihiliation: CXCL13:CXCR5 -> 
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13:CXCR5", MoleculeLocation.Bulk, store));
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

            ////////////////////////////////////////////////////////////////////////////////////
            // CXCL13 + CXCR5| <-> CXCL13:CXCR5|
            //
            // Barroso, Munoz, et al.
            // EBI2 regulates CXCL13-mediated responses by heterodimerization with CXCR5
            //// The FASEB Journal vol. 26 no. 12 4841-485
            // (measured) CXCL13/CXCR5 binding affinity:  KD = 30.4 molec/um^3
            // (measured) K_on = 7.7e-3 um^3/#-min
            // (measured) K_off = 0.21 1/min
            // calculated K_off/K_on = 27.3 #/um^3
            // revised:  1/19/2015
            kr = 0.21;
            kf = 7.7e-3;
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
            //// Choose a slower deactivation (A* -> A) rate   k_deactivation = k_activation / 100;
            //double f2 = 1e-1;
            // This seems to work better from preliminary simulations.
            double f2 = 10;
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
            // Vega et al., Technical Advance: Surface plasmon resonance-based analysis of CXCL12 binding 
            // using immobilized lentiviral particles. Journal of Leukocyte Biology Vol 90, 1-10, 2011. 
            // 
            // (measured) XCL12/CXCR4 binding affinity:  KD = 20.9 molec/um^3
            // (measured) k_off = 0.494/min
            // (measured) k_on = 4.2e-2 um^3/#-min
            // calculated K_off/K_on = 12 #/um^3
            // revised:  1/19/2015
            kr = 0.494;
            kf = 4.2e-2;
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
            //          k = 0.051 min(-1)
            double k1_CXCL13_CXCR5 = 0.051;
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
            // See the calibration document.
            //f2 = 0.1;
            //
            // BoundaryTransportFrom: CXCR5| -> CXCR5
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.BoundaryTransportFrom);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5|", MoleculeLocation.Boundary, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, store));
            cr.rate_const = 0.06;
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
            cr.rate_const = 0.054;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            // 
            // BoundaryTransport from cytosol to membrane
            // These values are chosen to get the desired receptor concentration at equilibrium
            // and a short time-to-equilibrium for the membrane receptor concentration.
            // See the calibration document.
            //f2 = 10;

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.BoundaryTransportTo);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5|", MoleculeLocation.Boundary, store));
            cr.rate_const = 3.3;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            // BoundaryTransportTo: CXCR4 -> CXCR4|
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.BoundaryTransportTo);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR4", MoleculeLocation.Bulk, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR4|", MoleculeLocation.Boundary, store));
            cr.rate_const = 3.0;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            //
            // Secretion reactions
            //
            string[] mol_name = { "CXCL12", "CXCL13" };
            double[] k_cytosol_to_pm = { 1.0, 1.0 };
            double[] k_pm_to_ecs = { 1.0, 1.0 };
            // BoundaryTransportTo: cytosol to plasma membrane
            for (int i = 0; i < mol_name.Length; i++)
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


            // Transcription

            // Revised 1/19/2015
            // Schwanhausser et al, Nature 2011 (473), 337-342
            // median 17 mRNAs per cell
            // 140 protein copies/(mRNA copy - hr)
            // Default: 2 gene copies per cell
            // Cell volume = V
            // V = 524 um3 for a 10 um diameter cell
            // kf = 17 * 140 / (2 * 60 * V)  copies/(gene-min-um3)
            kf = 0.4;

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
            //
            // Association: CXCR4 + CXCL12 -> CXCL12:CXCR4
            kr = 0.494;
            kf = 23.6;
            //
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Association);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL12", MoleculeLocation.Bulk, store));
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR4", MoleculeLocation.Bulk, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL12:CXCR4", MoleculeLocation.Bulk, store));
            cr.rate_const = kf;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            // Dissociation: CXCL13:CXCR5 -> CXCR5 + CXCL13
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Dissociation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL12:CXCR4", MoleculeLocation.Bulk, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL12", MoleculeLocation.Bulk, store));
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR4", MoleculeLocation.Bulk, store));
            cr.rate_const = kr;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            ////////////////////////////////////////////////////
            
            /////////////////////////////////////
            // Housekeeping molecule reactions
            ////////////////////////////////////

            double house_transcription = 0.5;
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gH", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("H", MoleculeLocation.Bulk, store));
            cr.rate_const = house_transcription;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            double house_equil = 1.0;
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("H", MoleculeLocation.Bulk, store));
            cr.rate_const = 2 * house_transcription / house_equil;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            ////////////////////////////////////////////
            // Reactions for Goldbeter-Koshland system
            ////////////////////////////////////////////

            // Transcription of Goldbeter-Koshland molecules
            double initialization_period = 10;
            double WT = 1.0;
            double W_transcription = WT / (2 * initialization_period);
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gW", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("W", MoleculeLocation.Bulk, store));
            cr.rate_const = W_transcription;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            double E2T = 1e-2;
            double E2_transcription = E2T / (2 * initialization_period);
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gE2", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("E2", MoleculeLocation.Bulk, store));
            cr.rate_const = E2_transcription;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            //double E1_transcription = 0.06 / (2 * 1500);
            double E1_transcription = 0.4;
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gE1", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("E1", MoleculeLocation.Bulk, store));
            cr.rate_const = E1_transcription;
            cr.GetTotalReactionString(store.entity_repository);
            cr.description = "This rate is computed to achieve a mean concentration of 0.06 after a mean activation period of 1500 minutes with 2 active copies of the gene.";
            store.entity_repository.reactions.Add(cr);

            //// degradation of Goldbeter-Koshland molecules 

            //double targetValue = 1.0; // Set a value lower than actual target (1), because some W stored in Wp and W:E1 and Wp:E2 complexes.
            ////cr = new ConfigReaction();
            ////cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            ////// reactants
            ////cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("W", MoleculeLocation.Bulk, store));
            ////cr.rate_const = copyNumber * transcriptionRate / targetValue;
            ////cr.GetTotalReactionString(store.entity_repository);
            ////store.entity_repository.reactions.Add(cr);
            //cr = new ConfigReaction();
            //cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.CatalyzedAnnihilation);
            //// modifiers
            //cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("H", MoleculeLocation.Bulk, store));
            //// reactants
            //cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("W", MoleculeLocation.Bulk, store));
            //cr.rate_const = 2*W_transcription/(targetValue*house_equil);
            //cr.GetTotalReactionString(store.entity_repository);
            //store.entity_repository.reactions.Add(cr);

            //cr = new ConfigReaction();
            //cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.CatalyzedAnnihilation);
            //// modifiers
            //cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("H", MoleculeLocation.Bulk, store));
            //// reactants
            //cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("Wp", MoleculeLocation.Bulk, store));
            //cr.rate_const = 2 * W_transcription / (targetValue * house_equil);
            //cr.GetTotalReactionString(store.entity_repository);
            //store.entity_repository.reactions.Add(cr);

            //cr = new ConfigReaction();
            //cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.CatalyzedAnnihilation);
            //// modifiers
            //cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("H", MoleculeLocation.Bulk, store));
            //// reactants
            //cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("W:E1", MoleculeLocation.Bulk, store));
            //cr.rate_const = 2 * W_transcription / (targetValue * house_equil);
            //cr.GetTotalReactionString(store.entity_repository);
            //store.entity_repository.reactions.Add(cr);

            //cr = new ConfigReaction();
            //cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.CatalyzedAnnihilation);
            //// modifiers
            //cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("H", MoleculeLocation.Bulk, store));
            //// reactants
            //cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("Wp:E2", MoleculeLocation.Bulk, store));
            //cr.rate_const = 2 * W_transcription / (targetValue * house_equil);
            //cr.GetTotalReactionString(store.entity_repository);
            //store.entity_repository.reactions.Add(cr);

            //targetValue = 1e-2; // Set a value lower than actual target (1e-2), because some E2 stored in Wp:E2 complex.
            ////cr = new ConfigReaction();
            ////cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Annihilation);
            ////// reactants
            ////cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("E2", MoleculeLocation.Bulk, store));
            ////cr.rate_const = copyNumber * transcriptionRate / targetValue;
            ////cr.GetTotalReactionString(store.entity_repository);
            ////store.entity_repository.reactions.Add(cr);
            //cr = new ConfigReaction();
            //cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.CatalyzedAnnihilation);
            //// modifiers
            //cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("H", MoleculeLocation.Bulk, store));
            //// reactants
            //cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("E2", MoleculeLocation.Bulk, store));
            //cr.rate_const = 2 * E2_transcription / (targetValue * house_equil);
            //cr.GetTotalReactionString(store.entity_repository);
            //store.entity_repository.reactions.Add(cr);

            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.CatalyzedAnnihilation);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("sDif1", MoleculeLocation.Bulk, store));
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("E1", MoleculeLocation.Bulk, store));
            cr.rate_const = 16.7;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            // Switch dynamics

            double k = 10;
            double d = 1;
            double a = 110;
            // Association: W + E1 -> W:E1
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Association);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("W", MoleculeLocation.Bulk, store));
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("E1", MoleculeLocation.Bulk, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("W:E1", MoleculeLocation.Bulk, store));
            cr.rate_const = a;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            // Association: Wp + E2 -> Wp:E2
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Association);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("Wp", MoleculeLocation.Bulk, store));
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("E2", MoleculeLocation.Bulk, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("Wp:E2", MoleculeLocation.Bulk, store));
            cr.rate_const = a;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            // Dissociation: W:E1 -> W + E1
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Dissociation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("W:E1", MoleculeLocation.Bulk, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("W", MoleculeLocation.Bulk, store));
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("E1", MoleculeLocation.Bulk, store));
            cr.rate_const = d;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            // Dissociation: Wp:E2 -> Wp + E2
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Dissociation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("Wp:E2", MoleculeLocation.Bulk, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("Wp", MoleculeLocation.Bulk, store));
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("E2", MoleculeLocation.Bulk, store));
            cr.rate_const = d;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            // Dissociation: W:E1 -> Wp + E1
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Dissociation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("W:E1", MoleculeLocation.Bulk, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("Wp", MoleculeLocation.Bulk, store));
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("E1", MoleculeLocation.Bulk, store));
            cr.rate_const = k;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);
            // Dissociation: Wp:E2 -> W + E2
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Dissociation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("Wp:E2", MoleculeLocation.Bulk, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("W", MoleculeLocation.Bulk, store));
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("E2", MoleculeLocation.Bulk, store));
            cr.rate_const = k;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            //////////////////////////////////////////////////////////////
            // Homeostasis reactions for pseudo-molecule A
            /////////////////////////////////////////////////////////////

            double AT = 250;
            double A_transcription =  AT / (2 * initialization_period);
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Transcription);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findGeneGuid("gA", store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("A", MoleculeLocation.Bulk, store));
            cr.rate_const = A_transcription;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            // Dimerizaton: 2 CXCR4| -> CXCR4|:CXCR4|
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.Dimerization);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR4|", MoleculeLocation.Boundary, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR4|:CXCR4|", MoleculeLocation.Boundary, store));
            cr.rate_const = 1.0;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            // Dimer dissociation: CXCR4|:CXCR4| -> 2 CXCR4| 
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.DimerDissociation);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR4|:CXCR4|", MoleculeLocation.Boundary, store));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR4|", MoleculeLocation.Boundary, store));
            cr.rate_const = 1.0;
            cr.GetTotalReactionString(store.entity_repository);
            store.entity_repository.reactions.Add(cr);

            ///////////////////////////////////////////////////////////////////////////////////
            // These are for testing these reaction templates and units. Uncomment if needed.
            ///////////////////////////////////////////////////////////////////////////////////

            //// Autocatalytic transformation: A + A* -> A* + A*  
            //cr = new ConfigReaction();
            //cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.AutocatalyticTransformation);
            //// reactants
            //cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("A", MoleculeLocation.Bulk, store));
            //cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("A*", MoleculeLocation.Bulk, store));
            //// products
            //cr.products_molecule_guid_ref.Add(findMoleculeGuid("A*", MoleculeLocation.Bulk, store));
            //cr.rate_const = 1.0;
            //cr.GetTotalReactionString(store.entity_repository);
            //store.entity_repository.reactions.Add(cr);

            //// Catalyzed association: Wp + E2 + E1 -> Wp:E2 + E1  
            //cr = new ConfigReaction();
            //cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.CatalyzedAssociation);
            //// reactants
            //cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("Wp", MoleculeLocation.Bulk, store));
            //cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("E2", MoleculeLocation.Bulk, store));
            //// modifiers
            //cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("E1", MoleculeLocation.Bulk, store));
            //// products
            //cr.products_molecule_guid_ref.Add(findMoleculeGuid("Wp:E2", MoleculeLocation.Bulk, store));
            //cr.rate_const = 1.0;
            //cr.GetTotalReactionString(store.entity_repository);
            //store.entity_repository.reactions.Add(cr);

            //// Catalyzed Dissociation: Wp:E2 + E1 -> Wp + E2 + E1
            //cr = new ConfigReaction();
            //cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.CatalyzedDissociation);
            //// reactants
            //cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("Wp:E2", MoleculeLocation.Bulk, store));
            //// modifiers
            //cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("E1", MoleculeLocation.Bulk, store));
            //// products
            //cr.products_molecule_guid_ref.Add(findMoleculeGuid("Wp", MoleculeLocation.Bulk, store));
            //cr.products_molecule_guid_ref.Add(findMoleculeGuid("E2", MoleculeLocation.Bulk, store));
            //cr.rate_const = 1.0;
            //cr.GetTotalReactionString(store.entity_repository);
            //store.entity_repository.reactions.Add(cr);

            //// Catalyzed Creation: E1 -> W + E1
            //cr = new ConfigReaction();
            //cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.CatalyzedCreation);
            //// modifiers
            //cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("E1", MoleculeLocation.Bulk, store));
            //// products
            //cr.products_molecule_guid_ref.Add(findMoleculeGuid("W", MoleculeLocation.Bulk, store));
            //cr.rate_const = 1.0;
            //cr.GetTotalReactionString(store.entity_repository);
            //store.entity_repository.reactions.Add(cr);

            //// Catalyzed Transformation: W + E1 -> Wp + E1
            //cr = new ConfigReaction();
            //cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.CatalyzedTransformation);
            //// reactants
            //cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("W", MoleculeLocation.Bulk, store));
            //// modifiers
            //cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("E1", MoleculeLocation.Bulk, store));
            //// products
            //cr.products_molecule_guid_ref.Add(findMoleculeGuid("Wp", MoleculeLocation.Bulk, store));
            //cr.rate_const = 1.0;
            //cr.GetTotalReactionString(store.entity_repository);
            //store.entity_repository.reactions.Add(cr);

            //// Catalyzed Dimerizaton: 2 CXCR4| + CXCL12:CXCR4| -> CXCR4|:CXCR4| + CXCL12:CXCR4|
            //cr = new ConfigReaction();
            //cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.CatalyzedDimerization);
            //// reactants
            //cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR4|", MoleculeLocation.Boundary, store));
            //// modifiers
            //cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("CXCL12:CXCR4|", MoleculeLocation.Boundary, store));
            //// products
            //cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR4|:CXCR4|", MoleculeLocation.Boundary, store));
            //cr.rate_const = 1.0;
            //cr.GetTotalReactionString(store.entity_repository);
            //store.entity_repository.reactions.Add(cr);

            //// Catlayzed Dimer dissociation: CXCR4|:CXCR4| + CXCL12:CXCR4| -> 2 CXCR4| + CXCL12:CXCR4|
            //cr = new ConfigReaction();
            //cr.reaction_template_guid_ref = store.findReactionTemplateGuid(ReactionType.CatalyzedDimerDissociation);
            //// reactants
            //cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR4|:CXCR4|", MoleculeLocation.Boundary, store));
            //// modifiers
            //cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("CXCL12:CXCR4|", MoleculeLocation.Boundary, store));
            //// products
            //cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR4|", MoleculeLocation.Boundary, store));
            //cr.rate_const = 1.0;
            //cr.GetTotalReactionString(store.entity_repository);
            //store.entity_repository.reactions.Add(cr);
        }

        private static void PredefinedReactionComplexesCreator(Level store)
        {
            ConfigReactionComplex crc = new ConfigReactionComplex("Ligand/Receptor");
            crc.description = "A simple example of a reaction complex for binding and unbinding of bulk ligand and receptor molecules. ";
            //MOLECULES
            double[] conc = new double[3] { 2, 1, 0 };
            string[] type = new string[3] { "CXCL13", "CXCR5", "CXCL13:CXCR5" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];

                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.VAT_MP);

                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    configMolPop.mp_distribution = hl;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.LEAN;

                    crc.molpops.Add(configMolPop);
                }
            }

            //REACTIONS
            type = new string[2] { "CXCL13:CXCR5 -> CXCL13 + CXCR5", "CXCL13 + CXCR5 -> CXCL13:CXCR5" };

            for (int i = 0; i < type.Length; i++)
            {
                ConfigReaction reac = findReaction(type[i], store);

                if (reac != null)
                {
                    crc.reactions.Add(reac.Clone(true));
                }
            }

            store.entity_repository.reaction_complexes.Add(crc);

            ////////////////////////////////////////////////////////////////
            crc = new ConfigReactionComplex("CXCL12/CXCR4 binding");
            crc.description = "Binding and unbinding of CXL12 and CXCR4 bulk molecules. ";
            //MOLECULES
            conc = new double[3] { 2, 1, 0 };
            type = new string[3] { "CXCL12", "CXCR4", "CXCL12:CXCR4" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];

                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.VAT_MP);

                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    configMolPop.mp_distribution = hl;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.LEAN;

                    crc.molpops.Add(configMolPop);
                }
            }
            //REACTIONS
            type = new string[2] { "CXCL12:CXCR4 -> CXCL12 + CXCR4", "CXCL12 + CXCR4 -> CXCL12:CXCR4" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigReaction reac = findReaction(type[i], store);

                if (reac != null)
                {
                    crc.reactions.Add(reac.Clone(true));
                }
            }
            store.entity_repository.reaction_complexes.Add(crc);

            ////////////////////////////////////////////////////////////////
            crc = new ConfigReactionComplex("CXCL13/CXCR5 binding");
            crc.description = "Binding and unbinding of CXL13 and CXCR5 bulk molecules. ";
            //MOLECULES
            conc = new double[3] { 2, 1, 0 };
            type = new string[3] { "CXCL13", "CXCR5", "CXCL13:CXCR5" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];

                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.VAT_MP);

                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    configMolPop.mp_distribution = hl;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.LEAN;

                    crc.molpops.Add(configMolPop);
                }
            }
            //REACTIONS
            type = new string[2] { "CXCL13:CXCR5 -> CXCL13 + CXCR5", "CXCL13 + CXCR5 -> CXCL13:CXCR5" };

            for (int i = 0; i < type.Length; i++)
            {
                ConfigReaction reac = findReaction(type[i], store);

                if (reac != null)
                {
                    crc.reactions.Add(reac.Clone(true));
                }
            }
            store.entity_repository.reaction_complexes.Add(crc);

            ////////////////////////////////////////////////////////////////
            crc = new ConfigReactionComplex("CXCR5 receptor production and recycling");
            crc.description = "A set of reactions for synthesis of CXCR5, transport to the membrane, internalization of receptor and receptor/ligand complex, and degradation of internalized receptor. ";
            // Bulk MOLECULES
            conc = new double[] { 0, 0 };
            type = new string[] { "CXCR5", "CXCL13:CXCR5" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];

                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.CELL_MP);

                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    configMolPop.mp_distribution = hl;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.LEAN;

                    crc.molpops.Add(configMolPop);
                }
            }
            // Boundary MOLECULES
            conc = new double[] { 0, 0 };
            type = new string[] { "CXCR5|", "CXCL13:CXCR5|" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, store)];

                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.CELL_MP);

                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    configMolPop.mp_distribution = hl;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.LEAN;

                    crc.molpops.Add(configMolPop);
                }
            }
            //GENES
            type = new string[] { "gCXCR5" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigGene cg = store.entity_repository.genes_dict[findGeneGuid(type[i], store)];
                if (cg != null)
                {
                    crc.genes.Add(cg.Clone(null));
                }
            }
            //REACTIONS
            type = new string[] { "gCXCR5 -> CXCR5 + gCXCR5", 
                                  "CXCR5 ->", "CXCR5 -> CXCR5|", 
                                  "CXCR5| -> CXCR5",
                                  "CXCL13:CXCR5| -> CXCL13:CXCR5",  
                                  "CXCL13:CXCR5 ->" }; 
            for (int i = 0; i < type.Length; i++)
            {
                ConfigReaction reac = findReaction(type[i], store);

                if (reac != null)
                {
                    crc.reactions.Add(reac.Clone(true));
                }
            }
            store.entity_repository.reaction_complexes.Add(crc);


            ////////////////////////////////////////////////////////////////
            crc = new ConfigReactionComplex("CXCR4 receptor production and recycling");
            crc.description = "A set of reactions for synthesis of CXCR4, transport to the membrane, internalization of receptor and receptor/ligand complex, and degradation of internalized receptor. ";
            // Bulk MOLECULES
            conc = new double[] { 0, 0 };
            type = new string[] { "CXCR4", "CXCL12:CXCR4" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];

                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.CELL_MP);

                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    configMolPop.mp_distribution = hl;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.LEAN;

                    crc.molpops.Add(configMolPop);
                }
            }
            // Boundary MOLECULES
            conc = new double[] { 0, 0 };
            type = new string[] { "CXCR4|", "CXCL12:CXCR4|" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, store)];

                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.CELL_MP);

                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    configMolPop.mp_distribution = hl;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.LEAN;

                    crc.molpops.Add(configMolPop);
                }
            }
            //GENES
            type = new string[] { "gCXCR4" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigGene cg = store.entity_repository.genes_dict[findGeneGuid(type[i], store)];
                if (cg != null)
                {
                    crc.genes.Add(cg.Clone(null));
                }
            }
            //REACTIONS
            type = new string[] { "gCXCR4 -> CXCR4 + gCXCR4", 
                                  "CXCR4 ->", "CXCR4 -> CXCR4|", 
                                  "CXCR4| -> CXCR4",
                                  "CXCL12:CXCR4| -> CXCL12:CXCR4",  
                                  "CXCL12:CXCR4 ->" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigReaction reac = findReaction(type[i], store);

                if (reac != null)
                {
                    crc.reactions.Add(reac.Clone(true));
                }
            }
            store.entity_repository.reaction_complexes.Add(crc);

            ////////////////////////////////////////////////////////////////
            crc = new ConfigReactionComplex("Goldbeter-Koshland switch reactions");
            crc.description = "Goldbeter A, Koshland DE. An amplified sensitivity arising from covalent modification in biological systems. Proc Natl Acad Sci USA 1981, 78:6840-6844.";
            crc.description = crc.description + "With these parameter choices and W_total=W+Wp >> E1_total and E2_total, the reactions will produce switch-like behavior for W and Wp around the point when E1_total/E2_total=1.";
            //MOLECULES
            type = new string[] { "W", "Wp", "E1", "E2", "W:E1", "Wp:E2" };
            conc = new double[type.Count()];
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];

                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.VAT_MP);

                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    configMolPop.mp_distribution = hl;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.LEAN;

                    crc.molpops.Add(configMolPop);
                }
            }
            //REACTIONS
            type = new string[] {   "W + E1 -> W:E1", 
                                    "Wp + E2 -> Wp:E2", 
                                    "W:E1 -> W + E1", 
                                    "Wp:E2 -> Wp + E2", 
                                    "W:E1 -> Wp + E1", 
                                    "Wp:E2 -> W + E2" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigReaction reac = findReaction(type[i], store);

                if (reac != null)
                {
                    crc.reactions.Add(reac.Clone(true));
                }
            }
            store.entity_repository.reaction_complexes.Add(crc);

            //////////////////////////////////////////////////////////////////////////////
            crc = new ConfigReactionComplex("Goldbeter-Koshland molecule homeostasis");
            crc.description = "A set of transcription reactions needed to synthesize the molecules for the Goldbeter-Koshland switch reactions. ";
            crc.description = crc.description + "sDif1 catalyzes the annihilation of E1. This can be used after the centroblast/centrocyte transition to arrest the cell cycle at the G0 phase. ";
            //BULK MOLECULES
             type = new string[] { "W", "E1", "E2", "sDif1"};
             conc = new double[type.Count()];
             for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];

                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.CELL_MP);

                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    configMolPop.mp_distribution = hl;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.LEAN;

                    crc.molpops.Add(configMolPop);
                }
            }
            //GENES
            type = new string[] { "gW", "gE1", "gE2", "gDif1" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigGene cg = store.entity_repository.genes_dict[findGeneGuid(type[i], store)];
                if (cg != null)
                {
                    crc.genes.Add(cg.Clone(null));
                }
            }
            //REACTIONS
            type = new string[] {   "gW -> W + gW", 
                                    "gE1 -> E1 + gE1", 
                                    "gE2 -> E2 + gE2", 
                                    "gDif1 -> sDif1 + gDif1", 
                                    "E1 + sDif1 -> sDif1" , 
                                    "sDif1 ->" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigReaction reac = findReaction(type[i], store);

                if (reac != null)
                {
                    crc.reactions.Add(reac.Clone(true));
                }
            }
            store.entity_repository.reaction_complexes.Add(crc);

            //////////////////////////////////////////////////////////////////////////////
            crc = new ConfigReactionComplex("GC B cell chemotaxis: cytosol reactions");
            crc.description = "A set of reactions that are needed by GC B cells for chemotaxis. These reactions should be added to the cytosol. ";
            crc.description = crc.description + "The magnitude and direction of the chemotactic force is proportional to the polarization of activated pseudo-molecule A*, which is proportional to the polarization of bound chemokine receptors (CXCL12:CXCR4| and CXCL13:CXCR5|). ";
            //BULK MOLECULES
            type = new string[] { "A", "A*" };
            conc = new double[type.Count()]; 
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];

                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    configMolPop.mp_distribution = hl;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.LEAN;

                    crc.molpops.Add(configMolPop);
                }
            }
            //BOUNDARY MOLECULES
            conc = new double[] { 0, 0 };
            type = new string[] { "CXCL12:CXCR4|", "CXCL13:CXCR5|" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, store)];

                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.CELL_MP);

                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    configMolPop.mp_distribution = hl;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.LEAN;

                    crc.molpops.Add(configMolPop);
                }
            }
            //GENES
            type = new string[] { "gA" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigGene cg = store.entity_repository.genes_dict[findGeneGuid(type[i], store)];
                if (cg != null)
                {
                    crc.genes.Add(cg.Clone(null));
                }
            }
            //REACTIONS
            type = new string[] { "gA -> A + gA", 
                                  "A* -> A", 
                                  "A + CXCL12:CXCR4| -> A* + CXCL12:CXCR4|",  
                                  "A + CXCL13:CXCR5| -> A* + CXCL13:CXCR5|" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigReaction reac = findReaction(type[i], store);

                if (reac != null)
                {
                    crc.reactions.Add(reac.Clone(true));
                }
            }
            store.entity_repository.reaction_complexes.Add(crc);

            //////////////////////////////////////////////////////////////////////////////
            crc = new ConfigReactionComplex("GC B cell chemotaxis: ECM reactions");
            crc.description = "A set of reactions that are needed by GC B cells for chemotaxis. These cells should be added to the ECM. ";
            crc.description = crc.description + "Non-uniform distributions of ligand (CXCL13 and CXCL12) in the ECM will cause non-uniform concentrations of bound chemokine receptors (CXCR5| and CXCR4|) and ";
            crc.description = crc.description + "non-uniform distributions of the pseudo-molecule A*. ";
            crc.description = crc.description + "The magnitude and direction of the chemotactic force is proportional to the polarization of activated pseudo-molecule A*, which is proportional to the polarization of bound chemokine receptors (CXCL12:CXCR4| and CXCL13:CXCR5|). ";
            //BULK MOLECULES
            type = new string[] { "CXCL12", "CXCL13" };
            conc = new double[type.Count()]; 
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, store)];

                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.CELL_MP);

                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    configMolPop.mp_distribution = hl;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.LEAN;

                    crc.molpops.Add(configMolPop);
                }
            }
            //BOUNDARY MOLECULES
            //conc = new double[] { 33, 33, 0, 0 };
            type = new string[] { "CXCR4|", "CXCR5|", "CXCL12:CXCR4|", "CXCL13:CXCR5|" };
            conc = new double[type.Count()];
            for (int i = 0; i < type.Length; i++)
            {
                ConfigMolecule configMolecule = store.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, store)];

                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.CELL_MP);

                    configMolPop.molecule = configMolecule.Clone(null);
                    configMolPop.Name = configMolecule.Name;

                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    configMolPop.mp_distribution = hl;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.LEAN;

                    crc.molpops.Add(configMolPop);
                }
            }
            //REACTIONS
            type = new string[] { "CXCL13:CXCR5| -> CXCL13 + CXCR5|",  "CXCL13 + CXCR5| -> CXCL13:CXCR5|",
                                    "CXCL12:CXCR4| -> CXCL12 + CXCR4|",  "CXCL12 + CXCR4| -> CXCL12:CXCR4|" };
            for (int i = 0; i < type.Length; i++)
            {
                ConfigReaction reac = findReaction(type[i], store);

                if (reac != null)
                {
                    crc.reactions.Add(reac.Clone(true));
                }
            }

            store.entity_repository.reaction_complexes.Add(crc);
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
            foreach (ConfigTransitionScheme s in store.entity_repository.diff_schemes)
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

        // given a reaction template guid, return the ConfigReactionTemplate
        public static ConfigReactionTemplate findReactionTemplateByGuid(string guid, Protocol protocol)
        {
            foreach (ConfigReactionTemplate crt in protocol.entity_repository.reaction_templates)
            {
                if (crt.entity_guid == guid)
                {
                    return crt;
                }
            }
            return null;
        }

        // given a string description of a reaction complex, return the ConfigReactionComplex that matches
        public static ConfigReactionComplex findReactionComplexByName(string crc_name, Level store)
        {
            foreach (ConfigReactionComplex crc in store.entity_repository.reaction_complexes)
            {
                if (crc.Name == crc_name) 
                {
                    return crc;
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
                driver.AddStateNamePlot(stateName[i], false);
                for (int j = 0; j < signal.GetLength(1); j++)
                {
                    ConfigMolTransitionDriverElement driverElement = new ConfigMolTransitionDriverElement();

                    driverElement.CurrentState = i;
                    driverElement.DestState = j;
                    driverElement.CurrentStateName = stateName[i];
                    driverElement.DestStateName = stateName[j];

                    //if (signal[i, j] != "" && signal[i, j] != null)
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

        /// <summary>
        /// New default scenario for first pass of Daphne germinal center simulation
        /// </summary>
        public static void CreateGCProtocol(Protocol protocol)
        {
            if (protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                throw new InvalidCastException();
            }

            protocol.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor;

            protocol.InitializeStorageClasses();

            //Load needed entities from User Store 
            Level userstore = new Level("Config\\Stores\\userstore.json", "Config\\Stores\\temp_userstore.json");
            userstore = userstore.Deserialize();

            // Load reaction templates from userstore
            LoadProtocolReactionTemplates(protocol, userstore);

            ConfigECSEnvironment envHandle = (ConfigECSEnvironment)protocol.scenario.environment;

            //EXPERIMENT
            protocol.experiment_name = "recycling of centroblasts and centrocytes";
            string descr="";
            descr = string.Format("{0}{1}", descr, "Cells go through an activation phase (~25 h), followed by a brief phase to initialize molecules for the cell cycle."); 
            descr = string.Format("{0}\n{1}", descr, "Cells then transition into a centroblast state where they upregulate CXCR4 receptor and migrate to the Dark Zone.");
            descr = string.Format("{0}{1}", descr, "While in the centroblast state, cells undergo ~10 cell divisions (~ 6 h cell cycle time) before transitioning to centrocytes.");
            descr = string.Format("{0}\n{1}", descr, "Centrocytes down-regulate CXCR4 receptor and upregulate CXCR5 receptor, causing them to migrate to the Light Zone.");
            descr = string.Format("{0}{1}", descr, "Centrocytes are rescued and transition back to centroblasts with a mean time of 6 h.");
            descr = string.Format("{0}\n{1}", descr, "The E1 molecule that drives the transition out to the G0 cell cycle phase is renewed after centrocyte rescue, ");
            descr = string.Format("{0}{1}", descr, "but at a lower level than after activation, so rescued cells undergo ~2-3 rounds of division.");
            descr = string.Format("{0}\n{1}", descr, "Centrocytes that transition into the apoptotic state upregulate production of the sApop molecule which drives cell death.");
            descr = string.Format("{0}{1}", descr, "The mean time for removal of dead cells from the simulation is 15 h.");
            descr = string.Format("{0}\n\n{1}", descr, "Light Zone: ");
            descr = string.Format("{0}\n{1}", descr, "Gaussian distribution of CXCL13 centered in the simulation space");
            descr = string.Format("{0}\n{1}", descr, "coordinates=(130, 130, 130), standard deviations=200");
            descr = string.Format("{0}\n\n{1}", descr, "Dark Zone");
            descr = string.Format("{0}\n{1}", descr, "Gaussian distribution of CXCL12 centered in the upper right corner of the simulation space");
            descr = string.Format("{0}\n{1}", descr, "coordinates=(222, 222, 222), standard deviation=100");
            protocol.experiment_description = descr;
            protocol.scenario.time_config.duration = 20160.0;
            protocol.scenario.time_config.rendering_interval = 60.0;
            protocol.scenario.time_config.sampling_interval = 15.0;
            protocol.scenario.time_config.integrator_step = 0.001;
            protocol.reporter_file_name = "simple_germinal_center";

            protocol.scenario.reactionsReport = true;

            //// mean time for removal = shape/rate
            //double mean_removal_time = 923.0; // min, from Feng's model of Victora et al
            //double shape = 10.0;
            //double rate = shape / mean_removal_time;
            //protocol.sim_params.Phagocytosis.ParamDistr = new GammaParameterDistribution();
            //protocol.sim_params.Phagocytosis.DistributionType = ParameterDistributionType.GAMMA;
            //((GammaParameterDistribution)protocol.sim_params.Phagocytosis.ParamDistr).Rate = rate;
            //((GammaParameterDistribution)protocol.sim_params.Phagocytosis.ParamDistr).Shape = shape;

            envHandle.extent_x = 260;
            envHandle.extent_y = 260;
            envHandle.extent_z = 260;
            envHandle.gridstep = 10;

            //ECS REACTION COMPLEXES - recursive
            string[] ecsReacComplex = new string[] { "GC B cell chemotaxis: ECM reactions" };
            int itemsLoaded = LoadProtocolRCs(protocol, ecsReacComplex, userstore);
            if (itemsLoaded != ecsReacComplex.Length)
            {
                System.Windows.MessageBox.Show("Unable to load all protocol reactions.");
            }

            //CELLS - recursive
            string[] cells = new string[] { "simple Germinal Center B cell" };
            itemsLoaded = itemsLoaded = LoadProtocolCells(protocol, cells, userstore);
            if (itemsLoaded != cells.Length)
            {
                System.Windows.MessageBox.Show("Unable to load all protocol cells.");
            }

            //ECM
            double sep = 130;
            double d = sep * Math.Cos(Math.PI/4.0);
            double[] c = new double[] { envHandle.extent_x / 2, envHandle.extent_y / 2, envHandle.extent_z / 2 };
            double  lz_sigma = 400,
                    dz_sigma = 200;
            double[,] box_trans = new double[,] { { c[0], c[1], c[2] }, 
                                                  { c[0] + d, c[1] + d, c[2] + d} };
            double[,] box_scale = new double[,] { { lz_sigma, lz_sigma, lz_sigma }, { dz_sigma, dz_sigma, dz_sigma } };

            System.Windows.Media.Color[] box_color = new System.Windows.Media.Color[] { System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.0f, 0.3f), 
                                                                                        System.Windows.Media.Color.FromScRgb(0.3f, 0.0f, 1.0f, 0.0f) };
            ConfigMolecularPopulation configMolPop = null;
            // ECM molecules
            string[] ecs_mols = new string[] { "CXCL13", "CXCL12" };
            for (int i = 0; i < ecs_mols.Count(); i++)
            {
                ConfigMolecule cm = userstore.entity_repository.molecules_dict[findMoleculeGuid(ecs_mols[i], MoleculeLocation.Bulk, userstore)];
                if (cm != null)
                {
                    configMolPop = new ConfigMolecularPopulation(ReportType.ECM_MP);
                    configMolPop.molecule = cm.Clone(null);
                    configMolPop.Name = cm.Name;

                    // Set the diffusion coefficient to zero
                    configMolPop.molecule.DiffusionCoefficient = 0.0;

                    // Gaussian Distrtibution
                    // Gaussian distribution parameters: coordinates of center, standard deviations (sigma), and peak concentrtation
                    // box x,y,z_scale parameters are 2*sigma
                    GaussianSpecification gaussSpec = new GaussianSpecification();
                    BoxSpecification box = new BoxSpecification();
                    box.x_trans = box_trans[i, 0];
                    box.y_trans = box_trans[i, 1];
                    box.z_trans = box_trans[i, 2];
                    box.x_scale = box_scale[i, 0];
                    box.y_scale = box_scale[i, 1];
                    box.z_scale = box_scale[i, 2];
                    gaussSpec.box_spec = box;
                    gaussSpec.gaussian_spec_color = box_color[i];
                    gaussSpec.gaussian_region_visibility = false;
                    gaussSpec.current_gaussian_region_visibility = false;
                    gaussSpec.box_spec.current_box_visibility = false;
                    gaussSpec.box_spec.box_visibility = false;

                    MolPopGaussian molPopGaussian = new MolPopGaussian();
                    molPopGaussian.peak_concentration = 500;
                    molPopGaussian.gauss_spec = gaussSpec;

                    configMolPop.mp_distribution = molPopGaussian;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.NONE;
                    ReportECM r = configMolPop.report_mp as ReportECM;
                    r.mean = false;

                    //rendering
                    ((TissueScenario)protocol.scenario).popOptions.AddRenderOptions(configMolPop.renderLabel, configMolPop.Name, false);

                    protocol.scenario.environment.comp.molpops.Add(configMolPop);
                }
            }

            //ECM reaction complexes
            foreach (string s in ecsReacComplex)
            {
                ConfigReactionComplex crc = findReactionComplexByName(s, userstore);
                protocol.scenario.environment.comp.reaction_complexes.Add(crc.Clone(true));
            }

            //CELLS

            // GC B cell
            ConfigCell configCell = findCell("simple Germinal Center B cell", protocol);

            // Cell placement
            CellPopulation cellPop = new CellPopulation();
            cellPop.Cell = configCell.Clone(true);
            cellPop.cellpopulation_name = configCell.CellName;
            cellPop.number = 1;
            double[] extents = new double[3] { envHandle.extent_x, envHandle.extent_y, envHandle.extent_z };
            double minDisSquared = 2 * protocol.entity_repository.cells_dict[cellPop.Cell.entity_guid].CellRadius;
            minDisSquared *= minDisSquared;
            cellPop.cellPopDist = new CellPopUniform(extents, minDisSquared, cellPop);
            cellPop.cellPopDist.Initialize(); 

            // Cell reporting
            cellPop.report_xvf.position = false;
            cellPop.report_xvf.velocity = false;
            cellPop.report_xvf.force = false;

            foreach (ConfigMolecularPopulation cmp in cellPop.Cell.membrane.molpops)
            {
                // Mean only
                cmp.report_mp.mp_extended = ExtendedReport.NONE;                
            }
            foreach (ConfigMolecularPopulation cmp in cellPop.Cell.cytosol.molpops)
            {
                // Mean only
                cmp.report_mp.mp_extended = ExtendedReport.LEAN;
            }
            foreach (ConfigMolecularPopulation mpECM in protocol.scenario.environment.comp.molpops)
            {
                // Turn off ECM probe reporting
                mpECM.report_mp.mp_extended = ExtendedReport.NONE;
            }

            cellPop.reportStates.Differentiation = true;
            cellPop.reportStates.Division = true;
            cellPop.reportStates.Generation = true;
            cellPop.reportStates.Death = true;
            cellPop.reportStates.Exit = true;

            //rendering
            ((TissueScenario)protocol.scenario).popOptions.AddRenderOptions(cellPop.renderLabel, cellPop.cellpopulation_name, true);
            //((TissueScenario)protocol.scenario).popOptions.cellPopOptions[0].renderMethod = RenderMethod.CELL_DIFF_STATE;

            ((TissueScenario)protocol.scenario).cellpopulations.Add(cellPop);

        }

        /// <summary>
        /// New default scenario for first pass of Daphne germinal center simulation
        /// </summary>
        public static void Create_CB_CC_Recycling_Protocol(Protocol protocol)
        {
            if (protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                throw new InvalidCastException();
            }

            protocol.Version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.Minor;
            
            protocol.InitializeStorageClasses();

            //Load needed entities from User Store 
            Level userstore = new Level("Config\\Stores\\userstore.json", "Config\\Stores\\temp_userstore.json");
            userstore = userstore.Deserialize();

            // Load reaction templates from userstore
            LoadProtocolReactionTemplates(protocol, userstore);

            ConfigECSEnvironment envHandle = (ConfigECSEnvironment)protocol.scenario.environment;

            //EXPERIMENT
            protocol.experiment_name = "recycling of centroblasts and centrocytes";
            string descr = "";
            descr = string.Format("{0}{1}", descr, "This is a simple example of cell transitions and movemente between the Dark and Light zones, ");
            descr = string.Format("{0}{1}", descr, "with no cell division or death.");
            descr = string.Format("{0}{1}", descr, "Cells start in the centroblast state, where they upregulate CXCR4 receptor and migrate to the Dark Zone.");
            descr = string.Format("{0}\n{1}", descr, "Centrocytes down-regulate CXCR4 receptor and upregulate CXCR5 receptor, causing them to migrate to the Light Zone.");
            descr = string.Format("{0}\n{1}", descr, "Centrocytes transition back to centroblasts with a mean time of 6 h.");
            descr = string.Format("{0}\n\n{1}", descr, "Light Zone: ");
            descr = string.Format("{0}\n{1}", descr, "Gaussian distribution of CXCL13 centered in the simulation space");
            descr = string.Format("{0}\n{1}", descr, "coordinates=(130, 130, 130), standard deviations=200");
            descr = string.Format("{0}\n\n{1}", descr, "Dark Zone");
            descr = string.Format("{0}\n{1}", descr, "Gaussian distribution of CXCL12 centered in the upper right corner of the simulation space");
            descr = string.Format("{0}\n{1}", descr, "coordinates=(222, 222, 222), standard deviation=100");
            protocol.experiment_description = descr;
            protocol.scenario.time_config.duration = 2000.0;
            protocol.scenario.time_config.rendering_interval = 15.0;
            protocol.scenario.time_config.sampling_interval = 15.0;
            protocol.scenario.time_config.integrator_step = 0.001;
            protocol.reporter_file_name = "centroblast-centrocyte_recycling";

            protocol.scenario.reactionsReport = true;

            envHandle.extent_x = 260;
            envHandle.extent_y = 260;
            envHandle.extent_z = 260;
            envHandle.gridstep = 10;

            //ECS REACTION COMPLEXES - recursive
            string[] ecsReacComplex = new string[] { "GC B cell chemotaxis: ECM reactions" };
            int itemsLoaded = LoadProtocolRCs(protocol, ecsReacComplex, userstore);
            if (itemsLoaded != ecsReacComplex.Length)
            {
                System.Windows.MessageBox.Show("Unable to load all protocol reactions.");
            }

            //CELLS - recursive
            string[] cells = new string[] { "centroblast-centrocyte recycling" };
            itemsLoaded = itemsLoaded = LoadProtocolCells(protocol, cells, userstore);
            if (itemsLoaded != cells.Length)
            {
                System.Windows.MessageBox.Show("Unable to load all protocol cells.");
            }

            //ECM
            double sep = 130;
            double d = sep * Math.Cos(Math.PI / 4.0);
            double[] c = new double[] { envHandle.extent_x / 2, envHandle.extent_y / 2, envHandle.extent_z / 2 };
            double lz_sigma = 400,
                    dz_sigma = 200;
            double[,] box_trans = new double[,] { { c[0], c[1], c[2] }, 
                                                  { c[0] + d, c[1] + d, c[2] + d} };
            double[,] box_scale = new double[,] { { lz_sigma, lz_sigma, lz_sigma }, { dz_sigma, dz_sigma, dz_sigma } };

            System.Windows.Media.Color[] box_color = new System.Windows.Media.Color[] { System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.0f, 0.3f), 
                                                                                        System.Windows.Media.Color.FromScRgb(0.3f, 0.0f, 1.0f, 0.0f) };
            ConfigMolecularPopulation configMolPop = null;
            // ECM molecules
            string[] ecs_mols = new string[] { "CXCL13", "CXCL12" };
            for (int i = 0; i < ecs_mols.Count(); i++)
            {
                ConfigMolecule cm = userstore.entity_repository.molecules_dict[findMoleculeGuid(ecs_mols[i], MoleculeLocation.Bulk, userstore)];
                if (cm != null)
                {
                    configMolPop = new ConfigMolecularPopulation(ReportType.ECM_MP);
                    configMolPop.molecule = cm.Clone(null);
                    configMolPop.Name = cm.Name;

                    // Set the diffusion coefficient to zero
                    configMolPop.molecule.DiffusionCoefficient = 0.0;

                    // Gaussian Distrtibution
                    // Gaussian distribution parameters: coordinates of center, standard deviations (sigma), and peak concentrtation
                    // box x,y,z_scale parameters are 2*sigma
                    GaussianSpecification gaussSpec = new GaussianSpecification();
                    BoxSpecification box = new BoxSpecification();
                    box.x_trans = box_trans[i, 0];
                    box.y_trans = box_trans[i, 1];
                    box.z_trans = box_trans[i, 2];
                    box.x_scale = box_scale[i, 0];
                    box.y_scale = box_scale[i, 1];
                    box.z_scale = box_scale[i, 2];
                    gaussSpec.box_spec = box;
                    gaussSpec.gaussian_spec_color = box_color[i];
                    gaussSpec.gaussian_region_visibility = false;
                    gaussSpec.current_gaussian_region_visibility = false;
                    gaussSpec.box_spec.current_box_visibility = false;
                    gaussSpec.box_spec.box_visibility = false;

                    MolPopGaussian molPopGaussian = new MolPopGaussian();
                    molPopGaussian.peak_concentration = 500;
                    molPopGaussian.gauss_spec = gaussSpec;

                    configMolPop.mp_distribution = molPopGaussian;

                    // Reporting
                    configMolPop.report_mp.mp_extended = ExtendedReport.NONE;
                    ReportECM r = configMolPop.report_mp as ReportECM;
                    r.mean = false;

                    //rendering
                    ((TissueScenario)protocol.scenario).popOptions.AddRenderOptions(configMolPop.renderLabel, configMolPop.Name, false);

                    protocol.scenario.environment.comp.molpops.Add(configMolPop);
                }
            }

            //ECM reaction complexes
            foreach (string s in ecsReacComplex)
            {
                ConfigReactionComplex crc = findReactionComplexByName(s, userstore);
                protocol.scenario.environment.comp.reaction_complexes.Add(crc.Clone(true));
            }

            //CELLS

            // CB-CC cycling cell
            ConfigCell configCell = findCell("centroblast-centrocyte recycling", protocol);

            // Cell placement
            CellPopulation cellPop = new CellPopulation();
            cellPop.Cell = configCell.Clone(true);
            cellPop.cellpopulation_name = configCell.CellName;
            cellPop.number = 20;
            double[] extents = new double[3] { envHandle.extent_x, envHandle.extent_y, envHandle.extent_z };
            double minDisSquared = 2 * protocol.entity_repository.cells_dict[cellPop.Cell.entity_guid].CellRadius;
            minDisSquared *= minDisSquared;
            cellPop.cellPopDist = new CellPopUniform(extents, minDisSquared, cellPop);
            cellPop.cellPopDist.Initialize();

            // Cell reporting
            cellPop.report_xvf.position = false;
            cellPop.report_xvf.velocity = false;
            cellPop.report_xvf.force = false;

            foreach (ConfigMolecularPopulation cmp in cellPop.Cell.membrane.molpops)
            {
                // Mean only
                cmp.report_mp.mp_extended = ExtendedReport.NONE;
            }
            foreach (ConfigMolecularPopulation cmp in cellPop.Cell.cytosol.molpops)
            {
                // Mean only
                cmp.report_mp.mp_extended = ExtendedReport.LEAN;
            }
            foreach (ConfigMolecularPopulation mpECM in protocol.scenario.environment.comp.molpops)
            {
                // Turn off ECM probe reporting
                mpECM.report_mp.mp_extended = ExtendedReport.NONE;
            }

            cellPop.reportStates.Differentiation = true;
            cellPop.reportStates.Division = false;
            cellPop.reportStates.Generation = false;
            cellPop.reportStates.Death = false;
            cellPop.reportStates.Exit = false;

            //rendering
            ((TissueScenario)protocol.scenario).popOptions.AddRenderOptions(cellPop.renderLabel, cellPop.cellpopulation_name, true);
            ((TissueScenario)protocol.scenario).popOptions.cellPopOptions[0].renderMethod = RenderMethod.CELL_DIFF_STATE;

            ((TissueScenario)protocol.scenario).cellpopulations.Add(cellPop);

        }
    }
}
