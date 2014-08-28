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

namespace DaphneGui.Pushing
{
    /// <summary>
    /// Interaction logic for PushReaction.xaml
    /// </summary>
    public partial class PushReaction : Window
    {
        public PushReaction()
        {
            InitializeComponent();
        }

        private void ReactionCancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void ReactionSaveButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
