using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace Daphne
{
    public class GuiReactionComplex
    {
        public string Name { get; set; }
        public ObservableCollection<ConfigReaction> Reactions { get; set; }
        public ObservableCollection<ConfigMolecule> Molecules { get; set; }
        public string gui_reaction_complex_guid { get; set; }

        [XmlIgnore]
        public Dictionary<string, Molecule> MolDict { get; set; }

        public GuiReactionComplex()
        {
            Guid id = Guid.NewGuid();
            gui_reaction_complex_guid = id.ToString();
            Reactions = new ObservableCollection<ConfigReaction>();
        }

        //public void ParseForMolecules()
        //{
        //    MolDict = new Dictionary<string, Molecule>();
        //    foreach (ConfigReaction grt in Reactions)
        //    {
        //        foreach (SpeciesReference sr in grt.listOfReactants)
        //        {
        //            ConfigMolecule gm = null;  // MainWindow.SC.SimConfig.FindMolecule(sr.species);
        //            if (gm != null) {
        //                if (!MolDict.ContainsKey(sr.species))
        //                {
        //                    Molecule mol = new Molecule(gm.Name, gm.MolecularWeight, gm.EffectiveRadius, gm.DiffusionCoefficient);
        //                    MolDict.Add(mol.Name, mol);
        //                }
        //            }
        //        }
        //        foreach (SpeciesReference sr in grt.listOfProducts)
        //        {
        //            ConfigMolecule gm = null; // MainWindow.SC.SimConfig.FindMolecule(sr.species);
        //            if (gm != null)
        //            {
        //                if (!MolDict.ContainsKey(sr.species))
        //                {
        //                    Molecule mol = new Molecule(gm.Name, gm.MolecularWeight, gm.EffectiveRadius, gm.DiffusionCoefficient);
        //                    MolDict.Add(mol.Name, mol);
        //                }
        //            }
        //        }
        //        foreach (SpeciesReference sr in grt.listOfModifiers)
        //        {
        //            ConfigMolecule gm = null; // MainWindow.SC.SimConfig.FindMolecule(sr.species);
        //            if (gm != null)
        //            {
        //                if (!MolDict.ContainsKey(sr.species))
        //                {
        //                    Molecule mol = new Molecule(gm.Name, gm.MolecularWeight, gm.EffectiveRadius, gm.DiffusionCoefficient);
        //                    MolDict.Add(mol.Name, mol);
        //                }
        //            }
        //        }
        //    }

        //}

    }
}
