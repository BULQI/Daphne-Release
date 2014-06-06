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
using System.Collections.ObjectModel;

namespace GuiDaphneApp
{
    /// <summary>
    /// Interaction logic for AddMolecules.xaml
    /// </summary>
    /// 

    public class MyMolecule
    {
        public string Name { get; set; }
        public double MolecularWeight { get; set; }
        public double EffectiveRadius { get; set; }
        
        public MyMolecule(string thisName, double thisMW, double thisEffRad)            
        {
            Name = thisName;
            MolecularWeight = thisMW;
            EffectiveRadius = thisEffRad;            
        }        
    }

    public partial class AddMolecules : Window
    {
        protected List<Molecule> newMols = new List<Molecule>();
        private ObservableCollection<MyMolecule> newMols2 = new ObservableCollection<MyMolecule>();
        public ObservableCollection<MyMolecule> NewMols2
        {
            get
            {
                return newMols2;
            }
            set
            {
                newMols2 = value;
            }
        }    

        protected List<Molecule> finalNew = new List<Molecule>();

        public List<Molecule> FinalNewMols
        {
            get
            {
                return finalNew;
            }
            set
            {
                finalNew = value;
            }
        }        
        
        //Constructor
        public AddMolecules(Dictionary<string, Molecule>.ValueCollection newOnes)
        {
            InitializeComponent();
            foreach (Molecule mol in newOnes)
            {
                newMols.Add(mol);
                MyMolecule mm = new MyMolecule(mol.Name, mol.MolecularWeight, mol.EffectiveRadius);
                newMols2.Add(mm);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            //HERE ITERATE THROUGH THE DATA GRID AND FILL THE NAME, WEIGHT, RADIUS VALUES INTO newMols            
            foreach (MyMolecule mol in dgMols.Items)
            {
                //This is not returning the edited values!!  Why?
                Molecule m = new Molecule(mol.Name, mol.MolecularWeight, mol.EffectiveRadius, 1);                
                finalNew.Add(m);
            }
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            dgMols.ItemsSource = NewMols2;
        }
    }
}
