using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ActiproSoftware.Windows.Controls.Docking;

using Workbench;
using Daphne;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Windows.Markup;
//using System.Windows.Forms;
using Microsoft.Win32;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace DaphneGui
{
    public class DataGridBehavior
    {
        #region DisplayRowNumber

        public static DependencyProperty DisplayRowNumberProperty =
            DependencyProperty.RegisterAttached("DisplayRowNumber",
                                                typeof(bool),
                                                typeof(DataGridBehavior),
                                                new FrameworkPropertyMetadata(false, OnDisplayRowNumberChanged));
        public static bool GetDisplayRowNumber(DependencyObject target)
        {
            return (bool)target.GetValue(DisplayRowNumberProperty);
        }
        public static void SetDisplayRowNumber(DependencyObject target, bool value)
        {
            target.SetValue(DisplayRowNumberProperty, value);
        }

        private static void OnDisplayRowNumberChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            DataGrid dataGrid = target as DataGrid;
            if ((bool)e.NewValue == true)
            {
                EventHandler<DataGridRowEventArgs> loadedRowHandler = null;
                loadedRowHandler = (object sender, DataGridRowEventArgs ea) =>
                {
                    if (GetDisplayRowNumber(dataGrid) == false)
                    {
                        dataGrid.LoadingRow -= loadedRowHandler;
                        return;
                    }
                    int num = ea.Row.GetIndex();
                    ea.Row.Header = ea.Row.GetIndex() + 1;
                };
                dataGrid.LoadingRow += loadedRowHandler;

                ItemsChangedEventHandler itemsChangedHandler = null;
                itemsChangedHandler = (object sender, ItemsChangedEventArgs ea) =>
                {
                    if (GetDisplayRowNumber(dataGrid) == false)
                    {
                        dataGrid.ItemContainerGenerator.ItemsChanged -= itemsChangedHandler;
                        return;
                    }
                    GetVisualChildCollection<DataGridRow>(dataGrid).
                        ForEach(d => d.Header = d.GetIndex());
                };
                dataGrid.ItemContainerGenerator.ItemsChanged += itemsChangedHandler;
            }
        }

        #endregion // DisplayRowNumber

        #region Get Visuals

        private static List<T> GetVisualChildCollection<T>(object parent) where T : Visual
        {
            List<T> visualCollection = new List<T>();
            GetVisualChildCollection(parent as DependencyObject, visualCollection);
            return visualCollection;
        }

        private static void GetVisualChildCollection<T>(DependencyObject parent, List<T> visualCollection) where T : Visual
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T)
                {
                    visualCollection.Add(child as T);
                }
                if (child != null)
                {
                    GetVisualChildCollection(child, visualCollection);
                }
            }
        }

        #endregion // Get Visuals
    }


    public class RowToIndexConverter : MarkupExtension, IValueConverter
    {
        static RowToIndexConverter converter;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            DataGridRow row = value as DataGridRow;
            if (row != null)
            {
                int ind = row.GetIndex();
                return row.GetIndex() + 1;
            }
            else
                return -1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (converter == null) converter = new RowToIndexConverter();
            return converter;
        }

        public RowToIndexConverter()
        {
        }
    }

    /// <summary>
    /// Interaction logic for SimConfigToolWindow.xaml
    /// </summary>
    public partial class SimConfigToolWindow : ToolWindow
    {
        private static bool newCellPopSelected = true;
        public SimConfigToolWindow()
        {
            InitializeComponent();
        }

        private void AddCellButton_Click(object sender, RoutedEventArgs e)
        {
            CellsDetailsExpander.IsExpanded = true;
            CellPopulation cs = new CellPopulation();
            cs.cell_guid_ref = MainWindow.SC.SimConfig.entity_repository.cells[0].cell_guid;
            cs.cellpopulation_name = "New cell";           
            cs.number = 50;
            CellLocation cl = new CellLocation();
            cl.X = 0; cl.Y = 0; cl.Z = 0;
            cs.cell_locations.Add(cl);
            cs.cellpopulation_constrained_to_region = false;
            cs.cellpopulation_color = System.Windows.Media.Color.FromScRgb(1.0f, 1.0f, 0.5f, 0.0f);
            MainWindow.SC.SimConfig.scenario.cellpopulations.Add(cs);
            CellPopsListBox.SelectedIndex = CellPopsListBox.Items.Count - 1;
        }

        private void RemoveCellButton_Click(object sender, RoutedEventArgs e)
        {
            CellPopulation current_item = (CellPopulation)CellPopsListBox.SelectedItem;
            MainWindow.SC.SimConfig.scenario.cellpopulations.Remove(current_item);
        }

        // Utility function used in AddGaussSpecButton_Click() and SolfacTypeComboBox_SelectionChanged()
        private void AddGaussianSpecification()
        {
            BoxSpecification box = new BoxSpecification();
            box.x_trans = 200;
            box.y_trans = 200;
            box.z_trans = 200;
            box.x_scale = 200;
            box.y_scale = 200;
            box.z_scale = 200;
            // Add box GUI property changed to VTK callback
            //////////box.PropertyChanged += MainWindow.SC.GUIInteractionToWidgetCallback;
            MainWindow.SC.SimConfig.entity_repository.box_specifications.Add(box);

            GaussianSpecification gg = new GaussianSpecification();
            gg.gaussian_spec_box_guid_ref = box.box_guid;
            gg.gaussian_spec_name = "New on-center gradient";
            gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            // Add gauss spec property changed to VTK callback (ellipsoid actor color & visibility)
            //////////gg.PropertyChanged += MainWindow.SC.GUIGaussianSurfaceVisibilityToggle;
            MainWindow.SC.SimConfig.entity_repository.gaussian_specifications.Add(gg);

            //////////// Add RegionControl & RegionWidget for the new gauss_spec
            //////////MainWindow.VTKBasket.AddGaussSpecRegionControl(gg);
            //////////MainWindow.GC.AddGaussSpecRegionWidget(gg);
            //////////// Connect the VTK callback
            //////////// TODO: MainWindow.GC.Regions[box.box_guid].SetCallback(new RegionWidget.CallbackHandler(this.WidgetInteractionToGUICallback));
            //////////MainWindow.GC.Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(MainWindow.GC.WidgetInteractionToGUICallback));
            //////////MainWindow.GC.Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(RegionFocusToGUISection));

            //////////MainWindow.GC.Rwc.Invalidate();
        }

        private void MolPopDistributionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only want to respond to purposeful user interaction, not just population and depopulation
            // of solfacs list
            ////////if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
            ////////    return;


            ConfigMolecularPopulation current_mol = (ConfigMolecularPopulation)lbEcsMolPops.SelectedItem;

            if (current_mol != null)
            {
                MolPopInfo current_item = current_mol.mpInfo;

                MolPopDistributionType new_dist_type = MolPopDistributionType.Gaussian;
                if (e.AddedItems.Count > 0)
                    new_dist_type = (MolPopDistributionType)e.AddedItems[0];

                
                // Only want to change distribution type if the combo box isn't just selecting 
                // the type of current item in the solfacs list box (e.g. when list selection is changed)

                if (current_item.mp_distribution == null)
                {
                }
                else if (current_item.mp_distribution.mp_distribution_type != null && current_item.mp_distribution.mp_distribution_type == new_dist_type)
                {
                    return;
                }
                //else
                //{
                    switch (new_dist_type)
                    {
                        case MolPopDistributionType.Homogeneous:
                            MolPopHomogeneousLevel shl = new MolPopHomogeneousLevel();
                            current_item.mp_distribution = shl;
                            break;
                        case MolPopDistributionType.LinearGradient:
                            MolPopLinearGradient slg = new MolPopLinearGradient();
                            current_item.mp_distribution = slg;
                            break;
                        case MolPopDistributionType.Gaussian:
                            // Make sure there is at least one gauss_spec in repository
                            if (MainWindow.SC.SimConfig.entity_repository.gaussian_specifications.Count == 0)
                            {
                                this.AddGaussianSpecification();
                            }
                            MolPopGaussianGradient sgg = new MolPopGaussianGradient();
                            sgg.gaussgrad_gauss_spec_guid_ref = MainWindow.SC.SimConfig.entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
                            current_item.mp_distribution = sgg;
                            break;
                        case MolPopDistributionType.CustomGradient:

                            var prev_distribution = current_item.mp_distribution;
                            MolPopCustomGradient scg = new MolPopCustomGradient();
                            current_item.mp_distribution = scg;

                            // Configure open file dialog box
                            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                            dlg.InitialDirectory = MainWindow.appPath;
                            dlg.DefaultExt = ".txt"; // Default file extension
                            dlg.Filter = "Custom chemokine field files (.txt)|*.txt"; // Filter files by extension

                            // Show open file dialog box
                            Nullable<bool> result = dlg.ShowDialog();

                            // Process open file dialog box results
                            if (result == true)
                            {
                                // Save filename here, but deserialization will happen in lockAndResetSim->initialState call
                                string filename = dlg.FileName;
                                scg.custom_gradient_file_string = filename;
                            }
                            else
                            {
                                current_item.mp_distribution = prev_distribution;
                            }
                            break;
                        default:
                            throw new ArgumentException("MolPopInfo distribution type out of range");
                    }
                //}
            }
        }

        /// <summary>
        /// Event handler for button press for changing custom chemokine distribution input file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SolfacCustomGradientFile_Click(object sender, RoutedEventArgs e)
        {
            MolPopInfo current_item = (MolPopInfo)lbEcsMolPops.SelectedItem;

            // Configure open file dialog box
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.InitialDirectory = Path.GetDirectoryName(((MolPopCustomGradient)current_item.mp_distribution).custom_gradient_file_uri.LocalPath);
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.Filter = "Custom chemokine field files (.txt)|*.txt"; // Filter files by extension

            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                // Save filename here, but deserialization will happen in lockAndResetSim->initialState call
                string filename = dlg.FileName;
                ((MolPopCustomGradient)current_item.mp_distribution).custom_gradient_file_string = filename;
            }
        }

        /// <summary>
        /// switch to the sim setup panel
        /// </summary>
        public void SelectSimSetupInGUI()
        {
            ConfigTabControl.SelectedIndex = 0;
        }

        /// <summary>
        /// switch to the sim setup panel, set the experiment name, highlight it, and set the focus for the box
        /// </summary>
        /// <param name="exp_name">new name</param>
        public void SelectSimSetupInGUISetExpName(string exp_name)
        {
            SelectSimSetupInGUI();
            experiment_name_box.Text = exp_name;
            experiment_name_box.SelectAll();
            experiment_name_box.Focus();
        }

        public void SelectSolfacInGUI(int index)
        {
            // Solfacs are in the second tab panel
            ConfigTabControl.SelectedIndex = 1;
            // Use list index here since not all solfacs.mp_distribution have this guid field
            lbEcsMolPops.SelectedIndex = index;
        }

        private void btnAddMolec_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecule gm = new ConfigMolecule();
            gm.ReadOnly = false;
            gm.ForegroundColor = System.Windows.Media.Colors.Black;
            //MainWindow.SC.SimConfig.entity_repository.UserdefMolecules.Add(gm);
            MainWindow.SC.SimConfig.entity_repository.molecules.Add(gm);
            lbEcsMolPops.SelectedIndex = lbEcsMolPops.Items.Count - 1;

            lbMol.SelectedIndex = lbMol.Items.Count - 1;
        }

        private void btnCopyMolec_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecule cm = (ConfigMolecule)lbMol.SelectedItem;

            if (cm == null)
                return;

            ConfigMolecule gm = new ConfigMolecule(cm);
            gm.ReadOnly = false;
            gm.ForegroundColor = System.Windows.Media.Colors.Black;
            MainWindow.SC.SimConfig.entity_repository.molecules.Add(gm);
            lbMol.SelectedIndex = lbMol.Items.Count - 1;
        }

        private void AddEcmMolButton_Click(object sender, RoutedEventArgs e)
        {
            //SolfacsDetailsExpander.IsExpanded = true;
            // Default to HomogeneousLevel for now...

            ConfigMolecularPopulation gmp = new ConfigMolecularPopulation();
            gmp.Name = "NewMP";
            gmp.mpInfo = new MolPopInfo("");
            gmp.mpInfo.mp_dist_name = "New distribution";
            gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 1.0f, 0.2f);
            MainWindow.SC.SimConfig.scenario.environment.ecs.molpops.Add(gmp);
            lbEcsMolPops.SelectedIndex = lbEcsMolPops.Items.Count - 1;
        }
        private void RemoveEcmMolButton_Click(object sender, RoutedEventArgs e)
        {
            int nIndex = lbEcsMolPops.SelectedIndex;
            if ( nIndex >= 0) {
                ConfigMolecularPopulation gmp = (ConfigMolecularPopulation)lbEcsMolPops.SelectedValue;
                MainWindow.SC.SimConfig.scenario.environment.ecs.molpops.Remove(gmp);
            }
        }
        private void AddEcmReacButton_Click(object sender, RoutedEventArgs e)
        {            
            ConfigReaction selected_grt = (ConfigReaction)lbAvailableReacs.SelectedItem;
            if (!MainWindow.SC.SimConfig.scenario.environment.ecs.reactions_guid_ref.Contains(selected_grt.reaction_guid))
            {
                //selected_grt.GetTotalReactionString(MainWindow.SC.SimConfig.entity_repository);
                MainWindow.SC.SimConfig.scenario.environment.ecs.reactions_guid_ref.Add(selected_grt.reaction_guid);
            }

        }
        private void RemoveEcmReacButton_Click(object sender, RoutedEventArgs e)
        {
            int nIndex = lbEcsReactions.SelectedIndex;
            if (nIndex >= 0)
            {
                ConfigReaction grt = (ConfigReaction)lbEcsReactions.SelectedValue;
                MainWindow.SC.SimConfig.entity_repository.reactions.Remove(grt);
                if (MainWindow.SC.SimConfig.scenario.environment.ecs.reactions_guid_ref.Contains(grt.reaction_guid))
                    MainWindow.SC.SimConfig.scenario.environment.ecs.reactions_guid_ref.Remove(grt.reaction_guid);
            }
        }
        private void AddReacCompButton_Click(object sender, RoutedEventArgs e)
        {
            ////GuiReactionComplex grc = new GuiReactionComplex();
            ////GuiReactionComplex sel = (GuiReactionComplex)lbAvailableReacCx.SelectedItem;
            ////grc = sel;
            ////MainWindow.SC.SimConfig.scenario.ReactionComplexes.Add(grc);
            ////AddReacCxExpander.IsExpanded = !AddReacCxExpander.IsExpanded;
        }
        private void RemoveReacCompButton_Click(object sender, RoutedEventArgs e)
        {
            ////int nIndex = ReactionComplexListBox.SelectedIndex;
            ////if (nIndex >= 0)
            ////{
            ////    GuiReactionComplex grc = (GuiReactionComplex)ReactionComplexListBox.SelectedValue;
            ////    MainWindow.SC.SimConfig.scenario.ReactionComplexes.Remove(grc);
            ////}
        }
        private void AddReacCompExpandButton_Click(object sender, RoutedEventArgs e)
        {            
            AddReacCxExpander.IsExpanded = !AddReacCxExpander.IsExpanded;
        }

        //Reaction Complexes/Differentiation Schemes tab
        private void btnAddComplex_Click(object sender, RoutedEventArgs e)
        {
            ////AddReacComplex arc = new AddReacComplex(ReactionComplexDialogType.AddComplex);
            ////if (arc.ShowDialog() == true)
            ////{
            ////    ////////lbComplexes.ItemsSource = null;
            ////    ////////lbComplexes.ItemsSource = Sim.RCList;
            ////    ////////lbComplexes.Focus();
            ////    ////////lbComplexes.SelectedItem = 0;
            ////}
        }

        private void btnEditComplex_Click(object sender, RoutedEventArgs e)
        {
            ////AddReacComplex arc = new AddReacComplex(ReactionComplexDialogType.EditComplex);
            ////if (arc.ShowDialog() == true)
            ////{
            ////    ////////lbComplexes.ItemsSource = null;
            ////    ////////lbComplexes.ItemsSource = Sim.RCList;
            ////}
        }

        private void btnRemoveComplex_Click(object sender, RoutedEventArgs e)
        {
            ////////ReactionComplex rc = (ReactionComplex)(lbComplexes.SelectedItem);
            ////////if (rc != null)
            ////////{
            ////////    Sim.RCList.Remove(rc);
            ////////    lbComplexes.ItemsSource = null;
            ////////    lbComplexes.ItemsSource = Sim.RCList;
            ////////    lbComplexes.SelectedItem = 0;
            ////////}
        }

        private void btnGraphComplex_Click(object sender, RoutedEventArgs e)
        {
            ////GuiReactionComplex grc = (GuiReactionComplex)(lbComplexes.SelectedItem);
            ////ReactionComplexSimulation rcs = new ReactionComplexSimulation(grc);
            ////rcs.Go();
            //grc.Run();

            //DependencyObject do = this.Parent;



            //*******************************
            //THIS IS NOT OK
            //*****************************
            ////////MainWindow.SC.SimConfig.ChartWindow.Title = "Reaction Complex: " + grc.Name;
            ////////MainWindow.SC.SimConfig.ChartWindow.RC = rcs.RC;
            ////////MainWindow.SC.SimConfig.ChartWindow.Activate();
            ////////MainWindow.SC.SimConfig.ChartWindow.Render();
            ////////MainWindow.SC.SimConfig.ChartWindow.slMaxTime.Maximum = rcs.RC.dMaxTime;
            ////////MainWindow.SC.SimConfig.ChartWindow.slMaxTime.Value = rcs.RC.dInitialTime;
              
            //////////rc = Sim.FindReactionComplex(rcname);

            ////////////The Go function calculates the initial list (dictionary) of concs for each molecule   
            //////////rc.Go();

            //ReacComplexChartWindow.Title = "Reaction Complex: " + grc.Name;

            //ChartViewToolWindow tw = (ChartViewToolWindow)(MainWindow.GetWindow(ReacComplexChartWindow));

            //tw.Title = "Reaction Complex: " + grc.Name;
            //////////ReacComplexChartWindow.RC = rc;
            //tw.Activate();
            //tw.Render();
            //////////ReacComplexChartWindow.slMaxTime.Maximum = rc.dMaxTime;
            //////////ReacComplexChartWindow.slMaxTime.Value = rc.dInitialTime;

        }

        private void cbCellPopDistributionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CellPopulation cp = (CellPopulation)CellPopsListBox.SelectedItem;
            CellPopDistributionType distType = (CellPopDistributionType)e.AddedItems[0];

            if (distType == CellPopDistributionType.Probability)
            {
                //lbCellPopDistributionSubType
                cp.cellPopDist = new CellPopUniformDistribution(5.0);
            }
            //current_item.mp_distribution = slg;

        }

        private void lbCellPopDistSubType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int index = 0;  //CellPopsDetailsExpander
        }

        private void CellAddReacCxButton_Click(object sender, RoutedEventArgs e)
        {
            ////if (lbCellAvailableReacCx.SelectedIndex != -1)
            ////{
            ////    GuiReactionComplex grc = (GuiReactionComplex)lbCellAvailableReacCx.SelectedValue;
            ////    if (!MainWindow.SC.SimConfig.scenario.ReactionComplexes.Contains(grc))
            ////        MainWindow.SC.SimConfig.scenario.ReactionComplexes.Add(grc);
            ////}
        }

        private void btnRemoveMolec_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecule gm = (ConfigMolecule)lbMol.SelectedValue;
            if (gm.ReadOnly == false)
            {
                int index = lbMol.SelectedIndex;
                //MainWindow.SC.SimConfig.entity_repository.UserdefMolecules.Remove(gm);
                MainWindow.SC.SimConfig.entity_repository.molecules.Remove(gm);

                lbMol.SelectedIndex = index;

                if (index >= lbMol.Items.Count)
                    lbMol.SelectedIndex = lbMol.Items.Count - 1;

                if (lbMol.Items.Count == 0)
                    lbMol.SelectedIndex = -1;

            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Cannot remove a predefined molecule");
            }
        }

        private void btnAddReaction_Click(object sender, RoutedEventArgs e)
        {
        }

        private void btnRemoveReaction_Click(object sender, RoutedEventArgs e)
        {
            ConfigReaction cr = (ConfigReaction)lvReactions.SelectedItem;
            if (cr.ReadOnly == true)
            {
                MessageBox.Show("Cannot remove a predefined reaction.");
            }
            else
            {
                MainWindow.SC.SimConfig.entity_repository.reactions.Remove(cr);
            }
        }

        private void MembraneAddReacButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cc = (ConfigCell)CellsListBox.SelectedItem;
            ConfigReaction cr = (ConfigReaction)lbCellAvailableReacs.SelectedItem;
            if (cc != null && cr != null)
                cc.membrane.reactions_guid_ref.Add(cr.reaction_guid);
        }

        private void CytosolAddReacButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cc = (ConfigCell)CellsListBox.SelectedItem;
            ConfigReaction cr = (ConfigReaction)lbCellAvailableReacs.SelectedItem;

            if (cc != null && cr != null)
                cc.cytosol.reactions_guid_ref.Add(cr.reaction_guid);
        }

        //private void rbBulk_Click(object sender, RoutedEventArgs e)
        //{
        //    if (rbBulk.IsChecked == true)
        //    {
        //        rbBoundary.IsChecked = false;
        //    }
        //}

        //private void rbBoundary_Click(object sender, RoutedEventArgs e)
        //{
        //    if (rbBoundary.IsChecked == true)
        //    {
        //        rbBulk.IsChecked = false;
        //    }
        //}

        private void lbMol_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConfigMolecule cm = (ConfigMolecule)lbMol.SelectedItem;
            if (cm == null)
                return;

            if (cm.molecule_location == MoleculeLocation.Boundary)
            {
                //rbBulk.IsChecked = false;
                //rbBoundary.IsChecked = true;
                chkBoundary.IsChecked = true;
            }
            else
            {
                //rbBulk.IsChecked = true;
                //rbBoundary.IsChecked = false;
                chkBoundary.IsChecked = false;
            }
        }

        private void ecmReactionCollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            ConfigReaction cr = e.Item as ConfigReaction;
            if (cr != null)
            {
                // Filter out cr if not in ecm reaction list 
                if (MainWindow.SC.SimConfig.scenario.environment.ecs.reactions_guid_ref.Contains(cr.reaction_guid))
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }

        private void membReactionCollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            ConfigReaction cr = e.Item as ConfigReaction;
            ConfigCell cc = (ConfigCell)CellsListBox.SelectedItem;
            if (cr != null && cc != null)
            {
                e.Accepted = false;
                // Filter out cr if not in ecm reaction list 
                if (cc.membrane.reactions_guid_ref.Contains(cr.reaction_guid))
                {
                    e.Accepted = true;
                }
                
            }
        }

        private void cytosolReactionsCollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            ConfigReaction cr = e.Item as ConfigReaction;
            ConfigCell cc = (ConfigCell)CellsListBox.SelectedItem;
            if (cr != null && cc != null)
            {
                // Filter out cr if not in ecm reaction list 
                if (cc.cytosol.reactions_guid_ref.Contains(cr.reaction_guid))
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }

        private void MembraneRemoveReacButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void CytosolRemoveReacButton_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AddCellButton_Click_1(object sender, RoutedEventArgs e)
        {
            ConfigCell cc = new ConfigCell();
            cc.CellName = "DefaultCell";
            cc.CellRadius = 10;   
            cc.TransductionConstant = 0;
            MainWindow.SC.SimConfig.entity_repository.cells.Add(cc);
            CellsListBox.SelectedIndex = CellsListBox.Items.Count - 1;
        }

        private void CollectionViewSource_Filter(object sender, FilterEventArgs e)
        {

        }

        private void membraneMolPopsCollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            
        }

        private void cytosolMoleculesCollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
        }

        private void MembraneAddMolButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecularPopulation gmp = new ConfigMolecularPopulation();
            gmp.Name = "NewMP";
            gmp.mpInfo = new MolPopInfo("");
            gmp.mpInfo.mp_dist_name = "New distribution";
            gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 1.0f, 0.2f);
            gmp.mpInfo.mp_render_on = true;
            gmp.mpInfo.mp_distribution = new MolPopHomogeneousLevel();

            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            cell.membrane.molpops.Add(gmp);
            CellMembraneMolPopsListBox.SelectedIndex = CellMembraneMolPopsListBox.Items.Count - 1;
        }
        private void MembraneRemoveMolButton_Click(object sender, RoutedEventArgs e)
        {            
            ConfigMolecularPopulation cmp = (ConfigMolecularPopulation)CellMembraneMolPopsListBox.SelectedItem;
            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            cell.membrane.molpops.Remove(cmp);
            CellMembraneMolPopsListBox.SelectedIndex = CellMembraneMolPopsListBox.Items.Count - 1;
        }
        private void CytosolAddMolButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecularPopulation gmp = new ConfigMolecularPopulation();
            gmp.Name = "NewMP";
            gmp.mpInfo = new MolPopInfo("");
            gmp.mpInfo.mp_dist_name = "New distribution";
            gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 1.0f, 0.2f);
            gmp.mpInfo.mp_render_on = true;
            gmp.mpInfo.mp_distribution = new MolPopHomogeneousLevel();

            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            cell.cytosol.molpops.Add(gmp);
            CellCytosolMolPopsListBox.SelectedIndex = CellCytosolMolPopsListBox.Items.Count - 1;
        }
        private void CytosolRemoveMolButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecularPopulation cmp = (ConfigMolecularPopulation)CellCytosolMolPopsListBox.SelectedItem;

            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            cell.cytosol.molpops.Remove(cmp);
            CellCytosolMolPopsListBox.SelectedIndex = CellCytosolMolPopsListBox.Items.Count - 1;
        }
        
        private void CellsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            if (cell == null)
                return;

            MembReacListBox.Items.Clear();
            foreach (string guid in cell.membrane.reactions_guid_ref) 
            {
                if (MainWindow.SC.SimConfig.entity_repository.reactions_dict.ContainsKey(guid))
                    MembReacListBox.Items.Add(MainWindow.SC.SimConfig.entity_repository.reactions_dict[guid]);
            }

            CytosolReacListBox.Items.Clear();
            foreach (string guid in cell.cytosol.reactions_guid_ref)
            {
                if (MainWindow.SC.SimConfig.entity_repository.reactions_dict.ContainsKey(guid))
                    CytosolReacListBox.Items.Add(MainWindow.SC.SimConfig.entity_repository.reactions_dict[guid]);
            }
        }

        private void CellMembraneMolPopsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //ListBox lb = (ListBox)e.Source;
            //ConfigMolecularPopulation cmp = (ConfigMolecularPopulation)lb.SelectedItem;
            //string molname = MainWindow.SC.SimConfig.entity_repository.molecules_dict[cmp.molecule_guid_ref].Name;
        }

        private void CellCytosolMolPopsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Binding b = new Binding();
            //b.ElementName = "CellCytosolMolPopsListBox";
            //PropertyPath pp = new PropertyPath(CellCytosolMolPopsListBox.SelectedItem);
            //DependencyProperty dp;
            //dp.PropertyType = CellCytosolMolPopsListBox.SelectedItem.GetType();
            //b.Path = pp;
            //CytoMolPopDetails.SetBinding(dp, b);
            
            //cc.SetBinding(
            //MembMolPopDetails.Content = cont
        }

        private void cbCellLocationType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            if (cb.SelectedIndex == -1)
                return;

            CellPopDistributionType cpdt = (CellPopDistributionType)cb.SelectedItem;
            if (cpdt == CellPopDistributionType.Probability)
            {
                
            }
            //ListBoxItem lbi = ((sender as ListBox).SelectedItem as ListBoxItem);

        }

        private void CellPopDistributionTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only want to respond to purposeful user interaction, not just population and depopulation
            // of solfacs list

            ////if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
            ////    return;

            ////CellPopulation current_cell_pop = (CellPopulation)CellPopsListBox.SelectedItem;

            ////if (current_cell_pop != null)
            ////{
            ////    MolPopInfo current_item = current_cell_pop.cpProbInfo;
            ////    CellPopProbDistributionType new_dist_type = (CellPopProbDistributionType)e.AddedItems[0];

            ////    // Only want to change distribution type if the combo box isn't just selecting 
            ////    // the type of current item in the solfacs list box (e.g. when list selection is changed)
                
            ////    if (current_item.mp_distribution.mp_distribution_type == null) {
            ////    }
            ////    else { 
            ////        MolPopDistributionType m = current_item.mp_distribution.mp_distribution_type;
            ////        if ((m == MolPopDistributionType.Homogeneous && new_dist_type == CellPopProbDistributionType.Uniform) || 
            ////            (m == MolPopDistributionType.Gaussian && new_dist_type == CellPopProbDistributionType.Gaussian))
            ////            return;
            ////    }
            ////    switch (new_dist_type)
            ////    {
            ////        case CellPopProbDistributionType.Uniform:
            ////            MolPopHomogeneousLevel shl = new MolPopHomogeneousLevel();
            ////            current_item.mp_distribution = shl;
            ////            break;
            ////        case CellPopProbDistributionType.Gaussian:
            ////            // Make sure there is at least one gauss_spec in repository
            ////            if (MainWindow.SC.SimConfig.entity_repository.gaussian_specifications.Count == 0)
            ////            {
            ////                this.AddGaussianSpecification();
            ////            }
            ////            MolPopGaussianGradient sgg = new MolPopGaussianGradient();
            ////            sgg.gaussgrad_gauss_spec_guid_ref = MainWindow.SC.SimConfig.entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
            ////            current_item.mp_distribution = sgg;
            ////            break;
            ////        default:
            ////            throw new ArgumentException("CellPopProbInfo distribution type out of range");
            ////    }
                
            ////}
        }

        private void cell_type_combo_box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //ConfigCell cc 

            ComboBox cb = (ComboBox)e.Source;
            if (cb == null)
                return;

            //ConfigCell cc = (ConfigCell)cb.SelectedItem;
            //string cellname = MainWindow.SC.SimConfig.entity_repository.cells_dict[cc.cell_guid].CellName;
            CellPopulation cp = (CellPopulation)CellPopsListBox.SelectedItem;
            if (cp == null)
                return;

            int nIndex = cb.SelectedIndex;
            if (nIndex < 0)
                return;

            cp.cell_guid_ref = MainWindow.SC.SimConfig.entity_repository.cells[nIndex].cell_guid;

        }

        private void numberBox_ValueChanged(object sender, ActiproSoftware.Windows.PropertyChangedRoutedEventArgs<int?> e)
        {
            if (e.OldValue == null || e.NewValue == null)
                return;

            int numOld = (int)e.OldValue;
            int numNew = (int)e.NewValue;

            if (numNew == numOld)
                return;
            
            CellPopulation cp = (CellPopulation)CellPopsListBox.SelectedItem;
            if (cp == null)
                return;

            if (numNew > numOld && numNew > cp.cell_locations.Count)
            {
                int rows_to_add = numNew - numOld;
                for (int i = 0; i < rows_to_add; i++)
                {
                    CellLocation cl = new CellLocation();
                    cl.X = 1; cl.Y = 1; cl.Z = 1;
                    cp.cell_locations.Add(cl);                    
                }
            }
            else if (numNew < numOld)
            {
                if (numOld > cp.cell_locations.Count)
                    numOld = cp.cell_locations.Count;

                int rows_to_delete = numOld - numNew;
            
                for (int i = rows_to_delete; i > 0; i--)
                {
                    cp.cell_locations.RemoveAt(numNew + i - 1);
                }
            }
            cp.number = cp.cell_locations.Count;

        }

        private void cellPopsListBoxSelChanged(object sender, SelectionChangedEventArgs e)
        {
            newCellPopSelected = true;
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        private void dgLocations_KeyDown(object sender, KeyEventArgs e)
        {
            CellPopulation cp = (CellPopulation)CellPopsListBox.SelectedItem;
            if (cp == null)
                return;

            if (e.Key == Key.V &&
                (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                string s = (string)Clipboard.GetData(DataFormats.Text);

                char[] delim = { '\t', '\r', '\n' };
                string[] paste = s.Split(delim, StringSplitOptions.RemoveEmptyEntries);

                cp.cell_locations.Clear();
                for (int i = 0; i < paste.Length; i += 3)
                {
                    CellLocation cl = new CellLocation(double.Parse(paste[i]), double.Parse(paste[i + 1]), double.Parse(paste[i + 2]));
                    cp.cell_locations.Add(cl);
                }

                cp.number = cp.cell_locations.Count;

            }

            
        }

        private void menuCoordinatesPaste_Click(object sender, RoutedEventArgs e)
        {
            CellPopulation cp = (CellPopulation)CellPopsListBox.SelectedItem;
            if (cp == null)
                return;

            string s = (string)Clipboard.GetData(DataFormats.Text);

            char[] delim = { '\t', '\r', '\n' };
            string[] paste = s.Split(delim, StringSplitOptions.RemoveEmptyEntries);

            cp.cell_locations.Clear();
            for (int i = 0; i < paste.Length; i += 3)
            {
                CellLocation cl = new CellLocation(double.Parse(paste[i]), double.Parse(paste[i + 1]), double.Parse(paste[i + 2]));
                cp.cell_locations.Add(cl);
            }
            cp.number = cp.cell_locations.Count;
            e.Handled = true;

        }

        private void dgLocations_Scroll(object sender, RoutedEventArgs e)
        {
            DataGrid dgData = (DataGrid)sender;
            //BindingExpression b = dgData.GetBindingExpression(System.Windows.Controls.DataGrid.ItemsSourceProperty);
            //b.UpdateTarget();
            //if (e.RoutedEvent.Name == 


            //IF CURRENTLY EDITING A CELL, WANT TO PREVENT CALLING REFRESH!  HOW TO DO?
            //if (dgData.IsEditing())  //DOESN'T WORK
            //if (dgData.SelectedIndex > -1)
            //    return;

            //if (dgData.SelectedCells.Count > 0)
            //{
            //    //DataGridCellInfo dgci = dgData.SelectedCells[0];
            //    DataGridCellInfo dgci = (DataGridCellInfo)dgData.SelectedCells[0];
            //    DataGridCell dgc = TryToFindGridCell(dgData, dgci);

            //    bool bEditing = dgc.IsEditing;
            //    if (bEditing == true)
            //        return;
            //}

            try
            {
                dgData.Items.Refresh();
            }
            catch 
            {
            }
        }

        private void dgLocations_Unloaded(Object sender, RoutedEventArgs e)
        {
            CellPopulation cp = (CellPopulation)CellPopsListBox.SelectedItem;
            if (cp == null)
                return;
            cp.number = cp.cell_locations.Count;
        }

        static DataGridCell TryToFindGridCell(DataGrid grid, DataGridCellInfo cellInfo)
        {
            DataGridCell result = null;
            DataGridRow row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(cellInfo.Item);
            if (row != null)
            {
                int columnIndex = grid.Columns.IndexOf(cellInfo.Column);
                if (columnIndex > -1)
                {
                    DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(row);
                    result = presenter.ItemContainerGenerator.ContainerFromIndex(columnIndex) as DataGridCell;
                }
            }
            return result;
        }

        static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

    }



    
}
