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

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for EditRCReactions.xaml
    /// </summary>
    public partial class EditRCReactions : Window
    {
        public EditRCReactions()
        {
            InitializeComponent();
        }

        public EditRCReactions(GuiReactionComplex rc)
        {
            InitializeComponent();
            lbRC.DataContext = rc;
            //dgRC.ItemsSource = rc.Reactions;
            tbRCName.Text = rc.Name;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
