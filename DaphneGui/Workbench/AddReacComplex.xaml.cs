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
        protected EntityRepository er;
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

        //To add a new rc
        public AddReacComplex(ReactionComplexDialogType type)
        {
            InitializeComponent();
            dlgType = type;

            Title = "Add Reaction Complex";
            if (type == ReactionComplexDialogType.EditComplex)
            {
                Title = "Edit Reaction Complex";
            }

            Initialize();
            lbAllReactions.ItemsSource = LeftList;
            lbCxReactions.ItemsSource = RightList;

        }

        //To edit an existing rc
        public AddReacComplex(ReactionComplexDialogType type, ConfigReactionComplex crc)
        {
            InitializeComponent();
            dlgType = type;
            selectedRC = crc;
            Initialize();
        }

        private void Initialize()
        {
            er = MainWindow.SC.SimConfig.entity_repository;
            LeftList.Clear();
            RightList.Clear();

            //if adding a new rc
            if (dlgType == ReactionComplexDialogType.AddComplex)
            {
                //leftList is whole reactions list initially
                foreach (ConfigReaction reac in er.reactions)
                {
                    LeftList.Add(reac);
                }                
                //rightList is empty initially    
              
            }

            //else edit existing rc
            else 
            {
                //leftList is whole reactions list minus rc reactions - make a copy of it  
                foreach (ConfigReaction reac in er.reactions)
                {
                    leftList.Add(reac);
                }
                
                //rightList is the reaction complex' reactions 
                foreach (string rguid in selectedRC.reactions_guid_ref)
                {
                    rightList.Add(er.reactions_dict[rguid]);  
                }

                //don't show in left list, reactions that are already in reac complex
                foreach (ConfigReaction cr in rightList)
                {
                    leftList.Remove(cr);
                }
               
                //rc name
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
                RightList.Add(cr);
                temp.Add(cr);                                
            }

            foreach (ConfigReaction cr in temp)
            {
                LeftList.Remove(cr);
            }

            //lbCxReactions.ItemsSource = null;
            //lbCxReactions.ItemsSource = RightList;

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
                LeftList.Add(reac);
                temp.Remove(reac);
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

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;

            if (dlgType == ReactionComplexDialogType.EditComplex)
            {
                selectedRC.reactions_guid_ref.Clear();
                foreach (ConfigReaction reac in RightList)
                {
                    selectedRC.reactions_guid_ref.Add(reac.reaction_guid);
                }
            }
            else
            {
                ConfigReactionComplex crc = new ConfigReactionComplex(txtRcName.Text);
                crc.ReadOnly = false;
                foreach (ConfigReaction reac in RightList)
                {
                    crc.reactions_guid_ref.Add(reac.reaction_guid);
                }
                er.reaction_complexes.Add(crc);
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
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
