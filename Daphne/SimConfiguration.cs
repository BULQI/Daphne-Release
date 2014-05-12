using System;
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
    public class SimConfigurator
    {
        public string FileName { get; set; }
       // public const string TempScenarioFile; // = "Config\\temp_scenario.json", TempUserDefFile = "Config\\temp_userdef.json";
        public SimConfiguration SimConfig { get; set; }

        public string TempScenarioFile { get; set; }
        public string TempUserDefFile { get; set; }

        //public UserDefinedGroup userDefGroup { get; set; }

        public SimConfigurator()
        {
            this.SimConfig = new SimConfiguration();
            //userDefGroup = new UserDefinedGroup();
            TempScenarioFile = "Config\\temp_scenario.json";
            TempUserDefFile = "Config\\temp_userdef.json";
        }

        public SimConfigurator(string filename)
        {
            if (filename == null)
            {
                throw new ArgumentNullException("filename");
            }

            TempScenarioFile = "Config\\temp_scenario.json";
            TempUserDefFile = "Config\\temp_userdef.json";

            this.FileName = filename;
            this.SimConfig = new SimConfiguration();

            //userDefGroup = new UserDefinedGroup();
            //string UserDefFileName = Directory.GetCurrentDirectory() + "\\config\\UserDefinedGroup.json";
            //var Settings = new JsonSerializerSettings();
            //Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            //Settings.TypeNameHandling = TypeNameHandling.Auto;
            //if (File.Exists(UserDefFileName))
            //{
            //    string readText = File.ReadAllText(UserDefFileName);
            //    userDefGroup = JsonConvert.DeserializeObject<UserDefinedGroup>(readText, Settings);
            //}
        }

        public void SerializeSimConfigToFile(bool tempFiles = false)
        {
            //skg daphne serialize to json Thursday, April 18, 2013
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;

            //userDefGroup.Reset();
            //userDefGroup.CopyFromConfig(SimConfig);
            
            //serialize SimConfig
            string jsonSpec = JsonConvert.SerializeObject(SimConfig, Newtonsoft.Json.Formatting.Indented, Settings);
            string jsonFile = tempFiles == true ? TempScenarioFile : FileName;

            try
            {
                File.WriteAllText(jsonFile, jsonSpec);
            }
            catch
            {
                MessageBox.Show("File.WriteAllText failed in SerializeSimConfigToFile. Filename and TempScenarioFile = " + FileName + ", " + TempScenarioFile);
            }

            ////serialize user defined objects
            //jsonSpec = JsonConvert.SerializeObject(userDefGroup, Newtonsoft.Json.Formatting.Indented, Settings);
            //jsonFile = tempFiles == true ? TempUserDefFile : "Config\\UserDefinedGroup.json";
            //File.WriteAllText(jsonFile, jsonSpec);
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

        public void DeserializeSimConfig(bool tempFiles = false)
        {
            //Deserialize JSON
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;

            //deserialize SimConfig
            string jsonFile = tempFiles == true ? TempScenarioFile : FileName;
            string readText = File.ReadAllText(jsonFile);
            SimConfig = JsonConvert.DeserializeObject<SimConfiguration>(readText, settings);
            SimConfig.InitializeStorageClasses();

            ////I think we need to remove the user def items if any, from SimConfig and copy latest userdefgroup items into the config
            ////SimConfig.RemoveUserDefinedItems();

            ////deserialize user defined objects
            //jsonFile = tempFiles == true ? TempUserDefFile : "Config\\UserDefinedGroup.json";
            //if (File.Exists(jsonFile))
            //{
            //    readText = File.ReadAllText(jsonFile);
            //    userDefGroup = JsonConvert.DeserializeObject<UserDefinedGroup>(readText, settings);
            //    userDefGroup.CopyToConfig(SimConfig);
            //}

        }

        public void DeserializeSimConfigFromString(string simConfigJson)
        {
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            SimConfig = JsonConvert.DeserializeObject<SimConfiguration>(simConfigJson, settings);
            SimConfig.InitializeStorageClasses();

            ////deserialize user defined items
            //string userfilename = Directory.GetCurrentDirectory() + "\\config\\UserDefinedGroup.json";
            //if (File.Exists(userfilename))
            //{
            //    string readText = File.ReadAllText(userfilename);
            //    userDefGroup = JsonConvert.DeserializeObject<UserDefinedGroup>(readText, settings);
            //    userDefGroup.CopyToConfig(SimConfig);
            //}
        }
    }

    public class UserDefinedGroup
    {
        public ObservableCollection<ConfigMolecule> user_molecules { get; set; }
        public ObservableCollection<ConfigCell> user_cells { get; set; }
        public ObservableCollection<ConfigReaction> user_reactions { get; set; }

        public UserDefinedGroup()
        {
            user_molecules = new ObservableCollection<ConfigMolecule>();
            user_reactions = new ObservableCollection<ConfigReaction>();
            user_cells = new ObservableCollection<ConfigCell>();
        }

        public void Reset()
        {
            user_molecules.Clear();
            user_cells.Clear();
            user_reactions.Clear();
        }

        public void CopyToConfig(SimConfiguration sc)
        {
            foreach (ConfigMolecule mol in user_molecules)
            {
                if (!sc.entity_repository.molecules_dict.ContainsKey(mol.molecule_guid))
                    sc.entity_repository.molecules.Add(mol);
            }
            foreach (ConfigCell cell in user_cells)
            {
                if (!sc.entity_repository.cells_dict.ContainsKey(cell.cell_guid))
                    sc.entity_repository.cells.Add(cell);
            }
            foreach (ConfigReaction reac in user_reactions)
            {
                if (!sc.entity_repository.reactions_dict.ContainsKey(reac.reaction_guid))
                    sc.entity_repository.reactions.Add(reac);
            }
        }

        public void CopyFromConfig(SimConfiguration sc)
        {
            //First copy all user defined from entity_repos
            foreach (ConfigMolecule mol in sc.entity_repository.molecules)
            {
                if (mol.ReadOnly == false)
                {
                    if (Contains(mol))
                    {
                        user_molecules.Remove(mol);
                    }

                    user_molecules.Add(mol);                    
                }
            }
            foreach (ConfigCell cell in sc.entity_repository.cells)
            {
                if (cell.ReadOnly == false)
                {
                    if (Contains(cell))
                    {
                        user_cells.Remove(cell);
                    }

                    user_cells.Add(cell);
                }
            }
            foreach (ConfigReaction reac in sc.entity_repository.reactions)
            {
                if (reac.ReadOnly == false && !Contains(reac))
                {
                    if (Contains(reac))
                    {
                        user_reactions.Remove(reac);
                    }

                    user_reactions.Add(reac);
                }
            }

        }

        public bool Contains(object userdefitem)
        {
            bool ret = false;

            if (userdefitem.GetType() == typeof(ConfigMolecule))
            {
                ConfigMolecule inputmol = (ConfigMolecule)userdefitem;
                foreach (ConfigMolecule mol in user_molecules)
                {
                    if (mol.molecule_guid == inputmol.molecule_guid)
                    {
                        ret = true;
                        break;
                    }
                }
            }
            if (userdefitem.GetType() == typeof(ConfigCell))
            {
                ConfigCell inputcell = (ConfigCell)userdefitem;
                foreach (ConfigCell cell in user_cells)
                {
                    if (cell.cell_guid == inputcell.cell_guid)
                    {
                        ret = true;
                        break;
                    }
                }
            }
            if (userdefitem.GetType() == typeof(ConfigReaction))
            {
                ConfigReaction inputreac = (ConfigReaction)userdefitem;
                foreach (ConfigReaction reac in user_reactions)
                {
                    if (reac.reaction_guid == inputreac.reaction_guid)
                    {
                        ret = true;
                        break;
                    }
                }
            }

            return ret;
        }

    }

    public class DaphneDouble : INotifyPropertyChanged
    {
        private double min;                         //minimum value allowed
        private double max;                         //maximum value allowed 

        public double Tick { get; set; }            //slider/edit box increment if applicable
        public int RangeMultFactor { get; set; }
        private double rangeFactor;                 //example - 2 means (min = value - value/2), (max = value + value/2)

        private string _format;                    //format string
        private int _decimal_places;            //number of decimal places to display
        private double _value;                     //the actual double value that the object contains
        private string fvalue;                      //string that represents value after formatting is applied
        private double sn_upper_threshold;                 //if value is greater than or equal to this number, then output in scientific notation        
        private double sn_lower_threshold;                 //if value is greater than or equal to this number, then output in scientific notation        

        public double Max
        {
            get
            {
                return max;
            }
            set
            {
                max = value;
                OnPropertyChanged("Max");
            }
        }

        public double Min
        {
            get
            {
                return min;
            }
            set
            {
                min = value;
                OnPropertyChanged("Min");
            }
        }

        public double RangeFactor
        {
            get
            {
                return rangeFactor;
            }
            set
            {
                rangeFactor = value;
                if (rangeFactor <= 0)
                    rangeFactor = 2;
            }
        }

        public string FValue
        {
            get
            {
                return fvalue;
            }
            set
            {
                fvalue = string.Format(_format, _value);
                OnPropertyChanged("FValue");
            }
        }

        public double Value
        {
            get
            {
                return _value;
            }
            set
            {
                _value = value;

                Format = "";
                FValue = string.Format(_format, _value);

                OnPropertyChanged("Value");
            }
        }

        public int DecimalPlaces
        {
            get
            {
                return _decimal_places;
            }
            set
            {
                _decimal_places = value;

                Format = "0";
                FValue = string.Format(_format, Value);

                OnPropertyChanged("DecimalPlaces");
            }
        }

        public double SnUpperThreshold
        {
            get
            {
                return sn_upper_threshold;
            }
            set
            {
                sn_upper_threshold = value;
                OnPropertyChanged("SnUpperThreshold");
                Format = "0";
                FValue = string.Format(_format, Value);
            }
        }

        public double SnLowerThreshold
        {
            get
            {
                return sn_lower_threshold;
            }
            set
            {
                sn_lower_threshold = value;
                OnPropertyChanged("SnLowerThreshold");
                Format = "0";
                FValue = string.Format(_format, Value);
            }
        }

        public string Format
        {
            get
            {
                return _format;
            }
            set
            {
                _format = "{0:N" + DecimalPlaces.ToString() + "}";
                if (Value >= SnUpperThreshold)
                {
                    if (DecimalPlaces == 0)
                        DecimalPlaces++;

                    _format = "{0:#.";
                    for (int i = 0; i < DecimalPlaces; i++)
                    {
                        _format += "#";
                    }

                    _format += "e+00}";
                }
                else if (Value <= SnLowerThreshold)
                {
                    if (DecimalPlaces == 0)
                        DecimalPlaces++;

                    _format = "{0:#.";
                    for (int i = 0; i < DecimalPlaces; i++)
                    {
                        _format += "#";
                    }

                    _format += "e-00}";
                }
                OnPropertyChanged("Format");
            }
        }

        public void SetMinMax()
        {
            Max = _value + _value / RangeFactor;
            Min = _value - _value / RangeFactor;
        }

        public void SetMinMax(double d)
        {
            Max = d + d / RangeFactor;
            Min = d - d / RangeFactor;
        }

        //Constructor
        public DaphneDouble()
        {
            ////_value = 20;
            _decimal_places = 3;
            SnUpperThreshold = 100;
            SnLowerThreshold = 0.01;
            RangeMultFactor = 10;
            RangeFactor = 2;
            Format = "-";
            ////fvalue = string.Format(_format, _value);
            ////Max = _value + _value / RangeFactor;
            ////Min = _value - _value / RangeFactor;
        }

        //Notification handling
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);

            }
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
        public Scenario rc_scenario { get; set; }
        public EntityRepository entity_repository { get; set; }
        public SimulationParams sim_params { get; set; }
        public string reporter_file_name { get; set; }

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
            rc_scenario = new Scenario();
            entity_repository = new EntityRepository();
            sim_params = new SimulationParams();

            //////LoadDefaultGlobalParameters();
            ////LoadUserDefinedItems();           

            // Utility storage
            // NOTE: No use adding CollectionChanged event handlers here since it gets wiped out by deserialization anyway...
            box_guid_box_dict = new Dictionary<string, BoxSpecification>();            
            cellpopulation_id_cellpopulation_dict = new Dictionary<int, CellPopulation>();

            reporter_file_name = "";
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

        public void RemoveUserDefinedItems()
        {
            foreach (ConfigMolecule mol in entity_repository.molecules.ToList())
            {
                if (mol.ReadOnly == false)
                {
                    entity_repository.molecules_dict.Remove(mol.molecule_guid);
                    entity_repository.molecules.Remove(mol);
                }
            }
            foreach (ConfigCell cell in entity_repository.cells.ToList())
            {
                if (cell.ReadOnly == false)
                {
                    entity_repository.cells_dict.Remove(cell.cell_guid);
                    entity_repository.cells.Remove(cell);
                }
            }
            foreach (ConfigReaction reac in entity_repository.reactions.ToList())
            {
                if (reac.ReadOnly == false)
                {
                    entity_repository.reactions_dict.Remove(reac.reaction_guid);
                    entity_repository.reactions.Remove(reac);
                }
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
            InitGaussSpecsAndGuidGaussDict();
            InitCellPopulationIDCellPopulationDict();
            InitMoleculeIDConfigMoleculeDict();
            InitMolPopIDConfigMolecularPopDict_ECMProbeDict();
            InitCellIDConfigCellDict();
            InitReactionTemplateIDConfigReactionTempalteDict();
            InitReactionIDConfigReactionDict();
            InitReactionComplexIDConfigReactionComplexDict();
            InitGaussCellPopulationUpdates();
            InitGeneIDConfigGeneDict();
            InitDiffSchemeIDConfigDiffSchemeDict();
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
        private void InitGaussSpecsAndGuidGaussDict()
        {
            entity_repository.gauss_guid_gauss_dict.Clear();
            foreach (GaussianSpecification gs in entity_repository.gaussian_specifications)
            {
                entity_repository.gauss_guid_gauss_dict.Add(gs.gaussian_spec_box_guid_ref, gs);
            }
            entity_repository.gaussian_specifications.CollectionChanged += new NotifyCollectionChangedEventHandler(gaussian_specifications_CollectionChanged);
        }

        private void InitCellPopulationIDCellPopulationDict()
        {
            cellpopulation_id_cellpopulation_dict.Clear();  
            foreach (CellPopulation cs in scenario.cellpopulations)
            {
                cellpopulation_id_cellpopulation_dict.Add(cs.cellpopulation_id, cs);

                if (cs.cellPopDist != null)
                {
                    cs.cellPopDist.cellPop = cs;
                }
            }
            scenario.cellpopulations.CollectionChanged += new NotifyCollectionChangedEventHandler(cellsets_CollectionChanged);
        }

        private void InitGaussCellPopulationUpdates()
        {
            foreach (CellPopulation cs in scenario.cellpopulations)
            {
                if (cs.cellPopDist.DistType == CellPopDistributionType.Gaussian)
                {
                    BoxSpecification box = box_guid_box_dict[((CellPopGaussian)cs.cellPopDist).box_guid];
                    ((CellPopGaussian)cs.cellPopDist).ParamReset(box);
                    box.PropertyChanged += new PropertyChangedEventHandler(((CellPopGaussian)cs.cellPopDist).CellPopGaussChanged);
                }
            }
        }

        private void InitMoleculeIDConfigMoleculeDict()
        {
            entity_repository.molecules_dict.Clear();
            foreach (ConfigMolecule cm in entity_repository.molecules)
            {
                entity_repository.molecules_dict.Add(cm.molecule_guid, cm);
            }
            entity_repository.molecules.CollectionChanged += new NotifyCollectionChangedEventHandler(molecules_CollectionChanged);
        }

        private void InitMolPopIDConfigMolecularPopDict_ECMProbeDict()
        {
            ConfigCompartment ecs = scenario.environment.ecs;

            ecs.molpops_dict.Clear();
            foreach (ConfigMolecularPopulation cmp in ecs.molpops)
            {
                ecs.molpops_dict.Add(cmp.molpop_guid, cmp);
            }

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
                entity_repository.diff_schemes_dict.Add(ds.diff_scheme_guid, ds);
            }
            entity_repository.molecules.CollectionChanged += new NotifyCollectionChangedEventHandler(diff_schemes_CollectionChanged);
        }


        private void InitGeneIDConfigGeneDict()
        {
            entity_repository.genes_dict.Clear();
            foreach (ConfigGene cg in entity_repository.genes)
            {
                entity_repository.genes_dict.Add(cg.gene_guid, cg);
            }
            entity_repository.molecules.CollectionChanged += new NotifyCollectionChangedEventHandler(genes_CollectionChanged);
        }

        

        private void InitCellIDConfigCellDict()
        {
            entity_repository.cells_dict.Clear();
            foreach (ConfigCell cc in entity_repository.cells)
            {
                entity_repository.cells_dict.Add(cc.cell_guid, cc);
            }
            entity_repository.cells.CollectionChanged += new NotifyCollectionChangedEventHandler(cells_CollectionChanged);

        }

        private void InitReactionIDConfigReactionDict()
        {
            entity_repository.reactions_dict.Clear();
            foreach (ConfigReaction cr in entity_repository.reactions)
            {
                entity_repository.reactions_dict.Add(cr.reaction_guid, cr);
                cr.GetTotalReactionString(entity_repository);
            }
            entity_repository.reactions.CollectionChanged += new NotifyCollectionChangedEventHandler(reactions_CollectionChanged);

        }

        private void InitReactionComplexIDConfigReactionComplexDict()
        {
            entity_repository.reaction_complexes_dict.Clear();
            foreach (ConfigReactionComplex crc in entity_repository.reaction_complexes)
            {
                entity_repository.reaction_complexes_dict.Add(crc.reaction_complex_guid, crc);
            }
            entity_repository.reaction_complexes.CollectionChanged += new NotifyCollectionChangedEventHandler(reaction_complexes_CollectionChanged);

        }

        private void InitReactionTemplateIDConfigReactionTempalteDict()
        {
            entity_repository.reaction_templates_dict.Clear();
            foreach (ConfigReactionTemplate crt in entity_repository.reaction_templates)
            {
                entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);
            }
            entity_repository.reaction_templates.CollectionChanged += new NotifyCollectionChangedEventHandler(template_reactions_CollectionChanged);
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
                    entity_repository.gauss_guid_gauss_dict.Add(gs.gaussian_spec_box_guid_ref, gs);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    GaussianSpecification gs = dd as GaussianSpecification;
                    entity_repository.gauss_guid_gauss_dict.Remove(gs.gaussian_spec_box_guid_ref);
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

                    foreach (ConfigMolecularPopulation mp in scenario.environment.ecs.molpops)
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

        //genes_CollectionChanged
        private void genes_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    ConfigGene cg = nn as ConfigGene;
                    entity_repository.genes_dict.Add(cg.gene_guid, cg);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigGene cg = dd as ConfigGene;

                    //Remove gene from genes_dict
                    entity_repository.genes_dict.Remove(cg.gene_guid);
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
                    entity_repository.diff_schemes_dict.Add(cds.diff_scheme_guid, cds);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigDiffScheme cds = dd as ConfigDiffScheme;

                    //Remove gene from genes_dict
                    entity_repository.genes_dict.Remove(cds.diff_scheme_guid);
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
                    entity_repository.molecules_dict.Add(cm.molecule_guid, cm);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigMolecule cm = dd as ConfigMolecule;

                    //Remove molecule from molecules_dict
                    entity_repository.molecules_dict.Remove(cm.molecule_guid);

                    //Remove all the ECM molpops that have this molecule type
                    foreach (KeyValuePair<string, ConfigMolecularPopulation> kvp in scenario.environment.ecs.molpops_dict.ToList())
                    {
                        if (kvp.Value.molecule_guid_ref == cm.molecule_guid)
                        {
                            scenario.environment.ecs.molpops_dict.Remove(kvp.Key);
                            scenario.environment.ecs.molpops.Remove(kvp.Value);
                        }
                    }

                    //Remove all the cell membrane molpops that have this molecule type
                    foreach (ConfigCell cell in entity_repository.cells)
                    {
                        if (cell.ReadOnly == false)
                        {
                            foreach (KeyValuePair<string, ConfigMolecularPopulation> kvp in cell.membrane.molpops_dict.ToList())
                            {
                                if (kvp.Value.molecule_guid_ref == cm.molecule_guid)
                                {
                                    cell.membrane.molpops_dict.Remove(kvp.Key);
                                    cell.membrane.molpops.Remove(kvp.Value);
                                }
                            }
                        }
                    }

                    //Remove all the cell cytosol molpops that have this molecule type
                    foreach (ConfigCell cell in entity_repository.cells)
                    {
                        if (cell.ReadOnly == false)
                        {
                            foreach (ConfigMolecularPopulation cmp in cell.cytosol.molpops.ToList())
                            {
                                if (cmp.molecule_guid_ref == cm.molecule_guid)
                                {
                                    //cell.cytosol.molpops_dict.Remove(kvp.Key);
                                    cell.cytosol.molpops.Remove(cmp);
                                }
                            }
                        }
                    }

                    //Remove all the reactions that use this molecule
                    foreach (KeyValuePair<string, ConfigReaction> kvp in entity_repository.reactions_dict.ToList())
                    {
                        ConfigReaction reac = kvp.Value;
                        if (reac.HasMolecule(cm.molecule_guid))
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

                    // add molpop into molpops_dict
                    scenario.environment.ecs.molpops_dict.Add(mp.molpop_guid, mp);

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

                    // remove from molpops_dict
                    scenario.environment.ecs.molpops_dict.Remove(mp.molpop_guid);

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
                    entity_repository.cells_dict.Add(cc.cell_guid, cc);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigCell cc = dd as ConfigCell;

                    //Remove this guid from ER cells_dict
                    entity_repository.cells_dict.Remove(cc.cell_guid);

                    //Remove all ECM cell populations with this cell guid
                    foreach (var cell_pop in scenario.cellpopulations.ToList())
                    {
                        if (cc.cell_guid == cell_pop.cell_guid_ref)
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
                    entity_repository.reactions_dict.Add(cr.reaction_guid, cr);
                    cr.GetTotalReactionString(entity_repository);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigReaction cr = dd as ConfigReaction;

                    //Remove entry from ER reactions_dict
                    entity_repository.reactions_dict.Remove(cr.reaction_guid);

                    //Remove all the ER reaction complex reactions that have this guid
                    foreach (ConfigReactionComplex comp in entity_repository.reaction_complexes)
                    {
                        if (comp.reactions_guid_ref.Contains(cr.reaction_guid) )
                            comp.reactions_guid_ref.Remove(cr.reaction_guid);
                    }                    

                    //Remove all the ECM reaction complex reactions that have this guid
                    if (scenario.environment.ecs.reaction_complexes_guid_ref.Contains(cr.reaction_guid))
                    {
                        scenario.environment.ecs.reaction_complexes_guid_ref.Remove(cr.reaction_guid);
                    }

                    //Remove all the ECM reactions that have this guid
                    if (scenario.environment.ecs.reactions_guid_ref.Contains(cr.reaction_guid))
                        scenario.environment.ecs.reactions_guid_ref.Remove(cr.reaction_guid);

                    //Remove all the cell membrane/cytosol reactions that have this guid
                    foreach (ConfigCell cell in entity_repository.cells)
                    {
                        if (cell.ReadOnly == false)
                        {
                            if (cell.membrane.reactions_guid_ref.Contains(cr.reaction_guid))
                                cell.membrane.reactions_guid_ref.Remove(cr.reaction_guid);

                            if (cell.cytosol.reactions_guid_ref.Contains(cr.reaction_guid))
                                cell.cytosol.reactions_guid_ref.Remove(cr.reaction_guid);
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
                    entity_repository.reaction_templates_dict.Add(crt.reaction_template_guid, crt);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigReactionTemplate crt = dd as ConfigReactionTemplate;
                    entity_repository.reaction_templates_dict.Remove(crt.reaction_template_guid);
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
                    entity_repository.reaction_complexes_dict.Add(crc.reaction_complex_guid, crc);
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    ConfigReactionComplex crt = dd as ConfigReactionComplex;
                    entity_repository.reaction_complexes_dict.Remove(crt.reaction_complex_guid);
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

        public List<ConfigReaction> GetBoundaryReactions(ConfigCompartment configComp)
        {
            List<string> reac_guids = new List<string>();
            List<ConfigReaction> config_reacs = new List<ConfigReaction>();

            foreach (string rguid in configComp.reactions_guid_ref)
            {
                ConfigReaction cr = entity_repository.reactions_dict[rguid];
                if (entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].isBoundary == true)
                {
                    reac_guids.Add(rguid);
                    config_reacs.Add(cr);
                }
            }

            foreach (string rcguid in configComp.reaction_complexes_guid_ref)
            {
                ConfigReactionComplex crc = entity_repository.reaction_complexes_dict[rcguid];
                foreach (string rguid in crc.reactions_guid_ref)
                {
                    if (reac_guids.Contains(rguid) == false)
                    {
                        ConfigReaction cr = entity_repository.reactions_dict[rguid];
                        if (entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].isBoundary == true)
                        {
                            //reac_guids.Add(rguid);
                            config_reacs.Add(cr);
                        }
                    }
                }
            }
            return config_reacs;
        }

        public List<ConfigReaction> GetBulkReactions(ConfigCompartment configComp)
        {
            List<string> reac_guids = new List<string>();
            List<ConfigReaction> config_reacs = new List<ConfigReaction>();

            foreach (string rguid in configComp.reactions_guid_ref)
            {
                ConfigReaction cr = entity_repository.reactions_dict[rguid];
                if (entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].isBoundary == false)
                {
                    reac_guids.Add(rguid);
                    config_reacs.Add(cr);
                }
            }

            foreach (string rcguid in configComp.reaction_complexes_guid_ref)
            {
                ConfigReactionComplex crc = entity_repository.reaction_complexes_dict[rcguid];
                foreach (string rguid in crc.reactions_guid_ref)
                {
                    if (reac_guids.Contains(rguid) == false)
                    {
                        ConfigReaction cr = entity_repository.reactions_dict[rguid];
                        if (entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref].isBoundary == false)
                        {
                            //reac_guids.Add(rguid);
                            config_reacs.Add(cr);
                        }
                    }
                }
            }
            return config_reacs;
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
        public SimStates simInterpolate { get; set; }
        public SimStates simCellSize { get; set; }
        public TimeConfig time_config { get; set; }
#if CELL_REGIONS
        public ObservableCollection<Region> regions { get; set; }
#endif
        public ConfigEnvironment environment { get; set; }
        public ObservableCollection<CellPopulation> cellpopulations { get; set; }

        

        public Scenario()
        {
            simInterpolate = SimStates.Linear;
            simCellSize = SimStates.Tiny;
            time_config = new TimeConfig();
#if CELL_REGIONS
            regions = new ObservableCollection<Region>();
#endif
            environment = new ConfigEnvironment();
            cellpopulations = new ObservableCollection<CellPopulation>();

        }

        public bool HasCell(ConfigCell cell)
        {
            bool res = false;
            foreach (CellPopulation cell_pop in cellpopulations)
            {
                if (cell_pop.cell_guid_ref == cell.cell_guid)
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
        public ObservableCollection<GaussianSpecification> gaussian_specifications { get; set; }
        public ObservableCollection<BoxSpecification> box_specifications { get; set; }
        public ObservableCollection<ConfigReactionComplex> reaction_complexes { get; set; }

        //All molecules, reactions, cells - Combined Predefined and User defined
        public ObservableCollection<ConfigCell> cells { get; set; }

        public ObservableCollection<ConfigMolecule> molecules { get; set; }
        public ObservableCollection<ConfigGene> genes { get; set; }

        public ObservableCollection<ConfigReaction> reactions { get; set; }

        public ObservableCollection<ConfigReactionTemplate> reaction_templates { get; set; }

        public Dictionary<string, ConfigMolecule> molecules_dict; // keyed by molecule_guid
        public Dictionary<string, ConfigGene> genes_dict; // keyed by gene_guid
        public Dictionary<string, ConfigReactionTemplate> reaction_templates_dict;
        public Dictionary<string, ConfigReaction> reactions_dict;
        public Dictionary<string, ConfigCell> cells_dict;
        public Dictionary<string, ConfigReactionComplex> reaction_complexes_dict;
        public Dictionary<string, GaussianSpecification> gauss_guid_gauss_dict;

        public ObservableCollection<ConfigDiffScheme> diff_schemes { get; set; }
        public Dictionary<string, ConfigDiffScheme> diff_schemes_dict;

        public EntityRepository()
        {
            gaussian_specifications = new ObservableCollection<GaussianSpecification>();
            box_specifications = new ObservableCollection<BoxSpecification>();
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
            gauss_guid_gauss_dict = new Dictionary<string, GaussianSpecification>();
            diff_schemes = new ObservableCollection<ConfigDiffScheme>();
            diff_schemes_dict = new Dictionary<string, ConfigDiffScheme>();
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
                    _extent_x = value;
                    CalculateNumGridPts();
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
                    CalculateNumGridPts();
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
                    CalculateNumGridPts();
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
                    CalculateNumGridPts();
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
            gridstep = 50;
            extent_x = 400;
            extent_y = 400;
            extent_z = 400;
            extent_min = 5;
            extent_max = 1000;
            gridstep_min = 1;
            gridstep_max = 100;
            initialized = true;
            toroidal = false;

            CalculateNumGridPts();

            ecs = new ConfigCompartment();
        }

        private bool initialized = false;
        
        private void CalculateNumGridPts()
        {
            if (initialized == false)
            {
                return;
            }

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

    /// <summary>
    /// Convert double to formatted string
    /// </summary>
    [ValueConversion(typeof(double), typeof(string))]
    public class DoubleToFormattedStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value == null)
                return "";

            string output = "";

            try
            {
                double dd = (double)value;
                System.Windows.FrameworkElement fe = (System.Windows.FrameworkElement)parameter;
                ConfigReaction reac = (ConfigReaction)(fe.DataContext);
                
                output = string.Format(reac.daph_rate_const.Format, dd);
            }
            catch
            {
                output = "";
            }

            return output;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double val = double.Parse((string)value);
            System.Windows.FrameworkElement fe = (System.Windows.FrameworkElement)parameter;
            ConfigReaction reac = (ConfigReaction)(fe.DataContext);
            reac.daph_rate_const.Value = val;
            //reac.rate_const = val;

            return val;
        }
    }



   

    public enum MoleculeLocation { Bulk = 0, Boundary }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(MoleculeLocation), typeof(string))]
    public class MoleculeLocationToShortStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the MoleculeLocation enum...
        private List<string> _molecule_location_strings = new List<string>()
                                {
                                    "bulk",
                                    "boundary"
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _molecule_location_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _molecule_location_strings.FindIndex(item => item == str);
            return (MoleculeLocation)Enum.ToObject(typeof(MoleculeLocation), (int)idx);
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

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(BoundaryType), typeof(string))]
    public class BoundaryTypeToShortStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the BoundaryType enum...
        private List<string> _boundary_type_strings = new List<string>()
                                {
                                    "zero flux",
                                    "toroidal"
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _boundary_type_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _boundary_type_strings.FindIndex(item => item == str);
            return (BoundaryType)Enum.ToObject(typeof(BoundaryType), (int)idx);
        }
    }
    
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
    
    //skg daphne
    public class ConfigMolecule 
    {
        public string molecule_guid { get; set; }
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
        public bool   ReadOnly { get; set; }
        public MoleculeLocation molecule_location { get; set; }

        public ConfigMolecule(string thisName, double thisMW, double thisEffRad, double thisDiffCoeff)
        {
            Guid id = Guid.NewGuid();
            molecule_guid = id.ToString();
            Name = thisName;
            MolecularWeight = thisMW;
            EffectiveRadius = thisEffRad;
            DiffusionCoefficient = thisDiffCoeff;
            ReadOnly = true;
            molecule_location = MoleculeLocation.Bulk;
        }

        public ConfigMolecule()
            : base()
        {
            Guid id = Guid.NewGuid();
            molecule_guid = id.ToString();
            Name = "Molecule_New001"; // +"_" + DateTime.Now.ToString("hhmmssffff");
            MolecularWeight = 1.0;
            EffectiveRadius = 5.0;
            DiffusionCoefficient = 2;
            ReadOnly = true;
            molecule_location = MoleculeLocation.Bulk;
        }

        public string GenerateNewName(SimConfiguration sc, string ending)
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
            while (FindMoleculeByName(sc, TempMolName) == true)
            {
                nSuffix++;
                suffix = ending + string.Format("{0:000}", nSuffix);
                TempMolName = OriginalName + suffix;
            }

            return TempMolName;
        }
       
        public ConfigMolecule Clone(SimConfiguration sc)
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);
            ConfigMolecule newmol = JsonConvert.DeserializeObject<ConfigMolecule>(jsonSpec, Settings);
            Guid id = Guid.NewGuid();
            newmol.molecule_guid = id.ToString();
            newmol.ReadOnly = false;
            newmol.Name = newmol.GenerateNewName(sc, "_Copy");
            
            return newmol;
        }        

        public static bool FindMoleculeByName(SimConfiguration sc, string tempMolName)
        {
            bool ret = false;
            foreach (ConfigMolecule mol in sc.entity_repository.molecules)
            {
                if (mol.Name == tempMolName)
                {
                    ret = true;
                    break;
                }
            }

            return ret;
        }

        public void ValidateName(SimConfiguration sc)
        {
            bool found = false;
            string tempMolName = Name;
            foreach (ConfigMolecule mol in sc.entity_repository.molecules)
            {
                if (mol.Name == tempMolName && mol.molecule_guid != molecule_guid)
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                Name = GenerateNewName(sc, "_Ch");
            }
        }

    }


    //  -----------------------------------------------------------------------
    //  Differentiation Schemes
    //

    /// <summary>
    /// Any molecule can be a gene
    /// </summary>
    public class ConfigGene
    {
        public string gene_guid { get; set; }
        public string Name { get; set; }
        public int CopyNumber { get; set; }
        public double ActivationLevel { get; set; }

        //public ConfigGene()
        //{
        ////    Guid id = Guid.NewGuid();
        //    gene_guid = id.ToString();
        //}

        public ConfigGene(string name, int copynum, double actlevel)
        {
            Guid id = Guid.NewGuid();
            gene_guid = id.ToString();

            Name = name;
            CopyNumber = copynum;
            ActivationLevel = actlevel;
        }

    }
    
    public class ConfigTransitionDriverElement
    {
        public string driver_element_guid { get; set; }
        public double Alpha { get; set; }
        public double Beta { get; set; }
        public string  driver_mol_guid_ref { get; set; }

        public int CurrentState { get; set; }
        public string CurrentStateName { get; set; }
        public int DestState { get; set; }
        public string DestStateName { get; set; }

        public ConfigTransitionDriverElement()
        {
            Guid id = Guid.NewGuid();
            driver_element_guid = id.ToString();
        }
    }

    public class ConfigTransitionDriver
    {
        public string driver_guid { get; set; }
        public int CurrentState { get; set; }
        public string StateName { get; set; }
        
        //public ObservableCollection<ConfigTransitionDriverElement> DriverElements { get; set; }
        public ObservableCollection<ObservableCollection<ConfigTransitionDriverElement>> DriverElements { get; set; }
        //public ObservableCollection<string> states;

        public ConfigTransitionDriver()
        {
            Guid id = Guid.NewGuid();
            driver_guid = id.ToString();
            //DriverElements = new ObservableCollection<ConfigTransitionDriverElement>();
            DriverElements = new ObservableCollection<ObservableCollection<ConfigTransitionDriverElement>>();
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

    public class ConfigDiffScheme
    {
        public string diff_scheme_guid { get; set; }
        public string Name { get; set; }

        //For regulators
        public ConfigTransitionDriver Driver { get; set; }
   
        //For epigenetic map
        //TBD

        public ConfigDiffScheme()
        {
            Guid id = Guid.NewGuid();
            diff_scheme_guid = id.ToString();
        }
    }

    //end of Diff Scheme classes --------------------------------------------------------------




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
        private string _molecule_guid_ref;
        public string molecule_guid_ref 
        {
            get
            {
                return _molecule_guid_ref;
            }
            set
            {
                _molecule_guid_ref = value;
            }

        }  // the molecule_guid of the molecule this mp contains
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
        private MolPopInfo _mp_Info;
        public MolPopInfo mpInfo
        {
            get { return _mp_Info; }
            set { _mp_Info = value; }
        }

        private ReportMP reportMP;
        public ReportMP report_mp
        {
            get { return reportMP; }
            set { reportMP = value; }
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
        }

    }

    public class ConfigCompartment : EntityModelBase
    {
        // private to simConfig; see comment in EntityRepository
        public ObservableCollection<ConfigMolecularPopulation> molpops { get; set; }
        public Dictionary<string, ConfigMolecularPopulation> molpops_dict;      //IS THIS NEEDED??
        private ObservableCollection<string> _reactions_guid_ref;
        public ObservableCollection<string> reactions_guid_ref
        {
            get { return _reactions_guid_ref; }
            set
            {
                if (_reactions_guid_ref == value)
                    return;
                else
                {
                    _reactions_guid_ref = value;
                    OnPropertyChanged("reactions_guid_ref");
                }
            }
        }
        public ObservableCollection<string> reaction_complexes_guid_ref { get; set; }

        public ConfigCompartment()
        {
            molpops = new ObservableCollection<ConfigMolecularPopulation>();
            reactions_guid_ref = new ObservableCollection<string>();
            reaction_complexes_guid_ref = new ObservableCollection<string>();
            molpops_dict = new Dictionary<string, ConfigMolecularPopulation>();

            molpops.CollectionChanged += new NotifyCollectionChangedEventHandler(molpops_CollectionChanged);
            reactions_guid_ref.CollectionChanged += new NotifyCollectionChangedEventHandler(reactions_guid_ref_CollectionChanged);
        }

        private void reactions_guid_ref_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            
        }

        private void molpops_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //if (e.Action == NotifyCollectionChangedAction.Add)
            //{
            //    foreach (var nn in e.NewItems)
            //    {
            //    }
            //}
            //else 
            //if (e.Action == NotifyCollectionChangedAction.Remove)
            //{
            //    foreach (var oo in e.OldItems)
            //    {
            //        ConfigMolecularPopulation cmp = oo as ConfigMolecularPopulation;
            //        if (entity_repository.reactions_dict(reactions_guid_ref).)
            //        {
            //            reactions_guid_ref.Remove(cmp.molecule_guid_ref);
            //        }
            //    }
            //}

            OnPropertyChanged("molpops");            
        }


        //Return true if this compartment has a molecular population with given molecule
        public bool HasMolecule(ConfigMolecule mol)
        {
            bool res = false;
            foreach (ConfigMolecularPopulation molpop in molpops)
            {
                if (molpop.molecule_guid_ref == mol.molecule_guid)
                {
                    return true;
                }
            }
            return res;
        }

        //Return true if this compartment has a molecular population with given molecule guid
        public bool HasMolecule(string molguid)
        {
            bool res = false;
            foreach (ConfigMolecularPopulation molpop in molpops)
            {
                if (molpop.molecule_guid_ref == molguid)
                {
                    return true;
                }
            }
            return res;
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
                if (molecule_guid == cmp.molecule_guid_ref)
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
        BoundaryDissociation, Generalized, BoundaryTransportFrom, BoundaryTransportTo  
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
                                    "BoundaryTransportFrom"
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

    public class ConfigReaction : EntityModelBase
    {
        public ConfigReaction()
        {
            Guid id = Guid.NewGuid();
            reaction_guid = id.ToString();

            daph_rate_const = new DaphneDouble();
            rate_const = 0;
            ReadOnly = true;

            reactants_molecule_guid_ref = new ObservableCollection<string>();
            products_molecule_guid_ref = new ObservableCollection<string>();
            modifiers_molecule_guid_ref = new ObservableCollection<string>();
        }

        public ConfigReaction(ConfigReaction reac)
        {
            Guid id = Guid.NewGuid();
            reaction_guid = id.ToString();
            reaction_template_guid_ref = reac.reaction_template_guid_ref;

            daph_rate_const = new DaphneDouble();
            rate_const = reac.rate_const;
            ReadOnly = false;

            reactants_molecule_guid_ref = new ObservableCollection<string>();
            products_molecule_guid_ref = new ObservableCollection<string>();
            modifiers_molecule_guid_ref = new ObservableCollection<string>();

            reactants_molecule_guid_ref = reac.reactants_molecule_guid_ref;
            products_molecule_guid_ref = reac.products_molecule_guid_ref;
            modifiers_molecule_guid_ref = reac.modifiers_molecule_guid_ref;
        }


        public void GetTotalReactionString(EntityRepository repos)
        {
            string s = "";
            int i = 0;

            foreach (string mol_guid_ref in reactants_molecule_guid_ref)
            {
                ConfigMolecule cm = repos.molecules_dict[mol_guid_ref];

                //stoichiometry
                int n = repos.reaction_templates_dict[reaction_template_guid_ref].reactants_stoichiometric_const[i];
                i++;
                if (n > 1)
                    s += n;
                s += cm.Name;
                s += " + ";
            }
            i = 0;
            foreach (string mol_guid_ref in modifiers_molecule_guid_ref)
            {
                ConfigMolecule cm = repos.molecules_dict[mol_guid_ref];

                //stoichiometry??
                int n = repos.reaction_templates_dict[reaction_template_guid_ref].modifiers_stoichiometric_const[i];
                i++;
                if (n > 1)
                    s += n;

                s += cm.Name;
                s += " + ";
            }

            char[] trimChars = { ' ', '+' };
            s = s.Trim(trimChars);

            s = s + " -> ";

            i = 0;
            foreach (string mol_guid_ref in products_molecule_guid_ref)
            {
                ConfigMolecule cm = repos.molecules_dict[mol_guid_ref];

                //stoichiometry??
                int n = repos.reaction_templates_dict[reaction_template_guid_ref].products_stoichiometric_const[i];
                i++;
                if (n > 1)
                    s += n;

                s += cm.Name;
                s += " + ";
            }
            i = 0;
            foreach (string mol_guid_ref in modifiers_molecule_guid_ref)
            {
                ConfigMolecule cm = repos.molecules_dict[mol_guid_ref];

                //stoichiometry??
                int n = repos.reaction_templates_dict[reaction_template_guid_ref].modifiers_stoichiometric_const[i];
                i++;
                if (n > 1)
                    s += n;

                s += cm.Name;
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
                if (repos.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                    return true;
            }

            return false;
        }
        public string reaction_guid { get; set; }
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
                daph_rate_const.Value = value;                
            } 
        }

        ////////////////////////
        // 1/15/2014 Turn on Json Ignore to flush out of scenario files. 
        // Not used anymore.
        // At some point in the future we can remove these.
        [JsonIgnore]
        private DaphneDouble _daph_rate_const;
        [JsonIgnore]
        public DaphneDouble daph_rate_const
        {
            get
            {
                return _daph_rate_const;
            }

            set
            {
                _daph_rate_const = value;
                OnPropertyChanged("daph_rate_const");
            }
        }
        //////////////////////////
        
        public bool ReadOnly { get; set; }
        // hold the molecule_guid_refs of the {reactant|product|modifier} molpops
        public ObservableCollection<string> reactants_molecule_guid_ref;
        public ObservableCollection<string> products_molecule_guid_ref;
        public ObservableCollection<string> modifiers_molecule_guid_ref;

        public string TotalReactionString { get; set; }

    }

    public class ConfigReactionTemplate
    {
        public string reaction_template_guid;
        public string name;
        // stoichiometric constants
        public ObservableCollection<int> reactants_stoichiometric_const;
        public ObservableCollection<int> products_stoichiometric_const;
        public ObservableCollection<int> modifiers_stoichiometric_const;      
        //reaction type
        public ReactionType reac_type { get; set; }
        // True if the reaction involves bulk and boundary molecules. Default is false.
        public bool isBoundary;

        public ConfigReactionTemplate()
        {
            Guid id = Guid.NewGuid();
            reaction_template_guid = id.ToString();
            reactants_stoichiometric_const = new ObservableCollection<int>();
            products_stoichiometric_const = new ObservableCollection<int>();
            modifiers_stoichiometric_const = new ObservableCollection<int>();
            isBoundary = false;
        }
    }

    public class ConfigReactionGuidRatePair : EntityModelBase
    {
        public string Guid { get; set; }

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

        [JsonIgnore]
        private DaphneDouble originalRate2;
        [JsonIgnore]
        public DaphneDouble OriginalRate2
        {
            get
            {
                return originalRate2;
            }
            set
            {
                
                    originalRate2 = value;
                    OnPropertyChanged("OriginalRate2");
                
            }
        }

        [JsonIgnore]
        private DaphneDouble reactionComplexRate2;
        //[JsonIgnore]
        public DaphneDouble ReactionComplexRate2
        {
            get
            {
                return reactionComplexRate2;
            }
            set
            {
                
                    reactionComplexRate2 = value;
                    OnPropertyChanged("ReactionComplexRate2");
               
            }
        }

        public ConfigReactionGuidRatePair()
        {
            OriginalRate2 = new DaphneDouble();
            ReactionComplexRate2 = new DaphneDouble();
        }
    }

    public class ConfigReactionComplex : EntityModelBase
    {
        public string Name { get; set; }
        public string reaction_complex_guid { get; set; }

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
        public bool ReadOnly { get; set; }

        public ObservableCollection<ConfigReactionGuidRatePair> ReactionRates { get; set; } 

        public ConfigReactionComplex()
        {
            Guid id = Guid.NewGuid();
            reaction_complex_guid = id.ToString();
            Name = "NewRC";
            reactions_guid_ref = new ObservableCollection<string>();
            molpops = new ObservableCollection<ConfigMolecularPopulation>();
            ReadOnly = true;
        }
        public ConfigReactionComplex(string name)
        {
            Guid id = Guid.NewGuid();
            reaction_complex_guid = id.ToString();
            Name = name;
            ReadOnly = true;
            reactions_guid_ref = new ObservableCollection<string>();
            molpops = new ObservableCollection<ConfigMolecularPopulation>();
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
            newrc.reaction_complex_guid = id.ToString();
            newrc.ReadOnly = false;
            newrc.Name = "NewRC";

            return newrc;
        }

        private bool HasMolecule(string guid) 
        {
            foreach (ConfigMolecularPopulation molpop in molpops)
            {
                if (molpop.molecule_guid_ref == guid)
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
                ConfigMolecule configMolecule = er.molecules_dict[molguid];
                if (configMolecule != null)
                {
                    ConfigMolecularPopulation configMolPop = new ConfigMolecularPopulation(ReportType.CELL_MP);
                    configMolPop.molecule_guid_ref = configMolecule.molecule_guid;
                    configMolPop.mpInfo = new MolPopInfo(configMolecule.Name);
                    configMolPop.Name = configMolecule.Name;
                    configMolPop.mpInfo.mp_dist_name = "Uniform";
                    configMolPop.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                    configMolPop.mpInfo.mp_render_blending_weight = 2.0;
                    MolPopHomogeneousLevel hl = new MolPopHomogeneousLevel();
                    hl.concentration = 1;
                    configMolPop.mpInfo.mp_distribution = hl;
                    if (HasMolecule(molguid) == false)
                    {
                        molpops.Add(configMolPop);
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

    public class ConfigCell
    {
        public ConfigCell()
        {
            CellName = "Default Cell";
            CellRadius = 5.0;

            Guid id = Guid.NewGuid();
            cell_guid = id.ToString();

            membrane = new ConfigCompartment();
            cytosol = new ConfigCompartment();
            locomotor_mol_guid_ref = "";
            ReadOnly = true;

            genes_guid_ref = new ObservableCollection<string>();

            diff_scheme_guid_ref = new ObservableCollection<string>();
        }

        public ConfigCell Clone()
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);
            ConfigCell newcell = JsonConvert.DeserializeObject<ConfigCell>(jsonSpec, Settings);
            Guid id = Guid.NewGuid();
            newcell.cell_guid = id.ToString();
            newcell.ReadOnly = false;
            return newcell;
        }

        public string CellName { get; set; }
        public double CellRadius { get; set; }
        public string signaling_mol_guid_ref { get; set; }
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


        public double TransductionConstant { get; set; }
        public double DragCoefficient { get; set; }
        public string cell_guid { get; set; }
        public bool ReadOnly { get; set; }

        public ConfigCompartment membrane { get; set; }
        public ConfigCompartment cytosol { get; set; }
        
        //FOR NOW, THIS IS HERE. MAYBE THER IS A BETTER PLACE FOR IT
        public ObservableCollection<string> genes_guid_ref { get; set; }
        public ObservableCollection<string> diff_scheme_guid_ref { get; set; }
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

    [ValueConversion(typeof(CellPopDistributionType), typeof(bool))]
    public class CellPopDistributionTypeToBoolConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the GlobalParameterType enum...
        
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            bool retval = true;
            try
            {
                if ((int)value == 0)
                    retval = true;
                else
                    retval = false;
            }
            catch
            {
                return true;
            }
            return retval;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            return str;
            //int idx = _cell_pop_dist_type_strings.FindIndex(item => item == str);
            //return (CellPopDistributionType)Enum.ToObject(typeof(CellPopDistributionType), (int)idx);
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
                wallDis = Math.Sqrt(minDisSquared);
            }
        }

        public CellPopDistribution(double[] _extents, double _minDisSquared, CellPopulation _cellPop)
        {
            cellStates = new ObservableCollection<CellState>();
            extents = (double[])_extents.Clone();
            MinDisSquared = _minDisSquared;

            // null case when deserializing Json
            // correct CellPopulation pointer added in SimConfiguration.InitCellPopulationIDCellPopulationDict
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
                    tries++;
                    if (tries > maxTry)
                    {
                        // Avoid infinite loops. Excessive iterations may indicate the cells density is too high.
                        System.Windows.MessageBox.Show("Exceeded max iterations for cell placement. Reduce cell density.");
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
        public void setState(SpatialState state)
        {
            List<double> tmp = new List<double>(SpatialState.Dim);
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
        public string cell_guid_ref { get; set; }
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
        public string cell_subset_guid_ref { get; set; }

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
            cell_subset_guid_ref = "";
            number = 1;
            cellpopulation_constrained_to_region = false;
            cellpopulation_region_guid_ref = "";
            wrt_region = RelativePosition.Inside;
            cellpopulation_color = new System.Windows.Media.Color();
            cellpopulation_render_on = true;
            cellpopulation_color = System.Windows.Media.Color.FromRgb(255, 255, 255);
            cellpopulation_predef_color = ColorList.Orange;
            cellpopulation_id = SimConfiguration.SafeCellPopulationID++;
            // reporting
            reportXVF = new ReportXVF();
            ecmProbe = new ObservableCollection<ReportECM>();
            ecm_probe_dict = new Dictionary<string, ReportECM>();
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
            set {
                _mp_render_on = value;                
                OnPropertyChanged("mp_render_on");
            }
        }

        public MolPopInfo()
        {            
        }

        public MolPopInfo(string name)
        {
            Guid id = Guid.NewGuid();
            mp_guid = id.ToString();
            mp_dist_name = name;
            // Default is static homogeneous level
            mp_distribution = new MolPopHomogeneousLevel();
            mp_amplitude_keyframes = new ObservableCollection<TimeAmpPair>();
            mp_color = new System.Windows.Media.Color();
            mp_color = System.Windows.Media.Color.FromRgb(255, 255, 255);
            mp_render_blending_weight = 1.0;
            mp_render_on = true;
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
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigMolecule> mol_list = cvs.Source as ObservableCollection<ConfigMolecule>;
            if (mol_list != null)
            {
                foreach (ConfigMolecule mol in mol_list)
                {
                    if (mol.molecule_guid == guid)
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
    [ValueConversion(typeof(string), typeof(int))]
    public class MolGUIDtoMolIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;
            int nIndex = -1;
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigMolecule> mol_list = cvs.Source as ObservableCollection<ConfigMolecule>;
            if (mol_list != null)
            {
                int i = 0;
                foreach (ConfigMolecule mol in mol_list)
                {
                    
                    if (mol.molecule_guid == guid)
                    {
                        nIndex = i;
                        break;
                    }
                    i++;
                }
            }
            return nIndex;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            int index = (int)value;
            string ret = "not found";
            
            return ret;
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
                    if (gene.gene_guid == guid)
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

            if (guid == null || guid.Length == 0)
                return null;

            ConfigGene thisGene = null;
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigGene> gene_list = cvs.Source as ObservableCollection<ConfigGene>;
            if (gene_list != null)
            {
                foreach (ConfigGene gene in gene_list)
                {
                    if (gene.gene_guid == guid)
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
                    if (cel.cell_guid == guid)
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
                    if (cel.cell_guid == guid)
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
                    if (cel.cell_guid == guid)
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
    /// Converter to go between cell pop and cell reaction strings
    /// </summary>
    [ValueConversion(typeof(ConfigCell), typeof(ObservableCollection<string>))]
    public class CellGuidToCellReactionsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ConfigCell cc = value as ConfigCell;

            if (cc == null)
                return null;

            ObservableCollection<string> reacStrings = new ObservableCollection<string>();            

            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigReaction> reac_list = cvs.Source as ObservableCollection<ConfigReaction>;
            if (reac_list != null)
            {
                foreach (ConfigReaction reac in reac_list)
                {
                    foreach (string rguid in cc.membrane.reactions_guid_ref) 
                    {
                        if (reac.reaction_guid == rguid) {
                            reacStrings.Add("membrane: " + reac.TotalReactionString);
                        }
                    }
                    foreach (string rguid in cc.cytosol.reactions_guid_ref)
                    {
                        if (reac.reaction_guid == rguid)
                        {
                            reacStrings.Add("cytosol: " + reac.TotalReactionString);
                        }
                    }
                }
            }

            if (reacStrings.Count == 0)
                reacStrings.Add("No reactions in this cell.");

            return reacStrings;
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
                    if (cr.reaction_guid == guid)
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
                    if (crc.reaction_complex_guid == guid)
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

            if (guid == null)
                return null;

            ConfigReactionComplex rcReturn = null;
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<ConfigReactionComplex> rc_list = cvs.Source as ObservableCollection<ConfigReactionComplex>;
            if (rc_list != null)
            {
                foreach (ConfigReactionComplex crc in rc_list)
                {
                    if (crc.reaction_complex_guid == guid)
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

    [ValueConversion(typeof(string), typeof(bool))]
    public class IsNullConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (value == null);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new InvalidOperationException("IsNullConverter can only be used OneWay.");
        }
    }

    // Base class for homog, linear, gauss distributions
    [XmlInclude(typeof(MolPopHomogeneousLevel)),
     XmlInclude(typeof(MolPopLinear)),
     XmlInclude(typeof(MolPopGaussian))]
     //XmlInclude(typeof(MolPopCustom))]
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
