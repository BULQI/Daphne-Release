// enable ASSUME_DEBUGGER for "Start without debugging", i.e. when Debugger.IsAttached == false
//#define ASSUME_DEBUGGER // NOTE: NEVER CHECK IN ENABLED

// enable RUNNING_PROFILER when running a profiling session; this will set paths and the database connection string
//#define RUNNING_PROFILER // NOTE: NEVER CHECK IN ENABLED

// enable CONTROL_PROFILER to automatically start and stop the simulation with a profiler session for accurate timing
// NOTE: this will only work if the scenario to be profiled is saved in the last scenario preference variable; to do so,
// run the app in the profiler having this flag disabled (but enable RUNNING_PROFILER), open the desired scenario,
// select the 'run last scenario' option in the menu, close the app, enable the flag, recompile, and profile
//#define CONTROL_PROFILER // NOTE: NEVER CHECK IN ENABLED

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Runtime.InteropServices;

using ActiproSoftware.Windows.Controls.Docking;
using Kitware.VTK;

using Ninject;
using Ninject.Parameters;

using Daphne;
using ManifoldRing;
using Workbench;

using System.Collections.ObjectModel;
using Newtonsoft.Json;

using DaphneGui.Pushing;

using SBMLayer;
using System.Security.Principal;
using System.Globalization;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static ToolWinBase toolWin;
        public static ToolWinBase ToolWin
        {
            get
            {
                return toolWin;
            }
            set
            {
                toolWin = value;
            }
        }

        public static DependencyProperty ToolWinTypeProperty =
            DependencyProperty.Register("ToolWinType", typeof(ToolWindowType), typeof(MainWindow), new FrameworkPropertyMetadata(ToolWindowType.BaseType));

        public ToolWindowType ToolWinType
        {
            get
            {
                return (ToolWindowType)GetValue(ToolWinTypeProperty);
            }
            set
            {
                SetValue(ToolWinTypeProperty, value);
            }
        }

        /// <summary>
        /// the absolute path where the installed, running executable resides
        /// </summary>
        public static string appPath;

        /// <summary>
        /// Path of the executable file in installation folder
        /// </summary>
        public string execPath;

        private DocWindow dw;
        private Thread simThread;
        private VCRControl vcrControl = null;
        public static Cell selectedCell = null;
        public static Object cellFitLock = new Object();
        public static double cellOpacity = 1.0;

        private static SimulationBase sim;
        public static SimulationBase Sim
        {
            get { return sim; }
            set { sim = value; }
        }

        private Process devHelpProc;
        private static SystemOfPersistence sop = null;
        private static int repetition;
        private static bool argDev = false, argBatch = false, argSave = false;
        private string argScenarioFile = "";
        private bool mutex = false;
        public ManualResetEvent runFinishedEvent = new ManualResetEvent(true);

        /// <summary>
        /// uri for the scenario file
        /// </summary>
        public static Uri protocol_path;
        private string orig_content, orig_path, SBMLFolderPath;
        private bool tempFileContent = false, postConstruction = false;

        private string orig_daphne_store_content, orig_user_store_content;

        private bool exportAllFlag = false;
        private string igGeneFolderName = "";

        /// <summary>
        /// constants used by the simulation and gui
        /// </summary>
        public static byte CONTROL_NONE = 0,
                           CONTROL_FORCE_RESET = (1 << 0),
                           CONTROL_PAST_LOAD = (1 << 1),
                           CONTROL_ZERO_FORCE = (1 << 2),
                           CONTROL_NEW_RUN = (1 << 3),
                           CONTROL_UPDATE_GUI = (1 << 4),
                           CONTROL_MOUSE_DRAG = (1 << 5);


        public static byte controlFlags = CONTROL_NONE;

        /// <summary>
        /// constants used to set the left mouse button state
        /// </summary>
        public static byte MOUSE_LEFT_NONE = 0,
                           MOUSE_LEFT_TRACK = 1,
                           MOUSE_LEFT_CELL_MOLCONCS = 2;

        public static byte mouseLeftState = MOUSE_LEFT_CELL_MOLCONCS; //Temporary - until Tracks are working.  //MOUSE_LEFT_NONE;

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


        private static IVTKGraphicsController gc;
        private static IVTKDataBasket vtkDataBasket;

        /// <summary>
        /// retrieve a pointer to the (for now, singular) VTK graphics actors
        /// </summary>
        public static IVTKGraphicsController GC
        {
            get { return gc; }
        }

        /// <summary>
        /// retrieve a pointer to the VTK data basket object
        /// </summary>
        public static IVTKDataBasket VTKBasket
        {
            get { return vtkDataBasket; }
        }

        /// <summary>
        /// retrieve a pointer to the configurator
        /// </summary>
        public static SystemOfPersistence SOP
        {
            get { return sop; }
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
            if (argBatch == true)
            {
                return true;
            }
            else
            {
                return false;
            }
#endif
        }

        public CellInfo SelectedCellInfo { get; set; }
        public ObservableCollection<CellMolecularInfo> currentConcs { get; set; }

        public static RoutedCommand SelectReportFolderCommand = new RoutedCommand();
        public static DocumentWindow ST_VTKDisplayDocWindow;
        public static CellStudioToolWindow ST_CellStudioToolWindow;
        public static ComponentsToolWindow ST_ComponentsToolWindow;
        public static ChartViewToolWindow ST_ReacComplexChartWindow;
        public static RenderSkinWindow ST_RenderSkinWindow;

        


        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        public MainWindow()
        {
            //This allows you to debug if you are running an installed version of Daphne
            //Debugger.Launch();

            InitializeComponent();

            ST_ReacComplexChartWindow = ReacComplexChartWindow;
            ST_RenderSkinWindow = renderSkinWindow;
            ST_VTKDisplayDocWindow = VTKDisplayDocWindow;
            ST_CellStudioToolWindow = CellStudioToolWindow;
            ST_ComponentsToolWindow = ComponentsToolWindow;

            this.ToolWinCellInfo.Close();

            SelectedCellInfo = new CellInfo();
            currentConcs = new ObservableCollection<CellMolecularInfo>();

            //DO NOT DELETE THIS
            //This code creates the DaphneStore and UserStore.
            //Only run this code during development.
            //Once development is completed editing of the DaphneStore and UserStore should be done through application (GUI).
            //Running this after development will make all the entities in the Daphne and User stores appear
            // to be different than the entities at the Protocol and Entity levels. 
            if (false)
            {
                try
                {
                    CreateDaphneAndUserStores();
                }
                catch (Exception e)
                {
                    showExceptionBox(exceptionMessage(e));
                }

            }
            //DO NOT DELETE THIS
            //This code creates the predefined scenarios.
            //Running this using the existing stores will not break user scenarios.
            if (false)
            {
                //This code re-generates the scenarios - DO NOT DELETE
                try
                {
                    CreateAndSerializeDaphneProtocols();
                }
                catch (Exception e)
                {
                    showExceptionBox(exceptionMessage(e));
                }
            }

            // NEED TO UPDATE RECENT FILES LIST CODE FOR DAPHNE!!!!

            // implementing the recent files list
            //RecentFileList.MaxNumberOfFiles = 10;
            // disable the following to cause saving into the registry
            // on my Windows 7 machine, using the xml persister will create the file C:\Users\Harald\AppData\Roaming\Microsoft\PlazaSur\RecentFileList.xml
            // and save the history there
            //RecentFileList.UseXmlPersister();
            // the event handler to run when clicking on an entry in the recent files list
            //RecentFileList.MenuClick += (s, e) => loadScenarioFromFile(e.Filepath);

            string[] args = Environment.GetCommandLineArgs();

            // current options:
            // -batch willrun the app automatically and will close it as soon as it is finished
            // -dev sets appPath to visual studio; omit this option when running an installed version
            // -help prints user help on command usage

            // when not running in Visual Studio, check if there are options
            if (AssumeIDE() == false && args.Length > 1)
            {
                AttachConsole(ATTACH_PARENT_PROCESS);
                for (int i = 1; i < args.Length; i++)
                {
                    string s = args[i].ToLowerInvariant();

                    if (s == "-help" || s == "-h")
                    {
                        Console.WriteLine("\n\nCommand line options are not case sensitive.");
                        Console.WriteLine("They can be abbreviated with the option's first letter.");
                        Console.WriteLine("\n -help or -h displays this online help.");
                        Console.WriteLine("\n -batch or -b causes the simulation to start running after it loads\n  and will close the application upon simulation finish.");
                        Console.WriteLine("\n -dev or -d sets the application path to the Visual Studio project;\n  omit when running a non-developer (installer) version.");
                        Console.WriteLine("\n -save or -s saves the simulation state upon termination;\n  uses the reporter name if one is entered.");
                        Console.WriteLine("\n -file:name or -f:name specifies the simulation file by name.");
                        Console.WriteLine("\nPress Enter to return to the DOS prompt.");
                        Environment.Exit(-1);
                        return;
                    }
                    else if (s == "-batch" || s == "-b")
                    {
                        argBatch = true;
                    }
                    else if (s == "-dev" || s == "-d")
                    {
                        argDev = true;
                    }
                    else if (s == "-save" || s == "-s")
                    {
                        argSave = true;
                    }
                    else if (s.StartsWith("-file") == true || s.StartsWith("-f") == true)
                    {
                        string[] arr = s.Split(':');

                        if (arr.Length == 2)
                        {
                            argScenarioFile = arr[1];
                        }
                        else
                        {
                            Console.WriteLine("\nImproper command line format. Closing.");
                            Console.WriteLine("\nPress Enter to return to the DOS prompt.");
                            Environment.Exit(-1);
                            return;
                        }
                    }
                    else
                    {
                        Console.WriteLine("\nYou specified an unknown command line option. Closing.");
                        Console.WriteLine("\nPress Enter to return to the DOS prompt.");
                        Environment.Exit(-1);
                        return;
                    }
                }
            }

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
                execPath = appPath;
            }
            else
            {
                appPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.ApplicationData) + @"\DaphneGui";
                execPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            }

            SetPathVariable();

            //Defines default location of SBML folder within Daphne's directory structure
            SBMLFolderPath = appPath + @"\Config\SBML\";
            //Used to check that SBML directory can be the initial directory
            string SBML_folder = new Uri(SBMLFolderPath).LocalPath;
            if (Directory.Exists(SBML_folder) == false)
            {
                Directory.CreateDirectory(SBML_folder);
            }

            // handle the application properties
            string file;

            autoZoomFitMenu.IsChecked = Properties.Settings.Default.autoZoomFit;
            openLastScenarioMenu.IsChecked = Properties.Settings.Default.lastOpenScenario != "";
            skipDataWriteMenu.IsChecked = Properties.Settings.Default.skipDataWrites;
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

            if (argScenarioFile != "")
            {
                file = argScenarioFile;
            }
            else if (openLastScenarioMenu.IsChecked == true)
            {
                file = Properties.Settings.Default.lastOpenScenario;
            }
            else
            {
                file = "daphne_driver_locomotion_scenario.json";
                //file = "daphne_vatRC_ligand_receptor_scenario.json";
            }

            int repeat = 0;
            bool file_exists;

            do
            {
                // attempt to load a default simulation file; if it doesn't exist disable the gui
                protocol_path = new Uri(appPath + @"\Config\" + file);
                orig_path = System.IO.Path.GetDirectoryName(protocol_path.LocalPath);

                file_exists = File.Exists(protocol_path.LocalPath);

                if (file_exists)
                {
                    ProtocolToolWindow.IsEnabled = true;
                    saveScenario.IsEnabled = true;
                    //displayTitle();
                }
                else
                {
                    ProtocolToolWindow.IsEnabled = false;
                    saveScenario.IsEnabled = false;

                    // notify the user; loading will always fail for protocols from the UserScenarios folder
                    if (openLastScenarioMenu.IsChecked == true)
                    {
                        string messageBoxText = "Opening the last protocol failed. Starting up with blank window.\nFunction only supported for files in the \\Config folder.";
                        string caption = "Protocol load failure";
                        MessageBoxButton button = MessageBoxButton.OK;
                        MessageBoxImage icon = MessageBoxImage.Error;

                        // Display message box
                        MessageBox.Show(messageBoxText, caption, button, icon);
                    }

                    // allow one repetition with the blank scenario
                    if (repeat < 1)
                    {
                        file = "daphne_blank_scenario.json";
                    }
                }
                repeat++;
            } while (file_exists == false && repeat < 2);

            ProtocolToolWindow.MW = this;
            ComponentsToolWindow.MW = this;

            // Hide fitting tab control until sim has ended
            this.LPFittingToolWindow.Close();
            this.menu_ActivateLPFitting.IsEnabled = false;
            this.ExportMenu.IsEnabled = false;
            // And hide stats results chart for now
            //this.ChartViewDocWindow.Close();

#if DATABASE_HOOKED_UP        
            this.menu_ActivateAnalysisChart.IsEnabled = false;
#endif

            if (file_exists)
            {
                sop = new SystemOfPersistence();

                //load renderskin before loading protocol
                {
                    //setup render skin 
                    string SkinFolderPath = new Uri(appPath + @"\Config\RenderSkin\").LocalPath;
                    if (!Directory.Exists(SkinFolderPath))
                    {
                        Directory.CreateDirectory(SkinFolderPath);
                        RenderSkin sk = new RenderSkin("default_skin", null);
                        sk.FileName = SkinFolderPath + "default_skin.json";
                        sop.SkinList.Add(sk);
                    }

                    string[] files = Directory.GetFiles(SkinFolderPath, "*.json");
                    foreach (string skfile in files)
                    {
                        try
                        {
                            RenderSkin sk = RenderSkin.DeserializeFromFile(skfile);
                            sop.SkinList.Add(sk);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Loading RenderSkin file {0} failed: {1}", skfile, e.ToString());
                        }
                    }
                    //create a default if none exxists
                    if (sop.SkinList.Count == 0)
                    {
                        RenderSkin sk = new RenderSkin("default_skin", null);
                        sk.FileName = SkinFolderPath + "default_skin.json";
                        sk.SerializeToFile();
                        sop.SkinList.Add(sk);
                    }

                }

                initialState(true, true, ReadJson(""));
                enableCritical(loadSuccess);
                if (loadSuccess == true)
                {
                    // after doing a full reset, don't require one immediately
                    MainWindow.SetControlFlag(MainWindow.CONTROL_FORCE_RESET, false);
                    UpdateGraphics();
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

            vcrControl = new VCRControl();

            //setup render skin 
            /*
            string SkinFolderPath = new Uri(appPath + @"\Config\RenderSkin\").LocalPath;
            if (!Directory.Exists(SkinFolderPath))
            {
                Directory.CreateDirectory(SkinFolderPath);
            }
            try
            {
                string[] files = Directory.GetFiles(SkinFolderPath, "*.json");
                foreach (string skfile in files)
                {
                    RenderSkin sk = RenderSkin.DeserializeFromFile(skfile);
                    sop.SkinList.Add(sk);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Loading RenderSkin failed: {0}", e.ToString());
            }
            */



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
            simThread = new Thread(new ThreadStart(run)) { IsBackground = true };
            //simThread.Priority = ThreadPriority.Normal;
            simThread.Start();
            dw = new DocWindow();

            // immediately run the simulation
            if (ControlledProfiling() == true)
            {
                runSim();
            }
            postConstruction = true;
        }

        public void UpdateGraphics()
        {
            if (gc == null) return;
            vtkDataBasket.UpdateData();
            gc.DrawFrame(sim.GetProgressPercent());
        }


        /// <summary>
        /// Code to create daphnestore 
        /// ONCE CREATED, DON'T NEED THIS CODE EVER AGAIN!
        /// </summary>
        public void CreateDaphneAndUserStores()
        {
            var userstore = new Level("Config\\daphne_userstore.json", "Config\\temp_userstore.json");
            var daphnestore = new Level("Config\\daphne_daphnestore.json", "Config\\temp_daphnestore.json");
            ProtocolCreators.CreateDaphneAndUserStores(daphnestore, userstore);
        }

        /// <summary>
        /// Create and serialize all scenarios
        /// </summary>
        public void CreateAndSerializeDaphneProtocols()
        {
            //BLANK SCENARIO
            var protocol = new Protocol("Config\\daphne_blank_scenario.json", "Config\\temp_protocol.json", Protocol.ScenarioType.TISSUE_SCENARIO);

            ProtocolCreators.CreateBlankProtocol(protocol);
            //serialize to json
            protocol.SerializeToFile();

            //DRIVER-LOCOMOTOR SCENARIO
            protocol = new Protocol("Config\\daphne_driver_locomotion_scenario.json", "Config\\temp_protocol.json", Protocol.ScenarioType.TISSUE_SCENARIO);

            ProtocolCreators.CreateDriverLocomotionProtocol(protocol);
            // serialize to json
            protocol.SerializeToFile();

            //DIFFUSIION SCENARIO
            protocol = new Protocol("Config\\daphne_diffusion_scenario.json", "Config\\temp_protocol.json", Protocol.ScenarioType.TISSUE_SCENARIO);

            ProtocolCreators.CreateDiffusionProtocol(protocol);
            //Serialize to json
            protocol.SerializeToFile();

            //LIGAND-RECEPTOR SCENARIO
            protocol = new Protocol("Config\\daphne_ligand_receptor_scenario.json", "Config\\temp_protocol.json", Protocol.ScenarioType.TISSUE_SCENARIO);

            ProtocolCreators.CreateLigandReceptorProtocol(protocol);
            //serialize to json
            protocol.SerializeToFile();

            //GC SCENARIO
            protocol = new Protocol("Config\\daphne_gc_cycling_scenario.json", "Config\\temp_protocol.json", Protocol.ScenarioType.TISSUE_SCENARIO);

            ProtocolCreators.CreateGCProtocol(protocol);
            //serialize to json
            protocol.SerializeToFile();

            // BLANK VAT-REACTION-COMPLEX SCENARIO
            protocol = new Protocol("Config\\daphne_vatRC_blank_scenario.json", "Config\\temp_protocol.json", Protocol.ScenarioType.VAT_REACTION_COMPLEX);
            ProtocolCreators.CreateVatRC_Blank_Protocol(protocol);
            // serialize
            protocol.SerializeToFile();

            // VAT REACTION-COMPLEX - LIGAND RECEPTOR SCENARIO
            protocol = new Protocol("Config\\daphne_vatRC_ligand_receptor_scenario.json", "Config\\temp_protocol.json", Protocol.ScenarioType.VAT_REACTION_COMPLEX);
            ProtocolCreators.CreateVatRC_LigandReceptor_Protocol(protocol);
            // serialize
            protocol.SerializeToFile();

            // VAT LIGAND REACTION-COMPLEX 2 SITE BINDING SCENARIO
            protocol = new Protocol("Config\\daphne_vatRC_2SiteAbBinding_scenario.json", "Config\\temp_protocol.json", Protocol.ScenarioType.VAT_REACTION_COMPLEX);
            ProtocolCreators.CreateVatRC_TwoSiteAbBinding_Protocol(protocol);
            // serialize
            protocol.SerializeToFile();






        }

        private void showScenarioInitial()
        {
            lockAndResetSim(true, ReadJson(""));
            if (loadSuccess == false)
            {
                return;
            }
            ProtocolToolWindow.IsEnabled = true;
            saveScenario.IsEnabled = true;
        }

        private void setScenarioPaths(string filename)
        {
            protocol_path = new Uri(filename);
            orig_path = System.IO.Path.GetDirectoryName(protocol_path.LocalPath);
            displayTitle();
        }

        private void loadScenarioFromFile(string filename)
        {
            showScenarioInitial();
            setScenarioPaths(filename);
        }

        private Nullable<bool> loadScenarioUsingDialog()
        {
            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = orig_path;
            dlg.DefaultExt = ".json"; // Default file extension
            dlg.Filter = "Sim Config JSON docs (.json)|*.json"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Save filename here, but deserialization will happen in lockAndResetSim->initialState call
                string filename = dlg.FileName;

                //RecentFileList.InsertFile(filename);
                setScenarioPaths(filename);
            }

            return result;
        }

        private void saveScenarioAs_Click(object sender, RoutedEventArgs e)
        {
            saveScenarioUsingDialog();
            tempFileContent = false;
        }


        /// <summary>
        /// Imports a model specification in SBML
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ImportSBML_Click(object sender, RoutedEventArgs e)
        {
            saveStoreFiles();

            //Check that previous changes are saved before loading new Protocol
            if (tempFileContent == true || saveTempFiles() == true)
            {
                applyTempFilesAndSave(true);
            }

            Protocol protocol = new Protocol("", "", Protocol.ScenarioType.TISSUE_SCENARIO);

            //Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = SBMLFolderPath;
            dlg.DefaultExt = ".xml"; // Default file extension
            dlg.Filter = "SBML files (.xml)|*.xml"; // Filter files by extension

            // Show open  file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                SBMLModel encodedSBML = new SBMLModel(dlg.FileName, protocol);
                //Extract directory path and file name of the file to be imported
                protocol = encodedSBML.ReadSBMLFile();
                if (protocol != null)
                {
                    if (encodedSBML.ContainsReactionComplex())
                    {
                        LoadReactionComplex(protocol);
                    }
                    else
                    {
                        LoadProtocolFromSBML(protocol);
                    }
                }
            }
        }

        /// <summary>
        /// Loads the imported reaction complex into the GUI
        /// gmk - SBML-specific. Needs to be modified for workbenches.
        /// </summary>
        /// <param name="protocol"></param>
        private void LoadReactionComplex(Protocol protocol)
        {
            //ReactionComplex that was added
            ConfigReactionComplex crc = protocol.entity_repository.reaction_complexes.Last();
                        // prevent when a fit is in progress
            lock (cellFitLock)
            {
                    sop.Protocol.entity_repository.reaction_complexes.Add(crc);
            
                //Add reaction complex
                foreach (ConfigMolecule cm in crc.molecules_dict.Values)
                {
                    sop.Protocol.entity_repository.molecules.Add(cm.Clone(null));
                }

                //Reactions in the reaction complex
                foreach (ConfigReaction cr in crc.reactions)
                {
                    if (!sop.Protocol.entity_repository.reaction_templates_dict.ContainsKey(cr.reaction_template_guid_ref))
                    {
                        sop.Protocol.entity_repository.reaction_templates.Add(protocol.entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref]);      
                    }
                    int index = sop.Protocol.entity_repository.reaction_templates.IndexOf(sop.Protocol.entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref]);           
                    cr.reaction_template_guid_ref = sop.Protocol.entity_repository.reaction_templates[index].entity_guid;
                    sop.Protocol.entity_repository.reactions.Add(cr);
                }
            }
        }

        /// <summary>
        /// Loads a new Protocol object into Daphne
        /// </summary>
        /// <param name="tempProtocol"></param>
        private void LoadProtocolFromSBML(Protocol protocol)
        {
            protocol.InitializeStorageClasses();
            
            //SetPaths
            protocol.FileName = Uri.UnescapeDataString(new Uri(appPath).LocalPath) + @"\Config\" + "scenario.json";
            protocol.TempFile = orig_path + @"\temp_scenario.json";

            protocol_path = new Uri(protocol.FileName);

            prepareProtocol(protocol);
            protocol.SerializeToFile(false);
        }


        /// <summary>
        /// Exports a model specification in SBML
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ExportSBML_Click(object sender, RoutedEventArgs e)
        {
            //Configure open file dialog box
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.InitialDirectory = SBMLFolderPath;
            dlg.DefaultExt = ".xml"; // Default file extension
            //dlg.Filter = "SBML format <Level3,Version1>Core (.xml)|*.xml "; //Add this for spatial models
            dlg.Filter = "SBML format <Level3,Version1>Core (.xml)|*.xml"+ "|SBML format <Level3,Version1>Spatial<Version1> (.xml)|*.xml";// Add this for spatial models
            dlg.FileName = "SBMLModel";

            // Show open  file dialog box
            Nullable<bool> result = dlg.ShowDialog();
            SBMLModel encodedSBML;
            // Process open file dialog box results
            if (result == true)
            {
                encodedSBML = new SBMLModel(dlg.FileName, sop.Protocol);
                encodedSBML.ConvertDaphneToSBML(dlg.FilterIndex);
            }

        }

        /// <summary>
        /// Exports a reaction complex specification in SBML
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void ExportReactionComplexSBML_Click(object sender, RoutedEventArgs e)
        {
            //Configure open file dialog box
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.InitialDirectory = SBMLFolderPath;
            dlg.DefaultExt = ".xml"; // Default file extension
            dlg.Filter = "SBML format <Level3,Version1>Core (.xml)|*.xml"; // Filter files by extension
            dlg.FileName = "SBMLReactionComplex";

            // Show open  file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                SBMLModel encodedSBML = new SBMLModel(dlg.FileName, sop.Protocol);
                ////////////ProtocolToolWindow.ConfigTabControl.SelectedItem = ComponentsToolWindow.tagLibraries;
                ComponentsToolWindow.ReacComplexExpander.IsExpanded = true;
                ConfigReactionComplex crc = ComponentsToolWindow.GetConfigReactionComplex();

                if (crc != null)
                {
                    encodedSBML.ConvertReactionComplexToSBML(crc);
                }
            }
        }

        /// <summary>
        /// Adds User environment variables for libraries
        /// </summary>
        private void SetPathVariable()
        {
            //Path of the dependencies folder
            string dependencies;
            if (AssumeIDE() == true) {
                dependencies = new Uri(Directory.GetParent(Directory.GetParent(Directory.GetParent(Directory.GetParent(new Uri(execPath).LocalPath).ToString()).ToString()).ToString()).ToString()).LocalPath + @"\dependencies";
            }
            else {
                dependencies = new Uri(appPath).LocalPath + @"\dependencies";
            }
            
            string pathEnv = System.Environment.GetEnvironmentVariable("PATH", EnvironmentVariableTarget.Process);

            // path for libSBML
            pathEnv += ";" + dependencies;

            // path for hdf5
            
            

            if (AssumeIDE() == true)
            {
                // path for hdf5
                pathEnv += ";" + dependencies + @"\hdf5";
            }
            else
            {
                //pathEnv = new Uri(execPath).LocalPath;
                string hdf5Path = (new Uri(execPath)).LocalPath;
                pathEnv += ";" + hdf5Path;
            }

            System.Environment.SetEnvironmentVariable("PATH", pathEnv, EnvironmentVariableTarget.Process);
            
        }

        private Nullable<bool> saveScenarioUsingDialog()
        {
            // Configure save file dialog box
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

            dlg.OverwritePrompt = true;
            dlg.InitialDirectory = orig_path;
            dlg.FileName = "scenario"; // Default file name
            dlg.DefaultExt = ".json"; // Default file extension
            dlg.Filter = "Sim Config JSON docs (.json)|*.json"; // Filter files by extension

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                string filename = dlg.FileName;
                // Save dialog catches trying to overwrite Read-Only files, so this should be safe...

                sop.Protocol.FileName = filename;
                sop.Protocol.SerializeToFile();

                orig_content = sop.Protocol.SerializeToStringSkipDeco();
                protocol_path = new Uri(filename);
                orig_path = System.IO.Path.GetDirectoryName(protocol_path.LocalPath);
                displayTitle();
            }
            return result;
        }

        private Nullable<bool> saveStoreUsingDialog(Level store, string fileName)
        {
            // Configure save file dialog box
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();

            dlg.OverwritePrompt = true;
            dlg.InitialDirectory = orig_path;
            dlg.FileName = "store"; // Default file name
            dlg.DefaultExt = ".json"; // Default file extension
            dlg.Filter = "Daphne Stores JSON docs (.json)|*.json"; // Filter files by extension

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                string filename = dlg.FileName;
                // Save dialog catches trying to overwrite Read-Only files, so this should be safe...
                store.FileName = filename;
                store.SerializeToFile();
            }
            return result;
        }

        private void lockSaveStartSim(bool completeReset)
        {
            // prevent when a fit is in progress
            lock (cellFitLock)
            {
                lock (sim)
                {
                    // re-initialize; if there are no cells, always do a full reset
                    // for VatRc, complete reset is expensive
                    if (ToolWinType == ToolWindowType.VatRC && !completeReset)
                    {
                        initialState(false, completeReset, null);
                    }
                    else
                    {
                        initialState(false, SimulationBase.dataBasket.Cells.Count < 1 || completeReset == true, ReadJson(""));
                    }
                    enableCritical(loadSuccess);
                    if (loadSuccess == false)
                    {
                        return;
                    }

                    // next time around, force a reset
                    MainWindow.SetControlFlag(MainWindow.CONTROL_FORCE_RESET, true);

                    // since the above call resets the experiment name each time, reset comparison string
                    // so we don't bother people about saving just because of this change
                    // NOTE: If we want to save scenario along with data, need to save after this GUID change is made...
                    //skip reserialize when user dragging 
                    if (ToolWinType != ToolWindowType.VatRC && !completeReset)
                    {
                        orig_content = sop.Protocol.SerializeToStringSkipDeco();
                    }

                    // this needs to come after setting orig_content
                    toolWin.LockSaveStartSim();
                    sim.restart();
                    UpdateGraphics();

                    // prevent the user from running certain tasks immediately, crashing the simulation
                    applyButton.IsEnabled = false;
                    enableFileMenu(false);
                    saveButton.IsEnabled = false;
                    analysisMenu.IsEnabled = false;
                    optionsMenu.IsEnabled = false;                    

                    gc.DisableComponents();
                    VCR_Toolbar.IsEnabled = false;
                    this.menu_ActivateSimSetup.IsEnabled = false;
                    if (ToolWinType == ToolWindowType.Tissue)
                    {
                        ProtocolToolWindow.Close();
                    }

                    ImportSBML.IsEnabled = false;
                    // prevent all fit/analysis-related things
                    hideFit();
                    ExportMenu.IsEnabled = false;
                }
            }
        }

        /// <summary>
        /// enable/disable items on the file menu
        /// </summary>
        /// <param name="enable">false to disable</param>
        private void enableFileMenu(bool enable)
        {
            loadScenario.IsEnabled = enable;
            saveScenario.IsEnabled = enable;
            saveScenarioAs.IsEnabled = enable;
            loadExp.IsEnabled = enable;
            //recentFileList.IsEnabled = enable;
            newScenario.IsEnabled = enable;
            ImportSBML.IsEnabled = enable;
            ExportSBML.IsEnabled = enable;
        }

        /// <summary>
        ///  enable/disable critical, i.e. when a config load error happens, gui elements
        /// </summary>
        /// <param name="enable">false to disable</param>
        private void enableCritical(bool enable)
        {
            runButton.IsEnabled = enable;
            applyButton.IsEnabled = enable;
            saveButton.IsEnabled = enable;
            abortButton.IsEnabled = false;
            analysisMenu.IsEnabled = enable;
            saveScenario.IsEnabled = enable;
            saveScenarioAs.IsEnabled = enable;
            ImportSBML.IsEnabled = enable;
            ExportSBML.IsEnabled = enable;
        }
        /// <summary>
        /// reset the simulation; will also apply the initial state; call after loading a scenario file
        /// </summary>
        /// <param name="newFile">true to indicate we are loading a new file</param>
        /// <param name="protocol">protocol object</param>
        private void lockAndResetSim(bool newFile, Protocol protocol)
        {
            // prevent when a fit is in progress
            lock (cellFitLock)
            {
                if (vcrControl != null)
                {
                    vcrControl.SetInactive();
                }
                initialState(newFile || tempFileContent, true, protocol);
                enableCritical(loadSuccess);
                if (loadSuccess == false)
                {
                    return;
                }

                // after doing a full reset, don't require one immediately unless we did a db load
                MainWindow.SetControlFlag(MainWindow.CONTROL_FORCE_RESET, MainWindow.CheckControlFlag(MainWindow.CONTROL_PAST_LOAD));

                sim.reset();
                // reset cell tracks and free memory
                //////////gc.CleanupTracks();
                gc.ResetGraphics();
                fitCellOpacitySlider.Value = 1.0;
                UpdateGraphics();

                // prevent all fit/analysis-related things
                hideFit();
                ExportMenu.IsEnabled = false;
            }
        }

        private void OpenExpSelectWindow(object sender, RoutedEventArgs e)
        {
            // if the file is open we'll have to close it
            if (DataBasket.hdf5file.isOpen() == true)
            {
                // close the file and all open groups
                DataBasket.hdf5file.close(true);
            }

            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.InitialDirectory = sim.Reporter.AppPath;
            dlg.DefaultExt = ".hdf5";
            dlg.Filter = "HDF5 VCR files (.hdf5)|*.hdf5"; 

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                DataBasket.hdf5file.initialize(dlg.FileName);
            }

            if (DataBasket.hdf5file.openRead() == false)
            {
                MessageBox.Show("The HDF5 file could not be opened or does not exist.", "HDF5 error", MessageBoxButton.OK);
                return;
            }

            string protocolString = null;

            // open the experiment parent group
            DataBasket.hdf5file.openGroup("/Experiment_VCR/");
            // read the protocol string
            DataBasket.hdf5file.readString("Protocol", ref protocolString);
            // close the file and all groups
            DataBasket.hdf5file.close(true);

            // do the loading
            MainWindow.SetControlFlag(MainWindow.CONTROL_PAST_LOAD, true);
            lockAndResetSim(true, ReadJson(protocolString));
            if (loadSuccess == false)
            {
                return;
            }
            MainWindow.SetControlFlag(MainWindow.CONTROL_PAST_LOAD, false);
            // this function does not exist currently, do we need this call?
            //sim.runStatSummary();
            vcrControl.LastFrame = false;
            GUIUpdate(true, true);
            displayTitle("Loaded past run " + DataBasket.hdf5file.FileName);
        }

        private void OpenLPFittingWindow(object sender, RoutedEventArgs e)
        {
            #region MyRegion
            //LPManager lpm = new LPManager();
            ////Console.WriteLine("ExpID is: " + sim.SC.id.ToString());
            //if (lpfw == null)
            //{
            //    lpfw = new LPFittingWindow(lpm, configurator.Protocol.experiment_db_id);
            //    lpfw.Show();
            //}
            //else
            //{
            //    if (lpfw.IsLoaded)
            //    {
            //        if (!lpfw.Activate())
            //        {
            //            lpfw.Close();
            //            lpfw = new LPFittingWindow(lpm, configurator.Protocol.experiment_db_id);
            //            lpfw.Show();
            //        }
            //    }
            //    else
            //    {
            //        lpfw = new LPFittingWindow(lpm, configurator.Protocol.experiment_db_id);
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
            //    cdw = new CellDivisionWindow(cdm, configurator.Protocol.experiment_db_id);
            //    cdw.Show();
            //}
            //else
            //{
            //    if (cdw.IsLoaded)
            //    {
            //        if (!cdw.Activate())
            //        {
            //            cdw.Close();
            //            cdw = new CellDivisionWindow(cdm, configurator.Protocol.experiment_db_id);
            //            cdw.Show();
            //        }
            //    }
            //    else
            //    {
            //        cdw = new CellDivisionWindow(cdm, configurator.Protocol.experiment_db_id);
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
            DataStorageMenu.IsEnabled = false;
            vcrControl.SetPlaybackState(VCRControlState.VCR_PLAY);
        }

        private void VCRbutton_Play_Unchecked(object sender, RoutedEventArgs e)
        {
            DataStorageMenu.IsEnabled = true;
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
            if (argDev == true)
            {
                return true;
            }
            else
            {
                return Debugger.IsAttached;
            }
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

        private string extractFileName()
        {
            string[] segments = protocol_path.LocalPath.Split('\\');

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

        private void skipDataWrite_Click(object sender, RoutedEventArgs e)
        {
            Properties.Settings.Default.skipDataWrites = skipDataWriteMenu.IsChecked;

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
        
        /// <summary>
        /// CanExecute method for select report folder command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CommandBindingSelectReportFolder_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        /// <summary>
        /// Execute method for select report folder command
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void CommandBindingSelectReportFolder_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();

            dlg.Description = "Select report output folder";
            dlg.SelectedPath = sim.Reporter.AppPath;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                sim.Reporter.AppPath = dlg.SelectedPath + @"\";
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
                string summaryFileOutputPath = System.Environment.GetFolderPath(System.Environment.SpecialFolder.MyDocuments) + @"\Daphne\";
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
            sw.WriteLine("//-- Exp:" + MainWindow.SOP.Protocol.experiment_name);
            sw.WriteLine("//-- Description:" + MainWindow.SOP.Protocol.experiment_description);
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
            double samplingFreq = SOP.Protocol.scenario.time_config.sampling_interval;
            //get the frameTime from the database.  
            //DataBaseTools.GetTimeFrame(SC.Protocol.experiment_db_id, frameTimeIds, frameTimeVals);

            int samplingStep = 0;
            //rebuild the simulation frame by using rendering interval and timeFrame ,
            //
            for (int i = 0; i < frameTimeIds.Count; i++)
            {
                if (frameTimeVals[i] >= samplingStep * SOP.Protocol.scenario.time_config.sampling_interval || (i == frameTimeIds.Count - 1))
                {
                    samplingTimeArr.Add(frameTimeIds[i]);
                    samplingStep++;
                }
            }//end of for loop
            //now with this array build the sql statement


            //Dictionary<int, Dictionary<int, BCellPhenotype>> cellState = DataBaseTools.GetBCellSummaryCellState(SC.Protocol.experiment_db_id, samplingTimeArr);

            ////n
            //Dictionary<int, Dictionary<string, double>> simState = DataBaseTools.GetBCellSummarySimState(SC.Protocol.experiment_db_id, samplingTimeArr);

            //Dictionary<int, Dictionary<int, double>> FDCState = DataBaseTools.GetFDCSummaryCellState(SC.Protocol.experiment_db_id, samplingTimeArr);






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
            //Dictionary<int, string> igData = DataBaseTools.GetIgGene(SC.Protocol.experiment_db_id);
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
            //Dictionary<int, GeneologyInfo> familytree = CellDivTools.GetFamilyTree(SC.Protocol.experiment_db_id);

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
            ////st = DataBaseTools.GetSynapse(SC.Protocol.experiment_db_id);

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
        /// Apply changes to the temporary file.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void applyButton_Click(object sender, RoutedEventArgs e)
        {
            // Workbench-specific code to preserve focus to the element that was in focus before "Apply" button clicked.
            ToolWin.Apply();
        }

        /// <summary>
        /// The essential components of the Apply button functionality.
        /// Workbenches call this method after they preserve focus information and before they restore focus.
        /// </summary>
        public void Apply()
        {
            runButton.IsEnabled = false;
            mutex = true;

            saveStoreFiles();
            saveTempFiles();
            // don't handle the vcr
            updateGraphicsAndGUI(false);
        }


        /// <summary>
        /// reset the simulation to a random initial state and ready it for running (time = 0); the simulation will then start automatically
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void runButton_Click(object sender, RoutedEventArgs e)
        {
            applyButton.IsEnabled = false;
            saveButton.IsEnabled = false;
            CellOptionsExpander.IsExpanded = false;
            ECMOptionsExpander.IsExpanded = false;
            mutex = true;

            runSim();
        }

        /// <summary>
        /// save when simulation is paused state
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveState_Click(object sender, RoutedEventArgs e)
        {
            save_simulation_state();
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
        /// set the left mouse button state
        /// </summary>
        /// <param name="state">state to set</param>
        /// <param name="set">true for set, false for clear</param>
        public static void SetMouseLeftState(byte state, bool set)
        {
            // set
            if (set == true)
            {
                mouseLeftState = state;
            }
            // clear
            else
            {
                mouseLeftState = MOUSE_LEFT_NONE;
            }
        }

        /// <summary>
        /// check the mouse left state
        /// </summary>
        /// <param name="state">state to check</param>
        public static bool CheckMouseLeftState(byte state)
        {
            return mouseLeftState == state;
        }

        //public enum ColorList { Red, Orange, Yellow, Green, Blue, Indigo, Violet, Custom }

        private void save3DView_Click(object sender, RoutedEventArgs e)
        {
            double[] rgb = { 1, 1, 1 };     //Values are between 0 and 1

            Save3DView dialog = new Save3DView();

            // Set image file format
            if (dialog.ShowDialog() == true)
            {
                vtkImageWriter writer;
                if (dialog.FileName.EndsWith("bmp"))
                {
                    writer = new vtkBMPWriter();
                }
                else if (dialog.FileName.EndsWith("jpg"))
                {
                    writer = new vtkJPEGWriter();
                    vtkJPEGWriter jw = (vtkJPEGWriter)writer;
                    jw.SetQuality(100);
                    jw.SetProgressive(0);
                }
                else if (dialog.FileName.EndsWith("png"))
                {
                    writer = new vtkPNGWriter();
                }
                else if (dialog.FileName.EndsWith("tif"))
                {
                    writer = new vtkTIFFWriter();
                }
                else
                {
                    writer = new vtkBMPWriter();
                }

                //Get selected color
                Color c = dialog.ActualColor;

                //Values need to be between 0 and 1
                rgb[0] = c.R / (double)255;
                rgb[1] = c.G / (double)255;
                rgb[2] = c.B / (double)255;

                ((VTKFullGraphicsController)MainWindow.GC).SaveToFile(dialog.FileName, writer, rgb);
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

        public static void GUIInteractionToWidgetCallback(object sender, PropertyChangedEventArgs e)
        {
            if (MainWindow.SOP.Protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                throw new InvalidCastException();
            }

            BoxSpecification box = (BoxSpecification)sender;

            if (box == null)
            {
                return;
            }

            if (box == null)
            {
                return;
            }

            if (e.PropertyName == "box_visibility")
            {
                ((VTKFullGraphicsController)MainWindow.GC).Regions[box.box_guid].ShowWidget(box.box_visibility);
            }

            // Catch-all for other scale / translation manipulations
            if (((VTKFullDataBasket)MainWindow.VTKBasket).Regions.ContainsKey(box.box_guid) == true && ((VTKFullGraphicsController)MainWindow.GC).Regions.ContainsKey(box.box_guid) == true)
            {
                ((VTKFullDataBasket)MainWindow.VTKBasket).Regions[box.box_guid].SetTransform(box.transform_matrix, RegionControl.PARAM_SCALE);
                ((VTKFullGraphicsController)MainWindow.GC).Regions[box.box_guid].SetTransform(box.transform_matrix, RegionControl.PARAM_SCALE);
                ((VTKFullGraphicsController)MainWindow.GC).Rwc.Invalidate();
            }
        }

        public static void GUIGaussianSurfaceVisibilityToggle(object sender, PropertyChangedEventArgs e)
        {
            if (gc is VTKFullGraphicsController == false)
            {
                throw new InvalidCastException();
            }

            GaussianSpecification gauss = (GaussianSpecification)sender;
            VTKFullGraphicsController gcHandle = (VTKFullGraphicsController)MainWindow.GC;

            if (gauss == null)
            {
                return;
            }

            if (e.PropertyName == "gaussian_region_visibility")
            {
                gcHandle.Regions[gauss.box_spec.box_guid].ShowActor(gcHandle.Rwc.RenderWindow, gauss.gaussian_region_visibility);
                gcHandle.Rwc.Invalidate();
            }
            if (e.PropertyName == "gaussian_spec_color")
            {
                gcHandle.Regions[gauss.box_spec.box_guid].SetColor(gauss.gaussian_spec_color.ScR, gauss.gaussian_spec_color.ScG, gauss.gaussian_spec_color.ScB);
                gcHandle.Regions[gauss.box_spec.box_guid].SetOpacity(gauss.gaussian_spec_color.ScA);
                gcHandle.Rwc.Invalidate();
            }
            return;
        }

        /// <summary>
        /// Takes care of loading a Protocol from string or file
        /// </summary>
        /// <param name="jsonScenarioString"></param>
        /// <returns></returns>
        private Protocol ReadJson(string jsonScenarioString)
        {
            Protocol protocol;

            // load past experiment
            if (jsonScenarioString != "")
            {
                protocol = new Protocol();
                protocol.TempFile = orig_path + @"\temp_protocol.json";
                // catch xaml parse exception if it's not a good sim config file
                try
                {
                    SystemOfPersistence.DeserializeExternalProtocolFromString(ref protocol, jsonScenarioString);
                    return protocol;
                }
                catch
                {
                    handleLoadFailure("That configuration has problems. Please select another experiment.");
                    return null;
                }
            }
            else
            {
                // catch xaml parse exception if it's not a good sim config file
                try
                {
                    protocol = new Protocol();
                    protocol.FileName = protocol_path.LocalPath;
                    protocol.TempFile = orig_path + @"\temp_protocol.json";
                    SystemOfPersistence.DeserializeExternalProtocol(ref protocol, tempFileContent);
                    return protocol;
                    //configurator.Protocol.ChartWindow = ReacComplexChartWindow;
                }
                catch
                {
                    handleLoadFailure("There is a problem loading the protocol file.\nPress OK, then try to load another.");
                    return null;
                }
            }
        }

        private void initialState(bool newFile, bool completeReset, Protocol protocol)
        {
            // if we read a new file we may have to disconnect event handlers if they were connected previously;
            // we always must deserialize the file
            if (newFile == true)
            {
                if (sop != null)
                {
                    if (protocol != null)
                    {
                        //sop = new SystemOfPersistence();
                        sop.Protocol = protocol;
                        orig_content = sop.Protocol.SerializeToStringSkipDeco();
                        orig_path = System.IO.Path.GetDirectoryName(protocol_path.LocalPath);
                    }

                    ////skg - Code needed to retrieve userstore and daphnestore - deserialize from files
                    ////      Do this once up front instead of doing each time user clicks Userstore or Daphnestore.
                    string storesPath = new Uri(appPath).LocalPath;
                    sop.UserStore.FileName = storesPath + @"\Config\daphne_userstore.json";
                    sop.UserStore.TempFile = storesPath + "Config\\temp_userstore.json";
                    sop.DaphneStore.FileName = storesPath + @"\Config\daphne_daphnestore.json";
                    sop.DaphneStore.TempFile = storesPath + "Config\\temp_daphnestore.json";
                    sop.DaphneStore = sop.DaphneStore.Deserialize();
                    sop.UserStore = sop.UserStore.Deserialize();
                    orig_daphne_store_content = sop.DaphneStore.SerializeToString();
                    orig_user_store_content = sop.UserStore.SerializeToString();
                }
            }

            if (sop.Protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == true)
            {
                // GUI Resources
                // Set the data context for the main tab control config GUI
                this.CellStudioToolWindow.DataContext = sop.Protocol;
                this.CellStudioToolWindow.CellsListBox.SelectedIndex = 0;
                this.ComponentsToolWindow.DataContext = sop.Protocol;

                if (newFile == true)
                {
                    if (ToolWinType != ToolWindowType.Tissue)
                    {
                        ToolWin = new ToolWinTissue();
                        ToolWinType = ToolWindowType.Tissue;
                        ToolWin.MW = this;

                        MdiTabContainer.Items.Clear();
                        MdiTabContainer.Items.Add(ST_VTKDisplayDocWindow);
                        MdiTabContainer.Items.Add(ST_ComponentsToolWindow);
                        MdiTabContainer.Items.Add(ST_CellStudioToolWindow);
                        MdiTabContainer.Items.Add(ST_RenderSkinWindow);
                        ST_VTKDisplayDocWindow.Activate();

                    }
                    ToolWin.Protocol = SOP.Protocol;
                    ToolWin.Title = ToolWin.TitleText;

                    ToolWin.Tag = sop;

                    if (ProtocolToolWindowContainer.Items.Count > 0)
                        ProtocolToolWindowContainer.Items.RemoveAt(0);

                    ProtocolToolWindowContainer.Items.Add(ToolWin);
                    ProtocolToolWindow = ((ToolWinTissue)ToolWin);
                }

                // only create during construction or when the type changes
                if (sim == null || sim is TissueSimulation == false)
                {
                    // create the simulation
                    sim = new TissueSimulation();
                    // set the reporter's path
                    sim.Reporter.AppPath = new Uri(appPath + @"\Generated\").LocalPath;
                    // vtk data basket to hold vtk data for entities with graphical representation
                    vtkDataBasket = new VTKFullDataBasket();
                    // graphics controller to manage vtk objects
                    gc = new VTKFullGraphicsController(this);
                }
            }
            else if (sop.Protocol.CheckScenarioType(Protocol.ScenarioType.VAT_REACTION_COMPLEX) == true)
            {
                this.ComponentsToolWindow.DataContext = sop.Protocol;
                this.ReacComplexChartWindow.DataContext = sop.Protocol;   //was causing a problem in chart page
                if (newFile == true)
                {
                    ReacComplexChartWindow.Reset();
                    if (ToolWinType != ToolWindowType.VatRC)
                    {
                        ToolWin = new ToolWinVatRC();
                        ToolWin.MW = this;
                        ToolWinType = ToolWindowType.VatRC;

                        MdiTabContainer.Items.Clear();
                        MdiTabContainer.Items.Add(ST_ComponentsToolWindow);
                        MdiTabContainer.Items.Add(ST_ReacComplexChartWindow);
                        ST_ReacComplexChartWindow.Activate();
                    }

                    ToolWin.Protocol = SOP.Protocol;
                    ToolWin.Title = ToolWin.TitleText;
                    ReacComplexChartWindow.redraw_flag = false;

                    if (ProtocolToolWindowContainer.Items.Count > 0)
                        ProtocolToolWindowContainer.Items.Clear();

                    ProtocolToolWindowContainer.Items.Add(ToolWin);
                    ProtocolToolWindow = ((ToolWinVatRC)ToolWin);
                }

                // only create during construction or when the type changes
                if (sim == null || sim is VatReactionComplex == false)
                {
                    // create the simulation
                    sim = new VatReactionComplex();
                    // set the reporter's path
                    sim.Reporter.AppPath = new Uri(appPath + @"\Generated\").LocalPath;
                    vtkDataBasket = new VTKVatRCDataBasket();
                    gc = new VTKNullGraphicsController();
                }
            }
            else
            {
                throw new NotImplementedException();
            }

            // NOTE: For now, setting data context of VTK MW display grid to only instance of GraphicsController.
            if (vtkDisplay_DockPanel.DataContext != gc)
            {
                vtkDisplay_DockPanel.DataContext = gc;
            }
            // set the save state menu's context to the simulation so we can change its enabled property based on values of the simulation
            saveState.DataContext = sim;

            // set up the simulation
            if (postConstruction == true && AssumeIDE() == true)
            {
                sim.Load(sop.Protocol, completeReset);
            }
            else
            {
                try
                {
                    sim.Load(sop.Protocol, completeReset);
                }
                catch (Exception e)
                {
                    handleLoadFailure(exceptionMessage(e));
                    return;
                }
            }

            // reporter file name
            sim.Reporter.FileName = sop.Protocol.reporter_file_name;

            //if (true)
            //{
            //    orig_content = sop.Protocol.SerializeToStringSkipDeco();
            //}

            //this cannot be set before databaseket get updated from loading the new scenario
            if (gc is VTKFullGraphicsController)
            {
                //CellRenderMethodCB.DataContext = sop.Protocol;
                CellOptionsExpander.DataContext = sop.Protocol;
            }
            vtkDataBasket.SetupVTKData(sop.Protocol);
            // Create all VTK visualization pipelines and elements
            gc.CreatePipelines();

            // clear the vcr cache
            if (vcrControl != null)
            {
                vcrControl.ReleaseVCR();
            }

            if (gc is VTKFullGraphicsController)
            {
                VTKFullGraphicsController gcHandle = (VTKFullGraphicsController)gc;

                if (newFile)
                {
                    gcHandle.recenterCamera();
                }
                gcHandle.Rwc.Invalidate();

                // TODO: Need to do this for all GCs eventually...
                // Add the RegionControl interaction event handlers here for easier reference to callback method
                foreach (KeyValuePair<string, RegionWidget> kvp in gcHandle.Regions)
                {
                    // NOTE: For now not doing any callbacks on property change for RegionControls...
                    kvp.Value.ClearCallbacks();
                    kvp.Value.AddCallback(new RegionWidget.CallbackHandler(gcHandle.WidgetInteractionToGUICallback));
                    kvp.Value.AddCallback(new RegionWidget.CallbackHandler(ToolWin.RegionFocusToGUISection));
                    kvp.Value.AddCallback(new RegionWidget.CallbackHandler(ToolWin.RegionFocusToGUISection));
                    kvp.Value.Gaussian.PropertyChanged += MainWindow.GUIGaussianSurfaceVisibilityToggle;
                    kvp.Value.Gaussian.box_spec.PropertyChanged += MainWindow.GUIInteractionToWidgetCallback;
                }
            }

            VCR_Toolbar.IsEnabled = false;
            //////////gc.ToolsToolbar_IsEnabled = true;
            //////////gc.DisablePickingButtons();

            loadSuccess = true;
        }

        /// <summary>
        /// display an exception message
        /// </summary>
        /// <param name="e">the exception</param>
        private string exceptionMessage(Exception e)
        {
            string msg = e.Message;

            // output the stack for developers
            if (AssumeIDE() == true)
            {
                msg += "\n\n" + e.StackTrace;
            }
            return msg;
        }

        /// <summary>
        /// show a message box with a string to respond to an exception
        /// </summary>
        /// <param name="s">exception message to display</param>
        private void showExceptionBox(string s)
        {
            MessageBox.Show(s, "Application error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// handle a problem during loading: blank the vtk screen and bulk of the gui
        /// </summary>
        /// <param name="s">message to display</param>
        private void handleLoadFailure(string s)
        {
            loadSuccess = false;
            sop.Protocol = new Protocol();
            sop.Protocol.experiment_name = "";
            sop.Protocol.experiment_description = "";
            orig_content = sop.Protocol.SerializeToStringSkipDeco();
            orig_path = System.IO.Path.GetDirectoryName(protocol_path.LocalPath);
            //ProtocolToolWindow.DataContext = sop.Protocol;
            CellStudioToolWindow.DataContext = sop.Protocol;


            ComponentsToolWindow.DataContext = sop.Protocol;
            //////////gc.Cleanup();
            //////////gc.Rwc.Invalidate();
            displayTitle("");
            showExceptionBox(s);
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
        /// utility to close output files, reporter, hdf5
        /// </summary>
        private void closeOutputFiles()
        {
            sim.Reporter.CloseReporter();
            DataBasket.hdf5file.close(true);
        }

        private void run()
        {
            while (true)
            {
                lock (sim)
                {
                    if (sim.RunStatus == SimulationBase.RUNSTAT_RUN)
                    {
                        // run the simulation forward to the next task
                        if (postConstruction == true && AssumeIDE() == true)
                        {
                            sim.RunForward();
                        }
                        else
                        {
                            try
                            {
                                sim.RunForward();
                            }
                            catch (Exception e)
                            {
                                showExceptionBox(exceptionMessage(e));
                                sim.RunStatus = SimulationBase.RUNSTAT_ABORT;
                            }
                        }

                        if (sim.RunStatus != SimulationBase.RUNSTAT_ABORT)
                        {
                            // check for flags and execute applicable task(s)
                            if (sim.CheckFlag(SimulationBase.SIMFLAG_RENDER) == true)
                            {
                                if (Properties.Settings.Default.skipDataWrites == false)
                                {
                                    if (sim.FrameData != null)
                                    {
                                        sim.FrameData.writeData(sim.FrameNumber - 1);
                                    }
                                }
                                UpdateGraphics();
                            }
                            if (sim.CheckFlag(SimulationBase.SIMFLAG_SAMPLE) == true && Properties.Settings.Default.skipDataWrites == false)
                            {
                                sim.Reporter.AppendReporter();
                            }

                            if (sim.RunStatus != SimulationBase.RUNSTAT_RUN)
                            {
                                // never rerun the simulation if the simulation was aborted
                                if (sim.RunStatus != SimulationBase.RUNSTAT_PAUSE && repetition < sop.Protocol.experiment_reps)
                                {
                                    runButton.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new GUIDelegateNoArgs(RerunSimulation));
                                }
                                else if (sim.RunStatus == SimulationBase.RUNSTAT_FINISHED)
                                {
                                    //signal run finished if any one is waiting
                                    runFinishedEvent.Set();
                                    // autosave the state
                                    if (argSave == true)
                                    {
                                        runButton.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new GUIDelegateNoArgs(save_simulation_state));
                                    }
                                    // close the reporter
                                    if (Properties.Settings.Default.skipDataWrites == false)
                                    {
                                        // reporter and hdf5 close
                                        closeOutputFiles();
                                    }
                                    // for profiling: close the application after a completed experiment
                                    if (ControlledProfiling() == true && repetition >= sop.Protocol.experiment_reps)
                                    {
                                        runButton.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new GUIDelegateNoArgs(CloseApp));
                                        return;
                                    }

                                    // update the gui; this is a non-issue if an application close just got requested, so may get skipped
                                    vcrControl.LastFrame = true;
                                    runButton.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new GUIDelegateTwoArgs(GUIUpdate), true, false);
                                }
                            }
                        }
                    }
                    else if (sim.RunStatus == SimulationBase.RUNSTAT_ABORT)
                    {
                        if (Properties.Settings.Default.skipDataWrites == false)
                        {
                            // reporter and hdf5 close
                            closeOutputFiles();
                        }
                        runButton.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new GUIDelegateOneArg(updateGraphicsAndGUI), true);
                        sim.RunStatus = SimulationBase.RUNSTAT_OFF;
                    }
                    else if (vcrControl != null && vcrControl.IsActive() == true)
                    {
                        vcrControl.Play();
                        if (vcrControl.IsActive() == false)
                        {
                            // switch from pause to the play button
                            VCRbutton_Play.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new GUIDelegateNoArgs(VCRUpdate));
                        }
                    }
                }
                if (mutex == true)
                {
                    runButton.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new GUIDelegateNoArgs(simControlUpdate));
                    mutex = false;
                }
            }
        }

        private void VCRUpdate()
        {
            VCRbutton_Play.IsChecked = false;
        }

        // gui update delegate; needed because we can't access the gui elements directly; they are part of a different thread
        private delegate void GUIDelegateNoArgs();
        private delegate void GUIDelegateOneArg(bool bArg);
        private delegate void GUIDelegateTwoArgs(bool bArg1, bool bArg2);

        // close the application
        private void CloseApp()
        {
            this.Close();
        }

        // rerun the simulation when multiple repetitions are specified
        private void RerunSimulation()
        {
            // increment the repetition
            repetition++;
            lockSaveStartSim(true);
        }

        // re-enable the gui elements that got disabled during a simulation run
        private void GUIUpdate(bool handleVCR, bool force)
        {
            if (handleVCR == true && skipDataWriteMenu.IsChecked == false && vcrControl.OpenVCR() == true)
            {
                VCR_Toolbar.IsEnabled = true;
                VCR_Toolbar.DataContext = vcrControl;
                VCRslider.Maximum = vcrControl.TotalFrames() - 1;
            }

            bool finished = false;

            // only allow fitting and other analysis that needs the database if database writing is on
            if (force || skipDataWriteMenu.IsChecked == false && sim.RunStatus == SimulationBase.RUNSTAT_FINISHED)
            {
                analysisMenu.IsEnabled = true;
                // NOTE: Uncomment this to open the LP Fitting ToolWindow after a run has completed
                // this.LPFittingToolWindow.Activate();
                this.menu_ActivateLPFitting.IsEnabled = true;
                this.ExportMenu.IsEnabled = true;
                // And show stats results chart
                // NOTE: If the stats charts can be displayed without the database saving, then these
                //   ChartViewDocWindow calls can be moved outside this if() block
                //if (runStatisticalSummaryMenu.IsChecked == true)
                //{
                //    this.ChartViewDocWindow.RenderPlots();
                //    this.ChartViewDocWindow.Open();
                //    this.menu_ActivateAnalysisChart.IsEnabled = true;
                //}
                finished = true;
            }

            //sim.RunStatus = Simulation.RUNSTAT_OFF;
            if (vcrControl.LastFrame == false && VCR_Toolbar.IsEnabled == true)
            {
                applyButton.IsEnabled = false;
                saveButton.IsEnabled = false;
                runButton.IsEnabled = false;
                loadScenario.IsEnabled = true;
                saveScenario.IsEnabled = false;
                saveScenarioAs.IsEnabled = false;
                loadExp.IsEnabled = true;
                //recentFileList.IsEnabled = true;
                newScenario.IsEnabled = true;
                ImportSBML.IsEnabled = false;
                ExportSBML.IsEnabled = false;
            }
            else
            {
                applyButton.IsEnabled = true;
                saveButton.IsEnabled = true;
                enableFileMenu(true);
            }
            abortButton.IsEnabled = false;
            runButton.Content = "Run";
            statusBarMessagePanel.Content = "Ready:  Protocol";
            optionsMenu.IsEnabled = true;
            // TODO: Should probably combine these...

            gc.EnableComponents(finished);
            toolWin.GUIUpdate(finished);

            // NOTE: Uncomment this to open the Sim Config ToolWindow after a run has completed
            this.ProtocolToolWindow.Activate();
            ToolWin.Activate();
            this.menu_ActivateSimSetup.IsEnabled = true;
            SetControlFlag(MainWindow.CONTROL_NEW_RUN, true);
            // TODO: These Focus calls will be a problem with multiple GCs...
            if (gc is VTKFullGraphicsController == true)
            {
                ((VTKFullGraphicsController)gc).Rwc.Focus();
            }
        }

        private void simControlUpdate()
        {
            runButton.IsEnabled = true;
        }

        private bool saveTempFiles()
        {
            // check if there were changes
            if (sop != null && sop.Protocol.SerializeToStringSkipDeco() != orig_content)
            {
                sop.Protocol.SerializeToFile(true);
                tempFileContent = true;
                return true;
            }
            return false;
        }

        private void saveStore(Level store, string storeName)
        {
            string messageBoxText = storeName + " has changed. Do you want to overwrite the information in " + System.IO.Path.GetFileName(store.FileName) + "?";
            string caption = storeName + " Changed";
            MessageBoxButton button = MessageBoxButton.YesNoCancel;
            MessageBoxImage icon = MessageBoxImage.Warning;

            // Display message box
            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);

            if (result == MessageBoxResult.Cancel)
            {
                return;
            }
            else if (result == MessageBoxResult.No)
            {
                saveStoreUsingDialog(store, store.FileName);
            }
            else
            {
                FileInfo info = new FileInfo(store.FileName);
                if (info.IsReadOnly == false || !info.Exists)
                {
                    store.SerializeToFile(false);
                    if (storeName == "DaphneStore")
                    {
                        orig_daphne_store_content = store.SerializeToString();
                    }
                    else if (storeName == "UserStore")
                    {
                        orig_user_store_content = store.SerializeToString();
                    }
                }
                else
                {
                    messageBoxText = "The file is write protected: " + sop.DaphneStore.FileName;
                    caption = "File write protected";
                    button = MessageBoxButton.OK;
                    icon = MessageBoxImage.Warning;
                    MessageBox.Show(messageBoxText, caption, button, icon);
                }
            }
        }

        private void saveStoreFiles()
        {
            //DaphneStore
            if (sop != null && sop.DaphneStore.SerializeToString() != orig_daphne_store_content)
            {
                saveStore(sop.DaphneStore, "DaphneStore");
                
                ////string messageBoxText = "Daphne store has changed. Do you want to overwrite the information in " + System.IO.Path.GetFileName(sop.DaphneStore.FileName) + "?";
                ////string caption = "Daphne Store Changed";
                ////MessageBoxButton button = MessageBoxButton.YesNoCancel;
                ////MessageBoxImage icon = MessageBoxImage.Warning;

                ////// Display message box
                ////MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);

                ////if (result == MessageBoxResult.Cancel)
                ////{
                ////    return;
                ////}
                ////else if (result == MessageBoxResult.No)
                ////{
                ////    saveStoreUsingDialog(sop.DaphneStore, sop.DaphneStore.FileName);
                ////}
                ////else
                ////{
                ////    FileInfo info = new FileInfo(sop.DaphneStore.FileName);
                ////    if (info.IsReadOnly == false || !info.Exists)
                ////    {
                ////        sop.DaphneStore.SerializeToFile(false);
                ////        orig_daphne_store_content = sop.DaphneStore.SerializeToString();
                ////    }
                ////    else
                ////    {
                ////        messageBoxText = "The file is write protected: " + sop.DaphneStore.FileName;
                ////        caption = "File write protected";
                ////        button = MessageBoxButton.OK;
                ////        icon = MessageBoxImage.Warning;
                ////        MessageBox.Show(messageBoxText, caption, button, icon);
                ////    }
                ////}
            }

            //UserStore
            if (sop != null && sop.UserStore.SerializeToString() != orig_user_store_content)
            {
                saveStore(sop.UserStore, "UserStore");

                ////FileInfo info = new FileInfo(sop.UserStore.FileName);
                ////if (info.IsReadOnly == false || !info.Exists)
                ////{
                ////    sop.UserStore.SerializeToFile(false);
                ////    orig_user_store_content = sop.UserStore.SerializeToString();
                ////}
                ////else
                ////{
                ////    string messageBoxText = "The file is write protected: " + sop.UserStore.FileName;
                ////    string caption = "File write protected";
                ////    MessageBoxButton button = MessageBoxButton.OK;
                ////    MessageBoxImage icon = MessageBoxImage.Warning;
                ////    MessageBox.Show(messageBoxText, caption, button, icon);
                ////}
            }
        }

        private bool applyTempFilesAndSave(bool discard)
        {
            if (tempFileContent == true)
            {
                sop.DeserializeProtocol(true);
                // handled this set of files
                tempFileContent = false;

                MessageBoxResult result = saveDialog();

                // apply changes, save if needed
                if (result == MessageBoxResult.Yes)
                {
                    // save into the same file
                    sop.Protocol.SerializeToFile();
                    orig_content = sop.Protocol.SerializeToStringSkipDeco();
                    orig_path = System.IO.Path.GetDirectoryName(protocol_path.LocalPath);
                }
                else if (result == MessageBoxResult.No)
                {
                    // allow saving with a different name
                    saveScenarioUsingDialog();
                }
                else // if we had a proper discard button, discard would have to be it's own option
                {
                    if (discard == true)
                    {
                        // reload the file; also resets the gui, discards changes
                        tempFileContent = false;
                        loadScenarioFromFile(protocol_path.LocalPath);
                    }
                    return false;
                }

                return true;
            }

            return true;
        }

        private void updateGraphicsAndGUI(bool handleVCR)
        {
            lockAndResetSim(false, ReadJson(""));
            // pass current experiment id to allow vcr playback even for partial runs
            vcrControl.LastFrame = true;
            runButton.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new GUIDelegateTwoArgs(GUIUpdate), handleVCR, false);

            //If main VTK window is not open, open it. Close the CellInfo tab.
            this.VTKDisplayDocWindow.Open();
            this.ToolWinCellInfo.Close();
        }

        /// <summary>
        /// Initiate a simulation run
        /// This needs to work for any scenario, i.e., it needs to work in the general case and not just for a specific scenario.
        /// MAKE SURE TO DO WHAT IT SAYS IN PREVIOUS LINE!!!!
        /// </summary>
        internal void runSim()
        {

            switch (ToolWinType)
            {
                case ToolWindowType.Tissue:
                    runSim_Tissue();
                    break;
                case ToolWindowType.VatRC:
                    runSim_VatRc();
                    break;
                default:
                    break;
            }
        }

        private void runSim_Tissue()
        {
            VTKDisplayDocWindow.Activate();
            if (sim.RunStatus == SimulationBase.RUNSTAT_RUN)
            {

                abortButton.IsEnabled = true;
                sim.RunStatus = SimulationBase.RUNSTAT_PAUSE;

                //AT THIS POINT, THE WHOLE TOOL BAR IS GREYED OUT.  
                //WE MUST ENABLE THE HAND TO ALLOW USER TO VIEW MOL CONCS DURING PAUSE.

                //NEED TO PIECE-MEAL GREY OUT ALL ICONS EXCEPT HAND
                if (gc is VTKFullGraphicsController == true)
                {
                    ((VTKFullGraphicsController)gc).ToolsToolbarEnableOnlyHand();
                }

                runButton.Content = "Continue";
                statusBarMessagePanel.Content = "Paused...";
                runButton.ToolTip = "Continue the Simulation.";

            }
            else if (sim.RunStatus == SimulationBase.RUNSTAT_PAUSE)
            {
                abortButton.IsEnabled = true;
                sim.RunStatus = SimulationBase.RUNSTAT_RUN;
                runButton.Content = "Pause";
                statusBarMessagePanel.Content = "Running...";

                if (gc is VTKFullGraphicsController == true)
                {
                    ((VTKFullGraphicsController)gc).ToolsToolbar_IsEnabled = false;
                }
            }
            else
            {
                if (vcrControl != null)
                {
                    vcrControl.SetInactive();
                }

                // only check for unique names if database writing is on and the unique names option is on
                /*if (skipDataWriteMenu.IsChecked == false && uniqueNamesMenu.IsChecked == true)
                {
                    string nameTemplate = "Enter a unique name here...";

                    // if database writing is on, notify the user that it may be a good idea to have unique experiment names
                    // don't consider the template name a good idea
                    if (configurator.Protocol.experiment_name == nameTemplate || checkExpNameUniqueness(configurator.Protocol.experiment_name) == false)
                    {
                        string messageBoxText = "Consider using a unique, meaningful experiment name.\n\nDo you want to go back to setup to make this change?";
                        string caption = "Experiment name unchanged";
                        MessageBoxButton button = MessageBoxButton.YesNoCancel;
                        MessageBoxImage icon = MessageBoxImage.Warning;

                        // Display message box
                        MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);

                        // Process message box results
                        switch (result)
                        {
                            case MessageBoxResult.Yes:
                                // show the sim setup panels if they are closed
                                ProtocolToolWindow.Activate();
                                // switch to the panel that has the name, give the name box the focus and change
                                // its content to something indicating what the user should do
                                ProtocolToolWindow.SelectSimSetupInGUISetExpName(nameTemplate);
                                return;
                            case MessageBoxResult.No:
                                break;
                            default:
                                return;
                        }
                    }
                }*/
                if (tempFileContent == false && sop.Protocol.SerializeToStringSkipDeco() == orig_content)
                {
                    // initiating a run starts always at repetition 1
                    repetition = 1;
                    // call with false (lockSaveStartSim(false)) or modify otherwise to enable the simulation to continue from the last visible state
                    // after a run or vcr playback
                    lockSaveStartSim(MainWindow.CheckControlFlag(MainWindow.CONTROL_FORCE_RESET));
                }
                else
                {
                    MessageBoxResult result = toolWin.ScenarioContentChanged();
                    // Process message box results
                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            sop.Protocol.SerializeToFile();
                            orig_content = sop.Protocol.SerializeToStringSkipDeco();
                            orig_path = System.IO.Path.GetDirectoryName(protocol_path.LocalPath);
                            // initiating a run starts always at repetition 1
                            repetition = 1;
                            lockSaveStartSim(true);
                            tempFileContent = false;
                            break;
                        case MessageBoxResult.No:
                            if (saveScenarioUsingDialog() == true)
                            {
                                // initiating a run starts always at repetition 1
                                repetition = 1;
                                lockSaveStartSim(true);
                                tempFileContent = false;
                            }
                            break;
                        case MessageBoxResult.None:
                            // initiating a run starts always at repetition 1
                            repetition = 1;
                            lockSaveStartSim(true);
                            tempFileContent = false;
                            break;
                        case MessageBoxResult.Cancel:
                            // Do nothing...
                            break;
                    }
                }

                if (sim.RunStatus == SimulationBase.RUNSTAT_READY)
                {
                    if (Properties.Settings.Default.skipDataWrites == false)
                    {
                        sim.Reporter.StartReporter(sim);

                        if (DataBasket.hdf5file.assembleFullPath(sim.Reporter.AppPath, sim.Reporter.FileName, "vcr", ".hdf5", true) == false)
                        {
                            MessageBox.Show("Error creating HDF5 file. File might be currently open.", "HDF5 error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                        DataBasket.hdf5file.openWrite(true);
                        // group for this experiment
                        DataBasket.hdf5file.createGroup("/Experiment_VCR");

                        // the protocol as string; needed to reload arbitrary past experiments
                        DataBasket.hdf5file.writeString("Protocol", sop.Protocol.SerializeToString());
                        // frames group
                        DataBasket.hdf5file.createGroup("VCR_Frames");
                    }

                    runButton.Content = "Pause";
                    runButton.ToolTip = "Pause the Simulation.";
                    statusBarMessagePanel.Content = "Running...";
                    abortButton.IsEnabled = true;

                    runFinishedEvent.Reset();
                    sim.RunStatus = SimulationBase.RUNSTAT_RUN;
                }
            }
        }

        /// <summary>
        /// Initiate a simulation run
        /// This needs to work for any scenario, i.e., it needs to work in the general case and not just for a specific scenario.
        /// MAKE SURE TO DO WHAT IT SAYS IN PREVIOUS LINE!!!!
        /// </summary>
        private void runSim_VatRc()
        {

            if (sim.RunStatus == SimulationBase.RUNSTAT_RUN) return;

            if (vcrControl != null)
            {
                vcrControl.SetInactive();
            }

            bool mouseDrag = MainWindow.CheckControlFlag(MainWindow.CONTROL_MOUSE_DRAG);
            repetition = 1;
            lockSaveStartSim(!mouseDrag);

            if (sim.RunStatus == SimulationBase.RUNSTAT_READY)
            {
                if (Properties.Settings.Default.skipDataWrites == false)
                {
                    sim.Reporter.StartReporter(sim);
                }
                runFinishedEvent.Reset();
                sim.RunStatus = SimulationBase.RUNSTAT_RUN;
            }

        }

        public MessageBoxResult saveDialog()
        {
            // Configure the message box to be displayed
            string messageBoxText = "Scenario parameters have changed. Do you want to overwrite the information in " + extractFileName() + "?";
            string caption = "Scenario Changed";
            MessageBoxButton button = MessageBoxButton.YesNoCancel;
            MessageBoxImage icon = MessageBoxImage.Warning;

            // Display message box
            return MessageBox.Show(messageBoxText, caption, button, icon);
        }

        public void DisplayCellInfo(int cellID)
        {
            if (cellID >= SimulationBase.dataBasket.Cells.Count)
            {
                MessageBox.Show("No cell exists with this ID.", "Invalid Cell Id",MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }
            else if (cellID < 0)
            {
                MessageBox.Show("Please enter a cell ID greater than 0.", "Invalid Cell Id", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            Cell selectedCell = SimulationBase.dataBasket.Cells[cellID];
            List<CellMolecularInfo> currConcs = new List<CellMolecularInfo>();

            txtCellIdent.Text = cellID.ToString();

            //enhancement - get cell location, velocity, force
            //double cellConc = selectedCell.
            tbMolConcs.Text = "Cell Id: " + cellID;

            SelectedCellInfo.ciList.Clear();
            currentConcs.Clear();

            CellXVF xvf = new CellXVF();
            xvf.name = "Location (μm)";
            xvf.x = selectedCell.SpatialState.X[0];
            xvf.y = selectedCell.SpatialState.X[1];
            xvf.z = selectedCell.SpatialState.X[2];
            SelectedCellInfo.ciList.Add(xvf);

            xvf = new CellXVF();
            xvf.name = "Velocity (μm/min)";
            xvf.x = selectedCell.SpatialState.V[0];
            xvf.y = selectedCell.SpatialState.V[1];
            xvf.z = selectedCell.SpatialState.V[2];
            SelectedCellInfo.ciList.Add(xvf);

            xvf = new CellXVF();
            xvf.name = "Force (μm/min2)";
            xvf.x = selectedCell.SpatialState.F[0];
            xvf.y = selectedCell.SpatialState.F[1];
            xvf.z = selectedCell.SpatialState.F[2];
            SelectedCellInfo.ciList.Add(xvf);

            //ItemsSource="{Binding Path=SelectedCellInfo.ciList}"
            lvCellXVF.ItemsSource = SelectedCellInfo.ciList;

            EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
            foreach (KeyValuePair<string, MolecularPopulation> kvp in SimulationBase.dataBasket.Cells[selectedCell.Cell_id].PlasmaMembrane.Populations)
            {
                string mol_name = er.molecules_dict[kvp.Key].Name;
                double conc = SimulationBase.dataBasket.Cells[selectedCell.Cell_id].PlasmaMembrane.Populations[kvp.Key].Conc.MeanValue();
                CellMolecularInfo cmi = new CellMolecularInfo();
                cmi.Molecule = "Cell: " + mol_name;
                cmi.Concentration = conc;
                // Passing zero vector to plasma membrane (TinySphere) returns the first moment of the moment-expansion field
                //cmi.Gradient = kvp.Value.Conc.Gradient(new double[3] { 0, 0, 0 });
                cmi.AddMoleculaInfo_gradient(kvp.Value.Conc.Gradient(new double[3] { 0, 0, 0 }));
                currConcs.Add(cmi);
                currentConcs.Add(cmi);
            }
            foreach (KeyValuePair<string, MolecularPopulation> kvp in SimulationBase.dataBasket.Cells[selectedCell.Cell_id].Cytosol.Populations)
            {
                string mol_name = er.molecules_dict[kvp.Key].Name;
                double conc = SimulationBase.dataBasket.Cells[selectedCell.Cell_id].Cytosol.Populations[kvp.Key].Conc.MeanValue();
                CellMolecularInfo cmi = new CellMolecularInfo();
                cmi.Molecule = "Cell: " + mol_name;
                cmi.Concentration = conc;
                // Passing zero vector to cytosol (TinyBall) returns the first moment of the moment-expansion field
                //cmi.Gradient = kvp.Value.Conc.Gradient(new double[3] { 0, 0, 0 });
                cmi.AddMoleculaInfo_gradient(kvp.Value.Conc.Gradient(new double[3] { 0, 0, 0 }));
                currConcs.Add(cmi);
                currentConcs.Add(cmi);
            }

            //need the ecm probe concentrations for this purpose
            foreach (ConfigMolecularPopulation mp in MainWindow.SOP.Protocol.scenario.environment.comp.molpops)
            {
                string name = MainWindow.SOP.Protocol.entity_repository.molecules_dict[mp.molecule.entity_guid].Name;
                double conc = SimulationBase.dataBasket.Environment.Comp.Populations[mp.molecule.entity_guid].Conc.Value(selectedCell.SpatialState.X);
                CellMolecularInfo cmi = new CellMolecularInfo();

                cmi.Molecule = "ECM: " + name;
                cmi.Concentration = conc;
                cmi.Gradient = SimulationBase.dataBasket.Environment.Comp.Populations[mp.molecule.entity_guid].Conc.Gradient(selectedCell.SpatialState.X);
                cmi.AddMoleculaInfo_gradient(SimulationBase.dataBasket.Environment.Comp.Populations[mp.molecule.entity_guid].Conc.Gradient(selectedCell.SpatialState.X));
                currConcs.Add(cmi);
                currentConcs.Add(cmi);
            }

            lvCellMolConcs.ItemsSource = currConcs;

            //Cell differentiation
            int nDiffState = selectedCell.DifferentiationState;
            if (selectedCell.Differentiator.State != null)
            {
                ObservableCollection<CellGeneInfo> gene_activations = new ObservableCollection<CellGeneInfo>();
                txtCellState.Text = selectedCell.Differentiator.State[nDiffState];
                ObservableCollection<double> activities = new ObservableCollection<double>();
                int len = selectedCell.Differentiator.activity.GetLength(1);
                for (int i = 0; i < len; i++)
                {
                    CellGeneInfo cgi = new CellGeneInfo();
                    cgi.Name = selectedCell.Genes[selectedCell.Differentiator.gene_id[i]].Name;
                    cgi.Activation = selectedCell.Differentiator.activity[nDiffState, i];
                    //double d = selectedCell.Differentiator.activity[nDiffState, i];
                    //activities.Add(d);  // = selectedCell.Differentiator.activity[nDiffState];
                    gene_activations.Add(cgi);
                }
                //lvCellDiff.ItemsSource = activities;
                lvCellDiff.ItemsSource = gene_activations;
            }

            //Cell division
            int nDivState = selectedCell.DividerState;
            if (selectedCell.Divider.State != null)
            {
                ObservableCollection<CellGeneInfo> gene_activations2 = new ObservableCollection<CellGeneInfo>();
                txtDivCellState.Text = selectedCell.Divider.State[nDivState];
                txtDivCellGen.Text = selectedCell.generation.ToString();
                ObservableCollection<double> activities = new ObservableCollection<double>();
                int len = selectedCell.Divider.activity.GetLength(1);
                for (int i = 0; i < len; i++)
                {
                    CellGeneInfo cgi = new CellGeneInfo();
                    if (i > len - 1)
                        break;

                    cgi.Name = selectedCell.Genes[selectedCell.Divider.gene_id[i]].Name;
                    cgi.Activation = selectedCell.Divider.activity[nDivState, i];
                    gene_activations2.Add(cgi);
                }
                lvCellDiv.ItemsSource = gene_activations2;
            }

            ToolWinCellInfo.Open();
            TabItemMolConcs.Visibility = System.Windows.Visibility.Visible;
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

#if DATABASE_HOOKED_UP   
            this.menu_ActivateAnalysisChart.IsEnabled = false;
#endif

            if (CheckMouseLeftState(MOUSE_LEFT_TRACK) == true)
            {
                SetMouseLeftState(MOUSE_LEFT_TRACK, false);
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
            if (tempFileContent == true || saveTempFiles() == true)
            {
                applyTempFilesAndSave(true);
            }

            Nullable<bool> result = loadScenarioUsingDialog();

            // Process open file dialog box results
            if (result == true)
            {
                prepareProtocol(ReadJson(""));
            }

        }

        // This sets whether the Save command can be executed, which enables/disables the menu item
        [DebuggerStepThrough]
        private void CommandBindingSave_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void CommandBindingSave_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            ToolWin.Apply();

            FileInfo fi = new FileInfo(sop.Protocol.FileName);

            if (fi.IsReadOnly == false || !fi.Exists)
            {
                sop.Protocol.SerializeToFile();
                orig_content = sop.Protocol.SerializeToStringSkipDeco();
            }
            else
            {
                string messageBoxText = "The file is write protected: " + sop.Protocol.FileName;
                string caption = "File write protected";
                MessageBoxButton button = MessageBoxButton.OK;
                MessageBoxImage icon = MessageBoxImage.Warning;

                // display message box
                MessageBox.Show(messageBoxText, caption, button, icon);
            }
            saveStoreFiles();
            tempFileContent = false;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            saveStoreFiles();

            if ((tempFileContent == true || saveTempFiles() == true) && applyTempFilesAndSave(false) == false)
            {
                // Note: this is a cute idea, canceling the exit, but we'd need a 'discard' button in addition
                // to cancel to make this usable in a convenient way; without it the user would be forced to save the changes
                //e.Cancel = true;
                //return;
            }

            // terminate the simulation thread first
            if (simThread != null && simThread.IsAlive)
            {
                simThread.Abort();
            }

            if (Properties.Settings.Default.skipDataWrites == false)
            {
                // reporter and hdf5 close
                closeOutputFiles();
            }

            // clear the vcr cache
            if (vcrControl != null)
            {
                vcrControl.ReleaseVCR();
            }

            // vtk cleanup
            gc.Cleanup();

            // close the dev help
            if (devHelpProc != null && devHelpProc.HasExited != true)
            {
                devHelpProc.CloseMainWindow();
                devHelpProc.Close();
            }

            // close any open document window
            if (dw != null)
            {
                dw.Close();
            }

            // if this option is selected then save the currently open scenario file name
            if (openLastScenarioMenu.IsChecked == true)
            {
                Properties.Settings.Default.lastOpenScenario = extractFileName();
            }

            // save the preferences
            Properties.Settings.Default.Save();

            //save renderSkin if changed.
            foreach (var skin in SOP.SkinList)
            {
                skin.SaveChanges();
            }

        }

        private void exitApp_Click(object sender, RoutedEventArgs e)
        {
            CloseApp();
        }

        private void CellSelectionToolCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;

            if (cb.SelectedIndex < 0)
            {
                return;
            }

            byte index = (byte)(cb.SelectedIndex);

            SetMouseLeftState(index, true);

            if (gc is VTKFullGraphicsController == true)
            {
                VTKFullGraphicsController gcHandle = (VTKFullGraphicsController)gc;

                if (index == MOUSE_LEFT_NONE)
                {
                    gcHandle.HandToolButton_IsEnabled = false;
                }
                else
                {
                    gcHandle.HandToolButton_IsEnabled = true;
                }
            }
        }

        /// <summary>
        /// This loads the blank scenario
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newScenario_Click(object sender, RoutedEventArgs e)
        {
            saveStoreFiles();

            if (tempFileContent == true || saveTempFiles() == true)
            {
                applyTempFilesAndSave(true);
            }

            //Grab the blank scenario
            string file = "daphne_blank_scenario.json";
            string filename = appPath + @"\Config\" + file;
            Uri uri_path = new Uri(filename);

            bool file_exists = File.Exists(uri_path.LocalPath);
            if (!file_exists)
            {
                MessageBox.Show("Blank scenario file not found.");
                return;
            }

            setScenarioPaths(filename);
            prepareProtocol(ReadJson(""));
        }

        private void prepareProtocol(Protocol protocol)
        {
            // prevent further loading if the protocol is invalid
            if (protocol == null)
            {
                return;
            }

            // show the inital state
            lockAndResetSim(true, protocol);
            if (loadSuccess == false)
            {
                return;
            }
            ProtocolToolWindow.IsEnabled = true;
            saveScenario.IsEnabled = true;
            displayTitle();

            if (sop.Protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == true)
            {
                if (ToolWinType == ToolWindowType.Tissue)
                {
                    VTKDisplayDocWindow.Activate();
                }
                else if (ToolWinType == ToolWindowType.VatRC)
                {
                    ReacComplexChartWindow.Activate();
                }
            }
            else if (sop.Protocol.CheckScenarioType(Protocol.ScenarioType.VAT_REACTION_COMPLEX) == true)
            {
                toolWin.Activate();
            }
        }

        private void abortButton_Click(object sender, RoutedEventArgs e)
        {
            runButton.IsEnabled = false;
            mutex = true;
            if (sim.RunStatus == SimulationBase.RUNSTAT_RUN || sim.RunStatus == SimulationBase.RUNSTAT_PAUSE)
            {
                sim.RunStatus = SimulationBase.RUNSTAT_ABORT;
            }
            // 1/14/15: this code seems to be legacy and no longer in use; remove in the future if no problems arise or reenable otherwise
            //else
            //{
            //    saveTempFiles();
            //    updateGraphicsAndGUI();
            //}
        }

        private void helpAbout_Click(object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.ShowDialog();
        }

        /// <summary>
        /// This GenericPush method is called for pushing entities into the Protocol level.
        ///
        /// </summary>
        /// <param name="source"></param>
        public static void GenericPush(ConfigEntity source)
        {
            bool UserWantsNewEntity = false;    //true if user wants to create a new entity instead of overwriting existing entity
            ConfigEntity newEntity = null;      //potential new entity if user wants to create a new one instead of overwriting existing entity

            if (source == null)
            {
                MessageBox.Show("Nothing to push");
                return;
            }

            if (source is ConfigMolecule)
            {
                //LET'S TRY A GENERIC PUSHER
                PushEntity pm = new PushEntity();
                pm.DataContext = MainWindow.SOP;
                pm.EntityLevelDetails.DataContext = source;
                pm.ComponentLevelDetails.DataContext = null;

                ConfigMolecule erMol = MainWindow.SOP.Protocol.FindMolecule(((ConfigMolecule)source).Name);
                if (erMol != null)
                {                    
                    pm.ComponentLevelDetails.DataContext = erMol;
                    newEntity = ((ConfigMolecule)source).Clone(MainWindow.SOP.Protocol);  //to be used only if user wants to save as new entity
                }

                //Show the confirmation dialog
                if (pm.ShowDialog() == false)
                    return;

                UserWantsNewEntity = pm.UserWantsNewEntity;

            }
            else if (source is ConfigReaction)
            {
                //Use generic pusher
                PushEntity pr = new PushEntity();
                pr.EntityLevelDetails.DataContext = source;
                pr.ComponentLevelDetails.DataContext = null;

                if (MainWindow.SOP.Protocol.entity_repository.reactions_dict.ContainsKey(source.entity_guid))
                {
                    pr.ComponentLevelDetails.DataContext = MainWindow.SOP.Protocol.entity_repository.reactions_dict[source.entity_guid];
                    newEntity = ((ConfigReaction)source).Clone(false);  //to be used only if user wants to save as new entity
                }

                if (pr.ShowDialog() == false)
                    return;

                UserWantsNewEntity = pr.UserWantsNewEntity;
            }
            else if (source is ConfigCell)
            {
                PushEntity pcell = new PushEntity();
                pcell.EntityLevelDetails.DataContext = source;
                pcell.ComponentLevelDetails.DataContext = null;

                if (MainWindow.SOP.Protocol.entity_repository.cells_dict.ContainsKey(source.entity_guid))
                {
                    pcell.ComponentLevelDetails.DataContext = MainWindow.SOP.Protocol.entity_repository.cells_dict[source.entity_guid];
                    newEntity = ((ConfigCell)source).Clone(false);  //to be used only if user wants to save as new entity
                }

                if (pcell.ShowDialog() == false)
                    return;

                UserWantsNewEntity = pcell.UserWantsNewEntity;

            }
            else if (source is ConfigGene)
            {
                PushEntity pm = new PushEntity();
                pm.DataContext = MainWindow.SOP;
                pm.EntityLevelDetails.DataContext = source;
                pm.ComponentLevelDetails.DataContext = null;

                ConfigGene erGene = MainWindow.SOP.Protocol.FindGene(((ConfigGene)source).Name);
                if (erGene != null)
                {
                    pm.ComponentLevelDetails.DataContext = erGene;
                    newEntity = ((ConfigGene)source).Clone(MainWindow.SOP.Protocol);  //to be used only if user wants to save as new entity
                }

                //Show the confirmation dialog
                if (pm.ShowDialog() == false)
                {
                    return;
                }

                UserWantsNewEntity = pm.UserWantsNewEntity;

            }
            else if (source is ConfigReactionComplex)
            {
                PushEntity pm = new PushEntity();
                pm.DataContext = MainWindow.SOP;
                pm.EntityLevelDetails.DataContext = source;
                pm.ComponentLevelDetails.DataContext = null;

                if (MainWindow.SOP.Protocol.entity_repository.reaction_complexes_dict.ContainsKey(source.entity_guid))
                {
                    ConfigReactionComplex erRC = MainWindow.SOP.Protocol.entity_repository.reaction_complexes_dict[source.entity_guid];
                    pm.ComponentLevelDetails.DataContext = erRC;
                    newEntity = ((ConfigReactionComplex)source).Clone(false);  //to be used only if user wants to save as new entity
                }

                //Show the confirmation dialog
                if (pm.ShowDialog() == false)
                {
                    return;
                }

                UserWantsNewEntity = pm.UserWantsNewEntity;
            }
            else if (source is ConfigTransitionScheme)
            {
                MessageBox.Show(string.Format("Entity type {0} 'save' operation not yet supported.", source.GetType().ToString()));
                return;
            }
            else if (source is ConfigTransitionDriver)
            {
                MessageBox.Show(string.Format("Entity type {0} 'save' operation not yet supported.", source.GetType().ToString()));
                return;
            }
            else
            {
                MessageBox.Show(string.Format("Entity type {0} 'save' operation not supported.", source.GetType().ToString()));
                return;
            }


            //If we get here, then the user confirmed a PUSH

            //Push the entity
            Protocol B = MainWindow.SOP.Protocol;
            Level.PushStatus status = B.pushStatus(source);
            if (status == Level.PushStatus.PUSH_INVALID)
            {
                MessageBox.Show("Entity not pushable.");
                return;
            }

            if (status == Level.PushStatus.PUSH_CREATE_ITEM)
            {
                B.repositoryPush(source, status); // push into B, inserts as new
            }
            else // the item exists; could be newer or older
            {
                if (UserWantsNewEntity == false)
                {
                    B.repositoryPush(source, status); // push into B - overwrites existing entity's properties
                }
                else //push as new
                {
                    source.GenerateNewName(MainWindow.SOP.Protocol, "_New");
                    B.repositoryPush(newEntity, Level.PushStatus.PUSH_CREATE_ITEM);  //create new entity in repository
                }


            }
        }

        private void menuUserStore_Click(object sender, RoutedEventArgs e)
        {
            //readStores();
            statusBarMessagePanel.Content = "Ready:  User Store";
            ProtocolToolWindow.Close();
            VTKDisplayDocWindow.Close();
            ReacComplexChartWindow.Close();
            ComponentsToolWindow.DataContext = SOP.UserStore;
            CellStudioToolWindow.DataContext = SOP.UserStore;
            ComponentsToolWindow.Refresh();
            ReturnToProtocolButton.Visibility = Visibility.Visible;
            menuProtocolStore.IsEnabled = true;
        }

        private void menuDaphneStore_Click(object sender, RoutedEventArgs e)
        {
            //readStores();
            statusBarMessagePanel.Content = "Ready:  Daphne Store";
            ProtocolToolWindow.Close();
            VTKDisplayDocWindow.Close();
            ReacComplexChartWindow.Close();
            ComponentsToolWindow.DataContext = SOP.DaphneStore;
            CellStudioToolWindow.DataContext = SOP.DaphneStore;
            ComponentsToolWindow.Refresh();
            ReturnToProtocolButton.Visibility = Visibility.Visible;
            menuProtocolStore.IsEnabled = true;
        }

        private void menuProtocolStore_Click(object sender, RoutedEventArgs e)
        {
            statusBarMessagePanel.Content = "Ready:  Protocol";
            ProtocolToolWindow.Open();
            ComponentsToolWindow.DataContext = SOP.Protocol;
            CellStudioToolWindow.DataContext = SOP.Protocol;
            ReturnToProtocolButton.Visibility = Visibility.Collapsed;
            menuProtocolStore.IsEnabled = false;

            if (SOP.Protocol.scenario is TissueScenario)
            {
                VTKDisplayDocWindow.Activate();
            }
            else
            {
                ReacComplexChartWindow.Activate();
            }
        }

        //private void readStores()
        //{
        //    //Code to retrieve userstore and daphnestore - deserialize from files
        //    sop.UserStore.FileName = "Config\\daphne_userstore.json";
        //    sop.UserStore.TempFile = "Config\\temp_userstore.json";
        //    sop.DaphneStore.FileName = "Config\\daphne_daphnestore.json";
        //    sop.DaphneStore.TempFile = "Config\\temp_daphnestore.json";
        //    sop.DaphneStore = sop.DaphneStore.Deserialize();
        //    sop.UserStore = sop.UserStore.Deserialize();
        //    orig_daphne_store_content = sop.DaphneStore.SerializeToString();
        //    orig_user_store_content = sop.UserStore.SerializeToString();
        //}

        private void pushMol_Click(object sender, RoutedEventArgs e)
        {
            //load the stores only as needed
            //readStores();

            PushBetweenLevels pushWindow = new PushBetweenLevels(PushBetweenLevels.PushLevelEntityType.Molecule);
            pushWindow.DataContext = SOP;

            pushWindow.ShowDialog();

            //if (pushWindow.ShowDialog() == true)
            //{
            //    saveStoreFiles();
            //}
        }

        private void pushGene_Click(object sender, RoutedEventArgs e)
        {
            //load the stores only as needed
            //readStores();

            PushBetweenLevels pushWindow = new PushBetweenLevels(PushBetweenLevels.PushLevelEntityType.Gene);
            pushWindow.DataContext = SOP;

            pushWindow.ShowDialog();

            //if (pushWindow.ShowDialog() == true)
            //{
            //    saveStoreFiles();
            //}
        }

        private void pushReac_Click(object sender, RoutedEventArgs e)
        {
            //load the stores only as needed
            //readStores();

            PushBetweenLevels pushWindow = new PushBetweenLevels(PushBetweenLevels.PushLevelEntityType.Reaction);
            pushWindow.DataContext = SOP;

            pushWindow.ShowDialog();

            //if (pushWindow.ShowDialog() == true)
            //{
            //    saveStoreFiles();
            //}
        }

        private void pushCell_Click(object sender, RoutedEventArgs e)
        {
            //load the stores only as needed
            //readStores();

            PushBetweenLevels pushWindow = new PushBetweenLevels(PushBetweenLevels.PushLevelEntityType.Cell);
            pushWindow.DataContext = SOP;

            pushWindow.ShowDialog();

            //if (pushWindow.ShowDialog() == true)
            //{
            //    saveStoreFiles();
            //}
        }


        private void pushDiffScheme_Click(object sender, RoutedEventArgs e)
        {
            //load the stores only as needed
            //readStores();
            PushBetweenLevels pushWindow = new PushBetweenLevels(PushBetweenLevels.PushLevelEntityType.DiffScheme);
            pushWindow.DataContext = SOP;

            pushWindow.ShowDialog();

            //if (pushWindow.ShowDialog() == true)
            //{
            //    saveStoreFiles();
            //}
        }

        private void pushTransDriver_Click(object sender, RoutedEventArgs e)
        {
            //load the stores only as needed
            //readStores();
            PushBetweenLevels pushWindow = new PushBetweenLevels(PushBetweenLevels.PushLevelEntityType.TransDriver);
            pushWindow.DataContext = SOP;

            pushWindow.ShowDialog();

            //if (pushWindow.ShowDialog() == true)
            //{
            //    saveStoreFiles();
            //}
        }

        private void pushReacComplex_Click(object sender, RoutedEventArgs e)
        {
            //load the stores only as needed
            //readStores();
            PushBetweenLevels pushWindow = new PushBetweenLevels(PushBetweenLevels.PushLevelEntityType.ReactionComplex);
            pushWindow.DataContext = SOP;

            pushWindow.ShowDialog();

            //if (pushWindow.ShowDialog() == true)
            //{
            //    saveStoreFiles();
            //}
        }

        /// <summary>
        /// when user select another render option, then reapply
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CellsColorByCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //reset display
            //remove unnecessary refresh/invalid refresh
            if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0 || gc as VTKFullGraphicsController == null) return;
            vtkDataBasket.SetupVTKData(sop.Protocol);
            gc.CreatePipelines();
            UpdateGraphics();
            (gc as VTKFullGraphicsController).Rwc.Invalidate();
        }

        private void CellRenderOnOffChanged(object sender, RoutedEventArgs e)
        {
            //only respond to the checkbox.
            if (e.OriginalSource is CheckBox == false) return;
            vtkDataBasket.SetupVTKData(sop.Protocol);
            gc.CreatePipelines();
            UpdateGraphics();
            (gc as VTKFullGraphicsController).Rwc.Invalidate();
        }

        private void btnShowCellInfoById_Click(object sender, RoutedEventArgs e)
        {
            int cellid;
            bool result = int.TryParse(txtCellIdent.Text, out cellid);
            DisplayCellInfo(cellid);
        }

        private void CellOptionsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            ECMOptionsExpander.IsExpanded = false;
        }

        private void ECMOptionsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            CellOptionsExpander.IsExpanded = false;
        }

        private void MolPopRenderOnOffChanged(object sender, RoutedEventArgs e)
        {
            vtkDataBasket.SetupVTKData(sop.Protocol);
            gc.CreatePipelines();
            UpdateGraphics();
            (gc as VTKFullGraphicsController).Rwc.Invalidate();
        }

        private void ReturnToProtocolButton_Click(object sender, RoutedEventArgs e)
        {
            statusBarMessagePanel.Content = "Ready:  Protocol";
            ProtocolToolWindow.Open();            
            ComponentsToolWindow.DataContext = SOP.Protocol;
            CellStudioToolWindow.DataContext = SOP.Protocol;
            ReturnToProtocolButton.Visibility = Visibility.Collapsed;
            menuProtocolStore.IsEnabled = false;

            if (SOP.Protocol.scenario is TissueScenario)
            {
                VTKDisplayDocWindow.Activate();
            }
            else
            {
                ReacComplexChartWindow.Activate();
            }
        }
        
    }


    /// <summary>
    /// exist to access renderpop options collection
    /// </summary>
    public class CellRenderPopFromProtocolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            var val = value as Protocol;
            if (val != null && val.scenario is TissueScenario)
            {
                var ts = val.scenario as TissueScenario;
                return ts.popOptions.cellPopOptions;
            }
            return null;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
