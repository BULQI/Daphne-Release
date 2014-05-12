using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Daphne;

namespace DaphneGui
{
    public static class MyExtensions
    {
        public static T GetVisualChild<T>(Visual parent) where T : Visual
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

        //There is a simple method for getting the current (selected) row of the DataGrid:
        public static DataGridRow GetSelectedRow(this DataGrid grid)
        {
            return (DataGridRow)grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem);
        }

        //We can also get a row by its indices:
        public static DataGridRow GetRow(this DataGrid grid, int index)
        {
            DataGridRow row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(index);
            if (row == null)
            {
                // May be virtualized, bring into view and try again.
                grid.UpdateLayout();
                grid.ScrollIntoView(grid.Items[index]);
                row = (DataGridRow)grid.ItemContainerGenerator.ContainerFromIndex(index);
            }
            return row;
        }

        //Now we can get a cell of a DataGrid by an existing row:
        public static DataGridCell GetCell(this DataGrid grid, DataGridRow row, int column)
        {
            if (row != null)
            {
                DataGridCellsPresenter presenter = GetVisualChild<DataGridCellsPresenter>(row);

                if (presenter == null)
                {
                    grid.ScrollIntoView(row, grid.Columns[column]);
                    presenter = GetVisualChild<DataGridCellsPresenter>(row);
                }

                DataGridCell cell = (DataGridCell)presenter.ItemContainerGenerator.ContainerFromIndex(column);
                return cell;
            }
            return null;
        }

        //Or we can simply select a row by its indices:
        public static DataGridCell GetCell(this DataGrid grid, int row, int column)
        {
            DataGridRow rowContainer = grid.GetRow(row);
            return grid.GetCell(rowContainer, column);
        }

        //The functions above are extension methods. Their use is simple:

        //var selectedRow = grid.GetSelectedRow();
        //var columnCell = grid.GetCell(selectedRow, 0);




        //public static string ReactantsString(this ReactionTemplate rt)
        //{
            
        //        string s = "";
        //        foreach (SpeciesReference sr in rt.listOfReactants)
        //        {
        //            if (sr.stoichiometry > 1)
        //                s += sr.stoichiometry;
        //            s += sr.species;
        //            s += " + ";
        //        }
        //        foreach (SpeciesReference sr in rt.listOfModifiers)
        //        {
        //            if (sr.stoichiometry > 1)
        //                s += sr.stoichiometry;
        //            s += sr.species;
        //            s += " + ";
        //        }
        //        char[] trimChars = { ' ', '+' };
        //        s = s.Trim(trimChars);
        //        return s;
                       

        //}
        //public static string ProductsString(this ReactionTemplate rt)
        //{
            
        //        string s = "";
        //        foreach (SpeciesReference sr in rt.listOfProducts)
        //        {
        //            if (sr.stoichiometry > 1)
        //                s += sr.stoichiometry;
        //            s += sr.species;
        //            s += " + ";
        //        }
        //        foreach (SpeciesReference sr in rt.listOfModifiers)
        //        {
        //            if (sr.stoichiometry > 1)
        //                s += sr.stoichiometry;
        //            s += sr.species;
        //            s += " + ";
        //        }
        //        char[] trimChars = { ' ', '+' };
        //        s = s.Trim(trimChars);
        //        return s;
            
        //}
        //public static string TotalReactionString(this ReactionTemplate rt)
        //{
        //    string s = "";
        //    foreach (SpeciesReference sr in rt.listOfReactants)
        //    {
        //        if (sr.stoichiometry > 1)
        //            s += sr.stoichiometry;
        //        s += sr.species;
        //        s += " + ";
        //    }
        //    foreach (SpeciesReference sr in rt.listOfModifiers)
        //    {
        //        if (sr.stoichiometry > 1)
        //            s += sr.stoichiometry;
        //        s += sr.species;
        //        s += " + ";
        //    }
        //    char[] trimChars = { ' ', '+' };
        //    s = s.Trim(trimChars);

        //    string totalString = s + " -> ";

        //    s = "";
        //    foreach (SpeciesReference sr in rt.listOfProducts)
        //    {
        //        if (sr.stoichiometry > 1)
        //            s += sr.stoichiometry;
        //        s += sr.species;
        //        s += " + ";
        //    }
        //    foreach (SpeciesReference sr in rt.listOfModifiers)
        //    {
        //        if (sr.stoichiometry > 1)
        //            s += sr.stoichiometry;
        //        s += sr.species;
        //        s += " + ";
        //    }
            
        //    s = s.Trim(trimChars);

        //    totalString += s;

        //    return totalString;
        //}    






    }
}
