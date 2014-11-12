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
using Daphne;
using System.ComponentModel;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for ReactionComplexControl.xaml
    /// </summary>
    public partial class ReactionComplexControl : UserControl, INotifyPropertyChanged
    {
        public ReactionComplexControl()
        {
            InitializeComponent();
            Tag = ListBoxReactionComplexes.SelectedItem;
        }

        public ConfigReactionComplex GetSelectedReactionComplex()
        {
            if (ListBoxReactionComplexes.SelectedIndex < 0)
                return null;

            ConfigReactionComplex crc = (ConfigReactionComplex)ListBoxReactionComplexes.SelectedItem;

            if (crc == null)
                return null;

            return crc;
        }

        private void ButtonEditComplex_Click(object sender, RoutedEventArgs e)
        {
            // Data context: compartment's list of reactions, need access to ER?
            ConfigReactionComplex crc = (ConfigReactionComplex)ListBoxReactionComplexes.SelectedItem;

            if (crc == null)
                return;

            AddReacComplex arc = new AddReacComplex(ReactionComplexDialogType.EditComplex, crc);
            arc.ShowDialog();
        }

        private void ButtonCopyComplex_Click(object sender, RoutedEventArgs e)
        {
            // This data context should be a compartment
            // Generally, the list will be in a compartment (either environment, membrane, or cytosol)
            if (ListBoxReactionComplexes.SelectedIndex < 0)
            {
                MessageBox.Show("Select a reaction complex to copy from.");
                return;
            }

            ConfigReactionComplex crcCurr = (ConfigReactionComplex)ListBoxReactionComplexes.SelectedItem;
            ConfigReactionComplex crcNew = crcCurr.Clone(false);

            ConfigCompartment cc = this.DataContext as ConfigCompartment;
            cc.reaction_complexes.Add(crcNew);

            ListBoxReactionComplexes.SelectedIndex = ListBoxReactionComplexes.Items.Count - 1;
        }

        private void ButtonNewReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            // This data context should be a compartment
            // Generally, the list will be in a compartment (either environment, membrane, or cytosol)
            // Will AddReacComplex need to access the ER for available reactions? 
            AddReacComplex arc = new AddReacComplex(ReactionComplexDialogType.AddComplex);
            if (arc.ShowDialog() == true)
                ListBoxReactionComplexes.SelectedIndex = ListBoxReactionComplexes.Items.Count - 1;

        }

        private void ButtonRemoveComplex_Click(object sender, RoutedEventArgs e)
        {
            // This data context should be the list of reactions
            // Generally, the list will be in a compartment (either environment, membrane, or cytosol)

            ConfigReactionComplex crc = (ConfigReactionComplex)(ListBoxReactionComplexes.SelectedItem);
            if (crc != null)
            {
                MessageBoxResult res;
                res = MessageBox.Show("Are you sure you would like to remove this reaction complex?", "Warning", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                    return;

                int index = ListBoxReactionComplexes.SelectedIndex;

                ConfigCompartment cc = this.DataContext as ConfigCompartment;
                cc.reaction_complexes.Remove(crc);

                ListBoxReactionComplexes.SelectedIndex = index;

                if (index >= ListBoxReactionComplexes.Items.Count)
                    ListBoxReactionComplexes.SelectedIndex = ListBoxReactionComplexes.Items.Count - 1;

                if (ListBoxReactionComplexes.Items.Count == 0)
                    ListBoxReactionComplexes.SelectedIndex = -1;

            }
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

        //SHOWMOLECULES
        public static DependencyProperty ShowMoleculesProperty = DependencyProperty.Register("ShowMolecules", typeof(Visibility), typeof(ReactionComplexControl), new FrameworkPropertyMetadata(Visibility.Hidden, ShowMoleculesPropertyChanged));
        public Visibility ShowMolecules
        {
            get { return (Visibility)GetValue(ShowMoleculesProperty); }
            set
            {
                SetValue(ShowMoleculesProperty, value);
                OnPropertyChanged("ShowMolecules");
            }
        }
        private static void ShowMoleculesPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ReactionComplexControl rcc = d as ReactionComplexControl;
            rcc.ShowMolecules = (Visibility)(e.NewValue);
        }


    }
}
