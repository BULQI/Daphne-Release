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

namespace GuiDaphneApp
{
    /// <summary>
    /// Interaction logic for AddSingleMolecule.xaml
    /// </summary>
    public partial class AddSingleMolecule : Window
    {
        public Molecule NewMolecule { get; set; }

        public AddSingleMolecule()
        {
            InitializeComponent();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            string name = txtMolName.Text;
            name = name.Trim();
            name = name.TrimStart('1', '2', '3', '4', '5', '6', '7', '8', '9', '0');
                        
            double wt = double.Parse(txtMolWt.Text);
            double rd = (double)txtRadius.Value;
            double diff = 1.0;
            NewMolecule = new Molecule(name, wt, rd, diff);

            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
