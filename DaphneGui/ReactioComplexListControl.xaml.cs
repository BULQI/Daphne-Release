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

using Daphne;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for ReactioComplexListControl.xaml
    /// </summary>
    public partial class ReactioComplexListControl : UserControl
    {
        public ReactioComplexListControl()
        {
            InitializeComponent();
        }

        protected void btnAddReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            //AddReacComplex arc = new AddReacComplex(ReactionComplexDialogType.AddComplex);
            //if (arc.ShowDialog() == true)
            //    lbComplexes.SelectedIndex = lbComplexes.Items.Count - 1;
        }

        protected void btnCopyReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            if (lbComplexes.SelectedIndex < 0)
            {
                MessageBox.Show("Select a reaction complex to copy from.");
                return;
            }

            ConfigReactionComplex crcCurr = (ConfigReactionComplex)lbComplexes.SelectedItem;
            ConfigReactionComplex crcNew = crcCurr.Clone(false);

            Level level = this.DataContext as Level;
            level.entity_repository.reaction_complexes.Add(crcNew);
            //MainWindow.SOP.Protocol.entity_repository.reaction_complexes.Add(crcNew);

            lbComplexes.SelectedIndex = lbComplexes.Items.Count - 1;
        }

        protected void btnEditReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            ConfigReactionComplex crc = (ConfigReactionComplex)lbComplexes.SelectedItem;
            
            if (crc == null)
                return;

            AddReacComplex arc = new AddReacComplex(ReactionComplexDialogType.EditComplex, crc);
            arc.ShowDialog();
        }

        protected void btnRemoveReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            ConfigReactionComplex crc = (ConfigReactionComplex)(lbComplexes.SelectedItem);
            if (crc != null)
            {
                MessageBoxResult res;
                res = MessageBox.Show("Are you sure you would like to remove this reaction complex?", "Warning", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                    return;

                int index = lbComplexes.SelectedIndex;

                Level level = this.DataContext as Level;
                level.entity_repository.reaction_complexes.Remove(crc);

                //                MainWindow.SOP.Protocol.entity_repository.reaction_complexes.Remove(crc);

                lbComplexes.SelectedIndex = index;

                if (index >= lbComplexes.Items.Count)
                    lbComplexes.SelectedIndex = lbComplexes.Items.Count - 1;

                if (lbComplexes.Items.Count == 0)
                    lbComplexes.SelectedIndex = -1;

            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }


    }
}
