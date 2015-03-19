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
using Daphne;
using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;
using DaphneGui.Pushing;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for CellDetailsReadOnlyControl.xaml
    /// </summary>
    public partial class CellDetailsReadOnlyControl : UserControl
    {
        private Level CurrentLevel = null;

        public CellDetailsReadOnlyControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This method is only needed if user goes to "Stores" and selects "Cells" and then selects a cell.
        /// </summary>
        /// <param name="currLevel"></param>
        public void SetCurrentLevel(Level currLevel)
        {
            CurrentLevel = currLevel;
        }

        private void memb_molecule_combo_box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)e.Source;
            if (cb == null)
                return;

            ConfigMolecularPopulation molpop = (ConfigMolecularPopulation)CellMembraneMolPopsListBox.SelectedItem;

            if (molpop == null)
                return;

            string curr_mol_pop_name = molpop.Name;
            string curr_mol_guid = "";
            curr_mol_guid = molpop.molecule.entity_guid;

            int nIndex = cb.SelectedIndex;
            if (nIndex < 0)
                return;

            Level level = MainWindow.GetLevelContext(this);
            if (level == null)
            {
                level = CurrentLevel;
            }
            //if user picked 'new molecule' then create new molecule in ER
            if (nIndex == (cb.Items.Count - 1))
            {
                ConfigMolecule newLibMol = new ConfigMolecule();
                //newLibMol.Name = newLibMol.GenerateNewName(MainWindow.SOP.Protocol, "_New");
                newLibMol.Name = newLibMol.GenerateNewName(level, "_New");
                newLibMol.molecule_location = MoleculeLocation.Boundary;
                AddEditMolecule aem = new AddEditMolecule(newLibMol, MoleculeDialogType.NEW);
                aem.Tag = DataContext as ConfigCell;

                //if user cancels out of new molecule dialog, set selected molecule back to what it was
                if (aem.ShowDialog() == false)
                {
                    if (e.RemovedItems.Count > 0)
                    {
                        cb.SelectedItem = e.RemovedItems[0];
                    }
                    else
                    {
                        cb.SelectedIndex = 0;
                    }
                    return;
                }
                //newLibMol.ValidateName(MainWindow.SOP.Protocol);
                newLibMol.ValidateName(level);
                //MainWindow.SOP.Protocol.entity_repository.molecules.Add(newLibMol);
                level.entity_repository.molecules.Add(newLibMol);
                molpop.molecule = newLibMol.Clone(null);
                molpop.Name = newLibMol.Name;

                CollectionViewSource.GetDefaultView(cb.ItemsSource).Refresh();
                cb.SelectedItem = newLibMol;
            }
            //user picked an existing molecule
            else
            {
                ConfigMolecule newmol = (ConfigMolecule)cb.SelectedItem;

                //if molecule has not changed, return
                if (newmol.entity_guid == curr_mol_guid)
                {
                    return;
                }

                //if molecule changed, then make a clone of the newly selected one from entity repository
                ConfigMolecule mol = (ConfigMolecule)cb.SelectedItem;
                molpop.molecule = mol.Clone(null);

                string new_mol_name = mol.Name;
                if (curr_mol_guid != molpop.molecule.entity_guid)
                {
                    molpop.Name = new_mol_name;

                    //Must update molecules_dict
                    ConfigCell cell = DataContext as ConfigCell;
                    if (cell != null)
                    {
                        if (cell.membrane.molecules_dict.ContainsKey(curr_mol_guid))
                        {
                            cell.membrane.molecules_dict.Remove(curr_mol_guid);
                        }
                        if (cell.membrane.molecules_dict.ContainsKey(molpop.molecule.entity_guid) == false)
                        {
                            cell.membrane.molecules_dict.Add(molpop.molecule.entity_guid, molpop.molecule);
                        }
                    }
                }
            }

        }

        private void memb_molecule_combo_box_GotFocus(object sender, RoutedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            CollectionViewSource.GetDefaultView(combo.ItemsSource).Refresh();
        }

        private void CellNucleusGenesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            txtGeneName.IsEnabled = false;

            ListBox lb = sender as ListBox;
            if (lb.SelectedIndex == -1 && lb.Items.Count > 0)
            {
                lb.SelectedIndex = 0;
            }
        }


        //I DON'T THINK THIS METHOD IS USED - SKG
        //IN ANY CASE, IT IS RELEVANT ONLY FOR CELL POPULATIONS, NOT RELEVANT FOR USERSTORE OR DAPHNESTORE
        private void gaussian_region_actor_checkbox_clicked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = e.OriginalSource as CheckBox;

            if (cb.CommandParameter == null)
            {
                return;
            }

            string guid = cb.CommandParameter as string;

            if (guid.Length > 0)
            {
                GaussianSpecification next;

                ((TissueScenario)MainWindow.SOP.Protocol.scenario).resetGaussRetrieve();
                while ((next = ((TissueScenario)MainWindow.SOP.Protocol.scenario).nextGaussSpec()) != null)
                {
                    if (next.box_spec.box_guid == guid)
                    {
                        next.gaussian_region_visibility = (bool)(cb.IsChecked);
                        break;
                    }
                }
            }
        }

        private void cyto_molecule_combo_box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)e.Source;
            if (cb == null)
                return;

            ConfigMolecularPopulation molpop = (ConfigMolecularPopulation)CellCytosolMolPopsListBox.SelectedItem;

            if (molpop == null)
                return;

            string curr_mol_pop_name = molpop.Name;
            string curr_mol_guid = "";
            curr_mol_guid = molpop.molecule.entity_guid;

            int nIndex = cb.SelectedIndex;
            if (nIndex < 0)
                return;

            Level level = MainWindow.GetLevelContext(this);
            if (level == null)
            {
                level = CurrentLevel;
            }

            //if user picked 'new molecule' then create new molecule in ER
            if (nIndex == (cb.Items.Count - 1))
            {
                ConfigMolecule newLibMol = new ConfigMolecule();

                //newLibMol.Name = newLibMol.GenerateNewName(MainWindow.SOP.Protocol, "_New");
                newLibMol.Name = newLibMol.GenerateNewName(level, "_New");

                AddEditMolecule aem = new AddEditMolecule(newLibMol, MoleculeDialogType.NEW);
                aem.Tag = this.Tag;    //DataContext as ConfigCell

                //if user cancels out of new molecule dialog, set selected molecule back to what it was
                if (aem.ShowDialog() == false)
                {
                    if (e.RemovedItems.Count > 0)
                    {
                        cb.SelectedItem = e.RemovedItems[0];
                    }
                    else
                    {
                        cb.SelectedIndex = 0;
                    }
                    return;
                }
                //newLibMol.ValidateName(MainWindow.SOP.Protocol);
                //MainWindow.SOP.Protocol.entity_repository.molecules.Add(newLibMol);
                newLibMol.ValidateName(level);
                level.entity_repository.molecules.Add(newLibMol);

                molpop.molecule = newLibMol.Clone(null);
                molpop.Name = newLibMol.Name;

                CollectionViewSource.GetDefaultView(cb.ItemsSource).Refresh();
                cb.SelectedItem = newLibMol;
            }
            //user picked an existing molecule
            else
            {
                ConfigMolecule newmol = (ConfigMolecule)cb.SelectedItem;

                //if molecule has not changed, return
                if (newmol.entity_guid == curr_mol_guid)
                {
                    return;
                }

                //if molecule changed, then make a clone of the newly selected one from entity repository
                ConfigMolecule mol = (ConfigMolecule)cb.SelectedItem;
                molpop.molecule = mol.Clone(null);

                string new_mol_name = mol.Name;
                if (curr_mol_guid != molpop.molecule.entity_guid)
                {
                    molpop.Name = new_mol_name;

                    //Must update molecules_dict
                    ConfigCell cell = DataContext as ConfigCell;
                    if (cell != null)
                    {
                        if (cell.cytosol.molecules_dict.ContainsKey(curr_mol_guid)) 
                        {
                            cell.cytosol.molecules_dict.Remove(curr_mol_guid);
                        }
                        cell.cytosol.molecules_dict.Add(molpop.molecule.entity_guid, molpop.molecule);
                    }
                }
            }

            var cvs = (CollectionViewSource)(FindResource("cytosolAvailableReactionsListView"));
            if (cvs.View == null) return; //not ready yet
            cvs.View.Refresh();
        }

        private void cyto_molecule_combo_box_GotFocus(object sender, RoutedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            CollectionViewSource.GetDefaultView(combo.ItemsSource).Refresh();
        }

        private void cytosolAvailableReactionsListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigReaction cr = e.Item as ConfigReaction;
            ConfigCell cc = DataContext as ConfigCell;

            if (cc == null)
            {
                e.Accepted = false;
                return;
            }

            //New filtering rules as of 3/5/15 bug 2426
            //Allow all reactions except what belongs in membrane (where each molecule is a boundary molecule)

            //EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
            Level level = MainWindow.GetLevelContext(this);
            if (level == null)
            {
                level = CurrentLevel;
            }
            EntityRepository er = level.entity_repository;

            if (cr.HasBulkMolecule(er) == true)
            {
                e.Accepted = true;
            }
            else
            {
                e.Accepted = false;
                return;
            }

            //Finally, if the cell cytosol already contains this reaction, exclude it from the available reactions list
            if (cc.cytosol.reactions_dict.ContainsKey(cr.entity_guid))
            {
                e.Accepted = false;
            }
        }

        private void membraneAvailableReactionsListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigReaction cr = e.Item as ConfigReaction;
            ConfigCell cc = DataContext as ConfigCell;

            if (cc == null)
            {
                e.Accepted = false;
                return;
            }

            //New filter as of 3/5/2015 for bug 2426
            //Molecules no longer need to be in the membrane. They will get added if needed.

            //If the reaction has any bulk molecules, it cannot go in the membrane

            //if (cr.HasBulkMolecule(MainWindow.SOP.Protocol.entity_repository) == true)
            Level level = MainWindow.GetLevelContext(this);
            if (level == null)
            {
                level = CurrentLevel;
            }
            if (cr.HasBulkMolecule(level.entity_repository) == true)
            {
                e.Accepted = false;
                return;
            }

            //Finally, if the cell membrane already contains this reaction, exclude it from the available reactions list
            if (cc.membrane.reactions_dict.ContainsKey(cr.entity_guid))
            {
                e.Accepted = false;
                return;
            }

            e.Accepted = true;
        }

        //THIS METHOD NEEDS TO BE IMPLEMENTED

        private void membraneAvailableReactionComplexesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigReactionComplex crc = e.Item as ConfigReactionComplex;
            ConfigCell cc = DataContext as ConfigCell;

            if (cc == null)
            {
                e.Accepted = false;
                return;
            }

            //if already in cytosol, return
            if (cc.membrane.reaction_complexes_dict.ContainsKey(crc.entity_guid))
            {
                e.Accepted = false;
                return;
            }

            //This filter is called for every reaction complex in the repository.

            // Only allow reaction complexes with membrane-bound molecules.
            bool bOK = true;

            foreach (KeyValuePair<string,ConfigMolecule> kvp in crc.molecules_dict)
            {
                if (kvp.Value.molecule_location == MoleculeLocation.Bulk)
                {
                    bOK = false;
                    break;
                }
            }

            e.Accepted = bOK;
        }

        //THIS METHOD NEEDS TO BE IMPLEMENTED
        private void cytosolAvailableReactionComplexesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigReactionComplex crc = e.Item as ConfigReactionComplex;
            ConfigCell cc = DataContext as ConfigCell;

            //if null cell, return
            if (cc == null)
            {
                e.Accepted = false;
                return;
            }

            //if already in cytosol, return
            if (cc.cytosol.reaction_complexes_dict.ContainsKey(crc.entity_guid))
            {
                e.Accepted = false;
                return;
            }

            // Allow any reaction commplex. Any missign molecules or genes will be added to the cell, as needed.

            e.Accepted = true;
        }
       
        private bool MembraneHasMolecule(ConfigCell cell, string molguid)
        {
            foreach (ConfigMolecularPopulation molpop in cell.membrane.molpops)
            {
                if (molguid == molpop.molecule.entity_guid)
                {
                    return true;
                }
            }
            return false;
        }
        private bool CytosolHasMolecule(ConfigCell cell, string molguid)
        {
            foreach (ConfigMolecularPopulation molpop in cell.cytosol.molpops)
            {
                if (molguid == molpop.molecule.entity_guid)
                {
                    return true;
                }
            }
            return false;
        }

        private bool CellPopsHaveMoleculeInMemb(string molguid)
        {
            bool ret = false;
            //This method should only get called if we have a tissue scenario so no need to use "LevelContext"
            foreach (CellPopulation cell_pop in ((TissueScenario)MainWindow.SOP.Protocol.scenario).cellpopulations)
            {
                if (MainWindow.SOP.Protocol.entity_repository.cells_dict.ContainsKey(cell_pop.Cell.entity_guid))
                {
                    ConfigCell cell = MainWindow.SOP.Protocol.entity_repository.cells_dict[cell_pop.Cell.entity_guid];
                    if (MembraneHasMolecule(cell, molguid))
                        return true;
                }
                else
                {
                    return ret;
                }
            }

            return ret;
        }
        private bool CellPopsHaveMoleculeInCytosol(string molguid)
        {
            bool ret = false;
            //This method should only get called if we have a tissue scenario so no need to use "LevelContext"
            foreach (CellPopulation cell_pop in ((TissueScenario)MainWindow.SOP.Protocol.scenario).cellpopulations)
            {
                ConfigCell cell = MainWindow.SOP.Protocol.entity_repository.cells_dict[cell_pop.Cell.entity_guid];
                if (CytosolHasMolecule(cell, molguid))
                    return true;
            }

            return ret;
        }

        private void btnNewDeathDriver_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;

            if (cell == null)
                return;

            if (cell.death_driver == null)
            {
                // The transition driver elements should default to ConfigMolTransitionDriverElement.
                // If we use ConfigDistrDriverElement as the default we won't be able to distinguish between meaningful and empty TDEs. 
                ConfigTransitionDriver config_td = new ConfigTransitionDriver();
                config_td.Name = "generic apoptosis";
                string[] stateName = new string[] { "alive", "dead" };
                string[,] signal = new string[,] { { "", "" }, { "", "" } };
                double[,] alpha = new double[,] { { 0, 0 }, { 0, 0 } };
                double[,] beta = new double[,] { { 0, 0 }, { 0, 0 } };

                //ProtocolCreators.LoadConfigTransitionDriverElements(config_td, signal, alpha, beta, stateName, MainWindow.SOP.Protocol);
                Level level = MainWindow.GetLevelContext(this);
                if (level == null)
                {
                    level = CurrentLevel;
                }
                ProtocolCreators.LoadConfigTransitionDriverElements(config_td, signal, alpha, beta, stateName, level);

                config_td.CurrentState = new DistributedParameter(0);
                config_td.StateName = config_td.states[(int)config_td.CurrentState.Sample()];
                cell.death_driver = config_td;
            }
        }

        private void btnDelDeathDriver_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;

            if (cell == null)
                return;

            //confirm deletion of driver
            MessageBoxResult res;
            res = MessageBox.Show("Are you sure you want to delete the selected cell's death driver?", "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;

            //delete driver
            cell.death_driver = null;

            ToolWinTissue twt = Tag as ToolWinTissue;
            CellPopulation cp = twt.CellPopControl.CellPopsListBox.SelectedItems[0] as CellPopulation;
            if (cp != null)
            {
                cp.reportStates.Death = false;
            }

        }

        /// <summary>
        /// This method creates a data grid column with a combo box in the header.
        /// The combo box contains genes that are not in the epigenetic map of of 
        /// the selected cell's differentiation scheme.  
        /// This allows the user to add genes to the epigenetic map.
        /// </summary>
        /// <returns></returns>
        public DataGridTextColumn CreateUnusedGenesColumn(ConfigTransitionScheme currScheme)
        {
            ConfigCell cell = DataContext as ConfigCell;

            //EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
            Level level = MainWindow.GetLevelContext(this);
            if (level == null)
            {
                level = CurrentLevel;
            }
            EntityRepository er = level.entity_repository;

            DataGridTextColumn editor_col = new DataGridTextColumn();
            editor_col.CanUserSort = false;
            DataTemplate HeaderTemplate = new DataTemplate();

            CollectionViewSource cvs1 = new CollectionViewSource();
            cvs1.SetValue(CollectionViewSource.SourceProperty, er.genes);

            if (currScheme == cell.diff_scheme)
            {
                cvs1.Filter += new FilterEventHandler(unusedGenesListView_Filter);
            }
            else
            {
                cvs1.Filter += new FilterEventHandler(unusedDivGenesListView_Filter);
            }

            CompositeCollection coll1 = new CompositeCollection();
            ConfigGene dummyItem = new ConfigGene("Add a gene", 0, 0);
            coll1.Add(dummyItem);
            CollectionContainer cc1 = new CollectionContainer();
            cc1.Collection = cvs1.View;
            coll1.Add(cc1);

            FrameworkElementFactory addGenesCombo = new FrameworkElementFactory(typeof(ComboBox));
            addGenesCombo.SetValue(ComboBox.WidthProperty, 100D);
            addGenesCombo.SetValue(ComboBox.ItemsSourceProperty, coll1);
            addGenesCombo.SetValue(ComboBox.DisplayMemberPathProperty, "Name");
            addGenesCombo.SetValue(ComboBox.ToolTipProperty, "Click here to add another gene column to the grid.");
            addGenesCombo.AddHandler(ComboBox.SelectionChangedEvent, new SelectionChangedEventHandler(comboAddGeneToEpigeneticMap_SelectionChanged));

            addGenesCombo.SetValue(ComboBox.SelectedIndexProperty, 0);
            addGenesCombo.Name = "ComboGenes";

            HeaderTemplate.VisualTree = addGenesCombo;
            editor_col.HeaderTemplate = HeaderTemplate;

            return editor_col;
        }

        /// <summary>
        /// This handler gets called when user selects a gene in the "add genes" 
        /// combo box in the upper right of the epigenetic map data grid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void comboAddGeneToEpigeneticMap_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if (combo.SelectedIndex <= 0)
                return;

            //ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;
            ConfigCell cell = DataContext as ConfigCell;

            if (cell == null)
                return;

            DataGrid dataGrid = (DataGrid)DiffSchemeDataGrid.FindVisualParent<DataGrid>(combo);
            if (dataGrid == null) return;
            ConfigTransitionScheme scheme = DiffSchemeDataGrid.GetDiffSchemeSource(dataGrid);
            if (scheme != null)
            {
                //this is the new way to creating datagrid dyamically with diffscheme specified in the grid
                //otherwise, it is the old way, remove those code when all changed to this new way.
                ConfigGene gene1 = (ConfigGene)combo.SelectedItem;
                if (gene1 == null)
                    return;

                if (scheme.genes.Contains(gene1.entity_guid)) return; //shouldnot happen...

                //If no states exist, then create at least 2 new ones
                if (scheme.Driver.states.Count == 0)
                {
                    scheme.AddState("state1");
                    scheme.AddState("state2");
                }

                scheme.AddGene(gene1.entity_guid);  

                //HERE, WE NEED TO ADD THE GENE TO THE CELL ALSO
                if (cell.HasGene(gene1.entity_guid) == false)
                {
                    ConfigGene newgene = gene1.Clone(null);
                    cell.genes.Add(newgene);
                }
              
                //force refresh
                //dataGrid.GetBindingExpression(DiffSchemeDataGrid.DiffSchemeSourceProperty).UpdateTarget();
                return;
            }
        }

        /// <summary>
        /// This method adds a differentiation state given a name 
        /// </summary>
        /// <param name="stateName"></param>
        private void AddDifferentiationState(string schemeName, string stateName)
        {
            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)return;

            ConfigTransitionScheme new_scheme = null;
            if (schemeName == "Division")
            {
                new_scheme = cell.div_scheme;
                if (new_scheme == null)
                {
                    cell.div_scheme = new_scheme = new ConfigTransitionScheme();
                }
            }
            else if (schemeName == "Differentiation")
            {
                new_scheme = cell.diff_scheme;
                if (new_scheme == null)
                {
                    cell.diff_scheme = new_scheme = new ConfigTransitionScheme();
                }
            }
            else return;

            new_scheme.AddState(stateName);

            //refresh display
            if (schemeName == "Division")
            {
                cell.div_scheme = null;
                cell.div_scheme = new_scheme;
            }
            else if (schemeName == "Differentiation")
            {
                cell.diff_scheme = null;
                cell.diff_scheme = new_scheme;
            }
        }

        private void btnNewDiffScheme_Click(object sender, RoutedEventArgs e)
        {

            string schemeName = ((Button)sender).Tag as string;
            if (schemeName == null) return;
            AddDifferentiationState(schemeName, "State0");
            AddDifferentiationState(schemeName, "State1");
        }

        private void btnDelDiffScheme_Click(object sender, RoutedEventArgs e)
        {

            string schemeName = ((Button)sender).Tag as string;
            if (schemeName == null) return;

            MessageBoxResult res;
            string message = string.Format("Are you sure you want to delete the selected cell's {0} scheme?", schemeName);
            res = MessageBox.Show(message, "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;

            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)
                return;

            ToolWinTissue twt = Tag as ToolWinTissue;
            CellPopulation cp = twt.CellPopControl.CellPopsListBox.SelectedItems[0] as CellPopulation;

            if (schemeName == "Division")
            {
                cell.div_scheme = null;
                if (cp != null)
                {
                    cp.reportStates.Division = false;
                }
            }
            else if (schemeName == "Differentiation")
            {
                cell.diff_scheme = null;
                if (cp != null)
                {
                    cp.reportStates.Differentiation = false;
                }
            }
        }

        private void EpigeneticMapGrid_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // TODO: Add event handler implementation here.
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            // iteratively traverse the visual tree
            while ((dep != null) && !(dep is DataGridCell) && !(dep is DataGridColumnHeader) && !(dep is DataGridRowHeader))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null)
                return;

            else if (dep is DataGridColumnHeader)
            {
                DataGridColumnHeader columnHeader = dep as DataGridColumnHeader;
                // do something
                DataGridBehavior.SetHighlightColumn(columnHeader.Column, true);
            }

            else if (dep is DataGridRowHeader)
            {
            }

            else if (dep is DataGridCell)
            {
                DataGridCell cell = dep as DataGridCell;
                // do something                
            }
        }

        private void EpigeneticMapGrid_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void unusedGenesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;

            e.Accepted = false;

            if (cell == null)
                return;

            ConfigTransitionScheme ds = cell.diff_scheme;
            ConfigGene gene = e.Item as ConfigGene;

            //REMOVED this for resolving bug 2429 - the combo should populate from er.genes
            //if gene is not in the cell's nucleus, then exclude it from the available gene pool
            //if (!cell.HasGene(gene.entity_guid))
            //    return;


            if (ds != null)
            {
                //if scheme already contains this gene, exclude it from the available gene pool
                if (ds.genes.Contains(gene.entity_guid))
                {
                    e.Accepted = false;
                }
                else
                {
                    e.Accepted = true;
                }
            }
            else
            {
                e.Accepted = true;
            }
        }

        private void unusedDivGenesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;

            e.Accepted = false;

            if (cell == null)
                return;

            ConfigTransitionScheme ds = cell.div_scheme;
            ConfigGene gene = e.Item as ConfigGene;

            if (ds != null)
            {
                //if scheme already contains this gene, exclude it from the available gene pool
                if (ds.genes.Contains(gene.entity_guid))
                {
                    e.Accepted = false;
                }
                else
                {
                    e.Accepted = true;
                }
            }
            else
            {
                e.Accepted = true;
            }
        }
        public static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                    else
                    {
                        // recursively drill down the tree
                        foundChild = FindChild<T>(child, childName);

                        // If the child is found, break so we do not overwrite the found child. 
                        if (foundChild != null) break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        //Cytosol reaction complex handlers

        private void CytoRCDetailsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }
        
        // UserControl methods
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // cyto_molecule_combo_box
            CollectionViewSource cvs = (CollectionViewSource)(FindResource("availableBulkMoleculesListView"));
            cvs.Filter += ToolWinBase.FilterFactory.BulkMolecules_Filter;

            // memb_molecule_combo_box
            cvs = (CollectionViewSource)(FindResource("availableBoundaryMoleculesListView"));
            cvs.Filter += ToolWinBase.FilterFactory.BoundaryMolecules_Filter;

            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)
            {
                return;
            }

            //Level level = null;
            //SetCurrentLevel(level);

            updateCollections(cell);
            updateSelectedMoleculesAndGenes(cell);
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)
            {
                return;
            }

            updateCollections(cell);
            updateSelectedMoleculesAndGenes(cell);

        }

        public void updateCollections(ConfigCell cell)
        {
            CollectionViewSource cvs;
            Level level = MainWindow.GetLevelContext(this);
            if (level == null)
            {
                level = CurrentLevel;
            }

            if (level == null)
            {
                //var sopTag = Tag as SystemOfPersistence;
                PushBetweenLevels pushwin = Window.GetWindow(this) as PushBetweenLevels;
                if (pushwin != null)
                {
                    CurrentLevel = pushwin.CurrentLevel;
                    level = pushwin.CurrentLevel;
                }
            }

            if (level == null)
                return;

            // MOLECULES

            // cyto_molecule_combo_box - filtered for bulk molecules in EntityRepository
            cvs = (CollectionViewSource)(FindResource("availableBulkMoleculesListView"));
            cvs.Source = new ObservableCollection<ConfigMolecule>();
            
            //cvs.Source = MainWindow.SOP.Protocol.entity_repository.molecules;
            cvs.Source = level.entity_repository.molecules;

            // memb_molecule_combo_box - filtered for boundary molecules in EntityRepository
            cvs = (CollectionViewSource)(FindResource("availableBoundaryMoleculesListView"));
            cvs.Source = new ObservableCollection<ConfigMolecule>();
            
            //cvs.Source = MainWindow.SOP.Protocol.entity_repository.molecules;
            cvs.Source = level.entity_repository.molecules;

            // list of cytosol molecules for use by division and differentitiation schemes
            cvs = (CollectionViewSource)(FindResource("moleculesListView"));
            cvs.Source = new ObservableCollection<ConfigMolecule>();
            foreach (ConfigMolecularPopulation configMolpop in cell.cytosol.molpops)
            {
                ((ObservableCollection<ConfigMolecule>)cvs.Source).Add(configMolpop.molecule);
            }

            // REACTIONS

            // lvCellAvailableReacs
            cvs = (CollectionViewSource)(FindResource("membraneAvailableReactionsListView"));
            cvs.Source = new ObservableCollection<ConfigReaction>();
            
            //cvs.Source = MainWindow.SOP.Protocol.entity_repository.reactions;
            cvs.Source = level.entity_repository.reactions;

            // lvCytosolAvailableReacs
            cvs = (CollectionViewSource)(FindResource("cytosolAvailableReactionsListView"));
            cvs.Source = new ObservableCollection<ConfigReaction>();
            
            //cvs.Source = MainWindow.SOP.Protocol.entity_repository.reactions;
            cvs.Source = level.entity_repository.reactions;

            cvs = (CollectionViewSource)(FindResource("membraneAvailableReactionComplexesListView"));
            cvs.Source = new ObservableCollection<ConfigReactionComplex>();
            
            //cvs.Source = MainWindow.SOP.Protocol.entity_repository.reaction_complexes;
            cvs.Source = level.entity_repository.reaction_complexes;

            cvs = (CollectionViewSource)(FindResource("cytosolAvailableReactionComplexesListView"));
            cvs.Source = new ObservableCollection<ConfigReactionComplex>();
            
            //cvs.Source = MainWindow.SOP.Protocol.entity_repository.reaction_complexes;
            cvs.Source = level.entity_repository.reaction_complexes;
        }

        //This is probably not needed any more but leaving here in case a problem occurs.
        //To be deleted for next checkin.
        public void updateSelectedMoleculesAndGenes(ConfigCell cell)
        {
            // Setting ListBox.SelectedItem = 0 in the xaml code only works the first time the tab is populated,
            // so do it manually here.

            CellMembraneMolPopsListBox.SelectedIndex = 0;
            CellCytosolMolPopsListBox.SelectedIndex = 0;
            CellNucleusGenesListBox.SelectedItem = 0;
        }

        /// <summary>
        /// This fixes the problem of selecting the 1st item in the mol pop list.
        /// This shouldn't be a problem in the first place, but the data binding seems to occur before the list is populated
        /// so the first item was not getting selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CellCytosolMolPopsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = sender as ListBox;
            if (lb.SelectedIndex == -1 && lb.Items.Count > 0)
            {
                lb.SelectedIndex = 0;
            }
        }

        private void CellMembraneMolPopsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = sender as ListBox;
            if (lb.SelectedIndex == -1 && lb.Items.Count > 0)
            {
                lb.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Switch between molecule-driven and distribution-driven transition driver elements for cell death.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeDeathTDEType_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null) return;

            if (cell.death_driver == null)
            {
                return;
            }

            if (cell.death_driver.DriverElements == null)
            {
                return;
            }

            ConfigTransitionDriverElement tde = cell.death_driver.DriverElements[0].elements[1];
            int CurrentState = tde.CurrentState,
                DestState = tde.DestState;
            string CurrentStateName = tde.CurrentStateName,
                    DestStateName = tde.DestStateName;

            if (tde.Type == TransitionDriverElementType.MOLECULAR)
            {
                // Switch to Distribution-driven
                tde = new ConfigDistrTransitionDriverElement();

                PoissonParameterDistribution poisson = new PoissonParameterDistribution();
                poisson.Mean = 1.0;
                ((ConfigDistrTransitionDriverElement)tde).Distr.ParamDistr = poisson;
                ((ConfigDistrTransitionDriverElement)tde).Distr.DistributionType = ParameterDistributionType.POISSON;
            }
            else
            {
                if (cell.cytosol.molpops.Count == 0)
                {
                    MessageBox.Show("Death can only be controlled by a probability distribution because there are no molecules in the cytosol. Add molecules from the store to control the death by molecular concentrations.", "No molecules available", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Switch to Molecule-driven
                tde = new ConfigMolTransitionDriverElement();
            }
            tde.CurrentStateName = CurrentStateName;
            tde.DestStateName = DestStateName;
            tde.CurrentState = CurrentState;
            tde.DestState = DestState;
           
            cell.death_driver.DriverElements[0].elements[1] = tde;
        }

        private void ChangeDataGridTDEType_Click(object sender, RoutedEventArgs e)
        { 
            Button button = sender as Button;

            var stack_panel = FindVisualParent<StackPanel>(button);
            if (stack_panel == null) return;

            ConfigTransitionDriverElement tde = stack_panel.DataContext as ConfigTransitionDriverElement;
            if (tde == null) return;
    
            TransitionDriverElementType type = tde.Type;
            int CurrentState = tde.CurrentState,
                DestState = tde.DestState;
            string CurrentStateName = tde.CurrentStateName,
                    DestStateName = tde.DestStateName;

            if (tde.Type == TransitionDriverElementType.MOLECULAR)
            {
                tde = new ConfigDistrTransitionDriverElement();
                ((ConfigDistrTransitionDriverElement)tde).Distr.ParamDistr = null;
                ((ConfigDistrTransitionDriverElement)tde).Distr.DistributionType = ParameterDistributionType.CONSTANT;
                //stack_panel.DataContext = tde;
            }
            else
            {
                tde = new ConfigMolTransitionDriverElement();                
            }
            tde.CurrentStateName = CurrentStateName;
            tde.DestStateName = DestStateName;
            tde.CurrentState = CurrentState;
            tde.DestState = DestState;

            // update the transition scheme
            DataGrid dataGrid = (DataGrid)DiffSchemeDataGrid.FindVisualParent<DataGrid>(button);
            if (dataGrid == null) return;
            ConfigTransitionScheme scheme = DiffSchemeDataGrid.GetDiffSchemeSource(dataGrid);
            if (scheme != null)
            {
                scheme.Driver.DriverElements[CurrentState].elements[DestState] = tde;
            }
        }

        public static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            // get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            // we’ve reached the end of the tree
            if (parentObject == null) return null;

            // check if the parent matches the type we’re looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                // use recursion to proceed with next level
                return FindVisualParent<T>(parentObject);
            }
        }

        public static T FindLogicalParent<T>(DependencyObject child) where T : DependencyObject
        {
            // get parent item
            DependencyObject parentObject = LogicalTreeHelper.GetParent(child);

            // we’ve reached the end of the tree
            if (parentObject == null) return null;

            // check if the parent matches the type we’re looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                // use recursion to proceed with next level
                return FindLogicalParent<T>(parentObject);
            }
        }
        private void menu2PullFromProto_Click(object sender, RoutedEventArgs e)
        {
            Level level = MainWindow.GetLevelContext(this);
            if (level == null)
            {
                level = CurrentLevel;
            }
            ConfigReaction reac = (ConfigReaction)CytosolReacListBox.SelectedValue;

            //if (MainWindow.SOP.Protocol.entity_repository.reactions_dict.ContainsKey(reac.entity_guid))
            if (level.entity_repository.reactions_dict.ContainsKey(reac.entity_guid))
            {
                //ConfigReaction protReaction = MainWindow.SOP.Protocol.entity_repository.reactions_dict[reac.entity_guid];
                ConfigReaction protReaction = level.entity_repository.reactions_dict[reac.entity_guid];
                ConfigReaction newreac = protReaction.Clone(true);

                ConfigCell cell = DataContext as ConfigCell;
                cell.cytosol.Reactions.Remove(reac);
                cell.cytosol.Reactions.Add(newreac);
            }
        }

        private void menuMembPushReacToProto_Click(object sender, RoutedEventArgs e)
        {
            if (MembReacListBox.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a reaction.");
                return;
            }

            ConfigReaction reac = (ConfigReaction)MembReacListBox.SelectedValue;
            ConfigReaction newreac = reac.Clone(true);
            MainWindow.GenericPush(newreac);
        }

        private void menuMembPullReacFromProto_Click(object sender, RoutedEventArgs e)
        {
            ConfigReaction reac = (ConfigReaction)MembReacListBox.SelectedValue;
            Level level = MainWindow.GetLevelContext(this);
            if (level == null)
            {
                level = CurrentLevel;
            }

            //if (MainWindow.SOP.Protocol.entity_repository.reactions_dict.ContainsKey(reac.entity_guid))
            if (level.entity_repository.reactions_dict.ContainsKey(reac.entity_guid))
            {
                //ConfigReaction protReaction = MainWindow.SOP.Protocol.entity_repository.reactions_dict[reac.entity_guid];
                ConfigReaction protReaction = level.entity_repository.reactions_dict[reac.entity_guid];
                ConfigReaction newreac = protReaction.Clone(true);

                ConfigCell cell = DataContext as ConfigCell;
                cell.membrane.Reactions.Remove(reac);
                cell.membrane.Reactions.Add(newreac);
            }
        }

        private void comboDeathMolPop2_Loaded(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;

            if (cell == null)
                return;

            //Don't do anything if driver type is distribution
            if (cell.death_driver.DriverElements[0].elements[1].Type == TransitionDriverElementType.DISTRIBUTION)
                return;

            ComboBox combo = sender as ComboBox;

            //If no death molecule selected, and there are bulk molecules, select 1st molecule.
            if (combo.SelectedIndex == -1 && combo.Items.Count > 0)
            {
                combo.SelectedIndex = 0;
            }
            //If no death molecule selected, and there are NO bulk molecules, issue a warning to acquire molecules from the user store.
            else if (combo.SelectedIndex == -1 && combo.Items.Count == 0)
            {
                //Since there are no molecules, create a default DISTRIBUTION driver and assign it.
                ConfigTransitionDriverElement tde = new ConfigDistrTransitionDriverElement();
                PoissonParameterDistribution poisson = new PoissonParameterDistribution();

                poisson.Mean = 1.0;
                ((ConfigDistrTransitionDriverElement)tde).Distr.ParamDistr = poisson;
                ((ConfigDistrTransitionDriverElement)tde).Distr.DistributionType = ParameterDistributionType.POISSON;

                cell.death_driver.DriverElements[0].elements[1] = tde;
            }
        }

        private void MembCreateNewReaction_Expanded(object sender, RoutedEventArgs e)
        {
            this.BringIntoView();
        }

        private void CytoSaveReacCompToProtocolButton_Click(object sender, RoutedEventArgs e)
        {
            if (CytoReactionComplexListBox.SelectedIndex < 0)
                return;

            ConfigReactionComplex crc = ((ConfigReactionComplex)(CytoReactionComplexListBox.SelectedItem));

            ConfigReactionComplex newcrc = crc.Clone(true);
            MainWindow.GenericPush(newcrc);
        }

        private void MembSaveReacCompToProtocolButton_Click(object sender, RoutedEventArgs e)
        {
            if (MembReactionComplexListBox.SelectedIndex < 0)
                return;

            ConfigReactionComplex crc = ((ConfigReactionComplex)(MembReactionComplexListBox.SelectedItem));

            ConfigReactionComplex newcrc = crc.Clone(true);
            MainWindow.GenericPush(newcrc);
        }

        private void DiffSchemeExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();

        }

        private void DivSchemeExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }

        private void MembRCDetailsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }

        private void CellMolPopsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }

        private void CellReacExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }

        private void ReacCompExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }

        private void CellDeathExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }

    }

}

/*
 * 
 *  <Expander   Header="Division"
                        Padding="5" ExpandDirection="Down"
                        IsExpanded="False"                                                  
                        x:Name="DivSchemeExpander"
                        Margin="0,4,0,0" 
                        Canvas.ZIndex="1"
                        BorderThickness="1"
                        BorderBrush="Black"                                        
                        Background="{shared:LinearGradientBrush #EEEEEE, #CCCCCC, GradientType=TopLeftToBottomRight}" 
                        Expanded="DivSchemeExpander_Expanded" >

                <!--Expanded="DivSchemeExpander_Expanded"-->
                <StackPanel Orientation="Vertical" ScrollViewer.VerticalScrollBarVisibility="Auto" ScrollViewer.CanContentScroll="True"
                            x:Name="spDivScheme">
                    <StackPanel>
                        <Grid >
                            <Grid.Resources>
                                <DataTemplate x:Key="geneListItemTemplate">
                                    <StackPanel Orientation="Horizontal">
                                        <TextBlock Width="50" Text="{Binding Path=Name}" />
                                        <CheckBox IsChecked="{Binding Path=Active}" />
                                    </StackPanel>
                                </DataTemplate>

                                <DataTemplate x:Key="stateListItemTemplate">
                                    <StackPanel Orientation="Vertical">
                                        <TextBlock VerticalAlignment="Top" Text="{Binding Path=Name}" />
                                        <ListBox VerticalContentAlignment="Top" HorizontalContentAlignment="Center" 
                                                            ItemsSource="{Binding Path=Genes}" DisplayMemberPath="Active" 
                                                            ItemTemplate="{Binding Source={StaticResource geneListItemTemplate}}" />
                                    </StackPanel>
                                </DataTemplate>

                                <DataTemplate x:Key="cellState2ListItemTemplate">
                                    <StackPanel Orientation="Horizontal">
                                        <TextBlock Width="20" Text="{Binding Path=Name}" />
                                        <TextBlock Width="10" Text="" />
                                        <TextBox Width="50" Text="{Binding Path=MolName}" />
                                    </StackPanel>
                                </DataTemplate>

                                <DataTemplate x:Key="divStatesListItemTemplate">
                                    <StackPanel Orientation="Horizontal">
                                        <TextBlock Width="80" Text="{Binding Path=Name}" />
                                    </StackPanel>
                                </DataTemplate>

                                <DataTemplate x:Key="cellStateGridItemTemplate">
                                    <StackPanel Orientation="Horizontal">
                                        <TextBlock Width="80" Text="{Binding Path=MolName}" />
                                    </StackPanel>
                                </DataTemplate>
                            </Grid.Resources>

                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="200" />
                                <ColumnDefinition Width="130" />
                                <ColumnDefinition Width="120" />
                                <ColumnDefinition Width="120" />
                                <ColumnDefinition Width="120" />
                            </Grid.ColumnDefinitions>
                            <Grid.RowDefinitions>
                                <RowDefinition Height="auto" />
                                <RowDefinition Height="auto" />
                                <RowDefinition Height="auto" />
                            </Grid.RowDefinitions>

                            <!-- DIVISION SCHEME -->
                            <StackPanel Grid.Column="0" Grid.Row="0" Grid.ColumnSpan="2" Orientation="Horizontal" HorizontalAlignment="Left"
                                        Visibility="{Binding Path=div_scheme, Converter={StaticResource ObjectToVisibilityConverter}}"
                                        >
                                <Button x:Name="btnDelDivScheme" Click="btnDelDiffScheme_Click" Tag="Division">Delete Division Scheme</Button>
                            </StackPanel>

                            <StackPanel Grid.Column="0" Grid.Row="0" Grid.ColumnSpan="2" Orientation="Horizontal" HorizontalAlignment="Left"
                                        Visibility="{Binding Path=div_scheme, Converter={StaticResource ObjectToVisibilityConverter}, ConverterParameter=Reverse}"
                                        >
                                <Button x:Name="btnNewDivScheme" Click="btnNewDiffScheme_Click" Tag="Division">New Division Scheme</Button>
                            </StackPanel>
                        </Grid>
                    </StackPanel>

                    <local:DiffSchemeDataGrid DataContext="{Binding div_scheme}" x:Name="DivSchemeGrid" 
                            Visibility="{Binding Converter={StaticResource ObjectToVisibilityConverter}, ConverterParameter=Collapsed}"
                                        Tag="div"/>
                    
                    
                </StackPanel>
            </Expander>
            <!-- Differentiation -->
            <Expander   
                    Padding="5" ExpandDirection="Down"
                    IsExpanded="False"
                    Header="Differentiation"                                                  
                    x:Name="DiffSchemeExpander"
                    Margin="0,4,0,0" 
                    Canvas.ZIndex="1"
                    BorderThickness="1"
                    BorderBrush="Black"
                    Background="{shared:LinearGradientBrush #EEEEEE, #CCCCCC, GradientType=TopLeftToBottomRight}"
                    Expanded="DiffSchemeExpander_Expanded"
                >

                <StackPanel Orientation="Vertical" ScrollViewer.VerticalScrollBarVisibility="Auto" ScrollViewer.CanContentScroll="True">
                    <StackPanel>
                        <Grid >
                            <Grid.Resources>
                                <DataTemplate x:Key="diffSchemeListItemTemplate2">
                                    <StackPanel Orientation="Horizontal">
                                        <TextBlock Text="{Binding Path=Hello}" />
                                    </StackPanel>
                                </DataTemplate>

                                <DataTemplate x:Key="geneListItemTemplate">
                                    <StackPanel Orientation="Horizontal">
                                        <TextBlock Width="50" Text="{Binding Path=Name}" />
                                        <CheckBox IsChecked="{Binding Path=Active}" />
                                    </StackPanel>
                                </DataTemplate>

                                <DataTemplate x:Key="stateListItemTemplate">
                                    <StackPanel Orientation="Vertical">
                                        <TextBlock VerticalAlignment="Top" Text="{Binding Path=Name}" />
                                        <ListBox VerticalContentAlignment="Top" HorizontalContentAlignment="Center" 
                                                            ItemsSource="{Binding Path=Genes}" DisplayMemberPath="Active" 
                                                            ItemTemplate="{Binding Source={StaticResource geneListItemTemplate}}" />
                                    </StackPanel>
                                </DataTemplate>

                                <DataTemplate x:Key="cellState2ListItemTemplate">
                                    <StackPanel Orientation="Horizontal">
                                        <TextBlock Width="20" Text="{Binding Path=Name}" />
                                        <TextBlock Width="10" Text="" />
                                        <TextBox Width="50" Text="{Binding Path=MolName}" />

                                    </StackPanel>
                                </DataTemplate>

                                <DataTemplate x:Key="diffStatesListItemTemplate">
                                    <StackPanel Orientation="Horizontal">
                                        <TextBlock Width="80" Text="{Binding Path=Name}" />
                                    </StackPanel>
                                </DataTemplate>

                                <DataTemplate x:Key="cellStateGridItemTemplate">
                                    <StackPanel Orientation="Horizontal">
                                        <TextBlock Width="80" Text="{Binding Path=MolName}" />
                                    </StackPanel>
                                </DataTemplate>
                            </Grid.Resources>

                            <Grid.ColumnDefinitions>
                                <ColumnDefinition Width="200" />
                                <ColumnDefinition Width="130" />
                                <ColumnDefinition Width="120" />
                                <ColumnDefinition Width="120" />
                                <ColumnDefinition Width="120" />
                            </Grid.ColumnDefinitions>
                            <Grid.RowDefinitions>
                                <RowDefinition Height="auto" />
                                <RowDefinition Height="auto" />
                                <RowDefinition Height="auto" />
                            </Grid.RowDefinitions>

                            <!-- DIFFERENTIATION SCHEME -->
                            <StackPanel Grid.Column="0" Grid.Row="0" Grid.ColumnSpan="2" Orientation="Horizontal" HorizontalAlignment="Left"
                                    Visibility="{Binding Path=diff_scheme, Converter={StaticResource ObjectToVisibilityConverter}}" >
                                <Button x:Name="btnDelDiffScheme" Click="btnDelDiffScheme_Click" Tag="Differentiation">Delete Differentiation Scheme</Button>
                            </StackPanel>

                            <StackPanel Grid.Column="0" Grid.Row="0" Grid.ColumnSpan="2" Orientation="Horizontal" HorizontalAlignment="Left"
                                    Visibility="{Binding Path=diff_scheme, Converter={StaticResource ObjectToVisibilityConverter}, ConverterParameter=Reverse}">        
                                <Button x:Name="btnNewDiffScheme" Click="btnNewDiffScheme_Click" Tag="Differentiation">New Differentiation Scheme</Button>
                            </StackPanel>
                        </Grid>
                    </StackPanel>
                    
                    <local:DiffSchemeDataGrid DataContext="{Binding diff_scheme}" x:Name="DiffSchemeGrid"
                            Visibility="{Binding Converter={StaticResource ObjectToVisibilityConverter}, ConverterParameter=Collapsed}" />

                </StackPanel>
            </Expander>
 * 
 * */


