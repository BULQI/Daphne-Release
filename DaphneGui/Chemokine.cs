using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MathNet.Numerics.LinearAlgebra;

namespace DaphneGui
{
    /// <summary>
    /// a 3D volume of voxels; each voxel has concentrations and gradients
    /// </summary>
    public class Chemokine : Grid
    {
        /// <summary>
        /// a voxel in the 3D volume
        /// </summary>
        public class CKGridVoxel
        {
            /// <summary>
            /// constructor
            /// </summary>
            public CKGridVoxel()
            {
            }

            /// <summary>
            /// access the gradients
            /// </summary>
            public Dictionary<string, Dictionary<string, Vector>> Gradients
            {
                get { return gradients; }
            }

            /// <summary>
            /// access the concentrations
            /// </summary>
            public Dictionary<string, Dictionary<string, double>> Concentrations
            {
                get { return concentrations; }
            }

            private Dictionary<string, Dictionary<string, Vector>> gradients = new Dictionary<string, Dictionary<string, Vector>>();
            private Dictionary<string, Dictionary<string, double>> concentrations = new Dictionary<string, Dictionary<string, double>>();
        }

        /// <summary>
        /// construct the chemokine
        /// </summary>
        /// <param name="gridSize">3D size in absolute units</param>
        /// <param name="gridStep">width of voxel</param>
        public Chemokine(Vector gridSize, double gridStep) : base(gridSize, gridStep)
        {
            grid = new CKGridVoxel[gridDim[0], gridDim[1], gridDim[2]];

            for (int i = 0; i < gridDim[0]; i++)
            {
                for (int j = 0; j < gridDim[1]; j++)
                {
                    for (int k = 0; k < gridDim[2]; k++)
                    {
                        grid[i, j, k] = new CKGridVoxel();
                    }
                }
            }
        }

        /// <summary>
        /// retrieve chemokine gradients at a given position
        /// </summary>
        /// <param name="position">absolute 3D position</param>
        /// <returns>list (dictionary) of chemical gradients at the position</returns>
        public Dictionary<string, Dictionary<string, Vector>> getChemokineGradients(Vector position)
        {
            int[] idx = findGridIndex(position);

            // no interpolation
            if (interpolate == false)
            {
                return getChemokineGradients(idx);
            }

            // interpolate
            Dictionary<string, Dictionary<string, Vector>> dic000 = getChemokineGradients(idx);

            // special case handling for boundary
            if(position[0] <= gridStep / 2 ||
               position[1] <= gridStep / 2 ||
               position[2] <= gridStep / 2 ||
               position[0] >= gridStep * (gridDim[0] - 0.5) ||
               position[1] >= gridStep * (gridDim[1] - 0.5) ||
               position[2] >= gridStep * (gridDim[2] - 0.5))
            {
                return dic000;
            }

            int[] di = new int[3];
            double[] dx = new double[3];

            for (int i = 0; i < 3; i++)
            {
                if (position[i] - (idx[i] + 0.5) * gridStep > 0)
                {
                    di[i] = 1;
                    dx[i] = (position[i] - (idx[i] + 0.5) * gridStep) / gridStep;
                }
                else
                {
                    di[i] = -1;
                    dx[i] = 1.0 - (position[i] - (idx[i] - 0.5) * gridStep) / gridStep;
                }
            }

            int[] tmp = new int[3];
            Dictionary<string, Dictionary<string, Vector>> dic100, dic010, dic110, dic001, dic101, dic011, dic111, retDic;

            retDic = new Dictionary<string, Dictionary<string, Vector>>();
            if (dic000 != null)
            {
                foreach (string key1 in dic000.Keys)
                {
                    retDic.Add(key1, new Dictionary<string, Vector>());
                }
            }

            tmp[0] = idx[0] + di[0];
            tmp[1] = idx[1];
            tmp[2] = idx[2];
            dic100 = getChemokineGradients(tmp);

            tmp[0] = idx[0];
            tmp[1] = idx[1] + di[1];
            tmp[2] = idx[2];
            dic010 = getChemokineGradients(tmp);

            tmp[0] = idx[0] + di[0];
            tmp[1] = idx[1] + di[1];
            tmp[2] = idx[2];
            dic110 = getChemokineGradients(tmp);

            tmp[0] = idx[0];
            tmp[1] = idx[1];
            tmp[2] = idx[2] + di[2];
            dic001 = getChemokineGradients(tmp);

            tmp[0] = idx[0] + di[0];
            tmp[1] = idx[1];
            tmp[2] = idx[2] + di[2];
            dic101 = getChemokineGradients(tmp);

            tmp[0] = idx[0];
            tmp[1] = idx[1] + di[1];
            tmp[2] = idx[2] + di[2];
            dic011 = getChemokineGradients(tmp);

            tmp[0] = idx[0] + di[0];
            tmp[1] = idx[1] + di[1];
            tmp[2] = idx[2] + di[2];
            dic111 = getChemokineGradients(tmp);

            if (dic000 != null)
            {
                foreach (string key1 in dic000.Keys)
                {
                    foreach (string key2 in dic000[key1].Keys)
                    {
                        double[] grad = new double[3], interpolGrad = new double[3];
                        double factor;

                        if (dic000 != null)
                        {
                            factor = ((1.0 - dx[0]) * (1.0 - dx[1]) * (1.0 - dx[2]));
                            grad = dic000[key1][key2];
                            interpolGrad[0] += grad[0] * factor;
                            interpolGrad[1] += grad[1] * factor;
                            interpolGrad[2] += grad[2] * factor;
                        }
                        if (dic100 != null)
                        {
                            factor = dx[0] * (1.0 - dx[1]) * (1.0 - dx[2]);
                            grad = dic100[key1][key2];
                            interpolGrad[0] += grad[0] * factor;
                            interpolGrad[1] += grad[1] * factor;
                            interpolGrad[2] += grad[2] * factor;
                        }
                        if (dic010 != null)
                        {
                            factor = (1.0 - dx[0]) * dx[1] * (1.0 - dx[2]);
                            grad = dic010[key1][key2];
                            interpolGrad[0] += grad[0] * factor;
                            interpolGrad[1] += grad[1] * factor;
                            interpolGrad[2] += grad[2] * factor;
                        }
                        if (dic001 != null)
                        {
                            factor = (1.0 - dx[0]) * (1.0 - dx[1]) * dx[2];
                            grad = dic001[key1][key2];
                            interpolGrad[0] += grad[0] * factor;
                            interpolGrad[1] += grad[1] * factor;
                            interpolGrad[2] += grad[2] * factor;
                        }
                        if (dic110 != null)
                        {
                            factor = dx[0] * dx[1] * (1.0 - dx[2]);
                            grad = dic110[key1][key2];
                            interpolGrad[0] += grad[0] * factor;
                            interpolGrad[1] += grad[1] * factor;
                            interpolGrad[2] += grad[2] * factor;
                        }
                        if (dic101 != null)
                        {
                            factor = dx[0] * (1.0 - dx[1]) * dx[2];
                            grad = dic101[key1][key2];
                            interpolGrad[0] += grad[0] * factor;
                            interpolGrad[1] += grad[1] * factor;
                            interpolGrad[2] += grad[2] * factor;
                        }
                        if (dic011 != null)
                        {
                            factor = (1.0 - dx[0]) * dx[1] * dx[2];
                            grad = dic011[key1][key2];
                            interpolGrad[0] += grad[0] * factor;
                            interpolGrad[1] += grad[1] * factor;
                            interpolGrad[2] += grad[2] * factor;
                        }
                        if (dic111 != null)
                        {
                            factor = dx[0] * dx[1] * dx[2];
                            grad = dic111[key1][key2];
                            interpolGrad[0] += grad[0] * factor;
                            interpolGrad[1] += grad[1] * factor;
                            interpolGrad[2] += grad[2] * factor;
                        }
                        retDic[key1].Add(key2, interpolGrad);
                    }
                }
            }

            return retDic;
        }

        /// <summary>
        /// retrieve chemokine gradients at a given grid index
        /// </summary>
        /// <param name="idx">a 3D index (integer)</param>
        /// <returns>list (dictionary) of chemical gradients at the index</returns>
        public Dictionary<string, Dictionary<string, Vector>> getChemokineGradients(int[] idx)
        {
            if (legalIndex(idx) == true)
            {
                return grid[idx[0], idx[1], idx[2]].Gradients;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// retrieve a list of chemokine concentrations at a given position
        /// </summary>
        /// <param name="position">3D position</param>
        /// <returns>the list/dictionary of concentrations at the position</returns>
        public Dictionary<string, Dictionary<string, double>> getChemokineConcentrations(Vector position)
        {
            int[] idx = findGridIndex(position);

            // no interpolation
            if (interpolate == false)
            {
                return getChemokineConcentrations(idx);
            }

            // interpolate
            Dictionary<string, Dictionary<string, double>> dic000 = getChemokineConcentrations(idx);

            // special case handling for boundary
            if(position[0] <= gridStep / 2 ||
               position[1] <= gridStep / 2 ||
               position[2] <= gridStep / 2 ||
               position[0] >= gridStep * (gridDim[0] - 0.5) ||
               position[1] >= gridStep * (gridDim[1] - 0.5) ||
               position[2] >= gridStep * (gridDim[2] - 0.5))
            {
                return dic000;
            }

            int[] di = new int[3];
            double[] dx = new double[3];

            for (int i = 0; i < 3; i++)
            {
                if (position[i] - (idx[i] + 0.5) * gridStep > 0)
                {
                    di[i] = 1;
                    dx[i] = (position[i] - (idx[i] + 0.5) * gridStep) / gridStep;
                }
                else
                {
                    di[i] = -1;
                    dx[i] = 1.0 - (position[i] - (idx[i] - 0.5) * gridStep) / gridStep;
                }
            }

            int[] tmp = new int[3];
            Dictionary<string, Dictionary<string, double>> dic100, dic010, dic110, dic001, dic101, dic011, dic111, retDic;

            retDic = new Dictionary<string, Dictionary<string, double>>();
            if (dic000 != null)
            {
                foreach (string key1 in dic000.Keys)
                {
                    retDic.Add(key1, new Dictionary<string, double>());
                }
            }

            tmp[0] = idx[0] + di[0];
            tmp[1] = idx[1];
            tmp[2] = idx[2];
            dic100 = getChemokineConcentrations(tmp);

            tmp[0] = idx[0];
            tmp[1] = idx[1] + di[1];
            tmp[2] = idx[2];
            dic010 = getChemokineConcentrations(tmp);

            tmp[0] = idx[0] + di[0];
            tmp[1] = idx[1] + di[1];
            tmp[2] = idx[2];
            dic110 = getChemokineConcentrations(tmp);

            tmp[0] = idx[0];
            tmp[1] = idx[1];
            tmp[2] = idx[2] + di[2];
            dic001 = getChemokineConcentrations(tmp);

            tmp[0] = idx[0] + di[0];
            tmp[1] = idx[1];
            tmp[2] = idx[2] + di[2];
            dic101 = getChemokineConcentrations(tmp);

            tmp[0] = idx[0];
            tmp[1] = idx[1] + di[1];
            tmp[2] = idx[2] + di[2];
            dic011 = getChemokineConcentrations(tmp);

            tmp[0] = idx[0] + di[0];
            tmp[1] = idx[1] + di[1];
            tmp[2] = idx[2] + di[2];
            dic111 = getChemokineConcentrations(tmp);

            if (dic000 != null)
            {
                foreach (string key1 in dic000.Keys)
                {
                    foreach (string key2 in dic000[key1].Keys)
                    {
                        double conc = 0;

                        if (dic000 != null)
                        {
                            conc += dic000[key1][key2] * ((1.0 - dx[0]) * (1.0 - dx[1]) * (1.0 - dx[2]));
                        }
                        if (dic100 != null)
                        {
                            conc += dic100[key1][key2] * (dx[0] * (1.0 - dx[1]) * (1.0 - dx[2]));
                        }
                        if (dic010 != null)
                        {
                            conc += dic010[key1][key2] * ((1.0 - dx[0]) * dx[1] * (1.0 - dx[2]));
                        }
                        if (dic001 != null)
                        {
                            conc += dic001[key1][key2] * ((1.0 - dx[0]) * (1.0 - dx[1]) * dx[2]);
                        }
                        if (dic110 != null)
                        {
                            conc += dic110[key1][key2] * (dx[0] * dx[1] * (1.0 - dx[2]));
                        }
                        if (dic101 != null)
                        {
                            conc += dic101[key1][key2] * (dx[0] * (1.0 - dx[1]) * dx[2]);
                        }
                        if (dic011 != null)
                        {
                            conc += dic011[key1][key2] * ((1.0 - dx[0]) * dx[1] * dx[2]);
                        }
                        if (dic111 != null)
                        {
                            conc += dic111[key1][key2] * (dx[0] * dx[1] * dx[2]);
                        }
                        retDic[key1].Add(key2, conc);
                    }
                }
            }

            return retDic;
        }

        /// <summary>
        /// retrieve a list of chemokine concentrations at a given grid index
        /// </summary>
        /// <param name="idx">3D index (integer)</param>
        /// <returns>the list/dictionary of concentrations at the index</returns>
        public Dictionary<string, Dictionary<string, double>> getChemokineConcentrations(int[] idx)
        {
            if (legalIndex(idx) == true)
            {
                return grid[idx[0], idx[1], idx[2]].Concentrations;
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// insert a solfac into the chemokine using a homogeneous (constant) gradient
        /// </summary>
        /// <param name="solf">entity describing the solfac</param>
        /// <param name="solfControl">pointer to the solfac controller</param>
        public void populateSolfacHomogeneous(MolPopInfo solf, SolfacTypeHomogeneousController solfControl)
        {
            for (int i = 0; i < gridDim[0]; i++)
            {
                for (int j = 0; j < gridDim[1]; j++)
                {
                    for (int k = 0; k < gridDim[2]; k++)
                    {
                        // add the concentration
                        if (grid[i, j, k].Concentrations.ContainsKey(solf.mp_type_guid_ref) == false)
                        {
                            grid[i, j, k].Concentrations.Add(solf.mp_type_guid_ref, new Dictionary<string, double>());
                        }
                        grid[i, j, k].Concentrations[solf.mp_type_guid_ref].Add(solf.mp_guid, solfControl.level);

                        // add the gradient
                        if (grid[i, j, k].Gradients.ContainsKey(solf.mp_type_guid_ref) == false)
                        {
                            grid[i, j, k].Gradients.Add(solf.mp_type_guid_ref, new Dictionary<string, Vector>());
                        }
                        grid[i, j, k].Gradients[solf.mp_type_guid_ref].Add(solf.mp_guid, new double[] { 0.0, 0.0, 0.0 });
                    }
                }
            }
        }

        /// <summary>
        /// insert a solfac into the chemokine using a linear gradient (max to min)
        /// </summary>
        /// <param name="solf">entity describing the solfac</param>
        /// <param name="solfControl">pointer to the solfac controller</param>
        public void populateSolfacLinear(MolPopInfo solf, SolfacTypeLinearController solfControl)
        {
            Vector solfGrad = new Vector(((MolPopLinearGradient)solf.mp_distribution).gradient_direction);
            double projDiag = 0.0;

            // safety: we have to have a normalized vector
            solfGrad = solfGrad.Normalize();
            // project the diagonal onto the gradient; find the length of the environment along the gradient
            for (int i = 0; i < 3; i++)
            {
                projDiag += solfGrad[i] * (gridDim[i] - 1);
            }

            // calculate the gradient
            Vector grad = new Vector(solfGrad);

            if (projDiag > 0.0)
            {
                grad *= (solfControl.max - solfControl.min) / projDiag;
            }
            else if (projDiag < 0.0)
            {
                grad *= (solfControl.min - solfControl.max) / projDiag;
            }
            else
            {
                grad *= 0.0;
            }

            for (int i = 0; i < gridDim[0]; i++)
            {
                for (int j = 0; j < gridDim[1]; j++)
                {
                    for (int k = 0; k < gridDim[2]; k++)
                    {
                        double concentration,
                               projPoint;

                        // project the sample onto the gradient
                        projPoint = solfGrad[0] * i + solfGrad[1] * j + solfGrad[2] * k;
                        if (projDiag > 0.0)
                        {
                            concentration = solfControl.min + projPoint / projDiag * (solfControl.max - solfControl.min);
                        }
                        else if (projDiag < 0.0)
                        {
                            concentration = solfControl.max + projPoint / projDiag * (solfControl.min - solfControl.max);
                        }
                        else
                        {
                            concentration = 0.0;
                        }

                        // add the concentration
                        if (grid[i, j, k].Concentrations.ContainsKey(solf.mp_type_guid_ref) == false)
                        {
                            grid[i, j, k].Concentrations.Add(solf.mp_type_guid_ref, new Dictionary<string, double>());
                        }
                        grid[i, j, k].Concentrations[solf.mp_type_guid_ref].Add(solf.mp_guid, concentration);

                        // add the gradient
                        if (grid[i, j, k].Gradients.ContainsKey(solf.mp_type_guid_ref) == false)
                        {
                            grid[i, j, k].Gradients.Add(solf.mp_type_guid_ref, new Dictionary<string, Vector>());
                        }
                        grid[i, j, k].Gradients[solf.mp_type_guid_ref].Add(solf.mp_guid, grad);
                    }
                }
            }
        }

        /// <summary>
        /// insert a solfac into the chemokine using a gaussian distribution
        /// </summary>
        /// <param name="solf">entity describing the solfac</param>
        /// <param name="region">a pointer to the region that controls the Gaussian</param>
        /// <param name="solfControl">pointer to the solfac controller</param>
        public void populateSolfacGaussian(MolPopInfo solf, RegionControl region, SolfacTypeGaussianController solfControl)
        {
            double fVal,
                   eVal,
                   dVal;
            Vector center,
                   pos = new double[3],
                   locPos = null,
                   grad = new double[3],
                   normal,
                   width;

            if (region != null)
            {
                center = new Vector(region.GetTransform().GetPosition());
                width = new Vector(region.GetExtents());
                // we want to use the full extents for the sigmas since that's what's displayed in the gui
                width *= 2;
            }
            else
            {
                center = new double[] { 200.0, 200.0, 200.0 };
                width = new double[] { 100.0, 200.0, 100.0 };
            }

            // add concentrations and the gradient
            for (int i = 0; i < gridDim[0]; i++)
            {
                for (int j = 0; j < gridDim[1]; j++)
                {
                    for (int k = 0; k < gridDim[2]; k++)
                    {
                        pos[0] = (0.5 + i) * gridStep;
                        pos[1] = (0.5 + j) * gridStep;
                        pos[2] = (0.5 + k) * gridStep;

                        // transform to local
                        if (region != null)
                        {
                            locPos = region.AbsoluteToLocalPreserveScale(pos[0], pos[1], pos[2]);
                        }

                        eVal = 0.0;
                        for (int e = 0; e < 3; e++)
                        {
                            grad[e] = pos[e] - center[e];
                            if (locPos != null)
                            {
                                eVal += Math.Pow(locPos[e], 2.0);
                            }
                            else
                            {
                                eVal += Math.Pow(grad[e], 2.0);
                            }
                        }

                        // if locPos was calculated then use it to compute a normal direction; otherwise use the current gradient
                        if (locPos != null)
                        {
                            normal = locPos.Normalize();
                        }
                        else
                        {
                            normal = grad.Clone().Normalize();
                        }

                        dVal = 0.0;
                        for (int e = 0; e < 3; e++)
                        {
                            dVal += Math.Pow(normal[e] * width[e], 2.0);
                        }

                        if (dVal == 0.0)
                        {
                            fVal = solfControl.amplitude;
                        }
                        else
                        {
                            fVal = solfControl.amplitude * Math.Exp(-eVal / (2 * dVal));
                        }

                        // add the concentration
                        if (grid[i, j, k].Concentrations.ContainsKey(solf.mp_type_guid_ref) == false)
                        {
                            grid[i, j, k].Concentrations.Add(solf.mp_type_guid_ref, new Dictionary<string, double>());
                        }
                        grid[i, j, k].Concentrations[solf.mp_type_guid_ref].Add(solf.mp_guid, fVal);

                        // add the gradient
                        if (grid[i, j, k].Gradients.ContainsKey(solf.mp_type_guid_ref) == false)
                        {
                            grid[i, j, k].Gradients.Add(solf.mp_type_guid_ref, new Dictionary<string, Vector>());
                        }
                        grad *= -fVal;
                        for (int e = 0; e < 3; e++)
                        {
                            grad[e] /= width[e] * width[e];
                        }
                        grid[i, j, k].Gradients[solf.mp_type_guid_ref].Add(solf.mp_guid, grad.Clone());
                    }
                }
            }
        }
        
        /// <summary>
        /// insert a solfac into the chemokine using a custom, user-specified gradient
        /// </summary>
        /// <param name="solf">entity describing the solfac</param>
        /// <param name="solfControl">pointer to the solfac controller</param>
        /// <returns>true for success</returns>
        public bool populateSolfacCustom(MolPopInfo solf, ref SolfacTypeController solfControl)
        {
            double conc = 0, min = 0, max = 0;
            double[] grad = new double[] { 0, 0, 0 };
            string line;
            string[] pieces;
            System.IO.StreamReader file;

            try
            {
                file = new System.IO.StreamReader(((SolfacTypeCustomController)solfControl).datafile);
            }
            catch
            {
                return false;
            }

            // add concentrations and the gradient
            for (int i = 0; i < gridDim[0]; i++)
            {
                for (int j = 0; j < gridDim[1]; j++)
                {
                    for (int k = 0; k < gridDim[2]; k++)
                    {
                        line = file.ReadLine();
                        if (line == null)
                        {
                            return false;
                        }
                        pieces = line.Split(' ');
                        if(pieces.Length < 4)
                        {
                            return false;
                        }
                        conc = double.Parse(pieces[0]);
                        if (i == j && j == k && k == 0)
                        {
                            min = max = conc;
                        }
                        else if (conc > max)
                        {
                            max = conc;
                        }
                        else if (conc < min)
                        {
                            min = conc;
                        }
                        grad[0] = double.Parse(pieces[1]);
                        grad[1] = double.Parse(pieces[2]);
                        grad[2] = double.Parse(pieces[3]);

                        // add the concentration
                        if (grid[i, j, k].Concentrations.ContainsKey(solf.mp_type_guid_ref) == false)
                        {
                            grid[i, j, k].Concentrations.Add(solf.mp_type_guid_ref, new Dictionary<string, double>());
                        }
                        grid[i, j, k].Concentrations[solf.mp_type_guid_ref].Add(solf.mp_guid, conc);

                        // add the gradient
                        if (grid[i, j, k].Gradients.ContainsKey(solf.mp_type_guid_ref) == false)
                        {
                            grid[i, j, k].Gradients.Add(solf.mp_type_guid_ref, new Dictionary<string, Vector>());
                        }
                        grid[i, j, k].Gradients[solf.mp_type_guid_ref].Add(solf.mp_guid, new Vector(new double[] { grad[0], grad[1], grad[2] }));
                    }
                }
            }
            // set min/ max
            ((SolfacTypeCustomController)solfControl).min = min;
            ((SolfacTypeCustomController)solfControl).max = max;

            file.Close();
            return true;
        }

        private CKGridVoxel[,,] grid;
        private bool interpolate = true;
    }
}
