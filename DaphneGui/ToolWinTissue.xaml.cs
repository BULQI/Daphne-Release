/*
Copyright (C) 2019 Kepler Laboratory of Quantitative Immunology

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY 
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
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
using System.Windows.Shapes;

using Daphne;
using ActiproSoftware.Windows.Controls.Docking;
using System.Globalization;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for ToolWinTissue.xaml
    /// </summary>
    public partial class ToolWinTissue : ToolWinBase
    {
        public ToolWinTissue()
        {
            TitleText = "Tissue Simulation";
            ToroidalVisibility = Visibility.Visible;
            SimRepetitionVisibility = Visibility.Visible;
            ZExtentVisibility = Visibility.Visible;
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// Add an instance of the default box to the entity repository.
        /// Default values: box center at center of ECS, box widths are 1/4 of ECS extents
        /// </summary>
        /// <param name="box"></param>
        public override void AddDefaultBoxSpec(BoxSpecification box)
        {
            ConfigECSEnvironment envHandle = (ConfigECSEnvironment)MainWindow.SOP.Protocol.scenario.environment;

            box.x_trans = envHandle.extent_x / 2;
            box.y_trans = envHandle.extent_y / 2;
            box.z_trans = envHandle.extent_z / 2; ;
            box.x_scale = envHandle.extent_x / 4; ;
            box.y_scale = envHandle.extent_x / 4; ;
            box.z_scale = envHandle.extent_x / 4; ;

            // Add box GUI property changed to VTK callback
            box.PropertyChanged += MainWindow.GUIInteractionToWidgetCallback;
        }

        /// <summary>
        /// Actions to preserve tab focus when the Apply button is pushed.
        /// </summary>
        public override void Apply()
        {
            TabItem selectedTab = toolWinTissue.ConfigTabControl.SelectedItem as TabItem;

            int cellPopSelectedIndex = -1;
            if (selectedTab == toolWinTissue.tabCellPop)
            {
                cellPopSelectedIndex = toolWinTissue.CellPopControl.CellPopsListBox.SelectedIndex;
            }

            int ecmMolPopSelIndex = -1;
            if (selectedTab == toolWinTissue.tabECM)
            {
                ecmMolPopSelIndex = toolWinTissue.EcmMolpopControl.lbEcsMolPops.SelectedIndex;
            }

            int reportECMmolSelectedIndex = -1;
            int reportCellSelectedIndex = -1;
            int reportCellPopSelectedIndex = -1;
            int reportCellStateSelectedIndex = -1;
            if (selectedTab == tabReports)
            {
                reportECMmolSelectedIndex = toolWinTissue.dgEcmMols.SelectedIndex;
                reportCellSelectedIndex = toolWinTissue.dgCellDetails.SelectedIndex;
                reportCellPopSelectedIndex = toolWinTissue.lbRptCellPops.SelectedIndex;
                reportCellStateSelectedIndex = toolWinTissue.dgCellStates.SelectedIndex;
            }

            MW.Apply();

            toolWinTissue.ConfigTabControl.SelectedItem = selectedTab;

            if (selectedTab == toolWinTissue.tabCellPop)
            {
                toolWinTissue.CellPopControl.CellPopsListBox.SelectedIndex = cellPopSelectedIndex;
            }
            else if (selectedTab == toolWinTissue.tabECM)
            {
                toolWinTissue.EcmMolpopControl.lbEcsMolPops.SelectedIndex = ecmMolPopSelIndex;
            }
            else if (selectedTab == toolWinTissue.tabReports)
            {
                toolWinTissue.dgEcmMols.SelectedIndex = reportECMmolSelectedIndex;
                toolWinTissue.dgCellDetails.SelectedIndex = reportCellSelectedIndex;
                toolWinTissue.lbRptCellPops.SelectedIndex = reportCellPopSelectedIndex;
                toolWinTissue.dgCellStates.SelectedIndex = reportCellStateSelectedIndex;
            }
        }

        protected override void ConfigTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (tabECM.IsSelected == true)
            {
                if (toolWinTissue.EcmMolpopControl.lbAvailableReacCx != null)
                {
                    if (toolWinTissue.EcmMolpopControl.lbAvailableReacCx.ItemsSource != null)
                    {
                        CollectionViewSource.GetDefaultView(toolWinTissue.EcmMolpopControl.lbAvailableReacCx.ItemsSource).Refresh();
                    }
                }
                if (toolWinTissue.EcmMolpopControl.lvAvailableReacs != null)
                {
                    if (toolWinTissue.EcmMolpopControl.lvAvailableReacs.ItemsSource != null)
                    {
                        CollectionViewSource.GetDefaultView(toolWinTissue.EcmMolpopControl.lvAvailableReacs.ItemsSource).Refresh();
                    }
                }
            }
        }

        public override void RegionFocusToGUISection(RegionWidget rw, bool transferMatrix)
        {
            // identify the widget's key
            string key = "";

            if (rw != null && ((VTKFullGraphicsController)MainWindow.GC).Regions.ContainsValue(rw) == true)
            {
                foreach (KeyValuePair<string, RegionWidget> kvp in ((VTKFullGraphicsController)MainWindow.GC).Regions)
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
                    // Determine if the region is associated with a molpop
                    bool gui_spot_found = false;

                    if (!gui_spot_found)
                    {
                        for (int r = 0; r < MainWindow.SOP.Protocol.scenario.environment.comp.molpops.Count; r++)
                        {
                            if (MainWindow.SOP.Protocol.scenario.environment.comp.molpops[r].mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian &&
                                ((MolPopGaussian)MainWindow.SOP.Protocol.scenario.environment.comp.molpops[r].mp_distribution).gauss_spec.box_spec.box_guid == key)
                            {
                                gui_spot_found = true;

                                // Select this molpop in the GUI
                                toolWinTissue.EcmMolpopControl.lbEcsMolPops.SelectedIndex = r;
                                //SelectMolpopInGUI(r);

                                // Select the ECM tab for focus
                                toolWinTissue.ConfigTabControl.SelectedItem = tabECM;
                                
                                break;
                            }
                        }
                    }
                    if (!gui_spot_found)
                    {
                        // Determine if the region is associated with a cellpop
                        for (int r = 0; r < ((TissueScenario)MainWindow.SOP.Protocol.scenario).cellpopulations.Count; r++)
                        {
                            if (((TissueScenario)MainWindow.SOP.Protocol.scenario).cellpopulations[r].cellPopDist.DistType == CellPopDistributionType.Gaussian)
                            {
                                if (((CellPopGaussian)((TissueScenario)MainWindow.SOP.Protocol.scenario).cellpopulations[r].cellPopDist).gauss_spec.box_spec.box_guid == key)
                                {
                                    gui_spot_found = true;

                                    // Select this cellpop in the GUI                      
                                    // gmk - need code to select index r in the listbox of cell populations
 
                                    // Select the Cellpop tab for focus
                                    toolWinTissue.ConfigTabControl.SelectedItem = tabCellPop;
                                }
                            }

                        }

                    }
                }
            }
        }

        private void btnNewSkinClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (skinNameTextBox.Visibility == System.Windows.Visibility.Collapsed)
            {
                skinLabel.Visibility = System.Windows.Visibility.Visible;
                skinNameTextBox.Visibility = System.Windows.Visibility.Visible;
                button.Content = "Create";
                return;
            }
            //get a name for the new skin
            string skinName = skinNameTextBox.Text;
            skinNameTextBox.Visibility = System.Windows.Visibility.Collapsed;
            skinLabel.Visibility = System.Windows.Visibility.Collapsed;
            skinNameTextBox.Text = "";
            button.Content = "New Skin";
            if (skinName == null || skinName.Length == 0)
            {
                skinNote.Text = "No Name Given";
                return;
            }

            RenderSkin skin = MainWindow.SOP.SkinList.Where(x => x.Name == skinName).SingleOrDefault();
            if (skin != null)
            {
                var result = MessageBox.Show("A skin with the given name exists, Do you want to overwrite it? ", "Warning", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes)
                {
                    skinNote.Text = "Creating new skin cancelled";
                    return;
                }
            }

            var er = MainWindow.SOP.UserStore.entity_repository;
            RenderSkin newrs = new RenderSkin(skinName, er);
            //serialize to file
            string SkinFilePath = new Uri(MainWindow.appPath + @"\Config\RenderSkin\" + skinName + ".json").LocalPath;
            newrs.SerializeToFile(SkinFilePath);
            newrs.FileName = SkinFilePath;
            if (skin != null)
            {
                int index = MainWindow.SOP.SkinList.IndexOf(skin);
                MainWindow.SOP.SkinList.RemoveAt(index);
                MainWindow.SOP.SkinList.Insert(index, newrs);
                skinNote.Text = "skin data regenerated";
            }
            else
            {
                MainWindow.SOP.SkinList.Add(newrs);
                skinNote.Text = "New Skin created";
            }

            var cv = (CollectionView)CollectionViewSource.GetDefaultView(MainWindow.SOP.SkinList);
            if (cv != null)
            {
                cv.MoveCurrentTo(newrs);
            }
        }

        private void Button_Click_Edit_RenderSkin(object sender, RoutedEventArgs e)
        {
            var item = skinChoiceComboBox.SelectedItem;

            MainWindow.ST_RenderSkinWindow.DataContext = item;
            MainWindow.ST_RenderSkinWindow.Visibility = System.Windows.Visibility.Visible;
            MainWindow.ST_RenderSkinWindow.Activate();
        }

        private void skinChoiceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainWindow.ST_RenderSkinWindow.Visibility != System.Windows.Visibility.Visible) return;
            var item = skinChoiceComboBox.SelectedItem;
            MainWindow.ST_RenderSkinWindow.DataContext = item;
            //MainWindow.ST_RenderSkinWindow.Activate();
        }

        /// <summary>
        /// Select the first cell in the list by default
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void CellPopTabItem_Loaded(object sender, RoutedEventArgs e)
        {
        }

        /// <summary>
        /// Select the first molecule in the list by default
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ECMTabItem_Loaded(object sender, RoutedEventArgs e)
        {
            EcmMolpopControl.lbEcsMolPops.SelectedIndex = 0;
        }

        /// <summary>
        /// Select first items in the lists by default.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void ReportsTabItem_Loaded(object sender, RoutedEventArgs e)
        {
            // gmk - re-evaluate this section. Not sure if it is doing anything useful.
            //skg - This line selects the first cell in the list - must use SelectedIndex, not SelectedItem.
            toolWinTissue.lbRptCellPops.SelectedIndex = 0;

            //toolWinTissue.dgEcmMols.SelectedIndex = 0;
            //toolWinTissue.dgCellDetails.SelectedIndex = 0;
            //toolWinTissue.dgCellStates.SelectedIndex = 0;
            //toolWinTissue.dgEcmProbeMols.SelectedIndex = 0;
            //toolWinTissue.dgCellMembMols.SelectedIndex = 0;
            //toolWinTissue.dgCellCytosolMols.SelectedIndex = 0;
        }

        private void CellDeathParamsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }

        private void InteractionParamsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }
    }

    public class CellToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return Visibility.Collapsed;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
