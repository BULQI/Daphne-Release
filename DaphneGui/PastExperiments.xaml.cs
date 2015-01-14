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
using System.Collections.ObjectModel;
using System.Data;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for PastExperiments.xaml
    /// </summary>
    public partial class PastExperiments : Window
    {
        public int SelectedExperiment { get; set; }
        public ObservableCollection<string> ExpNames { get; set; }

        public PastExperiments(List<string> enames)
        {
            InitializeComponent();
            ExpNames = new ObservableCollection<string>();

            foreach (string s in enames) {
                ExpNames.Add(s);
            }

            SelectedExperiment = -1;
            DataContext = this;
        }

        private void ButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            SelectedExperiment = ExpName_CB.SelectedIndex;
            DialogResult = true;
        }

        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            SelectedExperiment = ExpName_CB.SelectedIndex;
            if (SelectedExperiment != -1) {
                string SelExpName = ExpNames[SelectedExperiment];
                MessageBoxResult result = MessageBox.Show("Really delete " + SelExpName + "?", "Confirm delete", MessageBoxButton.YesNo,MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes) {
                    DialogResult = false;
                }
            }
        }

        private void ExpName_CB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            SelectedExperiment = -1;
            DialogResult = false;
        }
    }

}
