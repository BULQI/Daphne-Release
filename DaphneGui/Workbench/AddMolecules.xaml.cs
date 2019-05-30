/*
Copyright (C) 2019 Kepler Laboratory of Quantitative Immunology

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY 
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
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
