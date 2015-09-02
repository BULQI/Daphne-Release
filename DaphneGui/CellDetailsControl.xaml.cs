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
using System.Collections.ObjectModel;
using System.Windows.Controls.Primitives;
using DaphneGui.Pushing;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for CellDetailsControl.xaml
    /// </summary>
    public partial class CellDetailsControl : UserControl
    {
        private Level CurrentLevel = null;

        public CellDetailsControl()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This method is only needed if user goes to "Stores" and selects "Cells" and then selects a cell.
        /// </summary>
        /// <param name="currLevel"></param>
        public void SetCurrentLevel(Level currLevel)
        {
            CurrentLevel = currLevel;
        }

        private void memb_molecule_combo_box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)e.Source;
            if (cb == null)
                return;

            ConfigMolecularPopulation molpop = (ConfigMolecularPopulation)CellMembraneMolPopsListBox.SelectedItem;

            if (molpop == null)
                return;

            string curr_mol_pop_name = molpop.Name;
            string curr_mol_guid = "";
            curr_mol_guid = molpop.molecule.entity_guid;

            int nIndex = cb.SelectedIndex;
            if (nIndex < 0)
                return;

            Level level = MainWindow.GetLevelContext(this);
            if (level == null)
            {
                level = CurrentLevel;
            }
            //if user picked 'new molecule' then create new molecule in ER
            if (nIndex == (cb.Items.Count - 1))
            {
                ConfigMolecule newLibMol = new ConfigMolecule();
                //newLibMol.Name = newLibMol.GenerateNewName(MainWindow.SOP.Protocol, "_New");
                newLibMol.Name = newLibMol.GenerateNewName(level, "New");
                newLibMol.molecule_location = MoleculeLocation.Boundary;
                AddEditMolecule aem = new AddEditMolecule(newLibMol, MoleculeDialogType.NEW);
                aem.Tag = DataContext as ConfigCell;

                //if user cancels out of new molecule dialog, set selected molecule back to what it was
                if (aem.ShowDialog() == false)
                {
                    if (e.RemovedItems.Count > 0)
                    {
                        cb.SelectedItem = e.RemovedItems[0];
                    }
                    else
                    {
                        cb.SelectedIndex = 0;
                    }
                    return;
                }
                //newLibMol.ValidateName(MainWindow.SOP.Protocol);
                newLibMol.ValidateName(level);
                //MainWindow.SOP.Protocol.entity_repository.molecules.Add(newLibMol);
                level.entity_repository.molecules.Add(newLibMol);
                molpop.molecule = newLibMol.Clone(null);
                molpop.Name = newLibMol.Name;

                CollectionViewSource.GetDefaultView(cb.ItemsSource).Refresh();
                cb.SelectedItem = newLibMol;
            }
            //user picked an existing molecule
            else
            {
                ConfigMolecule newmol = (ConfigMolecule)cb.SelectedItem;

                //if molecule has not changed, return
                if (newmol.entity_guid == curr_mol_guid)
                {
                    return;
                }

                //if molecule changed, then make a clone of the newly selected one from entity repository
                ConfigMolecule mol = (ConfigMolecule)cb.SelectedItem;
                molpop.molecule = mol.Clone(null);

                string new_mol_name = mol.Name;
                if (curr_mol_guid != molpop.molecule.entity_guid)
                {
                    molpop.Name = new_mol_name;

                    //Must update molecules_dict
                    ConfigCell cell = DataContext as ConfigCell;
                    if (cell != null)
                    {
                        if (cell.membrane.molecules_dict.ContainsKey(curr_mol_guid))
                        {
                            cell.membrane.molecules_dict.Remove(curr_mol_guid);
                        }
                        if (cell.membrane.molecules_dict.ContainsKey(molpop.molecule.entity_guid) == false)
                        {
                            cell.membrane.molecules_dict.Add(molpop.molecule.entity_guid, molpop.molecule);
                        }
                    }
                }
            }

        }

        private void memb_molecule_combo_box_GotFocus(object sender, RoutedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            CollectionViewSource.GetDefaultView(combo.ItemsSource).Refresh();
        }

        private void CytosolAddMolButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)
                return;

            ConfigMolecularPopulation cmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
            cmp.Name = "NewMP";
            cmp.mp_distribution = new MolPopHomogeneousLevel();
            cmp.molecule = null;

            CollectionViewSource cvs = (CollectionViewSource)(FindResource("availableBulkMoleculesListView"));
            if (cvs.View != null)
            {
                foreach (ConfigMolecule item in cvs.View)
                {
                    if (cell.cytosol.molpops.Where(m => m.molecule.Name == item.Name).Any()) continue;
                    cmp.molecule = item;
                    cmp.Name = cmp.molecule.Name;
                    break;
                }
            }

            if (cmp.molecule == null)
            {
                MessageBox.Show("Please add more molecules from the store.");
                return;
            }

            cell.cytosol.molpops.Add(cmp);
            CellCytosolMolPopsListBox.SelectedIndex = CellCytosolMolPopsListBox.Items.Count - 1;
        }

        //// appears to be unused
        //private void CellMembraneMolPopsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    CollectionViewSource cvs = (CollectionViewSource)(FindResource("availableBoundaryMoleculesListView"));
        //    if (cvs == null || cvs.View == null)
        //        return;

        //    cvs.View.Refresh();

        //    if (e.AddedItems.Count == 0) return;
        //    var tmp = e.AddedItems[0] as ConfigMolecularPopulation;
        //    //var tmp = (sender as ComboBox).SelectedItem as ConfigMolecularPopulation;
        //    foreach (ConfigMolecule cm in cvs.View)
        //    {
        //        if (cm.Name == tmp.molecule.Name)
        //        {
        //            cvs.View.MoveCurrentTo(cm);
        //            return;
        //        }
        //    }
        //}

        private void CytosolRemoveMolButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecularPopulation cmp = (ConfigMolecularPopulation)CellCytosolMolPopsListBox.SelectedItem;

            if (cmp == null)
                return;

            MessageBoxResult res = MessageBox.Show("Removing this molecular population will remove cell reactions and reaction complexes that use this molecule. Are you sure you would like to proceed?", "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;

            ConfigCell cell = DataContext as ConfigCell;

            foreach (ConfigReaction cr in cell.cytosol.Reactions.ToList())
            {
                if (cr.HasMolecule(cmp.molecule.entity_guid))
                {
                    cell.cytosol.Reactions.Remove(cr);
                }
            }

            // Don't need to check membrane reactions. 
            // Membrane reactions can only have membrane-bound molecules, which will not be available for removal in cytosol.

            foreach (ConfigReactionComplex crc in cell.cytosol.reaction_complexes.ToList())
            {
                if (crc.molecules_dict.ContainsKey(cmp.molecule.entity_guid))
                {
                    cell.cytosol.reaction_complexes.Remove(crc);
                }
            }

            cell.cytosol.molpops.Remove(cmp);
            CellCytosolMolPopsListBox.SelectedIndex = CellCytosolMolPopsListBox.Items.Count - 1;

            if (lvCytosolAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();
        }

        private void MembraneAddMolButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)
                return;

            ConfigMolecularPopulation cmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
            cmp.Name = "NewMP";
            cmp.mp_distribution = new MolPopHomogeneousLevel();

            CollectionViewSource cvs = (CollectionViewSource)(FindResource("availableBoundaryMoleculesListView"));
            
            if (cvs == null) return;

            ObservableCollection<ConfigMolecule> mol_list = new ObservableCollection<ConfigMolecule>();

            foreach (ConfigMolecule item in cvs.View)
            {
                if (cell.membrane.molpops.Where(m => m.molecule.Name == item.Name).Any())
                    continue;
                
                if (item.molecule_location == MoleculeLocation.Boundary)
                {
                    mol_list.Add(item);
                }
                
            }

            if (mol_list != null && mol_list.Count > 0)
            {
                cmp.molecule = mol_list.First().Clone(null);
                cmp.Name = cmp.molecule.Name;
            }
            else
            {
                MessageBox.Show("All available molecular populations have already been added.", "Cytosol", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
      
            cell.membrane.molpops.Add(cmp);
            CellMembraneMolPopsListBox.SelectedIndex = CellMembraneMolPopsListBox.Items.Count - 1;


        }

        private void MembraneRemoveMolButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecularPopulation cmp = (ConfigMolecularPopulation)CellMembraneMolPopsListBox.SelectedItem;

            if (cmp == null)
                return;

            MessageBoxResult res = MessageBox.Show("Removing this molecular population will remove cell reactions and reaction complexes that use this molecule. Are you sure you would like to proceed?", "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;

            //ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            ConfigCell cell = DataContext as ConfigCell;

            foreach (ConfigReaction cr in cell.membrane.Reactions.ToList())
            {
                if (cr.HasMolecule(cmp.molecule.entity_guid))
                {
                    cell.membrane.Reactions.Remove(cr);
                }
            }
          
            foreach (ConfigReaction cr in cell.cytosol.Reactions.ToList())
            {
                if (cr.HasMolecule(cmp.molecule.entity_guid))
                {
                    cell.cytosol.Reactions.Remove(cr);
                }
            }

            foreach (ConfigReactionComplex crc in cell.cytosol.reaction_complexes.ToList())
            {
                if (crc.molecules_dict.ContainsKey(cmp.molecule.entity_guid))
                {
                    cell.cytosol.reaction_complexes.Remove(crc);
                }
            }

            foreach (ConfigReactionComplex crc in cell.membrane.reaction_complexes.ToList())
            {
                if (crc.molecules_dict.ContainsKey(cmp.molecule.entity_guid))
                {
                    cell.membrane.reaction_complexes.Remove(crc);
                }
            }

            cell.membrane.molpops.Remove(cmp);
            CellMembraneMolPopsListBox.SelectedIndex = CellMembraneMolPopsListBox.Items.Count - 1;

            if (lvCellAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCellAvailableReacs.ItemsSource).Refresh();
        }

        private void CellNucleusGenesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            txtGeneName.IsEnabled = false;

            ListBox lb = sender as ListBox;
            if (lb.SelectedIndex == -1 && lb.Items.Count > 0)
            {
                lb.SelectedIndex = 0;
            }
        }

        private void NucleusNewGeneButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)
            {
                MessageBox.Show("You must first select a cell. If no cell exists, you need to add one.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Level level = MainWindow.GetLevelContext(this);
            if (level == null)
            {
                level = CurrentLevel;
            }
            //Create a new default gene
            ConfigGene gene = new ConfigGene("g", 2, 1.0);
            //gene.Name = gene.GenerateNewName(MainWindow.SOP.Protocol, "New");
            gene.Name = gene.GenerateNewName(level, "New");

            //Display it in dialog and allow user to edit name, etc.
            AddEditGene aeg = new AddEditGene();
            aeg.DataContext = gene;

            //If cancelled from dialog, return.
            if (aeg.ShowDialog() == false)
                return;

            //Add new gene to cell
            cell.genes.Add(gene);

            //Clone new gene and add to ER
            ConfigGene erGene = gene.Clone(null);
            
            //MainWindow.SOP.Protocol.entity_repository.genes.Add(erGene);
            level.entity_repository.genes.Add(erGene);

            CellNucleusGenesListBox.SelectedIndex = CellNucleusGenesListBox.Items.Count - 1;
            CellNucleusGenesListBox.ScrollIntoView(CellNucleusGenesListBox.SelectedItem);

            txtGeneName.IsEnabled = true;
        }

        private void NucleusAddGeneButton_Click(object sender, RoutedEventArgs e)
        {
            //Get selected cell
            ConfigCell cell = DataContext as ConfigCell;

            //if no cell selected, return
            if (cell == null)
                return;

            //Show a dialog that gets the new gene's name
            AddGeneToCell ads = new AddGeneToCell(cell, MainWindow.GetLevelContext(this));

            if (ads.GeneComboBox.Items.Count == 0)
            {
                return;
            }

            //If user clicked 'apply' and not 'cancel'
            if (ads.ShowDialog() == true)
            {
                ConfigGene geneToAdd = ads.SelectedGene;
                if (geneToAdd == null)
                    return;

                ConfigGene newgene = geneToAdd.Clone(null);
                cell.genes.Add(newgene);
            }

            txtGeneName.IsEnabled = false;
        }

        private void NucleusRemoveGeneButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)
            {
                MessageBox.Show("Please select a cell first.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ConfigGene gene = (ConfigGene)CellNucleusGenesListBox.SelectedItem;

            if (gene == null)
            {
                MessageBox.Show("Please select a gene to remove before clicking the Remove button.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBoxResult res = MessageBox.Show("Removing this gene will remove cell reactions and reaction complexes that use this molecule. Are you sure you would like to proceed?", "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;

            foreach (ConfigReaction cr in cell.cytosol.Reactions.ToList())
            {
                if (cr.HasGene(gene.entity_guid))
                {
                    cell.cytosol.Reactions.Remove(cr);
                }
            }

            foreach (ConfigReactionComplex crc in cell.cytosol.reaction_complexes.ToList())
            {
                if (crc.genes_dict.ContainsKey(gene.entity_guid))
                {
                    cell.cytosol.reaction_complexes.Remove(crc);
                }
            }

            if (cell.diff_scheme != null)
            {
                if (cell.diff_scheme.genes.Contains(gene.entity_guid) == true)
                {
                    cell.diff_scheme.DeleteGene(gene.entity_guid);
                }
            }

            if (cell.div_scheme != null)
            {
                if (cell.div_scheme.genes.Contains(gene.entity_guid) == true)
                {
                    cell.div_scheme.DeleteGene(gene.entity_guid);
                }
            }

            if (cell.HasGene(gene.entity_guid)) {
                cell.genes.Remove(gene);
            }

            txtGeneName.IsEnabled = false;

            CellNucleusGenesListBox.SelectedIndex = CellNucleusGenesListBox.Items.Count - 1;

            if (lvCytosolAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();
            
        }

        private void MembraneRemoveReacButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;
            int nIndex = MembReacListBox.SelectedIndex;

            if (nIndex >= 0)
            {
                ConfigReaction cr = (ConfigReaction)MembReacListBox.SelectedValue;
                if (cr != null)
                {
                    cell.membrane.Reactions.Remove(cr);
                }
            }
        }

        private void CellAddReacExpander_Expanded(object sender, RoutedEventArgs e)
        {
            if (lvCellAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCellAvailableReacs.ItemsSource).Refresh();
            this.BringIntoView();
        }

        private void MembraneAddReacButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cc = DataContext as ConfigCell;
            bool needRefresh = false;

            Level protocol = MainWindow.GetLevelContext(this);

            //string message = "If the Membrane does not currently contain any of the molecules necessary for these reactions, then they will be added. ";
            //message = message + " Continue?";
            //MessageBoxResult result = MessageBox.Show(message, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            //if (result == MessageBoxResult.No)
            //{
            //    return;
            //}

            foreach (var item in lvCellAvailableReacs.SelectedItems)
            {
                ConfigReaction cr = (ConfigReaction)item;
                if (cc != null && cr != null)
                {
                    //This adds the reaction object to membrane
                    if (cc.membrane.reactions_dict.ContainsKey(cr.entity_guid) == false)
                    {                        
                        cc.membrane.Reactions.Add(cr.Clone(true));
                        needRefresh = true;

                        //If any molecules from new reaction don't exist in the membrane, clone and add them (can only be boundary molecules)                        
                        foreach (string molguid in cr.reactants_molecule_guid_ref)
                        {
                            if (protocol.entity_repository.molecules_dict.ContainsKey(molguid))
                            {
                                if (cc.membrane.HasMolecule(molguid) == false)
                                    cc.membrane.AddMolPop(protocol.entity_repository.molecules_dict[molguid].Clone(null), true);
                            }
                        }
                        foreach (string molguid in cr.products_molecule_guid_ref)
                        {
                            if (protocol.entity_repository.molecules_dict.ContainsKey(molguid))
                            {
                                if (cc.membrane.HasMolecule(molguid) == false)
                                    cc.membrane.AddMolPop(protocol.entity_repository.molecules_dict[molguid].Clone(null), true);
                            }
                        }
                        foreach (string molguid in cr.modifiers_molecule_guid_ref)
                        {
                            if (protocol.entity_repository.molecules_dict.ContainsKey(molguid))
                            {
                                if (cc.membrane.HasMolecule(molguid) == false)
                                    cc.membrane.AddMolPop(protocol.entity_repository.molecules_dict[molguid].Clone(null), true);
                            }
                        }
                    }
                }
            }

            //Refresh filter
            if (needRefresh && lvCellAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCellAvailableReacs.ItemsSource).Refresh();

        }

        private void CytosolReacListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
        }

        private void CytosolRemoveReacButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;
            int nIndex = CytosolReacListBox.SelectedIndex;
            if (nIndex >= 0)
            {
                ConfigReaction cr = (ConfigReaction)CytosolReacListBox.SelectedValue;
                if (cr != null)
                {
                    cell.cytosol.Reactions.Remove(cr);
                }
            }
        }

        private void CytosolAddReacButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cc = DataContext as ConfigCell;
            bool needRefresh = false;

            //Level protocol = MainWindow.SOP.Protocol;
            Level protocol = MainWindow.GetLevelContext(this);

            //string message = "If the Cytosol does not currently contain any of the molecules or genes necessary for these reactions, then they will be added appropriately. ";
            //message = message + " Continue?";
            //MessageBoxResult result = MessageBox.Show(message, "Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            //if (result == MessageBoxResult.No)
            //{
            //    return;
            //}

            foreach (var item in lvCytosolAvailableReacs.SelectedItems)
            {
                ConfigReaction cr = (ConfigReaction)item;
                if (cc != null && cr != null)
                {
                    // Add to reactions list only if the cell does not already contain this reaction
                    if (cc.cytosol.reactions_dict.ContainsKey(cr.entity_guid) == false)
                    {                        
                        cc.cytosol.Reactions.Add(cr.Clone(true));
                        needRefresh = true;

                        //Here, add any molecules or genes (from this new reaction) that are missing from the cell.
                        foreach (string molguid in cr.reactants_molecule_guid_ref)
                        {
                            //If molecule - can be bulk or boundary so have to add to appropriate compartment - membrane or cytosol
                            if (protocol.entity_repository.molecules_dict.ContainsKey(molguid))
                            {
                                ConfigMolecule mol = protocol.entity_repository.molecules_dict[molguid];
                                if (mol.molecule_location == MoleculeLocation.Boundary && cc.membrane.HasMolecule(molguid) == false)
                                    cc.membrane.AddMolPop(mol.Clone(null), true);
                                else if (mol.molecule_location == MoleculeLocation.Bulk && cc.cytosol.HasMolecule(molguid) == false)
                                    cc.cytosol.AddMolPop(mol.Clone(null), true);
                            }
                            //If gene, add to genes list
                            else if (protocol.entity_repository.genes_dict.ContainsKey(molguid))
                            {
                                if (cc.HasGene(molguid) == false)
                                    cc.genes.Add(protocol.entity_repository.genes_dict[molguid].Clone(null));
                            }
                        }
                        foreach (string molguid in cr.products_molecule_guid_ref)
                        {
                            //If molecule - can be bulk or boundary so have to add to appropriate compartment - membrane or cytosol
                            if (protocol.entity_repository.molecules_dict.ContainsKey(molguid))
                            {
                                ConfigMolecule mol = protocol.entity_repository.molecules_dict[molguid];
                                if (mol.molecule_location == MoleculeLocation.Boundary && cc.membrane.HasMolecule(molguid) == false)
                                    cc.membrane.AddMolPop(mol.Clone(null), true);
                                else if (mol.molecule_location == MoleculeLocation.Bulk && cc.cytosol.HasMolecule(molguid) == false)
                                    cc.cytosol.AddMolPop(mol.Clone(null), true);
                            }
                            //If gene, add to genes list
                            else if (protocol.entity_repository.genes_dict.ContainsKey(molguid))
                            {
                                if (cc.HasGene(molguid) == false)
                                    cc.genes.Add(protocol.entity_repository.genes_dict[molguid].Clone(null));
                            }
                        }
                        foreach (string molguid in cr.modifiers_molecule_guid_ref)
                        {
                            //If molecule - can be bulk or boundary so have to clone and add to appropriate compartment - membrane or cytosol
                            if (protocol.entity_repository.molecules_dict.ContainsKey(molguid))
                            {
                                ConfigMolecule mol = protocol.entity_repository.molecules_dict[molguid];
                                if (mol.molecule_location == MoleculeLocation.Boundary && cc.membrane.HasMolecule(molguid) == false)
                                    cc.membrane.AddMolPop(mol.Clone(null), true);
                                else if (mol.molecule_location == MoleculeLocation.Bulk && cc.cytosol.HasMolecule(molguid) == false)
                                    cc.cytosol.AddMolPop(mol.Clone(null), true);
                            }
                            //If gene, clone and add to genes list
                            else if (protocol.entity_repository.genes_dict.ContainsKey(molguid))
                            {
                                if (cc.HasGene(molguid) == false)
                                    cc.genes.Add(protocol.entity_repository.genes_dict[molguid].Clone(null));
                            }
                        }
                    }
                }
            }

            // Refresh the filter
            if (needRefresh && lvCytosolAvailableReacs.ItemsSource != null)
            {
                CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();
            }
        }

        private void CellAddReacExpander2_Expanded(object sender, RoutedEventArgs e)
        {
            if (lvCytosolAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lvCellAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCellAvailableReacs.ItemsSource).Refresh();
            if (lvCytosolAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();
        }

        //I DON'T THINK THIS METHOD IS USED - SKG
        //IN ANY CASE, IT IS RELEVANT ONLY FOR CELL POPULATIONS, NOT RELEVANT FOR USERSTORE OR DAPHNESTORE
        private void gaussian_region_actor_checkbox_clicked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = e.OriginalSource as CheckBox;

            if (cb.CommandParameter == null)
            {
                return;
            }

            string guid = cb.CommandParameter as string;

            if (guid.Length > 0)
            {
                GaussianSpecification next;

                ((TissueScenario)MainWindow.SOP.Protocol.scenario).resetGaussRetrieve();
                while ((next = ((TissueScenario)MainWindow.SOP.Protocol.scenario).nextGaussSpec()) != null)
                {
                    if (next.box_spec.box_guid == guid)
                    {
                        next.gaussian_region_visibility = (bool)(cb.IsChecked);
                        break;
                    }
                }
            }
        }

        private void cyto_molecule_combo_box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)e.Source;
            if (cb == null)
                return;

            ConfigMolecularPopulation molpop = (ConfigMolecularPopulation)CellCytosolMolPopsListBox.SelectedItem;

            if (molpop == null)
                return;

            string curr_mol_pop_name = molpop.Name;
            string curr_mol_guid = "";
            curr_mol_guid = molpop.molecule.entity_guid;

            int nIndex = cb.SelectedIndex;
            if (nIndex < 0)
                return;

            Level level = MainWindow.GetLevelContext(this);
            if (level == null)
            {
                level = CurrentLevel;
            }

            //if user picked 'new molecule' then create new molecule in ER
            if (nIndex == (cb.Items.Count - 1))
            {
                ConfigMolecule newLibMol = new ConfigMolecule();

                //newLibMol.Name = newLibMol.GenerateNewName(MainWindow.SOP.Protocol, "_New");
                newLibMol.Name = newLibMol.GenerateNewName(level, "New");

                AddEditMolecule aem = new AddEditMolecule(newLibMol, MoleculeDialogType.NEW);
                aem.Tag = this.Tag;    //DataContext as ConfigCell

                //if user cancels out of new molecule dialog, set selected molecule back to what it was
                if (aem.ShowDialog() == false)
                {
                    if (e.RemovedItems.Count > 0)
                    {
                        cb.SelectedItem = e.RemovedItems[0];
                    }
                    else
                    {
                        cb.SelectedIndex = 0;
                    }
                    return;
                }
                //newLibMol.ValidateName(MainWindow.SOP.Protocol);
                //MainWindow.SOP.Protocol.entity_repository.molecules.Add(newLibMol);
                newLibMol.ValidateName(level);
                level.entity_repository.molecules.Add(newLibMol);

                molpop.molecule = newLibMol.Clone(null);
                molpop.Name = newLibMol.Name;

                CollectionViewSource.GetDefaultView(cb.ItemsSource).Refresh();
                cb.SelectedItem = newLibMol;
            }
            //user picked an existing molecule
            else
            {
                ConfigMolecule newmol = (ConfigMolecule)cb.SelectedItem;

                //if molecule has not changed, return
                if (newmol.entity_guid == curr_mol_guid)
                {
                    return;
                }

                //if molecule changed, then make a clone of the newly selected one from entity repository
                ConfigMolecule mol = (ConfigMolecule)cb.SelectedItem;
                molpop.molecule = mol.Clone(null);

                string new_mol_name = mol.Name;
                if (curr_mol_guid != molpop.molecule.entity_guid)
                {
                    molpop.Name = new_mol_name;

                    //Must update molecules_dict
                    ConfigCell cell = DataContext as ConfigCell;
                    if (cell != null)
                    {
                        if (cell.cytosol.molecules_dict.ContainsKey(curr_mol_guid)) 
                        {
                            cell.cytosol.molecules_dict.Remove(curr_mol_guid);
                        }
                        cell.cytosol.molecules_dict.Add(molpop.molecule.entity_guid, molpop.molecule);
                    }
                }
            }

            var cvs = (CollectionViewSource)(FindResource("cytosolAvailableReactionsListView"));
            if (cvs.View == null) return; //not ready yet
            cvs.View.Refresh();
        }

        private void cyto_molecule_combo_box_GotFocus(object sender, RoutedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            CollectionViewSource.GetDefaultView(combo.ItemsSource).Refresh();
        }

        private void cytosolAvailableReactionsListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigReaction cr = e.Item as ConfigReaction;
            ConfigCell cc = DataContext as ConfigCell;

            if (cc == null)
            {
                e.Accepted = false;
                return;
            }

            //New filtering rules as of 3/5/15 bug 2426
            //Allow all reactions except what belongs in membrane (where each molecule is a boundary molecule)

            //EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
            Level level = MainWindow.GetLevelContext(this);
            if (level == null)
            {
                level = CurrentLevel;
            }
            EntityRepository er = level.entity_repository;

            if (cr.HasBulkMolecule(er) == true)
            {
                e.Accepted = true;
            }
            else
            {
                e.Accepted = false;
                return;
            }

            //If the cell cytosol already contains this reaction, exclude it from the available reactions list
            if (cc.cytosol.reactions_dict.ContainsKey(cr.entity_guid))
            {
                e.Accepted = false;
            }

            //skg 5/8/15 - MUST ALSO EXCLUDE REACTIONS THAT ARE IN THE REACTION COMPLEXES
            foreach (ConfigReactionComplex crc in cc.cytosol.reaction_complexes)
            {
                if (crc.reactions_dict.ContainsKey(cr.entity_guid))
                {
                    e.Accepted = false;
                }
            }
        }

        private void membraneAvailableReactionsListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigReaction cr = e.Item as ConfigReaction;
            ConfigCell cc = DataContext as ConfigCell;

            if (cc == null)
            {
                e.Accepted = false;
                return;
            }

            //New filter as of 3/5/2015 for bug 2426
            //Molecules no longer need to be in the membrane. They will get added if needed.

            //If the reaction has any bulk molecules, it cannot go in the membrane

            //if (cr.HasBulkMolecule(MainWindow.SOP.Protocol.entity_repository) == true)
            Level level = MainWindow.GetLevelContext(this);
            if (level == null)
            {
                level = CurrentLevel;
            }
            if (cr.HasBulkMolecule(level.entity_repository) == true)
            {
                e.Accepted = false;
                return;
            }

            //Finally, if the cell membrane already contains this reaction, exclude it from the available reactions list
            if (cc.membrane.reactions_dict.ContainsKey(cr.entity_guid))
            {
                e.Accepted = false;
                return;
            }

            //skg 5/8/15 - MUST ALSO EXCLUDE REACTIONS THAT ARE IN THE REACTION COMPLEXES
            foreach (ConfigReactionComplex crc in cc.membrane.reaction_complexes)
            {
                if (crc.reactions_dict.ContainsKey(cr.entity_guid))
                {
                    e.Accepted = false;
                    return;
                }
            }

            e.Accepted = true;
        }

        //THIS METHOD NEEDS TO BE IMPLEMENTED

        private void membraneAvailableReactionComplexesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigReactionComplex crc = e.Item as ConfigReactionComplex;
            ConfigCell cc = DataContext as ConfigCell;

            if (cc == null)
            {
                e.Accepted = false;
                return;
            }

            //if already in membrane, return
            if (cc.membrane.reaction_complexes_dict.ContainsKey(crc.entity_guid))
            {
                e.Accepted = false;
                return;
            }

            //This filter is called for every reaction complex in the repository.

            // Only allow reaction complexes with membrane-bound molecules.
            bool bOK = true;

            foreach (KeyValuePair<string,ConfigMolecule> kvp in crc.molecules_dict)
            {
                if (kvp.Value.molecule_location == MoleculeLocation.Bulk)
                {
                    bOK = false;
                    break;
                }
            }

            e.Accepted = bOK;
        }

        //THIS METHOD NEEDS TO BE IMPLEMENTED
        private void cytosolAvailableReactionComplexesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigReactionComplex crc = e.Item as ConfigReactionComplex;
            ConfigCell cc = DataContext as ConfigCell;

            //if null cell, return
            if (cc == null)
            {
                e.Accepted = false;
                return;
            }

            //if already in cytosol, return
            if (cc.cytosol.reaction_complexes_dict.ContainsKey(crc.entity_guid))
            {
                e.Accepted = false;
                return;
            }

            // Allow any reaction commplex. Any missign molecules or genes will be added to the cell, as needed.

            e.Accepted = true;
        }

        //private bool EcmHasMolecule(string molguid)
        //{
        //    foreach (ConfigMolecularPopulation molpop in MainWindow.SOP.Protocol.scenario.environment.comp.molpops)
        //    {
        //        if (molpop.molecule.entity_guid == molguid)
        //            return true;
        //    }
        //    return false;
        //}

        private bool MembraneHasMolecule(ConfigCell cell, string molguid)
        {
            foreach (ConfigMolecularPopulation molpop in cell.membrane.molpops)
            {
                if (molguid == molpop.molecule.entity_guid)
                {
                    return true;
                }
            }
            return false;
        }
        private bool CytosolHasMolecule(ConfigCell cell, string molguid)
        {
            foreach (ConfigMolecularPopulation molpop in cell.cytosol.molpops)
            {
                if (molguid == molpop.molecule.entity_guid)
                {
                    return true;
                }
            }
            return false;
        }

        private bool CellPopsHaveMoleculeInMemb(string molguid)
        {
            bool ret = false;
            //This method should only get called if we have a tissue scenario so no need to use "LevelContext"
            foreach (CellPopulation cell_pop in ((TissueScenario)MainWindow.SOP.Protocol.scenario).cellpopulations)
            {
                if (MainWindow.SOP.Protocol.entity_repository.cells_dict.ContainsKey(cell_pop.Cell.entity_guid))
                {
                    ConfigCell cell = MainWindow.SOP.Protocol.entity_repository.cells_dict[cell_pop.Cell.entity_guid];
                    if (MembraneHasMolecule(cell, molguid))
                        return true;
                }
                else
                {
                    return ret;
                }
            }

            return ret;
        }
        private bool CellPopsHaveMoleculeInCytosol(string molguid)
        {
            bool ret = false;
            //This method should only get called if we have a tissue scenario so no need to use "LevelContext"
            foreach (CellPopulation cell_pop in ((TissueScenario)MainWindow.SOP.Protocol.scenario).cellpopulations)
            {
                ConfigCell cell = MainWindow.SOP.Protocol.entity_repository.cells_dict[cell_pop.Cell.entity_guid];
                if (CytosolHasMolecule(cell, molguid))
                    return true;
            }

            return ret;
        }

        private void menu2PushToProto_Click(object sender, RoutedEventArgs e)
        {            
            if (CytosolReacListBox.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a reaction.");
                return;
            }

            ConfigReaction reac = (ConfigReaction)CytosolReacListBox.SelectedValue;
            ConfigReaction newreac = reac.Clone(true);
            MainWindow.GenericPush(newreac);
        }

        private void btnNewDeathDriver_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;

            if (cell == null)
                return;

            if (cell.death_driver == null)
            {
                // The transition driver elements should default to ConfigMolTransitionDriverElement.
                // If we use ConfigDistrDriverElement as the default we won't be able to distinguish between meaningful and empty TDEs. 
                ConfigTransitionDriver config_td = new ConfigTransitionDriver();
                config_td.Name = "generic apoptosis";
                string[] stateName = new string[] { "alive", "dead" };
                string[,] signal = new string[,] { { "", "" }, { "", "" } };
                double[,] alpha = new double[,] { { 0, 0 }, { 0, 0 } };
                double[,] beta = new double[,] { { 0, 0 }, { 0, 0 } };

                //ProtocolCreators.LoadConfigTransitionDriverElements(config_td, signal, alpha, beta, stateName, MainWindow.SOP.Protocol);
                Level level = MainWindow.GetLevelContext(this);
                if (level == null)
                {
                    level = CurrentLevel;
                }
                ProtocolCreators.LoadConfigTransitionDriverElements(config_td, signal, alpha, beta, stateName, level);

                config_td.CurrentState = new DistributedParameter(0);
                config_td.StateName = config_td.states[(int)config_td.CurrentState.Sample()];
                cell.death_driver = config_td;
            }
        }

        private void btnDelDeathDriver_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;

            if (cell == null)
                return;

            //confirm deletion of driver
            MessageBoxResult res;
            res = MessageBox.Show("Are you sure you want to delete the selected cell's death driver?", "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;

            //delete driver
            cell.death_driver = null;

            ToolWinTissue twt = Tag as ToolWinTissue;
            CellPopulation cp = twt.CellPopControl.CellPopsListBox.SelectedItems[0] as CellPopulation;
            if (cp != null)
            {
                cp.reportStates.Death = false;
            }

        }

        /// <summary>
        /// This method creates a data grid column with a combo box in the header.
        /// The combo box contains genes that are not in the epigenetic map of of 
        /// the selected cell's differentiation scheme.  
        /// This allows the user to add genes to the epigenetic map.
        /// </summary>
        /// <returns></returns>
        public DataGridTextColumn CreateUnusedGenesColumn(ConfigTransitionScheme currScheme)
        {
            ConfigCell cell = DataContext as ConfigCell;

            //EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
            Level level = MainWindow.GetLevelContext(this);
            if (level == null)
            {
                level = CurrentLevel;
            }
            EntityRepository er = level.entity_repository;

            DataGridTextColumn editor_col = new DataGridTextColumn();
            editor_col.CanUserSort = false;
            DataTemplate HeaderTemplate = new DataTemplate();

            CollectionViewSource cvs1 = new CollectionViewSource();
            cvs1.SetValue(CollectionViewSource.SourceProperty, er.genes);

            if (currScheme == cell.diff_scheme)
            {
                cvs1.Filter += new FilterEventHandler(unusedGenesListView_Filter);
            }
            else
            {
                cvs1.Filter += new FilterEventHandler(unusedDivGenesListView_Filter);
            }

            CompositeCollection coll1 = new CompositeCollection();
            ConfigGene dummyItem = new ConfigGene("Add a gene", 0, 0);
            coll1.Add(dummyItem);
            CollectionContainer cc1 = new CollectionContainer();
            cc1.Collection = cvs1.View;
            coll1.Add(cc1);

            FrameworkElementFactory addGenesCombo = new FrameworkElementFactory(typeof(ComboBox));
            addGenesCombo.SetValue(ComboBox.WidthProperty, 85D);
            addGenesCombo.SetValue(ComboBox.ItemsSourceProperty, coll1);
            addGenesCombo.SetValue(ComboBox.DisplayMemberPathProperty, "Name");
            addGenesCombo.SetValue(ComboBox.ToolTipProperty, "Click here to add another gene column to the grid.");
            addGenesCombo.AddHandler(ComboBox.SelectionChangedEvent, new SelectionChangedEventHandler(comboAddGeneToEpigeneticMap_SelectionChanged));

            addGenesCombo.SetValue(ComboBox.SelectedIndexProperty, 0);
            addGenesCombo.Name = "ComboGenes";

            HeaderTemplate.VisualTree = addGenesCombo;
            editor_col.HeaderTemplate = HeaderTemplate;

            return editor_col;
        }

        /// <summary>
        /// This handler gets called when user selects a gene in the "add genes" 
        /// combo box in the upper right of the epigenetic map data grid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void comboAddGeneToEpigeneticMap_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if (combo.SelectedIndex <= 0)
                return;

            //ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;
            ConfigCell cell = DataContext as ConfigCell;

            if (cell == null)
                return;

            DataGrid dataGrid = (DataGrid)DiffSchemeDataGrid.FindVisualParent<DataGrid>(combo);
            if (dataGrid == null) return;
            ConfigTransitionScheme scheme = DiffSchemeDataGrid.GetDiffSchemeSource(dataGrid);
            if (scheme != null)
            {

                // These next two statements are needed to prevent a crash during the Refresh operations, below.
                // The crash occurs when the user is still in editing mode in a cell and the Refresh method is called.
                // This is a known bug and fix.
                dataGrid.CommitEdit();
                dataGrid.CommitEdit();

                //this is the new way to creating datagrid dyamically with diffscheme specified in the grid
                //otherwise, it is the old way, remove those code when all changed to this new way.
                ConfigGene gene1 = (ConfigGene)combo.SelectedItem;
                if (gene1 == null)
                    return;

                if (scheme.genes.Contains(gene1.entity_guid)) return; //shouldnot happen...

                ConfigGene newgene = gene1.Clone(null);
                scheme.AddGene(newgene.entity_guid);

                // The default activation level is 1
                int i = scheme.genes.IndexOf(newgene.entity_guid);
                foreach (ConfigActivationRow row in scheme.activationRows)
                {
                    row.activations[i] = 0.0;
                }
                

                //HERE, WE NEED TO ADD THE GENE TO THE CELL ALSO
                if (cell.HasGene(gene1.entity_guid) == false)
                {
                    newgene = gene1.Clone(null);
                    cell.genes.Add(newgene);
                }
              
                //force refresh
                //dataGrid.GetBindingExpression(DiffSchemeDataGrid.DiffSchemeSourceProperty).UpdateTarget();
                return;
            }
        }

        private void btnNewDiffScheme_Click(object sender, RoutedEventArgs e)
        {
            string schemeName = ((Button)sender).Tag as string;
            if (schemeName == null) return;

            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null) return;

            Level level = MainWindow.GetLevelContext(this);

            if (schemeName == "Division")
            {
                    cell.div_scheme = new ConfigTransitionScheme();
                    cell.div_scheme.Name = "Division";
                    // Check whether the default name (above) is already taken.
                    cell.div_scheme.Name = cell.div_scheme.GenerateNewName(level, "_");
                    cell.div_scheme.InsertState("G1-S-G2-M", 0);
                    cell.div_scheme.InsertState("cytokinetic", 1);

                    //// Add a transition driver with arbitrary values
                    //ConfigDistrTransitionDriverElement distr_element = new ConfigDistrTransitionDriverElement();
                    //distr_element.Distr = new DistributedParameter();
                    //distr_element.Distr.DistributionType = ParameterDistributionType.CONSTANT;
                    //distr_element.Distr.ConstValue = 60; // minutes
                    //distr_element.CurrentState = 0;
                    //distr_element.CurrentStateName = cell.div_scheme.Driver.states[0];
                    //distr_element.DestState = 1;
                    //distr_element.DestStateName = cell.div_scheme.Driver.states[1];
                    //cell.div_scheme.Driver.DriverElements[0].elements[1] = distr_element;
                    level.entity_repository.diff_schemes.Add(cell.div_scheme.Clone(true));
            }
            else if (schemeName == "Differentiation")
            {
                    cell.diff_scheme = new ConfigTransitionScheme();
                    cell.diff_scheme.Name = "Differentiation";
                    // Check whether the default name (above) is already taken.
                    cell.diff_scheme.Name = cell.diff_scheme.GenerateNewName(level, "_");
                    cell.diff_scheme.InsertState("State0", 0);
                    cell.diff_scheme.InsertState("State1", 1);
                    //ConfigDistrTransitionDriverElement distr_element = new ConfigDistrTransitionDriverElement();
                    //distr_element.Distr = new DistributedParameter();
                    //distr_element.Distr.DistributionType = ParameterDistributionType.CONSTANT;
                    //distr_element.Distr.ConstValue = 180; // minutes
                    //distr_element.CurrentState = 0;
                    //distr_element.CurrentStateName = cell.diff_scheme.Driver.states[0];
                    //distr_element.DestState = 1;
                    //distr_element.DestStateName = cell.diff_scheme.Driver.states[1];
                    //cell.diff_scheme.Driver.DriverElements[0].elements[1] = distr_element;
                    level.entity_repository.diff_schemes.Add(cell.diff_scheme.Clone(true));
            }
            else return;
        }

        private void btnDelDiffScheme_Click(object sender, RoutedEventArgs e)
        {

            string schemeName = ((Button)sender).Tag as string;
            if (schemeName == null) return;

            MessageBoxResult res;
            string message = string.Format("Are you sure you want to delete the selected cell's {0} scheme?", schemeName);
            res = MessageBox.Show(message, "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;

            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)
                return;

            //Remove scheme from the cell
            if (schemeName == "Division")
            {
                cell.div_scheme = null;
            }
            else if (schemeName == "Differentiation")
            {
                cell.diff_scheme = null;
            }
            
            //Now remove scheme from the cell population
            ToolWinTissue twt = Tag as ToolWinTissue;

            //If we're looking at the cells library, that has no cell population, so return.
            if (twt == null)
                return;

            CellPopulation cp = twt.CellPopControl.CellPopsListBox.SelectedItems[0] as CellPopulation;

            if (schemeName == "Division")
            {
                if (cp != null)
                {
                    cp.reportStates.Division = false;
                }
            }
            else if (schemeName == "Differentiation")
            {
                if (cp != null)
                {
                    cp.reportStates.Differentiation = false;
                }
            }
        }

        /// <summary>
        /// This method is called when the user clicks on a different row in the differentiation grid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DiffRegGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
        }

        private void EpigeneticMapGrid_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // TODO: Add event handler implementation here.
            DependencyObject dep = (DependencyObject)e.OriginalSource;

            // iteratively traverse the visual tree
            while ((dep != null) && !(dep is DataGridCell) && !(dep is DataGridColumnHeader) && !(dep is DataGridRowHeader))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep == null)
                return;

            else if (dep is DataGridColumnHeader)
            {
                DataGridColumnHeader columnHeader = dep as DataGridColumnHeader;
                // do something
                DataGridBehavior.SetHighlightColumn(columnHeader.Column, true);
            }

            else if (dep is DataGridRowHeader)
            {
            }

            else if (dep is DataGridCell)
            {
                DataGridCell cell = dep as DataGridCell;
                // do something                
            }
        }


        private void EpigeneticMapGrid_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
        }

        private void unusedGenesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;

            e.Accepted = false;

            if (cell == null)
                return;

            ConfigTransitionScheme ds = cell.diff_scheme;
            ConfigGene gene = e.Item as ConfigGene;

            if (ds != null)
            {
                //if scheme already contains this gene, exclude it from the available gene pool
                if (ds.genes.Contains(gene.entity_guid))
                {
                    e.Accepted = false;
                }
                else
                {
                    e.Accepted = true;
                }
            }
            else
            {
                e.Accepted = true;
            }
        }

        private void unusedDivGenesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;

            e.Accepted = false;

            if (cell == null)
                return;

            ConfigTransitionScheme ds = cell.div_scheme;
            ConfigGene gene = e.Item as ConfigGene;

            if (ds != null)
            {
                //if scheme already contains this gene, exclude it from the available gene pool
                if (ds.genes.Contains(gene.entity_guid))
                {
                    e.Accepted = false;
                }
                else
                {
                    e.Accepted = true;
                }
            }
            else
            {
                e.Accepted = true;
            }
        }
        public static T FindChild<T>(DependencyObject parent, string childName) where T : DependencyObject
        {
            // Confirm parent and childName are valid. 
            if (parent == null) return null;

            T foundChild = null;

            int childrenCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childrenCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                // If the child is not of the request child type child
                T childType = child as T;
                if (childType == null)
                {
                    // recursively drill down the tree
                    foundChild = FindChild<T>(child, childName);

                    // If the child is found, break so we do not overwrite the found child. 
                    if (foundChild != null) break;
                }
                else if (!string.IsNullOrEmpty(childName))
                {
                    var frameworkElement = child as FrameworkElement;
                    // If the child's name is set for search
                    if (frameworkElement != null && frameworkElement.Name == childName)
                    {
                        // if the child's name is of the request name
                        foundChild = (T)child;
                        break;
                    }
                    else
                    {
                        // recursively drill down the tree
                        foundChild = FindChild<T>(child, childName);

                        // If the child is found, break so we do not overwrite the found child. 
                        if (foundChild != null) break;
                    }
                }
                else
                {
                    // child element found.
                    foundChild = (T)child;
                    break;
                }
            }

            return foundChild;
        }

        private void NucPushGeneButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;
            ConfigGene gene = (ConfigGene)CellNucleusGenesListBox.SelectedItem;

            if (gene == null)
                return;
            ConfigGene newgene = gene.Clone(null);
            MainWindow.GenericPush(newgene);
        }

        private void PushCytoMoleculeButton_Click(object sender, RoutedEventArgs e)
        {
            if (CellCytosolMolPopsListBox.SelectedIndex < 0)
                return;

            ConfigCell cell = DataContext as ConfigCell;
            ConfigMolecule mol = ((ConfigMolecularPopulation)(CellCytosolMolPopsListBox.SelectedItem)).molecule;

            ConfigMolecule newmol = mol.Clone(null);
            MainWindow.GenericPush(newmol);
        }

        private void PushMembMoleculeButton_Click(object sender, RoutedEventArgs e)
        {
            if (CellMembraneMolPopsListBox.SelectedIndex < 0)
                return;

            ConfigCell cell = DataContext as ConfigCell;
            ConfigMolecule mol = ((ConfigMolecularPopulation)(CellMembraneMolPopsListBox.SelectedItem)).molecule;

            ConfigMolecule newmol = mol.Clone(null);
            MainWindow.GenericPush(newmol);
        }

        private void PushMembReacButton_Click(object sender, RoutedEventArgs e)
        {
            if (MembReacListBox.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a reaction.");
                return;
            }

            ConfigReaction reac = (ConfigReaction)MembReacListBox.SelectedValue;
            ConfigReaction newreac = reac.Clone(true);
            MainWindow.GenericPush(newreac);
        }

        private void PushCytoReacButton_Click(object sender, RoutedEventArgs e)
        {
            if (CytosolReacListBox.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a reaction.");
                return;
            }

            ConfigReaction reac = (ConfigReaction)CytosolReacListBox.SelectedValue;
            ConfigReaction newreac = reac.Clone(true);
            MainWindow.GenericPush(newreac);
        }

        //Membrane reaction complex handlers

        private void MembAddReacCxExpander_Expanded(object sender, RoutedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(lbMembAvailableReacCx.ItemsSource).Refresh();
        }

        private void MembAddReacCxButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigReactionComplex crc = (ConfigReactionComplex)lbMembAvailableReacCx.SelectedItem;
            ConfigCell cell = DataContext as ConfigCell;

            if (crc != null)
            {
                string message = "If the membrane does not currently contain any of the molecules or genes necessary for these reactions, then they will be added. ";
                message = message + "Any duplicate reactions currently in the membrane will be removed. Continue?";
                MessageBoxResult result = MessageBox.Show(message, "Warning", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return;
                } 
                
                if (cell.membrane.reaction_complexes_dict.ContainsKey(crc.entity_guid) == false)
                {
                    // If the membrane does not have any of the required molecules, then add them.
                    foreach (ConfigMolecularPopulation molpop in crc.molpops)
                    {
                        if (molpop.molecule.molecule_location == MoleculeLocation.Boundary)
                        {
                            if (!cell.cytosol.HasMolecule(molpop.molecule))
                            {
                                if (molpop.report_mp.GetType() != typeof(ReportMP))
                                {
                                    molpop.report_mp = new ReportMP();
                                }

                                cell.cytosol.molpops.Add(molpop);
                            }
                        }
                        else
                        {            
                            MessageBox.Show("Membrane cannot add reactions involving bulk molecules.", "Warning");
                            return;
                        }
                    }

                    // Check for duplicate reactions. Remove duplicates from membrane. 
                    foreach (ConfigReaction reac in crc.reactions)
                    {
                        if (cell.membrane.reactions_dict.ContainsKey(reac.entity_guid))
                        {
                            cell.membrane.Reactions.Remove(cell.membrane.reactions_dict[reac.entity_guid]);
                        }
                    }

                    cell.membrane.reaction_complexes.Add(crc.Clone(true));
                    CollectionViewSource.GetDefaultView(lbMembAvailableReacCx.ItemsSource).Refresh();
                }
            }
        }

        private void MembRemoveReacCompButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;
            int nIndex = MembReactionComplexListBox.SelectedIndex;
            if (nIndex >= 0)
            {
                ConfigReactionComplex crc = (ConfigReactionComplex)MembReactionComplexListBox.SelectedItem;
                if (crc != null)
                {
                    cell.membrane.reaction_complexes.Remove(crc);
                    CollectionViewSource.GetDefaultView(lbMembAvailableReacCx.ItemsSource).Refresh();
                }
            }
        }

        //Cytosol reaction complex handlers

        private void CytoRCDetailsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(lbCytoAvailableReacCx.ItemsSource).Refresh();
            
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }

        private void CytoAddReacCxButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigReactionComplex crc = (ConfigReactionComplex)lbCytoAvailableReacCx.SelectedItem;
            ConfigCell cell = DataContext as ConfigCell;

            if (crc != null)
            {
                string message = "If the cell does not currently contain any of the molecules or genes necessary for these reactions, then they will be added. ";
                message = message + "Any duplicate reactions currently in the cytosol will be removed. Continue?";
                MessageBoxResult result = MessageBox.Show(message, "Warning", MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No)
                {
                    return;
                }

                if (cell.cytosol.reaction_complexes_dict.ContainsKey(crc.entity_guid) == false)
                {
                    cell.cytosol.reaction_complexes.Add(crc.Clone(true));

                    // Check for duplicate reactions. Remove duplicates from cytosol. 
                    foreach (ConfigReaction reac in crc.reactions)
                    {
                        if (cell.cytosol.reactions_dict.ContainsKey(reac.entity_guid))
                        {
                            cell.cytosol.Reactions.Remove(cell.cytosol.reactions_dict[reac.entity_guid]);
                        }
                    }

                    // If the cytosol does not have any of the required molecules, then add them.
                    foreach (ConfigMolecularPopulation molpop in crc.molpops)
                    {
                        if (molpop.molecule.molecule_location == MoleculeLocation.Bulk)
                        {
                            if (!cell.cytosol.HasMolecule(molpop.molecule))
                            {
                                if (molpop.report_mp.GetType() != typeof(ReportMP))
                                {
                                    molpop.report_mp = new ReportMP();
                                }

                                cell.cytosol.molpops.Add(molpop);
                            }
                        }
                        else
                        {
                            if (!cell.membrane.HasMolecule(molpop.molecule))
                            {
                                if (molpop.report_mp.GetType() != typeof(ReportMP))
                                {
                                    molpop.report_mp = new ReportMP();
                                }

                                cell.membrane.molpops.Add(molpop);
                            }
                        }
                    }

                    // If the cell does not have any of the required genes, then add them.
                    foreach (ConfigGene gene in crc.genes)
                    {
                        if (!cell.HasGene(gene.entity_guid))
                        {
                            cell.genes.Add(gene);
                        }
                    }


                    CollectionViewSource.GetDefaultView(lbCytoAvailableReacCx.ItemsSource).Refresh();
                }
                
            }
        }

        private void CytoRemoveReacCompButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;
            int nIndex = CytoReactionComplexListBox.SelectedIndex;
            if (nIndex >= 0)
            {
                ConfigReactionComplex crc = (ConfigReactionComplex)CytoReactionComplexListBox.SelectedItem;
                if (crc != null)
                {
                    cell.cytosol.reaction_complexes.Remove(crc);
                    CollectionViewSource.GetDefaultView(lbCytoAvailableReacCx.ItemsSource).Refresh();
                }
            }
        }

        private void AddReacCxExpander_Expanded(object sender, RoutedEventArgs e)
        {
            CollectionViewSource.GetDefaultView(lbCytoAvailableReacCx.ItemsSource).Refresh();
        }


        // UserControl methods
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            //Sort molecules in ascending order
            System.ComponentModel.SortDescription sd = new System.ComponentModel.SortDescription();
            sd.PropertyName = "Name";
            sd.Direction = System.ComponentModel.ListSortDirection.Ascending;

            // cyto_molecule_combo_box
            CollectionViewSource cvs = (CollectionViewSource)(FindResource("availableBulkMoleculesListView"));
            cvs.Filter += ToolWinBase.FilterFactory.BulkMolecules_Filter;
            cvs.SortDescriptions.Insert(0, sd);

            // memb_molecule_combo_box
            cvs = (CollectionViewSource)(FindResource("availableBoundaryMoleculesListView"));
            cvs.Filter += ToolWinBase.FilterFactory.BoundaryMolecules_Filter;
            cvs.SortDescriptions.Insert(0, sd);

            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)
            {
                return;
            }

            //Level level = null;
            //SetCurrentLevel(level);

            updateCollections(cell);
            updateSelectedMoleculesAndGenes(cell);
        }

        private void UserControl_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)
            {
                return;
            }

            updateCollections(cell);

            //CollectionViewSource cvs;

            //// MOLECULES

            //// cyto_molecule_combo_box - filtered for bulk molecules in EntityRepository
            //cvs = (CollectionViewSource)(FindResource("availableBulkMoleculesListView"));
            //cvs.Source = new ObservableCollection<ConfigMolecule>();
            //cvs.Source = MainWindow.SOP.Protocol.entity_repository.molecules;
            
            //// memb_molecule_combo_box - filtered for boundary molecules in EntityRepository
            //cvs = (CollectionViewSource)(FindResource("availableBoundaryMoleculesListView"));
            //cvs.Source = new ObservableCollection<ConfigMolecule>();
            //cvs.Source = MainWindow.SOP.Protocol.entity_repository.molecules;

            //// list of cytosol molecules for use by division and differentitiation schemes
            //cvs = (CollectionViewSource)(FindResource("moleculesListView"));
            //cvs.Source = new ObservableCollection<ConfigMolecule>();
            //foreach (ConfigMolecularPopulation configMolpop in cell.cytosol.molpops)
            //{
            //    ((ObservableCollection<ConfigMolecule>)cvs.Source).Add(configMolpop.molecule);
            //}

            //// REACTIONS

            //// lvCellAvailableReacs
            //cvs = (CollectionViewSource)(FindResource("membraneAvailableReactionsListView"));
            //cvs.Source = new ObservableCollection<ConfigReaction>();
            //cvs.Source = MainWindow.SOP.Protocol.entity_repository.reactions;

            //// lvCytosolAvailableReacs
            //cvs = (CollectionViewSource)(FindResource("cytosolAvailableReactionsListView"));
            //cvs.Source = new ObservableCollection<ConfigReaction>();
            //cvs.Source = MainWindow.SOP.Protocol.entity_repository.reactions;

            //cvs = (CollectionViewSource)(FindResource("membraneAvailableReactionComplexesListView"));
            //cvs.Source = new ObservableCollection<ConfigReactionComplex>();
            //cvs.Source = MainWindow.SOP.Protocol.entity_repository.reaction_complexes;

            //cvs = (CollectionViewSource)(FindResource("cytosolAvailableReactionComplexesListView"));
            //cvs.Source = new ObservableCollection<ConfigReactionComplex>();
            //cvs.Source = MainWindow.SOP.Protocol.entity_repository.reaction_complexes;

            updateSelectedMoleculesAndGenes(cell);

            ////EventHandler cytosolEventHandler = null;
            ////cytosolEventHandler = new EventHandler(delegate
            ////{
            ////    if (CellCytosolMolPopsListBox.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            ////    {
            ////        ConfigCell currcell = DataContext as ConfigCell;
            ////        if (currcell != null)
            ////        {
            ////            CellCytosolMolPopsListBox.SelectedIndex = 0;
            ////            //updateCytoMolCollection();
            ////            //cyto_molecule_combo_box.SelectedItem = cell.cytosol.molpops.First().molecule;
            ////        }

            ////        CellCytosolMolPopsListBox.ItemContainerGenerator.StatusChanged -= cytosolEventHandler;
            ////    }
            ////});

            ////CellCytosolMolPopsListBox.ItemContainerGenerator.StatusChanged += cytosolEventHandler;

        }

        public void updateCollections(ConfigCell cell)
        {
            CollectionViewSource cvs;
            Level level = MainWindow.GetLevelContext(this);
            if (level == null)
            {
                level = CurrentLevel;
            }

            if (level == null)
            {
                //var sopTag = Tag as SystemOfPersistence;
                PushBetweenLevels pushwin = Window.GetWindow(this) as PushBetweenLevels;
                if (pushwin != null)
                {
                    CurrentLevel = pushwin.CurrentLevel;
                    level = pushwin.CurrentLevel;
                }
            }

            if (level == null)
                return;

            // MOLECULES

            // cyto_molecule_combo_box - filtered for bulk molecules in EntityRepository
            cvs = (CollectionViewSource)(FindResource("availableBulkMoleculesListView"));
            cvs.Source = new ObservableCollection<ConfigMolecule>();
            
            //cvs.Source = MainWindow.SOP.Protocol.entity_repository.molecules;
            cvs.Source = level.entity_repository.molecules;

            // memb_molecule_combo_box - filtered for boundary molecules in EntityRepository
            cvs = (CollectionViewSource)(FindResource("availableBoundaryMoleculesListView"));
            cvs.Source = new ObservableCollection<ConfigMolecule>();
            
            //cvs.Source = MainWindow.SOP.Protocol.entity_repository.molecules;
            cvs.Source = level.entity_repository.molecules;

            // list of cytosol molecules for use by division and differentitiation schemes
            cvs = (CollectionViewSource)(FindResource("moleculesListView"));
            cvs.Source = new ObservableCollection<ConfigMolecule>();
            foreach (ConfigMolecularPopulation configMolpop in cell.cytosol.molpops)
            {
                ((ObservableCollection<ConfigMolecule>)cvs.Source).Add(configMolpop.molecule);
            }

            // REACTIONS

            // lvCellAvailableReacs
            cvs = (CollectionViewSource)(FindResource("membraneAvailableReactionsListView"));
            cvs.Source = new ObservableCollection<ConfigReaction>();
            
            //cvs.Source = MainWindow.SOP.Protocol.entity_repository.reactions;
            cvs.Source = level.entity_repository.reactions;

            // lvCytosolAvailableReacs
            cvs = (CollectionViewSource)(FindResource("cytosolAvailableReactionsListView"));
            cvs.Source = new ObservableCollection<ConfigReaction>();
            
            //cvs.Source = MainWindow.SOP.Protocol.entity_repository.reactions;
            cvs.Source = level.entity_repository.reactions;

            cvs = (CollectionViewSource)(FindResource("membraneAvailableReactionComplexesListView"));
            cvs.Source = new ObservableCollection<ConfigReactionComplex>();
            
            //cvs.Source = MainWindow.SOP.Protocol.entity_repository.reaction_complexes;
            cvs.Source = level.entity_repository.reaction_complexes;

            cvs = (CollectionViewSource)(FindResource("cytosolAvailableReactionComplexesListView"));
            cvs.Source = new ObservableCollection<ConfigReactionComplex>();
            
            //cvs.Source = MainWindow.SOP.Protocol.entity_repository.reaction_complexes;
            cvs.Source = level.entity_repository.reaction_complexes;
        }

        //This is probably not needed any more but leaving here in case a problem occurs.
        //To be deleted for next checkin.
        public void updateSelectedMoleculesAndGenes(ConfigCell cell)
        {
            // Setting ListBox.SelectedItem = 0 in the xaml code only works the first time the tab is populated,
            // so do it manually here.

            CellMembraneMolPopsListBox.SelectedIndex = 0;
            //if (cell.membrane.molpops.Count > 0)
            //{
            //    memb_molecule_combo_box.SelectedItem = cell.membrane.molpops.First().molecule;
            //}

            CellCytosolMolPopsListBox.SelectedIndex = 0;
            //if (cell.cytosol.molpops.Count > 0)
            //{
            //    cyto_molecule_combo_box.SelectedItem = cell.cytosol.molpops.First().molecule;
            //}

            CellNucleusGenesListBox.SelectedItem = 0;
            //if (cell.genes.Count > 0)
            //{
            //    CellNucleusGenesListBox.SelectedItem = cell.genes.First();
            //}
        }

        /// <summary>
        /// This fixes the problem of selecting the 1st item in the mol pop list.
        /// This shouldn't be a problem in the first place, but the data binding seems to occur before the list is populated
        /// so the first item was not getting selected.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CellCytosolMolPopsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = sender as ListBox;
            if (lb.SelectedIndex == -1 && lb.Items.Count > 0)
            {
                lb.SelectedIndex = 0;
            }
        }

        private void CellMembraneMolPopsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = sender as ListBox;
            if (lb.SelectedIndex == -1 && lb.Items.Count > 0)
            {
                lb.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Switch between molecule-driven and distribution-driven transition driver elements for cell death.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChangeDeathTDEType_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null) return;

            if (cell.death_driver == null)
            {
                return;
            }

            if (cell.death_driver.DriverElements == null)
            {
                return;
            }

            ConfigTransitionDriverElement tde = cell.death_driver.DriverElements[0].elements[1];
            int CurrentState = tde.CurrentState,
                DestState = tde.DestState;
            string CurrentStateName = tde.CurrentStateName,
                    DestStateName = tde.DestStateName;

            if (tde.Type == TransitionDriverElementType.MOLECULAR)
            {
                // Switch to Distribution-driven
                tde = new ConfigDistrTransitionDriverElement();

                PoissonParameterDistribution poisson = new PoissonParameterDistribution();
                poisson.Mean = 1.0;
                ((ConfigDistrTransitionDriverElement)tde).Distr.ParamDistr = poisson;
                ((ConfigDistrTransitionDriverElement)tde).Distr.DistributionType = ParameterDistributionType.POISSON;
            }
            else
            {
                if (cell.cytosol.molpops.Count == 0)
                {
                    MessageBox.Show("Death can only be controlled by a probability distribution because there are no molecules in the cytosol. Add molecules from the store to control the death by molecular concentrations.", "No molecules available", MessageBoxButton.OK, MessageBoxImage.Information);
                }

                // Switch to Molecule-driven
                tde = new ConfigMolTransitionDriverElement();
            }
            tde.CurrentStateName = CurrentStateName;
            tde.DestStateName = DestStateName;
            tde.CurrentState = CurrentState;
            tde.DestState = DestState;
           
            cell.death_driver.DriverElements[0].elements[1] = tde;
        }

        private void ChangeDataGridTDEType_Click(object sender, RoutedEventArgs e)
        { 
            Button button = sender as Button;

            var stack_panel = FindVisualParent<StackPanel>(button);
            if (stack_panel == null) return;

            ConfigTransitionDriverElement tde = stack_panel.DataContext as ConfigTransitionDriverElement;
            if (tde == null) return;
    
            TransitionDriverElementType type = tde.Type;
            int CurrentState = tde.CurrentState,
                DestState = tde.DestState;
            string CurrentStateName = tde.CurrentStateName,
                    DestStateName = tde.DestStateName;

            if (tde.Type == TransitionDriverElementType.MOLECULAR)
            {
                tde = new ConfigDistrTransitionDriverElement();
                ((ConfigDistrTransitionDriverElement)tde).Distr.ParamDistr = null;
                ((ConfigDistrTransitionDriverElement)tde).Distr.DistributionType = ParameterDistributionType.CONSTANT;
                //stack_panel.DataContext = tde;
            }
            else
            {
                tde = new ConfigMolTransitionDriverElement();                
            }
            tde.CurrentStateName = CurrentStateName;
            tde.DestStateName = DestStateName;
            tde.CurrentState = CurrentState;
            tde.DestState = DestState;

            // update the transition scheme
            DataGrid dataGrid = (DataGrid)DiffSchemeDataGrid.FindVisualParent<DataGrid>(button);
            if (dataGrid == null) return;
            ConfigTransitionScheme scheme = DiffSchemeDataGrid.GetDiffSchemeSource(dataGrid);
            if (scheme != null)
            {
                scheme.Driver.DriverElements[CurrentState].elements[DestState] = tde;
            }
        }

        public static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            // get parent item
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);

            // we’ve reached the end of the tree
            if (parentObject == null) return null;

            // check if the parent matches the type we’re looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                // use recursion to proceed with next level
                return FindVisualParent<T>(parentObject);
            }
        }

        public static T FindLogicalParent<T>(DependencyObject child) where T : DependencyObject
        {
            // get parent item
            DependencyObject parentObject = LogicalTreeHelper.GetParent(child);

            // we’ve reached the end of the tree
            if (parentObject == null) return null;

            // check if the parent matches the type we’re looking for
            T parent = parentObject as T;
            if (parent != null)
            {
                return parent;
            }
            else
            {
                // use recursion to proceed with next level
                return FindLogicalParent<T>(parentObject);
            }
        }
        private void menu2PullFromProto_Click(object sender, RoutedEventArgs e)
        {
            Level level = MainWindow.GetLevelContext(this);
            if (level == null)
            {
                level = CurrentLevel;
            }
            ConfigReaction reac = (ConfigReaction)CytosolReacListBox.SelectedValue;

            //if (MainWindow.SOP.Protocol.entity_repository.reactions_dict.ContainsKey(reac.entity_guid))
            if (level.entity_repository.reactions_dict.ContainsKey(reac.entity_guid))
            {
                //ConfigReaction protReaction = MainWindow.SOP.Protocol.entity_repository.reactions_dict[reac.entity_guid];
                ConfigReaction protReaction = level.entity_repository.reactions_dict[reac.entity_guid];
                ConfigReaction newreac = protReaction.Clone(true);

                ConfigCell cell = DataContext as ConfigCell;
                cell.cytosol.Reactions.Remove(reac);
                cell.cytosol.Reactions.Add(newreac);
            }
        }

        private void menuMembPushReacToProto_Click(object sender, RoutedEventArgs e)
        {
            if (MembReacListBox.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a reaction.");
                return;
            }

            ConfigReaction reac = (ConfigReaction)MembReacListBox.SelectedValue;
            ConfigReaction newreac = reac.Clone(true);
            MainWindow.GenericPush(newreac);
        }

        private void menuMembPullReacFromProto_Click(object sender, RoutedEventArgs e)
        {
            ConfigReaction reac = (ConfigReaction)MembReacListBox.SelectedValue;
            Level level = MainWindow.GetLevelContext(this);
            if (level == null)
            {
                level = CurrentLevel;
            }

            //if (MainWindow.SOP.Protocol.entity_repository.reactions_dict.ContainsKey(reac.entity_guid))
            if (level.entity_repository.reactions_dict.ContainsKey(reac.entity_guid))
            {
                //ConfigReaction protReaction = MainWindow.SOP.Protocol.entity_repository.reactions_dict[reac.entity_guid];
                ConfigReaction protReaction = level.entity_repository.reactions_dict[reac.entity_guid];
                ConfigReaction newreac = protReaction.Clone(true);

                ConfigCell cell = DataContext as ConfigCell;
                cell.membrane.Reactions.Remove(reac);
                cell.membrane.Reactions.Add(newreac);
            }
        }

        private void comboDeathMolPop2_Loaded(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;

            if (cell == null)
                return;

            //Don't do anything if driver type is distribution
            if (cell.death_driver.DriverElements[0].elements[1].Type == TransitionDriverElementType.DISTRIBUTION)
                return;

            ComboBox combo = sender as ComboBox;

            //If no death molecule selected, and there are bulk molecules, select 1st molecule.
            if (combo.SelectedIndex == -1 && combo.Items.Count > 0)
            {
                combo.SelectedIndex = 0;
            }
            //If no death molecule selected, and there are NO bulk molecules, issue a warning to acquire molecules from the user store.
            else if (combo.SelectedIndex == -1 && combo.Items.Count == 0)
            {
                //Since there are no molecules, create a default DISTRIBUTION driver and assign it.
                ConfigTransitionDriverElement tde = new ConfigDistrTransitionDriverElement();
                PoissonParameterDistribution poisson = new PoissonParameterDistribution();

                poisson.Mean = 1.0;
                ((ConfigDistrTransitionDriverElement)tde).Distr.ParamDistr = poisson;
                ((ConfigDistrTransitionDriverElement)tde).Distr.DistributionType = ParameterDistributionType.POISSON;

                cell.death_driver.DriverElements[0].elements[1] = tde;
            }
        }

        private void MembCreateNewReaction_Expanded(object sender, RoutedEventArgs e)
        {
            this.BringIntoView();
        }

        private void CytoSaveReacCompToProtocolButton_Click(object sender, RoutedEventArgs e)
        {
            if (CytoReactionComplexListBox.SelectedIndex < 0)
                return;

            ConfigReactionComplex crc = ((ConfigReactionComplex)(CytoReactionComplexListBox.SelectedItem));

            ConfigReactionComplex newcrc = crc.Clone(true);
            MainWindow.GenericPush(newcrc);
        }

        private void MembSaveReacCompToProtocolButton_Click(object sender, RoutedEventArgs e)
        {
            if (MembReactionComplexListBox.SelectedIndex < 0)
                return;

            ConfigReactionComplex crc = ((ConfigReactionComplex)(MembReactionComplexListBox.SelectedItem));

            ConfigReactionComplex newcrc = crc.Clone(true);
            MainWindow.GenericPush(newcrc);
        }

        private void DiffSchemeExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();

        }

        private void DivSchemeExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }

        private void MembRCDetailsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }

        private void CellMolPopsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }

        private void CellReacExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }

        private void ReacCompExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }

        private void CellDeathExpander_Expanded(object sender, RoutedEventArgs e)
        {
            FrameworkElement element = sender as FrameworkElement;
            if (element == null)
                return;

            element.BringIntoView();
        }

    }

}



        
