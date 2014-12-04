using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using ActiproSoftware.Windows.Controls.Docking;

using Daphne;
//using System.Windows.Data;
using System.Collections.ObjectModel;
//using System.Windows.Markup;
//using System.Windows.Forms;
using Microsoft.Win32;
using System.Windows.Media;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Linq;
using System.Windows.Data;


namespace DaphneGui
{
    /// <summary>
    /// Interaction logic for CellStudioToolWindow.xaml
    /// </summary>
    public partial class CellStudioToolWindow : ToolWindow
    {
        public CellStudioToolWindow()
        {
            InitializeComponent();
        }

        private void CellsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox lb = sender as ListBox;
            if (lb.SelectedItem != null)
            {
                ConfigCell cell = lb.SelectedItem as ConfigCell;
                if (cell != null)
                {
                    ucCellDetails.updateSelectedMoleculesAndGenes(cell);
                }
            }
        }

        private void AddLibCellButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cc = new ConfigCell();
            MainWindow.SOP.Protocol.entity_repository.cells.Add(cc);
            MainWindow.SOP.SelectedRenderSkin.AddRenderCell(cc.renderLabel, cc.CellName);
            CellsListBox.SelectedIndex = CellsListBox.Items.Count - 1;
        }

        private void RemoveCellButton_Click(object sender, RoutedEventArgs e)
        {
            int nIndex = CellsListBox.SelectedIndex;

            if (nIndex >= 0)
            {
                ConfigCell cell = (ConfigCell)CellsListBox.SelectedValue;
                MessageBoxResult res;
                if (MainWindow.SOP.Protocol.scenario.HasCell(cell))
                {
                    res = MessageBox.Show("If you delete this cell, corresponding cell populations will also be deleted. Would you like to continue?", "Warning", MessageBoxButton.YesNo);
                }
                else
                {
                    res = MessageBox.Show("Are you sure you would like to remove this cell?", "Warning", MessageBoxButton.YesNo);
                }

                if (res == MessageBoxResult.Yes)
                {
                    //MainWindow.SOP.Protocol.scenario.RemoveCellPopulation(cell);
                    MainWindow.SOP.Protocol.entity_repository.cells.Remove(cell);

                    CellsListBox.SelectedIndex = nIndex;

                    if (nIndex >= CellsListBox.Items.Count)
                        CellsListBox.SelectedIndex = CellsListBox.Items.Count - 1;

                    if (CellsListBox.Items.Count == 0)
                        CellsListBox.SelectedIndex = -1;
                }

            }
        }

        private void CopyCellButton_Click(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = (ConfigCell)CellsListBox.SelectedItem;
            if (cell == null)
            {
                return;
            }

            ConfigCell cellNew = cell.Clone(false);

            //Generate a new cell name
            cellNew.CellName = GenerateNewCellName(cell, "_Copy");

            MainWindow.SOP.Protocol.entity_repository.cells.Add(cellNew);
            MainWindow.SOP.SelectedRenderSkin.AddRenderCell(cellNew.renderLabel, cellNew.CellName);

            CellsListBox.SelectedIndex = CellsListBox.Items.Count - 1;
            CellsListBox.ScrollIntoView(CellsListBox.SelectedItem);

        }

        private void CellTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            ConfigCell cell = CellsListBox.SelectedItem as ConfigCell;

            if (cell == null)
                return;

            cell.ValidateName(MainWindow.SOP.Protocol);
            MainWindow.SOP.SelectedRenderSkin.SetRenderCellName(cell.renderLabel, cell.CellName);
        }

        private string GenerateNewCellName(ConfigCell cell)
        {
            int nSuffix = 1;
            string sSuffix = string.Format("_Copy{0:000}", nSuffix);
            string TempCellName = cell.CellName;
            while (FindCellBySuffix(sSuffix) == true)
            {
                TempCellName = cell.CellName.Replace(sSuffix, "");
                nSuffix++;
                sSuffix = string.Format("_Copy{0:000}", nSuffix);
            }
            TempCellName += sSuffix;
            return TempCellName;
        }

        private string GenerateNewCellName(ConfigCell cell, string ending)
        {
            int nSuffix = 1;
            string sSuffix = ending + string.Format("{0:000}", nSuffix);
            string TempCellName = cell.CellName;
            while (FindCellBySuffix(sSuffix) == true)
            {
                TempCellName = cell.CellName.Replace(sSuffix, "");
                nSuffix++;
                sSuffix = ending + string.Format("{0:000}", nSuffix);
            }
            TempCellName += sSuffix;
            return TempCellName;
        }

        // given a cell type name, check if it exists in repos
        private static bool FindCellBySuffix(string suffix)
        {
            foreach (ConfigCell cc in MainWindow.SOP.Protocol.entity_repository.cells)
            {
                if (cc.CellName.EndsWith(suffix))
                {
                    return true;
                }
            }
            return false;
        }

        private void CellStudio_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            CellsListBox.SelectedIndex = 0;
        }
    }
}
