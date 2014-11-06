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
using System.Windows.Shapes;

using Daphne;
using ActiproSoftware.Windows.Controls.Docking;
using Workbench;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for ToolWinTissue.xaml
    /// </summary>
    public partial class ToolWinTissue : ToolWinBase
    {

        public ToolWinTissue()
        {
            TitleText = "Tissue Simulation";
            ToroidalVisibility = Visibility.Visible;
            SimRepetitionVisibility = Visibility.Visible;
            ZExtentVisibility = Visibility.Visible;

            InitializeComponent();
            DataContext = this;

            VTKDisplayWindow = new VTKDisplaDocWindow();
            ContentComponents.Add(VTKDisplayWindow);

            var ComponentsToolWindow = new ComponentsToolWindow();
            ContentComponents.Add(ComponentsToolWindow);

            var CellStudioToolWindow = new CellStudioToolWindow();
            ContentComponents.Add(CellStudioToolWindow);


            var ReacComplexChartWindow = new ChartViewToolWindow();
            ContentComponents.Add(ReacComplexChartWindow);

            //CollectionViewSource cvs = (CollectionViewSource)(FindResource("EcsBulkMoleculesListView"));
            //cvs.Filter += FilterFactory.bulkMoleculesListView_Filter;
        }

        ///// <summary>
        ///// Add a new molecular population to the ECS. 
        ///// Defaults to first molecule (in EntityRepository) that is not associated with current molpops in the ecs.
        ///// Defaults to homogeneous distribution.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void AddEcmMolButton_Click(object sender, RoutedEventArgs e)
        //{
        //    if (MainWindow.SOP.Protocol.entity_repository.molecules.Count == 0)
        //    {
        //        MessageBox.Show("There are no molecules to choose. Acquire molecules from the User store.");
        //        return;
        //    }

        //    ConfigMolecule cm;
        //    //foreach (ConfigMolecule item in cvs.View)
        //    foreach (ConfigMolecule item in Protocol.entity_repository.molecules)
        //    {
        //        if (MainWindow.SOP.Protocol.scenario.environment.comp.molpops.Where(m => m.molecule.Name == item.Name).Any()) continue;

        //        // Take the first molecule that is not already in the ECS
        //        cm = item.Clone(null);
        //        if (cm == null) return;
        //        AddMolPopToCmpartment(cm, MainWindow.SOP.Protocol.scenario.environment.comp, false);
        //        break;
        //    }
        //    lbEcsMolPops.SelectedIndex = lbEcsMolPops.Items.Count - 1;
        //}

        /// <summary>
        /// Actions to preserve tab focus when the Apply button is pushed.
        /// </summary>
        public override void Apply()
        {
            TabItem selectedTab = toolWinTissue.ConfigTabControl.SelectedItem as TabItem;

            int cellPopSelIndex = -1;
            if (selectedTab == toolWinTissue.tabCellPop)
            {
                // gmk - fix after this functionality is added
                // cellPopSelIndex = toolWinTissue.CellPopsListBox.SelectedIndex;
            }

            int ecmMolPopSelIndex = -1;
            if (selectedTab == toolWinTissue.tabECM)
            {
                ecmMolPopSelIndex = toolWinTissue.EcmMolpopControl.lbEcsMolPops.SelectedIndex;
            }

            int reportECMmolSelectedIndex = -1;
            int reportCellSelectedIndex = -1;
            int reportCellPopSelectedIndex = -1;
            int reportCellStateSelectedIndex = -1;
            if (selectedTab == tabReports)
            {
                reportECMmolSelectedIndex = toolWinTissue.dgEcmMols.SelectedIndex;
                reportCellSelectedIndex = toolWinTissue.dgCellDetails.SelectedIndex;
                reportCellPopSelectedIndex = toolWinTissue.lbRptCellPops.SelectedIndex;
                reportCellStateSelectedIndex = toolWinTissue.dgCellStates.SelectedIndex;
            }

            MW.Apply();

            toolWinTissue.ConfigTabControl.SelectedItem = selectedTab;

            if (selectedTab == toolWinTissue.tabCellPop)
            {
                // gmk - fix after this functionality is added
                //toolWinTissue.CellPopsListBox.SelectedIndex = nCellPopSelIndex;
            }
            else if (selectedTab == toolWinTissue.tabECM)
            {
                toolWinTissue.EcmMolpopControl.lbEcsMolPops.SelectedIndex = ecmMolPopSelIndex;
            }
            else if (selectedTab == toolWinTissue.tabReports)
            {
                toolWinTissue.dgEcmMols.SelectedIndex = reportECMmolSelectedIndex;
                toolWinTissue.dgCellDetails.SelectedIndex = reportCellSelectedIndex;
                toolWinTissue.lbRptCellPops.SelectedIndex = reportCellPopSelectedIndex;
                toolWinTissue.dgCellStates.SelectedIndex = reportCellStateSelectedIndex;
            }
        }

        ///// <summary>
        ///// Select gradient direction for linear molpop distribution?
        ///// gmk - Try to write more generally and move to base class to be used by CellRC.
        ///// Make xaml code a control?
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //protected void cbBoundFace_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    ComboBox cb = (ComboBox)sender;

        //    if (cb.SelectedIndex == -1)
        //        return;

        //    if (cb.SelectedIndex == 0)
        //    {
        //        ((VTKFullGraphicsController)MainWindow.GC).OrientationMarker_IsChecked = false;
        //    }
        //    else
        //    {
        //        ((VTKFullGraphicsController)MainWindow.GC).OrientationMarker_IsChecked = true;
        //        ConfigMolecularPopulation cmp = (ConfigMolecularPopulation)(lbEcsMolPops.SelectedItem);
        //        if (cmp == null)
        //            return;
        //        if (cmp.mp_distribution.mp_distribution_type == MolPopDistributionType.Linear)
        //        {
        //            ((MolPopLinear)cmp.mp_distribution).Initalize((BoundaryFace)cb.SelectedItem);
        //        }
        //    }
        //}

        protected override void ConfigTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (tabECM.IsSelected == true)
            //{
            //    // gmk - fix after this functionality is added
            //    //if (lvAvailableReacs.ItemsSource != null)
            //    //{
            //    //    CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();
            //    //}
            //}
        }

        //private void ecs_molpop_molecule_combo_box_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{

        //    ComboBox cb = (ComboBox)e.Source;
        //    if (cb == null)
        //        return;

        //    ConfigMolecularPopulation molpop = (ConfigMolecularPopulation)lbEcsMolPops.SelectedItem;

        //    if (molpop == null)
        //        return;

        //    string curr_mol_pop_name = molpop.Name;
        //    string curr_mol_guid = "";
        //    curr_mol_guid = molpop.molecule.entity_guid;

        //    int nIndex = cb.SelectedIndex;
        //    if (nIndex < 0)
        //        return;

        //    //if user picked 'new molecule' then create new molecule in ER
        //    if (nIndex == (cb.Items.Count - 1))
        //    {
        //        ConfigMolecule newLibMol = new ConfigMolecule();
        //        newLibMol.Name = newLibMol.GenerateNewName(MainWindow.SOP.Protocol, "_New");
        //        AddEditMolecule aem = new AddEditMolecule(newLibMol, MoleculeDialogType.NEW);

        //        //Point position = cb.PointToScreen(new Point(0d, 0d));
        //        //double wid = cb.Width;
        //        //aem.Top = position.Y;
        //        //aem.Left = position.X + wid;

        //        //if user cancels out of new molecule dialog, set selected molecule back to what it was
        //        if (aem.ShowDialog() == false)
        //        {
        //            if (e.RemovedItems.Count > 0)
        //            {
        //                cb.SelectedItem = e.RemovedItems[0];
        //            }
        //            else
        //            {
        //                cb.SelectedIndex = 0;
        //            }
        //            return;
        //        }
        //        else
        //        {
        //            while (ConfigMolecule.FindMoleculeByName(MainWindow.SOP.Protocol, newLibMol.Name) == true)
        //            {
        //                string entered_name = newLibMol.Name;
        //                newLibMol.ValidateName(MainWindow.SOP.Protocol);
        //                MessageBox.Show(string.Format("A molecule named {0} already exists. Please enter a unique name or accept the newly generated name.", entered_name));
        //                aem = new AddEditMolecule(newLibMol, MoleculeDialogType.NEW);

        //                if (aem.ShowDialog() == false)
        //                {
        //                    if (e.RemovedItems.Count > 0)
        //                    {
        //                        cb.SelectedItem = e.RemovedItems[0];
        //                    }
        //                    else
        //                    {
        //                        cb.SelectedIndex = 0;
        //                    }
        //                    return;
        //                }
        //            }
        //        }

        //        Protocol B = MainWindow.SOP.Protocol;
        //        newLibMol.incrementChangeStamp();
        //        Level.PushStatus status = B.pushStatus(newLibMol);
        //        if (status == Level.PushStatus.PUSH_CREATE_ITEM)
        //        {
        //            B.repositoryPush(newLibMol, status); // push into B, inserts as new
        //        }

        //        molpop.molecule = newLibMol.Clone(null);
        //        molpop.Name = newLibMol.Name;
        //        cb.SelectedItem = newLibMol;


        //    }
        //    //user picked an existing molecule
        //    else
        //    {
        //        ConfigMolecule newmol = (ConfigMolecule)cb.SelectedItem;

        //        //if molecule has not changed, return
        //        if (newmol.entity_guid == curr_mol_guid)
        //        {
        //            return;
        //        }

        //        //if molecule changed, then make a clone of the newly selected one from entity repository
        //        ConfigMolecule mol = (ConfigMolecule)cb.SelectedItem;
        //        molpop.molecule = mol.Clone(null);

        //        string new_mol_name = mol.Name;
        //        if (curr_mol_guid != molpop.molecule.entity_guid)
        //            molpop.Name = new_mol_name;
        //    }

        //    ////update render informaiton
        //    //(MainWindow.SOP.Protocol.scenario as TissueScenario).popOptions.RemoveRenderOptions(molpop.renderLabel, false);
        //    //molpop.renderLabel = molpop.molecule.entity_guid;
        //    //(MainWindow.SOP.Protocol.scenario as TissueScenario).popOptions.AddRenderOptions(molpop.renderLabel, molpop.Name, false);

        //    //if (lvAvailableReacs.ItemsSource != null)
        //    //    CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();
        //}

        //private void MolPopDistributionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    // this window seems to implement the tissue scenario gui; throw an exception for now to enforce that;
        //    // Sanjeev, you probably need to have a hierachy of tool windows where each implements the gui for one case,
        //    // but I don't know for sure; we can discuss
        //    if (MainWindow.SOP.Protocol.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == false)
        //    {
        //        throw new InvalidCastException();
        //    }

        //    // Only want to respond to purposeful user interaction, not just population and depopulation
        //    // of solfacs list
        //    if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
        //    {
        //        return;
        //    }

        //    ConfigMolecularPopulation current_mol = (ConfigMolecularPopulation)lbEcsMolPops.SelectedItem;

        //    if (current_mol != null)
        //    {
        //        MolPopDistributionType new_dist_type = MolPopDistributionType.Homogeneous; // = MolPopDistributionType.Gaussian;

        //        if (e.AddedItems.Count > 0)
        //        {
        //            new_dist_type = (MolPopDistributionType)e.AddedItems[0];
        //        }


        //        // Only want to change distribution type if the combo box isn't just selecting 
        //        // the type of current item in the solfacs list box (e.g. when list selection is changed)

        //        if (current_mol.mp_distribution == null)
        //        {
        //        }
        //        else if (current_mol.mp_distribution.mp_distribution_type == new_dist_type)
        //        {
        //            return;
        //        }

        //        if (current_mol.mp_distribution != null)
        //        {
        //            if (new_dist_type != MolPopDistributionType.Gaussian && current_mol.mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
        //            {
        //                DeleteGaussianSpecification(current_mol.mp_distribution);
        //                MolPopGaussian mpg = current_mol.mp_distribution as MolPopGaussian;
        //                mpg.gauss_spec = null;
        //                ((VTKFullGraphicsController)MainWindow.GC).Rwc.Invalidate();
        //            }
        //        }
        //        switch (new_dist_type)
        //        {
        //            case MolPopDistributionType.Homogeneous:
        //                MolPopHomogeneousLevel shl = new MolPopHomogeneousLevel();
        //                current_mol.mp_distribution = shl;
        //                break;
        //            case MolPopDistributionType.Linear:
        //                MolPopLinear molpoplin = new MolPopLinear();
        //                // X face is default
        //                molpoplin.boundaryCondition.Add(new BoundaryCondition(MolBoundaryType.Dirichlet, Boundary.left, 0.0));
        //                molpoplin.boundaryCondition.Add(new BoundaryCondition(MolBoundaryType.Dirichlet, Boundary.right, 0.0));
        //                molpoplin.Initalize(BoundaryFace.X);
        //                molpoplin.boundary_face = BoundaryFace.X;
        //                current_mol.mp_dist_name = "Linear";
        //                current_mol.mp_distribution = molpoplin;
        //                break;

        //            case MolPopDistributionType.Gaussian:
        //                MolPopGaussian mpg = new MolPopGaussian();

        //                AddGaussianSpecification(mpg, current_mol);
        //                current_mol.mp_distribution = mpg;

        //                break;

        //            case MolPopDistributionType.Explicit:
        //                break;

        //            default:
        //                throw new ArgumentException("MolPop distribution type out of range");
        //        }
        //    }
        //}

        ///// <summary>
        ///// Push changes to an ECS molecule to Protocol level.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void PushEcmMoleculeButton_Click(object sender, RoutedEventArgs e)
        //{
        //    ConfigMolecularPopulation molpop = (ConfigMolecularPopulation)lbEcsMolPops.SelectedItem;
        //    if (molpop == null)
        //    {
        //        return;
        //    }
        //    MainWindow.GenericPush(molpop.molecule.Clone(null));
        //}


        ///// <summary>
        ///// Remove a molecular population from the ECS.
        ///// Remove any reactions and gaussian boxes that use the associated molecule.
        ///// </summary>
        ///// <param name="sender"></param>
        ///// <param name="e"></param>
        //private void RemoveEcmMolButton_Click(object sender, RoutedEventArgs e)
        //{
        //    int index = lbEcsMolPops.SelectedIndex;
        //    if (index >= 0)
        //    {
        //        ConfigMolecularPopulation cmp = (ConfigMolecularPopulation)lbEcsMolPops.SelectedValue;

        //        MessageBoxResult res = MessageBox.Show("Removing this molecular population will remove ECM reactions that use this molecule. Are you sure you would like to proceed?", "Warning", MessageBoxButton.YesNo);
        //        if (res == MessageBoxResult.No)
        //            return;

        //        foreach (ConfigReaction cr in MainWindow.SOP.Protocol.scenario.environment.comp.Reactions.ToList())
        //        {
        //            if (MainWindow.SOP.Protocol.entity_repository.reactions_dict[cr.entity_guid].HasMolecule(cmp.molecule.entity_guid))
        //            {
        //                MainWindow.SOP.Protocol.scenario.environment.comp.Reactions.Remove(cr);
        //            }
        //        }

        //        //Delete the gaussian box if any
        //        if (cmp.mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian)
        //        {
        //            DeleteGaussianSpecification(cmp.mp_distribution);
        //            MolPopGaussian mpg = cmp.mp_distribution as MolPopGaussian;
        //            mpg.gauss_spec = null;
        //            ((VTKFullGraphicsController)MainWindow.GC).Rwc.Invalidate();
        //        }

        //        //Delete the molecular population
        //        MainWindow.SOP.Protocol.scenario.environment.comp.molpops.Remove(cmp);

        //        // gmk - fix this when reactions are adde to the ECS tab
        //        //CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource).Refresh();
        //    }

        //    lbEcsMolPops.SelectedIndex = index;

        //    if (index >= lbEcsMolPops.Items.Count)
        //        lbEcsMolPops.SelectedIndex = lbEcsMolPops.Items.Count - 1;

        //    if (lbEcsMolPops.Items.Count == 0)
        //        lbEcsMolPops.SelectedIndex = -1;
        //}

        public override void RegionFocusToGUISection(RegionWidget rw, bool transferMatrix)
        {
            // identify the widget's key
            string key = "";

            if (rw != null && ((VTKFullGraphicsController)MainWindow.GC).Regions.ContainsValue(rw) == true)
            {
                foreach (KeyValuePair<string, RegionWidget> kvp in ((VTKFullGraphicsController)MainWindow.GC).Regions)
                {
                    if (kvp.Value == rw)
                    {
                        key = kvp.Key;
                        break;
                    }
                }

                // found?
                if (key != "")
                {
                    // Determine if the region is associated with a molpop
                    bool gui_spot_found = false;

                    if (!gui_spot_found)
                    {
                        for (int r = 0; r < MainWindow.SOP.Protocol.scenario.environment.comp.molpops.Count; r++)
                        {
                            if (MainWindow.SOP.Protocol.scenario.environment.comp.molpops[r].mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian &&
                                ((MolPopGaussian)MainWindow.SOP.Protocol.scenario.environment.comp.molpops[r].mp_distribution).gauss_spec.box_spec.box_guid == key)
                            {
                                gui_spot_found = true;

                                // Select this molpop in the GUI
                                toolWinTissue.EcmMolpopControl.lbEcsMolPops.SelectedIndex = r;
                                //SelectMolpopInGUI(r);

                                // Select the ECM tab for focus
                                toolWinTissue.ConfigTabControl.SelectedItem = tabECM;
                                
                                break;
                            }
                        }
                    }
                    if (!gui_spot_found)
                    {
                        // Determine if the region is associated with a cellpop
                        for (int r = 0; r < ((TissueScenario)MainWindow.SOP.Protocol.scenario).cellpopulations.Count; r++)
                        {
                            if (((TissueScenario)MainWindow.SOP.Protocol.scenario).cellpopulations[r].cellPopDist.DistType == CellPopDistributionType.Gaussian)
                            {
                                if (((CellPopGaussian)((TissueScenario)MainWindow.SOP.Protocol.scenario).cellpopulations[r].cellPopDist).gauss_spec.box_spec.box_guid == key)
                                {
                                    gui_spot_found = true;

                                    // Select this cellpop in the GUI
                                    //toolWinTissue.tabCellPop.???? = r;

                                    // Select the Cellpop tab for focus
                                    toolWinTissue.ConfigTabControl.SelectedItem = tabCellPop;


                                }
                            }

                        }

                    }
                }
            }
        }


        private void btnNewSkinClick(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            if (skinNameTextBox.Visibility == System.Windows.Visibility.Collapsed)
            {
                skinLabel.Visibility = System.Windows.Visibility.Visible;
                skinNameTextBox.Visibility = System.Windows.Visibility.Visible;
                button.Content = "Create";
                return;
            }
            //get a name for the new skin
            string skinName = skinNameTextBox.Text;
            skinNameTextBox.Visibility = System.Windows.Visibility.Collapsed;
            skinLabel.Visibility = System.Windows.Visibility.Collapsed;
            skinNameTextBox.Text = "";
            button.Content = "New Skin";
            if (skinName == null || skinName.Length == 0)
            {
                skinNote.Text = "No Name Given";
                return;
            }

            RenderSkin skin = MainWindow.SOP.SkinList.Where(x => x.Name == skinName).SingleOrDefault();
            if (skin != null)
            {
                var result = MessageBox.Show("A skin with the given name exists, Do you want to overwrite it? ", "Warning", MessageBoxButton.YesNo);
                if (result != MessageBoxResult.Yes)
                {
                    skinNote.Text = "Creating new skin cancelled";
                    return;
                }
            }

            var er = MainWindow.SOP.Protocol.entity_repository;
            RenderSkin newrs = new RenderSkin(skinName, er);
            //serialize to file
            string SkinFilePath = new Uri(MainWindow.appPath + @"\Config\RenderSkin\" + skinName + ".json").LocalPath;
            newrs.SerializeToFile(SkinFilePath);
            newrs.FileName = SkinFilePath;
            if (skin != null)
            {
                int index = MainWindow.SOP.SkinList.IndexOf(skin);
                MainWindow.SOP.SkinList.RemoveAt(index);
                MainWindow.SOP.SkinList.Insert(index, newrs);
                skinNote.Text = "skin data regenerated";
            }
            else
            {
                MainWindow.SOP.SkinList.Add(newrs);
                skinNote.Text = "New Skin created";
            }

            var cv = (CollectionView)CollectionViewSource.GetDefaultView(MainWindow.SOP.SkinList);
            if (cv != null)
            {
                cv.MoveCurrentTo(newrs);
            }
        }

        private void Button_Click_Edit_RenderSkin(object sender, RoutedEventArgs e)
        {
            var item = skinChoiceComboBox.SelectedItem;

            MainWindow.ST_RenderSkinWindow.DataContext = item;
            MainWindow.ST_RenderSkinWindow.Visibility = System.Windows.Visibility.Visible;
            MainWindow.ST_RenderSkinWindow.Activate();
        }

        private void skinChoiceComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MainWindow.ST_RenderSkinWindow.Visibility != System.Windows.Visibility.Visible) return;
            var item = skinChoiceComboBox.SelectedItem;
            MainWindow.ST_RenderSkinWindow.DataContext = item;
            //MainWindow.ST_RenderSkinWindow.Activate();
        }


    }
}
