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

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for AddDiffState.xaml
    /// </summary>
    public partial class AddDiffState : Window
    {
        public string StateName { get; set; }

        public AddDiffState()
        {
            InitializeComponent();
            StateName = "";
            DataContext = this;
        }

       
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
