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
    /// Interaction logic for PushEntity.xaml
    /// </summary>
    public partial class PushEntity : Window
    {
        public enum PushLevelEntityType { Molecule = 0, Gene, Reaction, Cell };

        public bool UserWantsNewEntity { get; set; }
        public bool IsReaction { get; set; }    //Need to disable 'save as new' button if Reaction

        public PushEntity()
        {
            UserWantsNewEntity = false;
            IsReaction = false;
            
            InitializeComponent();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }


        private void btnSave_Click_1(object sender, RoutedEventArgs e)
        {
            UserWantsNewEntity = true;
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ConfigEntity left = (ConfigEntity)EntityLevelDetails.DataContext;
            ConfigEntity right = (ConfigEntity)ComponentLevelDetails.DataContext;

            if (left is ConfigReaction)
            {
                IsReaction = true;
                btnSaveAsNew.IsEnabled = false;
            }
        }
    }

    public class EntityTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ReactionTemplate    { get; set; }
        public DataTemplate MoleculeTemplate    { get; set; }
        public DataTemplate GeneTemplate        { get; set; }
        public DataTemplate CellTemplate        { get; set; }
        public DataTemplate DiffSchemeTemplate  { get; set; }
        public DataTemplate RCTemplate          { get; set; }
        public DataTemplate TransDrivTemplate   { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
                return null;

            ConfigEntity curr_item = (ConfigEntity)item;

            if (item is ConfigMolecule)
                return MoleculeTemplate;
            else if (item is ConfigGene)
                return GeneTemplate;
            else if (item is ConfigCell)
                return CellTemplate;
            else if (item is ConfigTransitionScheme)
                return DiffSchemeTemplate;
            else if (item is ConfigReactionComplex)
                return RCTemplate;
            else if (item is ConfigTransitionDriver)
                return TransDrivTemplate;

            return ReactionTemplate;
        }
    }
}
