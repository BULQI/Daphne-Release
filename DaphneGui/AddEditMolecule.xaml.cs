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

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for AddEditMolecule.xaml
    /// </summary>
    /// 
    public enum MoleculeDialogType { NEW, EDIT }

    public partial class AddEditMolecule : Window
    {
        public string Caller { get; set; }
        public MoleculeDialogType DlgType { get; set; }
        public ConfigMolecule Mol { get; set; }
        public AddEditMolecule(ConfigMolecule mol, MoleculeDialogType type)
        {
            InitializeComponent();

            this.Owner = Application.Current.MainWindow;

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
            string caller = Tag as string;

            if (caller == "ecs")
            {
                if (Mol.Name.Contains("|") || Mol.molecule_location == MoleculeLocation.Boundary)
                {
                    MessageBox.Show("You cannot add a membrane bound molecule to the ECS.");
                    return;
                }
            }
            //Called from cell cytosol or membrane
            else {
                if (Mol.molecule_location == MoleculeLocation.Bulk  && Mol.Name.Contains("|"))
                {
                    MessageBox.Show("A molecule containing '|' must be membrane bound.");
                    return;
                }
                else if (Mol.molecule_location == MoleculeLocation.Boundary && Mol.Name.Contains("|") == false) 
                {
                    //If user forgot to add a pipe character, just add it automatically
                    Mol.Name += "|";
                }
                else if (Mol.molecule_location == MoleculeLocation.Bulk && caller == "membrane")
                {
                    MessageBox.Show("You cannot add a bulk molecule to the cell membrane.");
                    return;
                }
                else if (Mol.molecule_location == MoleculeLocation.Boundary && caller == "cytosol")
                {
                    MessageBox.Show("You cannot add a boundary molecule to the cell cytosol.");
                    return;
                }
            }
             
            DialogResult = true;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            //If called by ecs, we need to hide the membrane bound check box, by using a different template from xaml.
            Caller = Tag as string;
        }

    }
}
