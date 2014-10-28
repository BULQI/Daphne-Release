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

            InitializeComponent();
            DataContext = this;
        }

        public override void Apply()
        {
            TabItem selectedTab = toolWinTissue.ConfigTabControl.SelectedItem as TabItem;

            int nCellPopSelIndex = -1;
            if (selectedTab == toolWinTissue.tabCellPop)
            {
                // gmk - fix after this functionality is added
                // nCellPopSelIndex = toolWinTissue.CellPopsListBox.SelectedIndex;
            }

            int nMolPopSelIndex = -1;
            if (selectedTab == toolWinTissue.tabECM)
            {
                // gmk - fix after this functionality is added
                // nMolPopSelIndex = toolWinTissue.lbEcsMolPops.SelectedIndex;
            }

            int reportECMmolSelectedIndex = -1;
            int reportCellSelectedIndex = -1;
            int reportCellPopSelectedIndex = -1;
            // gmk - fix after this functionality is added
            if (selectedTab == tabReports)
            {
                reportECMmolSelectedIndex = dgEcmMols.SelectedIndex;
                //reportCellSelectedIndex = toolWinTissue.dgCellDetails.SelectedIndex;
                //reportCellPopSelectedIndex = toolWinTissue.lbRptCellPops.SelectedIndex;
            }


            MW.Apply();

            toolWinTissue.ConfigTabControl.SelectedItem = selectedTab;
            if (selectedTab == toolWinTissue.tabCellPop)
            {
                // gmk - fix after this functionality is added
                //toolWinTissue.CellPopsListBox.SelectedIndex = nCellPopSelIndex;
            }
            else if (selectedTab == toolWinTissue.tabECM)
            {
                // gmk - fix after this functionality is added
                //toolWinTissue.lbEcsMolPops.SelectedIndex = nMolPopSelIndex;
            }
            else if (selectedTab == toolWinTissue.tabReports)
            {
                // gmk - fix after this functionality is added
                dgEcmMols.SelectedIndex = reportECMmolSelectedIndex;
                //toolWinTissue.dgCellDetails.SelectedIndex = reportCellSelectedIndex;
                //toolWinTissue.lbRptCellPops.SelectedIndex = reportCellPopSelectedIndex;
            }
        }


        protected override void ConfigTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (tabECM.IsSelected == true)
            //{
            //    // gmk - fix after this functionality is added
            //    //if (lvAvailableReacs.ItemsSource != null)
            //    //{
            //    //    CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();
            //    //}
            //}
        }


    }
}
