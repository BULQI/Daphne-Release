using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using Daphne;
using Workbench;
using System.Xml.Serialization;

namespace DaphneGui
{
    public class GuiReactionComplex
    {
        public string Name { get; set; }
        public ObservableCollection<GuiReactionTemplate> Reactions { get; set; }
        public ObservableCollection<GuiMolecule> Molecules { get; set; }
        public string gui_reaction_complex_guid { get; set; }

        [XmlIgnore]
        public Dictionary<string, Molecule> MolDict { get; set; }

        public GuiReactionComplex()
        {
            Guid id = Guid.NewGuid();
            gui_reaction_complex_guid = id.ToString();
            Reactions = new ObservableCollection<GuiReactionTemplate>();
        }

        public void ParseForMolecules()
        {
            MolDict = new Dictionary<string, Molecule>();
            foreach (GuiReactionTemplate grt in Reactions)
            {
                foreach (SpeciesReference sr in grt.listOfReactants)
                {
                    GuiMolecule gm = MainWindow.SC.SimConfig.FindMolecule(sr.species);
                    if (gm != null) {
                        if (!MolDict.ContainsKey(sr.species))
                        {
                            Molecule mol = new Molecule(gm.Name, gm.MolecularWeight, gm.EffectiveRadius, gm.DiffusionCoefficient);
                            MolDict.Add(mol.Name, mol);
                        }
                    }
                }
                foreach (SpeciesReference sr in grt.listOfProducts)
                {
                    GuiMolecule gm = MainWindow.SC.SimConfig.FindMolecule(sr.species);
                    if (gm != null)
                    {
                        if (!MolDict.ContainsKey(sr.species))
                        {
                            Molecule mol = new Molecule(gm.Name, gm.MolecularWeight, gm.EffectiveRadius, gm.DiffusionCoefficient);
                            MolDict.Add(mol.Name, mol);
                        }
                    }
                }
                foreach (SpeciesReference sr in grt.listOfModifiers)
                {
                    GuiMolecule gm = MainWindow.SC.SimConfig.FindMolecule(sr.species);
                    if (gm != null)
                    {
                        if (!MolDict.ContainsKey(sr.species))
                        {
                            Molecule mol = new Molecule(gm.Name, gm.MolecularWeight, gm.EffectiveRadius, gm.DiffusionCoefficient);
                            MolDict.Add(mol.Name, mol);
                        }
                    }
                }
            }

            // Write out XML file
            MainWindow.SC.SerializeSimConfigToFile();
        }

        public void CopyReactionsTo(ReactionComplex rc)
        {
            //Copy reactions
            foreach (GuiReactionTemplate grt in Reactions)
            {
                rc.ReactionsInComplex.Add(grt);
            }

            
        }

        public void CopyMoleculesTo(ReactionComplex rc)
        {
            //Copy molecules
            foreach (KeyValuePair<string, Molecule> kvp in MolDict)
            {
                Molecule mol = new Molecule(kvp.Value.Name, kvp.Value.MolecularWeight, kvp.Value.EffectiveRadius, kvp.Value.DiffusionCoefficient);
                rc.AddMolecularPopulation(mol, 2.0);
            }
        }

        //public void Run()
        //{
        //    ReactionComplexSimulation rcs = new ReactionComplexSimulation(this);
        //    rcs.Go();
        //}
    }
}
