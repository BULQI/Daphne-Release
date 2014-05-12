using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;
using Newtonsoft.Json;

using Daphne;
using System.Windows.Data;

namespace DaphneGui
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
            return (GlobalParameterType)Enum.ToObject(typeof(GlobalParameterType), (int)idx);
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
    public class XMLReactionsSpec
    {
        public XMLReactionsSpec()
        {
            listOfReactions = new List<GuiReactionTemplate>();
        }

        public List<GuiReactionTemplate> listOfReactions;
    }

    public class ReactionsConfigurator
    {
        public ReactionsConfigurator()
        {
        }

        public void deserialize(string filename)
        {
            XmlSerializer xs;

            xs = new XmlSerializer(typeof(XMLReactionsSpec));

            using (Stream s = File.OpenRead(filename))
            {
                // content
                content = (XMLReactionsSpec)xs.Deserialize(s);
            }
        }

        public void serialize(string filename)
        {
            XmlSerializer xs = new XmlSerializer(typeof(XMLReactionsSpec));

            using (Stream s = File.Create(filename))
            {
                xs.Serialize(s, content);
            }
        }

        public XMLReactionsSpec content;


        public void TemplReacType(List<GuiReactionTemplate> rtList)
        {
            //List<ReactionTemplate> rtList = rtList;

            int rCnt, pCnt, rStoichSum, pStoichSum;

            foreach (GuiReactionTemplate rt in rtList)
            {
                rCnt = rt.listOfReactants.Count;
                pCnt = rt.listOfProducts.Count;

                rStoichSum = 0;
                foreach (SpeciesReference reactSpecies in rt.listOfReactants)
                {
                    rStoichSum = rStoichSum + reactSpecies.stoichiometry;
                }

                pStoichSum = 0;
                foreach (SpeciesReference prodSpecies in rt.listOfProducts)
                {
                    pStoichSum = pStoichSum + prodSpecies.stoichiometry;
                }

                if ((rt.listOfReactants.Count == 1) && (rt.listOfProducts.Count == 0) && (rStoichSum == 1))
                {
                    // annihilation a	→	0
                    rt.ReacType = ReactionType.Annihilation;

                }
                else if ((rt.listOfReactants.Count == 2) && (rt.listOfProducts.Count == 1) && (rStoichSum == 2) && (pStoichSum == 1))
                {
                    // association  a + b	→	c
                    rt.ReacType = ReactionType.Association;
                }
                //else if ((rt.listOfReactants.Count == 2) && (rt.listOfProducts.Count == 1) && (rStoichSum == 1) && (pStoichSum == 2))
                //{
                //    // association  e + a	→	2e
                //    rt.typeOfReaction = "autocatalyticTransformation";
                //}
                else if ((rt.listOfReactants.Count == 0) && (rt.listOfProducts.Count == 1))
                {
                    // creation (not allowed)  0 →	a
                    // TODO: handle this error properly
                    rt.ReacType = ReactionType.CatalyzedCreation;
                }
                else if ((rt.listOfReactants.Count == 1) && (rt.listOfProducts.Count == 1) && (rStoichSum == 2) && (pStoichSum == 1))
                {
                    // dimerizaton 2a → b
                    rt.ReacType = ReactionType.Dimerization;
                }
                else if ((rt.listOfReactants.Count == 1) && (rt.listOfProducts.Count == 1) && (rStoichSum == 1) && (pStoichSum == 2))
                {
                    // dimer dissociation b → 2a
                    rt.ReacType = ReactionType.DimerDissociation;
                }
                else if ((rt.listOfReactants.Count == 1) && (rt.listOfProducts.Count == 2) && (rStoichSum == 1) && (pStoichSum == 2))
                {
                    // dissociation c →	a + b
                    rt.ReacType = ReactionType.Dissociation;
                }
                else if ((rt.listOfReactants.Count == 1) && (rt.listOfProducts.Count == 1) && (rStoichSum == 1) && (pStoichSum == 1))
                {
                    // transformation   a →	b
                    rt.ReacType = ReactionType.Transformation;
                }
                else
                {
                    // generalized reaction 
                    // to do: check for nonsense coefficient combos?
                    // not implemented yet.
                    rt.ReacType = ReactionType.Generalized;
                }
            }
        }
    }
}
