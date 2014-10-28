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
            ToroidalVisibility = Visibility.Hidden;
            SimRepetitionVisibility = Visibility.Hidden;

            InitializeComponent();
            DataContext = this;
        }

        public override void Apply()
        {
            TabItem selectedTab = toolWinVatRC.ConfigTabControl.SelectedItem as TabItem;

            int reportVatMolSelectedIndex = -1;
            if (selectedTab == tabReports)
            {
                //reportVatMolSelectedIndex = ((DataGrid)Vat_MoleculeSettings.Content).SelectedIndex;
            }


            MW.Apply();

            toolWinVatRC.ConfigTabControl.SelectedItem = selectedTab;
            if (selectedTab == toolWinVatRC.tabReactionComplexes)
            {
                // gmk - add code to reset the selected RC
            }
            else if (selectedTab == toolWinVatRC.tabReports)
            {
                //((DataGrid)Vat_MoleculeSettings.Content).SelectedIndex = reportVatMolSelectedIndex = -1;
            }

        }

        protected override bool CellHasMolecule(string molguid, bool isMembrane, ConfigCell cell)
        {
            throw new Exception("VatReactionComplex does not implement CellHasMolecule method.");
        }

        protected override bool CellPopsHaveMolecule(string molguid, bool isMembrane)
        {
            throw new Exception("VatReactionComplex does not implement CellPopsHaveMolecule method.");
        }

        protected virtual void ecmAvailableReactionsListView_Filter(object sender, FilterEventArgs e)
        {
            throw new Exception("VatReactionComplex does not implement ecmAvailableReactionsListView_Filter method.");
        }


        public override void RegionFocusToGUISection(RegionWidget rw, bool transferMatrix)
        {
            throw new Exception("VatReactionComplex does not implement RegionFocusToGUISection method.");
        }

        public virtual void SelectGaussSpecInGUI(int index, string guid)
        {
            throw new Exception("VatReactionComplex does not implement SelectGaussSpecInGUI method.");
        }

        public override void SelectMolpopInGUI(int index)
        {
            throw new Exception("VatReactionComplex does not implement SelectMolpopInGUI method.");
        }


    }
}
