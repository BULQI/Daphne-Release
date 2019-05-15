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
using System.IO;
using System.Xml.Serialization;
using Newtonsoft.Json;

//using Daphne;
using System.Windows.Data;

namespace Daphne
{
    //public class SpeciesReference
    //{
    //    public SpeciesReference()
    //    {
    //        species = "";
    //        stoichiometry = 0;
    //    }

    //    public string species;
    //    public int stoichiometry;
    //}

    public class GuiSpeciesReference : SpeciesReference
    {
        public string Location { get; set; }
    }

    public enum ReactionType {  Association=0, Dissociation=1, Annihilation=2, Dimerization=3, DimerDissociation=4, 
                                Transformation=5, AutocatalyticTransformation=6, CatalyzedAnnihilation=7, 
                                CatalyzedAssociation=8, CatalyzedCreation=9, CatalyzedDimerization=10, CatalyzedDimerDissociation=11, 
                                CatalyzedDissociation=12, CatalyzedTransformation=13, BoundaryAssociation=14, BoundaryDissociation=15, Generalized=16}

    /// <summary>
    /// Converter to go between enum values and "human readable" strings for GUI
    /// </summary>
    [ValueConversion(typeof(ReactionType), typeof(string))]
    public class ReactionTypeToShortStringConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the GlobalParameterType enum...
        private List<string> _reaction_type_strings = new List<string>()
                                {
                                    "Association",
                                    "Dissociation",
                                    "Annihilation",
                                    "Dimerization",
                                    "DimerDissociation",
                                    "Transformation",
                                    "AutocatalyticTransformation",
                                    "CatalyzedAnnihilation",
                                    "CatalyzedAssociation",
                                    "CatalyzedCreation",
                                    "CatalyzedDimerization",
                                    "CatalyzedDimerDissociation",
                                    "CatalyzedTransformation",
                                    "CatalyzedDissociation",
                                    "BoundaryAssociation",
                                    "BoundaryDissociation",
                                    "Generalized"
                                };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return _reaction_type_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = _reaction_type_strings.FindIndex(item => item == str);
            return (ReactionType)Enum.ToObject(typeof(ReactionType), (int)idx);
        }
    }

    public class GuiReactionTemplate
    {
        public GuiReactionTemplate()
        {
            Guid id = Guid.NewGuid();
            gui_reaction_template_guid = id.ToString();
            listOfReactants = new List<GuiSpeciesReference>();
            listOfProducts = new List<GuiSpeciesReference>();
            listOfModifiers = new List<GuiSpeciesReference>();
            rateConst = 0;
            
            //typeOfReaction = "";
        }

        public void CopyTo(ReactionTemplate rt )
        {
            rt.rateConst = rateConst;
            //rt.typeOfReaction = typeOfReaction;
            rt.listOfModifiers.AddRange(listOfModifiers);
            rt.listOfProducts.AddRange(listOfProducts);
            rt.listOfReactants.AddRange(listOfReactants);
        }

        public ReactionType ReacType
        {
            get
            {
                return reacType;
            }
            set
            {
                reacType = value;
            }
        }

        public string ReacTypeString
        {
            get
            {
                string result = (string)new ReactionTypeToShortStringConverter().Convert(reacType, typeof(string), null, System.Globalization.CultureInfo.CurrentCulture);
                return result;
            }
        }

        public double RateConst
        {
            get
            {
                return rateConst;
            }
            set
            {
                rateConst = value;
            }
        }

        public string gui_reaction_template_guid { get; set; }

        [JsonIgnore]
        public string ReactantsString
        {
            get
            {
                string s = "";
                foreach (SpeciesReference sr in listOfReactants)
                {
                    if (sr.stoichiometry > 1)
                        s += sr.stoichiometry;
                    s += sr.species;
                    s += " + ";
                }
                foreach (SpeciesReference sr in listOfModifiers)
                {
                    if (sr.stoichiometry > 1)
                        s += sr.stoichiometry;
                    s += sr.species;
                    s += " + ";
                }
                char[] trimChars = { ' ', '+' };
                s = s.Trim(trimChars);
                return s;
            }
            set
            {
                reactantsString = value;
            }

        }
        [JsonIgnore]
        public string ProductsString
        {
            get
            {
                string s = "";
                foreach (SpeciesReference sr in listOfProducts)
                {
                    if (sr.stoichiometry > 1)
                        s += sr.stoichiometry;
                    s += sr.species;
                    s += " + ";
                }
                foreach (SpeciesReference sr in listOfModifiers)
                {
                    if (sr.stoichiometry > 1)
                        s += sr.stoichiometry;
                    s += sr.species;
                    s += " + ";
                }
                char[] trimChars = { ' ', '+' };
                s = s.Trim(trimChars);
                return s;
            }
            set
            {
                productsString = value;
            }
        }
        [JsonIgnore]
        public string TotalReactionString
        {
            get
            {
                return ReactantsString + " -> " + ProductsString;
            }
            set
            {
                totalReactionString = value;
            }
        }
        
        private double rateConst;
        private string reactantsString;
        private string productsString;
        private string totalReactionString;
        private ReactionType reacType;
        public List<GuiSpeciesReference> listOfReactants;
        public List<GuiSpeciesReference> listOfProducts;
        public List<GuiSpeciesReference> listOfModifiers;
        
    }

    public class GuiBoundaryReactionTemplate : GuiReactionTemplate
    {
        public GuiSpeciesReference ligand;
        public GuiSpeciesReference receptor;
        public GuiSpeciesReference complex;
        double fluxIntensityConstant;
    }

    public class GuiCatalyzedReactionTemplate : GuiReactionTemplate
    {
        public GuiSpeciesReference catalyst;
    }
    
}
