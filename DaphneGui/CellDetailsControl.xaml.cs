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

            DataGridDiffScheme dgds = new DataGridDiffScheme(this);

        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CollectionViewSource cvs = (CollectionViewSource)(FindResource("cytoGenes2ListView"));
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.genes;

            cvs = (CollectionViewSource)(FindResource("boundaryMoleculesListView"));
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.molecules;

            cvs = (CollectionViewSource)(FindResource("moleculesListView"));
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.molecules;

            cvs = (CollectionViewSource)(FindResource("cytosolAvailableReactionsListView"));
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.reactions;

            cvs = (CollectionViewSource)(FindResource("membraneAvailableReactionsListView"));
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.reactions;

            cvs = (CollectionViewSource)(FindResource("ecmAvailableReactionsListView"));
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.reactions;

            cvs = (CollectionViewSource)(FindResource("CytosolBulkMoleculesListView"));
            cvs.Source = MainWindow.SOP.Protocol.entity_repository.molecules;
            cvs.Filter += FilterFactory.bulkMoleculesListView_Filter;

            DiffSchemeExpander_Expanded(null, null);
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
            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)
                return;

            ConfigMolecularPopulation cmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
            cmp.Name = "NewMP";
            cmp.mp_dist_name = "New distribution";
            cmp.mp_color = System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 1.0f, 0.2f);
            cmp.mp_render_on = true;
            cmp.mp_distribution = new MolPopHomogeneousLevel();
            cmp.molecule = null;
            //cmp.molecule = MainWindow.SOP.Protocol.entity_repository.molecules.First().Clone(null);

            CollectionViewSource cvs = (CollectionViewSource)(FindResource("CytosolBulkMoleculesListView"));
            foreach (ConfigMolecule item in cvs.View)
            {
                if (cell.cytosol.molpops.Where(m => m.molecule.Name == item.Name).Any()) continue;
                cmp.molecule = item;
                break;
            }
            if (cmp.molecule == null) return;

            ////ObservableCollection<ConfigMolecule> mol_list = cvs.Source as ObservableCollection<ConfigMolecule>;
            ////if (mol_list != null)
            ////{
            ////    cmp.molecule = mol_list.First().Clone(null);
            ////}
            ////else
            ////{
            ////    return;
            ////}

            cell.cytosol.molpops.Add(cmp);
            CellCytosolMolPopsListBox.SelectedIndex = CellCytosolMolPopsListBox.Items.Count - 1;
        }

        private void CellMembraneMolPopsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CollectionViewSource cvs = (CollectionViewSource)(FindResource("boundaryMoleculesListView"));
            if (cvs == null || cvs.View == null)
                return;

            cvs.View.Refresh();

            if (e.AddedItems.Count == 0) return;
            var tmp = e.AddedItems[0] as ConfigMolecularPopulation;
            //var tmp = (sender as ComboBox).SelectedItem as ConfigMolecularPopulation;
            foreach (ConfigMolecule cm in cvs.View)
            {
                if (cm.Name == tmp.molecule.Name)
                {
                    cvs.View.MoveCurrentTo(cm);
                    return;
                }
            }
        }



        private void CellCytosolMolPopsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CollectionViewSource cvs = (CollectionViewSource)(FindResource("CytosolBulkMoleculesListView"));
            if (cvs == null || cvs.View == null)
                return;

            cvs.View.Refresh();

            if (e.AddedItems.Count == 0) return;
            var tmp = e.AddedItems[0] as ConfigMolecularPopulation;
            //var tmp = (sender as ComboBox).SelectedItem as ConfigMolecularPopulation;
            foreach (ConfigMolecule cm in cvs.View)
            {
                if (cm.Name == tmp.molecule.Name)
                {
                    cvs.View.MoveCurrentTo(cm);
                    return;
                }
            }
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
            //ReacParams2.IsExpanded = true;
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

        private void menu2PushToProto_Click(object sender, RoutedEventArgs e)
        {
            ListBox lb = sender as ListBox;
            if (lb.SelectedItems.Count == 0)
            {
                MessageBox.Show("No reactions selected");
            }
        }

        private void btnNewDeathDriver_Click(object sender, RoutedEventArgs e)
        {
            //ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
            ConfigCell cell = DataContext as ConfigCell;

            if (cell == null)
                return;

            if (cell.death_driver == null)
            {
                //I think this is what we want
                ConfigTransitionDriver config_td = new ConfigTransitionDriver();
                config_td.Name = "generic apoptosis";
                string[] stateName = new string[] { "alive", "dead" };
                string[,] signal = new string[,] { { "", "" }, { "", "" } };
                double[,] alpha = new double[,] { { 0, 0 }, { 0, 0 } };
                double[,] beta = new double[,] { { 0, 0 }, { 0, 0 } };
                ProtocolCreators.LoadConfigTransitionDriverElements(config_td, signal, alpha, beta, stateName, MainWindow.SOP.Protocol);
                config_td.CurrentState = 0;
                config_td.StateName = config_td.states[config_td.CurrentState];
                cell.death_driver = config_td;
            }
        }

        private void btnDelDeathDriver_Click(object sender, RoutedEventArgs e)
        {
            //ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
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

        }

        private void btnNewDivDriver_Click(object sender, RoutedEventArgs e)
        {
            //ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
            ConfigCell cell = DataContext as ConfigCell;

            if (cell == null)
                return;


            if (cell.div_driver == null)
            {
                //I think this is what we want
                ConfigTransitionDriver config_td = new ConfigTransitionDriver();
                config_td.Name = "generic division";
                string[] stateName = new string[] { "quiescent", "mitotic" };
                string[,] signal = new string[,] { { "", "" }, { "", "" } };
                double[,] alpha = new double[,] { { 0, 0 }, { 0, 0 } };
                double[,] beta = new double[,] { { 0, 0 }, { 0, 0 } };
                ProtocolCreators.LoadConfigTransitionDriverElements(config_td, signal, alpha, beta, stateName, MainWindow.SOP.Protocol);
                config_td.CurrentState = 0;
                config_td.StateName = config_td.states[config_td.CurrentState];
                cell.div_driver = config_td;
            }
        }

        private void btnDelDivDriver_Click(object sender, RoutedEventArgs e)
        {
            //ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
            ConfigCell cell = DataContext as ConfigCell;

            if (cell == null)
                return;

            //confirm deletion of driver
            MessageBoxResult res;
            res = MessageBox.Show("Are you sure you want to delete the selected cell's division driver?", "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;

            //delete driver
            cell.div_driver = null;
        }

        /// <summary>
        /// This generates the row headers for the Epigenetic Map grid.
        /// These headers represent differentiation state names and are editable.
        /// </summary>
        private void EpigeneticMapGenerateRowHeaders()
        {
            ConfigCell cell = DataContext as ConfigCell;
            if (cell == null)
                return;
            if (cell.diff_scheme == null)
                return;
            ConfigDiffScheme scheme = cell.diff_scheme;
            if (scheme == null)
                return;

            int rowcount = EpigeneticMapGrid.Items.Count;

            for (int ii = 0; ii < rowcount; ii++)
            {
                if (ii >= scheme.Driver.states.Count)
                    break;

                DataGridRow row = EpigeneticMapGrid.GetRow(ii);
                if (row != null)
                {
                    string sbind = string.Format("states[{0}]", ii);
                    Binding b = new Binding(sbind);
                    b.Path = new PropertyPath(sbind);
                    b.Mode = BindingMode.TwoWay;
                    b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;

                    //Create a TextBox so the state name is editable
                    FrameworkElementFactory txtStateName = new FrameworkElementFactory(typeof(TextBox));
                    txtStateName.SetValue(TextBox.StyleProperty, null);
                    //txtStateName.SetValue(TextBox.WidthProperty, 120D);
                    txtStateName.SetValue(TextBox.WidthProperty, 120D);
                    Thickness th = new Thickness(0D);
                    txtStateName.SetValue(TextBox.BorderThicknessProperty, th);
                    txtStateName.SetValue(TextBox.DataContextProperty, cell.diff_scheme.Driver);
                    txtStateName.SetBinding(TextBox.TextProperty, b);

                    DataGridRowHeader header = new DataGridRowHeader();
                    DataTemplate rowHeaderTemplate = new DataTemplate();

                    rowHeaderTemplate.VisualTree = txtStateName;
                    header.Style = null;
                    header.ContentTemplate = rowHeaderTemplate;
                    row.HeaderStyle = null;
                    row.Header = header;
                }
            }
        }

        private DataGridTemplateColumn CreateDiffRegColumn(EntityRepository er, ConfigCell cell, string state)
        {
            DataGridTemplateColumn col = new DataGridTemplateColumn();

            string sbind = string.Format("states[{0}]", DiffRegGrid.Columns.Count);
            Binding bcol = new Binding(sbind);
            bcol.Path = new PropertyPath(sbind);
            bcol.Mode = BindingMode.OneWay;

            //Create a TextBox so the state name is editable
            FrameworkElementFactory txtStateName = new FrameworkElementFactory(typeof(TextBlock));
            txtStateName.SetValue(TextBlock.StyleProperty, null);
            txtStateName.SetValue(TextBlock.DataContextProperty, cell.diff_scheme.Driver);
            txtStateName.SetBinding(TextBlock.TextProperty, bcol);

            //DataGridRowHeader header = new DataGridRowHeader();
            DataTemplate colHeaderTemplate = new DataTemplate();

            colHeaderTemplate.VisualTree = txtStateName;
            col.HeaderStyle = null;
            col.HeaderTemplate = colHeaderTemplate;
            col.CanUserSort = false;
            col.MinWidth = 50;

            //SET UP CELL LAYOUT - COMBOBOX OF MOLECULES PLUS ALPHA AND BETA VALUES

            //NON-EDITING TEMPLATE - THIS IS WHAT SHOWS WHEN NOT EDITING THE GRID CELL
            DataTemplate cellTemplate = new DataTemplate();

            //SET UP A TEXTBLOCK ONLY
            Binding bn = new Binding(string.Format("elements[{0}].driver_mol_guid_ref", DiffRegGrid.Columns.Count));
            bn.Mode = BindingMode.TwoWay;
            bn.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            MolGUIDtoMolNameConverter c = new MolGUIDtoMolNameConverter();
            bn.Converter = c;
            CollectionViewSource cvs = new CollectionViewSource();
            cvs.Source = er.molecules;
            bn.ConverterParameter = cvs;
            FrameworkElementFactory txtDriverMol = new FrameworkElementFactory(typeof(TextBlock));
            txtDriverMol.Name = "DriverTextBlock";
            txtDriverMol.SetBinding(TextBlock.TextProperty, bn);
            cellTemplate.VisualTree = txtDriverMol;

            //EDITING TEMPLATE - THIS IS WHAT SHOWS WHEN USER EDITS THE GRID CELL

            //SET UP A STACK PANEL THAT WILL CONTAIN A COMBOBOX AND AN EXPANDER
            FrameworkElementFactory spFactory = new FrameworkElementFactory(typeof(StackPanel));
            spFactory.Name = "mySpFactory";
            spFactory.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);

            DataTemplate cellEditingTemplate = new DataTemplate();

            //SET UP THE COMBO BOX
            FrameworkElementFactory comboMolPops = new FrameworkElementFactory(typeof(ComboBox));
            comboMolPops.Name = "MolPopComboBox";

            //------ Use a composite collection to insert "None" item
            CompositeCollection coll = new CompositeCollection();
            ConfigMolecularPopulation nullcmp = new ConfigMolecularPopulation(new ReportType());
            nullcmp.Name = "None";
            coll.Add(nullcmp);
            //ComboBoxItem nullItem = new ComboBoxItem();
            //nullItem.IsEnabled = true;
            //nullItem.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
            //nullItem.Content = "None";
            //coll.Add(nullItem);
            CollectionContainer cc = new CollectionContainer();
            cc.Collection = cell.cytosol.molpops;
            coll.Add(cc);
            comboMolPops.SetValue(ComboBox.ItemsSourceProperty, coll);

            //--------------

            comboMolPops.SetValue(ComboBox.DisplayMemberPathProperty, "Name");     //displays mol pop name
            comboMolPops.AddHandler(ComboBox.SelectionChangedEvent, new SelectionChangedEventHandler(comboMolPops_SelectionChanged));

            //NEED TO SOMEHOW CONVERT driver_mol_guid_ref to mol_pop!  Set up a converter and pass it the cytosol.
            MolGuidToMolPopForDiffConverter conv2 = new MolGuidToMolPopForDiffConverter();
            string sText = string.Format("elements[{0}].driver_mol_guid_ref", DiffRegGrid.Columns.Count);
            Binding b3 = new Binding(sText);
            b3.Mode = BindingMode.TwoWay;
            b3.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            b3.Converter = conv2;
            b3.ConverterParameter = cell.cytosol;
            comboMolPops.SetBinding(ComboBox.SelectedValueProperty, b3);
            comboMolPops.SetValue(ComboBox.ToolTipProperty, "Mol Pop Name");

            spFactory.AppendChild(comboMolPops);

            //--------------------------------------------------

            //SET UP AN EXPANDER THAT WILL CONTAIN ALPHA AND BETA

            //This disables the expander if no driver molecule is selected
            DriverElementToBoolConverter enabledConv = new DriverElementToBoolConverter();
            Binding bEnabled = new Binding(string.Format("elements[{0}].driver_mol_guid_ref", DiffRegGrid.Columns.Count));
            bEnabled.Mode = BindingMode.OneWay;
            bEnabled.Converter = enabledConv;

            //Expander
            FrameworkElementFactory expAlphaBeta = new FrameworkElementFactory(typeof(Expander));
            expAlphaBeta.SetValue(Expander.HeaderProperty, "Transition rate values");
            expAlphaBeta.SetValue(Expander.ExpandDirectionProperty, ExpandDirection.Down);
            expAlphaBeta.SetValue(Expander.BorderBrushProperty, Brushes.White);
            expAlphaBeta.SetValue(Expander.IsExpandedProperty, false);
            expAlphaBeta.SetValue(Expander.BackgroundProperty, Brushes.White);
            expAlphaBeta.SetBinding(Expander.IsEnabledProperty, bEnabled);

            FrameworkElementFactory spProduction = new FrameworkElementFactory(typeof(StackPanel));
            spProduction.SetValue(StackPanel.OrientationProperty, Orientation.Vertical);

            FrameworkElementFactory spAlpha = new FrameworkElementFactory(typeof(StackPanel));
            spAlpha.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            FrameworkElementFactory tbAlpha = new FrameworkElementFactory(typeof(TextBlock));
            tbAlpha.SetValue(TextBlock.TextProperty, "Background:  ");
            tbAlpha.SetValue(TextBlock.ToolTipProperty, "Background production rate");
            tbAlpha.SetValue(TextBox.WidthProperty, 110D);
            //tbAlpha.SetValue(TextBlock.WidthProperty, new GridLength(50, GridUnitType.Pixel));
            spAlpha.AppendChild(tbAlpha);

            //SET UP THE ALPHA TEXTBOX
            Binding b = new Binding(string.Format("elements[{0}].Alpha", DiffRegGrid.Columns.Count));
            b.Mode = BindingMode.TwoWay;
            b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            FrameworkElementFactory txtDriverAlpha = new FrameworkElementFactory(typeof(TextBox));
            txtDriverAlpha.SetBinding(TextBox.TextProperty, b);

            txtDriverAlpha.SetValue(TextBox.ToolTipProperty, "Background production rate");
            txtDriverAlpha.SetValue(TextBox.WidthProperty, 50D);
            spAlpha.AppendChild(txtDriverAlpha);
            spProduction.AppendChild(spAlpha);

            FrameworkElementFactory spBeta = new FrameworkElementFactory(typeof(StackPanel));
            spBeta.SetValue(StackPanel.OrientationProperty, Orientation.Horizontal);

            FrameworkElementFactory tbBeta = new FrameworkElementFactory(typeof(TextBlock));
            tbBeta.SetValue(TextBlock.TextProperty, "Linear coefficient:  ");
            tbBeta.SetValue(TextBox.WidthProperty, 110D);
            tbBeta.SetValue(TextBlock.ToolTipProperty, "Production rate linear coefficient");
            spBeta.AppendChild(tbBeta);

            //SET UP THE BETA TEXTBOX
            Binding beta = new Binding(string.Format("elements[{0}].Beta", DiffRegGrid.Columns.Count));
            beta.Mode = BindingMode.TwoWay;
            beta.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            FrameworkElementFactory txtDriverBeta = new FrameworkElementFactory(typeof(TextBox));
            txtDriverBeta.SetBinding(TextBox.TextProperty, beta);
            txtDriverBeta.SetValue(TextBox.WidthProperty, 50D);
            txtDriverBeta.SetValue(TextBox.ToolTipProperty, "Production rate linear coefficient");
            spBeta.AppendChild(txtDriverBeta);
            spProduction.AppendChild(spBeta);

            expAlphaBeta.AppendChild(spProduction);
            spFactory.AppendChild(expAlphaBeta);

            //---------------------------

            //set the visual tree of the data template
            cellEditingTemplate.VisualTree = spFactory;

            //set cell layout
            col.CellTemplate = cellTemplate;
            col.CellEditingTemplate = cellEditingTemplate;

            return col;
        }

        private void DiffRegGenerateRowHeaders()
        {
            //The code below generates the row headers
            //ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;
            ConfigCell cell = DataContext as ConfigCell;

            if (cell == null)
                return;

            if (cell.diff_scheme == null)
                return;

            ConfigDiffScheme scheme = cell.diff_scheme;

            if (scheme == null)
                return;

            int rowcount = DiffRegGrid.Items.Count;
            for (int ii = 0; ii < rowcount; ii++)
            {
                if (ii >= scheme.Driver.states.Count)
                    break;

                DataGridRow row = DiffRegGrid.GetRow(ii);
                if (row != null)
                {
                    string sbind = string.Format("states[{0}]", ii);
                    Binding b = new Binding(sbind);
                    b.Path = new PropertyPath(sbind);
                    b.Mode = BindingMode.OneWay;

                    //Create a TextBox so the state name is editable
                    FrameworkElementFactory txtStateName = new FrameworkElementFactory(typeof(TextBlock));
                    txtStateName.SetValue(TextBlock.StyleProperty, null);
                    txtStateName.SetValue(TextBlock.WidthProperty, 120D);
                    txtStateName.SetValue(TextBlock.DataContextProperty, cell.diff_scheme.Driver);
                    txtStateName.SetBinding(TextBlock.TextProperty, b);

                    DataGridRowHeader header = new DataGridRowHeader();
                    DataTemplate rowHeaderTemplate = new DataTemplate();

                    rowHeaderTemplate.VisualTree = txtStateName;
                    header.Style = null;
                    header.ContentTemplate = rowHeaderTemplate;
                    row.HeaderStyle = null;
                    row.Header = header;
                }
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

            //if cell does not have a diff scheme, create one
            if (cell.diff_scheme == null)
            {
                ConfigDiffScheme ds = new ConfigDiffScheme();

                ds.genes = new ObservableCollection<string>();
                ds.Name = "New diff scheme";
                ds.Driver = new ConfigTransitionDriver();
                ds.activationRows = new ObservableCollection<ConfigActivationRow>();

                cell.diff_scheme = ds;

            }

            ConfigDiffScheme scheme = cell.diff_scheme;
            ConfigGene gene = null;

            if (combo != null && combo.Items.Count > 0)
            {
                //Skip 0'th combo box item because it is the "None" string
                if (combo.SelectedIndex > 0)
                {
                    gene = (ConfigGene)combo.SelectedItem;
                    if (gene == null)
                        return;

                    if (!scheme.genes.Contains(gene.entity_guid))
                    {
                        //If no states exist, then create at least 2 new ones
                        if (scheme.Driver.states.Count == 0)
                        {
                            AddDifferentiationState("State1");
                            AddDifferentiationState("State2");
                            //menuAddState_Click(null, null);
                            //menuAddState_Click(null, null);
                        }

                        scheme.genes.Add(gene.entity_guid);
                        foreach (ConfigActivationRow row in scheme.activationRows)
                        {
                            row.activations.Add(1.0);
                        }
                    }
                }
            }

            if (gene == null)
                return;

            //Have to refresh the data grid!
            DataGridTextColumn col = new DataGridTextColumn();
            col.Header = gene.Name;
            col.CanUserSort = false;

            if (scheme.activationRows.Count > 0)
            {
                Binding b = new Binding(string.Format("activations[{0}]", scheme.activationRows[0].activations.Count - 1));   //EpigeneticMapGrid.Columns.Count-1));  
                b.Mode = BindingMode.TwoWay;
                b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                col.Binding = b;
            }

            //if (EpigeneticMapGrid.Columns == null || EpigeneticMapGrid.Columns.Count <= 0)
            //    return;                        

            EpigeneticMapGrid.Columns.Insert(EpigeneticMapGrid.Columns.Count - 1, col);

            combo.SelectedIndex = 0;

            //This deletes the last column
            int colcount = EpigeneticMapGrid.Columns.Count;
            DataGridTextColumn comboCol = EpigeneticMapGrid.Columns[colcount - 1] as DataGridTextColumn;
            EpigeneticMapGrid.Columns.Remove(comboCol);

            //This regenerates the last column
            EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
            comboCol = CreateUnusedGenesColumn();
            EpigeneticMapGrid.Columns.Add(comboCol);

        }

        /// <summary>
        /// This method updates the differentiation regulators grid.
        /// It is meant to be called after the user adds a new diff state.
        /// </summary>
        private void UpdateDiffRegGrid()
        {
            if (DiffRegGrid.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                int nRows = DiffRegGrid.Items.Count;
                for (int j = 0; j < nRows; j++)
                {
                    var currRow = DiffRegGrid.GetRow(j);
                    int nCols = DiffRegGrid.Columns.Count;
                    for (int i = 0; i < nCols; i++)
                    {
                        if (j == i)
                        {
                            var columnCell = DiffRegGrid.GetCell(currRow, i);
                            if (columnCell != null)
                            {
                                columnCell.IsEnabled = false;
                                columnCell.Background = Brushes.LightGray;
                            }
                        }
                        else
                        {
                            //Trying to disable the expander here but this does not work, at least not yet.
                            var columnCell = DiffRegGrid.GetCell(currRow, i);
                            ComboBox cbx = FindChild<ComboBox>(columnCell, "comboMolPops");
                        }
                    }
                }
            }

            //Generate the row headers
            DiffRegGenerateRowHeaders();
        }


        /// <summary>
        /// This method adds a differentiation state given a name 
        /// </summary>
        /// <param name="name"></param>
        private void AddDifferentiationState(string name)
        {
            EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
            //ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;
            ConfigCell cell = DataContext as ConfigCell;

            if (cell == null)
                return;

            //If no diff scheme defined for this cell, create one
            if (cell.diff_scheme == null)
            {
                ConfigDiffScheme ds = new ConfigDiffScheme();

                ds.genes = new ObservableCollection<string>();
                ds.Name = "New diff scheme";
                ds.Driver = new ConfigTransitionDriver();
                ds.activationRows = new ObservableCollection<ConfigActivationRow>();
                cell.diff_scheme = ds;
            }

            ConfigDiffScheme diff_scheme = cell.diff_scheme;
            diff_scheme.AddState(name);
            DiffRegGrid.Columns.Add(CreateDiffRegColumn(er, cell, name));

            EpigeneticMapGrid.ItemsSource = null;
            EpigeneticMapGrid.ItemsSource = diff_scheme.activationRows;
            EpigeneticMapGenerateRowHeaders();

            //if adding first row, then need to add the columns too - one for each gene
            if (diff_scheme.activationRows.Count == 1)
            {
                foreach (string gene_guid in diff_scheme.genes)
                {
                    DataGridTextColumn col = new DataGridTextColumn();
                    col.Header = er.genes_dict[gene_guid].Name;
                    col.CanUserSort = false;
                    Binding b = new Binding(string.Format("activations[{0}]", diff_scheme.activationRows[0].activations.Count - 1));
                    b.Mode = BindingMode.TwoWay;
                    b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    col.Binding = b;
                }

            }

            DiffRegGrid.ItemsSource = null;
            DiffRegGrid.ItemsSource = diff_scheme.Driver.DriverElements;
            UpdateDiffRegGrid();

        }

        private void btnNewDiffScheme_Click(object sender, RoutedEventArgs e)
        {
            AddDifferentiationState("State1");
            AddDifferentiationState("State2");
        }

        private void btnDelDiffScheme_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult res;
            res = MessageBox.Show("Are you sure you want to delete the selected cell's differentiation scheme?", "Warning", MessageBoxButton.YesNo);
            if (res == MessageBoxResult.No)
                return;

            //ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;
            ConfigCell cell = DataContext as ConfigCell;

            if (cell == null)
                return;

            cell.diff_scheme = null;

            //Clear the grids
            EpigeneticMapGrid.ItemsSource = null;
            EpigeneticMapGrid.Columns.Clear();
            DiffRegGrid.ItemsSource = null;
            DiffRegGrid.Columns.Clear();

            //Still want 'Add Genes' combo box
            DataGridTextColumn combo_col = CreateUnusedGenesColumn();
            EpigeneticMapGrid.Columns.Add(combo_col);
            EpigeneticMapGrid.ItemContainerGenerator.StatusChanged += new EventHandler(EpigeneticItemContainerGenerator_StatusChanged);
        }

        /// <summary>
        /// This method is called when the user changes a combo box selection in a grid cell
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void comboMolPops_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
#if false
            //This code is no good and is "iffed out". Was trying to get the grid cell where the user clicked.
            //Apparently, this is very hard to do in wpf.  How nice!

            //PROBABLY WILL NOT NEED THIS AT ALL BECAUSE THE DATA BINDINGS ARE WORKING. SO REMOVE IT WHEN WE'RE SURE WE DON'T NEED IT.

            //ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;
            //if (cell == null)
            //    return;

            //ComboBox combo = sender as ComboBox;
            //if (sender != null)
            //{
            //    DataGridCellInfo cellInfo = DiffRegGrid.CurrentCell;
            //    if (cellInfo == null)
            //        return;

            //    ConfigTransitionDriverRow driverRow = cell.diff_scheme.Driver.DriverElements[6];  //(ConfigTransitionDriverRow)cellInfo.Item;
                
            //    if (DiffRegGrid.CurrentColumn == null)
            //        return;

            //    int ncol = DiffRegGrid.CurrentColumn.DisplayIndex;
            //    if (ncol < 0)
            //        return;

            //    if (combo.SelectedIndex < 0)
            //        return;

            //    ConfigMolecularPopulation selMolPop = ((ConfigMolecularPopulation)(combo.SelectedItem));

            //    driverRow.elements[ncol].driver_mol_guid_ref = selMolPop.molecule_guid_ref;
            //}
#endif

        }


        /// <summary>
        /// This method is called when the user clicks on a different row in the differentiation grid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DiffRegGrid_SelectedCellsChanged(object sender, SelectedCellsChangedEventArgs e)
        {
            var selectedRow = DiffRegGrid.GetSelectedRow();
            if (selectedRow == null)
                return;

            int row = DiffRegGrid.SelectedIndex;

            DataGridCellInfo selected = DiffRegGrid.SelectedCells.First();
            DataGridColumn col = selected.Column;
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

        /// <summary>
        /// This method is called on right-click + "delete selected genes", 
        /// on the epigenetic map data grid. Selected columns (genes) will 
        /// get deleted.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuDeleteGenes_Click(object sender, RoutedEventArgs e)
        {
            EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
            //ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;
            ConfigCell cell = DataContext as ConfigCell;

            if (cell == null)
            {
                return;
            }

            if (cell.diff_scheme == null)
                return;

            ConfigDiffScheme diff_scheme = cell.diff_scheme;

            foreach (DataGridTextColumn col in EpigeneticMapGrid.Columns.ToList())
            {
                bool isSelected = DataGridBehavior.GetHighlightColumn(col);
                string gene_name = col.Header as string;
                string guid = MainWindow.SOP.Protocol.findGeneGuid(gene_name, MainWindow.SOP.Protocol);
                if (isSelected && guid != null && guid.Length > 0)
                {
                    diff_scheme.genes.Remove(guid);
                    EpigeneticMapGrid.Columns.Remove(col);
                }
            }

            //This deletes the last column
            int colcount = EpigeneticMapGrid.Columns.Count;
            DataGridTextColumn comboCol = EpigeneticMapGrid.Columns[colcount - 1] as DataGridTextColumn;
            EpigeneticMapGrid.Columns.Remove(comboCol);

            //This regenerates the last column
            comboCol = CreateUnusedGenesColumn();
            EpigeneticMapGrid.Columns.Add(comboCol);
        }

        private void menuDeleteStates_Click(object sender, RoutedEventArgs e)
        {
            EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
            //ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;
            ConfigCell cell = DataContext as ConfigCell;

            if (cell == null)
                return;

            if (cell.diff_scheme == null)
                return;

            ConfigDiffScheme diff_scheme = cell.diff_scheme;

            int i = 0;
            foreach (ConfigActivationRow diffrow in diff_scheme.activationRows.ToList())
            {
                if (EpigeneticMapGrid.SelectedItems.Contains(diffrow))
                {
                    int index = diff_scheme.activationRows.IndexOf(diffrow);
                    string stateToDelete = diff_scheme.Driver.states[index];

                    //this deletes the column from the differentiation regulators grid
                    DeleteDiffRegGridColumn(stateToDelete);

                    //this removes the activation row from the differentiation scheme
                    diff_scheme.RemoveActivationRow(diffrow);
                }
                i++;
            }

        }

        private void DeleteDiffRegGridColumn(string state)
        {
            foreach (DataGridTemplateColumn col in DiffRegGrid.Columns.ToList())
            {
                if ((string)(col.Header) == state)
                {
                    DiffRegGrid.Columns.Remove(col);
                    break;
                }
            }
        }

        private void EpigeneticMapGrid_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {

        }

        private void unusedGenesListView_Filter(object sender, FilterEventArgs e)
        {
            //ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            ConfigCell cell = DataContext as ConfigCell;

            e.Accepted = false;

            if (cell == null)
                return;

            ConfigDiffScheme ds = cell.diff_scheme;
            ConfigGene gene = e.Item as ConfigGene;

            //if gene is not in the cell's nucleus, then exclude it from the available gene pool
            if (!cell.genes_guid_ref.Contains(gene.entity_guid))
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

        /// <summary>
        /// This method is called when the user clicks on Add State menu item for Epigenetic Map grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuAddState_Click(object sender, RoutedEventArgs e)
        {
            //Show a dialog that gets the new state's name
            AddDiffState ads = new AddDiffState();

            if (ads.ShowDialog() == true)
            {
                AddDifferentiationState(ads.StateName);
            }
        }

        private void ContextMenuAddState_Click(object sender, RoutedEventArgs e)
        {

            DataGrid dataGrid = (sender as MenuItem).CommandTarget as DataGrid;
            //Show a dialog that gets the new state's name
            AddDiffState ads = new AddDiffState();
            if (ads.ShowDialog() != true) return;

            //DataGrid dataGrid = (sender as MenuItem).CommandTarget as DataGrid;
            var diff_scheme = DataGridDiffScheme.GetDiffSchemeSource(dataGrid);
            if (diff_scheme == null) return;

            diff_scheme.AddState(ads.StateName);
        }

        /// <summary>
        /// new version for datagrid by attaching diffscheme
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContextMenuDeleteGenes_Click(object sender, RoutedEventArgs e)
        {
            EntityRepository er = MainWindow.SOP.Protocol.entity_repository;

            DataGrid dataGrid = (sender as MenuItem).CommandTarget as DataGrid;

            var dx = dataGrid.DataContext;

            var diff_scheme = DataGridDiffScheme.GetDiffSchemeSource(dataGrid);
            if (diff_scheme == null) return;

            foreach (DataGridTextColumn col in dataGrid.Columns.ToList())
            {
                bool isSelected = DataGridBehavior.GetHighlightColumn(col);
                string gene_name = col.Header as string;
                string guid = MainWindow.SOP.Protocol.findGeneGuid(gene_name, MainWindow.SOP.Protocol);
                if (isSelected && guid != null && guid.Length > 0)
                {
                    diff_scheme.genes.Remove(guid);
                    dataGrid.Columns.Remove(col);
                }
            }

            ////This deletes the last column
            //int colcount = EpigeneticMapGrid.Columns.Count;
            //DataGridTextColumn comboCol = EpigeneticMapGrid.Columns[colcount - 1] as DataGridTextColumn;
            //EpigeneticMapGrid.Columns.Remove(comboCol);

            ////This regenerates the last column
            //comboCol = CreateUnusedGenesColumn(er);
            //EpigeneticMapGrid.Columns.Add(comboCol);
        }

        /// <summary>
        /// new version for datagrid by attaching diffscheme
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ContextMenuDeleteStates_Click(object sender, RoutedEventArgs e)
        {

            DataGrid dataGrid = (sender as MenuItem).CommandTarget as DataGrid;
            var diff_scheme = DataGridDiffScheme.GetDiffSchemeSource(dataGrid);
            if (diff_scheme == null) return;

            foreach (ConfigActivationRow diffrow in diff_scheme.activationRows.ToList())
            {
                if (dataGrid.SelectedItems.Contains(diffrow))
                {
                    int index = diff_scheme.activationRows.IndexOf(diffrow);
                    string stateToDelete = diff_scheme.Driver.states[index];

                    //this deletes the column from the differentiation regulators grid
                    //to do below.....
                    //DeleteDiffRegGridColumn(stateToDelete);

                    //this removes the activation row from the differentiation scheme
                    diff_scheme.RemoveActivationRow(diffrow);
                }
            }

            DataGridDiffScheme.update_datagrid_rowheaders(dataGrid);
        }


        /// <summary>
        /// This method gets called after the EpigeneticMapGrid gui objects are generated. 
        /// Here we can set up the row headers.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EpigeneticItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            if (EpigeneticMapGrid.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                EpigeneticMapGrid.ItemContainerGenerator.StatusChanged -= EpigeneticItemContainerGenerator_StatusChanged;
                EpigeneticMapGenerateRowHeaders();
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

        public void DiffSchemeExpander_Expanded(object sender, RoutedEventArgs e)
        {
            EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
            //ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;
            ConfigCell cell = DataContext as ConfigCell;

            //Clear the grids
            EpigeneticMapGrid.ItemsSource = null;
            EpigeneticMapGrid.Columns.Clear();

            DiffRegGrid.ItemsSource = null;
            DiffRegGrid.Columns.Clear();

            if (cell == null)
            {
                return;
            }

            //if cell does not have a diff scheme, return
            if (cell.diff_scheme == null)
            {
                //Create a column that allows the user to add genes to the grid
                DataGridTextColumn combo_col = CreateUnusedGenesColumn();
                EpigeneticMapGrid.Columns.Add(combo_col);
                return;
            }

            //Get the diff_scheme using the guid
            ConfigDiffScheme diff_scheme = cell.diff_scheme;

            //EPIGENETIC MAP SECTION
            EpigeneticMapGrid.DataContext = diff_scheme;
            EpigeneticMapGrid.ItemsSource = diff_scheme.activationRows;

            int nn = 0;
            foreach (string gene_guid in diff_scheme.genes)
            {
                //SET UP COLUMNS
                ConfigGene gene = er.genes_dict[gene_guid];
                DataGridTextColumn col = new DataGridTextColumn();
                col.Header = gene.Name;
                col.CanUserSort = false;
                Binding b = new Binding(string.Format("activations[{0}]", nn));
                b.Mode = BindingMode.TwoWay;
                b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                col.Binding = b;
                EpigeneticMapGrid.Columns.Add(col);
                nn++;
            }

            //Create a column that allows the user to add genes to the grid
            DataGridTextColumn editor_col = CreateUnusedGenesColumn();
            EpigeneticMapGrid.Columns.Add(editor_col);
            EpigeneticMapGrid.ItemContainerGenerator.StatusChanged += new EventHandler(EpigeneticItemContainerGenerator_StatusChanged);

            //EpigeneticMapGrid.Visibility = Visibility.Hidden;


            //----------------------------------
            //DIFFERENTIATION REGULATORS SECTION

            DiffRegGrid.ItemsSource = diff_scheme.Driver.DriverElements;
            //DiffRegGrid.DataContext = diff_scheme.Driver;
            DiffRegGrid.CanUserAddRows = false;
            DiffRegGrid.CanUserDeleteRows = false;

            int i = 0;
            foreach (string s in diff_scheme.Driver.states)
            {
                //SET UP COLUMN HEADINGS
                DataGridTemplateColumn col2 = new DataGridTemplateColumn();
                DiffRegGrid.Columns.Add(CreateDiffRegColumn(er, cell, s));
                i++;
            }

            DiffRegGrid.ItemContainerGenerator.StatusChanged += new EventHandler(DiffRegItemContainerGenerator_StatusChanged);
        }

        /// <summary>
        /// This method gets called after the DiffRegGrid gui objects are generated. 
        /// This is the place to disable the diagonal grid cells.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DiffRegItemContainerGenerator_StatusChanged(object sender, EventArgs e)
        {
            if (DiffRegGrid.ItemContainerGenerator.Status == GeneratorStatus.ContainersGenerated)
            {
                DiffRegGrid.ItemContainerGenerator.StatusChanged -= DiffRegItemContainerGenerator_StatusChanged;
                int nRows = DiffRegGrid.Items.Count;
                for (int j = 0; j < nRows; j++)
                {
                    var currRow = DiffRegGrid.GetRow(j);
                    int nCols = DiffRegGrid.Columns.Count;
                    for (int i = 0; i < nCols; i++)
                    {
                        if (j == i)
                        {
                            var columnCell = DiffRegGrid.GetCell(currRow, i);
                            if (columnCell != null)
                            {
                                columnCell.IsEnabled = false;
                                columnCell.Background = Brushes.LightGray;
                            }
                        }
                        else
                        {
                            //Trying to disable the expander here but this does not work, at least not yet.
                            var columnCell = DiffRegGrid.GetCell(currRow, i);
                            ComboBox cbx = FindChild<ComboBox>(columnCell, "comboMolPops");
                        }
                    }
                }
            }

            //Generate the row headers
            DiffRegGenerateRowHeaders();
        }


    }

    /// <summary>
    /// handling dynamc generation of Epigenetic map datagrid info
    /// </summary>
    public class DataGridDiffScheme
    {
        static CellDetailsControl parent = null;

        public DataGridDiffScheme(CellDetailsControl p) { parent = p; }

        /// <summary>
        /// ConfigDiffScheme Attached Dependency Property
        /// </summary>
        public static readonly DependencyProperty DiffSchemeSourceProperty =
            DependencyProperty.RegisterAttached("DiffSchemeSource",
            typeof(ConfigDiffScheme), typeof(DataGridDiffScheme),
                new FrameworkPropertyMetadata(null,
                    new PropertyChangedCallback(OnDiffSchemeChanged)));

        public static readonly DependencyProperty DiffSchemeTargetProperty =
            DependencyProperty.RegisterAttached("DiffSchemeTarget",
            typeof(string), typeof(DataGridDiffScheme),
            new FrameworkPropertyMetadata(null,
            null));

        /// <summary>
        /// Gets the DiffScheme property.  
        /// </summary>
        public static ConfigDiffScheme GetDiffSchemeSource(DependencyObject d)
        {
            return (ConfigDiffScheme)d.GetValue(DiffSchemeSourceProperty);
        }

        public static string GetDiffSchemeTarget(DependencyObject d)
        {
            return (string)d.GetValue(DiffSchemeTargetProperty);
        }

        /// <summary>
        /// Sets the MatrixSource property.  
        /// </summary>
        public static void SetDiffSchemeSource(DependencyObject d, ConfigDiffScheme value)
        {
            d.SetValue(DiffSchemeSourceProperty, value);
        }

        public static void SetDiffSchemeTarget(DependencyObject d, string value)
        {
            d.SetValue(DiffSchemeTargetProperty, value);
        }

        /// <summary>
        /// Handles changes to the MatrixSource property.
        /// </summary>
        private static void OnDiffSchemeChanged(DependencyObject d,
            DependencyPropertyChangedEventArgs e)
        {

            EntityRepository er = MainWindow.SOP.Protocol.entity_repository;

            DataGrid dataGrid = d as DataGrid;
            ConfigDiffScheme diffScheme = e.NewValue as ConfigDiffScheme;
            if (diffScheme == null) return;

            string DiffSchemeTarget = GetDiffSchemeTarget(dataGrid);

            if (DiffSchemeTarget == "EpigeneticMap")
            {
                Binding b1 = new Binding("activationRows") { Source = diffScheme };
                b1.Mode = BindingMode.TwoWay;
                dataGrid.SetBinding(DataGrid.ItemsSourceProperty, b1);

                //dataGrid.ItemsSource = diffScheme.activationRows;

                int count = 0;
                dataGrid.Columns.Clear();
                foreach (var gene_guid in diffScheme.genes)
                {
                    ConfigGene gene = er.genes_dict[gene_guid];

                    DataGridTextColumn col = new DataGridTextColumn();
                    col.Header = gene.Name;
                    col.CanUserSort = false;
                    Binding b = new Binding(string.Format("activations[{0}]", count));
                    b.Mode = BindingMode.TwoWay;
                    b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    col.Binding = b;
                    dataGrid.Columns.Add(col);
                    count++;
                }

                DataGridTextColumn combobox_col = CreateUnusedGenesColumn();
                dataGrid.Columns.Add(combobox_col);
            }
            else
            {
                dataGrid.ItemsSource = diffScheme.Driver.DriverElements;
                int count = 0;
                dataGrid.Columns.Clear();
                foreach (string s in diffScheme.Driver.states)
                {
                    DataGridTemplateColumn col = new DataGridTemplateColumn();
                    
                    //column header binding
                    Binding hb = new Binding(string.Format("states[{0}]", count));
                    hb.Mode = BindingMode.OneWay;
                    hb.Source = diffScheme.Driver;
                    FrameworkElementFactory txtStateName = new FrameworkElementFactory(typeof(TextBlock));
                    txtStateName.SetValue(TextBlock.StyleProperty, null);
                    //txtStateName.SetValue(TextBlock.DataContextProperty, cell.diff_scheme.Driver);
                    txtStateName.SetBinding(TextBlock.TextProperty, hb);
                    col.HeaderTemplate = new DataTemplate() { VisualTree = txtStateName };

                    col.CanUserSort = false;
                    //each cell is an ConfigTransitiionDriverelement object
                    //testing...testing...
                    Binding b = new Binding(string.Format("elements[{0}]", count));
                    //b.Mode = BindingMode.TwoWay;
                    //b.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    //sett binding???

                    var cellTemplate = parent.FindResource("DiffRegCellTemplate");
                    FrameworkElementFactory factory = new FrameworkElementFactory(typeof(ContentPresenter));
                    factory.SetValue(ContentPresenter.ContentTemplateProperty, cellTemplate);
                    factory.SetBinding(ContentPresenter.ContentProperty, b);
                    col.CellTemplate = new DataTemplate { VisualTree = factory };

                    //editing template
                    Binding b2 = new Binding(string.Format("elements[{0}]", count));
                    b2.Mode = BindingMode.TwoWay;
                    b2.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                    //sett binding???

                    var cellEditingTemplate = parent.FindResource("DiffRegCellEditingTemplate");
                    FrameworkElementFactory factory2 = new FrameworkElementFactory(typeof(ContentPresenter));
                    factory2.SetValue(ContentPresenter.ContentTemplateProperty, cellEditingTemplate);
                    factory2.SetBinding(ContentPresenter.ContentProperty, b2);
                    col.CellEditingTemplate = new DataTemplate { VisualTree = factory2 };

                    dataGrid.Columns.Add(col);
                    count++;
                }

                dataGrid.CellEditEnding -= new EventHandler<DataGridCellEditEndingEventArgs>(dataGrid_CellEditEnding);
                dataGrid.CellEditEnding += new EventHandler<DataGridCellEditEndingEventArgs>(dataGrid_CellEditEnding);
            }

            dataGrid.LoadingRow -= new EventHandler<DataGridRowEventArgs>(dataGrid_LoadingRow);
            dataGrid.LoadingRow += new EventHandler<DataGridRowEventArgs>(dataGrid_LoadingRow);
            dataGrid.TargetUpdated -= new EventHandler<DataTransferEventArgs>(dataGrid_TargetUpdated);
            dataGrid.TargetUpdated += new EventHandler<DataTransferEventArgs>(dataGrid_TargetUpdated);

        }

        static void dataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            DataGrid dg = (DataGrid)sender;
            foreach (DataGridColumn col in dg.Columns)
            {
                col.Width = DataGridLength.SizeToCells;
                col.Width = DataGridLength.Auto;
            }
        }

        static void dataGrid_TargetUpdated(object sender, DataTransferEventArgs e)
        {
            DataGrid dg = (DataGrid)sender;
            var diffScheme = GetDiffSchemeSource(dg);
            dg.RowHeaderWidth = 0;
            dg.RowHeaderWidth = Double.NaN;
        }

        private static void dataGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {

            DataGrid dataGrid = sender as DataGrid;
            var diffScheme = GetDiffSchemeSource(dataGrid);
            if (diffScheme == null) return;
            int index = e.Row.GetIndex();
            if (index < diffScheme.Driver.states.Count)
            {
                //e.Row.Header = context.RowHeaders[index];
                DataGridRowHeader dgr = new DataGridRowHeader();
                dgr.DataContext = diffScheme.Driver;
                Binding binding = new Binding(string.Format("states[{0}]", index));
                binding.NotifyOnTargetUpdated = true;
                binding.Mode = BindingMode.TwoWay;
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                dgr.SetBinding(DataGridRowHeader.ContentProperty, binding);
                e.Row.Header = dgr;
            }
        }

        public static void update_datagrid_rowheaders(DataGrid datagrid)
        {
            var diffScheme = GetDiffSchemeSource(datagrid);
            for (int i = 0; i < diffScheme.Driver.states.Count; i++)
            {
                DataGridRow row = (DataGridRow)datagrid.ItemContainerGenerator.ContainerFromIndex(i);
                if (row == null) continue;
                DataGridRowHeader dgr = new DataGridRowHeader();
                dgr.DataContext = diffScheme.Driver;
                Binding binding = new Binding(string.Format("states[{0}]", i));
                binding.NotifyOnTargetUpdated = true;
                binding.Mode = BindingMode.TwoWay;
                binding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
                dgr.SetBinding(DataGridRowHeader.ContentProperty, binding);
                row.Header = dgr;
            }
        }

        public static DataGridTextColumn CreateUnusedGenesColumn()
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

        private static void unusedGenesListView_Filter(object sender, FilterEventArgs e)
        {

            ConfigCell cell = parent.DataContext as ConfigCell;
            e.Accepted = false;

            if (cell == null)
                return;

            ConfigDiffScheme ds = cell.diff_scheme;
            ConfigGene gene = e.Item as ConfigGene;

            //if gene is not in the cell's nucleus, then exclude it from the available gene pool
            //this filter might be wrong, because it might be differ for the two schemes!!!
            if (!cell.genes_guid_ref.Contains(gene.entity_guid))
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

        /// <summary>
        /// This handler gets called when user selects a gene in the "add genes" 
        /// combo box in the upper right of the epigenetic map data grid.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public static void comboAddGeneToEpigeneticMap_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            if (combo.SelectedIndex <= 0) return;

            DataGrid dataGrid = (DataGrid)FindVisualParent<DataGrid>(combo);
            if (dataGrid == null) return;

            ConfigCell cell = dataGrid.DataContext as ConfigCell;
            if (cell == null)
                return;

            ConfigDiffScheme scheme = GetDiffSchemeSource(dataGrid);

            ConfigGene gene = (ConfigGene)combo.SelectedItem;
            if (gene == null)
                return;

            if (scheme.genes.Contains(gene.entity_guid)) return; //shouldnot happen...

            //If no states exist, then create at least 2 new ones
            if (scheme.Driver.states.Count == 0)
            {
                scheme.AddState("state1");
                scheme.AddState("state2");
            }

            scheme.genes.Add(gene.entity_guid);
            foreach (ConfigActivationRow row in scheme.activationRows)
            {
                row.activations.Add(1.0);
            }
            //force refresh
            SetDiffSchemeSource(dataGrid, null);
            SetDiffSchemeSource(dataGrid, scheme);
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


    }

    public class DataGridRowColumnIndexEqualValueConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values == null || values.Length < 2) return true;

            DataGridRow row = values[0] as DataGridRow;
            int row_index = row.GetIndex();
            DataGridTemplateColumn col = values[1] as DataGridTemplateColumn;
            int col_index = col.DisplayIndex;
            return row_index == col_index;
        }


        public object[] ConvertBack(object value, Type[] targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }

    }






}
