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

namespace GuiDaphneApp
{
    /// <summary>
    /// Interaction logic for AddDiffScheme.xaml
    /// </summary>
    /// 
    public enum DiffDialogType { AddDiff, EditDiff }
    public partial class AddDiffScheme : Window
    {
        private MainWindow mw;
        private DiffDialogType dlgType;
        private DiffScheme diffSchemeToEdit;

        public AddDiffScheme(MainWindow m, DiffDialogType type, DiffScheme ds = null)
        {
            mw = m;
            dlgType = type;

            InitializeComponent();

            Title = "Add Differentiation Scheme";
            if (type == DiffDialogType.EditDiff)
            {
                diffSchemeToEdit = ds;                 
                Loaded += MyLoadedRoutedEventHandler;                                              
            }
                        
        }
        
        public void MyLoadedRoutedEventHandler(Object sender, RoutedEventArgs e)
        {
            Loaded -= MyLoadedRoutedEventHandler;
            InitializeEditDialog();
        }
        
        private void InitializeEditDialog()
        {
            Title = "Edit Differentiation Scheme";
            string name = diffSchemeToEdit.Name;
            txtSchemeName.Text = name;
            int count = lbAllStates.Items.Count;
            for (int i = 0; i < count; i++) 
            {
                string s = (string) lbAllStates.Items[i];
                if (diffSchemeToEdit.HasState(s)) {            
                    lbAllStates.SelectedItems.Add(lbAllStates.Items[i]);
                }
            }
            count = lbAllMol.Items.Count;
            for (int i = 0; i < count; i++) {
                Molecule m = (Molecule)lbAllMol.Items[i];
                //string s = (string)lbAllMol.Items[i];
                string s = m.Name;
                if (diffSchemeToEdit.States[0].HasGene(s))
                {
                    lbAllMol.SelectedItems.Add(lbAllMol.Items[i]);
                }
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            int count = lbAllStates.Items.Count;
            if (dlgType == DiffDialogType.AddDiff)
            {
                string name = txtSchemeName.Text;
                if (name.Length == 0)
                {
                    MessageBox.Show("You must enter a scheme name.");
                    return;
                }
                else if (lbAllStates.SelectedItems.Count < 2)
                {
                    MessageBox.Show("You must select at least 2 differentiation states.");
                    return;
                }
                else if (lbAllMol.SelectedItems.Count < 1)
                {
                    MessageBox.Show("You must select at least 1 molecule.");
                    return;
                }

                string[] states = new string[lbAllStates.SelectedItems.Count];
                lbAllStates.SelectedItems.CopyTo(states, 0);

                string[] genes = new string[lbAllMol.SelectedItems.Count];
                //lbAllMol.SelectedItems.CopyTo(genes, 0);

                int num = lbAllMol.SelectedItems.Count;
                
                for (int i = 0; i < num; i++)
                {
                    Molecule m = (Molecule)lbAllMol.SelectedItems[i];
                    genes[i] = m.Name;
                }

                DiffScheme ds = new DiffScheme(name);
                ds.AddStatesGenes(states, genes);
                ds.AddCellStates();
                mw.Sim.ListDiffSchemes.Add(ds);
            }
            else if (dlgType == DiffDialogType.EditDiff)
            {
                string name = txtSchemeName.Text;
                if (name.Length == 0)
                {
                    MessageBox.Show("You must enter a scheme name.");
                    return;
                }
                else if (lbAllStates.SelectedItems.Count < 2)
                {
                    MessageBox.Show("You must select at least 2 differentiation states.");
                    return;
                }
                else if (lbAllMol.SelectedItems.Count < 1)
                {
                    MessageBox.Show("You must select at least 1 molecule.");
                    return;
                }

                //
                
                string[] states = new string[lbAllStates.SelectedItems.Count];
                lbAllStates.SelectedItems.CopyTo(states, 0);

                string[] genes = new string[lbAllMol.SelectedItems.Count];
                //lbAllMol.SelectedItems.CopyTo(genes, 0);
                int num = lbAllMol.SelectedItems.Count;

                for (int i = 0; i < num; i++)
                {
                    Molecule m = (Molecule)lbAllMol.SelectedItems[i];
                    genes[i] = m.Name;
                }

                DiffScheme newds = new DiffScheme(diffSchemeToEdit.Name);

                foreach (string statename in states)
                {
                    if (!diffSchemeToEdit.HasState(statename))
                    {
                        DiffState diff = new DiffState(statename);
                        newds.States.Add(diff);
                        foreach (string str in genes)
                        {
                            if (!diff.HasGene(str))
                            {
                                Gene newgene = new Gene(str);
                                diff.Genes.Add(newgene);
                            }
                        }
                    }
                    else
                    {
                        DiffState baseDS = diffSchemeToEdit.GetState(statename);
                        DiffState diff = new DiffState(baseDS);
                        newds.States.Add(diff);
                        foreach (string str in genes)
                        {
                            if (!diff.HasGene(str))
                            {
                                Gene newgene = new Gene(str);
                                diff.Genes.Add(newgene);
                            }
                        }                        
                    }
                }
                //int num = diffSchemeToEdit.States.Count;
                //for (int i = 0; i < num; i++)
                //{
                //    string n = diffSchemeToEdit.States[i].Name;
                //    if (states.Contains(n))
                //    {
                //        DiffState diff = new DiffState(diffSchemeToEdit.States[i]);
                //        newds.States.Add(diff);
                //        foreach (string str in genes)
                //        {
                //            if (!diff.HasGene(str))
                //            {
                //                Gene newgene = new Gene(str);
                //                diff.Genes.Add(newgene);
                //            }
                //        }
                //    }
                //}

                mw.Sim.ListDiffSchemes.Remove(diffSchemeToEdit);
                newds.StoreMolNames(new List<string>(genes));                
                newds.AddCellStates();                
                mw.Sim.ListDiffSchemes.Add(newds);
            }
     
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void btnSave_Click_1(object sender, RoutedEventArgs e)
        {

        }

        private void btnSaveState_Click(object sender, RoutedEventArgs e)
        {
            string name = txtStateName.Text;
            if (name.Length == 0)
            {
                MessageBox.Show("Please enter a state name.");
                return;
            }
            else if (mw.Sim.DiffStateNames.Contains(name))
            {
                MessageBox.Show("State name already exists.");
                return;
            }

            mw.Sim.DiffStateNames.Add(name);
        }

        
    }
}
