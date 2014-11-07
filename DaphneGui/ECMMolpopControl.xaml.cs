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

        /// <summary>
        /// Add an instance of the default box to the entity repository.
        /// Default values: box center at center of ECS, box widths are 1/4 of ECS extents
        /// </summary>
        /// <param name="box"></param>
        private void AddDefaultBoxSpec(BoxSpecification box)
        {
            ConfigECSEnvironment envHandle = (ConfigECSEnvironment)MainWindow.SOP.Protocol.scenario.environment;

            box.x_trans = envHandle.extent_x / 2;
            box.y_trans = envHandle.extent_y / 2;
            box.z_trans = envHandle.extent_z / 2; ;
            box.x_scale = envHandle.extent_x / 4; ;
            box.y_scale = envHandle.extent_x / 4; ;
            box.z_scale = envHandle.extent_x / 4; ;
            // Add box GUI property changed to VTK callback
            box.PropertyChanged += MainWindow.GUIInteractionToWidgetCallback;
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
            gg.gaussian_spec_name = "New on-center gradient";
            // gmk - fix after merging Axin's changes from main
            //Color spec_color = ColorHelper.pickASolidColor();
            //spec_color.A = 80;
            //gg.gaussian_spec_color = spec_color;    //System.Windows.Media.Color.FromScRgb(0.3f, 1.0f, 0.5f, 0.5f);
            
            // Add gauss spec property changed to VTK callback (ellipsoid actor color & visibility)
            gg.PropertyChanged += MainWindow.GUIGaussianSurfaceVisibilityToggle;
            mpg.gauss_spec = gg;

            // Add RegionControl & RegionWidget for the new gauss_spec
            ((VTKFullDataBasket)MainWindow.VTKBasket).AddGaussSpecRegionControl(gg);
            ((VTKFullGraphicsController)MainWindow.GC).AddGaussSpecRegionWidget(gg);
            // Connect the VTK callback
            ((VTKFullGraphicsController)MainWindow.GC).Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(((VTKFullGraphicsController)MainWindow.GC).WidgetInteractionToGUICallback));
            ((VTKFullGraphicsController)MainWindow.GC).Regions[box.box_guid].AddCallback(new RegionWidget.CallbackHandler(((ToolWinBase)MainWindow.ProtocolToolWin).RegionFocusToGUISection));
            
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
                newLibMol.incrementChangeStamp();
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

            ////update render informaiton
            //(MainWindow.SOP.Protocol.scenario as TissueScenario).popOptions.RemoveRenderOptions(molpop.renderLabel, false);
            //molpop.renderLabel = molpop.molecule.entity_guid;
            //(MainWindow.SOP.Protocol.scenario as TissueScenario).popOptions.AddRenderOptions(molpop.renderLabel, molpop.Name, false);

            //if (lvAvailableReacs.ItemsSource != null)
            //    CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();
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
                        current_mol.mp_dist_name = "Linear";
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

                // gmk - fix this when reactions are adde to the ECS tab
                //CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();
            }

            lbEcsMolPops.SelectedIndex = index;

            if (index >= lbEcsMolPops.Items.Count)
                lbEcsMolPops.SelectedIndex = lbEcsMolPops.Items.Count - 1;

            if (lbEcsMolPops.Items.Count == 0)
                lbEcsMolPops.SelectedIndex = -1;
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }


    }
}
