﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms.DataVisualization.Charting;
using System.Drawing;

namespace DaphneGui
{
    public class CellMolecularInfo
    {
        public CellMolecularInfo()
        {
            //Show = true;
        }
        public string Molecule { get; set; }        
        public string Concentration { get; set; }
        //public bool Show { get; set; }
    }

    public class MoleculeTriplet
    {
        public string Name { get; set; }
        public double Time { get; set; }
        public double Conc { get; set; }
    }

    public class TimeTrajectory
    {
        protected List<CellMolecularInfo> currConcs = new List<CellMolecularInfo>();
        protected Dictionary<string, bool> showTT = new Dictionary<string,bool>(); //whether to show tt for a given molecule by name
        protected Dictionary<double, List<CellMolecularInfo>> dictTT = new Dictionary<double,List<CellMolecularInfo>>();

        protected Dictionary<string, Dictionary<double, double>> dictTT2 = new Dictionary<string, Dictionary<double, double>>();

        List<double> listTimes = new List<double>();
        Dictionary<string, List<double>> dictConcs = new Dictionary<string, List<double>>();

        ////private ChartingManager chtManager;

        public TimeTrajectory(int cell_id)
        {
            CellId = cell_id;
        }

        public int CellId { get; set; }

        public Dictionary<string, bool> ShowTT { 
            get 
            { 
                return showTT; 
            } 
        }

        public Dictionary<double, List<CellMolecularInfo>> DictTT
        {
            get
            {
                return dictTT;
            }
            set
            {
                dictTT = value;
            }
        }

        public Dictionary<string, Dictionary<double, double>> DictTT2
        {
            get
            {
                return dictTT2;
            }
            set
            {
                dictTT2 = value;
            }
        }
        
        public List<CellMolecularInfo> CurrConcs
        {
            get
            {
                return currConcs;
            }
        }

        //public void Populate()
        //{
        //    //First set up the Show molecule in TT dictionary
        //    showTT.Clear();
        //    foreach (CellMolecularInfo cmi in currConcs)
        //    {
        //        showTT.Add(cmi.Molecule, cmi.Show);
        //    }

        //    //Now populate the time trajectory dictionary, get data from database
        //    //Need to write new code in Database.cs
        //    dictTT.Clear();
        //    DataReader reader = new DataReader(MainWindow.SC.SimConfig.experiment_db_id);
        //    //dictTT = reader.GetCellMolecularConcs(CellId);

        //    dictTT2.Clear();

        //    //THIS CODE GETS SIMPLE TRIPLETS FROM DATABASE - molecule name, time, conc
        //    List<MoleculeTriplet> listMol = new List<MoleculeTriplet>();
        //    listMol = reader.GetCellMolecularConcs(CellId);

        //    //Extract the list of times from the results
        //    listTimes.Clear();
        //    List<double> listConcs = new List<double>();
        //    foreach (MoleculeTriplet mt in listMol)
        //    {
        //        if (!listTimes.Contains(mt.Time))
        //        {
        //            listTimes.Add(mt.Time);
        //        }                
        //    }

        //    //Now extract a dictionary of molecule and conc lists
        //    string lastname = "";
        //    double lastconc = 0;
        //    int i = 0;
        //    foreach (MoleculeTriplet mt in listMol)
        //    {
        //        string thisname = mt.Name;
        //        double thisconc = mt.Conc;
        //        if (i == 0)
        //        {
        //            lastname = thisname;
        //            lastconc = thisconc;
        //        }
        //        i++;

        //        if (thisname != lastname)
        //        {
        //            dictConcs.Add(lastname, listConcs);
        //            listConcs = new List<double>();
        //        }
        //        listConcs.Add(thisconc);
        //        lastconc = thisconc;
        //        lastname = thisname;
        //    }

        //    if (listConcs.Count > 0)
        //    {
        //        dictConcs.Add(lastname, listConcs);
        //    }

        //}

        //Render data to MS chart - use ChartingManager class that is already implemented
        //public void Render(System.Windows.Forms.Panel p)
        //{
        //    chtManager = new ChartingManager();
        //    chtManager.setChartingPanel(p);
        //    Size sz = new Size();
        //    sz.Width = 700;
        //    sz.Height = 400;
        //    chtManager.ChartSize = sz;

        //    if (listTimes.Count > 0 && dictConcs.Count > 0)
        //    {
        //        chtManager.chartXY_Series(listTimes, dictConcs, "Time Trajectory of Molecular Concentrations Cell " + CellId, "Time", "Concentration", true);
        //    }
        //}
    }
}