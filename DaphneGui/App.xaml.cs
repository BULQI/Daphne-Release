using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        void App_Deactivated(object sender, EventArgs e)
        {
            // Application deactivated - close open items that don't automatically
            MainWindow mw = (DaphneGui.MainWindow)this.MainWindow;
            mw.CellOptionsExpander.IsExpanded = false;
            mw.ECMOptionsExpander.IsExpanded = false;
        }
    }
}
