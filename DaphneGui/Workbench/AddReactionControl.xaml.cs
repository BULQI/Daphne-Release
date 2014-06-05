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

            string[] wordList = this.FindResource("WordList") as string[];

            ICollectionView view = CollectionViewSource.GetDefaultView(wordList);

            new TextSearchFilter(view, this.txtSearch);
        }

        ///This is just some sample code
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
            if (lbMol2.SelectedItems.Count == 0)
                return;

            string reac = "";
            if (txtReac.Text.Length > 0)
                reac = " + ";

            foreach (var obj in lbMol2.SelectedItems)
            {
                if (obj.GetType() == typeof(ConfigMolecule))
                {
                    ConfigMolecule cm = obj as ConfigMolecule;
                    reacmolguids.Add(cm.molecule_guid);
                    reac += cm.Name;
                    reac += " + ";
                }
                else if (obj.GetType() == typeof(ConfigGene))
                {
                    ConfigGene cg = obj as ConfigGene;
                    reacmolguids.Add(cg.gene_guid);
                    reac += cg.Name;
                    reac += " + ";
                }
            }
            reac = reac.Substring(0, reac.Length - 3);

            txtReac.Text = txtReac.Text + reac;
            lbMol2.UnselectAll();

        }

        private void btnProd_Click(object sender, RoutedEventArgs e)
        {
            if (lbMol2.SelectedItems.Count == 0)
                return;

            string prod = "";
            if (txtProd.Text.Length > 0)
                prod = " + ";

            foreach (var obj in lbMol2.SelectedItems)
            {
                if (obj.GetType() == typeof(ConfigMolecule))
                {
                    ConfigMolecule cm = obj as ConfigMolecule;
                    prodmolguids.Add(cm.molecule_guid);
                    prod += cm.Name;
                    prod += " + ";
                }
                else if (obj.GetType() == typeof(ConfigGene))
                {
                    ConfigGene cg = obj as ConfigGene;
                    reacmolguids.Add(cg.gene_guid);
                    prod += cg.Name;
                    prod += " + ";
                }
            }

            prod = prod.Substring(0, prod.Length - 3);

            txtProd.Text = txtProd.Text + prod;
            lbMol2.UnselectAll();

        }

        private void btnUnselectAll_Click(object sender, RoutedEventArgs e)
        {
            lbMol2.UnselectAll();
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


        /// <summary>
        /// This is called when the user clicks Save in create reaction dialog
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            bool bValid = ParseUserInput();
            IdentifyModifiers();

            if (!bValid)
                return;

            string geneGuid = "";
            ConfigReaction cr = new ConfigReaction();
            cr.ReadOnly = false;
            cr.rate_const = inputRateConstant;

            cr.reaction_template_guid_ref = MainWindow.SC.SimConfig.IdentifyReactionType(inputReactants, inputProducts, inputModifiers);
            if (cr.reaction_template_guid_ref == "")
            {
                string msg = string.Format("Unsupported reaction.");
                MessageBox.Show(msg);
                return;
            }
            ConfigReactionTemplate crt = MainWindow.SC.SimConfig.entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref];

            // Don't have to add stoichiometry information since the reaction template knows it based on reaction type
            // For each list of reactants, products, and modifiers, add bulk then boundary molecules.
            // Transcription is the only reaction involving a gene. In that case the gene is a modifier.

            // Bulk Reactants
            foreach (KeyValuePair<string, int> kvp in inputReactants)
            {
                string guid = MainWindow.SC.SimConfig.findMoleculeGuidByName(kvp.Key);
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
                string molGuid = MainWindow.SC.SimConfig.findMoleculeGuidByName(kvp.Key);
                if (!cr.reactants_molecule_guid_ref.Contains(molGuid) && MainWindow.SC.SimConfig.entity_repository.molecules_dict[molGuid].molecule_location == MoleculeLocation.Boundary)
                {
                    cr.reactants_molecule_guid_ref.Add(molGuid);
                }
            }

            // Bulk Products
            foreach (KeyValuePair<string, int> kvp in inputProducts)
            {
                string guid = MainWindow.SC.SimConfig.findMoleculeGuidByName(kvp.Key);
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
                string molGuid = MainWindow.SC.SimConfig.findMoleculeGuidByName(kvp.Key);
                if (!cr.products_molecule_guid_ref.Contains(molGuid) && MainWindow.SC.SimConfig.entity_repository.molecules_dict[molGuid].molecule_location == MoleculeLocation.Boundary)
                {
                    cr.products_molecule_guid_ref.Add(molGuid);
                }
            }

            // Bulk modifiers
            foreach (KeyValuePair<string, int> kvp in inputModifiers)
            {
                string molGuid = MainWindow.SC.SimConfig.findMoleculeGuidByName(kvp.Key);
                geneGuid = MainWindow.SC.SimConfig.findGeneGuidByName(kvp.Key);
                if (molGuid == "" && geneGuid == "")  //this should never happen
                {
                    string msg = string.Format("Molecule/gene '{0}' does not exist in molecules library.  \nPlease first add the molecule to the molecules library and re-try.", kvp.Key);
                    MessageBox.Show(msg);
                    return;
                }
                if (molGuid != "")
                {
                    if (!cr.modifiers_molecule_guid_ref.Contains(molGuid) && MainWindow.SC.SimConfig.entity_repository.molecules_dict[molGuid].molecule_location == MoleculeLocation.Bulk)
                    {
                        cr.modifiers_molecule_guid_ref.Add(molGuid);
                    }
                }
                else if (geneGuid != "")
                {
                    if (!cr.modifiers_molecule_guid_ref.Contains(geneGuid))
                    {
                        cr.modifiers_molecule_guid_ref.Add(geneGuid);
                    }
                }
            }
            // Boundary modifiers
            foreach (KeyValuePair<string, int> kvp in inputModifiers)
            {
                string molGuid = MainWindow.SC.SimConfig.findMoleculeGuidByName(kvp.Key);
                if (molGuid != "")
                {
                    if (!cr.modifiers_molecule_guid_ref.Contains(molGuid) && MainWindow.SC.SimConfig.entity_repository.molecules_dict[molGuid].molecule_location == MoleculeLocation.Boundary)
                    {
                        cr.modifiers_molecule_guid_ref.Add(molGuid);
                    }
                }
            }

            //Generate the total string
            cr.GetTotalReactionString(MainWindow.SC.SimConfig.entity_repository);

            //Add the reaction to repository collection
            if (!MainWindow.SC.SimConfig.findReactionByTotalString(cr.TotalReactionString, MainWindow.SC.SimConfig))
            {
                MainWindow.SC.SimConfig.entity_repository.reactions.Add(cr);
            }
            else
            {
                string msg = string.Format("Reaction '{0}' already exists in reactions library.", cr.TotalReactionString);
                MessageBox.Show(msg);
                return;
            }

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

            if (phrase.Contains("-"))
            {
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

            if (phrase.Contains("-"))
            {
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
            string molGuid = MainWindow.SC.SimConfig.findMoleculeGuidByName(sMol);
            string geneGuid = MainWindow.SC.SimConfig.findGeneGuidByName(sMol);

            if (molGuid == "" && geneGuid == "")
            {
                string msg = string.Format("Molecule or gene '{0}' does not exist in molecules library.  \nPlease first add the molecule to the molecules library and re-try.", sMol);
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

        private bool HasMoleculeType(Dictionary<string, int> inputList, MoleculeLocation molLoc)
        {
            foreach (KeyValuePair<string, int> kvp in inputList)
            {
                string guid = MainWindow.SC.SimConfig.findMoleculeGuidByName(kvp.Key);
                // genes return guid = ""
                if (guid != "")
                {
                    if (MainWindow.SC.SimConfig.entity_repository.molecules_dict[guid].molecule_location == molLoc)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //New test code
            //FrameworkElementFactory lbMolsGenes = new FrameworkElementFactory(typeof(ListBox));
            //lbMolsGenes.Name = "MolsGenesListBox";

            CompositeCollection coll = new CompositeCollection();

            CollectionContainer cc = new CollectionContainer();
            cc.Collection = MainWindow.SC.SimConfig.entity_repository.molecules;
            coll.Add(cc);

            cc = new CollectionContainer();
            cc.Collection = MainWindow.SC.SimConfig.entity_repository.genes;
            coll.Add(cc);
            lbMol2.SetValue(ListBox.ItemsSourceProperty, coll);
            lbMol2.SetValue(ListBox.DisplayMemberPathProperty, "Name");
            //End test code
        }

        private void txtSearch_SelectionChanged(object sender, RoutedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            string searchText = tb.Text;
            searchText = searchText.Trim();
            searchText = searchText.ToLower();

            if (searchText.Length == 0)
                return;

            foreach (var item in lbMol2.Items)
            {
                string name = "";
                if (item.GetType() == typeof(ConfigMolecule))
                {
                    ConfigMolecule mol = (ConfigMolecule)item;
                    name = mol.Name;
                }
                else if (item.GetType() == typeof(ConfigGene))
                {
                    ConfigGene gene = (ConfigGene)item;
                    name = gene.Name;
                }
                name = name.ToLower();
                if (name.Contains(searchText))
                {
                    lbMol2.SelectedItem = item;
                    lbMol2.ScrollIntoView(lbMol2.SelectedItem);
                    break;
                }
            }
        }
    }
}
