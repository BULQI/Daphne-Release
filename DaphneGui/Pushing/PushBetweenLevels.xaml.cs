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
            PushLevelA = PushLevel.Component;
            PushLevelB = PushLevel.UserStore;
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
            switch (PushLevelA)
            {
                case PushLevel.Component:
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
                case PushLevel.Component:
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

            
            RightGroup.DataContext = LevelB.entity_repository;
            RightContent.DataContext = RightList;   //must do this first
            LeftGroup.DataContext = LevelA.entity_repository;
            LeftContent.DataContext = FilteredList(LeftList);

            //string[] TestItems = Enum.GetNames(typeof(PushLevel)));
            //var list = from item in TestItems where item != (string)(PushLevel.DaphneStore) select item;
            //LevelBComboBox.ItemsSource = list; 

        }

        private object FilteredList(object LeftList)
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
                    break;
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

        private void PushButton_Click(object sender, RoutedEventArgs e)
        {
            if (PushLevelA == PushLevelB)
            {
                MessageBox.Show("The destination level must be different from the source level.");
                return;
            }
            else if (((ObservableCollection<object>)(LeftList)).Count == 0) 
            {
                MessageBox.Show("There is nothing to push.");
                return;
            }

            switch (PushEntityType)
            {
                case PushLevelEntityType.Molecule:
                    ObservableCollection<ConfigMolecule> mols = (ObservableCollection<ConfigMolecule>)(LeftContent.DataContext);
                    break;
                case PushLevelEntityType.Gene:
                    ObservableCollection<ConfigGene> genes = (ObservableCollection<ConfigGene>)(LeftContent.DataContext);
                    break;
                default:
                    break;
            }


        }

        private void LevelAComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            PushLevel level = (PushLevel)combo.SelectedItem;

            PushLevelA = level;

            switch (PushLevelA)
            {
                case PushLevel.Component:
                    LevelA = MainWindow.SOP.Protocol;
                    break;
                case PushLevel.DaphneStore:
                    LevelA = MainWindow.SOP.DaphneStore;
                    break;
                case PushLevel.UserStore:
                    LevelA = MainWindow.SOP.UserStore;
                    break;
            }

            switch (PushEntityType)
            {
                case PushLevelEntityType.Molecule:
                    LeftList = LevelA.entity_repository.molecules;
                    break;
                case PushLevelEntityType.Gene:
                    LeftList = LevelA.entity_repository.genes;
                    break;
                case PushLevelEntityType.Reaction:
                    LeftList = LevelA.entity_repository.reactions;
                    break;
                case PushLevelEntityType.Cell:
                    LeftList = LevelA.entity_repository.cells;
                    break;
                default:
                    break;
            }

            if (LeftGroup == null)
                return;            

            LeftGroup.DataContext = LevelA.entity_repository;

            if (LeftContent == null)
                return;

            if (RightList == null)
            {
                LeftContent.DataContext = LeftList;
            }
            else
            {
                LeftContent.DataContext = FilteredList(LeftList);
            }

        }

        private void LevelBComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            PushLevel level = (PushLevel)combo.SelectedItem;

            PushLevelB = level;

            switch (PushLevelB)
            {
                case PushLevel.Component:
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
                    RightList = LevelB.entity_repository.molecules;
                    break;
                case PushLevelEntityType.Gene:
                    RightList = LevelB.entity_repository.genes;
                    break;
                case PushLevelEntityType.Reaction:
                    RightList = LevelB.entity_repository.reactions;
                    break;
                case PushLevelEntityType.Cell:
                    RightList = LevelB.entity_repository.cells;
                    break;
                default:
                    break;
            }

            if (RightGroup == null)
                return;

            RightGroup.DataContext = LevelB.entity_repository;

            if (RightContent == null)
                return;

            RightContent.DataContext = RightList;  
        }

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
