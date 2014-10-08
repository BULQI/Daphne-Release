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

namespace DaphneGui.Pushing
{
    /// <summary>
    /// Interaction logic for PushGene.xaml
    /// </summary>
    public partial class PushGene : Window
    {
        public PushGene()
        {
            InitializeComponent();
        }

        private void GeneSaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void GeneCancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
