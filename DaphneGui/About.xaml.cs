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
using System.Reflection;
using System.IO;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    public partial class About : Window
    {
        public About()
        {
            InitializeComponent();

            Assembly ass = Assembly.GetExecutingAssembly();
            string exename = ass.CodeBase;
            exename = new Uri(exename).LocalPath;
            txtVersion.Text = "Version: " + File.GetCreationTime(exename).ToLongDateString() + ", " + File.GetCreationTime(exename).ToLongTimeString();
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
