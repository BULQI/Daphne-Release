using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daphne
{
    public class Differentiator
    {
        private Random ran;
        private Cell cell;
        private MolecularPopulation[] geneActivities;
        // there must be exactly one signal for each differentiation state
        private MolecularPopulation[,] signals;
        private int nStates;
        public int[,] DifferentationMatrix;

        public Differentiator(MolecularPopulation[,] _signals, MolecularPopulation[] _genes, Cell _cell)
        {
            ran = new Random();
            geneActivities = _genes;
            signals = _signals;
            cell = _cell;
            nStates = signals.GetLength(0);
        }

        public void Step(double dt)
        {
            double u = ran.NextDouble();
            for (int i = 0; i < nStates; i++)
            {
                if (cell.DifferentiationState == i) continue;
                //if (signals[cell.DiffState, i] != null && u < signals[cell.DiffState, i].Conc * dt)
                //{
                //    cell.DiffState = i;
                //    for (int iGene = 0; iGene < geneActivities.Length; iGene++)
                //    {
                //        geneActivities[iGene].Conc = DifferentationMatrix[i,iGene];
                //    }
                //}
            }
       
        }
    }
}
