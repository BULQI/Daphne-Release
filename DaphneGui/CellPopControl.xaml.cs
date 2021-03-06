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
using System.Windows.Navigation;
using System.Windows.Shapes;

using Daphne;
using DaphneUserControlLib;
using System.ComponentModel;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for CellPopControl.xaml
    /// </summary>
    public partial class CellPopControl : UserControl, INotifyPropertyChanged
    {
        private ConfigCell selectedCell;
        public ConfigCell SelectedCell
        {
            get
            {
                return selectedCell;
            }
            set
            {
                selectedCell = value;
                OnPropertyChanged("SelectedCell");
            }
        }

        public CellPopControl()
        {
            InitializeComponent();
        }

        ///
        //Notification handling
        /// 
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        private void AddCellPopButton_Click(object sender, RoutedEventArgs e)
        {
            TissueScenario scenario = (TissueScenario)MainWindow.SOP.Protocol.scenario;
            ConfigECSEnvironment envHandle = (ConfigECSEnvironment)MainWindow.SOP.Protocol.scenario.environment;

            CellPopsDetailsExpander.IsExpanded = true;

            // Some relevant CellPopulation constructor defaults: 
            //      number = 1
            //      no instantiation of cellPopDist
            CellPopulation cp = new CellPopulation();

            EntityRepository er = MainWindow.SOP.Protocol.entity_repository;


            // Default to the first unused cell type
            foreach (ConfigCell cell in er.cells)
            {
                if (scenario.cellpopulations.Where(m => m.Cell.CellName == cell.CellName).Any()) continue;
                cp.Cell = cell.Clone(true);
                cp.cellpopulation_name = cell.CellName;
                break;
            }

            //If all cell types are used up already, then just get the first one
            if (cp.Cell == null)
            {
                // Default cell type and name to first entry in the cell repository
                if (er.cells.Count > 0)
                {
                    ConfigCell cell_to_clone = er.cells.First();
                    cp.Cell = cell_to_clone.Clone(true);
                    cp.cellpopulation_name = cp.Cell.CellName;
                }
                else
                {
                    MessageBox.Show("Please add cells from the User store first.");
                    return;
                }
            }



            double[] extents = new double[3] { envHandle.extent_x, 
                                               envHandle.extent_y, 
                                               envHandle.extent_z };
            double minDisSquared = 2 * cp.Cell.CellRadius;
            minDisSquared *= minDisSquared;

            // Default is uniform probability distribution
            cp.cellPopDist = new CellPopUniform(extents, minDisSquared, cp);
            cp.cellPopDist.Initialize();
            // Causes a new random seed for the random source
            // Otherwise we will get the same values every time if this is followed by Apply()
            cp.cellPopDist.Reset();

            //add rendering options to scenario
            (MainWindow.SOP.Protocol.scenario as TissueScenario).popOptions.AddRenderOptions(cp.renderLabel, cp.Cell.CellName, true);

            //This is needed because without it, a new cell pop was showing black square to the left.
            MainWindow.SOP.SelectedRenderSkin.AddRenderCell(cp.renderLabel, cp.Cell.CellName);

            scenario.cellpopulations.Add(cp);
            CellPopsListBox.SelectedIndex = CellPopsListBox.Items.Count - 1;
        }

        /// <summary>
        /// Associate box with GaussianSpecification's box.
        /// Add both to appropriate VTK lists.
        /// </summary>
        /// <param name="gg"></param>
        /// <param name="box"></param>
        private void AddGaussianSpecification(GaussianSpecification gg, BoxSpecification box)
        {
            gg.box_spec = box;
            gg.gaussian_spec_name = "";
            //gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            // Add gauss spec property changed to VTK callback (ellipsoid actor color & visibility)
            gg.PropertyChanged += MainWindow.GUIGaussianSurfaceVisibilityToggle;

            // Add RegionControl & RegionWidget for the new gauss_spec
            ((VTKFullDataBasket)MainWindow.VTKBasket).AddGaussSpecRegionControl(gg);
            ((VTKFullGraphicsController)MainWindow.GC).AddGaussSpecRegionWidget(gg);
            // Connect the VTK callback
            // TODO: MainWindow.GC.Regions[box.box_guid].SetCallback(new RegionWidget.CallbackHandler(this.WidgetInteractionToGUICallback));
            ((VTKFullGraphicsController)MainWindow.GC).Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(((VTKFullGraphicsController)MainWindow.GC).WidgetInteractionToGUICallback));
            ((VTKFullGraphicsController)MainWindow.GC).Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(((ToolWinBase)MainWindow.ToolWin).RegionFocusToGUISection));

            ((VTKFullGraphicsController)MainWindow.GC).RWC.Invalidate();
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
                MainWindow.ToolWin.Apply();
                //MainWindow  applyButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
            }
        }

        private void cbCellLocationType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ConfigECSEnvironment envHandle = (ConfigECSEnvironment)MainWindow.SOP.Protocol.scenario.environment;

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

            double[] extents = new double[3] { envHandle.extent_x, 
                                               envHandle.extent_y, 
                                               envHandle.extent_z };
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
                    cpg.gauss_spec = null;
                    ((VTKFullGraphicsController)MainWindow.GC).RWC.Invalidate();
                }
                cellPop.cellPopDist = new CellPopUniform(extents, minDisSquared, cellPop);
                cellPop.cellPopDist.Initialize();
            }
            else if (cpdt == CellPopDistributionType.Gaussian)
            {
                res = MessageBox.Show("The current cell positions will be changed. Continue?", "Warning", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                {
                    cb.SelectedItem = current_dist.DistType;
                    return;
                }

                // Create new default box
                BoxSpecification box = new BoxSpecification();
                ((ToolWinBase)MainWindow.ToolWin).AddDefaultBoxSpec(box);

                // Create new GaussianSpecification
                GaussianSpecification gg = new GaussianSpecification();
                var render_cell = MainWindow.SOP.GetRenderCell(cellPop.renderLabel);
                Color cellpop_color = render_cell.base_color.EntityColor;
                gg.gaussian_spec_color = System.Windows.Media.Color.FromScRgb(0.2f, cellpop_color.R, cellpop_color.G, cellpop_color.B);

                // Associate box with gg.box and add both to appropriate VTK lists
                AddGaussianSpecification(gg, box);
                
                // Create a new 
                cellPop.cellPopDist = new CellPopGaussian(extents, minDisSquared, cellPop);
                ((CellPopGaussian)cellPop.cellPopDist).InitializeGaussSpec(gg);
                cellPop.cellPopDist.Initialize();

                // Connect the VTK callback
                ((VTKFullGraphicsController)MainWindow.GC).Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(((VTKFullGraphicsController)MainWindow.GC).WidgetInteractionToGUICallback));
                ((VTKFullGraphicsController)MainWindow.GC).Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(((ToolWinBase)MainWindow.ToolWin).RegionFocusToGUISection));
            }
            else if (cpdt == CellPopDistributionType.Specific)
            {
                res = MessageBox.Show("Keep current cell locations?", "", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                {
                    cellPop.cellPopDist = new CellPopSpecific(extents, minDisSquared, cellPop);
                    cellPop.cellPopDist.Initialize();
                }
                else
                {
                    cellPop.cellPopDist = new CellPopSpecific(extents, minDisSquared, cellPop);
                }
                // Remove box and Gaussian if applicable.
                if (current_dist.DistType == CellPopDistributionType.Gaussian)
                {
                    DeleteGaussianSpecification(current_dist);
                    CellPopGaussian cpg = current_dist as CellPopGaussian;
                    cpg.gauss_spec = null;
                    ((VTKFullGraphicsController)MainWindow.GC).RWC.Invalidate();
                }
            }

            MainWindow.ToolWin.Apply();

            // needed by the slider?
            ToolBarTray tr = new ToolBarTray();

        }

        private void cell_type_combo_box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only want to respond to purposeful user interaction, not just population and depopulation
            // of cell type list
            //if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
            //    return;

            ComboBox cb = (ComboBox)e.Source;
            if (cb == null)
                return;

            CellPopulation cp = (CellPopulation)CellPopsListBox.SelectedItem;

            if (cb.IsDropDownOpen == false) return;
            cb.IsDropDownOpen = false;

            int nIndex = cb.SelectedIndex;
            if (nIndex < 0)
                return;

            //if user picked 'new cell type' then create new configcell in ER
            if (nIndex == (cb.Items.Count - 1))
            {
                ConfigCell newLibCell = new ConfigCell();
                newLibCell.CellName = newLibCell.GenerateNewName(MainWindow.SOP.Protocol, "_New");

                Protocol B = MainWindow.SOP.Protocol;
                Level.PushStatus status = B.pushStatus(newLibCell);
                if (status == Level.PushStatus.PUSH_CREATE_ITEM)
                {
                    B.repositoryPush(newLibCell, status); // push into B, inserts as new
                }

                if (cp != null)
                {
                    cp.Cell = newLibCell.Clone(true);
                    cp.Cell.CellName = newLibCell.CellName;
                    cp.renderLabel = cp.Cell.renderLabel;
                }
                else
                {
                    //If no cell pops exist, create a new one - part of bug 2457 fix

                    ConfigECSEnvironment envHandle = (ConfigECSEnvironment)MainWindow.SOP.Protocol.scenario.environment;
                    TissueScenario scenario = (TissueScenario)MainWindow.SOP.Protocol.scenario;

                    cp = new CellPopulation();
                    cp.Cell = newLibCell.Clone(false);
                    cp.cellpopulation_name = newLibCell.CellName;

                    double[] extents = new double[3] { envHandle.extent_x, 
                                               envHandle.extent_y, 
                                               envHandle.extent_z };
                    double minDisSquared = 2 * cp.Cell.CellRadius;
                    minDisSquared *= minDisSquared;

                    // Default is uniform probability distribution
                    cp.cellPopDist = new CellPopUniform(extents, minDisSquared, cp);
                    cp.cellPopDist.Initialize();
                    // Causes a new random seed for the random source
                    // Otherwise we will get the same values every time if this is followed by Apply()
                    cp.cellPopDist.Reset();
                    scenario.cellpopulations.Add(cp);

                    //add rendering options to scenario
                    (MainWindow.SOP.Protocol.scenario as TissueScenario).popOptions.AddRenderOptions(cp.renderLabel, cp.Cell.CellName, true);

                    //This is needed because without it, a new cell pop was showing black square to the left.
                    MainWindow.SOP.SelectedRenderSkin.AddRenderCell(cp.renderLabel, cp.Cell.CellName);

                    CellPopsListBox.SelectedIndex = CellPopsListBox.Items.Count - 1;
                    cb.SelectedIndex = 0;
                }

                
                MainWindow.SOP.SelectedRenderSkin.AddRenderCell(cp.renderLabel, cp.Cell.CellName);
            }
            //user picked existing cell type 
            else
            {
                if (cp == null)
                    return;

                string curr_cell_pop_name = cp.cellpopulation_name;
                string curr_cell_type_guid = "";
                curr_cell_type_guid = cp.Cell.entity_guid;

                ConfigCell cell_to_clone = MainWindow.SOP.Protocol.entity_repository.cells[nIndex];
                //thid entity_guid will already be different, since "cell" in cellpopulation is an instance
                //of configCell, it will has its own entity_guid - only the name stays the same ---
                if (cell_to_clone.entity_guid != curr_cell_type_guid)
                {
                    cp.Cell = cell_to_clone.Clone(true);

                    string new_cell_name = MainWindow.SOP.Protocol.entity_repository.cells[nIndex].CellName;
                    if (curr_cell_type_guid != cp.Cell.entity_guid) 
                    {
                        cp.cellpopulation_name = new_cell_name;
                    }
                }
            }

            //This forces the cell population to be reloaded and updates all details in GUI underneath
            int index = CellPopsListBox.SelectedIndex;
            CellPopsListBox.SelectedIndex = -1;
            CellPopsListBox.SelectedIndex = index;
        }

        private void cellPopsListBoxSelChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = sender as ListBox;
            if (lb.SelectedIndex < 0)
            {
                SelectedCell = null;
                return;
            }
            
            CellPopulation cp = (CellPopulation)(lb.SelectedItem);
            SelectedCell = cp.Cell;
        }

        private void DeleteGaussianSpecification(CellPopDistribution dist)
        {
            if (dist.DistType != CellPopDistributionType.Gaussian)
                return;

            CellPopGaussian cpg = dist as CellPopGaussian;

            if (cpg.gauss_spec == null || cpg.gauss_spec.box_spec == null)
            {
                return;
            }

            if (((VTKFullGraphicsController)MainWindow.GC).Regions.ContainsKey(cpg.gauss_spec.box_spec.box_guid) == true)
            {
                ((VTKFullGraphicsController)MainWindow.GC).RemoveRegionWidget(cpg.gauss_spec.box_spec.box_guid);
            }
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
            bool anyChange = cp.cellPopDist.CheckPositions();

            if (anyChange == true)
            {
                MainWindow.ToolWin.Apply();
            }

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
                // gmk: Do we ever get here?
                string s = (string)Clipboard.GetData(DataFormats.Text);

                char[] delim = { '\t', '\r', '\n' };
                string[] paste = s.Split(delim, StringSplitOptions.RemoveEmptyEntries);

                cp.CellStates.Clear();
                int n = 3 * (int)Math.Floor(paste.Length / 3.0);
                for (int i = 0; i < n; i += 3)
                {
                    cp.CellStates.Add(new CellState(double.Parse(paste[i]), double.Parse(paste[i + 1]), double.Parse(paste[i + 2]) ));
                }
                cp.number = cp.CellStates.Count;

            }
        }

        /// <summary>
        ///  Called when user selects "Specific" for cell population locations.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dgLocations_Unloaded(Object sender, RoutedEventArgs e)
        {
        }

        private void menuCoordinatesPaste_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Replace all cell positions with content of clipboard (No)?","Question",MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            CellPopulation cp = (CellPopulation)CellPopsListBox.SelectedItem;
            if (cp == null)
                return;

            string s = (string)Clipboard.GetData(DataFormats.Text);
            if (s == null)
                return;

            char[] delim = { '\t', '\r', '\n' };
            string[] paste = s.Split(delim, StringSplitOptions.RemoveEmptyEntries);
            if (paste == null)
                return;

            bool invalid = false;
            int num_invalid = 0;
            double x, y, z;

            cp.CellStates.Clear();
            int n = 3 * (int)Math.Floor(paste.Length / 3.0);
            for (int i = 0; i < n; i += 3)
            {
                if (Double.TryParse(paste[i], out x) && Double.TryParse(paste[i + 1], out y) && Double.TryParse(paste[i + 2], out z))
                {
                    if (cp.cellPopDist.AddByPosition(new double[] { x, y, z }) == false)
                    {
                        invalid = true;
                        num_invalid++;
                    }
                }
                else
                {
                    invalid = true;
                    num_invalid++;
                }
            }

            cp.number = cp.CellStates.Count;

            if (invalid == true)
            {
                MessageBox.Show("Ignored " + num_invalid.ToString() + " invalid cell coordinates.");
            }

            MainWindow.ToolWin.Apply();
        }

        private void menuCoordinatesTester_Click(object sender, RoutedEventArgs e)
        {
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
                // Causes a new random seed for the random source
                // Otherwise we will get the same values every time if this is followed by Apply()
                cp.cellPopDist.Reset();
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

        //This method is called when the user clicks the Remove Cell button
        private void RemoveCellPopButton_Click(object sender, RoutedEventArgs e)
        {
            TissueScenario scenario = (TissueScenario)MainWindow.SOP.Protocol.scenario;

            int index = CellPopsListBox.SelectedIndex;
            CellPopulation current_item = (CellPopulation)CellPopsListBox.SelectedItem;

            if (current_item == null)
                return;

            MessageBoxResult res;
            res = MessageBox.Show("Are you sure you would like to remove this cell population?", "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;

            //Remove gaussian spec if any
            if (current_item.cellPopDist.DistType == CellPopDistributionType.Gaussian)
            {
                DeleteGaussianSpecification(current_item.cellPopDist);
                CellPopGaussian cpg = current_item.cellPopDist as CellPopGaussian;
                cpg.gauss_spec = null;
            }
            ((VTKFullGraphicsController)MainWindow.GC).RWC.Invalidate();

            //Remove the cell population
            scenario.cellpopulations.Remove(current_item);

            //remove rendering option if no other refernece
            string label = current_item.renderLabel;
            bool safe_to_remove = (MainWindow.SOP.Protocol.scenario as TissueScenario).RenderPopReferenceCount(label, true) == 0;
            if (safe_to_remove)
            {
                (MainWindow.SOP.Protocol.scenario as TissueScenario).popOptions.RemoveRenderOptions(label, true);
            }

            CellPopsListBox.SelectedIndex = index;

            if (index >= CellPopsListBox.Items.Count)
                CellPopsListBox.SelectedIndex = CellPopsListBox.Items.Count - 1;

            if (CellPopsListBox.Items.Count == 0)
                CellPopsListBox.SelectedIndex = -1;

            // gmk - Remove this cell population from the render skin editor?

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CellPopulation cp = (CellPopulation)(CellPopsListBox.SelectedItem);
            if (cp == null)
            {
                if (CellPopsListBox.Items.Count > 0)
                {
                    cp = (CellPopulation)CellPopsListBox.Items[0];
                    SelectedCell = cp.Cell;
                    CellPopsListBox.SelectedIndex = 0;
                    CellPopsListBox.SelectedItem = cp;
                }
                else
                {
                    SelectedCell = null;
                }
            }
            else
            {
                SelectedCell = cp.Cell;
            }
        }
    }


    public class CellPopDistTypeTooltipConverter : IValueConverter
    {
        // NOTE: This method is a bit fragile since the list of strings needs to 
        // correspond in length and index with the GlobalParameterType enum...
        private List<string> tooltip_strings = new List<string>()
        {
            "Specify: User can assign specific cell locations.",
            "Uniform: Cells will be evenly distributed.",
            "Gaussian: Uses a Gaussian algorithm to place the cells."
        };

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value as string == "") return value;
            if (value == null) 
                return value as string;

            try
            {
                int n = (int)value;
                return tooltip_strings[(int)value];
            }
            catch
            {
                return "";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string str = (string)value;
            int idx = tooltip_strings.FindIndex(item => item == str);
            return (CellPopDistributionType)Enum.ToObject(typeof(CellPopDistributionType), (int)idx);
        }
    }

}
