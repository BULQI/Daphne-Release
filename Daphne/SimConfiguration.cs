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
using System.Windows;
using System.Windows.Markup;

namespace Daphne
{
    /// <summary>
    /// ties together all levels of storage
    /// </summary>
    public class SystemOfPersistence
    {
        /// <summary>
        /// Protocol level, contains Entity level
        /// </summary>
        public Protocol Protocol { get; set; }
        /// <summary>
        /// Daphne level
        /// </summary>
        public Level DaphneStore { get; set; }
        /// <summary>
        /// User level
        /// </summary>
        public Level UserStore { get; set; }

        /// <summary>
        /// The main palette containing graphics properties used to render various objects (i.e. colors, etc)
        /// </summary>
        public RenderPalette Palette { get; set; }

        /// <summary>
        /// constructor
        /// </summary>
        public SystemOfPersistence()
        {
            Protocol = new Protocol("", "Config\\temp_protocol.json");
            Palette = new RenderPalette();
            //DaphneStore = new Level("", "Config\\temp_daphnestore.json");
            //UserStore = new Level("", "Config\\temp_userstore.json");
        }

        /// <summary>
        /// deserialize the daphne store
        /// </summary>
        /// <param name="tempFiles">true for handling temporary files</param>
        public void DeserializeDaphneStore(bool tempFiles = false)
        {
            DaphneStore = DaphneStore.Deserialize(tempFiles);
        }

        /// <summary>
        /// deserialize the daphne store; the latter given as a string
        /// </summary>
        /// <param name="jsonFile">json file content as string</param>
        public void DeserializeDaphneStoreFromString(string jsonFile)
        {
            DaphneStore = DaphneStore.DeserializeFromString(jsonFile);
        }

        /// <summary>
        /// deserialize the user store
        /// </summary>
        /// <param name="tempFiles">true for handling temporary files</param>
        public void DeserializeUserStore(bool tempFiles = false)
        {
            UserStore = UserStore.Deserialize(tempFiles);
        }

        /// <summary>
        /// deserialize the user store; the latter given as a string
        /// </summary>
        /// <param name="jsonFile">json file content as string</param>
        public void DeserializeUserStoreFromString(string jsonFile)
        {
            UserStore = UserStore.DeserializeFromString(jsonFile);
        }

        /// <summary>
        /// deserialize the protocol
        /// </summary>
        /// <param name="tempFiles">true for handling temporary files</param>
        public void DeserializeProtocol(bool tempFiles = false)
        {
            Protocol = (Protocol)Protocol.Deserialize(tempFiles);
        }

        /// <summary>
        /// deserialize an external protocol (not the one part of this class)
        /// </summary>
        /// <param name="tempFiles">true for handling temporary files</param>
        public static void DeserializeExternalProtocol(ref Protocol protocol, bool tempFiles = false)
        {
            protocol = (Protocol)protocol.Deserialize(tempFiles);
        }

        /// <summary>
        /// deserialize the protocol from a string
        /// </summary>
        /// <param name="jsonFile">json file content as string</param>
        public void DeserializeProtocolFromString(string jsonFile)
        {
            Protocol = (Protocol)Protocol.DeserializeFromString(jsonFile);
        }

        /// <summary>
        /// deserialize an external protocol from a string
        /// </summary>
        /// <param name="jsonFile">json file content as string</param>
        public static void DeserializeExternalProtocolFromString(ref Protocol protocol, string jsonFile)
        {
            protocol = (Protocol)protocol.DeserializeFromString(jsonFile);
        }
    }

    /// <summary>
    /// base for all levels
    /// </summary>
    public class Level
    {
        /// <summary>
        /// constructor
        /// </summary>
        public Level() : this("", "")
        {
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="fileName">name of the storage file</param>
        /// <param name="tempFile">name of the temporary file</param>
        public Level(string fileName, string tempFile)
        {
            if (tempFile == null)
            {
                throw new ArgumentNullException("filename");
            }

            FileName = fileName;
            TempFile = tempFile;
            entity_repository = new EntityRepository();
        }

        /// <summary>
        /// serialize the level to file
        /// </summary>
        /// <param name="tempFiles">true when wanting to serialize temporary file(s)</param>
        public void SerializeToFile(bool tempFiles = false)
        {
            //skg daphne serialize to json Thursday, April 18, 2013
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;

            //serialize Protocol
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);
            string jsonFile = tempFiles == true ? TempFile : FileName;

            try
            {
                File.WriteAllText(jsonFile, jsonSpec);
            }
            catch
            {
                MessageBox.Show("File.WriteAllText failed in SerializeToFile. Filename and TempFile = " + FileName + ", " + TempFile);
            }
        }

        /// <summary>
        /// serialize to string
        /// </summary>
        /// <returns>level content as string</returns>
        public string SerializeToString()
        {
            //skg daphne serialize to json string Wednesday, May 08, 2013
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);
            return jsonSpec;
        }

        /// <summary>
        /// deserialize this level
        /// </summary>
        /// <param name="tempFiles">true when wanting to deserialize temporary file(s)</param>
        /// <returns>deserialized level as object for further assignment</returns>
        public virtual Level Deserialize(bool tempFiles = false)
        {
            //Deserialize JSON
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            //deserialize
            string jsonFile = tempFiles == true ? TempFile : FileName;
            string readText = File.ReadAllText(jsonFile);
            Level local = JsonConvert.DeserializeObject<Level>(readText, settings);

            // after deserialization the names are blank, restore them
            local.FileName = FileName;
            local.TempFile = TempFile;
            return local;
        }

        /// <summary>
        /// deserialize this level from string format
        /// </summary>
        /// <param name="jsonFile">file content in string</param>
        /// <returns>deserialized level as object for further assignment</returns>
        public virtual Level DeserializeFromString(string jsonFile)
        {
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            Level local = JsonConvert.DeserializeObject<Level>(jsonFile, settings);

            // after deserialization the names are blank, restore them
            local.FileName = FileName;
            local.TempFile = TempFile;
            return local;
        }

        [JsonIgnore]
        public string FileName { get; set; }
        [JsonIgnore]
        public string TempFile { get; set; }

        /// <summary>
        /// entity repository storing all available entities in this level
        /// </summary>
        public EntityRepository entity_repository { get; set; }
    }

    /// <summary>
    /// the protocol is a special type of level; it has extra information that set up an experiment with the entities of the entity repository
    /// </summary>
    public class Protocol : Level
    {
        public static int SafeCellPopulationID = 0;
        public int experiment_db_id { get; set; }
        public string experiment_name { get; set; }
        public int experiment_reps { get; set; }
        public string experiment_guid { get; set; }
        public string experiment_description { get; set; }
        public Scenario scenario { get; set; }
        public Scenario rc_scenario { get; set; }
        public SimulationParams sim_params { get; set; }
        public string reporter_file_name { get; set; }

        //public ChartViewToolWindow ChartWindow;

        /// <summary>
        /// constructor
        /// </summary>
        public Protocol() : this("", "")
        {
        }

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="fileName">protocol file</param>
        /// <param name="tempFile">temporary file</param>
        public Protocol(string fileName, string tempFile) : base(fileName, tempFile)
        {
            Guid id = Guid.NewGuid();

            experiment_guid = id.ToString();
            experiment_db_id = 0;
            experiment_name = "Experiment1";
            experiment_reps = 1;
            experiment_description = "Whole sim config description";
            scenario = new Scenario();
            rc_scenario = new Scenario();
            sim_params = new SimulationParams();

            //////LoadDefaultGlobalParameters();

            reporter_file_name = "";
        }

        /// <summary>
        /// serialize the protocol to a string, skip the 'decorations', i.e. experiment name and description
        /// </summary>
        /// <returns>the protocol serialized to a string</returns>
        public string SerializeToStringSkipDeco()
        {
            // remember name and description
            string exp_name = experiment_name,
                   exp_desc = experiment_description,
                   ret;

            // temporarily set name and description to empty strings
            experiment_name = "";
            experiment_description = "";
            // serialize to string
            ret = SerializeToString();
            // reset to the remembered string values
            experiment_name = exp_name;
            experiment_description = exp_desc;
            // return serialized string
            return ret;
        }

        /// <summary>
        /// override deserialization for the protocol; needs to handle extra data only contained in the protocol level
        /// </summary>
        /// <param name="tempFiles">true to indicate deserialization of the temporary file(s)</param>
        /// <returns>deserialized protocol as Level object</returns>
        public override Level Deserialize(bool tempFiles = false)
        {
            //Deserialize JSON
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            //deserialize
            string jsonFile = tempFiles == true ? TempFile : FileName;
            string readText = File.ReadAllText(jsonFile);
            Protocol local = JsonConvert.DeserializeObject<Protocol>(readText, settings);

            // after deserialization, the names are blank, restore them
            local.FileName = FileName;
            local.TempFile = TempFile;

            local.InitializeStorageClasses();

            return local;
        }

        /// <summary>
        /// override deserialization from string for the protocol; needs to handle extra data only contained in the protocol level
        /// </summary>
        /// <param name="jsonFile">the protocol file in string format</param>
        /// <returns>deserialized protocol as Level object</returns>
        public override Level DeserializeFromString(string jsonFile)
        {
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            Protocol local = JsonConvert.DeserializeObject<Protocol>(jsonFile, settings);

            // after deserialization the names are blank, restore them
            local.FileName = FileName;
            local.TempFile = TempFile;

            local.InitializeStorageClasses();

            return local;
        }

        /// <summary>
        /// Routine called when the environment extent changes
        /// Updates all box specifications in repository with correct max & min for sliders in GUI
        /// Also updates VTK visual environment box
        /// Also updates cell coordinates
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void environment_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            // Check that cells are still inside the simulation space.
            foreach (CellPopulation cellPop in scenario.cellpopulations)
            {
                cellPop.cellPopDist.Resize(new double[3] { scenario.environment.extent_x, scenario.environment.extent_y, scenario.environment.extent_z});
            }

            // Update VTK environment box 
            var env = scenario.environment;
            /////MainWindow.VTKBasket.EnvironmentController.setupBox(env.extent_x, env.extent_y, env.extent_z);
            // NOTE: Rwc.Invalidate happens to also be called because every box specification has a callback to
            //   GUIInteractionToWidgetCallback on its PropertyChanged, and that calls Invalidate, but wanted to
            //   call it here, too, for environment change explicitly just in case box specs are handled differently
            //   in the future, didn't want the environment box to stop updating "live" mysteriously.
            /////MainWindow.GC.Rwc.Invalidate();
        }

        /// <summary>
        /// CollectionChanged not called during deserialization, so manual call to set up utility classes.
        /// Also take care of any other post-deserialization setup.
        /// </summary>
        public void InitializeStorageClasses()
        {
            // GenerateNewExperimentGUID();
            FindNextSafeCellPopulationID();
            scenario.InitBoxExtentsAndGuidBoxDict();
            scenario.InitGaussSpecsAndGuidGaussDict();
            scenario.InitCellPopulationIDCellPopulationDict();
            scenario.InitGaussCellPopulationUpdates();
            InitMoleculeIDConfigMoleculeDict();
            InitMolPopIDConfigMolecularPopDict_ECMProbeDict();
            InitCellIDConfigCellDict();
            InitReactionTemplateIDConfigReactionTemplateDict();
            InitGeneIDConfigGeneDict();
            InitReactionIDConfigReactionDict();
            InitReactionComplexIDConfigReactionComplexDict();
            InitDiffSchemeIDConfigDiffSchemeDict();
            InitTransitionDriversDict();
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

        private void InitMoleculeIDConfigMoleculeDict()
        {
            entity_repository.molecules_dict.Clear();
            foreach (ConfigMolecule cm in entity_repository.molecules)
            {
                entity_repository.molecules_dict.Add(cm.entity_guid, cm);
            }
            entity_repository.molecules.CollectionChanged += new NotifyCollectionChangedEventHandler(molecules_CollectionChanged);
        }

        private void InitMolPopIDConfigMolecularPopDict_ECMProbeDict()
        {
            ConfigCompartment ecs = scenario.environment.ecs;

            // build ecm_probe_dict
            foreach (CellPopulation cp in scenario.cellpopulations)
            {
                cp.ecm_probe_dict.Clear();
                foreach (ReportECM recm in cp.ecm_probe)
                {
                    cp.ecm_probe_dict.Add(recm.molpop_guid_ref, recm);
                }
            }

            ecs.molpops.CollectionChanged += new NotifyCollectionChangedEventHandler(ecm_molpops_CollectionChanged);
        }

        private void InitDiffSchemeIDConfigDiffSchemeDict()
        {
            entity_repository.diff_schemes_dict.Clear();
            foreach (ConfigDiffScheme ds in entity_repository.diff_schemes)
            {
                entity_repository.diff_schemes_dict.Add(ds.entity_guid, ds);
            }
            entity_repository.diff_schemes.CollectionChanged += new NotifyCollectionChangedEventHandler(diff_schemes_CollectionChanged);
        }
        
        private void InitTransitionDriversDict()
        {
            entity_repository.transition_drivers_dict.Clear();
            foreach (ConfigTransitionDriver tran in entity_repository.transition_drivers)
            {
                entity_repository.transition_drivers_dict.Add(tran.entity_guid, tran);
            }
            entity_repository.diff_schemes.CollectionChanged += new NotifyCollectionChangedEventHandler(transition_drivers_CollectionChanged);
        }


        private void InitGeneIDConfigGeneDict()
        {
            entity_repository.genes_dict.Clear();
            foreach (ConfigGene cg in entity_repository.genes)
            {
                entity_repository.genes_dict.Add(cg.entity_guid, cg);
            }
            entity_repository.genes.CollectionChanged += new NotifyCollectionChangedEventHandler(genes_CollectionChanged);
        }

        

        private void InitCellIDConfigCellDict()
        {
            entity_repository.cells_dict.Clear();
            foreach (ConfigCell cc in entity_repository.cells)
            {
                entity_repository.cells_dict.Add(cc.entity_guid, cc);
            }
            entity_repository.cells.CollectionChanged += new NotifyCollectionChangedEventHandler(cells_CollectionChanged);

        }

        private void InitReactionIDConfigReactionDict()
        {
            entity_repository.reactions_dict.Clear();
            foreach (ConfigReaction cr in entity_repository.reactions)
            {
                entity_repository.reactions_dict.Add(cr.entity_guid, cr);
                cr.GetTotalReactionString(entity_repository);
            }
            entity_repository.reactions.CollectionChanged += new NotifyCollectionChangedEventHandler(reactions_CollectionChanged);

        }

        private void InitReactionComplexIDConfigReactionComplexDict()
        {
            entity_repository.reaction_complexes_dict.Clear();
            foreach (ConfigReactionComplex crc in entity_repository.reaction_complexes)
            {
                entity_repository.reaction_complexes_dict.Add(crc.entity_guid, crc);
            }
            entity_repository.reaction_complexes.CollectionChanged += new NotifyCollectionChangedEventHandler(reaction_complexes_CollectionChanged);

        }

        private void InitReactionTemplateIDConfigReactionTemplateDict()
        {
            entity_repository.reaction_templates_dict.Clear();
            foreach (ConfigReactionTemplate crt in entity_repository.reaction_templates)
            {
                entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);
            }
            entity_repository.reaction_templates.CollectionChanged += new NotifyCollectionChangedEventHandler(template_reactions_CollectionChanged);
        }

        //genes_CollectionChanged
        private void genes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigGene cg = nn as ConfigGene;

                    if (cg != null)
                    {
                        entity_repository.genes_dict.Add(cg.entity_guid, cg);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigGene cg = dd as ConfigGene;

                    //Remove gene from genes_dict
                    entity_repository.genes_dict.Remove(cg.entity_guid);
                }
            }
        }

        //diff_schemes_CollectionChanged
        private void diff_schemes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigDiffScheme cds = nn as ConfigDiffScheme;

                    if (cds != null)
                    {
                        entity_repository.diff_schemes_dict.Add(cds.entity_guid, cds);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigDiffScheme cds = dd as ConfigDiffScheme;

                    //Remove gene from genes_dict
                    entity_repository.diff_schemes_dict.Remove(cds.entity_guid);
                }
            }
        }

        private void transition_drivers_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigTransitionDriver tran = nn as ConfigTransitionDriver;

                    if (tran != null)
                    {
                        entity_repository.transition_drivers_dict.Add(tran.entity_guid, tran);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigTransitionDriver tran = dd as ConfigTransitionDriver;

                    //Remove gene from transition_drivers_dict
                    entity_repository.transition_drivers_dict.Remove(tran.entity_guid);
                }
            }
        }

        private void molecules_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            // Need to figure out how to signal to the collection view source that the collection has changed and it should refresh
            // This is not currently a problem because it is handled in memb_molecule_combo_box_GotFocus and cyto_molecule_combo_box_GotFocus
            // But this may be the better place to handle it.

            // Raise a CollectionChanged event with Action set to Reset to refresh the UI. 
            // cvsBoundaryMolListView.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            // NotifyCollectionChangedEventArgs a = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
            // entity_repository.molecules    ///OnCollectionChanged(a);


            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigMolecule cm = nn as ConfigMolecule;
                    entity_repository.molecules_dict.Add(cm.entity_guid, cm);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigMolecule cm = dd as ConfigMolecule;

                    //Remove molecule from molecules_dict
                    entity_repository.molecules_dict.Remove(cm.entity_guid);

                    foreach (ConfigMolecularPopulation cmp in scenario.environment.ecs.molpops.ToList())
                    {
                        if (cmp.molecule.entity_guid == cm.entity_guid)
                        {
                            scenario.environment.ecs.molpops.Remove(cmp);
                        }
                    }

                    //Remove all the cell membrane molpops that have this molecule type
                    foreach (ConfigCell cell in entity_repository.cells)
                    {
                        foreach (ConfigMolecularPopulation cmp in cell.membrane.molpops.ToList())
                        {
                            if (cmp.molecule.entity_guid == cm.entity_guid)
                            {
                                cell.membrane.molpops.Remove(cmp);
                            }
                        }
                    }

                    //Remove all the cell cytosol molpops that have this molecule type
                    foreach (ConfigCell cell in entity_repository.cells)
                    {
                        foreach (ConfigMolecularPopulation cmp in cell.cytosol.molpops.ToList())
                        {
                            if (cmp.molecule.entity_guid == cm.entity_guid)
                            {
                                cell.cytosol.molpops.Remove(cmp);
                            }
                        }
                    }

                    //Remove all the reactions that use this molecule
                    foreach (KeyValuePair<string, ConfigReaction> kvp in entity_repository.reactions_dict.ToList())
                    {
                        ConfigReaction reac = kvp.Value;
                        if (reac.HasMolecule(cm.entity_guid))
                        {
                            entity_repository.reactions_dict.Remove(kvp.Key);
                            entity_repository.reactions.Remove(kvp.Value);
                        }
                    }
                }
            }
        }

        private void ecm_molpops_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigMolecularPopulation mp = nn as ConfigMolecularPopulation;

                    ////// add molpop into molpops_dict
                    ////if (!scenario.environment.ecs.molpops_dict.ContainsKey(mp.molpop_guid))
                    ////{
                    ////    scenario.environment.ecs.molpops_dict.Add(mp.molpop_guid, mp);
                    ////}

                    // add ecm report
                    foreach (CellPopulation cp in scenario.cellpopulations)
                    {
                        ReportECM er = new ReportECM();

                        er.molpop_guid_ref = mp.molpop_guid;
                        cp.ecm_probe.Add(er);
                        cp.ecm_probe_dict.Add(mp.molpop_guid, er);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigMolecularPopulation mp = dd as ConfigMolecularPopulation;

                    ////// remove from molpops_dict
                    ////if (scenario.environment.ecs.molpops_dict.ContainsKey(mp.molpop_guid))
                    ////{
                    ////    scenario.environment.ecs.molpops_dict.Remove(mp.molpop_guid);
                    ////}

                    // remove ecm report
                    foreach (CellPopulation cp in scenario.cellpopulations)
                    {
                        // need to keep an eye on this; this poses an inefficient way of doing the removal; it should not happen excessively; if it did, we'd need a change here
                        cp.ecm_probe.Remove(cp.ecm_probe_dict[mp.molpop_guid]);
                        cp.ecm_probe_dict.Remove(mp.molpop_guid);
                    }
                }
            }
        }


        private void cells_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigCell cc = nn as ConfigCell;
                    entity_repository.cells_dict.Add(cc.entity_guid, cc);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigCell cc = dd as ConfigCell;

                    //Remove this guid from ER cells_dict
                    entity_repository.cells_dict.Remove(cc.entity_guid);

                    //Remove all ECM cell populations with this cell guid
                    foreach (var cell_pop in scenario.cellpopulations.ToList())
                    {
                        if (cc.entity_guid == cell_pop.Cell.entity_guid)
                            scenario.cellpopulations.Remove(cell_pop);
                    }
                }
            }
        }

        private void reactions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigReaction cr = nn as ConfigReaction;
                    entity_repository.reactions_dict.Add(cr.entity_guid, cr);
                    cr.GetTotalReactionString(entity_repository);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigReaction cr = dd as ConfigReaction;

                    //Remove entry from ER reactions_dict
                    entity_repository.reactions_dict.Remove(cr.entity_guid);

                    //Remove all the ER reaction complex reactions that have this guid
                    foreach (ConfigReactionComplex comp in entity_repository.reaction_complexes)
                    {
                        if (comp.reactions_guid_ref.Contains(cr.entity_guid))
                        {
                            comp.reactions_guid_ref.Remove(cr.entity_guid);
                        }
                    }                    

                    //Remove all the ECM reaction complex reactions that have this guid
                    if (scenario.environment.ecs.reaction_complexes_guid_ref.Contains(cr.entity_guid))
                    {
                        scenario.environment.ecs.reaction_complexes_guid_ref.Remove(cr.entity_guid);
                    }

                    //Remove all the ECM reactions that have this guid
                    if (scenario.environment.ecs.Reactions.Contains(cr))
                    {
                        scenario.environment.ecs.Reactions.Remove(cr);
                    }

                    //Remove all the cell membrane/cytosol reactions that have this guid
                    foreach (ConfigCell cell in entity_repository.cells)
                    {
                        if (cell.membrane.Reactions.Contains(cr))
                        {
                            cell.membrane.Reactions.Remove(cr);
                        }

                        if (cell.cytosol.Reactions.Contains(cr))
                        {
                            cell.cytosol.Reactions.Remove(cr);
                        }
                    }
                }                
            }
        }

        private void template_reactions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigReactionTemplate crt = nn as ConfigReactionTemplate;
                    entity_repository.reaction_templates_dict.Add(crt.entity_guid, crt);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigReactionTemplate crt = dd as ConfigReactionTemplate;
                    entity_repository.reaction_templates_dict.Remove(crt.entity_guid);
                }
            }
        }

        private void reaction_complexes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigReactionComplex crc = nn as ConfigReactionComplex;
                    entity_repository.reaction_complexes_dict.Add(crc.entity_guid, crc);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigReactionComplex crt = dd as ConfigReactionComplex;
                    entity_repository.reaction_complexes_dict.Remove(crt.entity_guid);
                }
            }
        }

        public ConfigMolecule FindMolecule(string name)
        {
            ConfigMolecule gm = null;

            foreach (ConfigMolecule g in entity_repository.molecules)
            {
                if (g.Name == name)
                {
                    gm = g;
                    break;
                }
            }
            return gm;
        }

        // given a gene name, find its guid
        public string findGeneGuid(string name, Protocol protocol)
        {
            foreach (ConfigGene gene in protocol.entity_repository.genes)
            {
                if (gene.Name == name)
                {
                    return gene.entity_guid;
                }
            }
            return "";
        }

        // given a total reaction string, find the ConfigCell object
        public bool findReactionByTotalString(string total, Protocol protocol)
        {            
            //Get left and right side molecules of new reaction
            List<string> newReactants = getReacLeftSide(total);   
            List<string> newProducts = getReacRightSide(total);

            //Loop through all existing reactions
            foreach (ConfigReaction reac in protocol.entity_repository.reactions)
            {
                //Get left and right side molecules of each reaction in er
                List<string> currReactants = getReacLeftSide(reac.TotalReactionString);
                List<string> currProducts = getReacRightSide(reac.TotalReactionString);

                //Key step! 
                //Check if the list of reactants and products in new reaction equals 
                //the list of reactants and products in this current reaction
                if (newReactants.SequenceEqual(currReactants) && newProducts.SequenceEqual(currProducts))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// This method takes the ConfigReaction's TotalReactionString and returns a sorted 
        /// list of molecule strings on the left side, i.e. the reactants.
        /// </summary>
        /// <param name="total"></param>
        /// <returns></returns>
        private List<string> getReacLeftSide(string total) 
        {
            int len = total.Length;
            int index = total.IndexOf("->");
            string left = total.Substring(0, index);
            left = left.Replace(" ", "");
            char[] separator = { '+' };
            string[] reactants = left.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            List<string> listLeft = new List<string>(reactants);
            listLeft.Sort();
            return listLeft;
        }

        /// <summary>
        /// This method takes the ConfigReaction's TotalReactionString and returns a sorted 
        /// list of molecule strings on the right side, i.e. the products.
        /// </summary>
        /// <param name="total"></param>
        /// <returns></returns>
        private List<string> getReacRightSide(string total)
        {
            int len = total.Length;
            int index = total.IndexOf("->");
            string right = total.Substring(index + 2);
            right = right.Replace(" ", "");
            char[] separator = { '+' };
            string[] products = right.Split(separator, StringSplitOptions.RemoveEmptyEntries);
            List<string> listRight = new List<string>(products);
            listRight.Sort();
            return listRight;
        }


        /// <summary>
        /// Select transcription reactions in the compartment.
        /// </summary>
        /// <param name="configComp">the compartment</param>
        /// <returns></returns>
        public List<ConfigReaction> GetTranscriptionReactions(ConfigCompartment configComp)
        {
            List<string> reac_guids = new List<string>();
            List<ConfigReaction> config_reacs = new List<ConfigReaction>();

            // Compartment reactions
            foreach (ConfigReaction cr in configComp.Reactions)
            {
                if (entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Transcription)
                {
                    reac_guids.Add(cr.entity_guid);
                    config_reacs.Add(cr);
                }
            }

            // Compartment reaction complexes
            foreach (string rcguid in configComp.reaction_complexes_guid_ref)
            {
                ConfigReactionComplex crc = entity_repository.reaction_complexes_dict[rcguid];

                foreach (string rguid in crc.reactions_guid_ref)
                {
                    if (reac_guids.Contains(rguid) == false)
                    {
                        ConfigReaction cr = entity_repository.reactions_dict[rguid];

                        if (entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Transcription)
                        {
                            config_reacs.Add(cr);
                        }
                    }
                }
            }

            return config_reacs;
        }

        /// <summary>
        /// Select boundary or bulk reactions in the compartment.
        /// </summary>
        /// <param name="configComp">the compartment</param>
        /// <param name="boundMol">boolean: true to select boundary, false to select bulk</param>
        /// <returns></returns>
        public List<ConfigReaction> GetReactions(ConfigCompartment configComp, bool boundMol)
        {
            List<string> reac_guids = new List<string>();
            List<ConfigReaction> config_reacs = new List<ConfigReaction>();

            // Compartment reactions
            foreach (ConfigReaction cr in configComp.Reactions)
            {
                if (entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].isBoundary == boundMol)
                {
                    reac_guids.Add(cr.entity_guid);
                    config_reacs.Add(cr);
                }
            }

            // Compartment reaction complexes
            foreach (string rcguid in configComp.reaction_complexes_guid_ref)
            {
                ConfigReactionComplex crc = entity_repository.reaction_complexes_dict[rcguid];

                foreach (string rguid in crc.reactions_guid_ref)
                {
                    if (reac_guids.Contains(rguid) == false)
                    {
                        ConfigReaction cr = entity_repository.reactions_dict[rguid];

                        if (entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].isBoundary == boundMol)
                        {
                            config_reacs.Add(cr);
                        }
                    }
                }
            }

            return config_reacs;
        }

        // given a reaction template type, find its guid
        public string findReactionTemplateGuid(ReactionType rt)
        {
            foreach (ConfigReactionTemplate crt in entity_repository.reaction_templates)
            {
                if (crt.reac_type == rt)
                {
                    return crt.entity_guid;
                }
            }
            return null;
        }

        public string findMoleculeGuidByName(string inputMolName)
        {
            string guid = "";
            foreach (ConfigMolecule cm in entity_repository.molecules)
            {
                if (cm.Name == inputMolName)
                {
                    guid = cm.entity_guid;
                    break;
                }
            }
            return guid;
        }

        public bool HasMoleculeType(Dictionary<string, int> inputList, MoleculeLocation molLoc)
        {
            foreach (KeyValuePair<string, int> kvp in inputList)
            {
                string guid = findMoleculeGuidByName(kvp.Key);
                if (entity_repository.molecules_dict[guid].molecule_location == molLoc)
                {
                    return true;
                }
            }
            return false;
        }

        private bool HasGene(Dictionary<string, int> inputList)
        {
            foreach (KeyValuePair<string, int> kvp in inputList)
            {
                string guid = findGeneGuidByName(kvp.Key);
                if (guid != "")
                {
                    return true;
                }
            }
            return false;
        }


        /// <summary>
        /// Given a gene name, check if it exists in repository - return guid
        /// </summary>
        /// <param name="inputGeneName"></param>
        /// <returns></returns>

        public string findGeneGuidByName(string inputGeneName)
        {
            string guid = "";
            foreach (ConfigGene cg in this.entity_repository.genes)
            {
                if (cg.Name == inputGeneName)
                {
                    guid = cg.entity_guid;
                    break;
                }
            }
            return guid;
        }

        public string IdentifyReactionType(Dictionary<string, int> inputReactants, Dictionary<string, int> inputProducts, Dictionary<string, int> inputModifiers)
        {
            string reaction_template_guid_ref = "";

            int totalReacStoich = 0;
            foreach (KeyValuePair<string, int> kvp in inputReactants)
            {
                totalReacStoich += kvp.Value;
            }

            int totalProdStoich = 0;
            foreach (KeyValuePair<string, int> kvp in inputProducts)
            {
                totalProdStoich += kvp.Value;
            }

            int totalModStoich = 0;
            foreach (KeyValuePair<string, int> kvp in inputModifiers)
            {
                totalModStoich += kvp.Value;
            }

            if (HasGene(inputReactants) || HasGene(inputProducts))
            {
                // No reactions supported for genes as reactant or product
                return reaction_template_guid_ref;
            }

            bool geneModifier = HasGene(inputModifiers);
            bool boundProd = HasMoleculeType(inputProducts, MoleculeLocation.Boundary);

            if (geneModifier)
            {
                if ((inputModifiers.Count > 1) || (inputProducts.Count != 1) || (inputReactants.Count != 0) || (totalModStoich > 1) || (totalProdStoich > 1) || (boundProd))
                {
                    // Gene transcription reaction does not support these possibilities
                    return reaction_template_guid_ref;
                }
                else
                {
                    return findReactionTemplateGuid(ReactionType.Transcription);
                }
            }

           
            bool bulkProd = HasMoleculeType(inputProducts, MoleculeLocation.Bulk);
            bool boundReac = HasMoleculeType(inputReactants, MoleculeLocation.Boundary);
            bool bulkReac = HasMoleculeType(inputReactants, MoleculeLocation.Bulk);
            bool boundMod = HasMoleculeType(inputModifiers, MoleculeLocation.Boundary);
            bool bulkMod = HasMoleculeType(inputModifiers, MoleculeLocation.Bulk);

            int bulkBoundVal = 1,
                    modVal = 10,
                    reacVal = 100,
                    prodVal = 1000,
                    reacStoichVal = 10000,
                    prodStoichVal = 100000,
                    modStoichVal = 1000000;

            if (inputModifiers.Count > 9 || inputReactants.Count > 9 || inputProducts.Count > 9 || totalReacStoich > 9 || totalProdStoich > 9 || totalModStoich > 9)
            {
                throw new Exception("Unsupported reaction with current typing algorithm.\n");
            }

            int reacNum = inputModifiers.Count * modVal
                            + inputReactants.Count * reacVal
                            + inputProducts.Count * prodVal
                            + totalReacStoich * reacStoichVal
                            + totalProdStoich * prodStoichVal
                            + totalModStoich * modStoichVal;

            if ((boundReac || boundProd || boundMod) && (bulkReac || bulkProd || bulkMod))
            {
                reacNum += bulkBoundVal;
            }

            switch (reacNum)
            {
                // Interior
                case 10100:
                    return findReactionTemplateGuid(ReactionType.Annihilation);
                case 121200:
                    return findReactionTemplateGuid(ReactionType.Association);
                case 121100:
                    return findReactionTemplateGuid(ReactionType.Dimerization);
                case 211100:
                    return findReactionTemplateGuid(ReactionType.DimerDissociation);
                case 212100:
                    return findReactionTemplateGuid(ReactionType.Dissociation);
                case 111100:
                    return findReactionTemplateGuid(ReactionType.Transformation);
                case 221200:
                    return findReactionTemplateGuid(ReactionType.AutocatalyticTransformation);
                // Interior Catalyzed (catalyst stoichiometry doesn't change)
                case 1010110:
                    return findReactionTemplateGuid(ReactionType.CatalyzedAnnihilation);
                case 1121210:
                    return findReactionTemplateGuid(ReactionType.CatalyzedAssociation);
                case 1101010:
                    return findReactionTemplateGuid(ReactionType.CatalyzedCreation);
                case 1121110:
                    return findReactionTemplateGuid(ReactionType.CatalyzedDimerization);
                case 1211110:
                    return findReactionTemplateGuid(ReactionType.CatalyzedDimerDissociation);
                case 1212110:
                    return findReactionTemplateGuid(ReactionType.CatalyzedDissociation);
                case 1111110:
                    return findReactionTemplateGuid(ReactionType.CatalyzedTransformation);
                // Bulk/Boundary reactions
                case 121201:
                    if ((boundProd) && (boundReac))
                    {
                        // The product and one of the reactants must be boundary molecules 
                        return findReactionTemplateGuid(ReactionType.BoundaryAssociation);
                    }
                    else
                    {
                        return reaction_template_guid_ref;
                    }
                case 212101:
                    if ((boundProd) && (boundReac))
                    {
                        // The reactant and one of the products must be boundary molecules 
                        return findReactionTemplateGuid(ReactionType.BoundaryDissociation);
                    }
                    else
                    {
                        return reaction_template_guid_ref;
                    }
                case 111101:
                    if (boundReac)
                    {
                        return findReactionTemplateGuid(ReactionType.BoundaryTransportFrom);
                    }
                    else
                    {
                        return findReactionTemplateGuid(ReactionType.BoundaryTransportTo);
                    }
                // Catalyzed Bulk/Boundary reactions
                case 1111111:
                    if (boundMod)
                    {
                        return findReactionTemplateGuid(ReactionType.CatalyzedBoundaryActivation);
                    }
                    else
                    {
                        return reaction_template_guid_ref;
                    }
                // Generalized reaction
                default:
                    // Not implemented yet
                    return reaction_template_guid_ref;
            }
        }
    }

    // start at > 0 as zero seems to be the default for metadata when a property is not present
    public enum SimStates { Linear = 1, Cubic, Tiny, Large };
    
    public class Scenario
    {
        public SimStates simInterpolate { get; set; }
        public SimStates simCellSize { get; set; }
        public TimeConfig time_config { get; set; }
        public ConfigEnvironment environment { get; set; }
        public ObservableCollection<CellPopulation> cellpopulations { get; set; }

        public ObservableCollection<GaussianSpecification> gaussian_specifications { get; set; }
        public ObservableCollection<BoxSpecification> box_specifications { get; set; }

        // Convenience utility storage (not serialized)
        [XmlIgnore]
        public Dictionary<string, BoxSpecification> box_guid_box_dict;

        [XmlIgnore]
        public Dictionary<int, CellPopulation> cellpopulation_id_cellpopulation_dict;   

        [XmlIgnore]
        public Dictionary<string, GaussianSpecification> gauss_guid_gauss_dict;
        

        public Scenario()
        {
            simInterpolate = SimStates.Linear;
            simCellSize = SimStates.Tiny;
            time_config = new TimeConfig();
            environment = new ConfigEnvironment();
            cellpopulations = new ObservableCollection<CellPopulation>();

            gaussian_specifications = new ObservableCollection<GaussianSpecification>();
            box_specifications = new ObservableCollection<BoxSpecification>();

            // Utility storage
            // NOTE: No use adding CollectionChanged event handlers here since it gets wiped out by deserialization anyway...
            box_guid_box_dict = new Dictionary<string, BoxSpecification>();
            cellpopulation_id_cellpopulation_dict = new Dictionary<int, CellPopulation>();
            gauss_guid_gauss_dict = new Dictionary<string, GaussianSpecification>();
        }

        private void SetBoxSpecExtents(BoxSpecification bs)
        {
            bs.x_scale_max = environment.extent_x;
            bs.x_scale_min = environment.extent_min;
            bs.x_trans_max = 1.5 * environment.extent_x;
            bs.x_trans_min = -environment.extent_x / 2.0;

            bs.y_scale_max = environment.extent_y;
            bs.y_scale_min = environment.extent_min;
            bs.y_trans_max = 1.5 * environment.extent_y;
            bs.y_trans_min = -environment.extent_y / 2.0;

            bs.z_scale_max = environment.extent_z;
            bs.z_scale_min = environment.extent_min;
            bs.z_trans_max = 1.5 * environment.extent_z;
            bs.z_trans_min = -environment.extent_z / 2.0;
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

        private void gaussian_specifications_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    GaussianSpecification gs = nn as GaussianSpecification;
                    gauss_guid_gauss_dict.Add(gs.gaussian_spec_box_guid_ref, gs);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    GaussianSpecification gs = dd as GaussianSpecification;
                    gauss_guid_gauss_dict.Remove(gs.gaussian_spec_box_guid_ref);
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

                    foreach (ConfigMolecularPopulation mp in environment.ecs.molpops)
                    {
                        ReportECM er = new ReportECM();

                        er.molpop_guid_ref = mp.molpop_guid;
                        cs.ecm_probe.Add(er);
                        cs.ecm_probe_dict.Add(mp.molpop_guid, er);
                    }
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

        public void InitBoxExtentsAndGuidBoxDict()
        {
            box_guid_box_dict.Clear();
            foreach (BoxSpecification bs in box_specifications)
            {
                box_guid_box_dict.Add(bs.box_guid, bs);

                // Piggyback on this routine to set initial extents from environment values
                SetBoxSpecExtents(bs);
            }
            box_specifications.CollectionChanged += new NotifyCollectionChangedEventHandler(box_specifications_CollectionChanged);
        }

        public void InitGaussSpecsAndGuidGaussDict()
        {
            gauss_guid_gauss_dict.Clear();
            foreach (GaussianSpecification gs in gaussian_specifications)
            {
                gauss_guid_gauss_dict.Add(gs.gaussian_spec_box_guid_ref, gs);
            }
            gaussian_specifications.CollectionChanged += new NotifyCollectionChangedEventHandler(gaussian_specifications_CollectionChanged);
        }

        public void InitCellPopulationIDCellPopulationDict()
        {
            cellpopulation_id_cellpopulation_dict.Clear();
            foreach (CellPopulation cs in cellpopulations)
            {
                cellpopulation_id_cellpopulation_dict.Add(cs.cellpopulation_id, cs);

                if (cs.cellPopDist != null)
                {
                    cs.cellPopDist.cellPop = cs;
                }
            }
            cellpopulations.CollectionChanged += new NotifyCollectionChangedEventHandler(cellsets_CollectionChanged);
        }

        public void InitGaussCellPopulationUpdates()
        {
            foreach (CellPopulation cs in cellpopulations)
            {
                if (cs.cellPopDist.DistType == CellPopDistributionType.Gaussian)
                {
                    BoxSpecification box = box_guid_box_dict[((CellPopGaussian)cs.cellPopDist).box_guid];
                    ((CellPopGaussian)cs.cellPopDist).ParamReset(box);
                    box.PropertyChanged += new PropertyChangedEventHandler(((CellPopGaussian)cs.cellPopDist).CellPopGaussChanged);
                }
            }
        }

        public CellPopulation GetCellPopulation(int key)
        {
            if (cellpopulation_id_cellpopulation_dict.ContainsKey(key) == true)
            {
                return cellpopulation_id_cellpopulation_dict[key];
            }
            else
            {
                throw new Exception("Population ID does not exist.");
            }
        }

        public bool HasCell(ConfigCell cell)
        {
            bool res = false;
            foreach (CellPopulation cell_pop in cellpopulations)
            {
                if (cell_pop.Cell.entity_guid == cell.entity_guid)
                {
                    return true;
                }
            }
            return res;
        }
    }

    public class SimulationParams
    {
        public SimulationParams()
        {
            // default value
            phi1 = 100;
        }
        public double phi1 { get; set; }
        public double phi2 { get; set; }
    }

    public class EntityRepository 
    {
        public ObservableCollection<ConfigReactionComplex> reaction_complexes { get; set; }

        //All molecules, reactions, cells - Combined Predefined and User defined
        public ObservableCollection<ConfigCell> cells { get; set; }
        public ObservableCollection<ConfigMolecule> molecules { get; set; }
        public ObservableCollection<ConfigGene> genes { get; set; }
        public ObservableCollection<ConfigReaction> reactions { get; set; }
        public ObservableCollection<ConfigReactionTemplate> reaction_templates { get; set; }
        public ObservableCollection<ConfigDiffScheme> diff_schemes { get; set; }
        public ObservableCollection<ConfigTransitionDriver> transition_drivers { get; set; }

        [JsonIgnore]
        public Dictionary<string, ConfigMolecule> molecules_dict; // keyed by molecule_guid
        [JsonIgnore]
        public Dictionary<string, ConfigGene> genes_dict; // keyed by gene_guid
        [JsonIgnore]
        public Dictionary<string, ConfigReactionTemplate> reaction_templates_dict;
        [JsonIgnore]
        public Dictionary<string, ConfigReaction> reactions_dict;
        [JsonIgnore]
        public Dictionary<string, ConfigCell> cells_dict;
        [JsonIgnore]
        public Dictionary<string, ConfigReactionComplex> reaction_complexes_dict;
        [JsonIgnore]
        public Dictionary<string, ConfigDiffScheme> diff_schemes_dict;
        [JsonIgnore]
        public Dictionary<string, ConfigTransitionDriver> transition_drivers_dict;


        public EntityRepository()
        {
            cells = new ObservableCollection<ConfigCell>();
            molecules = new ObservableCollection<ConfigMolecule>();
            genes = new ObservableCollection<ConfigGene>();
            reactions = new ObservableCollection<ConfigReaction>();
            reaction_templates = new ObservableCollection<ConfigReactionTemplate>();
            molecules_dict = new Dictionary<string, ConfigMolecule>();
            genes_dict = new Dictionary<string, ConfigGene>();
            reaction_templates_dict = new Dictionary<string, ConfigReactionTemplate>();
            reactions_dict = new Dictionary<string, ConfigReaction>();
            cells_dict = new Dictionary<string, ConfigCell>();
            reaction_complexes = new ObservableCollection<ConfigReactionComplex>();
            reaction_complexes_dict = new Dictionary<string, ConfigReactionComplex>();
            diff_schemes = new ObservableCollection<ConfigDiffScheme>();
            diff_schemes_dict = new Dictionary<string, ConfigDiffScheme>();
            transition_drivers = new ObservableCollection<ConfigTransitionDriver>();
            transition_drivers_dict = new Dictionary<string, ConfigTransitionDriver>();
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
            rendering_interval = 1;
            sampling_interval = 1;
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
                    int saveValue = _extent_x;
                    _extent_x = value;
                    if (CalculateNumGridPts())
                    {
                        OnPropertyChanged("extent_x");
                    }
                    else
                    {
                        _extent_x = saveValue;
                        System.Windows.MessageBox.Show("System must have at least 3 grid points on a side.");
                    }
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
                    int saveValue = _extent_y;
                    _extent_y = value;
                    if (CalculateNumGridPts())
                    {
                        OnPropertyChanged("extent_y");
                    }
                    else
                    {
                        _extent_y = saveValue;
                        System.Windows.MessageBox.Show("System must have at least 3 grid points on a side.");
                    }
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
                    int saveValue = _extent_z;
                    _extent_z = value;
                    if (CalculateNumGridPts())
                    {
                        OnPropertyChanged("extent_z");
                    }
                    else
                    {
                        _extent_z = saveValue;
                        System.Windows.MessageBox.Show("System must have at least 3 grid points on a side.");
                    }
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
                    double saveValue = _gridstep;
                    _gridstep = value;
                    if (CalculateNumGridPts())
                    {
                        OnPropertyChanged("gridstep");
                    }
                    else
                    {
                        _gridstep = saveValue;
                        System.Windows.MessageBox.Show("System must have at least 3 grid points on a side.");
                    }
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

        public ConfigCompartment ecs { get; set; }

        private bool _toroidal;
        public bool toroidal
        {
            get { return _toroidal; }
            set
            {
                if (_toroidal == value)
                    return;
                else
                {
                    _toroidal = value;
                    OnPropertyChanged("toroidal");
                }
            }
        }

        public ConfigEnvironment()
        {
            gridstep = 10;
            extent_x = 200;
            extent_y = 200;
            extent_z = 200;
            extent_min = 5;
            extent_max = 1000;
            gridstep_min = 1;
            gridstep_max = 100;
            initialized = true;
            toroidal = false;

            // Don't need to check the boolean returned, since we know these values are okay.
            CalculateNumGridPts();

            ecs = new ConfigCompartment();
        }

        private bool initialized = false;
        
        private bool CalculateNumGridPts()
        {
            if (initialized == false)
            {
                return true;
            }

            int[] pt = new int[3];

            pt[0] = (int)Math.Ceiling((decimal)(extent_x / gridstep)) + 1;
            pt[1] = (int)Math.Ceiling((decimal)(extent_y / gridstep)) + 1;
            pt[2] = (int)Math.Ceiling((decimal)(extent_z / gridstep)) + 1;

            // Must have at least 3 grid points for gradient routines at boundary points
            if ((pt[0] < 3) || (pt[1] < 3) || (pt[2] < 3))
            {
                return false;
            }

            NumGridPts = pt;

            return true;
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

    public enum ColorList { Red, Orange, Yellow, Green, Blue, Indigo, Violet, Custom }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(ColorList), typeof(int))]
    public class ColorListToIntConverter : IValueConverter
    {        
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return 1;

            try
            {
                int index = (int)value;
                return index;
            }
            catch
            {
                return 1;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return ColorList.Orange;

            int idx = (int)value;
            return (ColorList)Enum.ToObject(typeof(ColorList), (int)idx);
        }
    }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(ColorList), typeof(string))]
    public class ColorListToStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the MoleculeLocation enum...
        private List<string> _color_strings = new List<string>()
                                {
                                    "Red",
                                    "Orange",
                                    "Yellow",
                                    "Green",
                                    "Blue",
                                    "Indigo",
                                    "Violet",
                                    "Custom"
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _color_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return ColorList.Orange;

            int idx = (int)value;
            return (ColorList)Enum.ToObject(typeof(ColorList), (int)idx);
        }
    }

    /// <summary>
    /// Convert color enum to type Color
    /// </summary>
    [ValueConversion(typeof(ColorList), typeof(Color))]
    public class ColorListToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {            
            Color col = Color.FromRgb(255, 0, 0);

            if (value == null)
                return col;

            try
            {
                int index = (int)value;
                ColorList colEnum = (ColorList)Enum.ToObject(typeof(ColorList), (int)index);
                //ColorList colEnum = (ColorList)value;

                switch (colEnum)
                {
                    case ColorList.Red:
                        col = Color.FromRgb(255, 0, 0);
                        break;
                    case ColorList.Orange:
                        col = Colors.Orange;
                        break;
                    case ColorList.Yellow:
                        col = Color.FromRgb(255, 255, 0);
                        break;
                    case ColorList.Green:
                        col = Color.FromRgb(0, 255, 0);
                        break;
                    case ColorList.Blue:
                        col = Color.FromRgb(0, 0, 255);
                        break;
                    case ColorList.Indigo:
                        col = Color.FromRgb(64, 0, 192);
                        break;
                    case ColorList.Violet:
                        col = Color.FromRgb(192, 0, 255);
                        break;
                    case ColorList.Custom:
                        col = (Color)parameter;
                        break;
                    default:
                        break;
                }

                return col;
            }
            catch
            {
                return col;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return 0;
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
            else if (col == Colors.Orange)
                ret = "Orange";
            else if (col == Colors.Yellow)
                ret = "Yellow";
            else if (col == Colors.Green)
                ret = "Green";
            else if (col == Colors.Blue)
                ret = "Blue";
            else if (col == Colors.Indigo)
                ret = "Indigo";
            else if (col == Colors.Violet)
                ret = "Violet";            
            else
                ret = "Custom";

            return ret;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public enum MoleculeLocation { Bulk = 0, Boundary }

    public class DiffSchemeToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bResult = true;
            ConfigDiffScheme ds = value as ConfigDiffScheme;

            if (ds == null)
            {
                bResult = false;
            }

            return bResult;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ConfigDiffScheme ds = null;

            return ds;
        }
    }

    public class DivDeathDriverToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bResult = true;
            ConfigTransitionDriver dr = value as ConfigTransitionDriver;

            if (dr == null)
            {
                bResult = false;
            }

            return bResult;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ConfigTransitionDriver dr = null;

            return dr;
        }
    }

    public class DiffSchemeToDiffNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string name = "";
            ConfigDiffScheme scheme = value as ConfigDiffScheme;

            if (scheme != null)
            {
                name = scheme.Name;
            }

            return name;
        }
        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ConfigDiffScheme scheme = null;

            return scheme;
        }
    }

    /// <summary>
    /// Converter to go between enum values and boolean values for GUI checkbox
    /// </summary>
    [ValueConversion(typeof(MoleculeLocation), typeof(bool))]
    public class MoleculeLocationToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            
            int n = (int)value;
            if (n == 1)
                return true;
            else
                return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bVal = (bool)value;
            int idx = 0;
            if (bVal == true)
                idx = 1;
           
            return (MoleculeLocation)Enum.ToObject(typeof(MoleculeLocation), (int)idx);
        }
    }

    [ValueConversion(typeof(MoleculeLocation), typeof(bool))]
    public class EnumBooleanConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;

            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            return parameterValue.Equals(value);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            return Enum.Parse(targetType, parameterString);
        }
        #endregion
    }


    public enum BoundaryType { Zero_Flux = 0, Toroidal }
    
    [ValueConversion(typeof(bool), typeof(int))]
    public class BoolToIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return 0;

            return ((bool)value == true) ? 1 : 0;   
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return ((int)value == 1) ? true : false;
        }
    }




    public enum BoundaryFace {None=0, X, Y, Z }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(BoundaryFace), typeof(string))]
    public class BoundaryFaceToShortStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the BoundaryFace enum...
        private List<string> _boundary_face_strings = new List<string>()
                                {
                                    "None",
                                    "X",
                                    "Y",
                                    "Z"
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _boundary_face_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _boundary_face_strings.FindIndex(item => item == str);
            return (BoundaryFace)Enum.ToObject(typeof(BoundaryFace), (int)idx);
        }
    }

    /// <summary>
    /// Converter to go between enum values and boolean values for GUI checkbox
    /// </summary>
    [ValueConversion(typeof(BoundaryFace), typeof(string))]
    public class BoundaryFaceToVisStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return "Hidden";

            int n = (int)value;
            if (n != (int)BoundaryFace.None)
                return "Visible";
            else
                return "Hidden";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool bVal = (bool)value;
            int idx = 0;
            if (bVal == true)
                idx = 1;

            return (BoundaryFace)Enum.ToObject(typeof(BoundaryFace), (int)idx);
        }
    }

    /// <summary>
    /// base class for applicable config entities
    /// </summary>
    public abstract class ConfigEntity : EntityModelBase
    {
        public ConfigEntity()
        {
            Guid id = Guid.NewGuid();

            entity_guid = id.ToString();
            // initialize time_stamp
        }

        public string entity_guid { get; set; }
        // Time time_stamp { get; set; }
    }
    
    /// <summary>
    /// config molecule
    /// </summary>
    public class ConfigMolecule : ConfigEntity
    {
        private string mol_name;
        public string Name {
            get
            {
                return mol_name;
            }
            set
            {
                bool bFound = false;  // FindMolecule(value);
                if (bFound == true)
                {
                } 
                else 
                    mol_name = value;
            }
        }

        public double MolecularWeight { get; set; }
        public double EffectiveRadius { get; set; }
        public double DiffusionCoefficient { get; set; }
        
        public MoleculeLocation molecule_location { get; set; }

        public ConfigMolecule(string thisName, double thisMW, double thisEffRad, double thisDiffCoeff) : base()
        {
            Name = thisName;
            MolecularWeight = thisMW;
            EffectiveRadius = thisEffRad;
            DiffusionCoefficient = thisDiffCoeff;
            molecule_location = MoleculeLocation.Bulk;
        }

        public ConfigMolecule() : base()
        {
            Name = "Molecule_New001"; // +"_" + DateTime.Now.ToString("hhmmssffff");
            MolecularWeight = 1.0;
            EffectiveRadius = 5.0;
            DiffusionCoefficient = 2;
            molecule_location = MoleculeLocation.Bulk;
        }

        public string GenerateNewName(Protocol protocol, string ending)
        {
            string OriginalName = Name;

            if (OriginalName.Contains(ending))
            {
                int index = OriginalName.IndexOf(ending);
                OriginalName = OriginalName.Substring(0, index);
            }

            int nSuffix = 1;
            string suffix = ending + string.Format("{0:000}", nSuffix);
            string TempMolName = OriginalName + suffix;
            while (FindMoleculeByName(protocol, TempMolName) == true)
            {
                nSuffix++;
                suffix = ending + string.Format("{0:000}", nSuffix);
                TempMolName = OriginalName + suffix;
            }

            return TempMolName;
        }
       
        /// <summary>
        /// create a clone of a molecule
        /// </summary>
        /// <param name="protocol">null to create a literal copy</param>
        /// <returns></returns>
        public ConfigMolecule Clone(Protocol protocol)
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);
            ConfigMolecule newmol = JsonConvert.DeserializeObject<ConfigMolecule>(jsonSpec, Settings);

            if (protocol != null)
            {
                Guid id = Guid.NewGuid();

                newmol.entity_guid = id.ToString();
                newmol.Name = newmol.GenerateNewName(protocol, "_Copy");
            }
            
            return newmol;
        }        

        public static bool FindMoleculeByName(Protocol protocol, string tempMolName)
        {
            bool ret = false;
            foreach (ConfigMolecule mol in protocol.entity_repository.molecules)
            {
                if (mol.Name == tempMolName)
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }

        public void ValidateName(Protocol protocol)
        {
            bool found = false;
            string tempMolName = Name;
            foreach (ConfigMolecule mol in protocol.entity_repository.molecules)
            {
                if (mol.Name == tempMolName && mol.entity_guid != entity_guid)
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                Name = GenerateNewName(protocol, "_Copy");
            }
        }

    }


    //  -----------------------------------------------------------------------
    //  Differentiation Schemes
    //

    /// <summary>
    /// Any molecule can be a gene
    /// </summary>
    public class ConfigGene : ConfigEntity
    {
        public string Name { get; set; }
        public int CopyNumber { get; set; }
        public double ActivationLevel { get; set; }

        public ConfigGene(string name, int copynum, double actlevel) : base()
        {
            Name = name;
            CopyNumber = copynum;
            ActivationLevel = actlevel;
        }

        public ConfigGene Clone(Protocol protocol)
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);
            ConfigGene newgene = JsonConvert.DeserializeObject<ConfigGene>(jsonSpec, Settings);
            Guid id = Guid.NewGuid();

            newgene.entity_guid = id.ToString();
            newgene.Name = newgene.GenerateNewName(protocol, "_Copy");

            return newgene;
        }

        public string GenerateNewName(Protocol protocol, string ending)
        {
            string OriginalName = Name;

            if (OriginalName.Contains(ending))
            {
                int index = OriginalName.IndexOf(ending);
                OriginalName = OriginalName.Substring(0, index);
            }

            int nSuffix = 1;
            string suffix = ending + string.Format("{0:000}", nSuffix);
            string TempMolName = OriginalName + suffix;
            while (FindGeneByName(protocol, TempMolName) == true)
            {
                nSuffix++;
                suffix = ending + string.Format("{0:000}", nSuffix);
                TempMolName = OriginalName + suffix;
            }

            return TempMolName;
        }

        public static bool FindGeneByName(Protocol protocol, string geneName)
        {
            bool ret = false;
            foreach (ConfigGene gene in protocol.entity_repository.genes)
            {
                if (gene.Name == geneName)
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }

        public void ValidateName(Protocol protocol)
        {
            bool found = false;
            string tempGeneName = Name;
            foreach (ConfigGene gene in protocol.entity_repository.genes)
            {
                if (gene.Name == tempGeneName && gene.entity_guid != entity_guid)
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                Name = GenerateNewName(protocol, "_Copy");
            }
        }

    }
    
    public class ConfigTransitionDriverElement 
    {
        //public string driver_element_guid { get; set; }
        public double Alpha { get; set; }
        public double Beta { get; set; }
        public string driver_mol_guid_ref { get; set; }

        public int CurrentState { get; set; }
        public string CurrentStateName { get; set; }
        public int DestState { get; set; }
        public string DestStateName { get; set; }

        public ConfigTransitionDriverElement()
        {
            //Guid id = Guid.NewGuid();
            //driver_element_guid = id.ToString();
            driver_mol_guid_ref = "";
        }
    }

    public class ConfigTransitionDriverRow 
    {
        public ObservableCollection<ConfigTransitionDriverElement> elements { get; set; }

        public ConfigTransitionDriverRow()
        {
            elements = new ObservableCollection<ConfigTransitionDriverElement>();
        }
    }

    public class ConfigTransitionDriver : ConfigEntity
    {
        public string Name { get; set; }
        public int CurrentState { get; set; }
        public string StateName { get; set; }
        
        public ObservableCollection<ConfigTransitionDriverRow> DriverElements { get; set; }
        public ObservableCollection<string> states { get; set; }

        public ConfigTransitionDriver() : base()
        {
            DriverElements = new ObservableCollection<ConfigTransitionDriverRow>();
            states = new ObservableCollection<string>();
        }

        public ConfigTransitionDriver Clone(bool identical)
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);

            ConfigTransitionDriver new_ctd = JsonConvert.DeserializeObject<ConfigTransitionDriver>(jsonSpec, Settings);

            if (identical == false)
            {
                Guid id = Guid.NewGuid();

                new_ctd.entity_guid = id.ToString();
            }

            return new_ctd;
        }
    }

    //A Differentiation Scheme has a name and one list of states, each state with its genes and their boolean values
    //For example, one differentiation scheme's epigenetic map could look like this:
    //
    //    State/Gene     gCXCR5   gsDiv   gsDif1   gsDif2   gIg
    //    ------------------------------------------------------  
    //    Centroblast      0        1        1        0      0
    //    Centrocyte       1        0        0        1      0
    //    Plasmacyte       1        0        0        0      1
    //
    //Its regulators could look like this:
    //
    //    State/State     Centroblast   Centrocyte   Plasmacyte
    //    ------------------------------------------------------  
    //    Centroblast        none         gCXCR5       gIg       
    //    Centrocyte        gsDiv          none       gsDif2        
    //    Plasmacyte        gsDif1        gsDif2       none   
    
    public class ConfigDiffScheme : ConfigEntity, IEquatable<ConfigDiffScheme>
    {
        public string Name { get; set; }

        //For regulators
        public ConfigTransitionDriver Driver { get; set; }
   
        //Epigenetic map information
        //  Genes (guids) affected by differentiation states
        private ObservableCollection<string> _genes;
        public ObservableCollection<string> genes 
        {
            get
            {
                return _genes;
            }
            set
            {
                _genes = value;
                OnPropertyChanged("genes");
            }
        }

        //  Gene activations for each state
        //  The order of states (rows) should match the order in Drive.states
        public ObservableCollection<ConfigActivationRow> activationRows { get; set; }

        public ConfigDiffScheme() : base()
        {
            //genes.CollectionChanged += new NotifyCollectionChangedEventHandler(genes_CollectionChanged);
        }

        private void genes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("genes");
        }

        public void RemoveActivationRow(ConfigActivationRow row)
        {
            int index = activationRows.IndexOf(row);
            if (index > -1 && index < activationRows.Count)
            {
                activationRows.Remove(row);
                Driver.states.RemoveAt(index);
                Driver.DriverElements.RemoveAt(index);
            }
        }

        public void AddState(string sname)
        {
            //Add a row in Epigenetic Table
            ConfigActivationRow row = new ConfigActivationRow();
            for (int i = 0; i < genes.Count; i++)
            {
                row.activations.Add(1);
            }

            Driver.states.Add(sname);
            activationRows.Add(row);

            OnPropertyChanged("activationRows");

            //Add a row AND a column in Differentiation Table
            ConfigTransitionDriverRow trow; 

            //Add a column to existing rows
            for (int k = 0; k < Driver.states.Count-1; k++)
            {
                trow = Driver.DriverElements[k];
                ConfigTransitionDriverElement e = new ConfigTransitionDriverElement();
                e.Alpha = 0;
                e.Beta = 0;
                e.driver_mol_guid_ref = "";
                e.CurrentStateName = Driver.states[k];
                e.CurrentState = k;
                e.DestState = Driver.states.Count - 1;
                e.DestStateName = Driver.states[Driver.states.Count - 1];
                trow.elements.Add(e);
            }
            
            //Add a row
            trow = new ConfigTransitionDriverRow();
            for (int j = 0; j < Driver.states.Count; j++ )
            {
                ConfigTransitionDriverElement e = new ConfigTransitionDriverElement();
                e.Alpha = 0;
                e.Beta = 0;
                e.driver_mol_guid_ref = "";
                e.CurrentStateName = sname;
                e.CurrentState = Driver.states.Count - 1;
                e.DestState = j;
                e.DestStateName = Driver.states[j];
                trow.elements.Add(e);
            }

            Driver.DriverElements.Add(trow);

            OnPropertyChanged("Driver");
        }

        public ConfigDiffScheme Clone(bool identical)
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);

            ConfigDiffScheme new_cds = JsonConvert.DeserializeObject<ConfigDiffScheme>(jsonSpec, Settings);

            if (identical == false)
            {
                Guid id = Guid.NewGuid();

                new_cds.entity_guid = id.ToString();
            }

            return new_cds;
        }

        public bool Equals(ConfigDiffScheme other)
        {
            if (other == null)
            {
                return this.Name == "None";
            }
            return this.Name == other.Name;
        }
    }

    public class ConfigActivationRow : EntityModelBase
    {
        private ObservableCollection<double> _activations;
        public ObservableCollection<double> activations
        {
            get
            {
                return _activations;
            }
            set
            {
                _activations = value;
                OnPropertyChanged("activations");
            }
        }

        public ConfigActivationRow()
        {
            activations = new ObservableCollection<double>();
            activations.CollectionChanged += new NotifyCollectionChangedEventHandler(activations_CollectionChanged);
        }

        private void activations_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnPropertyChanged("activations");
        }
    }

    public enum ExtendedReport { NONE, LEAN, COMPLETE }; 

    public class ReportMP
    {
        public ExtendedReport mp_extended { get; set; }
        public string molpop_guid_ref { get; set; }

        public ReportMP()
        {
            mp_extended = ExtendedReport.NONE;
        }
    }

    public class ReportECM : ReportMP
    {
        public bool mean { get; set; }

        public ReportECM()
            : base()
        {
            mean = false;
        }
    }

    public enum ReportType { CELL_MP, ECM_MP };

    // Note: Neumann option may be added later.
    public enum MolBoundaryType { None = 0, Dirichlet, Neumann }
    public enum Boundary { None = 0, left = 1, right, bottom, top, back, front };
    public class BoundaryCondition
    {
        public MolBoundaryType boundaryType { get; set; }
        public Boundary boundary { get; set; }
        public double concVal { get; set; }

        public BoundaryCondition()
        {            
        }

        public BoundaryCondition(MolBoundaryType _boundaryType, Boundary _boundary)
        {
            boundaryType = _boundaryType;
            boundary = _boundary;
        }

        public BoundaryCondition(MolBoundaryType _boundaryType, Boundary _boundary, double val)
        {
            boundaryType = _boundaryType;
            boundary = _boundary;
            concVal = val;
        }
    }

    public class ConfigMolecularPopulation : EntityModelBase
    {
        public string molpop_guid { get; set; }
        public ConfigMolecule molecule { get; set; }
        private string _Name;
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                OnPropertyChanged("Name");
            }
        }       

        private ReportMP reportMP;
        public ReportMP report_mp
        {
            get { return reportMP; }
            set { reportMP = value; }
        }

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
        private bool _mp_render_on;
        public bool mp_render_on
        {
            get
            {
                return _mp_render_on;
            }
            set
            {
                _mp_render_on = value;
                OnPropertyChanged("mp_render_on");
            }
        }
                    
        public ConfigMolecularPopulation(ReportType rt)
        {
            Guid id = Guid.NewGuid();
            molpop_guid = id.ToString();

            if (rt == ReportType.CELL_MP)
            {
                reportMP = new ReportMP();
            }
            else if (rt == ReportType.ECM_MP)
            {
                reportMP = new ReportECM();
            }
            else
            {
                throw new Exception("Undefined report type in ConfigMolecularPopulation.");
            }
            reportMP.molpop_guid_ref = molpop_guid;
            
            mp_distribution = new MolPopHomogeneousLevel();
            mp_amplitude_keyframes = new ObservableCollection<TimeAmpPair>();
            mp_color = new System.Windows.Media.Color();
            mp_color = System.Windows.Media.Color.FromRgb(255, 255, 255);
            mp_render_blending_weight = 1.0;
            mp_render_on = true;
        }
    }

    public class ConfigCompartment : EntityModelBase
    {
        // private to Protocol; see comment in EntityRepository
        public ObservableCollection<ConfigMolecularPopulation> molpops { get; set; }

        [JsonIgnore]
        public Dictionary<string, ConfigMolecularPopulation> molpops_dict;
        [JsonIgnore]
        public Dictionary<string, ConfigMolecule> molecules_dict;  //key=molecule_guid(string), value=ConfigMolecule
        [JsonIgnore]
        public Dictionary<string, ConfigReaction> reactions_dict;

        private ObservableCollection<ConfigReaction> _reactions;
        public ObservableCollection<ConfigReaction> Reactions
        {
            get { return _reactions; }
            set
            {
                if (_reactions == value)
                    return;
                else
                {
                    _reactions = value;
                    OnPropertyChanged("Reactions");
                }
            }
        }

        public ObservableCollection<string> reaction_complexes_guid_ref { get; set; }

        public ConfigCompartment()
        {
            molpops = new ObservableCollection<ConfigMolecularPopulation>();
            _reactions = new ObservableCollection<ConfigReaction>();
            reaction_complexes_guid_ref = new ObservableCollection<string>();
            molpops_dict = new Dictionary<string, ConfigMolecularPopulation>();
            molecules_dict = new Dictionary<string, ConfigMolecule>();
            reactions_dict = new Dictionary<string, ConfigReaction>();

            molpops.CollectionChanged += new NotifyCollectionChangedEventHandler(molpops_CollectionChanged);
            _reactions.CollectionChanged += new NotifyCollectionChangedEventHandler(reactions_CollectionChanged);
        }

        private void reactions_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigReaction cr = nn as ConfigReaction;

                    if (reactions_dict.ContainsKey(cr.entity_guid) == false)
                    {
                        reactions_dict.Add(cr.entity_guid, cr);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigReaction cr = dd as ConfigReaction;

                    if (reactions_dict.ContainsKey(cr.entity_guid) == true)
                    {
                        reactions_dict.Remove(cr.entity_guid);
                    }
                }
            }
        }

        private void molpops_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigMolecularPopulation mp = nn as ConfigMolecularPopulation;

                    // add molpop into molpops_dict
                    if (molpops_dict.ContainsKey(mp.molpop_guid) == false)
                    {
                        molpops_dict.Add(mp.molpop_guid, mp);
                    }
                    if (molecules_dict.ContainsKey(mp.molecule.entity_guid) == false)
                    {
                        molecules_dict.Add(mp.molecule.entity_guid, mp.molecule);
                    }
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigMolecularPopulation mp = dd as ConfigMolecularPopulation;

                    // remove from molpops_dict
                    if (molpops_dict.ContainsKey(mp.molpop_guid) == true)
                    {
                        molpops_dict.Remove(mp.molpop_guid);
                    }
                    if (molecules_dict.ContainsKey(mp.molecule.entity_guid) == true)
                    {
                        molecules_dict.Remove(mp.molecule.entity_guid);
                    }
                }
            }

            OnPropertyChanged("molpops");            
        }

        /// <summary>
        /// get a reaction with a specified guid
        /// </summary>
        /// <param name="guid">guid for lookup</param>
        /// <returns>null if unsuccessful, the reaction otherwise</returns>
        public ConfigReaction GetReaction(string guid)
        {
            if (reactions_dict.ContainsKey(guid) == true)
            {
                return reactions_dict[guid];
            }
            return null;
        }

        //Return true if this compartment has a molecular population with given molecule
        public bool HasMolecule(ConfigMolecule mol)
        {
            if (molecules_dict.ContainsKey(mol.entity_guid))
            {
                return true;
            }
            return false;
        }

        //Return true if this compartment has a molecular population with given molecule guid
        public bool HasMolecule(string molguid)
        {
            if (molecules_dict.ContainsKey(molguid))
            {
                return true;
            }
            return false;
        }

        //Return true if this compartment has all the molecules in the given list of molecule guids
        public bool HasMolecules(ObservableCollection<string> mol_guid_refs)
        {
            bool res = true;
            foreach (string molguid in mol_guid_refs)
            {
                if (!HasMolecule(molguid))
                {
                    res = false;
                    break;
                }
            }
            return res;
        }

        //Remove a molecular population given a molecule guid
        public void RemoveMolecularPopulation(string molecule_guid)
        {
            string molpop_guid = "";

            ConfigMolecularPopulation delMolPop = null;
            foreach (ConfigMolecularPopulation cmp in molpops)
            {
                if (molecule_guid == cmp.molecule.entity_guid)
                {
                    molpop_guid = cmp.molpop_guid;
                    delMolPop = cmp;
                    break;
                }
            }

            if (molpop_guid.Length > 0)
            {
                molpops.Remove(delMolPop);
            }
        }
    }

    public enum ReactionType
    {
        Association = 0, Dissociation, Annihilation, Dimerization, DimerDissociation,
        Transformation, AutocatalyticTransformation, CatalyzedAnnihilation,
        CatalyzedAssociation, CatalyzedCreation, CatalyzedDimerization, CatalyzedDimerDissociation,
        CatalyzedTransformation, CatalyzedDissociation, CatalyzedBoundaryActivation, BoundaryAssociation, 
        BoundaryDissociation, Generalized, BoundaryTransportFrom, BoundaryTransportTo,
        Transcription
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
                                    "CatalyzedBoundaryActivation",
                                    "BoundaryAssociation",
                                    "BoundaryDissociation",
                                    "Generalized",
                                    "BoundaryTransportTo",
                                    "BoundaryTransportFrom",
                                    "Transcription"
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

    public class ConfigReaction : ConfigEntity
    {
        public ConfigReaction() : base()
        {
            rate_const = 0;
            
            reactants_molecule_guid_ref = new ObservableCollection<string>();
            products_molecule_guid_ref = new ObservableCollection<string>();
            modifiers_molecule_guid_ref = new ObservableCollection<string>();
        }

        public ConfigReaction(ConfigReaction reac) : base()
        {
            reaction_template_guid_ref = reac.reaction_template_guid_ref;

            rate_const = reac.rate_const;

            reactants_molecule_guid_ref = new ObservableCollection<string>();
            products_molecule_guid_ref = new ObservableCollection<string>();
            modifiers_molecule_guid_ref = new ObservableCollection<string>();

            reactants_molecule_guid_ref = reac.reactants_molecule_guid_ref;
            products_molecule_guid_ref = reac.products_molecule_guid_ref;
            modifiers_molecule_guid_ref = reac.modifiers_molecule_guid_ref;
        }

        /// <summary>
        /// create a clone of a reaction
        /// </summary>
        /// <param name="identical">true to create a literal copy</param>
        /// <returns></returns>
        public ConfigReaction Clone(bool identical)
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);
            ConfigReaction newreaction = JsonConvert.DeserializeObject<ConfigReaction>(jsonSpec, Settings);

            if (identical == false)
            {
                Guid id = Guid.NewGuid();

                newreaction.entity_guid = id.ToString();
            }
            return newreaction;
        }

        public void GetTotalReactionString(EntityRepository repos)
        {
            string s = "";
            int i = 0;

            // Reactants
            foreach (string mol_guid_ref in reactants_molecule_guid_ref)
            {
                //stoichiometry
                int n = repos.reaction_templates_dict[reaction_template_guid_ref].reactants_stoichiometric_const[i];
                i++;
                if (n > 1)
                    s += n;
                s += repos.molecules_dict[mol_guid_ref].Name;
                s += " + ";
            }

            i = 0;
            // Modifiers
            foreach (string mol_guid_ref in modifiers_molecule_guid_ref)
            {
                //stoichiometry
                int n = repos.reaction_templates_dict[reaction_template_guid_ref].modifiers_stoichiometric_const[i];
                i++;
                if (n > 1)
                    s += n;

                if (repos.genes_dict.ContainsKey(mol_guid_ref))
                {
                    s += repos.genes_dict[modifiers_molecule_guid_ref[0]].Name;
                }
                else
                {
                    s += repos.molecules_dict[mol_guid_ref].Name;
                }
                s += " + ";
            }

            char[] trimChars = { ' ', '+' };
            s = s.Trim(trimChars);

            s = s + " -> ";

            i = 0;
            // Products
            foreach (string mol_guid_ref in products_molecule_guid_ref)
            {
                //stoichiometry
                int n = repos.reaction_templates_dict[reaction_template_guid_ref].products_stoichiometric_const[i];
                i++;
                if (n > 1)
                    s += n;

                s += repos.molecules_dict[mol_guid_ref].Name;
                s += " + ";
            }
            i = 0;
            // Modifiers
            foreach (string mol_guid_ref in modifiers_molecule_guid_ref)
            {
                //stoichiometry
                int n = repos.reaction_templates_dict[reaction_template_guid_ref].modifiers_stoichiometric_const[i];
                i++;
                if (n > 1)
                    s += n;

                if (repos.genes_dict.ContainsKey(mol_guid_ref))
                {
                    s += repos.genes_dict[modifiers_molecule_guid_ref[0]].Name;
                }
                else
                {
                    s += repos.molecules_dict[mol_guid_ref].Name;
                }
                s += " + ";
            }

            s = s.Trim(trimChars);
            TotalReactionString = s;
        }

        public bool HasMolecule(string molguid)
        {
            if (reactants_molecule_guid_ref.Contains(molguid) || products_molecule_guid_ref.Contains(molguid) || modifiers_molecule_guid_ref.Contains(molguid))
            {
                return true;
            }

            return false;
        }

        public bool HasBoundaryMolecule(EntityRepository repos)
        {
            foreach (string molguid in reactants_molecule_guid_ref)
            {
                if (repos.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                    return true;
            }
            foreach (string molguid in products_molecule_guid_ref)
            {
                if (repos.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                    return true;
            }
            foreach (string molguid in modifiers_molecule_guid_ref)
            {
                if (!repos.genes_dict.ContainsKey(molguid))
                {
                    if (repos.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                        return true;
                }
            }

            return false;
        }
        public string reaction_template_guid_ref { get; set; }

        private double _rate_const;
        public double rate_const 
        { 
            get
            {
                return _rate_const;
            }
            set
            { 
                _rate_const = value;
                OnPropertyChanged("rate_const");
            } 
        }

        // hold the molecule_guid_refs of the {reactant|product|modifier} molpops
        public ObservableCollection<string> reactants_molecule_guid_ref;
        public ObservableCollection<string> products_molecule_guid_ref;
        public ObservableCollection<string> modifiers_molecule_guid_ref;

        public string TotalReactionString { get; set; }

    }

    public class ConfigReactionTemplate : ConfigEntity
    {
        public string name;
        // stoichiometric constants
        public ObservableCollection<int> reactants_stoichiometric_const;
        public ObservableCollection<int> products_stoichiometric_const;
        public ObservableCollection<int> modifiers_stoichiometric_const;      
        //reaction type
        public ReactionType reac_type { get; set; }
        // True if the reaction involves bulk and boundary molecules. Default is false.
        public bool isBoundary;

        public ConfigReactionTemplate() : base()
        {
            reactants_stoichiometric_const = new ObservableCollection<int>();
            products_stoichiometric_const = new ObservableCollection<int>();
            modifiers_stoichiometric_const = new ObservableCollection<int>();
            isBoundary = false;
        }
    }

    public class ConfigReactionGuidRatePair : ConfigEntity
    {
        public ConfigReactionGuidRatePair() : base()
        {
        }

        private double originalRate;
        public double OriginalRate 
        {
            get
            {
                return originalRate;
            }
            set
            {
                originalRate = value;
                OnPropertyChanged("OriginalRate");
            }
        }
        private double reactionComplexRate;
        public double ReactionComplexRate
        {
            get
            {
                return reactionComplexRate;
            }
            set
            {
                reactionComplexRate = value;
                OnPropertyChanged("ReactionComplexRate");
            }
        }
    }

    public class ConfigReactionComplex : ConfigEntity
    {
        public string Name { get; set; }

        private ObservableCollection<string> _reactions_guid_ref;
        public ObservableCollection<string> reactions_guid_ref 
        {
            get
            {
                return _reactions_guid_ref;
            }
            set
            {
                _reactions_guid_ref = value;
                OnPropertyChanged("reactions_guid_ref");
            }
        }
        public ObservableCollection<ConfigMolecularPopulation> molpops { get; set; }
        public ObservableCollection<ConfigGene> genes { get; set; }        

        public ObservableCollection<ConfigReactionGuidRatePair> ReactionRates { get; set; } 

        public ConfigReactionComplex() : base()
        {
            Name = "NewRC";
            reactions_guid_ref = new ObservableCollection<string>();
            molpops = new ObservableCollection<ConfigMolecularPopulation>();
            genes = new ObservableCollection<ConfigGene>();
        }

        public ConfigReactionComplex(string name) : base()
        {
            Name = name;
            reactions_guid_ref = new ObservableCollection<string>();
            molpops = new ObservableCollection<ConfigMolecularPopulation>();
            genes = new ObservableCollection<ConfigGene>();
            ReactionRates = new ObservableCollection<ConfigReactionGuidRatePair>();
        }
        
        public ConfigReactionComplex Clone()
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);

            ConfigReactionComplex newrc = JsonConvert.DeserializeObject<ConfigReactionComplex>(jsonSpec, Settings);
            Guid id = Guid.NewGuid();

            newrc.entity_guid = id.ToString();
            newrc.Name = "NewRC";

            return newrc;
        }

        private bool HasMolecule(string guid) 
        {
            foreach (ConfigMolecularPopulation molpop in molpops)
            {
                if (molpop.molecule.entity_guid == guid)
                {
                    return true;
                }
            }

            return false;
        }
        private bool HasGene(string guid)
        {
            foreach (ConfigGene gene in genes)
            {
                if (gene.entity_guid == guid)
                {
                    return true;
                }
            }

            return false;
        }

        private void AddMolsToReaction(EntityRepository er, ConfigReaction reac, ObservableCollection<string> mols)
        {
            foreach (string molguid in mols)
            {
                if (er.genes_dict.ContainsKey(molguid))
                {
                    if (HasGene(molguid) == false)
                    {
                        ConfigGene configGene = new ConfigGene(er.genes_dict[molguid].Name, er.genes_dict[molguid].CopyNumber, er.genes_dict[molguid].ActivationLevel);
                        configGene.entity_guid = er.genes_dict[molguid].entity_guid;
                        genes.Add(configGene);
                    }
                }
                else
                {
                    ConfigMolecule configMolecule = er.molecules_dict[molguid];
                    if (configMolecule != null)
                    {
                        ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.CELL_MP);

                        configMolPop.molecule.entity_guid = configMolecule.entity_guid;
                        configMolPop.Name = configMolecule.Name;
                        configMolPop.mp_dist_name = "Uniform";
                        configMolPop.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                        configMolPop.mp_render_blending_weight = 2.0;
                        configMolPop.mp_render_on = true;

                        MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();

                        hl.concentration = 1;
                        configMolPop.mp_distribution = hl;
                        if (HasMolecule(molguid) == false)
                        {
                            molpops.Add(configMolPop);
                        }
                    }
                }
            }
        }

        public void RefreshMolPops(EntityRepository er)
        {
            //Clear mol pop list and regenerate it
            molpops.Clear();
            foreach (string reacguid in reactions_guid_ref)
            {
                ConfigReaction reac = er.reactions_dict[reacguid];
                AddMolsToReaction(er, reac, reac.reactants_molecule_guid_ref);
                AddMolsToReaction(er, reac, reac.products_molecule_guid_ref);
                AddMolsToReaction(er, reac, reac.modifiers_molecule_guid_ref);
            }
        }
    }

    public class ConfigCell : ConfigEntity
    {
        public ConfigCell() : base()
        {
            CellName = "Default Cell";
            CellRadius = 5.0;
            TransductionConstant = 0.0;
            DragCoefficient = 1.0;

            membrane = new ConfigCompartment();
            cytosol = new ConfigCompartment();
            locomotor_mol_guid_ref = "";

            // behaviors
            genes_guid_ref = new ObservableCollection<string>();
        }

        public ConfigCell Clone(bool identical)
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);
            ConfigCell newcell = JsonConvert.DeserializeObject<ConfigCell>(jsonSpec, Settings);

            if (identical == false)
            {
                Guid id = Guid.NewGuid();

                newcell.entity_guid = id.ToString();
            }
            return newcell;
        }

        private string cellName;
        public string CellName
        {
            get
            {
                return cellName;
            }

            set
            {
                cellName = value;
                OnPropertyChanged("CellName");
            }
        }

        private double cellRadius;
        public double CellRadius 
        { 
            get
            {
                return cellRadius;
            }
            set
            {
                cellRadius = value ;
                OnPropertyChanged("CellRadius");
            }
        }

        private string _locomotor_mol_guid_ref;
        public string locomotor_mol_guid_ref
        {
            get
            {
                return _locomotor_mol_guid_ref;
            }
            set
            {
                if (value == null)
                {
                    _locomotor_mol_guid_ref = "";
                }                
                else
                {
                    _locomotor_mol_guid_ref = value;
                }
            }
        }

        private double transductionConstant;
        public double TransductionConstant
        {
            get
            {
                return transductionConstant;
            }
            set
            {
                transductionConstant = value;
                OnPropertyChanged("TransductionConstant");
            }
        }

        private double dragCoefficient;
        public double DragCoefficient
        {
            get
            {
                return dragCoefficient;
            }
            set
            {
                dragCoefficient = value;
                OnPropertyChanged("DragCoefficient");
            }
        }

        public ConfigCompartment membrane { get; set; }
        public ConfigCompartment cytosol { get; set; }
        
        //FOR NOW, THIS IS HERE. MAYBE THER IS A BETTER PLACE FOR IT
        public ObservableCollection<string> genes_guid_ref { get; set; }

        private ConfigDiffScheme _diff_scheme;
        public ConfigDiffScheme diff_scheme
        {
            get
            {
                return _diff_scheme;
            }

            set
            {
                _diff_scheme = value;
                OnPropertyChanged("diff_scheme");
            }
        }

        // ConfigTransitionDriver contains ConfigTransitionDriverElement
        // ConfigTransitionDriverElement contains information about 
        //      signaling molecule that drives cell death and alphas and betas
        private ConfigTransitionDriver _death_driver;
        public ConfigTransitionDriver death_driver
        {
            get
            {
                return _death_driver;
            }

            set
            {
                _death_driver = value;
                OnPropertyChanged("death_driver");
            }
        }

        private ConfigTransitionDriver _div_driver;
        public ConfigTransitionDriver div_driver 
        {
            get
            {
                return _div_driver;
            }

            set
            {
                _div_driver = value;
                OnPropertyChanged("div_driver");
            }
        }

        public int CurrentDeathState;
        public int CurrentDivState;

        //Return true if this compartment has a molecular population with given molecule
        public bool HasGene(string gene_guid)
        {
            return genes_guid_ref.Contains(gene_guid);
        }

        //Return true if this cell has all the genes in the given list of gene guids
        public bool HasGenes(ObservableCollection<string> gene_guids)
        {
            bool res = true;
            foreach (string guid in gene_guids)
            {
                if (!HasGene(guid))
                {
                    res = false;
                    break;
                }
            }
            return res;
        }

        /// <summary>
        /// This method looks for duplicate names with newly created (or copied) cell
        /// If it is a duplicate, a suffix like "_Copy" is added
        /// </summary>
        /// <param name="sc"></param>
        public void ValidateName(Protocol protocol)
        {
            bool found = false;
            string newCellName = CellName;
            foreach (ConfigCell cell in protocol.entity_repository.cells)
            {
                if (cell.CellName == newCellName && cell.entity_guid != entity_guid)
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                CellName = GenerateNewName(protocol, "_Copy");
            }
        }

        public string GenerateNewName(Protocol protocol, string ending)
        {
            string OriginalName = CellName;

            if (OriginalName.Contains(ending))
            {
                int index = OriginalName.IndexOf(ending);
                OriginalName = OriginalName.Substring(0, index);
            }

            int nSuffix = 1;
            string suffix = ending + string.Format("{0:000}", nSuffix);
            string NewCellName = OriginalName + suffix;
            while (FindCellByName(protocol, NewCellName) == true)
            {
                nSuffix++;
                suffix = ending + string.Format("{0:000}", nSuffix);
                NewCellName = OriginalName + suffix;
            }

            return NewCellName;
        }

        public static bool FindCellByName(Protocol protocol, string cellName)
        {
            bool ret = false;
            foreach (ConfigCell cell in protocol.entity_repository.cells)
            {
                if (cell.CellName == cellName)
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }
    }

    public enum CellPopDistributionType { Specific, Uniform, Gaussian }

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
                "Uniform",
                "Gaussian"
            };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                int n = (int)value;
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

    public interface ProbDistribution3D
    {
        /// <summary>
        /// Return x,y,z coordinates for the next cell using the appropriate probability density distribution.
        /// </summary>
        /// <returns>double[3] {x,y,z}</returns>
        double[] nextPosition();
        /// <summary>
        /// Update extents of ECS and other distribution-specific tasks.
        /// </summary>
        /// <param name="newExtents"></param>
        void Resize(double[] newExtents);
    }

    /// <summary>
    /// Contains information for positioning cells in the ECS according to specfied distributions.
    /// </summary>
    public abstract class CellPopDistribution : EntityModelBase, ProbDistribution3D
    {
        private CellPopDistributionType _DistType;
        public CellPopDistributionType DistType
        {
            get { return _DistType; }
            set
            {
                if (_DistType == value)
                    return;
                else
                {
                    _DistType = value;
                    OnPropertyChanged("DistType");
                }
            }
        }
        private ObservableCollection<CellState> cellStates;
        public ObservableCollection<CellState> CellStates
        {
            get { return cellStates; }
            set
            {
                cellStates = value;
                OnPropertyChanged("CellStates");
            }
        }

        // We need to update (reduce) cellPop.number if we reach the maximum tries 
        // for cell placement before all the cells are placed
        public CellPopulation cellPop;

        // Limits for placing cells
        private double[] extents;
        public double[] Extents 
        { 
            get { return extents; }
            set { extents=value; }
        }
        // Separation distance from boundaries
        private double wallDis;
        // Minimum separation (squared) for cells
        private double minDisSquared;
        public double MinDisSquared 
        { 
            get { return minDisSquared; }
            set 
            { 
                minDisSquared = value;
                //// Generally, minDisSquared is the square of the cell diameter.
                //// Then, center of cell can be one radius from the ECS boundary.
                // wallDis = Math.Sqrt(minDisSquared) / 2.0;
                wallDis = 0;
            }
        }

        public CellPopDistribution(double[] _extents, double _minDisSquared, CellPopulation _cellPop)
        {
            cellStates = new ObservableCollection<CellState>();
            extents = (double[])_extents.Clone();
            MinDisSquared = _minDisSquared;

            // null case when deserializing Json
            // correct CellPopulation pointer added in Protocoluration.InitCellPopulationIDCellPopulationDict
            if (_cellPop != null)
            {
                cellPop = _cellPop;
            }
        }

        /// <summary>
        /// Check that the new cell position is within the specified bounds.
        /// </summary>
        /// <param name="pos">the position of the next cell</param>
        /// <returns></returns>
        protected bool inBounds(double[] pos)
        {
            if ((pos[0] < wallDis || pos[0] > Extents[0] - wallDis) ||
                (pos[1] < wallDis || pos[1] > Extents[1] - wallDis) ||
                (pos[2] < wallDis || pos[2] > Extents[2] - wallDis))
            {
                return false;
            } 
            return true;
        }
        /// <summary>
        /// Return true if the position of the new cell doesn't overlap with existing cell positions.
        /// NOTE: We should be checking for overlap with all cell populations. Not sure how to do this, yet.
        /// </summary>
        /// <param name="pos">the position of the next cell</param>
        /// <returns></returns>
        protected bool noOverlap(double[] pos)
        {
            double disSquared = 0;
            foreach (CellState cellState in this.cellStates)
            {
                disSquared = (cellState.X - pos[0]) * (cellState.X - pos[0]) 
                           + (cellState.Y - pos[1]) * (cellState.Y - pos[1])
                           + (cellState.Z - pos[2]) * (cellState.Z - pos[2]);
                if (disSquared < minDisSquared)
                {
                    return false;
                }
            }
            return true;
        }
 
        /// <summary>
        /// Remove n cells from the end of the list
        /// </summary>
        /// <param name="num"></param>
        public void RemoveCells(int num)
        {
            int i = 0;
            while ( (i < num) && (cellStates.Count > 0))
            {
                cellStates.RemoveAt(cellStates.Count - 1);
                i++;
            }
        }

        /// <summary>
        /// Check that position is in-bounds and doesn't overlap.
        /// If so, add to cell location list.
        /// </summary>
        /// <param name="pos">x,y,z coordinates</param>
        /// <returns></returns>
        public bool AddByPosition(double[] pos)
        {
            if (inBounds(pos) && noOverlap(pos))
            {
                cellStates.Add(new CellState(pos[0], pos[1], pos[2]));
                return true;
            }
            return false;
        }

        /// <summary>
        /// Add n cells using the appropriate probability density distribution.
        /// </summary>
        /// <param name="n"></param>
        public void AddByDistr(int n)
        {
            // NOTE: The maxTry settings has been arbitrarily chosen and may need to be adjusted.
            int maxTry = 1000;
            int i = 0;

            int tries = 0; 
            while (i < n)
            {

                if (AddByPosition(nextPosition()))
                {
                    i++;
                    tries = 0;
                }
                else
                {
                    // Avoid infinite loops. Excessive iterations may indicate the cells density is too high.
                    tries++;
                    if (tries > maxTry)
                    {
                        if (CellStates.Count < 1)
                        {
                            AddByPosition( new double[] {Extents[0] / 2.0, Extents[1] / 2.0, Extents[2] / 2.0 } );
                        }
                        System.Windows.MessageBox.Show("Exceeded max iterations for cell placement. Cell density is too high. Limiting cell count to " + cellStates.Count + ".");
                        OnPropertyChanged("CellStates");
                        cellPop.number = CellStates.Count;
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Return x,y,z coordinates for the next cell using the appropriate probability density distribution.
        /// </summary>
        /// <returns>double[3] {x,y,z}</returns>
        public abstract double[] nextPosition();

        // Clear the current cell states and repopulate
        // Needed if Gaussian/Box parameters change.
        public void Reset()
        {
            int number = CellStates.Count;
            CellStates.Clear();
            AddByDistr(number);
        }
 
        // <summary>
        // Triggered by OnUpdate of ConfigEnvironment
        // Update distributions accordingly.
        // </summary>
        // <param name="newextents"></param>
        public abstract void Resize(double[] newExtents);

        /// <summary>
        /// Check that all cells are in-bounds. 
        /// </summary>
        public void CheckPositions()
        {
            double[] pos;
            int number = CellStates.Count;

            // Remove out-of-bounds cells
            for (int i = CellStates.Count - 1; i >= 0; i--)
            {
                pos = new double[3] { CellStates[i].X, CellStates[i].Y, CellStates[i].Z };
                if (!inBounds(pos))
                {
                    cellStates.RemoveAt(i);
                }
            }

            // Replace removed cells
            int cellsToAdd = number - CellStates.Count;
            if (cellsToAdd > 0)
            {
                AddByDistr(cellsToAdd);
            }
        }
    }

    /// <summary>
    /// Uses uniform probability density for initial placement of cells. 
    /// NOTE: It may make more sense to have this be the (non-abstract) base class.
    /// </summary>
    public class CellPopSpecific : CellPopDistribution
    {
        public CellPopSpecific(double[] extents, double minDisSquared, CellPopulation _cellPop)
            : base(extents, minDisSquared, _cellPop)
        {
            DistType = CellPopDistributionType.Specific;
            MathNet.Numerics.RandomSources.RandomSource ran = new MathNet.Numerics.RandomSources.MersenneTwisterRandomSource();

            if (_cellPop != null)
            {
                AddByDistr(cellPop.number);
            }
            else
            {
                // json deserialization puts us here
                AddByDistr(1);
            }
            OnPropertyChanged("CellStates");
        }

        public override double[] nextPosition()
        {
            return new double[3] {  Extents[0] * Rand.UniformDist.NextDouble(), 
                                    Extents[1] * Rand.UniformDist.NextDouble(), 
                                    Extents[2] * Rand.UniformDist.NextDouble() };
        }

        public override void Resize(double[] newExtents)
        {
            // Check whether extents changed
            if ((Extents[0] != newExtents[0]) || (Extents[1] != newExtents[1]) || (Extents[2] != newExtents[2]))
            {
                Extents = (double[])newExtents.Clone();
                CheckPositions();
            }
        }
    }

    /// <summary>
    /// Placement of cells via uniform probability density.
    /// </summary>
    public class CellPopUniform : CellPopDistribution
    {
        public CellPopUniform(double[] extents, double minDisSquared, CellPopulation _cellPop)
            : base(extents, minDisSquared, _cellPop)
        {
            DistType = CellPopDistributionType.Uniform;
            if (_cellPop != null)
            {
                AddByDistr(_cellPop.number);
            }
            else
            {
                // json deserialization puts us here
                AddByDistr(1);
            }
            OnPropertyChanged("CellStates");
        }

        public override double[] nextPosition()
        {
            return new double[3] {  Extents[0] * Rand.UniformDist.NextDouble(), 
                                    Extents[1] * Rand.UniformDist.NextDouble(), 
                                    Extents[2] * Rand.UniformDist.NextDouble() };
        }

        public override void Resize(double[] newExtents)
        {
            // Check whether extents changed
            if ((Extents[0] != newExtents[0]) || (Extents[1] != newExtents[1]) || (Extents[2] != newExtents[2]))
            {
                Extents = (double[])newExtents.Clone();
                Reset();
            }
        }
    }

    /// <summary>
    /// Placement of cells via normal probability density.
    /// Cell placement updates as box center and width changes.
    /// </summary>
    public class CellPopGaussian : CellPopDistribution
    {
        private string _gauss_spec_guid_ref;
        public string gauss_spec_guid_ref
        {
            get { return _gauss_spec_guid_ref; }
            set
            {
                if (_gauss_spec_guid_ref == value)
                    return;
                else
                {
                    _gauss_spec_guid_ref = value;
                }
            }
        }
        private string _box_guid;
        public string box_guid
        {
            get { return _box_guid; }
            set
            {
                if (_box_guid == value)
                    return;
                else
                {
                    _box_guid = value;
                }
            }
        }
        // The standard deviations of the distribution
        private double[] sigma;

        // transformation matrix for converting from absolute (simulation) 
        // to local (box) coordinates
        private double[][] ATL = new double[][] {   new double[]{1.0, 0.0, 0.0, 0.0},
                                                    new double[]{0.0, 1.0, 0.0, 0.0},
                                                    new double[]{0.0, 0.0, 1.0, 0.0},
                                                    new double[]{0.0, 0.0, 0.0, 1.0} };

        public CellPopGaussian(double[] extents, double minDisSquared, BoxSpecification _box, CellPopulation _cellPop)
            : base(extents, minDisSquared, _cellPop)  
        {
            DistType = CellPopDistributionType.Gaussian;
            gauss_spec_guid_ref = "";

            if (_box != null)
            {
                _box_guid = _box.box_guid;
                _box.PropertyChanged += new PropertyChangedEventHandler(CellPopGaussChanged);
                sigma = new double[3] { _box.x_scale / 2, _box.y_scale / 2, _box.z_scale / 2 };
                setRotationMatrix(_box);
            }
            else
            {
                // We get here when deserializing from json
                sigma = new double[3] { extents[0] / 4, extents[1] / 4, extents[2] / 4};
            }
            if (_cellPop != null)
            {
                AddByDistr(_cellPop.number);
            }
            else
            {
                // json deserialization puts us here
                AddByDistr(1);
            }
 
            OnPropertyChanged("CellStates");
        }

        public override void Resize(double[] newExtents)
        {
            // Check whether extents changed
            if ((Extents[0] != newExtents[0]) || (Extents[1] != newExtents[1]) || (Extents[2] != newExtents[2]))
            {
                Extents = (double[])newExtents.Clone();
                Reset();
            }
        }

        public void ParamReset(BoxSpecification box)
        {
            sigma = new double[3] { box.x_scale / 2, box.y_scale / 2, box.z_scale / 2 };
            setRotationMatrix(box);
        }

        private void setRotationMatrix(BoxSpecification box)
        {
            // 4x4 transformation matrix comprising:
            //      normalized 3x3 rotation matrix
            //      translation information
            for (int i = 0; i < 3; i++)
            {
                ATL[i][0] = box.transform_matrix[i][0] / box.getScale((byte)0);
                ATL[i][1] = box.transform_matrix[i][1] / box.getScale((byte)1);
                ATL[i][2] = box.transform_matrix[i][2] / box.getScale((byte)2);
                ATL[i][3] = box.transform_matrix[i][3];
            }
        }

        public void CellPopGaussChanged(object sender, PropertyChangedEventArgs e)
        {
            BoxSpecification box = (BoxSpecification)sender;
            ParamReset(box);
            Reset();
        }

        public override double[] nextPosition()
        {
            // Draw three random coordinates from normal distributions centered at the origin of the simulation coordinate system.
            // normal distribution centered at zero with specified sigmas
            double[] pos = new double[3] {  sigma[0] * Rand.NormalDist.NextDouble(), 
                                            sigma[1] * Rand.NormalDist.NextDouble(), 
                                            sigma[2] * Rand.NormalDist.NextDouble() };

            // The new position rotated and translated  with the box coordinate system
            double[] posRotated = new double[3];
            // rotates and translates center of distribution
            for (int i = 0; i < 3; i++)
            {
                posRotated[i] = pos[0] * ATL[i][0] +
                                pos[1] * ATL[i][1] +
                                pos[2] * ATL[i][2] +
                                ATL[i][3];
            }
            return posRotated;
        }
    }

    public class CellState
    {

        //for cell's sate X V F location
        [JsonProperty]
        internal double[] ConfigState { get; set; }


        [JsonIgnore]
        public double X 
        {
            get { return Math.Round(ConfigState[0], 2) ; }
            set { ConfigState[0] = value; }
        }

        [JsonIgnore]
        public double Y 
        {
            get { return Math.Round(ConfigState[1], 2); }
            set { ConfigState[1] = value; }
        }

        [JsonIgnore]
        public double Z 
        {
            get { return Math.Round(ConfigState[2], 2); }
            set { ConfigState[2] = value; }
        }

        public CellState()
        {
            ConfigState = new double[] { 1, 1, 1, 0, 0, 0, 0, 0, 0 };
        }
        public CellState(double x, double y, double z)
        {
            ConfigState = new double[]{ x, y, z, 0,0,0,0,0,0 };
        }


        //map concentration info into molpop info.
        public Dictionary<string, double[]> configMolPop = new Dictionary<string, double[]>();
        public void setState(CellSpatialState state)
        {
            List<double> tmp = new List<double>(CellSpatialState.Dim);
            tmp.AddRange(state.X);
            tmp.AddRange(state.V);
            tmp.AddRange(state.F);
            this.ConfigState = tmp.ToArray();
        }

        public void addMolPopulation(string key, MolecularPopulation mp)
        {
            configMolPop.Add(key, mp.CopyArray());
        }
    }

    public class ReportXVF
    {
        public bool position { get; set; }
        public bool velocity { get; set; }
        public bool force { get; set; }

        public ReportXVF()
        {
            position = false;
            velocity = false;
            force = false;
        }
    }

    public class CellPopulation : EntityModelBase
    {
        //public string cell_guid_ref { get; set; }
        public ConfigCell Cell { get; set; }
        private string _Name;
        public string cellpopulation_name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                OnPropertyChanged("cellpopulation_name");
            }
        }
        public string cellpopulation_guid { get; set; }
        public int cellpopulation_id { get; set; }

        private ReportXVF reportXVF;
        public ReportXVF report_xvf
        {
            get { return reportXVF; }
            set
            {
                reportXVF = value;
            }
        }

        private ObservableCollection<ReportECM> ecmProbe;
        public ObservableCollection<ReportECM> ecm_probe
        {
            get { return ecmProbe; }
        }

        [JsonIgnore]
        public Dictionary<string, ReportECM> ecm_probe_dict;

        private int _number;
        public int number
        {
            get { return _number; }
            set
            {
                if (_number == value)
                    return;
                else
                {
                    _number = value;
                    OnPropertyChanged("number");
                }
            }
        }

        public bool cellpopulation_render_on { get; set; }
        private Color _cellpopulation_color;   //this is used if cellpopulation_predef_color is set to ColorList.Custom
        public Color cellpopulation_color
        {
            get
            {
                return _cellpopulation_color;
            }
            set
            {
                _cellpopulation_color = value;
                OnPropertyChanged("cellpopulation_color");
            }  
        }
        private ColorList _cellpopulation_predef_color;                         //these are predefined colors plus a "Custom" option
        public ColorList cellpopulation_predef_color 
        {
            get
            {
                return _cellpopulation_predef_color;
            }
            set
            {
                _cellpopulation_predef_color = value;
                ColorListToColorConverter conv = new ColorListToColorConverter();
                Color cc = (Color)conv.Convert(value, typeof(Color), cellpopulation_color, System.Globalization.CultureInfo.CurrentCulture);
                cellpopulation_color = cc;
                //OnPropertyChanged("cellpopulation_color");
            }
        }
        
        private CellPopDistribution _cellPopDist;
        public CellPopDistribution cellPopDist
        {
            get { return _cellPopDist; }
            set
            {
                if (_cellPopDist == value)
                    return;
                else
                {
                    _cellPopDist = value;
                    OnPropertyChanged("cellPopDist");
                }
            }
        }

        public CellPopulation()
        {
            Guid id = Guid.NewGuid();

            cellpopulation_guid = id.ToString();
            cellpopulation_name = "";
            number = 1;
            cellpopulation_color = new System.Windows.Media.Color();
            cellpopulation_render_on = true;
            cellpopulation_color = System.Windows.Media.Color.FromRgb(255, 255, 255);
            cellpopulation_predef_color = ColorList.Orange;
            cellpopulation_id = Protocol.SafeCellPopulationID++;
            // reporting
            reportXVF = new ReportXVF();
            ecmProbe = new ObservableCollection<ReportECM>();
            ecm_probe_dict = new Dictionary<string, ReportECM>();
        }  
    }

    /// <summary>
    /// Converter to go between molecule GUID references in MolPops
    /// and molecule names kept in the repository of molecules.
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class MolGUIDtoMolNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;
            string mol_name = "";

            if (parameter == null || guid == "")
                return mol_name;

            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigMolecule> mol_list = cvs.Source as ObservableCollection<ConfigMolecule>;
            if (mol_list != null)
            {
                foreach (ConfigMolecule mol in mol_list)
                {
                    if (mol.entity_guid == guid)
                    {
                        mol_name = mol.Name;
                        break;
                    }
                }
            }
            return mol_name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    /// <summary>
    /// Converter to go between molecule GUID references in MolPops
    /// and molecule names kept in the repository of molecules.
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class MolPopGUIDtoMolPopNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (parameter == null)
                return "";

            string guid = value as string;
            string molpop_name = "";
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigMolecularPopulation> molpop_list = cvs.Source as ObservableCollection<ConfigMolecularPopulation>;
            if (molpop_list != null)
            {
                foreach (ConfigMolecularPopulation mp in molpop_list)
                {
                    if (mp.molpop_guid == guid)
                    {
                        molpop_name = mp.Name;
                        break;
                    }
                }
            }
            return molpop_name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    /// <summary>
    /// Converter to go between gene GUID references in cytosol
    /// and gene names kept in the repository of genes.
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class GeneGUIDtoNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;
            string gene_name = "";
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigGene> gene_list = cvs.Source as ObservableCollection<ConfigGene>;
            if (gene_list != null)
            {
                foreach (ConfigGene gene in gene_list)
                {
                    if (gene.entity_guid == guid)
                    {
                        gene_name = gene.Name;
                        break;
                    }
                }
            }
            return gene_name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    /// <summary>
    /// Converter to go between gene GUID references in cytosol
    /// and ConfigGene kept in the repository of genes.
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class GeneGUIDtoConfigGeneConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;

            if (guid == "")
                return null;

            ConfigGene thisGene = null;
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigGene> gene_list = cvs.Source as ObservableCollection<ConfigGene>;
            if (gene_list != null)
            {
                foreach (ConfigGene gene in gene_list)
                {
                    if (gene.entity_guid == guid)
                    {
                        thisGene = gene;
                        break;
                    }
                }
            }
            return thisGene;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    public class MolGuidToMolPopForDiffConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string driver_mol_guid = value as string;
            ConfigCompartment cc =parameter as ConfigCompartment;
            ConfigMolecularPopulation MyMolPop = null;

            if (driver_mol_guid == "" || cc == null)
                return MyMolPop;

            foreach (ConfigMolecularPopulation molpop in cc.molpops)
            {
                if (molpop.molecule.entity_guid == driver_mol_guid)
                {
                    MyMolPop = molpop;
                    break;
                }
            }

            return MyMolPop;

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ConfigMolecularPopulation molpop = value as ConfigMolecularPopulation;

            if (molpop != null)
            {
                return molpop.molecule.entity_guid;
            }

            return "";
        }

    }

    public class DriverElementToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string driver_mol_guid = value as string;
            bool enabled = true;

            if (driver_mol_guid == "")
                enabled = false;
            
            return enabled;

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

    }

    /// <summary>
    /// Converter to go between molecule GUID references in MolPops
    /// and molecule names kept in the repository of molecules.
    /// </summary>
    [ValueConversion(typeof(string), typeof(ObservableCollection<ConfigMolecularPopulation>))]
    public class CellGUIDtoCellNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;
            string cell_name = "";
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigCell> cell_list = cvs.Source as ObservableCollection<ConfigCell>;
            if (cell_list != null)
            {
                foreach (ConfigCell cel in cell_list)
                {
                    if (cel.entity_guid == guid)
                    {
                        cell_name = cel.CellName;
                        break;
                    }
                }
            }
            return cell_name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    /// <summary>
    /// Converter to go between cell pop and cell membrane MolPops
    /// </summary>
    [ValueConversion(typeof(string), typeof(ObservableCollection<ConfigMolecularPopulation>))]
    public class CellGuidToCellMembMolPopsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            string guid = value as string;
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigCell> cell_list = cvs.Source as ObservableCollection<ConfigCell>;
            if (cell_list != null)
            {
                foreach (ConfigCell cel in cell_list)
                {
                    if (cel.entity_guid == guid)
                    {
                        return cel.membrane.molpops;                        
                    }
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    /// <summary>
    /// Converter to go between cell pop and cell cytosol MolPops
    /// </summary>
    [ValueConversion(typeof(string), typeof(ObservableCollection<ConfigMolecularPopulation>))]
    public class CellGuidToCellCytoMolPopsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            string guid = value as string;
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigCell> cell_list = cvs.Source as ObservableCollection<ConfigCell>;
            if (cell_list != null)
            {
                foreach (ConfigCell cel in cell_list)
                {
                    if (cel.entity_guid == guid)
                    {
                        return cel.cytosol.molpops;
                    }
                }
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    /// <summary>
    /// Converter to go between molecule GUID references in MolPops
    /// and molecule names kept in the repository of molecules.
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class ReactionGUIDtoReactionStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;
            string reac_string = "";
            //string cult = culture as string;

            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigReaction> reac_list = cvs.Source as ObservableCollection<ConfigReaction>;
            if (reac_list != null)
            {
                foreach (ConfigReaction cr in reac_list)
                {
                    if (cr.entity_guid == guid)
                    {
                        //This next if is a complete hack!
                        if (culture.Name == "en-US")
                            reac_string = cr.TotalReactionString;
                        else
                            reac_string = cr.rate_const.ToString("G5", System.Globalization.CultureInfo.InvariantCulture); //ToString("E3");  //("#.##E0");        //("#.00");
                        break;
                    }
                }
            }
            return reac_string;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    /// <summary>
    /// Converter to go between molecule GUID references in MolPops
    /// and molecule names kept in the repository of molecules.
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class ReactionComplexGUIDtoReactionComplexStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;
            string rc_string = "";

            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigReactionComplex> rc_list = cvs.Source as ObservableCollection<ConfigReactionComplex>;
            if (rc_list != null)
            {
                foreach (ConfigReactionComplex crc in rc_list)
                {
                    if (crc.entity_guid == guid)
                    {
                        //This next if is a complete hack!
                        rc_string = crc.Name;
                        break;
                    }
                }
            }
            return rc_string;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    //ReacComplexGUIDtoReactionStringsConverter
    /// <summary>
    /// Converter to go between molecule GUID references in MolPops
    /// and molecule names kept in the repository of molecules.
    /// </summary>
    [ValueConversion(typeof(string), typeof(ConfigReactionComplex))]
    public class ReacComplexGUIDtoReactionComplexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;

            if (guid == "")
                return null;

            ConfigReactionComplex rcReturn = null;
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigReactionComplex> rc_list = cvs.Source as ObservableCollection<ConfigReactionComplex>;
            if (rc_list != null)
            {
                foreach (ConfigReactionComplex crc in rc_list)
                {
                    if (crc.entity_guid == guid)
                    {
                        rcReturn = crc; 
                        break;
                    }
                }
            }
            return rcReturn ;
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

    //ColorListToBrushConverter
    /// <summary>
    /// Convert ColorList enum to SolidBrush for rectangle fills
    /// </summary>
    public class ColorListToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Color col = Color.FromRgb(255, 0, 0);
            if (value == null)
                return col;

            try
            {
                int index = (int)value;
                ColorList colEnum = (ColorList)Enum.ToObject(typeof(ColorList), (int)index);

                switch (colEnum)
                {
                    case ColorList.Red:
                        col = Color.FromRgb(255, 0, 0);
                        break;
                    case ColorList.Orange:
                        col = Colors.Orange;
                        break;
                    case ColorList.Yellow:
                        col = Color.FromRgb(255, 255, 0);
                        break;
                    case ColorList.Green:
                        col = Color.FromRgb(0, 255, 0);
                        break;
                    case ColorList.Blue:
                        col = Color.FromRgb(0, 0, 255);
                        break;
                    case ColorList.Indigo:
                        col = Color.FromRgb(64, 0, 192);
                        break;
                    case ColorList.Violet:
                        col = Color.FromRgb(192, 0, 255);
                        break;
                    case ColorList.Custom:
                    default:
                        break;
                }

                return new System.Windows.Media.SolidColorBrush(col);
            }
            catch
            {
                return new System.Windows.Media.SolidColorBrush(col);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return 0;
        }
    }

    //public enum MolPopDistributionType { Homogeneous, Linear, Gaussian, Custom, Explicit }
    public enum MolPopDistributionType { Homogeneous, Linear, Gaussian, Explicit }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(MolPopDistributionType), typeof(string))]
    public class MolPopDistributionTypeToStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the GlobalParameterType enum...
        private List<string> _molpop_dist_type_strings = new List<string>()
                                {
                                    "Homogeneous",
                                    "Linear",
                                    "Gaussian",
                                    //"Custom",
                                    "Explicit",
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _molpop_dist_type_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _molpop_dist_type_strings.FindIndex(item => item == str);
            return (MolPopDistributionType)Enum.ToObject(typeof(MolPopDistributionType), (int)idx);
        }
    }

    // Base class for homog, linear, gauss distributions
    [XmlInclude(typeof(MolPopHomogeneousLevel)),
     XmlInclude(typeof(MolPopLinear)),
     XmlInclude(typeof(MolPopGaussian))]
    public abstract class MolPopDistribution : EntityModelBase
    {
        [XmlIgnore]
        public MolPopDistributionType mp_distribution_type { get; protected set; }
        public List<BoundaryCondition> boundaryCondition { get; set; }        

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
            concentration = 1.0;
        }
    }

    public class MolPopLinear : MolPopDistribution
    {
        public double x1 { get; set; }
        public int dim { get; set; }
        private BoundaryFace _boundary_face;
        public BoundaryFace boundary_face
        {
            get
            {
                return _boundary_face;
            }
            set
            {
                _boundary_face = value;
                dim = (int)_boundary_face - 1;
            }
        }
        
        public MolPopLinear()
        {
            mp_distribution_type = MolPopDistributionType.Linear;
            x1 = 0;
            boundaryCondition = new List<BoundaryCondition>();
        }

        public void Initalize(BoundaryFace b_face)
        {
            boundary_face = b_face;
            dim = (int)boundary_face - 1;

            switch (boundary_face)
            {
                case BoundaryFace.X:
                    boundaryCondition[0].boundary = Boundary.left;
                    boundaryCondition[1].boundary = Boundary.right;
                    break;

                case BoundaryFace.Y:
                    boundaryCondition[0].boundary = Boundary.bottom;
                    boundaryCondition[1].boundary = Boundary.top;
                    break;

                case BoundaryFace.Z:
                    boundaryCondition[0].boundary = Boundary.back;
                    boundaryCondition[1].boundary = Boundary.front;
                    break;
                case BoundaryFace.None:
                    break;

            }
        }
    }

    public class MolPopGaussian : MolPopDistribution
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
                    //OnPropertyChanged("gaussgrad_gauss_spec_guid_ref");
                }
            }
        }

        public MolPopGaussian()
        {
            mp_distribution_type = MolPopDistributionType.Gaussian;
            peak_concentration = 1.0;
            gaussgrad_gauss_spec_guid_ref = "";
        }
    }

    /// <summary>
    /// added to store intermediate run state
    /// </summary>
    public class MolPopExplicit : MolPopDistribution
    {
        public MolPopExplicit()
        {
            mp_distribution_type = MolPopDistributionType.Explicit;
        }

        public double[] conc;
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

        ////Add this after 2/4/14
        ////public bool DrawAsWireframe { get; set; }

        public GaussianSpecification()
        {
            gaussian_spec_name = "";
            gaussian_spec_box_guid_ref = "";
            gaussian_region_visibility = true;
            gaussian_spec_color = new System.Windows.Media.Color();
            gaussian_spec_color = System.Windows.Media.Color.FromRgb(255, 255, 255);

            ////Add this after 2/4/14
            ////DrawAsWireframe = false;
        }
    }

    //Graphics classes
    public class RenderColor
    {
        public System.Windows.Media.Color EntityColor { get; set; }  // RGB plus alpha channel
    }

    public class RenderCellPop
    {
        RenderColor base_color;  // solid color for applicable render methods
        //ObservableCollection<RenderColor> state_colors
        //ObservableCollection<RenderColor> gen_colors
        bool renderOn;          // toggles rendering
        //Method renderMethod;    // indicates the render option
        int shades;             // number of shades for applicable options
    }


    public class RenderMolPop
    {
        RenderColor color;     // the one color used
        bool renderOn;         // toggles rendering
        //Method renderMethod;   // render option
        double min, max;       // to scale when rendering by conc
        int shades;            // number of shades for applicable options
        double blendingWeight; // controls color mixing for multiple molpops
    }

    public class RenderDrawing
    {
        public RenderColor bg_color { get; set; }     // the background color

        public RenderDrawing()
        {
            bg_color = new RenderColor();
            bg_color.EntityColor = Color.FromScRgb(255.0f, 128.0f, 0.0f, 0.0f);
        }
    }

    public class RenderPalette
    {
        ObservableCollection<RenderCellPop> renderCellPops;
        ObservableCollection<RenderMolPop> renderMolPops;
        public RenderDrawing renderDrawing { get; set; }

        public RenderPalette()
        {
            renderDrawing = new RenderDrawing();
        }
    }


    // UTILITY CLASSES =======================
    public class BoxSpecification : EntityModelBase
    {
        public string box_guid { get; set; }
        public double[][] transform_matrix { get; set; }
        private bool _box_visibility = true;
        private bool _blob_visibility = true;
        private bool _current_blob_visibility = true;
        private bool _current_box_visibility = true;
        
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
        public bool blob_visibility
        {
            get { return _blob_visibility; }
            set
            {
                if (_blob_visibility == value)
                    return;
                else
                {
                    _blob_visibility = value;
                    OnPropertyChanged("blob_visibility");
                }
            }
        }
        public bool current_box_visibility
        {
            get { return _current_box_visibility; }
            set
            {
                if (_current_box_visibility == value)
                    return;
                else
                {
                    _current_box_visibility = value;
                    OnPropertyChanged("current_box_visibility");
                }
            }
        }
        public bool current_blob_visibility
        {
            get { return _current_blob_visibility; }
            set
            {
                if (_current_blob_visibility == value)
                    return;
                else
                {
                    _current_blob_visibility = value;
                    OnPropertyChanged("current_blob_visibility");
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

        public double getScale(byte i)
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

        [JsonIgnore]
        public double half_x_scale
        {
            get
            {
                return x_scale / 2;
            }
            set
            {
                x_scale = 2 * value;
                base.OnPropertyChanged("x_scale");                
            }
        }
        [JsonIgnore]
        public double half_y_scale
        {
            get
            {
                return y_scale / 2;
            }
            set
            {
                y_scale = 2 * value;
                base.OnPropertyChanged("y_scale");
            }
        }
        [JsonIgnore]
        public double half_z_scale
        {
            get
            {
                return z_scale / 2;
            }
            set
            {
                z_scale = 2 * value;
                base.OnPropertyChanged("z_scale");
            }
        }

        public BoxSpecification()
        {
            Guid id = Guid.NewGuid();

            box_guid = id.ToString();
            box_visibility = true;
            blob_visibility = true;
            current_box_visibility = true;
            current_blob_visibility = true;
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
                base.OnPropertyChanged("half_x_scale");
            }
            if (y_scale_change == true)
            {
                base.OnPropertyChanged("y_scale");
                base.OnPropertyChanged("half_y_scale");
            }
            if (z_scale_change == true)
            {
                base.OnPropertyChanged("z_scale");
                base.OnPropertyChanged("half_z_scale");
            }
        }

        public void BoxChangedEventHandler(object obj, EventArgs e)
        {
            // not sure if anything goes here.
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
    /// Convert Reporter enum to boolean
    /// </summary>
    [ValueConversion(typeof(ExtendedReport), typeof(bool))]
    public class RptEnumBooleanConverter : IValueConverter
    {
        #region IValueConverter Members
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;

            object parameterValue = Enum.Parse(value.GetType(), parameterString);

            bool ret = parameterValue.Equals(value);
            return ret;

        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string parameterString = parameter as string;
            if (parameterString == null)
                return DependencyProperty.UnsetValue;

            bool chk = (bool)value;

            if (chk == false)
                return Enum.Parse(targetType, "NONE");

            return Enum.Parse(targetType, parameterString);
        }
        #endregion
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
            // Console.WriteLine(String.Format(msg));
        }
#endif

        #endregion // IDisposable Members
    }

}
