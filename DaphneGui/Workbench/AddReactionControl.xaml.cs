using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using Daphne;
using System.Diagnostics;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for AddReactionControl.xaml
    /// </summary>
    public partial class AddReactionControl : UserControl
    {
        private List<string> reacmolguids;
        private List<string> prodmolguids;
        private Dictionary<string, int> inputReactants;
        private Dictionary<string, int> inputProducts;
        private Dictionary<string, int> inputModifiers;
        public double inputRateConstant { get; set; }
        
        public AddReactionControl()
        {
            InitializeComponent();

            reacmolguids = new List<string>();
            prodmolguids = new List<string>();
            inputReactants = new Dictionary<string, int>();
            inputProducts = new Dictionary<string, int>();
            inputModifiers = new Dictionary<string, int>();
            inputRateConstant = 2.0;

            //InitializeWordList();



            string[] wordList =
                this.FindResource("WordList") as string[];

            ICollectionView view =
                CollectionViewSource.GetDefaultView(wordList);

            new TextSearchFilter(view, this.txtSearch);
        }

        ////private void InitializeWordList()
        ////{
        ////    //string[] wordList;
        ////    List<string> molnames = new List<string>();

        ////    foreach (ConfigMolecule cm in lbMol.Items)
        ////    {
        ////        molnames.Add(cm.Name);
        ////    }            

        ////    string[] wordList = molnames.ToArray();


        ////    //    this.FindResource("WordList") as string[];

        ////    ICollectionView view = CollectionViewSource.GetDefaultView(wordList);

        ////    new TextSearchFilter(view, this.txtSearch);
        ////}

        private void btnReac_Click(object sender, RoutedEventArgs e)
        {
            if (lbMol.SelectedItems.Count == 0)
                return;

            string reac = "";
            if (txtReac.Text.Length > 0)
                reac = " + ";
            foreach (ConfigMolecule cm in lbMol.SelectedItems)
            {
                reacmolguids.Add(cm.molecule_guid);
                reac += cm.Name;
                reac += " + ";
            }
            reac = reac.Substring(0, reac.Length - 3);

            txtReac.Text = txtReac.Text + reac;
            lbMol.UnselectAll();
        }

        private void btnProd_Click(object sender, RoutedEventArgs e)
        {
            if (lbMol.SelectedItems.Count == 0)
                return;

            string prod = "";
            if (txtProd.Text.Length > 0)
                prod = " + ";
            foreach (ConfigMolecule cm in lbMol.SelectedItems)
            {
                prodmolguids.Add(cm.molecule_guid);
                prod += cm.Name;
                prod += " + ";
            }
            prod = prod.Substring(0, prod.Length - 3);

            txtProd.Text = txtProd.Text + prod;
            lbMol.UnselectAll();
        }

        private void btnUnselectAll_Click(object sender, RoutedEventArgs e)
        {
            lbMol.UnselectAll();
        }
        
        private void btnRClear_Click(object sender, RoutedEventArgs e)
        {
            txtReac.Text = "";
            reacmolguids.Clear();
        }

        private void btnPClear_Click(object sender, RoutedEventArgs e)
        {
            txtProd.Text = "";
            prodmolguids.Clear();
        }

        // given a reaction type, find its guid
        private static string findReactionTemplateGuid(ReactionType rt, SimConfiguration sc)
        {
            foreach (ConfigReactionTemplate crt in sc.entity_repository.reaction_templates)
            {
                if (crt.reac_type == rt)
                {
                    return crt.reaction_template_guid;
                }
            }
            return null;
        }

        // given a molecule name, check if it exists in repository - return
        private static string findMoleculeGuidByName(string inputMolName)
        {
            string guid = "";
            foreach (ConfigMolecule cm in MainWindow.SC.SimConfig.entity_repository.molecules)
            {
                if (cm.Name == inputMolName)
                {
                    guid = cm.molecule_guid;
                    break;
                }
            }
            return guid;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            bool bValid = ParseUserInput();
            IdentifyModifiers();

            if (!bValid)
                return;

            ConfigReaction cr = new ConfigReaction();
            cr.ReadOnly = false;
            cr.rate_const = inputRateConstant;

            cr.reaction_template_guid_ref = IdentifyReactionType();
            if (cr.reaction_template_guid_ref == "")
            {
                string msg = string.Format("Unsupported reaction.");
                MessageBox.Show(msg);
                return;
            }
            ConfigReactionTemplate crt = MainWindow.SC.SimConfig.entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref];

            // Don't have to add stoichiometry information since the reaction template knows it based on reaction type
            // For each list of reactants, products, and modifiers, add bulk then boundary molecules.

            // Bulk Reactants
            foreach (KeyValuePair<string, int> kvp in inputReactants)
            {
                string guid = findMoleculeGuidByName(kvp.Key);
                if (guid == "")  //this should never happen
                {
                    string msg = string.Format("Molecule '{0}' does not exist in molecules library.  \nPlease first add the molecule to the molecules library and re-try.", kvp.Key);
                    MessageBox.Show(msg);
                    return;
                }
                if (!cr.reactants_molecule_guid_ref.Contains(guid) && MainWindow.SC.SimConfig.entity_repository.molecules_dict[guid].molecule_location == MoleculeLocation.Bulk)
                {
                    cr.reactants_molecule_guid_ref.Add(guid);
                }
            }
            // Boundary Reactants
            foreach (KeyValuePair<string, int> kvp in inputReactants)
            {
                string guid = findMoleculeGuidByName(kvp.Key);
                if (!cr.reactants_molecule_guid_ref.Contains(guid) && MainWindow.SC.SimConfig.entity_repository.molecules_dict[guid].molecule_location == MoleculeLocation.Boundary)
                {
                    cr.reactants_molecule_guid_ref.Add(guid);
                }
            }

            // Bulk Products
            foreach (KeyValuePair<string, int> kvp in inputProducts)
            {
                string guid = findMoleculeGuidByName(kvp.Key);
                if (guid == "")  //this should never happen
                {
                    string msg = string.Format("Molecule '{0}' does not exist in molecules library.  \nPlease first add the molecule to the molecules library and re-try.", kvp.Key);
                    MessageBox.Show(msg);
                    return;
                }
                if (!cr.products_molecule_guid_ref.Contains(guid) && MainWindow.SC.SimConfig.entity_repository.molecules_dict[guid].molecule_location == MoleculeLocation.Bulk)
                {
                    cr.products_molecule_guid_ref.Add(guid);
                }
            }
            // Boundary Products
            foreach (KeyValuePair<string, int> kvp in inputProducts)
            {
                string guid = findMoleculeGuidByName(kvp.Key);
                if (!cr.products_molecule_guid_ref.Contains(guid) && MainWindow.SC.SimConfig.entity_repository.molecules_dict[guid].molecule_location == MoleculeLocation.Boundary)
                {
                    cr.products_molecule_guid_ref.Add(guid);
                }
            }

            // Bulk modifiers
            foreach (KeyValuePair<string, int> kvp in inputModifiers)
            {
                string guid = findMoleculeGuidByName(kvp.Key);
                if (guid == "")  //this should never happen
                {
                    string msg = string.Format("Molecule '{0}' does not exist in molecules library.  \nPlease first add the molecule to the molecules library and re-try.", kvp.Key);
                    MessageBox.Show(msg);
                    return;
                }
                if (!cr.modifiers_molecule_guid_ref.Contains(guid) && MainWindow.SC.SimConfig.entity_repository.molecules_dict[guid].molecule_location == MoleculeLocation.Bulk)
                {
                    cr.modifiers_molecule_guid_ref.Add(guid);
                }
            }
            // Boundary modifiers
            foreach (KeyValuePair<string, int> kvp in inputModifiers)
            {
                string guid = findMoleculeGuidByName(kvp.Key);
                if (!cr.modifiers_molecule_guid_ref.Contains(guid) && MainWindow.SC.SimConfig.entity_repository.molecules_dict[guid].molecule_location == MoleculeLocation.Boundary)
                {
                    cr.modifiers_molecule_guid_ref.Add(guid);
                }
            }

            //Add the reaction to repository collection
            MainWindow.SC.SimConfig.entity_repository.reactions.Add(cr);

            txtReac.Text = "";
            reacmolguids.Clear();
            txtProd.Text = "";
            prodmolguids.Clear();
        }

        private bool ParseUserInput()
        {
            bool retval = true;
            
            inputReactants.Clear();
            inputProducts.Clear();
            inputModifiers.Clear();

            //THIS CODE PARSES REACTION INPUT BY USER
            //LEFT SIDE
            string phrase = txtReac.Text;

            if (phrase.Contains("-")) {
                string msg = string.Format("Reactants field contains invalid character '-'.  \nPlease fix and re-try.");
                MessageBox.Show(msg);
                txtReac.Focus();
                return false;
            }

            string[] tokensLeft;
            string[] stringSeparators = new string[] { "+" };

            tokensLeft = phrase.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
            int count = tokensLeft.Count();

            for (int i = 0; i < count; i++)
            {
                tokensLeft[i] = tokensLeft[i].Trim();
            }

            //RIGHT SIDE
            phrase = txtProd.Text;

            if (phrase.Contains("-")) {
                string msg = string.Format("Products field contains invalid character '-'.  \nPlease fix and re-try.");
                MessageBox.Show(msg);
                txtProd.Focus();
                return false;
            }

            string[] tokensRight;

            tokensRight = phrase.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
            count = tokensRight.Count();

            for (int i = 0; i < count; i++)
            {
                tokensRight[i] = tokensRight[i].Trim();
            }

            //NOW IF THERE ARE COEFFICIENTS, LIKE N, STRIP THOSE OFF AND SAVE THEM

            foreach (string str in tokensLeft)
            {
                string sMol = str.TrimStart('1', '2', '3', '4', '5', '6', '7', '8', '9', '0');
                int len1 = sMol.Length;
                int len2 = str.Length;
                int diff = len2 - len1;

                if (!ValidateMoleculeName(sMol))
                    return false;

                if (diff == 0)
                {
                    if (!inputReactants.ContainsKey(sMol))
                        inputReactants.Add(sMol, 1);
                    else
                        inputReactants[sMol] += 1;
                }
                else
                {
                    string sCoeff = str.Substring(0, diff);
                    int nCoeff = int.Parse(sCoeff);

                    if (nCoeff <= 0)
                    {
                        //error and return
                        MessageBox.Show("Reactants field contains invalid stoichiometric coefficient.  Please fix this and re-try.");
                        return false;
                    }
                    if (!inputReactants.ContainsKey(sMol))
                        inputReactants.Add(sMol, nCoeff);
                    else
                        inputReactants[sMol] += nCoeff;
                    
                }
            }

            foreach (string str in tokensRight)
            {
                string sMol = str.TrimStart('1', '2', '3', '4', '5', '6', '7', '8', '9', '0');
                int len1 = sMol.Length;
                int len2 = str.Length;
                int diff = len2 - len1;
                
                if (!ValidateMoleculeName(sMol))
                    return false;

                if (diff == 0)
                {
                    if (!inputProducts.ContainsKey(sMol))
                        inputProducts.Add(sMol, 1);
                    else
                        inputProducts[sMol] += 1;
                }
                else
                {
                    string sCoeff = str.Substring(0, diff);
                    int nCoeff = int.Parse(sCoeff);

                    if (nCoeff <= 0)
                    {
                        //error and return
                        MessageBox.Show("Products field contains invalid stoichiometric coefficient.  Please fix this and re-try.");
                        return false;
                    }
                    if (!inputProducts.ContainsKey(sMol))
                        inputProducts.Add(sMol, nCoeff);
                    else
                        inputProducts[sMol] += nCoeff;

                }
            }

            //Check rate constant
            string rate = txtRate.Text.Trim();
            if (rate.Length <= 0)
            {
                MessageBox.Show("Please enter a rate constant.");
                txtRate.Focus();
                return false;
            }

            double dRate;
            bool bRate = double.TryParse(rate, out dRate);

            if (bRate == false)
            {
                MessageBox.Show("Invalid rate constant entered.");
                txtRate.Focus();
                return false;
            }

            inputRateConstant = dRate; //double.Parse(rate);

            return retval;
        }

        private bool ValidateMoleculeName(string sMol)
        {
            string molGuid = findMoleculeGuidByName(sMol);
            if (molGuid == "")
            {
                string msg = string.Format("Molecule '{0}' does not exist in molecules library.  \nPlease first add the molecule to the molecules library and re-try.", sMol);
                MessageBox.Show(msg);
                return false;
            }
            return true;
        }

        private void IdentifyModifiers()
        {
            if (inputReactants == null || inputProducts == null)
            {
                return;
            }

            foreach (KeyValuePair<string, int> kvpReac in inputReactants)
            {
                foreach (KeyValuePair<string, int> kvpProd in inputProducts)
                {
                    if ((kvpProd.Key == kvpReac.Key) && (kvpProd.Value == kvpReac.Value))
                    {
                        inputModifiers.Add(kvpReac.Key, kvpReac.Value);
                    }
                }
            }
            foreach (KeyValuePair<string, int> kvp in inputModifiers)
            {
                inputReactants.Remove(kvp.Key);
                inputProducts.Remove(kvp.Key);
            }
        }

        private bool HasMoleculeType(Dictionary<string,int> inputList, MoleculeLocation molLoc)
        {
            foreach (KeyValuePair<string, int> kvp in inputList)
            {
                string guid = findMoleculeGuidByName(kvp.Key);
                if (MainWindow.SC.SimConfig.entity_repository.molecules_dict[guid].molecule_location == molLoc)
                {
                    return true;
                }
            }
            return false;
        }
        
        private string IdentifyReactionType()
        {
            string reaction_template_guid_ref = "";

            bool boundReac = HasMoleculeType(inputReactants, MoleculeLocation.Boundary);
            bool bulkReac = HasMoleculeType(inputReactants, MoleculeLocation.Bulk);
            int totalReacStoich = 0;
            foreach (KeyValuePair<string, int> kvp in inputReactants)
            {
                totalReacStoich += kvp.Value;
            }

            bool boundProd = HasMoleculeType(inputProducts, MoleculeLocation.Boundary);
            bool bulkProd = HasMoleculeType(inputProducts, MoleculeLocation.Bulk);
            int totalProdStoich = 0;
            foreach (KeyValuePair<string, int> kvp in inputProducts)
            {
                totalProdStoich += kvp.Value;
            }

            bool boundMod = HasMoleculeType(inputModifiers, MoleculeLocation.Boundary);
            bool bulkMod = HasMoleculeType(inputModifiers, MoleculeLocation.Bulk);
            int totalModStoich = 0;
            foreach (KeyValuePair<string, int> kvp in inputModifiers)
            {
                totalModStoich += kvp.Value;
            }

            int     bulkBoundVal = 1,
                    modVal = 10,            
                    reacVal = 100,
                    prodVal = 1000,
                    reacStoichVal = 10000,
                    prodStoichVal = 100000,
                    modStoichVal = 1000000;

            if (inputModifiers.Count > 9 || inputReactants.Count > 9 || inputProducts.Count > 9 || totalReacStoich > 9 || totalProdStoich > 9 || totalModStoich > 9)
            {
                throw new Exception("Unsupported reaction with current typing algorithm.\n");
            }

            int reacNum = inputModifiers.Count * modVal
                            + inputReactants.Count * reacVal
                            + inputProducts.Count * prodVal
                            + totalReacStoich * reacStoichVal
                            + totalProdStoich * prodStoichVal
                            + totalModStoich * modStoichVal;
    
            if ((boundReac || boundProd || boundMod) && (bulkReac || bulkProd || bulkMod))
            {
                reacNum += bulkBoundVal;
            }

            switch (reacNum)
            {
                // Interior
                case 10100:
                    return findReactionTemplateGuid(ReactionType.Annihilation, MainWindow.SC.SimConfig);
                case 121200:
                    return findReactionTemplateGuid(ReactionType.Association, MainWindow.SC.SimConfig);
                case 121100:
                    return findReactionTemplateGuid(ReactionType.Dimerization, MainWindow.SC.SimConfig);
                case 211100:
                    return findReactionTemplateGuid(ReactionType.DimerDissociation, MainWindow.SC.SimConfig);
                case 212100:
                    return findReactionTemplateGuid(ReactionType.Dissociation, MainWindow.SC.SimConfig);
                case 111100:
                    return findReactionTemplateGuid(ReactionType.Transformation, MainWindow.SC.SimConfig);
                case 221200:
                    return findReactionTemplateGuid(ReactionType.AutocatalyticTransformation, MainWindow.SC.SimConfig);
                // Interior Catalyzed (catalyst stoichiometry doesn't change)
                case 1010110:
                    return findReactionTemplateGuid(ReactionType.CatalyzedAnnihilation, MainWindow.SC.SimConfig);
                case 1121210:
                    return findReactionTemplateGuid(ReactionType.CatalyzedAssociation, MainWindow.SC.SimConfig);
                case 1101010:
                    return findReactionTemplateGuid(ReactionType.CatalyzedCreation, MainWindow.SC.SimConfig);
                case 1121110:
                    return findReactionTemplateGuid(ReactionType.CatalyzedDimerization, MainWindow.SC.SimConfig);
                case 1211110:
                    return findReactionTemplateGuid(ReactionType.CatalyzedDimerDissociation, MainWindow.SC.SimConfig);
                case 1212110:
                    return findReactionTemplateGuid(ReactionType.CatalyzedDissociation, MainWindow.SC.SimConfig);
                case 1111110:
                    return findReactionTemplateGuid(ReactionType.CatalyzedTransformation, MainWindow.SC.SimConfig);
                // Bulk/Boundary reactions
                case 121201:
                    if ((boundProd) && (boundReac))
                    {
                        // The product and one of the reactants must be boundary molecules 
                        return findReactionTemplateGuid(ReactionType.BoundaryAssociation, MainWindow.SC.SimConfig);
                    }
                    else
                    {
                        return reaction_template_guid_ref;
                    }
                case 212101:
                    if ((boundProd) && (boundReac))
                    {
                        // The reactant and one of the products must be boundary molecules 
                        return findReactionTemplateGuid(ReactionType.BoundaryDissociation, MainWindow.SC.SimConfig);
                    }
                    else
                    {
                        return reaction_template_guid_ref;
                    }
                case 111101:
                    if (boundReac)
                    {
                        return findReactionTemplateGuid(ReactionType.BoundaryTransportFrom, MainWindow.SC.SimConfig);
                    }
                    else
                    {
                        return findReactionTemplateGuid(ReactionType.BoundaryTransportTo, MainWindow.SC.SimConfig);
                    }
                // Catalyzed Bulk/Boundary reactions
                case 1111111:
                    if (boundMod)
                    {
                        return findReactionTemplateGuid(ReactionType.CatalyzedBoundaryActivation, MainWindow.SC.SimConfig);
                    }
                    else
                    {
                        return reaction_template_guid_ref;
                    }
                // Generalized reaction
                default:
                    // Not implemented yet
                    return reaction_template_guid_ref;
            }
        }

        private void txtSearch_SelectionChanged(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            string searchText = tb.Text;
            searchText = searchText.Trim();
            searchText = searchText.ToLower();

            foreach (var item in lbMol.Items)
            {
                ConfigMolecule mol = (ConfigMolecule)item;
                string name = mol.Name;
                name = name.ToLower();
                if (name.Contains(searchText))
                {
                    lbMol.SelectedItem = item;
                    lbMol.ScrollIntoView(lbMol.SelectedItem);
                    break;
                }
            }
        }
    }
}
