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
using TBKMath;

using System.ComponentModel;
using System.Runtime.InteropServices; 
//#define NLOPT_DLL

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

        // parameters for batch run
        string processName;
        string processArgs;

        long seed = 0;
        public struct nlopt_opt
        {
            public double my_param;
            public nlopt_opt(double _param)
            {
                my_param = _param;
            }
        };
        struct foo {
            int n; /* dimension */
            int L; /* size of each rectangle (2n+3) */
            double magic_eps; /* Jones' epsilon parameter (1e-4 is recommended) */
            int which_diam; /* which measure of hyper-rectangle diam to use:
			            0 = Jones, 1 = Gablonsky */
            int which_div; /* which way to divide rects:
            0: orig. Jones (divide all longest sides)
            1: Gablonsky (cubes divide all, rects longest)
            2: Jones Encyc. Opt.: pick random longest side */
            int which_opt; /* which rects are considered "potentially optimal"
            0: Jones (all pts on cvx hull, even equal pts)
            1: Gablonsky DIRECT-L (pick one pt, if equal pts)
            2: ~ 1, but pick points randomly if equal pts 
            ... 2 seems to suck compared to just picking oldest pt */
  
            double lb, ub; // const pointers?
            //nlopt_stopping *stop; /* stopping criteria */
            nlopt_func f; void *f_data;
            double *work; /* workspace, of length >= 2*n */
            int *iwork; /* workspace, length >= n */
            double minf, *xmin; /* minimum so far */
     
            /* red-black tree of hyperrects, sorted by (d,f,age) in
            lexographical order */
            rb_tree rtree;
            int age; /* age for next new rect */
            double **hull; /* array to store convex hull */
            int hull_len; /* allocated length of hull array */
} 

        enum nlopt_result
        {
            NLOPT_FAILURE = -1, /* generic failure code */
            NLOPT_INVALID_ARGS = -2,
            NLOPT_OUT_OF_MEMORY = -3,
            NLOPT_ROUNDOFF_LIMITED = -4,
            NLOPT_FORCED_STOP = -5,
            NLOPT_SUCCESS = 1, /* generic success code */
            NLOPT_STOPVAL_REACHED = 2,
            NLOPT_FTOL_REACHED = 3,
            NLOPT_XTOL_REACHED = 4,
            NLOPT_MAXEVAL_REACHED = 5,
            NLOPT_MAXTIME_REACHED = 6
        } ;

        double nlopt_func(unsigned n, const double x, ref double gradient, ref void *func_data);

        //[DllImport(@"C:\Users\gmkepler\Documents\Visual Studio 2010\Projects\BU-TFS\Daphne\Daphne-grace\DaphneOptim\bin\x64\Debug\libnolpt-0.dll", EntryPoint = "nlopt_srand")]
        [DllImport("libnlopt-0.dll", EntryPoint = "nlopt_srand")]
        static extern void nlopt_srand(long seed);
        [DllImport("libnlopt-0.dll", EntryPoint = "nlopt_srand")]
        static extern nlopt_opt nlopt_create(int algorithm, int n);
        [DllImport("libnlopt-0.dll", EntryPoint = "nlopt_srand")]
        static extern nlopt_result nlopt_optimize(nlopt_opt opt, ref double x, ref double opt_f);

        public MainWindow()
        {
            InitializeComponent();

            //nlopt_srand(seed);
            nlopt_opt opt = new nlopt_opt(1);
            opt = nlopt_create(0, 1);

            nlopt_result opt_result = nlopt_result.NLOPT_FAILURE;
            double x = 0.0;
            double opt_f = 0.0;
            opt_result = nlopt_optimize(opt, ref x, ref opt_f);

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

            processArgs = @"-d -b -f:" + file;
            processName = @"C:\Users\gmkepler\Documents\Visual Studio 2010\Projects\BU-TFS\Daphne\Daphne-grace\DaphneGui\bin\x64\Debug\DaphneGui.exe";

            //// analyze results


            Console.WriteLine("Process ended.");
        }

        private void Run_Click(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Hello World?");

            // user defines parameters
            List<double> paramVal = new List<double>();
            paramVal.Add(1.0);

            // analyze results
            List<double> scale = new List<double>();
            List<bool> fixt = new List<bool>();
            Annealer annealer = new Annealer();
            annealer.Initialize(new Annealer.ObjectiveFunctionDelegate(CostFunction), 1, scale, fixt);
            

            Console.WriteLine("Goodbye cruel world!");

        }

        private void Run()
        {
            ProcessStartInfo info = new ProcessStartInfo(processName, processArgs);
            Process process = new Process();
            process.StartInfo = info;
            process.StartInfo.ErrorDialog = true;
            //process.StartInfo.CreateNoWindow = true;
            process.Start();
            process.WaitForExit();
        }

        //public delegate double CostFunction(List<double> theta);
        public double CostFunction(ref List<double> paramVal)
        {
            double cost = 0;

            // modify parameters in scenario file
            ModifyScenario(paramVal);

            // run scenario with new parameters
            Run();

            // evaluate cost


            return cost;
        }

        private void ModifyScenario(List<double> paramVal)
        {
            // user supply code to update the scenario with new parameter values
            sop.Protocol.scenario.time_config.duration = paramVal[0];

            // save scenario
            sop.Protocol.SerializeToFile(false);

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
