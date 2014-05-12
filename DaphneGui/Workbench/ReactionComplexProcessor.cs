﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Daphne;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using Ninject;
using Ninject.Parameters;
using ManifoldRing;

namespace Workbench
{
    public class ReactionComplexProcessor //: SimConfiguration
    {
        Simulation Sim { get; set; }
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
        }

        public void Initialize(SimConfiguration mainSC, ConfigReactionComplex crc, Simulation sim )
        {
            double minVal = 1e7;

            foreach (string guid in crc.reactions_guid_ref)
            {
                ConfigReaction cr = mainSC.entity_repository.reactions_dict[guid];
                minVal = Math.Min(minVal, cr.rate_const);
            }

            dInitialTime = 5 / minVal;
            dMaxTime = 2 * dInitialTime;
            MaxTime = (int)dMaxTime;

            SaveOriginalConcs();
            SaveInitialConcs();

            Sim = sim;

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
        }


        //Save the initial concs in a temp array in case user wants to discard the changes
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
        }

        //Save the initial concs. If user drags graph, use dictInitialConcs to update the initial concs
        public void SaveInitialConcs()
        {
            dictInitialConcs.Clear();

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
            }
        }

        private void UpdateRateConstants()
        {
            //foreach (ConfigReaction rt in GRTList)
            //{
            //    foreach (Reaction r in Reactions)
            //    {
            //        r.RateConstant = rt.rate_const;
            //    }
            //}
        }
    }
}