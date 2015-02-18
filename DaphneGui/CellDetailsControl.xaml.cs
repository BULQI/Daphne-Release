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
        public CellDetailsControl()
        {
            InitializeComponent();

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

            //if user picked 'new molecule' then create new molecule in ER
            if (nIndex == (cb.Items.Count - 1))
            {
                ConfigMolecule newLibMol = new ConfigMolecule();
                newLibMol.Name = newLibMol.GenerateNewName(MainWindow.SOP.Protocol, "_New");
                newLibMol.molecule_location = MoleculeLocation.Boundary;
                AddEditMolecule aem = new AddEditMolecule(newLibMol, MoleculeDialogType.NEW);

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
                MainWindow.SOP.Protocol.entity_repository.molecules.Add(newLibMol);
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
                MessageBox.Show("Please add more molecules from the User store.");
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

            MessageBoxResult res = MessageBox.Show("Removing this molecular population will remove cell reactions that use this molecule. Are you sure you would like to proceed?", "Warning", MessageBoxButton.YesNo);
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

            foreach (ConfigReaction cr in cell.membrane.Reactions.ToList())
            {
                if (cr.HasMolecule(cmp.molecule.entity_guid))
                {
                    cell.membrane.Reactions.Remove(cr);
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

            MessageBoxResult res = MessageBox.Show("Removing this molecular population will remove cell reactions that use this molecule. Are you sure you would like to proceed?", "Warning", MessageBoxButton.YesNo);
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
            ConfigGene gene = new ConfigGene("g", 0, 0);
            gene.Name = gene.GenerateNewName(MainWindow.SOP.Protocol, "New");

            ConfigCell cell = DataContext as ConfigCell;
            cell.genes.Add(gene);
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
            AddGeneToCell ads = new AddGeneToCell(cell);

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

                cell.genes.Add(geneToAdd);
            }

            txtGeneName.IsEnabled = false;
        }

        private void NucleusRemoveGeneButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = DataContext as ConfigCell;
            ConfigGene gene = (ConfigGene)CellNucleusGenesListBox.SelectedItem;

            MessageBoxResult res = MessageBox.Show("Are you sure you would like to remove this gene from this cell?", "Warning", MessageBoxButton.YesNo);

            if (res == MessageBoxResult.No)
                return;

            if (cell.HasGene(gene.entity_guid)) {
                cell.genes.Remove(gene);
            }

            txtGeneName.IsEnabled = false;
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
        }

        private void MembraneAddReacButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cc = DataContext as ConfigCell;
            bool needRefresh = false;

            foreach (var item in lvCellAvailableReacs.SelectedItems)
            {
                ConfigReaction cr = (ConfigReaction)item;
                if (cc != null && cr != null)
                {
                    if (cc.membrane.reactions_dict.ContainsKey(cr.entity_guid) == false)
                    {
                        cc.membrane.Reactions.Add(cr.Clone(true));

                        needRefresh = true;
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

            //if user picked 'new molecule' then create new molecule in ER
            if (nIndex == (cb.Items.Count - 1))
            {
                ConfigMolecule newLibMol = new ConfigMolecule();
                newLibMol.Name = newLibMol.GenerateNewName(MainWindow.SOP.Protocol, "_New");
                AddEditMolecule aem = new AddEditMolecule(newLibMol, MoleculeDialogType.NEW);

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
                MainWindow.SOP.Protocol.entity_repository.molecules.Add(newLibMol);
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

            ObservableCollection<string> membBound = new ObservableCollection<string>();
            ObservableCollection<string> gene_guids = new ObservableCollection<string>();
            ObservableCollection<string> bulk = new ObservableCollection<string>();
            EntityRepository er = MainWindow.SOP.Protocol.entity_repository;

            foreach (string molguid in cr.reactants_molecule_guid_ref)
                if (er.molecules_dict.ContainsKey(molguid) && er.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                    membBound.Add(molguid);
                else if (er.genes_dict.ContainsKey(molguid))
                    gene_guids.Add(molguid);
                else
                    bulk.Add(molguid);

            foreach (string molguid in cr.products_molecule_guid_ref)
                if (er.molecules_dict.ContainsKey(molguid) && er.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                    membBound.Add(molguid);
                else if (er.genes_dict.ContainsKey(molguid))
                    gene_guids.Add(molguid);
                else
                    bulk.Add(molguid);

            foreach (string molguid in cr.modifiers_molecule_guid_ref)
                if (er.molecules_dict.ContainsKey(molguid) && er.molecules_dict[molguid].molecule_location == MoleculeLocation.Boundary)
                    membBound.Add(molguid);
                else if (er.genes_dict.ContainsKey(molguid))
                    gene_guids.Add(molguid);
                else
                    bulk.Add(molguid);

            bool bOK = true;
            bool bTranscription = false;

            if (er.reaction_templates_dict[cr.reaction_template_guid_ref].reac_type == ReactionType.Transcription)
            {
                bTranscription = bulk.Count > 0 && gene_guids.Count > 0 && cc.HasGenes(gene_guids) && cc.cytosol.HasMolecules(bulk);
                if (bTranscription == true)
                {
                    bOK = true;
                }
                else
                {
                    bOK = false;
                }
            }
            else
            {
                if (bulk.Count <= 0)
                    bOK = false;

                if (bOK && membBound.Count > 0)
                    bOK = cc.membrane.HasMolecules(membBound);

                if (bOK)
                    bOK = cc.cytosol.HasMolecules(bulk);

            }

            //Finally, if the cell cytosol already contains this reaction, exclude it from the available reactions list
            if (bOK == true)
            {
                if (cc.cytosol.reactions_dict.ContainsKey(cr.entity_guid))
                {
                    bOK = false;
                }
            }

            e.Accepted = bOK;
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

            //This filter is called for every reaction in the repository.
            //For current reaction, if all of its molecules are in the membrane, then the reaction should be included.
            //Otherwise, exclude it.

            //First check if all the molecules in the reactants list exist in the membrane
            bool bOK = false;
            bOK = cc.membrane.HasMolecules(cr.reactants_molecule_guid_ref);

            //If bOK is true, that means the molecules in the reactants list all exist in the membrane
            //Now check the products list
            if (bOK)
                bOK = cc.membrane.HasMolecules(cr.products_molecule_guid_ref);

            //Loop through modifiers list
            if (bOK)
                bOK = cc.membrane.HasMolecules(cr.modifiers_molecule_guid_ref);

            //Finally, if the cell membrane already contains this reaction, exclude it from the available reactions list
            if (bOK == true)
            {
                if (cc.membrane.reactions_dict.ContainsKey(cr.entity_guid))
                {
                    bOK = false;
                }
            }

            e.Accepted = bOK;
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

            //if already in cytosol, return
            if (cc.membrane.reaction_complexes_dict.ContainsKey(crc.entity_guid))
            {
                e.Accepted = false;
                return;
            }

            //This filter is called for every reaction complex in the repository.
            //For current reaction complex, if all of its reactions are in the membrane, then the reaction complex should be included.
            //Otherwise, exclude it.

            //Check if all the reactions in the reaction complex reaction list exist in the membrane
            bool bOK = true;

            foreach (ConfigReaction cr in crc.reactions)
            {
                string guid = cr.entity_guid;
                if (cc.membrane.GetReaction(guid) == null)
                {
                    //if even one reaction is not found in cell cytosol, return false
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

            //This filter is called for every reaction complex in the repository.
            //For current reaction complex, if all of its reactions are in the cytosol, then the reaction complex should be included.
            //Otherwise, exclude it.

            //Check if all the reactions in the reaction complex reaction list exist in the cytosol
            bool bOK = true;

            foreach (ConfigReaction cr in crc.reactions)
            {
                string guid = cr.entity_guid;
                if (cc.cytosol.GetReaction(guid) == null)
                {
                    //if even one reaction is not found in cell cytosol, return false
                    bOK = false;
                    break;
                }
            }

            e.Accepted = bOK;
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
                ProtocolCreators.LoadConfigTransitionDriverElements(config_td, signal, alpha, beta, stateName, MainWindow.SOP.Protocol);
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
        public DataGridTextColumn CreateUnusedGenesColumn()
        {
            EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
            DataGridTextColumn editor_col = new DataGridTextColumn();
            editor_col.CanUserSort = false;
            DataTemplate HeaderTemplate = new DataTemplate();

            CollectionViewSource cvs1 = new CollectionViewSource();
            cvs1.SetValue(CollectionViewSource.SourceProperty, er.genes);
            cvs1.Filter += new FilterEventHandler(unusedGenesListView_Filter);

            CompositeCollection coll1 = new CompositeCollection();
            ConfigGene dummyItem = new ConfigGene("Add a gene", 0, 0);
            coll1.Add(dummyItem);
            CollectionContainer cc1 = new CollectionContainer();
            cc1.Collection = cvs1.View;
            coll1.Add(cc1);

            FrameworkElementFactory addGenesCombo = new FrameworkElementFactory(typeof(ComboBox));
            addGenesCombo.SetValue(ComboBox.WidthProperty, 100D);
            addGenesCombo.SetValue(ComboBox.ItemsSourceProperty, coll1);
            addGenesCombo.SetValue(ComboBox.DisplayMemberPathProperty, "Name");
            addGenesCombo.SetValue(ComboBox.ToolTipProperty, "Click here to add another gene column to the grid.");
            addGenesCombo.AddHandler(ComboBox.SelectionChangedEvent, new SelectionChangedEventHandler(comboAddGeneToEpigeneticMap_SelectionChanged));

            addGenesCombo.SetValue(ComboBox.SelectedIndexProperty, 0);

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
                //this is the new way to creating datagrid dyamically with diffscheme specified in the grid
                //otherwise, it is the old way, remove those code when all changed to this new way.
                ConfigGene gene1 = (ConfigGene)combo.SelectedItem;
                if (gene1 == null)
                    return;

                if (scheme.genes.Contains(gene1.entity_guid)) return; //shouldnot happen...

                //If no states exist, then create at least 2 new ones
                if (scheme.Driver.states.Count == 0)
                {
                    scheme.AddState("state1");
                    scheme.AddState("state2");
                }

                scheme.AddGene(gene1.entity_guid);
              
                //force refresh
                //dataGrid.GetBindingExpression(DiffSchemeDataGrid.DiffSchemeSourceProperty).UpdateTarget();
                return;
            }
        }

        /// <summary>
        /// This method adds a differentiation state given a name 
        /// </summary>
        /// <param name="stateName"></param>
        private void AddDifferentiationState(string schemeName, string stateName)
        {
            EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)return;

            ConfigTransitionScheme new_scheme = null;
            if (schemeName == "Division")
            {
                new_scheme = cell.div_scheme;
                if (new_scheme == null)
                {
                    cell.div_scheme = new_scheme = new ConfigTransitionScheme();
                }
            }
            else if (schemeName == "Differentiation")
            {
                new_scheme = cell.diff_scheme;
                if (new_scheme == null)
                {
                    cell.diff_scheme = new_scheme = new ConfigTransitionScheme();
                }
            }
            else return;

            new_scheme.AddState(stateName);

            //refresh display
            if (schemeName == "Division")
            {
                cell.div_scheme = null;
                cell.div_scheme = new_scheme;
            }
            else if (schemeName == "Differentiation")
            {
                cell.diff_scheme = null;
                cell.diff_scheme = new_scheme;
            }
        }

        private void btnNewDiffScheme_Click(object sender, RoutedEventArgs e)
        {

            string schemeName = ((Button)sender).Tag as string;
            if (schemeName == null) return;
            AddDifferentiationState(schemeName, "State0");
            AddDifferentiationState(schemeName, "State1");
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

            ToolWinTissue twt = Tag as ToolWinTissue;
            CellPopulation cp = twt.CellPopControl.CellPopsListBox.SelectedItems[0] as CellPopulation;

            if (schemeName == "Division")
            {
                cell.div_scheme = null;
                if (cp != null)
                {
                    cp.reportStates.Division = false;
                }
            }
            else if (schemeName == "Differentiation")
            {
                cell.diff_scheme = null;
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

            //if gene is not in the cell's nucleus, then exclude it from the available gene pool
            if (!cell.HasGene(gene.entity_guid))
                return;


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
                if (cell.membrane.reaction_complexes_dict.ContainsKey(crc.entity_guid) == false)
                {
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
        }

        private void CytoAddReacCxButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigReactionComplex crc = (ConfigReactionComplex)lbCytoAvailableReacCx.SelectedItem;
            ConfigCell cell = DataContext as ConfigCell;

            if (crc != null)
            {
                if (cell.cytosol.reaction_complexes_dict.ContainsKey(crc.entity_guid) == false)
                {
                    cell.cytosol.reaction_complexes.Add(crc.Clone(true));
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
            // cyto_molecule_combo_box
            CollectionViewSource cvs = (CollectionViewSource)(FindResource("availableBulkMoleculesListView"));
            cvs.Filter += ToolWinBase.FilterFactory.BulkMolecules_Filter;

            // memb_molecule_combo_box
            cvs = (CollectionViewSource)(FindResource("availableBoundaryMoleculesListView"));
            cvs.Filter += ToolWinBase.FilterFactory.BoundaryMolecules_Filter;

            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)
            {
                return;
            }

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

            // MOLECULES

            // cyto_molecule_combo_box - filtered for bulk molecules in EntityRepository
            cvs = (CollectionViewSource)(FindResource("availableBulkMoleculesListView"));
            cvs.Source = new ObservableCollection<ConfigMolecule>();
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.molecules;

            // memb_molecule_combo_box - filtered for boundary molecules in EntityRepository
            cvs = (CollectionViewSource)(FindResource("availableBoundaryMoleculesListView"));
            cvs.Source = new ObservableCollection<ConfigMolecule>();
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.molecules;

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
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.reactions;

            // lvCytosolAvailableReacs
            cvs = (CollectionViewSource)(FindResource("cytosolAvailableReactionsListView"));
            cvs.Source = new ObservableCollection<ConfigReaction>();
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.reactions;

            cvs = (CollectionViewSource)(FindResource("membraneAvailableReactionComplexesListView"));
            cvs.Source = new ObservableCollection<ConfigReactionComplex>();
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.reaction_complexes;

            cvs = (CollectionViewSource)(FindResource("cytosolAvailableReactionComplexesListView"));
            cvs.Source = new ObservableCollection<ConfigReactionComplex>();
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.reaction_complexes;
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
                ((ConfigDistrTransitionDriverElement)tde).Distr.ParamDistr = new PoissonParameterDistribution();
                ((ConfigDistrTransitionDriverElement)tde).Distr.DistributionType = ParameterDistributionType.POISSON;
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
            ConfigReaction reac = (ConfigReaction)CytosolReacListBox.SelectedValue;
            if (MainWindow.SOP.Protocol.entity_repository.reactions_dict.ContainsKey(reac.entity_guid))
            {
                ConfigReaction protReaction = MainWindow.SOP.Protocol.entity_repository.reactions_dict[reac.entity_guid];
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

            if (MainWindow.SOP.Protocol.entity_repository.reactions_dict.ContainsKey(reac.entity_guid))
            {
                ConfigReaction protReaction = MainWindow.SOP.Protocol.entity_repository.reactions_dict[reac.entity_guid];
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
                MessageBox.Show("There are no molecules in the cytosol. Please get molecules from the store.", "No molecules available", MessageBoxButton.OK, MessageBoxImage.Information);

                //Since there are no molecules, create a default DISTRIBUTION driver and assign it.
                ConfigTransitionDriverElement tde = new ConfigDistrTransitionDriverElement();
                PoissonParameterDistribution poisson = new PoissonParameterDistribution();

                poisson.Mean = 1.0;
                ((ConfigDistrTransitionDriverElement)tde).Distr.ParamDistr = poisson;
                ((ConfigDistrTransitionDriverElement)tde).Distr.DistributionType = ParameterDistributionType.POISSON;

                cell.death_driver.DriverElements[0].elements[1] = tde;
            }
        }
        
    }

}



        
