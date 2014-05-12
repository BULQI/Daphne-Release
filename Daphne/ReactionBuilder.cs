using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daphne
{
    public static class ReactionBuilder
    {
        public static bool CompartHasThisReaction(List<ReactionTemplate> rtList, ReactionTemplate RT)
        {
            // Compare the compartments list of reactions templates to the global list

            if (rtList.Count == 0) return false;

            foreach (ReactionTemplate rt in rtList)
            {
                if (rt == RT)
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasAllReactants(Dictionary<string, MolecularPopulation> molPops, ReactionTemplate rt)
        {
            // Compare the molecules in molPops to the Reaction species in the reaction template
            // Return true if all the species in the reaction template have matches in the molecular population

            if (rt.listOfReactants.Count == 0)
            {
                return true;
            }

            foreach (SpeciesReference spRef in rt.listOfReactants)
            {
                // as soon as there is one not found we can return false
                if (molPops.ContainsKey(spRef.species) == false)
                {
                    return false;
                }
            }

            return true;
        }

        public static bool HasAllModifiers(Dictionary<string, MolecularPopulation> molPops, ReactionTemplate rt)
        {
            // Compare the molecules in molPops to the Modifier species in the reaction template
            // Return true if all the species in the reaction template have matches in the molecular population

            if (rt.listOfModifiers.Count == 0)
            {
                return true;
            }

            foreach (SpeciesReference spRef in rt.listOfModifiers)
            {
                // as soon as there is one not found we can return false
                if (molPops.ContainsKey(spRef.species) == false)
                {
                    return false;
                }
            }

            return true;
        }

        public static void AddNeededProducts(ReactionTemplate rt, Compartment C, Dictionary<string, Molecule> MolDict)
        {
            // Check the product molecules exist as MolecularPopulations in the Compartment
            // If not, add a molecular population to the compartment
            foreach (SpeciesReference spRef in rt.listOfProducts)
            {
                // not contained? add it
                if (C.Populations.ContainsKey(spRef.species) == false)
                {
                    C.AddMolecularPopulation(MolDict[spRef.species], 0.0);
                }
            }
        }

        public static void ReactionSwitch(Compartment C, ReactionTemplate rt)
        {
            MolecularPopulation reactant0, reactant1, product0, product1, catalyst;

            // TODO: Error catching
            // TODO: We only allow for one catalyst. Check for more than one catalyst?

            // Remove any "_cat" suffixes that indicate a catalyzed reaction
            string[] rtType = rt.typeOfReaction.Split('_');

            // Indicates a catalyzed reaction
            bool tfCat = (rt.listOfModifiers.Count > 0);
            if (tfCat)
            {
                catalyst = C.Populations[rt.listOfModifiers[0].species];
            }
            else
            {
                catalyst = null;
            }

            switch (rtType[0])
            {
                case ("annihilation"):

                    reactant0 = C.Populations[rt.listOfReactants[0].species];

                    if (tfCat)
                    {
                        // Catalyzed annihilation a + e	→	e
                        C.reactions.Add(new CatalyzedAnnihilation(catalyst,reactant0, rt.rateConst));
                    }
                    else
                    {
                        // annihilation a	→	0
                        C.reactions.Add(new Annihilation(reactant0, rt.rateConst));
                    }

                    C.rtList.Add(rt);
                    break;

                case ("association"):

                    reactant0 = C.Populations[rt.listOfReactants[0].species];
                    reactant1 = C.Populations[rt.listOfReactants[1].species];
                    product0 = C.Populations[rt.listOfProducts[0].species];

                    if (tfCat)
                    {
                        // Catalyzed association e + a + b	→	c + e
                       C.reactions.Add(new CatalyzedAssociation(catalyst, reactant0, reactant1, product0, rt.rateConst));
                    }
                    else
                    {
                        // association  a + b	→	c
                        C.reactions.Add(new Association(reactant0, reactant1, product0, rt.rateConst));
                    }

                    C.rtList.Add(rt);
                    break;

                //case ("autocatalyticTransformation"):

                //    reactant0 = C.Populations[rt.listOfReactants[0].species];
                //    reactant1 = C.Populations[rt.listOfReactants[1].species];
                //    product0 = C.Populations[rt.listOfProducts[0].species];

                //    if (tfCat)
                //    {
                //        // Catalyzed association e + a	→	2e
                //        C.reactions.Add(new AutocatalyticTransformation(catalyst, reactant0, rt.rateConst));
                //    }
                //    else
                //    {
                //        // Does not have a non-catalytic implementation 
                //        // TODO: Error catching here
                //    }

                //    C.rtList.Add(rt);
                //    break;

                case ("creation"):

                    product0 = C.Populations[rt.listOfProducts[0].species];

                    if (tfCat)
                    {
                        // Catalyzed creation e →	a + e
                        C.reactions.Add(new CatalyzedCreation(catalyst, product0, rt.rateConst));
                    }
                    else
                    {
                        // creation (not allowed)  0 →	a
                        // TODO: handle this error properly
                    }

                    C.rtList.Add(rt);
                    break;

                case ("dimerization"):

                    reactant0 = C.Populations[rt.listOfReactants[0].species];
                    product0 = C.Populations[rt.listOfProducts[0].species];

                    if (tfCat)
                    {
                        // Catalyzed dimerization a + a + e →	b + e
                        C.reactions.Add(new CatalyzedDimerization(catalyst,reactant0, product0, rt.rateConst));
                    }
                    else
                    {
                        // dimerizaton 2a → b
                        C.reactions.Add(new Dimerization(reactant0, product0, rt.rateConst));
                    }

                    C.rtList.Add(rt);
                    break;

                case ("dimerDissociation"):

                    reactant0 = C.Populations[rt.listOfReactants[0].species];
                    product0 = C.Populations[rt.listOfProducts[0].species];

                    if (tfCat)
                    {
                        // Catalyzed dimerization a + a + e →	b + e
                        C.reactions.Add(new CatalyzedDimerDissociation(catalyst,reactant0, product0, rt.rateConst));
                    }
                    else
                    {
                        // dimer dissociation b → 2a
                        C.reactions.Add(new DimerDissociation(reactant0, product0, rt.rateConst));
                    }

                    C.rtList.Add(rt);
                    break;

                case ("dissociation"):

                    reactant0 = C.Populations[rt.listOfReactants[0].species];
                    product0 = C.Populations[rt.listOfProducts[0].species];
                    product1 = C.Populations[rt.listOfProducts[1].species];

                    if (tfCat)
                    {
                        // Catalyzed dissociation a + e →	b + c + e
                        C.reactions.Add(new CatalyzedDissociation(catalyst, reactant0, product0, product1, rt.rateConst));
                    }
                    else
                    {
                        // dimer dissociation a → b + c
                        C.reactions.Add(new Dissociation(reactant0, product0, product1, rt.rateConst));
                    }

                    C.rtList.Add(rt);
                    break;

                case ("transformation"):

                    reactant0 = C.Populations[rt.listOfReactants[0].species];
                    product0 = C.Populations[rt.listOfProducts[0].species];

                    if (tfCat)
                    {
                        // Catalyzed dissociation a + e →	b + e
                        C.reactions.Add(new CatalyzedTransformation(catalyst, reactant0, product0, rt.rateConst));
                    }
                    else
                    {
                        // dimer dissociation a → b 
                        C.reactions.Add(new Transformation(reactant0, product0, rt.rateConst));
                    }

                    C.rtList.Add(rt);
                    break;

                case ("generalized"):

                    // TODO: implement generalized reactions
                    C.rtList.Add(rt);
                    break;

            }
        }

        public static bool CompartReactions(Compartment C, List<ReactionTemplate> rtList, Dictionary<string, Molecule> MolDict)
        {
            // Return true if a product molecule or reaction is added
            // If so we will need to reevaluate all compartments again

            bool changed = false;

            foreach (ReactionTemplate rt in rtList)
            {
                // Check to see if this reaction already exists in the compartment
                if (ReactionBuilder.CompartHasThisReaction(C.rtList, rt) == false)
                {
                    // Check for all the necessary reactants and catalysts
                    if (HasAllReactants(C.Populations, rt) == true && HasAllModifiers(C.Populations, rt) == true)
                    {
                        // Add any missing products
                        AddNeededProducts(rt, C, MolDict);

                        // Add reaction and reaction template to Compartment
                        ReactionSwitch(C, rt);
                        changed = true;
                    }
                }
            }

            return changed;
        }

        /// <summary>
        /// Compare molecular populations in the embedding and embedded manifold to the molecules in the list of reaction templates.
        /// Add reactions that involve molecular populations from both manifolds to the compartrment containing the embedded manifold as boundary reactions.
        /// Currently, onyl BoundaryAssocation and BoundaryDissociation are implemented as boundary reactions.
        /// NOTE: Recognizing and assigning boundary reactions is tricky and this algorithm may not be robust as new boundary
        /// reactions or situations arise. 
        /// For example, it is assumed that missing product molecules always get added to embedded manifold.
        /// </summary>
        /// <param name="C1">Compartment containing the embedding manifold</param>
        /// <param name="C2">Compartment containing the embedded manifold</param>
        /// <param name="rtList">List of template reactions</param>
        /// <param name="MolDict">A dictionary with molecule names and molecules as the key/value pairs</param>
        /// <returns></returns>
        public static bool CompartBoundaryReactions(Compartment C1, Compartment C2, List<ReactionTemplate> rtList, Dictionary<string, Molecule> MolDict)
        {
            // Return true if a reaction is added
            // If so we will need to reevaluate all compartments again
            // NOTE: C1 contains the embedding manifold and C2 contains the embedded manifold

            bool changed = false;

            // Lists of Compartments associated with each reactant, modifier, or product molecular population
            List<Compartment> reacComp;
            List<Compartment> modComp;
            List<Compartment> prodComp;

            foreach (ReactionTemplate rt in rtList)
            {
                // Check to see if this reaction already exists in the compartment associated with the embedded manifold
                if ( ReactionBuilder.CompartHasThisReaction(C2.rtList, rt) == false )
                {
                    // NOTE: At this point, we only have BoundaryAssociation and BoundaryDissociation
                    if (rt.typeOfReaction == "association" || rt.typeOfReaction == "dissociation")
                    {
                        // NOTE: The rest of the algorithm is more general than it needs to be for boundary association
                        // and dissociation, but we may need the generality for other types of boundary reactions

                        // Check for all the necessary reactants and catalysts
                        reacComp = ShareAllReactants(C1, C2, rt);
                        modComp = ShareAllModifiers(C1, C2, rt);

                        if ( (reacComp.Count > 0 || rt.listOfReactants.Count == 0) &&
                            (modComp.Count > 0 || rt.listOfModifiers.Count == 0)  )
                        {                        
                            // NOTE: Missing product molecules always get added to embedded manifold. 
                            // QUESTION: How robust is this assumption?
                            prodComp = AddNeededBoundaryProducts(rt, C1, C2, MolDict);

                            // Check to see whether each compartment has at least one reactant, modifier, or product
                            // molecular population associated with this reaction. Otherwise, don't add it.
                            // e.g., CXCL13 -> 0
                            if ( BothCompsInThisReaction(reacComp, modComp, prodComp, C1, C2) == true)
                            {
                                // Add reaction and reaction template to the Compartment of the embedded manifold
                                ReactionSwitchList(reacComp, modComp, prodComp, C2, rt);

                                changed = true;
                            }

                        }
                    }
                }
            }

            return changed;
        }

        public static bool BothCompsInThisReaction(List<Compartment> reacComp, List<Compartment> modComp, List<Compartment> prodComp, Compartment C1, Compartment C2)
        {
            bool tfC1 = false;
            bool tfC2 = false;

            // Check to see whether each compartment has at least one reactant, modifier, or product
            // molecular population associated with this reaction. Otherwise, don't add it.
            // e.g., CXCL13 -> 0

            foreach (Compartment c in reacComp)
            {
                if (c == C1) 
                {
                    tfC1 = true;
                }
                else if (c == C2) 
                {
                    tfC2 = true;
                }
            }

            foreach (Compartment c in modComp)
            {
                if (c == C1)
                {
                    tfC1 = true;
                }
                else if (c == C2)
                {
                    tfC2 = true;
                }
            }

            foreach (Compartment c in prodComp)
            {
                if (c == C1)
                {
                    tfC1 = true;
                }
                else if (c == C2)
                {
                    tfC2 = true;
                }
            }
           
            
            return (tfC1 && tfC2);
        }

        public static List<Compartment> AddNeededBoundaryProducts(ReactionTemplate rt, Compartment C1, Compartment C2, Dictionary<string, Molecule> MolDict)
        {
            // Check to see if the product molecules exist as a MolecularPopulation in either of the Compartments
            // C1 is the embedding compartment and C2 is the embedded compartment
            // If not, add a molecular population to the embedded compartment

            List<Compartment> prodComp = new List<Compartment>();

            if (rt.listOfReactants.Count == 0)
            {
                return prodComp;
            }
            
            foreach (SpeciesReference spRef in rt.listOfProducts)
            {
                if (C1.Populations.ContainsKey(spRef.species) == true)
                {
                    // This molecular population corresponding to the product molecule exists in the embedding manifold
                    // Add the compartment containing embedding manifold to the list product compartments
                    prodComp.Add(C1);
                }
                else if (C2.Populations.ContainsKey(spRef.species) == true)
                {
                    // This molecular population corresponding to the product molecule exists in the embedded manifold
                    // Add the compartment containing embedded manifold to the list of product compartments
                    prodComp.Add(C2);
                }
                else
                {
                    // Product molecular population does not exist. Add it to the compartment containing the embedded manifold.
                    C2.AddMolecularPopulation(MolDict[spRef.species], 0.0);
                    prodComp.Add(C2);
                }
            }

            return prodComp;
        }

        public static List<Compartment> ShareAllReactants(Compartment C1, Compartment C2, ReactionTemplate rt)
        {
            // Compare the moleculecular populations in Compartments C1 and C2 to the Reactant species in the reaction template
            // C1 is the embedding compartment and C2 is the embedded compartment
            // Return a list of Compartments associated with each Reactant
            // Return an empty list if there are no Reactants specified in the reaction template or all the Reactants are not present

            List<Compartment> reacComp = new List<Compartment>();
            Dictionary<string, MolecularPopulation> molPops1 = C1.Populations;
            Dictionary<string, MolecularPopulation> molPops2 = C2.Populations;

            if (rt.listOfReactants.Count == 0)
            {
                return reacComp;
            }

            foreach (SpeciesReference spRef in rt.listOfReactants)
            {
                if (molPops1.ContainsKey(spRef.species) == true)
                {
                    reacComp.Add(C1);
                }
                else if (molPops2.ContainsKey(spRef.species) == true)
                {
                    reacComp.Add(C2);
                }
                else
                {
                    return new List<Compartment>();
                }
            }

            return reacComp;
        }

        public static List<Compartment> ShareAllModifiers(Compartment C1, Compartment C2, ReactionTemplate rt)
        {
            // Compare the moleculecular populations in Compartments C1 and C2 to the Modifier species in the reaction template
            // C1 is the embedding compartment and C2 is the embedded compartment
            // Return a list of Compartments associated with each Modifier
            // Return an empty list if there are no Modifiers specified in the reaction template or all the Modifiers are not present

            List<Compartment> modComp = new List<Compartment>();
            Dictionary<string, MolecularPopulation> molPops1 = C1.Populations;
            Dictionary<string, MolecularPopulation> molPops2 = C2.Populations;

            if (rt.listOfModifiers.Count == 0)
            {
                return modComp;
            }

            foreach (SpeciesReference spRef in rt.listOfModifiers)
            {
                if (molPops1.ContainsKey(spRef.species) == true)
                {
                    modComp.Add(C1);
                }
                else if (molPops2.ContainsKey(spRef.species) == true)
                {
                    modComp.Add(C2);
                }
                else
                {
                    return new List<Compartment>();
                }
            }
            return modComp;
        }

        public static void ReactionSwitchList(List<Compartment> reacComp, List<Compartment> modComp, List<Compartment> prodComp, Compartment embeddedComp, ReactionTemplate rt)
        {
            MolecularPopulation receptor, ligand, complex;

            // TODO: Error catching
            // NOTE: The arguments passed into ReactionSwitchList are more general than they need to be for boundary association
            // and dissociation, but we may need the generality for other types of boundary reactions

            switch (rt.typeOfReaction)
            {

                case ("association"):

                    if (reacComp[0].Interior.Dim == 3)
                    {
                        receptor = reacComp[1].Populations[rt.listOfReactants[0].species];
                        ligand = reacComp[0].Populations[rt.listOfReactants[1].species];
                    }
                    else
                    {
                        receptor = reacComp[0].Populations[rt.listOfReactants[0].species];
                        ligand = reacComp[1].Populations[rt.listOfReactants[1].species];
                    }
                    complex = prodComp[0].Populations[rt.listOfProducts[0].species];

                    // boundary association  receptor + ligand	→	comples
                    embeddedComp.reactions.Add(new BoundaryAssociation(receptor, ligand, complex, rt.rateConst));
                    embeddedComp.rtList.Add(rt);

                    break;


                case ("dissociation"):

                    if (prodComp[0].Interior.Dim == 3)
                    {
                        receptor = prodComp[1].Populations[rt.listOfProducts[0].species];
                        ligand = prodComp[0].Populations[rt.listOfProducts[1].species];
                    }
                    else
                    {
                        receptor = prodComp[0].Populations[rt.listOfProducts[0].species];
                        ligand = prodComp[1].Populations[rt.listOfProducts[1].species];
                    }
                    complex = reacComp[0].Populations[rt.listOfReactants[0].species];

                    // boundary association  receptor + ligand	→	complex
                    embeddedComp.reactions.Add(new BoundaryDissociation(receptor, ligand, complex, rt.rateConst));
 
                    embeddedComp.rtList.Add(rt);
                    break;

            }
        }



    }

}


