﻿using System;
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
using System.Collections.ObjectModel;

using Daphne;
using DaphneUserControlLib;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for MolpopControl.xaml
    /// </summary>
    public partial class ECMMolpopControl : UserControl
    {
        public ECMMolpopControl()
        {
            InitializeComponent();
            this.DataContext = this;
        }

         /// <summary>
        /// Add a new molecular population to the ECS. 
        /// Defaults to first molecule (in EntityRepository) that is not associated with current molpops in the ecs.
        /// Defaults to homogeneous distribution.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void AddEcmMolButton_Click(object sender, RoutedEventArgs e)
        {
            if (MainWindow.SOP.Protocol.entity_repository.molecules.Count == 0)
            {
                MessageBox.Show("There are no molecules to choose. Acquire molecules from the User store.");
                return;
            }

            ConfigMolecule cm;
            foreach (ConfigMolecule item in MainWindow.SOP.Protocol.entity_repository.molecules)
            {
                if (MainWindow.SOP.Protocol.scenario.environment.comp.molpops.Where(m => m.molecule.Name == item.Name).Any()) continue;

                // Take the first molecule that is not already in the ECS
                cm = item.Clone(null);
                if (cm == null) return;

                ConfigMolecularPopulation cmp = new ConfigMolecularPopulation(ReportType.ECM_MP);        
                cmp.molecule = item.Clone(null);
                cmp.Name = item.Name;
                MainWindow.SOP.Protocol.scenario.environment.comp.molpops.Add(cmp);

                break;
            }
            lbEcsMolPops.SelectedIndex = lbEcsMolPops.Items.Count - 1;

            CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();
            CollectionViewSource.GetDefaultView(lbAvailableReacCx.ItemsSource).Refresh();

            // Show details if user adds a molpop
            MolDetailsExpander.IsExpanded = true;
        }

        private void AddEcmReacButton_Click(object sender, RoutedEventArgs e)
        {
            bool needRefresh = false;

            foreach (var item in lvAvailableReacs.SelectedItems)
            {
                ConfigReaction reac = (ConfigReaction)item;

                if (MainWindow.SOP.Protocol.scenario.environment.comp.reactions_dict.ContainsKey(reac.entity_guid) == false)
                {
                    MainWindow.SOP.Protocol.scenario.environment.comp.Reactions.Add(reac.Clone(true));
                    needRefresh = true;
                }
            }

            //Refresh the filter
            if (needRefresh && lvAvailableReacs.ItemsSource != null)
                CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();
        }


        private void AddEcmReacCompButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigReactionComplex crc = (ConfigReactionComplex)lbAvailableReacCx.SelectedItem;

            if (crc != null)
            {
                if (MainWindow.SOP.Protocol.scenario.environment.comp.reaction_complexes.Contains(crc) == false)
                {
                    MainWindow.SOP.Protocol.scenario.environment.comp.reaction_complexes.Add(crc.Clone(true));
                    CollectionViewSource.GetDefaultView(lbAvailableReacCx.ItemsSource).Refresh();
                }
            }
        }

        /// <summary>
        /// Select gradient direction for linear molpop distribution?
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbBoundFace_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cb = (ComboBox)sender;

            if (cb.SelectedIndex == -1)
                return;

            if (cb.SelectedIndex == 0)
            {
                ((VTKFullGraphicsController)MainWindow.GC).OrientationMarker_IsChecked = false;
            }
            else
            {
                ((VTKFullGraphicsController)MainWindow.GC).OrientationMarker_IsChecked = true;
                ConfigMolecularPopulation cmp = (ConfigMolecularPopulation)(lbEcsMolPops.SelectedItem);
                if (cmp == null)
                    return;
                if (cmp.mp_distribution.mp_distribution_type == MolPopDistributionType.Linear)
                {
                    ((MolPopLinear)cmp.mp_distribution).Initalize((BoundaryFace)cb.SelectedItem);
                }
            }
        }

        private void AddGaussianSpecification(MolPopGaussian mpg, ConfigMolecularPopulation molpop)
        {
            BoxSpecification box = new BoxSpecification();
            box.x_trans = 100;
            box.y_trans = 100;
            box.z_trans = 100;
            box.x_scale = 100;
            box.y_scale = 100;
            box.z_scale = 100;
            // Add box GUI property changed to VTK callback
            box.PropertyChanged += MainWindow.GUIInteractionToWidgetCallback;

            GaussianSpecification gg = new GaussianSpecification();
            gg.box_spec = box;
           
            Color spec_color = ColorHelper.pickASolidColor();
            spec_color.A = 80;
            gg.gaussian_spec_color = spec_color;    //System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            
            // Add gauss spec property changed to VTK callback (ellipsoid actor color & visibility)
            gg.PropertyChanged += MainWindow.GUIGaussianSurfaceVisibilityToggle;
            mpg.gauss_spec = gg;

            // Add RegionControl & RegionWidget for the new gauss_spec
            ((VTKFullDataBasket)MainWindow.VTKBasket).AddGaussSpecRegionControl(gg);
            ((VTKFullGraphicsController)MainWindow.GC).AddGaussSpecRegionWidget(gg);
            // Connect the VTK callback
            ((VTKFullGraphicsController)MainWindow.GC).Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(((VTKFullGraphicsController)MainWindow.GC).WidgetInteractionToGUICallback));
            ((VTKFullGraphicsController)MainWindow.GC).Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(((ToolWinBase)MainWindow.ToolWin).RegionFocusToGUISection));
            
            ((VTKFullGraphicsController)MainWindow.GC).Rwc.Invalidate();
        }

        /// <summary>
        /// Delete a Gaussian specification.
        /// </summary>
        /// <param name="dist"></param>
        private void DeleteGaussianSpecification(MolPopDistribution dist)
        {
            MolPopGaussian mpg = dist as MolPopGaussian;

            if (mpg.gauss_spec == null || mpg.gauss_spec.box_spec == null)
            {
                return;
            }

            if (((VTKFullGraphicsController)MainWindow.GC).Regions.ContainsKey(mpg.gauss_spec.box_spec.box_guid) == true)
            {
                ((VTKFullGraphicsController)MainWindow.GC).RemoveRegionWidget(mpg.gauss_spec.box_spec.box_guid);
            }
        }

        /// <summary>
        /// Filter for bulk molecules. 
        /// gmk - Rename? Should be usable by all workbenches.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void EcsMoleculesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigMolecule mol = e.Item as ConfigMolecule;
            if (mol == null || mol.molecule_location != MoleculeLocation.Bulk)
            {
                e.Accepted = false;
                return;
            }
            e.Accepted = true;
            return;
        }


        /// <summary>
        /// Filter (return true) for reaction that has necessary molecules in the environment comp and one or more cell membrane.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ecmAvailableReactionsListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigReaction cr = e.Item as ConfigReaction;

            if (cr == null)
            {
                return;
            }

            e.Accepted = reactionIsAvailable(cr);
        }

        protected virtual void ecmAvailableReactionComplexesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigReactionComplex crc = e.Item as ConfigReactionComplex;
            bool bOK = true;

            if (crc == null)
            {
                return;
            }

            foreach (ConfigReaction cr in crc.reactions)
            {
                if (reactionIsAvailable(cr) == false)
                {
                    bOK = false;
                }
            }

            //Finally, if the ecm already contains this reaction complex, exclude it from the available reactions list
            if (MainWindow.SOP.Protocol.scenario.environment.comp.reaction_complexes_dict.ContainsKey(crc.entity_guid) == true)
            {
                bOK = false;
            }

            e.Accepted = bOK;
        }

        private bool reactionIsAvailable(ConfigReaction cr)
        {
            bool bOK = true;
            foreach (string molguid in cr.reactants_molecule_guid_ref)
            {
                //if (MainWindow.ToolWin.CompartmentHasMolecule(molguid, MainWindow.ToolWin.Protocol.scenario.environment.comp) == false
                if (MainWindow.ToolWin.Protocol.scenario.environment.comp.HasMolecule(molguid) == false
                        && MainWindow.ToolWin.CellPopsHaveMolecule(molguid, true) == false )
                {
                    return false;
                }
            }

            foreach (string molguid in cr.products_molecule_guid_ref)
            {
                if (MainWindow.ToolWin.Protocol.scenario.environment.comp.HasMolecule(molguid) == false
                            && MainWindow.ToolWin.CellPopsHaveMolecule(molguid, true) == false)
                {
                    return false;
                }
            }

            foreach (string molguid in cr.modifiers_molecule_guid_ref)
            {
                if (MainWindow.ToolWin.Protocol.scenario.environment.comp.HasMolecule(molguid) == false
                            && MainWindow.ToolWin.CellPopsHaveMolecule(molguid, true) == false)
                {
                    return false;
                }
            }

            //Finally, if the ecm already contains this reaction, exclude it from the available reactions list
            if (MainWindow.SOP.Protocol.scenario.environment.comp.reactions_dict.ContainsKey(cr.entity_guid) == true)
            {
                return false;
            }

            return bOK;
        }

        /// <summary>
        /// gmk - pushing may not work 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ecs_molpop_molecule_combo_box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            ComboBox cb = (ComboBox)e.Source;
            if (cb == null)
                return;

            ConfigMolecularPopulation molpop = (ConfigMolecularPopulation)lbEcsMolPops.SelectedItem;

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
                else
                {
                    while (ConfigMolecule.FindMoleculeByName(MainWindow.SOP.Protocol, newLibMol.Name) == true)
                    {
                        string entered_name = newLibMol.Name;
                        newLibMol.ValidateName(MainWindow.SOP.Protocol);
                        MessageBox.Show(string.Format("A molecule named {0} already exists. Please enter a unique name or accept the newly generated name.", entered_name));
                        aem = new AddEditMolecule(newLibMol, MoleculeDialogType.NEW);

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
                    }
                }

                Protocol B = MainWindow.SOP.Protocol;
                Level.PushStatus status = B.pushStatus(newLibMol);
                if (status == Level.PushStatus.PUSH_CREATE_ITEM)
                {
                    B.repositoryPush(newLibMol, status); // push into B, inserts as new
                }

                molpop.molecule = newLibMol.Clone(null);
                molpop.Name = newLibMol.Name;
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

            if (lvAvailableReacs.ItemsSource != null)
            {
                CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();
            }
            if (lbAvailableReacCx.ItemsSource != null)
            {
                CollectionViewSource.GetDefaultView(lbAvailableReacCx.ItemsSource).Refresh();
            }

            CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();
            CollectionViewSource.GetDefaultView(lbAvailableReacCx.ItemsSource).Refresh();

            //update render informaiton
            (MainWindow.SOP.Protocol.scenario as TissueScenario).popOptions.RemoveRenderOptions(molpop.renderLabel, false);
            molpop.renderLabel = molpop.molecule.entity_guid;
            (MainWindow.SOP.Protocol.scenario as TissueScenario).popOptions.AddRenderOptions(molpop.renderLabel, molpop.Name, false);


        }

        /// <summary>
        /// gmk - This is called when the gaussian eyeball button is clicked in the GUI, but doesn't seem to do anything,
        /// so I commented out the code. Target for removal.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void gaussian_region_actor_checkbox_clicked(object sender, RoutedEventArgs e)
        {
            //CheckBox cb = e.OriginalSource as CheckBox;

            //if (cb.CommandParameter == null)
            //{
            //    return;
            //}
        }

        private void MolPopDistributionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only want to respond to purposeful user interaction, not just population and depopulation
            // of ecm molecules list
            if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
            {
                return;
            }

            ConfigMolecularPopulation current_mol = (ConfigMolecularPopulation)lbEcsMolPops.SelectedItem;

            if (current_mol != null)
            {
                MolPopDistributionType new_dist_type = MolPopDistributionType.Homogeneous; 

                if (e.AddedItems.Count > 0)
                {
                    new_dist_type = (MolPopDistributionType)e.AddedItems[0];
                }


                // Only want to change distribution type if the combo box isn't just selecting 
                // the type of current item in the molpops list box (e.g. when list selection is changed)

                if (current_mol.mp_distribution == null)
                {
                }
                else if (current_mol.mp_distribution.mp_distribution_type == new_dist_type)
                {
                    return;
                }

                if (current_mol.mp_distribution != null)
                {
                    if (new_dist_type != MolPopDistributionType.Gaussian && current_mol.mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
                    {
                        DeleteGaussianSpecification(current_mol.mp_distribution);
                        MolPopGaussian mpg = current_mol.mp_distribution as MolPopGaussian;
                        mpg.gauss_spec = null;
                        ((VTKFullGraphicsController)MainWindow.GC).Rwc.Invalidate();
                    }
                }
                switch (new_dist_type)
                {
                    case MolPopDistributionType.Homogeneous:
                        MolPopHomogeneousLevel shl = new MolPopHomogeneousLevel();
                        current_mol.mp_distribution = shl;
                        break;
                    case MolPopDistributionType.Linear:
                        MolPopLinear molpoplin = new MolPopLinear();
                        // X face is default
                        molpoplin.boundaryCondition.Add(new BoundaryCondition(MolBoundaryType.Dirichlet, Boundary.left, 0.0));
                        molpoplin.boundaryCondition.Add(new BoundaryCondition(MolBoundaryType.Dirichlet, Boundary.right, 0.0));
                        molpoplin.Initalize(BoundaryFace.X);
                        molpoplin.boundary_face = BoundaryFace.X;
                      
                        current_mol.mp_distribution = molpoplin;
                        break;

                    case MolPopDistributionType.Gaussian:
                        MolPopGaussian mpg = new MolPopGaussian();

                        AddGaussianSpecification(mpg, current_mol);
                        current_mol.mp_distribution = mpg;

                        break;

                    case MolPopDistributionType.Explicit:
                        break;

                    default:
                        throw new ArgumentException("MolPop distribution type out of range");
                }
            }
        }

        /// <summary>
        /// Push changes to an ECS molecule to Protocol level.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void PushEcmMoleculeButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigMolecularPopulation molpop = (ConfigMolecularPopulation)lbEcsMolPops.SelectedItem;
            if (molpop == null)
            {
                return;
            }
            MainWindow.GenericPush(molpop.molecule.Clone(null));

            CollectionViewSource.GetDefaultView(ecm_molecule_combo_box.ItemsSource).Refresh();
        }

        private void PushEcmReacButton_Click(object sender, RoutedEventArgs e)
        {
            if (lvEcsReactions.SelectedIndex < 0)
            {
                MessageBox.Show("Please select a reaction.");
                return;
            }

            ConfigReaction reac = (ConfigReaction)lvEcsReactions.SelectedValue;
            ConfigReaction newreac = reac.Clone(true);
            MainWindow.GenericPush(newreac);
        }

        /// <summary>
        /// Remove a molecular population from the ECS.
        /// Remove any reactions and gaussian boxes that use the associated molecule.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveEcmMolButton_Click(object sender, RoutedEventArgs e)
        {
            int index = lbEcsMolPops.SelectedIndex;
            if (index >= 0)
            {
                ConfigMolecularPopulation cmp = (ConfigMolecularPopulation)lbEcsMolPops.SelectedValue;

                MessageBoxResult res = MessageBox.Show("Removing this molecular population will remove ECM reactions that use this molecule. Are you sure you would like to proceed?", "Warning", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                    return;

                foreach (ConfigReaction cr in MainWindow.SOP.Protocol.scenario.environment.comp.Reactions.ToList())
                {
                    if (MainWindow.SOP.Protocol.entity_repository.reactions_dict[cr.entity_guid].HasMolecule(cmp.molecule.entity_guid))
                    {
                        MainWindow.SOP.Protocol.scenario.environment.comp.Reactions.Remove(cr);
                    }
                }

                foreach (ConfigReactionComplex crc in MainWindow.SOP.Protocol.scenario.environment.comp.reaction_complexes.ToList())
                {
                    if (crc.molecules_dict.ContainsKey(cmp.molecule.entity_guid))
                    {
                        MainWindow.SOP.Protocol.scenario.environment.comp.reaction_complexes.Remove(crc);
                    }
                }

                //Delete the gaussian box if any
                if (cmp.mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
                {
                    DeleteGaussianSpecification(cmp.mp_distribution);
                    MolPopGaussian mpg = cmp.mp_distribution as MolPopGaussian;
                    mpg.gauss_spec = null;
                    ((VTKFullGraphicsController)MainWindow.GC).Rwc.Invalidate();
                }

                //Delete the molecular population
                MainWindow.SOP.Protocol.scenario.environment.comp.molpops.Remove(cmp);
            }

            lbEcsMolPops.SelectedIndex = index;

            if (index >= lbEcsMolPops.Items.Count)
                lbEcsMolPops.SelectedIndex = lbEcsMolPops.Items.Count - 1;

            if (lbEcsMolPops.Items.Count == 0)
                lbEcsMolPops.SelectedIndex = -1;

            CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();
            CollectionViewSource.GetDefaultView(lbAvailableReacCx.ItemsSource).Refresh();
        }

        private void RemoveEcmReacButton_Click(object sender, RoutedEventArgs e)
        {
            if (lvEcsReactions.SelectedIndex < 0)
                return;

            ConfigReaction reac = (ConfigReaction)lvEcsReactions.SelectedValue;
            if (MainWindow.SOP.Protocol.scenario.environment.comp.reactions_dict.ContainsKey(reac.entity_guid))
            {
                MainWindow.SOP.Protocol.scenario.environment.comp.Reactions.Remove(reac);

            }
        }

        private void RemoveEcmReacCompButton_Click(object sender, RoutedEventArgs e)
        {
            int nIndex = ReactionComplexListBox.SelectedIndex;

            if (nIndex >= 0)
            {
                ConfigReactionComplex rc = (ConfigReactionComplex)ReactionComplexListBox.SelectedItem;
                MainWindow.SOP.Protocol.scenario.environment.comp.reaction_complexes.Remove(rc);
                CollectionViewSource.GetDefaultView(lbAvailableReacCx.ItemsSource).Refresh();
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            CollectionViewSource cvs = (CollectionViewSource)(FindResource("EcsBulkMoleculesListView"));
            cvs.Filter += ToolWinBase.FilterFactory.BulkMolecules_Filter;

            lbEcsMolPops.SelectedIndex = 0;
        }


    }
}
