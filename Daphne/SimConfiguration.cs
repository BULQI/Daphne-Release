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
        public Scenario rc_scenario { get; set; }
        public EntityRepository entity_repository { get; set; }
        public SimulationParams sim_params { get; set; }

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

            ////LoadDefaultGlobalParameters();
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
            }
            scenario.cellpopulations.CollectionChanged += new NotifyCollectionChangedEventHandler(cellsets_CollectionChanged);
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

        private void molecules_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
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
                    entity_repository.molecules_dict.Remove(cm.molecule_guid);
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
                    entity_repository.cells_dict.Remove(cc.cell_guid);
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
                    entity_repository.reactions_dict.Remove(cr.reaction_guid);
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
    }

    public class SimulationParams
    {
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
        public ObservableCollection<ConfigReaction> reactions { get; set; }

        public ObservableCollection<ConfigReactionTemplate> reaction_templates { get; set; }

        public Dictionary<string, ConfigMolecule> molecules_dict; // keyed by molecule_guid
        public Dictionary<string, ConfigReactionTemplate> reaction_templates_dict;
        public Dictionary<string, ConfigReaction> reactions_dict;
        public Dictionary<string, ConfigCell> cells_dict;
        public Dictionary<string, ConfigReactionComplex> reaction_complexes_dict;
        public Dictionary<string, GaussianSpecification> gauss_guid_gauss_dict;

        public EntityRepository()
        {
            gaussian_specifications = new ObservableCollection<GaussianSpecification>();
            box_specifications = new ObservableCollection<BoxSpecification>();
            cells = new ObservableCollection<ConfigCell>();
            molecules = new ObservableCollection<ConfigMolecule>();
            reactions = new ObservableCollection<ConfigReaction>();
            reaction_templates = new ObservableCollection<ConfigReactionTemplate>();
            molecules_dict = new Dictionary<string, ConfigMolecule>();
            reaction_templates_dict = new Dictionary<string, ConfigReactionTemplate>();
            reactions_dict = new Dictionary<string, ConfigReaction>();
            cells_dict = new Dictionary<string, ConfigCell>();
            reaction_complexes = new ObservableCollection<ConfigReactionComplex>();
            reaction_complexes_dict = new Dictionary<string, ConfigReactionComplex>();
            gauss_guid_gauss_dict = new Dictionary<string, GaussianSpecification>();
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

        public bool toroidal { get; set; }

        

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

    public enum BoundaryType { NoFlux = 0, Toroidal }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(BoundaryType), typeof(string))]
    public class BoundaryTypeToShortStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the MoleculeLocation enum...
        private List<string> _boundary_type_strings = new List<string>()
                                {
                                    "no flux",
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

    public enum BoundaryFace {None=0, X, Y, Z }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(BoundaryType), typeof(string))]
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
            Name = "NewMolecule"; // +"_" + DateTime.Now.ToString("hhmmssffff");
            MolecularWeight = 1.0;
            EffectiveRadius = 5.0;
            DiffusionCoefficient = 2;
            ReadOnly = true;
            molecule_location = MoleculeLocation.Bulk;
        }

        public ConfigMolecule(ConfigMolecule gm)
        {
            Guid id = Guid.NewGuid();
            molecule_guid = id.ToString();
            Name = "CopyOf_" + gm.Name; // +"_" + DateTime.Now.ToString("hhmmssffff");
            MolecularWeight = gm.MolecularWeight;
            EffectiveRadius = gm.EffectiveRadius;
            DiffusionCoefficient = gm.DiffusionCoefficient;
            ReadOnly = gm.ReadOnly;
            molecule_location = gm.molecule_location;
        }

        public ConfigMolecule Clone()
        {
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(this, Newtonsoft.Json.Formatting.Indented, Settings);
            ConfigMolecule newmol = JsonConvert.DeserializeObject<ConfigMolecule>(jsonSpec, Settings);
            Guid id = Guid.NewGuid();
            newmol.molecule_guid = id.ToString();
            newmol.ReadOnly = false;
            return newmol;
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
        public MolBoundaryType boundaryType;
        public Boundary boundary;
        public double val;

        public BoundaryCondition(MolBoundaryType _boundaryType, Boundary _boundary)
        {
            boundaryType = _boundaryType;
            boundary = _boundary;
        }
    }
    public class ConfigMolecularPopulation //: EntityModelBase
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
        public string Name { get; set; }
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

        public List<BoundaryCondition> boundaryCondition { get; set; }
        private double _boundaryVal1;
        public double boundaryVal1 
        { 
            get
            {
                return _boundaryVal1;
            }
            set
            {
                _boundaryVal1 = value;
                if (_boundary_face != BoundaryFace.None)
                {
                    boundaryCondition[0].val = value;
                    // If linear is already set, then c1 needs to be updated here.
                    if (mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Linear)
                    {
                        MolPopLinear mpl = mpInfo.mp_distribution as MolPopLinear;
                        mpl.c1 = value;
                    }
                }
            }
        }
        private double _boundaryVal2;
        public double boundaryVal2
        {
            get
            {
                return _boundaryVal2;
            }
            set
            {
                _boundaryVal2 = value;
                if (_boundary_face != BoundaryFace.None)
                {
                    boundaryCondition[1].val = value;
                    // If linear is already set, then c2 needs to be updated here.
                    if (mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Linear)
                    {
                        MolPopLinear mpl = mpInfo.mp_distribution as MolPopLinear;
                        mpl.c2 = value;
                    }
                }
            }
        }
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

                // NOTE: Future implementations may allow both Neumann and Dirichlet at the same face
                // or additional faces to be added. Until then...

                if (_boundary_face == BoundaryFace.None)
                {
                    // Clear boundaryCondition list.
                    boundaryCondition = new List<BoundaryCondition>();
                }
                else
                {
                    if (boundaryCondition.Count == 0)
                    {
                        // Neumann not implemented yet. 
                        MolBoundaryType molBoundType = MolBoundaryType.Dirichlet;
                        switch (value)
                        {
                            case BoundaryFace.X:
                                boundaryCondition.Add(new BoundaryCondition(molBoundType, Boundary.left));
                                boundaryCondition.Add(new BoundaryCondition(molBoundType, Boundary.right));
                                break;
                            case BoundaryFace.Y:
                                boundaryCondition.Add(new BoundaryCondition(molBoundType, Boundary.bottom));
                                boundaryCondition.Add(new BoundaryCondition(molBoundType, Boundary.top));
                                break;
                            case BoundaryFace.Z:
                                boundaryCondition.Add(new BoundaryCondition(molBoundType, Boundary.back));
                                boundaryCondition.Add(new BoundaryCondition(molBoundType, Boundary.front));
                                break;
                            default:
                                throw new Exception("Improper boundary face selection");
                        }
                    }
                    else
                    {
                        switch (value)
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
                            default:
                                throw new Exception("Improper boundary face selection");
                        }
                    }

                    // If linear is already set, then dim needs to be updated here.
                    if (mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Linear)
                    {
                        MolPopLinear mpl = mpInfo.mp_distribution as MolPopLinear;
                        mpl.dim = (int)_boundary_face - 1;
                    }
                }
            }
        }
                    
        public ConfigMolecularPopulation(ReportType rt)
        {
            Guid id = Guid.NewGuid();

            molpop_guid = id.ToString();
            boundary_face = BoundaryFace.None;
            boundaryCondition = new List<BoundaryCondition>();

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
        public Dictionary<string, ConfigMolecularPopulation> molpops_dict;
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

    public class ConfigReaction
    {
        public ConfigReaction()
        {
            Guid id = Guid.NewGuid();
            reaction_guid = id.ToString();

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

            rate_const = reac.rate_const;
            ReadOnly =false;

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

        public string reaction_guid { get; set; }
        public string reaction_template_guid_ref { get; set; }
        public double rate_const { get; set; }
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

        public ConfigReactionTemplate()
        {
            Guid id = Guid.NewGuid();
            reaction_template_guid = id.ToString();
            reactants_stoichiometric_const = new ObservableCollection<int>();
            products_stoichiometric_const = new ObservableCollection<int>();
            modifiers_stoichiometric_const = new ObservableCollection<int>();
        }
    }

    public class ConfigReactionComplex
    {
        public string Name { get; set; }
        public string reaction_complex_guid { get; set; }
        public ObservableCollection<string> reactions_guid_ref { get; set; }
        public ObservableCollection<ConfigMolecularPopulation> molpops { get; set; }
        public bool ReadOnly { get; set; }

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
        }
        public ConfigReactionComplex(ConfigReactionComplex src)
        {
            Guid id = Guid.NewGuid();
            reaction_complex_guid = id.ToString();
            Name = "NewRC";
            ReadOnly = false;
            reactions_guid_ref = new ObservableCollection<string>();
            reactions_guid_ref = src.reactions_guid_ref;
            molpops = new ObservableCollection<ConfigMolecularPopulation>();
            molpops = src.molpops;
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
        public string locomotor_mol_guid_ref { get; set; }
        public double TransductionConstant { get; set; }
        public double DragCoefficient { get; set; }
        public string cell_guid { get; set; }
        public bool ReadOnly { get; set; }

        public ConfigCompartment membrane { get; set; }
        public ConfigCompartment cytosol { get; set; }
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
        //We should use these variables instead of the box spec, but for now, hold off.
        //private double[] center;
        //private double[] sigma;
        //private double peak;
        //public double[] Center { get; set; }
        //public double[] Sigma { get; set; }
        //public double   Peak { get; set; }

        public CellPopProbDistributionType DistType { get; set; }

        public CellPopGaussianDistribution()
        {
            cpDistributionType = CellPopDistributionType.Probability;
            DistType = CellPopProbDistributionType.Gaussian;
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
            get { return ConfigState[0]; }
            set { ConfigState[0] = value; }
        }

        [JsonIgnore]
        public double Y 
        {
            get { return ConfigState[1]; }
            set { ConfigState[1] = value; }
        }

        [JsonIgnore]
        public double Z 
        {
            get { return ConfigState[2]; }
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


        //map conctration info into molpop info.
        public Dictionary<string, double[]> configMolPop = new Dictionary<string, double[]>();
        public void setState(SpatialState state)
        {
            List<double> tmp = new List<double>(9);
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

    public class CellPopulation // : EntityModelBase
    {
        public string cell_guid_ref { get; set; }
        public string cellpopulation_name { get; set; }
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
                    //OnPropertyChanged("number");
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
        public System.Windows.Media.Color cellpopulation_color { get; set; }
        public CellPopDistribution cellPopDist { get; set; }

        private ObservableCollection<CellState> _cell_list;
        public ObservableCollection<CellState> cell_list
        {
            get { return _cell_list; }
            set
            {
                _cell_list = value;
                number = value == null ? 0 : _cell_list.Count;
                //OnPropertyChanged("cell_list");
            }
        } 

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

            cellPopDist = new CellPopSpecifyLocation();
            CellPopDistTypes = new ObservableCollection<CellPopDistType>();
            InitDistTypes();

            cell_list = new ObservableCollection<CellState>();

            // reporting
            reportXVF = new ReportXVF();
            ecmProbe = new ObservableCollection<ReportECM>();
            ecm_probe_dict = new Dictionary<string, ReportECM>();
        }

        //public ObservableCollection<ConfigMolecularPopulation> GetCellMembraneMols()
        //{
        //    ConfigCell cc = entity_repository.cells[cell_guid_ref];
        //    return cc.membrane.molpops;
        //}
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
            ////if (index >= 0)
            ////{
            ////    System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ////    ObservableCollection<ConfigMolecule> mol_list = cvs.Source as ObservableCollection<ConfigMolecule>;
            ////    if (mol_list != null)
            ////    {
            ////        ret = mol_list[index].molecule_guid;
            ////    }
            ////}
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
                            reac_string = cr.rate_const.ToString();
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

    public enum MolPopDistributionType { Homogeneous, Linear, Gaussian, Custom, Explicit }

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
                                    "Custom",
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
     XmlInclude(typeof(MolPopGaussian)),
     XmlInclude(typeof(MolPopCustom))]
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

    public class MolPopLinear : MolPopDistribution
    {
        public double c1 { get; set; }
        public double c2 { get; set; }
        public double x1 { get; set; }
        public int dim { get; set; }
        
        public MolPopLinear()
        {
            mp_distribution_type = MolPopDistributionType.Linear;
            x1 = 0;
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
            peak_concentration = 100.0;
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

    public class MolPopCustom : MolPopDistribution
    {
        private Uri _custom_gradient_file_uri = new Uri("c:\\temp2"/*DaphneGui.MainWindow.appPath*/);
        private string _custom_gradient_file_string = "c:\\temp2"; //DaphneGui.MainWindow.appPath;

        public MolPopCustom()
        {
            mp_distribution_type = MolPopDistributionType.Custom;
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
                    //OnPropertyChanged("custom_gradient_file_uri");
                    //OnPropertyChanged("custom_gradient_file_string");
                    //OnPropertyChanged("custom_gradient_file_name");
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
                    //OnPropertyChanged("custom_gradient_file_uri");
                    //OnPropertyChanged("custom_gradient_file_string");
                    //OnPropertyChanged("custom_gradient_file_name");
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
        private bool _blob_visibility = true;
        
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

    ////public enum CellPopDistributionType { Uniform, Gaussian }

    /////// <summary>
    /////// Converter to go between enum values and "human readable" strings for GUI
    /////// </summary>
    ////[ValueConversion(typeof(CellPopDistributionType), typeof(string))]
    ////public class CellPopDistributionTypeToStringConverter : IValueConverter
    ////{
    ////    // NOTE: This method is a bit fragile since the list of strings needs to 
    ////    // correspond in length and index with the GlobalParameterType enum...
    ////    private List<string> _cell_pop_dist_type_strings = new List<string>()
    ////                            {
    ////                                "Homogeneous",
    ////                                "Gaussian"
    ////                            };

    ////    public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    ////    {
    ////        try
    ////        {
    ////            return _cell_pop_dist_type_strings[(int)value];
    ////        }
    ////        catch
    ////        {
    ////            return "";
    ////        }
    ////    }

    ////    public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
    ////    {
    ////        string str = (string)value;
    ////        int idx = _cell_pop_dist_type_strings.FindIndex(item => item == str);
    ////        return (CellPopDistributionType)Enum.ToObject(typeof(CellPopDistributionType), (int)idx);
    ////    }
    ////}

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
