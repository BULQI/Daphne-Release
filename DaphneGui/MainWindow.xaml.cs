using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using ActiproSoftware.Windows.Controls.Docking;
using Kitware.VTK;
using System.IO;
using System.Diagnostics;
using System.Threading;

using Daphne;
using Workbench;

using System.Collections.ObjectModel;
using Newtonsoft.Json;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        /// <summary>
        /// the absulute path where the installed, running executable resides
        /// </summary>
        public static string appPath;

        private DocWindow dw;
        private Thread simThread;
        private VCRControl vcrControl;
        public static Object cellFitLock = new Object();
        public static double cellOpacity = 1.0;


        private static Simulation sim;
        public static Simulation Sim
        {
            get { return MainWindow.sim; }
            set { MainWindow.sim = value; }
        }

        private bool blueHandToolButton_IsChecked = false;

        public bool BlueHandToolButton_IsChecked
        {
            get { return blueHandToolButton_IsChecked; }
            set
            {
                if (blueHandToolButton_IsChecked == value)
                    return;
                else
                {
                    blueHandToolButton_IsChecked = value;

                    MainWindow.SetControlFlag(MainWindow.CONTROL_MOLCONCS_ENABLED, value);
                    //gc.Rwc.RenderWindow.SetCurrentCursor(!value ? CURSOR_ARROW : CURSOR_HAND);
                    //Rwc.Invalidate();
                    //base.OnPropertyChanged("HandToolButton_IsChecked");
                }
            }
        }
        private Process devHelpProc;
        private static SimConfigurator configurator = null;
        private static int repetition;

        /// <summary>
        /// uri for the scenario file
        /// </summary>
        public static Uri xmlPath;
        public static Uri jsonPath;
        private string orig_content;
        private string orig_path = @"c:\temp";

        private bool exportAllFlag = false;
        private string igGeneFolderName = "";

        /// <summary>
        /// constants used by the simulation and gui
        /// </summary>
        public static byte CONTROL_NONE = 0,
                           CONTROL_FORCE_RESET = (1 << 0),
                           CONTROL_DB_LOAD = (1 << 1),
                           CONTROL_PICKING_ENABLED = (1 << 2),
                           CONTROL_ZERO_FORCE = (1 << 3),
                           CONTROL_NEW_RUN = (1 << 4),
                           CONTROL_UPDATE_GUI = (1 << 5),
                           CONTROL_MOLCONCS_ENABLED = (1 << 6);

        public static byte controlFlags = CONTROL_NONE;

        /// <summary>
        /// constants used in progress bar updating
        /// </summary>
        public static byte PROGRESS_NONE = 0,
                           PROGRESS_INIT = (1 << 0),
                           PROGRESS_RUNNING = (1 << 1),
                           PROGRESS_COMPLETE = (1 << 2),
                           PROGRESS_RESET = (1 << 3),
                           PROGRESS_LOCK = (1 << 4);
        public static byte fitStatus = PROGRESS_NONE;

        /// <summary>
        /// scenario load flag, success == true
        /// </summary>
        public static bool loadSuccess;

        ////////private static VTKGraphicsController gc;
        ////////private static VTKDataBasket vtkdataBasket;

        /// <summary>
        /// retrieve a pointer to the (for now, singular) VTK graphics actors
        /// </summary>
        ////////public static VTKGraphicsController GC
        ////////{
        ////////    get { return gc; }
        ////////}

        /// <summary>
        /// retrieve a pointer to the VTK data basket object
        /// </summary>
        ////////public static VTKDataBasket VTKBasket
        ////////{
        ////////    get { return vtkdataBasket; }
        ////////}

        /// <summary>
        /// retrieve a pointer to the configurator
        /// </summary>
        public static SimConfigurator SC
        {
            get { return configurator; }
        }

        /// <summary>
        /// check if a particular control flag is set
        /// </summary>
        /// <param name="flag">flag to check</param>
        /// <returns>true if set</returns>
        public static bool CheckControlFlag(byte flag)
        {
            return (controlFlags & flag) != 0;
        }

        /// <summary>
        /// retrieve the repetition number
        /// </summary>
        public static int Repetition
        {
            get { return repetition; }
        }

        /// <summary>
        /// utility to query whether we are running a profiling session; need to enable the RUNNING_PROFILER flag and recompile
        /// </summary>
        /// <returns></returns>
        public static bool ControlledProfiling()
        {
#if CONTROL_PROFILER
            return true;
#else
            return false;
#endif
        }

        /// <summary>
        /// custom routed command for delete db
        /// </summary>
        public static RoutedCommand DeleteDBCommand = new RoutedCommand();

        /// <summary>
        /// executed command handler for delete db
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CommandBindingDeleteDB_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            string messageBoxText = "Are you sure you want to clear the database?";
            string caption = "Clear database";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Warning;

            // Display message box
            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);

            // Process message box results
            if (result == MessageBoxResult.Yes)
            {
                //////////DataBaseTools.DeleteDataBase();
                System.Windows.MessageBox.Show("All records deleted!");
                VCR_Toolbar.IsEnabled = false; //Make sure that playback of half-deleted datasets is impossible.
                if (vcrControl != null)
                {
                    vcrControl.ReleaseVCR();
                }
            }
        }

        /// <summary>
        /// can execute command handler for delete db
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CommandBindingDeleteDB_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }






        public MainWindow()
        {
            InitializeComponent();

            this.ToolWinCellInfo.Close();

            // Generate a test sim configuration
            //These generate the default and blank scenarios so should be uncommented to do so and then commented out again
            
            //skg daphne Thursday, April 18, 2013
            CreateAndSerializeDiffusionScenario();
            CreateAndSerializeTestScenario();

            //CreateAndSerializeDefaultScenario();
            //CreateAndSerializeBlankScenario();

            // implementing the recent files list
            RecentFileList.MaxNumberOfFiles = 10;
            // disable the following to cause saving into the registry
            // on my Windows 7 machine, using the xml persister will create the file C:\Users\Harald\AppData\Roaming\Microsoft\PlazaSur\RecentFileList.xml
            // and save the history there
            RecentFileList.UseXmlPersister();
            // the event handler to run when clicking on an entry in the recent files list
            RecentFileList.MenuClick += (s, e) => loadScenarioFromFile(e.Filepath);


            // get the screen size and set the application window size accordingly
            System.Drawing.Rectangle r = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            double factor = 0.9,                    // 90% coverage
                   ratio = 4.0 / 3.0,               // width to height standard ratio 4:3
                   widthAdj = r.Width * factor,     // coverage adjusted width
                   heightAdj = r.Height * factor;   // coverage adjusted height

            // is the available screen space larger than the default?
            if (widthAdj > Width && heightAdj > Height)
            {
                // is the screen height the limiting dimension?
                if (widthAdj / r.Height > ratio)
                {
                    Height = heightAdj;
                    Width = (int)(Height * ratio);
                }
                else
                {
                    Width = widthAdj;
                    Height = (int)(Width / ratio);
                }
                // show the window centered
                Left = (r.Width - Width) / 2;
                Top = (r.Height - Height) / 2;
            }



            // Dialog behavior requires owner to be set
            // are we running from within the IDE?
            if (AssumeIDE() == true)
            {
                appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            }
            else
            {
                appPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + @"\DaphneGui";
            }



            // handle the application properties
            string file;

            autoZoomFitMenu.IsChecked = Properties.Settings.Default.autoZoomFit;
            openLastScenarioMenu.IsChecked = Properties.Settings.Default.lastOpenScenario != "";
            skipDataWriteMenu.IsChecked = Properties.Settings.Default.skipDataBaseWrites;
            // TEMP_SUMMARY
            writeCellSummariesMenu.IsChecked = Properties.Settings.Default.writeCellsummaries;

            // we can't allow these options if the database writes are disabled
            if (skipDataWriteMenu.IsChecked == true)
            {
                runStatisticalSummaryMenu.IsEnabled = false;
                runStatisticalSummaryMenu.IsChecked = false;
                uniqueNamesMenu.IsEnabled = false;
                uniqueNamesMenu.IsChecked = false;
            }
            else
            {
                runStatisticalSummaryMenu.IsEnabled = true;
                runStatisticalSummaryMenu.IsChecked = Properties.Settings.Default.runStatisticalSummary;
                uniqueNamesMenu.IsEnabled = true;
                uniqueNamesMenu.IsChecked = Properties.Settings.Default.suggestExpNameChange;
            }

            if (openLastScenarioMenu.IsChecked == true)
            {
                file = Properties.Settings.Default.lastOpenScenario;
            }
            else
            {
                //skg daphne Thursday, April 18, 2013
                //file = "daphne_test_scenario.xml";
                //file = "default_scenario.xml";
                file = "daphne_test_scenario.json";
            }

            // attempt to load a default simulation file; if it doesn't exist disable the gui
            // and make the window show up empty
            xmlPath = new Uri(appPath + @"\Config\" + file); // new Uri("xmlfile1.xml", UriKind.Relative);
            //orig_path = System.IO.Path.GetDirectoryName(xmlPath.LocalPath);
            //bool file_exists = File.Exists(xmlPath.LocalPath);

            //skg daphne Wednesday, May 08, 2013
            string tempPath = appPath + @"\Config\" + file;
            jsonPath = new Uri(tempPath);
            orig_path = System.IO.Path.GetDirectoryName(jsonPath.LocalPath);
            bool file_exists = File.Exists(jsonPath.LocalPath);
            

            if (file_exists)
            {
                SimConfigToolWindow.IsEnabled = true;
                saveScenario.IsEnabled = true;
                //displayTitle();
            }
            else
            {
                SimConfigToolWindow.IsEnabled = false;
                saveScenario.IsEnabled = false;
            }

            // Hide fitting tab control until sim has ended
            this.LPFittingToolWindow.Close();
            this.menu_ActivateLPFitting.IsEnabled = false;
            this.ExportMenu.IsEnabled = false;
            // And hide stats results chart for now
            //this.ChartViewDocWindow.Close();
            this.menu_ActivateAnalysisChart.IsEnabled = false;

            //SKIP VTK GRAPHICS WINDOW FOR NOW
            // create the simulation
            //////////sim = new Simulation();
            // data basket to hold simulation entities
            //////////dataBasket = new DataBasket();
            // vtk data basket to hold vtk data for entities with graphical representation
            //////////vtkdataBasket = new VTKDataBasket();
            // graphics controller to manage vtk objects
            //////////gc = new VTKGraphicsController(this);
            // NOTE: For now, setting data context of VTK MW display grid to only instance of GraphicsController.
            //////////this.vtkDisplay_DockPanel.DataContext = gc;
            // this.SimConfigSplitContainer.ResizeSlots(new double[2]{0.2, 0.8});

            if (file_exists)
            {
                initialState(true, true, "");
                enableCritical(loadSuccess);
                if (loadSuccess == true)
                {
                    // after doing a full reset, don't require one immediately
                    MainWindow.SetControlFlag(MainWindow.CONTROL_FORCE_RESET, false);
                    //////////vtkdataBasket.UpdateData();
                    //////////gc.DrawFrame(sim.GetProgressPercent());
                    displayTitle();
                }
                else
                {
                    // if we are unsuccessfully trying to load the default scenario then recreate it
                    // NOTE: keep disabled for safety; it seems a hacky thing to do
                    // (what if the user has something important in that file and it gets overwritten?)
                    //if (file == "default_scenario.xml")
                    //{
                    //    CreateAndSerializeDefaultScenario();
                    //}
                    Properties.Settings.Default.lastOpenScenario = "";
                    openLastScenarioMenu.IsChecked = false;
                }
            }

            //we need to check for database connection

            //SKIP DB OPERATIONS FOR NOW
            ////////string dsn = ConnectionString.dsn;
            ////////var sqlconn = new SQLiteConnection(dsn);

            ////////try
            ////////{
            ////////    sqlconn.Open();
            ////////    sqlconn.Close();
            ////////}
            ////////catch
            ////////{
            ////////    System.Windows.MessageBox.Show("No database connection detected. Database writing is skipped. Please check your databased settings.");
            ////////    skipDataWriteMenu.IsChecked = true;
            ////////    skipDataWriteMenu.IsEnabled = false;
            ////////    Properties.Settings.Default.skipDataBaseWrites = true;
            ////////    deleteDataBase.IsEnabled = false;
            ////////    runStatisticalSummaryMenu.IsChecked = false;
            ////////    runStatisticalSummaryMenu.IsEnabled = false;
            ////////    uniqueNamesMenu.IsChecked = false;
            ////////    uniqueNamesMenu.IsEnabled = false;
            ////////}

            // create the simulation thread
            //////////simThread = new Thread(new ThreadStart(run)) { IsBackground = true };
            //simThread.Priority = ThreadPriority.Normal;
            //////////simThread.Start();
            dw = new DocWindow();

            // immediately run the simulation
            if (ControlledProfiling() == true)
            {
                runSim();
            }




        }

        /// <summary>
        /// New default scenario for first pass of Daphne
        /// </summary>
        public void CreateAndSerializeDiffusionScenario()
        {
            var config = new SimConfigurator("Config\\daphne_ecm_scenario.json");
            var sim_config = config.SimConfig;

            // Experiment
            sim_config.experiment_name = "Diffusion Scenario";
            sim_config.experiment_description = "Initial scenario with predefined Molecules and Reactions, Compartment ECM with 1 molecular population";
            sim_config.scenario.time_config.duration = 100;
            sim_config.scenario.time_config.rendering_interval = 0.3;
            sim_config.scenario.time_config.sampling_interval = 1440;
            //sim_config.scenario.ManType = ManifoldType.BoundedRectangularPrism;

            // Global Paramters
            sim_config.LoadDefaultGlobalParameters();
            sim_config.ChartWindow = ReacComplexChartWindow;

            // Entity Repository
            EntityRepository repository = new EntityRepository();

            ////////// Possible solfac types
            SolfacType st = new SolfacType();
            st.solfac_type_name = "cxcl13";
            st.solfac_type_receptor_name = "cxcr5";
            repository.solfac_types.Add(st);

            sim_config.entity_repository = repository;

            // Gaussian Gradients
            GaussianSpecification gg = new GaussianSpecification();
            BoxSpecification box = new BoxSpecification();
            box.x_scale = 125;
            box.y_scale = 125;
            box.z_scale = 125;
            box.x_trans = 100;
            box.y_trans = 300;
            box.z_trans = 100;
            repository.box_specifications.Add(box);
            gg.gaussian_spec_box_guid_ref = box.box_guid;
            gg.gaussian_spec_name = "Off-center gaussian";
            gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            sim_config.entity_repository.gaussian_specifications.Add(gg);

            // Regions
            box = new BoxSpecification();
            box.x_scale = 100;
            box.y_scale = 100;
            box.z_scale = 100;
            box.x_trans = 300;
            box.y_trans = 300;
            box.z_trans = 300;
            repository.box_specifications.Add(box);
            Region reg = new Region("Ellipsoidal region", RegionShape.Ellipsoid);
            reg.region_box_spec_guid_ref = box.box_guid;
            reg.region_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.5f, 1.0f, 0.5f);
            sim_config.scenario.regions.Add(reg);

            box = new BoxSpecification();
            box.x_scale = 50;
            box.y_scale = 50;
            box.z_scale = 300;
            box.x_trans = 100;
            box.y_trans = 100;
            box.z_trans = 200;
            repository.box_specifications.Add(box);
            reg = new Region("Box region", RegionShape.Rectangular);
            reg.region_box_spec_guid_ref = box.box_guid;
            reg.region_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.5f, 0.7f, 1.0f);
            sim_config.scenario.regions.Add(reg);
            //end skg
            
            //SKG DAPHNE Wednesday, April 10, 2013 4:04:14 PM

            //Dictionary<string, Molecule> MolDict;
            //string molSpec = "CXCR5\t1.0\t0.0\t1.0\nCXCL13\t\t\t6.0e3\nCXCR5:CXCL13\t\t\t0.0\ngCXCR5\t\t\t\ndriver\t\t\t\nCXCL12\t7.96\t\t6.0e3\n";
            //MolDict = MoleculeBuilder.Go(molSpec);

            var query =
                from mol in sim_config.PredefMolecules
                where mol.Name == "CXCL13"
                select mol;

            GuiMolecularPopulation gmp = null;
            foreach (GuiMolecule gm in query)
            {
                gmp = new GuiMolecularPopulation();
                gmp.Molecule = new GuiMolecule(gm);
                gmp.mpInfo = new MolPopInfo("My " + gm.Name);
                gmp.Name = "My " + gm.Name;
                gmp.mpInfo.mp_name = "Gaussian gradient";
                gmp.mpInfo.mp_type_guid_ref = sim_config.entity_repository.solfac_types[0].solfac_type_guid;
                gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                gmp.mpInfo.mp_render_blending_weight = 2.0;
                MolPopGaussianGradient sgg = new MolPopGaussianGradient();
                sgg.peak_concentration = 10;
                sgg.gaussgrad_gauss_spec_guid_ref = sim_config.entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
                gmp.mpInfo.mp_distribution = sgg;
                sim_config.scenario.MolPops.Add(gmp);
            }

            //
            // Create Extracellular fluid
            // 

            int[] numGridPts = { 21, 21, 21 };  //{ 21, 21, 21 };

            // spatial extent in each dimension
            double[] XCellExtent = { 1000.0, 1000.0, 1000.0 };

            //
            // Add all molecular populations
            //

            // Set [CXCL13]max ~ f*Kd, where Kd is the CXCL13:CXCR5 binding affinity and f is a constant
            // Kd ~ 3 nM for CXCL12:CXCR4. Estimate the same binding affinity for CXCL13:CXCR5.
            // 1 nM = (1e-6)*(1e-18)*(6.022e23) molecule/um^3

            ////////double maxConc = 2 * (3.0) * (1e-6) * (1e-18) * (6.022e23);

            ////////double[] sigma = { Interior.Extents[0] / 5.0, 
            ////////                   Interior.Extents[1] / 5.0, 
            ////////                   Interior.Extents[2] / 5.0 };

            ////////double[] center = new double[Interior.Dim];
            ////////center[0] = Interior.Extents[0] / 2.0;
            ////////center[1] = Interior.Extents[1] / 2.0;
            ////////center[2] = Interior.Extents[2] / 2.0;

            // Add a ligand MolecularPopulation whose concentration (molecules/um^3) is a Gaussian field
            //ScalarField gsf = new DiscreteScalarField(ECS.Interior, new GaussianFieldInitializer(center, sigma, maxConc));

            //ECS.AddMolecularPopulation(MolDict["CXCL13"], gsf);
            //ECS.AddMolecularPopulation(MyMol, gsf);

            //Diffusion scenario has no reactions
            //BUT WE'LL TRY SOME ANYWAY
            GuiReactionTemplate grt = new GuiReactionTemplate();

            // Write out XML file - SHOULD REMOVE THIS!
            config.SerializeSimConfigToFile();

            //Write out listOfReactions to json file
            //JsonSerializer serializer = new JsonSerializer();
            //using (StreamWriter sw = new StreamWriter(@"c:\temp\json_output2.txt"))
            //using (JsonWriter writer = new JsonTextWriter(sw))
            //{
            //    //serializer.Serialize(writer, sim_config.ReactionTemplateList);
            //    serializer.Serialize(writer, sim_config.GuiReactionTemplates);
            //}


            //skg daphne serialize to json Thursday, April 18, 2013
            var settings = new JsonSerializerSettings();
            settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            settings.TypeNameHandling = TypeNameHandling.Auto;

            string jsonSpec = JsonConvert.SerializeObject(config.SimConfig, Newtonsoft.Json.Formatting.Indented, settings);
            string jsonFile = config.FileName;
            //jsonFile = jsonFile.Replace(".xml", ".txt");
            File.WriteAllText(jsonFile, jsonSpec);

        }

        /// <summary>
        /// Test scenario for first pass of Daphne
        /// </summary>
        public void CreateAndSerializeTestScenario()
        {
            var config = new SimConfigurator("Config\\daphne_test_scenario.json");
            var sim_config = config.SimConfig;

            // Experiment
            sim_config.experiment_name = "Test Scenario";
            sim_config.experiment_description = "Initial scenario with predefined Molecules and Reactions, Compartment ECM with molecular populations, reactions, reaction complexes, and manifold";
            sim_config.scenario.time_config.duration = 100;
            sim_config.scenario.time_config.rendering_interval = 0.3;
            sim_config.scenario.time_config.sampling_interval = 1440;

            // Global Paramters
            sim_config.LoadDefaultGlobalParameters();
            sim_config.ChartWindow = ReacComplexChartWindow;

            // Entity Repository
            EntityRepository repository = new EntityRepository();

            // Possible solfac types - Do we still need these?
            SolfacType st = new SolfacType();
            st.solfac_type_name = "cxcl13";
            st.solfac_type_receptor_name = "cxcr5";
            repository.solfac_types.Add(st);

            sim_config.entity_repository = repository;

            // Gaussian Gradients
            GaussianSpecification gg = new GaussianSpecification();
            BoxSpecification box = new BoxSpecification();
            box.x_scale = 125;
            box.y_scale = 125;
            box.z_scale = 125;
            box.x_trans = 100;
            box.y_trans = 300;
            box.z_trans = 100;
            repository.box_specifications.Add(box);
            gg.gaussian_spec_box_guid_ref = box.box_guid;
            gg.gaussian_spec_name = "Off-center gaussian";
            gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            sim_config.entity_repository.gaussian_specifications.Add(gg);

            // Regions
            box = new BoxSpecification();
            box.x_scale = 100;
            box.y_scale = 100;
            box.z_scale = 100;
            box.x_trans = 300;
            box.y_trans = 300;
            box.z_trans = 300;
            repository.box_specifications.Add(box);
            Region reg = new Region("Ellipsoidal region", RegionShape.Ellipsoid);
            reg.region_box_spec_guid_ref = box.box_guid;
            reg.region_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.5f, 1.0f, 0.5f);
            sim_config.scenario.regions.Add(reg);

            box = new BoxSpecification();
            box.x_scale = 50;
            box.y_scale = 50;
            box.z_scale = 300;
            box.x_trans = 100;
            box.y_trans = 100;
            box.z_trans = 200;
            repository.box_specifications.Add(box);
            reg = new Region("Box region", RegionShape.Rectangular);
            reg.region_box_spec_guid_ref = box.box_guid;
            reg.region_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.5f, 0.7f, 1.0f);
            sim_config.scenario.regions.Add(reg);

            //SKG DAPHNE Wednesday, April 10, 2013 4:04:14 PM
            var query =
                from mol in sim_config.PredefMolecules
                where mol.Name == "CXCL13" || mol.Name == "CXCR5"
                select mol;

            GuiMolecularPopulation gmp = null;
            foreach (GuiMolecule gm in query)
            {
                gmp = new GuiMolecularPopulation();
                gmp.Molecule = new GuiMolecule(gm);
                gmp.mpInfo = new MolPopInfo("My " + gm.Name);
                gmp.Name = "My " + gm.Name;
                gmp.mpInfo.mp_name = "Gaussian gradient";
                gmp.mpInfo.mp_type_guid_ref = sim_config.entity_repository.solfac_types[0].solfac_type_guid;
                gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                gmp.mpInfo.mp_render_blending_weight = 2.0;
                MolPopGaussianGradient sgg = new MolPopGaussianGradient();
                sgg.peak_concentration = 10;
                sgg.gaussgrad_gauss_spec_guid_ref = sim_config.entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
                gmp.mpInfo.mp_distribution = sgg;
                sim_config.scenario.MolPops.Add(gmp);
            }

            int[] numGridPts = { 2, 2, 2 };  //{ 21, 21, 21 };

            // spatial extent in each dimension
            double[] XCellExtent = { 1000.0, 1000.0, 1000.0 };

            //Try some reactions
            GuiReactionTemplate grt = new GuiReactionTemplate();
            grt = sim_config.PredefReactions[1];
            sim_config.scenario.Reactions.Add(grt);

            grt = new GuiReactionTemplate();
            grt = sim_config.PredefReactions[2];
            sim_config.scenario.Reactions.Add(grt);

            //Try some reaction complexes
            GuiReactionComplex rc = new GuiReactionComplex();
            rc.Name = "RC1";
            
            grt = new GuiReactionTemplate();
            grt = sim_config.PredefReactions[5];
            rc.Reactions.Add(grt);

            grt = new GuiReactionTemplate();
            grt = sim_config.PredefReactions[6];
            rc.Reactions.Add(grt);

            grt = new GuiReactionTemplate();
            grt = sim_config.PredefReactions[7];
            rc.Reactions.Add(grt);

            grt = new GuiReactionTemplate();
            grt = sim_config.PredefReactions[8];
            rc.Reactions.Add(grt);

            sim_config.scenario.ReactionComplexes.Add(rc);

            rc = new GuiReactionComplex();
            rc.Name = "RC2";

            grt = new GuiReactionTemplate();
            grt = sim_config.PredefReactions[1];
            rc.Reactions.Add(grt);

            grt = new GuiReactionTemplate();
            grt = sim_config.PredefReactions[2];
            rc.Reactions.Add(grt);

            grt = new GuiReactionTemplate();
            grt = sim_config.PredefReactions[3];
            rc.Reactions.Add(grt);

            grt = new GuiReactionTemplate();
            grt = sim_config.PredefReactions[4];
            rc.Reactions.Add(grt);

            sim_config.scenario.ReactionComplexes.Add(rc);

            //End skg daphne

            // Serialize to XML file - REMOVE THIS
            config.SerializeSimConfigToFile();

            //skg daphne serialize to json Thursday, April 18, 2013
            var Settings = new JsonSerializerSettings();
            Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            Settings.TypeNameHandling = TypeNameHandling.Auto;
            string jsonSpec = JsonConvert.SerializeObject(config.SimConfig, Newtonsoft.Json.Formatting.Indented, Settings);
            string jsonFile = config.FileName;
            File.WriteAllText(jsonFile, jsonSpec);

        }

        

        private void showScenarioInitial()
        {
            lockAndResetSim(true, "");
            if (loadSuccess == false)
            {
                return;
            }
            SimConfigToolWindow.IsEnabled = true;
            saveScenario.IsEnabled = true;
        }

        //skg daphne
        //private void setScenarioPaths(string filename)
        //{
        //    xmlPath = new Uri(filename);
        //    orig_path = System.IO.Path.GetDirectoryName(xmlPath.LocalPath);
        //    displayTitle();
        //}
        private void setScenarioPaths(string filename)
        {
            jsonPath = new Uri(filename);
            orig_path = System.IO.Path.GetDirectoryName(jsonPath.LocalPath);
            displayTitle();
        }

        private void loadScenarioFromFile(string filename)
        {
            setScenarioPaths(filename);
            showScenarioInitial();
        }

        private Nullable<bool> loadScenarioUsingDialog()
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = orig_path;
            //skg daphne changes
            //dlg.DefaultExt = ".xml"; // Default file extension
            //dlg.Filter = "Sim Config XML docs (.xml)|*.xml"; // Filter files by extension
            dlg.DefaultExt = ".json"; // Default file extension
            dlg.Filter = "Sim Config JSON docs (.json)|*.json"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Save filename here, but deserialization will happen in lockAndResetSim->initialState call
                string filename = dlg.FileName;

                RecentFileList.InsertFile(filename);
                setScenarioPaths(filename);
            }

            return result;

        }

        private void saveScenarioAs_Click(object sender, RoutedEventArgs e)
        {
            saveScenarioUsingDialog();
        }

        private Nullable<bool> saveScenarioUsingDialog()
        {
            // Configure save file dialog box
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.InitialDirectory = orig_path;
            dlg.FileName = "scenario"; // Default file name
            //dlg.DefaultExt = ".xml"; // Default file extension
            //dlg.Filter = "Sim Config XML docs (.xml)|*.xml"; // Filter files by extension
            dlg.DefaultExt = ".json"; // Default file extension
            dlg.Filter = "Sim Config JSON docs (.json)|*.json"; // Filter files by extension

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                string filename = dlg.FileName;
                // Save dialog catches trying to overwrite Read-Only files, so this should be safe...
                
                configurator.FileName = filename;
                configurator.SerializeSimConfigToFile();
                orig_content = configurator.SerializeSimConfigToStringSkipDeco();
                xmlPath = new Uri(filename);
                orig_path = System.IO.Path.GetDirectoryName(xmlPath.LocalPath);
                displayTitle();
                
            }
            return result;
        }

        /// <summary>
        /// reset the simulation; will also apply the initial state; call after loading a scenario file
        /// </summary>
        /// <param name="newFile">true to indicate we are loading a new file</param>
        /// <param name="xmlConfigString">scenario as a string</param>
        private void lockAndResetSim(bool newFile, string xmlConfigString)
        {
            // prevent when a fit is in progress
            lock (cellFitLock)
            {
                if (vcrControl != null)
                {
                    vcrControl.SetInactive();
                }
                initialState(newFile, true, xmlConfigString);
                enableCritical(loadSuccess);
                if (loadSuccess == false)
                {
                    return;
                }

                // after doing a full reset, don't require one immediately unless we did a db load
                MainWindow.SetControlFlag(MainWindow.CONTROL_FORCE_RESET, MainWindow.CheckControlFlag(MainWindow.CONTROL_DB_LOAD));

                //REMOVE VTK AND GRAPHICS WINDOW CODE FOR NOW
                //////////sim.reset();
                // reset cell tracks and free memory
                //////////gc.CleanupTracks();
                //////////gc.CellController.SetCellOpacities(1.0);
                //////////fitCellOpacitySlider.Value = 1.0;
                //////////vtkdataBasket.UpdateData();
                //////////gc.DrawFrame(sim.GetProgressPercent());

                // prevent all fit/analysis-related things
                hideFit();
                ExportMenu.IsEnabled = false;
            }
        }

        private void OpenExpSelectWindow(object sender, RoutedEventArgs e)
        {
            #region MyRegion
            //esw = new ExpSelectWindow(-1);
            //esw.Owner = this;
            //esw.ShowDialog();
            //if (esw.expselected)
            //{
            //    MainWindow.SetControlFlag(MainWindow.CONTROL_DB_LOAD, true);
            //    lockAndResetSim(true, esw.SelectedXML);
            //    if (loadSuccess == false)
            //    {
            //        return;
            //    }
            //    MainWindow.SetControlFlag(MainWindow.CONTROL_DB_LOAD, false);
            //    sim.runStatSummary();
            //    GUIUpdate(esw.SelectedExperiment, true);
            //    displayTitle("DB experiment id " + esw.SelectedExperiment);
            //} 
            #endregion
        }

        private void OpenLPFittingWindow(object sender, RoutedEventArgs e)
        {
            #region MyRegion
            //LPManager lpm = new LPManager();
            ////debugOut("ExpID is: " + sim.SC.id.ToString());
            //if (lpfw == null)
            //{
            //    lpfw = new LPFittingWindow(lpm, configurator.SimConfig.experiment_db_id);
            //    lpfw.Show();
            //}
            //else
            //{
            //    if (lpfw.IsLoaded)
            //    {
            //        if (!lpfw.Activate())
            //        {
            //            lpfw.Close();
            //            lpfw = new LPFittingWindow(lpm, configurator.SimConfig.experiment_db_id);
            //            lpfw.Show();
            //        }
            //    }
            //    else
            //    {
            //        lpfw = new LPFittingWindow(lpm, configurator.SimConfig.experiment_db_id);
            //        lpfw.Show();
            //    }
            //} 
            #endregion
        }

        private void OpenCellDivisionWindow(object sender, RoutedEventArgs e)
        {
            #region MyRegion
            //if (cdm == null)
            //{
            //    cdm = new CellDivisionAnalysisManager();
            //}

            //if (cdw == null)
            //{
            //    cdw = new CellDivisionWindow(cdm, configurator.SimConfig.experiment_db_id);
            //    cdw.Show();
            //}
            //else
            //{
            //    if (cdw.IsLoaded)
            //    {
            //        if (!cdw.Activate())
            //        {
            //            cdw.Close();
            //            cdw = new CellDivisionWindow(cdm, configurator.SimConfig.experiment_db_id);
            //            cdw.Show();
            //        }
            //    }
            //    else
            //    {
            //        cdw = new CellDivisionWindow(cdm, configurator.SimConfig.experiment_db_id);
            //        cdw.Show();
            //    }
            //} 
            #endregion

        }

        private void VCRbutton_First_Click(object sender, RoutedEventArgs e)
        {
            vcrControl.MoveToFrame(0, false);
        }

        private void VCRbutton_Play_Checked(object sender, RoutedEventArgs e)
        {
            DataBaseMenu.IsEnabled = false;
            vcrControl.SetPlaybackState(VCRControlState.VCR_PLAY);
        }

        private void VCRbutton_Play_Unchecked(object sender, RoutedEventArgs e)
        {
            DataBaseMenu.IsEnabled = true;
            vcrControl.SetInactive();
        }

        private void VCRbutton_Back_Click(object sender, RoutedEventArgs e)
        {
            vcrControl.Advance(-1);
        }

        private void VCRbutton_Forward_Click(object sender, RoutedEventArgs e)
        {
            vcrControl.Advance(1);
        }

        private void VCRbutton_Last_Click(object sender, RoutedEventArgs e)
        {
            vcrControl.MoveToFrame(vcrControl.TotalFrames() - 1);
        }

        private void VCRSlider_LeftMouse_Down(object sender, MouseButtonEventArgs e)
        {
            vcrControl.SaveState();
            vcrControl.SetInactive();
        }

        private void VCRSlider_LeftMouse_Up(object sender, MouseButtonEventArgs e)
        {
            vcrControl.SetPlaybackState(vcrControl.SavedState);
            if (vcrControl.IsActive())
            {
                VCRbutton_Play.IsChecked = true;
            }
        }

        private void autoZoomFit_click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.autoZoomFit = autoZoomFitMenu.IsChecked;
        }

        /// <summary>
        /// utility to query whether we are running from within the IDE or want to make the program think we do; useful for profiling and 
        /// running without debugging from within the IDE
        /// </summary>
        /// <returns>true for running inside the IDE</returns>
        public static bool AssumeIDE()
        {
#if ASSUME_DEBUGGER || RUNNING_PROFILER || CONTROL_PROFILER
            return true;
#else
            return Debugger.IsAttached;
#endif
        }

        private void openLastScenario_click(object sender, RoutedEventArgs e)
        {
            if (openLastScenarioMenu.IsChecked == true)
            {
                Properties.Settings.Default.lastOpenScenario = extractFileName();
            }
            else
            {
                Properties.Settings.Default.lastOpenScenario = "";
            }
        }

        //skg daphne
        //private string extractFileName()
        //{
        //    string[] segments = xmlPath.LocalPath.Split('\\');

        //    return segments.Last();
        //}
        private string extractFileName()
        {
            string[] segments = jsonPath.LocalPath.Split('\\');

            return segments.Last();
        }

        private void runCellSummaries_click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.writeCellsummaries = writeCellSummariesMenu.IsChecked;
        }

        private void runStatisticalSummary_click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.runStatisticalSummary = runStatisticalSummaryMenu.IsChecked;
        }

        private void uniqueNames_click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.suggestExpNameChange = uniqueNamesMenu.IsChecked;
        }

        private void skipDataBase_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.skipDataBaseWrites = skipDataWriteMenu.IsChecked;

            // these options aren't feasible without the database output being enabled
            if (skipDataWriteMenu.IsChecked)
            {
                runStatisticalSummaryMenu.IsChecked = false;
                runStatisticalSummaryMenu.IsEnabled = false;
                uniqueNamesMenu.IsChecked = false;
                uniqueNamesMenu.IsEnabled = false;
            }
            else
            {
                runStatisticalSummaryMenu.IsEnabled = true;
                runStatisticalSummaryMenu.IsChecked = Properties.Settings.Default.runStatisticalSummary;
                uniqueNamesMenu.IsEnabled = true;
                uniqueNamesMenu.IsChecked = Properties.Settings.Default.suggestExpNameChange;
            }
        }

        private void bufferDatabaseWriteMenu_Click(object sender, RoutedEventArgs e)
        {
            if (bufferDatabaseWriteMenu.IsChecked)
            {
                Properties.Settings.Default.bufferDataBaseWriting = true;
            }
            else
            {
                Properties.Settings.Default.bufferDataBaseWriting = false;
            }

        }

        /// <summary>
        /// the helper method to open the text writer for data export
        /// </summary>
        /// <param name="sw">the text writer to be open and used for writing</param>
        /// <returns>the delimitor string for either /t or , csv</returns>
        private bool openTextStream(ref StreamWriter sw, ref string delimiterStr, string namePrefix)
        {
            // Create a new save file dialog
            System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();
            delimiterStr = "\t";
            // Sets the current file name filter string, which determines 
            // the choices that appear in the "Save as file type" or 
            // "Files of type" box in the dialog box.

            if (exportAllFlag)
            {
                //save all or want to write cell summary
                TBKMath.FileIDs fileID = new TBKMath.FileIDs();
                string summaryFileOutputPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + @"\PlazaSur\";
                sw = File.CreateText(summaryFileOutputPath + namePrefix + fileID.Stamp + ".txt");
                igGeneFolderName = summaryFileOutputPath;

                return true;
            }
            saveFileDialog1.Filter = "Text Files (*.txt)|*.txt|CSV Files (*.csv)|*.csv|All Files (*.*)|*.*";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            // Set image file format
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {

                if (saveFileDialog1.FileName.EndsWith("csv"))
                {
                    delimiterStr = ",";
                }
            }
            else
                return false;
            igGeneFolderName = Directory.GetParent(saveFileDialog1.FileName).FullName + @"\";
            sw = File.CreateText(saveFileDialog1.FileName);

            return true;
        }

        private void printHeader(StreamWriter sw, String notes)
        {
            sw.WriteLine("//-- Exp:" + MainWindow.SC.SimConfig.experiment_name);
            sw.WriteLine("//-- Description:" + MainWindow.SC.SimConfig.experiment_description);
            sw.WriteLine(notes);
            sw.WriteLine("");

        }

        /// <summary>
        /// event handler to b cell summary export menu
        /// </summary>
        /// <param name="sender">object sender</param>
        /// <param name="e">event arguments</param>
        private void bCellSummaryExport_Click(object sender, RoutedEventArgs e)
        {
            string delimiterStr = "\t";
            StreamWriter sw = null;
            if (!openTextStream(ref sw, ref delimiterStr, "BCellSummary"))
                return;

            printHeader(sw, "");
            sw.WriteLine("time(minutes)" + delimiterStr + "nCB" + delimiterStr + "nCC" + delimiterStr + "nSPCyte" + delimiterStr + "nLPCyte" + delimiterStr + "nMem" + delimiterStr + "nBCells" + delimiterStr + "AbConc" + delimiterStr + "MeanLogAffinity" + delimiterStr + "VarLogAffinity" + delimiterStr + "FDC.complexes" + delimiterStr + "NAbTypes");

            //connect the database to get all the information about b cell summary.
            //first we need to build the sampling time array.

            List<int> samplingTimeArr = new List<int>();//used to hold the time_id for the sampling timeframe from the database
            List<int> frameTimeIds = new List<int>(); //used to hold all the sorted framed time ids of the simulation
            List<double> frameTimeVals = new List<double>();//used to hold all the sorted framed time values of the simulation
            //go through
            double samplingFreq = SC.SimConfig.scenario.time_config.sampling_interval;
            //get the frameTime from the database.  
            //DataBaseTools.GetTimeFrame(SC.SimConfig.experiment_db_id, frameTimeIds, frameTimeVals);

            int samplingStep = 0;
            //rebuild the simulation frame by using rendering interval and timeFrame ,
            //
            for (int i = 0; i < frameTimeIds.Count; i++)
            {
                if (frameTimeVals[i] >= samplingStep * SC.SimConfig.scenario.time_config.sampling_interval || (i == frameTimeIds.Count - 1))
                {
                    samplingTimeArr.Add(frameTimeIds[i]);
                    samplingStep++;
                }
            }//end of for loop
            //now with this array build the sql statement


            //Dictionary<int, Dictionary<int, BCellPhenotype>> cellState = DataBaseTools.GetBCellSummaryCellState(SC.SimConfig.experiment_db_id, samplingTimeArr);

            ////n
            //Dictionary<int, Dictionary<string, double>> simState = DataBaseTools.GetBCellSummarySimState(SC.SimConfig.experiment_db_id, samplingTimeArr);

            //Dictionary<int, Dictionary<int, double>> FDCState = DataBaseTools.GetFDCSummaryCellState(SC.SimConfig.experiment_db_id, samplingTimeArr);






            //go through each time point and collect summary and write out results
            //foreach (int i in samplingTimeArr)
            //{
            //    //get b cell summary
            //    //start generating the summaries.
            //    int nBCentrocyte = 0;
            //    int nBCentroblast = 0;
            //    int nMem = 0;
            //    int nPlasmacyteShort = 0;
            //    int nPlasmacyteLong = 0;
            //    int nTotal = 0;
            //    double totalComplex = 0;
            //    foreach (KeyValuePair<Int32, BCellPhenotype> cpheno in cellState[i])
            //    {
            //        switch (cpheno.Value)
            //        {
            //            case BCellPhenotype.Centroblast:
            //                nBCentroblast++;
            //                break;
            //            case BCellPhenotype.Centrocyte:
            //                nBCentrocyte++;
            //                break;
            //            case BCellPhenotype.LongLivedPlasmacyte:
            //                nPlasmacyteLong++;
            //                break;
            //            case BCellPhenotype.ShortLivedPlasmaCyte:
            //                nPlasmacyteShort++;
            //                break;
            //            case BCellPhenotype.MemoryCell:
            //                nMem++;
            //                break;

            //        }
            //        nTotal++;
            //    }//each Bcell state

            //    //FDC summary
            //    foreach (KeyValuePair<int, double> fdcComplexDensity in FDCState[i])
            //    {
            //        //
            //        totalComplex += fdcComplexDensity.Value;
            //    }

            //    //writing out
            //    sw.WriteLine(frameTimeVals[frameTimeIds.IndexOf(i)] + delimiterStr + nBCentroblast + delimiterStr + nBCentrocyte + delimiterStr + nPlasmacyteShort + delimiterStr + nPlasmacyteLong + delimiterStr + nMem + delimiterStr + nTotal + delimiterStr +
            //                       simState[i]["IgConcentration"] + delimiterStr + simState[i]["MeanLogAffinity"] + delimiterStr +
            //                       simState[i]["VarLogAffinity"] + delimiterStr + totalComplex + delimiterStr + simState[i]["AntibodyCount"]);

            //}//each time id

            ////done
            //sw.Close();
            //sw = null;
            ////now writing the Ig in fasta format
            //TBKMath.FileIDs fileID = new TBKMath.FileIDs();
            //Directory.CreateDirectory(igGeneFolderName + fileID.Stamp);
            //StreamWriter fastaWriter = File.CreateText(igGeneFolderName + fileID.Stamp + @"\Ig.fasta");
            //Dictionary<int, string> igData = DataBaseTools.GetIgGene(SC.SimConfig.experiment_db_id);
            //foreach (KeyValuePair<int, string> kvpc in igData)
            //{
            //    fastaWriter.WriteLine(">" + kvpc.Value);
            //}
            //fastaWriter.Close();
            //fastaWriter = null;






        }

        private void bCellPedigreeExport_Click(object sender, RoutedEventArgs e)
        {
            string delimiterStr = "\t";
            StreamWriter sw = null;
            bool flag = openTextStream(ref sw, ref delimiterStr, "BCellPedSummary");
            if (!flag)
                return;
            printHeader(sw, "//-- Event Keys: -1, Cell Death; 1, Cell Division; 2, To Plasmacyte; 3, To Memory.");
            sw.WriteLine("Cell_ID" + delimiterStr + "ParentID" + delimiterStr + "Event" + delimiterStr + "Age" + delimiterStr + "Affinity");




            ////get family tree first.
            //Dictionary<int, GeneologyInfo> familytree = CellDivTools.GetFamilyTree(SC.SimConfig.experiment_db_id);

            ////now what?
            //foreach (KeyValuePair<int, GeneologyInfo> kvpc in familytree)
            //{
            //    int motherID;
            //    //GeneologyInfo daughterNode=null;
            //    if (kvpc.Value.Generation == 0 && kvpc.Value.CellType == (int)CellBaseTypeLabel.BCell && kvpc.Value.DieOrDivide != 0)//this is founder.
            //    {
            //        motherID = kvpc.Value.CellId;
            //        printOneNode(ref familytree, motherID, motherID, ref sw, delimiterStr);
            //    }
            //}

            //clean up
            sw.Close();
            sw = null;
        }

        private void bCellFDCSummaryExport_Click(object sender, RoutedEventArgs e)
        {
            string delimiterStr = "\t";
            StreamWriter sw = null;
            if (!openTextStream(ref sw, ref delimiterStr, "BCellFDCSummary"))
            {
                return;
            }
            printHeader(sw, "");
            sw.WriteLine("BCell FDC interaction start and end");





            ////Dictionary<int, string> st = new Dictionary<int, string>();
            ////st = DataBaseTools.GetSynapse(SC.SimConfig.experiment_db_id);

            ////foreach (KeyValuePair<int, string> kv in st)
            ////{
            ////    sw.WriteLine(kv.Value);
            ////}





            //clean up
            sw.Close();
            sw = null;
        }

        private void allExport_Click(object sender, RoutedEventArgs e)
        {
            exportAllFlag = true;
            this.bCellFDCSummaryExport_Click(sender, e);
            this.bCellSummaryExport_Click(sender, e);
            this.bCellPedigreeExport_Click(sender, e);

            exportAllFlag = false;
        }//end of this method

        private void devHelp_click(object sender, RoutedEventArgs e)
        {
            devHelpProc = System.Diagnostics.Process.Start(new Uri(appPath + @"\help\documentation.chm").LocalPath);
        }

        private void fittingHelp_click(object sender, RoutedEventArgs e)
        {
            if (dw.HasBeenClosed)
            {
                dw = new DocWindow();
            }
            dw.webBrowser.Navigate(new Uri(appPath + @"\help\GaussianProcessesDoc.mht"));
            dw.topicBox.Text = "Gaussian Processes";
            dw.Show();
        }

        private void locomotionHelp_click(object sender, RoutedEventArgs e)
        {
            if (dw.HasBeenClosed)
            {
                dw = new DocWindow();
            }
            dw.webBrowser.Navigate(new Uri(appPath + @"\help\LocomotionDoc.mht"));
            dw.topicBox.Text = "Locomotion";
            dw.Show();
        }

        private void receptorHelp_click(object sender, RoutedEventArgs e)
        {
            if (dw.HasBeenClosed)
            {
                dw = new DocWindow();
            }
            dw.webBrowser.Navigate(new Uri(appPath + @"\help\ChemokineReceptorExpressionDynamicsDoc.mht"));
            dw.topicBox.Text = "Chemokine Receptors";
            dw.Show();
        }

        /// <summary>
        /// reset the simulation to a random initial state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void resetButton_Click(object sender, RoutedEventArgs e)
        {
            ////////if (sim.RunStatus == Simulation.RUNSTAT_RUN)
            ////////{
            ////////    sim.RunStatus = Simulation.RUNSTAT_ABORT;
            ////////}
            ////////else
            ////////{
            ////////    reset();
            ////////}
        }

        /// <summary>
        /// reset the simulation to a random initial state and ready it for running (time = 0); the simulation will then start automatically
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void runButton_Click(object sender, RoutedEventArgs e)
        {
            //runSim();
        }

        private void BlueHandToolButton_Click(object sender, RoutedEventArgs e)
        {
            blueHandToolButton_IsChecked = !blueHandToolButton_IsChecked;

            MainWindow.SetControlFlag(MainWindow.CONTROL_MOLCONCS_ENABLED, false);
            /////gc.Rwc.RenderWindow.SetCurrentCursor(VTKGraphicsController.GET_CURSOR_ARROW);

            if (blueHandToolButton_IsChecked)
            {
                MainWindow.SetControlFlag(MainWindow.CONTROL_MOLCONCS_ENABLED, true);
                /////gc.Rwc.RenderWindow.SetCurrentCursor(VTKGraphicsController.GET_CURSOR_HAND);
            }
        }

        /// <summary>
        /// set or clear a particular control flag
        /// </summary>
        /// <param name="flag">flag to set</param>
        /// <param name="set">true for set, false for clear</param>
        public static void SetControlFlag(byte flag, bool set)
        {
            // set
            if (set == true)
            {
                controlFlags |= flag;
            }
            // clear
            else
            {
                controlFlags &= (byte)~flag;
            }

        }

        private void save3DView_Click(object sender, RoutedEventArgs e)
        {
            // Create a new save file dialog
            System.Windows.Forms.SaveFileDialog saveFileDialog1 = new System.Windows.Forms.SaveFileDialog();

            // Sets the current file name filter string, which determines 
            // the choices that appear in the "Save as file type" or 
            // "Files of type" box in the dialog box.
            saveFileDialog1.Filter = "Bitmap (*.bmp)|*.bmp|JPEG (*.jpg)|*.jpg|PNG (*.png)|*.png|TIFF (*.tif)|*.tif";
            saveFileDialog1.FilterIndex = 2;
            saveFileDialog1.RestoreDirectory = true;

            // Set image file format
            if (saveFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                vtkImageWriter writer;
                if (saveFileDialog1.FileName.EndsWith("bmp"))
                {
                    writer = new vtkBMPWriter();
                }
                else if (saveFileDialog1.FileName.EndsWith("jpg"))
                {
                    writer = new vtkJPEGWriter();
                    vtkJPEGWriter jw = (vtkJPEGWriter)writer;
                    jw.SetQuality(100);
                    jw.SetProgressive(0);
                }
                else if (saveFileDialog1.FileName.EndsWith("png"))
                {
                    writer = new vtkPNGWriter();
                }
                else if (saveFileDialog1.FileName.EndsWith("tif"))
                {
                    writer = new vtkTIFFWriter();
                }
                else
                {
                    writer = new vtkBMPWriter();
                }

                //gc.SaveToFile(saveFileDialog1.FileName, writer);
            }
        }

        private void fitZeroForceCheckBox_Click(object sender, RoutedEventArgs e)
        {
            //MainWindow.SetControlFlag(MainWindow.CONTROL_ZERO_FORCE, (bool)fitZeroForceCheckBox.IsChecked);
            //if (sim != null)
            //{
            //    gc.ToggleCellFitTracks(MainWindow.CheckControlFlag(MainWindow.CONTROL_ZERO_FORCE));
            //    gc.DrawFrame(sim.GetProgressPercent());
            //    if (selectedCell != null)
            //    {
            //        FitBoxUpdate();
            //    }
            //}
            //// TODO: These Force calls will be a problem with multiple VTK windows...
            //if (gc != null && gc.Rwc != null && this.LPFittingToolWindow.IsVisible)
            //{
            //    gc.Rwc.Focus();
            //}
        }

        private void fitCellOpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            cellOpacity = (double)fitCellOpacitySlider.Value;
            //if (sim != null)
            //{
            //    gc.CellController.SetCellOpacities(cellOpacity);
            //    gc.DrawFrame(sim.GetProgressPercent());
            //}
            //// TODO: These Force calls will be a problem with multiple VTK windows...
            //if (gc != null && gc.Rwc != null)
            //{
            //    gc.Rwc.Focus();
            //}
        }



        private void run()
        {
            
        }

        /// <summary>
        /// draw the initial cell states
        /// </summary>
        /// <param name="newFile">true to indicate we are reading a new file</param>
        /// <param name="completeReset">true to fully reinitialize the simulation</param>
        /// <param name="xmlScenarioString">string representation of a scenario</param>
//        private void initialState(bool newFile, bool completeReset, string xmlScenarioString)
//        {
//            // if we read a new file we may have to disconnect event handlers if they were connected previously;
//            // we always must deserialize the file
//            if (newFile == true)
//            {
//                if (configurator != null)
//                {
//                    // if we configured a simulation prior to this call, remove all property changed event handlers
//                    for (int i = 0; i < configurator.SimConfig.entity_repository.box_specifications.Count; i++)
//                    {
//                        configurator.SimConfig.entity_repository.box_specifications[i].PropertyChanged -= configurator.GUIInteractionToWidgetCallback;
//                    }
//                    for (int i = 0; i < configurator.SimConfig.scenario.regions.Count; i++)
//                    {
//                        configurator.SimConfig.scenario.regions[i].PropertyChanged -= configurator.GUIRegionSurfacePropertyChange;
//                    }
//                    for (int i = 0; i < configurator.SimConfig.entity_repository.gaussian_specifications.Count; i++)
//                    {
//                        configurator.SimConfig.entity_repository.gaussian_specifications[i].PropertyChanged -= configurator.GUIGaussianSurfaceVisibilityToggle;
//                    }
//                }
//                // load past experiment
//                if (xmlScenarioString != "")
//                {
//                    // reinitialize the configurator
//                    configurator = new SimConfigurator();
//                    // catch xaml parse exception if it's not a good sim config file
//                    try
//                    {
//                        configurator.DeserializeSimConfigFromString(xmlScenarioString);
//                    }
//                    catch
//                    {
//                        //MessageBoxResult tmp = MessageBox.Show("That configuration has problems. Please select another experiment.");
//                        handleLoadFailure("That configuration has problems. Please select another experiment.");
//                        return;
//                    }
//                }
//                else
//                {
//                    configurator = new SimConfigurator(xmlPath.LocalPath);
//                    // catch xaml parse exception if it's not a good sim config file
//                    try
//                    {
//                        //SKG Tuesday, May 07, 2013 - HERE MUST REPLACE WITH JSON DESERIALIZER
//                        configurator.DeserializeSimConfig();
//                        configurator.SimConfig.ChartWindow = ReacComplexChartWindow;
//                    }
//                    catch
//                    {
//                        //MessageBox.Show("There is a problem loading the configuration file.\nPress OK, then try to load another.", "Application error", MessageBoxButton.OK, MessageBoxImage.Error);
//                        handleLoadFailure("There is a problem loading the configuration file.\nPress OK, then try to load another.");
//                        return;
//                    }
//                }
//                orig_content = configurator.SerializeSimConfigToStringSkipDeco();
//                orig_path = System.IO.Path.GetDirectoryName(xmlPath.LocalPath);
//            }

//            // (re)connect the handlers for the property changed event
//            for (int i = 0; i < configurator.SimConfig.entity_repository.box_specifications.Count; i++)
//            {
//                configurator.SimConfig.entity_repository.box_specifications[i].PropertyChanged += configurator.GUIInteractionToWidgetCallback;
//            }
//            for (int i = 0; i < configurator.SimConfig.scenario.regions.Count; i++)
//            {
//                configurator.SimConfig.scenario.regions[i].PropertyChanged += configurator.GUIRegionSurfacePropertyChange;
//            }
//            for (int i = 0; i < configurator.SimConfig.entity_repository.gaussian_specifications.Count; i++)
//            {
//                configurator.SimConfig.entity_repository.gaussian_specifications[i].PropertyChanged += configurator.GUIGaussianSurfaceVisibilityToggle;
//            }

//            // GUI Resources
//            // Set the data context for the main tab control config GUI
//            this.SimConfigToolWindow.DataContext = configurator.SimConfig;

//            // set up the simulation
//            //////////if (sim.setupSimulation(completeReset) == false)
//            //////////{
//            //////////    handleLoadFailure("There is a problem loading the configuration file.\nPress OK, then try to load another.");
//            //////////    return;
//            //////////}

//            // Create all VTK visualization pipelines and elements
//            gc.CreatePipelines();

//            // clear the vcr cache
//            if (vcrControl != null)
//            {
//                vcrControl.ReleaseVCR();
//            }

//            if (newFile)
//            {
//                gc.recenterCamera();
//            }
//            gc.Rwc.Invalidate();

//            // TODO: Need to do this for all GCs eventually...
//            // Add the RegionControl interaction event handlers here for easier reference to callback method
//            foreach (KeyValuePair<string, RegionWidget> kvp in gc.Regions)
//            {
//                // NOTE: For now not doing any callbacks on property change for RegionControls...
//                kvp.Value.ClearCallbacks();
//                kvp.Value.AddCallback(new RegionWidget.CallbackHandler(gc.WidgetInteractionToGUICallback));
//                kvp.Value.AddCallback(new RegionWidget.CallbackHandler(SimConfigToolWindow.RegionFocusToGUISection));
//            }

//            VCR_Toolbar.IsEnabled = false;
//            gc.ToolsToolbar_IsEnabled = true;
//            gc.DisablePickingButtons();

//#if LANGEVIN_TIMING
//            gc.CellRenderMethod = CellRenderMethod.CELL_RENDER_VERTS;
//#endif
//            loadSuccess = true;
//        }

        private void initialState(bool newFile, bool completeReset, string jsonScenarioString)
        {
            // if we read a new file we may have to disconnect event handlers if they were connected previously;
            // we always must deserialize the file
            if (newFile == true)
            {
                if (configurator != null)
                {
                    // if we configured a simulation prior to this call, remove all property changed event handlers

                    //skg REMOVE VTK AND GRAPHICS STUFF FOR NOW
                    //////////for (int i = 0; i < configurator.SimConfig.entity_repository.box_specifications.Count; i++)
                    //////////{
                    //////////    configurator.SimConfig.entity_repository.box_specifications[i].PropertyChanged -= configurator.GUIInteractionToWidgetCallback;
                    //////////}
                    //////////for (int i = 0; i < configurator.SimConfig.scenario.regions.Count; i++)
                    //////////{
                    //////////    configurator.SimConfig.scenario.regions[i].PropertyChanged -= configurator.GUIRegionSurfacePropertyChange;
                    //////////}
                    //////////for (int i = 0; i < configurator.SimConfig.entity_repository.gaussian_specifications.Count; i++)
                    //////////{
                    //////////    configurator.SimConfig.entity_repository.gaussian_specifications[i].PropertyChanged -= configurator.GUIGaussianSurfaceVisibilityToggle;
                    //////////}
                }
                // load past experiment
                if (jsonScenarioString != "")
                {
                    // reinitialize the configurator
                    configurator = new SimConfigurator();
                    // catch xaml parse exception if it's not a good sim config file
                    try
                    {
                        configurator.DeserializeSimConfigFromString(jsonScenarioString);
                    }
                    catch
                    {
                        //MessageBoxResult tmp = MessageBox.Show("That configuration has problems. Please select another experiment.");
                        handleLoadFailure("That configuration has problems. Please select another experiment.");
                        return;
                    }
                }
                else
                {
                    configurator = new SimConfigurator(jsonPath.LocalPath);
                    // catch xaml parse exception if it's not a good sim config file
                    try
                    {
                        //SKG Tuesday, May 07, 2013 - HERE MUST REPLACE WITH JSON DESERIALIZER
                        configurator.DeserializeSimConfig();
                        configurator.SimConfig.ChartWindow = ReacComplexChartWindow;
                    }
                    catch
                    {
                        //MessageBox.Show("There is a problem loading the configuration file.\nPress OK, then try to load another.", "Application error", MessageBoxButton.OK, MessageBoxImage.Error);
                        handleLoadFailure("There is a problem loading the configuration file.\nPress OK, then try to load another.");
                        return;
                    }
                }
                orig_content = configurator.SerializeSimConfigToStringSkipDeco();
                //orig_path = System.IO.Path.GetDirectoryName(xmlPath.LocalPath);
                orig_path = System.IO.Path.GetDirectoryName(jsonPath.LocalPath);
            }

            // (re)connect the handlers for the property changed event
            //skg daphne REMOVE VTK AND GRAPHICS STUFF FOR NOW
            //////////for (int i = 0; i < configurator.SimConfig.entity_repository.box_specifications.Count; i++)
            //////////{
            //////////    configurator.SimConfig.entity_repository.box_specifications[i].PropertyChanged += configurator.GUIInteractionToWidgetCallback;
            //////////}
            //////////for (int i = 0; i < configurator.SimConfig.scenario.regions.Count; i++)
            //////////{
            //////////    configurator.SimConfig.scenario.regions[i].PropertyChanged += configurator.GUIRegionSurfacePropertyChange;
            //////////}
            //////////for (int i = 0; i < configurator.SimConfig.entity_repository.gaussian_specifications.Count; i++)
            //////////{
            //////////    configurator.SimConfig.entity_repository.gaussian_specifications[i].PropertyChanged += configurator.GUIGaussianSurfaceVisibilityToggle;
            //////////}

            // GUI Resources
            // Set the data context for the main tab control config GUI
            this.SimConfigToolWindow.DataContext = configurator.SimConfig;

            // set up the simulation
            //////////if (sim.setupSimulation(completeReset) == false)
            //////////{
            //////////    handleLoadFailure("There is a problem loading the configuration file.\nPress OK, then try to load another.");
            //////////    return;
            //////////}

            //////////// Create all VTK visualization pipelines and elements
            //////////gc.CreatePipelines();

            //////////// clear the vcr cache
            //////////if (vcrControl != null)
            //////////{
            //////////    vcrControl.ReleaseVCR();
            //////////}

            //////////if (newFile)
            //////////{
            //////////    gc.recenterCamera();
            //////////}
            //////////gc.Rwc.Invalidate();

            //////////// TODO: Need to do this for all GCs eventually...
            //////////// Add the RegionControl interaction event handlers here for easier reference to callback method
            //////////foreach (KeyValuePair<string, RegionWidget> kvp in gc.Regions)
            //////////{
            //////////    // NOTE: For now not doing any callbacks on property change for RegionControls...
            //////////    kvp.Value.ClearCallbacks();
            //////////    kvp.Value.AddCallback(new RegionWidget.CallbackHandler(gc.WidgetInteractionToGUICallback));
            //////////    kvp.Value.AddCallback(new RegionWidget.CallbackHandler(SimConfigToolWindow.RegionFocusToGUISection));
            //////////}

            //////////VCR_Toolbar.IsEnabled = false;
            //////////gc.ToolsToolbar_IsEnabled = true;
            //////////gc.DisablePickingButtons();

#if LANGEVIN_TIMING
            gc.CellRenderMethod = CellRenderMethod.CELL_RENDER_VERTS;
#endif
            loadSuccess = true;
        }

        /// <summary>
        /// blank the vtk screen and bulk of the gui
        /// </summary>
        private void handleLoadFailure(string s)
        {
            loadSuccess = false;
            configurator.SimConfig = new SimConfiguration();
            configurator.SimConfig.experiment_name = "";
            configurator.SimConfig.experiment_description = "";
            SimConfigToolWindow.DataContext = configurator.SimConfig;
            //////////gc.Cleanup();
            //////////gc.Rwc.Invalidate();
            displayTitle("");
            MessageBox.Show(s, "Application error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// display a string in the window's title bar;
        /// default is "PlazaSur - file" where 'file' is the currently loaded simulation file
        /// </summary>
        private void displayTitle(string s = null)
        {
            if (s == null)
            {
                Title = "Daphne - " + extractFileName();
            }
            else
            {
                Title = "Daphne - " + s;
            }
        }

        /// <summary>
        ///  enable/disable critical, i.e. when a config load error happens, gui elements
        /// </summary>
        /// <param name="enable">false to disable</param>
        private void enableCritical(bool enable)
        {
            resetButton.IsEnabled = enable;
            runButton.IsEnabled = enable;
            analysisMenu.IsEnabled = enable;
            saveScenario.IsEnabled = enable;
            saveScenarioAs.IsEnabled = enable;
        }

        /// <summary>
        /// initiate a simulation run
        /// </summary>
        private void runSim()
        {
            MessageBox.Show("In runSim()");
            //////////sim.refreshDatabaseBufferRows();
            //////////if (sim.RunStatus == Simulation.RUNSTAT_RUN)
            //////////{
            //////////    //resetButton.IsEnabled = true;
            //////////    resetButton.Content = "Reset";
            //////////    sim.RunStatus = Simulation.RUNSTAT_PAUSE;
            //////////    runButton.Content = "Continue";
            //////////    statusBarMessagePanel.Content = "Paused...";
            //////////    runButton.ToolTip = "Continue the Simulation.";
            //////////}
            //////////else if (sim.RunStatus == Simulation.RUNSTAT_PAUSE)
            //////////{
            //////////    //resetButton.IsEnabled = false;
            //////////    resetButton.Content = "Abort";
            //////////    sim.RunStatus = Simulation.RUNSTAT_RUN;
            //////////    runButton.Content = "Pause";
            //////////    statusBarMessagePanel.Content = "Running...";
            //////////}
            //////////else
            //////////{
            //////////    if (vcrControl != null)
            //////////    {
            //////////        vcrControl.SetInactive();
            //////////    }

            //////////    // only check for unique names if database writing is on and the unique names option is on
            //////////    if (skipDataWriteMenu.IsChecked == false && uniqueNamesMenu.IsChecked == true)
            //////////    {
            //////////        string nameTemplate = "Enter a unique name here...";

            //////////        // if database writing is on, notify the user that it may be a good idea to have unique experiment names
            //////////        // don't consider the template name a good idea
            //////////        if (configurator.SimConfig.experiment_name == nameTemplate || checkExpNameUniqueness(configurator.SimConfig.experiment_name) == false)
            //////////        {
            //////////            string messageBoxText = "Consider using a unique, meaningful experiment name.\n\nDo you want to go back to Sim Setup to make this change?";
            //////////            string caption = "Experiment name unchanged";
            //////////            MessageBoxButton button = MessageBoxButton.YesNoCancel;
            //////////            MessageBoxImage icon = MessageBoxImage.Warning;

            //////////            // Display message box
            //////////            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);

            //////////            // Process message box results
            //////////            switch (result)
            //////////            {
            //////////                case MessageBoxResult.Yes:
            //////////                    // show the sim setup panels if they are closed
            //////////                    SimConfigToolWindow.Activate();
            //////////                    // switch to the panel that has the name, give the name box the focus and change
            //////////                    // its content to something indicating what the user should do
            //////////                    SimConfigToolWindow.SelectSimSetupInGUISetExpName(nameTemplate);
            //////////                    return;
            //////////                case MessageBoxResult.No:
            //////////                    break;
            //////////                default:
            //////////                    return;
            //////////            }
            //////////        }
            //////////    }

            //////////    if (configurator.SerializeSimConfigToStringSkipDeco() == orig_content)
            //////////    {
            //////////        // initiating a run starts always at repetition 1
            //////////        repetition = 1;
            //////////        // call with false (lockSaveStartSim(false)) or modify otherwise to enable the simulation to continue from the last visible state
            //////////        // after a run or vcr playback
            //////////        lockSaveStartSim(MainWindow.CheckControlFlag(MainWindow.CONTROL_FORCE_RESET));
            //////////    }
            //////////    else
            //////////    {
            //////////        // Configure the message box to be displayed
            //////////        string messageBoxText = "Scenario parameters have changed. Do you want to overwrite the information in " + extractFileName() + "?";
            //////////        string caption = "Scenario Changed";
            //////////        MessageBoxButton button = MessageBoxButton.YesNoCancel;
            //////////        MessageBoxImage icon = MessageBoxImage.Warning;

            //////////        // Display message box
            //////////        MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);

            //////////        // Process message box results
            //////////        switch (result)
            //////////        {
            //////////            case MessageBoxResult.Yes:
            //////////                configurator.SerializeSimConfigToFile();
            //////////                orig_content = configurator.SerializeSimConfigToStringSkipDeco();
            //////////                orig_path = Path.GetDirectoryName(xmlPath.LocalPath);
            //////////                // initiating a run starts always at repetition 1
            //////////                repetition = 1;
            //////////                lockSaveStartSim(true);
            //////////                break;
            //////////            case MessageBoxResult.No:
            //////////                if (saveScenarioUsingDialog() == true)
            //////////                {
            //////////                    // initiating a run starts always at repetition 1
            //////////                    repetition = 1;
            //////////                    lockSaveStartSim(true);
            //////////                }
            //////////                break;
            //////////            case MessageBoxResult.Cancel:
            //////////                // Do nothing...
            //////////                break;
            //////////        }
            //////////    }
            //////////    if (sim.RunStatus == Simulation.RUNSTAT_RUN)
            //////////    {
            //////////        runButton.Content = "Pause";
            //////////        runButton.ToolTip = "Pause the Simulation.";
            //////////        statusBarMessagePanel.Content = "Running...";
            //////////    }
            //////////}
        }

        private void hideFit()
        {
            // clear the fit text and hide all controls associated with the fitting
            fitMessageBox.Text = "";

            // immediate reset
            fitStatus = PROGRESS_RESET;

            // hide the fitting tab and clear all fit settings
            this.LPFittingToolWindow.Close();
            this.menu_ActivateLPFitting.IsEnabled = false;
            // And hide stats results chart for now
            //////////this.ChartViewDocWindow.Close();
            this.menu_ActivateAnalysisChart.IsEnabled = false;
            if (MainWindow.CheckControlFlag(MainWindow.CONTROL_PICKING_ENABLED) == true)
            {
                MainWindow.SetControlFlag(MainWindow.CONTROL_PICKING_ENABLED, false);
                //////////gc.CellController.SetCellOpacities(1.0);
                //////////gc.DisablePickingButtons();
            }
        }

        // This sets whether the Open command can be executed, which enables/disables the menu item
        private void CommandBindingOpen_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBindingOpen_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            Nullable<bool> result = loadScenarioUsingDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // show the inital state
                lockAndResetSim(true, "");
                if (loadSuccess == false)
                {
                    return;
                }
                SimConfigToolWindow.IsEnabled = true;
                saveScenario.IsEnabled = true;
                displayTitle();
            }
        }

        // This sets whether the Save command can be executed, which enables/disables the menu item
        private void CommandBindingSave_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBindingSave_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            FileInfo fi = new FileInfo(configurator.FileName);

            if (fi.IsReadOnly == false)
            {
                configurator.SerializeSimConfigToFile();
                orig_content = configurator.SerializeSimConfigToStringSkipDeco();
            }
            else
            {
                string messageBoxText = "The file is write protected: " + configurator.FileName;
                string caption = "File write protected";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Warning;

                // display message box
                MessageBox.Show(messageBoxText, caption, button, icon);
            }
        }

















        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            dw.Close();
        }

        // close the application
        private void CloseApp()
        {            
            this.Close();
        }

        private void exitApp_Click(object sender, RoutedEventArgs e)
        {
            CloseApp();
        }
    }
}
