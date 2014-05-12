﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Daphne;
using System.Collections.ObjectModel;
using System.IO;
using Newtonsoft.Json;
using System.Xml;

using DaphneGui;
using ManifoldRing;


namespace Workbench
{
    public class ReactionComplexSimulation
    {
        //These are for graphing the concentrations.  This data is generated during the Go method.
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

        private ObservableCollection<MolecularPopulation> obsMolPop = new ObservableCollection<MolecularPopulation>();
        public ObservableCollection<MolecularPopulation> ObsMolPop
        {
            get
            {
                return obsMolPop;
            }
            set
            {
                obsMolPop = value;
            }
        }

        //Reactions
        private List<ConfigReaction> rtList = new List<ConfigReaction>();
        public List<ConfigReaction> ReactionTemplateList
        {
            get
            {
                return rtList;
            }
            set
            {
                rtList = value;
            }
        }

        //Reaction Complex
        private ReactionComplex rc = null;
        public ReactionComplex RC
        {
            get
            {
                return rc;
            }
            set
            {
                rc = value;
            }
        }

        
        public ReactionComplexSimulation()
        {
        }

        public ReactionComplexSimulation(GuiReactionComplex grc)
        {
            TinyBall ball = new TinyBall();

            ball.Initialize(new double[] { 5.0 });
            rc = new ReactionComplex(grc.Name, ball);
            
            //grc.CopyReactionsTo(rc);
            foreach (ConfigReaction grt in grc.Reactions)
            {
                rc.ReactionsInComplex.Add(grt);
            }
            ////grc.ParseForMolecules();
            //grc.CopyMoleculesTo(rc);
            rc.Initialize();

            foreach (KeyValuePair<string, Molecule> kvp in grc.MolDict)
            {
                rc.AddMolecularPopulation(kvp.Value, "const", new double[] { 2.0 });
            }

            foreach (ConfigReaction grt in rc.ReactionsInComplex)
            {
                ////ReactionTemplate rt = new ReactionTemplate();
                ////grt.CopyTo(rt);
                ////ReactionBuilder.ReactionSwitch(rc, rt);
            }

            foreach (KeyValuePair<string, MolecularPopulation> kvp in rc.Populations)
            {
                ObsMolPop.Add(kvp.Value);
            }

            rc.SaveOriginalConcs();
            rc.SaveInitialConcs();
            
        }

        public void Go()
        {
            if (rc != null)
                rc.Go();
        }

        public bool RemoveMolecule(MolecularPopulation mp)
        {
            //ObsMolPop.Remove(mp);
            return ObsMolPop.Remove(mp);            
        }
    }    
}
