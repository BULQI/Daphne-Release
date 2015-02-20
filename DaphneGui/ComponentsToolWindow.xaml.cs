using System;
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
            ConfigGene gm = new ConfigGene("g", 0, 0);
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
            gm.Name = gm.GenerateNewName(level, "_New");

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
            if (level is Protocol)
            {
                Protocol p = level as Protocol;
                cm.ValidateName(p);
            }

            int index = dgLibMolecules.SelectedIndex;
            dgLibMolecules.InvalidateVisual();
            dgLibMolecules.Items.Refresh();
            dgLibMolecules.SelectedIndex = index;
            cm = (ConfigMolecule)dgLibMolecules.SelectedItem;
            dgLibMolecules.ScrollIntoView(cm);
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

        //LIBRARIES REACTION COMPLEXES HANDLERS

        private void btnCopyReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            ListBox lbComplexes = RCControl.ListBoxReactionComplexes;
            if (lbComplexes.SelectedIndex < 0)
            {
                MessageBox.Show("Select a reaction complex to copy from.");
                return;
            }

            ConfigReactionComplex crcCurr = (ConfigReactionComplex)lbComplexes.SelectedItem;
            ConfigReactionComplex crcNew = crcCurr.Clone(false);

            Level level = this.DataContext as Level;
            level.entity_repository.reaction_complexes.Add(crcNew);

            lbComplexes.SelectedIndex = lbComplexes.Items.Count - 1;
        }

        private void btnAddReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            ListBox lbComplexes = RCControl.ListBoxReactionComplexes;
            NewEditReacComplex arc = new NewEditReacComplex(ReactionComplexDialogType.NewComplex, null);
            if (arc.ShowDialog() == true)
                lbComplexes.SelectedIndex = lbComplexes.Items.Count - 1;
        }

        private void btnEditReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            ListBox lbComplexes = RCControl.ListBoxReactionComplexes;
            ConfigReactionComplex crc = (ConfigReactionComplex)lbComplexes.SelectedItem;
            if (crc == null)
                return;

            NewEditReacComplex arc = new NewEditReacComplex(ReactionComplexDialogType.EditComplex, crc, null);
            arc.ShowDialog();

        }

        private void btnRemoveReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            ListBox lbComplexes = RCControl.ListBoxReactionComplexes;
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

                lbComplexes.SelectedIndex = index;

                if (index >= lbComplexes.Items.Count)
                    lbComplexes.SelectedIndex = lbComplexes.Items.Count - 1;

                if (lbComplexes.Items.Count == 0)
                    lbComplexes.SelectedIndex = -1;

            }
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

        //private void MolTextBox_LostFocus(object sender, RoutedEventArgs e)
        //{
        //    ConfigMolecule cm = dgLibMolecules.SelectedItem as ConfigMolecule;

        //    if (cm == null)
        //        return;

        //    Level level = this.DataContext as Level;
        //    if (level is Protocol)
        //    {
        //        Protocol p = level as Protocol;
        //        cm.ValidateName(p);
        //    }

        //    MainWindow.SOP.SelectedRenderSkin.SetRenderMolName(cm.renderLabel, cm.Name);

        //    int index = dgLibMolecules.SelectedIndex;
        //    dgLibMolecules.InvalidateVisual();
        //    dgLibMolecules.Items.Refresh();
        //    dgLibMolecules.SelectedIndex = index;
        //    cm = (ConfigMolecule)dgLibMolecules.SelectedItem;
        //    dgLibMolecules.ScrollIntoView(cm);
        //}

        private void DrawSelectedReactionComplex()
        {
            ListBox lbComplexes = RCControl.ListBoxReactionComplexes;
            ConfigReactionComplex crc = (ConfigReactionComplex)(lbComplexes.SelectedItem);

            //Cleanup any previous RC stuff
            Protocol p = null;
            Level level = this.DataContext as Level;

            if (level is Protocol)
            {
                p = level as Protocol;
                foreach (ConfigCell cell in p.entity_repository.cells.ToList())
                {
                    if (cell.CellName == "RCCell")
                    {
                        p.entity_repository.cells.Remove(cell);
                    }
                }
            }

            ConfigCell cc = new ConfigCell();

            cc.CellName = "RCCell";
            foreach (ConfigMolecularPopulation cmp in crc.molpops)
            {
                cc.cytosol.molpops.Add(cmp);
            }

            foreach (ConfigReaction cr in crc.reactions)
            {
                cc.cytosol.Reactions.Add(cr.Clone(true));
            }
            p.entity_repository.cells.Add(cc);

            CellPopulation cp = new CellPopulation();
            cp.Cell = cc;
            cp.cellpopulation_name = "RC cell";
            cp.number = 1;

            MainWindow.Sim.Load(p, true);
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

        private void btnSaveReacToProtocol_Click(object sender, RoutedEventArgs e)
        {
            ListBox lbComplexes = RCControl.ListBoxReactionComplexes;
            ListView lvReacComplexReactions = RCControl.ListViewReacComplexReactions;
            ConfigReactionComplex crc = (ConfigReactionComplex)(lbComplexes.SelectedItem);

            if (crc == null)
                return;

            if (lvReacComplexReactions.SelectedItems.Count <= 0)
                return;

            ConfigReaction reac = (ConfigReaction)(lvReacComplexReactions.SelectedItem);

            ConfigReaction newreac = reac.Clone(true);
            MainWindow.GenericPush(newreac);
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

        //private Cursor _cursor;

        //private void OnResizeThumbDragStarted(object sender, DragStartedEventArgs e)
        //{
        //    _cursor = Cursor;
        //    Cursor = Cursors.SizeNWSE;
        //}

        //private void OnResizeThumbDragCompleted(object sender, DragCompletedEventArgs e)
        //{
        //    Cursor = _cursor;
        //}

        //private void OnResizeThumbDragDelta(object sender, DragDeltaEventArgs e)
        //{
        //    double yChange = e.VerticalChange;
        //    double yNew = dgLibMolecules.ActualHeight + yChange;

        //    //make sure not to resize to negative width or heigth  
        //    if (yNew < dgLibMolecules.MinHeight)
        //        yNew = dgLibMolecules.MinHeight;

        //    if (yNew > dgLibMolecules.MaxHeight)
        //        yNew = dgLibMolecules.MaxHeight;
            
        //    dgLibMolecules.Height = yNew;
        //}




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
