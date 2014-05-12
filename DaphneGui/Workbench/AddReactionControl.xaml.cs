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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;
using Daphne;

namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for AddReactionControl.xaml
    /// </summary>
    public partial class AddReactionControl : UserControl
    {
        private List<string> reacmolguids;
        private List<string> prodmolguids;
        public AddReactionControl()
        {
            InitializeComponent();

            reacmolguids = new List<string>();
            prodmolguids = new List<string>();

            //InitializeWordList();



            string[] wordList =
                this.FindResource("WordList") as string[];

            ICollectionView view =
                CollectionViewSource.GetDefaultView(wordList);

            new TextSearchFilter(view, this.txtSearch);
        }

        ////private void InitializeWordList()
        ////{
        ////    //string[] wordList;
        ////    List<string> molnames = new List<string>();

        ////    foreach (ConfigMolecule cm in lbMol.Items)
        ////    {
        ////        molnames.Add(cm.Name);
        ////    }            

        ////    string[] wordList = molnames.ToArray();


        ////    //    this.FindResource("WordList") as string[];

        ////    ICollectionView view = CollectionViewSource.GetDefaultView(wordList);

        ////    new TextSearchFilter(view, this.txtSearch);
        ////}

        private void btnReac_Click(object sender, RoutedEventArgs e)
        {
            if (lbMol.SelectedItems.Count == 0)
                return;

            string reac = "";
            if (txtReac.Text.Length > 0)
                reac = " + ";
            foreach (ConfigMolecule cm in lbMol.SelectedItems)
            {
                reacmolguids.Add(cm.molecule_guid);
                reac += cm.Name;
                reac += " + ";
            }
            reac = reac.Substring(0, reac.Length - 3);

            txtReac.Text = txtReac.Text + reac;
        }

        private void btnProd_Click(object sender, RoutedEventArgs e)
        {
            if (lbMol.SelectedItems.Count == 0)
                return;

            string prod = "";
            if (txtProd.Text.Length > 0)
                prod = " + ";
            foreach (ConfigMolecule cm in lbMol.SelectedItems)
            {
                prodmolguids.Add(cm.molecule_guid);
                prod += cm.Name;
                prod += " + ";
            }
            prod = prod.Substring(0, prod.Length - 3);

            txtProd.Text = txtProd.Text + prod;
        }

        private void btnUnselect_Click(object sender, RoutedEventArgs e)
        {
            lbMol.UnselectAll();
        }

        private void btnRClear_Click(object sender, RoutedEventArgs e)
        {
            txtReac.Text = "";
            reacmolguids.Clear();
        }

        private void btnPClear_Click(object sender, RoutedEventArgs e)
        {
            txtProd.Text = "";
            prodmolguids.Clear();
        }

        // given a reaction type, find its guid
        private static string findReactionTemplateGuid(ReactionType rt, SimConfiguration sc)
        {
            foreach (ConfigReactionTemplate crt in sc.entity_repository.reaction_templates)
            {
                if (crt.reac_type == rt)
                {
                    return crt.reaction_template_guid;
                }
            }
            return null;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            ConfigReaction cr = new ConfigReaction();
            cr.rate_const = Convert.ToDouble(txtRate.Text);
            cr.reaction_template_guid_ref = findReactionTemplateGuid(ReactionType.Association, MainWindow.SC.SimConfig);  //MainWindow.SC.SimConfig.entity_repository.reaction_templates_dict[2].reaction_template_guid;  //findReactionTemplateGuid(ReactionType.BoundaryAssociation, MainWindow.SC.SimConfig);
            foreach (string s in reacmolguids) {
                if (!cr.reactants_molecule_guid_ref.Contains(s))
                    cr.reactants_molecule_guid_ref.Add(s);
            }

            //THIS IS NOT OK
            //UNTIL WE HAVE AUTOMATIC REACTIONS, WE NEED USER TO INPUT REACTION_TYPE
            cr.reaction_template_guid_ref = MainWindow.SC.SimConfig.entity_repository.reaction_templates[(int)ReactionType.Association].reaction_template_guid;

            //increment stoichiometry
            string guid = cr.reaction_template_guid_ref;
            ConfigReactionTemplate crt = MainWindow.SC.SimConfig.entity_repository.reaction_templates_dict[guid];

            // the indices of cr and crt match
            ////for(int i = 0; i < cr.reactants_molecule_guid_ref.Count; i++)
            ////{
            ////    crt.reactants_stoichiometric_const[i] += 1;
            ////}

            //----------------------------------

            foreach (string s in prodmolguids)
            {
                if (!cr.products_molecule_guid_ref.Contains(s))
                    cr.products_molecule_guid_ref.Add(s);
            }

            //increment stoichiometry            
            ////for (int i = 0; i < cr.products_molecule_guid_ref.Count; i++)
            ////{
            ////    crt.products_stoichiometric_const[i] += 1;
            ////}


            MainWindow.SC.SimConfig.entity_repository.reactions.Add(cr);
            //cr.GetTotalReactionString(MainWindow.SC.SimConfig.entity_repository);

        }
    }
}
