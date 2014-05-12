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

        public static bool HasAllReactants(List<MolecularPopulation> molPops, ReactionTemplate rt)
        {
            // Compare the molecules in molPops to the Reaction species in the reaction template
            // Return true if all the species in the reaction template have matches in the molecular population

            if (rt.listOfReactants.Count == 0) return true;

            bool tf = false;

            foreach (SpeciesReference spRef in rt.listOfReactants)
            {
                tf = false;
                foreach (MolecularPopulation molpop in molPops)
                {
                    if (spRef.species == molpop.Molecule.Name)
                    {
                        tf = true;
                        continue;
                    }
                }

                if (!tf) return false;
            }

            return tf;
        }

        public static bool HasAllModifiers(List<MolecularPopulation> molPops, ReactionTemplate rt)
        {
            // Compare the molecules in molPops to the Modifier species in the reaction template
            // Return true if all the species in the reaction template have matches in the molecular population

            if (rt.listOfModifiers.Count == 0) return true;

            bool tf = false;

            foreach (SpeciesReference spRef in rt.listOfModifiers)
            {
                tf = false;
                foreach (MolecularPopulation molpop in molPops)
                {
                    if (spRef.species == molpop.Molecule.Name)
                    {
                        tf = true;
                        continue;
                    }
                }

                if (!tf) return false;
            }

            return tf;
        }

        public static bool AddNeededProducts(ReactionTemplate rt, Compartment C, Dictionary<string, Molecule> MolDict)
        {
            // Check the product molecules exist as MolecularPopulations in the Compartment
            // If not, add a molecular population to the compartment

            bool tf, addedProduct = false;

            foreach (SpeciesReference spRef in rt.listOfProducts)
            {
                tf = false;
                foreach (MolecularPopulation molpop in C.Populations)
                {
                    if (spRef.species == molpop.Molecule.Name)
                    {
                        tf = true;
                        continue;
                    }
                }

                if (!tf)
                {
                    C.AddMolecularPopulation(spRef.species, MolDict[spRef.species], 0.0);
                    addedProduct = true;
                }
            }

            return addedProduct;
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
                catalyst = C.molpopDict[rt.listOfModifiers[0].species];
            }
            else
            {
                catalyst = null;
            }

            switch (rtType[0])
            {
                case ("annihilation"):

                    reactant0 = C.molpopDict[rt.listOfReactants[0].species];

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

                    reactant0 = C.molpopDict[rt.listOfReactants[0].species];
                    reactant1 = C.molpopDict[rt.listOfReactants[1].species];
                    product0 = C.molpopDict[rt.listOfProducts[0].species];

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

                case ("creation"):

                    product0 = C.molpopDict[rt.listOfProducts[0].species];

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

                    reactant0 = C.molpopDict[rt.listOfReactants[0].species];
                    product0 = C.molpopDict[rt.listOfProducts[0].species];

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

                    reactant0 = C.molpopDict[rt.listOfReactants[0].species];
                    product0 = C.molpopDict[rt.listOfProducts[0].species];

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

                    reactant0 = C.molpopDict[rt.listOfReactants[0].species];
                    product0 = C.molpopDict[rt.listOfProducts[0].species];
                    product1 = C.molpopDict[rt.listOfProducts[1].species];

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

                    reactant0 = C.molpopDict[rt.listOfReactants[0].species];
                    product0 = C.molpopDict[rt.listOfProducts[0].species];

                    if (tfCat)
                    {
                        // Catalyzed dissociation a + e →	b + c + e
                        C.reactions.Add(new CatalyzedTransformation(catalyst, reactant0, product0, rt.rateConst));
                    }
                    else
                    {
                        // dimer dissociation a → b + c
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
            bool tfReactants, tfModifiers;

            foreach (ReactionTemplate rt in rtList)
            {
                // Check to see if this reaction already exists in the compartment
                if (!ReactionBuilder.CompartHasThisReaction(C.rtList, rt))
                {
                    // Check for all the necessary reactants and catalysts
                    tfReactants = HasAllReactants(C.Populations, rt);
                    tfModifiers = HasAllModifiers(C.Populations, rt);

                    if (tfReactants && tfModifiers)
                    {
                        // Add any missing products
                        if (AddNeededProducts(rt, C, MolDict))
                        {
                            changed = true;
                        }

                        // Add reaction and reaction template to Compartment
                        ReactionSwitch(C, rt);
                        changed = true;

                    }
                }

            }

            return changed;
        }

        public static bool CompartBoundaryReactions(Compartment C, List<ReactionTemplate> rtList, Dictionary<string, Molecule> MolDict)
        {
            bool changed = false;

            foreach (ReactionTemplate rt in rtList)
            {

            }

            return changed;
        }
    }

}


