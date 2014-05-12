using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.IO;
using System.Diagnostics;
using Ninject;
using Ninject.Parameters;
using ManifoldRing;
using System.Collections.Specialized;
using Newtonsoft.Json;

namespace Daphne
{
    public class ReactionComplexProcessor : EntityModelBase
    {
        public Simulation Sim { get; set; }
        public SimConfiguration SC { get; set; }
        public ConfigReactionComplex CRC { get; set; }

        public int nSteps { get; set; }
        public double dt { get; set; }

        //These are for graphing the concentrations.  This data is generated during the Go method.
        public int MaxTime { get; set; }
        public double dMaxTime { get; set; }
        public double dInitialTime { get; set; }

        public int nTestVariable { get; set; }

        protected List<double> listTimes = new List<double>();
        public List<double> ListTimes
        {
            get
            {
                return listTimes;
            }
            set
            {
                listTimes = value;
            }
        }

        //This dict is used by chart view to plot the points and draw the graph
        protected Dictionary<string, List<double>> dictGraphConcs = new Dictionary<string, List<double>>();
        public Dictionary<string, List<double>> DictGraphConcs
        {
            get
            {
                return dictGraphConcs;
            }
            set
            {
                dictGraphConcs = value;
            }
        }

        protected Dictionary<string, double> dictOriginalConcs = new Dictionary<string, double>();
        protected Dictionary<string, double> dictInitialConcs = new Dictionary<string, double>();
        
        //for wpf binding
        private ObservableCollection<MolConcInfo> _initConcs;
        public ObservableCollection<MolConcInfo> initConcs 
        { 
            get
            {
                return _initConcs;
            }
            set
            {
                _initConcs = value;
            } 
        }

        public Dictionary<string, MolConcInfo> initConcsDict; 

        private ObservableCollection<ConfigReaction> reacs = new ObservableCollection<ConfigReaction>();
        public ObservableCollection<ConfigReaction> ReactionsInComplex
        {
            get
            {
                return reacs;
            }
            set
            {
                reacs = value;
            }
        }

        public ReactionComplexProcessor() : base()
        {
            nTestVariable = 199;
            initConcs = new ObservableCollection<MolConcInfo>();
            initConcsDict = new Dictionary<string, MolConcInfo>();

            initConcs.CollectionChanged += new NotifyCollectionChangedEventHandler(initConcs_CollectionChanged);
        }

        private void initConcs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                foreach (var nn in e.NewItems)
                {
                }
            }
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

            OnPropertyChanged("initConcs");
        }

        public void Initialize(SimConfiguration mainSC, ConfigReactionComplex crc, Simulation sim )
        {
            Sim = sim;
            SC = mainSC;
            CRC = crc;

            double minVal = 1e7;

            //foreach (string guid in crc.reactions_guid_ref)
            //{
            //    ConfigReaction cr = mainSC.entity_repository.reactions_dict[guid];
            //    minVal = Math.Min(minVal, cr.rate_const);
            //}

            foreach (ConfigReactionGuidRatePair grp in crc.ReactionRates)
            {
                minVal = Math.Min(minVal, grp.ReactionComplexRate);
            }

            dInitialTime = 5 / minVal;
            dMaxTime = 2 * dInitialTime;
            MaxTime = (int)dMaxTime;

            //dInitialTime = 3.33;

            SaveOriginalConcs();
            SaveInitialConcs();
            SaveReactions(crc);
            

        }

        //*************************************************************************
        //This runs the simulation and calculates the concentrations with each step
        public void Go()
        {
            dictGraphConcs.Clear();
            listTimes.Clear();

            Compartment comp = Simulation.dataBasket.Cells[0].Cytosol;

            foreach (KeyValuePair<string, MolecularPopulation> kvp in comp.Populations)
            {
                string molguid = kvp.Key;
                List<double> concList = new List<double>();
                dictGraphConcs.Add(molguid, concList);
            }

            double[] initArray = new double[1];

            foreach (KeyValuePair<string, MolecularPopulation> kvp in comp.Populations)
            {
                string molguid = kvp.Key;
                double conc = dictInitialConcs[molguid];

                initArray[0] = conc;

                ScalarField sf = SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", comp.Interior));
                sf.Initialize("const", initArray);
                comp.Populations[molguid].Conc *= 0;
                comp.Populations[molguid].Conc += sf;
                
                dictGraphConcs[molguid].Add(conc);
            }

            //Now do the steps
            dt = 1.0e-3;
            dt = 0.01;
            nSteps = (int)((double)dInitialTime / dt);
            //We will not show all points;  we will show every nth point.
            int interval = nSteps / 100;
            if (interval == 0)
                interval = 1;

            listTimes.Add(0);

            //string concString = "";
            TimeSpan total = new TimeSpan(0, 0, 0);
            double[] defaultLoc = { 0.0, 0.0, 0.0 };

            //string output;
            //string filename = "Config\\new_reaction_complex_output.txt";


            //using (StreamWriter writer = File.CreateText(filename))
            //{
                for (int i = 1; i < nSteps; i++)
                {
                    //Add to graph only if it is at an interval
                    bool AtInterval = (i % interval == 0);
                    if (AtInterval)
                        listTimes.Add(dt * i);


                    //Stopwatch sw = new Stopwatch();
                    //sw.Start();
                    Sim.Step(dt);    //**************************STEP**********************************
                    //sw.Stop();
                    //Console.WriteLine("Elapsed={0}", sw.Elapsed);
                    //total += sw.Elapsed;

                    //sw.Restart();
                   // double currtime = i * dt;
                    //output = currtime.ToString();

                    //Add to graph, only if it is at interval
                    if (AtInterval)
                    {
                        foreach (KeyValuePair<string, MolecularPopulation> kvp in comp.Populations)
                        {
                            string molguid = kvp.Key;
                            double conc = comp.Populations[molguid].Conc.Value(defaultLoc);
                            dictGraphConcs[molguid].Add(conc);
                            //output += "\t" + conc;
                        }
                        //writer.WriteLine(output);
                    }
                    //sw.Stop();
                    //Console.WriteLine("Elapsed={0}", sw.Elapsed);
                    //total += sw.Elapsed;

                }
            //}

            //At this point, the list of times is populated and so is the dictionary of concentrations by molecule
            //string path = @"c:\temp\concs.txt";            
            //File.AppendAllText(path, concString);    

            //MessageBox.Show("Finished processing reaction complex.");

        }


        //This method updates the conc of the given molecule
        public void EditConc(string moleculeKey, double conc)
        {
            dictInitialConcs[moleculeKey] = conc;
            //initConcsDict[moleculeKey].conc = conc;
            OnPropertyChanged("initConcs");
        }


        //Save the original concs in a temp array in case user wants to discard the changes
        public void SaveOriginalConcs()
        {
            dictOriginalConcs.Clear();

            if (Simulation.dataBasket.Cells.Count <= 0)
                return;
            
            Compartment comp = Simulation.dataBasket.Cells[0].Cytosol;

            if (comp == null)
                return;

            foreach (KeyValuePair<string, MolecularPopulation> kvp in comp.Populations)
            {
                string molguid = kvp.Key;
                double conc = comp.Populations[molguid].Conc.Value(new double[] { 0.0, 0.0, 0.0 });
                dictOriginalConcs[molguid] = conc;
            }
        }

        //Restores original concs
        //If user made changes by dragging initial concs and wants to discard the changes, do that here
        //by copying the original concs back to mol pops
        public void RestoreOriginalConcs()
        {
            foreach (KeyValuePair<string, double> kvp in dictOriginalConcs)
            {
                dictInitialConcs[kvp.Key] = kvp.Value;
            }
            OnPropertyChanged("initConcs");
        }

        public void OverwriteOriginalConcs()
        {
            if (Simulation.dataBasket.Cells.Count <= 0)
                return;

            Compartment comp = Simulation.dataBasket.Cells[0].Cytosol;
            if (comp == null)
                return;

            dictOriginalConcs.Clear();

            double[] initArray = new double[1];

            //Copy current (may have changed) initial concs to Originals dict
            foreach (KeyValuePair<string, double> kvp in dictInitialConcs)
            {
                dictOriginalConcs[kvp.Key] = kvp.Value;

                //Now overwrite the concs in SimConfiguration
                ConfigMolecularPopulation mol_pop = (ConfigMolecularPopulation)(CRC.molpops[0]);
                MolPopHomogeneousLevel homo = (MolPopHomogeneousLevel)mol_pop.mpInfo.mp_distribution;
                homo.concentration = kvp.Value;
                
                //initArray[0] = kvp.Value;
                //ScalarField sf = SimulationModule.kernel.Get<ScalarField>(new ConstructorArgument("m", comp.Interior));
                //sf.Initialize("const", initArray);
                //comp.Populations[kvp.Key].Conc *= 0;
                //comp.Populations[kvp.Key].Conc += sf;
            }           
        }

        //Save the initial concs. If user drags graph, use dictInitialConcs to update the initial concs
        public void SaveInitialConcs()
        {
            dictInitialConcs.Clear();
            initConcs.Clear();
            initConcsDict.Clear();

            if (Simulation.dataBasket.Cells.Count <= 0)
                return;

            Compartment comp = Simulation.dataBasket.Cells[0].Cytosol;
            if (comp == null)
                return;

            foreach (KeyValuePair<string, MolecularPopulation> kvp in comp.Populations)
            {
                string molguid = kvp.Key;
                //double conc = 0.0;
                double conc = comp.Populations[molguid].Conc.Value(new double[] { 0.0, 0.0, 0.0 });
                dictInitialConcs[molguid] = conc;

                MolConcInfo mci = new MolConcInfo(molguid, conc, SC);
                initConcs.Add(mci);
                initConcsDict.Add(molguid, mci);

            }
        }

        public void SaveReactions(ConfigReactionComplex crc)
        {
            foreach (string rguid in crc.reactions_guid_ref) 
            {
                ReactionsInComplex.Add(SC.entity_repository.reactions_dict[rguid]);
            }

            //foreach (string rguid in crc.reactions_guid_ref)
            //{
            //    ConfigReaction r = new ConfigReaction();
            //    r = SC.entity_repository.reactions_dict[rguid];
            //    ReactionsInComplex.Add(r);
            //}

            //var Settings = new JsonSerializerSettings();
            //Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            //Settings.TypeNameHandling = TypeNameHandling.Auto;
            //string jsonSpec = JsonConvert.SerializeObject(, Newtonsoft.Json.Formatting.Indented, Settings);
            //ConfigReactionComplex newcrc = JsonConvert.DeserializeObject<ConfigReactionComplex>(jsonSpec, Settings);
        }        

        public void UpdateRateConstants()
        {
            foreach (ConfigReactionGuidRatePair grp in CRC.ReactionRates)
            {
                string guid = grp.Guid;
                ConfigReaction cr = SC.entity_repository.reactions_dict[guid];
                cr.rate_const = grp.ReactionComplexRate2.Value;
            }

            //Compartment comp = Simulation.dataBasket.Cells[0].Cytosol;

            //comp.Reactions.Clear();

            //foreach (ConfigReaction reac in ReactionsInComplex)
            //{
            //    double rate = reac.rate_const;                
            //    //NEED TO UPDATE WITH THIS VALUE SOMEHOW

            //    Reaction r = new Reaction();

            //    //comp.Reactions.Add(
            //}
        }

        public void RestoreOriginalRateConstants()
        {
            foreach (ConfigReactionGuidRatePair grp in CRC.ReactionRates)
            {
                string guid = grp.Guid;
                ConfigReaction cr = SC.entity_repository.reactions_dict[guid];
                ////////cr.rate_const = grp.OriginalRate;
                ////////grp.ReactionComplexRate = grp.OriginalRate;
                cr.rate_const = grp.OriginalRate2.Value;
                grp.ReactionComplexRate2.Value = grp.OriginalRate2.Value;
            }            
        }

        
        
    }

    public class MolConcInfo
    {
        public double conc { get; set; }
        public string molguid { get; set; }
        public string molname { get; set; }

        public MolConcInfo()
        {
        }

        public MolConcInfo(string guid, double c, SimConfiguration sc)
        {
            molguid = guid;
            conc = c;
            molname = sc.entity_repository.molecules_dict[guid].Name ;
        }
    }
}
