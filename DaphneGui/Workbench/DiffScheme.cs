using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;

namespace GuiDaphneApp
{
    //A Differentiation Scheme has a name and one list of states, each state with its genes and their boolean values
    //For example, one differentiation scheme could be like this:
    //
    //    State/Gene     gCXCR5   gsDiv   gsDif1   gsDif2   gIg
    //    ------------------------------------------------------  
    //    Centroblast      0        1        1        0      0
    //    Centrocyte       1        0        0        1      0
    //    Plasmacyte       1        0        0        0      1

    public class DiffScheme
    {
        public ObservableCollection<string> molecNames = new ObservableCollection<string>();
        
        private ObservableCollection<CellState> cellStates = new ObservableCollection<CellState>();
        public ObservableCollection<CellState> CellStates
        {
            get
            {
                return cellStates;
            }
            set
            {
                cellStates = value;
            }
        }  

        private ObservableCollection<DiffState> states = new ObservableCollection<DiffState>();
        public ObservableCollection<DiffState> States
        {
            get
            {
                return states;
            }
            set
            {
                states = value;
            }
        }        

        public string Name { get; set; }

        public DiffScheme(string name)
        {
            Name = name;           
        }

        public void StoreMolNames(List<string> names)
        {
            foreach(string s in names) {
                molecNames.Add(s);
            }

        }

        public void AddStatesGenes(string[] states, string[] genes) 
        {
            Gene gene = null;
            DiffState ds = null;
            foreach (string s in states)
            {
                ds = new DiffState(s);
                foreach (string g in genes)
                {
                    gene = new Gene(g);
                    ds.Genes.Add(gene);
                }
                States.Add(ds);
            }
        }

        public void CopyTo(DiffScheme ds)
        {
            foreach (DiffState st in this.states)
            {
                DiffState state = new DiffState(st.Name);
                foreach (Gene g in st.Genes)
                {
                    Gene gene = new Gene(g.Name);
                    gene.Active = g.Active;
                    state.Genes.Add(gene);
                }
                ds.States.Add(state);
            }
            foreach (CellState cs in this.cellStates)
            {
                CellState cstate = new CellState(cs.Name);
                cstate.Active = true;
                foreach (CellState cs2 in cs.CellStates)
                {
                    CellState cs3 = new CellState(cs2.Name,cs2.MolName);
                    cs3.Active = cs2.Active;
                    cstate.CellStates.Add(cs3);
                }
                ds.CellStates.Add(cstate);
            }
        }

        public bool HasState(string name)
        {
            bool retval = false;

            foreach (DiffState ds in this.states)
            {
                if (ds.Name == name)
                {
                    retval = true;
                    break;
                }
            }

            return retval;
        }

        public DiffState GetState(string name)
        {
            DiffState diff = null;
            foreach (DiffState ds in states)
            {
                if (ds.Name == name)
                {
                    diff = ds;
                    break;
                }
                    
            }
            return diff;

        }

        public void AddCellStates()
        {
            CellState cs = null;
            foreach (DiffState ds in States)
            {
                cs = new CellState(ds.Name);
                cs.Active = true;
                foreach (DiffState ds2 in States)
                {
                    CellState cs2 = new CellState(ds2.Name,"E");
                    cs2.Active = true;
                    if (cs.Name == cs2.Name)
                    {
                        cs2.Active = false;
                        cs2.MolName = "";
                    }
                    cs.CellStates.Add(cs2);
                }
                CellStates.Add(cs);
            }
        }

    }

    //Differentiation State is made up of one set of genes with boolean values for the given state
    //A state could be Centroblast, Centrocyte, Plasmacyte, etc.    
    public class DiffState
    {        
        public string Name { get; set; }
        private ObservableCollection<Gene> genes = new ObservableCollection<Gene>();
        public ObservableCollection<Gene> Genes
        {
            get
            {
                return genes;
            }
            set
            {
                genes = value;                
            }
        }

        public DiffState(string name)
        {
            Name = name;
        }

        public DiffState(DiffState ds)
        {
            this.Name = ds.Name;
            foreach (Gene g in ds.Genes)
            {
                Gene gene = new Gene(g.Name);
                gene.Active = g.Active;
                this.Genes.Add(gene);
            }
        }

        public bool HasGene(string name)
        {
            bool retval = false;

            foreach (Gene g in this.Genes)
            {
                if (g.Name == name)
                {
                    retval = true;
                    break;
                }
            }

            return retval;
        }


    }
    public class Gene
    {
        public string Name { get; set; }
        public bool Active { get; set; }

        public Gene(string name, bool val)
        {
            Name = name;
            Active = val;
        }

        public Gene(string name)
        {
            Name = name;
            Active = false;
        }
    }

    public class CellState
    {
        public string Name { get; set; }
        public string MolName { get; set; }
        public bool Active { get; set; }

        private ObservableCollection<CellState> cellStates = new ObservableCollection<CellState>();
        public ObservableCollection<CellState> CellStates
        {
            get
            {
                return cellStates;
            }
            set
            {
                cellStates = value;
            }
        }

        public CellState(string name)
        {
            Name = name;
        }

        public CellState(string name, string molname)
        {
            Name = name;
            MolName = molname;
        }        

    }

}
