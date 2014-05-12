using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ActiproSoftware.Windows.Controls.Docking;

using Workbench;


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

        private void AddRegionButton_Click(object sender, RoutedEventArgs e)
        {
            RegionsDetailsExpander.IsExpanded = true;

            

            BoxSpecification box = new BoxSpecification();
            box.x_trans = 200;
            box.y_trans = 200;
            box.z_trans = 200;
            box.x_scale = 100;
            box.y_scale = 100;
            box.z_scale = 100;
            // Add box GUI property changed to VTK callback
            //////////box.PropertyChanged += MainWindow.SC.GUIInteractionToWidgetCallback;
            //////////MainWindow.SC.SimConfig.entity_repository.box_specifications.Add(box);
            //////////Region reg = new Region("New region", RegionShape.Ellipsoid);
            //////////reg.region_box_spec_guid_ref = box.box_guid;
            //////////reg.region_color = System.Windows.Media.Color.FromScRgb(0.3f, 0.6f, 0.31f, 0.7f);
            //////////// Add region GUI property changed to VTK callback
            //////////reg.PropertyChanged += MainWindow.SC.GUIRegionSurfacePropertyChange;
            //////////MainWindow.SC.SimConfig.scenario.regions.Add(reg);
            //////////RegionsListBox.SelectedIndex = RegionsListBox.Items.Count - 1;

            //////////// Add RegionControl & RegionWidget for the new region
            //////////MainWindow.VTKBasket.AddRegionRegionControl(reg);
            //////////MainWindow.GC.AddRegionRegionWidget(reg);
            //////////// Connect the VTK callback
            //////////MainWindow.GC.Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(MainWindow.GC.WidgetInteractionToGUICallback));
            //////////MainWindow.GC.Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(RegionFocusToGUISection));
            
            //////////MainWindow.GC.Rwc.Invalidate();
        }

        private void RemoveRegionButton_Click(object sender, RoutedEventArgs e)
        {
            Region current_item = (Region)RegionsListBox.SelectedItem;
            string current_guid = current_item.region_box_spec_guid_ref;
            bool being_used = false;

            // Check to make sure no cellpopulations are using this region
            for (int ii = 0; ii < MainWindow.SC.SimConfig.scenario.cellpopulations.Count; ii++)
            {
                if (MainWindow.SC.SimConfig.scenario.cellpopulations[ii].cellpopulation_region_guid_ref == current_guid)
                    being_used = true;
            }
            if (being_used)
            {
                // Pop up notice
                MessageBoxResult tmp = MessageBox.Show("The region you are trying to delete is being used to constrain a cell set.");
                return;
            }
            else
            {
                BoxSpecification bs = MainWindow.SC.SimConfig.box_guid_box_dict[current_guid];

                //////////// Remove box property changed callback
                //////////bs.PropertyChanged -= MainWindow.SC.GUIInteractionToWidgetCallback;
                //////////// Remove box from entity_repository list
                //////////MainWindow.SC.SimConfig.entity_repository.box_specifications.Remove(bs);
                //////////// Remove region property changed callback
                //////////current_item.PropertyChanged -= MainWindow.SC.GUIRegionSurfacePropertyChange;
                //////////// Remove region from scenario regions list
                //////////MainWindow.SC.SimConfig.scenario.regions.Remove(current_item);

                //////////// Remove the RegionControl & RegionWidget(s)
                //////////MainWindow.VTKBasket.RemoveRegionControl(current_guid);
                //////////MainWindow.GC.RemoveRegionWidget(current_guid);
                //////////MainWindow.GC.Rwc.Invalidate();
            }
        }

        private void AddCellButton_Click(object sender, RoutedEventArgs e)
        {
            CellsDetailsExpander.IsExpanded = true;
            CellPopulation cs = new CellPopulation();
            cs.cellpopulation_name = "New motile cell";
            // Make sure there is at least one cell type in repository
            if (MainWindow.SC.SimConfig.entity_repository.cell_subsets.Count == 0)
            {
                CellSubset ct = new CellSubset();
                //ct.cell_subset_name = "bcell";  //skg 5/25/12
                ct.InitializeReceptorLevels(MainWindow.SC.SimConfig.entity_repository.solfac_types);
                MainWindow.SC.SimConfig.entity_repository.cell_subsets.Add(ct);
            }
            cs.cell_subset_guid_ref = MainWindow.SC.SimConfig.entity_repository.cell_subsets[0].cell_subset_guid;
            cs.number = 50;
            cs.cellpopulation_constrained_to_region = false;
            cs.cellpopulation_color = System.Windows.Media.Color.FromScRgb(1.0f, 1.0f, 0.5f, 0.0f);
            MainWindow.SC.SimConfig.scenario.cellpopulations.Add(cs);
            CellSetsListBox.SelectedIndex = CellSetsListBox.Items.Count - 1;
        }

        private void RemoveCellButton_Click(object sender, RoutedEventArgs e)
        {
            CellPopulation current_item = (CellPopulation)CellSetsListBox.SelectedItem;
            MainWindow.SC.SimConfig.scenario.cellpopulations.Remove(current_item);
        }

        private void AddSolfacButton_Click(object sender, RoutedEventArgs e)
        {
            SolfacsDetailsExpander.IsExpanded = true;
            // Default to HomogeneousLevel for now...
            MolPopInfo mpi = new MolPopInfo("New Soluble Factor");
            //mpi.mp_name = "New Soluble Factor";
            mpi.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 1.0f, 0.2f);
            if (MainWindow.SC.SimConfig.entity_repository.solfac_types.Count > 0)
            {
                mpi.mp_type_guid_ref = MainWindow.SC.SimConfig.entity_repository.solfac_types[0].solfac_type_guid;
            }
            mpi.mp_is_time_varying = false;
            //MainWindow.SC.SimConfig.scenario.solfacs.Add(solfac);
            MolPopsListBox.SelectedIndex = MolPopsListBox.Items.Count - 1;
        }

        private void RemoveSolfacButton_Click(object sender, RoutedEventArgs e)
        {
            MolPopInfo current_item = (MolPopInfo)MolPopsListBox.SelectedItem;
            //MainWindow.SC.SimConfig.scenario.solfacs.Remove(current_item);
        }

        //private void AddSolfacTypeButton_Click(object sender, RoutedEventArgs e)
        //{
        //    SolfacType st = new SolfacType();
        //    st.solfac_type_name = "solfac";
        //    st.solfac_type_receptor_name = "receptor";
        //    MainWindow.SC.SimConfig.entity_repository.solfac_types.Add(st);
        //    SolfacTypesListBox.SelectedIndex = SolfacTypesListBox.Items.Count - 1;
        //    // Also need to update cell types receptor levels
        //    foreach (CellSubset ct in MainWindow.SC.SimConfig.entity_repository.cell_subsets)
        //    {
        //        //skg 6/1/12 changed
        //        if (ct.cell_subset_type is BCellSubsetType)
        //        {
        //            BCellSubsetType bcst = (BCellSubsetType)ct.cell_subset_type;
        //            bcst.cell_subset_type_receptor_params.Add(new ReceptorParameters(st.solfac_type_guid));
        //        }
        //        else if (ct.cell_subset_type is TCellSubsetType)
        //        {
        //            TCellSubsetType tcst = (TCellSubsetType)ct.cell_subset_type;
        //            tcst.cell_subset_type_receptor_params.Add(new ReceptorParameters(st.solfac_type_guid));
        //        }
        //    }
        //}

        //private void RemoveSolfacTypeButton_Click(object sender, RoutedEventArgs e)
        //{
        //    // NOTE: Right now this allows users to delete any solfac type that isn't currently being used.
        //    //   We should probably start implementing a "protected" list of core solfac types
        //    //   that users can't delete.
        //    SolfacType current_item = (SolfacType)SolfacTypesListBox.SelectedItem;
        //    string current_guid = current_item.solfac_type_guid;
        //    bool being_used = false;

        //    // Check to make sure no solfacs are using this solfac type
        //    //////////for (int ii = 0; ii < MainWindow.SC.SimConfig.scenario.solfacs.Count; ii++)
        //    //////////{
        //    //////////    //if (MainWindow.SC.SimConfig.scenario.solfacs[ii].mp_type_guid_ref == current_item.solfac_type_guid)
        //    //////////        being_used = true;
        //    //////////}
        //    if (being_used)
        //    {
        //        // Pop up notice
        //        MessageBoxResult tmp = MessageBox.Show("The soluble factor type you are trying to delete is being used by a soluble factor.");
        //        return;
        //    }
        //    else
        //    {
        //        MainWindow.SC.SimConfig.entity_repository.solfac_types.Remove(current_item);
        //        // Also need to update cell types receptor levels
        //        foreach (CellSubset ct in MainWindow.SC.SimConfig.entity_repository.cell_subsets)
        //        {
        //            int relp_to_remove_idx = -1;

        //            ////skg 6/1/12 changed

        //            if (ct.cell_subset_type is BCellSubsetType)
        //            {
        //                BCellSubsetType bcst = (BCellSubsetType)ct.cell_subset_type;
        //                for (int jj = 0; jj < bcst.cell_subset_type_receptor_params.Count; jj++)
        //                {
        //                    //skg 5/27/12 changed
        //                    if (bcst.cell_subset_type_receptor_params[jj].receptor_solfac_type_guid_ref == current_guid)
        //                    {
        //                        relp_to_remove_idx = jj;
        //                        break;
        //                    }
        //                }
        //                if (relp_to_remove_idx >= 0)
        //                {
        //                    bcst.cell_subset_type_receptor_params.Remove(bcst.cell_subset_type_receptor_params[relp_to_remove_idx]);
        //                }
        //            }
        //            else if (ct.cell_subset_type is TCellSubsetType)
        //            {
        //                TCellSubsetType tcst = (TCellSubsetType)ct.cell_subset_type;
        //                for (int jj = 0; jj < tcst.cell_subset_type_receptor_params.Count; jj++)
        //                {
        //                    //skg 5/27/12 changed
        //                    if (tcst.cell_subset_type_receptor_params[jj].receptor_solfac_type_guid_ref == current_guid)
        //                    {
        //                        relp_to_remove_idx = jj;
        //                        break;
        //                    }
        //                }
        //                if (relp_to_remove_idx >= 0)
        //                {
        //                    tcst.cell_subset_type_receptor_params.Remove(tcst.cell_subset_type_receptor_params[relp_to_remove_idx]);
        //                }
        //            }
        //            else 
        //            {
        //                //TO DO?
        //            }
        //        }
        //    }
        //}
        private void AddSolfacTimeAmpButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement...
        }

        private void RemoveSolfacTimeAmpButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement...
        }

        private void AddCellTypeButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Also add a "duplicate" button for making a copy of an existing cell type
            CellSubset ct = new CellSubset();

            //skg 6/6/12 - Should InitializeReceptorLevels be moved to CellSubtype?
            
            ct.InitializeReceptorLevels(MainWindow.SC.SimConfig.entity_repository.solfac_types);
            MainWindow.SC.SimConfig.entity_repository.cell_subsets.Add(ct);
            CellTypesListBox.SelectedIndex = CellTypesListBox.Items.Count - 1;
        }

        private void RemoveCellTypeButton_Click(object sender, RoutedEventArgs e)
        {
            // NOTE: Right now this allows users to delete any cell type that isn't currently being used.
            //   We should probably start implementing a "protected" list of core cell types
            //   that users can't delete.
            CellSubset current_item = (CellSubset)CellTypesListBox.SelectedItem;
            bool being_used = false;

            // Check to make sure no cellpopulations are using this cell type
            for (int ii = 0; ii < MainWindow.SC.SimConfig.scenario.cellpopulations.Count; ii++)
            {
                if (MainWindow.SC.SimConfig.scenario.cellpopulations[ii].cell_subset_guid_ref == current_item.cell_subset_guid)
                    being_used = true;
            }
            if (being_used)
            {
                // Pop up notice
                MessageBoxResult tmp = MessageBox.Show("The cell type you are trying to delete is being used by a cell set.");
                return;
            }
            else
            {
                MainWindow.SC.SimConfig.entity_repository.cell_subsets.Remove(current_item);
            }
        }

        private void AddGaussSpecButton_Click(object sender, RoutedEventArgs e)
        {
            this.AddGaussianSpecification();
            GaussianSpecsListBox.SelectedIndex = GaussianSpecsListBox.Items.Count - 1;
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

        private void RemoveGaussSpecButton_Click(object sender, RoutedEventArgs e)
        {
            GaussianSpecification current_item = (GaussianSpecification)GaussianSpecsListBox.SelectedItem;
            string current_guid = current_item.gaussian_spec_box_guid_ref;
            bool being_used = false;

            // Check to make sure no solfacs are using this gauss_spec
            ////////for (int ii = 0; ii < MainWindow.SC.SimConfig.scenario.solfacs.Count; ii++)
            ////////{
            ////////    if (MainWindow.SC.SimConfig.scenario.solfacs[ii].mp_distribution is MolPopGaussianGradient)
            ////////    {
            ////////        MolPopGaussianGradient sgg = MainWindow.SC.SimConfig.scenario.solfacs[ii].mp_distribution as MolPopGaussianGradient;
            ////////        if (sgg != null && sgg.gaussgrad_gauss_spec_guid_ref == current_guid)
            ////////            being_used = true;
            ////////    }
            ////////}
            if (being_used)
            {
                // Pop up notice
                MessageBoxResult tmp = MessageBox.Show("The gaussian specification you are trying to delete is being used by a soluble factor.");
                return;
            }
            else
            {
                // Find the box_spec associated with this region
                int box_id = -1;
                for (int jj = 0; jj < MainWindow.SC.SimConfig.entity_repository.box_specifications.Count; jj++)
                {
                    if (MainWindow.SC.SimConfig.entity_repository.box_specifications[jj].box_guid == current_guid)
                    {
                        box_id = jj;
                        break;
                    }
                }
                if (box_id == -1)
                {
                    // Should never reach here... pop up notice
                    MessageBoxResult tmp = MessageBox.Show("Problem: Box spec for that gaussian spec can't be found...");
                    return;
                }
                //////////// Remove box property changed callback
                //////////MainWindow.SC.SimConfig.entity_repository.box_specifications[box_id].PropertyChanged -= MainWindow.SC.GUIInteractionToWidgetCallback;
                //////////// Remove box from entity_repository list
                //////////MainWindow.SC.SimConfig.entity_repository.box_specifications.Remove(MainWindow.SC.SimConfig.entity_repository.box_specifications[box_id]);
                //////////// Remove region property changed callback
                //////////current_item.PropertyChanged -= MainWindow.SC.GUIGaussianSurfaceVisibilityToggle;
                //////////// Remove region from scenario regions list
                //////////MainWindow.SC.SimConfig.entity_repository.gaussian_specifications.Remove(current_item);

                //////////// Remove the RegionControl & RegionWidget(s)
                //////////MainWindow.VTKBasket.RemoveRegionControl(current_guid);
                //////////MainWindow.GC.RemoveRegionWidget(current_guid);
                //////////MainWindow.GC.Rwc.Invalidate();
            }
        }

        private void SolfacDistributionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only want to respond to purposeful user interaction, not just population and depopulation
            // of solfacs list
            if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
                return;


            GuiMolecularPopulation current_mol = (GuiMolecularPopulation)MolPopsListBox.SelectedItem;
            MolPopInfo current_item = current_mol.mpInfo;
            MolPopDistributionType new_dist_type = (MolPopDistributionType)e.AddedItems[0];

            // Only want to change solfac distribution type if the combo box isn't just selecting 
            // the type of current item in the solfacs list box (e.g. when list selection is changed)
            if (current_item.mp_distribution.mp_distribution_type == new_dist_type)
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

        public void SelectRegionInGUI(int index, string guid)
        {
            // Regions are in the second (entities) tab panel
            ConfigTabControl.SelectedIndex = 1;
            // Because each region will have a unique box guid, can use data-binding-y way of setting selection
            RegionsListBox.SelectedIndex = index;
            RegionsListBox.SelectedValuePath = "region_box_spec_guid_ref";
            RegionsListBox.SelectedValue = guid;
        }

        public void SelectSolfacInGUI(int index)
        {
            // Solfacs are in the second tab panel
            ConfigTabControl.SelectedIndex = 1;
            // Use list index here since not all solfacs.mp_distribution have this guid field
            MolPopsListBox.SelectedIndex = index;
        }

        public void SelectGaussSpecInGUI(int index, string guid)
        {
            // Gaussian specs are in the third tab panel
            ConfigTabControl.SelectedIndex = 2;
            // Use list index here since not all solfacs.mp_distribution have this guid field
            GaussianSpecsListBox.SelectedIndex = index;
            GaussianSpecsListBox.SelectedValuePath = "gaussian_spec_box_guid_ref";
            GaussianSpecsListBox.SelectedValue = guid;
        }

        //////////public void RegionFocusToGUISection(RegionWidget rw, bool transferMatrix)
        //////////{
        //////////    // identify the widget's key
        //////////    string key = "";

        //////////    if (rw != null && MainWindow.GC.Regions.ContainsValue(rw) == true)
        //////////    {
        //////////        foreach (KeyValuePair<string, RegionWidget> kvp in MainWindow.GC.Regions)
        //////////        {
        //////////            if (kvp.Value == rw)
        //////////            {
        //////////                key = kvp.Key;
        //////////                break;
        //////////            }
        //////////        }

        //////////        // found?
        //////////        if (key != "")
        //////////        {
        //////////            // Select the correct region/solfac/gauss_spec in the GUI's lists
        //////////            bool gui_spot_found = false;

        //////////            for (int r = 0; r < MainWindow.SC.SimConfig.scenario.regions.Count; r++)
        //////////            {
        //////////                // See whether the current widget is for a Region
        //////////                if (MainWindow.SC.SimConfig.scenario.regions[r].region_box_spec_guid_ref == key)
        //////////                {
        //////////                    SelectRegionInGUI(r, key);
        //////////                    gui_spot_found = true;
        //////////                    break;
        //////////                }
        //////////            }
        //////////            if (!gui_spot_found)
        //////////            {
        //////////                // Next check whether any Solfacs use this right gaussian_spec for this box
        //////////                //////////for (int r = 0; r < MainWindow.SC.SimConfig.scenario.solfacs.Count; r++)
        //////////                //////////{
        //////////                //////////    // We'll just be picking the first one that uses 
        //////////                //////////    if (MainWindow.SC.SimConfig.scenario.solfacs[r].mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian &&
        //////////                //////////        ((MolPopGaussianGradient)MainWindow.SC.SimConfig.scenario.solfacs[r].mp_distribution).gaussgrad_gauss_spec_guid_ref == key)
        //////////                //////////    {
        //////////                //////////        SelectSolfacInGUI(r);
        //////////                //////////        gui_spot_found = true;
        //////////                //////////        break;
        //////////                //////////    }
        //////////                //////////}
        //////////            }
        //////////            if (!gui_spot_found)
        //////////            {
        //////////                // Last check the gaussian_specs for this box guid
        //////////                for (int r = 0; r < MainWindow.SC.SimConfig.entity_repository.gaussian_specifications.Count; r++)
        //////////                {
        //////////                    // We'll just be picking the first one that uses 
        //////////                    if (MainWindow.SC.SimConfig.entity_repository.gaussian_specifications[r].gaussian_spec_box_guid_ref == key)
        //////////                    {
        //////////                        SelectGaussSpecInGUI(r, key);
        //////////                        gui_spot_found = true;
        //////////                        break;
        //////////                    }
        //////////                }
        //////////            }
        //////////        }
        //////////    }
        //////////}

        private void CellSubsetTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only want to respond to purposeful user interaction, not just population and depopulation
            // of cell subsets list
            if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
                return;

            CellSubset current_item = (CellSubset)CellTypesListBox.SelectedItem;
            CellBaseTypeLabel new_base_type = (CellBaseTypeLabel)e.AddedItems[0];

            // Only want to change cell_subset_type if the combo box isn't just selecting 
            // the type of current item in the cell subsets list box (i.e. when list selection is changed)
          
            if (current_item.cell_subset_type.baseCellType == new_base_type)
            {
                return;
            }
            else
            {
                switch (new_base_type)
                {
                    case CellBaseTypeLabel.BCell:
                        CellSubsetType bc = new BCellSubsetType();
                        current_item.cell_subset_type = bc;
                        current_item.InitializeReceptorLevels(MainWindow.SC.SimConfig.entity_repository.solfac_types);
                        break;
                    case  CellBaseTypeLabel.TCell:
                        CellSubsetType tc = new TCellSubsetType();
                        current_item.cell_subset_type = tc;
                        current_item.InitializeReceptorLevels(MainWindow.SC.SimConfig.entity_repository.solfac_types);
                        break;
                    case  CellBaseTypeLabel.FDC:
                        CellSubsetType fd = new FDCellSubsetType();
                        current_item.cell_subset_type = fd;
                        break;
                    
                    default:
                        throw new ArgumentException("Base cell type out of range");
                }
           }
        }

        private void BindTestButton_Click(object sender, RoutedEventArgs e)
        {
            CellSubset current_item = (CellSubset)CellTypesListBox.SelectedItem;
             
            if (current_item.cell_subset_type.baseCellType == CellBaseTypeLabel.FDC)
            {
                //FDCellSubsetType fd = (FDCellSubsetType)current_item.cell_subset_type;
                //fd.FCReceptorDensity = 333;
            }
        }

        private void CellSubsetTypePhenotypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CellSubset current_item = (CellSubset)CellTypesListBox.SelectedItem;

            if (current_item != null)
            {
                if (current_item.cell_subset_type.baseCellType == CellBaseTypeLabel.BCell)
                {
                    //BCellSubsetType bcst = (BCellSubsetType)(current_item.cell_subset_type);
                    //BCellPhenotype bp = bcst.Phenotype;
                }
            }
        }

        private void CellSubsetTypePhenotypeComboBox_SelectionChanged_1(object sender, SelectionChangedEventArgs e)
        {

        }

        private void btnAddMolec_Click(object sender, RoutedEventArgs e)
        {
            int i = 0;
            i++;

            lbMol.ItemsSource = "{Binding Path=PredefMolecules}";

            int count = lbMol.Items.Count;

        }

        private void AddMolButton_Click(object sender, RoutedEventArgs e)
        {
            SolfacsDetailsExpander.IsExpanded = true;
            // Default to HomogeneousLevel for now...

            GuiMolecularPopulation gmp = new GuiMolecularPopulation();
            gmp.Name = "NewMP";
            gmp.mpInfo = new MolPopInfo();
            gmp.mpInfo.mp_name = "New Soluble Factor";
            gmp.mpInfo.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 1.0f, 0.2f);
            if (MainWindow.SC.SimConfig.entity_repository.solfac_types.Count > 0)
            {
                gmp.mpInfo.mp_type_guid_ref = MainWindow.SC.SimConfig.entity_repository.solfac_types[0].solfac_type_guid;
            }
            gmp.mpInfo.mp_is_time_varying = false;
            MainWindow.SC.SimConfig.scenario.MolPops.Add(gmp);
            MolPopsListBox.SelectedIndex = MolPopsListBox.Items.Count - 1;
        }
        private void RemoveMolButton_Click(object sender, RoutedEventArgs e)
        {
        }
        private void AddReacButton_Click(object sender, RoutedEventArgs e)
        {
            GuiReactionTemplate new_grt = new GuiReactionTemplate();
            GuiReactionTemplate selected_grt = (GuiReactionTemplate)lbAvailableReacs.SelectedItem;

            new_grt = selected_grt;
            MainWindow.SC.SimConfig.scenario.Reactions.Add(new_grt);            

        }
        private void RemoveReacButton_Click(object sender, RoutedEventArgs e)
        {
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
            MainWindow.SC.SimConfig.ChartWindow.Title = "Reaction Complex: " + grc.Name;
            MainWindow.SC.SimConfig.ChartWindow.RC = rcs.RC;
            MainWindow.SC.SimConfig.ChartWindow.Activate();
            MainWindow.SC.SimConfig.ChartWindow.Render();
            MainWindow.SC.SimConfig.ChartWindow.slMaxTime.Maximum = rcs.RC.dMaxTime;
            MainWindow.SC.SimConfig.ChartWindow.slMaxTime.Value = rcs.RC.dInitialTime;
              
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
    }
}
