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
    /// Interaction logic for ReactionComplexesInStore.xaml
    /// </summary>
    public partial class ReactionComplexesInStore : Window
    {
        public ReactionComplexesInStore()
        {
            InitializeComponent();
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            if (ListBoxReactionComplexes.Items.Count == 0)
            {
                MessageBox.Show("There are no reaction complexes in the Protocol store.", "None available", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (ListBoxReactionComplexes.SelectedItems.Count == 0)
            {
                MessageBox.Show("Please select a reaction complex to add to the scenario.", "No items selected", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            DialogResult = true;
            foreach (var item in ListBoxReactionComplexes.SelectedItems)
            {
                ConfigReactionComplex crc = (ConfigReactionComplex)item;
                ConfigCompartment comp = Tag as ConfigCompartment;

                if (comp.reaction_complexes_dict.ContainsKey(crc.entity_guid) == false)
                    comp.reaction_complexes.Add(crc.Clone(true));
            }
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void availableReactionComplexesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigReactionComplex crc = e.Item as ConfigReactionComplex;
            ConfigCompartment comp = Tag as ConfigCompartment;

            if (comp.reaction_complexes_dict.ContainsKey(crc.entity_guid))
                e.Accepted = false;
            else 
                e.Accepted = true;
        }

    }
}
