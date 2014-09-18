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
using System.Collections.ObjectModel;

namespace DaphneGui.Pushing
{
    /// <summary>
    /// Interaction logic for PushByLevel.xaml
    /// </summary>
    public partial class PushByLevel : Window
    {
        public enum PushLevelEntityType { Molecule = 0, Gene, Reaction, Cell };

        public PushLevelEntityType PushEntityType { get; set; }
        public PushLevel PushLevelA { get; set; }
        public PushLevel PushLevelB { get; set; }
        public Level LevelA { get; set; }
        public Level LevelB { get; set; }

        public PushByLevel(PushLevelEntityType type)
        {
            PushEntityType = type;
            PushLevelA = PushLevel.Component;
            PushLevelB = PushLevel.UserStore;
            InitializeComponent();
            //DataContext = this;
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

        private void PushButton_Click(object sender, RoutedEventArgs e)
        {
            if (LevelAComboBox.SelectedIndex == LevelBComboBox.SelectedIndex)
            {
                MessageBox.Show("The destination level must be different from the source level.");
            }
        }

        //Sets the Tag property for the content control
        private void SetContentTag(ContentControl cc, Level level)
        {
            switch (PushEntityType)
            {
                case PushLevelEntityType.Molecule:
                    cc.Tag = level.entity_repository.molecules;
                    //cc.Tag = FindResource("pushMoleculesListView");
                    break;
                case PushLevelEntityType.Gene:
                    cc.Tag = level.entity_repository.genes;
                    break;
                case PushLevelEntityType.Reaction:
                    cc.Tag = level.entity_repository.reactions;
                    break;
                case PushLevelEntityType.Cell:
                    cc.Tag = level.entity_repository.cells;
                    break;
                default:
                    break;
            }
        }


        //This combo box allows user to select a level to push to
        private void LevelAComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            PushLevel level = (PushLevel)combo.SelectedItem;

            PushLevelA = level;

            if (LevelAContent == null)
                return;

            LevelAContent.DataContext = this; // PushEntityType;

            switch (level)
            {
                case PushLevel.Component:
                    SetContentTag(LevelAContent, MainWindow.SOP.Protocol);
                    LevelA = MainWindow.SOP.Protocol;
                    break;
                case PushLevel.DaphneStore:
                    SetContentTag(LevelAContent, MainWindow.SOP.DaphneStore);
                    LevelA = MainWindow.SOP.DaphneStore;
                    break;
                case PushLevel.UserStore:
                    SetContentTag(LevelAContent, MainWindow.SOP.UserStore);
                    LevelA = MainWindow.SOP.UserStore;
                    break;
            }
        }

        private void LevelBComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            PushLevel level = (PushLevel)combo.SelectedItem;

            PushLevelB = level;

            if (LevelBContent == null)
                return;

            LevelBContent.DataContext = this;  // PushEntityType;

            switch (level)
            {
                case PushLevel.Component:
                    SetContentTag(LevelBContent, MainWindow.SOP.Protocol);
                    LevelB = MainWindow.SOP.Protocol;
                    break;
                case PushLevel.DaphneStore:
                    SetContentTag(LevelBContent, MainWindow.SOP.DaphneStore);
                    LevelB = MainWindow.SOP.DaphneStore;
                    break;
                case PushLevel.UserStore:
                    SetContentTag(LevelBContent, MainWindow.SOP.UserStore);
                    LevelB = MainWindow.SOP.UserStore;
                    break;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (LevelAContent == null)
                return;

            LevelAContent.DataContext = this;  // PushEntityType;

            switch (PushLevelA)
            {
                case PushLevel.Component:
                    SetContentTag(LevelAContent, MainWindow.SOP.Protocol);
                    LevelA = MainWindow.SOP.Protocol;
                    break;
                case PushLevel.DaphneStore:
                    SetContentTag(LevelAContent, MainWindow.SOP.DaphneStore);
                    LevelA = MainWindow.SOP.DaphneStore;
                    break;
                case PushLevel.UserStore:
                    SetContentTag(LevelAContent, MainWindow.SOP.UserStore);
                    LevelA = MainWindow.SOP.UserStore;
                    break;
            }
            LevelAContent.DataContext = LevelA;


            if (LevelBContent == null)
                return;
            
            switch (PushLevelB)
            {
                case PushLevel.Component:
                    SetContentTag(LevelBContent, MainWindow.SOP.Protocol);
                    LevelB = MainWindow.SOP.Protocol;
                    break;
                case PushLevel.DaphneStore:
                    SetContentTag(LevelBContent, MainWindow.SOP.DaphneStore);
                    LevelB = MainWindow.SOP.DaphneStore;
                    break;
                case PushLevel.UserStore:
                    SetContentTag(LevelBContent, MainWindow.SOP.UserStore);
                    LevelB = MainWindow.SOP.UserStore;
                    break;
            }
            LevelBContent.DataContext = LevelB;
        }

        private void pushMoleculesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigMolecule source_mol = e.Item as ConfigMolecule;
            ConfigMolecule dest_mol = e.Item as ConfigMolecule;

            if (source_mol == null)
                return;

            Dictionary<string, ConfigMolecule> dest_collection_dict = null;
            ObservableCollection<ConfigMolecule> dest_collection = null;
            PushLevel level = (PushLevel)LevelBComboBox.SelectedItem;

            if (level == PushLevel.DaphneStore)
            {
                dest_collection_dict = MainWindow.SOP.DaphneStore.entity_repository.molecules_dict;
                dest_collection = MainWindow.SOP.DaphneStore.entity_repository.molecules;
            }
            else if (level == PushLevel.Component)
            {
                dest_collection_dict = MainWindow.SOP.Protocol.entity_repository.molecules_dict;
                dest_collection = MainWindow.SOP.Protocol.entity_repository.molecules;
            }
            else if (level == PushLevel.UserStore)
            {
                dest_collection_dict = MainWindow.SOP.UserStore.entity_repository.molecules_dict;
                dest_collection = MainWindow.SOP.UserStore.entity_repository.molecules;
            }

            if (dest_collection_dict == null || dest_collection == null)
                return;

            if (dest_collection_dict.Count == 0)
            {
                dest_mol = dest_collection.First(m => m.entity_guid == source_mol.entity_guid);
            }
            else if (!dest_collection_dict.ContainsKey(source_mol.entity_guid))
            {
                e.Accepted = true;
                return;
            }
            else
            {
                dest_mol = dest_collection_dict[source_mol.entity_guid];
            }

            if (dest_mol == null)
            {
                e.Accepted = true;
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
        private void pushGenesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigGene source_entity = e.Item as ConfigGene;
            ConfigGene dest_entity = e.Item as ConfigGene;

            if (source_entity == null)
                return;

            Dictionary<string, ConfigGene> dest_collection_dict = null;
            ObservableCollection<ConfigGene> dest_collection = null;
            PushLevel level = (PushLevel)LevelBComboBox.SelectedItem;

            if (level == PushLevel.DaphneStore)
            {
                dest_collection_dict = MainWindow.SOP.DaphneStore.entity_repository.genes_dict;
                dest_collection = MainWindow.SOP.DaphneStore.entity_repository.genes;
            }
            else if (level == PushLevel.Component)
            {
                dest_collection_dict = MainWindow.SOP.Protocol.entity_repository.genes_dict;
                dest_collection = MainWindow.SOP.Protocol.entity_repository.genes;
            }
            else if (level == PushLevel.UserStore)
            {
                dest_collection_dict = MainWindow.SOP.UserStore.entity_repository.genes_dict;
                dest_collection = MainWindow.SOP.UserStore.entity_repository.genes;
            }

            if (dest_collection_dict == null || dest_collection == null)
                return;

            if (dest_collection_dict.Count == 0)
            {
                dest_entity = dest_collection.First(m => m.entity_guid == source_entity.entity_guid);
            }
            else if (!dest_collection_dict.ContainsKey(source_entity.entity_guid))
            {
                e.Accepted = true;
                return;
            }
            else
            {
                dest_entity = dest_collection_dict[source_entity.entity_guid];
            }

            if (dest_entity == null)
            {
                e.Accepted = true;
            }
            else if (source_entity.change_stamp != dest_entity.change_stamp)
            {
                e.Accepted = true;
            }
            else
            {
                e.Accepted = false;
            }
        }
        private void pushReactionsListView_Filter(object sender, FilterEventArgs e)
        {
            e.Accepted = true;
        }
        private void pushCellsListView_Filter(object sender, FilterEventArgs e)
        {
            e.Accepted = true;
        }
    }

    public class PushLevelTemplateSelector : DataTemplateSelector
    {
        public DataTemplate PushLevelMoleculeTemplate { get; set; }
        public DataTemplate PushLevelGeneTemplate { get; set; }
        public DataTemplate PushLevelReactionTemplate { get; set; }
        public DataTemplate PushLevelCellTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            //FrameworkElement element = container as FrameworkElement;

            if (item != null && item is PushByLevel.PushLevelEntityType)
            {
                PushByLevel.PushLevelEntityType type = (PushByLevel.PushLevelEntityType)item;

                switch (type)
                {
                    case PushByLevel.PushLevelEntityType.Molecule:
                        return PushLevelMoleculeTemplate;
                    case PushByLevel.PushLevelEntityType.Gene:
                        return PushLevelGeneTemplate;
                    case PushByLevel.PushLevelEntityType.Reaction:
                        return PushLevelReactionTemplate;
                    case PushByLevel.PushLevelEntityType.Cell:
                        return PushLevelCellTemplate;
                }
            }
           
            return PushLevelMoleculeTemplate;  
        }
    }
}
