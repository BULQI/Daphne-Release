﻿using System;
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

namespace DaphneGui.CellPopDynamics
{
    /// <summary>
    /// Interaction logic for PlotsWindow.xaml
    /// </summary>
    public partial class PlotOptionsWindow : Window
    {
        public PlotOptionsWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var scenario = DataContext;
        }
    }
}
