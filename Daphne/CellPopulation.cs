using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NativeDaphne;

namespace Daphne
{
    public class CellsPopulation : Nt_CellPopulation
    {

        //may not be needed
        public Dictionary<int, Cell> cellDictionary { get; set; }

        public CellsPopulation(int id)
        {
            PopulationId = id;
            cellDictionary = new Dictionary<int, Cell>();
        }

        public new void RemoveCell(int cell_id, bool completeRemoval = true)
        {
            if (completeRemoval == true)
            {
                cellDictionary.Remove(cell_id);
            }
            base.RemoveCell(cell_id, completeRemoval);
        }

        public void AddCell(int cell_id, Cell cell)
        {
            cellDictionary.Add(cell.Cell_id, cell);
            base.AddCell(cell);
        }

    }
}
