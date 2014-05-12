using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daphne
{

    /// <summary>
    /// Use this version if Molecule is a class, instead of a struct
    /// </summary>
    public static class MoleculeBuilder
    {
        public static Dictionary<string, Molecule> Go(string MolSpec)
        {
            string[] molString = MolSpec.Split('\n');

            int nNames = molString.GetLength(0) - 1;
            Molecule[] mol = new Molecule[nNames];
            Dictionary<string, Molecule> molDict = new Dictionary<string, Molecule>();

            for (int i = 0; i < nNames; i++)
            {
                string[] molField = molString[i].Split('\t');
                int nVals = molField.GetLength(0);
                string Name;
                double MolecularWeight=0, EffectiveRadius=0, DiffusionCoefficient=0;

                // In future, check for duplicate molecule names?
                Name = molField[0];

                // Other checks?
                if (molField[1].Length > 0)
                {
                    MolecularWeight = Convert.ToDouble(molField[1]);
                }
                if (molField[2].Length > 0)
                {
                    EffectiveRadius = Convert.ToDouble(molField[2]);
                }
                if (molField[3].Length > 0)
                {
                    DiffusionCoefficient = Convert.ToDouble(molField[3]);
                }

                mol[i] = new Molecule(Name, MolecularWeight, EffectiveRadius, DiffusionCoefficient);

                molDict.Add(mol[i].Name, mol[i]);
            }

            return molDict;
        }
    }
}

