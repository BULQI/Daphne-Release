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
            CellPopsDetailsExpander.IsExpanded = true;

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

        //This method is called when the user clicks the Remove Cell button
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

        // Used to specify Gaussian distibution for cell positions
        private void AddGaussianSpecification(GaussianSpecification gg, BoxSpecification box)
        {
            gg.gaussian_spec_box_guid_ref = box.box_guid;
            gg.gaussian_spec_name = "";
            gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
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
                        MolPopLinear molpoplin = new MolPopLinear();
                        // X face is default
                        molpoplin.boundaryCondition.Add(new BoundaryCondition(MolBoundaryType.Dirichlet, Boundary.left, 0.0));
                        molpoplin.boundaryCondition.Add(new BoundaryCondition(MolBoundaryType.Dirichlet, Boundary.right, 0.0));
                        molpoplin.Initalize(BoundaryFace.X);
                        molpoplin.boundary_face = BoundaryFace.X;
                        current_item.mp_dist_name = "Linear";
                        current_item.mp_distribution = molpoplin;
                        current_item.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.89f, 0.11f, 0.11f);
                        current_item.mp_render_blending_weight = 2.0;
                        break;

                    case MolPopDistributionType.Gaussian:
                        MolPopGaussian mpg = new MolPopGaussian();

                        AddGaussianSpecification(mpg);
                        current_item.mp_distribution = mpg;

                        break;
                    
                    case MolPopDistributionType.Explicit:
                        break;

                    default:
                        throw new ArgumentException("MolPopInfo distribution type out of range");
                }
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

#if allow_dist_from_file
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
#endif

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

            gmp.molecule_guid_ref = MainWindow.SC.SimConfig.entity_repository.molecules[0].entity_guid;
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
        
        private void selectedCellTransitionDeathDriverListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            ConfigTransitionDriver driver = e.Item as ConfigTransitionDriver;
            if (driver != null)
            {
                // Filter out driver if its guid does not match selected cell's driver guid
                if (cell != null && driver.driver_guid == cell.death_driver_guid_ref)
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }

        private void selectedCellTransitionDivisionDriverListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            ConfigTransitionDriver driver = e.Item as ConfigTransitionDriver;
            if (driver != null)
            {
                // Filter out driver if its guid does not match selected cell's driver guid
                if (cell != null && driver.driver_guid == cell.div_driver_guid_ref)
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }

        private void unusedGenesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;

            e.Accepted = false;

            if (cell == null)
                return;

            //if (cell.diff_scheme_guid_ref == "")
            //    return;

            ConfigDiffScheme ds = cell.diff_scheme;
            ConfigGene gene = e.Item as ConfigGene;

            //if gene is not in the cell's nucleus, then exclude it from the available gene pool
            if (!cell.genes_guid_ref.Contains(gene.entity_guid))
                return;


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
            bool needRefresh = false;

            foreach (var item in lvAvailableReacs.SelectedItems)
            {
                ConfigReaction reac = (ConfigReaction)item;

                if (!MainWindow.SC.SimConfig.scenario.environment.ecs.reactions_guid_ref.Contains(reac.entity_guid))
                {
                    MainWindow.SC.SimConfig.scenario.environment.ecs.reactions_guid_ref.Add(reac.entity_guid);
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

            string guid = (string)lvEcsReactions.SelectedValue;
            ConfigReaction grt = MainWindow.SC.SimConfig.entity_repository.reactions_dict[guid];
            if (MainWindow.SC.SimConfig.scenario.environment.ecs.reactions_guid_ref.Contains(grt.entity_guid))
            {
                MainWindow.SC.SimConfig.scenario.environment.ecs.reactions_guid_ref.Remove(grt.entity_guid);
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

            //Finally, if the ecm already contains this reaction, exclude it from the available reactions list
            if (MainWindow.SC.SimConfig.scenario.environment.ecs.reactions_guid_ref.Contains(cr.entity_guid))
                bOK = false;

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

            //Loop through modifiers list
            if (bOK)
                bOK = cc.membrane.HasMolecules(cr.modifiers_molecule_guid_ref);

            //Finally, if the cell membrane already contains this reaction, exclude it from the available reactions list
            if (cc.membrane.reactions_guid_ref.Contains(cr.entity_guid))
                bOK = false;

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
            ObservableCollection<string> gene_guids = new ObservableCollection<string>();
            ObservableCollection<string> bulk = new ObservableCollection<string>();
            EntityRepository er = MainWindow.SC.SimConfig.entity_repository;

            foreach (string molguid in cr.reactants_molecule_guid_ref)
                if (er.molecules_dict.ContainsKey(molguid) && er.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                    membBound.Add(molguid);
                else if (er.genes_dict.ContainsKey(molguid))
                    gene_guids.Add(molguid);
                else
                    bulk.Add(molguid);

            foreach (string molguid in cr.products_molecule_guid_ref)
                if (er.molecules_dict.ContainsKey(molguid) && er.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                    membBound.Add(molguid);
                else if (er.genes_dict.ContainsKey(molguid))
                    gene_guids.Add(molguid);
                else
                    bulk.Add(molguid);

            foreach (string molguid in cr.modifiers_molecule_guid_ref)
                if (er.molecules_dict.ContainsKey(molguid) && er.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                    membBound.Add(molguid);
                else if (er.genes_dict.ContainsKey(molguid))
                    gene_guids.Add(molguid);
                else
                    bulk.Add(molguid);

            bool bOK = true;
            bool bTranscription = false;

            if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Transcription)
            {
                bTranscription = bulk.Count > 0 && gene_guids.Count > 0 && cc.HasGenes(gene_guids) && cc.cytosol.HasMolecules(bulk);
                if (bTranscription == true)
                {
                    bOK = true;
                }
                else
                {
                    bOK = false;
                }
            }
            else
            {
                if (bulk.Count <= 0)
                    bOK = false;

                if (bOK && membBound.Count > 0)
                    bOK = cc.membrane.HasMolecules(membBound);

                if (bOK)
                    bOK = cc.cytosol.HasMolecules(bulk);

            }

            //Finally, if the cell cytosol already contains this reaction, exclude it from the available reactions list
            if (cc.cytosol.reactions_guid_ref.Contains(cr.entity_guid))
                bOK = false;

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
            ConfigReactionComplex crcNew = crcCurr.Clone();

            MainWindow.SC.SimConfig.entity_repository.reaction_complexes.Add(crcNew);

            lbComplexes.SelectedIndex = lbComplexes.Items.Count - 1;            
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

            //btnGraphReactionComplex.IsChecked = true;
        }

        private void cbCellPopDistributionType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void lbCellPopDistSubType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void CellAddReacCxButton_Click(object sender, RoutedEventArgs e)
        {

#if allow_rc_in_cell
            if (lbCellAvailableReacCx.SelectedIndex != -1)
            {
                ConfigReactionComplex grc = (ConfigReactionComplex)lbCellAvailableReacCx.SelectedValue;
                if (!MainWindow.SC.SimConfig.scenario.ReactionComplexes.Contains(grc))
                    MainWindow.SC.SimConfig.scenario.ReactionComplexes.Add(grc);
            } 
#endif
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
                MainWindow.SC.SimConfig.scenario.environment.ecs.RemoveMolecularPopulation(gm.entity_guid);
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

            ReacComplexExpander.IsExpanded = true;
        }

        //CELLS EVENT HANDLERS
        private void MembraneAddReacButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cc = (ConfigCell)CellsListBox.SelectedItem;
            bool needRefresh = false;

            foreach (var item in lvCellAvailableReacs.SelectedItems)
            {
                ConfigReaction cr = (ConfigReaction)item;
                if (cc != null && cr != null)
                {
                    if (!cc.membrane.reactions_guid_ref.Contains(cr.entity_guid))
                    {
                        cc.membrane.reactions_guid_ref.Add(cr.entity_guid);

                        needRefresh = true;
                    }
                }
            }

            //Refresh filter
            if (needRefresh && lvCellAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCellAvailableReacs.ItemsSource).Refresh();

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
            bool needRefresh = false;

            foreach (var item in lvCytosolAvailableReacs.SelectedItems)
            {
                ConfigReaction cr = (ConfigReaction)item;
                if (cc != null && cr != null)
                {
                    //Add to reactions list only if the cell does not already contain this reaction
                    if (!cc.cytosol.reaction_complexes_guid_ref.Contains(cr.entity_guid))
                    {
                        cc.cytosol.reactions_guid_ref.Add(cr.entity_guid);

                        needRefresh = true;
                    }
                }
            }

            //Refresh the filter
            if (needRefresh && lvCytosolAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();
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
            if (cell == null)
                return; 

            cell.membrane.molpops.Add(gmp);
            CellMembraneMolPopsListBox.SelectedIndex = CellMembraneMolPopsListBox.Items.Count - 1;
        }

        private void MembraneRemoveMolButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecularPopulation cmp = (ConfigMolecularPopulation)CellMembraneMolPopsListBox.SelectedItem;

            if (cmp == null)
                return;

            MessageBoxResult res = MessageBox.Show("Removing this molecular population will remove cell reactions that use this molecule. Are you sure you would like to proceed?", "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;

            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;

            foreach (string reacguid in cell.membrane.reactions_guid_ref.ToList())
            {
                ConfigReaction reac = MainWindow.SC.SimConfig.entity_repository.reactions_dict[reacguid];
                if (reac.HasMolecule(cmp.molecule_guid_ref))
                {
                    cell.membrane.reactions_guid_ref.Remove(reacguid);
                }
            }
            //added 1/10/14
            foreach (string reacguid in cell.cytosol.reactions_guid_ref.ToList())
            {
                ConfigReaction reac = MainWindow.SC.SimConfig.entity_repository.reactions_dict[reacguid];
                if (reac.HasMolecule(cmp.molecule_guid_ref))
                {
                    cell.cytosol.reactions_guid_ref.Remove(reacguid);
                }
            }            

            cell.membrane.molpops.Remove(cmp);
            CellMembraneMolPopsListBox.SelectedIndex = CellMembraneMolPopsListBox.Items.Count - 1;

            if (lvCellAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCellAvailableReacs.ItemsSource).Refresh();
        }

        private void CytosolAddMolButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecularPopulation cmp = new ConfigMolecularPopulation(ReportType.CELL_MP);

            
            cmp.Name = "NewMP";
            cmp.mpInfo = new MolPopInfo("");
            cmp.mpInfo.mp_dist_name = "New distribution";
            cmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 1.0f, 0.2f);
            cmp.mpInfo.mp_render_on = true;
            cmp.mpInfo.mp_distribution = new MolPopHomogeneousLevel();

            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            if (cell == null)
                return; 

            cell.cytosol.molpops.Add(cmp);
            CellCytosolMolPopsListBox.SelectedIndex = CellCytosolMolPopsListBox.Items.Count - 1;

            //cytosolMolPopDetailsTemplate
            //cyto_molecule_combo_box.SelectedIndex = 0;
        }

        private void CytosolRemoveMolButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecularPopulation cmp = (ConfigMolecularPopulation)CellCytosolMolPopsListBox.SelectedItem;

            if (cmp == null)
                return;

            MessageBoxResult res = MessageBox.Show("Removing this molecular population will remove cell reactions that use this molecule. Are you sure you would like to proceed?", "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;
            
            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;

            foreach (string reacguid in cell.cytosol.reactions_guid_ref.ToList())
            {
                ConfigReaction reac = MainWindow.SC.SimConfig.entity_repository.reactions_dict[reacguid];
                if (reac.HasMolecule(cmp.molecule_guid_ref))
                {
                    cell.cytosol.reactions_guid_ref.Remove(reacguid);
                }
            }

            //added 1/10/14
            foreach (string reacguid in cell.membrane.reactions_guid_ref.ToList())
            {
                ConfigReaction reac = MainWindow.SC.SimConfig.entity_repository.reactions_dict[reacguid];
                if (reac.HasMolecule(cmp.molecule_guid_ref))
                {
                    cell.membrane.reactions_guid_ref.Remove(reacguid);
                }
            }

            cell.cytosol.molpops.Remove(cmp);
            CellCytosolMolPopsListBox.SelectedIndex = CellCytosolMolPopsListBox.Items.Count - 1;

            if (lvCytosolAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();            
        }

        private void CellsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvCellAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCellAvailableReacs.ItemsSource).Refresh();
            if (lvCytosolAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();

            DiffSchemeExpander_Expanded(null, null);

        }

        private void CellMembraneMolPopsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void CellCytosolMolPopsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
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
                if (MainWindow.SC.SimConfig.scenario.environment.ecs.reactions_guid_ref.Contains(cr.entity_guid))
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
                if (cc.membrane.reactions_guid_ref.Contains(cr.entity_guid))
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
                if (crc.reactions_guid_ref.Contains(cr.entity_guid))
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
                if (cc.cytosol.reactions_guid_ref.Contains(cr.entity_guid))
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

                ////Add this after 2/4/14
                ////gg.DrawAsWireframe = true;

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

            // Only want to respond to purposeful user interaction, not just population and depopulation
            // of solfacs list
            if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
                return;

            ComboBox cb = (ComboBox)e.Source;
            if (cb == null)
                return;

            //ConfigCell cc = (ConfigCell)cb.SelectedItem;
            //string cellname = MainWindow.SC.SimConfig.entity_repository.cells_dict[cc.cell_guid].CellName;
            CellPopulation cp = (CellPopulation)CellPopsListBox.SelectedItem;
            if (cp == null)
                return;

            string curr_cell_pop_name = cp.cellpopulation_name;
            string curr_cell_type_guid = "";
            curr_cell_type_guid = cp.cell_guid_ref;

            int nIndex = cb.SelectedIndex;
            if (nIndex < 0)
                return;

            cp.cell_guid_ref = MainWindow.SC.SimConfig.entity_repository.cells[nIndex].cell_guid;

            string new_cell_name = MainWindow.SC.SimConfig.entity_repository.cells[nIndex].CellName;
            if (curr_cell_type_guid != cp.cell_guid_ref) // && curr_cell_pop_name.Length == 0)
                cp.cellpopulation_name = new_cell_name;

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

        /// <summary>
        /// Called when a cell in the data grid is selected and after each key stroke.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
            for (int i = cp.cellPopDist.CellStates.Count - 1; i >= 0; i--)
            {
                double[] pos = new double[3] { cp.cellPopDist.CellStates[i].X, cp.cellPopDist.CellStates[i].Y, cp.cellPopDist.CellStates[i].Z };
                // X
                if (cp.cellPopDist.CellStates[i].X < 0) 
                {
                    // cp.cellPopDist.CellStates[i].X = 0;
                    pos[0] = 0;
                    changed = true;
                }
                if (cp.cellPopDist.CellStates[i].X > cp.cellPopDist.Extents[0]) 
                {
                    // cp.cellPopDist.CellStates[i].X = cp.cellPopDist.Extents[0];
                    pos[0] = cp.cellPopDist.Extents[0];
                    changed = true;
                }
                // Y
                if (cp.cellPopDist.CellStates[i].Y < 0) 
                {
                    //cp.cellPopDist.CellStates[i].Y = 0;
                    pos[1] = 0;
                    changed = true;
                }
                if (cp.cellPopDist.CellStates[i].Y > cp.cellPopDist.Extents[1])
                {
                    //cp.cellPopDist.CellStates[i].Y = cp.cellPopDist.Extents[1];
                    pos[1] = cp.cellPopDist.Extents[1];
                    changed = true;
                }
                // Z
                if (cp.cellPopDist.CellStates[i].Z < 0)
                {
                    //cp.cellPopDist.CellStates[i].Z = 0;
                    pos[2] = 0;
                    changed = true;
                }
                if (cp.cellPopDist.CellStates[i].Z > cp.cellPopDist.Extents[2])
                {
                    //cp.cellPopDist.CellStates[i].Z = cp.cellPopDist.Extents[2];
                    pos[2] = cp.cellPopDist.Extents[2];
                    changed = true;
                }
                if (changed)
                {
                    // Can't update coordinates directly or the datagrid doesn't update properly
                    // (e.g., cp.cellPopDist.CellStates[i].Z = cp.cellPopDist.Extents[2];)
                    cp.cellPopDist.CellStates.RemoveAt(i);
                    cp.cellPopDist.AddByPosition(pos);
                }
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
                if (cmp.mpInfo.mp_distribution.mp_distribution_type == MolPopDistributionType.Linear)
                {
                    ((MolPopLinear) cmp.mpInfo.mp_distribution).Initalize((BoundaryFace) cb.SelectedItem);
                }
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

        private void molecule_combo_box2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)e.Source;
            if (cb == null)
                return;

            ConfigMolecularPopulation molpop = (ConfigMolecularPopulation)lbEcsMolPops.SelectedItem;

            if (molpop == null)
                return;

            string curr_mol_pop_name = molpop.Name;
            string curr_mol_guid = "";
            curr_mol_guid = molpop.molecule_guid_ref;

            int nIndex = cb.SelectedIndex;
            if (nIndex < 0)
                return;

            ConfigMolecule mol = (ConfigMolecule)cb.SelectedItem;
            molpop.molecule_guid_ref = mol.entity_guid;

            string new_mol_name = mol.Name;
            if (curr_mol_guid != molpop.molecule_guid_ref)
                molpop.Name = new_mol_name;

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

        
        private void DrawSelectedReactionComplex()
        {
            ConfigReactionComplex crc = (ConfigReactionComplex)(lbComplexes.SelectedItem);

            //Cleanup any previous RC stuff
            foreach (ConfigCell cell in MainWindow.SC.SimConfig.entity_repository.cells.ToList())
            {
                if (cell.CellName == "RCCell")
                {
                    MainWindow.SC.SimConfig.entity_repository.cells.Remove(cell);
                }
            }
            //crc.RCSim.reset();
            MainWindow.SC.SimConfig.rc_scenario.cellpopulations.Clear();
            // end of cleanup

            ConfigCell cc = new ConfigCell();
            cc.CellName = "RCCell";
            foreach (ConfigMolecularPopulation cmp in crc.molpops)
            {
                cc.cytosol.molpops.Add(cmp);
            }
            foreach (ConfigGene configGene in crc.genes)
            {
                cc.genes_guid_ref.Add(configGene.entity_guid);
            }
            foreach (string rguid in crc.reactions_guid_ref)
            {
                cc.cytosol.reactions_guid_ref.Add(rguid);
            }
            MainWindow.SC.SimConfig.entity_repository.cells.Add(cc);
            MainWindow.SC.SimConfig.rc_scenario.cellpopulations.Clear();

            CellPopulation cp = new CellPopulation();
            cp.cell_guid_ref = cc.cell_guid;
            cp.cellpopulation_name = "RC cell";
            cp.number = 1;

            // Add cell population distribution information
            double[] extents = new double[3] { MainWindow.SC.SimConfig.rc_scenario.environment.extent_x, 
                                               MainWindow.SC.SimConfig.rc_scenario.environment.extent_y, 
                                               MainWindow.SC.SimConfig.rc_scenario.environment.extent_z };
            double minDisSquared = 2 * MainWindow.SC.SimConfig.entity_repository.cells_dict[cp.cell_guid_ref].CellRadius;
            minDisSquared *= minDisSquared;
            cp.cellPopDist = new CellPopSpecific(extents, minDisSquared, cp);
            cp.cellPopDist.CellStates[0] = new CellState(MainWindow.SC.SimConfig.rc_scenario.environment.extent_x,
                                                            MainWindow.SC.SimConfig.rc_scenario.environment.extent_y / 2,
                                                            MainWindow.SC.SimConfig.rc_scenario.environment.extent_z / 2);

            cp.cellpopulation_constrained_to_region = false;
            cp.cellpopulation_color = System.Windows.Media.Color.FromScRgb(1.0f, 1.0f, 0.5f, 0.0f);
            MainWindow.SC.SimConfig.rc_scenario.cellpopulations.Add(cp);

            ReactionComplexProcessor Processor = new ReactionComplexProcessor();
            MainWindow.Sim.Load(MainWindow.SC.SimConfig, true, true);

            Processor.Initialize(MainWindow.SC.SimConfig, crc, MainWindow.Sim);
            Processor.Go();

            MainWindow.ST_ReacComplexChartWindow.Title = "Reaction Complex: " + crc.Name;
            MainWindow.ST_ReacComplexChartWindow.RC = Processor;  //crc.Processor;
            MainWindow.ST_ReacComplexChartWindow.DataContext = Processor; //crc.Processor;
            MainWindow.ST_ReacComplexChartWindow.Render();

            MainWindow.ST_ReacComplexChartWindow.dblMaxTime.Number = Processor.dInitialTime;  //crc.Processor.dInitialTime;
            MW.VTKDisplayDocWindow.Activate();
            MainWindow.ST_ReacComplexChartWindow.Activate();
            MainWindow.ST_ReacComplexChartWindow.toggleButton = btnGraphReactionComplex;
        }

        private void btnGraphReactionComplex_Checked(object sender, RoutedEventArgs e)
        {
            if (lbComplexes.SelectedIndex < 0)
            {
                MessageBox.Show("Select a reaction complex to process.");
                return;
            }

            if (btnGraphReactionComplex.IsChecked == true)
            {
                DrawSelectedReactionComplex();
                btnGraphReactionComplex.IsChecked = false;
            }
        }

        private void btnGraphReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            if (lbComplexes.SelectedIndex < 0)
            {
                MessageBox.Show("Select a reaction complex to process.");
                return;
            }

            DrawSelectedReactionComplex();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvCellAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCellAvailableReacs.ItemsSource).Refresh();
            if (lvCytosolAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();
        }

        private void CellAddReacExpander_Expanded(object sender, RoutedEventArgs e)
        {
            if (lvCellAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCellAvailableReacs.ItemsSource).Refresh();
        }

        private void CellAddReacExpander2_Expanded(object sender, RoutedEventArgs e)
        {
            if (lvCytosolAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();
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
            if (MainWindow.SC == null)
                return;
            if (MainWindow.SC.SimConfig == null)
                return;
            if (MainWindow.SC.SimConfig.scenario == null)
                return;

            //Need to do this to make sure that the sampling and rendering sliders get the correct "Value"s
            double temp_samp = MainWindow.SC.SimConfig.scenario.time_config.sampling_interval;
            double temp_rand = MainWindow.SC.SimConfig.scenario.time_config.rendering_interval;

            sampling_interval_slider.Maximum = time_duration_slider.Value;
            if (temp_samp > sampling_interval_slider.Maximum)
                temp_samp = sampling_interval_slider.Maximum;
            sampling_interval_slider.Value = temp_samp;
            MainWindow.SC.SimConfig.scenario.time_config.sampling_interval = sampling_interval_slider.Value;
            
            time_step_slider.Maximum = time_duration_slider.Value;
            if (temp_rand > time_step_slider.Maximum)
                temp_rand = time_step_slider.Maximum;
            time_step_slider.Value = temp_rand;
            MainWindow.SC.SimConfig.scenario.time_config.rendering_interval = time_step_slider.Value;

        }

        private void sampling_interval_slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (MainWindow.SC == null)
                return;

            if (MainWindow.SC.SimConfig == null)
                return;

            if (MainWindow.SC.SimConfig.scenario == null)
                return;

            if (MainWindow.SC.SimConfig.scenario.time_config.sampling_interval > sampling_interval_slider.Maximum)
                MainWindow.SC.SimConfig.scenario.time_config.sampling_interval = sampling_interval_slider.Value;

            if (MainWindow.SC.SimConfig.scenario.time_config.rendering_interval > time_step_slider.Maximum)
                MainWindow.SC.SimConfig.scenario.time_config.rendering_interval = time_step_slider.Value;
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
            curr_mol_guid = molpop.molecule_guid_ref;

            int nIndex = cb.SelectedIndex;
            if (nIndex < 0)
                return;

            ConfigMolecule mol = (ConfigMolecule)cb.SelectedItem;
            molpop.molecule_guid_ref = mol.entity_guid;

            string new_mol_name = mol.Name;
            if (curr_mol_guid != molpop.molecule_guid_ref)
                molpop.Name = new_mol_name;

            CollectionViewSource.GetDefaultView(lvCellAvailableReacs.ItemsSource).Refresh();
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
            curr_mol_guid = molpop.molecule_guid_ref;

            int nIndex = cb.SelectedIndex;
            if (nIndex < 0)
                return;

            ConfigMolecule mol = (ConfigMolecule)cb.SelectedItem;
            molpop.molecule_guid_ref = mol.entity_guid;

            string new_mol_name = mol.Name;
            if (curr_mol_guid != molpop.molecule_guid_ref)
                molpop.Name = new_mol_name;
            
            CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();
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

        private void btnRemoveGene_Click(object sender, RoutedEventArgs e)
        {
            ConfigGene gene = (ConfigGene)dgLibGenes.SelectedValue;
            MessageBoxResult res;

            res = MessageBox.Show("Are you sure you would like to remove this gene?", "Warning", MessageBoxButton.YesNo);

            if (res == MessageBoxResult.No)
                return;

            int index = dgLibMolecules.SelectedIndex;
            //MainWindow.SC.SimConfig.scenario.environment.ecs.RemoveMolecularPopulation(gm.molecule_guid);
            MainWindow.SC.SimConfig.entity_repository.genes.Remove(gene);
            dgLibGenes.SelectedIndex = index;

            if (index >= dgLibMolecules.Items.Count)
                dgLibGenes.SelectedIndex = dgLibGenes.Items.Count - 1;

            if (dgLibGenes.Items.Count == 0)
                dgLibGenes.SelectedIndex = -1;

        }

        private void btnCopyGene_Click(object sender, RoutedEventArgs e)
        {
            ConfigGene gene = (ConfigGene)dgLibGenes.SelectedItem;

            if (gene == null)
                return;

            ConfigGene newgene = gene.Clone(MainWindow.SC.SimConfig);
            MainWindow.SC.SimConfig.entity_repository.genes.Add(newgene);
            dgLibGenes.SelectedIndex = dgLibGenes.Items.Count - 1;

            gene = (ConfigGene)dgLibMolecules.SelectedItem;
            dgLibGenes.ScrollIntoView(newgene);
        }

        private void btnAddGene_Click(object sender, RoutedEventArgs e)
        {
            ConfigGene gm = new ConfigGene("NewGene", 0, 0);
            gm.Name = gm.GenerateNewName(MainWindow.SC.SimConfig, "_New");
            MainWindow.SC.SimConfig.entity_repository.genes.Add(gm);
            dgLibGenes.SelectedIndex = dgLibGenes.Items.Count - 1;

            ConfigGene cm = (ConfigGene)dgLibGenes.SelectedItem;
            dgLibGenes.ScrollIntoView(cm);
        }

        private void cyto_gene_combo_box_GotFocus(object sender, RoutedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            CollectionViewSource.GetDefaultView(combo.ItemsSource).Refresh();
        }

        private void cyto_gene_combo_box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)e.Source;
            if (cb == null)
                return;

            ////ConfigMolecularPopulation molpop = (ConfigMolecularPopulation)CellCytosolMolPopsListBox.SelectedItem;

            ////if (molpop == null)
            ////    return;

            ////string curr_mol_pop_name = molpop.Name;
            ////string curr_mol_guid = "";
            ////curr_mol_guid = molpop.molecule_guid_ref;

            ////int nIndex = cb.SelectedIndex;
            ////if (nIndex < 0)
            ////    return;

            ////ConfigMolecule mol = (ConfigMolecule)cb.SelectedItem;
            ////molpop.molecule_guid_ref = mol.molecule_guid;

            ////string new_mol_name = mol.Name;
            ////if (curr_mol_guid != molpop.molecule_guid_ref)
            ////    molpop.Name = new_mol_name;
            
            //CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();

        }

        private void CellNucleusGenesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void NucleusAddGeneButton_Click(object sender, RoutedEventArgs e)
        {
            //Get selected cell
            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;

            //if no cell selected, return
            if (cell == null)
                return;

            //Show a dialog that gets the new gene's name
            AddGeneToCell ads = new AddGeneToCell(cell);

            //If user clicked 'apply' and not 'cancel'
            if (ads.ShowDialog() == true)
            {
                ConfigGene geneToAdd = ads.SelectedGene;
                if (geneToAdd == null)
                    return;

                cell.genes_guid_ref.Add(geneToAdd.entity_guid);
            }
        }

        private void NucleusRemoveGeneButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            string gene_guid = (string)CellNucleusGenesListBox.SelectedItem;

            if (gene_guid == "")
                return;

            MessageBoxResult res = MessageBox.Show("Are you sure you would like to remove this gene from this cell?", "Warning", MessageBoxButton.YesNo);

            if (res == MessageBoxResult.No)
                return;

            if (cell.genes_guid_ref.Contains(gene_guid))
            {
                cell.genes_guid_ref.Remove(gene_guid);
            }
        }

        private void DiffSchemeExpander_Expanded(object sender, RoutedEventArgs e)
        {
            EntityRepository er = MainWindow.SC.SimConfig.entity_repository;
            ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;

            //Clear the grids
            EpigeneticMapGrid.ItemsSource = null;
            EpigeneticMapGrid.Columns.Clear();

            DiffRegGrid.ItemsSource = null;
            DiffRegGrid.Columns.Clear();

            if (cell == null)
            {
                return;
            }

            //if cell does not have a diff scheme, return
            if (cell.diff_scheme == null)
            {
                //Create a column that allows the user to add genes to the grid
                DataGridTextColumn combo_col = CreateUnusedGenesColumn(er);
                EpigeneticMapGrid.Columns.Add(combo_col);
                return;
            }

            //Get the diff_scheme using the guid
            ConfigDiffScheme diff_scheme = cell.diff_scheme;   //er.diff_schemes_dict[cell.diff_scheme_guid_ref];

            //EPIGENETIC MAP SECTION
            EpigeneticMapGrid.DataContext = diff_scheme;
            EpigeneticMapGrid.ItemsSource = diff_scheme.activationRows;

            int nn = 0;
            foreach (string gene_guid in diff_scheme.genes)
            {
                //SET UP COLUMNS
                ConfigGene gene = er.genes_dict[gene_guid];
                DataGridTextColumn col = new DataGridTextColumn();
                col.Header = gene.Name;                
                col.CanUserSort = false;
                Binding b = new Binding(string.Format("activations[{0}]", nn));
                b.Mode = BindingMode.TwoWay;
                b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                col.Binding = b;
                EpigeneticMapGrid.Columns.Add(col);
                nn++;
            }

            //Create a column that allows the user to add genes to the grid
            DataGridTextColumn editor_col = CreateUnusedGenesColumn(er);
            EpigeneticMapGrid.Columns.Add(editor_col);
            EpigeneticMapGrid.ItemContainerGenerator.StatusChanged += new EventHandler(EpigeneticItemContainerGenerator_StatusChanged);


            //----------------------------------
            //DIFFERENTIATION REGULATORS SECTION

            DiffRegGrid.ItemsSource = diff_scheme.Driver.DriverElements;
            //DiffRegGrid.DataContext = diff_scheme.Driver;
            DiffRegGrid.CanUserAddRows = false;
            DiffRegGrid.CanUserDeleteRows = false;

            int i = 0;
            foreach (string s in diff_scheme.Driver.states)
            {
                //SET UP COLUMN HEADINGS
                DataGridTemplateColumn col2 = new DataGridTemplateColumn();
                DiffRegGrid.Columns.Add(CreateDiffRegColumn(er, cell, s));
                i++;
            }

            DiffRegGrid.ItemContainerGenerator.StatusChanged += new EventHandler(DiffRegItemContainerGenerator_StatusChanged);
        }

        private DataGridTemplateColumn CreateDiffRegColumn(EntityRepository er, ConfigCell cell, string state)
        {
            DataGridTemplateColumn col = new DataGridTemplateColumn();

            string sbind = string.Format("SelectedItem.diff_scheme.Driver.states[{0}]", DiffRegGrid.Columns.Count);
            Binding bcol = new Binding(sbind);
            bcol.ElementName = "CellsListBox";
            bcol.Path = new PropertyPath(sbind);
            bcol.Mode = BindingMode.OneWay;

            //Create a TextBox so the state name is editable
            FrameworkElementFactory txtStateName = new FrameworkElementFactory(typeof(TextBlock));
            txtStateName.SetValue(TextBlock.StyleProperty, null);
            txtStateName.SetBinding(TextBlock.TextProperty, bcol);

            //DataGridRowHeader header = new DataGridRowHeader();
            DataTemplate colHeaderTemplate = new DataTemplate();

            colHeaderTemplate.VisualTree = txtStateName;
            col.HeaderStyle = null;
            col.HeaderTemplate = colHeaderTemplate;
            col.CanUserSort = false;
            col.MinWidth = 50;

            //SET UP CELL LAYOUT - COMBOBOX OF MOLECULES PLUS ALPHA AND BETA VALUES

            //NON-EDITING TEMPLATE - THIS IS WHAT SHOWS WHEN NOT EDITING THE GRID CELL
            DataTemplate cellTemplate = new DataTemplate();

            //SET UP A TEXTBLOCK ONLY
            Binding bn = new Binding(string.Format("elements[{0}].driver_mol_guid_ref", DiffRegGrid.Columns.Count));
            bn.Mode = BindingMode.TwoWay;
            bn.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            MolGUIDtoMolNameConverter c = new MolGUIDtoMolNameConverter();
            bn.Converter = c;
            CollectionViewSource cvs = new CollectionViewSource();
            cvs.Source = er.molecules;
            bn.ConverterParameter = cvs;
            FrameworkElementFactory txtDriverMol = new FrameworkElementFactory(typeof(TextBlock));
            txtDriverMol.Name = "DriverTextBlock";
            txtDriverMol.SetBinding(TextBlock.TextProperty, bn);
            cellTemplate.VisualTree = txtDriverMol;

            //EDITING TEMPLATE - THIS IS WHAT SHOWS WHEN USER EDITS THE GRID CELL

            //SET UP A STACK PANEL THAT WILL CONTAIN A COMBOBOX AND AN EXPANDER
            FrameworkElementFactory spFactory = new FrameworkElementFactory(typeof(StackPanel));
            spFactory.Name = "mySpFactory";
            spFactory.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);

            DataTemplate cellEditingTemplate = new DataTemplate();

            //SET UP THE COMBO BOX
            FrameworkElementFactory comboMolPops = new FrameworkElementFactory(typeof(ComboBox));
            comboMolPops.Name = "MolPopComboBox";

            //------ Use a composite collection to insert "None" item
            CompositeCollection coll = new CompositeCollection();
            ComboBoxItem nullItem = new ComboBoxItem();
            nullItem.IsEnabled = true;
            nullItem.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            nullItem.Content = "None";
            coll.Add(nullItem);
            CollectionContainer cc = new CollectionContainer();
            cc.Collection = cell.cytosol.molpops;
            coll.Add(cc);
            comboMolPops.SetValue(ComboBox.ItemsSourceProperty, coll);

            //--------------

            comboMolPops.SetValue(ComboBox.DisplayMemberPathProperty, "Name");     //displays mol pop name
            comboMolPops.AddHandler(ComboBox.SelectionChangedEvent, new SelectionChangedEventHandler(comboMolPops_SelectionChanged));

            //NEED TO SOMEHOW CONVERT driver_mol_guid_ref to mol_pop!  Set up a converter and pass it the cytosol.
            MolGuidToMolPopForDiffConverter conv2 = new MolGuidToMolPopForDiffConverter();
            string sText = string.Format("elements[{0}].driver_mol_guid_ref", DiffRegGrid.Columns.Count);
            Binding b3 = new Binding(sText);
            b3.Mode = BindingMode.TwoWay;
            b3.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            b3.Converter = conv2;
            b3.ConverterParameter = cell.cytosol;
            comboMolPops.SetBinding(ComboBox.SelectedValueProperty, b3);
            comboMolPops.SetValue(ComboBox.ToolTipProperty, "Mol Pop Name");

            spFactory.AppendChild(comboMolPops);

            //--------------------------------------------------

            //SET UP AN EXPANDER THAT WILL CONTAIN ALPHA AND BETA

            //This disables the expander if no driver molecule is selected
            DriverElementToBoolConverter enabledConv = new DriverElementToBoolConverter();
            Binding bEnabled = new Binding(string.Format("elements[{0}].driver_mol_guid_ref", DiffRegGrid.Columns.Count));
            bEnabled.Mode = BindingMode.OneWay;
            bEnabled.Converter = enabledConv;

            //Expander
            FrameworkElementFactory expAlphaBeta = new FrameworkElementFactory(typeof(Expander));
            expAlphaBeta.SetValue(Expander.HeaderProperty, "Transition rate values");
            expAlphaBeta.SetValue(Expander.ExpandDirectionProperty, ExpandDirection.Down);
            expAlphaBeta.SetValue(Expander.BorderBrushProperty, Brushes.White);
            expAlphaBeta.SetValue(Expander.IsExpandedProperty, false);
            expAlphaBeta.SetValue(Expander.BackgroundProperty, Brushes.White);
            expAlphaBeta.SetBinding(Expander.IsEnabledProperty, bEnabled);

            FrameworkElementFactory spProduction = new FrameworkElementFactory(typeof(StackPanel));
            spProduction.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);

            FrameworkElementFactory spAlpha = new FrameworkElementFactory(typeof(StackPanel));
            spAlpha.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            FrameworkElementFactory tbAlpha = new FrameworkElementFactory(typeof(TextBlock));
            tbAlpha.SetValue(TextBlock.TextProperty, "Background:  ");
            tbAlpha.SetValue(TextBlock.ToolTipProperty, "Background production rate");
            tbAlpha.SetValue(TextBox.WidthProperty, 110D);
            //tbAlpha.SetValue(TextBlock.WidthProperty, new GridLength(50, GridUnitType.Pixel));
            spAlpha.AppendChild(tbAlpha);

            //SET UP THE ALPHA TEXTBOX
            Binding b = new Binding(string.Format("elements[{0}].Alpha", DiffRegGrid.Columns.Count));
            b.Mode = BindingMode.TwoWay;
            b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            FrameworkElementFactory txtDriverAlpha = new FrameworkElementFactory(typeof(TextBox));
            txtDriverAlpha.SetBinding(TextBox.TextProperty, b);

            txtDriverAlpha.SetValue(TextBox.ToolTipProperty, "Background production rate");
            txtDriverAlpha.SetValue(TextBox.WidthProperty, 50D);
            spAlpha.AppendChild(txtDriverAlpha);
            spProduction.AppendChild(spAlpha);

            FrameworkElementFactory spBeta = new FrameworkElementFactory(typeof(StackPanel));
            spBeta.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            FrameworkElementFactory tbBeta = new FrameworkElementFactory(typeof(TextBlock));
            tbBeta.SetValue(TextBlock.TextProperty, "Linear coefficient:  ");
            tbBeta.SetValue(TextBox.WidthProperty, 110D);
            tbBeta.SetValue(TextBlock.ToolTipProperty, "Production rate linear coefficient");
            spBeta.AppendChild(tbBeta);

            //SET UP THE BETA TEXTBOX
            Binding beta = new Binding(string.Format("elements[{0}].Beta", DiffRegGrid.Columns.Count));
            beta.Mode = BindingMode.TwoWay;
            beta.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            FrameworkElementFactory txtDriverBeta = new FrameworkElementFactory(typeof(TextBox));
            txtDriverBeta.SetBinding(TextBox.TextProperty, beta);
            txtDriverBeta.SetValue(TextBox.WidthProperty, 50D);
            txtDriverBeta.SetValue(TextBox.ToolTipProperty, "Production rate linear coefficient");
            spBeta.AppendChild(txtDriverBeta);
            spProduction.AppendChild(spBeta);

            expAlphaBeta.AppendChild(spProduction);
            spFactory.AppendChild(expAlphaBeta);

            //---------------------------

            //set the visual tree of the data template
            cellEditingTemplate.VisualTree = spFactory;

            //set cell layout
            col.CellTemplate = cellTemplate;
            col.CellEditingTemplate = cellEditingTemplate;

            return col;
        }


        /// <summary>
        /// This method creates a data grid column with a combo box in the header.
        /// The combo box contains genes that are not in the epigenetic map of of 
        /// the selected cell's differentiation scheme.  
        /// This allows the user to add genes to the epigenetic map.
        /// </summary>
        /// <returns></returns>
        private DataGridTextColumn CreateUnusedGenesColumn(EntityRepository er)
        {
            DataGridTextColumn editor_col = new DataGridTextColumn();
            editor_col.CanUserSort = false;
            DataGridRowHeader header = new DataGridRowHeader();
            DataTemplate rowHeaderTemplate = new DataTemplate();

            CollectionViewSource cvs1 = new CollectionViewSource();
            cvs1.SetValue(CollectionViewSource.SourceProperty, er.genes);
            cvs1.Filter += new FilterEventHandler(unusedGenesListView_Filter);

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

            rowHeaderTemplate.VisualTree = addGenesCombo;
            header.ContentTemplate = rowHeaderTemplate;
            editor_col.Header = header;

            return editor_col;
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
       
        //-----------------------------------------

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

            ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;
            if (cell == null)
                return;

            //if (cell.diff_scheme_guid_ref == "")
            //    return;

            //if cell does not have a diff scheme, create one
            if (cell.diff_scheme == null)
            {
                ConfigDiffScheme ds = new ConfigDiffScheme();

                ds.genes = new ObservableCollection<string>();
                ds.Name = "New diff scheme";
                ds.Driver = new ConfigTransitionDriver();
                ds.activationRows = new ObservableCollection<ConfigActivationRow>();

                cell.diff_scheme = ds;

            }

            ConfigDiffScheme scheme = cell.diff_scheme;
            ConfigGene gene = null;

            if (combo != null && combo.Items.Count > 0)
            {
                //Skip 0'th combo box item because it is the "None" string
                if (combo.SelectedIndex > 0)
                {
                    gene = (ConfigGene)combo.SelectedItem;       
                    if (gene == null)
                        return;

                    if (!scheme.genes.Contains(gene.entity_guid))
                    {
                        //If no states exist, then create at least 2 new ones
                        if (scheme.Driver.states.Count == 0)
                        {
                            AddDifferentiationState("State1");
                            AddDifferentiationState("State2");
                            //menuAddState_Click(null, null);
                            //menuAddState_Click(null, null);
                        }

                        scheme.genes.Add(gene.entity_guid);
                        foreach (ConfigActivationRow row in scheme.activationRows)
                        {
                            row.activations.Add(1.0);
                        }
                    }
                }
            }

            if (gene == null)
                return;

            //Have to refresh the data grid!
            DataGridTextColumn col = new DataGridTextColumn();
            col.Header = gene.Name;
            col.CanUserSort = false;

            if (scheme.activationRows.Count > 0)
            {
                Binding b = new Binding(string.Format("activations[{0}]", scheme.activationRows[0].activations.Count - 1));   //EpigeneticMapGrid.Columns.Count-1));  
                b.Mode = BindingMode.TwoWay;
                b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                col.Binding = b;
            }

            //if (EpigeneticMapGrid.Columns == null || EpigeneticMapGrid.Columns.Count <= 0)
            //    return;                        

            EpigeneticMapGrid.Columns.Insert(EpigeneticMapGrid.Columns.Count - 1, col);

            combo.SelectedIndex = 0;

            //This deletes the last column
            int colcount = EpigeneticMapGrid.Columns.Count;
            DataGridTextColumn comboCol = EpigeneticMapGrid.Columns[colcount - 1] as DataGridTextColumn;
            EpigeneticMapGrid.Columns.Remove(comboCol);

            //This regenerates the last column
            EntityRepository er = MainWindow.SC.SimConfig.entity_repository;
            comboCol = CreateUnusedGenesColumn(er);
            EpigeneticMapGrid.Columns.Add(comboCol);

        }

        /// <summary>
        /// This method is called on right-click + "delete selected genes", 
        /// on the epigenetic map data grid. Selected columns (genes) will 
        /// get deleted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuDeleteGenes_Click(object sender, RoutedEventArgs e)
        {
            EntityRepository er = MainWindow.SC.SimConfig.entity_repository;
            ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;
            if (cell == null)
            {
                return;
            }

            if (cell.diff_scheme == null)
                return;

            ConfigDiffScheme diff_scheme = cell.diff_scheme;

            foreach (DataGridTextColumn col in EpigeneticMapGrid.Columns.ToList())
            {
                bool isSelected = DataGridBehavior.GetHighlightColumn(col);
                string gene_name = col.Header as string;
                string guid = MainWindow.SC.SimConfig.findGeneGuid(gene_name, MainWindow.SC.SimConfig);
                if (isSelected && guid != null && guid.Length > 0)
                {
                    diff_scheme.genes.Remove(guid);
                    EpigeneticMapGrid.Columns.Remove(col);
                }
            }

            //This deletes the last column
            int colcount = EpigeneticMapGrid.Columns.Count;
            DataGridTextColumn comboCol = EpigeneticMapGrid.Columns[colcount - 1] as DataGridTextColumn;
            EpigeneticMapGrid.Columns.Remove(comboCol);

            //This regenerates the last column
            comboCol = CreateUnusedGenesColumn(er);
            EpigeneticMapGrid.Columns.Add(comboCol);
        }

        private void menuDeleteStates_Click(object sender, RoutedEventArgs e)
        {
            EntityRepository er = MainWindow.SC.SimConfig.entity_repository;
            ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;

            if (cell == null)
                return;

            if (cell.diff_scheme == null)
                return;

            ConfigDiffScheme diff_scheme = cell.diff_scheme;

            int i = 0;
            foreach (ConfigActivationRow diffrow in diff_scheme.activationRows.ToList())
            {
                if (EpigeneticMapGrid.SelectedItems.Contains(diffrow))
                {
                    int index = diff_scheme.activationRows.IndexOf(diffrow);
                    string stateToDelete = diff_scheme.Driver.states[index];

                    //this deletes the column from the differentiation regulators grid
                    DeleteDiffRegGridColumn(stateToDelete);

                    //this removes the activation row from the differentiation scheme
                    diff_scheme.RemoveActivationRow(diffrow);
                }
                i++;
            }

        }

        private void DeleteDiffRegGridColumn(string state)
        {
            foreach (DataGridTemplateColumn col in DiffRegGrid.Columns.ToList())
            {
                if ((string)(col.Header) == state)
                {
                    DiffRegGrid.Columns.Remove(col);
                    break;
                }
            }
        }
        
        /// <summary>
        /// This method is called when the user clicks on Add State menu item for Epigenetic Map grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuAddState_Click(object sender, RoutedEventArgs e)
        {
            //Show a dialog that gets the new state's name
            AddDiffState ads = new AddDiffState();

            if (ads.ShowDialog() == true)
            {
                AddDifferentiationState(ads.StateName);
            }
        }

        /// <summary>
        /// This method adds a differentiation state given a name 
        /// </summary>
        /// <param name="name"></param>
        private void AddDifferentiationState(string name)
        {
            EntityRepository er = MainWindow.SC.SimConfig.entity_repository;
            ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;

            if (cell == null)
                return;

            //If no diff scheme defined for this cell, create one
            if (cell.diff_scheme == null)
            {
                ConfigDiffScheme ds = new ConfigDiffScheme();

                ds.genes = new ObservableCollection<string>();
                ds.Name = "New diff scheme";
                ds.Driver = new ConfigTransitionDriver();
                ds.activationRows = new ObservableCollection<ConfigActivationRow>();
                cell.diff_scheme = ds;
            }

            ConfigDiffScheme diff_scheme = cell.diff_scheme;
            diff_scheme.AddState(name);
            DiffRegGrid.Columns.Add(CreateDiffRegColumn(er, cell, name));

            EpigeneticMapGrid.ItemsSource = null;
            EpigeneticMapGrid.ItemsSource = diff_scheme.activationRows;
            EpigeneticMapGenerateRowHeaders();

            //if adding first row, then need to add the columns too - one for each gene
            if (diff_scheme.activationRows.Count == 1)
            {
                foreach (string gene_guid in diff_scheme.genes)
                {
                    DataGridTextColumn col = new DataGridTextColumn();
                    col.Header = er.genes_dict[gene_guid].Name;
                    col.CanUserSort = false;
                    Binding b = new Binding(string.Format("activations[{0}]", diff_scheme.activationRows[0].activations.Count - 1));
                    b.Mode = BindingMode.TwoWay;
                    b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    col.Binding = b;
                }

            }

            DiffRegGrid.ItemsSource = null;
            DiffRegGrid.ItemsSource = diff_scheme.Driver.DriverElements;
            UpdateDiffRegGrid();

        }

        /// <summary>
        /// This method updates the differentiation regulators grid.
        /// It is meant to be called after the user adds a new diff state.
        /// </summary>
        private void UpdateDiffRegGrid()
        {
            if (DiffRegGrid.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                int nRows = DiffRegGrid.Items.Count;
                for (int j = 0; j < nRows; j++)
                {
                    var currRow = DiffRegGrid.GetRow(j);
                    int nCols = DiffRegGrid.Columns.Count;
                    for (int i = 0; i < nCols; i++)
                    {
                        if (j == i)
                        {
                            var columnCell = DiffRegGrid.GetCell(currRow, i);
                            if (columnCell != null)
                            {
                                columnCell.IsEnabled = false;
                                columnCell.Background = Brushes.LightGray;
                            }
                        }
                        else
                        {
                            //Trying to disable the expander here but this does not work, at least not yet.
                            var columnCell = DiffRegGrid.GetCell(currRow, i);
                            ComboBox cbx = FindChild<ComboBox>(columnCell, "comboMolPops");
                        }
                    }
                }
            }

            //Generate the row headers
            DiffRegGenerateRowHeaders();
        }

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

        /// <summary>
        /// This method gets called after the DiffRegGrid gui objects are generated. 
        /// This is the place to disable the diagonal grid cells.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DiffRegItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            if (DiffRegGrid.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                DiffRegGrid.ItemContainerGenerator.StatusChanged -= DiffRegItemContainerGenerator_StatusChanged;
                int nRows = DiffRegGrid.Items.Count;
                for (int j = 0; j < nRows; j++)
                {
                    var currRow = DiffRegGrid.GetRow(j);
                    int nCols = DiffRegGrid.Columns.Count;
                    for (int i = 0; i < nCols; i++)
                    {
                        if (j == i)
                        {
                            var columnCell = DiffRegGrid.GetCell(currRow, i);
                            if (columnCell != null)
                            {
                                columnCell.IsEnabled = false;
                                columnCell.Background = Brushes.LightGray;
                            }
                        }
                        else
                        {
                            //Trying to disable the expander here but this does not work, at least not yet.
                            var columnCell = DiffRegGrid.GetCell(currRow, i);
                            ComboBox cbx = FindChild<ComboBox>(columnCell, "comboMolPops");
                        }
                    }
                }
            }

            //Generate the row headers
            DiffRegGenerateRowHeaders();
        }

        private void DiffRegGenerateRowHeaders()
        {
            //The code below generates the row headers
            ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;
            if (cell == null)
                return;

            if (cell.diff_scheme == null)
                return;

            ConfigDiffScheme scheme = cell.diff_scheme;

            if (scheme == null)
                return;

            int rowcount = DiffRegGrid.Items.Count;
            for (int ii = 0; ii < rowcount; ii++)
            {
                if (ii >= scheme.Driver.states.Count)
                    break;

                DataGridRow row = DiffRegGrid.GetRow(ii);
                if (row != null)
                {
                    string sbind = string.Format("SelectedItem.diff_scheme.Driver.states[{0}]", ii);
                    Binding b = new Binding(sbind);
                    b.ElementName = "CellsListBox";
                    b.Path = new PropertyPath(sbind);
                    b.Mode = BindingMode.OneWay;

                    //Create a TextBox so the state name is editable
                    FrameworkElementFactory txtStateName = new FrameworkElementFactory(typeof(TextBlock));
                    txtStateName.SetValue(TextBlock.StyleProperty, null);
                    txtStateName.SetValue(TextBlock.WidthProperty, 120D);
                    txtStateName.SetBinding(TextBlock.TextProperty, b);

                    DataGridRowHeader header = new DataGridRowHeader();
                    DataTemplate rowHeaderTemplate = new DataTemplate();

                    rowHeaderTemplate.VisualTree = txtStateName;
                    header.Style = null;
                    header.ContentTemplate = rowHeaderTemplate;
                    row.HeaderStyle = null;
                    row.Header = header;
                }
            }
        }

        /// <summary>
        /// This method gets called after the EpigeneticMapGrid gui objects are generated. 
        /// Here we can set up the row headers.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EpigeneticItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            if (EpigeneticMapGrid.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                EpigeneticMapGrid.ItemContainerGenerator.StatusChanged -= EpigeneticItemContainerGenerator_StatusChanged;
                EpigeneticMapGenerateRowHeaders();
            }
        }

        /// <summary>
        /// This generates the row headers for the Epigenetic Map grid.
        /// These headers represent differentiation state names and are editable.
        /// </summary>
        private void EpigeneticMapGenerateRowHeaders()
        {
            ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;
            if (cell == null)
                return;
            if (cell.diff_scheme == null)
                return;
            ConfigDiffScheme scheme = cell.diff_scheme;
            if (scheme == null)
                return;
            
            int rowcount = EpigeneticMapGrid.Items.Count;

            for (int ii = 0; ii < rowcount; ii++)
            {
                if (ii >= scheme.Driver.states.Count)
                    break;

                DataGridRow row = EpigeneticMapGrid.GetRow(ii);
                if (row != null)
                {
                    string sbind = string.Format("SelectedItem.diff_scheme.Driver.states[{0}]", ii);
                    Binding b = new Binding(sbind);
                    b.ElementName = "CellsListBox";
                    b.Path = new PropertyPath(sbind);
                    b.Mode = BindingMode.TwoWay;
                    b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

                    //Create a TextBox so the state name is editable
                    FrameworkElementFactory txtStateName = new FrameworkElementFactory(typeof(TextBox));
                    txtStateName.SetValue(TextBox.StyleProperty, null);
                    //txtStateName.SetValue(TextBox.WidthProperty, 120D);
                    txtStateName.SetValue(TextBox.WidthProperty, 120D);
                    Thickness th = new Thickness(0D);
                    txtStateName.SetValue(TextBox.BorderThicknessProperty, th);
                    txtStateName.SetBinding(TextBox.TextProperty, b);

                    DataGridRowHeader header = new DataGridRowHeader();
                    DataTemplate rowHeaderTemplate = new DataTemplate();

                    rowHeaderTemplate.VisualTree = txtStateName;
                    header.Style = null;
                    header.ContentTemplate = rowHeaderTemplate;
                    row.HeaderStyle = null;
                    row.Header = header;
                }
            }
        }

        /// <summary>
        /// This method is called when the user clicks on a different row in the differentiation grid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DiffRegGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var selectedRow = DiffRegGrid.GetSelectedRow();
            if (selectedRow == null)
                return;

            int row = DiffRegGrid.SelectedIndex;

            DataGridCellInfo selected = DiffRegGrid.SelectedCells[0];
            DataGridColumn col = selected.Column;
        }

        // given a molecule name and location, find its guid
        public static string findMoleculeGuid(string name, MoleculeLocation ml, SimConfiguration sc)
        {
            foreach (ConfigMolecule cm in sc.entity_repository.molecules)
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

        public static T FindChild<T>(DependencyObject parent)  where T : DependencyObject
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

        private void cbCellDiffSchemes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Don't want to do anything when first display this combo box
            //Only do something if user really clicked and selected a different scheme

            if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
                return;

            ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
            if (cell == null)
                return;

            ComboBox combo = sender as ComboBox;

            if (combo.SelectedIndex == -1)
                return;

            if (combo.SelectedIndex == 0)
            {
                cell.diff_scheme = null;
                cell.diff_scheme_guid_ref = "";
                combo.Text = "None";
            }
            else
            {
                ConfigDiffScheme diffNew = (ConfigDiffScheme)combo.SelectedItem;

                if (diffNew.diff_scheme_guid == cell.diff_scheme_guid_ref)
                    return;

                EntityRepository er = MainWindow.SC.SimConfig.entity_repository;
                if (er.diff_schemes_dict.ContainsKey(diffNew.diff_scheme_guid) == true)
                {
                    cell.diff_scheme_guid_ref = diffNew.diff_scheme_guid;
                    cell.diff_scheme = er.diff_schemes_dict[diffNew.diff_scheme_guid].Clone();
                }
            }
            int nIndex = CellsListBox.SelectedIndex;
            CellsListBox.SelectedIndex = -1;
            CellsListBox.SelectedIndex = nIndex;
        }
        
        private void chkHasDivDriver_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
            if (cell == null)
                return;

            CheckBox ch = sender as CheckBox;
            if (ch.IsChecked == false)
            {
                cell.div_driver = null;
            }
            else
            {
                if (cell.div_driver == null)
                {
                    EntityRepository er = MainWindow.SC.SimConfig.entity_repository;
                    //cell.div_driver_guid_ref = er.transition_drivers[3].driver_guid;
                    cell.div_driver_guid_ref = FindFirstDivDriver().driver_guid;
                    if (cell.div_driver_guid_ref == "")
                    {
                        MessageBox.Show("No division drivers are defined");
                        return;
                    }

                    if (er.transition_drivers_dict.ContainsKey(cell.div_driver_guid_ref) == true)
                    {
                        cell.div_driver = er.transition_drivers_dict[cell.div_driver_guid_ref].Clone();
                    }
                }
            }
        }

        private ConfigTransitionDriver FindFirstDeathDriver()
        {
            ConfigTransitionDriver driver = null;
            foreach (ConfigTransitionDriver d in MainWindow.SC.SimConfig.entity_repository.transition_drivers)
            {
                string name = d.Name;
                if (name.Contains("apoptosis"))
                {
                    driver = d;
                    break;
                }
            }

            return driver;
        }

        private ConfigTransitionDriver FindFirstDivDriver()
        {
            ConfigTransitionDriver driver = null;
            foreach (ConfigTransitionDriver d in MainWindow.SC.SimConfig.entity_repository.transition_drivers)
            {
                string name = d.Name;
                if (name.Contains("division"))
                {
                    driver = d;
                    break;
                }
            }

            return driver;
        }

        private void btnNewDiffScheme_Click(object sender, RoutedEventArgs e)
        {
            AddDifferentiationState("State1");
            AddDifferentiationState("State2");
        }

        private void btnDelDiffScheme_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res;
            res = MessageBox.Show("Are you sure you want to delete the selected cell's differentiation scheme?", "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;

            ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;
            if (cell == null)
                return;

            cell.diff_scheme = null;

            //Clear the grids
            EpigeneticMapGrid.ItemsSource = null;
            EpigeneticMapGrid.Columns.Clear();
            DiffRegGrid.ItemsSource = null;
            DiffRegGrid.Columns.Clear();

            //Still want 'Add Genes' combo box
            DataGridTextColumn combo_col = CreateUnusedGenesColumn(MainWindow.SC.SimConfig.entity_repository);
            EpigeneticMapGrid.Columns.Add(combo_col);
            EpigeneticMapGrid.ItemContainerGenerator.StatusChanged += new EventHandler(EpigeneticItemContainerGenerator_StatusChanged);
        }

        private void btnNewDeathDriver_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
            if (cell == null)
                return;
            
            if (cell.death_driver == null)
            {
                EntityRepository er = MainWindow.SC.SimConfig.entity_repository;
                //cell.death_driver_guid_ref = er.transition_drivers[2].driver_guid;
                cell.death_driver_guid_ref = FindFirstDeathDriver().driver_guid;
                if (cell.death_driver_guid_ref == "")
                {
                    MessageBox.Show("No death drivers are defined");
                    return;
                }
                if (er.transition_drivers_dict.ContainsKey(cell.death_driver_guid_ref) == true)
                {
                    cell.death_driver = er.transition_drivers_dict[cell.death_driver_guid_ref].Clone();
                }
            }
        }

        private void btnDelDeathDriver_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
            if (cell == null)
                return;

            //confirm deletion of driver
            MessageBoxResult res;
            res = MessageBox.Show("Are you sure you want to delete the selected cell's death driver?", "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;

            //delete driver
            cell.death_driver = null;
            
        }

        private void btnNewDivDriver_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
            if (cell == null)
                return;

            
            if (cell.div_driver == null)
            {
                EntityRepository er = MainWindow.SC.SimConfig.entity_repository;
                cell.div_driver_guid_ref = FindFirstDivDriver().driver_guid;
                if (cell.div_driver_guid_ref == "")
                {
                    MessageBox.Show("No division drivers are defined");
                    return;
                }

                if (er.transition_drivers_dict.ContainsKey(cell.div_driver_guid_ref) == true)
                {
                    cell.div_driver = er.transition_drivers_dict[cell.div_driver_guid_ref].Clone();
                }
            }
        }

        private void btnDelDivDriver_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
            if (cell == null)
                return;

            //confirm deletion of driver
            MessageBoxResult res;
            res = MessageBox.Show("Are you sure you want to delete the selected cell's division driver?", "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;

            //delete driver
            cell.div_driver = null;
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
}


