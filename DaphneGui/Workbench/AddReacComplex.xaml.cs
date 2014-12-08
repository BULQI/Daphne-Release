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

        private ConfigCompartment comp;
        private Point mouseLocation;

        //To create a new rc
        public NewEditReacComplex(ReactionComplexDialogType type, ConfigCompartment _comp)
        {
            InitializeComponent();
            dlgType = type;
            comp = _comp;

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
        public NewEditReacComplex(ReactionComplexDialogType type, ConfigReactionComplex crc, ConfigCompartment _comp)
        {
            InitializeComponent();
            dlgType = type;
            selectedRC = crc;
            comp = _comp;
            Initialize();
        }

        private void Initialize()
        {
            LeftList.Clear();
            RightList.Clear();

            //if adding a new rc
            if (dlgType == ReactionComplexDialogType.NewComplex)
            {
                //leftList is whole reactions list initially
                foreach (ConfigReaction reac in MainWindow.SOP.Protocol.entity_repository.reactions)
                {
                    if (reac.HasBoundaryMolecule(MainWindow.SOP.Protocol.entity_repository) == false)
                    {
                        LeftList.Add(reac);
                    }
                }                
                //rightList is empty initially    
            }

            //else edit existing rc
            else 
            {
                // leftList is whole reactions list minus rc reactions - make a copy of it  
                foreach (ConfigReaction reac in MainWindow.SOP.Protocol.entity_repository.reactions)
                {
                    if (reac.HasBoundaryMolecule(MainWindow.SOP.Protocol.entity_repository) == false)
                    {
                        LeftList.Add(reac);
                    }
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
            DialogResult = true;
            bool edited = false;

            //Edit existing
            if (dlgType == ReactionComplexDialogType.EditComplex)
            {
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
                            selectedRC.AddReactionMolPops(newreac, MainWindow.SOP.Protocol.entity_repository);
                            edited = true;
                        
                    }
                }
                if (edited)
                {
                    VatReactionComplexScenario s = MainWindow.SOP.Protocol.scenario as VatReactionComplexScenario;
                    s.InitializeAllMols();
                    s.InitializeAllReacs();
                }
            }
            else
            //Add new RC
            {
                ConfigReactionComplex crc = new ConfigReactionComplex(txtRcName.Text);
                crc.Name = crc.GenerateNewName(MainWindow.SOP.Protocol, "_New");
                crc.ValidateName(MainWindow.SOP.Protocol);

                foreach (ConfigReaction reac in RightList)
                {
                    crc.reactions.Add(reac);
                    crc.AddReactionMolPops(reac, MainWindow.SOP.Protocol.entity_repository);
                }
                if (comp != null)
                {
                    comp.reaction_complexes.Add(crc);
                }
                MainWindow.SOP.Protocol.entity_repository.reaction_complexes.Add(crc);
            }
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
