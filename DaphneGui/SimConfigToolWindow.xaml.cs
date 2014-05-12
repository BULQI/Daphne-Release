using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ActiproSoftware.Windows.Controls.Docking;


using Daphne;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Windows.Markup;
//using System.Windows.Forms;
using Microsoft.Win32;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Linq;

using Ninject;
using Ninject.Parameters;

using Workbench;
using Newtonsoft.Json;

namespace DaphneGui
{

    /// <summary>
    /// Interaction logic for SimConfigToolWindow.xaml
    /// </summary>
    public partial class SimConfigToolWindow : ToolWindow
    {        

        //private static bool newCellPopSelected = true;
        public SimConfigToolWindow()
        {
            InitializeComponent();
        }

        public MainWindow MW { get; set; }

        private void AddCellPopButton_Click(object sender, RoutedEventArgs e)
        {
            CellsDetailsExpander.IsExpanded = true;

            // Some relevant CellPopulation constructor defaults: 
            //      number = 1
            //      no instantiation of cellPopDist
            CellPopulation cs = new CellPopulation();

            // Default cell type and name to first entry in the cell repository
            cs.cell_guid_ref = MainWindow.SC.SimConfig.entity_repository.cells[0].cell_guid;
            cs.cellpopulation_name = MainWindow.SC.SimConfig.entity_repository.cells[0].CellName;

            double[] extents = new double[3] { MainWindow.SC.SimConfig.scenario.environment.extent_x, 
                                               MainWindow.SC.SimConfig.scenario.environment.extent_y, 
                                               MainWindow.SC.SimConfig.scenario.environment.extent_z };
            double minDisSquared = 2*MainWindow.SC.SimConfig.entity_repository.cells_dict[cs.cell_guid_ref].CellRadius;
            minDisSquared *= minDisSquared;

            // Default is uniform probability distribution
            cs.cellPopDist = new CellPopUniform(extents, minDisSquared, cs);

            cs.cellpopulation_constrained_to_region = false;
            cs.cellpopulation_color = System.Windows.Media.Color.FromScRgb(1.0f, 1.0f, 0.5f, 0.0f);
            MainWindow.SC.SimConfig.scenario.cellpopulations.Add(cs);
            CellPopsListBox.SelectedIndex = CellPopsListBox.Items.Count - 1;
        }

        private void RemoveCellPopButton_Click(object sender, RoutedEventArgs e)
        {
            int index = CellPopsListBox.SelectedIndex;
            CellPopulation current_item = (CellPopulation)CellPopsListBox.SelectedItem;

            MessageBoxResult res;
            res = MessageBox.Show("Are you sure you would like to remove this cell population?", "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;
                       
            MainWindow.SC.SimConfig.scenario.cellpopulations.Remove(current_item);

            CellPopsListBox.SelectedIndex = index;

            if (index >= CellPopsListBox.Items.Count)
                CellPopsListBox.SelectedIndex = CellPopsListBox.Items.Count - 1;

            if (CellPopsListBox.Items.Count == 0)
                CellPopsListBox.SelectedIndex = -1;
        }

        // Utility function used in AddGaussSpecButton_Click() and SolfacTypeComboBox_SelectionChanged()
        private void AddGaussianSpecification(MolPopGaussian mpg)
        {
            BoxSpecification box = new BoxSpecification();
            box.x_trans = 100;
            box.y_trans = 100;
            box.z_trans = 100;
            box.x_scale = 100;
            box.y_scale = 100;
            box.z_scale = 100;
            // Add box GUI property changed to VTK callback
            box.PropertyChanged += MainWindow.GUIInteractionToWidgetCallback;
            MainWindow.SC.SimConfig.entity_repository.box_specifications.Add(box);

            GaussianSpecification gg = new GaussianSpecification();
            gg.gaussian_spec_box_guid_ref = box.box_guid;
            gg.gaussian_spec_name = "New on-center gradient";
            gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            // Add gauss spec property changed to VTK callback (ellipsoid actor color & visibility)
            gg.PropertyChanged += MainWindow.GUIGaussianSurfaceVisibilityToggle;
            MainWindow.SC.SimConfig.entity_repository.gaussian_specifications.Add(gg);
            mpg.gaussgrad_gauss_spec_guid_ref = gg.gaussian_spec_box_guid_ref;

            // Add RegionControl & RegionWidget for the new gauss_spec
            MainWindow.VTKBasket.AddGaussSpecRegionControl(gg);
            MainWindow.GC.AddGaussSpecRegionWidget(gg);
            // Connect the VTK callback
            // TODO: MainWindow.GC.Regions[box.box_guid].SetCallback(new RegionWidget.CallbackHandler(this.WidgetInteractionToGUICallback));
            MainWindow.GC.Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(MainWindow.GC.WidgetInteractionToGUICallback));
            MainWindow.GC.Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(RegionFocusToGUISection));

            MainWindow.GC.Rwc.Invalidate();
        }

        private void DeleteGaussianSpecification(MolPopDistribution dist)
        {
            MolPopGaussian mpg = dist as MolPopGaussian;
            string guid = mpg.gaussgrad_gauss_spec_guid_ref;

            if (MainWindow.SC.SimConfig.entity_repository.gauss_guid_gauss_dict.ContainsKey(guid))
            {
                GaussianSpecification gs = MainWindow.SC.SimConfig.entity_repository.gauss_guid_gauss_dict[guid];
                MainWindow.SC.SimConfig.entity_repository.gaussian_specifications.Remove(gs);
                MainWindow.SC.SimConfig.entity_repository.gauss_guid_gauss_dict.Remove(guid);
                MainWindow.GC.RemoveRegionWidget(guid);
            }

        }

        /// <summary>
        /// Add an instance of the default box to the entity repository.
        /// Default values: box center at center of ECS, box widths are 1/4 of ECS extents
        /// </summary>
        /// <param name="box"></param>
        private void AddDefaultBoxSpec(BoxSpecification box)
        {
            box.x_trans = MainWindow.SC.SimConfig.scenario.environment.extent_x / 2;
            box.y_trans = MainWindow.SC.SimConfig.scenario.environment.extent_y / 2;
            box.z_trans = MainWindow.SC.SimConfig.scenario.environment.extent_z / 2; ;
            box.x_scale = MainWindow.SC.SimConfig.scenario.environment.extent_x / 4; ;
            box.y_scale = MainWindow.SC.SimConfig.scenario.environment.extent_x / 4; ;
            box.z_scale = MainWindow.SC.SimConfig.scenario.environment.extent_x / 4; ;
            // Add box GUI property changed to VTK callback
            box.PropertyChanged += MainWindow.GUIInteractionToWidgetCallback;
            MainWindow.SC.SimConfig.entity_repository.box_specifications.Add(box);
        }

        // Utility function used in AddGaussSpecButton_Click() and SolfacTypeComboBox_SelectionChanged()
        private void AddGaussianSpecification(GaussianSpecification gg, BoxSpecification box)
        {
            gg.gaussian_spec_box_guid_ref = box.box_guid;
            gg.gaussian_spec_name = "";
            gg.gaussian_region_visibility = false;
            // gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            // Add gauss spec property changed to VTK callback (ellipsoid actor color & visibility)
            gg.PropertyChanged += MainWindow.GUIGaussianSurfaceVisibilityToggle;
            MainWindow.SC.SimConfig.entity_repository.gaussian_specifications.Add(gg);

            // Add RegionControl & RegionWidget for the new gauss_spec
            MainWindow.VTKBasket.AddGaussSpecRegionControl(gg);
            MainWindow.GC.AddGaussSpecRegionWidget(gg);
            // Connect the VTK callback
            // TODO: MainWindow.GC.Regions[box.box_guid].SetCallback(new RegionWidget.CallbackHandler(this.WidgetInteractionToGUICallback));
            MainWindow.GC.Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(MainWindow.GC.WidgetInteractionToGUICallback));
            MainWindow.GC.Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(RegionFocusToGUISection));

            MainWindow.GC.Rwc.Invalidate();
        }

        private void DeleteGaussianSpecification(CellPopDistribution dist)
        {
            CellPopGaussian cpg = dist as CellPopGaussian;
            string guid = cpg.gauss_spec_guid_ref;

            if (MainWindow.SC.SimConfig.entity_repository.gauss_guid_gauss_dict.ContainsKey(guid))
            {
                GaussianSpecification gs = MainWindow.SC.SimConfig.entity_repository.gauss_guid_gauss_dict[guid];
                MainWindow.SC.SimConfig.entity_repository.gaussian_specifications.Remove(gs);
                MainWindow.SC.SimConfig.entity_repository.gauss_guid_gauss_dict.Remove(guid);
                MainWindow.GC.RemoveRegionWidget(guid);

                //// Remove box
                //BoxSpecification box = MainWindow.SC.SimConfig.box_guid_box_dict[gs.gaussian_spec_box_guid_ref];
                //MainWindow.SC.SimConfig.entity_repository.box_specifications.Remove(box);
                //MainWindow.SC.SimConfig.box_guid_box_dict.Remove(gs.gaussian_spec_box_guid_ref);
                //// box.PropertyChanged += MainWindow.GUIInteractionToWidgetCallback;
                //MainWindow.SC.SimConfig.entity_repository.box_specifications.Remove(box);
            }

        }

        

        private void MolPopDistributionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only want to respond to purposeful user interaction, not just population and depopulation
            // of solfacs list
            if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
                return;


            ConfigMolecularPopulation current_mol = (ConfigMolecularPopulation)lbEcsMolPops.SelectedItem;

            if (current_mol != null)
            {
                MolPopInfo current_item = current_mol.mpInfo;
                MolPopDistributionType new_dist_type = MolPopDistributionType.Homogeneous; // = MolPopDistributionType.Gaussian;

                if (e.AddedItems.Count > 0)
                {
                    new_dist_type = (MolPopDistributionType)e.AddedItems[0];
                }


                // Only want to change distribution type if the combo box isn't just selecting 
                // the type of current item in the solfacs list box (e.g. when list selection is changed)

                if (current_item.mp_distribution == null)
                {
                }
                else if (current_item.mp_distribution.mp_distribution_type == new_dist_type)
                {
                    return;
                }

                if (current_item.mp_distribution != null)
                {
                    if (new_dist_type != MolPopDistributionType.Gaussian && current_item.mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
                    {
                        DeleteGaussianSpecification(current_item.mp_distribution);
                        MolPopGaussian mpg = current_item.mp_distribution as MolPopGaussian;
                        mpg.gaussgrad_gauss_spec_guid_ref = "";
                        MainWindow.GC.Rwc.Invalidate();
                    }
                }
                switch (new_dist_type)
                {
                    case MolPopDistributionType.Homogeneous:
                        MolPopHomogeneousLevel shl = new MolPopHomogeneousLevel();
                        current_item.mp_distribution = shl;
                        break;
                    case MolPopDistributionType.Linear:
                        //MolPopLinear cur = current_mol.mpInfo.mp_distribution as MolPopLinear;
                        //if (cur.boundaryCondition.Count != 2)
                        //{
                        //    MessageBox.Show("Specify values for boundary conditions first.");
                        //    return;
                        //}
                        MolPopLinear molpoplin = new MolPopLinear();                        
                        molpoplin.boundary_face = BoundaryFace.X;
                        molpoplin.dim = 0;
                        molpoplin.x1 = 0;
                        molpoplin.boundaryCondition = new List<BoundaryCondition>(); 
                        BoundaryCondition bc = new BoundaryCondition(MolBoundaryType.Dirichlet, Boundary.left, 0.0);                    
                        molpoplin.boundaryCondition.Add(bc);
                        bc = new BoundaryCondition(MolBoundaryType.Dirichlet, Boundary.right, 0.0);
                        molpoplin.boundaryCondition.Add(bc);
                        current_item.mp_dist_name = "Linear";
                        current_item.mp_distribution = molpoplin;
                        current_item.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                        current_item.mp_render_blending_weight = 2.0;
                        break;

                    case MolPopDistributionType.Gaussian:
                        // Make sure there is at least one gauss_spec in repository
                        ////if (MainWindow.SC.SimConfig.entity_repository.gaussian_specifications.Count == 0)
                        ////{
                        ////    this.AddGaussianSpecification();
                        ////}
                        MolPopGaussian mpg = new MolPopGaussian();

                        AddGaussianSpecification(mpg);
                        current_item.mp_distribution = mpg;

                        break;
                    
                    case MolPopDistributionType.Explicit:
                        break;

                    default:
                        throw new ArgumentException("MolPopInfo distribution type out of range");
                }
                //}
            }
        }

        private void MembraneMolPopDistributionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only want to respond to purposeful user interaction, not just population and depopulation
            // of solfacs list
            ////////if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
            ////////    return;


            ConfigMolecularPopulation current_mol = (ConfigMolecularPopulation)CellMembraneMolPopsListBox.SelectedItem;

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
                else if (current_item.mp_distribution.mp_distribution_type == new_dist_type)
                {
                    return;
                }
                switch (new_dist_type)
                {
                    case MolPopDistributionType.Homogeneous:
                        MolPopHomogeneousLevel shl = new MolPopHomogeneousLevel();
                        current_item.mp_distribution = shl;
                        break;
                    case MolPopDistributionType.Linear:
                        MolPopLinear slg = new MolPopLinear();
                        current_item.mp_distribution = slg;
                        break;
                    case MolPopDistributionType.Gaussian:
                        // Make sure there is at least one gauss_spec in repository
                        ////if (MainWindow.SC.SimConfig.entity_repository.gaussian_specifications.Count == 0)
                        ////{
                        ////    this.AddGaussianSpecification();
                        ////}
                        MolPopGaussian sgg = new MolPopGaussian();
                        //sgg.gaussgrad_gauss_spec_guid_ref = MainWindow.SC.SimConfig.entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
                        //string gauss_guid = sgg.gaussgrad_gauss_spec_guid_ref;
                        //MainWindow.SC.SimConfig.entity_repository.gaussian_specifications.Add(sgg);

                        GaussianSpecification gg = new GaussianSpecification();
                        BoxSpecification box = new BoxSpecification();
                        box.x_scale = 200;
                        box.y_scale = 200;
                        box.z_scale = 200;
                        box.x_trans = 500;
                        box.y_trans = 500;
                        box.z_trans = 500;
                        MainWindow.SC.SimConfig.entity_repository.box_specifications.Add(box);
                        gg.gaussian_spec_box_guid_ref = box.box_guid;
                        gg.gaussian_spec_name = "Off-center gaussian";
                        gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
                        MainWindow.SC.SimConfig.entity_repository.gaussian_specifications.Add(gg);
                        sgg.gaussgrad_gauss_spec_guid_ref = gg.gaussian_spec_box_guid_ref;
                        current_item.mp_distribution = sgg;
                        break;                    
                    default:
                        throw new ArgumentException("MolPopInfo distribution type out of range");
                }
            }
        }

        private void CytosolMolPopDistributionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only want to respond to purposeful user interaction, not just population and depopulation
            // of solfacs list
            ////////if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
            ////////    return;


            ConfigMolecularPopulation current_mol = (ConfigMolecularPopulation)CellCytosolMolPopsListBox.SelectedItem;

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
                else if (current_item.mp_distribution.mp_distribution_type == new_dist_type)
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
                    case MolPopDistributionType.Linear:
                        MolPopLinear slg = new MolPopLinear();
                        current_item.mp_distribution = slg;
                        break;
                    case MolPopDistributionType.Gaussian:
                        // Make sure there is at least one gauss_spec in repository
                        ////if (MainWindow.SC.SimConfig.entity_repository.gaussian_specifications.Count == 0)
                        ////{
                        ////    this.AddGaussianSpecification();
                        ////}
                        MolPopGaussian sgg = new MolPopGaussian();
                        sgg.gaussgrad_gauss_spec_guid_ref = MainWindow.SC.SimConfig.entity_repository.gaussian_specifications[0].gaussian_spec_box_guid_ref;
                        current_item.mp_distribution = sgg;
                        break;
                    //case MolPopDistributionType.Custom:

                    //    var prev_distribution = current_item.mp_distribution;
                    //    MolPopCustom scg = new MolPopCustom();
                    //    current_item.mp_distribution = scg;

                    //    // Configure open file dialog box
                    //    Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
                    //    dlg.InitialDirectory = MainWindow.appPath;
                    //    dlg.DefaultExt = ".txt"; // Default file extension
                    //    dlg.Filter = "Custom chemokine field files (.txt)|*.txt"; // Filter files by extension

                    //    // Show open file dialog box
                    //    Nullable<bool> result = dlg.ShowDialog();

                    //    // Process open file dialog box results
                    //    if (result == true)
                    //    {
                    //        // Save filename here, but deserialization will happen in lockAndResetSim->initialState call
                    //        string filename = dlg.FileName;
                    //        scg.custom_gradient_file_string = filename;
                    //    }
                    //    else
                    //    {
                    //        current_item.mp_distribution = prev_distribution;
                    //    }
                    //    break;
                    default:
                        throw new ArgumentException("MolPopInfo distribution type out of range");
                }
                //}
            }
        }

        ///// <summary>
        ///// Event handler for button press for changing custom chemokine distribution input file
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void SolfacCustomGradientFile_Click(object sender, RoutedEventArgs e)
        //{
        //    MolPopInfo current_item = (MolPopInfo)lbEcsMolPops.SelectedItem;

        //    // Configure open file dialog box
        //    Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
        //    dlg.InitialDirectory = Path.GetDirectoryName(((MolPopCustom)current_item.mp_distribution).custom_gradient_file_uri.LocalPath);
        //    dlg.DefaultExt = ".txt"; // Default file extension
        //    dlg.Filter = "Custom chemokine field files (.txt)|*.txt"; // Filter files by extension

        //    // Show open file dialog box
        //    Nullable<bool> result = dlg.ShowDialog();

        //    // Process open file dialog box results
        //    if (result == true)
        //    {
        //        // Save filename here, but deserialization will happen in lockAndResetSim->initialState call
        //        string filename = dlg.FileName;
        //        ((MolPopCustom)current_item.mp_distribution).custom_gradient_file_string = filename;
        //    }
        //}

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
            //ConfigTabControl.SelectedIndex = 1;
            // Use list index here since not all solfacs.mp_distribution have this guid field
            lbEcsMolPops.SelectedIndex = index;
        }        

        //ECM TAB EVENT HANDLERS
        private void AddEcmMolButton_Click(object sender, RoutedEventArgs e)
        {
            //SolfacsDetailsExpander.IsExpanded = true;
            // Default to HomogeneousLevel for now...

            if (MainWindow.SC.SimConfig.entity_repository.molecules.Count == 0)
            {
                MessageBox.Show("There are no molecules to choose from. Please start with a blank scenario or other scenario that contains molecules.");
                return;
            }
            
            ConfigMolecularPopulation gmp = new ConfigMolecularPopulation(ReportType.ECM_MP);
            
            gmp.molecule_guid_ref = MainWindow.SC.SimConfig.entity_repository.molecules[0].molecule_guid;
            gmp.Name = MainWindow.SC.SimConfig.entity_repository.molecules[0].Name;
            gmp.mpInfo = new MolPopInfo("");
            gmp.mpInfo.mp_dist_name = "New distribution";
            gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 1.0f, 0.2f);
            MainWindow.SC.SimConfig.scenario.environment.ecs.molpops.Add(gmp);
            lbEcsMolPops.SelectedIndex = lbEcsMolPops.Items.Count - 1;
        }
        private void RemoveEcmMolButton_Click(object sender, RoutedEventArgs e)
        {
            int index = lbEcsMolPops.SelectedIndex;
            if (index >= 0)
            {
                ConfigMolecularPopulation cmp = (ConfigMolecularPopulation)lbEcsMolPops.SelectedValue;

                MessageBoxResult res = MessageBox.Show("Removing this molecular population will remove ECM reactions that use this molecule. Are you sure you would like to proceed?", "Warning", MessageBoxButton.YesNo);                
                if (res == MessageBoxResult.No)
                    return;

                foreach (string reacguid in MainWindow.SC.SimConfig.scenario.environment.ecs.reactions_guid_ref.ToList())
                {
                    if (MainWindow.SC.SimConfig.entity_repository.reactions_dict[reacguid].HasMolecule(cmp.molecule_guid_ref))
                    {
                        MainWindow.SC.SimConfig.scenario.environment.ecs.reactions_guid_ref.Remove(reacguid);
                    }
                }
                MainWindow.SC.SimConfig.scenario.environment.ecs.molpops.Remove(cmp);

                CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();

                //ConfigReaction dummy = new ConfigReaction();
                //MainWindow.SC.SimConfig.entity_repository.reactions.Add(dummy);
                //MainWindow.SC.SimConfig.entity_repository.reactions.Remove(dummy);
                //AddReacExpander.InvalidateVisual();
                //lvAvailableReacs.Items.Clear();
                //lvAvailableReacs.InvalidateVisual();
                //CollectionViewSource cvs = ConfigTabControl.FindResource("ecmAvailableReacs") as CollectionViewSource;
                //cvs.View.Refresh();
                //lvAvailableReacs.
            }

            lbEcsMolPops.SelectedIndex = index;

            if (index >= lbEcsMolPops.Items.Count)
                lbEcsMolPops.SelectedIndex = lbEcsMolPops.Items.Count - 1;

            if (lbEcsMolPops.Items.Count == 0)
                lbEcsMolPops.SelectedIndex = -1;
        }

        private void bulkMoleculesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigMolecule mol = e.Item as ConfigMolecule;
            if (mol != null )
            {
                // Filter out mol if membrane bound 
                if (mol.molecule_location == MoleculeLocation.Bulk)
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }
        private void cytoBulkMoleculesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigMolecule mol = e.Item as ConfigMolecule;
            if (mol != null)
            {
                // Filter out mol if membrane bound 
                if (mol.molecule_location == MoleculeLocation.Bulk)
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }

        

        private bool EcmHasMolecule(string molguid)
        {
            foreach (ConfigMolecularPopulation molpop in MainWindow.SC.SimConfig.scenario.environment.ecs.molpops)
            {
                if (molpop.molecule_guid_ref == molguid)
                    return true;
            }
            return false;
        }
        private bool CellPopsHaveMoleculeInMemb(string molguid)
        {
            bool ret = false;
            foreach (CellPopulation cell_pop in MainWindow.SC.SimConfig.scenario.cellpopulations)
            {
                ConfigCell cell = MainWindow.SC.SimConfig.entity_repository.cells_dict[cell_pop.cell_guid_ref];
                if (MembraneHasMolecule(cell, molguid))
                    return true;
            }
            
            return ret;
        }
        //CellPopsHaveMoleculeInCytosol
        private bool CellPopsHaveMoleculeInCytosol(string molguid)
        {
            bool ret = false;
            foreach (CellPopulation cell_pop in MainWindow.SC.SimConfig.scenario.cellpopulations)
            {
                ConfigCell cell = MainWindow.SC.SimConfig.entity_repository.cells_dict[cell_pop.cell_guid_ref];
                if (CytosolHasMolecule(cell, molguid))
                    return true;
            }

            return ret;
        }

        private void AddEcmReacButton_Click(object sender, RoutedEventArgs e)
        {
            if (lvAvailableReacs.SelectedIndex == -1)
                return;

            ConfigReaction reac = (ConfigReaction)lvAvailableReacs.SelectedItem;

            //HERE MUST CHECK IF THE ECM HAS THE MOLECULES NEEDED BY THIS REACTION
            //Reactants
            foreach (string newmolguid in reac.reactants_molecule_guid_ref)
            {
                bool bFound = false;

                ConfigMolecule cm = MainWindow.SC.SimConfig.entity_repository.molecules_dict[newmolguid];
                if (cm != null)
                {
                    if (cm.molecule_location == MoleculeLocation.Boundary)
                    {
                        if (CellPopsHaveMoleculeInMemb(newmolguid))
                        {
                            bFound = true;
                        }
                    }
                    else
                    {
                        if (EcmHasMolecule(newmolguid))
                        {
                            bFound = true;
                        }
                    }

                    if (bFound == false)
                    {
                        string msg = string.Format("Molecule {0} not found in ECM or cell membranes.  Please add molecule before adding this reaction.", cm.Name);
                        MessageBox.Show(msg);
                        return;
                    }
                }
            }
            //Products
            foreach (string newmolguid in reac.products_molecule_guid_ref)
            {
                bool bFound = false;

                ConfigMolecule cm = MainWindow.SC.SimConfig.entity_repository.molecules_dict[newmolguid];
                if (cm != null)
                {
                    if (cm.molecule_location == MoleculeLocation.Boundary)
                    {
                        if (CellPopsHaveMoleculeInMemb(newmolguid))
                        {
                            bFound = true;
                        }
                    }
                    else
                    {
                        if (EcmHasMolecule(newmolguid))
                        {
                            bFound = true;
                        }
                    }

                    if (bFound == false)
                    {
                        string msg = string.Format("Molecule {0} not found in ECM or cell membranes.  Please add molecule before adding this reaction.", cm.Name);
                        MessageBox.Show(msg);
                        return;
                    }
                }
            }
            //Modifiers
            foreach (string newmolguid in reac.modifiers_molecule_guid_ref)
            {
                bool bFound = false;

                ConfigMolecule cm = MainWindow.SC.SimConfig.entity_repository.molecules_dict[newmolguid];
                if (cm != null)
                {
                    if (cm.molecule_location == MoleculeLocation.Boundary)
                    {
                        if (CellPopsHaveMoleculeInMemb(newmolguid))
                        {
                            bFound = true;
                        }
                    }
                    else
                    {
                        if (EcmHasMolecule(newmolguid))
                        {
                            bFound = true;
                        }
                    }

                    if (bFound == false)
                    {
                        string msg = string.Format("Molecule {0} not found in ECM or cell membranes.  Please add molecule before adding this reaction.", cm.Name);
                        MessageBox.Show(msg);
                        return;
                    }
                }
            }


            if (!MainWindow.SC.SimConfig.scenario.environment.ecs.reactions_guid_ref.Contains(reac.reaction_guid))
            {
                MainWindow.SC.SimConfig.scenario.environment.ecs.reactions_guid_ref.Add(reac.reaction_guid);
            }

        }
        private void RemoveEcmReacButton_Click(object sender, RoutedEventArgs e)
        {
            int nIndex = lvEcsReactions.SelectedIndex;
            if (nIndex >= 0)
            {
                string guid = (string)lvEcsReactions.SelectedValue;
                ConfigReaction grt = MainWindow.SC.SimConfig.entity_repository.reactions_dict[guid];
                if (MainWindow.SC.SimConfig.scenario.environment.ecs.reactions_guid_ref.Contains(grt.reaction_guid))
                {
                    MainWindow.SC.SimConfig.scenario.environment.ecs.reactions_guid_ref.Remove(grt.reaction_guid);
                }
            }
        }

        private void ecmAvailableReactionsListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigReaction cr = e.Item as ConfigReaction;

            bool bOK = false;
            foreach (string molguid in cr.reactants_molecule_guid_ref) {
                if (EcmHasMolecule(molguid) || CellPopsHaveMoleculeInMemb(molguid))
                {
                    bOK = true;
                }
                else
                {
                    bOK = false;
                    break;
                }
            }
            if (bOK)
            {
                foreach (string molguid in cr.products_molecule_guid_ref)
                {
                    if (EcmHasMolecule(molguid) || CellPopsHaveMoleculeInMemb(molguid))
                    {
                        bOK = true;
                    }
                    else
                    {
                        bOK = false;
                        break;
                    }
                }
            }
            if (bOK)
            {
                foreach (string molguid in cr.modifiers_molecule_guid_ref)
                {
                    if (EcmHasMolecule(molguid) || CellPopsHaveMoleculeInMemb(molguid))
                    {
                        bOK = true;
                    }
                    else
                    {
                        bOK = false;
                        break;
                    }
                }
            }

            e.Accepted = bOK;
        }

        private void membraneAvailableReactionsListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigReaction cr = e.Item as ConfigReaction;

            if (CellsListBox.SelectedIndex < 0)
            {
                e.Accepted = false;
                return;
            }

            ConfigCell cc = CellsListBox.SelectedItem as ConfigCell;

            //This filter is called for every reaction in the repository.
            //For current reaction, if all of its molecules are in the membrane, then the reaction should be included.
            //Otherwise, exclude it.

            //First check if all the molecules in the reactants list exist in the membrane
            bool bOK = false;
            bOK = cc.membrane.HasMolecules(cr.reactants_molecule_guid_ref);

            //If bOK is true, that means the molecules in the reactants list all exist in the membrane
            //Now check the products list
            if (bOK)
                bOK = cc.membrane.HasMolecules(cr.products_molecule_guid_ref);

            //Finally, loop through modifiers list
            if (bOK)
                bOK = cc.membrane.HasMolecules(cr.modifiers_molecule_guid_ref);

            e.Accepted = bOK;

            #region Old Code - remove after well tested
            //foreach (string molguid in cr.reactants_molecule_guid_ref)
            //{
            //    ConfigMolecule mol = MainWindow.SC.SimConfig.entity_repository.molecules_dict[molguid];
            //    if (cc.membrane.HasMolecule(mol))
            //    {
            //        bOK = true;
            //    }
            //    else
            //    {
            //        bOK = false;
            //        break;
            //    }
            //}

            //If bOK is true, that means the molecules in the reactants list all exist in the membrane
            //Now check the products list

            //if (bOK)
            //{
            //    foreach (string molguid in cr.products_molecule_guid_ref)
            //    {
            //        ConfigMolecule mol = MainWindow.SC.SimConfig.entity_repository.molecules_dict[molguid];
            //        if (cc.membrane.HasMolecule(mol))
            //        {
            //            bOK = true;
            //        }
            //        else
            //        {
            //            bOK = false;
            //            break;
            //        }
            //    }
            //}

            //Finally, loop through modifiers list
            //if (bOK)
            //{
            //    foreach (string molguid in cr.modifiers_molecule_guid_ref)
            //    {
            //        ConfigMolecule mol = MainWindow.SC.SimConfig.entity_repository.molecules_dict[molguid];
            //        if (cc.membrane.HasMolecule(mol))
            //        {
            //            bOK = true;
            //        }
            //        else
            //        {
            //            bOK = false;
            //            break;
            //        }
            //    }
            //}
            #endregion
           
            e.Accepted = bOK;
        }

        private void cytosolAvailableReactionsListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigReaction cr = e.Item as ConfigReaction;

            if (CellsListBox.SelectedIndex < 0)
            {
                e.Accepted = false;
                return;
            }

            ConfigCell cc = CellsListBox.SelectedItem as ConfigCell;

            ObservableCollection<string> membBound = new ObservableCollection<string>();
            ObservableCollection<string> bulk = new ObservableCollection<string>();
            EntityRepository er = MainWindow.SC.SimConfig.entity_repository;

            foreach (string molguid in cr.reactants_molecule_guid_ref)
                if (er.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                    membBound.Add(molguid);
                else
                    bulk.Add(molguid);

            foreach (string molguid in cr.products_molecule_guid_ref)
                if (er.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                    membBound.Add(molguid);
                else
                    bulk.Add(molguid);

            foreach (string molguid in cr.modifiers_molecule_guid_ref)
                if (er.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                    membBound.Add(molguid);
                else
                    bulk.Add(molguid);

            bool bOK = true;

            if (bulk.Count <= 0)
                bOK = false;

            if (bOK && membBound.Count > 0)
                bOK = cc.membrane.HasMolecules(membBound);

            if (bOK)
                bOK = cc.cytosol.HasMolecules(bulk);

            e.Accepted = bOK;

        }


        private void AddEcmReacCompButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigReactionComplex crc = (ConfigReactionComplex)lbAvailableReacCx.SelectedItem;
            if (crc != null)
            {
                if (!MainWindow.SC.SimConfig.scenario.environment.ecs.reaction_complexes_guid_ref.Contains(crc.reaction_complex_guid))
                {
                    MainWindow.SC.SimConfig.scenario.environment.ecs.reaction_complexes_guid_ref.Add(crc.reaction_complex_guid);
                }
            }
        }

        private void RemoveEcmReacCompButton_Click(object sender, RoutedEventArgs e)
        {
            int nIndex = ReactionComplexListBox.SelectedIndex;
            if (nIndex >= 0)
            {                
                string guid = (string)ReactionComplexListBox.SelectedValue;
                MainWindow.SC.SimConfig.scenario.environment.ecs.reaction_complexes_guid_ref.Remove(guid);
            }
        }

        //LIBRARIES REACTION COMPLEXES HANDLERS

        private void btnCopyReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            if (lbComplexes.SelectedIndex < 0)
            {
                MessageBox.Show("Select a reaction complex to copy from.");
                return;
            }

            ConfigReactionComplex crcCurr = (ConfigReactionComplex)lbComplexes.SelectedItem;
            ConfigReactionComplex crcNew = new ConfigReactionComplex(crcCurr);

            MainWindow.SC.SimConfig.entity_repository.reaction_complexes.Add(crcNew);

            ////AddReacComplex arc = new AddReacComplex(ReactionComplexDialogType.EditComplex);
            ////if (arc.ShowDialog() == true)
            ////{
            ////    ////////lbComplexes.ItemsSource = null;
            ////    ////////lbComplexes.ItemsSource = Sim.RCList;
            ////}    

        }

        private void btnAddReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            AddReacComplex arc = new AddReacComplex(ReactionComplexDialogType.AddComplex);
            if (arc.ShowDialog() == true)
                lbComplexes.SelectedIndex = lbComplexes.Items.Count - 1;
        }

        private void btnEditReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            ConfigReactionComplex crc = (ConfigReactionComplex)lbComplexes.SelectedItem;
            if (crc == null)
                return;

            if (crc.ReadOnly == false)
            {
                AddReacComplex arc = new AddReacComplex(ReactionComplexDialogType.EditComplex, crc);
                arc.ShowDialog();
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Cannot edit a predefined reaction complex.");
            }
        }

        private void btnRemoveReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            ConfigReactionComplex crc = (ConfigReactionComplex)(lbComplexes.SelectedItem);
            if (crc != null)
            {
                if (crc.ReadOnly == false)
                {
                    MessageBoxResult res;
                    res = MessageBox.Show("Are you sure you would like to remove this reaction complex?", "Warning", MessageBoxButton.YesNo);
                    if (res == MessageBoxResult.No)
                        return;

                    int index = lbComplexes.SelectedIndex;
                    MainWindow.SC.SimConfig.entity_repository.reaction_complexes.Remove(crc);

                    lbComplexes.SelectedIndex = index;

                    if (index >= lbComplexes.Items.Count)
                        lbComplexes.SelectedIndex = lbComplexes.Items.Count - 1;

                    if (lbComplexes.Items.Count == 0)
                        lbComplexes.SelectedIndex = -1;
                }
                else
                {
                    MessageBox.Show("Cannot remove a predefined reaction complex.");
                }
            }
        }

        private void btnGraphReactionComplex_Click(object sender, RoutedEventArgs e)
        {

            if (lbComplexes.SelectedIndex < 0)
            {
                MessageBox.Show("Select a reaction complex to process.");
                return;
            }

            ConfigReactionComplex crc = (ConfigReactionComplex)(lbComplexes.SelectedItem);

            //

            ConfigCell cc = new ConfigCell();
            cc.CellName = "RCCell";
            foreach (ConfigMolecularPopulation cmp in crc.molpops)
            {
                cc.cytosol.molpops.Add(cmp);
            }
            foreach (string rguid in crc.reactions_guid_ref)
            {
                cc.cytosol.reactions_guid_ref.Add(rguid);
            }
            MainWindow.SC.SimConfig.entity_repository.cells.Add(cc);
            //MainWindow.SC.SimConfig.entity_repository.cells_dict.Add(cc.cell_guid, cc);
            MainWindow.SC.SimConfig.rc_scenario.cellpopulations.Clear();

            CellPopulation cp = new CellPopulation();
            cp.cell_guid_ref = cc.cell_guid;
            cp.cellpopulation_name = "RC cell";
            cp.number = 1;
            // cp.cell_list.Add(new CellState());
            cp.cellPopDist.CellStates.Add(new CellState());
            cp.cellpopulation_constrained_to_region = false;
            cp.cellpopulation_color = System.Windows.Media.Color.FromScRgb(1.0f, 1.0f, 0.5f, 0.0f);
            MainWindow.SC.SimConfig.rc_scenario.cellpopulations.Add(cp);
            //

            Simulation rcSim = new Simulation();
            rcSim.Load(MainWindow.SC.SimConfig, true, true);
            //rcSim.LoadReactionComplex(MainWindow.SC.SimConfig, grc, true);

            ReactionComplexProcessor rcp = new ReactionComplexProcessor();
            rcp.Initialize(MainWindow.SC.SimConfig, crc, rcSim);
            rcp.Go();

            MainWindow.ST_ReacComplexChartWindow.Title = "Reaction Complex: " + crc.Name;
            MainWindow.ST_ReacComplexChartWindow.RC = rcp;
            MainWindow.ST_ReacComplexChartWindow.Render();
            
            MainWindow.ST_ReacComplexChartWindow.slMaxTime.Maximum = rcp.dMaxTime;
            MainWindow.ST_ReacComplexChartWindow.slMaxTime.Value = rcp.dInitialTime;

            MainWindow.SC.SimConfig.entity_repository.cells.Remove(cc);

            MW.VTKDisplayDocWindow.Activate();
            MainWindow.ST_ReacComplexChartWindow.Activate();
            
        }


        private void cbCellPopDistributionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //CellPopulation cp = (CellPopulation)CellPopsListBox.SelectedItem;
            //CellPopDistributionType distType = (CellPopDistributionType)e.AddedItems[0];

            //if (distType == CellPopDistributionType.Uniform)
            //{
            //    //cp.cellPopDist = new CellPopUniform();
            //}
        }

        private void lbCellPopDistSubType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void CellAddReacCxButton_Click(object sender, RoutedEventArgs e)
        {
            ////if (lbCellAvailableReacCx.SelectedIndex != -1)
            ////{
            ////    ConfigReactionComplex grc = (ConfigReactionComplex)lbCellAvailableReacCx.SelectedValue;
            ////    if (!MainWindow.SC.SimConfig.scenario.ReactionComplexes.Contains(grc))
            ////        MainWindow.SC.SimConfig.scenario.ReactionComplexes.Add(grc);
            ////}
        }

        //LIBRARIES TAB EVENT HANDLERS
        //MOLECULES        
        private void btnAddLibMolecule_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecule gm = new ConfigMolecule();
            gm.Name = gm.GenerateNewName(MainWindow.SC.SimConfig, "_New");
            gm.ReadOnly = false;
            MainWindow.SC.SimConfig.entity_repository.molecules.Add(gm);
            dgLibMolecules.SelectedIndex = dgLibMolecules.Items.Count - 1;

            ConfigMolecule cm = (ConfigMolecule)dgLibMolecules.SelectedItem;
            dgLibMolecules.ScrollIntoView(cm);
        }

        private void btnCopyMolecule_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecule cm = (ConfigMolecule)dgLibMolecules.SelectedItem;

            if (cm == null)
                return;

            //ConfigMolecule gm = new ConfigMolecule(cm);
            ConfigMolecule newmol = cm.Clone(MainWindow.SC.SimConfig);
            MainWindow.SC.SimConfig.entity_repository.molecules.Add(newmol);
            dgLibMolecules.SelectedIndex = dgLibMolecules.Items.Count - 1;

            cm = (ConfigMolecule)dgLibMolecules.SelectedItem;
            dgLibMolecules.ScrollIntoView(cm);
        }

        private void btnRemoveMolecule_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecule gm = (ConfigMolecule)dgLibMolecules.SelectedValue;
            if (gm.ReadOnly == false)
            {
                MessageBoxResult res;
                if (MainWindow.SC.SimConfig.scenario.environment.ecs.HasMolecule(gm))
                {
                    res = MessageBox.Show("If you remove this molecule, corresponding entities that depend on this molecule will also be deleted. Would you like to continue?", "Warning", MessageBoxButton.YesNo);
                }
                else
                {
                    res = MessageBox.Show("Are you sure you would like to remove this molecule?", "Warning", MessageBoxButton.YesNo);
                }

                if (res == MessageBoxResult.No)
                    return;

                int index = dgLibMolecules.SelectedIndex;
                MainWindow.SC.SimConfig.scenario.environment.ecs.RemoveMolecularPopulation(gm.molecule_guid);
                MainWindow.SC.SimConfig.entity_repository.molecules.Remove(gm);
                dgLibMolecules.SelectedIndex = index;

                if (index >= dgLibMolecules.Items.Count)
                    dgLibMolecules.SelectedIndex = dgLibMolecules.Items.Count - 1;

                if (dgLibMolecules.Items.Count == 0)
                    dgLibMolecules.SelectedIndex = -1;

            }
            else
            {
                System.Windows.Forms.MessageBox.Show("Cannot remove a predefined molecule.");
            }
        }

        private void boundaryMoleculesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigMolecule mol = e.Item as ConfigMolecule;
            if (mol != null)
            {
                // Filter out mol if membrane bound 
                if (mol.molecule_location == MoleculeLocation.Boundary)
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }

        //REACTIONS EVENT HANDLERS        
        private void btnRemoveReaction_Click(object sender, RoutedEventArgs e)
        {
            ConfigReaction cr = (ConfigReaction)lvReactions.SelectedItem;
            if (cr == null)
            {
                return;
            }
            if (cr.ReadOnly == true)
            {
                MessageBox.Show("Cannot remove a predefined reaction.");
            }
            else
            {
                MainWindow.SC.SimConfig.entity_repository.reactions.Remove(cr);
            }
        }

        private void btnCopyReaction_Click(object sender, RoutedEventArgs e)
        {
            ConfigReaction cr = (ConfigReaction)lvReactions.SelectedItem;
            if (cr == null)
            {
                return;
            }

            ConfigReaction crNew = new ConfigReaction(cr);
            MainWindow.SC.SimConfig.entity_repository.reactions.Add(crNew);
        }

        //CELLS EVENT HANDLERS
        private void MembraneAddReacButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cc = (ConfigCell)CellsListBox.SelectedItem;

            foreach (var item in lvCellAvailableReacs.SelectedItems)
            {
                ConfigReaction cr = (ConfigReaction)item;
                if (cc != null && cr != null)
                {
                    //Here, must check if the cell has the molecules that are needed by this reaction
                    //Reactants
                    foreach (string newmolguid in cr.reactants_molecule_guid_ref)
                    {
                        bool bFound = false;

                        ConfigMolecule cm = MainWindow.SC.SimConfig.entity_repository.molecules_dict[newmolguid];
                        if (cm != null) {
                            if (cm.molecule_location == MoleculeLocation.Boundary)
                            {
                                if (MembraneHasMolecule(cc, newmolguid)) {
                                    bFound = true;
                                }
                            }
                            else 
                            {
                                if (CytosolHasMolecule(cc, newmolguid)) {
                                    bFound = true;
                                }
                            }

                            if (bFound == false) {
                                string msg = string.Format("Molecule {0} not found in cell.  Please add molecule to cell before adding this reaction.", cm.Name);
                                MessageBox.Show(msg);
                                return;
                            }
                        }                        
                    }
                    //Products
                    foreach (string newmolguid in cr.products_molecule_guid_ref)
                    {
                        bool bFound = false;

                        ConfigMolecule cm = MainWindow.SC.SimConfig.entity_repository.molecules_dict[newmolguid];
                        if (cm != null)
                        {
                            if (cm.molecule_location == MoleculeLocation.Boundary)
                            {
                                if (MembraneHasMolecule(cc, newmolguid))
                                {
                                    bFound = true;
                                }
                            }
                            else
                            {
                                if (CytosolHasMolecule(cc, newmolguid))
                                {
                                    bFound = true;
                                }
                            }

                            if (bFound == false)
                            {
                                string msg = string.Format("Molecule {0} not found in cell.  Please add molecule to cell before adding this reaction.", cm.Name);
                                MessageBox.Show(msg);
                                return;
                            }
                        }
                    }
                    //Modifiers
                    foreach (string newmolguid in cr.modifiers_molecule_guid_ref)
                    {
                        bool bFound = false;

                        ConfigMolecule cm = MainWindow.SC.SimConfig.entity_repository.molecules_dict[newmolguid];
                        if (cm != null)
                        {
                            if (cm.molecule_location == MoleculeLocation.Boundary)
                            {
                                if (MembraneHasMolecule(cc, newmolguid))
                                {
                                    bFound = true;
                                }
                            }
                            else
                            {
                                if (CytosolHasMolecule(cc, newmolguid))
                                {
                                    bFound = true;
                                }
                            }

                            if (bFound == false)
                            {
                                string msg = string.Format("Molecule {0} not found in cell.  Please add molecule to cell before adding this reaction.", cm.Name);
                                MessageBox.Show(msg);
                                return;
                            }
                        }
                    }


                    if (!cc.membrane.reactions_guid_ref.Contains(cr.reaction_guid)) {
                        cc.membrane.reactions_guid_ref.Add(cr.reaction_guid);
                    }
                }
            }
        }

        private bool MembraneHasMolecule(ConfigCell cell, string molguid)
        {
            foreach (ConfigMolecularPopulation molpop in cell.membrane.molpops)
            {
                if (molguid == molpop.molecule_guid_ref)
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
                if (molguid == molpop.molecule_guid_ref)
                {
                    return true;
                }
            }           
            return false;
        }

        private void CytosolAddReacButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cc = (ConfigCell)CellsListBox.SelectedItem;

            foreach (var item in lvCytosolAvailableReacs.SelectedItems)
            {
                ConfigReaction cr = (ConfigReaction)item;
                if (cc != null && cr != null)
                {
                    //Here, must check if the cell has the molecules that are needed by this reaction
                    //Reactants
                    //foreach (string newmolguid in cr.reactants_molecule_guid_ref)
                    //{
                    //    bool bFound = false;

                    //    ConfigMolecule cm = MainWindow.SC.SimConfig.entity_repository.molecules_dict[newmolguid];
                    //    if (cm != null)
                    //    {
                    //        if (cm.molecule_location == MoleculeLocation.Boundary)
                    //        {
                    //            if (MembraneHasMolecule(cc, newmolguid))
                    //            {
                    //                bFound = true;
                    //            }
                    //        }
                    //        else
                    //        {
                    //            if (CytosolHasMolecule(cc, newmolguid))
                    //            {
                    //                bFound = true;
                    //            }
                    //        }

                    //        if (bFound == false)
                    //        {
                    //            string msg = string.Format("Molecule {0} not found in cell.  Please add molecule to cell before adding this reaction.", cm.Name);
                    //            MessageBox.Show(msg);
                    //            return;
                    //        }
                    //    }
                    //}
                    //Products
                    //foreach (string newmolguid in cr.products_molecule_guid_ref)
                    //{
                    //    bool bFound = false;

                    //    ConfigMolecule cm = MainWindow.SC.SimConfig.entity_repository.molecules_dict[newmolguid];
                    //    if (cm != null)
                    //    {
                    //        if (cm.molecule_location == MoleculeLocation.Boundary)
                    //        {
                    //            if (MembraneHasMolecule(cc, newmolguid))
                    //            {
                    //                bFound = true;
                    //            }
                    //        }
                    //        else
                    //        {
                    //            if (CytosolHasMolecule(cc, newmolguid))
                    //            {
                    //                bFound = true;
                    //            }
                    //        }

                    //        if (bFound == false)
                    //        {
                    //            string msg = string.Format("Molecule {0} not found in cell.  Please add molecule to cell before adding this reaction.", cm.Name);
                    //            MessageBox.Show(msg);
                    //            return;
                    //        }
                    //    }
                    //}
                    //Modifiers 
                    //foreach (string newmolguid in cr.modifiers_molecule_guid_ref)
                    //{
                    //    bool bFound = false;

                    //    ConfigMolecule cm = MainWindow.SC.SimConfig.entity_repository.molecules_dict[newmolguid];
                    //    if (cm != null)
                    //    {
                    //        if (cm.molecule_location == MoleculeLocation.Boundary)
                    //        {
                    //            if (MembraneHasMolecule(cc, newmolguid))
                    //            {
                    //                bFound = true;
                    //            }
                    //        }
                    //        else
                    //        {
                    //            if (CytosolHasMolecule(cc, newmolguid))
                    //            {
                    //                bFound = true;
                    //            }
                    //        }

                    //        if (bFound == false)
                    //        {
                    //            string msg = string.Format("Molecule {0} not found in cell.  Please add molecule to cell before adding this reaction.", cm.Name);
                    //            MessageBox.Show(msg);
                    //            return;
                    //        }
                    //    }
                    //}
                    if (!cc.cytosol.reaction_complexes_guid_ref.Contains(cr.reaction_guid))
                    {
                        cc.cytosol.reactions_guid_ref.Add(cr.reaction_guid);
                    }
                }
            }
        }

        private void MembraneAddMolButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecularPopulation gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
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
            ConfigMolecularPopulation gmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
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
            if (lvCellAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCellAvailableReacs.ItemsSource).Refresh();
            if (lvCytosolAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();
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

        private void MembraneRemoveReacButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            int nIndex = MembReacListBox.SelectedIndex;
            if (nIndex >= 0)
            {
                string guid = (string)MembReacListBox.SelectedValue;
                if (cell.membrane.reactions_guid_ref.Contains(guid))
                {
                    cell.membrane.reactions_guid_ref.Remove(guid);
                }
            }
        }

        private void CytosolRemoveReacButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            int nIndex = CytosolReacListBox.SelectedIndex;
            if (nIndex >= 0)
            {
                string guid = (string)CytosolReacListBox.SelectedValue;
                if (cell.cytosol.reactions_guid_ref.Contains(guid))
                {
                    cell.cytosol.reactions_guid_ref.Remove(guid);
                }
            }
        }

        //****************************************************************************************************************



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
                // Filter out cr if not in membrane reaction list 
                if (cc.membrane.reactions_guid_ref.Contains(cr.reaction_guid))
                {
                    e.Accepted = true;
                }

            }
        }

        private void ecmReactionComplexReactionCollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (ReactionComplexListBox.SelectedIndex < 0)
                return;

            ConfigReaction cr = e.Item as ConfigReaction;
            string guidRC = (string)ReactionComplexListBox.SelectedItem;

            e.Accepted = true;

            if (guidRC != null && cr != null)
            {
                ConfigReactionComplex crc = MainWindow.SC.SimConfig.entity_repository.reaction_complexes_dict[guidRC];
                e.Accepted = false;
                // Filter out cr if not in ecm reaction list 
                if (crc.reactions_guid_ref.Contains(cr.reaction_guid))
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
                // Filter out cr if not in cytosol reaction list 
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

        private void cellMolPopsCollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            //if (treeCellPops.SelectedItem != null)
            //    return;

            //ConfigCell cc = e.Item as ConfigCell;
            //string guidCell = cc.cell_guid;

            //e.Accepted = true;

            //if (guidRC != null && cr != null)
            //{
            //    ConfigReactionComplex crc = MainWindow.SC.SimConfig.entity_repository.reaction_complexes_dict[guidRC];
            //    e.Accepted = false;
            //    // Filter out cr if not in ecm reaction list 
            //    if (crc.reactions_guid_ref.Contains(cr.reaction_guid))
            //    {
            //        e.Accepted = true;
            //    }
            //}
        }



        private void AddLibCellButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cc = new ConfigCell();
            cc.ReadOnly = false;
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

        private void cbCellLocationType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            if (cb.SelectedIndex == -1)
                return;
  
            // Only want to respond to purposeful user interaction, not just population and depopulation
            // of cell population distribution type list
            if (e.AddedItems.Count <= 0 || e.RemovedItems.Count == 0)
                return;

            // cellPop points to the current CellPopulation
            CellPopulation cellPop = (CellPopulation)CellPopsListBox.SelectedItem;
            if (cellPop == null)
            {
                return;
            }
            // current_dist points to the current distribution in cellPop
            CellPopDistribution current_dist = cellPop.cellPopDist;
            
            // The new Distribtuion TYPE 
            CellPopDistributionType new_dist_type = (CellPopDistributionType)e.AddedItems[0];

            // Only want to change distribution type if the combo box isn't just selecting 
            // the type of current item in the list box (e.g. when list selection is changed)
            if (current_dist == null)
            {
            }
            else if (current_dist.DistType == new_dist_type)
            {
                return;
            }

            double[] extents = new double[3] { MainWindow.SC.SimConfig.scenario.environment.extent_x, 
                                               MainWindow.SC.SimConfig.scenario.environment.extent_y, 
                                               MainWindow.SC.SimConfig.scenario.environment.extent_z };
            double minDisSquared = 2*MainWindow.SC.SimConfig.entity_repository.cells_dict[cellPop.cell_guid_ref].CellRadius;
            minDisSquared *= minDisSquared;

            MessageBoxResult res;
            CellPopDistributionType cpdt = (CellPopDistributionType)cb.SelectedItem;
            if (cpdt == CellPopDistributionType.Uniform)
            {
                res = MessageBox.Show("The current cell positions will be changed. Continue?", "Warning", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                {
                    cb.SelectedItem = current_dist.DistType;
                    return;
                }
                // Only remove box and Gaussian if the user answered yes to the above.
                if (current_dist.DistType == CellPopDistributionType.Gaussian)
                {
                    DeleteGaussianSpecification(current_dist);
                    CellPopGaussian cpg = current_dist as CellPopGaussian;
                    cpg.gauss_spec_guid_ref = "";
                    MainWindow.GC.Rwc.Invalidate();
                }
                cellPop.cellPopDist = new CellPopUniform(extents, minDisSquared, cellPop);
            }
            else if (cpdt == CellPopDistributionType.Gaussian)
            {
                res = MessageBox.Show("The current cell positions will be changed. Continue?", "Warning", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                {
                    cb.SelectedItem = current_dist.DistType;
                    return;
                }

                BoxSpecification box = new BoxSpecification();
                AddDefaultBoxSpec(box);
                GaussianSpecification gg = new GaussianSpecification();
                AddGaussianSpecification(gg, box);

                cellPop.cellPopDist = new CellPopGaussian(extents, minDisSquared, box, cellPop);             
                ((CellPopGaussian)cellPop.cellPopDist).gauss_spec_guid_ref = gg.gaussian_spec_box_guid_ref;

                // Connect the VTK callback
                MainWindow.GC.Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(MainWindow.GC.WidgetInteractionToGUICallback));
                MainWindow.GC.Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(RegionFocusToGUISection));

            }
            else  if (cpdt == CellPopDistributionType.Specific)
            {
                res = MessageBox.Show("Keep current cell locations?", "", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                {
                    cellPop.cellPopDist = new CellPopSpecific(extents, minDisSquared, cellPop);
                }
                else
                {
                    CellPopulation tempCellPop = new CellPopulation();
                    tempCellPop.cellPopDist = cellPop.cellPopDist;
                    cellPop.cellPopDist = new CellPopSpecific(extents, minDisSquared, cellPop);
                    cellPop.cellPopDist.CellStates = tempCellPop.cellPopDist.CellStates;
                }
                // Remove box and Gaussian if applicable.
                if (current_dist.DistType == CellPopDistributionType.Gaussian)
                {
                    DeleteGaussianSpecification(current_dist);
                    CellPopGaussian cpg = current_dist as CellPopGaussian;
                    cpg.gauss_spec_guid_ref = "";
                    MainWindow.GC.Rwc.Invalidate();
                }
            }
            //ListBoxItem lbi = ((sender as ListBox).SelectedItem as ListBoxItem);
            ToolBarTray tr = new ToolBarTray();

        }
#if CELL_REGIONS
        public void SelectRegionInGUI(int index, string guid)
        {
            // Regions are in the second (entities) tab panel
            ConfigTabControl.SelectedIndex = 1;
            // Because each region will have a unique box guid, can use data-binding-y way of setting selection
            //RegionsListBox.SelectedIndex = index;
            //RegionsListBox.SelectedValuePath = "region_box_spec_guid_ref";
            //RegionsListBox.SelectedValue = guid;
        }
#endif
        public void SelectGaussSpecInGUI(int index, string guid)
        {
            bool isBoxInCell = false;

            foreach (CellPopulation cp in MainWindow.SC.SimConfig.scenario.cellpopulations)
            {
                CellPopDistribution cpd = cp.cellPopDist;
                if (cpd.DistType == CellPopDistributionType.Gaussian)
                {
                    CellPopGaussian cpg = cpd as CellPopGaussian;
                    if (cpg.gauss_spec_guid_ref == guid)
                    {
                        //BoxSpecification box = MainWindow.SC.SimConfig.box_guid_box_dict[guid];
                        //cpg.Reset();
                        isBoxInCell = true;
                        break;
                    }
                }
            }

            if (isBoxInCell == true)
            {
                ConfigTabControl.SelectedItem = tabCellPop;
            }
            else
            {
                ConfigTabControl.SelectedItem = tabECM;
            }
        }

        public void RegionFocusToGUISection(RegionWidget rw, bool transferMatrix)
        {
            // identify the widget's key
            string key = "";

            if (rw != null && MainWindow.GC.Regions.ContainsValue(rw) == true)
            {
                foreach (KeyValuePair<string, RegionWidget> kvp in MainWindow.GC.Regions)
                {
                    if (kvp.Value == rw)
                    {
                        key = kvp.Key;
                        break;
                    }
                }

                // found?
                if (key != "")
                {
                    // Select the correct region/solfac/gauss_spec in the GUI's lists
                    bool gui_spot_found = false;
#if CELL_REGIONS
                    for (int r = 0; r < MainWindow.SC.SimConfig.scenario.regions.Count; r++)
                    {
                        // See whether the current widget is for a Region
                        if (MainWindow.SC.SimConfig.scenario.regions[r].region_box_spec_guid_ref == key)
                        {
                            SelectRegionInGUI(r, key);
                            gui_spot_found = true;
                            break;
                        }
                    }
#endif
                    if (!gui_spot_found)
                    {
                        // Next check whether any Solfacs use this right gaussian_spec for this box
                        for (int r = 0; r < MainWindow.SC.SimConfig.scenario.environment.ecs.molpops.Count; r++)
                        {
                            // We'll just be picking the first one that uses 
                            if (MainWindow.SC.SimConfig.scenario.environment.ecs.molpops[r].mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian &&
                                ((MolPopGaussian)MainWindow.SC.SimConfig.scenario.environment.ecs.molpops[r].mpInfo.mp_distribution).gaussgrad_gauss_spec_guid_ref == key)
                            {
                                SelectSolfacInGUI(r);
                                //gui_spot_found = true;
                                break;
                            }
                        }
                    }
                    if (!gui_spot_found)
                    {
                        // Last check the gaussian_specs for this box guid
                        for (int r = 0; r < MainWindow.SC.SimConfig.entity_repository.gaussian_specifications.Count; r++)
                        {
                            // We'll just be picking the first one that uses 
                            if (MainWindow.SC.SimConfig.entity_repository.gaussian_specifications[r].gaussian_spec_box_guid_ref == key)
                            {
                                SelectGaussSpecInGUI(r, key);
                                //gui_spot_found = true;
                                break;
                            }
                        }
                    }
                }
            }
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

        /// <summary>
        /// Called when the number of cells in the Cell Population changes.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

            cp.number = numNew;
            if (numNew > numOld && numNew > cp.cellPopDist.CellStates.Count)
            {
                int rows_to_add = numNew - numOld;
                cp.cellPopDist.AddByDistr(rows_to_add);
            }
            else if (numNew < numOld)
            {
                if (numOld > cp.cellPopDist.CellStates.Count)
                {
                    numOld = cp.cellPopDist.CellStates.Count;
                }

                int rows_to_delete = numOld - numNew;
                cp.cellPopDist.RemoveCells(rows_to_delete);
            }
            cp.number = cp.cellPopDist.CellStates.Count;
        }

        private void cellPopsListBoxSelChanged(object sender, SelectionChangedEventArgs e)
        {
            //newCellPopSelected = true;
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

                cp.cellPopDist.CellStates.Clear();
                for (int i = 0; i < paste.Length; i += 3)
                {
                    cp.cellPopDist.CellStates.Add(new CellState(double.Parse(paste[i]), double.Parse(paste[i + 1]), double.Parse(paste[i + 2])));
                }

                cp.number = cp.cellPopDist.CellStates.Count;

            }
        }

        private void menuCoordinatesPaste_Click(object sender, RoutedEventArgs e)
        {
            //CellPopulation cp = (CellPopulation)CellPopsListBox.SelectedItem;
            //if (cp == null)
            //    return;

            //string s = (string)Clipboard.GetData(DataFormats.Text);

            //char[] delim = { '\t', '\r', '\n' };
            //string[] paste = s.Split(delim, StringSplitOptions.RemoveEmptyEntries);

            //cp.cell_locations.Clear();
            //for (int i = 0; i < paste.Length-2; i += 3)
            //{
            //    CellLocation cl = new CellLocation(double.Parse(paste[i]), double.Parse(paste[i + 1]), double.Parse(paste[i + 2]));
            //    cp.cell_locations.Add(cl);
            //}
            //cp.number = cp.cell_locations.Count;
            //e.Handled = true;
        }

        private void menuCoordinatesTester_Click(object sender, RoutedEventArgs e)
        {
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

        /// <summary>
        ///  Called when user selects "Specific" for cell population locations.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgLocations_Unloaded(Object sender, RoutedEventArgs e)
        {
            CellPopulation cp = (CellPopulation)CellPopsListBox.SelectedItem;
            if (cp == null)
                return;
            //cp.number = cp.cell_list.Count;
            cp.number = cp.cellPopDist.CellStates.Count;

            if (cp.cellPopDist.DistType == CellPopDistributionType.Specific)
            {
                CellPopSpecific cps = cp.cellPopDist as CellPopSpecific;
                //cps.CopyLocations(cp);
            }
        }        

        private void blob_actor_checkbox_clicked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = e.OriginalSource as CheckBox;
            string guid = cb.CommandParameter as string;
            if (guid.Length > 0)
            {
                if (MainWindow.SC.SimConfig.entity_repository.gauss_guid_gauss_dict.ContainsKey(guid))
                {
                    GaussianSpecification gs = MainWindow.SC.SimConfig.entity_repository.gauss_guid_gauss_dict[guid];
                    gs.gaussian_region_visibility = !gs.gaussian_region_visibility;
                }
            }

            //ConfigMolecularPopulation cmp = (ConfigMolecularPopulation)lbEcsMolPops.SelectedItem;
            //MolPopGaussian mpg = cmp.mpInfo.mp_distribution as MolPopGaussian;
            //if (mpg != null)
            //{
            //    string guid = mpg.gaussgrad_gauss_spec_guid_ref;
            //    if (guid.Length > 0)
            //    {
            //        if (MainWindow.SC.SimConfig.entity_repository.gauss_guid_gauss_dict.ContainsKey(guid))
            //        {
            //            GaussianSpecification gs = MainWindow.SC.SimConfig.entity_repository.gauss_guid_gauss_dict[guid];
            //            gs.gaussian_region_visibility = !gs.gaussian_region_visibility;
            //        }
            //    }
            //}
            //else
            //{
                
            //}
        }

        private void btnTesterClicked(object sender, RoutedEventArgs e)
        {
            int x = 1;
            x++;
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

        private void cbBoundFace_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;

            if (cb.SelectedIndex == 0)
            {
                MainWindow.GC.OrientationMarker_IsChecked = false;
                //if (MolPopDistComboBox != null)
                //{
                //    if (MolPopDistComboBox.SelectedIndex == 1)
                //    {
                //        MolPopDistComboBox.SelectedIndex = 0;
                //    }
                //}
            }
            else
            {
                MainWindow.GC.OrientationMarker_IsChecked = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int x = 1;
            x++;
        }

        private void MolPopDistributionTypeComboBox_GotFocus(object sender, RoutedEventArgs e)
        {
            ConfigMolecularPopulation cmp = (ConfigMolecularPopulation)(lbEcsMolPops.SelectedItem);
            if (cmp == null)
                return;

            ComboBox cb = sender as ComboBox;
            int index = (int)MolPopDistributionType.Linear;
            ComboBoxItem cbi = (ComboBoxItem)(cb.ItemContainerGenerator.ContainerFromIndex(index));

            cbi.IsEnabled = true;
            if (comboToroidal.SelectedIndex > 0)
            //if (cbToroidal.IsChecked == true)
            {
                cbi.IsEnabled = false;
            }
            else if (cmp == null)
            {
                cbi.IsEnabled = false;
            }
            else if (cmp.mpInfo.mp_distribution.GetType() == typeof(MolPopLinear))
            {
                MolPopLinear mpl = cmp.mpInfo.mp_distribution as MolPopLinear;
                if (mpl.boundary_face == BoundaryFace.None)
                {
                    cbi.IsEnabled = false;
                }
            }

            index = (int)MolPopDistributionType.Explicit;
            cbi = (ComboBoxItem)(cb.ItemContainerGenerator.ContainerFromIndex(index));
            cbi.IsEnabled = false;
        }

        private void RemoveCellButton_Click(object sender, RoutedEventArgs e)
        {
            int nIndex = CellsListBox.SelectedIndex;
            
            if (nIndex >= 0)
            {
                ConfigCell cell = (ConfigCell)CellsListBox.SelectedValue;

                if (cell.ReadOnly == false)
                {
                    MessageBoxResult res;
                    if (MainWindow.SC.SimConfig.scenario.HasCell(cell))
                    {
                        res = MessageBox.Show("If you delete this cell, corresponding cell populations will also be deleted. Would you like to continue?", "Warning", MessageBoxButton.YesNo);
                    }
                    else
                    {
                        res = MessageBox.Show("Are you sure you would like to remove this cell?", "Warning", MessageBoxButton.YesNo);
                    }

                    if (res == MessageBoxResult.Yes)
                    {
                        //MainWindow.SC.SimConfig.scenario.RemoveCellPopulation(cell);
                        MainWindow.SC.SimConfig.entity_repository.cells.Remove(cell);

                        CellsListBox.SelectedIndex = nIndex;

                        if (nIndex >= CellsListBox.Items.Count)
                            CellsListBox.SelectedIndex = CellsListBox.Items.Count - 1;

                        if (CellsListBox.Items.Count == 0)
                            CellsListBox.SelectedIndex = -1;
                    }
                }
                else
                {
                    System.Windows.Forms.MessageBox.Show("Cannot remove a predefined cell.");
                }

            }            
        }

        private void CopyCellButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            if (cell == null)
            {
                return;
            }

            ConfigCell cellNew = cell.Clone();
            
            //Generate a new cell name
            cellNew.CellName = GenerateNewCellName(cell, "_Copy");

            MainWindow.SC.SimConfig.entity_repository.cells.Add(cellNew);
            CellsListBox.SelectedIndex = CellsListBox.Items.Count - 1;
            CellsListBox.ScrollIntoView(CellsListBox.SelectedItem);
        }

        private string GenerateNewCellName(ConfigCell cell)
        {
            int nSuffix = 1;
            string sSuffix = string.Format("_Copy{0:000}", nSuffix);
            string TempCellName = cell.CellName;
            while (FindCellBySuffix(sSuffix) == true)
            {
                TempCellName = cell.CellName.Replace(sSuffix, "");
                nSuffix++;
                sSuffix = string.Format("_Copy{0:000}", nSuffix);
            }
            TempCellName += sSuffix;
            return TempCellName;
        }

        private string GenerateNewCellName(ConfigCell cell, string ending)
        {
            int nSuffix = 1;
            string sSuffix = ending + string.Format("{0:000}", nSuffix);
            string TempCellName = cell.CellName;
            while (FindCellBySuffix(sSuffix) == true)
            {
                TempCellName = cell.CellName.Replace(sSuffix, "");
                nSuffix++;
                sSuffix = ending + string.Format("{0:000}", nSuffix);
            }
            TempCellName += sSuffix;
            return TempCellName;
        }

        // given a cell type name, check if it exists in repos
        private static bool FindCellBySuffix(string suffix)
        {
            foreach (ConfigCell cc in MainWindow.SC.SimConfig.entity_repository.cells)
            {
                if (cc.CellName.EndsWith(suffix))
                {
                    return true;
                }
            }
            return false;
        }

        private void cbCellColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox ColorComboBox = sender as ComboBox;
            int n = ColorComboBox.SelectedIndex;
        }

        private void AddReacExpander_Expanded(object sender, RoutedEventArgs e)
        {
            //lvAvailableReacs.InvalidateVisual();
            //lvAvailableReacs.ClearValue(ListView.ItemsSourceProperty);
            //lvAvailableReacs.DataContext = null;
            //lvAvailableReacs.DataContext = MainWindow.SC.SimConfig.entity_repository.molecules;
            //EcmAvailableReactions.Refresh();
        }

        private void molecule_combo_box2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();
        }

        private void TabItem_Loaded(object sender, RoutedEventArgs e)
        {
            lbRptCellPops.SelectedIndex = 0;
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ConfigMolecule cm = dgLibMolecules.SelectedItem as ConfigMolecule;

            if (cm == null)
                return;

            cm.ValidateName(MainWindow.SC.SimConfig);
        }

        private void comboToroidal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = e.Source as ComboBox;
            if (cb.SelectedIndex == (int)(BoundaryType.Toroidal))
            {
                MessageBoxResult res;
                res = MessageBox.Show("If you change the boundary condition to toroidal, all molecular populations using Linear initial distribution will be changed to Homogeneous.  Are you sure you would like to proceed?", "Warning", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                {
                    cb.SelectedIndex = (int)(BoundaryType.Zero_Flux);
                    return;
                }

                foreach (ConfigMolecularPopulation cmp in MainWindow.SC.SimConfig.scenario.environment.ecs.molpops)
                {
                    if (cmp.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Linear)
                    {
                        MolPopHomogeneousLevel shl = new MolPopHomogeneousLevel();
                        cmp.mpInfo.mp_distribution = shl;
                    }
                }
            }
        }
    }      
    
     

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
}


