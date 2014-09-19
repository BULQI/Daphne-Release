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
using System.IO;
using System.Diagnostics;
using System.Xml;
using System.Collections.ObjectModel;

using Daphne;
using ManifoldRing;
using Newtonsoft;
using Ninject;
using Ninject.Parameters;

namespace DaphneOptim
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

        /// <summary>
        /// Path of the executable file in installation folder
        /// </summary>
        public string execPath = string.Empty;

        private static Simulation sim;
        public static Simulation Sim
        {
            get { return sim; }
            set { sim = value; }
        }

        private Reporter reporter;
        
        /// <summary>
        /// uri for the scenario file
        /// </summary>
        public static Uri protocol_path;
        private string orig_content, orig_path;
        private bool tempFileContent = false, postConstruction = false;
        private static bool argDev = false, argBatch = false, argSave = false;
        private string argScenarioFile = "";

        /// <summary>
        /// scenario load flag, success == true
        /// </summary>
        public static bool loadSuccess;

        private static SystemOfPersistence sop = null;
        /// <summary>
        /// retrieve a pointer to the configurator
        /// </summary>
        public static SystemOfPersistence SOP
        {
            get { return sop; }
        }
      
        
        public MainWindow()
        {
            InitializeComponent();

            // Point to DaphneGui
            //appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().CodeBase);
            appPath = @"C:\Users\gmkepler\Documents\Visual Studio 2010\Projects\BU-TFS\Daphne\Daphne-grace\DaphneGui\bin\x64\Debug\";

            // the scenario to optimize
            string file = "daphne_driver_locomotion_scenario.json";

            bool file_exists;

            protocol_path = new Uri(appPath + @"\Config\" + file);
            orig_path = System.IO.Path.GetDirectoryName(protocol_path.LocalPath);

            file_exists = File.Exists(protocol_path.LocalPath);
            if (!file_exists)
            {
                file = "daphne_blank_scenario.json";
            }

            // create the simulation
            sim = new Simulation();

            // reporter
            reporter = new Reporter();
            reporter.AppPath = orig_path + @"\";

            if (file_exists)
            {
                sop = new SystemOfPersistence();
                initialState_Abridged(true, true, ReadJson(""));
            }

            //// create the simulation thread
            //simThread = new Thread(new ThreadStart(run)) { IsBackground = true };
            ////simThread.Priority = ThreadPriority.Normal;
            //simThread.Start();

            postConstruction = true;

            // modify scenario
            sop.Protocol.scenario.time_config.duration = 1.0;

            // save scenario
            sop.Protocol.SerializeToFile(false);

            // run scenario
            string processArgs = @"-d -b -f:daphne_driver_locomotion_scenario.json";
            string processName = @"C:\Users\gmkepler\Documents\Visual Studio 2010\Projects\BU-TFS\Daphne\Daphne-grace\DaphneGui\bin\x64\Debug\DaphneGui.exe";
            ProcessStartInfo info = new ProcessStartInfo(processName, processArgs);
            Process process = new Process();
            process.StartInfo = info;
            process.StartInfo.ErrorDialog = true;
            //process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();

            // analyze results


            Console.WriteLine("Process ended.");
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Hello World?");

            Console.WriteLine("Goodbye cruel world!");

        }

        private void initialState_Abridged(bool newFile, bool completeReset, Protocol protocol)
        {
            // if we read a new file we may have to disconnect event handlers if they were connected previously;
            // we always must deserialize the file
            if (newFile == true)
            {
                if (protocol != null)
                {
                    //sop = new SystemOfPersistence();
                    sop.Protocol = protocol;
                    orig_content = sop.Protocol.SerializeToStringSkipDeco();
                    orig_path = System.IO.Path.GetDirectoryName(protocol_path.LocalPath);
               }
            }

            // reporter file name
            reporter.FileName = sop.Protocol.reporter_file_name;

            // temporary solution to avoid popup resaving states -axin
            if (true)
            {
                orig_content = sop.Protocol.SerializeToStringSkipDeco();
            }

            loadSuccess = true;
        }

        /// <summary>
        /// utility to query whether we are running from within the IDE or want to make the program think we do; useful for profiling and 
        /// running without debugging from within the IDE
        /// </summary>
        /// <returns>true for running inside the IDE</returns>
        public static bool AssumeIDE_copied()
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

        /// <summary>
        /// handle a problem during loading: blank the vtk screen and bulk of the gui
        /// </summary>
        /// <param name="s">message to display</param>
        private void handleLoadFailure_Abridged(string s)
        {
            loadSuccess = false;
            sop.Protocol = new Protocol();
            sop.Protocol.experiment_name = "";
            sop.Protocol.experiment_description = "";
            orig_content = sop.Protocol.SerializeToStringSkipDeco();
            orig_path = System.IO.Path.GetDirectoryName(protocol_path.LocalPath);
            //showExceptionBox(s);
            MessageBox.Show(s, "Application error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        /// <summary>
        /// display an exception message
        /// </summary>
        /// <param name="e">the exception</param>
        private string exceptionMessage_copied(Exception e)
        {
            string msg = e.Message;

            // output the stack for developers
            if (AssumeIDE_copied() == true)
            {
                msg += "\n\n" + e.StackTrace;
            }
            return msg;
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
                    handleLoadFailure_Abridged("That configuration has problems. Please select another experiment.");
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
                    handleLoadFailure_Abridged("There is a problem loading the protocol file.\nPress OK, then try to load another.");
                    return null;
                }
            }
        }

    }

}
