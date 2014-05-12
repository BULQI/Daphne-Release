﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Serialization;


namespace Daphne
{
    public class SpeciesReference
    {
        public SpeciesReference()
        {
            species = "";
            stoichiometry = 0;
        }

        public string species;
        public int stoichiometry;
    }

    public class ReactionTemplate
    {
        public ReactionTemplate()
        {
            listOfReactants = new List<SpeciesReference>();
            listOfProducts = new List<SpeciesReference>();
            rateConst = 0;
            listOfModifiers = new List<SpeciesReference>();
            typeOfReaction = "";
        }

        public List<SpeciesReference> listOfReactants;
        public List<SpeciesReference> listOfProducts;
        public double rateConst;
        public List<SpeciesReference> listOfModifiers;
        public string typeOfReaction;
    }

    public class XMLReactionsSpec
    {
        public XMLReactionsSpec()
        {
            listOfReactions = new List<ReactionTemplate>();
        }

        public List<ReactionTemplate> listOfReactions;
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


        public void TemplReacType(List<ReactionTemplate> rtList)
        {
            //List<ReactionTemplate> rtList = rtList;

            int rCnt, pCnt, rStoichSum, pStoichSum;

            foreach (ReactionTemplate rt in rtList)
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

