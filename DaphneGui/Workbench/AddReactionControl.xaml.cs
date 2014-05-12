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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for AddReactionControl.xaml
    /// </summary>
    public partial class AddReactionControl : UserControl
    {
        public AddReactionControl()
        {
            InitializeComponent();
            string[] wordList =
                this.FindResource("WordList") as string[];

            ICollectionView view =
                CollectionViewSource.GetDefaultView(wordList);

            new TextSearchFilter(view, this.txtSearch);
        }

        private void btnReac_Click(object sender, RoutedEventArgs e)
        {
            if (lbMol.SelectedItems.Count == 0)
                return;

            string reac = "";
            if (txtReac.Text.Length > 0)
                reac = " + ";
            foreach (string s in lbMol.SelectedItems)
            {
                reac += s;
                reac += " + ";
            }
            reac = reac.Substring(0, reac.Length - 3);

            txtReac.Text = txtReac.Text + reac;
        }

        private void btnProd_Click(object sender, RoutedEventArgs e)
        {
            if (lbMol.SelectedItems.Count == 0)
                return;

            string prod = "";
            if (txtProd.Text.Length > 0)
                prod = " + ";
            foreach (string s in lbMol.SelectedItems)
            {
                prod += s;
                prod += " + ";
            }
            prod = prod.Substring(0, prod.Length - 3);

            txtProd.Text = txtProd.Text + prod;
        }

        private void btnUnselect_Click(object sender, RoutedEventArgs e)
        {
            lbMol.UnselectAll();
        }

        private void btnRClear_Click(object sender, RoutedEventArgs e)
        {
            txtReac.Text = "";
        }

        private void btnPClear_Click(object sender, RoutedEventArgs e)
        {
            txtProd.Text = "";
        }
    }
}
