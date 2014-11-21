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
using System.Collections.ObjectModel;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for ToolWinVatRC.xaml
    /// </summary>
    public partial class ToolWinVatRC : ToolWinBase
    {
        public ToolWinVatRC()
        {
            TitleText = "Vat Reaction Complex";
            ToroidalVisibility = Visibility.Collapsed;
            SimRepetitionVisibility = Visibility.Hidden;
            // Shouldn't have to set this, since extents control isn't used here but...
            ZExtentVisibility = Visibility.Hidden;

            InitializeComponent();
            
            DataContext = this;
            
        }

        public override void Apply()
        {
            TabItem selectedTab = toolWinVatRC.ConfigTabControl.SelectedItem as TabItem;

            int reportVatMolSelectedIndex = -1;
            int vatRCSelectedIndex = -1;
            if (selectedTab == toolWinVatRC.tabReactionComplexes)
            {
                vatRCSelectedIndex = RCControl.ListBoxReactionComplexes.SelectedIndex;
            }
            else if (selectedTab == tabReports)
            {
                reportVatMolSelectedIndex = dgVatMols.SelectedIndex;
            }

            MW.Apply();

            toolWinVatRC.ConfigTabControl.SelectedItem = selectedTab;
            if (selectedTab == toolWinVatRC.tabReactionComplexes)
            {
                RCControl.ListBoxReactionComplexes.SelectedIndex = vatRCSelectedIndex;
            }
            else if (selectedTab == toolWinVatRC.tabReports)
            {
                dgVatMols.SelectedIndex = reportVatMolSelectedIndex;
            }

        }

        private void ButtonSaveRCToProtocol_Click(object sender, RoutedEventArgs e)
        {
            ConfigReactionComplex crc_curr = (ConfigReactionComplex)(RCControl.ListBoxReactionComplexes.SelectedItem);
            ConfigReactionComplex crc_new = crc_curr.Clone(true);
            MainWindow.GenericPush(crc_new);
        }

        protected override bool CellHasMolecule(string molguid, bool isMembrane, ConfigCell cell)
        {
            throw new Exception("VatReactionComplex does not implement CellHasMolecule method.");
        }

        public override bool CellPopsHaveMolecule(string molguid, bool isMembrane)
        {
            throw new Exception("VatReactionComplex does not implement CellPopsHaveMolecule method.");
        }

        public override void RegionFocusToGUISection(RegionWidget rw, bool transferMatrix)
        {
            throw new Exception("VatReactionComplex does not implement RegionFocusToGUISection method.");
        }

        public ConfigReactionComplex GetSelectedReactionComplex()
        {
            return RCControl.GetSelectedReactionComplex();
        }

        protected override void ReportsTabItem_Loaded(object sender, RoutedEventArgs e)
        {
            ((VatReactionComplexScenario)Protocol.scenario).InitializeAllMols();
            dgVatMols.ItemsSource = ((VatReactionComplexScenario)Protocol.scenario).AllMols;
        }

        public override void GUIUpdate(bool finished)
        {
            if (finished)
            {
                MW.ReacComplexChartWindow.Tag = MainWindow.Sim;
                MW.ReacComplexChartWindow.MW = MW;

                //MW.ReacComplexChartWindow.DataContext = GetSelectedReactionComplex();
                MW.ReacComplexChartWindow.DataContext = this.Protocol;

                //Protocol p = this.Protocol;
                //VatReactionComplexScenario s = p.scenario as VatReactionComplexScenario;
                //s.AllMols;   
             
                MW.ReacComplexChartWindow.Activate();
                MW.ReacComplexChartWindow.Render();
                
            }
        }

        public override void LockSaveStartSim()
        {
        }

        /// <summary>
        /// VatRC skips the dialog for saving the Protocol because of the interactive real-time controls.
        /// gmk - Still need to workout mechanisms for reminding users to save Protocols before exiting.
        /// </summary>
        /// <returns></returns>
        public override MessageBoxResult ScenarioContentChanged()
        {
            return MessageBoxResult.None;
        }

        private void GenerateReport_ButtonClick(object sender, RoutedEventArgs e)
        {
            ((VatReactionComplexReporter)MainWindow.Sim.Reporter).reportOn = true;
            MW.runButton_Click(null, null);
        }

    }
}
