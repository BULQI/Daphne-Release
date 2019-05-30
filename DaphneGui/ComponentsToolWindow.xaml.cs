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
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ActiproSoftware.Windows.Controls.Docking;

using Daphne;
using System.Collections.ObjectModel;
using Microsoft.Win32;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Linq;
using System.Windows.Data;
using System.ComponentModel;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for ComponentsToolWindow.xaml
    /// </summary>
    public partial class ComponentsToolWindow : ToolWindow
    {
        public MainWindow MW { get; set; }

        public Level CurrentLevel { get; set; }

        public ComponentsToolWindow()
        {
            InitializeComponent();
        }

        private void btnRemoveGene_Click(object sender, RoutedEventArgs e)
        {
            ConfigGene gene = (ConfigGene)dgLibGenes.SelectedValue;
            MessageBoxResult res;

            res = MessageBox.Show("Are you sure you would like to remove this gene?", "Warning", MessageBoxButton.YesNo);

            if (res == MessageBoxResult.No)
                return;

            int index = dgLibGenes.SelectedIndex;
            Level level = (Level)(this.DataContext);
            level.entity_repository.genes.Remove(gene);
            dgLibGenes.SelectedIndex = index;

            if (index >= dgLibGenes.Items.Count)
                dgLibGenes.SelectedIndex = dgLibGenes.Items.Count - 1;

            if (dgLibGenes.Items.Count == 0)
                dgLibGenes.SelectedIndex = -1;

        }

        private void btnCopyGene_Click(object sender, RoutedEventArgs e)
        {
            ConfigGene gene = (ConfigGene)dgLibGenes.SelectedItem;

            if (gene == null)
                return;

            Level level = DataContext as Level;
            // ConfigGene newgene = gene.Clone(MainWindow.SOP.Protocol);
            ConfigGene newgene = gene.Clone(level);

            level.entity_repository.genes.Add(newgene);
            
            dgLibGenes.SelectedItem = newgene;
            dgLibGenes.ScrollIntoView(newgene);
        }

        private void btnAddGene_Click(object sender, RoutedEventArgs e)
        {
            Level level = (Level)(this.DataContext);
            ConfigGene gm = new ConfigGene("g", 2, 1);
            gm.Name = gm.GenerateNewName(level, "New");
            
            level.entity_repository.genes.Add(gm);

            dgLibGenes.SelectedItem = gm;
            dgLibGenes.ScrollIntoView(gm);
        }

        //LIBRARIES TAB EVENT HANDLERS
        //MOLECULES        
        private void btnAddLibMolecule_Click(object sender, RoutedEventArgs e)
        {
            Level level = (Level)(this.DataContext);
            ConfigMolecule gm = new ConfigMolecule();
            gm.Name = gm.GenerateNewName(level, "New");

            level.entity_repository.molecules.Add(gm);
            MainWindow.SOP.SelectedRenderSkin.AddRenderMol(gm.renderLabel, gm.Name);

            dgLibMolecules.SelectedItem = gm;
            dgLibMolecules.ScrollIntoView(gm);
        }

        private void btnCopyMolecule_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecule cm = (ConfigMolecule)dgLibMolecules.SelectedItem;

            if (cm == null)
                return;

            Level level = (Level)(this.DataContext);
            ConfigMolecule newmol = cm.Clone(level);

            level.entity_repository.molecules.Add(newmol);
            MainWindow.SOP.SelectedRenderSkin.AddRenderMol(newmol.renderLabel, newmol.Name);

            //dgLibMolecules.SelectedIndex = dgLibMolecules.Items.Count - 1;

            dgLibMolecules.SelectedItem = newmol;
            dgLibMolecules.ScrollIntoView(newmol);
        }

        private void btnRemoveMolecule_Click(object sender, RoutedEventArgs e)
        {
            int index = dgLibMolecules.SelectedIndex;

            if (index < 0)
                return;

            MessageBoxResult res;

            res = MessageBox.Show("Are you sure you would like to remove this molecule?", "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;

            ConfigMolecule gm = (ConfigMolecule)dgLibMolecules.SelectedValue;
            Level level = (Level)(this.DataContext);

            try
            {
                ConfigMolecule molToRemove = level.entity_repository.molecules.First(mol => mol.entity_guid == gm.entity_guid);
                level.entity_repository.molecules.Remove(molToRemove);

                dgLibMolecules.SelectedIndex = index;
                if (index >= dgLibMolecules.Items.Count)
                    dgLibMolecules.SelectedIndex = dgLibMolecules.Items.Count - 1;

                if (dgLibMolecules.Items.Count == 0)
                    dgLibMolecules.SelectedIndex = -1;
            }
            catch
            {
            }

        }

        private void MolTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ConfigMolecule cm = dgLibMolecules.SelectedItem as ConfigMolecule;

            if (cm == null)
                return;

            Level level = this.DataContext as Level;
            cm.ValidateName(level);

            int index = dgLibMolecules.SelectedIndex;
            dgLibMolecules.InvalidateVisual();
            dgLibMolecules.Items.Refresh();
            dgLibMolecules.SelectedIndex = index;
            cm = (ConfigMolecule)dgLibMolecules.SelectedItem;
            dgLibMolecules.ScrollIntoView(cm);
        }

        private void MolLocation_Changed(object sender, RoutedEventArgs e)
        {
            MolTextBox_LostFocus(sender, e);
        }

        //LIBRARY REACTIONS EVENT HANDLERS        
        private void btnRemoveReaction_Click(object sender, RoutedEventArgs e)
        {
            ConfigReaction cr = (ConfigReaction)lvReactions.SelectedItem;
            if (cr == null)
            {
                return;
            }

            Level level = this.DataContext as Level;
            level.entity_repository.reactions.Remove(cr);

            //DO WE NEED TO REMOVE REACTION COMPLEXES THAT USE THIS REACTION?
        }

        private void GeneTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ConfigGene gene = dgLibGenes.SelectedItem as ConfigGene;

            if (gene == null)
                return;

            int index = dgLibGenes.SelectedIndex;

            Level level = this.DataContext as Level;
            if (level is Protocol) {
                Protocol p = level as Protocol;
                gene.ValidateName(p);
            }

            dgLibGenes.InvalidateVisual();

            dgLibGenes.Items.Refresh();
            dgLibGenes.SelectedIndex = index;
            gene = (ConfigGene)dgLibGenes.SelectedItem;
            dgLibGenes.ScrollIntoView(gene);

        }

        public ConfigReactionComplex GetConfigReactionComplex()
        {
            ListBox lbComplexes = RCControl.ListBoxReactionComplexes;
            if (lbComplexes.SelectedIndex < 0)
            {
                MessageBox.Show("Select a reaction complex to process.");
                return null;
            }
            return (ConfigReactionComplex)(lbComplexes.SelectedItem);
        }

        private void MyComponentsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var dc = this.DataContext;
            // gmk - fix this 
            //DataContext = MainWindow.SOP.Protocol;

            ICollectionView molview = CollectionViewSource.GetDefaultView(dgLibMolecules.ItemsSource);
            molview.SortDescriptions.Clear();
            SortDescription molSort = new SortDescription("molecule_location", ListSortDirection.Descending);
            molview.SortDescriptions.Add(molSort);
            molSort = new SortDescription("Name", ListSortDirection.Ascending);
            molview.SortDescriptions.Add(molSort);

            ICollectionView geneview = CollectionViewSource.GetDefaultView(dgLibGenes.ItemsSource);
            geneview.SortDescriptions.Clear();
            SortDescription geneSort = new SortDescription("Name", ListSortDirection.Ascending);
            geneview.SortDescriptions.Add(geneSort);
        }

        private void MoleculesExpander_Expanded(object sender, RoutedEventArgs e)
        {
            ICollectionView view = CollectionViewSource.GetDefaultView(dgLibMolecules.ItemsSource);
            view.SortDescriptions.Clear();
            SortDescription sd = new SortDescription("molecule_location", ListSortDirection.Descending);
            view.SortDescriptions.Add(sd);
            sd = new SortDescription("Name", ListSortDirection.Ascending);
            view.SortDescriptions.Add(sd);
        }

        public void Refresh()
        {
            if (MoleculesExpander.IsExpanded == true)
            {
                MoleculesExpander.IsExpanded = false;
                MoleculesExpander.IsExpanded = true;
            }

            if (GenesExpander.IsExpanded == true)
            {
                GenesExpander.IsExpanded = false;
                GenesExpander.IsExpanded = true;
            }

            if (ReactionsExpander.IsExpanded == true)
            {
                ReactionsExpander.IsExpanded = false;
                ReactionsExpander.IsExpanded = true;
            }

            if (ReacComplexExpander.IsExpanded == true)
            {
                ReacComplexExpander.IsExpanded = false;
                ReacComplexExpander.IsExpanded = true;
            }

        }

        private void dgLibGenes_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
        }

        private void GenesExpander_Expanded(object sender, RoutedEventArgs e)
        {
            ICollectionView geneview = CollectionViewSource.GetDefaultView(dgLibGenes.ItemsSource);
            geneview.SortDescriptions.Clear();
            SortDescription geneSort = new SortDescription("Name", ListSortDirection.Ascending);
            geneview.SortDescriptions.Add(geneSort);
        }

        private void ReacComplexExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }

        private void ReactionsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }

    }

    /// <summary>
    /// If a reaction complex is selected, 
    ///     return visible 
    /// else 
    ///     return collapsed
    /// </summary>
    public class selectedRCToBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            ConfigReactionComplex rc = value as ConfigReactionComplex;
            if (rc == null)
                return Visibility.Collapsed;
            else
                return Visibility.Visible;
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return false;
        }
    }
}
