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
using System.ComponentModel;
using System.Reflection;
using System.Globalization;
using DaphneGui.Pushing;
using System.Diagnostics;

namespace DaphneGui
{

    /// <summary>
    /// Interaction logic for ProtocolToolWindow.xaml
    /// </summary>
    public partial class ProtocolToolWindow : ToolWindow
    {
        //private static bool newCellPopSelected = true;
        public ProtocolToolWindow()
        {
            InitializeComponent();

            CollectionViewSource cvs = (CollectionViewSource)(FindResource("EcsBulkMoleculesListView"));
            cvs.Filter += FilterFactory.bulkMoleculesListView_Filter;
        }

        public MainWindow MW { get; set; }

        private void AddCellPopButton_Click(object sender, RoutedEventArgs e)
        {
            CellPopsDetailsExpander.IsExpanded = true;

            // Some relevant CellPopulation constructor defaults: 
            //      number = 1
            //      no instantiation of cellPopDist
            CellPopulation cp = new CellPopulation();

            EntityRepository er = MainWindow.SOP.Protocol.entity_repository;

            // Default cell type and name to first entry in the cell repository
            if (MainWindow.SOP.Protocol.entity_repository.cells.Count > 0)
            {
                //guid to object changes
                ConfigCell cell_to_clone = er.cells.First();

                cp.Cell = cell_to_clone.Clone(false);
                cp.cellpopulation_name = cp.Cell.CellName;
            }
            else
            {
                MessageBox.Show("Please create a cell type first.");
                return;
            }

            double[] extents = new double[3] { MainWindow.SOP.Protocol.scenario.environment.extent_x, 
                                               MainWindow.SOP.Protocol.scenario.environment.extent_y, 
                                               MainWindow.SOP.Protocol.scenario.environment.extent_z };
            //guid to object changes
            //double minDisSquared = 2 * MainWindow.SOP.Protocol.entity_repository.cells_dict[cp.Cell.entity_guid].CellRadius;
            double minDisSquared = 2 * cp.Cell.CellRadius;
            minDisSquared *= minDisSquared;

            // Default is uniform probability distribution
            cp.cellPopDist = new CellPopUniform(extents, minDisSquared, cp);

            cp.cellpopulation_color = System.Windows.Media.Color.FromScRgb(1.0f, 1.0f, 0.5f, 0.0f);
            MainWindow.SOP.Protocol.scenario.cellpopulations.Add(cp);
            CellPopsListBox.SelectedIndex = CellPopsListBox.Items.Count - 1;
        }

        //This method is called when the user clicks the Remove Cell button
        private void RemoveCellPopButton_Click(object sender, RoutedEventArgs e)
        {
            int index = CellPopsListBox.SelectedIndex;
            CellPopulation current_item = (CellPopulation)CellPopsListBox.SelectedItem;

            MessageBoxResult res;
            res = MessageBox.Show("Are you sure you would like to remove this cell population?", "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;

            //Remove gaussian spec if any
            if (current_item.cellPopDist.DistType == CellPopDistributionType.Gaussian)
            {
                DeleteGaussianSpecification(current_item.cellPopDist);
                CellPopGaussian cpg = current_item.cellPopDist as CellPopGaussian;
                cpg.gauss_spec_guid_ref = "";
            }
            MainWindow.GC.Rwc.Invalidate();

            //Remove the cell population
            MainWindow.SOP.Protocol.scenario.cellpopulations.Remove(current_item);

            CellPopsListBox.SelectedIndex = index;

            if (index >= CellPopsListBox.Items.Count)
                CellPopsListBox.SelectedIndex = CellPopsListBox.Items.Count - 1;

            if (CellPopsListBox.Items.Count == 0)
                CellPopsListBox.SelectedIndex = -1;
        }

        // Utility function used in AddGaussSpecButton_Click() and SolfacTypeComboBox_SelectionChanged()
        private void AddGaussianSpecification(MolPopGaussian mpg, ConfigMolecularPopulation molpop)
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
            MainWindow.SOP.Protocol.scenario.box_specifications.Add(box);

            GaussianSpecification gg = new GaussianSpecification();
            gg.gaussian_spec_box_guid_ref = box.box_guid;
            gg.gaussian_spec_name = "New on-center gradient";
            gg.gaussian_spec_color = molpop.mp_color;    //System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            // Add gauss spec property changed to VTK callback (ellipsoid actor color & visibility)
            gg.PropertyChanged += MainWindow.GUIGaussianSurfaceVisibilityToggle;
            MainWindow.SOP.Protocol.scenario.gaussian_specifications.Add(gg);
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

            if (guid == "")
                return;

            if (MainWindow.SOP.Protocol.scenario.gauss_guid_gauss_dict.ContainsKey(guid))
            {
                GaussianSpecification gs = MainWindow.SOP.Protocol.scenario.gauss_guid_gauss_dict[guid];
                MainWindow.SOP.Protocol.scenario.gaussian_specifications.Remove(gs);
                MainWindow.SOP.Protocol.scenario.gauss_guid_gauss_dict.Remove(guid);
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
            box.x_trans = MainWindow.SOP.Protocol.scenario.environment.extent_x / 2;
            box.y_trans = MainWindow.SOP.Protocol.scenario.environment.extent_y / 2;
            box.z_trans = MainWindow.SOP.Protocol.scenario.environment.extent_z / 2; ;
            box.x_scale = MainWindow.SOP.Protocol.scenario.environment.extent_x / 4; ;
            box.y_scale = MainWindow.SOP.Protocol.scenario.environment.extent_x / 4; ;
            box.z_scale = MainWindow.SOP.Protocol.scenario.environment.extent_x / 4; ;
            // Add box GUI property changed to VTK callback
            box.PropertyChanged += MainWindow.GUIInteractionToWidgetCallback;
            MainWindow.SOP.Protocol.scenario.box_specifications.Add(box);
        }

        // Used to specify Gaussian distibution for cell positions
        private void AddGaussianSpecification(GaussianSpecification gg, BoxSpecification box)
        {
            gg.gaussian_spec_box_guid_ref = box.box_guid;
            gg.gaussian_spec_name = "";
            //gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            // Add gauss spec property changed to VTK callback (ellipsoid actor color & visibility)
            gg.PropertyChanged += MainWindow.GUIGaussianSurfaceVisibilityToggle;
            MainWindow.SOP.Protocol.scenario.gaussian_specifications.Add(gg);

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
            if (dist.DistType != CellPopDistributionType.Gaussian)
                return;

            CellPopGaussian cpg = dist as CellPopGaussian;
            string guid = cpg.gauss_spec_guid_ref;

            if (guid == "")
                return;

            if (MainWindow.SOP.Protocol.scenario.gauss_guid_gauss_dict.ContainsKey(guid))
            {
                GaussianSpecification gs = MainWindow.SOP.Protocol.scenario.gauss_guid_gauss_dict[guid];
                MainWindow.SOP.Protocol.scenario.gaussian_specifications.Remove(gs);
                MainWindow.SOP.Protocol.scenario.gauss_guid_gauss_dict.Remove(guid);
                MainWindow.GC.RemoveRegionWidget(guid);

                //// Remove box
                //BoxSpecification box = MainWindow.SOP.Protocol.box_guid_box_dict[gs.gaussian_spec_box_guid_ref];
                //MainWindow.SOP.Protocol.entity_repository.box_specifications.Remove(box);
                //MainWindow.SOP.Protocol.box_guid_box_dict.Remove(gs.gaussian_spec_box_guid_ref);
                //// box.PropertyChanged += MainWindow.GUIInteractionToWidgetCallback;
                //MainWindow.SOP.Protocol.entity_repository.box_specifications.Remove(box);
            }

        }


        private void EcsMolPopsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

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
                MolPopDistributionType new_dist_type = MolPopDistributionType.Homogeneous; // = MolPopDistributionType.Gaussian;

                if (e.AddedItems.Count > 0)
                {
                    new_dist_type = (MolPopDistributionType)e.AddedItems[0];
                }


                // Only want to change distribution type if the combo box isn't just selecting 
                // the type of current item in the solfacs list box (e.g. when list selection is changed)

                if (current_mol.mp_distribution == null)
                {
                }
                else if (current_mol.mp_distribution.mp_distribution_type == new_dist_type)
                {
                    return;
                }

                if (current_mol.mp_distribution != null)
                {
                    if (new_dist_type != MolPopDistributionType.Gaussian && current_mol.mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
                    {
                        DeleteGaussianSpecification(current_mol.mp_distribution);
                        MolPopGaussian mpg = current_mol.mp_distribution as MolPopGaussian;
                        mpg.gaussgrad_gauss_spec_guid_ref = "";
                        MainWindow.GC.Rwc.Invalidate();
                    }
                }
                switch (new_dist_type)
                {
                    case MolPopDistributionType.Homogeneous:
                        MolPopHomogeneousLevel shl = new MolPopHomogeneousLevel();
                        current_mol.mp_distribution = shl;
                        break;
                    case MolPopDistributionType.Linear:
                        MolPopLinear molpoplin = new MolPopLinear();
                        // X face is default
                        molpoplin.boundaryCondition.Add(new BoundaryCondition(MolBoundaryType.Dirichlet, Boundary.left, 0.0));
                        molpoplin.boundaryCondition.Add(new BoundaryCondition(MolBoundaryType.Dirichlet, Boundary.right, 0.0));
                        molpoplin.Initalize(BoundaryFace.X);
                        molpoplin.boundary_face = BoundaryFace.X;
                        current_mol.mp_dist_name = "Linear";
                        current_mol.mp_distribution = molpoplin;
                        current_mol.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                        current_mol.mp_render_blending_weight = 2.0;
                        break;

                    case MolPopDistributionType.Gaussian:
                        MolPopGaussian mpg = new MolPopGaussian();

                        AddGaussianSpecification(mpg, current_mol);
                        current_mol.mp_distribution = mpg;

                        break;

                    case MolPopDistributionType.Explicit:
                        break;

                    default:
                        throw new ArgumentException("MolPop distribution type out of range");
                }
            }
        }

        //        private void MembraneMolPopDistributionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //        {
        //            // Only want to respond to purposeful user interaction, not just population and depopulation
        //            // of solfacs list
        //            ////////if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
        //            ////////    return;


        //            ConfigMolecularPopulation current_mol = (ConfigMolecularPopulation)CellMembraneMolPopsListBox.SelectedItem;

        //            if (current_mol != null)
        //            {
        //                MolPopDistributionType new_dist_type = MolPopDistributionType.Gaussian;
        //                if (e.AddedItems.Count > 0)
        //                    new_dist_type = (MolPopDistributionType)e.AddedItems[0];


        //                // Only want to change distribution type if the combo box isn't just selecting 
        //                // the type of current item in the solfacs list box (e.g. when list selection is changed)

        //                if (current_mol.mp_distribution == null)
        //                {
        //                }
        //                else if (current_mol.mp_distribution.mp_distribution_type == new_dist_type)
        //                {
        //                    return;
        //                }
        //                switch (new_dist_type)
        //                {
        //                    case MolPopDistributionType.Homogeneous:
        //                        MolPopHomogeneousLevel shl = new MolPopHomogeneousLevel();
        //                        current_mol.mp_distribution = shl;
        //                        break;
        //                    case MolPopDistributionType.Linear:
        //                        MolPopLinear slg = new MolPopLinear();
        //                        current_mol.mp_distribution = slg;
        //                        break;
        //                    case MolPopDistributionType.Gaussian:
        //                        // Make sure there is at least one gauss_spec in repository
        //                        ////if (MainWindow.SOP.Protocol.entity_repository.gaussian_specifications.Count == 0)
        //                        ////{
        //                        ////    this.AddGaussianSpecification();
        //                        ////}
        //                        MolPopGaussian sgg = new MolPopGaussian();
        //                        GaussianSpecification gg = new GaussianSpecification();
        //                        BoxSpecification box = new BoxSpecification();
        //                        box.x_scale = 200;
        //                        box.y_scale = 200;
        //                        box.z_scale = 200;
        //                        box.x_trans = 500;
        //                        box.y_trans = 500;
        //                        box.z_trans = 500;
        //                        MainWindow.SOP.Protocol.scenario.box_specifications.Add(box);
        //                        gg.gaussian_spec_box_guid_ref = box.box_guid;
        //                        gg.gaussian_spec_name = "Off-center gaussian";
        //                        gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
        //                        MainWindow.SOP.Protocol.scenario.gaussian_specifications.Add(gg);
        //                        sgg.gaussgrad_gauss_spec_guid_ref = gg.gaussian_spec_box_guid_ref;
        //                        current_mol.mp_distribution = sgg;
        //                        break;
        //                    default:
        //                        throw new ArgumentException("MolPop distribution type out of range");
        //                }
        //            }
        //        }

        //        private void CytosolMolPopDistributionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //        {
        //            // Only want to respond to purposeful user interaction, not just population and depopulation
        //            // of solfacs list
        //            ////////if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
        //            ////////    return;


        //            ConfigMolecularPopulation current_mol = (ConfigMolecularPopulation)CellCytosolMolPopsListBox.SelectedItem;

        //            if (current_mol != null)
        //            {

        //                MolPopDistributionType new_dist_type = MolPopDistributionType.Gaussian;
        //                if (e.AddedItems.Count > 0)
        //                    new_dist_type = (MolPopDistributionType)e.AddedItems[0];


        //                // Only want to change distribution type if the combo box isn't just selecting 
        //                // the type of current item in the solfacs list box (e.g. when list selection is changed)

        //                if (current_mol.mp_distribution == null)
        //                {
        //                }
        //                else if (current_mol.mp_distribution.mp_distribution_type == new_dist_type)
        //                {
        //                    return;
        //                }
        //                //else
        //                //{
        //                switch (new_dist_type)
        //                {
        //                    case MolPopDistributionType.Homogeneous:
        //                        MolPopHomogeneousLevel shl = new MolPopHomogeneousLevel();
        //                        current_mol.mp_distribution = shl;
        //                        break;
        //                    case MolPopDistributionType.Linear:
        //                        MolPopLinear slg = new MolPopLinear();
        //                        current_mol.mp_distribution = slg;
        //                        break;
        //                    case MolPopDistributionType.Gaussian:
        //                        // Make sure there is at least one gauss_spec in repository
        //                        ////if (MainWindow.SOP.Protocol.entity_repository.gaussian_specifications.Count == 0)
        //                        ////{
        //                        ////    this.AddGaussianSpecification();
        //                        ////}
        //                        MolPopGaussian sgg = new MolPopGaussian();
        //                        sgg.gaussgrad_gauss_spec_guid_ref = MainWindow.SOP.Protocol.scenario.gaussian_specifications[0].gaussian_spec_box_guid_ref;
        //                        current_mol.mp_distribution = sgg;
        //                        break;

        //#if allow_dist_from_file
        //                    //case MolPopDistributionType.Custom:

        //                    //    var prev_distribution = current_mol.mp_distribution;
        //                    //    MolPopCustom scg = new MolPopCustom();
        //                    //    current_mol.mp_distribution = scg;

        //                    //    // Configure open file dialog box
        //                    //    Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
        //                    //    dlg.InitialDirectory = MainWindow.appPath;
        //                    //    dlg.DefaultExt = ".txt"; // Default file extension
        //                    //    dlg.Filter = "Custom chemokine field files (.txt)|*.txt"; // Filter files by extension

        //                    //    // Show open file dialog box
        //                    //    Nullable<bool> result = dlg.ShowDialog();

        //                    //    // Process open file dialog box results
        //                    //    if (result == true)
        //                    //    {
        //                    //        // Save filename here, but deserialization will happen in lockAndResetSim->initialState call
        //                    //        string filename = dlg.FileName;
        //                    //        scg.custom_gradient_file_string = filename;
        //                    //    }
        //                    //    else
        //                    //    {
        //                    //        current_mol.mp_distribution = prev_distribution;
        //                    //    }
        //                    //    break;  
        //#endif

        //                    default:
        //                        throw new ArgumentException("MolPop distribution type out of range");
        //                }
        //                //}
        //            }
        //        }


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

            if (MainWindow.SOP.Protocol.entity_repository.molecules.Count == 0)
            {
                MessageBox.Show("There are no molecules to choose from. Please start with a blank scenario or other scenario that contains molecules.");
                return;
            }

            //add a moleculepop that is not already added
            ConfigMolecularPopulation gmp = new ConfigMolecularPopulation(ReportType.ECM_MP);
            CollectionViewSource cvs = (CollectionViewSource)(FindResource("EcsBulkMoleculesListView"));
            if (cvs == null) return;
            foreach (ConfigMolecule item in cvs.View)
            {
                if (MainWindow.SOP.Protocol.scenario.environment.ecs.molpops.Where(m => m.molecule.Name == item.Name).Any()) continue;
                gmp.molecule = item.Clone(null);
                gmp.Name = item.Name;
                break;
            }
            if (gmp.molecule == null) return;
            gmp.mp_dist_name = "New distribution";
            gmp.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 1.0f, 0.2f);
            MainWindow.SOP.Protocol.scenario.environment.ecs.molpops.Add(gmp);
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

                foreach (ConfigReaction cr in MainWindow.SOP.Protocol.scenario.environment.ecs.Reactions.ToList())
                {
                    if (MainWindow.SOP.Protocol.entity_repository.reactions_dict[cr.entity_guid].HasMolecule(cmp.molecule.entity_guid))
                    {
                        MainWindow.SOP.Protocol.scenario.environment.ecs.Reactions.Remove(cr);
                    }
                }

                //Delete the gaussian box if any
                if (cmp.mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
                {
                    DeleteGaussianSpecification(cmp.mp_distribution);
                    MolPopGaussian mpg = cmp.mp_distribution as MolPopGaussian;
                    mpg.gaussgrad_gauss_spec_guid_ref = "";
                    MainWindow.GC.Rwc.Invalidate();
                }

                //Delete the molecular population
                MainWindow.SOP.Protocol.scenario.environment.ecs.molpops.Remove(cmp);

                CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();

            }

            lbEcsMolPops.SelectedIndex = index;

            if (index >= lbEcsMolPops.Items.Count)
                lbEcsMolPops.SelectedIndex = lbEcsMolPops.Items.Count - 1;

            if (lbEcsMolPops.Items.Count == 0)
                lbEcsMolPops.SelectedIndex = -1;
        }

        private bool EcmHasMolecule(string molguid)
        {
            foreach (ConfigMolecularPopulation molpop in MainWindow.SOP.Protocol.scenario.environment.ecs.molpops)
            {
                if (molpop.molecule.entity_guid == molguid)
                    return true;
            }
            return false;
        }
        private bool CellPopsHaveMoleculeInMemb(string molguid)
        {
            bool ret = false;
            foreach (CellPopulation cell_pop in MainWindow.SOP.Protocol.scenario.cellpopulations)
            {
                ConfigCell cell = cell_pop.Cell;
                if (MembraneHasMolecule(cell, molguid))
                    return true;
            }

            return ret;
        }
        private bool CellPopsHaveMoleculeInCytosol(string molguid)
        {
            bool ret = false;
            foreach (CellPopulation cell_pop in MainWindow.SOP.Protocol.scenario.cellpopulations)
            {
                ConfigCell cell = MainWindow.SOP.Protocol.entity_repository.cells_dict[cell_pop.Cell.entity_guid];
                if (CytosolHasMolecule(cell, molguid))
                    return true;
            }

            return ret;
        }

        private void AddEcmReacButton_Click(object sender, RoutedEventArgs e)
        {
            bool needRefresh = false;

            foreach (var item in lvAvailableReacs.SelectedItems)
            {
                ConfigReaction reac = (ConfigReaction)item;

                if (MainWindow.SOP.Protocol.scenario.environment.ecs.reactions_dict.ContainsKey(reac.entity_guid) == false)
                {
                    MainWindow.SOP.Protocol.scenario.environment.ecs.Reactions.Add(reac.Clone(true));
                    needRefresh = true;
                }
            }

            //Refresh the filter
            if (needRefresh && lvAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();

        }

        private void RemoveEcmReacButton_Click(object sender, RoutedEventArgs e)
        {
            if (lvEcsReactions.SelectedIndex < 0)
                return;

            ConfigReaction reac = (ConfigReaction)lvEcsReactions.SelectedValue;
            if (MainWindow.SOP.Protocol.scenario.environment.ecs.reactions_dict.ContainsKey(reac.entity_guid))
            {
                MainWindow.SOP.Protocol.scenario.environment.ecs.Reactions.Remove(reac);
            }
        }

        private void AddEcmReacCompButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigReactionComplex crc = (ConfigReactionComplex)lbAvailableReacCx.SelectedItem;
            if (crc != null)
            {
                if (!MainWindow.SOP.Protocol.scenario.environment.ecs.reaction_complexes_guid_ref.Contains(crc.entity_guid))
                {
                    MainWindow.SOP.Protocol.scenario.environment.ecs.reaction_complexes_guid_ref.Add(crc.entity_guid);
                }
            }
        }

        private void RemoveEcmReacCompButton_Click(object sender, RoutedEventArgs e)
        {
            int nIndex = ReactionComplexListBox.SelectedIndex;
            if (nIndex >= 0)
            {
                string guid = (string)ReactionComplexListBox.SelectedValue;
                MainWindow.SOP.Protocol.scenario.environment.ecs.reaction_complexes_guid_ref.Remove(guid);
            }
        }

        //LIBRARIES REACTION COMPLEXES HANDLERS

        //private void btnCopyReactionComplex_Click(object sender, RoutedEventArgs e)
        //{
        //    if (lbComplexes.SelectedIndex < 0)
        //    {
        //        MessageBox.Show("Select a reaction complex to copy from.");
        //        return;
        //    }

        //    ConfigReactionComplex crcCurr = (ConfigReactionComplex)lbComplexes.SelectedItem;
        //    ConfigReactionComplex crcNew = crcCurr.Clone();

        //    MainWindow.SOP.Protocol.entity_repository.reaction_complexes.Add(crcNew);

        //    lbComplexes.SelectedIndex = lbComplexes.Items.Count - 1;            
        //}

        //private void btnAddReactionComplex_Click(object sender, RoutedEventArgs e)
        //{
        //    AddReacComplex arc = new AddReacComplex(ReactionComplexDialogType.AddComplex);
        //    if (arc.ShowDialog() == true)
        //        lbComplexes.SelectedIndex = lbComplexes.Items.Count - 1;
        //}

        //private void btnEditReactionComplex_Click(object sender, RoutedEventArgs e)
        //{
        //    ConfigReactionComplex crc = (ConfigReactionComplex)lbComplexes.SelectedItem;
        //    if (crc == null)
        //        return;

        //    AddReacComplex arc = new AddReacComplex(ReactionComplexDialogType.EditComplex, crc);
        //    arc.ShowDialog();

        //}

        //private void btnRemoveReactionComplex_Click(object sender, RoutedEventArgs e)
        //{
        //    ConfigReactionComplex crc = (ConfigReactionComplex)(lbComplexes.SelectedItem);
        //    if (crc != null)
        //    {
        //        MessageBoxResult res;
        //        res = MessageBox.Show("Are you sure you would like to remove this reaction complex?", "Warning", MessageBoxButton.YesNo);
        //        if (res == MessageBoxResult.No)
        //            return;

        //        int index = lbComplexes.SelectedIndex;
        //        MainWindow.SOP.Protocol.entity_repository.reaction_complexes.Remove(crc);

        //        lbComplexes.SelectedIndex = index;

        //        if (index >= lbComplexes.Items.Count)
        //            lbComplexes.SelectedIndex = lbComplexes.Items.Count - 1;

        //        if (lbComplexes.Items.Count == 0)
        //            lbComplexes.SelectedIndex = -1;

        //    }

        //    //btnGraphReactionComplex.IsChecked = true;
        //}



        ////LIBRARIES TAB EVENT HANDLERS
        ////MOLECULES        
        //private void btnAddLibMolecule_Click(object sender, RoutedEventArgs e)
        //{
        //    ConfigMolecule gm = new ConfigMolecule();
        //    gm.Name = gm.GenerateNewName(MainWindow.SOP.Protocol, "_New");
        //    MainWindow.SOP.Protocol.entity_repository.molecules.Add(gm);
        //    dgLibMolecules.SelectedIndex = dgLibMolecules.Items.Count - 1;

        //    ConfigMolecule cm = (ConfigMolecule)dgLibMolecules.SelectedItem;
        //    dgLibMolecules.ScrollIntoView(cm);
        //}

        //private void btnCopyMolecule_Click(object sender, RoutedEventArgs e)
        //{
        //    ConfigMolecule cm = (ConfigMolecule)dgLibMolecules.SelectedItem;

        //    if (cm == null)
        //        return;

        //    //ConfigMolecule gm = new ConfigMolecule(cm);
        //    ConfigMolecule newmol = cm.Clone(MainWindow.SOP.Protocol);
        //    MainWindow.SOP.Protocol.entity_repository.molecules.Add(newmol);
        //    dgLibMolecules.SelectedIndex = dgLibMolecules.Items.Count - 1;

        //    cm = (ConfigMolecule)dgLibMolecules.SelectedItem;
        //    dgLibMolecules.ScrollIntoView(cm);
        //}

        //private void btnRemoveMolecule_Click(object sender, RoutedEventArgs e)
        //{
        //    ConfigMolecule gm = (ConfigMolecule)dgLibMolecules.SelectedValue;

        //    MessageBoxResult res;
        //    if (MainWindow.SOP.Protocol.scenario.environment.ecs.HasMolecule(gm))
        //    {
        //        res = MessageBox.Show("If you remove this molecule, corresponding entities that depend on this molecule will also be deleted. Would you like to continue?", "Warning", MessageBoxButton.YesNo);
        //    }
        //    else
        //    {
        //        res = MessageBox.Show("Are you sure you would like to remove this molecule?", "Warning", MessageBoxButton.YesNo);
        //    }

        //    if (res == MessageBoxResult.No)
        //        return;

        //    int index = dgLibMolecules.SelectedIndex;
        //    MainWindow.SOP.Protocol.scenario.environment.ecs.RemoveMolecularPopulation(gm.entity_guid);
        //    MainWindow.SOP.Protocol.entity_repository.molecules.Remove(gm);
        //    dgLibMolecules.SelectedIndex = index;

        //    if (index >= dgLibMolecules.Items.Count)
        //        dgLibMolecules.SelectedIndex = dgLibMolecules.Items.Count - 1;

        //    if (dgLibMolecules.Items.Count == 0)
        //        dgLibMolecules.SelectedIndex = -1;

        //}

        //LIBRARY REACTIONS EVENT HANDLERS        
        //private void btnRemoveReaction_Click(object sender, RoutedEventArgs e)
        //{
        //    ConfigReaction cr = (ConfigReaction)lvReactions.SelectedItem;
        //    if (cr == null)
        //    {
        //        return;
        //    }

        //    MainWindow.SOP.Protocol.entity_repository.reactions.Remove(cr);
        //}

        //CELLS EVENT HANDLERS

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

        //////private void CellsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //////{
        //////    //if (lvCellAvailableReacs.ItemsSource != null)
        //////    //    CollectionViewSource.GetDefaultView(lvCellAvailableReacs.ItemsSource).Refresh();
        //////    //if (lvCytosolAvailableReacs.ItemsSource != null)
        //////    //    CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();

        //////    //DiffSchemeExpander_Expanded(null, null);
        //////    ucCellDetails.DiffSchemeExpander_Expanded(null, null);

        //////}

        //****************************************************************************************************************



        ////private void ecmReactionCollectionViewSource_Filter(object sender, FilterEventArgs e)
        ////{
        ////    ConfigReaction cr = e.Item as ConfigReaction;
        ////    if (cr != null)
        ////    {
        ////        // Filter out cr if not in ecm reaction list 
        ////        if (MainWindow.SOP.Protocol.scenario.environment.ecs.reactions_guid_ref.Contains(cr.entity_guid))
        ////        {
        ////            e.Accepted = true;
        ////        }
        ////        else
        ////        {
        ////            e.Accepted = false;
        ////        }
        ////    }
        ////}

        ////private void membReactionCollectionViewSource_Filter(object sender, FilterEventArgs e)
        ////{
        ////    ConfigReaction cr = e.Item as ConfigReaction;
        ////    ConfigCell cc = (ConfigCell)CellsListBox.SelectedItem;
        ////    if (cr != null && cc != null)
        ////    {
        ////        e.Accepted = false;
        ////        // Filter out cr if not in membrane reaction list 
        ////        if (cc.membrane.reactions_guid_ref.Contains(cr.entity_guid))
        ////        {
        ////            e.Accepted = true;
        ////        }

        ////    }
        ////}

        ////private void ecmReactionComplexReactionCollectionViewSource_Filter(object sender, FilterEventArgs e)
        ////{
        ////    if (ReactionComplexListBox.SelectedIndex < 0)
        ////        return;

        ////    ConfigReaction cr = e.Item as ConfigReaction;
        ////    string guidRC = (string)ReactionComplexListBox.SelectedItem;

        ////    e.Accepted = true;

        ////    if (guidRC != null && cr != null)
        ////    {
        ////        ConfigReactionComplex crc = MainWindow.SOP.Protocol.entity_repository.reaction_complexes_dict[guidRC];
        ////        e.Accepted = false;
        ////        // Filter out cr if not in ecm reaction list 
        ////        if (crc.reactions_guid_ref.Contains(cr.entity_guid))
        ////        {
        ////            e.Accepted = true;
        ////        }
        ////    }
        ////}

        ////private void cytosolReactionsCollectionViewSource_Filter(object sender, FilterEventArgs e)
        ////{
        ////    ConfigReaction cr = e.Item as ConfigReaction;
        ////    ConfigCell cc = (ConfigCell)CellsListBox.SelectedItem;
        ////    if (cr != null && cc != null)
        ////    {
        ////        // Filter out cr if not in cytosol reaction list 
        ////        if (cc.cytosol.reactions_guid_ref.Contains(cr.entity_guid))
        ////        {
        ////            e.Accepted = true;
        ////        }
        ////        else
        ////        {
        ////            e.Accepted = false;
        ////        }
        ////    }
        ////}

        ////private void cellMolPopsCollectionViewSource_Filter(object sender, FilterEventArgs e)
        ////{
        ////    //if (treeCellPops.SelectedItem != null)
        ////    //    return;

        ////    //ConfigCell cc = e.Item as ConfigCell;
        ////    //string guidCell = cc.cell_guid;

        ////    //e.Accepted = true;

        ////    //if (guidRC != null && cr != null)
        ////    //{
        ////    //    ConfigReactionComplex crc = MainWindow.SOP.Protocol.entity_repository.reaction_complexes_dict[guidRC];
        ////    //    e.Accepted = false;
        ////    //    // Filter out cr if not in ecm reaction list 
        ////    //    if (crc.reactions_guid_ref.Contains(cr.reaction_guid))
        ////    //    {
        ////    //        e.Accepted = true;
        ////    //    }
        ////    //}
        ////}




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

            double[] extents = new double[3] { MainWindow.SOP.Protocol.scenario.environment.extent_x, 
                                               MainWindow.SOP.Protocol.scenario.environment.extent_y, 
                                               MainWindow.SOP.Protocol.scenario.environment.extent_z };
            double minDisSquared = 2 * MainWindow.SOP.Protocol.entity_repository.cells_dict[cellPop.Cell.entity_guid].CellRadius;
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

                ////Add this after 2/4/14
                ////gg.DrawAsWireframe = true;

                //gg.gaussian_spec_color = cellPop.cellpopulation_color;
                gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.2f, cellPop.cellpopulation_color.R, cellPop.cellpopulation_color.G, cellPop.cellpopulation_color.B);
                AddGaussianSpecification(gg, box);

                cellPop.cellPopDist = new CellPopGaussian(extents, minDisSquared, box, cellPop);
                ((CellPopGaussian)cellPop.cellPopDist).gauss_spec_guid_ref = gg.gaussian_spec_box_guid_ref;

                // Connect the VTK callback
                MainWindow.GC.Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(MainWindow.GC.WidgetInteractionToGUICallback));
                MainWindow.GC.Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(RegionFocusToGUISection));

            }
            else if (cpdt == CellPopDistributionType.Specific)
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
                    cellPop.CellStates = tempCellPop.CellStates;
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

        public void SelectGaussSpecInGUI(int index, string guid)
        {
            bool isBoxInCell = false;

            foreach (CellPopulation cp in MainWindow.SOP.Protocol.scenario.cellpopulations)
            {
                CellPopDistribution cpd = cp.cellPopDist;
                if (cpd.DistType == CellPopDistributionType.Gaussian)
                {
                    CellPopGaussian cpg = cpd as CellPopGaussian;
                    if (cpg.gauss_spec_guid_ref == guid)
                    {
                        //BoxSpecification box = MainWindow.SOP.Protocol.box_guid_box_dict[guid];
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

                    if (!gui_spot_found)
                    {
                        // Next check whether any Solfacs use this right gaussian_spec for this box
                        for (int r = 0; r < MainWindow.SOP.Protocol.scenario.environment.ecs.molpops.Count; r++)
                        {
                            // We'll just be picking the first one that uses 
                            if (MainWindow.SOP.Protocol.scenario.environment.ecs.molpops[r].mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian &&
                                ((MolPopGaussian)MainWindow.SOP.Protocol.scenario.environment.ecs.molpops[r].mp_distribution).gaussgrad_gauss_spec_guid_ref == key)
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
                        for (int r = 0; r < MainWindow.SOP.Protocol.scenario.gaussian_specifications.Count; r++)
                        {
                            // We'll just be picking the first one that uses 
                            if (MainWindow.SOP.Protocol.scenario.gaussian_specifications[r].gaussian_spec_box_guid_ref == key)
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

            // Only want to respond to purposeful user interaction, not just population and depopulation
            // of solfacs list
            if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
                return;

            ComboBox cb = (ComboBox)e.Source;
            if (cb == null)
                return;

            //ConfigCell cc = (ConfigCell)cb.SelectedItem;
            //string cellname = MainWindow.SOP.Protocol.entity_repository.cells_dict[cc.cell_guid].CellName;
            CellPopulation cp = (CellPopulation)CellPopsListBox.SelectedItem;
            if (cp == null)
                return;

            if (cb.IsDropDownOpen == false) return;
            cb.IsDropDownOpen = false;

            string curr_cell_pop_name = cp.cellpopulation_name;
            string curr_cell_type_guid = "";
            curr_cell_type_guid = cp.Cell.entity_guid;

            int nIndex = cb.SelectedIndex;
            if (nIndex < 0)
                return;

            //if user picked 'new cell type' then create new configcell in ER
            if (nIndex == (cb.Items.Count - 1))
            {
                ConfigCell newLibCell = new ConfigCell();
                newLibCell.CellName = newLibCell.GenerateNewName(MainWindow.SOP.Protocol, "_New");
                ////AddEditMolecule aem = new AddEditMolecule(newLibCell, MoleculeDialogType.NEW);

                //////if user cancels out of new cell dialog, set selected cell back to what it was
                ////if (aem.ShowDialog() == false)
                ////{
                ////    if (e.RemovedItems.Count > 0)
                ////    {
                ////        cb.SelectedItem = e.RemovedItems[0];
                ////    }
                ////    else
                ////    {
                ////        cb.SelectedIndex = 0;
                ////    }
                ////    return;
                ////}

                Protocol B = MainWindow.SOP.Protocol;
                newLibCell.incrementChangeStamp();
                Level.PushStatus status = B.pushStatus(newLibCell);
                if (status == Level.PushStatus.PUSH_CREATE_ITEM)
                {
                    B.repositoryPush(newLibCell, status); // push into B, inserts as new
                }

                cp.Cell = newLibCell.Clone(true);
                cp.Cell.CellName = newLibCell.CellName;
                cb.SelectedItem = newLibCell;
            }
            //user picked existing cell type 
            else
            {
                ConfigCell cell_to_clone = MainWindow.SOP.Protocol.entity_repository.cells[nIndex];
                //thid entity_guid will already be different, since "cell" in cellpopulation is an instance
                //of configCell, it will has its own entity_guid - only the name stays the same ---
                if (cell_to_clone.entity_guid != curr_cell_type_guid)
                {
                cp.Cell = cell_to_clone.Clone(true);

                    string new_cell_name = MainWindow.SOP.Protocol.entity_repository.cells[nIndex].CellName;
                    if (curr_cell_type_guid != cp.Cell.entity_guid) // && curr_cell_pop_name.Length == 0)
                    {
                        cp.cellpopulation_name = new_cell_name;
                    }
                }
                //ucCellPopCellDetails.DataContext = cp.Cell;
            }
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
            if (numNew > numOld && numNew > cp.CellStates.Count)
            {
                int rows_to_add = numNew - numOld;
                cp.cellPopDist.AddByDistr(rows_to_add);
            }
            else if (numNew < numOld)
            {
                if (numOld > cp.CellStates.Count)
                {
                    numOld = cp.CellStates.Count;
                }

                int rows_to_delete = numOld - numNew;
                cp.RemoveCells(rows_to_delete);
            }
            cp.number = cp.CellStates.Count;
        }

        private void cellPopsListBoxSelChanged(object sender, SelectionChangedEventArgs e)
        {
            //newCellPopSelected = true;
        }

        private void DataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            e.Row.Header = (e.Row.GetIndex() + 1).ToString();
        }

        /// <summary>
        /// Called after each key stroke in a selected cell in the data grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

                cp.CellStates.Clear();
                for (int i = 0; i < paste.Length; i += 3)
                {
                    cp.CellStates.Add(new CellState(double.Parse(paste[i]), double.Parse(paste[i + 1]), double.Parse(paste[i + 2])));
                }

                cp.number = cp.CellStates.Count;

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

        /// <summary>
        ///  Called when user selects "Specific" for cell population locations.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgLocations_Unloaded(Object sender, RoutedEventArgs e)
        {
            // 11/10/2013 gmk: Don't think this is needed anymore.
            // Delete?

            //CellPopulation cp = (CellPopulation)CellPopsListBox.SelectedItem;
            //if (cp == null)
            //    return;
            ////cp.number = cp.cell_list.Count;
            //cp.number = cp.cellPopDist.CellStates.Count;

            //if (cp.cellPopDist.DistType == CellPopDistributionType.Specific)
            //{
            //    CellPopSpecific cps = cp.cellPopDist as CellPopSpecific;
            //    //cps.CopyLocations(cp);
            //}
        }

        /// <summary>
        /// Call after focus lost from selected cell in the data grid.
        /// Check that positions are still in bounds.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgLocations_CheckPositions(Object sender, RoutedEventArgs e)
        {
            CellPopulation cp = (CellPopulation)CellPopsListBox.SelectedItem;
            if (cp == null)
                return;

            // Remove out-of-bounds cells
            bool changed = false;
            for (int i = cp.CellStates.Count - 1; i >= 0; i--)
            {
                double[] pos = new double[3] { cp.CellStates[i].X, cp.CellStates[i].Y, cp.CellStates[i].Z };
                // X
                if (cp.CellStates[i].X < 0)
                {
                    // cp.cellPopDist.CellStates[i].X = 0;
                    pos[0] = 0;
                    changed = true;
                }
                if (cp.CellStates[i].X > cp.cellPopDist.Extents[0])
                {
                    // cp.cellPopDist.CellStates[i].X = cp.cellPopDist.Extents[0];
                    pos[0] = cp.cellPopDist.Extents[0];
                    changed = true;
                }
                // Y
                if (cp.CellStates[i].Y < 0)
                {
                    //cp.cellPopDist.CellStates[i].Y = 0;
                    pos[1] = 0;
                    changed = true;
                }
                if (cp.CellStates[i].Y > cp.cellPopDist.Extents[1])
                {
                    //cp.cellPopDist.CellStates[i].Y = cp.cellPopDist.Extents[1];
                    pos[1] = cp.cellPopDist.Extents[1];
                    changed = true;
                }
                // Z
                if (cp.CellStates[i].Z < 0)
                {
                    //cp.cellPopDist.CellStates[i].Z = 0;
                    pos[2] = 0;
                    changed = true;
                }
                if (cp.CellStates[i].Z > cp.cellPopDist.Extents[2])
                {
                    //cp.cellPopDist.CellStates[i].Z = cp.cellPopDist.Extents[2];
                    pos[2] = cp.cellPopDist.Extents[2];
                    changed = true;
                }
                if (changed)
                {
                    // Can't update coordinates directly or the datagrid doesn't update properly
                    // (e.g., cp.cellPopDist.CellStates[i].Z = cp.cellPopDist.Extents[2];)
                    cp.CellStates.RemoveAt(i);
                    cp.cellPopDist.AddByPosition(pos);
                }
            }

        }

        private void blob_actor_checkbox_clicked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = e.OriginalSource as CheckBox;

            if (cb.CommandParameter == null)
                return;

            string guid = cb.CommandParameter as string;
            if (guid.Length > 0)
            {
                if (MainWindow.SOP.Protocol.scenario.gauss_guid_gauss_dict.ContainsKey(guid))
                {
                    GaussianSpecification gs = MainWindow.SOP.Protocol.scenario.gauss_guid_gauss_dict[guid];
                    gs.gaussian_region_visibility = (bool)(cb.IsChecked);
                }
            }
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

            if (cb.SelectedIndex == -1)
                return;

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
                ConfigMolecularPopulation cmp = (ConfigMolecularPopulation)(lbEcsMolPops.SelectedItem);
                if (cmp == null)
                    return;
                if (cmp.mp_distribution.mp_distribution_type == MolPopDistributionType.Linear)
                {
                    ((MolPopLinear)cmp.mp_distribution).Initalize((BoundaryFace)cb.SelectedItem);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            int x = 1;
            x++;
        }


        ////private string GenerateNewCellName(ConfigCell cell)
        ////{
        ////    int nSuffix = 1;
        ////    string sSuffix = string.Format("_Copy{0:000}", nSuffix);
        ////    string TempCellName = cell.CellName;
        ////    while (FindCellBySuffix(sSuffix) == true)
        ////    {
        ////        TempCellName = cell.CellName.Replace(sSuffix, "");
        ////        nSuffix++;
        ////        sSuffix = string.Format("_Copy{0:000}", nSuffix);
        ////    }
        ////    TempCellName += sSuffix;
        ////    return TempCellName;
        ////}

        ////private string GenerateNewCellName(ConfigCell cell, string ending)
        ////{
        ////    int nSuffix = 1;
        ////    string sSuffix = ending + string.Format("{0:000}", nSuffix);
        ////    string TempCellName = cell.CellName;
        ////    while (FindCellBySuffix(sSuffix) == true)
        ////    {
        ////        TempCellName = cell.CellName.Replace(sSuffix, "");
        ////        nSuffix++;
        ////        sSuffix = ending + string.Format("{0:000}", nSuffix);
        ////    }
        ////    TempCellName += sSuffix;
        ////    return TempCellName;
        ////}

        ////// given a cell type name, check if it exists in repos
        ////private static bool FindCellBySuffix(string suffix)
        ////{
        ////    foreach (ConfigCell cc in MainWindow.SOP.Protocol.entity_repository.cells)
        ////    {
        ////        if (cc.CellName.EndsWith(suffix))
        ////        {
        ////            return true;
        ////        }
        ////    }
        ////    return false;
        ////}

        private void cbCellColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox ColorComboBox = sender as ComboBox;
            int n = ColorComboBox.SelectedIndex;
        }

        private void genericRepositoryPush(ConfigEntity e)
        {
            //-get status
            //-make sure it’s pushable at all, not all entities are
            //-(A)if it’s pushable, they may want to push it even if it is older (to reset or such), and you have to ask for that
            //-(B) if it’s a new item there is no further choice, execute the push
            //-(C)if it’s an existing item they may want to override the original or insert as new; to insert as new, regenerate the guid; execute the push

            Protocol B = MainWindow.SOP.Protocol;
            Level.PushStatus status = B.pushStatus(e);

            //Is pushable
            if (e is ConfigMolecule)
            {
                B.repositoryPush(e, status); // push into B, inserts as new
            }

        }


        private void ecs_molpop_molecule_combo_box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            ComboBox cb = (ComboBox)e.Source;
            if (cb == null)
                return;

            ConfigMolecularPopulation molpop = (ConfigMolecularPopulation)lbEcsMolPops.SelectedItem;

            if (molpop == null)
                return;

            string curr_mol_pop_name = molpop.Name;
            string curr_mol_guid = "";
            curr_mol_guid = molpop.molecule.entity_guid;

            int nIndex = cb.SelectedIndex;
            if (nIndex < 0)
                return;

            //if user picked 'new molecule' then create new molecule in ER
            if (nIndex == (cb.Items.Count - 1))
            {
                ConfigMolecule newLibMol = new ConfigMolecule();
                newLibMol.Name = newLibMol.GenerateNewName(MainWindow.SOP.Protocol, "_New");
                AddEditMolecule aem = new AddEditMolecule(newLibMol, MoleculeDialogType.NEW);

                //Point position = cb.PointToScreen(new Point(0d, 0d));
                //double wid = cb.Width;
                //aem.Top = position.Y;
                //aem.Left = position.X + wid;

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

                //MainWindow.SOP.Protocol.entity_repository.molecules.Add(newLibMol);
                //molpop.molecule = newLibMol.Clone(null);
                //molpop.Name = newLibMol.Name;
                //cb.SelectedItem = newLibMol;

                Protocol B = MainWindow.SOP.Protocol;
                newLibMol.incrementChangeStamp();
                Level.PushStatus status = B.pushStatus(newLibMol);
                if (status == Level.PushStatus.PUSH_CREATE_ITEM)
                {
                    B.repositoryPush(newLibMol, status); // push into B, inserts as new
                }

                molpop.molecule = newLibMol.Clone(null);
                molpop.Name = newLibMol.Name;
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
                    molpop.Name = new_mol_name;
            }

            if (lvAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();
        }

        private void TabItem_Loaded(object sender, RoutedEventArgs e)
        {
            lbRptCellPops.SelectedIndex = 0;
            ICollectionView icv = CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource);
            if (icv != null)
            {
                icv.Refresh();
            }
        }

        //private void MolTextBox_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    ConfigMolecule cm = dgLibMolecules.SelectedItem as ConfigMolecule;

        //    if (cm == null)
        //        return;

        //    cm.ValidateName(MainWindow.SOP.Protocol);

        //    int index = dgLibMolecules.SelectedIndex;
        //    dgLibMolecules.InvalidateVisual();
        //    dgLibMolecules.Items.Refresh();
        //    dgLibMolecules.SelectedIndex = index;
        //    cm = (ConfigMolecule)dgLibMolecules.SelectedItem;
        //    dgLibMolecules.ScrollIntoView(cm);
        //}

        private void comboToroidal_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = e.Source as ComboBox;

            if (!cb.IsDropDownOpen)
                return;

            if (cb.SelectedIndex == (int)(BoundaryType.Toroidal))
            {
                MessageBoxResult res;
                res = MessageBox.Show("If you change the boundary condition to toroidal, all molecular populations using Linear initial distribution will be changed to Homogeneous.  Are you sure you would like to proceed?", "Warning", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                {
                    cb.SelectedIndex = (int)(BoundaryType.Zero_Flux);
                    return;
                }

                foreach (ConfigMolecularPopulation cmp in MainWindow.SOP.Protocol.scenario.environment.ecs.molpops)
                {
                    if (cmp.mp_distribution.mp_distribution_type == MolPopDistributionType.Linear)
                    {
                        MolPopHomogeneousLevel shl = new MolPopHomogeneousLevel();
                        cmp.mp_distribution = shl;
                    }
                }
            }
        }


        //private void DrawSelectedReactionComplex()
        //{
        //    ConfigReactionComplex crc = (ConfigReactionComplex)(lbComplexes.SelectedItem);

        //    //Cleanup any previous RC stuff
        //    foreach (ConfigCell cell in MainWindow.SOP.Protocol.entity_repository.cells.ToList())
        //    {
        //        if (cell.CellName == "RCCell")
        //        {
        //            MainWindow.SOP.Protocol.entity_repository.cells.Remove(cell);
        //        }
        //    }
        //    //crc.RCSim.reset();
        //    MainWindow.SOP.Protocol.rc_scenario.cellpopulations.Clear();
        //    // end of cleanup

        //    ConfigCell cc = new ConfigCell();
        //    cc.CellName = "RCCell";
        //    foreach (ConfigMolecularPopulation cmp in crc.molpops)
        //    {
        //        cc.cytosol.molpops.Add(cmp);
        //    }
        //    foreach (ConfigGene configGene in crc.genes)
        //    {
        //        cc.genes_guid_ref.Add(configGene.entity_guid);
        //    }
        //    foreach (string rguid in crc.reactions_guid_ref)
        //    {
        //        if (MainWindow.SOP.Protocol.entity_repository.reactions_dict.ContainsKey(rguid) == true)
        //        {
        //            ConfigReaction cr = MainWindow.SOP.Protocol.entity_repository.reactions_dict[rguid];

        //            cc.cytosol.Reactions.Add(cr.Clone(true));
        //        }
        //    }
        //    MainWindow.SOP.Protocol.entity_repository.cells.Add(cc);
        //    MainWindow.SOP.Protocol.rc_scenario.cellpopulations.Clear();

        //    CellPopulation cp = new CellPopulation();
        //    cp.Cell = cc;
        //    //cp.Cell.entity_guid = cc.entity_guid;
        //    cp.cellpopulation_name = "RC cell";
        //    cp.number = 1;

        //    // Add cell population distribution information
        //    double[] extents = new double[3] { MainWindow.SOP.Protocol.rc_scenario.environment.extent_x, 
        //                                       MainWindow.SOP.Protocol.rc_scenario.environment.extent_y, 
        //                                       MainWindow.SOP.Protocol.rc_scenario.environment.extent_z };
        //    double minDisSquared = 2 * MainWindow.SOP.Protocol.entity_repository.cells_dict[cp.Cell.entity_guid].CellRadius;
        //    minDisSquared *= minDisSquared;
        //    cp.cellPopDist = new CellPopSpecific(extents, minDisSquared, cp);
        //    cp.cellPopDist.CellStates[0] = new CellState(MainWindow.SOP.Protocol.rc_scenario.environment.extent_x,
        //                                                    MainWindow.SOP.Protocol.rc_scenario.environment.extent_y / 2,
        //                                                    MainWindow.SOP.Protocol.rc_scenario.environment.extent_z / 2);

        //    cp.cellpopulation_color = System.Windows.Media.Color.FromScRgb(1.0f, 1.0f, 0.5f, 0.0f);
        //    MainWindow.SOP.Protocol.rc_scenario.cellpopulations.Add(cp);

        //    ReactionComplexProcessor Processor = new ReactionComplexProcessor();
        //    MainWindow.Sim.Load(MainWindow.SOP.Protocol, true, true);

        //    Processor.Initialize(MainWindow.SOP.Protocol, crc, MainWindow.Sim);
        //    Processor.Go();

        //    MainWindow.ST_ReacComplexChartWindow.Title = "Reaction Complex: " + crc.Name;
        //    MainWindow.ST_ReacComplexChartWindow.RC = Processor;  //crc.Processor;
        //    MainWindow.ST_ReacComplexChartWindow.DataContext = Processor; //crc.Processor;
        //    MainWindow.ST_ReacComplexChartWindow.Render();

        //    MainWindow.ST_ReacComplexChartWindow.dblMaxTime.Number = Processor.dInitialTime;  //crc.Processor.dInitialTime;
        //    MW.VTKDisplayDocWindow.Activate();
        //    MainWindow.ST_ReacComplexChartWindow.Activate();
        //    MainWindow.ST_ReacComplexChartWindow.toggleButton = btnGraphReactionComplex;
        //}

        //private void btnGraphReactionComplex_Checked(object sender, RoutedEventArgs e)
        //{
        //    if (lbComplexes.SelectedIndex < 0)
        //    {
        //        MessageBox.Show("Select a reaction complex to process.");
        //        return;
        //    }

        //    if (btnGraphReactionComplex.IsChecked == true)
        //    {
        //        DrawSelectedReactionComplex();
        //        btnGraphReactionComplex.IsChecked = false;
        //    }
        //}

        //private void btnGraphReactionComplex_Click(object sender, RoutedEventArgs e)
        //{
        //    if (lbComplexes.SelectedIndex < 0)
        //    {
        //        MessageBox.Show("Select a reaction complex to process.");
        //        return;
        //    }

        //    DrawSelectedReactionComplex();
        //}

        //public ConfigReactionComplex GetConfigReactionComplex()
        //{
        //    if (lbComplexes.SelectedIndex < 0)
        //    {
        //        MessageBox.Show("Select a reaction complex to process.");
        //        return null;
        //    }
        //    return (ConfigReactionComplex)(lbComplexes.SelectedItem);
        //}

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (lvCellAvailableReacs.ItemsSource != null)
            //    CollectionViewSource.GetDefaultView(lvCellAvailableReacs.ItemsSource).Refresh();
            //if (lvCytosolAvailableReacs.ItemsSource != null)
            //    CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();
        }

        private void CellAddReacExpander_Expanded(object sender, RoutedEventArgs e)
        {
            //if (lvCellAvailableReacs.ItemsSource != null)
            //    CollectionViewSource.GetDefaultView(lvCellAvailableReacs.ItemsSource).Refresh();
        }

        private void CellAddReacExpander2_Expanded(object sender, RoutedEventArgs e)
        {
            //if (lvCytosolAvailableReacs.ItemsSource != null)
            //    CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();
        }

        //This helps in refreshing the available reactions for the ECM tab
        private void ConfigTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabECM.IsSelected == true)
            {
                if (lvAvailableReacs.ItemsSource != null)
                    CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();
            }
        }

        private void time_duration_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MainWindow.SOP == null)
                return;
            if (MainWindow.SOP.Protocol == null)
                return;
            if (MainWindow.SOP.Protocol.scenario == null)
                return;

            //Need to do this to make sure that the sampling and rendering sliders get the correct "Value"s
            double temp_samp = MainWindow.SOP.Protocol.scenario.time_config.sampling_interval;
            double temp_rand = MainWindow.SOP.Protocol.scenario.time_config.rendering_interval;

            sampling_interval_slider.Maximum = time_duration_slider.Value;
            if (temp_samp > sampling_interval_slider.Maximum)
                temp_samp = sampling_interval_slider.Maximum;
            sampling_interval_slider.Value = temp_samp;
            MainWindow.SOP.Protocol.scenario.time_config.sampling_interval = sampling_interval_slider.Value;

            time_step_slider.Maximum = time_duration_slider.Value;
            if (temp_rand > time_step_slider.Maximum)
                temp_rand = time_step_slider.Maximum;
            time_step_slider.Value = temp_rand;
            MainWindow.SOP.Protocol.scenario.time_config.rendering_interval = time_step_slider.Value;

        }

        private void sampling_interval_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MainWindow.SOP == null)
                return;

            if (MainWindow.SOP.Protocol == null)
                return;

            if (MainWindow.SOP.Protocol.scenario == null)
                return;

            if (MainWindow.SOP.Protocol.scenario.time_config.sampling_interval > sampling_interval_slider.Maximum)
                MainWindow.SOP.Protocol.scenario.time_config.sampling_interval = sampling_interval_slider.Value;

            if (MainWindow.SOP.Protocol.scenario.time_config.rendering_interval > time_step_slider.Maximum)
                MainWindow.SOP.Protocol.scenario.time_config.rendering_interval = time_step_slider.Value;
        }


        private void memb_molecule_combo_box_GotFocus(object sender, RoutedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            CollectionViewSource.GetDefaultView(combo.ItemsSource).Refresh();
        }

        private void cyto_molecule_combo_box_GotFocus(object sender, RoutedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            CollectionViewSource.GetDefaultView(combo.ItemsSource).Refresh();
        }

        private void btnRegenerateCellPositions_Click(object sender, RoutedEventArgs e)
        {
            // cellPop points to the current CellPopulation
            CellPopulation cellPop = (CellPopulation)CellPopsListBox.SelectedItem;
            if (cellPop == null)
            {
                return;
            }
            // current_dist points to the current distribution in cellPop
            CellPopDistribution current_dist = cellPop.cellPopDist;

            if (current_dist.DistType == CellPopDistributionType.Gaussian || current_dist.DistType == CellPopDistributionType.Uniform)
            {
                current_dist.Reset();
                MW.resetButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }

        //private void btnRemoveGene_Click(object sender, RoutedEventArgs e)
        //{
        //    ConfigGene gene = (ConfigGene)dgLibGenes.SelectedValue;
        //    MessageBoxResult res;

        //    res = MessageBox.Show("Are you sure you would like to remove this gene?", "Warning", MessageBoxButton.YesNo);

        //    if (res == MessageBoxResult.No)
        //        return;

        //    int index = dgLibMolecules.SelectedIndex;
        //    //MainWindow.SOP.Protocol.scenario.environment.ecs.RemoveMolecularPopulation(gm.molecule_guid);
        //    MainWindow.SOP.Protocol.entity_repository.genes.Remove(gene);
        //    dgLibGenes.SelectedIndex = index;

        //    if (index >= dgLibMolecules.Items.Count)
        //        dgLibGenes.SelectedIndex = dgLibGenes.Items.Count - 1;

        //    if (dgLibGenes.Items.Count == 0)
        //        dgLibGenes.SelectedIndex = -1;

        //}

        //private void btnCopyGene_Click(object sender, RoutedEventArgs e)
        //{
        //    ConfigGene gene = (ConfigGene)dgLibGenes.SelectedItem;

        //    if (gene == null)
        //        return;

        //    ConfigGene newgene = gene.Clone(MainWindow.SOP.Protocol);
        //    MainWindow.SOP.Protocol.entity_repository.genes.Add(newgene);
        //    dgLibGenes.SelectedIndex = dgLibGenes.Items.Count - 1;

        //    gene = (ConfigGene)dgLibGenes.SelectedItem;
        //    dgLibGenes.ScrollIntoView(newgene);
        //}

        //private void btnAddGene_Click(object sender, RoutedEventArgs e)
        //{
        //    ConfigGene gm = new ConfigGene("NewGene", 0, 0);
        //    gm.Name = gm.GenerateNewName(MainWindow.SOP.Protocol, "_New");
        //    MainWindow.SOP.Protocol.entity_repository.genes.Add(gm);
        //    dgLibGenes.SelectedIndex = dgLibGenes.Items.Count - 1;

        //    ConfigGene cm = (ConfigGene)dgLibGenes.SelectedItem;
        //    dgLibGenes.ScrollIntoView(cm);
        //}

        ////private void cyto_gene_combo_box_GotFocus(object sender, RoutedEventArgs e)
        ////{
        ////    ComboBox combo = sender as ComboBox;
        ////    CollectionViewSource.GetDefaultView(combo.ItemsSource).Refresh();
        ////}

        ////private void cyto_gene_combo_box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        ////{
        ////    ComboBox cb = (ComboBox)e.Source;
        ////    if (cb == null)
        ////        return;
        ////}

        /// <summary>
        /// This method is called when the user changes a combo box selection in a grid cell
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void comboMolPops_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
#if false
            //This code is no good and is "iffed out". Was trying to get the grid cell where the user clicked.
            //Apparently, this is very hard to do in wpf.  How nice!

            //PROBABLY WILL NOT NEED THIS AT ALL BECAUSE THE DATA BINDINGS ARE WORKING. SO REMOVE IT WHEN WE'RE SURE WE DON'T NEED IT.

            //ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;
            //if (cell == null)
            //    return;

            //ComboBox combo = sender as ComboBox;
            //if (sender != null)
            //{
            //    DataGridCellInfo cellInfo = DiffRegGrid.CurrentCell;
            //    if (cellInfo == null)
            //        return;

            //    ConfigTransitionDriverRow driverRow = cell.diff_scheme.Driver.DriverElements[6];  //(ConfigTransitionDriverRow)cellInfo.Item;
                
            //    if (DiffRegGrid.CurrentColumn == null)
            //        return;

            //    int ncol = DiffRegGrid.CurrentColumn.DisplayIndex;
            //    if (ncol < 0)
            //        return;

            //    if (combo.SelectedIndex < 0)
            //        return;

            //    ConfigMolecularPopulation selMolPop = ((ConfigMolecularPopulation)(combo.SelectedItem));

            //    driverRow.elements[ncol].driver_mol_guid_ref = selMolPop.molecule_guid_ref;
            //}
#endif

        }

        // given a molecule name and location, find its guid
        public static string findMoleculeGuid(string name, MoleculeLocation ml, Protocol protocol)
        {
            foreach (ConfigMolecule cm in protocol.entity_repository.molecules)
            {
                if (cm.Name == name && cm.molecule_location == ml)
                {
                    return cm.entity_guid;
                }
            }
            return "";
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

        public static T FindChild<T>(DependencyObject parent) where T : DependencyObject
        {
            // Confirm parent is valid. 
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
                    foundChild = FindChild<T>(child);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
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

        private void EpigeneticMapGrid_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        ////private void cbCellDiffSchemes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        ////{
        ////    //Don't want to do anything when first display this combo box
        ////    //Only do something if user really clicked and selected a different scheme

        ////    if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
        ////        return;

        ////    ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
        ////    if (cell == null)
        ////        return;

        ////    ComboBox combo = sender as ComboBox;

        ////    if (combo.SelectedIndex == -1)
        ////        return;

        ////    if (combo.SelectedIndex == 0)
        ////    {
        ////        cell.diff_scheme = null;
        ////        combo.Text = "None";
        ////    }
        ////    else
        ////    {
        ////        ConfigDiffScheme diffNew = (ConfigDiffScheme)combo.SelectedItem;

        ////        if (cell.diff_scheme != null && diffNew.entity_guid == cell.diff_scheme.entity_guid)
        ////        {
        ////            return;
        ////        }

        ////        EntityRepository er = MainWindow.SOP.Protocol.entity_repository;

        ////        if (er.diff_schemes_dict.ContainsKey(diffNew.entity_guid) == true)
        ////        {
        ////            cell.diff_scheme = er.diff_schemes_dict[diffNew.entity_guid].Clone(true);
        ////        }
        ////    }
        ////    int nIndex = CellsListBox.SelectedIndex;
        ////    CellsListBox.SelectedIndex = -1;
        ////    CellsListBox.SelectedIndex = nIndex;
        ////}

        ////private void chkHasDivDriver_Click(object sender, RoutedEventArgs e)
        ////{
        ////    ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
        ////    if (cell == null)
        ////        return;

        ////    CheckBox ch = sender as CheckBox;
        ////    if (ch.IsChecked == false)
        ////    {
        ////        cell.div_driver = null;
        ////    }
        ////    else
        ////    {
        ////        if (cell.div_driver == null)
        ////        {
        ////            EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
        ////            ConfigTransitionDriver driver = FindFirstDivDriver();

        ////            if (driver == null)
        ////            {
        ////                MessageBox.Show("No division drivers are defined");
        ////                return;
        ////            }

        ////            if (er.transition_drivers_dict.ContainsKey(driver.entity_guid) == true)
        ////            {
        ////                cell.div_driver = er.transition_drivers_dict[driver.entity_guid].Clone(true);
        ////            }
        ////        }
        ////    }
        ////}

        ////private ConfigTransitionDriver FindFirstDeathDriver()
        ////{
        ////    ConfigTransitionDriver driver = null;

        ////    foreach (ConfigTransitionDriver d in MainWindow.SOP.Protocol.entity_repository.transition_drivers)
        ////    {
        ////        string name = d.Name;
        ////        if (name.Contains("apoptosis"))
        ////        {
        ////            driver = d;
        ////            break;
        ////        }
        ////    }

        ////    return driver;
        ////}

        ////private ConfigTransitionDriver FindFirstDivDriver()
        ////{
        ////    ConfigTransitionDriver driver = null;
        ////    foreach (ConfigTransitionDriver d in MainWindow.SOP.Protocol.entity_repository.transition_drivers)
        ////    {
        ////        string name = d.Name;
        ////        if (name.Contains("division"))
        ////        {
        ////            driver = d;
        ////            break;
        ////        }
        ////    }

        ////    return driver;
        ////}

        //private void btnNewDiffScheme_Click(object sender, RoutedEventArgs e)
        //{
        //    AddDifferentiationState("State1");
        //    AddDifferentiationState("State2");
        //}

        //private void btnDelDiffScheme_Click(object sender, RoutedEventArgs e)
        //{
        //    MessageBoxResult res;
        //    res = MessageBox.Show("Are you sure you want to delete the selected cell's differentiation scheme?", "Warning", MessageBoxButton.YesNo);
        //    if (res == MessageBoxResult.No)
        //        return;

        //    ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;
        //    if (cell == null)
        //        return;

        //    cell.diff_scheme = null;

        //    //Clear the grids
        //    EpigeneticMapGrid.ItemsSource = null;
        //    EpigeneticMapGrid.Columns.Clear();
        //    DiffRegGrid.ItemsSource = null;
        //    DiffRegGrid.Columns.Clear();

        //    //Still want 'Add Genes' combo box
        //    DataGridTextColumn combo_col = CreateUnusedGenesColumn(MainWindow.SOP.Protocol.entity_repository);
        //    EpigeneticMapGrid.Columns.Add(combo_col);
        //    EpigeneticMapGrid.ItemContainerGenerator.StatusChanged += new EventHandler(EpigeneticItemContainerGenerator_StatusChanged);
        //}

        ////private void btnNewDeathDriver_Click(object sender, RoutedEventArgs e)
        ////{
        ////    ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
        ////    if (cell == null)
        ////        return;

        ////    if (cell.death_driver == null)
        ////    {
        ////        ////EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
        ////        ////ConfigTransitionDriver driver = FindFirstDeathDriver();

        ////        ////if (driver == null)
        ////        ////{
        ////        ////    MessageBox.Show("No death drivers are defined");
        ////        ////    return;
        ////        ////}

        ////        ////if (er.transition_drivers_dict.ContainsKey(driver.entity_guid) == true)
        ////        ////{
        ////        ////    cell.death_driver = er.transition_drivers_dict[driver.entity_guid].Clone(false);
        ////        ////}

        ////        //I think this is what we want
        ////        ConfigTransitionDriver config_td = new ConfigTransitionDriver();
        ////        config_td.Name = "generic apoptosis";
        ////        string[] stateName = new string[] { "alive", "dead" };
        ////        string[,] signal = new string[,] { { "", "" }, { "", "" } };
        ////        double[,] alpha = new double[,] { { 0, 0 }, { 0, 0 } };
        ////        double[,] beta = new double[,] { { 0, 0 }, { 0, 0 } };
        ////        ProtocolCreators.LoadConfigTransitionDriverElements(config_td, signal, alpha, beta, stateName, MainWindow.SOP.Protocol);
        ////        config_td.CurrentState = 0;
        ////        config_td.StateName = config_td.states[config_td.CurrentState];
        ////        cell.death_driver = config_td;
        ////    }
        ////}

        ////private void btnDelDeathDriver_Click(object sender, RoutedEventArgs e)
        ////{
        ////    ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
        ////    if (cell == null)
        ////        return;

        ////    //confirm deletion of driver
        ////    MessageBoxResult res;
        ////    res = MessageBox.Show("Are you sure you want to delete the selected cell's death driver?", "Warning", MessageBoxButton.YesNo);
        ////    if (res == MessageBoxResult.No)
        ////        return;

        ////    //delete driver
        ////    cell.death_driver = null;

        ////}

        ////private void btnNewDivDriver_Click(object sender, RoutedEventArgs e)
        ////{
        ////    ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
        ////    if (cell == null)
        ////        return;


        ////    if (cell.div_driver == null)
        ////    {
        ////        ////EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
        ////        ////ConfigTransitionDriver driver = FindFirstDivDriver();

        ////        ////if (driver == null)
        ////        ////{
        ////        ////    MessageBox.Show("No division drivers are defined");
        ////        ////    return;
        ////        ////}

        ////        ////if (er.transition_drivers_dict.ContainsKey(driver.entity_guid) == true)
        ////        ////{
        ////        ////    cell.div_driver = er.transition_drivers_dict[driver.entity_guid].Clone(false);
        ////        ////}

        ////        //I think this is what we want
        ////        ConfigTransitionDriver config_td = new ConfigTransitionDriver();
        ////        config_td.Name = "generic division";
        ////        string[] stateName = new string[] { "quiescent", "mitotic" };
        ////        string[,] signal = new string[,] { { "", "" }, { "", "" } };
        ////        double[,] alpha = new double[,] { { 0, 0 }, { 0, 0 } };
        ////        double[,] beta = new double[,] { { 0, 0 }, { 0, 0 } };
        ////        ProtocolCreators.LoadConfigTransitionDriverElements(config_td, signal, alpha, beta, stateName, MainWindow.SOP.Protocol);
        ////        config_td.CurrentState = 0;
        ////        config_td.StateName = config_td.states[config_td.CurrentState];
        ////        cell.div_driver = config_td;
        ////    }
        ////}

        ////private void btnDelDivDriver_Click(object sender, RoutedEventArgs e)
        ////{
        ////    ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
        ////    if (cell == null)
        ////        return;

        ////    //confirm deletion of driver
        ////    MessageBoxResult res;
        ////    res = MessageBox.Show("Are you sure you want to delete the selected cell's division driver?", "Warning", MessageBoxButton.YesNo);
        ////    if (res == MessageBoxResult.No)
        ////        return;

        ////    //delete driver
        ////    cell.div_driver = null;
        ////}

        //private void GeneTextBox_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    ConfigGene gene = dgLibGenes.SelectedItem as ConfigGene;

        //    if (gene == null)
        //        return;

        //    int index = dgLibGenes.SelectedIndex;

        //    gene.ValidateName(MainWindow.SOP.Protocol);

        //    dgLibGenes.InvalidateVisual();

        //    dgLibGenes.Items.Refresh();
        //    dgLibGenes.SelectedIndex = index;
        //    gene = (ConfigGene)dgLibGenes.SelectedItem;
        //    dgLibGenes.ScrollIntoView(gene);

        //}

        ////private void CellTextBox_LostFocus(object sender, RoutedEventArgs e)
        ////{
        ////    ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;

        ////    if (cell == null)
        ////        return;

        ////    cell.ValidateName(MainWindow.SOP.Protocol);
        ////}

        private void molPopColorEditBox_ValueChanged(object sender, RoutedEventArgs e)
        {
            ConfigMolecularPopulation mol_pop = (ConfigMolecularPopulation)lbEcsMolPops.SelectedItem;

            if (mol_pop == null || mol_pop.mp_distribution == null)
                return;

            if (mol_pop.mp_distribution.mp_distribution_type != MolPopDistributionType.Gaussian)
                return;

            MolPopGaussian mpg = mol_pop.mp_distribution as MolPopGaussian;

            string gauss_guid = mpg.gaussgrad_gauss_spec_guid_ref;

            if (MainWindow.SOP.Protocol.scenario.gauss_guid_gauss_dict.ContainsKey(gauss_guid))
            {
                GaussianSpecification gs = MainWindow.SOP.Protocol.scenario.gauss_guid_gauss_dict[gauss_guid];
                gs.gaussian_spec_color = mol_pop.mp_color;
            }
        }

        private void cellPopColor_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CellPopulation cellPop = (CellPopulation)CellPopsListBox.SelectedItem;
            if (cellPop == null)
                return;

            CellPopDistribution current_dist = cellPop.cellPopDist;

            if (current_dist.DistType != CellPopDistributionType.Gaussian)
                return;

            string gauss_guid = ((CellPopGaussian)(cellPop.cellPopDist)).gauss_spec_guid_ref;

            if (MainWindow.SOP.Protocol.scenario.gauss_guid_gauss_dict.ContainsKey(gauss_guid))
            {
                GaussianSpecification gg = MainWindow.SOP.Protocol.scenario.gauss_guid_gauss_dict[gauss_guid];
                //gg.gaussian_spec_color = cellPop.cellpopulation_color;
                gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.2f, cellPop.cellpopulation_color.R, cellPop.cellpopulation_color.G, cellPop.cellpopulation_color.B);
            }

        }

        //LIBRARIES REACTION COMPLEXES HANDLERS

        //private void btnCopyReactionComplex_Click(object sender, RoutedEventArgs e)
        //{
        //    if (lbComplexes.SelectedIndex < 0)
        //    {
        //        MessageBox.Show("Select a reaction complex to copy from.");
        //        return;
        //    }

        //    ConfigReactionComplex crcCurr = (ConfigReactionComplex)lbComplexes.SelectedItem;
        //    ConfigReactionComplex crcNew = crcCurr.Clone();

        //    MainWindow.SOP.Protocol.entity_repository.reaction_complexes.Add(crcNew);

        //    lbComplexes.SelectedIndex = lbComplexes.Items.Count - 1;
        //}

        //private void btnAddReactionComplex_Click(object sender, RoutedEventArgs e)
        //{
        //    AddReacComplex arc = new AddReacComplex(ReactionComplexDialogType.AddComplex);
        //    if (arc.ShowDialog() == true)
        //        lbComplexes.SelectedIndex = lbComplexes.Items.Count - 1;
        //}

        //private void btnEditReactionComplex_Click(object sender, RoutedEventArgs e)
        //{
        //    ConfigReactionComplex crc = (ConfigReactionComplex)lbComplexes.SelectedItem;
        //    if (crc == null)
        //        return;

        //    AddReacComplex arc = new AddReacComplex(ReactionComplexDialogType.EditComplex, crc);
        //    arc.ShowDialog();

        //}

        //private void btnRemoveReactionComplex_Click(object sender, RoutedEventArgs e)
        //{
        //    ConfigReactionComplex crc = (ConfigReactionComplex)(lbComplexes.SelectedItem);
        //    if (crc != null)
        //    {
        //        MessageBoxResult res;
        //        res = MessageBox.Show("Are you sure you would like to remove this reaction complex?", "Warning", MessageBoxButton.YesNo);
        //        if (res == MessageBoxResult.No)
        //            return;

        //        int index = lbComplexes.SelectedIndex;
        //        MainWindow.SOP.Protocol.entity_repository.reaction_complexes.Remove(crc);

        //        lbComplexes.SelectedIndex = index;

        //        if (index >= lbComplexes.Items.Count)
        //            lbComplexes.SelectedIndex = lbComplexes.Items.Count - 1;

        //        if (lbComplexes.Items.Count == 0)
        //            lbComplexes.SelectedIndex = -1;

        //    }

        //    //btnGraphReactionComplex.IsChecked = true;
        //}




        private void ecm_molecule_combo_box_GotFocus(object sender, RoutedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
        }

        private void PushEcmMoleculeButton_Click(object sender, RoutedEventArgs e)
        {
            //Error case
            ConfigMolecularPopulation molpop = (ConfigMolecularPopulation)lbEcsMolPops.SelectedItem;
            if (molpop == null)
                return;

            ConfigMolecule mol = molpop.molecule;
            GenericPush(mol);

            ////////if (mol == null)
            ////////    return;

            //Really, this can never be a newly created molecule
            //All we want to do is to push the molecule but should
            //show a confirmation dialog that shows current and new values.

            ////////PushMolecule pm = new PushMolecule();
            ////////pm.DataContext = MainWindow.SOP;
            ////////pm.EntityLevelMolDetails.DataContext = mol;

            ////////ConfigMolecule erMol = MainWindow.SOP.Protocol.FindMolecule(mol.Name);


            ////////if (erMol != null)
            ////////{
            ////////    pm.ComponentLevelMolDetails.DataContext = erMol;
            ////////}

            //////////Here show the confirmation dialog
            ////////if (pm.ShowDialog() == false)
            ////////{
            ////////    //User clicked Cancel
            ////////    return;
            ////////}

            //////////If we get here, then the user confirmed a PUSH

            ////////Protocol B = MainWindow.SOP.Protocol;
            ////////Level.PushStatus status = B.pushStatus(mol);
            ////////if (status == Level.PushStatus.PUSH_CREATE_ITEM)
            ////////{
            ////////    B.repositoryPush(mol, status); // push into B, inserts as new
            ////////}
            ////////else // the item exists; could be newer or older
            ////////{
            ////////    B.repositoryPush(mol, status); // push into B
            ////////}

        }

        private void EcsPushCellButton_Click(object sender, RoutedEventArgs e)
        {
            CellPopulation cellpop = (CellPopulation)CellPopsListBox.SelectedItem;
            if (cellpop == null)
                return;

            ConfigCell cell = cellpop.Cell;
            GenericPush(cell);
        }

        private void PushEcmReacButton_Click(object sender, RoutedEventArgs e)
        {
            if (lvEcsReactions.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a reaction.");
                return;
            }

            ConfigReaction reac = (ConfigReaction)lvEcsReactions.SelectedValue;
            GenericPush(reac);

            ////////PushReaction pr = new PushReaction();
            ////////pr.EntityLevelReactionDetails.DataContext = reac;

            ////////if (!MainWindow.SOP.Protocol.entity_repository.reactions_dict.ContainsKey(reac.entity_guid))
            ////////    return;

            ////////GenericPush(reac, MainWindow.SOP.Protocol.entity_repository.reactions_dict[reac.entity_guid]);


            ////////pr.ComponentLevelReactionDetails.DataContext = MainWindow.SOP.Protocol.entity_repository.reactions_dict[reac.entity_guid];

            ////////if (pr.ShowDialog() == false)
            ////////{
            ////////    return;
            ////////}

            //////////If we get here, then the user confirmed a PUSH

            ////////Protocol B = MainWindow.SOP.Protocol;
            ////////Level.PushStatus status = B.pushStatus(reac);
            ////////if (status == Level.PushStatus.PUSH_CREATE_ITEM)
            ////////{
            ////////    B.repositoryPush(reac, status); // push into B, inserts as new
            ////////}
            ////////else // the item exists; could be newer or older
            ////////{
            ////////    B.repositoryPush(reac, status); // push into B
            ////////}
        }

        private void GenericPush(ConfigEntity source)
        {
            if (source == null)
            {
                MessageBox.Show("Nothing to push");
                return;
            }

            if (source is ConfigMolecule)
            {
                //LET'S TRY A GENERIC PUSHER
                PushEntity pm = new PushEntity();
                pm.DataContext = MainWindow.SOP;
                pm.EntityLevelDetails.DataContext = source;

                ConfigMolecule erMol = MainWindow.SOP.Protocol.FindMolecule(((ConfigMolecule)source).Name);
                if (erMol != null)
                    pm.ComponentLevelDetails.DataContext = erMol;

                //Show the confirmation dialog
                if (pm.ShowDialog() == false)
                    return;

            }
            else if (source is ConfigReaction)
            {
                //Use generic pusher
                PushEntity pr = new PushEntity();
                pr.EntityLevelDetails.DataContext = source;

                if (MainWindow.SOP.Protocol.entity_repository.reactions_dict.ContainsKey(source.entity_guid))
                    pr.ComponentLevelDetails.DataContext = MainWindow.SOP.Protocol.entity_repository.reactions_dict[source.entity_guid];

                if (pr.ShowDialog() == false)
                    return;

            }
            else if (source is ConfigCell)
            {
                //Use generic pusher - not yet done for cells


                //This works
                PushCell pc = new PushCell();
                pc.DataContext = MainWindow.SOP;
                pc.EntityLevelCellDetails.DataContext = source;

                if (MainWindow.SOP.Protocol.entity_repository.cells_dict.ContainsKey(source.entity_guid))
                    pc.ComponentLevelCellDetails.DataContext = MainWindow.SOP.Protocol.entity_repository.cells_dict[source.entity_guid];

                //Show the confirmation dialog
                if (pc.ShowDialog() == false)
                    return;
            }
            else
            {
                MessageBox.Show("Entity type 'save' operation not supported.");
                return;
            }


            //If we get here, then the user confirmed a PUSH

            //Push the entity
            Protocol B = MainWindow.SOP.Protocol;
            Level.PushStatus status = B.pushStatus(source);
            if (status == Level.PushStatus.PUSH_INVALID)
            {
                MessageBox.Show("Entity not pushable.");
                return;
            }

            if (status == Level.PushStatus.PUSH_CREATE_ITEM)
            {
                B.repositoryPush(source, status); // push into B, inserts as new
            }
            else // the item exists; could be newer or older
            {
                B.repositoryPush(source, status); // push into B
            }
        }

    }

    public class DatabindingDebugConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            //Debugger.Break();
            //double newvalue = (double)value - 1.0;
            if (value is ConfigCell)
            {
                return ((ConfigCell)value).DragCoefficient;
            }
            //return newvalue;
            return value;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            Debugger.Break();
            return value;
        }
    }


    public class diffSchemeValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            ConfigDiffScheme val = value as ConfigDiffScheme;
            if (val != null && val.Name == "") return null;
            return value;
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

        #region HighlightColumn

        public static bool GetHighlightColumn(DependencyObject obj)
        {
            return (bool)obj.GetValue(HighlightColumnProperty);
        }

        public static void SetHighlightColumn(DependencyObject obj, bool value)
        {
            bool oldvalue = GetHighlightColumn(obj);

            obj.SetValue(HighlightColumnProperty, !oldvalue);
        }

        // Using a DependencyProperty as the backing store for HighlightColumn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HighlightColumnProperty =
            DependencyProperty.RegisterAttached("HighlightColumn", typeof(bool),
            typeof(DataGridBehavior), new FrameworkPropertyMetadata(false, OnHighlightColumnPropertyChanged));

        public static bool GetIsCellHighlighted(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsCellHighlightedProperty);
        }

        public static void SetIsCellHighlighted(DependencyObject obj, bool value)
        {
            obj.SetValue(IsCellHighlightedProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsCellHighlighted.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCellHighlightedProperty =
            DependencyProperty.RegisterAttached("IsCellHighlighted", typeof(bool), typeof(DataGridBehavior),
            new UIPropertyMetadata(false));

        private static void OnHighlightColumnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Console.WriteLine(e.NewValue);
            DataGridCell cell = sender as DataGridCell;

            if (cell != null)
            {
                DataGrid dg = GetDataGridFromCell(cell);
                DataGridColumn column = cell.Column;

                for (int i = 0; i < dg.Items.Count; i++)
                {
                    DataGridRow row = dg.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                    DataGridCell currentCell = GetCell(row, column);
                    if (currentCell != null)
                    {
                        currentCell.SetValue(DataGridBehavior.IsCellHighlightedProperty, e.NewValue);
                    }
                }
            }
            else
            {
                DataGridColumn col = sender as DataGridColumn;
                if (col == null)
                    return;

                DataGrid dg = GetDataGridFromColumn(col);
                for (int i = 0; i < dg.Items.Count; i++)
                {
                    DataGridRow row = dg.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                    DataGridCell currentCell = GetCell(row, col);
                    if (currentCell != null)
                    {
                        currentCell.SetValue(DataGridBehavior.IsCellHighlightedProperty, e.NewValue);
                    }
                }
            }
        }

        private static DataGrid GetDataGridFromCell(DataGridCell cell)
        {
            DataGrid retVal = null;
            FrameworkElement fe = cell;
            while ((retVal == null) && (fe != null))
            {
                if (fe is DataGrid)
                    retVal = fe as DataGrid;
                else
                    fe = VisualTreeHelper.GetParent(fe) as FrameworkElement;
            }
            return retVal;
        }

        private static DataGrid GetDataGridFromColumn(DataGridColumn col)
        {
            DataGrid retVal = null;

            retVal = col.GetType().GetProperty("DataGridOwner", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(col, null) as DataGrid;

            return retVal;
        }

        private static DataGridCell GetCell(DataGridRow row, DataGridColumn column)
        {
            DataGridCell retVal = null;
            DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(row);
            if (presenter != null)
            {
                for (int i = 0; i < presenter.Items.Count; i++)
                {
                    DataGridCell cell = presenter.ItemContainerGenerator.ContainerFromIndex(i) as DataGridCell;
                    if ((cell != null) && (cell.Column == column))
                    {
                        retVal = cell;
                        break;
                    }
                }
            }

            return retVal;
        }

        #endregion


        #region Get Visuals

        private static T GetVisualChild<T>(Visual parent) where T : Visual
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

    public class FilterFactory
    {
        private object Context { get; set; }

        //public static EventHandler CreateShowHandlerFor(object context)
        //    {
        //        CommonFilter handler = new CommonEventHandler();

        //        handler.Context = context;

        //        return new EventHandler(handler.HandleGenericShow);
        //    }

        public static void bulkMoleculesListView_Filter(object sender, FilterEventArgs e)
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
    }
}


    ///SAMPLE CODE TO INJECT OBJECT INTO A COMMON EVENT HANDLER
    //public class CommonEventHandler
    //{
    //    private CommonEventHandler() { }

    //    private object Context { get; set; }

    //    public static EventHandler CreateShowHandlerFor(object context)
    //    {
    //        CommonEventHandler handler = new CommonEventHandler();

    //        handler.Context = context;

    //        return new EventHandler(handler.HandleGenericShow);
    //    }

    //    private void HandleGenericShow(object sender, EventArgs e)
    //    {
    //        Console.WriteLine(this.Context);
    //    }
    //}

    //class Program
    //{
    //    static void Main(string[] args)
    //    {
    //        EventHandler show5 = CommonEventHandler.CreateShowHandlerFor(5);
    //        EventHandler show7 = CommonEventHandler.CreateShowHandlerFor(7);

    //        show5(null, EventArgs.Empty);
    //        Console.WriteLine("===");
    //        show7(null, EventArgs.Empty);
    //    }
    //}



