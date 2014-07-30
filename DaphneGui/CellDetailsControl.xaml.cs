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

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CollectionViewSource cvs = (CollectionViewSource)(FindResource("cytoGenes2ListView"));
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.genes;

            cvs = (CollectionViewSource)(FindResource("boundaryMoleculesListView"));
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.molecules;

            cvs = (CollectionViewSource)(FindResource("cytosolAvailableReactionsListView"));
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.reactions;

            cvs = (CollectionViewSource)(FindResource("membraneAvailableReactionsListView"));
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.reactions;

            cvs = (CollectionViewSource)(FindResource("ecmAvailableReactionsListView"));
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.reactions;

            cvs = (CollectionViewSource)(FindResource("newBulkMoleculesListView"));
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.molecules;
            cvs.Filter += FilterFactory.bulkMoleculesListView_Filter;
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
                    molpop.Name = new_mol_name;
            }
            
        }

        private void memb_molecule_combo_box_GotFocus(object sender, RoutedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            CollectionViewSource.GetDefaultView(combo.ItemsSource).Refresh();
        }

        private void CytosolAddMolButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecularPopulation cmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
            cmp.Name = "NewMP";
            cmp.mp_dist_name = "New distribution";
            cmp.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 1.0f, 0.2f);
            cmp.mp_render_on = true;
            cmp.mp_distribution = new MolPopHomogeneousLevel();
            //cmp.molecule = MainWindow.SOP.Protocol.entity_repository.molecules.First().Clone(null);

            CollectionViewSource cvs = (CollectionViewSource)(FindResource("newBulkMoleculesListView"));
            ObservableCollection<ConfigMolecule> mol_list = cvs.Source as ObservableCollection<ConfigMolecule>;
            if (mol_list != null)
            {
                cmp.molecule = mol_list.First().Clone(null);
            }
            else
            {
                return;
            }

            //ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)
                return;

            cell.cytosol.molpops.Add(cmp);
            CellCytosolMolPopsListBox.SelectedIndex = CellCytosolMolPopsListBox.Items.Count - 1;
        }

        private void CytosolRemoveMolButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecularPopulation cmp = (ConfigMolecularPopulation)CellCytosolMolPopsListBox.SelectedItem;

            if (cmp == null)
                return;

            MessageBoxResult res = MessageBox.Show("Removing this molecular population will remove cell reactions that use this molecule. Are you sure you would like to proceed?", "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;

            //ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            ConfigCell cell = DataContext as ConfigCell;

            foreach (ConfigReaction cr in cell.cytosol.Reactions.ToList())
            {
                if (cr.HasMolecule(cmp.molecule.entity_guid))
                {
                    cell.cytosol.Reactions.Remove(cr);
                }
            }

            //added 1/10/14
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
            ConfigMolecularPopulation cmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
            cmp.Name = "NewMP";
            cmp.mp_dist_name = "New distribution";
            cmp.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 1.0f, 0.2f);
            cmp.mp_render_on = true;
            cmp.mp_distribution = new MolPopHomogeneousLevel();

            CollectionViewSource cvs = (CollectionViewSource)(FindResource("newBoundaryMoleculesListView"));
            //cvs.Filter += new FilterEventHandler(boundaryMoleculesListView_Filter);

            ObservableCollection<ConfigMolecule> mol_list = new ObservableCollection<ConfigMolecule>();
            mol_list = cvs.Source as ObservableCollection<ConfigMolecule>;
            if (mol_list != null)
            {
                cmp.molecule = mol_list.First().Clone(null);
            }
            else
            {
                return;
            }

            //ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)
                return;

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
            //added 1/10/14
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
        }

        private void NucleusNewGeneButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigGene gene = new ConfigGene("NewGene", 0, 0);
            gene.Name = gene.GenerateNewName(MainWindow.SOP.Protocol, "_New");
            MainWindow.SOP.Protocol.entity_repository.genes.Add(gene);
            //ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            ConfigCell cell = DataContext as ConfigCell;
            cell.genes_guid_ref.Add(gene.entity_guid);
            //CollectionViewSource.GetDefaultView(CellNucleusGenesListBox.ItemsSource).Refresh();
            CellNucleusGenesListBox.SelectedIndex = CellNucleusGenesListBox.Items.Count - 1;

            string guid = (string)CellNucleusGenesListBox.SelectedItem;
            CellNucleusGenesListBox.ScrollIntoView(guid);
            txtGeneName.IsEnabled = true;
        }

        private void NucleusAddGeneButton_Click(object sender, RoutedEventArgs e)
        {
            //Get selected cell
            //ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            ConfigCell cell = DataContext as ConfigCell;

            //if no cell selected, return
            if (cell == null)
                return;

            //Show a dialog that gets the new gene's name
            AddGeneToCell ads = new AddGeneToCell(cell);

            //If user clicked 'apply' and not 'cancel'
            if (ads.ShowDialog() == true)
            {
                ConfigGene geneToAdd = ads.SelectedGene;
                if (geneToAdd == null)
                    return;

                cell.genes_guid_ref.Add(geneToAdd.entity_guid);
            }

            txtGeneName.IsEnabled = false;
        }

        private void NucleusRemoveGeneButton_Click(object sender, RoutedEventArgs e)
        {
            //ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            ConfigCell cell = DataContext as ConfigCell;
            string gene_guid = (string)CellNucleusGenesListBox.SelectedItem;

            if (gene_guid == "")
                return;

            MessageBoxResult res = MessageBox.Show("Are you sure you would like to remove this gene from this cell?", "Warning", MessageBoxButton.YesNo);

            if (res == MessageBoxResult.No)
                return;

            if (cell.genes_guid_ref.Contains(gene_guid))
            {
                cell.genes_guid_ref.Remove(gene_guid);
            }

            txtGeneName.IsEnabled = false;
        }

        private void MembraneRemoveReacButton_Click(object sender, RoutedEventArgs e)
        {
            //ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
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
            ////ConfigCell cc = (ConfigCell)CellsListBox.SelectedItem;
            ConfigCell cc = DataContext as ConfigCell;
            bool needRefresh = false;

            foreach (var item in lvCellAvailableReacs.SelectedItems)
            {
                ConfigReaction cr = (ConfigReaction)item;
                if (cc != null && cr != null)
                {
                    if (cc.membrane.Reactions.Contains(cr) == false)
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
            ReacParams2.IsExpanded = true;
        }

        private void CytosolRemoveReacButton_Click(object sender, RoutedEventArgs e)
        {
            //ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
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
            ////ConfigCell cc = (ConfigCell)CellsListBox.SelectedItem;
            ConfigCell cc = DataContext as ConfigCell;
            bool needRefresh = false;

            foreach (var item in lvCytosolAvailableReacs.SelectedItems)
            {
                ConfigReaction cr = (ConfigReaction)item;
                if (cc != null && cr != null)
                {
                    //Add to reactions list only if the cell does not already contain this reaction
                    if (cc.cytosol.reaction_complexes_guid_ref.Contains(cr.entity_guid) == false)
                    {
                        cc.cytosol.Reactions.Add(cr.Clone(true));

                        needRefresh = true;
                    }
                }
            }

            //Refresh the filter
            if (needRefresh && lvCytosolAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();
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

        private void boundaryMoleculesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigMolecule mol = e.Item as ConfigMolecule;
            e.Accepted = true;

            if (mol != null)
            {
                // Filter out mol if membrane bound 
                if (mol.molecule_location == MoleculeLocation.Boundary)
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }

        private void blob_actor_checkbox_clicked(object sender, RoutedEventArgs e)
        {
            CheckBox cb = e.OriginalSource as CheckBox;

            if (cb.CommandParameter == null)
                return;

            string guid = cb.CommandParameter as string;
            if (guid.Length > 0)
            {
                if (MainWindow.SOP.Protocol.scenario.gauss_guid_gauss_dict.ContainsKey(guid))
                {
                    GaussianSpecification gs = MainWindow.SOP.Protocol.scenario.gauss_guid_gauss_dict[guid];
                    gs.gaussian_region_visibility = (bool)(cb.IsChecked);
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
                    molpop.Name = new_mol_name;
            }

            //ConfigMolecule mol = (ConfigMolecule)cb.SelectedItem;
            //molpop.molecule = mol.Clone(null);

            //string new_mol_name = mol.Name;
            //if (curr_mol_guid != molpop.molecule.entity_guid)
            //    molpop.Name = new_mol_name;

            CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();
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

            ////ConfigCell cc = CellsListBox.SelectedItem as ConfigCell;

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
            if (cc.cytosol.Reactions.Contains(cr))
                bOK = false;

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

            ////ConfigCell cc = CellsListBox.SelectedItem as ConfigCell;

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
            if (cc.membrane.Reactions.Contains(cr))
                bOK = false;

            e.Accepted = bOK;
        }

        private void ecmAvailableReactionsListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigReaction cr = e.Item as ConfigReaction;

            bool bOK = false;
            foreach (string molguid in cr.reactants_molecule_guid_ref)
            {
                if (EcmHasMolecule(molguid) || CellPopsHaveMoleculeInMemb(molguid))
                {
                    bOK = true;
                }
                else
                {
                    bOK = false;
                    break;
                }
            }
            if (bOK)
            {
                foreach (string molguid in cr.products_molecule_guid_ref)
                {
                    if (EcmHasMolecule(molguid) || CellPopsHaveMoleculeInMemb(molguid))
                    {
                        bOK = true;
                    }
                    else
                    {
                        bOK = false;
                        break;
                    }
                }
            }
            if (bOK)
            {
                foreach (string molguid in cr.modifiers_molecule_guid_ref)
                {
                    if (EcmHasMolecule(molguid) || CellPopsHaveMoleculeInMemb(molguid))
                    {
                        bOK = true;
                    }
                    else
                    {
                        bOK = false;
                        break;
                    }
                }
            }

            //Finally, if the ecm already contains this reaction, exclude it from the available reactions list
            if (MainWindow.SOP.Protocol.scenario.environment.ecs.Reactions.Contains(cr))
                bOK = false;

            e.Accepted = bOK;
        }

        private void bulkMoleculesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigMolecule mol = e.Item as ConfigMolecule;
            if (mol != null)
            {
                // Filter out mol if membrane bound 
                if (mol.molecule_location == MoleculeLocation.Bulk)
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }

        private bool EcmHasMolecule(string molguid)
        {
            foreach (ConfigMolecularPopulation molpop in MainWindow.SOP.Protocol.scenario.environment.ecs.molpops)
            {
                if (molpop.molecule.entity_guid == molguid)
                    return true;
            }
            return false;
        }

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
            foreach (CellPopulation cell_pop in MainWindow.SOP.Protocol.scenario.cellpopulations)
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
            foreach (CellPopulation cell_pop in MainWindow.SOP.Protocol.scenario.cellpopulations)
            {
                ConfigCell cell = MainWindow.SOP.Protocol.entity_repository.cells_dict[cell_pop.Cell.entity_guid];
                if (CytosolHasMolecule(cell, molguid))
                    return true;
            }

            return ret;
        }

        




       
    }
}
