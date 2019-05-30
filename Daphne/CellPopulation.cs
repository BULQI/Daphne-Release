/*
Copyright (C) 2019 Kepler Laboratory of Quantitative Immunology

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY 
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
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

        public void RemoveCell(int cell_id, bool completeRemoval = true)
        {
            if (completeRemoval == true)
            {
                cellDictionary.Remove(cell_id);
            }
            base.RemoveCell(cell_id);
        }

        public void AddCell(int cell_id, Cell cell)
        {
            cellDictionary.Add(cell.Cell_id, cell);
            base.AddCell(cell);
        }

    }
}