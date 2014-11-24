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
using System.Windows.Media;
using System.Reflection;
using System.Windows.Controls.Primitives;
using System.Windows.Markup;
using System.Diagnostics;


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

    public class DataGridBehavior
    {
        #region DisplayRowNumber

        public static DependencyProperty DisplayRowNumberProperty =
            DependencyProperty.RegisterAttached("DisplayRowNumber",
                                                typeof(bool),
                                                typeof(DataGridBehavior),
                                                new FrameworkPropertyMetadata(false, OnDisplayRowNumberChanged));
        public static bool GetDisplayRowNumber(DependencyObject target)
        {
            return (bool)target.GetValue(DisplayRowNumberProperty);
        }
        public static void SetDisplayRowNumber(DependencyObject target, bool value)
        {
            target.SetValue(DisplayRowNumberProperty, value);
        }

        private static void OnDisplayRowNumberChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            DataGrid dataGrid = target as DataGrid;
            if ((bool)e.NewValue == true)
            {
                EventHandler<DataGridRowEventArgs> loadedRowHandler = null;
                loadedRowHandler = (object sender, DataGridRowEventArgs ea) =>
                {
                    if (GetDisplayRowNumber(dataGrid) == false)
                    {
                        dataGrid.LoadingRow -= loadedRowHandler;
                        return;
                    }
                    int num = ea.Row.GetIndex();
                    ea.Row.Header = ea.Row.GetIndex() + 1;
                };
                dataGrid.LoadingRow += loadedRowHandler;
                ItemsChangedEventHandler itemsChangedHandler = null;
                itemsChangedHandler = (object sender, ItemsChangedEventArgs ea) =>
                {
                    if (GetDisplayRowNumber(dataGrid) == false)
                    {
                        dataGrid.ItemContainerGenerator.ItemsChanged -= itemsChangedHandler;
                        return;
                    }
                    GetVisualChildCollection<DataGridRow>(dataGrid).
                        ForEach(d => d.Header = d.GetIndex());
                };
                dataGrid.ItemContainerGenerator.ItemsChanged += itemsChangedHandler;
            }
        }

        #endregion // DisplayRowNumber

        #region HighlightColumn

        public static bool GetHighlightColumn(DependencyObject obj)
        {
            return (bool)obj.GetValue(HighlightColumnProperty);
        }

        public static void SetHighlightColumn(DependencyObject obj, bool value)
        {
            bool oldvalue = GetHighlightColumn(obj);

            obj.SetValue(HighlightColumnProperty, !oldvalue);
        }

        // Using a DependencyProperty as the backing store for HighlightColumn.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty HighlightColumnProperty =
            DependencyProperty.RegisterAttached("HighlightColumn", typeof(bool),
            typeof(DataGridBehavior), new FrameworkPropertyMetadata(false, OnHighlightColumnPropertyChanged));

        public static bool GetIsCellHighlighted(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsCellHighlightedProperty);
        }

        public static void SetIsCellHighlighted(DependencyObject obj, bool value)
        {
            obj.SetValue(IsCellHighlightedProperty, value);
        }

        // Using a DependencyProperty as the backing store for IsCellHighlighted.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty IsCellHighlightedProperty =
            DependencyProperty.RegisterAttached("IsCellHighlighted", typeof(bool), typeof(DataGridBehavior),
            new UIPropertyMetadata(false));

        private static void OnHighlightColumnPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            Console.WriteLine(e.NewValue);
            DataGridCell cell = sender as DataGridCell;

            if (cell != null)
            {
                DataGrid dg = GetDataGridFromCell(cell);
                DataGridColumn column = cell.Column;

                for (int i = 0; i < dg.Items.Count; i++)
                {
                    DataGridRow row = dg.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                    DataGridCell currentCell = GetCell(row, column);
                    if (currentCell != null)
                    {
                        currentCell.SetValue(DataGridBehavior.IsCellHighlightedProperty, e.NewValue);
                    }
                }
            }
            else
            {
                DataGridColumn col = sender as DataGridColumn;
                if (col == null)
                    return;

                DataGrid dg = GetDataGridFromColumn(col);
                for (int i = 0; i < dg.Items.Count; i++)
                {
                    DataGridRow row = dg.ItemContainerGenerator.ContainerFromIndex(i) as DataGridRow;
                    DataGridCell currentCell = GetCell(row, col);
                    if (currentCell != null)
                    {
                        currentCell.SetValue(DataGridBehavior.IsCellHighlightedProperty, e.NewValue);
                    }
                }
            }
        }

        private static DataGrid GetDataGridFromCell(DataGridCell cell)
        {
            DataGrid retVal = null;
            FrameworkElement fe = cell;
            while ((retVal == null) && (fe != null))
            {
                if (fe is DataGrid)
                    retVal = fe as DataGrid;
                else
                    fe = VisualTreeHelper.GetParent(fe) as FrameworkElement;
            }
            return retVal;
        }

        private static DataGrid GetDataGridFromColumn(DataGridColumn col)
        {
            DataGrid retVal = null;

            retVal = col.GetType().GetProperty("DataGridOwner", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(col, null) as DataGrid;

            return retVal;
        }

        private static DataGridCell GetCell(DataGridRow row, DataGridColumn column)
        {
            DataGridCell retVal = null;
            DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(row);
            if (presenter != null)
            {
                for (int i = 0; i < presenter.Items.Count; i++)
                {
                    DataGridCell cell = presenter.ItemContainerGenerator.ContainerFromIndex(i) as DataGridCell;
                    if ((cell != null) && (cell.Column == column))
                    {
                        retVal = cell;
                        break;
                    }
                }
            }

            return retVal;
        }

        #endregion

        #region Get Visuals

        private static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }

        private static List<T> GetVisualChildCollection<T>(object parent) where T : Visual
        {
            List<T> visualCollection = new List<T>();
            GetVisualChildCollection(parent as DependencyObject, visualCollection);
            return visualCollection;
        }

        private static void GetVisualChildCollection<T>(DependencyObject parent, List<T> visualCollection) where T : Visual
        {
            int count = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < count; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T)
                {
                    visualCollection.Add(child as T);
                }
                if (child != null)
                {
                    GetVisualChildCollection(child, visualCollection);
                }
            }
        }

        #endregion // Get Visuals
    }

    public class RowToIndexConverter : MarkupExtension, IValueConverter
    {
        static RowToIndexConverter converter;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            DataGridRow row = value as DataGridRow;
            if (row != null)
            {
                int ind = row.GetIndex();
                return row.GetIndex() + 1;
            }
            else
                return -1;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            if (converter == null) converter = new RowToIndexConverter();
            return converter;
        }

        public RowToIndexConverter()
        {
        }
    }

    public class DatabindingDebugConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            Debugger.Break();
            return value;
        }
    }

    public class CellPopulationToSolidBrushConv : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            var cellpop = value as CellPopulation;
            if (cellpop == null) return null;

            RenderCell rc = MainWindow.SOP.GetRenderCell(cellpop.renderLabel);
            if (rc == null) return null;
            return new SolidColorBrush(rc.base_color.EntityColor);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class diffSchemeValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, CultureInfo culture)
        {
            ConfigTransitionScheme val = value as ConfigTransitionScheme;
            if (val != null && val.Name == "") return null;
            return value;
        }
    }

}
