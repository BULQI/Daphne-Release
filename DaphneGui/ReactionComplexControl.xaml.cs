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

        private ConfigReactionComplex selRC;
        public ConfigReactionComplex SelectedReactionComplex 
        {
            get
            {
                return selRC;
            }
            set
            {
                selRC = value;
                OnPropertyChanged("SelectedReactionComplex");
            }
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
            ConfigReactionComplex crc = (ConfigReactionComplex)ListBoxReactionComplexes.SelectedItem;

            if (crc == null)
                return;

            ConfigCompartment comp = null;
            //EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
            Level level = MainWindow.GetLevelContext(this);

            if (this.DataContext.GetType() == typeof(ConfigCompartment))
            {
                comp = (ConfigCompartment)this.DataContext;
            }

            NewEditReacComplex arc = new NewEditReacComplex(ReactionComplexDialogType.EditComplex, crc, comp, level);          
            arc.ShowDialog();
        }

        private void ButtonAddComplex_Click(object sender, RoutedEventArgs e)
        {
            Level level = MainWindow.GetLevelContext(this);

            ReactionComplexesInStore rcis = new ReactionComplexesInStore();
            rcis.DataContext = level.entity_repository;
            if (level is Protocol)
            {
                rcis.Tag = MainWindow.SOP.Protocol.scenario.environment.comp;
                if (rcis.ShowDialog() == true)
                {
                    ListBoxReactionComplexes.SelectedIndex = ListBoxReactionComplexes.Items.Count - 1;
                    if (ListBoxReactionComplexes.SelectedIndex < 0 || ListBoxReactionComplexes.SelectedIndex > ListBoxReactionComplexes.Items.Count)
                    {
                        ListBoxReactionComplexes.SelectedIndex = 0;
                    }
                }
            }
        }

        private void ButtonCopyComplex_Click(object sender, RoutedEventArgs e)
        {
            Level level = MainWindow.GetLevelContext(this);

            if (ListBoxReactionComplexes.SelectedIndex < 0)
            {
                MessageBox.Show("Select a reaction complex to copy from.");
                return;
            }

            //Need to add rc to the compartment plus the entity repository
            ConfigReactionComplex crcCurr = (ConfigReactionComplex)ListBoxReactionComplexes.SelectedItem;
            ConfigReactionComplex crcCopy = crcCurr.Clone(false);
            crcCopy.Name = crcCopy.GenerateNewName(level, "_Copy");            
            crcCopy.ValidateName(level);
            
            //MainWindow.SOP.Protocol.entity_repository.reaction_complexes.Add(crcCopy);
            level.entity_repository.reaction_complexes.Add(crcCopy);

            ConfigCompartment cc = this.DataContext as ConfigCompartment;
            if (cc != null)
            {
                ConfigReactionComplex crcLocal = crcCopy.Clone(true);
                cc.reaction_complexes.Add(crcLocal);
            }

            ListBoxReactionComplexes.SelectedIndex = ListBoxReactionComplexes.Items.Count - 1;
        }

        private void ButtonNewReactionComplex_Click(object sender, RoutedEventArgs e)
        {
            ConfigCompartment comp = null;
            Level level = MainWindow.GetLevelContext(this);

            //EntityRepository er = MainWindow.SOP.Protocol.entity_repository;
            EntityRepository er = level.entity_repository;

            if (this.DataContext.GetType() == typeof(ConfigCompartment))
            {
                comp = (ConfigCompartment)this.DataContext;               
            }

            NewEditReacComplex dlg = new NewEditReacComplex(ReactionComplexDialogType.NewComplex, comp, level);
            if (dlg.ShowDialog() == true)
                ListBoxReactionComplexes.SelectedIndex = ListBoxReactionComplexes.Items.Count - 1;
        }

        private void ButtonRemoveComplex_Click(object sender, RoutedEventArgs e)
        {
            ConfigReactionComplex crc = (ConfigReactionComplex)(ListBoxReactionComplexes.SelectedItem);
            if (crc != null)
            {
                MessageBoxResult res;
                res = MessageBox.Show("Are you sure you would like to remove this reaction complex?", "Warning", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.No)
                    return;

                int index = ListBoxReactionComplexes.SelectedIndex;

                
                foreach (ConfigReaction reac in crc.reactions.ToList())
                {
                    crc.RemoveReaction(reac);
                }

                ConfigCompartment cc = this.DataContext as ConfigCompartment;
                if (cc != null)
                {
                    cc.reaction_complexes.Remove(crc);                    
                }
                else
                {
                    EntityRepository er = this.DataContext as EntityRepository;
                    if (er != null)
                    {
                        er.reaction_complexes.Remove(crc);
                    }
                }
                
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

        //SHOW MOLECULES
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

        //SHOW ADD BUTTON
        public static DependencyProperty ShowAddButtonProperty = DependencyProperty.Register("ShowAddButton", typeof(Visibility), typeof(ReactionComplexControl), new FrameworkPropertyMetadata(Visibility.Collapsed, ShowAddButtonPropertyChanged));
        public Visibility ShowAddButton
        {
            get { return (Visibility)GetValue(ShowAddButtonProperty); }
            set
            {
                SetValue(ShowAddButtonProperty, value);
                OnPropertyChanged("ShowAddButton");
            }
        }
        private static void ShowAddButtonPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ReactionComplexControl rcc = d as ReactionComplexControl;
            rcc.ShowAddButton = (Visibility)(e.NewValue);
        }

        private void ListBoxReactionComplexes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = sender as ListBox;
            if (lb != null)
            {
                if (lb.SelectedIndex > -1)
                {
                    SelectedReactionComplex = (ConfigReactionComplex)lb.SelectedItem;
                }
                else
                {
                    SelectedReactionComplex = null;
                }
            }
        }
    }
}
