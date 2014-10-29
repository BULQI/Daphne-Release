using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Windows;
using System.Windows.Controls;
using ActiproSoftware.Windows.Controls.Docking;
using System.Windows.Data;
using System.Collections.ObjectModel;

using Daphne;

namespace DaphneGui
{
    public class ToolWinBase : ToolWindow
    {
        public MainWindow MW { get; set; }
        public Protocol Protocol { get; set; }
        public string TitleText { get; set; }
        public Visibility ToroidalVisibility { get; set; }
        public Visibility SimRepetitionVisibility { get; set; }
        // This would be set to Hidden if we implement a 2D environment
        // and want to reuse the EnvironmentExtents control.
        // This field is not relevant to VatRC.
        public Visibility ZExtentVisibility { get; set; }

        public ToolWinBase()
        {
            TitleText = "";
        }

        /// <summary>
        /// Functionality to preserve focus when the Apply button is clicked.
        /// The base implementation does not preserve focus. 
        /// </summary>
        public virtual void Apply()
        {
            MW.Apply();
        }


        /// <summary>
        /// Check for a molecule in the specified compartment of the cell.
        /// </summary>
        /// <param name="molguid"></param>
        /// <param name="isMembrane"></param>
        /// <param name="cell"></param>
        /// <returns></returns>
        protected virtual bool CellHasMolecule(string molguid, bool isMembrane, ConfigCell cell)
        {
            if (isMembrane == true)
            {
                if (CompartmentHasMolecule(molguid, cell.membrane))
                {
                    return true;
                }
            }
            else if (CompartmentHasMolecule(molguid, cell.membrane))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Return true if the molecule is located in the specified compartment (cytosol or membrane).
        /// Modified and reorganized from previous code. 
        /// Has not been evaluated yet.
        /// </summary>
        /// <param name="molguid"></param>
        /// <param name="isMembrane"></param>
        /// <returns></returns>
        protected virtual bool CellPopsHaveMolecule(string molguid, bool isMembrane)
        {
            foreach (CellPopulation cell_pop in ((TissueScenario)Protocol.scenario).cellpopulations)
            {
                if (CellHasMolecule(molguid, isMembrane, cell_pop.Cell) == true)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Check to see if a compartment contains a molecular population of type molecule.
        /// gmk - Modified and reorganized from previous code. Needs evaluation.
        /// </summary>
        /// <param name="molguid"></param>
        /// <param name="compartment"></param>
        /// <returns></returns>
        protected bool CompartmentHasMolecule(string molguid, ConfigCompartment compartment)
        {
            foreach (ConfigMolecularPopulation molpop in compartment.molpops)
            {
                if (molpop.molecule.entity_guid == molguid)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Functionality to refresh elements when the selected tab changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ConfigTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        /// <summary>
        /// Filter (return true) for reaction that has necessary molecules in the environment comp and one or more cell membrane.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ecmAvailableReactionsListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigReaction cr = e.Item as ConfigReaction;
            bool bOK = false;

            foreach (string molguid in cr.reactants_molecule_guid_ref)
            {
                //if (EcmHasMolecule(molguid) || CellPopsHaveMoleculeInMemb(molguid))
                if (CompartmentHasMolecule(molguid, Protocol.scenario.environment.comp) || CellPopsHaveMolecule(molguid, true))
                {
                    bOK = true;
                }
                else
                {
                    bOK = false;
                    break;
                }
            }
            if (bOK)
            {
                foreach (string molguid in cr.products_molecule_guid_ref)
                {
                    //if (EcmHasMolecule(molguid) || CellPopsHaveMoleculeInMemb(molguid))
                    if (CompartmentHasMolecule(molguid, Protocol.scenario.environment.comp) || CellPopsHaveMolecule(molguid, true))
                    {
                        bOK = true;
                    }
                    else
                    {
                        bOK = false;
                        break;
                    }
                }
            }
            if (bOK)
            {
                foreach (string molguid in cr.modifiers_molecule_guid_ref)
                {
                    //if (EcmHasMolecule(molguid) || CellPopsHaveMoleculeInMemb(molguid))
                    if (CompartmentHasMolecule(molguid, Protocol.scenario.environment.comp) || CellPopsHaveMolecule(molguid, true))
                    {
                        bOK = true;
                    }
                    else
                    {
                        bOK = false;
                        break;
                    }
                }
            }

            //Finally, if the ecm already contains this reaction, exclude it from the available reactions list
            if (MainWindow.SOP.Protocol.scenario.environment.comp.reactions_dict.ContainsKey(cr.entity_guid) == true)
            {
                bOK = false;
            }

            e.Accepted = bOK;
        }

        /// <summary>
        /// Filter for bulk molecules. 
        /// gmk - Rename? Should be usable by all workbenches.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void EcsMoleculesListView_Filter(object sender, FilterEventArgs e)
        {
            ConfigMolecule mol = e.Item as ConfigMolecule;
            if (mol == null || mol.molecule_location != MoleculeLocation.Bulk)
            {
                e.Accepted = false;
                return;
            }
            e.Accepted = true;
            return;
        }

        /// <summary>
        /// gmk - Do we need this? Why only for bulkMoleculesListView_Filter? 
        /// Okay for all workbenches?
        /// </summary>
        public class FilterFactory
        {
            private object Context { get; set; }

            public static void bulkMoleculesListView_Filter(object sender, FilterEventArgs e)
            {
                ConfigMolecule mol = e.Item as ConfigMolecule;
                if (mol != null)
                {
                    // Filter out mol if membrane bound 
                    if (mol.molecule_location == MoleculeLocation.Bulk)
                    {
                        e.Accepted = true;
                    }
                    else
                    {
                        e.Accepted = false;
                    }
                }
            }
        }

        /// <summary>
        /// Converter to go between molecule GUID references in MolPops
        /// and molecule names kept in the repository of molecules.
        /// </summary>
        [ValueConversion(typeof(string), typeof(string))]
        public class MolGUIDtoMolNameConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                string guid = value as string;
                string mol_name = "";

                if (parameter == null || guid == "")
                    return mol_name;

                System.Windows.Data.CollectionViewSource cvs = parameter as System.Windows.Data.CollectionViewSource;
                ObservableCollection<ConfigMolecule> mol_list = cvs.Source as ObservableCollection<ConfigMolecule>;
                if (mol_list != null)
                {
                    foreach (ConfigMolecule mol in mol_list)
                    {
                        if (mol.entity_guid == guid)
                        {
                            mol_name = mol.Name;
                            break;
                        }
                    }
                }
                return mol_name;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                // TODO: Should probably put something real here, but right now it never gets called,
                // so I'm not sure what the value and parameter objects would be...
                return "y";
            }
        }

        /// <summary>
        /// Moved from SimConfigToolWindow.xaml.cs but not evaluated.
        /// </summary>
        /// <param name="rw"></param>
        /// <param name="transferMatrix"></param>
        public virtual void RegionFocusToGUISection(RegionWidget rw, bool transferMatrix)
        {
            // identify the widget's key
            string key = "";

            if (rw != null && ((VTKFullGraphicsController)MainWindow.GC).Regions.ContainsValue(rw) == true)
            {
                foreach (KeyValuePair<string, RegionWidget> kvp in ((VTKFullGraphicsController)MainWindow.GC).Regions)
                {
                    if (kvp.Value == rw)
                    {
                        key = kvp.Key;
                        break;
                    }
                }

                // found?
                if (key != "")
                {
                    // Select the correct region/solfac/gauss_spec in the GUI's lists
                    bool gui_spot_found = false;

                    if (!gui_spot_found)
                    {
                        // Next check whether any Solfacs use this right gaussian_spec for this box
                        for (int r = 0; r < MainWindow.SOP.Protocol.scenario.environment.comp.molpops.Count; r++)
                        {
                            // We'll just be picking the first one that uses 
                            if (MainWindow.SOP.Protocol.scenario.environment.comp.molpops[r].mp_distribution.mp_distribution_type == MolPopDistributionType.Gaussian &&
                                ((MolPopGaussian)MainWindow.SOP.Protocol.scenario.environment.comp.molpops[r].mp_distribution).gauss_spec.box_spec.box_guid == key)
                            {
                                SelectMolpopInGUI(r);
                                //gui_spot_found = true;
                                break;
                            }
                        }
                    }
                    if (!gui_spot_found)
                    {
                        GaussianSpecification next;
                        int count = 0;

                        MainWindow.SOP.Protocol.scenario.resetGaussRetrieve();
                        while ((next = MainWindow.SOP.Protocol.scenario.nextGaussSpec()) != null)
                        {
                            if (next.box_spec.box_guid == key)
                            {
                                SelectGaussSpecInGUI(count, key);
                                //gui_spot_found = true;
                                break;
                            }
                            count++;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Moved from SimConfigToolWindow.xaml.cs but not evaluated.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ReportsTabItem_Loaded(object sender, RoutedEventArgs e)
        {
            // gmk - uncomment and fix
            //lbRptCellPops.SelectedIndex = 0;
            //ICollectionView icv = CollectionViewSource.GetDefaultView(lvAvailableReacs.ItemsSource);
            //if (icv != null)
            //{
            //    icv.Refresh();
            //}
        }

        /// <summary>
        /// Moved from SimConfigToolWindow.xaml.cs but not evaluated.
        /// </summary>
        protected virtual void Save_Selected_Tab()
        {
            // Called by MainWindow.resetButton_Click
            //Code to preserve focus to the element that was in focus before "Apply" button clicked.

            //TabItem selectedTab = ConfigTabControl.SelectedItem as TabItem;

            //int nCellPopSelIndex = -1;
            //if (selectedTab == ProtocolToolWindow.tabCellPop)
            //{
            //    nCellPopSelIndex = ProtocolToolWindow.CellPopsListBox.SelectedIndex;
            //}

            //int nMolPopSelIndex = -1;
            //if (selectedTab == ProtocolToolWindow.tabECM)
            //{
            //    nMolPopSelIndex = ProtocolToolWindow.lbEcsMolPops.SelectedIndex;
            //}
        }

        /// <summary>
        /// Moved from SimConfigToolWindow.xaml.cs but not evaluated.
        /// May need to move method body into override version in TissueSimulation.
        /// </summary>
        /// <param name="index"></param>
        /// <param name="guid"></param>
        public virtual void SelectGaussSpecInGUI(int index, string guid)
        {
            TissueScenario scenario = (TissueScenario)MainWindow.SOP.Protocol.scenario;
            bool isBoxInCell = false;

            foreach (CellPopulation cp in scenario.cellpopulations)
            {
                CellPopDistribution cpd = cp.cellPopDist;

                if (cpd.DistType == CellPopDistributionType.Gaussian)
                {
                    CellPopGaussian cpg = cpd as CellPopGaussian;

                    if (cpg.gauss_spec.box_spec.box_guid == guid)
                    {
                        //cpg.Reset();
                        isBoxInCell = true;
                        break;
                    }
                }
            }

            // gmk - uncomment
            if (isBoxInCell == true)
            {
                //    MW.ConfigTabControl.SelectedItem = tabCellPop;
                //}
                //else
                //{
                //    MW.ConfigTabControl.SelectedItem = tabECM;
            }
        }

        public virtual void SelectMolpopInGUI(int index)
        {
            // gmk - uncomment and fix
            //lbEcsMolPops.SelectedIndex = index;
        }







    }
}
