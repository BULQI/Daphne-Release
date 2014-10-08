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

            int index = dgLibMolecules.SelectedIndex;
            //MainWindow.SOP.Protocol.scenario.environment.ecs.RemoveMolecularPopulation(gm.molecule_guid);
            Level level = (Level)(this.DataContext);
            level.entity_repository.genes.Remove(gene);
            //MainWindow.SOP.Protocol.entity_repository.genes.Remove(gene);
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

            Level level = DataContext as Level;
            ConfigGene newgene = gene.Clone(MainWindow.SOP.Protocol);
            //MainWindow.SOP.Protocol.entity_repository.genes.Add(newgene);

            //Level level = (Level)(this.DataContext);
            level.entity_repository.genes.Add(newgene);
            
            dgLibGenes.SelectedIndex = dgLibGenes.Items.Count - 1;

            gene = (ConfigGene)dgLibGenes.SelectedItem;
            dgLibGenes.ScrollIntoView(newgene);
        }

        private void btnAddGene_Click(object sender, RoutedEventArgs e)
        {
            Level level = (Level)(this.DataContext);
            ConfigGene gm = new ConfigGene("NewGene", 0, 0);
            //gm.Name = gm.GenerateNewName(MainWindow.SOP.Protocol, "_New");
            gm.Name = gm.GenerateNewName(level, "_New");
            //MainWindow.SOP.Protocol.entity_repository.genes.Add(gm);
            
            level.entity_repository.genes.Add(gm);

            dgLibGenes.SelectedIndex = dgLibGenes.Items.Count - 1;

            ConfigGene cg = (ConfigGene)dgLibGenes.SelectedItem;

            if (cg != null)
                dgLibGenes.ScrollIntoView(cg);
        }

        //LIBRARIES TAB EVENT HANDLERS
        //MOLECULES        
        private void btnAddLibMolecule_Click(object sender, RoutedEventArgs e)
        {
            Level level = (Level)(this.DataContext);
            ConfigMolecule gm = new ConfigMolecule();
            //gm.Name = gm.GenerateNewName(MainWindow.SOP.Protocol, "_New");
            gm.Name = gm.GenerateNewName(level, "_New");
            //MainWindow.SOP.Protocol.entity_repository.molecules.Add(gm);

            level.entity_repository.molecules.Add(gm);

            dgLibMolecules.SelectedIndex = dgLibMolecules.Items.Count - 1;

            ConfigMolecule cm = (ConfigMolecule)dgLibMolecules.SelectedItem;
            dgLibMolecules.ScrollIntoView(cm);
        }

        private void btnCopyMolecule_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecule cm = (ConfigMolecule)dgLibMolecules.SelectedItem;

            if (cm == null)
                return;

            Level level = (Level)(this.DataContext);
            //ConfigMolecule gm = new ConfigMolecule(cm);
            //ConfigMolecule newmol = cm.Clone(MainWindow.SOP.Protocol);
            ConfigMolecule newmol = cm.Clone(level);
            //MainWindow.SOP.Protocol.entity_repository.molecules.Add(newmol);
            
            level.entity_repository.molecules.Add(newmol);

            dgLibMolecules.SelectedIndex = dgLibMolecules.Items.Count - 1;

            cm = (ConfigMolecule)dgLibMolecules.SelectedItem;
            dgLibMolecules.ScrollIntoView(cm);
        }

        private void btnRemoveMolecule_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecule gm = (ConfigMolecule)dgLibMolecules.SelectedValue;

            MessageBoxResult res;
            Level level = (Level)(this.DataContext);

            if (level is Protocol)
            {
                Protocol prot = level as Protocol;
                //level.entity_repository.molecules.Add(newmol);
                if (prot.scenario.environment.comp.HasMolecule(gm))

                //if (MainWindow.SOP.Protocol.scenario.environment.comp.HasMolecule(gm))
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

                //MainWindow.SOP.Protocol.scenario.environment.comp.RemoveMolecularPopulation(gm.entity_guid);
                //MainWindow.SOP.Protocol.entity_repository.molecules.Remove(gm);

                prot.scenario.environment.comp.RemoveMolecularPopulation(gm.entity_guid);
                prot.entity_repository.molecules.Remove(gm);

                dgLibMolecules.SelectedIndex = index;

                if (index >= dgLibMolecules.Items.Count)
                    dgLibMolecules.SelectedIndex = dgLibMolecules.Items.Count - 1;

                if (dgLibMolecules.Items.Count == 0)
                    dgLibMolecules.SelectedIndex = -1;
            }

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

            //MainWindow.SOP.Protocol.entity_repository.reactions.Remove(cr);
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
            ConfigReactionComplex crcNew = crcCurr.Clone(false);

            Level level = this.DataContext as Level;
            level.entity_repository.reaction_complexes.Add(crcNew);
            //MainWindow.SOP.Protocol.entity_repository.reaction_complexes.Add(crcNew);

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

                Level level = this.DataContext as Level;
                level.entity_repository.reaction_complexes.Remove(crc);

//                MainWindow.SOP.Protocol.entity_repository.reaction_complexes.Remove(crc);

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

            Level level = this.DataContext as Level;
            if (level is Protocol) {
                Protocol p = level as Protocol;
                gene.ValidateName(p);
            }

            //gene.ValidateName(MainWindow.SOP.Protocol);

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

            Level level = this.DataContext as Level;
            if (level is Protocol)
            {
                Protocol p = level as Protocol;
                cm.ValidateName(p);
            }

            //cm.ValidateName(MainWindow.SOP.Protocol);

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

            //foreach (ConfigCell cell in MainWindow.SOP.Protocol.entity_repository.cells.ToList())
            //{
            //    if (cell.CellName == "RCCell")
            //    {
            //        MainWindow.SOP.Protocol.entity_repository.cells.Remove(cell);
            //    }
            //}
            //crc.RCSim.reset();
            //MainWindow.SOP.Protocol.rc_scenario.cellpopulations.Clear();
#if OLD_RC
            p.rc_scenario.cellpopulations.Clear();

#endif
            // end of cleanup

            ConfigCell cc = new ConfigCell();

            cc.CellName = "RCCell";
            foreach (ConfigMolecularPopulation cmp in crc.molpops)
            {
                cc.cytosol.molpops.Add(cmp);
            }
#if OLD_RC
           foreach (ConfigGene configGene in crc.genes)
            {
                cc.genes.Add(configGene);
                
            }
#endif
            foreach (ConfigReaction cr in crc.reactions)
            {
                cc.cytosol.Reactions.Add(cr.Clone(true));
            }
            //MainWindow.SOP.Protocol.entity_repository.cells.Add(cc);
            //MainWindow.SOP.Protocol.rc_scenario.cellpopulations.Clear();
            p.entity_repository.cells.Add(cc);
#if OLD_RC
            p.rc_scenario.cellpopulations.Clear();
#endif

            CellPopulation cp = new CellPopulation();
            cp.Cell = cc;
            //cp.Cell.entity_guid = cc.entity_guid;
            cp.cellpopulation_name = "RC cell";
            cp.number = 1;

#if OLD_RC
            //ConfigECSEnvironment envHandle = (ConfigECSEnvironment)MainWindow.SOP.Protocol.rc_scenario.environment;
            ConfigECSEnvironment envHandle = (ConfigECSEnvironment)p.rc_scenario.environment;

            // Add cell population distribution information
            double[] extents = new double[3] { envHandle.extent_x, envHandle.extent_y, envHandle.extent_z };
            //double minDisSquared = 2 * MainWindow.SOP.Protocol.entity_repository.cells_dict[cp.Cell.entity_guid].CellRadius;
            double minDisSquared = 2 * p.entity_repository.cells_dict[cp.Cell.entity_guid].CellRadius;
            minDisSquared *= minDisSquared;
            cp.cellPopDist = new CellPopSpecific(extents, minDisSquared, cp);
            cp.CellStates[0] = new CellState(envHandle.extent_x, envHandle.extent_y / 2, envHandle.extent_z / 2);


            cp.cellpopulation_color = System.Windows.Media.Color.FromScRgb(1.0f, 1.0f, 0.5f, 0.0f);
            p.rc_scenario.cellpopulations.Add(cp);
            //MainWindow.SOP.Protocol.rc_scenario.cellpopulations.Add(cp);
#endif
            ReactionComplexProcessor Processor = new ReactionComplexProcessor();

            MainWindow.Sim.Load(p, true);
            //MainWindow.Sim.Load(MainWindow.SOP.Protocol, true, true);

            //Processor.Initialize(MainWindow.SOP.Protocol, crc, (TissueSimulation)MainWindow.Sim);
            Processor.Initialize(p, crc, (TissueSimulation)MainWindow.Sim);
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
            var dc = this.DataContext;
            //DataContext = MainWindow.SOP.Protocol;
        }

        








    }
}
