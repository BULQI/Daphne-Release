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
using System.Collections.ObjectModel;

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

        public ConfigCompartment ARCComp
        {
            get { return (ConfigCompartment)GetValue(ARCCompProperty); }
            set { SetValue(ARCCompProperty, value); }
        }
        public static readonly DependencyProperty ARCCompProperty =
            DependencyProperty.Register("ARCComp", typeof(ConfigCompartment), typeof(AddReactionControl),
            new PropertyMetadata(new PropertyChangedCallback(ARCCompChanged)));
        private static void ARCCompChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            
        }

        public ConfigCell ARCCell
        {
            get { return (ConfigCell)GetValue(ARCCellProperty); }
            set { SetValue(ARCCellProperty, value); }
        }
        public static readonly DependencyProperty ARCCellProperty =
            DependencyProperty.Register("ARCCell", typeof(ConfigCell), typeof(AddReactionControl),
            new PropertyMetadata(new PropertyChangedCallback(ARCCellChanged)));
        private static void ARCCellChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }

        public ObservableCollection<ConfigReaction> ARCReactions
        {
            get { return (ObservableCollection<ConfigReaction>)GetValue(ARCReactionsProperty); }
            set { SetValue(ARCReactionsProperty, value); }
        }
        public static readonly DependencyProperty ARCReactionsProperty =
            DependencyProperty.Register("ARCReactions", typeof(ObservableCollection<ConfigReaction>), typeof(AddReactionControl),
            new PropertyMetadata(new PropertyChangedCallback(ARCReactionsChanged)));
        private static void ARCReactionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {

        }


        public static DependencyProperty CurrentReactionComplexProperty = DependencyProperty.Register("CurrentReactionComplex", typeof(ConfigReactionComplex), typeof(AddReactionControl), new FrameworkPropertyMetadata(null, CurrentReactionComplexPropertyChanged));
        public ConfigReactionComplex CurrentReactionComplex
        {
            get { return (ConfigReactionComplex)GetValue(CurrentReactionComplexProperty); }
            set
            {
                SetValue(CurrentReactionComplexProperty, value);
                OnPropertyChanged("CurrentReactionComplex");
            }
        }
        private static void CurrentReactionComplexPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            AddReactionControl arc = d as AddReactionControl;
            arc.CurrentReactionComplex = (ConfigReactionComplex)(e.NewValue);
        }

        ///
        //Notification handling
        /// 
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }


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

                    reacmolguids.Add(cm.entity_guid);
                    reac += cm.Name;
                    reac += " + ";
                }
                else if (obj.GetType() == typeof(ConfigGene))
                {
                    ConfigGene cg = obj as ConfigGene;

                    reacmolguids.Add(cg.entity_guid);
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

                    prodmolguids.Add(cm.entity_guid);
                    prod += cm.Name;
                    prod += " + ";
                }
                else if (obj.GetType() == typeof(ConfigGene))
                {
                    ConfigGene cg = obj as ConfigGene;

                    reacmolguids.Add(cg.entity_guid);
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
            if (!bValid)
                return;

            IdentifyModifiers();

            string geneGuid = "";
            ConfigReaction cr = new ConfigReaction();
            cr.rate_const = inputRateConstant;

            cr.reaction_template_guid_ref = MainWindow.SOP.Protocol.IdentifyReactionType(inputReactants, inputProducts, inputModifiers);
            if (cr.reaction_template_guid_ref == "")
            {
                string msg = string.Format("Unsupported reaction.");
                MessageBox.Show(msg, "Unsupported reaction", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            ConfigReactionTemplate crt = MainWindow.SOP.Protocol.entity_repository.reaction_templates_dict[cr.reaction_template_guid_ref];

            // Don't have to add stoichiometry information since the reaction template knows it based on reaction type
            // For each list of reactants, products, and modifiers, add bulk then boundary molecules.
            // Transcription is the only reaction involving a gene. In that case the gene is a modifier.

            // Bulk Reactants
            foreach (KeyValuePair<string, int> kvp in inputReactants)
            {
                string guid = MainWindow.SOP.Protocol.findMoleculeGuidByName(kvp.Key);
                if (guid == "")  //this should never happen
                {
                    string msg = string.Format("Molecule '{0}' does not exist in molecules library.  \nPlease first add the molecule to the molecules library and re-try.", kvp.Key);
                    MessageBox.Show(msg);
                    return;
                }
                if (!cr.reactants_molecule_guid_ref.Contains(guid) && MainWindow.SOP.Protocol.entity_repository.molecules_dict[guid].molecule_location == MoleculeLocation.Bulk)
                {
                    cr.reactants_molecule_guid_ref.Add(guid);
                }
            }
            // Boundary Reactants
            foreach (KeyValuePair<string, int> kvp in inputReactants)
            {
                string molGuid = MainWindow.SOP.Protocol.findMoleculeGuidByName(kvp.Key);
                if (!cr.reactants_molecule_guid_ref.Contains(molGuid) && MainWindow.SOP.Protocol.entity_repository.molecules_dict[molGuid].molecule_location == MoleculeLocation.Boundary)
                {
                    cr.reactants_molecule_guid_ref.Add(molGuid);
                }
            }

            // Bulk Products
            foreach (KeyValuePair<string, int> kvp in inputProducts)
            {
                string guid = MainWindow.SOP.Protocol.findMoleculeGuidByName(kvp.Key);
                if (guid == "")  //this should never happen
                {
                    string msg = string.Format("Molecule '{0}' does not exist in molecules library.  \nPlease first add the molecule to the molecules library and re-try.", kvp.Key);
                    MessageBox.Show(msg);
                    return;
                }
                if (!cr.products_molecule_guid_ref.Contains(guid) && MainWindow.SOP.Protocol.entity_repository.molecules_dict[guid].molecule_location == MoleculeLocation.Bulk)
                {
                    cr.products_molecule_guid_ref.Add(guid);
                }
            }
            // Boundary Products
            foreach (KeyValuePair<string, int> kvp in inputProducts)
            {
                string molGuid = MainWindow.SOP.Protocol.findMoleculeGuidByName(kvp.Key);
                if (!cr.products_molecule_guid_ref.Contains(molGuid) && MainWindow.SOP.Protocol.entity_repository.molecules_dict[molGuid].molecule_location == MoleculeLocation.Boundary)
                {
                    cr.products_molecule_guid_ref.Add(molGuid);
                }
            }

            // Bulk modifiers
            foreach (KeyValuePair<string, int> kvp in inputModifiers)
            {
                string molGuid = MainWindow.SOP.Protocol.findMoleculeGuidByName(kvp.Key);
                geneGuid = MainWindow.SOP.Protocol.findGeneGuidByName(kvp.Key);
                if (molGuid == "" && geneGuid == "")  //this should never happen
                {
                    string msg = string.Format("Molecule/gene '{0}' does not exist in molecules library.  \nPlease first add the molecule to the molecules library and re-try.", kvp.Key);
                    MessageBox.Show(msg);
                    return;
                }
                if (molGuid != "")
                {
                    if (!cr.modifiers_molecule_guid_ref.Contains(molGuid) && MainWindow.SOP.Protocol.entity_repository.molecules_dict[molGuid].molecule_location == MoleculeLocation.Bulk)
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
                string molGuid = MainWindow.SOP.Protocol.findMoleculeGuidByName(kvp.Key);
                if (molGuid != "")
                {
                    if (!cr.modifiers_molecule_guid_ref.Contains(molGuid) && MainWindow.SOP.Protocol.entity_repository.molecules_dict[molGuid].molecule_location == MoleculeLocation.Boundary)
                    {
                        cr.modifiers_molecule_guid_ref.Add(molGuid);
                    }
                }
            }

            //Generate the total string
            cr.GetTotalReactionString(MainWindow.SOP.Protocol.entity_repository);

            //Validate the reaction for its specific environment before adding

            bool isBoundaryReaction = false;
            if (cr.IsBoundaryReaction(MainWindow.SOP.Protocol.entity_repository) == true)
            {
                isBoundaryReaction = true;
            }

            if (ARCReactions != null)
            {
                if (HasReaction(cr, ARCReactions) == true)
                {
                    return;
                }
            }

            bool wasAdded = false;
            string reacEnvironment = Tag as string;

            switch(reacEnvironment)
            {
                case "ecs":
                    TissueScenario ts = (TissueScenario)MainWindow.SOP.Protocol.scenario;
                    ConfigECSEnvironment configEcs = (ConfigECSEnvironment)ts.environment;
                    if (configEcs.ValidateReaction(cr, MainWindow.SOP.Protocol) == false)
                    {
                        MessageBox.Show("Not a valid reaction for this environment."); 
                        return;
                    }
 
                    break;

                case "cytosol":
                    ObservableCollection<string> bulkMols = cr.GetBulkMolecules(MainWindow.SOP.Protocol.entity_repository);
                    if (bulkMols.Count < 1)
                    {
                        MessageBox.Show("Not a valid reaction for this environment.");
                        return;
                    }
                    break;


                case "membrane":
                    if (isBoundaryReaction == true)
                    {
                        MessageBox.Show("Boundary reactions are not supported in this environment.");
                        return;
                    }
                    break;

                case "vatRC":
                    if (isBoundaryReaction == true)
                    {
                        MessageBox.Show("Boundary reactions are not supported in this environment.");
                        return;
                    }
                    else
                    {
                        VatReactionComplexScenario s = MainWindow.SOP.Protocol.scenario as VatReactionComplexScenario;
                        ConfigReactionComplex crc = DataContext as ConfigReactionComplex;
                        crc.AddReactionMolPopsAndGenes(cr, MainWindow.SOP.Protocol.entity_repository);
                        s.InitializeAllMols();
                        s.InitializeAllReacs();
                    }
                    break;

                case "component_reacs":
                    break;

                case "component_rc":
                    this.CurrentReactionComplex.AddReactionMolPopsAndGenes(cr, MainWindow.SOP.Protocol.entity_repository);
                    break;

                default:
                    return;    
            }

            ARCReactions.Add(cr);
            wasAdded = true;

            //Add the reaction to repository collection if it doesn't already exist there.
            if (!MainWindow.SOP.Protocol.findReactionByTotalString(cr.TotalReactionString, MainWindow.SOP.Protocol) && wasAdded)
            {
                MainWindow.SOP.Protocol.entity_repository.reactions.Add(cr);
            }

            txtReac.Text = "";
            reacmolguids.Clear();
            txtProd.Text = "";
            prodmolguids.Clear();
        }

        private bool HasReaction(ConfigReaction cr, ObservableCollection<ConfigReaction> reactions)
        {
            if (cr == null || reactions == null)
            {
                return false;
            }

            // If this reaction already exists in the ObservableCollection, then don't add
            if (MainWindow.SOP.Protocol.findReactionByTotalString(cr.TotalReactionString, reactions) == true)
            {
                MessageBox.Show("This reaction already exists in this environment.");
                return true;
            }
            else
            {
                return false;
            }
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
            phrase = phrase.Replace(" ", "");

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
            phrase = phrase.Replace(" ", "");

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
            string molGuid = MainWindow.SOP.Protocol.findMoleculeGuidByName(sMol);
            string geneGuid = MainWindow.SOP.Protocol.findGeneGuidByName(sMol);

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
                string guid = MainWindow.SOP.Protocol.findMoleculeGuidByName(kvp.Key);
                // genes return guid = ""
                if (guid != "")
                {
                    if (MainWindow.SOP.Protocol.entity_repository.molecules_dict[guid].molecule_location == molLoc)
                    {
                        return true;
                    }
                }
            }
            return false;
        }


        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            this.BringIntoView();
            populateCollection();
        }

        private void populateCollection()
        {
            CompositeCollection coll = new CompositeCollection();
            CollectionContainer cc = new CollectionContainer();
            Level level;
            string reacEnvironment = Tag as string;
            switch (reacEnvironment)
            {
                case "ecs":
                    TissueScenario ts = (TissueScenario)MainWindow.SOP.Protocol.scenario;
                    ConfigECSEnvironment configEcs = (ConfigECSEnvironment)ts.environment;
                    ARCComp = configEcs.comp;
                    ARCCell = null;
                    if (ARCComp != null)
                    {
                        ARCReactions = ARCComp.Reactions ?? null;
                    }
                    if (configEcs != null)
                    {
                        //cc.Collection = configEcs.comp.molecules_dict.Values.ToArray();
                        cc.Collection = ARCComp.molecules_dict.Values.ToArray();
                        coll.Add(cc);
                        foreach (CellPopulation cellpop in ts.cellpopulations)
                        {
                            cc = new CollectionContainer();
                            cc.Collection = cellpop.Cell.membrane.molecules_dict.Values.ToArray();
                            coll.Add(cc);
                        }
                    }
                    break;

                case "cytosol":
                    ARCCell = this.DataContext as ConfigCell;
                    if (ARCCell != null)
                    {
                        ARCComp = ARCCell.cytosol;
                        ARCReactions = ARCComp.Reactions;
                        cc.Collection = ARCCell.cytosol.molecules_dict.Values.ToArray();
                        coll.Add(cc);
                        cc = new CollectionContainer();
                        cc.Collection = ARCCell.membrane.molecules_dict.Values.ToArray();
                        coll.Add(cc);
                        cc = new CollectionContainer();
                        cc.Collection = ARCCell.genes;
                        coll.Add(cc);
                    }
                    break;

                case "membrane":
                    ARCCell = this.DataContext as ConfigCell;
                    if (ARCCell != null)
                    {
                        ARCComp = ARCCell.membrane;
                        ARCReactions = ARCComp.Reactions;
                        cc.Collection = ARCComp.molecules_dict.Values.ToArray();
                        coll.Add(cc);
                    }
                    break;

                case "vatRC":
                    ARCCell = null;
                    ARCComp = null;                  
                    ConfigReactionComplex crc = this.DataContext as ConfigReactionComplex;
                    if (crc != null)
                    {
                        ARCReactions = crc.reactions;
                        cc.Collection = MainWindow.SOP != null ? MainWindow.SOP.Protocol.entity_repository.molecules : null;
                        coll.Add(cc);
                    }
                    break;

                case "component_reacs":
                    ARCCell = null;
                    ARCComp = null;
                    level = this.DataContext as Level;
                    if (level != null)
                    {
                        ARCReactions = level.entity_repository.reactions;
                        cc.Collection = level.entity_repository.molecules;
                        coll.Add(cc);

                        cc = new CollectionContainer();
                        cc.Collection = level.entity_repository.genes;
                        coll.Add(cc);
                    }
                    break;

                case "component_rc":
                    ARCCell = null;
                    ARCComp = null;                   
                    level = this.DataContext as Level;
                    crc = this.CurrentReactionComplex as ConfigReactionComplex;
                    if (crc != null)
                    {                        
                        ARCReactions = crc.reactions;
                        cc.Collection = level != null ? level.entity_repository.molecules : null;
                        coll.Add(cc);

                        cc = new CollectionContainer();
                        cc.Collection = level != null ? level.entity_repository.genes : null;
                        coll.Add(cc);
                    }
                    break;

                default:
                    break;

            }

            lbMol2.SetValue(ListBox.ItemsSourceProperty, coll);
            lbMol2.SetValue(ListBox.DisplayMemberPathProperty, "Name");
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

        private void btnCreateNewMol_Click(object sender, RoutedEventArgs e)
        {
            string environment = this.Tag as string;
            if ((environment == "membrane" || environment == "cytosol") && ((this.DataContext as ConfigCell) == null))
            {
                MessageBox.Show("You must first select a cell. If no cell exists, you need to add one.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ConfigMolecule newLibMol = new ConfigMolecule();
            newLibMol.Name = newLibMol.GenerateNewName(MainWindow.SOP.Protocol, "_New");
            AddEditMolecule aem = new AddEditMolecule(newLibMol, MoleculeDialogType.NEW);
            aem.Tag = this.Tag;

            //do if user did not cancel from dialog box
            if (aem.ShowDialog() == true)
            {
                //Add new mol to Protocol
                newLibMol.ValidateName(MainWindow.SOP.Protocol);
                MainWindow.SOP.Protocol.entity_repository.molecules.Add(newLibMol);
                
                //Need to add a mol pop to cell also
                if (environment == "membrane")
                {
                    ConfigCell cell = this.DataContext as ConfigCell;
                    if (cell.membrane.HasMolecule(newLibMol) == false)
                    {
                        bool isCell = true;
                        cell.membrane.AddMolPop(newLibMol, isCell);
                        populateCollection();
                    }
                }
                else if (environment == "cytosol")
                {
                    ConfigCell cell = this.DataContext as ConfigCell;
                    if (cell.cytosol.HasMolecule(newLibMol) == false)
                    {
                        bool isCell = true;
                        cell.cytosol.AddMolPop(newLibMol, isCell);
                        populateCollection();
                    }
                }

                //Add new mol pop to the ecs
                else if (environment == "ecs")
                {                    
                    bool isCell = false;                    
                    MainWindow.SOP.Protocol.scenario.environment.comp.AddMolPop(newLibMol, isCell);
                    populateCollection();
                }
            }
        }

        /// <summary>
        /// Must update dependency properties after data context change
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            populateCollection();
        }
        
    }
}
