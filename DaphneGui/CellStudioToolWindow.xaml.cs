using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ActiproSoftware.Windows.Controls.Docking;

using Daphne;
//using System.Windows.Data;
using System.Collections.ObjectModel;
//using System.Windows.Markup;
//using System.Windows.Forms;
using Microsoft.Win32;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Linq;
using System.Windows.Data;


namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for CellStudioToolWindow.xaml
    /// </summary>
    public partial class CellStudioToolWindow : ToolWindow
    {
        public CellStudioToolWindow()
        {
            InitializeComponent();
        }

        private void CellsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (lvCellAvailableReacs.ItemsSource != null)
            //    CollectionViewSource.GetDefaultView(lvCellAvailableReacs.ItemsSource).Refresh();
            //if (lvCytosolAvailableReacs.ItemsSource != null)
            //    CollectionViewSource.GetDefaultView(lvCytosolAvailableReacs.ItemsSource).Refresh();

            //DiffSchemeExpander_Expanded(null, null);
            ucCellDetails.DiffSchemeExpander_Expanded(null, null);

        }

        private void AddLibCellButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cc = new ConfigCell();
            MainWindow.SOP.Protocol.entity_repository.cells.Add(cc);
            CellsListBox.SelectedIndex = CellsListBox.Items.Count - 1;
        }

        private void RemoveCellButton_Click(object sender, RoutedEventArgs e)
        {
            int nIndex = CellsListBox.SelectedIndex;

            if (nIndex >= 0)
            {
                ConfigCell cell = (ConfigCell)CellsListBox.SelectedValue;
                MessageBoxResult res;
                if (MainWindow.SOP.Protocol.scenario.HasCell(cell))
                {
                    res = MessageBox.Show("If you delete this cell, corresponding cell populations will also be deleted. Would you like to continue?", "Warning", MessageBoxButton.YesNo);
                }
                else
                {
                    res = MessageBox.Show("Are you sure you would like to remove this cell?", "Warning", MessageBoxButton.YesNo);
                }

                if (res == MessageBoxResult.Yes)
                {
                    //MainWindow.SOP.Protocol.scenario.RemoveCellPopulation(cell);
                    MainWindow.SOP.Protocol.entity_repository.cells.Remove(cell);

                    CellsListBox.SelectedIndex = nIndex;

                    if (nIndex >= CellsListBox.Items.Count)
                        CellsListBox.SelectedIndex = CellsListBox.Items.Count - 1;

                    if (CellsListBox.Items.Count == 0)
                        CellsListBox.SelectedIndex = -1;
                }

            }
        }

        private void CopyCellButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            if (cell == null)
            {
                return;
            }

            ConfigCell cellNew = cell.Clone(false);

            //Generate a new cell name
            cellNew.CellName = GenerateNewCellName(cell, "_Copy");

            MainWindow.SOP.Protocol.entity_repository.cells.Add(cellNew);
            CellsListBox.SelectedIndex = CellsListBox.Items.Count - 1;
            CellsListBox.ScrollIntoView(CellsListBox.SelectedItem);

        }

        private void CellAddReacCxButton_Click(object sender, RoutedEventArgs e)
        {

#if allow_rc_in_cell
            if (lbCellAvailableReacCx.SelectedIndex != -1)
            {
                ConfigReactionComplex grc = (ConfigReactionComplex)lbCellAvailableReacCx.SelectedValue;
                if (!MainWindow.SOP.Protocol.scenario.ReactionComplexes.Contains(grc))
                    MainWindow.SOP.Protocol.scenario.ReactionComplexes.Add(grc);
            } 
#endif
        }

        private void CellTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;

            if (cell == null)
                return;

            cell.ValidateName(MainWindow.SOP.Protocol);
        }



        private string GenerateNewCellName(ConfigCell cell)
        {
            int nSuffix = 1;
            string sSuffix = string.Format("_Copy{0:000}", nSuffix);
            string TempCellName = cell.CellName;
            while (FindCellBySuffix(sSuffix) == true)
            {
                TempCellName = cell.CellName.Replace(sSuffix, "");
                nSuffix++;
                sSuffix = string.Format("_Copy{0:000}", nSuffix);
            }
            TempCellName += sSuffix;
            return TempCellName;
        }

        private string GenerateNewCellName(ConfigCell cell, string ending)
        {
            int nSuffix = 1;
            string sSuffix = ending + string.Format("{0:000}", nSuffix);
            string TempCellName = cell.CellName;
            while (FindCellBySuffix(sSuffix) == true)
            {
                TempCellName = cell.CellName.Replace(sSuffix, "");
                nSuffix++;
                sSuffix = ending + string.Format("{0:000}", nSuffix);
            }
            TempCellName += sSuffix;
            return TempCellName;
        }

        // given a cell type name, check if it exists in repos
        private static bool FindCellBySuffix(string suffix)
        {
            foreach (ConfigCell cc in MainWindow.SOP.Protocol.entity_repository.cells)
            {
                if (cc.CellName.EndsWith(suffix))
                {
                    return true;
                }
            }
            return false;
        }

        private void cbCellDiffSchemes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //Don't want to do anything when first display this combo box
            //Only do something if user really clicked and selected a different scheme

            if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
                return;

            ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
            if (cell == null)
                return;

            ComboBox combo = sender as ComboBox;

            if (combo.SelectedIndex == -1)
                return;

            if (combo.SelectedIndex == 0)
            {
                cell.diff_scheme = null;
                combo.Text = "None";
            }
            else
            {
                ConfigDiffScheme diffNew = (ConfigDiffScheme)combo.SelectedItem;

                if (cell.diff_scheme != null && diffNew.entity_guid == cell.diff_scheme.entity_guid)
                {
                    return;
                }

                EntityRepository er = MainWindow.SOP.Protocol.entity_repository;

                if (er.diff_schemes_dict.ContainsKey(diffNew.entity_guid) == true)
                {
                    cell.diff_scheme = er.diff_schemes_dict[diffNew.entity_guid].Clone(true);
                }
            }
            int nIndex = CellsListBox.SelectedIndex;
            CellsListBox.SelectedIndex = -1;
            CellsListBox.SelectedIndex = nIndex;
        }

        private void chkHasDivDriver_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
            if (cell == null)
                return;

            CheckBox ch = sender as CheckBox;
            if (ch.IsChecked == false)
            {
                cell.div_driver = null;
            }
            else
            {
                if (cell.div_driver == null)
                {
                    EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
                    ConfigTransitionDriver driver = FindFirstDivDriver();

                    if (driver == null)
                    {
                        MessageBox.Show("No division drivers are defined");
                        return;
                    }

                    if (er.transition_drivers_dict.ContainsKey(driver.entity_guid) == true)
                    {
                        cell.div_driver = er.transition_drivers_dict[driver.entity_guid].Clone(true);
                    }
                }
            }
        }

        private ConfigTransitionDriver FindFirstDeathDriver()
        {
            ConfigTransitionDriver driver = null;

            foreach (ConfigTransitionDriver d in MainWindow.SOP.Protocol.entity_repository.transition_drivers)
            {
                string name = d.Name;
                if (name.Contains("apoptosis"))
                {
                    driver = d;
                    break;
                }
            }

            return driver;
        }

        private ConfigTransitionDriver FindFirstDivDriver()
        {
            ConfigTransitionDriver driver = null;
            foreach (ConfigTransitionDriver d in MainWindow.SOP.Protocol.entity_repository.transition_drivers)
            {
                string name = d.Name;
                if (name.Contains("division"))
                {
                    driver = d;
                    break;
                }
            }

            return driver;
        }


        private void btnNewDeathDriver_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
            if (cell == null)
                return;

            if (cell.death_driver == null)
            {
                ////EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
                ////ConfigTransitionDriver driver = FindFirstDeathDriver();

                ////if (driver == null)
                ////{
                ////    MessageBox.Show("No death drivers are defined");
                ////    return;
                ////}

                ////if (er.transition_drivers_dict.ContainsKey(driver.entity_guid) == true)
                ////{
                ////    cell.death_driver = er.transition_drivers_dict[driver.entity_guid].Clone(false);
                ////}

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
            ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
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
            ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
            if (cell == null)
                return;


            if (cell.div_driver == null)
            {
                ////EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
                ////ConfigTransitionDriver driver = FindFirstDivDriver();

                ////if (driver == null)
                ////{
                ////    MessageBox.Show("No division drivers are defined");
                ////    return;
                ////}

                ////if (er.transition_drivers_dict.ContainsKey(driver.entity_guid) == true)
                ////{
                ////    cell.div_driver = er.transition_drivers_dict[driver.entity_guid].Clone(false);
                ////}

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
            ConfigCell cell = (ConfigCell)(CellsListBox.SelectedItem);
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


    }
}
