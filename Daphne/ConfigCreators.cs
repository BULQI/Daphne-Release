﻿using System;
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

            //reaction complexes
            PredefinedReactionComplexesCreator(sc);
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
                gmp.mpInfo.mp_type_guid_ref = gm.molecule_guid;

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

            foreach (ConfigMolecularPopulation cmp in sc.scenario.environment.ecs.molpops)
            {
                ReportMP rmp = new ReportMP();
                rmp.molpop_guid_ref = cmp.molpop_guid;
                cp.EcmProbeMP.Add(rmp);
            }

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
            sc.experiment_name = "Cell locomotion with driver molecule.";
            sc.experiment_description = "Cell moves toward center of simulation. Drag coefficient=2. Cytosol molecule A* drives locomotion. The CXCL13 in the ECS is diffusing, so the driving force will dissipate over time.";
            //sc.experiment_description = "ECS with CXCL13 and one cell."
            //    + " PlasmaMembrane CXCR5 and CXCR5:CXCL13 surface molecules."
            //    + " Cytosol A molecule is activated to A* by CXCR5:CXCL13."
            //    + " The CXCL13 initial distribution is Gaussian centered in the ECM.";
            sc.scenario.environment.extent_x = 500;
            sc.scenario.environment.extent_y = 500;
            sc.scenario.environment.extent_z = 500;
            sc.scenario.environment.extent_min = 5;
            sc.scenario.environment.extent_max = 1000;
            sc.scenario.environment.gridstep_min = 1;
            sc.scenario.environment.gridstep_max = 100;
            sc.scenario.environment.gridstep = 50;

            sc.scenario.time_config.duration = 200;
            sc.scenario.time_config.rendering_interval = 0.3;
            sc.scenario.time_config.sampling_interval = 1440;

            // Global Paramters
            LoadDefaultGlobalParameters(sc);
            //ChartWindow = ReacComplexChartWindow;

            // Gaussian Gradients
            GaussianSpecification gg = new GaussianSpecification();
            BoxSpecification box = new BoxSpecification();
            box.x_scale = 100;
            box.y_scale = 100;
            box.z_scale = 100;
            box.x_trans = 250;
            box.y_trans = 250;
            box.z_trans = 250;
            sc.entity_repository.box_specifications.Add(box);
            gg.gaussian_spec_box_guid_ref = box.box_guid;
            //gg.gaussian_spec_name = "Off-center gaussian";

            gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            sc.entity_repository.gaussian_specifications.Add(gg);

            var query =
                from mol in sc.entity_repository.molecules
                where (mol.Name == "CXCL13" && mol.molecule_location == MoleculeLocation.Bulk)
                select mol;

            ConfigMolecularPopulation gmp = null;
            // ecs
            foreach (ConfigMolecule gm in query)
            {
                gmp = new ConfigMolecularPopulation();
                gmp.molecule_guid_ref = gm.molecule_guid;
                gmp.mpInfo = new MolPopInfo(gm.Name);
                gmp.Name = gm.Name;
                gmp.mpInfo.mp_dist_name = "Gaussian";
                gmp.mpInfo.mp_type_guid_ref = gm.molecule_guid;
                //gmp.mpInfo.mp_type_guid_ref = gm.Name;

                gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                gmp.mpInfo.mp_render_blending_weight = 2.0;

                MolPopGaussian sgg = new MolPopGaussian();
                sgg.peak_concentration = 2 * 3.0 * 1e-6 * 1e-18 * 6.022e23;      
                sgg.gaussgrad_gauss_spec_guid_ref = sc.entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
                gmp.mpInfo.mp_distribution = sgg;
                sc.scenario.environment.ecs.molpops.Add(gmp);

                

            }

            //ADD CELLS AND CELL MOLECULES
            //This code will add the cell and the predefined ConfigCell already has the molecules needed
            ConfigCell gc = findCell("Leukocyte_staticReceptor_motile", sc);
            CellPopulation cp = new CellPopulation();

            cp.cellpopulation_name = "Motile_leukocytes";
            cp.number = 1;
            cp.cellpopulation_constrained_to_region = true;
            cp.wrt_region = RelativePosition.Inside;
            cp.cellpopulation_color = System.Windows.Media.Color.FromScRgb(1.0f, 0.30f, 0.69f, 0.29f);
            cp.cell_guid_ref = gc.cell_guid;
            CellLocation cl = new CellLocation();
            cl.X = 400;
            cl.Y = 250;
            cl.Z = 250;
            cp.cell_locations.Add(cl);

            foreach (ConfigMolecularPopulation cmp in sc.scenario.environment.ecs.molpops)
            {
                ReportMP rmp = new ReportMP();
                rmp.molpop_guid_ref = cmp.molpop_guid;
                cp.EcmProbeMP.Add(rmp);
            }

            sc.scenario.cellpopulations.Add(cp);

            //EXTERNAL REACTIONS - I.E. IN EXTRACELLULAR SPACE

            string[] type = new string[2] {"CXCR5_membrane + CXCL13 -> CXCR5:CXCL13_membrane",
                                  "CXCR5:CXCL13_membrane -> CXCR5_membrane + CXCL13"};
            ConfigReaction reac;
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], sc);
                if (reac != null)
                {
                    sc.scenario.environment.ecs.reactions_guid_ref.Add(reac.reaction_guid);
                }
            }
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
                gmp.mpInfo.mp_type_guid_ref = gm.molecule_guid;

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
            sc.experiment_description = "Libraries only.";
            sc.scenario.time_config.duration = 100;
            sc.scenario.time_config.rendering_interval = 0.3;
            sc.scenario.time_config.sampling_interval = 1440;

            // Global Paramters
            LoadDefaultGlobalParameters(sc);

            //// Gaussian Gradients
            //GaussianSpecification gg = new GaussianSpecification();
            //BoxSpecification box = new BoxSpecification();
            //box.x_scale = 200;
            //box.y_scale = 200;
            //box.z_scale = 200;
            //box.x_trans = 500;
            //box.y_trans = 500;
            //box.z_trans = 500;
            //sc.entity_repository.box_specifications.Add(box);
            //gg.gaussian_spec_box_guid_ref = box.box_guid;
            //gg.gaussian_spec_name = "Off-center gaussian";
            //gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            //sc.entity_repository.gaussian_specifications.Add(gg);

        }

        private static void PredefinedCellsCreator(SimConfiguration sc)
        {
            ConfigCell gc;
            double[] conc;
            //ReactionType[] reacType;
            string[] type;
            ConfigMolecularPopulation gmp;
            ConfigMolecule cm;
            ConfigReaction reac;

            //
            // Leukocyte_staticReceptor
            // Leukocyte with fixed number of receptor molecules and no locomotion
            //
            gc = new ConfigCell();
            gc.CellName = "Leukocyte_staticReceptor";
            gc.CellRadius = 10.0;

            //MOLECULES IN MEMBRANE
            conc = new double[2] { 250, 0 };
            type = new string[2] { "CXCR5_membrane", "CXCR5:CXCL13_membrane" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = sc.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, sc)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation();
                    gmp.molecule_guid_ref = cm.molecule_guid;
                    gmp.mpInfo = new MolPopInfo(cm.Name);
                    gmp.Name = cm.Name;

                    gmp.mpInfo.mp_dist_name = "Uniform";
                    gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mpInfo.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mpInfo.mp_distribution = hl;
                    gc.membrane.molpops.Add(gmp);
                }
            }
            sc.entity_repository.cells.Add(gc);

            //
            // Leukocyte_staticReceptor_motile
            // Leukocyte with fixed number of receptor molecules with locomotion driven by A*.
            //
            gc = new ConfigCell();
            gc.CellName = "Leukocyte_staticReceptor_motile";
            gc.CellRadius = 10.0;

            //MOLECULES IN MEMBRANE
            conc = new double[2] { 250, 0 };
            type = new string[2] { "CXCR5_membrane", "CXCR5:CXCL13_membrane" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = sc.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, sc)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation();
                    gmp.molecule_guid_ref = cm.molecule_guid;
                    gmp.mpInfo = new MolPopInfo(cm.Name);
                    gmp.Name = cm.Name;

                    gmp.mpInfo.mp_dist_name = "Uniform";
                    gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mpInfo.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mpInfo.mp_distribution = hl;
                    gc.membrane.molpops.Add(gmp);
                }
            }

            //MOLECULES IN Cytosol
            conc = new double[2] { 250, 0 };
            type = new string[2] { "A", "A*" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = sc.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, sc)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation();
                    gmp.molecule_guid_ref = cm.molecule_guid;
                    gmp.mpInfo = new MolPopInfo(cm.Name);
                    gmp.Name = cm.Name;

                    gmp.mpInfo.mp_dist_name = "Uniform";
                    gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mpInfo.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mpInfo.mp_distribution = hl;
                    gc.cytosol.molpops.Add(gmp);
                }
            }
            gc.locomotor_mol_guid_ref = findMoleculeGuid("A*", MoleculeLocation.Bulk, sc);

            // Reactions in Cytosol
            type = new string[2] {"A + CXCR5:CXCL13_membrane -> A* + CXCR5:CXCL13_membrane",
                                          "A* -> A"};
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], sc);
                if (reac != null)
                {
                    gc.cytosol.reactions_guid_ref.Add(reac.reaction_guid);
                }
            }

            gc.DragCoefficient = 2.0;
            gc.TransductionConstant = 1e6;

            sc.entity_repository.cells.Add(gc);

            //
            // Leukocyte_dynamicReceptor_motile
            // Leukocyte with dynamic concentration of receptor molecules with locomotion driven by A*.
            //
            gc = new ConfigCell();
            gc.CellName = "Leukocyte_dynamicReceptor_motile";
            gc.CellRadius = 10.0;

            //MOLECULES IN MEMBRANE
            conc = new double[2] { 250, 0 };
            type = new string[2] { "CXCR5_membrane", "CXCR5:CXCL13_membrane" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = sc.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Boundary, sc)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation();
                    gmp.molecule_guid_ref = cm.molecule_guid;
                    gmp.mpInfo = new MolPopInfo(cm.Name);
                    gmp.Name = cm.Name;

                    gmp.mpInfo.mp_dist_name = "Uniform";
                    gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mpInfo.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mpInfo.mp_distribution = hl;
                    gc.membrane.molpops.Add(gmp);
                }
            }

            //MOLECULES IN Cytosol
            conc = new double[5] { 250, 0, 1, 0, 0 };
            type = new string[5] { "A", "A*", "gCXCR5", "CXCR5", "CXCR5:CXCL13" };
            for (int i = 0; i < type.Length; i++)
            {
                cm = sc.entity_repository.molecules_dict[findMoleculeGuid(type[i], MoleculeLocation.Bulk, sc)];
                if (cm != null)
                {
                    gmp = new ConfigMolecularPopulation();
                    gmp.molecule_guid_ref = cm.molecule_guid;
                    gmp.mpInfo = new MolPopInfo(cm.Name);
                    gmp.Name = cm.Name;

                    gmp.mpInfo.mp_dist_name = "Uniform";
                    gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    gmp.mpInfo.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = conc[i];
                    gmp.mpInfo.mp_distribution = hl;
                    gc.cytosol.molpops.Add(gmp);
                }
            }
            gc.locomotor_mol_guid_ref = findMoleculeGuid("A*", MoleculeLocation.Bulk, sc);

            // Reactions in Cytosol
            type = new string[8] {"A + CXCR5:CXCL13_membrane -> A* + CXCR5:CXCL13_membrane",
                                  "A* -> A",
                                  "gCXCR5 -> CXCR5 + gCXCR5",
                                  "CXCR5 -> CXCR5_membrane",
                                  "CXCR5_membrane -> CXCR5",
                                  "CXCR5 ->",
                                  "CXCR5:CXCL13_membrane -> CXCR5:CXCL13", 
                                  "CXCR5:CXCL13 ->" 
                                };
            for (int i = 0; i < type.Length; i++)
            {
                reac = findReaction(type[i], sc);
                if (reac != null)
                {
                    gc.cytosol.reactions_guid_ref.Add(reac.reaction_guid);
                }
            }

            gc.DragCoefficient = 2.0;
            gc.TransductionConstant = 1e6;

            sc.entity_repository.cells.Add(gc);


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

            gc = new ConfigCell();
            gc.CellName = "BCell";
            gc.CellRadius = 5.0;
            gc.TransductionConstant = 1e4;
            gc.locomotor_mol_guid_ref = findMoleculeGuid("A*", MoleculeLocation.Bulk, sc);

            //MOLECULES IN MEMBRANE
            var query1 =
                from mol in sc.entity_repository.molecules
                where mol.Name == "CXCR5_membrane" || mol.Name == "CXCR5:CXCL13_membrane"
                select mol;

            gmp = null;
            foreach (ConfigMolecule gm in query1)
            {
                gmp = new ConfigMolecularPopulation();
                gmp.molecule_guid_ref = gm.molecule_guid;
                gmp.mpInfo = new MolPopInfo("My " + gm.Name);
                gmp.Name = "My " + gm.Name;
                gmp.mpInfo.mp_dist_name = "Constant level";
                gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                gmp.mpInfo.mp_render_blending_weight = 2.0;
                gmp.mpInfo.mp_type_guid_ref = gm.molecule_guid;

                MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();

                if (gm.Name == "CXCR5_membrane")
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
                gmp.mpInfo.mp_type_guid_ref = gm.molecule_guid;

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
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            cm = new ConfigMolecule("CXCR5_membrane", 1.0, 1.0, 1e-7);
            cm.molecule_location = MoleculeLocation.Boundary;
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            cm = new ConfigMolecule("CXCR5:CXCL13_membrane", 1.0, 1.0, 1e-7);
            cm.molecule_location = MoleculeLocation.Boundary;
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            cm = new ConfigMolecule("A", 1.0, 1.0, 6e3);
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            cm = new ConfigMolecule("A*", 1.0, 1.0, 6e3);
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            cm = new ConfigMolecule("gCXCR5", 1.0, 1.0, 1e-7);
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            // CXCR5 in the cytosol
            cm = new ConfigMolecule("CXCR5", 1.0, 1.0, 1e3);
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);

            // CXCR5:CXCL13 in the cytosol
            cm = new ConfigMolecule("CXCR5:CXCL13", 1.0, 1.0, 1e3);
            sc.entity_repository.molecules.Add(cm);
            sc.entity_repository.molecules_dict.Add(cm.molecule_guid, cm);


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
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);
            
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
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

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
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

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
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

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
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

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
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

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
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

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
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

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
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            // BoundaryTransportFrom
            // A -> a
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            // products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.BoundaryTransportFrom;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

            // BoundaryTransportTo
            // a -> A
            crt = new ConfigReactionTemplate();
            // reactants
            crt.reactants_stoichiometric_const.Add(1);
            // products
            crt.products_stoichiometric_const.Add(1);
            // type
            crt.reac_type = ReactionType.BoundaryTransportTo;
            crt.name = (string)new ReactionTypeToShortStringConverter().Convert(crt.reac_type, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

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
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

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
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

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
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

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
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

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
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

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
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

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
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);

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
            sc.entity_repository.reaction_templates.Add(crt);
            sc.entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);
            
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

            // BoundaryAssociation: CXCR5_membrane + CXCL13 -> CXCR5:CXCL13_membrane
            ConfigReaction cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.BoundaryAssociation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5_membrane", MoleculeLocation.Boundary, sc));
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5:CXCL13_membrane", MoleculeLocation.Boundary, sc));
            cr.rate_const = 2.0;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            // BoundaryDissociation:  CXCR5:CXCL13_membrane ->  CXCR5_membrane + CXCL13
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.BoundaryDissociation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5:CXCL13_membrane", MoleculeLocation.Boundary, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5_membrane", MoleculeLocation.Boundary, sc));
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, sc));
            cr.rate_const = 1.0;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            /////////////////////////////////////////////
            // NOTE: These next 2 seem to be holdover from previous naming convention. Look into eliminating.
            // Association: CXCR5 + CXCL13 -> CXCR5:CXCL13
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Association, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, sc));
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5:CXCL13", MoleculeLocation.Bulk, sc));
            cr.rate_const = 2.0;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            // Dissociation: CXCR5:CXCL13 -> CXCR5 + CXCL13
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Dissociation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5:CXCL13", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, sc));
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, sc));
            cr.rate_const = 2.0;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);
            //////////////////////////////////////////////////

            // Annihiliation: CXCR5 -> 
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Annihilation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, sc));
            cr.rate_const = 2.0;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            // Annihiliation: CXCR5:CXCL13 -> 
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Annihilation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCL13", MoleculeLocation.Bulk, sc));
            cr.rate_const = 2.0;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            // CatalyzedBoundaryActivation: CXCR5:CXCL13_membrane + A -> CXCR5:CXCL13_membrane + A*
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.CatalyzedBoundaryActivation, sc);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("CXCR5:CXCL13_membrane", MoleculeLocation.Boundary, sc));
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("A", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("A*", MoleculeLocation.Bulk, sc));
            cr.rate_const = 2.0;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            // Transformation: A -> A*
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Transformation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("A*", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("A", MoleculeLocation.Bulk, sc));
            cr.rate_const = 10.0;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            // Catalyzed Creation: gCXCR5 -> gCXCR5 + CXCR5
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.CatalyzedCreation, sc);
            // modifiers
            cr.modifiers_molecule_guid_ref.Add(findMoleculeGuid("gCXCR5", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, sc));
            cr.rate_const = 10.0;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            // BoundaryTransportTo: CXCR5 -> CXCR5_membrane
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Transformation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5_membrane", MoleculeLocation.Boundary, sc));
            cr.rate_const = 1.0;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            // BoundaryTransportFrom: CXCR5_membrane -> CXCR5
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Transformation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5_membrane", MoleculeLocation.Boundary, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5", MoleculeLocation.Bulk, sc));
            cr.rate_const = 1.0e-1;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            // BoundaryTransportFrom: CXCR5:CXCL13_membrane -> CXCR5:CXCL13
            cr = new ConfigReaction();
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Transformation, sc);
            // reactants
            cr.reactants_molecule_guid_ref.Add(findMoleculeGuid("CXCR5:CXCL13_membrane", MoleculeLocation.Boundary, sc));
            // products
            cr.products_molecule_guid_ref.Add(findMoleculeGuid("CXCR5:CXCL13", MoleculeLocation.Bulk, sc));
            cr.rate_const = 1.0e-1;
            cr.GetTotalReactionString(sc.entity_repository);
            sc.entity_repository.reactions.Add(cr);

            //Write out into json file!
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(sc.entity_repository.reactions, Newtonsoft.Json.Formatting.Indented, Settings);
            string jsonFile = "Config\\DaphnePredefinedReactions.txt";
            File.WriteAllText(jsonFile, jsonSpec);
        }

        private static void PredefinedReactionComplexesCreator(SimConfiguration sc)
        {
            ConfigReactionComplex crc = new ConfigReactionComplex("Ligand/Receptor");

            //MOLECULES
            var query =
                from mol in sc.entity_repository.molecules
                where mol.Name == "CXCR5" || mol.Name == "CXCR5:CXCL13" || mol.Name == "CXCL13"
                select mol;

            ConfigMolecularPopulation cmp = null;
            foreach (ConfigMolecule cm in query)
            {
                cmp = new ConfigMolecularPopulation();
                cmp.molecule_guid_ref = cm.molecule_guid;
                cmp.mpInfo = new MolPopInfo("My " + cm.Name);
                cmp.Name = "My " + cm.Name;
                cmp.mpInfo.mp_dist_name = "Constant level";
                cmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                cmp.mpInfo.mp_render_blending_weight = 2.0;
                cmp.mpInfo.mp_type_guid_ref = cm.Name;

                MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                if (cm.Name == "CXCR5")
                {
                    hl.concentration = 1.0;
                }
                else
                {
                    hl.concentration = 2.0;
                }
                cmp.mpInfo.mp_distribution = hl;

                crc.molpops.Add(cmp);
            }

            //REACTIONS
            string guid = findReactionGuid(ReactionType.Association, sc);
            crc.reactions_guid_ref.Add(guid);
            guid = findReactionGuid(ReactionType.Dissociation, sc);
            crc.reactions_guid_ref.Add(guid);

            foreach (ConfigReaction cr in sc.entity_repository.reactions)
            {
                foreach (ConfigReactionTemplate crt in sc.entity_repository.reaction_templates)
                {
                    if (cr.reaction_template_guid_ref == crt.reaction_template_guid && crt.reac_type == ReactionType.Annihilation)
                    {
                        crc.reactions_guid_ref.Add(cr.reaction_guid);
                    }
                }                
            }

            sc.entity_repository.reaction_complexes.Add(crc);

            //----------------------------

            crc = new ConfigReactionComplex("Bistable");
            foreach (ConfigMolecule cm in query)
            {
                cmp = new ConfigMolecularPopulation();
                cmp.molecule_guid_ref = cm.molecule_guid;
                cmp.mpInfo = new MolPopInfo("My " + cm.Name);
                cmp.Name = "My " + cm.Name;
                cmp.mpInfo.mp_dist_name = "Constant level";
                cmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                cmp.mpInfo.mp_render_blending_weight = 2.0;
                cmp.mpInfo.mp_type_guid_ref = cm.Name;

                MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                if (cm.Name == "CXCR5")
                {
                    hl.concentration = 2.0;
                }
                else
                {
                    hl.concentration = 4.0;
                }
                cmp.mpInfo.mp_distribution = hl;

                crc.molpops.Add(cmp);
            }

            guid = findReactionGuid(ReactionType.Association, sc);
            crc.reactions_guid_ref.Add(guid);
            guid = findReactionGuid(ReactionType.Dissociation, sc);
            crc.reactions_guid_ref.Add(guid);

            sc.entity_repository.reaction_complexes.Add(crc);

        }

        // given a string description of a reaction, return the ConfigReaction that matches
        public static ConfigReaction findReaction(string rt, SimConfiguration sc)
        {
            foreach (ConfigReaction cr in sc.entity_repository.reactions)
            {
                if (cr.TotalReactionString == rt) //cr.reaction_template_guid_ref == template_guid)
                {
                    return cr;
                }
            }
            return null;
        }
    }

}
