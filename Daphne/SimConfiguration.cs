﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;

using System.Xml.Serialization;
using Newtonsoft.Json;
using System.Linq;

using System.Windows.Media;
using System.Windows.Data;

namespace Daphne
{
    public class SimConfigurator
    {
        public string FileName { get; set; }
        public SimConfiguration SimConfig { get; set; }

        public SimConfigurator()
        {
            this.SimConfig = new SimConfiguration();
        }

        public SimConfigurator(string filename)
        {
            if (filename == null)
                throw new ArgumentNullException("filename");

            this.FileName = filename;
            this.SimConfig = new SimConfiguration();
        }

        public void SerializeSimConfigToFile()
        {
            //skg daphne serialize to json Thursday, April 18, 2013
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(SimConfig, Newtonsoft.Json.Formatting.Indented, Settings);
            string jsonFile = FileName;
            File.WriteAllText(jsonFile, jsonSpec);
        }

        /// <summary>
        /// brief version of serialize config to string
        /// </summary>
        /// <returns></returns>
        public string SerializeSimConfigToString()
        {
            //skg daphne serialize to json string Wednesday, May 08, 2013
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(SimConfig, Newtonsoft.Json.Formatting.Indented, Settings);
            return jsonSpec;
        }

        /// <summary>
        /// serialize the config to a string, skip the 'decorations', i.e. experiment name and description
        /// </summary>
        /// <returns></returns>
        public string SerializeSimConfigToStringSkipDeco()
        {
            // remember name and description
            string exp_name = SimConfig.experiment_name,
                   exp_desc = SimConfig.experiment_description,
                   ret;

            // temporarily set name and description to empty strings
            SimConfig.experiment_name = "";
            SimConfig.experiment_description = "";
            // serialize to string
            ret = SerializeSimConfigToString();
            // reset to the remembered string values
            SimConfig.experiment_name = exp_name;
            SimConfig.experiment_description = exp_desc;
            // return serialized string
            return ret;
        }

        public void DeserializeSimConfig()
        {
            //Deserialize JSON - THIS CODE WORKS - PUT IT IN APPROPRIATE PLACE (INITIALSTATE OR SOMETHING) - REPLACE XML WITH THIS
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            string readText = File.ReadAllText(FileName);
            SimConfig = JsonConvert.DeserializeObject<SimConfiguration>(readText, settings);
            SimConfig.InitializeStorageClasses();
        }

        public void DeserializeSimConfigFromString(string simConfigJson)
        {
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            SimConfig = JsonConvert.DeserializeObject<SimConfiguration>(simConfigJson, settings);
            SimConfig.InitializeStorageClasses();
        }
    }

    public class SimConfiguration
    {
        public static int SafeCellPopulationID = 0;
        public int experiment_db_id { get; set; }
        public string experiment_name { get; set; }
        public int experiment_reps { get; set; }
        public string experiment_guid { get; set; }
        public string experiment_description { get; set; }
        public Scenario scenario { get; set; }
        public EntityRepository entity_repository { get; set; }

        //[XmlIgnore]
        //[JsonIgnore]
        //public ChartViewToolWindow ChartWindow;

        // Convenience utility storage (not serialized)
        // NOTE: These could be moved to entity_repository...
        [XmlIgnore]
        public Dictionary<string, BoxSpecification> box_guid_box_dict;
        [XmlIgnore]
        public Dictionary<int, CellPopulation> cellpopulation_id_cellpopulation_dict;   

        public SimConfiguration()
        {
            Guid id = Guid.NewGuid();
            experiment_guid = id.ToString();
            experiment_db_id = 0;
            experiment_name = "Experiment1";
            experiment_reps = 1;
            experiment_description = "Whole sim config description";
            scenario = new Scenario();
            entity_repository = new EntityRepository();

            //entity_repository.PredefReactions = new ObservableCollection<ConfigReaction>();
            //entity_repository.PredefMolecules = new ObservableCollection<ConfigMolecule>();
            entity_repository.PredefReactionComplexes = new ObservableCollection<GuiReactionComplex>();
            entity_repository.PredefCells = new ObservableCollection<ConfigCell>();

            entity_repository.AllMolecules = new ObservableCollection<ConfigMolecule>();
            entity_repository.AllReactions = new ObservableCollection<ConfigReaction>();

            ////LoadDefaultGlobalParameters();

            ////entity_repository.UserdefMolecules = new ObservableCollection<ConfigMolecule>();
            ////entity_repository.UserdefReactions = new ObservableCollection<ConfigReaction>();
            ////entity_repository.UserdefReactionComplexes = new ObservableCollection<GuiReactionComplex>();
            ////entity_repository.UserdefCells = new ObservableCollection<ConfigCell>();

            //LoadUserDefinedItems();
            

            // Utility storage
            // NOTE: No use adding CollectionChanged event handlers here since it gets wiped out by deserialization anyway...
            box_guid_box_dict = new Dictionary<string, BoxSpecification>();
            cellpopulation_id_cellpopulation_dict = new Dictionary<int, CellPopulation>();   
        }

        /// <summary>
        /// Routine called when the environment extent changes
        /// Updates all box specifications in repository with correct max & min for sliders in GUI
        /// Also updates VTK visual environment box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void environment_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Update all BoxSpecifications
            ////foreach (BoxSpecification bs in entity_repository.box_specifications)
            ////{
            ////    // NOTE: Uncomment and add in code if really need to adjust directions independently for efficiency
            ////    //if (e.PropertyName == "extent_x")
            ////    //{
            ////    //    // set x direction
            ////    //}
            ////    //if (e.PropertyName == "extent_y")
            ////    //{
            ////    //    // set x direction
            ////    //}
            ////    //if (e.PropertyName == "extent_z")
            ////    //{
            ////    //    // set x direction
            ////    //}

            ////    // For now just setting all every time...
            ////    SetBoxSpecExtents(bs);
            ////}

            // Update VTK environment box 
            var env = scenario.environment;
            /////MainWindow.VTKBasket.EnvironmentController.setupBox(env.extent_x, env.extent_y, env.extent_z);
            // NOTE: Rwc.Invalidate happens to also be called because every box specification has a callback to
            //   GUIInteractionToWidgetCallback on its PropertyChanged, and that calls Invalidate, but wanted to
            //   call it here, too, for environment change explicitly just in case box specs are handled differently
            //   in the future, didn't want the environment box to stop updating "live" mysteriously.
            /////MainWindow.GC.Rwc.Invalidate();
        }

        private void SetBoxSpecExtents(BoxSpecification bs)
        {
            bs.x_scale_max = scenario.environment.extent_x;
            bs.x_scale_min = scenario.environment.extent_min;
            bs.x_trans_max = 1.5 * scenario.environment.extent_x;
            bs.x_trans_min = -scenario.environment.extent_x / 2.0;

            bs.y_scale_max = scenario.environment.extent_y;
            bs.y_scale_min = scenario.environment.extent_min;
            bs.y_trans_max = 1.5 * scenario.environment.extent_y;
            bs.y_trans_min = -scenario.environment.extent_y / 2.0;

            bs.z_scale_max = scenario.environment.extent_z;
            bs.z_scale_min = scenario.environment.extent_min;
            bs.z_trans_max = 1.5 * scenario.environment.extent_z;
            bs.z_trans_min = -scenario.environment.extent_z / 2.0;
        }

        private void PREDEFINEDCELLSCREATOR()
        {
            
            //---------------------------------------------------------------------------------------
            //BCell 

            ConfigCell gc = new ConfigCell();
            gc.CellName = "BCell";
            gc.CellRadius = 5.0;

            //MOLECULES IN MEMBRANE
            var query1 =
                from mol in entity_repository.AllMolecules
                where mol.Name == "CXCR5" || mol.Name == "CXCR5:CXCL13"
                select mol;

            ConfigMolecularPopulation gmp = null;
            foreach (ConfigMolecule gm in query1)
            {
                gmp = new ConfigMolecularPopulation();
                gmp.Molecule = new ConfigMolecule(gm);
                gmp.mpInfo = new MolPopInfo("My " + gm.Name);
                gmp.Name = "My " + gm.Name;
                if (gm.Name == "CXCR5" || gm.Name == "CXCR5:CXCL13")
                {
                    gmp.Location = MolPopPosition.Membrane;
                }

                gmp.mpInfo.mp_dist_name = "Gaussian gradient";
                gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                gmp.mpInfo.mp_render_blending_weight = 2.0;
                //MolPopGaussianGradient sgg = new MolPopGaussianGradient();
                //if (gm.Name == "CXCR5")
                //    sgg.peak_concentration = 125;
                //else
                //    sgg.peak_concentration = 130;
                //sgg.gaussgrad_gauss_spec_guid_ref = entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
                //gmp.mpInfo.mp_distribution = sgg;
                
                gc.CellMolPops.Add(gmp);
            }

            //MOLECULES IN CYTOSOL
            var query2 =
                from mol in entity_repository.AllMolecules
                where mol.Name == "driver"
                select mol;

            gmp = null;
            foreach (ConfigMolecule gm in query2)
            {
                gmp = new ConfigMolecularPopulation();
                gmp.Molecule = new ConfigMolecule(gm);
                gmp.mpInfo = new MolPopInfo("My " + gm.Name);
                gmp.Name = "My " + gm.Name;
                if (gm.Name == "driver")
                {
                    gmp.Location = MolPopPosition.Cytosol;
                }

                gmp.mpInfo.mp_dist_name = "Gaussian gradient";
                gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                gmp.mpInfo.mp_render_blending_weight = 2.0;
                //MolPopGaussianGradient sgg = new MolPopGaussianGradient();
                //sgg.peak_concentration = 250;
                //sgg.gaussgrad_gauss_spec_guid_ref = entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
                //gmp.mpInfo.mp_distribution = sgg;

                gc.CellMolPops.Add(gmp);
            }

            entity_repository.PredefCells.Add(gc);

            //---------------------------------------------------------------------------------------
            //TCell 

            gc = new ConfigCell();
            gc.CellName = "TCell";
            gc.CellRadius = 5.0;

            entity_repository.PredefCells.Add(gc);

            gc = new ConfigCell();
            gc.CellName = "FDC";
            gc.CellRadius = 5.0;

            entity_repository.PredefCells.Add(gc);

            //Write out into json file!
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(entity_repository.PredefCells, Newtonsoft.Json.Formatting.Indented, Settings);
            string jsonFile = "Config\\DaphnePredefinedCells.txt";
            File.WriteAllText(jsonFile, jsonSpec);
        }

        //Following function needs to be called only once
        private void PREDEFINEDREACTIONSCREATOR()
        {

            //Test code to read in json containing object "PredefinedReactions"
            //string readText = File.ReadAllText("TESTER.TXT");
            //PredefReactions = JsonConvert.DeserializeObject<ObservableCollection<ConfigReaction>>(readText);

            //---------------------------
            ObservableCollection<ConfigReaction> predefReacs = new ObservableCollection<ConfigReaction>();

            //BoundaryAssociation
            GuiBoundaryReactionTemplate gbrt = new GuiBoundaryReactionTemplate();

            GuiSpeciesReference gsr = new GuiSpeciesReference();
            gsr.species = "CXCR5";
            gsr.stoichiometry = 1;
            gsr.Location = "membrane";
            gbrt.listOfReactants.Add(gsr);
            gbrt.receptor = gsr;

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCL13";
            gsr.stoichiometry = 1;
            gsr.Location = "ecs";
            gbrt.listOfReactants.Add(gsr);
            gbrt.ligand = gsr;

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCR5:CXCL13";
            gsr.stoichiometry = 1;
            gsr.Location = "membrane";
            gbrt.listOfProducts.Add(gsr);
            gbrt.complex = gsr;

            gbrt.RateConst = 2.0;
            gbrt.ReacType = ReactionType.BoundaryAssociation;

            predefReacs.Add(gbrt);
            //entity_repository.PredefReactions.Add(gbrt);

            //----------------------------------------------
            //BoundaryDissociation
            gbrt = new GuiBoundaryReactionTemplate();

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCR5";
            gsr.stoichiometry = 1;
            gsr.Location = "membrane";
            gbrt.listOfProducts.Add(gsr);
            gbrt.receptor = gsr;

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCL13";
            gsr.stoichiometry = 1;
            gsr.Location = "ecs";
            gbrt.listOfProducts.Add(gsr);
            gbrt.ligand = gsr;

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCR5:CXCL13";
            gsr.stoichiometry = 1;
            gsr.Location = "membrane";
            gbrt.listOfReactants.Add(gsr);
            gbrt.complex = gsr;

            gbrt.RateConst = 2.0;
            gbrt.ReacType = ReactionType.BoundaryDissociation;

            predefReacs.Add(gbrt);
            //entity_repository.PredefReactions.Add(gbrt);

            //Association
            ConfigReaction grt = new ConfigReaction();

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCR5";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            grt.listOfReactants.Add(gsr);

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCL13";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            grt.listOfReactants.Add(gsr);

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCR5:CXCL13";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            grt.listOfProducts.Add(gsr);

            grt.RateConst = 2.0;
            grt.ReacType = ReactionType.Association;

            predefReacs.Add(grt);
            //entity_repository.PredefReactions.Add(grt);

            //Dissociation
            grt = new ConfigReaction();

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCR5:CXCL13";
            gsr.stoichiometry = 1;
            gsr.Location = "none";
            grt.listOfReactants.Add(gsr);

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCR5";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            grt.listOfProducts.Add(gsr);

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCL13";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            grt.listOfProducts.Add(gsr);

            grt.RateConst = 2.0;
            grt.ReacType = ReactionType.Dissociation;
            predefReacs.Add(grt);
            //entity_repository.PredefReactions.Add(grt);

            //Dissociation
            grt = new ConfigReaction();

            gsr = new GuiSpeciesReference();
            gsr.species = "E";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            grt.listOfReactants.Add(gsr);

            gsr = new GuiSpeciesReference();
            gsr.species = "A";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            grt.listOfProducts.Add(gsr);

            gsr = new GuiSpeciesReference();
            gsr.species = "E";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            grt.listOfProducts.Add(gsr);

            grt.RateConst = 1.0;
            grt.ReacType = ReactionType.Dissociation;
            predefReacs.Add(grt);
            //entity_repository.PredefReactions.Add(grt);

            //CatalyzedCreation
            GuiCatalyzedReactionTemplate gcrt = new GuiCatalyzedReactionTemplate();

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCR5";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            gcrt.listOfProducts.Add(gsr);

            gsr = new GuiSpeciesReference();
            gsr.species = "gCXCR5";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            gcrt.listOfModifiers.Add(gsr);

            gcrt.RateConst = 2.0;
            gcrt.catalyst = gsr;
            gcrt.ReacType = ReactionType.CatalyzedCreation;
            predefReacs.Add(grt);
            //entity_repository.PredefReactions.Add(grt);

            //CatalyzedCreation
            gcrt = new GuiCatalyzedReactionTemplate();
            
            gsr = new GuiSpeciesReference();
            gsr.species = "E";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            gcrt.listOfModifiers.Add(gsr);

            gsr = new GuiSpeciesReference();
            gsr.species = "X";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            gcrt.listOfProducts.Add(gsr);

            gcrt.RateConst = 0.6;
            gcrt.catalyst = gsr;
            gcrt.ReacType = ReactionType.CatalyzedCreation;

            predefReacs.Add(gcrt);
            //entity_repository.PredefReactions.Add(gcrt);

            //Annihilation
            grt = new ConfigReaction();

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCR5";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            grt.listOfReactants.Add(gsr);

            grt.RateConst = 2.0;
            grt.ReacType = ReactionType.Annihilation;

            predefReacs.Add(grt);
            //entity_repository.PredefReactions.Add(grt);

            //Annihilation
            grt = new ConfigReaction();

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCL13";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            grt.listOfReactants.Add(gsr);

            grt.RateConst = 2.0;
            grt.ReacType = ReactionType.Annihilation;
            predefReacs.Add(grt);
            //entity_repository.PredefReactions.Add(grt);

            //Annihilation
            grt = new ConfigReaction();

            gsr = new GuiSpeciesReference();
            gsr.species = "Y";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            grt.listOfReactants.Add(gsr);

            grt.RateConst = 1.0;
            grt.ReacType = ReactionType.Annihilation;
            predefReacs.Add(grt);
            //entity_repository.PredefReactions.Add(grt);

            //CatalyzedAnnihilation
            grt = new ConfigReaction();

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCR5";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            grt.listOfModifiers.Add(gsr);

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCL13";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            grt.listOfReactants.Add(gsr);

            grt.RateConst = 2.0;
            grt.ReacType = ReactionType.CatalyzedAnnihilation;
            predefReacs.Add(grt);
            //entity_repository.PredefReactions.Add(grt);

            //CatalyzedAnnihilation
            gcrt = new GuiCatalyzedReactionTemplate();

            gsr = new GuiSpeciesReference();
            gsr.species = "Y";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            gcrt.listOfModifiers.Add(gsr);

            gsr = new GuiSpeciesReference();
            gsr.species = "X";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            gcrt.listOfReactants.Add(gsr);

            gcrt.RateConst = 2.0;
            gcrt.ReacType = ReactionType.CatalyzedAnnihilation;

            predefReacs.Add(grt);
            //entity_repository.PredefReactions.Add(gcrt);

            //CatalyzedDimerDissociation
            gcrt = new GuiCatalyzedReactionTemplate();

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCR5";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            gcrt.listOfReactants.Add(gsr);

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCR5:CXCL13";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            gcrt.listOfProducts.Add(gsr);

            gsr = new GuiSpeciesReference();
            gsr.species = "CXCL13";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            gcrt.listOfModifiers.Add(gsr);

            gcrt.RateConst = 2.0;
            gcrt.ReacType = ReactionType.CatalyzedDimerDissociation;
            gcrt.catalyst = gsr;
            predefReacs.Add(gcrt);
            //entity_repository.PredefReactions.Add(gcrt);

            //Generalized
            grt = new ConfigReaction();

            gsr = new GuiSpeciesReference();
            gsr.species = "A";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            grt.listOfReactants.Add(gsr);

            gsr = new GuiSpeciesReference();
            gsr.species = "X";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            grt.listOfReactants.Add(gsr);

            gsr = new GuiSpeciesReference();
            gsr.species = "X";
            gsr.stoichiometry = 2;
            gsr.Location = "na";
            grt.listOfProducts.Add(gsr);

            grt.RateConst = 1.0;
            grt.ReacType = ReactionType.Generalized;
            
            predefReacs.Add(grt);
            //entity_repository.PredefReactions.Add(grt);

            //Generalized
            grt = new ConfigReaction();

            gsr = new GuiSpeciesReference();
            gsr.species = "X";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            grt.listOfReactants.Add(gsr);

            gsr = new GuiSpeciesReference();
            gsr.species = "Y";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            grt.listOfReactants.Add(gsr);

            gsr = new GuiSpeciesReference();
            gsr.species = "Y";
            gsr.stoichiometry = 2;
            gsr.Location = "na";
            grt.listOfProducts.Add(gsr);

            grt.RateConst = 1.0;
            grt.ReacType = ReactionType.Generalized;

            predefReacs.Add(grt);
            //entity_repository.PredefReactions.Add(grt);

            //Generalized
            grt = new ConfigReaction();

            gsr = new GuiSpeciesReference();
            gsr.species = "X";
            gsr.stoichiometry = 2;
            gsr.Location = "na";
            grt.listOfReactants.Add(gsr);

            gsr = new GuiSpeciesReference();
            gsr.species = "X";
            gsr.stoichiometry = 1;
            gsr.Location = "na";
            grt.listOfProducts.Add(gsr);

            gsr = new GuiSpeciesReference();
            gsr.species = "Y";
            gsr.stoichiometry = 2;
            gsr.Location = "na";
            grt.listOfProducts.Add(gsr);

            grt.RateConst = 1.0;
            grt.ReacType = ReactionType.Generalized;
            predefReacs.Add(grt);
            //entity_repository.PredefReactions.Add(grt);


            //Write out into json file!
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(predefReacs, Newtonsoft.Json.Formatting.Indented, Settings);
            string jsonFile = "Config\\DaphnePredefinedReactions.txt";
            File.WriteAllText(jsonFile, jsonSpec);

        }

        public void LoadDefaultGlobalParameters()
        {
            //skg daphne
            PREDEFINEDREACTIONSCREATOR();
            
            string path = "Config\\DaphnePredefinedReactions.txt";
            //string readText = File.ReadAllText(path);
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            string readText = File.ReadAllText(path);

            ObservableCollection<ConfigReaction> predefReacs = new ObservableCollection<ConfigReaction>();
            predefReacs = JsonConvert.DeserializeObject<ObservableCollection<ConfigReaction>>(readText, settings);
            foreach (ConfigReaction cr in predefReacs)
            {
                entity_repository.AllReactions.Add(cr);
            }

            //Old way
            ////entity_repository.PredefReactions = JsonConvert.DeserializeObject<ObservableCollection<ConfigReaction>>(readText, settings);

            //GuiBoundaryReactionTemplate grt = (GuiBoundaryReactionTemplate)PredefReactions[0]; 

            //Code to read predef mols from a file
            path = "Config\\DaphnePredefinedMolecules.txt";
            readText = File.ReadAllText(path);

            //First method
            ObservableCollection<ConfigMolecule> predefMols = new ObservableCollection<ConfigMolecule>();
            predefMols = JsonConvert.DeserializeObject<ObservableCollection<ConfigMolecule>>(readText);
            foreach (ConfigMolecule gm in predefMols)
            {
                entity_repository.AllMolecules.Add(gm);
            }

            //var Settings = new JsonSerializerSettings();
            //Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            //Settings.TypeNameHandling = TypeNameHandling.Auto;
            //string jsonSpec = JsonConvert.SerializeObject(entity_repository.AllMolecules, Newtonsoft.Json.Formatting.Indented, Settings);
            //string jsonFile = path;
            //File.WriteAllText(jsonFile, jsonSpec);

            //Alternate method - if we want everything including predef mols to be stored in one scenario file
            //entity_repository.AllMolecules = JsonConvert.DeserializeObject<ObservableCollection<ConfigMolecule>>(readText);


            PREDEFINEDCELLSCREATOR();

            entity_repository.PredefCells.Clear();
            path = "Config\\DaphnePredefinedCells.txt";
            readText = File.ReadAllText(path);
            entity_repository.PredefCells = JsonConvert.DeserializeObject<ObservableCollection<ConfigCell>>(readText, settings);

            GuiReactionComplex rc = new GuiReactionComplex();
            rc.Name = "Bistable";
            rc.Reactions.Add(entity_repository.AllReactions[5]);
            rc.Reactions.Add(entity_repository.AllReactions[6]);
            rc.Reactions.Add(entity_repository.AllReactions[7]);
            rc.Reactions.Add(entity_repository.AllReactions[8]);
            entity_repository.PredefReactionComplexes.Add(rc);

            rc = new GuiReactionComplex();
            rc.Name = "Receptor/Ligand";
            rc.Reactions.Add(entity_repository.AllReactions[0]);
            rc.Reactions.Add(entity_repository.AllReactions[1]);
            rc.Reactions.Add(entity_repository.AllReactions[2]);
            rc.Reactions.Add(entity_repository.AllReactions[3]);
            entity_repository.PredefReactionComplexes.Add(rc);
        }

        public void CreateAndSerializeLigandReceptorScenario()
        {
            // Experiment
            experiment_name = "Ligand Receptor Scenario";
            experiment_description = "Initial scenario with predefined Molecules and Reactions, Compartment ECM with molecular populations, reactions, reaction complexes, and manifold";
            scenario.time_config.duration = 100;
            scenario.time_config.rendering_interval = 0.3;
            scenario.time_config.sampling_interval = 1440;

            // Global Paramters
            LoadDefaultGlobalParameters();
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
            entity_repository.box_specifications.Add(box);
            gg.gaussian_spec_box_guid_ref = box.box_guid;
            gg.gaussian_spec_name = "Off-center gaussian";
            gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            entity_repository.gaussian_specifications.Add(gg);

            //ADD ECS MOL POPS
            //string molSpec = "CXCR5\t1.0\t0.0\t1.0\nCXCL13\t\t\t6.0e3\nCXCR5:CXCL13\t\t\t0.0\ngCXCR5\t\t\t\ndriver\t\t\t\nCXCL12\t7.96\t\t6.0e3\n";
            //SKG DAPHNE Wednesday, April 10, 2013 4:04:14 PM
            //THIS VAR IS NOT OK
            var query =
                from mol in entity_repository.AllMolecules
                where mol.Name == "CXCL13"
                select mol;

            ConfigMolecularPopulation gmp = null;
            foreach (ConfigMolecule gm in query)
            {
                gmp = new ConfigMolecularPopulation();
                gmp.Molecule = new ConfigMolecule(gm);
                gmp.mpInfo = new MolPopInfo("My " + gm.Name);
                gmp.Name = "My " + gm.Name;
                gmp.Location = MolPopPosition.ECS;
                gmp.mpInfo.mp_dist_name = "Gaussian gradient";
                gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                gmp.mpInfo.mp_render_blending_weight = 2.0;
                MolPopGaussianGradient sgg = new MolPopGaussianGradient();
                sgg.peak_concentration = 10;
                //sgg.gaussgrad_gauss_spec_guid_ref = entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
                gmp.mpInfo.mp_distribution = sgg;
                scenario.MolPops.Add(gmp);
            }

            //ADD CELLS AND MOLECULES IN THE CELLS
            ConfigCell gc = new ConfigCell();
            gc.CellName = "BCell";
            gc.CellRadius = 4.0;
           
            CellPopulation cp = new CellPopulation();
            cp.cellpopulation_name = "My BCell";
            cp.number = 1;
            cp.cellpopulation_constrained_to_region = true;
            cp.wrt_region = RelativePosition.Inside;
            cp.cellpopulation_color = System.Windows.Media.Color.FromScRgb(1.0f, 0.30f, 0.69f, 0.29f);
            cp.CellType = gc;

            var query1 =
                from mol in entity_repository.AllMolecules
                where mol.Name == "CXCR5" || mol.Name == "CXCR5:CXCL13"
                select mol;

            gmp = null;
            foreach (ConfigMolecule gm in query1)
            {
                gmp = new ConfigMolecularPopulation();
                gmp.Molecule = new ConfigMolecule(gm);
                gmp.mpInfo = new MolPopInfo("My " + gm.Name);
                gmp.Name = "My " + gm.Name;
                if (gm.Name == "CXCR5" || gm.Name == "CXCR5:CXCL13")
                {
                    gmp.Location = MolPopPosition.Membrane;
                }

                gmp.mpInfo.mp_dist_name = "Gaussian gradient";
                gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                gmp.mpInfo.mp_render_blending_weight = 2.0;
                MolPopGaussianGradient sgg = new MolPopGaussianGradient();
                sgg.peak_concentration = 10;
                //sgg.gaussgrad_gauss_spec_guid_ref = entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
                gmp.mpInfo.mp_distribution = sgg;

                gc.CellMolPops.Add(gmp);
            }

            //NO REACTIONS INSIDE CELL FOR THIS SCENARIO

            scenario.cellpopulations.Add(cp);


            //---------------------------------------------------------------

            //EXTERNAL REACTIONS - I.E. IN EXTRACELLULAR SPACE
            GuiBoundaryReactionTemplate grt = (GuiBoundaryReactionTemplate)(entity_repository.AllReactions[0]);    //The 0'th reaction is Boundary Association
            scenario.Reactions.Add(grt);

            grt = (GuiBoundaryReactionTemplate)entity_repository.AllReactions[1];    //The 1st reaction is Boundary Dissociation
            scenario.Reactions.Add(grt);

        }


        /// <summary>
        /// 
        /// </summary>
        public void CreateAndSerializeDriverLocomotionScenario()
        {
            // Experiment
            experiment_name = "Driver Locomotion Scenario";
            experiment_description = "Initial scenario with predefined Molecules and Reactions, Compartment ECM with molecular populations, reactions, reaction complexes, manifold, locomotor";
            scenario.time_config.duration = 100;
            scenario.time_config.rendering_interval = 0.3;
            scenario.time_config.sampling_interval = 1440;

            // Global Paramters
            LoadDefaultGlobalParameters();
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
            entity_repository.box_specifications.Add(box);
            gg.gaussian_spec_box_guid_ref = box.box_guid;
            gg.gaussian_spec_name = "Off-center gaussian";
            gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            entity_repository.gaussian_specifications.Add(gg);

            var query =
                from mol in entity_repository.AllMolecules
                where mol.Name == "CXCL13"
                select mol;

            ConfigMolecularPopulation gmp = null;
            foreach (ConfigMolecule gm in query)
            {
                gmp = new ConfigMolecularPopulation();
                gmp.Molecule = new ConfigMolecule(gm);
                gmp.mpInfo = new MolPopInfo("My " + gm.Name);
                gmp.Name = "My " + gm.Name;
                gmp.Location = MolPopPosition.ECS;
                gmp.mpInfo.mp_dist_name = "Gaussian gradient";
                gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                gmp.mpInfo.mp_render_blending_weight = 2.0;
                MolPopGaussianGradient sgg = new MolPopGaussianGradient();
                sgg.peak_concentration = 10;
                //sgg.gaussgrad_gauss_spec_guid_ref = entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
                gmp.mpInfo.mp_distribution = sgg;
                scenario.MolPops.Add(gmp);
            }

            //ADD CELLS AND MOLECULES IN THE CELLS
            ConfigCell gc = new ConfigCell();
            gc.CellName = "BCell";
            gc.CellRadius = 4.0;

            CellPopulation cp = new CellPopulation();
            cp.cellpopulation_name = "My-B-Cell";
            cp.number = 1;
            cp.cellpopulation_constrained_to_region = true;
            cp.wrt_region = RelativePosition.Inside;
            cp.cellpopulation_color = System.Windows.Media.Color.FromScRgb(1.0f, 0.30f, 0.69f, 0.29f);
            cp.CellType = gc;

            //MOLECULES IN MEMBRANE
            var query1 =
                from mol in entity_repository.AllMolecules
                where mol.Name == "CXCR5" || mol.Name == "CXCR5:CXCL13"
                select mol;

            gmp = null;
            foreach (ConfigMolecule gm in query1)
            {
                gmp = new ConfigMolecularPopulation();
                gmp.Molecule = new ConfigMolecule(gm);
                gmp.mpInfo = new MolPopInfo("My " + gm.Name);
                gmp.Name = "My " + gm.Name;
                if (gm.Name == "CXCR5" || gm.Name == "CXCR5:CXCL13")
                {
                    gmp.Location = MolPopPosition.Membrane;
                }

                gmp.mpInfo.mp_dist_name = "Gaussian gradient";
                gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                gmp.mpInfo.mp_render_blending_weight = 2.0;
                MolPopGaussianGradient sgg = new MolPopGaussianGradient();
                if (gm.Name == "CXCR5")
                    sgg.peak_concentration = 125;
                else
                    sgg.peak_concentration = 130;
                //sgg.gaussgrad_gauss_spec_guid_ref = entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
                gmp.mpInfo.mp_distribution = sgg;

                gc.CellMolPops.Add(gmp);
            }

            //MOLECULES IN CYTOSOL
            var query2 =
                from mol in entity_repository.AllMolecules
                where mol.Name == "driver"
                select mol;

            gmp = null;
            foreach (ConfigMolecule gm in query2)
            {
                gmp = new ConfigMolecularPopulation();
                gmp.Molecule = new ConfigMolecule(gm);
                gmp.mpInfo = new MolPopInfo("My " + gm.Name);
                gmp.Name = "My " + gm.Name;
                if (gm.Name == "driver")
                {
                    gmp.Location = MolPopPosition.Cytosol;
                }

                gmp.mpInfo.mp_dist_name = "Gaussian gradient";
                gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                gmp.mpInfo.mp_render_blending_weight = 2.0;
                MolPopGaussianGradient sgg = new MolPopGaussianGradient();
                sgg.peak_concentration = 250;
                //sgg.gaussgrad_gauss_spec_guid_ref = entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
                gmp.mpInfo.mp_distribution = sgg;

                gc.CellMolPops.Add(gmp);
            }

            //NO REACTIONS INSIDE CELL FOR THIS SCENARIO

            scenario.cellpopulations.Add(cp);

            //-------------------------------------------------------------

            //EXTERNAL REACTIONS - I.E. IN EXTRACELLULAR SPACE
            GuiBoundaryReactionTemplate grt = (GuiBoundaryReactionTemplate)(entity_repository.AllReactions[0]);    //The 0'th reaction is Boundary Association

            scenario.Reactions.Add(grt);
            grt = new GuiBoundaryReactionTemplate();
            grt = (GuiBoundaryReactionTemplate)entity_repository.AllReactions[1];    //The 1st reaction is Boundary Dissociation

            scenario.Reactions.Add(grt);
        }

        /// <summary>
        /// New default scenario for first pass of Daphne
        /// </summary>
        public void CreateAndSerializeDiffusionScenario()
        {
            // Experiment
            experiment_name = "Diffusion Scenario";
            experiment_description = "Initial scenario with 1 Compartment ECM with 1 molecular population, no cells or reactions";
            scenario.time_config.duration = 100;
            scenario.time_config.rendering_interval = 0.3;
            scenario.time_config.sampling_interval = 1440;

            // Global Paramters
            LoadDefaultGlobalParameters();
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
            entity_repository.box_specifications.Add(box);
            gg.gaussian_spec_box_guid_ref = box.box_guid;
            gg.gaussian_spec_name = "Off-center gaussian";
            gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            entity_repository.gaussian_specifications.Add(gg);

            //SKG DAPHNE Wednesday, April 10, 2013 4:04:14 PM
            var query =
                from mol in entity_repository.AllMolecules
                where mol.Name == "CXCL13"
                select mol;

            ConfigMolecularPopulation gmp = null;
            foreach (ConfigMolecule gm in query)
            {
                gmp = new ConfigMolecularPopulation();
                gmp.Molecule = new ConfigMolecule(gm);
                gmp.mpInfo = new MolPopInfo("My " + gm.Name);
                gmp.Name = "My " + gm.Name;
                gmp.mpInfo.mp_dist_name = "Gaussian gradient";
                gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                gmp.mpInfo.mp_render_blending_weight = 2.0;
                MolPopGaussianGradient sgg = new MolPopGaussianGradient();
                sgg.peak_concentration = 10;
                //sgg.gaussgrad_gauss_spec_guid_ref = entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
                gmp.mpInfo.mp_distribution = sgg;
                scenario.MolPops.Add(gmp);
            }

        }


        /// <summary>
        /// CollectionChanged not called during deserialization, so manual call to set up utility classes.
        /// Also take care of any other post-deserialization setup.
        /// </summary>
        public void InitializeStorageClasses()
        {
            // GenerateNewExperimentGUID();
            FindNextSafeCellPopulationID();
            InitBoxExtentsAndGuidBoxDict();
            InitCellPopulationIDCellPopulationDict();
            // Set callback to update box specification extents when environment extents change
            scenario.environment.PropertyChanged += new PropertyChangedEventHandler(environment_PropertyChanged);
        }

        /// <summary>
        /// Any time need a new experiment GUID, such as before each run
        /// or after deserialization.
        /// </summary>
        public void GenerateNewExperimentGUID()
        {
            Guid id = Guid.NewGuid();
            experiment_guid = id.ToString();
        }

        /// <summary>
        /// Making sure that SafeCellPopulationID is greater than largest ID read in after deserialization.
        /// </summary>
        public void FindNextSafeCellPopulationID()
        {
            int max_id = 0;
            foreach (CellPopulation cs in scenario.cellpopulations)
            {
                if (cs.cellpopulation_id > max_id)
                    max_id = cs.cellpopulation_id;
            }
            SafeCellPopulationID = max_id + 1;
        }

        private void InitBoxExtentsAndGuidBoxDict()
        {
            box_guid_box_dict.Clear();
            foreach (BoxSpecification bs in entity_repository.box_specifications)
            {
                box_guid_box_dict.Add(bs.box_guid, bs);

                // Piggyback on this routine to set initial extents from environment values
                SetBoxSpecExtents(bs);
            }
            entity_repository.box_specifications.CollectionChanged += new NotifyCollectionChangedEventHandler(box_specifications_CollectionChanged);
        }

        private void InitCellPopulationIDCellPopulationDict()
        {
            cellpopulation_id_cellpopulation_dict.Clear();  
            foreach (CellPopulation cs in scenario.cellpopulations)
            {
                cellpopulation_id_cellpopulation_dict.Add(cs.cellpopulation_id, cs); 
            }
            scenario.cellpopulations.CollectionChanged += new NotifyCollectionChangedEventHandler(cellsets_CollectionChanged);

        }

        private void box_specifications_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    BoxSpecification bs = nn as BoxSpecification;
                    box_guid_box_dict.Add(bs.box_guid, bs);

                    // Piggyback on this callback to set extents from environment for new box specifications
                    SetBoxSpecExtents(bs);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    BoxSpecification bs = dd as BoxSpecification;
                    box_guid_box_dict.Remove(bs.box_guid);
                }
            }
        }

        private void cellsets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    CellPopulation cs = nn as CellPopulation;
                    cellpopulation_id_cellpopulation_dict.Add(cs.cellpopulation_id, cs);   
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    CellPopulation cs = dd as CellPopulation;
                    cellpopulation_id_cellpopulation_dict.Remove(cs.cellpopulation_id);   
                }
            }
        }

        public ConfigMolecule FindMolecule(string name)
        {
            ConfigMolecule gm = null;

            foreach (ConfigMolecule g in entity_repository.AllMolecules)
            {
                if (g.Name == name)
                {
                    gm = g;
                    break;
                }
            }
            return gm;
        }
    }

    // start at > 0 as zero seems to be the default for metadata when a property is not present
    public enum SimStates { Linear = 1, Cubic, Tiny, Large };

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(SimStates), typeof(string))]
    public class SimStatesToShortStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the SimStates enum...
        private List<string> _sim_states_strings = new List<string>()
                                {
                                    "linear",
                                    "cubic",
                                    "tiny",
                                    "large"
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _sim_states_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _sim_states_strings.FindIndex(item => item == str);
            return (SimStates)Enum.ToObject(typeof(SimStates), (int)idx);
        }
    }

    public class Scenario
    {
        public string description { get; set; }
        public SimStates simInterpolate { get; set; }
        public SimStates simCellSize { get; set; }
        public TimeConfig time_config { get; set; }
        public ConfigEnvironment environment { get; set; }
        public ObservableCollection<CellPopulation> cellpopulations { get; set; }

        //skg daphne       
        public ObservableCollection<ConfigReaction> Reactions { get; set; }
        public ObservableCollection<ConfigMolecularPopulation> MolPops { get; set; }
        public ObservableCollection<GuiReactionComplex> ReactionComplexes { get; set; }

        public Scenario()
        {
            description = "Scenario description";
            simInterpolate = SimStates.Linear;
            simCellSize = SimStates.Tiny;
            time_config = new TimeConfig();
            environment = new ConfigEnvironment();
            cellpopulations = new ObservableCollection<CellPopulation>();

            Reactions = new ObservableCollection<ConfigReaction>();
            MolPops = new ObservableCollection<ConfigMolecularPopulation>();
            ReactionComplexes = new ObservableCollection<GuiReactionComplex>();
        }
    }
    
    public class EntityRepository
    {
        public ObservableCollection<GaussianSpecification> gaussian_specifications { get; set; }
        public ObservableCollection<BoxSpecification> box_specifications { get; set; }
       
        [JsonIgnore]
        public ObservableCollection<GuiReactionComplex> PredefReactionComplexes { get; set; }
        public ObservableCollection<ConfigCell> PredefCells { get; set; }

        //All molecules - Combined Predefined Molecules and User defined Molecules        
        public ObservableCollection<ConfigMolecule> AllMolecules { get; set; }
        //All molecules - Combined Predef reactions and User defined reactions
        public ObservableCollection<ConfigReaction> AllReactions { get; set; }

        public EntityRepository()
        {
            gaussian_specifications = new ObservableCollection<GaussianSpecification>();
            box_specifications = new ObservableCollection<BoxSpecification>();
        }
    }

    public class TimeConfig
    {
        public double duration { get; set; }
        public double rendering_interval { get; set; }
        public double sampling_interval { get; set; }

        public TimeConfig()
        {
            duration = 100;
            rendering_interval = 3;
            sampling_interval = 1440; // maybe ask Tom for a good value; it will be in the magnitude of days, so the gui field must have a large enough upper limit
        }
    }

    public class ConfigEnvironment : EntityModelBase
    {
        private int _extent_x;
        private int _extent_y;
        private int _extent_z;
        private double _gridstep;
        public int extent_x
        {
            get { return _extent_x; }
            set
            {
                if (_extent_x == value)
                    return;
                else
                {
                    _extent_x = value;
                    OnPropertyChanged("extent_x");
                }
            }
        }
        public int extent_y
        {
            get { return _extent_y; }
            set
            {
                if (_extent_y == value)
                    return;
                else
                {
                    _extent_y = value;
                    OnPropertyChanged("extent_y");
                }
            }
        }
        public int extent_z
        {
            get { return _extent_z; }
            set
            {
                if (_extent_z == value)
                    return;
                else
                {
                    _extent_z = value;
                    OnPropertyChanged("extent_z");
                }
            }
        }
        public double gridstep
        {
            get { return _gridstep; }
            set
            {
                if (_gridstep == value)
                    return;
                else
                {
                    _gridstep = value;
                    OnPropertyChanged("gridstep");
                }
            }
        }
        public int[] NumGridPts { get; set; }

        [XmlIgnore]
        public int extent_min { get; set; }
        [XmlIgnore]
        public int extent_max { get; set; }
        [XmlIgnore]
        public int gridstep_min { get; set; }
        [XmlIgnore]
        public int gridstep_max { get; set; }

        public ConfigEnvironment()
        {
            extent_x = 400;
            extent_y = 400;
            extent_z = 400;
            extent_min = 5;
            extent_max = 1000;
            gridstep_min = 1;
            gridstep_max = 100;
            gridstep = 50;

            CalculateNumGridPts();
        }

        public void CalculateNumGridPts()
        {
            int[] pt = new int[3];
            pt[0] = (int)Math.Ceiling((decimal)(extent_x / gridstep)) + 1;
            pt[1] = (int)Math.Ceiling((decimal)(extent_y / gridstep)) + 1;
            pt[2] = (int)Math.Ceiling((decimal)(extent_z / gridstep)) + 1;

            NumGridPts = pt;
        }
    }

    public enum RegionShape { Rectangular, Ellipsoid }
    
    public class Region : EntityModelBase
    {
        private string _region_name = "";
        public string region_name
        {
            get { return _region_name; }
            set
            {
                if (_region_name == value)
                    return;
                else
                {
                    _region_name = value;
                    OnPropertyChanged("region_name");
                }
            }
        }
        private RegionShape _region_type = RegionShape.Ellipsoid;
        public RegionShape region_type 
        {
            get { return _region_type; }
            set
            {
                if (_region_type == value)
                    return;
                else
                {
                    _region_type = value;
                    OnPropertyChanged("region_type");
                }
            }
        }
        public string region_box_spec_guid_ref { get; set; }
        private bool _region_visibility = true;
        public bool region_visibility
        {
            get { return _region_visibility; }
            set
            {
                if (_region_visibility == value)
                    return;
                else
                {
                    _region_visibility = value;
                    OnPropertyChanged("region_visibility");
                }
            }
        }
        private System.Windows.Media.Color _region_color;
        public System.Windows.Media.Color region_color
        {
            get { return _region_color; }
            set
            {
                if (_region_color == value)
                    return;
                else
                {
                    _region_color = value;
                    OnPropertyChanged("region_color");
                }
            }
        }

        public Region()
        {
            region_name = "Default Region";
            region_box_spec_guid_ref = "";
            region_visibility = true;
            region_color = new System.Windows.Media.Color();
            region_color = System.Windows.Media.Color.FromRgb(255, 255, 255);
        }

        public Region(string name, RegionShape type)
        {
            region_name = name;
            region_type = type;
            region_box_spec_guid_ref = "";
            region_visibility = true;
            region_color = new System.Windows.Media.Color();
            region_color = System.Windows.Media.Color.FromRgb(255, 255, 255);
        }
    }
    
    public enum RelativePosition { Inside, Surface, Outside }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(RelativePosition), typeof(string))]
    public class RelativePositionToShortStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the GlobalParameterType enum...
        private List<string> _relative_position_strings = new List<string>()
                                {
                                    "in",
                                    "on",
                                    "outside"
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _relative_position_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _relative_position_strings.FindIndex(item => item == str);
            return (RelativePosition)Enum.ToObject(typeof(RelativePosition), (int)idx);
        }
    }

    [ValueConversion(typeof(Color), typeof(string))]
    public class TextToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string ret = "Red";
            Color col = (Color)value;
            if (col == Colors.Red)
                ret = "Red";
            else
                ret = "Black";

            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    //skg daphne
    public class ConfigMolecule : EntityModelBase
    {
        public string Name { get; set; }
        public double MolecularWeight { get; set; }
        public double EffectiveRadius { get; set; }
        public double DiffusionCoefficient { get; set; }
        public string config_molecule_guid { get; set; }
        public bool ReadOnly { get; set; }
        public Color ForegroundColor { get; set; }

        public ConfigMolecule(string thisName, double thisMW, double thisEffRad, double thisDiffCoeff)
        {
            Guid id = Guid.NewGuid();
            config_molecule_guid = id.ToString();
            Name = thisName;
            MolecularWeight = thisMW;
            EffectiveRadius = thisEffRad;
            DiffusionCoefficient = thisDiffCoeff;
            ReadOnly = true;
            ForegroundColor = Colors.Red;
        }

        public ConfigMolecule()
            : base()
        {
            Guid id = Guid.NewGuid();
            config_molecule_guid = id.ToString();
            Name = "MolName";
            MolecularWeight = 1.0;
            EffectiveRadius = 5.0;
            DiffusionCoefficient = 2;
            ReadOnly = true;
            ForegroundColor = Colors.Red;
        }

        public ConfigMolecule(ConfigMolecule gm)
        {
            Guid id = Guid.NewGuid();
            config_molecule_guid = id.ToString();
            Name = gm.Name;
            MolecularWeight = gm.MolecularWeight;
            EffectiveRadius = gm.EffectiveRadius;
            DiffusionCoefficient = gm.DiffusionCoefficient;
            ForegroundColor = gm.ForegroundColor;
        }
    }

    //skg daphne new classes
    public class ConfigMolecularPopulation : EntityModelBase
    {
        public ConfigMolecularPopulation()
            : base()
        {
            Guid id = Guid.NewGuid();
            config_mol_pop_guid = id.ToString();
        }
        public ConfigMolecule Molecule { get; set; }
        public string Name { get; set; }
        private MolPopInfo _mp_Info;
        public MolPopInfo mpInfo
        {
            get { return _mp_Info; }
            set { _mp_Info = value; }
        }

        public string config_mol_pop_guid { get; set; }

        //For molecules in cells
        public MolPopPosition Location { get; set; }

    }

    /// <summary>
    /// Classes needed for Reactions
    /// </summary>
    public class GuiSpeciesReference : SpeciesReference
    {
        public string Location { get; set; }
    }

    public enum ReactionType
    {
        Association = 0, Dissociation = 1, Annihilation = 2, Dimerization = 3, DimerDissociation = 4,
        Transformation = 5, AutocatalyticTransformation = 6, CatalyzedAnnihilation = 7,
        CatalyzedAssociation = 8, CatalyzedCreation = 9, CatalyzedDimerization = 10, CatalyzedDimerDissociation = 11,
        CatalyzedDissociation = 12, CatalyzedTransformation = 13, BoundaryAssociation = 14, BoundaryDissociation = 15, Generalized = 16
    }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(ReactionType), typeof(string))]
    public class ReactionTypeToShortStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the GlobalParameterType enum...
        private List<string> _reaction_type_strings = new List<string>()
                                {
                                    "Association",
                                    "Dissociation",
                                    "Annihilation",
                                    "Dimerization",
                                    "DimerDissociation",
                                    "Transformation",
                                    "AutocatalyticTransformation",
                                    "CatalyzedAnnihilation",
                                    "CatalyzedAssociation",
                                    "CatalyzedCreation",
                                    "CatalyzedDimerization",
                                    "CatalyzedDimerDissociation",
                                    "CatalyzedTransformation",
                                    "CatalyzedDissociation",
                                    "BoundaryAssociation",
                                    "BoundaryDissociation",
                                    "Generalized"
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _reaction_type_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _reaction_type_strings.FindIndex(item => item == str);
            return (ReactionType)Enum.ToObject(typeof(ReactionType), (int)idx);
        }
    }

    public class ConfigReaction
    {
        public ConfigReaction()
        {
            Guid id = Guid.NewGuid();
            config_reaction_guid = id.ToString();
            listOfReactants = new List<GuiSpeciesReference>();
            listOfProducts = new List<GuiSpeciesReference>();
            listOfModifiers = new List<GuiSpeciesReference>();
            rateConst = 0;
        }

        public void CopyTo(ReactionTemplate rt)
        {
            rt.rateConst = rateConst;
            rt.listOfModifiers.AddRange(listOfModifiers);
            rt.listOfProducts.AddRange(listOfProducts);
            rt.listOfReactants.AddRange(listOfReactants);
        }

        public ReactionType ReacType
        {
            get
            {
                return reacType;
            }
            set
            {
                reacType = value;
            }
        }

        public string ReacTypeString
        {
            get
            {
                string result = (string)new ReactionTypeToShortStringConverter().Convert(reacType, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
                return result;
            }
        }

        public double RateConst
        {
            get
            {
                return rateConst;
            }
            set
            {
                rateConst = value;
            }
        }

        public string config_reaction_guid { get; set; }

        [JsonIgnore]
        public string ReactantsString
        {
            get
            {
                string s = "";
                foreach (SpeciesReference sr in listOfReactants)
                {
                    if (sr.stoichiometry > 1)
                        s += sr.stoichiometry;
                    s += sr.species;
                    s += " + ";
                }
                foreach (SpeciesReference sr in listOfModifiers)
                {
                    if (sr.stoichiometry > 1)
                        s += sr.stoichiometry;
                    s += sr.species;
                    s += " + ";
                }
                char[] trimChars = { ' ', '+' };
                s = s.Trim(trimChars);
                return s;
            }
            set
            {
                reactantsString = value;
            }

        }
        [JsonIgnore]
        public string ProductsString
        {
            get
            {
                string s = "";
                foreach (SpeciesReference sr in listOfProducts)
                {
                    if (sr.stoichiometry > 1)
                        s += sr.stoichiometry;
                    s += sr.species;
                    s += " + ";
                }
                foreach (SpeciesReference sr in listOfModifiers)
                {
                    if (sr.stoichiometry > 1)
                        s += sr.stoichiometry;
                    s += sr.species;
                    s += " + ";
                }
                char[] trimChars = { ' ', '+' };
                s = s.Trim(trimChars);
                return s;
            }
            set
            {
                productsString = value;
            }
        }
        [JsonIgnore]
        public string TotalReactionString
        {
            get
            {
                return ReactantsString + " -> " + ProductsString;
            }
            set
            {
                totalReactionString = value;
            }
        }

        private double rateConst;
        private string reactantsString;
        private string productsString;
        private string totalReactionString;
        private ReactionType reacType;
        public List<GuiSpeciesReference> listOfReactants;
        public List<GuiSpeciesReference> listOfProducts;
        public List<GuiSpeciesReference> listOfModifiers;

    }

    public class GuiBoundaryReactionTemplate : ConfigReaction
    {
        public GuiSpeciesReference ligand;
        public GuiSpeciesReference receptor;
        public GuiSpeciesReference complex;
        double fluxIntensityConstant;
    }

    public class GuiCatalyzedReactionTemplate : ConfigReaction
    {
        public GuiSpeciesReference catalyst;
    }














    public enum MolPopPosition { Cytosol, Membrane, ECS }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(MolPopPosition), typeof(string))]
    public class MolPopPositionToShortStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the GlobalParameterType enum...
        private List<string> _molpop_position_strings = new List<string>()
                                {
                                    "cytosol",
                                    "membrane",
                                    "ecs"
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _molpop_position_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _molpop_position_strings.FindIndex(item => item == str);
            return (MolPopPosition)Enum.ToObject(typeof(MolPopPosition), (int)idx);
        }
    }

    public class ConfigCell
    {
        public ConfigCell()
        {
            CellName = "Default Cell";
            CellRadius = 5.0;

            CellMolPops = new ObservableCollection<ConfigMolecularPopulation>();
        }

        public string CellName { get; set; }
        public double CellRadius { get; set; }

        public ObservableCollection<ConfigMolecularPopulation> CellMolPops { get; set; }
        public ObservableCollection<ConfigReaction> CellReactions { get; set; }
        public ObservableCollection<GuiReactionComplex> CellReactionComplexes { get; set; }
    }

    public class CellPopDistType
    {
        public string Name { get; set; }
        public ObservableCollection<CellPopDistSubtype> DistSubtypes { get; set; }

        public override string ToString()
        {
            return Name;
        }
    }

    public class CellPopDistSubtype
    {
        public string Label { get; set; }
        public override string ToString()
        {
            return Label;
        }
    }

    public enum CellPopDistributionType { Specific, Probability }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(CellPopDistributionType), typeof(string))]
    public class CellPopDistributionTypeToStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the GlobalParameterType enum...
        private List<string> _cell_pop_dist_type_strings = new List<string>()
            {
                "Specify cell coordinates",
                "Probability Distribution"
            };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _cell_pop_dist_type_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _cell_pop_dist_type_strings.FindIndex(item => item == str);
            return (CellPopDistributionType)Enum.ToObject(typeof(CellPopDistributionType), (int)idx);
        }
    }

    public enum CellPopProbDistributionType { Uniform, Gaussian }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(CellPopProbDistributionType), typeof(string))]
    public class CellPopProbDistributionTypeToStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the GlobalParameterType enum...
        private List<string> _cell_pop_prob_dist_type_strings = new List<string>()
            {
                "Uniform",
                "Gaussian"
            };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _cell_pop_prob_dist_type_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _cell_pop_prob_dist_type_strings.FindIndex(item => item == str);
            return (CellPopProbDistributionType)Enum.ToObject(typeof(CellPopProbDistributionType), (int)idx);
        }
    }

    public enum CellPopSpecificLocationType { Coord, File }

    public abstract class CellPopDistribution
    {
        public CellPopDistributionType cpDistributionType { get; protected set; }
    }

    public class CellPopSpecifyLocation : CellPopDistribution
    {
        public CellPopSpecificLocationType LocationType { get; set; }
        public CellPopSpecifyLocation()
        {
            cpDistributionType = CellPopDistributionType.Specific;
            LocationType = CellPopSpecificLocationType.Coord;
        }
    }

    public class CellPopLocationFile : CellPopDistribution
    {
        public string fileName { get; set; }
        public CellPopSpecificLocationType LocationType { get; set; }
        public CellPopLocationFile(string file)
        {
            cpDistributionType = CellPopDistributionType.Specific;
            LocationType = CellPopSpecificLocationType.File;
            fileName = file;
        }
    }

    public class CellPopUniformDistribution : CellPopDistribution
    {
        public double Conc { get; set; }
        public CellPopProbDistributionType DistType { get; set; }
        public CellPopUniformDistribution(double conc)
        {
            Conc = conc;
            cpDistributionType = CellPopDistributionType.Probability;
            DistType = CellPopProbDistributionType.Uniform;
        }
    }

    public class CellPopGaussianDistribution : CellPopDistribution
    {
        private double[] center;
        private double[] sigma;
        private double peak;

        public double[] Center { get; set; }
        public double[] Sigma { get; set; }
        public double   Peak { get; set; }

        public CellPopProbDistributionType DistType { get; set; }

        public CellPopGaussianDistribution()
        {
            cpDistributionType = CellPopDistributionType.Probability;
            DistType = CellPopProbDistributionType.Gaussian;
        }
    }

    public class CellPopulation
    {
        public string cellpopulation_name { get; set; }
        public string cellpopulation_guid { get; set; }
        public int cellpopulation_id { get; set; }
        public string cell_subset_guid_ref { get; set; }
        public int number { get; set; }
        // TODO: Need to abstract out positioning to include pos specification for single cell...
        private bool _cellpopulation_constrained_to_region = false;
        public bool cellpopulation_constrained_to_region 
        {
            get { return _cellpopulation_constrained_to_region; }
            set
            {
                if (_cellpopulation_constrained_to_region == value)
                    return;
                else
                {
                    _cellpopulation_constrained_to_region = value;
                    // NOTE: For now, manually blanking out guid_ref if false selected
                    //   so cell population will be correct and searching for "used" regions
                    //   will not turn up unwanted references...
                    if (_cellpopulation_constrained_to_region == false)
                        cellpopulation_region_guid_ref = "";
                }
            }
        }
        public string cellpopulation_region_guid_ref { get; set; }
        public RelativePosition wrt_region { get; set; }
        public bool cellpopulation_render_on { get; set; }
        public System.Windows.Media.Color cellpopulation_color { get; set; }

        //skg Daphne
        public ConfigCell CellType { get; set; }
        public double MaxConc { get; set; }
        public double[] Sigma { get; set; }
        public double[] Center { get; set; }
        public CellPopDistribution cellPopDist;

        public ObservableCollection<CellPopDistType> CellPopDistTypes { get; set; }
        private void InitDistTypes()
        {
            CellPopDistType d = new CellPopDistType();
            d.Name = "Specify cell coordinates";
            d.DistSubtypes = new ObservableCollection<CellPopDistSubtype>();

            CellPopDistSubtype d2 = new CellPopDistSubtype();
            d2.Label = "Input coordinates";
            d.DistSubtypes.Add(d2);

            d2 = new CellPopDistSubtype();
            d2.Label = "Specify coordinates file";
            d.DistSubtypes.Add(d2);

            CellPopDistTypes.Add(d);

            d = new CellPopDistType();
            d.Name = "Probability distribution";
            d.DistSubtypes = new ObservableCollection<CellPopDistSubtype>();

            d2 = new CellPopDistSubtype();
            d2.Label = "Uniform";
            d.DistSubtypes.Add(d2);

            d2 = new CellPopDistSubtype();
            d2.Label = "Gaussian";
            d.DistSubtypes.Add(d2);

            CellPopDistTypes.Add(d);
        }

        public CellPopulation()
        {
            Guid id = Guid.NewGuid();
            cellpopulation_guid = id.ToString();
            cellpopulation_name = "";
            cell_subset_guid_ref = "";
            number = 100;
            cellpopulation_constrained_to_region = false;
            cellpopulation_region_guid_ref = "";
            wrt_region = RelativePosition.Inside;
            cellpopulation_color = new System.Windows.Media.Color();
            cellpopulation_render_on = true;
            cellpopulation_color = System.Windows.Media.Color.FromRgb(255, 255, 255);
            cellpopulation_id = SimConfiguration.SafeCellPopulationID++;

            cellPopDist = new CellPopUniformDistribution(10.0);            
            CellPopDistTypes = new ObservableCollection<CellPopDistType>();
            InitDistTypes();
        }
    }

    // MolPopInfo ==================================
    public class MolPopInfo : EntityModelBase
    {
        public string mp_guid { get; set; }
        private string _mp_dist_name = "";
        public string mp_dist_name
        {
            get { return _mp_dist_name; }
            set
            {
                if (_mp_dist_name == value)
                    return;
                else
                {
                    _mp_dist_name = value;
                    OnPropertyChanged("mp_dist_name");
                }
            }
        }
        private string _mp_type_guid_ref;
        public string mp_type_guid_ref
        {
            get { return _mp_type_guid_ref; }
            set
            {
                if (_mp_type_guid_ref == value)
                    return;
                else
                {
                    _mp_type_guid_ref = value;
                    OnPropertyChanged("mp_type_guid_ref");
                }
            }
        }

        private MolPopDistribution _mp_distribution;
        public MolPopDistribution mp_distribution
        {
            get { return _mp_distribution; }
            set
            {
                if (_mp_distribution == value)
                    return;
                else
                {
                    _mp_distribution = value;
                    OnPropertyChanged("mp_distribution");
                }
            }
        }
        public bool mp_is_time_varying { get; set; }
        public ObservableCollection<TimeAmpPair> mp_amplitude_keyframes { get; set; }
        private System.Windows.Media.Color _mp_color;
        public System.Windows.Media.Color mp_color
        {
            get { return _mp_color; }
            set
            {
                if (_mp_color == value)
                    return;
                else
                {
                    _mp_color = value;
                    OnPropertyChanged("mp_color");
                }
            }
        }
        public double mp_render_blending_weight { get; set; }
        public bool mp_render_on { get; set; }

        public MolPopInfo()
        {            
        }
        public MolPopInfo(string name)
        {
            Guid id = Guid.NewGuid();
            mp_guid = id.ToString();
            mp_dist_name = name;
            mp_type_guid_ref = "";
            // Default is static homogeneous level
            mp_distribution = new MolPopHomogeneousLevel();
            mp_is_time_varying = false;
            mp_amplitude_keyframes = new ObservableCollection<TimeAmpPair>();
            mp_color = new System.Windows.Media.Color();
            mp_color = System.Windows.Media.Color.FromRgb(255, 255, 255);
            mp_render_blending_weight = 1.0;
            mp_render_on = true;
        }
    }

    /// <summary>
    /// Converter to go between cell type GUID references in CellSets
    /// and cell type names kept in the repository list of CellSubset(s).
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class RegionGUIDtoNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;
            string region_name = "";
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<Region> region_list = cvs.Source as ObservableCollection<Region>;
            if (region_list != null)
            {
                foreach (Region rg in region_list)
                {
                    if (rg.region_box_spec_guid_ref == guid)
                    {
                        region_name = rg.region_name;
                        break;
                    }
                }
            }
            return region_name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    /// <summary>
    /// Convert System.Windows.Media.Color to SolidBrush for rectangle fills
    /// </summary>
    public class SWMColorToSolidBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            System.Windows.Media.Color color;

            try
            {
                color = (System.Windows.Media.Color)value;
            }
            catch
            {
                color = System.Windows.Media.Color.FromRgb(0, 0, 0);
            }
            return new System.Windows.Media.SolidColorBrush(color);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public enum MolPopDistributionType { Homogeneous, LinearGradient, Gaussian, CustomGradient }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(MolPopDistributionType), typeof(string))]
    public class MolPopDistributionTypeToStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the GlobalParameterType enum...
        private List<string> _solfac_dist_type_strings = new List<string>()
                                {
                                    "Homogeneous",
                                    "Linear Gradient",
                                    "Gaussian",
                                    "Custom Gradient"
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _solfac_dist_type_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _solfac_dist_type_strings.FindIndex(item => item == str);
            return (MolPopDistributionType)Enum.ToObject(typeof(MolPopDistributionType), (int)idx);
        }
    }


    

    // Base class for homog, linear, gauss distributions
    [XmlInclude(typeof(MolPopHomogeneousLevel)),
     XmlInclude(typeof(MolPopLinearGradient)),
     XmlInclude(typeof(MolPopGaussianGradient)),
     XmlInclude(typeof(MolPopCustomGradient))]
    public abstract class MolPopDistribution : EntityModelBase
    {
        [XmlIgnore]
        public MolPopDistributionType mp_distribution_type { get; protected set; }

        public MolPopDistribution()
        {
        }
    }

    public class MolPopHomogeneousLevel : MolPopDistribution
    {
        public double concentration { get; set; }

        public MolPopHomogeneousLevel()
        {
            mp_distribution_type = MolPopDistributionType.Homogeneous;
            concentration = 10.0;
        }
    }

    public class MolPopLinearGradient : MolPopDistribution
    {
        public double[] gradient_direction { get; set; }
        public double min_concentration { get; set; }
        public double max_concentration { get; set; }
        public double x_direction
        {
            get { return gradient_direction[0]; }
            set
            {
                if (value != gradient_direction[0])
                {
                    gradient_direction[0] = value;
                }
            }
        }
        public double y_direction
        {
            get { return gradient_direction[1]; }
            set
            {
                if (value != gradient_direction[1])
                {
                    gradient_direction[1] = value;
                }
            }
        }
        public double z_direction
        {
            get { return gradient_direction[2]; }
            set
            {
                if (value != gradient_direction[2])
                {
                    gradient_direction[2] = value;
                }
            }
        }

        public MolPopLinearGradient()
        {
            mp_distribution_type = MolPopDistributionType.LinearGradient;
            gradient_direction = new double[3] { 1.0, 0.0, 0.0 };
            min_concentration = 0.0;
            max_concentration = 100.0;
        }
    }

    public class MolPopGaussianGradient : MolPopDistribution
    {
        public double peak_concentration { get; set; }
        private string _gaussgrad_gauss_spec_guid_ref;
        public string gaussgrad_gauss_spec_guid_ref
        {
            get { return _gaussgrad_gauss_spec_guid_ref; }
            set
            {
                if (_gaussgrad_gauss_spec_guid_ref == value)
                    return;
                else
                {
                    _gaussgrad_gauss_spec_guid_ref = value;
                    OnPropertyChanged("gaussgrad_gauss_spec_guid_ref");
                }
            }
        }

        public MolPopGaussianGradient()
        {
            mp_distribution_type = MolPopDistributionType.Gaussian;
            peak_concentration = 100.0;
            gaussgrad_gauss_spec_guid_ref = "";
        }
    }

    public class MolPopCustomGradient : MolPopDistribution
    {
        private Uri _custom_gradient_file_uri = new Uri("c:\\temp2"/*DaphneGui.MainWindow.appPath*/);
        private string _custom_gradient_file_string = "c:\\temp2"; //DaphneGui.MainWindow.appPath;

        public MolPopCustomGradient()
        {
            mp_distribution_type = MolPopDistributionType.CustomGradient;
        }

        [XmlIgnore]
        public Uri custom_gradient_file_uri
        {
            get { return _custom_gradient_file_uri; }
            set
            {
                if (_custom_gradient_file_uri == value)
                    return;
                else
                {
                    _custom_gradient_file_uri = value;
                    _custom_gradient_file_string = value.AbsolutePath;
                    OnPropertyChanged("custom_gradient_file_uri");
                    OnPropertyChanged("custom_gradient_file_string");
                    OnPropertyChanged("custom_gradient_file_name");
                }
            }
        }

        public string custom_gradient_file_string
        {
            get { return _custom_gradient_file_string; }
            set
            {
                if (_custom_gradient_file_string == value)
                    return;
                else
                {
                    _custom_gradient_file_string = value;
                    _custom_gradient_file_uri = new Uri(value);
                    OnPropertyChanged("custom_gradient_file_uri");
                    OnPropertyChanged("custom_gradient_file_string");
                    OnPropertyChanged("custom_gradient_file_name");
                }
            }
        }

        [XmlIgnore]
        public string custom_gradient_file_name
        {
            get { return _custom_gradient_file_uri.Segments[_custom_gradient_file_uri.Segments.Length - 1]; }
        }
    }

    public class GaussianSpecification : EntityModelBase
    {
        private string _gaussian_spec_name = "";
        public string gaussian_spec_name 
        {
            get { return _gaussian_spec_name; }
            set
            {
                if (_gaussian_spec_name == value)
                    return;
                else
                {
                    _gaussian_spec_name = value;
                    OnPropertyChanged("gaussian_spec_name");
                }
            }
        }
        public string gaussian_spec_box_guid_ref { get; set; }
        private bool _gaussian_region_visibity = true;
        public bool gaussian_region_visibility 
        {
            get { return _gaussian_region_visibity; }
            set
            {
                if (_gaussian_region_visibity == value)
                    return;
                else
                {
                    _gaussian_region_visibity = value;
                    OnPropertyChanged("gaussian_region_visibility");
                }
            }
        }
        private System.Windows.Media.Color _gaussian_spec_color;
        public System.Windows.Media.Color gaussian_spec_color
        {
            get { return _gaussian_spec_color; }
            set
            {
                if (_gaussian_spec_color == value)
                    return;
                else
                {
                    _gaussian_spec_color = value;
                    OnPropertyChanged("gaussian_spec_color");
                }
            }
        }

        public GaussianSpecification()
        {
            gaussian_spec_name = "Default gaussian gradient name";
            gaussian_spec_box_guid_ref = "";
            gaussian_region_visibility = true;
            gaussian_spec_color = new System.Windows.Media.Color();
            gaussian_spec_color = System.Windows.Media.Color.FromRgb(255, 255, 255);
        }
    }


    // UTILITY CLASSES =======================
    public class BoxSpecification : EntityModelBase
    {
        public string box_guid { get; set; }
        public double[][] transform_matrix { get; set; }
        private bool _box_visibility = true;
        
        // Range values calculated based on environment extents
        private double _x_trans_max;
        private double _x_trans_min;
        private double _x_scale_max;
        private double _x_scale_min;
        private double _y_trans_max;
        private double _y_trans_min;
        private double _y_scale_max;
        private double _y_scale_min;
        private double _z_trans_max;
        private double _z_trans_min;
        private double _z_scale_max;
        private double _z_scale_min;

        [XmlIgnore]
        public double x_trans_max
        {
            get { return _x_trans_max; }
            set
            {
                if (_x_trans_max == value)
                    return;
                else
                {
                    _x_trans_max = value;
                    OnPropertyChanged("x_trans_max");
                    // This doesn't seem to be taken care of by GUI itself...
                    if (x_trans > _x_trans_max) x_trans = _x_trans_max;
                }
            }
        }

        [XmlIgnore]
        public double x_trans_min
        {
            get { return _x_trans_min; }
            set
            {
                if (_x_trans_min == value)
                    return;
                else
                {
                    _x_trans_min = value;
                    OnPropertyChanged("x_trans_min");
                    // This doesn't seem to be taken care of by GUI itself...
                    if (x_trans < _x_trans_min) x_trans = _x_trans_min;
                }
            }
        }

        [XmlIgnore]
        public double x_scale_max
        {
            get { return _x_scale_max; }
            set
            {
                if (_x_scale_max == value)
                    return;
                else
                {
                    _x_scale_max = value;
                    OnPropertyChanged("x_scale_max");
                    // This doesn't seem to be taken care of by GUI itself...
                    if (x_scale > _x_scale_max) x_scale = _x_scale_max;
                }
            }
        }

        [XmlIgnore]
        public double x_scale_min
        {
            get { return _x_scale_min; }
            set
            {
                if (_x_scale_min == value)
                    return;
                else
                {
                    _x_scale_min = value;
                    OnPropertyChanged("x_scale_min");
                    // This doesn't seem to be taken care of by GUI itself...
                    if (x_scale < _x_scale_min) x_scale = _x_scale_min;
                }
            }
        }

        [XmlIgnore]
        public double y_trans_max
        {
            get { return _y_trans_max; }
            set
            {
                if (_y_trans_max == value)
                    return;
                else
                {
                    _y_trans_max = value;
                    OnPropertyChanged("y_trans_max");
                    if (y_trans > _y_trans_max) y_trans = _y_trans_max;
                }
            }
        }

        [XmlIgnore]
        public double y_trans_min
        {
            get { return _y_trans_min; }
            set
            {
                if (_y_trans_min == value)
                    return;
                else
                {
                    _y_trans_min = value;
                    OnPropertyChanged("y_trans_min");
                    if (y_trans < _y_trans_min) y_trans = _y_trans_min;
                }
            }
        }

        [XmlIgnore]
        public double y_scale_max
        {
            get { return _y_scale_max; }
            set
            {
                if (_y_scale_max == value)
                    return;
                else
                {
                    _y_scale_max = value;
                    OnPropertyChanged("y_scale_max");
                    if (y_scale > _y_scale_max) y_scale = _y_scale_max;
                }
            }
        }

        [XmlIgnore]
        public double y_scale_min
        {
            get { return _y_scale_min; }
            set
            {
                if (_y_scale_min == value)
                    return;
                else
                {
                    _y_scale_min = value;
                    OnPropertyChanged("y_scale_min");
                    if (y_scale < _y_scale_min) y_scale = _y_scale_min;
                }
            }
        }

        [XmlIgnore]
        public double z_trans_max
        {
            get { return _z_trans_max; }
            set
            {
                if (_z_trans_max == value)
                    return;
                else
                {
                    _z_trans_max = value;
                    OnPropertyChanged("z_trans_max");
                    if (z_trans > _z_trans_max) z_trans = _z_trans_max;
                }
            }
        }

        [XmlIgnore]
        public double z_trans_min
        {
            get { return _z_trans_min; }
            set
            {
                if (_z_trans_min == value)
                    return;
                else
                {
                    _z_trans_min = value;
                    OnPropertyChanged("z_trans_min");
                    if (z_trans < _z_trans_min) z_trans = _z_trans_min;
                }
            }
        }

        [XmlIgnore]
        public double z_scale_max
        {
            get { return _z_scale_max; }
            set
            {
                if (_z_scale_max == value)
                    return;
                else
                {
                    _z_scale_max = value;
                    OnPropertyChanged("z_scale_max");
                    if (z_scale > _z_scale_max) z_scale = _z_scale_max;
                }
            }
        }

        [XmlIgnore]
        public double z_scale_min
        {
            get { return _z_scale_min; }
            set
            {
                if (_z_scale_min == value)
                    return;
                else
                {
                    _z_scale_min = value;
                    OnPropertyChanged("z_scale_min");
                    if (z_scale < _z_scale_min) z_scale = _z_scale_min;
                }
            }
        }

        public bool box_visibility 
        {
            get { return _box_visibility; }
            set
            {
                if (_box_visibility == value)
                    return;
                else
                {
                    _box_visibility = value;
                    OnPropertyChanged("box_visibility");
                }
            }
        }
        public double x_scale
        {
            get {
                return getScale(0);
            }
            set
            {
                double current = getScale(0);

                if (value != current)
                {
                    if (current != 0.0)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            transform_matrix[i][0] /= current;
                            transform_matrix[i][0] *= value;
                        }
                    }
                    else
                    {
                        transform_matrix[0][0] = value;
                        transform_matrix[1][0] = 0.0;
                        transform_matrix[2][0] = 0.0;
                    }
                    base.OnPropertyChanged("x_scale");
                }
            }
        }
        public double y_scale
        {
            get
            {
                return getScale(1);
            }
            set
            {
                double current = getScale(1);

                if (value != current)
                {
                    if (current != 0.0)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            transform_matrix[i][1] /= current;
                            transform_matrix[i][1] *= value;
                        }
                    }
                    else
                    {
                        transform_matrix[0][1] = 0.0;
                        transform_matrix[1][1] = value;
                        transform_matrix[2][1] = 0.0;
                    }
                    base.OnPropertyChanged("y_scale");
                }
            }
        }
        public double z_scale
        {
            get
            {
                return getScale(2);
            }
            set
            {
                double current = getScale(2);

                if (value != current)
                {
                    if (current != 0.0)
                    {
                        for (int i = 0; i < 3; i++)
                        {
                            transform_matrix[i][2] /= current;
                            transform_matrix[i][2] *= value;
                        }
                    }
                    else
                    {
                        transform_matrix[0][2] = 0.0;
                        transform_matrix[1][2] = 0.0;
                        transform_matrix[2][2] = value;
                    }
                    base.OnPropertyChanged("z_scale");
                }
            }
        }

        public double x_trans
        {
            get { return transform_matrix[0][3]; }
            set
            {
                if (value != transform_matrix[0][3])
                {
                    transform_matrix[0][3] = value;
                    base.OnPropertyChanged("x_trans");
                }
            }
        }
        public double y_trans
        {
            get { return transform_matrix[1][3]; }
            set
            {
                if (value != transform_matrix[1][3])
                {
                    transform_matrix[1][3] = value;
                    base.OnPropertyChanged("y_trans");
                }
            }
        }
        public double z_trans
        {
            get { return transform_matrix[2][3]; }
            set
            {
                if (value != transform_matrix[2][3])
                {
                    transform_matrix[2][3] = value;
                    base.OnPropertyChanged("z_trans");
                }
            }
        }

        private double getScale(byte i)
        {
            if (i >= 3)
            {
                return 0.0;
            }

            double scale = Math.Sqrt(transform_matrix[0][i] * transform_matrix[0][i] +
                                     transform_matrix[1][i] * transform_matrix[1][i] +
                                     transform_matrix[2][i] * transform_matrix[2][i]);

            return scale;
        }

        public BoxSpecification()
        {
            Guid id = Guid.NewGuid();
            box_guid = id.ToString();
            box_visibility = true;
            transform_matrix = new double[][] {
                new double[]{1.0, 0.0, 0.0, 0.0},
                new double[]{0.0, 1.0, 0.0, 0.0},
                new double[]{0.0, 0.0, 1.0, 0.0},
                new double[]{0.0, 0.0, 0.0, 1.0} };
        }

        public void SetMatrix(double[][] value)
        {
            bool x_scale_change = false,
                 y_scale_change = false,
                 z_scale_change = false,
                 matrix_change  = false;

            for (int row = 0; row < 4; row++)
            {
                for (int col = 0; col < 4; col++)
                {
                    if (value[row][col] != transform_matrix[row][col])
                    {
                        transform_matrix[row][col] = value[row][col];
                        // call handler once only
                        if (matrix_change == false)
                        {
                            matrix_change = true;
                        }

                        // handle scaling
                        if (x_scale_change == false && row < 3 && col == 0)
                        {
                            x_scale_change = true;
                        }
                        else if (y_scale_change == false && row < 3 && col == 1)
                        {
                            y_scale_change = true;
                        }
                        else if (z_scale_change == false && row < 3 && col == 2)
                        {
                            z_scale_change = true;
                        }

                        // handle translations
                        else if (row == 0 && col == 3)
                        {
                            base.OnPropertyChanged("x_trans");
                        }
                        else if (row == 1 && col == 3)
                        {
                            base.OnPropertyChanged("y_trans");
                        }
                        else if (row == 2 && col == 3)
                        {
                            base.OnPropertyChanged("z_trans");
                        }
                    }
                }
            }

            // call property changed handlers
            if (matrix_change == true)
            {
                base.OnPropertyChanged("transform_matrix");
            }
            if (x_scale_change == true)
            {
                base.OnPropertyChanged("x_scale");
            }
            if (y_scale_change == true)
            {
                base.OnPropertyChanged("y_scale");
            }
            if (z_scale_change == true)
            {
                base.OnPropertyChanged("z_scale");
            }
        }
    }

    public class TimeAmpPair
    {
        // Not clear whether this should be time step or real time value...
        private double _time_value;
        public double time_value
        {
            get { return _time_value; }
            set
            {
                if (value >= 0.0)
                {
                    _time_value = value;
                }
            }
        }
        private double _amplitude;
        public double amplitude
        {
            get { return _amplitude; }
            set
            {
                if (value >= 0.0 && value <= 1.0)
                {
                    _amplitude = value;
                }
            }
        }

        public TimeAmpPair()
        {
            time_value = 0.0;
            amplitude = 0.0;
        }

        public TimeAmpPair(double ts, double a)
        {
            time_value = ts;
            amplitude = a;
        }
    }

    /// <summary>
    /// Converter to test > 1 -> true, not -> false for "s" plural addition
    /// </summary>
    [ValueConversion(typeof(int), typeof(bool))]
    public class ManyToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return ((int)value) > 1;
            }
            catch
            {
                return false;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return false;
        }
    }

    /// <summary>
    /// Base class for all EntityModel classes.
    /// It provides support for property change notifications 
    /// and disposal.  This class is abstract.
    /// </summary>
    public abstract class EntityModelBase : INotifyPropertyChanged, IDisposable
    {
        #region Constructor

        protected EntityModelBase()
        {
        }

        #endregion // Constructor

        #region DisplayName

        /// <summary>
        /// Returns the user-friendly name of this object.
        /// Child classes can set this property to a new value,
        /// or override it to determine the value on-demand.
        /// </summary>
        // public virtual string DisplayName { get; protected set; }

        #endregion // DisplayName

        #region Debugging Aides

        /// <summary>
        /// Warns the developer if this object does not have
        /// a public property with the specified name. This 
        /// method does not exist in a Release build.
        /// </summary>
        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        public void VerifyPropertyName(string propertyName)
        {
            // Verify that the property name matches a real,  
            // public, instance property on this object.
            if (TypeDescriptor.GetProperties(this)[propertyName] == null)
            {
                string msg = "Invalid property name: " + propertyName;

                if (this.ThrowOnInvalidPropertyName)
                    throw new Exception(msg);
                else
                    Debug.Fail(msg);
            }
        }

        /// <summary>
        /// Returns whether an exception is thrown, or if a Debug.Fail() is used
        /// when an invalid property name is passed to the VerifyPropertyName method.
        /// The default value is false, but subclasses used by unit tests might 
        /// override this property's getter to return true.
        /// </summary>
        protected virtual bool ThrowOnInvalidPropertyName { get; private set; }

        #endregion // Debugging Aides

        #region INotifyPropertyChanged Members

        /// <summary>
        /// Raised when a property on this object has a new value.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises this object's PropertyChanged event.
        /// </summary>
        /// <param name="propertyName">The property that has a new value.</param>
        protected virtual void OnPropertyChanged(string propertyName)
        {
            this.VerifyPropertyName(propertyName);

            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        #endregion // INotifyPropertyChanged Members

        #region IDisposable Members

        /// <summary>
        /// Invoked when this object is being removed from the application
        /// and will be subject to garbage collection.
        /// </summary>
        public void Dispose()
        {
            this.OnDispose();
        }

        /// <summary>
        /// Child classes can override this method to perform 
        /// clean-up logic, such as removing event handlers.
        /// </summary>
        protected virtual void OnDispose()
        {
        }

#if DEBUG
        /// <summary>
        /// Useful for ensuring that ViewModel objects are properly garbage collected.
        /// </summary>
        ~EntityModelBase()
        {
            // string msg = string.Format("{0} ({1}) ({2}) Finalized", this.GetType().Name, this.DisplayName, this.GetHashCode());
            // string msg = string.Format("{0} ({1}) Finalized", this.GetType().Name, this.GetHashCode());
            // MainWindow.debugOut(String.Format(msg));
        }
#endif

        #endregion // IDisposable Members
    }
}
