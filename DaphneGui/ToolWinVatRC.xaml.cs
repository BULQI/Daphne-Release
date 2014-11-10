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
            ToroidalVisibility = Visibility.Hidden;
            SimRepetitionVisibility = Visibility.Hidden;
            // Shouldn't have to set this, since extents control isn't used here but...
            ZExtentVisibility = Visibility.Hidden;
            //GetMolsInAllRCs(Protocol.scenario.environment.comp);

            InitializeComponent();
            
            DataContext = this;
            
        }

        protected override void AddGaussianSpecification(GaussianSpecification gg, BoxSpecification box)
        {
            throw new Exception("VatReactionComplex does not implement AddGaussianSpecification method.");
        }

        public override void Apply()
        {
            TabItem selectedTab = toolWinVatRC.ConfigTabControl.SelectedItem as TabItem;

            int reportVatMolSelectedIndex = -1;
            if (selectedTab == tabReports)
            {
                reportVatMolSelectedIndex = vatControl.dgVatMols.SelectedIndex;
            }

            MW.Apply();

            toolWinVatRC.ConfigTabControl.SelectedItem = selectedTab;
            if (selectedTab == toolWinVatRC.tabReactionComplexes)
            {
                // gmk - add code to reset the selected RC
            }
            else if (selectedTab == toolWinVatRC.tabReports)
            {
                vatControl.dgVatMols.SelectedIndex = reportVatMolSelectedIndex;
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

        protected override void ecmAvailableReactionsListView_Filter(object sender, FilterEventArgs e)
        {
            throw new Exception("VatReactionComplex does not implement ecmAvailableReactionsListView_Filter method.");
        }


        public override void RegionFocusToGUISection(RegionWidget rw, bool transferMatrix)
        {
            throw new Exception("VatReactionComplex does not implement RegionFocusToGUISection method.");
        }

        public ConfigReactionComplex GetSelectedReactionComplex()
        {
            return RCControl.GetSelectedReactionComplex();
        }
        
    }
}
