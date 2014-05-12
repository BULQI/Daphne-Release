using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Daphne;
using System.IO;
using System.Collections.ObjectModel;
using System.Diagnostics;
using ManifoldRing;

namespace DaphneGui
{
    public class ReactionComplex : Compartment
    {
        public string Name { get; set; }
        public int nSteps { get; set; }
        public double dt { get; set; }

        //These are for graphing the concentrations.  This data is generated during the Go method.
        public int MaxTime { get; set; }
        public double dMaxTime { get; set; }
        public double dInitialTime { get; set; }        

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

        private ObservableCollection<GuiReactionTemplate> reacs = new ObservableCollection<GuiReactionTemplate>();
        public ObservableCollection<GuiReactionTemplate> ReactionsInComplex
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

        public List<GuiReactionTemplate> guiRtList;
        public List<GuiReactionTemplate> GRTList
        {
            get
            {
                return guiRtList;
            }
            set
            {
                guiRtList = value;
            }
        }

        public ReactionComplex(string name, Manifold interior) : base(interior)
        {
            Name = name;
            MaxTime = 1;
            dMaxTime = 1;
        }

        public void Initialize()
        {
            double minVal = 1e7;
            //Reactions.Clear();
            foreach (GuiReactionTemplate rt in ReactionsInComplex)
            {
                minVal = Math.Min(minVal, rt.RateConst);                
            }

            dInitialTime = 5 / minVal;            
            dMaxTime = 2 * dInitialTime;
            MaxTime = (int)dMaxTime;
            
        }

        public void LoadMolecules(Dictionary<string, Molecule> MolDict)
        {
            List<string> molNames2 = new List<string>();
            foreach (GuiReactionTemplate rt in GRTList)
            {
                foreach (SpeciesReference sr in rt.listOfReactants)
                {
                    if (!molNames2.Contains(sr.species))
                        molNames2.Add(sr.species);
                }

                foreach (SpeciesReference sr in rt.listOfProducts)
                {
                    if (!molNames2.Contains(sr.species))
                        molNames2.Add(sr.species);
                }

                foreach (SpeciesReference sr in rt.listOfModifiers)
                {
                    if (!molNames2.Contains(sr.species))
                        molNames2.Add(sr.species);
                }
            }

            //Here add molecular population to this compartment
            foreach (string s in molNames2)
            {
                if (MolDict.ContainsKey(s))
                {
                    double initialConc = 2.0;                    
                    AddMolecularPopulation(MolDict[s], initialConc);
                }
            }

            //Now add the actual reactions using ReactionBuilder class
            foreach (GuiReactionTemplate grt in GRTList)
            {
                ReactionTemplate rt = new ReactionTemplate();
                grt.CopyTo(rt);
                ReactionBuilder.ReactionSwitch(this, rt);
            }

        }

        //This runs the simulation and calculates the concentrations with each step
        public void Go()
        {
            dictGraphConcs.Clear();
            listTimes.Clear();
            //UpdateRateConstants();

            //First set up the dictionary of list of doubles with apporpriate number of entries (how many mol pops are in this rc)
            //int nCount = Populations.Count;
            //for (int j = 0; j < nCount; j++)
            //{
            //    string molname = Populations[j].Name;
            //    List<double> concList = new List<double>();
            //    dictGraphConcs.Add(molname, concList);
            //}

            foreach (KeyValuePair<string, MolecularPopulation> kvp in Populations)
            {
                string molname = kvp.Key;
                List<double> concList = new List<double>();
                dictGraphConcs.Add(molname, concList);
            }

            //Add initial concs to dict for graphing
            //int count = Populations.Count;
            //for (int j = 0; j < count; j++)
            //{
            //    string molname = Populations[j].Name;
            //    //double conc = Populations[j].Conc.array[0];                
            //    double conc = dictInitialConcs[molname];
            //    Populations[j].Conc.array[0] = conc;    //set the mol pop conc from the dictInitialConcs dictionary since user may have dragged
            //    dictGraphConcs[molname].Add(conc);
            //}
            foreach (KeyValuePair<string, MolecularPopulation> kvp in Populations)
            {
                string molname = kvp.Key;
                double conc = dictInitialConcs[molname];
                //CONC PROBLEM
                ScalarField s = new ScalarField(Interior, new ConstFieldInitializer(conc));
                Populations[molname].Conc = s;
                //////////Populations[molname].Conc.Value = conc;  //set the mol pop conc from the dictInitialConcs dictionary since user may have dragged
                dictGraphConcs[molname].Add(conc);
            }

            //Now do the steps
            
            dt = 1.0e-3;            
            nSteps = (int)((double)dInitialTime / dt);
            
            //We will not show all points;  we will show every nth point.
            int interval = nSteps / 100;
            if (interval == 0)
                interval = 1;

            listTimes.Add(0);

            //string concString = "";
            TimeSpan total = new TimeSpan(0, 0, 0);
            for (int i = 1; i < nSteps; i++)
            {
                //Add to graph only if it is at an interval
                bool AtInterval = (i % interval == 0);
                if (AtInterval)
                    listTimes.Add(dt * i);


                Stopwatch sw = new Stopwatch();
                sw.Start();
                Step(dt);
                sw.Stop();
                //Console.WriteLine("Elapsed={0}", sw.Elapsed);
                total += sw.Elapsed;
                //Add to graph only if it is at interval
                sw.Restart();
                if (AtInterval)
                {
                    foreach (KeyValuePair<string, MolecularPopulation> kvp in Populations)
                    {                        
                        string molname = kvp.Key;
                        //double conc = 0.0;

                        //CONC PROBLEM
                        //////////double conc = Populations[molname].Conc.ConcArray[0];
                        double conc = Populations[molname].Conc.Value(new double[] { 0.0, 0.0, 0.0 });

                        //FUDGE FACTOR for testing - adds random amounts to conc
                        //double conc2 = conc * 0.1;
                        //double ran = RandomNumber(0.0, conc2);
                        //conc = conc + ran;
                        //  

                        dictGraphConcs[molname].Add(conc);                        
                    }
                }
                sw.Stop();
                //Console.WriteLine("Elapsed={0}", sw.Elapsed);
                total += sw.Elapsed;
            }   
         
            //At this point, the list of times is populated and so is the dictionary of concentrations by molecule
            //string path = @"c:\temp\concs.txt";            
            //File.AppendAllText(path, concString);            
        }

        ////Function to get random number
        //private static readonly Random random = new Random();
        //private static readonly object syncLock = new object();
        //public static double RandomNumber(double minimum, double maximum)
        //{
        //    lock (syncLock)
        //    { // synchronize
        //        return random.NextDouble() * (maximum - minimum) + minimum;
        //    }
        //}


        //This method updates the conc of the given molecule
        public void EditConc(string mol, double conc)
        {
            //MolecularPopulation mp = molpopDict[mol];
            MolecularPopulation mp = Populations[mol];
            //CONC PROBLEM
            ScalarField s = new ScalarField(Interior, new ConstFieldInitializer(conc));
            mp.Conc = s;
            //////////mp.Conc.ConcArray[0] = conc;
            dictInitialConcs[mol] = conc;
        }


        //Save the initial concs in a temp array in case user wants to discard the changes
        public void SaveOriginalConcs()
        {
            dictOriginalConcs.Clear();            
            foreach (KeyValuePair<string, MolecularPopulation> kvp in Populations)
            {
                string molname = kvp.Key;
                //double conc = 0.0;
                //CONC PROBLEM
                ////////////double conc = Populations[molname].Conc.ConcArray[0];
                double conc = Populations[molname].Conc.Value(new double[] { 0.0, 0.0, 0.0 });
                dictOriginalConcs[molname] = conc;
            }
        }        

        //Restores original concs
        //If user made changes by dragging initial concs and wants to discard the changes, do that here
        //by copying the original concs back to mol pops
        public void RestoreOriginalConcs()
        {
            foreach (KeyValuePair<string, double> kvp in dictOriginalConcs)
            {
                //MolecularPopulation mp = molpopDict[kvp.Key];
                MolecularPopulation mp = Populations[kvp.Key];

                //CONC PROBLEM
                ScalarField s = new ScalarField(Interior, new ConstFieldInitializer(kvp.Value));
                mp.Conc = s;
                ////////////mp.Conc.ConcArray[0] = kvp.Value;
                dictInitialConcs[kvp.Key] = kvp.Value;
            }
        }

        //Save the initial concs. If user drags graph, use dictInitialConcs to update the initial concs
        public void SaveInitialConcs()
        {
            dictInitialConcs.Clear();            

            foreach (KeyValuePair<string, MolecularPopulation> kvp in Populations)
            {
                string molname = kvp.Key;
                //double conc = 0.0;
                double conc = Populations[molname].Conc.Value(new double[] { 0.0, 0.0, 0.0 });
                dictInitialConcs[molname] = conc;
            }
        }

        private void UpdateRateConstants()
        {
            foreach (GuiReactionTemplate rt in GRTList)
            {
                foreach (Reaction r in Reactions)
                {
                    r.RateConstant = rt.RateConst;
                }
            }
        }
    }
}
