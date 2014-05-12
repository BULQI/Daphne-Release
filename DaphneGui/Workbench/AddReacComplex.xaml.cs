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

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for AddReacComplex.xaml
    /// </summary>
    /// 
    public enum ReactionComplexDialogType { AddComplex, EditComplex }
    public partial class AddReacComplex : Window
    {
        protected MainWindow mw;
        protected ReactionComplexDialogType dlgType;

        private List<ReactionTemplate> leftList = new List<ReactionTemplate>();
        private List<ReactionTemplate> rightList = new List<ReactionTemplate>();

        public List<ReactionTemplate> LeftList
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
        public List<ReactionTemplate> RightList
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

        public AddReacComplex(ReactionComplexDialogType type)
        {
            InitializeComponent();
            dlgType = type;

            Title = "Add Reaction Complex";
            if (type == ReactionComplexDialogType.EditComplex)
            {
                Title = "Edit Reaction Complex";
            }

            //mw = m;

            Initialize();

            lbReactions.DataContext = MainWindow.SC.SimConfig.entity_repository.PredefReactionComplexes;
            lbReactions.ItemsSource = LeftList;
            lbCxReactions.ItemsSource = RightList;

        }

        private void Initialize()
        {
            //if adding a new rc
            if (dlgType == ReactionComplexDialogType.AddComplex)
            {
                //leftList is whole reactions list initially
                //leftList = mw.Sim.ReactionTemplateList;
                ////////leftList = new List<ReactionTemplate>(mw.Sim.ReactionTemplateList);
                //rightList is empty initially    
              
            }
            //else editing existing rc
            else 
            {
                //leftList is whole reactions list minus rc list - make a copy of it               
                //leftList = mw.Sim.ReactionTemplateList;  //this just points to original list, don't want that
                ////////leftList = new List<ReactionTemplate>(mw.Sim.ReactionTemplateList);

                ////////ReactionComplex rc = (ReactionComplex)(mw.lbComplexes.SelectedItem);
                
                //rightList is the reaction complex list initially - make a copy of it                
                ////////rightList = new List<ReactionTemplate>(rc.RTList);
                ////////foreach (ReactionTemplate rt in rightList)
                ////////{
                ////////    leftList.Remove(rt);
                ////////}
               
                //rc name
                ////////txtRcName.Text = rc.Name;
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            if (dlgType == ReactionComplexDialogType.EditComplex)
            {
                //ReactionComplex rc = (ReactionComplex)(mw.lbComplexes.SelectedItem);                
                //mw.Sim.EditReactionComplex(rc, RightList);
            }
            else
            {
                //ReactionComplex rc = new ReactionComplex(txtRcName.Text, new TinyBall());              
                //mw.Sim.AddReactionComplex(rc, RightList);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            foreach (ReactionTemplate rt in lbReactions.SelectedItems)
            {
                ReactionTemplate rtnew = new ReactionTemplate();
                rtnew = rt;
                RightList.Add(rtnew);
                LeftList.Remove(rt);                
            }

            lbCxReactions.ItemsSource = null;
            lbCxReactions.ItemsSource = RightList;

            lbReactions.ItemsSource = null;
            lbReactions.ItemsSource = LeftList;
                        
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            foreach (ReactionTemplate rt in lbCxReactions.SelectedItems)
            {
                ReactionTemplate rtnew = new ReactionTemplate();
                rtnew = rt;
                LeftList.Add(rtnew);
                RightList.Remove(rt);
            }

            lbCxReactions.ItemsSource = null;
            lbCxReactions.ItemsSource = RightList;

            lbReactions.ItemsSource = null;
            lbReactions.ItemsSource = LeftList;
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
