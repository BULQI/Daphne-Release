/*
Copyright (C) 2019 Kepler Laboratory of Quantitative Immunology

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY 
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
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
            
            foreach (var item in ListBoxReactionComplexes.SelectedItems)
            {
                ConfigReactionComplex crc = (ConfigReactionComplex)item;
                ConfigCompartment comp = Tag as ConfigCompartment;

                if (comp.reaction_complexes_dict.ContainsKey(crc.entity_guid) == false)
                    comp.reaction_complexes.Add(crc.Clone(true));
            }
            DialogResult = true;
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
