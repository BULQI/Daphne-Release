using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;

using Daphne;

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

    public class GuiReactionTemplate
    {
        public GuiReactionTemplate()
        {
            listOfReactants = new List<SpeciesReference>();
            listOfProducts = new List<SpeciesReference>();
            rateConst = 0;
            listOfModifiers = new List<SpeciesReference>();
            typeOfReaction = "";
        }

        public void CopyTo(ReactionTemplate rt )
        {
            rt.rateConst = rateConst;
            rt.typeOfReaction = typeOfReaction;
            rt.listOfModifiers.AddRange(listOfModifiers);
            rt.listOfProducts.AddRange(listOfProducts);
            rt.listOfReactants.AddRange(listOfReactants);
        }

        public double RateConst
        {
            get
            {
                return rateConst;
            }
        }
        public string TypeOfReaction
        {
            get
            {
                return typeOfReaction;
            }
        }


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

        public List<SpeciesReference> listOfReactants;
        public List<SpeciesReference> listOfProducts;
        public double rateConst;
        public List<SpeciesReference> listOfModifiers;
        public string typeOfReaction;
        private string reactantsString;
        private string productsString;
        private string totalReactionString;

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
                    rt.typeOfReaction = "annihilation";

                }
                else if ((rt.listOfReactants.Count == 2) && (rt.listOfProducts.Count == 1) && (rStoichSum == 2) && (pStoichSum == 1))
                {
                    // association  a + b	→	c
                    rt.typeOfReaction = "association";
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
                    rt.typeOfReaction = "creation";
                }
                else if ((rt.listOfReactants.Count == 1) && (rt.listOfProducts.Count == 1) && (rStoichSum == 2) && (pStoichSum == 1))
                {
                    // dimerizaton 2a → b
                    rt.typeOfReaction = "dimerization";
                }
                else if ((rt.listOfReactants.Count == 1) && (rt.listOfProducts.Count == 1) && (rStoichSum == 1) && (pStoichSum == 2))
                {
                    // dimer dissociation b → 2a
                    rt.typeOfReaction = "dimerDissociation";
                }
                else if ((rt.listOfReactants.Count == 1) && (rt.listOfProducts.Count == 2) && (rStoichSum == 1) && (pStoichSum == 2))
                {
                    // dissociation c →	a + b
                    rt.typeOfReaction = "dissociation";
                }
                else if ((rt.listOfReactants.Count == 1) && (rt.listOfProducts.Count == 1) && (rStoichSum == 1) && (pStoichSum == 1))
                {
                    // transformation   a →	b
                    rt.typeOfReaction = "transformation";
                }
                else
                {
                    // generalized reaction 
                    // to do: check for nonsense coefficient combos?
                    // not implemented yet.
                    rt.typeOfReaction = "generalized";
                }
                // Every reaction has a catalyzed counterpart
                // Keep the same basic reaction types and account for catalyzers later when the reactions are created
                // See ReactionSwithch() in ReactionBuilder.cs
                if (rt.listOfModifiers.Count > 0)
                {
                    rt.typeOfReaction = rt.typeOfReaction + "_cat";
                }

            }

        }


    }

}
