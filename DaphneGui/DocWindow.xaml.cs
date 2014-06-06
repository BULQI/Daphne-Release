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
    /// Interaction logic for DocWindow.xaml
    /// </summary>
    public partial class DocWindow : Window
    {
        public bool HasBeenClosed;
        public DocWindow()
        {
            InitializeComponent();
            HasBeenClosed = false;
        }

        private void CCIButton_Click(object sender, RoutedEventArgs e)
        {
            webBrowser.Navigate(new Uri("http://computationalimmunology.org/"));
            Show();
        }

        private void backButton_Click(object sender, RoutedEventArgs e)
        {
            if (webBrowser.CanGoBack)
            {
                webBrowser.GoBack();
            }
        }

        private void forthButton_Click(object sender, RoutedEventArgs e)
        {
            if (webBrowser.CanGoForward)
            {
                webBrowser.GoForward();
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            HasBeenClosed = true;
        }




    }
}
