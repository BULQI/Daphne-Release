using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows;
using System.Windows.Controls;
using ActiproSoftware.Windows.Controls.Docking;

using Daphne;

namespace DaphneGui
{
    public class ToolWinBase : ToolWindow
    {
        public MainWindow MW { get; set; }
        public string TitleText { get; set; }
        public Visibility ToroidalVisibility { get; set; }
        public Visibility SimRepetitionVisibility { get; set; }

        public ToolWinBase()
        {
            
        }

        /// <summary>
        /// Functionality to preserve focus when the Apply button is clicked.
        /// The base implementation does not preserve focus. 
        /// </summary>
        public virtual void Apply()
        {
            MW.Apply();
        }


        /// <summary>
        /// Functionality to refresh elements when the selected tab changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ConfigTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (tabECM.IsSelected == true)
            //{
            //    if (lvAvailableReacs.ItemsSource != null)
            //        CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();
            //}
        }

        public virtual void RegionFocusToGUISection(RegionWidget rw, bool transferMatrix)
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
                    // Select the correct region/solfac/gauss_spec in the GUI's lists
                    bool gui_spot_found = false;

                    if (!gui_spot_found)
                    {
                        // Next check whether any Solfacs use this right gaussian_spec for this box
                        for (int r = 0; r < MainWindow.SOP.Protocol.scenario.environment.comp.molpops.Count; r++)
                        {
                            // We'll just be picking the first one that uses 
                            if (MainWindow.SOP.Protocol.scenario.environment.comp.molpops[r].mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian &&
                                ((MolPopGaussian)MainWindow.SOP.Protocol.scenario.environment.comp.molpops[r].mp_distribution).gauss_spec.box_spec.box_guid == key)
                            {
                                SelectMolpopInGUI(r);
                                //gui_spot_found = true;
                                break;
                            }
                        }
                    }
                    if (!gui_spot_found)
                    {
                        GaussianSpecification next;
                        int count = 0;

                        MainWindow.SOP.Protocol.scenario.resetGaussRetrieve();
                        while ((next = MainWindow.SOP.Protocol.scenario.nextGaussSpec()) != null)
                        {
                            if (next.box_spec.box_guid == key)
                            {
                                SelectGaussSpecInGUI(count, key);
                                //gui_spot_found = true;
                                break;
                            }
                            count++;
                        }
                    }
                }
            }
        }

        protected virtual void Save_Selected_Tab()
        {
            // Called by MainWindow.resetButton_Click
            //Code to preserve focus to the element that was in focus before "Apply" button clicked.

            //TabItem selectedTab = ConfigTabControl.SelectedItem as TabItem;

            //int nCellPopSelIndex = -1;
            //if (selectedTab == ProtocolToolWindow.tabCellPop)
            //{
            //    nCellPopSelIndex = ProtocolToolWindow.CellPopsListBox.SelectedIndex;
            //}

            //int nMolPopSelIndex = -1;
            //if (selectedTab == ProtocolToolWindow.tabECM)
            //{
            //    nMolPopSelIndex = ProtocolToolWindow.lbEcsMolPops.SelectedIndex;
            //}
        }

        public void SelectGaussSpecInGUI(int index, string guid)
        {
            // this window seems to implement the tissue scenario gui; throw an exception for now to enforce that;
            // Sanjeev, you probably need to have a hierachy of tool windows where each implements the gui for one case,
            // but I don't know for sure; we can discuss
            if (MainWindow.SOP.Protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
            {
                throw new InvalidCastException();
            }

            TissueScenario scenario = (TissueScenario)MainWindow.SOP.Protocol.scenario;
            bool isBoxInCell = false;

            foreach (CellPopulation cp in scenario.cellpopulations)
            {
                CellPopDistribution cpd = cp.cellPopDist;

                if (cpd.DistType == CellPopDistributionType.Gaussian)
                {
                    CellPopGaussian cpg = cpd as CellPopGaussian;

                    if (cpg.gauss_spec.box_spec.box_guid == guid)
                    {
                        //cpg.Reset();
                        isBoxInCell = true;
                        break;
                    }
                }
            }

            // gmk - uncomment
            if (isBoxInCell == true)
            {
            //    MW.ConfigTabControl.SelectedItem = tabCellPop;
            //}
            //else
            //{
            //    MW.ConfigTabControl.SelectedItem = tabECM;
            }
        }

        public virtual void SelectMolpopInGUI(int index)
        {
            // gmk - uncomment and fix
            //lbEcsMolPops.SelectedIndex = index;
        }

        protected virtual void ReportsTabItem_Loaded(object sender, RoutedEventArgs e)
        {
            // gmk - uncomment and fix
            //lbRptCellPops.SelectedIndex = 0;
            //ICollectionView icv = CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource);
            //if (icv != null)
            //{
            //    icv.Refresh();
            //}
        }


    }
}
