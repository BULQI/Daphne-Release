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

        private void CellNucleusGenesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            txtGeneName.IsEnabled = false;

            ListBox lb = sender as ListBox;
            if (lb.SelectedIndex == -1 && lb.Items.Count > 0)
            {
                lb.SelectedIndex = 0;
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

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            // cyto_molecule_combo_box
            CollectionViewSource cvs = (CollectionViewSource)(FindResource("availableBulkMoleculesListView"));
            cvs.Filter += ToolWinBase.FilterFactory.BulkMolecules_Filter;

            // memb_molecule_combo_box
            cvs = (CollectionViewSource)(FindResource("availableBoundaryMoleculesListView"));
            cvs.Filter += ToolWinBase.FilterFactory.BoundaryMolecules_Filter;

            PropertiesGrid.IsEnabled = false;

            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)
            {
                return;
            }

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

            CollectionViewSource cvs2 = (CollectionViewSource)(FindResource("driverMoleculesListView"));
            if (cvs2.Source == null)
            {
                cvs2.Source = new ObservableCollection<ConfigMolecule>();
            }
            ((ObservableCollection<ConfigMolecule>)cvs2.Source).Clear();

            int locoMol = -1;
            foreach (ConfigMolecularPopulation configMolpop in cell.cytosol.molpops)
            {
                ((ObservableCollection<ConfigMolecule>)cvs2.Source).Add(configMolpop.molecule);
                if (configMolpop.molecule.entity_guid == cell.locomotor_mol_guid_ref)
                {
                    locoMol = cell.cytosol.molpops.IndexOf(configMolpop);
                }
            }
            cbLocomotorDriver1.SelectedIndex = locoMol;

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

