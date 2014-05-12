using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows.Data;
using System.Xml.Serialization;
using Daphne;
using Newtonsoft.Json;

using Workbench;

namespace DaphneGui
{
    public class SimConfigurator
    {
        public string FileName { get; set; }
        private XmlSerializer serializer = new XmlSerializer(typeof(SimConfiguration));
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
            //TextWriter myWriter = new StreamWriter(FileName);
            //serializer.Serialize(myWriter, SimConfig);
            //myWriter.Close();

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
            //StringWriter outStream = new StringWriter();
            //serializer.Serialize(outStream, SimConfig);
            //return outStream.ToString();

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

        //skg daphne changes
        //public void DeserializeSimConfig()
        //{
        //    FileStream myFileStream = new FileStream(FileName, FileMode.Open);
        //    SimConfig = (SimConfiguration)serializer.Deserialize(myFileStream);
        //    myFileStream.Close();
        //    SimConfig.InitializeStorageClasses();
        //}
        public void DeserializeSimConfig()
        {
            //Deserialize JSON - THIS CODE WORKS - PUT IT IN APPROPRIATE PLACE (INITIALSTATE OR SOMETHING) - REPLACE XML WITH THIS
            var settings = new JsonSerializerSettings();
            settings.TypeNameHandling = TypeNameHandling.Auto;
            string readText = File.ReadAllText(FileName);
            SimConfig = JsonConvert.DeserializeObject<SimConfiguration>(readText, settings);
            SimConfig.InitializeStorageClasses();
        }

        //skg daphne changes
        //public void DeserializeSimConfigFromString(string simConfigXML)
        //{
        //    StringReader configStringReader = new StringReader(simConfigXML);
        //    SimConfig = (SimConfiguration)serializer.Deserialize(configStringReader);
        //    configStringReader.Close();
        //    SimConfig.InitializeStorageClasses();
        //}
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
        public ObservableCollection<GlobalParameter> global_parameters { get; set; }
        public EntityRepository entity_repository { get; set; }

        //skg daphne Tuesday, April 16, 2013                             
        public ObservableCollection<GuiReactionTemplate>    PredefReactions { get; set; }
        public ObservableCollection<GuiMolecule>            PredefMolecules { get; set; }
        public ObservableCollection<GuiReactionComplex> PredefReactionComplexes { get; set; }

        [XmlIgnore]
        [JsonIgnore]
        public ChartViewToolWindow ChartWindow;

        // Convenience utility storage (not serialized)
        // NOTE: These could be moved to entity_repository...
        [XmlIgnore]
        public Dictionary<string, CellSubset> cellsubset_guid_cellsubset_dict;   
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
            global_parameters = new ObservableCollection<GlobalParameter>();
            entity_repository = new EntityRepository();

            //skg daphne
            PredefReactions = new ObservableCollection<GuiReactionTemplate>();
            PredefMolecules = new ObservableCollection<GuiMolecule>();
            PredefReactionComplexes = new ObservableCollection<GuiReactionComplex>();

            // Utility storage
            // NOTE: No use adding CollectionChanged event handlers here since it gets wiped out by deserialization anyway...
            cellsubset_guid_cellsubset_dict = new Dictionary<string, CellSubset>();   
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
            foreach (BoxSpecification bs in entity_repository.box_specifications)
            {
                // NOTE: Uncomment and add in code if really need to adjust directions independently for efficiency
                //if (e.PropertyName == "extent_x")
                //{
                //    // set x direction
                //}
                //if (e.PropertyName == "extent_y")
                //{
                //    // set x direction
                //}
                //if (e.PropertyName == "extent_z")
                //{
                //    // set x direction
                //}

                // For now just setting all every time...
                SetBoxSpecExtents(bs);
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

        public void LoadDefaultGlobalParameters()
        {
            var force_params = new ForceParams();
            global_parameters.Add(force_params);
            var locomotor_params = new LocomotorParams();
            global_parameters.Add(locomotor_params);
            var synapse_params = new SynapseParams();
            global_parameters.Add(synapse_params);

            //skg daphne
            string path = "Config\\DaphnePredefinedReactions.txt";
            string readText = File.ReadAllText(path);
            PredefReactions = JsonConvert.DeserializeObject<ObservableCollection<GuiReactionTemplate>>(readText);

            path = "Config\\DaphnePredefinedMolecules.txt";
            readText = File.ReadAllText(path);
            PredefMolecules = JsonConvert.DeserializeObject<ObservableCollection<GuiMolecule>>(readText);

            GuiReactionComplex rc = new GuiReactionComplex();
            rc.Name = "Bistable";
            rc.Reactions.Add(PredefReactions[5]);
            rc.Reactions.Add(PredefReactions[6]);
            rc.Reactions.Add(PredefReactions[7]);
            rc.Reactions.Add(PredefReactions[8]);
            PredefReactionComplexes.Add(rc);

            rc = new GuiReactionComplex();
            rc.Name = "Receptor/Ligand";
            rc.Reactions.Add(PredefReactions[0]);
            rc.Reactions.Add(PredefReactions[1]);
            rc.Reactions.Add(PredefReactions[2]);
            rc.Reactions.Add(PredefReactions[3]);
            PredefReactionComplexes.Add(rc);

        }

        /// <summary>
        /// CollectionChanged not called during deserialization, so manual call to set up utility classes.
        /// Also take care of any other post-deserialization setup.
        /// </summary>
        public void InitializeStorageClasses()
        {
            // GenerateNewExperimentGUID();
            FindNextSafeCellPopulationID();
            InitCellSubsetGuidCellSubsetDict();
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

        private void InitCellSubsetGuidCellSubsetDict()
        {
            cellsubset_guid_cellsubset_dict.Clear();
            foreach (CellSubset ct in entity_repository.cell_subsets)
            {
                cellsubset_guid_cellsubset_dict.Add(ct.cell_subset_guid, ct);
            }
            entity_repository.cell_subsets.CollectionChanged += new NotifyCollectionChangedEventHandler(cellsubsets_CollectionChanged); 
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

        // Keeping utility storage up to date when collections change
        private void cellsubsets_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                    CellSubset ct = nn as CellSubset;
                    cellsubset_guid_cellsubset_dict.Add(ct.cell_subset_guid, ct);   
                }
            }
            else if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (var dd in e.OldItems)
                {
                    CellSubset ct = dd as CellSubset;
                    cellsubset_guid_cellsubset_dict.Remove(ct.cell_subset_guid);   
                }
            }
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

        public GuiMolecule FindMolecule(string name)
        {
            GuiMolecule gm = null;

            foreach (GuiMolecule g in PredefMolecules)
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

    public class Scenario
    {
        public string description { get; set; }
        public TimeConfig time_config { get; set; }
        public Environment environment { get; set; }
        public ObservableCollection<Region> regions { get; set; }        
        public ObservableCollection<CellPopulation> cellpopulations { get; set; }

        //skg daphne       
        public ObservableCollection<GuiReactionTemplate> Reactions { get; set; }
        public ObservableCollection<GuiMolecularPopulation> MolPops { get; set; }
        public ObservableCollection<GuiReactionComplex> ReactionComplexes { get; set; }

        public Scenario()
        {
            description = "Scenario description";
            time_config = new TimeConfig();
            environment = new Environment();
            regions = new ObservableCollection<Region>();
            cellpopulations = new ObservableCollection<CellPopulation>();

            //skg daphne
            Reactions = new ObservableCollection<GuiReactionTemplate>();
            MolPops = new ObservableCollection<GuiMolecularPopulation>();
            ReactionComplexes = new ObservableCollection<GuiReactionComplex>();
        }
    }

    
    public class EntityRepository
    {
        public ObservableCollection<SolfacType> solfac_types { get; set; }
        public ObservableCollection<CellSubset> cell_subsets { get; set; }
        public ObservableCollection<GaussianSpecification> gaussian_specifications { get; set; }
        public ObservableCollection<BoxSpecification> box_specifications { get; set; }

        public EntityRepository()
        {
            solfac_types = new ObservableCollection<SolfacType>();
            cell_subsets = new ObservableCollection<CellSubset>();
            gaussian_specifications = new ObservableCollection<GaussianSpecification>();
            box_specifications = new ObservableCollection<BoxSpecification>();
        }

        /// <summary>
        /// NOTE: This version clears out all existing entries!!
        /// This needs to be called after all solfacs types have been added
        /// to the repository so each cell type gets a duplicate list of receptors
        /// and the correct length array of weights/expression levels.
        /// </summary>
        public void ResetCellTypesReceptorsLists()
        {
            foreach (CellSubset ct in cell_subsets)
            {
                //skg 5/31/12 changed                                
                if (ct.cell_subset_type is BCellSubsetType)
                {
                    //ct.cell_subset_type.cell_subset_type_receptor_params.Clear();
                    BCellSubsetType bcst = (BCellSubsetType)ct.cell_subset_type;
                    bcst.cell_subset_type_receptor_params.Clear();
                    foreach (SolfacType st in solfac_types)
                    {
                        //skg 5/31/12 changed
                        //ct.cell_subset_type.cell_subset_type_receptor_params.Add(new ReceptorParameters(st.solfac_type_guid));
                        bcst.cell_subset_type_receptor_params.Add(new ReceptorParameters(st.solfac_type_guid));
                    }
                }
                else if (ct.cell_subset_type is TCellSubsetType)
                {
                    TCellSubsetType tcst = (TCellSubsetType)ct.cell_subset_type;
                    tcst.cell_subset_type_receptor_params.Clear();
                    foreach (SolfacType st in solfac_types)
                    {
                        tcst.cell_subset_type_receptor_params.Add(new ReceptorParameters(st.solfac_type_guid));
                    }
                }
            }
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


    public class Environment : EntityModelBase
    {
        private int _extent_x;
        private int _extent_y;
        private int _extent_z;
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
        [XmlIgnore]
        public int extent_min { get; set; }
        [XmlIgnore]
        public int extent_max { get; set; }

        public Environment()
        {
            extent_x = 400;
            extent_y = 400;
            extent_z = 400;
            extent_min = 5;
            extent_max = 1000;
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

    //skg daphne
    public class GuiMolecule : EntityModelBase
    {
        public string Name { get; set; }
        public double MolecularWeight { get; set; }
        public double EffectiveRadius { get; set; }
        public double DiffusionCoefficient { get; set; }

        public GuiMolecule(string thisName, double thisMW, double thisEffRad, double thisDiffCoeff)
        {
            Name = thisName;
            MolecularWeight = thisMW;
            EffectiveRadius = thisEffRad;
            DiffusionCoefficient = thisDiffCoeff;
        }

        public GuiMolecule() : base()
        {
            Name = "MolName";
            MolecularWeight = 1.0;
            EffectiveRadius = 5.0;
            DiffusionCoefficient = 2;
        }

        public GuiMolecule(GuiMolecule gm)
        {
            Name = gm.Name;
            MolecularWeight = gm.MolecularWeight;
            EffectiveRadius = gm.EffectiveRadius;
            DiffusionCoefficient = gm.DiffusionCoefficient;
        }

    }

    //skg daphne
    public class GuiMolecularPopulation : EntityModelBase
    {
        public GuiMolecularPopulation()
            : base()
        {
        }
        public GuiMolecule Molecule { get; set; }
        public string Name { get; set; }
        private MolPopInfo _mp_Info;
        public MolPopInfo mpInfo         
        {
            get { return _mp_Info; }
            set { _mp_Info = value; }
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
            return (GlobalParameterType)Enum.ToObject(typeof(GlobalParameterType), (int)idx);
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

        public CellPopulation()
        {
            Guid id = Guid.NewGuid();
            cellpopulation_guid = id.ToString();
            cellpopulation_name = "Default Cell";
            cell_subset_guid_ref = "";
            number = 100;
            cellpopulation_constrained_to_region = false;
            cellpopulation_region_guid_ref = "";
            wrt_region = RelativePosition.Inside;
            cellpopulation_color = new System.Windows.Media.Color();
            cellpopulation_render_on = true;
            cellpopulation_color = System.Windows.Media.Color.FromRgb(255, 255, 255);
            cellpopulation_id = SimConfiguration.SafeCellPopulationID++;
        }
    }

    
    public enum CellBaseTypeLabel { BCell, TCell, FDC }

    public class CellSubset : EntityModelBase
    {
        public string cell_subset_guid { get; set; }
   
        private CellSubsetType _cell_subset_type;
        public CellSubsetType cell_subset_type
        {
            get { return _cell_subset_type; }
            set
            {
                if (_cell_subset_type == value)
                    return;
                else
                {
                    _cell_subset_type = value;
                    OnPropertyChanged("cell_subset_type");
                }
            }
        }

        public CellSubset()
        {
            Guid id = Guid.NewGuid();
            cell_subset_guid = id.ToString();
            cell_subset_type = new BCellSubsetType();            
        }

        public int FindSolfacIndex(string guid)
        {
            int idx = -1;
            //skg 5/31/12 changed
            if (cell_subset_type is BCellSubsetType)
            {
                BCellSubsetType bcst = (BCellSubsetType)cell_subset_type;
                for (int ii = 0; ii < bcst.cell_subset_type_receptor_params.Count; ii++)
                {
                    //skg 5/27/12 changed
                    if (bcst.cell_subset_type_receptor_params[ii].receptor_solfac_type_guid_ref == guid)
                    {
                        idx = ii;
                        break;
                    }
                }
            }
            else if (cell_subset_type is TCellSubsetType)
            {
                TCellSubsetType tcst = (TCellSubsetType)cell_subset_type;
                for (int ii = 0; ii < tcst.cell_subset_type_receptor_params.Count; ii++)
                {                    
                    if (tcst.cell_subset_type_receptor_params[ii].receptor_solfac_type_guid_ref == guid)
                    {
                        idx = ii;
                        break;
                    }
                }
            }
            return idx;
        }

        public void InitializeReceptorLevels(ObservableCollection<SolfacType> solfac_types)
        {
            //skg 6/1/12
            if (cell_subset_type is BCellSubsetType)
            {
                BCellSubsetType bcst = (BCellSubsetType)cell_subset_type;
                bcst.cell_subset_type_receptor_params.Clear();
                foreach (SolfacType st in solfac_types)
                {
                    bcst.cell_subset_type_receptor_params.Add(new ReceptorParameters(st.solfac_type_guid));
                }
            }
            else if (cell_subset_type is TCellSubsetType)
            {
                TCellSubsetType tcst = (TCellSubsetType)cell_subset_type;
                tcst.cell_subset_type_receptor_params.Clear();
                foreach (SolfacType st in solfac_types)
                {
                    tcst.cell_subset_type_receptor_params.Add(new ReceptorParameters(st.solfac_type_guid));
                }
            }
        }
    }

    //skg 5/24/12
    //base class for BCellSubsetType , TCellSubsetType , FDCellSubsetType 
    [XmlInclude(typeof(BCellSubsetType)),
     XmlInclude(typeof(TCellSubsetType)),
     XmlInclude(typeof(FDCellSubsetType))]    
    public class CellSubsetType
    {
        //public ObservableCollection<ReceptorParameters> cell_subset_type_receptor_params { get; set; }
        public string cell_subset_name { get; set; }
        public CellBaseTypeLabel baseCellType { get; set; }

        //Common Activation parameters
        public double initialActivationSignal { get; set; }

        [XmlIgnore] 
        public bool cell_subset_type_divides { get; set; }
        
        //[XmlIgnore]
        //public int cell_subset_type_receptor_index { get; set; }

        public CellSubsetType()
        {
            //baseCellTypeLabel = CellBaseTypeLabel.BCell;
            //cell_type_name = "Default cell type name";
            cell_subset_type_divides = false;
            //cell_subset_type_receptor_params = new ObservableCollection<ReceptorParameters>();
            //cell_subset_type_receptor_index = 0;
        }
    }

    public enum BCellPhenotype:int { ShortLivedPlasmaCyte, Centroblast, Centrocyte, LongLivedPlasmacyte, MemoryCell };

    public class BCellSubsetType : CellSubsetType
    {
        public ObservableCollection<ReceptorParameters> cell_subset_type_receptor_params { get; set; }

        [XmlIgnore]
        public int cell_subset_type_receptor_index { get; set; }

        public BCellPhenotype Phenotype { get; set; }

        //Activation parameters        
        public double alpha { get; set; }
        public double bcr0 { get; set; }
        public double secondRoundActivationSignal { get; set; }

        //Mutation parameters
        public double initialMutationRate { get; set; }
        public double secondRoundMutationRate { get; set; }
        public double lambdaPlus { get; set; }
        public double lambdaMinus { get; set; }
        public double initialAffinity { get; set; }

        //CC Rescue parameters
        public double rateRescue { get; set; }
        public double probPlasmacyte { get; set; }
        public double probRecycle { get; set; }
        public double complexDepletionRate { get; set; }

        //Intracellular Signaling parameters        
        public double mitoticInterval { get; set; }
        public double lambdaAp { get; set; }
        public double lambdaDif { get; set; }
        public double lambdaDiv { get; set; }
        public double lambdaResc { get; set; }
        public double kappaAp { get; set; }
        public double kappaDif { get; set; }
        public double kappaDiv { get; set; }
        public double kappaResc { get; set; }
        public double muAp { get; set; }
        public double muDif { get; set; }
        public double muResc { get; set; }

        public BCellSubsetType()
            : base()
        {
            //Set base cell label type
            baseCellType = CellBaseTypeLabel.BCell;
            cell_subset_name = "Centroblast";
            cell_subset_type_divides = true;
            cell_subset_type_receptor_params = new ObservableCollection<ReceptorParameters>();
            cell_subset_type_receptor_index = 0;

            //Activation properties
            initialActivationSignal = 128;
            alpha = 1;
            bcr0 = 320;
            secondRoundActivationSignal = 8;

            //Mutation params
            initialMutationRate = 0;
            secondRoundMutationRate = 0.001;
            lambdaPlus = 0.333;
            lambdaMinus = 0.111;
            initialAffinity = 1000000;

            //Intracellular Signaling properties
            mitoticInterval = 360;
            lambdaAp = 0.00556;
            lambdaDif = 0.00556;
            lambdaDiv = 0.00556;
            lambdaResc = 0.00556;
            kappaAp = 3.86E-6;
            kappaDif = 1.54e-5;
            kappaDiv = 1.54e-5;
            kappaResc = 3.86E-6;
            muAp = 0.0444;
            muDif = 0.1778;
            muResc = 0.0444;

            //CC Rescue properties
            rateRescue = 0.01667;
            probPlasmacyte = 0.25;
            probRecycle = 0.65;
            complexDepletionRate = 2.0E-6;

            //Phenotype
            Phenotype = BCellPhenotype.Centroblast;

            //Receptors

        }

    }
    public enum TCellPhenotype { FollicularHelper };

    public class TCellSubsetType : CellSubsetType
    {
        public ObservableCollection<ReceptorParameters> cell_subset_type_receptor_params { get; set; }
        public TCellPhenotype Phenotype { get; set; }

        [XmlIgnore]
        public int cell_subset_type_receptor_index { get; set; }

        //Activation parameters  - //SET TO ZERO
        public double alpha { get; set; }
        public double tcr0 { get; set; }       

        //Intracellular Signaling parameters  //SET TO ZERO      
        public double mitoticInterval { get; set; }
        public double lambdaAp { get; set; }
        public double lambdaDif { get; set; }
        public double lambdaDiv { get; set; }
        public double lambdaResc { get; set; }
        public double kappaAp { get; set; }
        public double kappaDif { get; set; }
        public double kappaDiv { get; set; }
        public double kappaResc { get; set; }
        public double muAp { get; set; }
        public double muDif { get; set; }
        public double muResc { get; set; }

        public TCellSubsetType()
            : base()
        {
            baseCellType = CellBaseTypeLabel.TCell;
            cell_subset_name = "T Follicular Helper";
            cell_subset_type_divides = true;
            cell_subset_type_receptor_params = new ObservableCollection<ReceptorParameters>();
            cell_subset_type_receptor_index = 0;

            //Phenotype
            Phenotype = TCellPhenotype.FollicularHelper;

            //Activation properties            
            initialActivationSignal = 0;
            alpha = 1;
            tcr0 = 320;                        

            //Intracellular Signaling properties
            mitoticInterval = 360;
            lambdaAp = 0.00556;
            lambdaDif = 0.00556;
            lambdaDiv = 0.00556;
            lambdaResc = 0.00556;
            kappaAp = 3.86E-6;
            kappaDif = 1.54e-5;
            kappaDiv = 1.54e-5;
            kappaResc = 3.86E-6;
            muAp = 0.0444;
            muDif = 0.1778;
            muResc = 0.0444;
        }
    }
    public class FDCellSubsetType : CellSubsetType
    {
        public double FCReceptorDensity     { get; set; }   //molecules/square micron
        public double TotalSurfaceArea      { get; set; }   //square microns
        public double InitialMeanAffinity   { get; set; }   //inverse molar
        public double SynapseArea           { get; set; }   //square microns

        public FDCellSubsetType()
            : base()
        {
            baseCellType = CellBaseTypeLabel.FDC;
            cell_subset_name = "FDC";
            initialActivationSignal = 0;

            //FDC Parameters
            FCReceptorDensity = 320;
            TotalSurfaceArea = 1.3E3;
            InitialMeanAffinity = 1.0E6;
            SynapseArea = 25;            
        }
    }

    public class ReceptorParameters
    {
        public string receptor_solfac_type_guid_ref { get; set; }
        public CkReceptorParams receptor_params { get; set; }

        public ReceptorParameters()
        {
            receptor_solfac_type_guid_ref = "";
            receptor_params = new CkReceptorParams();
        }

        public ReceptorParameters(string guid)
        {
            receptor_solfac_type_guid_ref = guid;
            receptor_params = new CkReceptorParams();
        }
    }

    public class CkReceptorParams
    {
        public double ckr_epsilon { get; set; }
        public double ckr_kappa { get; set; }
        public double ckr_pi { get; set; }
        public double ckr_tau { get; set; }
        public double ckr_delta { get; set; }
        public double ckr_u { get; set; }

        public CkReceptorParams()
        {
            ckr_epsilon = 0.1;
            ckr_kappa = 0.1;
            ckr_pi = 0.1;
            ckr_tau = 0.0;
            ckr_delta = 0.001;
            ckr_u = 0.0;
        }
    }


    // skg daphne
    // MolPopInfo ==================================
    public class MolPopInfo : EntityModelBase
    {
        public string mp_guid { get; set; }
        private string _mp_name = "";
        public string mp_name
        {
            get { return _mp_name; }
            set
            {
                if (_mp_name == value)
                    return;
                else
                {
                    _mp_name = value;
                    OnPropertyChanged("mp_name");
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
            mp_name = name;
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

    // Solfac type and receptor name are both keyed off of solfac_type_guid.
    // So, in reality, both names are just for human readability.
    public class SolfacType
    {
        public string solfac_type_guid { get; set; }
        public string solfac_type_name { get; set; }
        public string solfac_type_receptor_name { get; set; }

        public SolfacType()
        {
            Guid id = Guid.NewGuid();
            solfac_type_guid = id.ToString();
            solfac_type_name = "Solfac_A";
            solfac_type_receptor_name = "Receptor_A";
        }
    }

    /// <summary>
    /// Converter to go between cell type GUID references in CellSets
    /// and cell type names kept in the repository list of CellSubset(s).
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class CellTypeGUIDtoNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;
            string cell_subset_name = "";
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<CellSubset> cell_subset_list = cvs.Source as ObservableCollection<CellSubset>;
            if (cell_subset_list != null)
            {
                foreach (CellSubset ct in cell_subset_list)
                {
                    if (ct.cell_subset_guid == guid)
                    {
                        //skg 5/25/12 Changed this                         
                        cell_subset_name = ct.cell_subset_type.cell_subset_name;

                        break;
                    }
                }
            }
            return cell_subset_name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
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
    /// Converter to go between solfac GUID references in CellSubset list of ReceptorExpressionLevelPairs
    /// and receptor names kept in the repository list of SolfacType(s).
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class SolfacGUIDtoReceptorNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;
            string receptor_name = "";
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<SolfacType> solfac_list = cvs.Source as ObservableCollection<SolfacType>;
            if (solfac_list != null)
            {
                foreach (SolfacType st in solfac_list)
                {
                    if (st.solfac_type_guid == guid)
                    {
                        receptor_name = st.solfac_type_receptor_name + " (" + st.solfac_type_name + ")";
                        break;
                    }
                }
            }
            return receptor_name;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // TODO: Should probably put something real here, but right now it never gets called,
            // so I'm not sure what the value and parameter objects would be...
            return "y";
        }
    }

    /// <summary>
    /// Converter to go between solfac GUID references in CellSubset list of ReceptorExpressionLevelPairs
    /// and receptor names kept in the repository list of SolfacType(s).
    /// </summary>
    [ValueConversion(typeof(string), typeof(string))]
    public class SolfacGUIDtoSolfacNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string guid = value as string;
            string receptor_name = "";
            System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
            ObservableCollection<SolfacType> solfac_list = cvs.Source as ObservableCollection<SolfacType>;
            if (solfac_list != null)
            {
                foreach (SolfacType st in solfac_list)
                {
                    if (st.solfac_type_guid == guid)
                    {
                        receptor_name = st.solfac_type_name;
                        break;
                    }
                }
            }
            return receptor_name;
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
    public class SolfacDistributionTypeToStringConverter : IValueConverter
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
        private Uri _custom_gradient_file_uri = new Uri(MainWindow.appPath);
        private string _custom_gradient_file_string = MainWindow.appPath;

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


    // SIM PARAMETERS CLASSES ================================

    public enum GlobalParameterType { ForceParams, LocomotorParams, SynapseParams }

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(GlobalParameterType), typeof(string))]
    public class GlobalParamTypeToStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the GlobalParameterType enum...
        private List<string> _global_param_type_strings = new List<string>()
                                {
                                    "Adhesion",
                                    "Locomotion",
                                    "Synapse"
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _global_param_type_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _global_param_type_strings.FindIndex(item => item == str);
            return (GlobalParameterType)Enum.ToObject(typeof(GlobalParameterType), (int)idx);
        }
    }

    // Base class for any global parameter types
    [XmlInclude(typeof(ForceParams)),
     XmlInclude(typeof(LocomotorParams)),
     XmlInclude(typeof(SynapseParams))]
    public class GlobalParameter
    {
        [XmlIgnore]
        public GlobalParameterType global_parameter_type { get; protected set; }

        public GlobalParameter()
        {
        }
    }

    public class ForceParams : GlobalParameter
    {
        public double force_delta { get; set; }
        public double force_phi1 { get; set; }
        public double force_phi2 { get; set; }

        public ForceParams()
        {
            global_parameter_type = GlobalParameterType.ForceParams;

            force_delta = 15.0;
            force_phi1 = 300.0;
            force_phi2 = 0.44;
        }
    }

    public class LocomotorParams : GlobalParameter
    {
        public double loco_gamma { get; set; }
        public double loco_sigma { get; set; }
        public double loco_zeta { get; set; }
        public double loco_chi { get; set; }

        public LocomotorParams()
        {
            global_parameter_type = GlobalParameterType.LocomotorParams;

            loco_gamma = 1.0;
            loco_sigma = 10.0;
            loco_zeta = 1.0;
            loco_chi = 30;
        }
    }

    public class SynapseParams : GlobalParameter
    {
        public double Alpha { get; set; }
        public double Beta { get; set; }
        public double Kappa { get; set; }
        public double Delta { get; set; }
        public double Epsilon { get; set; }        

        public SynapseParams()
        {
            global_parameter_type = GlobalParameterType.SynapseParams;

            Alpha = 15.0;
            Beta = 300.0;
            Kappa = 0.44;
            Delta = 0.44;
            Epsilon = 0.44;
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
