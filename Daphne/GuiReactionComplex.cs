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
