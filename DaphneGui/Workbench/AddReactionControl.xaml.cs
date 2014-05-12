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
            lbMol.UnselectAll();
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
            lbMol.UnselectAll();
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
            string rate = txtRate.Text.Trim();
            if (rate.Length <= 0)
            {
                MessageBox.Show("Please enter a rate constant.");
                txtRate.Focus();
                return;
            }

            bool bValid = ParseUserInput(txtReac.Text, txtProd.Text);

            ConfigReaction cr = new ConfigReaction();
            cr.ReadOnly = false;
            cr.ForegroundColor = System.Windows.Media.Colors.Black;
            cr.rate_const = Convert.ToDouble(txtRate.Text);

            //----------------------------------
            //Reactants
            foreach (string s in reacmolguids) {
                if (!cr.reactants_molecule_guid_ref.Contains(s))
                    cr.reactants_molecule_guid_ref.Add(s);
            }

            //----------------------------------
            //Products
            foreach (string s in prodmolguids)
            {
                if (!cr.products_molecule_guid_ref.Contains(s))
                    cr.products_molecule_guid_ref.Add(s);
            }

            //NEED MODIFIERS TOO IN GUI?  NO!

            //THIS IS NOT OK
            //UNTIL WE HAVE AUTOMATIC REACTIONS, WE NEED USER TO INPUT REACTION_TYPE
            cr.reaction_template_guid_ref = MainWindow.SC.SimConfig.entity_repository.reaction_templates[(int)ReactionType.Association].reaction_template_guid;

            //Add the reaction to repository collection
            MainWindow.SC.SimConfig.entity_repository.reactions.Add(cr);
        }

        public class InputMol
        {
            public string molguid;
            public int coeff;
        }

        private bool ParseUserInput(string txtLeftSide, string txtRightSide)
        {
            bool retval = true;

            //Dictionaries of molguid/coeff pairs
            Dictionary<string, int> recordsLeft = new Dictionary<string, int>();
            Dictionary<string, int> recordsRight = new Dictionary<string, int>();

            //----------------------------------
            //Reactants
            foreach (string s in reacmolguids)
            {
                if (recordsLeft.ContainsKey(s)) {
                    recordsLeft[s] += 1;
                }
                else 
                    recordsLeft.Add(s, 1);
            }

            //----------------------------------
            //Products
            foreach (string s in prodmolguids)
            {
                if (recordsRight.ContainsKey(s)) {
                    recordsRight[s] += 1;
                }
                else
                    recordsRight.Add(s, 1);
            }

            //////THIS CODE PARSES REACTION INPUT BY USER
            //////LEFT SIDE
            ////string phrase = txtLeftSide;
            ////string[] tokensLeft;
            ////string[] stringSeparators = new string[] { "+" };

            ////tokensLeft = phrase.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
            ////int count = tokensLeft.Count();

            ////for (int i = 0; i < count; i++)
            ////{
            ////    tokensLeft[i] = tokensLeft[i].Trim();
            ////}

            //////RIGHT SIDE
            ////phrase = txtRightSide;
            ////string[] tokensRight;

            ////tokensRight = phrase.Split(stringSeparators, StringSplitOptions.RemoveEmptyEntries);
            ////count = tokensRight.Count();

            ////for (int i = 0; i < count; i++)
            ////{
            ////    tokensRight[i] = tokensRight[i].Trim();
            ////}

            //////NOW IF THERE ARE COEFFICIENTS, LIKE N, STRIP THOSE OFF AND SAVE THEM OR CREATE N MOLECULES OF THAT TYPE IN REACTION CLASS.
            //////FOR GUI MOCKUPS, COEFFICIENTS ARE NOT RELEVANT. BUT NEED TO STRIP THEM OFF SO WE GET THE MOLECULE NAME RIGHT.           
            ////List<Molecule> molNames = new List<Molecule>();
            ////List<Molecule> molLeft = new List<Molecule>();
            ////List<int> stoicLeft = new List<int>();
            ////List<int> stoicRight = new List<int>();
            

            ////foreach (string str in tokensLeft)
            ////{
            ////    string sMol = str.TrimStart('1', '2', '3', '4', '5', '6', '7', '8', '9', '0');
            ////    int len1 = sMol.Length;
            ////    int len2 = str.Length;
            ////    int diff = len2 - len1;
            ////    if (diff == 0)
            ////        stoicLeft.Add(1);
            ////    else
            ////    {
            ////        string sCoeff = str.Substring(0, diff);
            ////        stoicLeft.Add(int.Parse(sCoeff));
            ////    }
            ////    //Molecule molec = new Molecule(sMol, 1, 1, 1);
            ////    //molNames.Add(molec);
            ////    //molLeft.Add(molec);
            ////}
            ////List<Molecule> molRight = new List<Molecule>();
            ////foreach (string str in tokensRight)
            ////{
            ////    string sMol = str.TrimStart('1', '2', '3', '4', '5', '6', '7', '8', '9', '0');
            ////    int len1 = sMol.Length;
            ////    int len2 = str.Length;
            ////    int diff = len2 - len1;
            ////    if (diff == 0)
            ////        stoicRight.Add(1);
            ////    else
            ////    {
            ////        string sCoeff = str.Substring(0, diff);
            ////        stoicRight.Add(int.Parse(sCoeff));
            ////    }
            ////    //Molecule molec = new Molecule(sMol, 1, 1, 1);
            ////    //molNames.Add(molec);
            ////    //molRight.Add(molec);
            ////}

            //NOW CHECK TO SEE WHICH MOLECULES ARE NOT ALREADY DEFINED            
            ////Dictionary<string, Molecule> newDic = new Dictionary<string, Molecule>();
            ////foreach (Molecule mol in molNames)
            ////{
            ////    //Make sure molecule not already in main dictionary
            ////    if (!Sim.MolecDict.ContainsKey(mol.Name))
            ////    {
            ////        //Now make sure molecule not already in the potential new list (dictionary)
            ////        if (!newDic.ContainsKey(mol.Name))
            ////        {
            ////            newDic.Add(mol.Name, mol);
            ////        }
            ////    }
            ////}

            return retval;
        }
    }
}
