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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace DaphneUserControlLib
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class ParamDistrControl : UserControl
    {
        public ParamDistrControl()
        {
            InitializeComponent();
        }

        private void cbParamDistr_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Only want to respond to purposeful user interaction, not just population and depopulation
            // of parameter distribution type
            if (e.AddedItems.Count == 0 || e.RemovedItems.Count == 0)
            {
                return;
            }

            //DistributedParameter distrParam = (DistributedParameter)ParamDistrType;









        }
    }
}
