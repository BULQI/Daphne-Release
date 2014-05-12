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

using ActiproSoftware.Windows.Controls.Docking;
using Kitware.VTK;

using Ninject;
using Ninject.Parameters;

using Daphne;
using ManifoldRing;
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
        /// the absolute path where the installed, running executable resides
        /// </summary>
        public static string appPath;

        private DocWindow dw;
        private Thread simThread;
        private VCRControl vcrControl = null;
        public static Cell selectedCell = null;
        public static Object cellFitLock = new Object();
        public static double cellOpacity = 1.0;

        private static Simulation sim;
        public static Simulation Sim
        {
            get { return sim; }
            set { sim = value; }
        }

        private Reporter reporter;
        private Process devHelpProc;
        private static SimConfigurator configurator = null;
        private static int repetition;

        /// <summary>
        /// uri for the scenario file
        /// </summary>
        public static Uri scenario_path;
        private string orig_content, orig_path;

        private bool exportAllFlag = false;
        private string igGeneFolderName = "";

        /// <summary>
        /// constants used by the simulation and gui
        /// </summary>
        public static byte CONTROL_NONE = 0,
                           CONTROL_FORCE_RESET = (1 << 0),
                           CONTROL_DB_LOAD = (1 << 1),
                           CONTROL_ZERO_FORCE = (1 << 2),
                           CONTROL_NEW_RUN = (1 << 3),
                           CONTROL_UPDATE_GUI = (1 << 4);

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

        private static VTKGraphicsController gc;
        private static VTKDataBasket vtkDataBasket;

        /// <summary>
        /// retrieve a pointer to the (for now, singular) VTK graphics actors
        /// </summary>
        public static VTKGraphicsController GC
        {
            get { return gc; }
        }

        /// <summary>
        /// retrieve a pointer to the VTK data basket object
        /// </summary>
        public static VTKDataBasket VTKBasket
        {
            get { return vtkDataBasket; }
        }

        /// <summary>
        /// retrieve a pointer to the configurator
        /// </summary>
        public static SimConfigurator SC
        {
            get { return configurator; }
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

        public static ChartViewToolWindow ST_ReacComplexChartWindow;

        public MainWindow()
        {
            InitializeComponent();

            ST_ReacComplexChartWindow = ReacComplexChartWindow;

            this.ToolWinCellInfo.Close();

            CreateAndSerializeDaphneScenarios();

            // NEED TO UPDATE RECENT FILES LIST CODE FOR DAPHNE!!!!

            // implementing the recent files list
            //RecentFileList.MaxNumberOfFiles = 10;
            // disable the following to cause saving into the registry
            // on my Windows 7 machine, using the xml persister will create the file C:\Users\Harald\AppData\Roaming\Microsoft\PlazaSur\RecentFileList.xml
            // and save the history there
            //RecentFileList.UseXmlPersister();
            // the event handler to run when clicking on an entry in the recent files list
            //RecentFileList.MenuClick += (s, e) => loadScenarioFromFile(e.Filepath);


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
                file = "daphne_driver_locomotion_scenario.json";
                //file = "daphne_blank_scenario.json";
            }

            // attempt to load a default simulation file; if it doesn't exist disable the gui
            //skg daphne Wednesday, May 08, 2013
            scenario_path = new Uri(appPath + @"\Config\" + file);
            orig_path = System.IO.Path.GetDirectoryName(scenario_path.LocalPath);

            bool file_exists = File.Exists(scenario_path.LocalPath);

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

            SimConfigToolWindow.MW = this;

            // Hide fitting tab control until sim has ended
            this.LPFittingToolWindow.Close();
            this.menu_ActivateLPFitting.IsEnabled = false;
            this.ExportMenu.IsEnabled = false;
            // And hide stats results chart for now
            //this.ChartViewDocWindow.Close();

#if DATABASE_HOOKED_UP        
            this.menu_ActivateAnalysisChart.IsEnabled = false;
#endif

            // create the simulation
            sim = new Simulation();
            // reporter
            reporter = new Reporter();

            // vtk data basket to hold vtk data for entities with graphical representation
            vtkDataBasket = new VTKDataBasket();
            // graphics controller to manage vtk objects
            gc = new VTKGraphicsController(this);
            // NOTE: For now, setting data context of VTK MW display grid to only instance of GraphicsController.
            vtkDisplay_DockPanel.DataContext = gc;
            // this.SimConfigSplitContainer.ResizeSlots(new double[2]{0.2, 0.8});
            // set the save state menu's context to the simulation so we can change its enabled property based on values of the simulation
            saveState.DataContext = sim;

            if (file_exists)
            {
                initialState(true, true, "");
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
        }

        public void UpdateGraphics()
        {
            vtkDataBasket.UpdateData();
            gc.DrawFrame(sim.GetProgressPercent());
        }

        /// <summary>
        /// Create and serialize all scenarios
        /// </summary>
        public void CreateAndSerializeDaphneScenarios()
        {
            //BLANK SCENARIO
            var config = new SimConfigurator("Config\\daphne_blank_scenario.json");
            ConfigCreators.CreateAndSerializeBlankScenario(config.SimConfig);
            //serialize to json
            config.SerializeSimConfigToFile();

            //DRIVER-LOCOMOTOR SCENARIO
            config = new SimConfigurator("Config\\daphne_driver_locomotion_scenario.json");
            ConfigCreators.CreateAndSerializeDriverLocomotionScenario(config.SimConfig);
            // serialize to json
            config.SerializeSimConfigToFile();

            //DIFFUSIION SCENARIO
            config = new SimConfigurator("Config\\daphne_diffusion_scenario.json");
            ConfigCreators.CreateAndSerializeDiffusionScenario(config.SimConfig);
            //Serialize to json
            config.SerializeSimConfigToFile();

            //LIGAND-RECEPTOR SCENARIO
            config = new SimConfigurator("Config\\daphne_ligand_receptor_scenario.json");
            ConfigCreators.CreateAndSerializeLigandReceptorScenario(config.SimConfig);
            //serialize to json
            config.SerializeSimConfigToFile();


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

        private void setScenarioPaths(string filename)
        {
            scenario_path = new Uri(filename);
            orig_path = System.IO.Path.GetDirectoryName(scenario_path.LocalPath);
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
        }

        private Nullable<bool> saveScenarioUsingDialog()
        {
            // Configure save file dialog box
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
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
                
                configurator.FileName = filename;
                configurator.SerializeSimConfigToFile();

                orig_content = configurator.SerializeSimConfigToStringSkipDeco();
                scenario_path = new Uri(filename);
                orig_path = System.IO.Path.GetDirectoryName(scenario_path.LocalPath);
                displayTitle();
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
                    initialState(false, Simulation.dataBasket.Cells.Count < 1 || completeReset == true, "");
                    enableCritical(loadSuccess);
                    if (loadSuccess == false)
                    {
                        return;
                    }

                    // it doesn't make sense to run a simulation if there are no cells after the reinitialization
                    //if (Simulation.dataBasket.Cells.Count < 1)
                    //{
                    //    MessageBox.Show("Aborting simulation! Load a valid scenario or add cells into the current one.", "Empty simulation", MessageBoxButton.OK, MessageBoxImage.Error);
                    //    return;
                    //}

                    // next time around, force a reset
                    MainWindow.SetControlFlag(MainWindow.CONTROL_FORCE_RESET, true);
#if CELL_REGIONS
                // hide the cell regions
                foreach (Region rr in configurator.SimConfig.scenario.regions)
                {
                    // Use the utility dict to find the box associated with this region
                    BoxSpecification bb = configurator.SimConfig.box_guid_box_dict[rr.region_box_spec_guid_ref];

                    // Property changed notifications will take care of turning off the Widgets and Actors
                    bb.box_visibility = false;
                    rr.region_visibility = false;
                }
#endif
                    // hide the regions used to control Gaussians
                    foreach (GaussianSpecification gg in configurator.SimConfig.entity_repository.gaussian_specifications)
                    {
                        // Use the utility dict to find the box associated with this region
                        BoxSpecification bb = configurator.SimConfig.box_guid_box_dict[gg.gaussian_spec_box_guid_ref];

                        // Property changed notifications will take care of turning off the Widgets and Actors
                        bb.box_visibility = false;
                        gg.gaussian_region_visibility = false;
                    }

                    //// always reset the simulation for now to start at the beginning
                    //if (Properties.Settings.Default.skipDataBaseWrites == false)
                    //{
                    //    DataBaseTools.CreateExpInDataBase();
                    //    DataBaseTools.SaveCellSetIDs();
                    //    DataBaseTools.CreateSaveAttributes();
                    //}

                    // since the above call resets the experiment name each time, reset comparison string
                    // so we don't bother people about saving just because of this change
                    // NOTE: If we want to save scenario along with data, need to save after this GUID change is made...
                    orig_content = configurator.SerializeSimConfigToStringSkipDeco();
                    sim.restart();
                    UpdateGraphics();

                    // prevent the user from running certain tasks immediately, crashing the simulation
                    //resetButton.IsEnabled = false;
                    resetButton.Content = "Abort";
                    //runButton.IsEnabled = false;
                    enableFileMenu(false);
                    analysisMenu.IsEnabled = false;
                    optionsMenu.IsEnabled = false;
                    gc.ToolsToolbar_IsEnabled = false;
                    gc.DisablePickingButtons();
                    VCR_Toolbar.IsEnabled = false;
                    this.menu_ActivateSimSetup.IsEnabled = false;
                    SimConfigToolWindow.Close();

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
            recentFileList.IsEnabled = enable;
            //exitApp.IsEnabled = enable;
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

                sim.reset();
                // reset cell tracks and free memory
                //////////gc.CleanupTracks();
                gc.CellController.SetCellOpacities(1.0);
                fitCellOpacitySlider.Value = 1.0;
                UpdateGraphics();

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
            ////Console.WriteLine("ExpID is: " + sim.SC.id.ToString());
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

        private string extractFileName()
        {
            string[] segments = scenario_path.LocalPath.Split('\\');

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

        private void setReporterFolder_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dlg = new System.Windows.Forms.FolderBrowserDialog();

            dlg.Description = "Select report output folder";
            dlg.SelectedPath = appPath;
            if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                reporter.ReportFolder = dlg.SelectedPath;
            }
            else
            {
                reporter.ReportFolder = appPath;
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
            if (sim.RunStatus == Simulation.RUNSTAT_RUN)
            {
                sim.RunStatus = Simulation.RUNSTAT_ABORT;
            }
            else
            {
                applyChanges();
            }
        }

        /// <summary>
        /// reset the simulation to a random initial state and ready it for running (time = 0); the simulation will then start automatically
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void runButton_Click(object sender, RoutedEventArgs e)
        {
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

        public static void GUIInteractionToWidgetCallback(object sender, PropertyChangedEventArgs e)
        {
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
                MainWindow.GC.Regions[box.box_guid].ShowWidget(box.box_visibility);
            }

            // Catch-all for other scale / translation manipulations
            if (MainWindow.VTKBasket.Regions.ContainsKey(box.box_guid) && MainWindow.GC.Regions.ContainsKey(box.box_guid))
            {
                MainWindow.VTKBasket.Regions[box.box_guid].SetTransform(box.transform_matrix, RegionControl.PARAM_SCALE);
                MainWindow.GC.Regions[box.box_guid].SetTransform(box.transform_matrix, RegionControl.PARAM_SCALE);
                MainWindow.GC.Rwc.Invalidate();
            }
        }
#if CELL_REGIONS
        public static void GUIRegionSurfacePropertyChange(object sender, PropertyChangedEventArgs e)
        {
            Region region = (Region)sender;

            if (region == null)
            {
                return;
            }

            if (e.PropertyName == "region_visibility")
            {
                MainWindow.GC.Regions[region.region_box_spec_guid_ref].ShowActor(MainWindow.GC.Rwc.RenderWindow, region.region_visibility);
                MainWindow.GC.Rwc.Invalidate();
            }
            if (e.PropertyName == "region_type")
            {
                MainWindow.VTKBasket.Regions[region.region_box_spec_guid_ref].SetShape(region.region_type);
                MainWindow.GC.Regions[region.region_box_spec_guid_ref].SetShape(MainWindow.GC.Rwc.RenderWindow, region.region_type);
                MainWindow.GC.Rwc.Invalidate();
            }
            if (e.PropertyName == "region_color")
            {
                MainWindow.GC.Regions[region.region_box_spec_guid_ref].SetColor(region.region_color.ScR, region.region_color.ScG, region.region_color.ScB);
                MainWindow.GC.Regions[region.region_box_spec_guid_ref].SetOpacity(region.region_color.ScA);
                MainWindow.GC.Rwc.Invalidate();
            }
            return;
        }
#endif
        public static void GUIGaussianSurfaceVisibilityToggle(object sender, PropertyChangedEventArgs e)
        {
            GaussianSpecification gauss = (GaussianSpecification)sender;

            if (gauss == null)
            {
                return;
            }

            if (e.PropertyName == "gaussian_region_visibility")
            {
                MainWindow.GC.Regions[gauss.gaussian_spec_box_guid_ref].ShowActor(MainWindow.GC.Rwc.RenderWindow, gauss.gaussian_region_visibility);
                MainWindow.GC.Rwc.Invalidate();
            }
            if (e.PropertyName == "gaussian_spec_color")
            {
                MainWindow.GC.Regions[gauss.gaussian_spec_box_guid_ref].SetColor(gauss.gaussian_spec_color.ScR, gauss.gaussian_spec_color.ScG, gauss.gaussian_spec_color.ScB);
                MainWindow.GC.Regions[gauss.gaussian_spec_box_guid_ref].SetOpacity(gauss.gaussian_spec_color.ScA);
                MainWindow.GC.Rwc.Invalidate();
            }
            return;
        }

        private void initialState(bool newFile, bool completeReset, string jsonScenarioString)
        {
            // if we read a new file we may have to disconnect event handlers if they were connected previously;
            // we always must deserialize the file
            if (newFile == true)
            {
                if (configurator != null)
                {
                    // if we configured a simulation prior to this call, remove all property changed event handlers

                    for (int i = 0; i < configurator.SimConfig.entity_repository.box_specifications.Count; i++)
                    {
                        configurator.SimConfig.entity_repository.box_specifications[i].PropertyChanged -= GUIInteractionToWidgetCallback;
                    }
#if CELL_REGIONS
                    for (int i = 0; i < configurator.SimConfig.scenario.regions.Count; i++)
                    {
                        configurator.SimConfig.scenario.regions[i].PropertyChanged -= GUIRegionSurfacePropertyChange;
                    }
#endif
                    for (int i = 0; i < configurator.SimConfig.entity_repository.gaussian_specifications.Count; i++)
                    {
                        configurator.SimConfig.entity_repository.gaussian_specifications[i].PropertyChanged -= GUIGaussianSurfaceVisibilityToggle;
                    }
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
                    configurator = new SimConfigurator(scenario_path.LocalPath);
                    // catch xaml parse exception if it's not a good sim config file
                    try
                    {
                        configurator.DeserializeSimConfig();
                        //configurator.SimConfig.ChartWindow = ReacComplexChartWindow;
                    }
                    catch (Exception e)
                    {
                        string output = string.Format("Error deserializing: {0}", e.Message);
                        MessageBox.Show(output);
                        //MessageBox.Show("There is a problem loading the configuration file.\nPress OK, then try to load another.", "Application error", MessageBoxButton.OK, MessageBoxImage.Error);
                        handleLoadFailure("There is a problem loading the configuration file.\nPress OK, then try to load another.");
                        return;
                    }
                }
                orig_content = configurator.SerializeSimConfigToStringSkipDeco();
                orig_path = System.IO.Path.GetDirectoryName(scenario_path.LocalPath);
            }

            // (re)connect the handlers for the property changed event
            for (int i = 0; i < configurator.SimConfig.entity_repository.box_specifications.Count; i++)
            {
                configurator.SimConfig.entity_repository.box_specifications[i].PropertyChanged += GUIInteractionToWidgetCallback;
            }
#if CELL_REGIONS
            for (int i = 0; i < configurator.SimConfig.scenario.regions.Count; i++)
            {
                configurator.SimConfig.scenario.regions[i].PropertyChanged += GUIRegionSurfacePropertyChange;
            }
#endif
            for (int i = 0; i < configurator.SimConfig.entity_repository.gaussian_specifications.Count; i++)
            {
                configurator.SimConfig.entity_repository.gaussian_specifications[i].PropertyChanged += GUIGaussianSurfaceVisibilityToggle;
            }

            // GUI Resources
            // Set the data context for the main tab control config GUI
            this.SimConfigToolWindow.DataContext = configurator.SimConfig;

            // set up the simulation
            if (sim.Load(configurator.SimConfig, completeReset) == false)
            {
                handleLoadFailure("There is a problem loading the configuration file.\nPress OK, then try to load another.");
                return;
            }

            //temporary solution to avoid popup resaving states -axin
            if (true)
            {
                orig_content = configurator.SerializeSimConfigToStringSkipDeco();
            }

            vtkDataBasket.SetupVTKData(configurator.SimConfig);
            // Create all VTK visualization pipelines and elements
            gc.CreatePipelines();

            // clear the vcr cache
            if (vcrControl != null)
            {
                vcrControl.ReleaseVCR();
            }

            if (newFile)
            {
                gc.recenterCamera();
            }
            gc.Rwc.Invalidate();

            // TODO: Need to do this for all GCs eventually...
            // Add the RegionControl interaction event handlers here for easier reference to callback method
            foreach (KeyValuePair<string, RegionWidget> kvp in gc.Regions)
            {
                // NOTE: For now not doing any callbacks on property change for RegionControls...
                kvp.Value.ClearCallbacks();
                kvp.Value.AddCallback(new RegionWidget.CallbackHandler(gc.WidgetInteractionToGUICallback));
                kvp.Value.AddCallback(new RegionWidget.CallbackHandler(SimConfigToolWindow.RegionFocusToGUISection));
            }

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

        private void run()
        {
            while (true)
            {
                lock (sim)
                {
                    if (sim.RunStatus == Simulation.RUNSTAT_RUN)
                    {
                        // run the simulation forward to the next task
                        sim.RunForward();

                        // check for flags and execute applicable task(s)
                        if (sim.CheckFlag(Simulation.SIMFLAG_RENDER) == true)
                        {
                            UpdateGraphics();
                        }
                        if (sim.CheckFlag(Simulation.SIMFLAG_SAMPLE) == true && Properties.Settings.Default.skipDataBaseWrites == false)
                        {
                            reporter.AppendReporter(configurator.SimConfig, sim);
                        }

                        if (sim.RunStatus != Simulation.RUNSTAT_RUN)
                        {
                            // never rerun the simulation if the simulation was aborted
                            if (sim.RunStatus != Simulation.RUNSTAT_PAUSE && repetition < configurator.SimConfig.experiment_reps)
                            {
                                runButton.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new GUIDelegateNoArgs(RerunSimulation));
                            }
                            // for profiling: close the application after a completed experiment
                            else if (ControlledProfiling() == true && sim.RunStatus == Simulation.RUNSTAT_FINISHED && repetition >= configurator.SimConfig.experiment_reps)
                            {
                                runButton.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new GUIDelegateNoArgs(CloseApp));
                                return;
                            }
                            else if (sim.RunStatus == Simulation.RUNSTAT_FINISHED)
                            {
                                if (Properties.Settings.Default.skipDataBaseWrites == false)
                                {
                                    reporter.CloseReporter();
                                }
                                runButton.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new GUIDelegateTwoArgs(GUIUpdate), -1, false);
                            }
                        }
                    }
                    else if (sim.RunStatus == Simulation.RUNSTAT_ABORT)
                    {
                        if (Properties.Settings.Default.skipDataBaseWrites == false)
                        {
                            reporter.CloseReporter();
                        }
                        runButton.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new GUIDelegateNoArgs(applyChanges));
                        sim.RunStatus = Simulation.RUNSTAT_OFF;
                    }
                    //else if (vcrControl != null && vcrControl.IsActive() == true)
                    //{
                    //    vcrControl.Play();
                    //    if (vcrControl.IsActive() == false)
                    //    {
                    //        // switch from pause to the play button
                    //        VCRbutton_Play.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new GUIDelegateNoArgs(VCRUpdate));
                    //    }
                    //}
                }
            }
        }

        // gui update delegate; needed because we can't access the gui elements directly; they are part of a different thread
        private delegate void GUIDelegateNoArgs();
        private delegate void GUIDelegateTwoArgs(int iArg, bool bArg);

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
        private void GUIUpdate(int expID, bool force)
        {
            //if (skipDataWriteMenu.IsChecked == false && OpenVCR(true, expID) == true)
            //{
            //    VCR_Toolbar.IsEnabled = true;
            //    VCR_Toolbar.DataContext = vcrControl;
            //    VCRslider.Maximum = vcrControl.TotalFrames() - 1;
            //}

            // only allow fitting and other analysis that needs the database if database writing is on
            if (force || skipDataWriteMenu.IsChecked == false && sim.RunStatus == Simulation.RUNSTAT_FINISHED)
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
                gc.EnablePickingButtons();
            }

            //sim.RunStatus = Simulation.RUNSTAT_OFF;
            resetButton.IsEnabled = true;
            resetButton.Content = "Apply";
            runButton.Content = "Run";
            statusBarMessagePanel.Content = "Ready";
            //runButton.IsEnabled = true;
            enableFileMenu(true);
            optionsMenu.IsEnabled = true;
            // TODO: Should probably combine these...
            gc.ToolsToolbar_IsEnabled = true;
            SolfacRenderingCB.IsEnabled = true;
            // NOTE: Uncomment this to open the Sim Config ToolWindow after a run has completed
            this.SimConfigToolWindow.Activate();
            this.menu_ActivateSimSetup.IsEnabled = true;
            SetControlFlag(MainWindow.CONTROL_NEW_RUN, true);
            // TODO: These Focus calls will be a problem with multiple GCs...
            gc.Rwc.Focus();
        }

        private void applyChanges()
        {
            // check if there were changes
            if (configurator.SerializeSimConfigToStringSkipDeco() != orig_content)
            {
                MessageBoxResult result = saveDialog();

                // apply changes, save if needed
                if (result == MessageBoxResult.Yes)
                {
                    // save into the same file
                    configurator.SerializeSimConfigToFile();
                    orig_content = configurator.SerializeSimConfigToStringSkipDeco();
                    orig_path = System.IO.Path.GetDirectoryName(scenario_path.LocalPath);
                }
                else if (result == MessageBoxResult.No)
                {
                    // allow saving with a different name
                    saveScenarioUsingDialog();
                }
                else
                {
                    // reload the file; also resets the gui, discards changes
                    loadScenarioFromFile(scenario_path.LocalPath);
                    return;
                }
            }

            lockAndResetSim(false, "");
            //also need to delete every for this experiment in database.
            //DataBaseTools.DeleteExperiment(configurator.SimConfig.experiment_db_id);
            //SC.SimConfig.experiment_db_id = -1;//reset
            runButton.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.SystemIdle, new GUIDelegateTwoArgs(GUIUpdate), -1, false);

            //If main VTK window is not open, open it. Close the CellInfo tab.
            this.VTKDisplayDocWindow.Open();
            this.ToolWinCellInfo.Close();
        }

        /// <summary>
        /// Initiate a simulation run
        /// This needs to work for any scenario, i.e., it needs to work in the general case and not just for a specific scenario.
        /// MAKE SURE TO DO WHAT IT SAYS IN PREVIOUS LINE!!!!
        /// </summary>
        private void runSim()
        {
            VTKDisplayDocWindow.Activate();
            //MessageBox.Show("In runSim()");

            //CALL THIS FOR TESTING - WRITES OUT CONC VALUES FOR EACH STEP
            //YOU CAN ONLY CALL THIS AFTER LOADING A DRIVER-LOCOMOTOR SCENARIO
            //TestStepperLocomotion(nSteps, dt);

            //YOU CAN ONLY CALL THIS AFTER LOADING A LIGAND-RECEPTOR SCENARIO
            //TestStepperLigandReceptor(nSteps, 0.01);

            //sim.refreshDatabaseBufferRows();
            if (sim.RunStatus == Simulation.RUNSTAT_RUN)
            {
                //resetButton.IsEnabled = true;
                resetButton.Content = "Abort";
                sim.RunStatus = Simulation.RUNSTAT_PAUSE;
                runButton.Content = "Continue";
                statusBarMessagePanel.Content = "Paused...";
                runButton.ToolTip = "Continue the Simulation.";
            }
            else if (sim.RunStatus == Simulation.RUNSTAT_PAUSE)
            {
                //resetButton.IsEnabled = false;
                resetButton.Content = "Abort";
                sim.RunStatus = Simulation.RUNSTAT_RUN;
                runButton.Content = "Pause";
                statusBarMessagePanel.Content = "Running...";
            }
            else
            {
                if (vcrControl != null)
                {
                    vcrControl.SetInactive();
                }
                if (sim.RunStatus == Simulation.RUNSTAT_FINISHED)
                {
                    sim.RunStatus = Simulation.RUNSTAT_OFF;
                }
                if (sim.RunStatus == Simulation.RUNSTAT_OFF && Properties.Settings.Default.skipDataBaseWrites == false)
                {
                    reporter.StartReporter(configurator.SimConfig);
                }

                // only check for unique names if database writing is on and the unique names option is on
                /*if (skipDataWriteMenu.IsChecked == false && uniqueNamesMenu.IsChecked == true)
                {
                    string nameTemplate = "Enter a unique name here...";

                    // if database writing is on, notify the user that it may be a good idea to have unique experiment names
                    // don't consider the template name a good idea
                    if (configurator.SimConfig.experiment_name == nameTemplate || checkExpNameUniqueness(configurator.SimConfig.experiment_name) == false)
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
                                SimConfigToolWindow.Activate();
                                // switch to the panel that has the name, give the name box the focus and change
                                // its content to something indicating what the user should do
                                SimConfigToolWindow.SelectSimSetupInGUISetExpName(nameTemplate);
                                return;
                            case MessageBoxResult.No:
                                break;
                            default:
                                return;
                        }
                    }
                }*/

                if (configurator.SerializeSimConfigToStringSkipDeco() == orig_content)
                {
                    // initiating a run starts always at repetition 1
                    repetition = 1;
                    // call with false (lockSaveStartSim(false)) or modify otherwise to enable the simulation to continue from the last visible state
                    // after a run or vcr playback
                    lockSaveStartSim(MainWindow.CheckControlFlag(MainWindow.CONTROL_FORCE_RESET));
                }
                else
                {
                    // Display message box
                    MessageBoxResult result = saveDialog();

                    // Process message box results
                    switch (result)
                    {
                        case MessageBoxResult.Yes:
                            configurator.SerializeSimConfigToFile();
                            orig_content = configurator.SerializeSimConfigToStringSkipDeco();
                            orig_path = System.IO.Path.GetDirectoryName(scenario_path.LocalPath);
                            // initiating a run starts always at repetition 1
                            repetition = 1;
                            lockSaveStartSim(true);
                            break;
                        case MessageBoxResult.No:
                            if (saveScenarioUsingDialog() == true)
                            {
                                // initiating a run starts always at repetition 1
                                repetition = 1;
                                lockSaveStartSim(true);
                            }
                            break;
                        case MessageBoxResult.Cancel:
                            // Do nothing...
                            break;
                    }
                }
                if (sim.RunStatus == Simulation.RUNSTAT_RUN)
                {
                    runButton.Content = "Pause";
                    runButton.ToolTip = "Pause the Simulation.";
                    statusBarMessagePanel.Content = "Running...";
                }
            }
        }

        private MessageBoxResult saveDialog()
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
            Cell selectedCell = Simulation.dataBasket.Cells[cellID];
            List<CellMolecularInfo> currConcs = new List<CellMolecularInfo>();

            labelMolConcs.Content = labelMolConcs.Content + cellID.ToString();

            //need the ecm probe concentrations for this purpose
            foreach (ConfigMolecularPopulation mp in MainWindow.SC.SimConfig.scenario.environment.ecs.molpops)
            {
                string name = MainWindow.SC.SimConfig.entity_repository.molecules_dict[mp.molecule_guid_ref].Name;
                double conc = Simulation.dataBasket.ECS.Space.Populations[mp.molecule_guid_ref].Conc.Value(selectedCell.State.X);
                CellMolecularInfo cmi = new CellMolecularInfo();
                cmi.Molecule = "ECM: " + name;
                cmi.Concentration = conc.ToString("#.000");
                currConcs.Add(cmi);
            }
            
            EntityRepository er = MainWindow.SC.SimConfig.entity_repository;
            foreach (KeyValuePair<string, MolecularPopulation> kvp in Simulation.dataBasket.Cells[selectedCell.Cell_id].PlasmaMembrane.Populations)
            {
                string mol_name = er.molecules_dict[kvp.Key].Name;
                double conc = Simulation.dataBasket.Cells[selectedCell.Cell_id].PlasmaMembrane.Populations[kvp.Key].Conc.Value(new double[] { 0.0, 0.0, 0.0 });
                CellMolecularInfo cmi = new CellMolecularInfo();
                cmi.Molecule = "Cell: " + mol_name;
                cmi.Concentration = conc.ToString("#.000");
                currConcs.Add(cmi);
            }
            foreach (KeyValuePair<string, MolecularPopulation> kvp in Simulation.dataBasket.Cells[selectedCell.Cell_id].Cytosol.Populations)
            {
                string mol_name = er.molecules_dict[kvp.Key].Name;
                //double conc = Simulation.dataBasket.Cells[selectedCell.Cell_id].Cytosol.Populations[kvp.Key].Conc.Value(selectedCell.State.X);
                double conc = Simulation.dataBasket.Cells[selectedCell.Cell_id].Cytosol.Populations[kvp.Key].Conc.Value(new double[] { 0.0, 0.0, 0.0 });
                CellMolecularInfo cmi = new CellMolecularInfo();
                cmi.Molecule = "Cell: " + mol_name;
                cmi.Concentration = conc.ToString("#.000");
                currConcs.Add(cmi);
            }

            dgCellMolConcs.ItemsSource = currConcs;
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
                MainWindow.ST_ReacComplexChartWindow.ClearChart();
                VTKDisplayDocWindow.Activate();
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
            // terminate the simulation thread first
            if (simThread != null && simThread.IsAlive)
            {
                simThread.Abort();
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
        }

        private void exitApp_Click(object sender, RoutedEventArgs e)
        {
            CloseApp();
        }

        private void CellSelectionToolCB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;

            if (cb.SelectedIndex < 0)
                return;

            byte index = (byte)(cb.SelectedIndex);

            SetMouseLeftState(index, true);

            if (index == MOUSE_LEFT_NONE)
                gc.HandToolButton_IsEnabled = false;
            else
                gc.HandToolButton_IsEnabled = true;
            
        }
    }
}
