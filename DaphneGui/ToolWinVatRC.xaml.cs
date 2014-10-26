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
        public ToolWinVatRC()  : base()
        {
            InitializeComponent();
            TitleText = "Vat Reaction Complex workbench";
            ToroidalVisibility = Visibility.Hidden;
            SimRepetitionVisibility = Visibility.Hidden;
        }

        public override void RegionFocusToGUISection(RegionWidget rw, bool transferMatrix)
        {
            throw new Exception("VatReactionComplex does not implement RegionFocusToGUISection method.");
        }

        public override void SelectMolpopInGUI(int index)
        {
            throw new Exception("VatReactionComplex does not implement SelectMolpopInGUI method.");
        }


    }
}
