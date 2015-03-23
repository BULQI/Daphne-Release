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
        public Level CurrentLevel { get; set; }

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

        //Cytosol reaction complex handlers

        private void CytoRCDetailsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }

        /// <summary>
        /// This selects first item in each of the three molecules/genes lists.
        /// </summary>
        /// <param name="cell"></param>
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

        //Transition Schemes code here

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
                return null;

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

            DataGrid dataGrid = (DataGrid)DiffSchemeReadOnlyDataGrid.FindVisualParent<DataGrid>(combo);
            if (dataGrid == null) return;
            ConfigTransitionScheme scheme = DiffSchemeReadOnlyDataGrid.GetDiffSchemeSource(dataGrid);
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
            DataGrid dataGrid = (DataGrid)DiffSchemeReadOnlyDataGrid.FindVisualParent<DataGrid>(button);
            if (dataGrid == null) return;
            ConfigTransitionScheme scheme = DiffSchemeReadOnlyDataGrid.GetDiffSchemeSource(dataGrid);
            if (scheme != null)
            {
                scheme.Driver.DriverElements[CurrentState].elements[DestState] = tde;
            }
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

            updateSelectedMoleculesAndGenes(cell);
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)
            {
                return;
            }

            updateSelectedMoleculesAndGenes(cell);

            PushBetweenLevels pushwin = Window.GetWindow(this) as PushBetweenLevels;
            if (pushwin != null)
            {
                CurrentLevel = pushwin.CurrentLevel;
            }

            // list of cytosol molecules for use by division and differentitiation schemes
            CollectionViewSource cvs = (CollectionViewSource)(FindResource("moleculesListView"));
            cvs.Source = new ObservableCollection<ConfigMolecule>();
            foreach (ConfigMolecularPopulation configMolpop in cell.cytosol.molpops)
            {
                ((ObservableCollection<ConfigMolecule>)cvs.Source).Add(configMolpop.molecule);
            }

        }

        //Cell death code
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

        //These are helper methods, and they are like extension methods.
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

    }

}

