/*
Copyright (C) 2019 Kepler Laboratory of Quantitative Immunology

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY 
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
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
        public TissueSimulation Sim { get; set; }
        public Protocol SC { get; set; }
        public ConfigReactionComplex CRC { get; set; }

        public int nSteps { get; set; }
        public double dt { get; set; }

        //These are for graphing the concentrations.  This data is generated during the Go method.
        public int MaxTime { get; set; }

        private double dmaxtime;
        public double dMaxTime 
        {
            get
            {
                return dmaxtime;
            }
            set
            {
                if (dmaxtime != value)
                {
                    dmaxtime = value;
                    OnPropertyChanged("dMaxTime");
                }
            }
        }

        private double dinittime;
        public double dInitialTime
        {
            get
            {
                return dinittime;
            }
            set
            {
                if (dinittime != value)
                {
                    dinittime = value;
                    OnPropertyChanged("dInitialTime");
                }
            }
        }

        public int nTestVariable { get; set; }

        //List of times that will be graphed on x-axis. There is only one times list no matter how many molecules
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

        //save the original concentrations
        protected Dictionary<string, double> dictOriginalConcs = new Dictionary<string, double>();

        //Initial concentrations - user can change initial concentrations of molecules
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

        //Convenience dictionary of initial concs and mol info
        public Dictionary<string, MolConcInfo> initConcsDict; 

        public ReactionComplexProcessor() : base()
        {
            nTestVariable = 199;
            initConcs = new ObservableCollection<MolConcInfo>();
            initConcsDict = new Dictionary<string, MolConcInfo>();

            initConcs.CollectionChanged += new NotifyCollectionChangedEventHandler(initConcs_CollectionChanged);
        }

        private void initConcs_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //if (e.Action == NotifyCollectionChangedAction.Add)
            //{
            //    foreach (var nn in e.NewItems)
            //    {
            //    }
            //}            
            OnPropertyChanged("initConcs");
        }

        public void Initialize(Protocol mainSC, ConfigReactionComplex crc, TissueSimulation sim)
        {
            Sim = sim;
            SC = mainSC;
            CRC = crc;

            double minVal = 1e7;
#if OLD_RC
            foreach (ConfigReactionGuidRatePair grp in crc.ReactionRates)
            {
                minVal = Math.Min(minVal, grp.ReactionComplexRate);
            }
#endif
            dInitialTime = 1 / minVal;
            dMaxTime = 2 * dInitialTime;
            MaxTime = (int)dMaxTime;

            SaveOriginalConcs();
            SaveInitialConcs();
        }

        public void Reinitialize()
        {
            Sim.Load(SC, true);

            double minVal = 1e7;
#if OLD_RC
            foreach (ConfigReactionGuidRatePair grp in CRC.ReactionRates)
            {
                minVal = Math.Min(minVal, grp.ReactionComplexRate);
            }
#endif
            dInitialTime = 1 / minVal;
            dMaxTime = 2 * dInitialTime;
            MaxTime = (int)dMaxTime;
        }

        private void SetTimeValues()
        {
            double minVal = 1e7;
#if OLD_RC
            foreach (ConfigReactionGuidRatePair grp in CRC.ReactionRates)
            {
                minVal = Math.Min(minVal, grp.ReactionComplexRate);
            }
#endif
            dInitialTime = 5 / minVal;
            dMaxTime = 2 * dInitialTime;
            MaxTime = (int)dMaxTime;
        }

        //*************************************************************************
        //This runs the simulation and calculates the concentrations with each step
        public void Go()
        {
            dictGraphConcs.Clear();
            listTimes.Clear();

            if (SimulationBase.dataBasket.Cells.Count == 0)
                return;
                
            Compartment comp = SimulationBase.dataBasket.Cells.First().Value.Cytosol;

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
            // nSteps = (double)dInitialTime / dt);
            nSteps = Math.Min((int)((double)dInitialTime / dt),10000);
            //We will not show all points;  we will show every nth point.

            if (nSteps <= 1)
                return;

            int interval = nSteps / 100;
            if (interval == 0)
                interval = 1;

            listTimes.Add(0);

            TimeSpan total = new TimeSpan(0, 0, 0);
            double[] defaultLoc = { 0.0, 0.0, 0.0 };

            for (int i = 1; i < nSteps; i++)
            {
                ////Add to graph only if it is at an interval
                //bool AtInterval = (i % interval == 0);
                //if (AtInterval)
                //{
                //    listTimes.Add(dt * i);
                //}
#if false       
                //stopwatch example code
                //Stopwatch sw = new Stopwatch();
                //sw.Start();
                //sw.Stop();
                //sw.Restart();
                //total += sw.Elapsed;  
#endif
                Sim.Step(dt);    //**************************STEP**********************************

                //Add to graph, only if it is at interval
                if (i % interval == 0)
                {
                    listTimes.Add(dt * i);
                    foreach (KeyValuePair<string, MolecularPopulation> kvp in comp.Populations)
                    {
                        //string molguid = kvp.Key;
                        //double conc = comp.Populations[molguid].Conc.Value(defaultLoc);
                        //dictGraphConcs[molguid].Add(conc);
                        dictGraphConcs[kvp.Key].Add(comp.Populations[kvp.Key].Conc.Value(defaultLoc));
                    }
                }

            }
            //At this point, the list of times is populated and so is the dictionary of concentrations by molecule
        }

        public void SetTimeMinMax()
        {
            double minVal = 1e7;
#if OLD_RC
            foreach (ConfigReactionGuidRatePair grp in CRC.ReactionRates)
            {
                minVal = Math.Min(minVal, grp.ReactionComplexRate);
            }
#endif
            //MaxTime = (int)dMaxTime;
            dInitialTime = 5 / minVal;
            dMaxTime = 2 * dInitialTime;
        }

        //This method updates the conc of the given molecule
        public void EditConc(string moleculeKey, double conc)
        {
            dictInitialConcs[moleculeKey] = conc;
            initConcsDict[moleculeKey].conc = conc;
            OnPropertyChanged("initConcs");
            //Go();
            
        }

        //Save the original concs in a temp array in case user wants to discard the changes
        public void SaveOriginalConcs()
        {
            dictOriginalConcs.Clear();

            if (SimulationBase.dataBasket.Cells.Count <= 0)
                return;

            Compartment comp = SimulationBase.dataBasket.Cells.First().Value.Cytosol;

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
            if (SimulationBase.dataBasket.Cells.Count <= 0)
                return;

            Compartment comp = SimulationBase.dataBasket.Cells.First().Value.Cytosol;
            if (comp == null)
                return;

            dictOriginalConcs.Clear();

            double[] initArray = new double[1];

            //Copy current (may have changed) initial concs to Originals dict
            foreach (KeyValuePair<string, double> kvp in dictInitialConcs)
            {
                dictOriginalConcs[kvp.Key] = kvp.Value;

                //Now overwrite the concs in Protocoluration
                ConfigMolecularPopulation mol_pop = (ConfigMolecularPopulation)(CRC.molpops.First());
                MolPopHomogeneousLevel homo = (MolPopHomogeneousLevel)mol_pop.mp_distribution;
                homo.concentration = kvp.Value;                
            }           
        }

        //Save the initial concs. If user drags graph, use dictInitialConcs to update the initial concs
        public void SaveInitialConcs()
        {
            dictInitialConcs.Clear();
            initConcs.Clear();
            initConcsDict.Clear();

            if (SimulationBase.dataBasket.Cells.Count <= 0)
                return;

            Compartment comp = SimulationBase.dataBasket.Cells.First().Value.Cytosol;
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

        public void UpdateRateConstants()
        {
#if OLD_RC
            foreach (ConfigReactionGuidRatePair grp in CRC.ReactionRates)
            {
                string guid = grp.entity_guid;
                ConfigReaction cr = SC.entity_repository.reactions_dict[guid];
                cr.rate_const = grp.ReactionComplexRate;
            }
#endif
        }

        public void RestoreOriginalRateConstants()
        {
#if OLD_RC
            foreach (ConfigReactionGuidRatePair grp in CRC.ReactionRates)
            {
                string guid = grp.entity_guid;
                ConfigReaction cr = SC.entity_repository.reactions_dict[guid];
                cr.rate_const = grp.OriginalRate;
                grp.ReactionComplexRate = grp.OriginalRate;
            }
#endif
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

        public MolConcInfo(string guid, double c, Protocol protocol)
        {
            molguid = guid;
            conc = c;
            molname = protocol.entity_repository.molecules_dict[guid].Name ;
        }
    }
}
