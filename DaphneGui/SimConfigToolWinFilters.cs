using ActiproSoftware.Windows.Controls.Docking;
using Daphne;
using System.Windows.Data;
using System.Collections.ObjectModel;
using System.Linq;

namespace DaphneGui
{

    /// <summary>
    /// Interaction logic for ProtocolToolWindow.xaml
    /// </summary>
    public partial class ProtocolToolWindow : ToolWindow
    {
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

        /// <summary>
        /// this filter retains the list of molecules that are not already in cytosol of a cell
        /// and thus available to be added.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CytosolMoleculesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigMolecule mol = e.Item as ConfigMolecule;
            if (mol == null || mol.molecule_location != MoleculeLocation.Bulk)
            {
                e.Accepted = false;
                return;
            }

            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            if (cell == null)
            {
                e.Accepted = true;
                return;
            }
            CollectionView colView = (CollectionView)CollectionViewSource.GetDefaultView(cell.cytosol.molpops);
            //check if the molecule is in the list
            foreach (ConfigMolecularPopulation cfp in colView)
            {
                if (cfp == colView.CurrentItem) continue;
                if (cfp.molecule.Name == mol.Name)
                {
                    e.Accepted = false;
                    return;
                }
            }
            e.Accepted = true;
            return;
        }

        private void EcsMoleculesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigMolecule mol = e.Item as ConfigMolecule;
            if (mol == null || mol.molecule_location != MoleculeLocation.Bulk)
            {
                e.Accepted = false;
                return;
            }

            if (this.DataContext == null)return;
            var ecs_molpops = MainWindow.SOP.Protocol.scenario.environment.ecs.molpops;
            if (ecs_molpops == null)
            {
                e.Accepted = true;
                return;
            }
            CollectionView colView = (CollectionView)CollectionViewSource.GetDefaultView(ecs_molpops);
            //check if the molecule is in the list
            foreach (ConfigMolecularPopulation cfp in colView)
            {
                if (cfp == colView.CurrentItem) continue;
                if (cfp.molecule.Name == mol.Name)
                {
                    e.Accepted = false;
                    return;
                }
            }
            e.Accepted = true;
            return;
        }


        private void selectedCellTransitionDeathDriverListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            ConfigTransitionDriver driver = e.Item as ConfigTransitionDriver;
            if (driver != null)
            {
                // Filter out driver if its guid does not match selected cell's driver guid
                if (cell != null && cell.death_driver != null && driver.entity_guid == cell.death_driver.entity_guid)
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }

        private void selectedCellTransitionDivisionDriverListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            ConfigTransitionDriver driver = e.Item as ConfigTransitionDriver;
            if (driver != null)
            {
                // Filter out driver if its guid does not match selected cell's driver guid
                if (cell != null && cell.div_driver != null && driver.entity_guid == cell.div_driver.entity_guid)
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }

        private void unusedGenesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;

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

        private void membraneAvailableReactionsListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigReaction cr = e.Item as ConfigReaction;

            if (CellsListBox.SelectedIndex < 0)
            {
                e.Accepted = false;
                return;
            }

            ConfigCell cc = CellsListBox.SelectedItem as ConfigCell;

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

        private void cytosolAvailableReactionsListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigReaction cr = e.Item as ConfigReaction;

            if (CellsListBox.SelectedIndex < 0)
            {
                e.Accepted = false;
                return;
            }

            ConfigCell cc = CellsListBox.SelectedItem as ConfigCell;

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

        /*
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
         */

        private void AvailableBoundaryMoleculesListView_Filter(object sender, FilterEventArgs e)
        {

            ConfigMolecule mol = e.Item as ConfigMolecule;
            if (mol == null || mol.molecule_location != MoleculeLocation.Boundary)
            {
                e.Accepted = false;
                return;
            }

            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            if (cell == null)
            {
                e.Accepted = true;
                return;
            }
            CollectionView colView = (CollectionView)CollectionViewSource.GetDefaultView(cell.membrane.molpops);
            //check if the molecule is in the list
            foreach(ConfigMolecularPopulation cfp in colView)
            {
                if (cfp == colView.CurrentItem)continue;
                if (cfp.molecule.Name == mol.Name)
                {
                    e.Accepted = false;
                    return;
                }
            }
            e.Accepted = true;
            return;
        }




        private void ecmReactionCollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            ConfigReaction cr = e.Item as ConfigReaction;
            if (cr != null)
            {
                // Filter out cr if not in ecm reaction list 
                if (MainWindow.SOP.Protocol.scenario.environment.ecs.Reactions.Contains(cr))
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }

        private void membReactionCollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            ConfigReaction cr = e.Item as ConfigReaction;
            ConfigCell cc = (ConfigCell)CellsListBox.SelectedItem;
            if (cr != null && cc != null)
            {
                e.Accepted = false;
                // Filter out cr if not in membrane reaction list 
                if (cc.membrane.Reactions.Contains(cr))
                {
                    e.Accepted = true;
                }

            }
        }

        private void ecmReactionComplexReactionCollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            if (ReactionComplexListBox.SelectedIndex < 0)
                return;

            ConfigReaction cr = e.Item as ConfigReaction;
            string guidRC = (string)ReactionComplexListBox.SelectedItem;

            e.Accepted = true;

            if (guidRC != null && cr != null)
            {
                ConfigReactionComplex crc = MainWindow.SOP.Protocol.entity_repository.reaction_complexes_dict[guidRC];
                e.Accepted = false;
                // Filter out cr if not in ecm reaction list 
                if (crc.reactions_guid_ref.Contains(cr.entity_guid))
                {
                    e.Accepted = true;
                }
            }
        }

        private void cytosolReactionsCollectionViewSource_Filter(object sender, FilterEventArgs e)
        {
            ConfigReaction cr = e.Item as ConfigReaction;
            ConfigCell cc = (ConfigCell)CellsListBox.SelectedItem;
            if (cr != null && cc != null)
            {
                // Filter out cr if not in cytosol reaction list 
                if (cc.cytosol.Reactions.Contains(cr))
                {
                    e.Accepted = true;
                }
                else
                {
                    e.Accepted = false;
                }
            }
        }

        
    }
}