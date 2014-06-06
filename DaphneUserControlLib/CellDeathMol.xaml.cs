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
using System.ComponentModel;

namespace DaphneUserControlLib
{
    /// <summary>
    /// Interaction logic for CellDeathMol.xaml
    /// </summary>
    public partial class CellDeathMol : UserControl, INotifyPropertyChanged
    {
        //public ComboBox DeathComboBox 
        //{
        //    get
        //    {
        //        return cbMolList;
        //    }
        //}

        public CellDeathMol()
        {
            InitializeComponent();
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            DeathComboBox = cbMolList;
        }

        ///
        //Notification handling
        /// 
        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                var e = new PropertyChangedEventArgs(propertyName);
                handler(this, e);
            }
        }

        //------------------------------------------------------------------------
        ///Dependency Properties
        ///

        //Combobox Items
        public static DependencyProperty ComboItemsProperty = DependencyProperty.Register("ComboItems", typeof(object), typeof(CellDeathMol), new FrameworkPropertyMetadata(ComboItemsPropertyChanged));
        public object ComboItems
        {
            get { return (List<object>)GetValue(ComboItemsProperty); }
            set
            {
                SetValue(ComboItemsProperty, value);                
                OnPropertyChanged("ComboItems");
                
            }
        }
        public static void ComboItemsPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CellDeathMol uc = d as CellDeathMol;
            uc.ComboItems = (object)(e.NewValue);

            //Type objType = uc.ComboItems.GetType();
            //object objInstance = Activator.CreateInstance(objType);

            //ItemPropertyInfo info = objInstance.GetProperty
            //uc.DeathComboBox.ItemsSource = objInstance;
        }

        //Display Path
        public static DependencyProperty DisplayPathProperty = DependencyProperty.Register("DisplayPath", typeof(string), typeof(CellDeathMol), new FrameworkPropertyMetadata(DisplayPathPropertyChanged));
        public string DisplayPath
        {
            get { return (string)GetValue(DisplayPathProperty); }
            set
            {
                SetValue(DisplayPathProperty, value);
                OnPropertyChanged("DisplayPath");
            }
        }
        public static void DisplayPathPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CellDeathMol uc = d as CellDeathMol;
            uc.DisplayPath = (string)(e.NewValue);
        }


        public static DependencyProperty DeathComboBoxProperty = DependencyProperty.Register("DeathComboBox", typeof(ComboBox), typeof(CellDeathMol), new FrameworkPropertyMetadata(DeathComboBoxPropertyChanged));
        public ComboBox DeathComboBox
        {
            get { return (ComboBox)GetValue(DeathComboBoxProperty); }
            set
            {
                SetValue(DeathComboBoxProperty, value);
                OnPropertyChanged("DeathComboBox");
            }
        }
        public static void DeathComboBoxPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            CellDeathMol uc = d as CellDeathMol;
            uc.DeathComboBox = (ComboBox)(e.NewValue);
        }


        private void cbMolList_DropDownOpened(object sender, EventArgs e)
        {
            int count = cbMolList.Items.Count;
        }
    }
}
