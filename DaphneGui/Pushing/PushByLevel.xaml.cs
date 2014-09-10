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

namespace DaphneGui.Pushing
{
    /// <summary>
    /// Interaction logic for PushByLevel.xaml
    /// </summary>
    public partial class PushByLevel : Window
    {
        public enum PushLevelEntityType { Molecule = 0, Gene, Reaction, Cell };
        public PushLevelEntityType PushEntityType { get; set; }
        public Level LevelA { get; set; }
        public Level LevelB { get; set; }

        public PushByLevel(PushLevelEntityType type)
        {
            PushEntityType = type;
            InitializeComponent();
            DataContext = this;
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
            PushType type = (PushType)combo.SelectedItem;

            if (LevelAContent == null)
                return;

            LevelAContent.DataContext = PushEntityType;

            switch (type) {
                case PushType.Component:
                    SetContentTag(LevelAContent, MainWindow.SOP.Protocol);
                    break;
                case PushType.DaphneStore:
                    SetContentTag(LevelAContent, MainWindow.SOP.DaphneStore);
                    break;
                case PushType.UserStore:
                    SetContentTag(LevelAContent, MainWindow.SOP.UserStore);                    
                    break;
            }
        }

        private void LevelBComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox combo = sender as ComboBox;
            PushType type = (PushType)combo.SelectedItem;

            if (LevelBContent == null)
                return;

            LevelBContent.DataContext = PushEntityType;

            switch (type)
            {
                case PushType.Component:
                    SetContentTag(LevelBContent, MainWindow.SOP.Protocol);
                    break;
                case PushType.DaphneStore:
                    SetContentTag(LevelBContent, MainWindow.SOP.DaphneStore);
                    break;
                case PushType.UserStore:
                    SetContentTag(LevelBContent, MainWindow.SOP.UserStore);                    
                    break;
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LevelAComboBox.SelectedIndex = 0;
            LevelBComboBox.SelectedIndex = 1;
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
