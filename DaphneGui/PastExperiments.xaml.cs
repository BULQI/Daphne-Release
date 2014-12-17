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
            SetUpExpComboBox();
            DataContext = this;
        }

        private void SetUpExpComboBox()
        {
            //Exps = DataBaseTools.GetExperiments();
            //ExpName_CB.IsEnabled = true;
            //ExpName_CB.IsEditable = true;
            //ExpName_CB.Items.Clear();
            //Exps.Sort((x, y) => string.Compare(x.ExpName, y.ExpName));
            //for (int i = 0; i < Exps.Count; i++)
            //{
            //    ExpName_CB.Items.Add(i.ToString() + ") " + Exps[i].ExpName);
            //}
            //ExpName_CB.Text = "select experiment";
            //ExpName_CB.SelectedIndex = -1;
            //Description_TB.Text = "";
            //for (int i = 0; i < Exps.Count; i++)
            //{
            //    if (Exps[i].ExpId == selectedexperiment)
            //    {
            //        selectedexperimentname = Exps[i].ExpName;
            //        ExpName_CB.SelectedIndex = i;
            //        Description_TB.Text = Exps[i].ExpDesc;
            //    }
            //}
        }


        private void ButtonOpen_Click(object sender, RoutedEventArgs e)
        {
            SelectedExperiment = ExpName_CB.SelectedIndex;
            DialogResult = true;
        }

        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            if (ExpName_CB.SelectedIndex != -1)
            {
                //selectedexperiment = Exps[ExpName_CB.SelectedIndex].ExpId;
                //selectedexperimentname = Exps[ExpName_CB.SelectedIndex].ExpName;
                //if (System.Windows.Forms.MessageBox.Show("Really delete " + selectedexperimentname + "?", "Confirm delete", MessageBoxButtons.YesNo) == System.Windows.Forms.DialogResult.Yes)
                //{
                //    DataBaseTools.DeleteExperiment(selectedexperiment);
                //    selectedexperiment = -1;
                //    SetUpExpComboBox();
                //}
            }
        }

        private void ExpName_CB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
