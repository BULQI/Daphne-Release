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
            ToroidalVisibility = Visibility.Hidden;
            SimRepetitionVisibility = Visibility.Hidden;
            InitializeComponent();
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

            int nRepEcmMolSelIndex = -1;
            int nRepCellSelIndex = -1;
            int nRepCellPopSelIndex = -1;
            // gmk - fix after this functionality is added
            //if (selectedTab == toolWinTissuetabReports)
            //{
            //    nRepEcmMolSelIndex = reportControl.dgEcmMols.SelectedIndex;
            //    nRepEcmMolSelIndex = toolWinTissue.dgEcmMols.SelectedIndex;
            //    nRepCellSelIndex = toolWinTissue.dgCellDetails.SelectedIndex;
            //    nRepCellPopSelIndex = toolWinTissue.lbRptCellPops.SelectedIndex;
            //}


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
                //toolWinTissue.dgEcmMols.SelectedIndex = nRepEcmMolSelIndex;
                //toolWinTissue.dgCellDetails.SelectedIndex = nRepCellSelIndex;
                //toolWinTissue.lbRptCellPops.SelectedIndex = nRepCellPopSelIndex;
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

    }
}
