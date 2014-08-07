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
using Daphne;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for AddEditMolecule.xaml
    /// </summary>
    /// 
    public enum MoleculeDialogType { NEW, EDIT }

    public partial class AddEditMolecule : Window
    {
        public MoleculeDialogType DlgType { get; set; }
        public ConfigMolecule Mol { get; set; }
        public AddEditMolecule(ConfigMolecule mol, MoleculeDialogType type)
        {
            InitializeComponent();

            Mol = mol;
            DlgType = type;

            if (DlgType == MoleculeDialogType.EDIT)
            {
                this.Title = "Edit a Molecule";
            }
            else
            {
                this.Title = "Create a New Molecule";
            }
                
            DataContext = this;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}
