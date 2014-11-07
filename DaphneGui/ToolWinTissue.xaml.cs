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
using System.Windows.Shapes;

using Daphne;
using ActiproSoftware.Windows.Controls.Docking;

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
                // gmk - fix after this functionality is added
                //if (lvAvailableReacs.ItemsSource != null)
                //{
                //    CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();
                //}
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

    }
}
