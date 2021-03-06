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

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for NewEditReacComplex.xaml
    /// </summary>
    /// 
    public enum ReactionComplexDialogType { NewComplex, EditComplex }
    public partial class NewEditReacComplex : Window
    {
        protected ReactionComplexDialogType dlgType;
        protected ConfigReactionComplex selectedRC;

        private ObservableCollection<ConfigReaction> leftList = new ObservableCollection<ConfigReaction>();
        private ObservableCollection<ConfigReaction> rightList = new ObservableCollection<ConfigReaction>();

        public ObservableCollection<ConfigReaction> LeftList
        {
            get
            {
                return leftList;
            }
            set
            {
                leftList = value;
            }
        }
        public ObservableCollection<ConfigReaction> RightList
        {
            get
            {
                return rightList;
            }
            set
            {
                rightList = value;
            }
        }

        //private ConfigCompartment comp;
        private Point mouseLocation;
        //private ObservableCollection<ConfigReactionComplex> rc_list;
        private ConfigCompartment comp;
        private EntityRepository er;

        //Added for LevelContext - Protocol or UserStore or DaphneStore.
        //This window is not a child of the MainWindow so we cannot use the
        //GetLevelContext method.
        private Level level;

        //To create a new rc
        public NewEditReacComplex(ReactionComplexDialogType type, ConfigCompartment _comp, Level _level)
        {
            InitializeComponent();
            this.Owner = Application.Current.MainWindow;
            dlgType = type;
            comp = _comp;
            er = _level.entity_repository;
            level = _level;
            Title = "New Reaction Complex";
            Initialize();
            lbAllReactions.ItemsSource = LeftList;
            lbCxReactions.ItemsSource = RightList;

        }

        //To edit an existing rc
        public NewEditReacComplex(ReactionComplexDialogType type, ConfigReactionComplex crc, ConfigCompartment _comp, Level _level)    
        {
            InitializeComponent();
            this.Owner = Application.Current.MainWindow;
            dlgType = type;
            selectedRC = crc;
            comp = _comp;
            er = _level.entity_repository;
            level = _level;
            Title = "Edit Reaction Complex";
            Initialize();
        }

        private void Initialize()
        {
            LeftList.Clear();
            RightList.Clear();

            //Level level = MainWindow.SOP.Protocol;

            //if adding a new rc
            if (dlgType == ReactionComplexDialogType.NewComplex)
            {
                //leftList is whole reactions list initially
                foreach (ConfigReaction reac in level.entity_repository.reactions)
                {
                    LeftList.Add(reac);
                }                
                //rightList is empty initially    
            }

            //else edit existing rc
            else 
            {
                // leftList is whole reactions list minus rc reactions - make a copy of it  
                foreach (ConfigReaction reac in level.entity_repository.reactions)
                {
                    LeftList.Add(reac);
                }
                
                // rightList is the reaction complex' reactions 
                foreach (ConfigReaction cr in selectedRC.reactions)
                {
                    RightList.Add(cr);  
                }

                // don't show in left list, reactions that are already in reac complex
                foreach (ConfigReaction cr in rightList)
                {
                    bool exists = LeftList.Where(r => r.entity_guid == cr.entity_guid).Any();
                    if (exists == true)
                    {
                        ConfigReaction leftReac = LeftList.Where(r => r.entity_guid == cr.entity_guid).First();
                        if (leftReac != null)
                        {
                            LeftList.Remove(leftReac);
                        }
                    }
                }
               
                // rc name
                txtRcName.Text = selectedRC.Name;
            }

            lbCxReactions.ItemsSource = null;
            lbCxReactions.ItemsSource = RightList;

            lbAllReactions.ItemsSource = null;
            lbAllReactions.ItemsSource = LeftList;
        }
        
        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            List<ConfigReaction> temp = new List<ConfigReaction>();
            foreach (ConfigReaction cr in lbAllReactions.SelectedItems)
            {
                if (RightList.Where(m => m.entity_guid == cr.entity_guid).Any()) continue;
                {
                    RightList.Add(cr);
                    temp.Add(cr);
                }        
            }

            foreach (ConfigReaction cr in temp)
            {
                LeftList.Remove(cr);
            }

            //listbox does not refresh without this
            lbAllReactions.ItemsSource = null;
            lbAllReactions.ItemsSource = LeftList;                  
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            List<ConfigReaction> temp = new List<ConfigReaction>();
            foreach (ConfigReaction cr in RightList)
            {
                temp.Add(cr); 
            }

            foreach (ConfigReaction reac in lbCxReactions.SelectedItems)
            {
                temp.Remove(reac);
                if (LeftList.Where(m => m.entity_guid == reac.entity_guid).Any()) continue;
                {
                    LeftList.Add(reac);
                }
            }

            RightList.Clear();
            foreach (ConfigReaction cr in temp)
            {
                RightList.Add(cr);
            }

            lbCxReactions.ItemsSource = null;
            lbCxReactions.ItemsSource = RightList;
            lbAllReactions.ItemsSource = null;
            lbAllReactions.ItemsSource = LeftList;
        }

        /// <summary>
        /// This method is called when user clicks Save button.
        /// It can save changes to a RC or also add a new rc depending on dlgType.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            bool edited = false;
            //Protocol level = MainWindow.SOP.Protocol;

            string rcname = txtRcName.Text;
            rcname = rcname.Trim();
            if (rcname.Length == 0)
            {
                MessageBox.Show("Please enter a name.", "Missing name", MessageBoxButton.OK, MessageBoxImage.Stop);
                return;
            }

            //Edit existing
            if (dlgType == ReactionComplexDialogType.EditComplex)
            {
                //Name
                selectedRC.Name = txtRcName.Text;
                selectedRC.ValidateName(level);

                //For removed reactions
                foreach (ConfigReaction cr in selectedRC.reactions.ToList())
                {
                    if (RightList.Where(m => m.entity_guid == cr.entity_guid).Any()) continue;
                    {
                        selectedRC.RemoveReaction(cr);
                        edited = true;
                    }
                }
                //For added reactions
                foreach (ConfigReaction reac in RightList)
                {
                    if (selectedRC.reactions_dict.ContainsKey(reac.entity_guid) != true)
                    {                      
                        ConfigReaction newreac = reac.Clone(true);
                        selectedRC.reactions.Add(newreac);
                        selectedRC.AddReactionMolPopsAndGenes(newreac, level.entity_repository);
                        edited = true;                
                    }
                }
                if (edited)
                {
                    //This is only for Vat Protocol
                    if (level is Protocol)
                    {
                        if (MainWindow.SOP.Protocol.scenario.GetType() == typeof(VatReactionComplexScenario))
                        {
                            VatReactionComplexScenario s = MainWindow.SOP.Protocol.scenario as VatReactionComplexScenario;
                            s.InitializeAllMols(true);
                            s.InitializeAllReacs();
                        }
                    }
                }
            }
            else
            //Add new RC
            {
                if (RightList.Count == 0) 
                {
                    MessageBox.Show("Please add some reactions to the reaction complex.", "Missing reactions", MessageBoxButton.OK, MessageBoxImage.Stop);
                    return;
                }
                ConfigReactionComplex crc = new ConfigReactionComplex(txtRcName.Text);
                //crc.Name = crc.GenerateNewName(MainWindow.ST_CurrentLevel, "_New");
                crc.ValidateName(level);

                foreach (ConfigReaction reac in RightList)
                {
                    crc.reactions.Add(reac);
                    //crc.AddReactionMolPopsAndGenes(reac, MainWindow.SOP.Protocol.entity_repository);
                    crc.AddReactionMolPopsAndGenes(reac, level.entity_repository);
                }

                if (comp != null)
                {
                    comp.reaction_complexes.Add(crc);
                }
                level.entity_repository.reaction_complexes.Add(crc);
            }
            DialogResult = true;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void LeftListBox_MouseMove(object sender, MouseEventArgs e)
        {
            ListBox lbSource = sender as ListBox;
            if (lbSource.SelectedIndex < 0)
                return;

            //mouseLocation = System.Windows.Forms.Control.MousePosition;
            mouseLocation = Mouse.GetPosition(Application.Current.MainWindow);
            //MoleculeAdorner.location = PointToScreen(mouseLocation);

            ConfigReaction reac = lbSource.SelectedItem as ConfigReaction;

            if (reac != null && e.LeftButton == MouseButtonState.Pressed)
            {
                DragDrop.DoDragDrop(lbSource,
                             reac,
                             DragDropEffects.Copy);

                //adorner = new MoleculeAdorner(lbSource);
                //AdornerLayer.GetAdornerLayer(lbSource).Add(adorner);

            }
        }

        private void RightListBox_Drop(object sender, DragEventArgs e)
        {
            List<ConfigReaction> temp = new List<ConfigReaction>();
            foreach (ConfigReaction cr in lbAllReactions.SelectedItems)
            {
                if (RightList.Where(m => m.entity_guid == cr.entity_guid).Any()) continue;
                {
                    RightList.Add(cr);
                    temp.Add(cr);
                }
            }

            foreach (ConfigReaction cr in temp)
            {
                LeftList.Remove(cr);
            }

            //listbox does not refresh without this
            lbAllReactions.ItemsSource = null;
            lbAllReactions.ItemsSource = LeftList;         
        }

        private void RightListBox_DragEnter(object sender, DragEventArgs e)
        {
            //ListBox lb = sender as ListBox;
            //if (lb != null)
            //{
            //    ConfigReaction reac = e.Data.GetData(typeof(ConfigReaction)) as ConfigReaction;
            //    if (reac != null)
            //    {
            //        string name = reac.TotalReactionString;
            //    }

            //}

        }

        private void RightListBox_DragLeave(object sender, DragEventArgs e)
        {

        }

        private void RightListBox_DragOver(object sender, DragEventArgs e)
        {
            //e.Effects = DragDropEffects.Copy;
        }


        private void AddReactions_Expanded(object sender, RoutedEventArgs e)
        {
            Height = 780;
        }

        private void AddReactions_Collapsed(object sender, RoutedEventArgs e)
        {
            Height = 400;
        }
    }
}
