﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ActiproSoftware.Windows.Controls.Docking;

using Workbench;
using Daphne;
using System.Windows.Data;
using System.Collections.ObjectModel;


namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for SimConfigToolWindow.xaml
    /// </summary>
    public partial class SimConfigToolWindow : ToolWindow
    {
        public SimConfigToolWindow()
        {
            InitializeComponent();
        }

        //private void AddRegionButton_Click(object sender, RoutedEventArgs e)
        //{
        //    //RegionsDetailsExpander.IsExpanded = true;

        //    BoxSpecification box = new BoxSpecification();
        //    box.x_trans = 200;
        //    box.y_trans = 200;
        //    box.z_trans = 200;
        //    box.x_scale = 100;
        //    box.y_scale = 100;
        //    box.z_scale = 100;
        //    // Add box GUI property changed to VTK callback
        //    //////////box.PropertyChanged += MainWindow.SC.GUIInteractionToWidgetCallback;
        //    //////////MainWindow.SC.SimConfig.entity_repository.box_specifications.Add(box);
        //    //////////Region reg = new Region("New region", RegionShape.Ellipsoid);
        //    //////////reg.region_box_spec_guid_ref = box.box_guid;
        //    //////////reg.region_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.6f, 0.31f, 0.7f);
        //    //////////// Add region GUI property changed to VTK callback
        //    //////////reg.PropertyChanged += MainWindow.SC.GUIRegionSurfacePropertyChange;
        //    //////////MainWindow.SC.SimConfig.scenario.regions.Add(reg);
        //    //////////RegionsListBox.SelectedIndex = RegionsListBox.Items.Count - 1;

        //    //////////// Add RegionControl & RegionWidget for the new region
        //    //////////MainWindow.VTKBasket.AddRegionRegionControl(reg);
        //    //////////MainWindow.GC.AddRegionRegionWidget(reg);
        //    //////////// Connect the VTK callback
        //    //////////MainWindow.GC.Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(MainWindow.GC.WidgetInteractionToGUICallback));
        //    //////////MainWindow.GC.Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(RegionFocusToGUISection));
            
        //    //////////MainWindow.GC.Rwc.Invalidate();
        //}

        

        private void AddCellButton_Click(object sender, RoutedEventArgs e)
        {
            CellsDetailsExpander.IsExpanded = true;
            CellPopulation cs = new CellPopulation();
            cs.cellpopulation_name = "New motile cell";           
            cs.number = 50;
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
            if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
                return;


            ConfigMolecularPopulation current_mol = (ConfigMolecularPopulation)MolPopsListBox.SelectedItem;
            MolPopInfo current_item = current_mol.mpInfo;
            MolPopDistributionType new_dist_type = (MolPopDistributionType)e.AddedItems[0];

            // Only want to change distribution type if the combo box isn't just selecting 
            // the type of current item in the solfacs list box (e.g. when list selection is changed)
            if (current_item.mp_distribution.mp_distribution_type != null && current_item.mp_distribution.mp_distribution_type == new_dist_type)
            {
                return;
            }
            else
            {
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
            }
        }

        /// <summary>
        /// Event handler for button press for changing custom chemokine distribution input file
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SolfacCustomGradientFile_Click(object sender, RoutedEventArgs e)
        {
            MolPopInfo current_item = (MolPopInfo)MolPopsListBox.SelectedItem;

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
            MolPopsListBox.SelectedIndex = index;
        }

        private void btnAddMolec_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecule gm = new ConfigMolecule();
            gm.ReadOnly = false;
            gm.ForegroundColor = System.Windows.Media.Colors.Black;
            //MainWindow.SC.SimConfig.entity_repository.UserdefMolecules.Add(gm);
            MainWindow.SC.SimConfig.entity_repository.AllMolecules.Add(gm);
            MolPopsListBox.SelectedIndex = MolPopsListBox.Items.Count - 1;

            lbMol.SelectedIndex = lbMol.Items.Count - 1;
        }

        private void AddMolButton_Click(object sender, RoutedEventArgs e)
        {
            //SolfacsDetailsExpander.IsExpanded = true;
            // Default to HomogeneousLevel for now...

            ConfigMolecularPopulation gmp = new ConfigMolecularPopulation();
            gmp.Name = "NewMP";
            gmp.mpInfo = new MolPopInfo();
            gmp.mpInfo.mp_dist_name = "New distribution";
            gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 1.0f, 0.2f);
            gmp.mpInfo.mp_is_time_varying = false;
            MainWindow.SC.SimConfig.scenario.MolPops.Add(gmp);
            MolPopsListBox.SelectedIndex = MolPopsListBox.Items.Count - 1;
        }
        private void RemoveMolButton_Click(object sender, RoutedEventArgs e)
        {
            int nIndex = MolPopsListBox.SelectedIndex;
            if ( nIndex >= 0) {
                ConfigMolecularPopulation gmp = (ConfigMolecularPopulation)MolPopsListBox.SelectedValue;
                MainWindow.SC.SimConfig.scenario.MolPops.Remove(gmp);
            }
        }
        private void AddReacButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigReaction new_grt = new ConfigReaction();
            ConfigReaction selected_grt = (ConfigReaction)lbAvailableReacs.SelectedItem;

            new_grt = selected_grt;
            MainWindow.SC.SimConfig.scenario.Reactions.Add(new_grt);            

        }
        private void RemoveReacButton_Click(object sender, RoutedEventArgs e)
        {
            int nIndex = ReacListBox.SelectedIndex;
            if (nIndex >= 0)
            {
                ConfigReaction grt = (ConfigReaction)ReacListBox.SelectedValue;
                MainWindow.SC.SimConfig.scenario.Reactions.Remove(grt);
            }
        }
        private void AddReacCompButton_Click(object sender, RoutedEventArgs e)
        {
            GuiReactionComplex grc = new GuiReactionComplex();
            GuiReactionComplex sel = (GuiReactionComplex)lbAvailableReacCx.SelectedItem;
            grc = sel;
            MainWindow.SC.SimConfig.scenario.ReactionComplexes.Add(grc);
            AddReacCxExpander.IsExpanded = !AddReacCxExpander.IsExpanded;
        }
        private void RemoveReacCompButton_Click(object sender, RoutedEventArgs e)
        {
            int nIndex = ReactionComplexListBox.SelectedIndex;
            if (nIndex >= 0)
            {
                GuiReactionComplex grc = (GuiReactionComplex)ReactionComplexListBox.SelectedValue;
                MainWindow.SC.SimConfig.scenario.ReactionComplexes.Remove(grc);
            }
        }
        private void AddReacCompExpandButton_Click(object sender, RoutedEventArgs e)
        {            
            AddReacCxExpander.IsExpanded = !AddReacCxExpander.IsExpanded;
        }

        //Reaction Complexes/Differentiation Schemes tab
        private void btnAddComplex_Click(object sender, RoutedEventArgs e)
        {
            AddReacComplex arc = new AddReacComplex(ReactionComplexDialogType.AddComplex);
            if (arc.ShowDialog() == true)
            {
                ////////lbComplexes.ItemsSource = null;
                ////////lbComplexes.ItemsSource = Sim.RCList;
                ////////lbComplexes.Focus();
                ////////lbComplexes.SelectedItem = 0;
            }
        }

        private void btnEditComplex_Click(object sender, RoutedEventArgs e)
        {
            AddReacComplex arc = new AddReacComplex(ReactionComplexDialogType.EditComplex);
            if (arc.ShowDialog() == true)
            {
                ////////lbComplexes.ItemsSource = null;
                ////////lbComplexes.ItemsSource = Sim.RCList;
            }
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
            GuiReactionComplex grc = (GuiReactionComplex)(lbComplexes.SelectedItem);
            ReactionComplexSimulation rcs = new ReactionComplexSimulation(grc);
            rcs.Go();
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
            if (lbCellAvailableReacCx.SelectedIndex != -1)
            {
                GuiReactionComplex grc = (GuiReactionComplex)lbCellAvailableReacCx.SelectedValue;
                if (!MainWindow.SC.SimConfig.scenario.ReactionComplexes.Contains(grc))
                    MainWindow.SC.SimConfig.scenario.ReactionComplexes.Add(grc);
            }
        }

        private void sliderGridStep_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            double v = e.NewValue;

            MainWindow.SC.SimConfig.scenario.environment.CalculateNumGridPts();
        }

        private void btnRemoveMolec_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecule gm = (ConfigMolecule)lbMol.SelectedValue;
            if (gm.ReadOnly == false)
            {
                int index = lbMol.SelectedIndex;
                //MainWindow.SC.SimConfig.entity_repository.UserdefMolecules.Remove(gm);
                MainWindow.SC.SimConfig.entity_repository.AllMolecules.Remove(gm);

                lbMol.SelectedIndex = index;

                if (index >= lbMol.Items.Count)
                    lbMol.SelectedIndex = lbMol.Items.Count - 1;

                if (lbMol.Items.Count == 0)
                    lbMol.SelectedIndex = -1;

            }
            else
            {
                MessageBox.Show("Cannot remove a predefined molecule");
            }
        }

        
    }

   
    
}
