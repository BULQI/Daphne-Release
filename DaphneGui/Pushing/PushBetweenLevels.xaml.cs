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
using System.Globalization;
using System.Data;
using System.Collections;

namespace DaphneGui.Pushing
{
    /// <summary>
    /// Interaction logic for PushBetweenLevels.xaml
    /// </summary>
    public partial class PushBetweenLevels : Window
    {
        public enum PushLevelEntityType { Molecule = 0, Gene, Reaction, Cell, DiffScheme, ReactionTemplate, ReactionComplex, TransDriver };

        public PushLevelEntityType PushEntityType { get; set; }
        public PushLevel PushLevelA { get; set; }
        public PushLevel PushLevelB { get; set; }
        public Level LevelA { get; set; }
        public Level LevelB { get; set; }

        public object LeftList { get; set; }
        public object RightList { get; set; }
        public List<string> equalGuids { get; set; }

        public double GridHeight { get; set; }
        public Level CurrentLevel { get; set; }

        public PushBetweenLevels(PushLevelEntityType type, Level currLevel)
        {
            this.Owner = Application.Current.MainWindow;
            PushEntityType = type;
            PushLevelA = PushLevel.UserStore;
            PushLevelB = PushLevel.Protocol;
            LeftList = null;
            RightList = null;
            equalGuids = new List<string>();
            InitializeComponent();

            
            CurrentLevel = currLevel;
            MaxHeight = SystemParameters.PrimaryScreenHeight * 0.9;
            GridHeight = MaxHeight * 0.85;

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
            else if (type == PushLevelEntityType.ReactionComplex)
            {
                this.Title = "Save Reaction Complexes Between Levels";
            }
            else if (type == PushLevelEntityType.ReactionTemplate)
            {
                this.Title = "Save Reaction Templates Between Levels";
            }
            else if (type == PushLevelEntityType.TransDriver)
            {
                this.Title = "Save Transition Drivers Between Levels";
            }
            else if (type == PushLevelEntityType.DiffScheme)
            {
                this.Title = "Save Differentiation Schemes Between Levels";
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {            
            //MaxHeight = SystemParameters.PrimaryScreenHeight * 0.9;
            var desktopWorkingArea = System.Windows.Forms.Screen.PrimaryScreen.WorkingArea;
            this.Left = desktopWorkingArea.Left + 20;
            this.Top = desktopWorkingArea.Top + 20;
            ResetGrids();
            ActualButtonImage.Source = RightImage.Source;
        }

        public static List<T> GetLogicalChildCollection<T>(object parent) where T : DependencyObject
        {
            List<T> logicalCollection = new List<T>();
            GetLogicalChildCollection(parent as DependencyObject, logicalCollection);
            return logicalCollection;
        }

        private static void GetLogicalChildCollection<T>(DependencyObject parent, List<T> logicalCollection) where T : DependencyObject
        {
            IEnumerable children = LogicalTreeHelper.GetChildren(parent);
            foreach (object child in children)
            {
                if (child is DependencyObject)
                {
                    DependencyObject depChild = child as DependencyObject;
                    if (child is T)
                    {
                        logicalCollection.Add(child as T);
                    }
                    GetLogicalChildCollection(depChild, logicalCollection);
                }
            }
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
                    MainWindow.ToolWin.PushReactionFilter(LeftList, LevelA);
                    RightList = LevelB.entity_repository.reactions;
                    MainWindow.ToolWin.PushReactionFilter(RightList, LevelB);
                    break;
                case PushLevelEntityType.Cell:
                    LeftList = LevelA.entity_repository.cells;
                    RightList = LevelB.entity_repository.cells;
                    break;
                case PushLevelEntityType.DiffScheme:
                    LeftList = LevelA.entity_repository.diff_schemes;
                    RightList = LevelB.entity_repository.diff_schemes;
                    break;
                case PushLevelEntityType.ReactionTemplate:
                    LeftList = LevelA.entity_repository.reaction_templates;
                    RightList = LevelB.entity_repository.reaction_templates;
                    break;
                case PushLevelEntityType.ReactionComplex:
                    LeftList = LevelA.entity_repository.reaction_complexes;
                    MainWindow.ToolWin.PushReactionComplexFilter(LeftList, LevelA);
                    RightList = LevelB.entity_repository.reaction_complexes;
                    MainWindow.ToolWin.PushReactionComplexFilter(RightList, LevelB);
                    break;
                case PushLevelEntityType.TransDriver:
                    LeftList = LevelA.entity_repository.transition_drivers;
                    RightList = LevelB.entity_repository.transition_drivers;
                    break;
                default:
                    break;
            }

            //If window not yet loaded, controls are null so return;
            if (RightGroup == null || RightContent == null || LeftGroup == null || LeftContent == null)
            {
                return;
            }

            GetEqualEntities();

            RightGroup.DataContext = LevelB.entity_repository;
            RightContent.DataContext = RightList;   //must do this first (??)
            LeftGroup.DataContext = LevelA.entity_repository;
            //LeftContent.DataContext = LeftList;  
            
            LeftContent.DataContext = null;
            LeftContent.DataContext = LeftList;
        }

        /// <summary>
        /// This method populates a list of guids of those entities that are in both lists and are equal.
        /// This is used later when rendering the grid rows and any equal entities are shown gray.
        /// </summary>
        private void GetEqualEntities()
        {
            equalGuids.Clear();
            switch (PushEntityType)
            {
                case PushLevelEntityType.Molecule:
                    ObservableCollection<ConfigMolecule> molleft = (ObservableCollection<ConfigMolecule>)LeftList;
                    ObservableCollection<ConfigMolecule> molright = (ObservableCollection<ConfigMolecule>)RightList;

                    foreach (ConfigMolecule mol in molleft)
                    {
                        ConfigMolecule mol2 = FindMolInList(molright, mol);
                        if (mol2 != null)
                        {
                            if (mol.Equals(mol2))
                            {
                                if (equalGuids.Contains(mol.entity_guid) == false)
                                    equalGuids.Add(mol.entity_guid);
                            }
                        }
                    }
                    foreach (ConfigMolecule mol in molright)
                    {
                        ConfigMolecule mol2 = FindMolInList(molleft, mol);
                        if (mol2 != null)
                        {
                            if (mol.Equals(mol2))
                            {
                                if (equalGuids.Contains(mol.entity_guid) == false)
                                    equalGuids.Add(mol.entity_guid);
                            }
                        }
                    }
                    break;
                case PushLevelEntityType.Gene:
                    ObservableCollection<ConfigGene> geneleft = (ObservableCollection<ConfigGene>)LeftList;
                    ObservableCollection<ConfigGene> generight = (ObservableCollection<ConfigGene>)RightList;

                    foreach (ConfigGene gene in geneleft)
                    {
                        ConfigGene gene2 = FindGeneInList(generight, gene);
                        if (gene2 != null)
                        {
                            if (gene.Equals(gene2))
                            {
                                if (equalGuids.Contains(gene.entity_guid) == false)
                                    equalGuids.Add(gene.entity_guid);
                            }
                        }
                    }
                    foreach (ConfigGene gene in generight)
                    {
                        ConfigGene gene2 = FindGeneInList(geneleft, gene);
                        if (gene2 != null)
                        {
                            if (gene.Equals(gene2))
                            {
                                if (equalGuids.Contains(gene.entity_guid) == false)
                                    equalGuids.Add(gene.entity_guid);
                            }
                        }
                    }
                    break;
                case PushLevelEntityType.Reaction:
                    ObservableCollection<ConfigReaction> reacleft = (ObservableCollection<ConfigReaction>)LeftList;
                    ObservableCollection<ConfigReaction> reacright = (ObservableCollection<ConfigReaction>)RightList;

                    foreach (ConfigReaction reac in reacleft)
                    {
                        ConfigReaction reac2 = FindReactionInList(reacright, reac);
                        if (reac2 != null)
                        {
                            if (reac.Equals(reac2))
                            {
                                //equalGuids.Add(reac.entity_guid);
                            }
                        }
                    }
                    foreach (ConfigReaction reac in reacright)
                    {
                        ConfigReaction reac2 = FindReactionInList(reacleft, reac);
                        if (reac2 != null)
                        {
                            if (reac.Equals(reac2))
                            {
                                equalGuids.Add(reac.entity_guid);
                            }
                        }
                    }
                    break;
                case PushLevelEntityType.Cell:
                    ObservableCollection<ConfigCell> cellleft = (ObservableCollection<ConfigCell>)LeftList;
                    ObservableCollection<ConfigCell> cellright = (ObservableCollection<ConfigCell>)RightList;

                    foreach (ConfigCell cell in cellleft)
                    {
                        ConfigCell cell2 = FindCellInList(cellright, cell);
                        if (cell2 != null)
                        {
                            if (cell.Equals(cell2))
                            {
                                equalGuids.Add(cell.entity_guid);
                            }
                        }
                    }
                    foreach (ConfigCell cell in cellright)
                    {
                        ConfigCell cell2 = FindCellInList(cellleft, cell);
                        if (cell2 != null)
                        {
                            if (cell.Equals(cell2))
                            {
                                equalGuids.Add(cell.entity_guid);
                            }
                        }
                    }
                    break;
                case PushLevelEntityType.ReactionComplex:
                    ObservableCollection<ConfigReactionComplex> rcleft = (ObservableCollection<ConfigReactionComplex>)LeftList;
                    ObservableCollection<ConfigReactionComplex> rcright = (ObservableCollection<ConfigReactionComplex>)RightList;
                    foreach (ConfigReactionComplex rc in rcleft)
                    {
                        ConfigReactionComplex rc2 = FindReacCompInList(rcright, rc);
                        if (rc2 != null)
                        {
                            if (rc.Equals(rc2))
                            {
                                equalGuids.Add(rc.entity_guid);
                            }
                        }
                    }
                    foreach (ConfigReactionComplex rc in rcright)
                    {
                        ConfigReactionComplex rc2 = FindReacCompInList(rcleft, rc);
                        if (rc2 != null)
                        {
                            if (rc.Equals(rc2))
                            {
                                equalGuids.Add(rc.entity_guid);
                            }
                        }
                    }
                    break;
                case PushLevelEntityType.ReactionTemplate:
                    ObservableCollection<ConfigReactionTemplate> rtleft = (ObservableCollection<ConfigReactionTemplate>)LeftList;
                    ObservableCollection<ConfigReactionTemplate> rtright = (ObservableCollection<ConfigReactionTemplate>)RightList;
                    foreach (ConfigReactionTemplate rt in rtleft)
                    {
                        ConfigReactionTemplate rt2 = FindReacTempInList(rtright, rt);
                        if (rt2 != null)
                        {
                            if (rt.Equals(rt2))
                            {
                                equalGuids.Add(rt.entity_guid);
                            }
                        }
                    }
                    foreach (ConfigReactionTemplate rt in rtright)
                    {
                        ConfigReactionTemplate rt2 = FindReacTempInList(rtleft, rt);
                        if (rt2 != null)
                        {
                            if (rt.Equals(rt2))
                            {
                                equalGuids.Add(rt.entity_guid);
                            }
                        }
                    }
                    break;
                case PushLevelEntityType.TransDriver:
                    ObservableCollection<ConfigTransitionDriver> tdleft = (ObservableCollection<ConfigTransitionDriver>)LeftList;
                    ObservableCollection<ConfigTransitionDriver> tdright = (ObservableCollection<ConfigTransitionDriver>)RightList;
                    foreach (ConfigTransitionDriver td in tdleft)
                    {
                        ConfigTransitionDriver td2 = FindTransDriverInList(tdright, td);
                        if (td2 != null)
                        {
                            if (td.Equals(td2))
                            {
                                equalGuids.Add(td.entity_guid);
                            }
                        }
                    }
                    break;
                case PushLevelEntityType.DiffScheme:
                    ObservableCollection<ConfigTransitionScheme> dsleft = (ObservableCollection<ConfigTransitionScheme>)LeftList;
                    ObservableCollection<ConfigTransitionScheme> dsright = (ObservableCollection<ConfigTransitionScheme>)RightList;
                    foreach (ConfigTransitionScheme ds in dsleft)
                    {
                        ConfigTransitionScheme ds2 = FindDiffSchemeInList(dsright, ds);
                        if (ds2 != null)
                        {
                            if (ds.Equals(ds2))
                            {
                                equalGuids.Add(ds.entity_guid);
                            }
                        }
                    }
                    foreach (ConfigTransitionScheme ds in dsright)
                    {
                        ConfigTransitionScheme ds2 = FindDiffSchemeInList(dsleft, ds);
                        if (ds2 != null)
                        {
                            if (ds.Equals(ds2))
                            {
                                equalGuids.Add(ds.entity_guid);
                            }
                        }
                    }
                    break;
                default:
                    break;
                    
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
        private ConfigReactionComplex FindReacCompInList(ObservableCollection<ConfigReactionComplex> list, ConfigReactionComplex entity)
        {
            foreach (ConfigReactionComplex e in list)
            {
                if (entity.entity_guid == e.entity_guid)
                {
                    return e;
                }
            }
            return null;
        }
        private ConfigTransitionScheme FindDiffSchemeInList(ObservableCollection<ConfigTransitionScheme> list, ConfigTransitionScheme entity)
        {
            foreach (ConfigTransitionScheme e in list)
            {
                if (entity.entity_guid == e.entity_guid)
                {
                    return e;
                }
            }
            return null;
        }
        private ConfigTransitionDriver FindTransDriverInList(ObservableCollection<ConfigTransitionDriver> list, ConfigTransitionDriver entity)
        {
            foreach (ConfigTransitionDriver e in list)
            {
                if (entity.entity_guid == e.entity_guid)
                {
                    return e;
                }
            }
            return null;
        }
        private ConfigReactionTemplate FindReacTempInList(ObservableCollection<ConfigReactionTemplate> list, ConfigReactionTemplate entity)
        {
            foreach (ConfigReactionTemplate e in list)
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
            e.Accepted = true;
        }

        private void LevelAComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            PushLevel level = (PushLevel)combo.SelectedItem;

            PushLevelA = level;

            //if (PushLevelA == PushLevel.Protocol && (string)(PushButtonArrow.Tag) == "Right")
            if (PushLevelA == PushLevel.Protocol)
            {
                PushLevelB = PushLevel.UserStore;
                LevelBComboBox.SelectedIndex = 1;
            }
            //else if (PushLevelA == PushLevel.UserStore)
            //{
            //    PushLevelB = PushLevel.Protocol;
            //    LevelBComboBox.SelectedIndex = 0;
            //}

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

        
        /// <summary>
        /// can execute command handler for delete db - enables/disables the Push button
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void PushCommandCanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            
            DataGrid grid = sender as DataGrid;

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
                    if (!(obj is ConfigEntity))
                    {
                        e.CanExecute = false;
                    }
                    else
                        e.CanExecute = true;
                }
            }
            
        }

        public void PushCommandExecuted(object sender, ExecutedRoutedEventArgs e)
        {
            PushDialog(sender as DataGrid);
        }

        private void PushDialog(DataGrid grid)
        {
            if (grid == null)
                return;

            if (PushLevelA == PushLevelB)
            {
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
                //Push items
                foreach (ConfigEntity ent in grid.SelectedItems)
                {
                    if (equalGuids.Contains(ent.entity_guid) == false)
                    {
                        GenericPusher(ent, LevelA, LevelB);
                    }
                }

                //Disable pushed items
                for (int i = 0; i < grid.Items.Count; i++)
                {
                    DataGridRow row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(i);
                    if (row != null)
                    {
                        if (row.IsSelected)
                        {
                            Color col = Color.FromRgb(228, 228, 228);
                            row.Background = new SolidColorBrush(col);
                            row.IsEnabled = false;
                            row.IsSelected = false;
                        }
                    }
                }
                ResetGrids();
            }
            
        }

        private void GenericPusher(ConfigEntity entity, Level levelA, Level levelB)
        {
            //ConfigEntity newEntity = null;
            //bool UserWantsNewEntity = false;
            Level.PushStatus status = levelB.pushStatus(entity);
            if (status == Level.PushStatus.PUSH_INVALID)
            {
                MessageBox.Show(string.Format("Entity {0} not pushable.", entity.entity_guid));
                return;
            }

            //If the entity is new, must clone it here and then push
            switch (PushEntityType)
            {
                case PushLevelEntityType.Molecule:
                    ConfigMolecule newmol = ((ConfigMolecule)entity).Clone(null);                    
                    levelB.repositoryPush(newmol, status);
                    MainWindow.SOP.SelectedRenderSkin.AddRenderMol(newmol.renderLabel, newmol.Name);
                    break;
                case PushLevelEntityType.Gene:
                    ConfigGene newgene = ((ConfigGene)entity).Clone(null);
                    levelB.repositoryPush(newgene, status);
                    break;
                case PushLevelEntityType.Reaction:
                    ConfigReaction newreac = ((ConfigReaction)entity).Clone(true);
                    levelB.repositoryPush(newreac, status, levelA, true);
                    break;
                case PushLevelEntityType.Cell:
                    ConfigCell newcell = ((ConfigCell)entity).Clone(true);
                    levelB.repositoryPush(newcell, status, levelA, true);
                    MainWindow.SOP.SelectedRenderSkin.AddRenderCell(newcell.renderLabel, newcell.CellName);
                    break;
                case PushLevelEntityType.DiffScheme:
                    ConfigTransitionScheme newscheme = ((ConfigTransitionScheme)entity).Clone(true);
                    levelB.repositoryPush(newscheme, status, levelA, true);
                    break;
                case PushLevelEntityType.ReactionTemplate:
                    ConfigReactionTemplate newreactemp = ((ConfigReactionTemplate)entity).Clone(true);
                    levelB.repositoryPush(newreactemp, status, levelA, true);
                    break;
                case PushLevelEntityType.ReactionComplex:
                    ConfigReactionComplex newrc = ((ConfigReactionComplex)entity).Clone(true);
                    levelB.repositoryPush(newrc, status, levelA, true);
                    break;
                default:
                    break;
            }

        }

        private void grid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            ConfigEntity entity = e.Row.Item as ConfigEntity;
            e.Row.IsEnabled = true;

            if (equalGuids.Contains(entity.entity_guid))
            {
                // Access cell values values if needed like this...
                // var colValue = row["ColumnName1]";
                // var colValue2 = row["ColumName2]";

                // Set the background color of the DataGrid row based on whatever data you like from the row.
                Color col = Color.FromRgb(228, 228, 228);
                e.Row.Background = new SolidColorBrush(col);                    //Brushes.LightGray;
                e.Row.IsEnabled = false;
            }
            else
            {
                e.Row.Background = Brushes.White;
            }
        }

        /// <summary>
        /// On double click on an item in left grid, push it to right grid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void LeftGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            DataGrid grid = sender as DataGrid;
            ConfigEntity entity = (ConfigEntity)grid.CurrentItem;
            if (entity == null)
                return;

            PushDialog(grid);

        }

        private void datagrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ////This does not have any effect because of the command binding stuff
            //DataGrid grid = sender as DataGrid;
            
            //PushButtonArrow.IsEnabled = true;
            //if (grid.SelectedItems.Count <= 0)
            //    PushButtonArrow.IsEnabled = false;
        }

        private void LeftContent_Loaded(object sender, RoutedEventArgs e)
        {
            ContentControl cc = sender as ContentControl;
            DataTemplateSelector dts = cc.ContentTemplateSelector;

            //DataTemplate temp = PushLevelEntityTemplateSelector.PushLevelCellTemplate;
            //if (dts is PushLevel)
            //{
            //}
            

            List<UIElement> elements = new List<UIElement>();
            GetLogicalChildCollection(cc, elements);

            foreach (UIElement child in elements)
            {
                Type t = child.GetType();
                if (child.GetType() == typeof(CellDetailsControl))
                {
                    MessageBox.Show("Found celldetailscontrol");
                    //((CellDetailsControl)child).IsEnabled = false;
                }
            }
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
        public DataTemplate PushLevelDiffSchemeTemplate { get; set; }
        public DataTemplate PushLevelReacComplexTemplate { get; set; }
        public DataTemplate PushLevelTransDriverTemplate { get; set; }
        public DataTemplate PushLevelReacTemplateTemplate { get; set; }

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
                else if (item is ObservableCollection<ConfigTransitionScheme>)
                    return PushLevelDiffSchemeTemplate;
                else if (item is ObservableCollection<ConfigReactionComplex>)
                    return PushLevelReacComplexTemplate;
                else if (item is ObservableCollection<ConfigTransitionDriver>)
                    return PushLevelTransDriverTemplate;
                else if (item is ObservableCollection<ConfigReactionTemplate>)
                    return PushLevelReacTemplateTemplate;
            }

            return PushLevelMoleculeTemplate;
        }
    }

    /// <summary>
    /// Extension methods to the DependencyObject class.
    /// </summary>
    public static class ViewExtensions
    {
        public static T GetChildOfType<T>(this DependencyObject depObj)
        where T : DependencyObject
        {
            if (depObj == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depObj); i++)
            {
                var child = VisualTreeHelper.GetChild(depObj, i);

                var result = (child as T) ?? GetChildOfType<T>(child);
                if (result != null) return result;
            }
            return null;
        }

        /// <summary>
        /// Finds a Child of a given item in the visual tree. 
        /// </summary>
        /// <param name="parent">A direct parent of the queried item.</param>
        /// <typeparam name="T">The type of the queried item.</typeparam>
        /// <param name="childName">x:Name or Name of child. </param>
        /// <returns>The first parent item that matches the submitted type parameter. 
        /// If not matching item can be found, 
        /// a null parent is being returned.</returns>
        public static T FindChildByName<T>(this DependencyObject parent, string childName) 
        where T : DependencyObject
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
                foundChild = FindChildByName<T>(child, childName);

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

    }

    /// <summary>
    /// This converter returns true if a given push level is daphne store.
    /// See PushLevel enum.
    /// 
    /// </summary>
    public class PushLevelIsDaphneStoreConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (Enum.IsDefined(value.GetType(), value) == false)
                return DependencyProperty.UnsetValue;

            PushLevel enumValue = (PushLevel)value;

            if (enumValue == PushLevel.DaphneStore)
                return true;

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return PushLevel.Protocol;
        }
    }
}
