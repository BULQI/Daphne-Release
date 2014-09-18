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
        public PushEntity()
        {
            InitializeComponent();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }

    public class EntityTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ReactionTemplate { get; set; }
        public DataTemplate MoleculeTemplate { get; set; }
        public DataTemplate GeneTemplate     { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item == null)
                return null;

            ConfigEntity curr_item = (ConfigEntity)item;

            if (item is ConfigMolecule)
                return MoleculeTemplate;
            else if (item is ConfigGene)
                return GeneTemplate;

            return ReactionTemplate;
        }
    }
}
