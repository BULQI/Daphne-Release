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
using System.Globalization;
using System.ComponentModel;

namespace DaphneGui
{
    public interface IRegionFocus
    {
        void RegionFocusToGUISection(RegionWidget rw, bool transferMatrix);
    }

    public enum ToolWindowType {BaseType, Tissue, VatRC};

    public class ToolWinBase : ToolWindow, IRegionFocus, INotifyPropertyChanged
    {
        public MainWindow MW { get; set; }

        private Protocol _protocol;
        public Protocol Protocol
        {
            get { return _protocol; }
            set
            {
                if (_protocol != value)
                {
                    _protocol = value;
                    OnPropertyChanged("Protocol");
                }
            }
        }

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
        /// Add an instance of the default box to the entity repository.
        /// Default values: box center at center of ECS, box widths are 1/4 of ECS extents
        /// </summary>
        /// <param name="box"></param>
        public virtual void AddDefaultBoxSpec(BoxSpecification box)
        {
        }

        //Moved to ConfigCompartment
        ///// <summary>
        ///// Add a molecular population to a compartement.
        ///// Intended as a utility to be used by the derived classes.
        ///// </summary>
        ///// <param name="mol"></param>
        ///// <param name="comp"></param>
        ///// <param name="isCell"></param>
        //protected void AddMolPopToCmpartment(ConfigMolecule mol, ConfigCompartment comp, Boolean isCell)
        //{
        //    ConfigMolecularPopulation cmp;

        //    if (isCell == true)
        //    {
        //        cmp = new ConfigMolecularPopulation(ReportType.CELL_MP);
        //    }
        //    else
        //    {
        //        cmp = new ConfigMolecularPopulation(ReportType.ECM_MP);
        //    }
        //    cmp.molecule = mol.Clone(null);
        //    cmp.Name = mol.Name;
        //    comp.molpops.Add(cmp);
        //}

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
                if (cell.membrane.HasMolecule(molguid))
                {
                    return true;
                }
            }
            else if (cell.cytosol.HasMolecule(molguid))
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
        public virtual bool CellPopsHaveMolecule(string molguid, bool isMembrane)
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

        // skg - remove this - belongs in ConfigCompartment
        /// <summary>
        /// Check to see if a compartment contains a molecular population of type molecule.
        /// gmk - Modified and reorganized from previous code. Needs evaluation.
        /// </summary>
        /// <param name="molguid"></param>
        /// <param name="compartment"></param>
        /// <returns></returns>
        //protected bool CompartmentHasMolecule(string molguid, ConfigCompartment compartment)
        //{
        //    foreach (ConfigMolecularPopulation molpop in compartment.molpops)
        //    {
        //        if (molpop.molecule.entity_guid == molguid)
        //            return true;
        //    }
        //    return false;
        //}


        /// <summary>
        /// Functionality to refresh elements when the selected tab changes
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ConfigTabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        /// <summary> 
        /// Gather filters that may be reused throughout the GUI.
        /// </summary>
        public class FilterFactory
        {
            private object Context { get; set; }

            public static void BoundaryMolecules_Filter(object sender, FilterEventArgs e)
            {
                ConfigMolecule mol = e.Item as ConfigMolecule;
                e.Accepted = true;

                if (mol != null)
                {
                    // Filter out mol if membrane bound 
                    if (mol.molecule_location == MoleculeLocation.Boundary)
                    {
                        e.Accepted = true;
                    }
                    else
                    {
                        e.Accepted = false;
                    }
                }
            }

            public static void BulkMolecules_Filter(object sender, FilterEventArgs e)
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
        }

        /// <summary>
        /// Initialization of the reports tab.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected virtual void ReportsTabItem_Loaded(object sender, RoutedEventArgs e)
        {
        }


        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }

        /// <summary>
        /// Workbench specific tasks to update the GUI after a simulation.
        /// Reset the box and Gaussian visibilities to there values prior to running the simulation.
        /// </summary>
        /// <param name="finished"></param>
        public virtual void GUIUpdate(bool finished)
        {
            GaussianSpecification next;

            Protocol.scenario.resetGaussRetrieve();
            while ((next = Protocol.scenario.nextGaussSpec()) != null)
            {
                BoxSpecification box = next.box_spec;

                // Save current visibility statuses
                box.box_visibility = box.current_box_visibility;
                next.gaussian_region_visibility = next.current_gaussian_region_visibility;
            }
        }

        /// <summary>
        /// Workbench specific tasks prior to running the simulation.
        /// Save the box and Gaussian visibility settings.
        /// </summary>
        public virtual void LockSaveStartSim()
        {
            GaussianSpecification next;

            Protocol.scenario.resetGaussRetrieve();
            while ((next = Protocol.scenario.nextGaussSpec()) != null)
            {
                BoxSpecification box = next.box_spec;

                // Save current visibility statuses
                box.current_box_visibility = box.box_visibility;
                next.current_gaussian_region_visibility = next.gaussian_region_visibility;

                // Property changed notifications will take care of turning off the Widgets and Actors
                box.box_visibility = false;
                next.gaussian_region_visibility = false;
            }
        }

        /// <summary>
        /// Display messsage box with choices for saving the Protocol to file.
        /// </summary>
        /// <returns></returns>
        public virtual MessageBoxResult ScenarioContentChanged()
        {
            return MW.saveDialog();
        }

    }


    public class ToolwinComponentVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            ToolWindowType type = (ToolWindowType)value;
            if (type == ToolWindowType.Tissue)
            {
                switch (parameter as string)
                {
                    case "VTKDisplayWindow":
                    case "ComponentsToolWindow":
                    case "CellStudioToolWindow":
                    case "ComponentsToolWindow_Genes":
                        return Visibility.Visible;
                }
            }
            else if (type == ToolWindowType.VatRC)
            {
                switch (parameter as string)
                {
                    case "ReacComplexChartWindow":
                    case "ComponentsToolWindow":
                        return Visibility.Visible;
                }
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }


}
