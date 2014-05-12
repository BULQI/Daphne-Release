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
    public class ConfigCreators
    {
        public static void LoadDefaultGlobalParameters(SimConfiguration sc)
        {
            // molecules
            PredefinedMoleculesCreator(sc);

            // template reactions
            PredefinedReactionTemplatesCreator(sc);

            //code to create reactions
            PredefinedReactionsCreator(sc);

            // cells
            PredefinedCellsCreator(sc);
        }

        public static void CreateAndSerializeLigandReceptorScenario(SimConfiguration sc)
        {
            // Experiment
            sc.experiment_name = "Ligand Receptor Scenario";
            sc.experiment_description = "Initial scenario with predefined Molecules and Reactions, Compartment ECM with molecular populations, reactions, reaction complexes, and manifold";
            sc.scenario.time_config.duration = 100;
            sc.scenario.time_config.rendering_interval = 0.3;
            sc.scenario.time_config.sampling_interval = 1440;

            // Global Paramters
            LoadDefaultGlobalParameters(sc);

            // Gaussian Gradients
            GaussianSpecification gg = new GaussianSpecification();
            BoxSpecification box = new BoxSpecification();
            box.x_scale = 125;
            box.y_scale = 125;
            box.z_scale = 125;
            box.x_trans = 100;
            box.y_trans = 300;
            box.z_trans = 100;
            sc.entity_repository.box_specifications.Add(box);
            gg.gaussian_spec_box_guid_ref = box.box_guid;
            gg.gaussian_spec_name = "Off-center gaussian";
            gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            sc.entity_repository.gaussian_specifications.Add(gg);

            //ADD ECS MOL POPS
            //string molSpec = "CXCR5\t1.0\t0.0\t1.0\nCXCL13\t\t\t6.0e3\nCXCR5:CXCL13\t\t\t0.0\ngCXCR5\t\t\t\ndriver\t\t\t\nCXCL12\t7.96\t\t6.0e3\n";
            //SKG DAPHNE Wednesday, April 10, 2013 4:04:14 PM
            var query =
                from mol in sc.entity_repository.molecules
                where mol.Name == "CXCL13"
                select mol;

            ConfigMolecularPopulation gmp = null;
            foreach (ConfigMolecule gm in query)
            {
                gmp = new ConfigMolecularPopulation();
                gmp.molecule_guid_ref = gm.molecule_guid;
                gmp.mpInfo = new MolPopInfo("My " + gm.Name);
                gmp.Name = "My " + gm.Name;
                gmp.mpInfo.mp_dist_name = "Gaussian gradient";
                gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                gmp.mpInfo.mp_render_blending_weight = 2.0;
                MolPopGaussian sgg = new MolPopGaussian();
                sgg.peak_concentration = 10;
                sgg.gaussgrad_gauss_spec_guid_ref = sc.entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
                gmp.mpInfo.mp_distribution = sgg;
                sc.scenario.environment.ecs.molpops.Add(gmp);
            }


            //ADD CELLS AND CELL MOLECULES
            //This code will add the cell and the predefined ConfigCell already has the molecules needed
            ConfigCell gc = findCell("BCell", sc);
            CellPopulation cp = new CellPopulation();

            cp.cellpopulation_name = "My-B-Cell";
            cp.number = 1;
            cp.cellpopulation_constrained_to_region = true;
            cp.wrt_region = RelativePosition.Inside;
            cp.cellpopulation_color = System.Windows.Media.Color.FromScRgb(1.0f, 0.30f, 0.69f, 0.29f);
            cp.cell_guid_ref = gc.cell_guid;
            CellLocation cl = new CellLocation();
            cl.X = 10;
            cl.Y = 100;
            cl.Z = 1000;
            cp.cell_locations.Add(cl);

            //NO REACTIONS INSIDE CELL FOR THIS SCENARIO

            sc.scenario.cellpopulations.Add(cp);

            //---------------------------------------------------------------

            ////////EXTERNAL REACTIONS - I.E. IN EXTRACELLULAR SPACE
            //////GuiBoundaryReactionTemplate grt = (GuiBoundaryReactionTemplate)(entity_repository.AllReactions[0]);    //The 0'th reaction is Boundary Association
            //////scenario.Reactions.Add(grt);

            //////grt = (GuiBoundaryReactionTemplate)entity_repository.AllReactions[1];    //The 1st reaction is Boundary Dissociation
            //////scenario.Reactions.Add(grt);

        }

        /// <summary>
        /// 
        /// </summary>
        public static void CreateAndSerializeDriverLocomotionScenario(SimConfiguration sc)
        {
            // Experiment
            sc.experiment_name = "Driver Locomotion Scenario";
            sc.experiment_description = "Initial scenario with predefined Molecules and Reactions, Compartment ECM with molecular populations, reactions, reaction complexes, manifold, locomotor";

            sc.scenario.environment.extent_x = 1000;
            sc.scenario.environment.extent_y = 1000;
            sc.scenario.environment.extent_z = 1000;
            sc.scenario.environment.extent_min = 5;
            sc.scenario.environment.extent_max = 1000;
            sc.scenario.environment.gridstep_min = 1;
            sc.scenario.environment.gridstep_max = 100;
            sc.scenario.environment.gridstep = 50;

            sc.scenario.time_config.duration = 100;
            sc.scenario.time_config.rendering_interval = 0.3;
            sc.scenario.time_config.sampling_interval = 1440;

            // Global Paramters
            LoadDefaultGlobalParameters(sc);
            //ChartWindow = ReacComplexChartWindow;

            // Gaussian Gradients
            GaussianSpecification gg = new GaussianSpecification();
            BoxSpecification box = new BoxSpecification();
            box.x_scale = 200;
            box.y_scale = 200;
            box.z_scale = 200;
            box.x_trans = 500;
            box.y_trans = 500;
            box.z_trans = 500;
            sc.entity_repository.box_specifications.Add(box);
            gg.gaussian_spec_box_guid_ref = box.box_guid;
            gg.gaussian_spec_name = "Off-center gaussian";
            gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            sc.entity_repository.gaussian_specifications.Add(gg);

            var query =
                from mol in sc.entity_repository.molecules
                where mol.Name == "CXCL13"
                select mol;

            ConfigMolecularPopulation gmp = null;
            // ecs
            foreach (ConfigMolecule gm in query)
            {
                gmp = new ConfigMolecularPopulation();                
                gmp.molecule_guid_ref = gm.molecule_guid;
                gmp.mpInfo = new MolPopInfo("My " + gm.Name);
                gmp.Name = "My " + gm.Name;
                gmp.mpInfo.mp_dist_name = "Gaussian gradient";
                gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                gmp.mpInfo.mp_render_blending_weight = 2.0;
                MolPopGaussian sgg = new MolPopGaussian();
                sgg.peak_concentration = 2 * 3.0 * 1e-6 * 1e-18 * 6.022e23; //3.6132 // 10;              
                sgg.gaussgrad_gauss_spec_guid_ref = sc.entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
                gmp.mpInfo.mp_distribution = sgg;                                               
                sc.scenario.environment.ecs.molpops.Add(gmp);
            }

            //ADD CELLS AND CELL MOLECULES
            //This code will add the cell and the predefined ConfigCell already has the molecules needed
            ConfigCell gc = findCell("BCell", sc);            
            CellPopulation cp = new CellPopulation();

            cp.cellpopulation_name = "My-B-Cell";
            cp.number = 1;
            cp.cellpopulation_constrained_to_region = true;
            cp.wrt_region = RelativePosition.Inside;
            cp.cellpopulation_color = System.Windows.Media.Color.FromScRgb(1.0f, 0.30f, 0.69f, 0.29f);
            cp.cell_guid_ref = gc.cell_guid;
            CellLocation cl = new CellLocation();
            cl.X = 10;
            cl.Y = 100;
            cl.Z = 1000;
            cp.cell_locations.Add(cl);

            //NO REACTIONS INSIDE CELL FOR THIS SCENARIO

            sc.scenario.cellpopulations.Add(cp);

            //-------------------------------------------------------------

            //EXTERNAL REACTIONS - I.E. IN EXTRACELLULAR SPACE
            string guid = findReactionGuid(ReactionType.BoundaryAssociation, sc);
            sc.scenario.environment.ecs.reactions_guid_ref.Add(guid);
            guid = findReactionGuid(ReactionType.BoundaryDissociation, sc);
            sc.scenario.environment.ecs.reactions_guid_ref.Add(guid);
        }

        /// <summary>
        /// New default scenario for first pass of Daphne
        /// </summary>
        public static void CreateAndSerializeDiffusionScenario(SimConfiguration sc)
        {
            // Experiment
            sc.experiment_name = "Diffusion Scenario";
            sc.experiment_description = "Initial scenario with 1 Compartment ECM with 1 molecular population, no cells or reactions";
            sc.scenario.time_config.duration = 100;
            sc.scenario.time_config.rendering_interval = 0.3;
            sc.scenario.time_config.sampling_interval = 1440;

            // Global Paramters
            LoadDefaultGlobalParameters(sc);
            //ChartWindow = ReacComplexChartWindow;

            // Gaussian Gradients
            GaussianSpecification gg = new GaussianSpecification();
            BoxSpecification box = new BoxSpecification();
            box.x_scale = 125;
            box.y_scale = 125;
            box.z_scale = 125;
            box.x_trans = 100;
            box.y_trans = 300;
            box.z_trans = 100;
            sc.entity_repository.box_specifications.Add(box);
            gg.gaussian_spec_box_guid_ref = box.box_guid;
            gg.gaussian_spec_name = "Off-center gaussian";
            gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            sc.entity_repository.gaussian_specifications.Add(gg);

            //SKG DAPHNE Wednesday, April 10, 2013 4:04:14 PM
            var query =
                from mol in sc.entity_repository.molecules
                where mol.Name == "CXCL13"
                select mol;

            ConfigMolecularPopulation gmp = null;
            // ecs
            foreach (ConfigMolecule gm in query)
            {
                gmp = new ConfigMolecularPopulation();
                gmp.molecule_guid_ref = gm.molecule_guid;
                gmp.mpInfo = new MolPopInfo("My " + gm.Name);
                gmp.Name = "My " + gm.Name;
                gmp.mpInfo.mp_dist_name = "Gaussian gradient";
                gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                gmp.mpInfo.mp_render_blending_weight = 2.0;
                MolPopGaussian sgg = new MolPopGaussian();
                sgg.peak_concentration = 10;
                sgg.gaussgrad_gauss_spec_guid_ref = sc.entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
                gmp.mpInfo.mp_distribution = sgg;
                sc.scenario.environment.ecs.molpops.Add(gmp);
            }
        }

         /// <summary>
        /// New default scenario for first pass of Daphne
        /// </summary>
        public static void CreateAndSerializeBlankScenario(SimConfiguration sc)
        {
            // Experiment
            sc.experiment_name = "Blank Scenario";
            sc.experiment_description = "Scenario with 1 Compartment ECM, no molecules, no cells or reactions";
            sc.scenario.time_config.duration = 100;
            sc.scenario.time_config.rendering_interval = 0.3;
            sc.scenario.time_config.sampling_interval = 1440;

            // Global Paramters
            LoadDefaultGlobalParameters(sc);

            // Gaussian Gradients
            GaussianSpecification gg = new GaussianSpecification();
            BoxSpecification box = new BoxSpecification();
            box.x_scale = 200;
            box.y_scale = 200;
            box.z_scale = 200;
            box.x_trans = 500;
            box.y_trans = 500;
            box.z_trans = 500;
            sc.entity_repository.box_specifications.Add(box);
            gg.gaussian_spec_box_guid_ref = box.box_guid;
            gg.gaussian_spec_name = "Off-center gaussian";
            gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            sc.entity_repository.gaussian_specifications.Add(gg);

        }

        private static void PredefinedCellsCreator(SimConfiguration sc)
        {           

            //Some input Grace sent on 6/19/13
           
            //T-Cell: radius = 5 micrometers, no molecules, no reactions, no reaction complexes
            //FDC: radius = 5 micrometers, no molecules, no reactions, no reaction complexes
            //B-Cell: radius = 5 micrometers
            //          Cytoplasm Molecules: gCXCR5, CXCR5, driver
            //          PlasmaMembrane Molecules: CXCR5, CXCR5:CXCL13
            //          Reactions: 
            //              gCXCR5 -> CXCR5  (cytosol)
            //              CXCR5 (plasma membrane) + CXCL13 (ecm) -> CXCR5:CXCL13 (plasma membrane)
            //              CXCR5:CXCL13 (plasma membrane) -> CXCR5 (plasma membrane) + CXCL13 (ecm)
            //
            //---------------------------------------------------------------------------------------
            //BCell 

            ConfigCell gc = new ConfigCell();
            gc.CellName = "BCell";
            gc.CellRadius = 5.0;
            gc.TransductionConstant = 1e4;
            gc.locomotor_mol_guid_ref = findMoleculeGuid("A*", MoleculeLocation.Bulk, sc);

            //MOLECULES IN MEMBRANE
            var query1 =
                from mol in sc.entity_repository.molecules
                where mol.Name == "CXCR5" || mol.Name == "CXCR5:CXCL13"
                select mol;

            ConfigMolecularPopulation gmp = null;
            foreach (ConfigMolecule gm in query1)
            {
                gmp = new ConfigMolecularPopulation();
                gmp.molecule_guid_ref = gm.molecule_guid;
                gmp.mpInfo = new MolPopInfo("My " + gm.Name);
                gmp.Name = "My " + gm.Name;

                gmp.mpInfo.mp_dist_name = "Constant level";
                gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                gmp.mpInfo.mp_render_blending_weight = 2.0;
                MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                if (gm.Name == "CXCR5")
                {
                    hl.concentration = 125;
                }
                else
                {
                    hl.concentration = 130;
                }
                gmp.mpInfo.mp_distribution = hl;
                
                //gc
                gc.membrane.molpops.Add(gmp);
            }

            //MOLECULES IN CYTOSOL
            var query2 =
                from mol in sc.entity_repository.molecules
                where mol.molecule_guid == gc.locomotor_mol_guid_ref
                select mol;

            gmp = null;
            foreach (ConfigMolecule gm in query2)
            {
                gmp = new ConfigMolecularPopulation();
                gmp.molecule_guid_ref = gm.molecule_guid;
                gmp.mpInfo = new MolPopInfo("My " + gm.Name);
                gmp.Name = "My " + gm.Name;

                gmp.mpInfo.mp_dist_name = "Constant level";
                gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                gmp.mpInfo.mp_render_blending_weight = 2.0;
                MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                hl.concentration = 250;
                gmp.mpInfo.mp_distribution = hl;

                gc.cytosol.molpops.Add(gmp);
            }

            sc.entity_repository.cells.Add(gc);

            //Reactions
            //None

            //---------------------------------------------------------------------------------------
            //TCell 

            gc = new ConfigCell();
            gc.CellName = "TCell";
            gc.CellRadius = 5.0;

            sc.entity_repository.cells.Add(gc);

            gc = new ConfigCell();
            gc.CellName = "FDC";
            gc.CellRadius = 5.0;

            sc.entity_repository.cells.Add(gc);

            //Write out into json file!
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(sc.entity_repository.cells, Newtonsoft.Json.Formatting.Indented, Settings);
            string jsonFile = "Config\\DaphnePredefinedCells.txt";
            File.WriteAllText(jsonFile, jsonSpec);
        }

        private static void PredefinedMoleculesCreator(SimConfiguration sc)
        {
            ConfigMolecule cm;
            
            cm = new ConfigMolecule("CXCL13", 1.0, 1.0, 6e3);
            sc.entity_repository.molecules.Add(cm);

            cm = new ConfigMolecule("CXCR5", 1.0, 1.0, 0.0);
            cm.molecule_location = MoleculeLocation.Boundary;
            sc.entity_repository.molecules.Add(cm);

            cm = new ConfigMolecule("CXCR5:CXCL13", 1.0, 1.0, 0.0);
            cm.molecule_location = MoleculeLocation.Boundary;
            sc.entity_repository.molecules.Add(cm);

            cm = new ConfigMolecule("A", 1.0, 1.0, 6e3);
            sc.entity_repository.molecules.Add(cm);

            cm = new ConfigMolecule("A*", 1.0, 1.0, 6e3);
            sc.entity_repository.molecules.Add(cm);

            //Write out into json file!
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(sc.entity_repository.molecules, Newtonsoft.Json.Formatting.Indented, Settings);
            string jsonFile = "Config\\DaphnePredefinedMolecules.txt";
            File.WriteAllText(jsonFile, jsonSpec);
        }

        //Following function needs to be called only once
        private static void PredefinedReactionTemplatesCreator(SimConfiguration sc)
        {
            //Test code to read in json containing object "PredefinedReactions"
            //string readText = File.ReadAllText("TESTER.TXT");
            //entity_repository.reactions = JsonConvert.DeserializeObject<ObservableCollection<ConfigReaction>>(readText);
            ConfigReactionTemplate crt;

            //BoundaryAssociation
            crt = new ConfigReactionTemplate();

            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.BoundaryAssociation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);

            //BoundaryDissociation
            crt = new ConfigReactionTemplate();

            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            //products
            crt.products_stoichiometric_const.Add(1);
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.BoundaryDissociation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);

            // CatalyzedBoundaryActivation
            crt = new ConfigReactionTemplate();

            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            crt.reactants_stoichiometric_const.Add(1);
            // products
            crt.products_stoichiometric_const.Add(1);
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.CatalyzedBoundaryActivation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);

            // Transformation
            crt = new ConfigReactionTemplate();

            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            // products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.Transformation;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);

            //Write out into json file!
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(sc.entity_repository.reaction_templates, Newtonsoft.Json.Formatting.Indented, Settings);
            string jsonFile = "Config\\DaphnePredefinedReactionTemplates.txt";
            File.WriteAllText(jsonFile, jsonSpec);
        }

        // given a molecule name and location, find its guid
        public static string findMoleculeGuid(string name, MoleculeLocation ml, SimConfiguration sc)
        {
            foreach (ConfigMolecule cm in sc.entity_repository.molecules)
            {
                if (cm.Name == name && cm.molecule_location == ml)
                {
                    return cm.molecule_guid;
                }
            }
            return null;
        }

        // given a reaction template type, find its guid
        public static string findReactionTemplateGuid(ReactionType rt, SimConfiguration sc)
        {
            foreach (ConfigReactionTemplate crt in sc.entity_repository.reaction_templates)
            {
                if (crt.reac_type == rt)
                {
                    return crt.reaction_template_guid;
                }
            }
            return null;
        }

        // this only works if we have only one reaction per type of reaction
        public static string findReactionGuid(ReactionType rt, SimConfiguration sc)
        {
            string template_guid = findReactionTemplateGuid(rt, sc);

            if (template_guid != null)
            {
                foreach (ConfigReaction cr in sc.entity_repository.reactions)
                {
                    if (cr.reaction_template_guid_ref == template_guid)
                    {
                        return cr.reaction_guid;
                    }
                }
            }
            return null;
        }

        // given a cell type name like BCell, find the ConfigCell object
        public static ConfigCell findCell(string name, SimConfiguration sc)
        {
            foreach (ConfigCell cc in sc.entity_repository.cells)
            {
                if (cc.CellName == name)
                {
                    return cc;
                }
            }
            return null;
        }

        //Following function needs to be called only once
        private static void PredefinedReactionsCreator(SimConfiguration sc)
        {
            //Test code to read in json containing object "PredefinedReactions"
            //string readText = File.ReadAllText("TESTER.TXT");
            //entity_repository.reactions = JsonConvert.DeserializeObject<ObservableCollection<ConfigReaction>>(readText);

            // BoundaryAssociation
            ConfigReaction cr = new ConfigReaction();

            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.BoundaryAssociation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Boundary, sc));
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5:CXCL13", MoleculeLocation.Boundary, sc));
            cr.rate_const = 2.0;
            sc.entity_repository.reactions.Add(cr);

            // BoundaryDissociation
            cr = new ConfigReaction();

            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.BoundaryDissociation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5:CXCL13", MoleculeLocation.Boundary, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Boundary, sc));
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, sc));
            cr.rate_const = 1.0;
            sc.entity_repository.reactions.Add(cr);

            // CatalyzedBoundaryActivation
            cr = new ConfigReaction();

            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.CatalyzedBoundaryActivation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5:CXCL13", MoleculeLocation.Boundary, sc));
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("A", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5:CXCL13", MoleculeLocation.Boundary, sc));
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("A*", MoleculeLocation.Bulk, sc));
            cr.rate_const = 2.0;
            sc.entity_repository.reactions.Add(cr);

            // Transformation
            cr = new ConfigReaction();

            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Transformation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("A*", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("A", MoleculeLocation.Bulk, sc));
            cr.rate_const = 10.0;
            sc.entity_repository.reactions.Add(cr);

            //Write out into json file!
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(sc.entity_repository.reactions, Newtonsoft.Json.Formatting.Indented, Settings);
            string jsonFile = "Config\\DaphnePredefinedReactions.txt";
            File.WriteAllText(jsonFile, jsonSpec);
        }
    }
}
