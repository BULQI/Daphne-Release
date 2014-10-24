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

using System.Globalization;

using Daphne;
using DaphneUserControlLib;

using System.ComponentModel;


namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for ReportControl.xaml
    /// </summary>
    public partial class ReportControl : UserControl
    {
        public ReportControl()
        {
            InitializeComponent();
        }

        //protected void TabItem_Loaded(object sender, RoutedEventArgs e)
        //{
        //    lbRptCellPops.SelectedIndex = 0;
        //    // Uncomment these and fix?
        //    //ICollectionView icv = CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource);
        //    //if (icv != null)
        //    //{
        //    //    icv.Refresh();
        //    //}
        //}

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {

        }

    }
}
