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

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for ComponentsToolWindow.xaml
    /// </summary>
    public partial class ComponentsToolWindow : ToolWindow
    {
        public MainWindow MW { get; set; }

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

            int index = dgLibMolecules.SelectedIndex;
            //MainWindow.SOP.Protocol.scenario.environment.ecs.RemoveMolecularPopulation(gm.molecule_guid);
            MainWindow.SOP.Protocol.entity_repository.genes.Remove(gene);
            dgLibGenes.SelectedIndex = index;

            if (index >= dgLibMolecules.Items.Count)
                dgLibGenes.SelectedIndex = dgLibGenes.Items.Count - 1;

            if (dgLibGenes.Items.Count == 0)
                dgLibGenes.SelectedIndex = -1;

        }

        private void btnCopyGene_Click(object sender, RoutedEventArgs e)
        {
            ConfigGene gene = (ConfigGene)dgLibGenes.SelectedItem;

            if (gene == null)
                return;

            ConfigGene newgene = gene.Clone(MainWindow.SOP.Protocol);
            MainWindow.SOP.Protocol.entity_repository.genes.Add(newgene);
            dgLibGenes.SelectedIndex = dgLibGenes.Items.Count - 1;

            gene = (ConfigGene)dgLibGenes.SelectedItem;
            dgLibGenes.ScrollIntoView(newgene);
        }

        private void btnAddGene_Click(object sender, RoutedEventArgs e)
        {
            ConfigGene gm = new ConfigGene("NewGene", 0, 0);
            gm.Name = gm.GenerateNewName(MainWindow.SOP.Protocol, "_New");
            MainWindow.SOP.Protocol.entity_repository.genes.Add(gm);
            dgLibGenes.SelectedIndex = dgLibGenes.Items.Count - 1;

            ConfigGene cg = (ConfigGene)dgLibGenes.SelectedItem;

            if (cg != null)
                dgLibGenes.ScrollIntoView(cg);
        }

        //LIBRARIES TAB EVENT HANDLERS
        //MOLECULES        
        private void btnAddLibMolecule_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecule gm = new ConfigMolecule();
            gm.Name = gm.GenerateNewName(MainWindow.SOP.Protocol, "_New");
            MainWindow.SOP.Protocol.entity_repository.molecules.Add(gm);
            dgLibMolecules.SelectedIndex = dgLibMolecules.Items.Count - 1;

            ConfigMolecule cm = (ConfigMolecule)dgLibMolecules.SelectedItem;
            dgLibMolecules.ScrollIntoView(cm);
        }

        private void btnCopyMolecule_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecule cm = (ConfigMolecule)dgLibMolecules.SelectedItem;

            if (cm == null)
                return;

            //ConfigMolecule gm = new ConfigMolecule(cm);
            ConfigMolecule newmol = cm.Clone(MainWindow.SOP.Protocol);
            MainWindow.SOP.Protocol.entity_repository.molecules.Add(newmol);
            dgLibMolecules.SelectedIndex = dgLibMolecules.Items.Count - 1;

            cm = (ConfigMolecule)dgLibMolecules.SelectedItem;
            dgLibMolecules.ScrollIntoView(cm);
        }

        private void btnRemoveMolecule_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecule gm = (ConfigMolecule)dgLibMolecules.SelectedValue;

            MessageBoxResult res;
            if (MainWindow.SOP.Protocol.scenario.environment.ecs.HasMolecule(gm))
            {
                res = MessageBox.Show("If you remove this molecule, corresponding entities that depend on this molecule will also be deleted. Would you like to continue?", "Warning", MessageBoxButton.YesNo);
            }
            else
            {
                res = MessageBox.Show("Are you sure you would like to remove this molecule?", "Warning", MessageBoxButton.YesNo);
            }

            if (res == MessageBoxResult.No)
                return;

            int index = dgLibMolecules.SelectedIndex;
            MainWindow.SOP.Protocol.scenario.environment.ecs.RemoveMolecularPopulation(gm.entity_guid);
            MainWindow.SOP.Protocol.entity_repository.molecules.Remove(gm);
            dgLibMolecules.SelectedIndex = index;

            if (index >= dgLibMolecules.Items.Count)
                dgLibMolecules.SelectedIndex = dgLibMolecules.Items.Count - 1;

            if (dgLibMolecules.Items.Count == 0)
                dgLibMolecules.SelectedIndex = -1;

        }

        //LIBRARY REACTIONS EVENT HANDLERS        
        private void btnRemoveReaction_Click(object sender, RoutedEventArgs e)
        {
            ConfigReaction cr = (ConfigReaction)lvReactions.SelectedItem;
            if (cr == null)
            {
                return;
            }

            MainWindow.SOP.Protocol.entity_repository.reactions.Remove(cr);
        }

        //LIBRARIES REACTION COMPLEXES HANDLERS

        private void btnCopyReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            if (lbComplexes.SelectedIndex < 0)
            {
                MessageBox.Show("Select a reaction complex to copy from.");
                return;
            }

            ConfigReactionComplex crcCurr = (ConfigReactionComplex)lbComplexes.SelectedItem;
            ConfigReactionComplex crcNew = crcCurr.Clone();

            MainWindow.SOP.Protocol.entity_repository.reaction_complexes.Add(crcNew);

            lbComplexes.SelectedIndex = lbComplexes.Items.Count - 1;
        }

        private void btnAddReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            AddReacComplex arc = new AddReacComplex(ReactionComplexDialogType.AddComplex);
            if (arc.ShowDialog() == true)
                lbComplexes.SelectedIndex = lbComplexes.Items.Count - 1;
        }

        private void btnEditReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            ConfigReactionComplex crc = (ConfigReactionComplex)lbComplexes.SelectedItem;
            if (crc == null)
                return;

            AddReacComplex arc = new AddReacComplex(ReactionComplexDialogType.EditComplex, crc);
            arc.ShowDialog();

        }

        private void btnRemoveReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            ConfigReactionComplex crc = (ConfigReactionComplex)(lbComplexes.SelectedItem);
            if (crc != null)
            {
                MessageBoxResult res;
                res = MessageBox.Show("Are you sure you would like to remove this reaction complex?", "Warning", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                    return;

                int index = lbComplexes.SelectedIndex;
                MainWindow.SOP.Protocol.entity_repository.reaction_complexes.Remove(crc);

                lbComplexes.SelectedIndex = index;

                if (index >= lbComplexes.Items.Count)
                    lbComplexes.SelectedIndex = lbComplexes.Items.Count - 1;

                if (lbComplexes.Items.Count == 0)
                    lbComplexes.SelectedIndex = -1;

            }

            //btnGraphReactionComplex.IsChecked = true;
        }

        private void GeneTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ConfigGene gene = dgLibGenes.SelectedItem as ConfigGene;

            if (gene == null)
                return;

            int index = dgLibGenes.SelectedIndex;

            gene.ValidateName(MainWindow.SOP.Protocol);

            dgLibGenes.InvalidateVisual();

            dgLibGenes.Items.Refresh();
            dgLibGenes.SelectedIndex = index;
            gene = (ConfigGene)dgLibGenes.SelectedItem;
            dgLibGenes.ScrollIntoView(gene);

        }

        private void MolTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ConfigMolecule cm = dgLibMolecules.SelectedItem as ConfigMolecule;

            if (cm == null)
                return;

            cm.ValidateName(MainWindow.SOP.Protocol);

            int index = dgLibMolecules.SelectedIndex;
            dgLibMolecules.InvalidateVisual();
            dgLibMolecules.Items.Refresh();
            dgLibMolecules.SelectedIndex = index;
            cm = (ConfigMolecule)dgLibMolecules.SelectedItem;
            dgLibMolecules.ScrollIntoView(cm);
        }

        private void btnGraphReactionComplex_Checked(object sender, RoutedEventArgs e)
        {
            if (lbComplexes.SelectedIndex < 0)
            {
                MessageBox.Show("Select a reaction complex to process.");
                return;
            }

            if (btnGraphReactionComplex.IsChecked == true)
            {
                DrawSelectedReactionComplex();
                btnGraphReactionComplex.IsChecked = false;
            }
        }

        private void DrawSelectedReactionComplex()
        {
            ConfigReactionComplex crc = (ConfigReactionComplex)(lbComplexes.SelectedItem);

            //Cleanup any previous RC stuff
            foreach (ConfigCell cell in MainWindow.SOP.Protocol.entity_repository.cells.ToList())
            {
                if (cell.CellName == "RCCell")
                {
                    MainWindow.SOP.Protocol.entity_repository.cells.Remove(cell);
                }
            }
            //crc.RCSim.reset();
            MainWindow.SOP.Protocol.rc_scenario.cellpopulations.Clear();
            // end of cleanup

            ConfigCell cc = new ConfigCell();
            cc.CellName = "RCCell";
            foreach (ConfigMolecularPopulation cmp in crc.molpops)
            {
                cc.cytosol.molpops.Add(cmp);
            }
            foreach (ConfigGene configGene in crc.genes)
            {
                cc.genes_guid_ref.Add(configGene.entity_guid);
            }
            foreach (string rguid in crc.reactions_guid_ref)
            {
                if (MainWindow.SOP.Protocol.entity_repository.reactions_dict.ContainsKey(rguid) == true)
                {
                    ConfigReaction cr = MainWindow.SOP.Protocol.entity_repository.reactions_dict[rguid];

                    cc.cytosol.Reactions.Add(cr.Clone(true));
                }
            }
            MainWindow.SOP.Protocol.entity_repository.cells.Add(cc);
            MainWindow.SOP.Protocol.rc_scenario.cellpopulations.Clear();

            CellPopulation cp = new CellPopulation();
            cp.Cell = cc;
            //cp.Cell.entity_guid = cc.entity_guid;
            cp.cellpopulation_name = "RC cell";
            cp.number = 1;

            // Add cell population distribution information
            double[] extents = new double[3] { MainWindow.SOP.Protocol.rc_scenario.environment.extent_x, 
                                               MainWindow.SOP.Protocol.rc_scenario.environment.extent_y, 
                                               MainWindow.SOP.Protocol.rc_scenario.environment.extent_z };
            double minDisSquared = 2 * MainWindow.SOP.Protocol.entity_repository.cells_dict[cp.Cell.entity_guid].CellRadius;
            minDisSquared *= minDisSquared;
            cp.cellPopDist = new CellPopSpecific(extents, minDisSquared, cp);
            cp.cellPopDist.CellStates[0] = new CellState(MainWindow.SOP.Protocol.rc_scenario.environment.extent_x,
                                                            MainWindow.SOP.Protocol.rc_scenario.environment.extent_y / 2,
                                                            MainWindow.SOP.Protocol.rc_scenario.environment.extent_z / 2);

            cp.cellpopulation_color = System.Windows.Media.Color.FromScRgb(1.0f, 1.0f, 0.5f, 0.0f);
            MainWindow.SOP.Protocol.rc_scenario.cellpopulations.Add(cp);

            ReactionComplexProcessor Processor = new ReactionComplexProcessor();
            MainWindow.Sim.Load(MainWindow.SOP.Protocol, true, true);

            Processor.Initialize(MainWindow.SOP.Protocol, crc, MainWindow.Sim);
            Processor.Go();

            MainWindow.ST_ReacComplexChartWindow.Title = "Reaction Complex: " + crc.Name;
            MainWindow.ST_ReacComplexChartWindow.RC = Processor;  //crc.Processor;
            MainWindow.ST_ReacComplexChartWindow.DataContext = Processor; //crc.Processor;
            MainWindow.ST_ReacComplexChartWindow.Render();

            MainWindow.ST_ReacComplexChartWindow.dblMaxTime.Number = Processor.dInitialTime;  //crc.Processor.dInitialTime;
            MW.VTKDisplayDocWindow.Activate();
            MainWindow.ST_ReacComplexChartWindow.Activate();
            MainWindow.ST_ReacComplexChartWindow.toggleButton = btnGraphReactionComplex;
        }

        public ConfigReactionComplex GetConfigReactionComplex()
        {
            if (lbComplexes.SelectedIndex < 0)
            {
                MessageBox.Show("Select a reaction complex to process.");
                return null;
            }
            return (ConfigReactionComplex)(lbComplexes.SelectedItem);
        }

        private void MyComponentsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            DataContext = MainWindow.SOP.Protocol;
        }

        








    }
}
