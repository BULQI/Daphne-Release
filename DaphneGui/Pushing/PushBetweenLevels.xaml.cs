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
using System.Collections.ObjectModel;
using Daphne;
using System.Reflection;
using System.IO;

namespace DaphneGui.Pushing
{
    /// <summary>
    /// Interaction logic for PushBetweenLevels.xaml
    /// </summary>
    public partial class PushBetweenLevels : Window
    {
        public enum PushLevelEntityType { Molecule = 0, Gene, Reaction, Cell };

        public PushLevelEntityType PushEntityType { get; set; }
        public PushLevel PushLevelA { get; set; }
        public PushLevel PushLevelB { get; set; }
        public Level LevelA { get; set; }
        public Level LevelB { get; set; }

        public object LeftList { get; set; }
        public object RightList { get; set; }

        public PushBetweenLevels(PushLevelEntityType type)
        {
            PushEntityType = type;
            PushLevelA = PushLevel.UserStore;
            PushLevelB = PushLevel.Protocol;
            LeftList = null;
            RightList = null;
            InitializeComponent();
            Tag = this;

            if (type == PushLevelEntityType.Molecule)
            {
                this.Title = "Save Molecules Between Levels";
            }
            else if (type == PushLevelEntityType.Reaction)
            {
                this.Title = "Save Reactions Between Levels";
            }
            else if (type == PushLevelEntityType.Gene)
            {
                this.Title = "Save Genes Between Levels";
            }
            else if (type == PushLevelEntityType.Cell)
            {
                this.Title = "Save Cells Between Levels";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ResetGrids();
            ActualButtonImage.Source = RightImage.Source;
            
        }

        private void ResetGrids()
        {
            switch (PushLevelA)
            {
                case PushLevel.Protocol:
                    LevelA = MainWindow.SOP.Protocol;
                    break;
                case PushLevel.DaphneStore:
                    LevelA = MainWindow.SOP.DaphneStore;
                    break;
                case PushLevel.UserStore:
                    LevelA = MainWindow.SOP.UserStore;
                    break;
            }

            switch (PushLevelB)
            {
                case PushLevel.Protocol:
                    LevelB = MainWindow.SOP.Protocol;
                    break;
                case PushLevel.DaphneStore:
                    LevelB = MainWindow.SOP.DaphneStore;
                    break;
                case PushLevel.UserStore:
                    LevelB = MainWindow.SOP.UserStore;
                    break;
            }

            switch (PushEntityType)
            {
                case PushLevelEntityType.Molecule:
                    LeftList = LevelA.entity_repository.molecules;
                    RightList = LevelB.entity_repository.molecules;
                    break;
                case PushLevelEntityType.Gene:
                    LeftList = LevelA.entity_repository.genes;
                    RightList = LevelB.entity_repository.genes;
                    break;
                case PushLevelEntityType.Reaction:
                    LeftList = LevelA.entity_repository.reactions;
                    RightList = LevelB.entity_repository.reactions;
                    break;
                case PushLevelEntityType.Cell:
                    LeftList = LevelA.entity_repository.cells;
                    RightList = LevelB.entity_repository.cells;
                    break;
                default:
                    break;
            }

            //If window not yet loaded, controls are null so return;
            if (RightGroup == null || RightContent == null || LeftGroup == null || LeftContent == null)
            {
                return;
            }

            RightGroup.DataContext = LevelB.entity_repository;
            RightContent.DataContext = FilteredRightList();          //RightList;   //must do this first (??)
            LeftGroup.DataContext = LevelA.entity_repository;
            LeftContent.DataContext = FilteredLeftList();
        }

        private object FilteredLeftList()
        {
            switch (PushEntityType)
            {
                case PushLevelEntityType.Molecule:
                    ObservableCollection<ConfigMolecule> left = (ObservableCollection<ConfigMolecule>)LeftList;
                    ObservableCollection<ConfigMolecule> right = (ObservableCollection<ConfigMolecule>)RightList;
                    ObservableCollection<ConfigMolecule> filtered_mol_list = new ObservableCollection<ConfigMolecule>();

                    foreach (ConfigMolecule mol in left)
                    {
                        ConfigMolecule mol2 = FindMolInList(right, mol);
                        if (mol2 == null) {
                            filtered_mol_list.Add(mol);
                        }
                        else {
                            if (mol.change_stamp != mol2.change_stamp) {
                                filtered_mol_list.Add(mol);
                            }
                        }
                    }
                    return filtered_mol_list;
                case PushLevelEntityType.Gene:
                    ObservableCollection<ConfigGene> left2 = (ObservableCollection<ConfigGene>)LeftList;
                    ObservableCollection<ConfigGene> right2 = (ObservableCollection<ConfigGene>)RightList;
                    ObservableCollection<ConfigGene> filtered_gene_list = new ObservableCollection<ConfigGene>();

                    foreach (ConfigGene gene in left2)
                    {
                        ConfigGene gene2 = FindGeneInList(right2, gene);
                        if (gene2 == null) {
                            filtered_gene_list.Add(gene);
                        }
                        else {
                            if (gene.change_stamp != gene2.change_stamp)
                            {
                                filtered_gene_list.Add(gene);
                            }
                        }
                    }
                    return filtered_gene_list;
                case PushLevelEntityType.Reaction:
                    ObservableCollection<ConfigReaction> left3 = (ObservableCollection<ConfigReaction>)LeftList;
                    ObservableCollection<ConfigReaction> right3 = (ObservableCollection<ConfigReaction>)RightList;
                    ObservableCollection<ConfigReaction> filtered_reac_list = new ObservableCollection<ConfigReaction>();

                    foreach (ConfigReaction reac in left3)
                    {
                        ConfigReaction reac2 = FindReactionInList(right3, reac);
                        if (reac2 == null)
                        {
                            filtered_reac_list.Add(reac);
                        }
                        else {
                            if (reac.change_stamp != reac2.change_stamp)
                            {
                                filtered_reac_list.Add(reac);
                            }
                        }
                    }
                    return filtered_reac_list;
                case PushLevelEntityType.Cell:
                    ObservableCollection<ConfigCell> left4 = (ObservableCollection<ConfigCell>)LeftList;
                    ObservableCollection<ConfigCell> right4 = (ObservableCollection<ConfigCell>)RightList;
                    ObservableCollection<ConfigCell> filtered_cell_list = new ObservableCollection<ConfigCell>();

                    foreach (ConfigCell cell in left4)
                    {
                        ConfigCell cell2 = FindCellInList(right4, cell);
                        if (cell2 == null)
                        {
                            filtered_cell_list.Add(cell);
                        }
                        else {
                            if (cell.change_stamp != cell2.change_stamp)
                            {
                                filtered_cell_list.Add(cell);
                            }
                        }
                    }
                    return filtered_cell_list;
                default:
                    return null;
            }
        }

        private object FilteredRightList()
        {
            switch (PushEntityType)
            {
                case PushLevelEntityType.Molecule:
                    ObservableCollection<ConfigMolecule> left = (ObservableCollection<ConfigMolecule>)LeftList;
                    ObservableCollection<ConfigMolecule> right = (ObservableCollection<ConfigMolecule>)RightList;
                    ObservableCollection<ConfigMolecule> filtered_mol_list = new ObservableCollection<ConfigMolecule>();

                    foreach (ConfigMolecule mol in right)
                    {
                        ConfigMolecule mol2 = FindMolInList(left, mol);
                        if (mol2 != null)
                        {
                            if (mol.change_stamp != mol2.change_stamp)
                            {
                                filtered_mol_list.Add(mol);
                            }
                        }
                    }
                    return filtered_mol_list;
                case PushLevelEntityType.Gene:
                    ObservableCollection<ConfigGene> left2 = (ObservableCollection<ConfigGene>)LeftList;
                    ObservableCollection<ConfigGene> right2 = (ObservableCollection<ConfigGene>)RightList;
                    ObservableCollection<ConfigGene> filtered_gene_list = new ObservableCollection<ConfigGene>();

                    foreach (ConfigGene gene in right2)
                    {
                        ConfigGene gene2 = FindGeneInList(left2, gene);
                        if (gene.change_stamp != gene2.change_stamp)
                            {
                                filtered_gene_list.Add(gene);
                            }
                    }
                    return filtered_gene_list;
                case PushLevelEntityType.Reaction:
                    ObservableCollection<ConfigReaction> left3 = (ObservableCollection<ConfigReaction>)LeftList;
                    ObservableCollection<ConfigReaction> right3 = (ObservableCollection<ConfigReaction>)RightList;
                    ObservableCollection<ConfigReaction> filtered_reac_list = new ObservableCollection<ConfigReaction>();

                    foreach (ConfigReaction reac in right3)
                    {
                        ConfigReaction reac2 = FindReactionInList(left3, reac);
                        if (reac.change_stamp != reac2.change_stamp)
                            {
                                filtered_reac_list.Add(reac);
                            }
                    }
                    return filtered_reac_list;
                case PushLevelEntityType.Cell:
                    ObservableCollection<ConfigCell> left4 = (ObservableCollection<ConfigCell>)LeftList;
                    ObservableCollection<ConfigCell> right4 = (ObservableCollection<ConfigCell>)RightList;
                    ObservableCollection<ConfigCell> filtered_cell_list = new ObservableCollection<ConfigCell>();

                    foreach (ConfigCell cell in right4)
                    {
                        ConfigCell cell2 = FindCellInList(left4, cell);
                        if (cell.change_stamp != cell2.change_stamp)
                            {
                                filtered_cell_list.Add(cell);
                            }
                    }
                    return filtered_cell_list;
                default:
                    return null;
            }
        }

        //Helper methods
        private ConfigMolecule FindMolInList(ObservableCollection<ConfigMolecule> list, ConfigMolecule mol)
        {
            foreach (ConfigMolecule m in list)
            {
                if (mol.entity_guid == m.entity_guid)
                {
                    return m;
                }
            }
            return null;
        }
        private ConfigGene FindGeneInList(ObservableCollection<ConfigGene> list, ConfigGene entity)
        {
            foreach (ConfigGene e in list)
            {
                if (entity.entity_guid == e.entity_guid)
                {
                    return e;
                }
            }
            return null;
        }
        private ConfigReaction FindReactionInList(ObservableCollection<ConfigReaction> list, ConfigReaction entity)
        {
            foreach (ConfigReaction e in list)
            {
                if (entity.entity_guid == e.entity_guid)
                {
                    return e;
                }
            }
            return null;
        }
        private ConfigCell FindCellInList(ObservableCollection<ConfigCell> list, ConfigCell entity)
        {
            foreach (ConfigCell e in list)
            {
                if (entity.entity_guid == e.entity_guid)
                {
                    return e;
                }
            }
            return null;
        }

        private void pushMoleculesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigMolecule source_mol = e.Item as ConfigMolecule;
            ConfigMolecule dest_mol = null;

            if (source_mol == null)
                return;

            ObservableCollection<ConfigMolecule> dest_collection = (ObservableCollection<ConfigMolecule>)RightList;

            foreach (ConfigMolecule mol in dest_collection)
            {
                if (mol.entity_guid == source_mol.entity_guid)
                {
                    dest_mol = mol;
                    break;
                }
            }
            if (dest_mol == null) {
                e.Accepted = true;
                return;
            }
            else if (source_mol.change_stamp != dest_mol.change_stamp)
            {
                e.Accepted = true;
            }
            else
            {
                e.Accepted = false;
            }
        }

        private void LevelAComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            PushLevel level = (PushLevel)combo.SelectedItem;

            PushLevelA = level;

            if (PushLevelA == PushLevel.Protocol && (string)(PushButtonArrow.Tag) == "Right")
            {
                PushLevelB = PushLevel.UserStore;
                LevelBComboBox.SelectedIndex = 1;
            }

            if (LeftGroup == null)
                return;

            ResetGrids();
        }

        private void LevelBComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            PushLevel level = (PushLevel)combo.SelectedItem;

            PushLevelB = level;

            //if (PushLevelB == PushLevel.Protocol && (string)(PushButtonArrow.Tag) == "Left")
            //{
            //    PushLevelA = PushLevel.UserStore;
            //}

            if (RightGroup == null)
                return;

            ResetGrids();
        }

        public void PushCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            if (PushLevelA == PushLevelB) {
                MessageBox.Show("The target library must be different from the source library");
                return;
            }

            string messageBoxText = "Are you sure you want to save the selected entities to the target library?";
            string caption = "Save Entities";
            MessageBoxButton button = MessageBoxButton.YesNo;
            MessageBoxImage icon = MessageBoxImage.Question;

            // Display message box
            MessageBoxResult result = MessageBox.Show(messageBoxText, caption, button, icon);

            // Process message box results
            if (result == MessageBoxResult.Yes)
            {
                //Get the datagrid
                //Push each item that is in SelectedItems list
                List<DataGrid> grids = DataGridBehavior.GetVisualChildCollection<DataGrid>(LeftGridStackPanel);                
                var grid = grids[0];
                foreach (ConfigEntity ent in grid.SelectedItems)
                {
                    GenericPusher(ent, LevelA, LevelB);
                }
            }
            ResetGrids();
        }

        /// <summary>
        /// can execute command handler for delete db - enables/disables the Push button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PushCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            
            DataGrid grid = sender as DataGrid;
            e.CanExecute = true;
            return;

            if (grid != null)
            {
                if (grid.Items.Count == 0)
                {
                    grid.Items.Clear();
                    e.CanExecute = false;
                }
                else if (grid.SelectedItems.Count <= 0)
                {
                    e.CanExecute = false;
                }
                else
                {
                    object obj = grid.SelectedItems[0];
                    if ( !(obj is ConfigEntity) ) {
                        e.CanExecute = false;
                    }
                    else
                        e.CanExecute = true;
                }
            }
            else
            {
                e.CanExecute = false;
            }
        }

        private void GenericPusher(ConfigEntity entity, Level levelA, Level levelB)
        {
            ConfigEntity newEntity = null;
            bool UserWantsNewEntity = false;
            //Level.PushStatus status = levelB.pushStatus(entity);
            //if (status == Level.PushStatus.PUSH_INVALID)
            //{
            //    MessageBox.Show(string.Format("Entity {0} not pushable.", entity.entity_guid));
            //    return;
            //}

            ////if (status == Level.PushStatus.PUSH_CREATE_ITEM)
            ////{
                //If the entity is new, must clone it here and then push
                switch (PushEntityType)
                {
                    case PushLevelEntityType.Molecule:
                        ConfigMolecule newmol = ((ConfigMolecule)entity).Clone(null);
                        Level.PushStatus status = levelB.pushStatus(newmol);
                        if (status == Level.PushStatus.PUSH_INVALID)
                        {
                            MessageBox.Show(string.Format("Entity {0} not pushable.", entity.entity_guid));
                            return;
                        }
                        levelB.repositoryPush(newmol, status);
                        break;
                    case PushLevelEntityType.Gene:
                        ConfigGene newgene = ((ConfigGene)entity).Clone(null);
                        status = levelB.pushStatus(newgene);
                        if (status == Level.PushStatus.PUSH_INVALID)
                        {
                            MessageBox.Show(string.Format("Entity {0} not pushable.", entity.entity_guid));
                            return;
                        }
                        levelB.repositoryPush(newgene, status);
                        break;
                    case PushLevelEntityType.Reaction:
                        ConfigReaction newreac = ((ConfigReaction)entity).Clone(true);
                        status = levelB.pushStatus(newreac);
                        if (status == Level.PushStatus.PUSH_INVALID)
                        {
                            MessageBox.Show(string.Format("Entity {0} not pushable.", entity.entity_guid));
                            return;
                        }
                        levelB.repositoryPush(newreac, status);
                        break;
                    case PushLevelEntityType.Cell:
                        ConfigCell newcell = ((ConfigCell)entity).Clone(true);
                        status = levelB.pushStatus(newcell);
                        if (status == Level.PushStatus.PUSH_INVALID)
                        {
                            MessageBox.Show(string.Format("Entity {0} not pushable.", entity.entity_guid));
                            return;
                        }
                        levelB.repositoryPush(newcell, status);
                        break;
                    default:
                        break;
                }
            ////}


            //else // the item exists; could be newer or older
            //{
            //    if (UserWantsNewEntity == false)
            //    {
            //        levelB.repositoryPush(entity, status); // push into B - overwrites existing entity's properties
            //    }
            //    else //push as new
            //    {
            //        //levelB.repositoryPush(newEntity, Level.PushStatus.PUSH_CREATE_ITEM);  //create new entity in repository
            //    }
            //}
        }

        private void PushButtonArrow_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            if ((string)btn.Tag == "Right")
            {
                btn.Tag = "Left";
                ActualButtonImage.Source = LeftImage.Source;
            }
            else
            {
                btn.Tag = "Right";
                ActualButtonImage.Source = RightImage.Source;
            }
            
            ResetGrids();
        }

    }  //End of PushBetweenLevels class


    public class DataGridBehavior
    {
        #region Get Visuals

        private static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

        public static List<T> GetVisualChildCollection<T>(object parent) where T : Visual
        {
            List<T> visualCollection = new List<T>();
            GetVisualChildCollection(parent as DependencyObject, visualCollection);
            return visualCollection;
        }

        private static void GetVisualChildCollection<T>(DependencyObject parent, List<T> visualCollection) where T : Visual
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T)
                {
                    visualCollection.Add(child as T);
                }
                if (child != null)
                {
                    GetVisualChildCollection(child, visualCollection);
                }
            }
        }

        #endregion // Get Visuals
    }

    public static class MyCommands
    {
        public static readonly RoutedCommand PushCommand = new RoutedCommand("PushCommand", typeof(MyCommands));
    }

    public class PushLevelEntityTemplateSelector : DataTemplateSelector
    {
        public DataTemplate PushLevelMoleculeTemplate { get; set; }
        public DataTemplate PushLevelGeneTemplate { get; set; }
        public DataTemplate PushLevelReactionTemplate { get; set; }
        public DataTemplate PushLevelCellTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item != null) {

                if (item is ObservableCollection<ConfigMolecule>)
                    return PushLevelMoleculeTemplate;
                else if (item is ObservableCollection<ConfigGene>)
                    return PushLevelGeneTemplate;
                else if (item is ObservableCollection<ConfigReaction>)
                    return PushLevelReactionTemplate;
                else if (item is ObservableCollection<ConfigCell>)
                    return PushLevelCellTemplate;
            }

            return PushLevelMoleculeTemplate;
        }
    }
}
