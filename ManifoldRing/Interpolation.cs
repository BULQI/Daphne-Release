﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// using MathNet.Numerics.LinearAlgebra;

namespace ManifoldRing
{
    /// <summary>
    /// interpolator interface
    /// </summary>
    public interface Interpolator
    {
        void Init(InterpolatedNodes m, bool toroidal);
        double Interpolate(double[] x, ScalarField sf);
        double[] Gradient(double[] x, ScalarField sf);
        ScalarField Laplacian(ScalarField sf);
        ScalarField DiffusionFlux(ScalarField flux, Transform t);
        ScalarField DirichletBC(ScalarField from, Transform t, ScalarField to);
    }

    public abstract class NodeInterpolator : Interpolator
    {
        protected bool toroidal;
        /// <summary>
        /// Return the sparse matrix with coefficients and indices for interpolation of scalar field at arbitrary position x.
        /// </summary>
        /// <param name="x">Spatial point for interpolation</param>
        /// <returns></returns>
        protected InterpolatedNodes m;
        // Used to create the value, gradient, and laplacian operators
        // We may be able to change access to 'protected' if Convert() is moved out of ScalarField
        protected abstract LocalMatrix[] interpolationMatrix(double[] x);
        /// <summary>
        /// Calculate sparse matrix for computing Laplacian at every grid point.
        /// Only computed once.
        /// Impose toroidal or zero flux BCs.
        /// Dirichlet or Neumann BCs will be imposed later, as needed.
        /// </summary>
        /// <returns></returns>
        protected abstract LocalMatrix[][] laplacianMatrix();
        /// <summary>
        /// Calculate sparse matrix for computing the gradient at arbitrary position x.
        /// </summary>
        /// <param name="x">Spatial point for gradient calculation</param>
        /// <returns></returns>
        protected abstract LocalMatrix[][] gradientMatrix(double[] x);

        // Used to compute value, gradient, and laplacian
        protected LocalMatrix[] interpolationOperator;
        protected LocalMatrix[][] gradientOperator;
        protected LocalMatrix[][] laplacianOperator;

        // computed gradient (at a point) and laplacian
        protected double[] gradient;
        protected ScalarField laplacian;
        public abstract double Integration(ScalarField sf);

        public NodeInterpolator()
        {
        }

        public virtual void Init(InterpolatedNodes m, bool _toroidal)
        {
            this.m = m;
            toroidal = _toroidal;
            laplacian = new ScalarField(m);
            laplacianOperator = new LocalMatrix[m.ArraySize][];
            laplacianOperator = laplacianMatrix();
            gradientOperator = new LocalMatrix[m.Dim][];
            gradient = new double[m.Dim];
        }

        public double Interpolate(double[] x, ScalarField sf)
        {
            LocalMatrix[] lm = interpolationMatrix(x);
            double value = 0;

            if (lm != null)
            {
                for (int i = 0; i < lm.Length; i++)
                {
                    value += lm[i].Coefficient * sf.array[lm[i].Index];
                }
            }
            return value;
        }

        public double[] Gradient(double[] x, ScalarField sf)
        {
            LocalMatrix[][] lm = gradientMatrix(x);

            if (lm != null)
            {
                for (int i = 0; i < m.Dim; i++)
                {
                    gradient[i] = 0.0;

                    for (int j = 0; j < lm[i].Length; j++)
                    {
                        gradient[i] += lm[i][j].Coefficient * sf.array[lm[i][j].Index];
                    }
                }
            }
            return gradient;
        }

        public ScalarField Laplacian(ScalarField sf)
        {
            for (int i = 0; i < sf.array.Length; i++)
            {
                laplacian.array[i] = 0.0;

                for (int j = 0; j < laplacianOperator[i].Length; j++)
                {
                    laplacian.array[i] += laplacianOperator[i][j].Coefficient * sf.array[laplacianOperator[i][j].Index];
                }
            }
            return laplacian;
        }

        public ScalarField DiffusionFlux(ScalarField flux, Transform t)
        {
            ScalarField temp = new ScalarField(m); 
            int n;

            for (int i = 0; i < flux.M.PrincipalPoints.Length; i++)
            {
                // Find the node in this manifold that is closest to the principal point
                n = m.indexArrayToLinearIndex(m.localToIndexArray(t.toContaining(flux.M.PrincipalPoints[i])));
                if (n >= 0 && n < temp.array.Length)
                {
                    temp.array[n] += 2 * flux.Value(flux.M.PrincipalPoints[i]) / m.StepSize();
                }
            }
            return temp;
        }

        /// <summary>
        /// Impose Dirichlet boundary conditions
        /// NOTE: This algorithm is best when there is a one-to-one correspondance between 
        /// boundary and interior manifold principla points (nodes). May not be as accurate
        /// when there is not a one-to-one correspondance.
        /// </summary>
        /// <param name="from">Field specified on the boundary manifold</param>
        /// <param name="t">Transform that specifies the geometric relationship between 
        /// the boundary and interior manifolds </param>
        /// <param name="to">Field specified on the interior manifold</param>
        /// <returns>The field after imposing Dirichlet boundary conditions</returns>
        public ScalarField DirichletBC(ScalarField from, Transform t, ScalarField sf)
        {
            int n;
            for (int i = 0; i < from.M.PrincipalPoints.Length; i++)
            {
                // Find the node in this manifold that is closest to the principal point
                n = m.indexArrayToLinearIndex(m.localToIndexArray(t.toContaining(from.M.PrincipalPoints[i])));
                if (n >= 0 && n < sf.array.Length)
                {
                    sf.array[n] = from.Value(from.M.PrincipalPoints[i]);
                }
            }
            return sf;
        }
    }

    /// <summary>
    /// Trilinear 3D interpolation
    /// </summary>
    public class Trilinear3D : NodeInterpolator
    {
        public Trilinear3D() : base()
        {
            interpolationOperator = new LocalMatrix[8];
        }

        public override void Init(InterpolatedNodes m, bool _toroidal)
        {
            base.Init(m, _toroidal);
            for (int i = 0; i < m.Dim; i++)
            {
                gradientOperator[i] = new LocalMatrix[8];
            }
        }

        // Don't need to account for toroidal BCs with this low-order scheme. 
        protected override LocalMatrix[] interpolationMatrix(double[] x)
        {
            int[] idx = m.localToIndexArray(x);

            if (idx[0] == m.NodesPerSide(0) - 1)
            {
                idx[0]--;
            }
            if (idx[1] == m.NodesPerSide(1) - 1)
            {
                idx[1]--;
            }
            if (idx[2] == m.NodesPerSide(2) - 1)
            {
                idx[2]--;
            }

            double dx = x[0] / m.StepSize() - idx[0],
                   dy = x[1] / m.StepSize() - idx[1],
                   dz = x[2] / m.StepSize() - idx[2],
                   dxmult, dymult, dzmult;

            int n = 0;

            for (int di = 0; di < 2; di++)
            {
                for (int dj = 0; dj < 2; dj++)
                {
                    for (int dk = 0; dk < 2; dk++)
                    {
                        dxmult = di == 0 ? (1 - dx) : dx;
                        dymult = dj == 0 ? (1 - dy) : dy;
                        dzmult = dk == 0 ? (1 - dz) : dz;
                        interpolationOperator[n].Index = (idx[0] + di) + (idx[1] + dj) * m.NodesPerSide(0) + (idx[2] + dk) * m.NodesPerSide(0) * m.NodesPerSide(1);
                        interpolationOperator[n].Coefficient = dxmult * dymult * dzmult;
                        n++;
                    }
                }
            }
            return interpolationOperator;
        }
        
        // Don't need to account for toroidal BCs with this low-order scheme. 
        protected override LocalMatrix[][] gradientMatrix(double[] x)
        {
            int[] idx = m.localToIndexArray(x);

            if (idx[0] == m.NodesPerSide(0) - 1)
            {
                idx[0]--;
            }
            if (idx[1] == m.NodesPerSide(1) - 1)
            {
                idx[1]--;
            }
            if (idx[2] == m.NodesPerSide(2) - 1)
            {
                idx[2]--;
            }

            double dx = x[0] / m.StepSize() - idx[0],
                   dy = x[1] / m.StepSize() - idx[1],
                   dz = x[2] / m.StepSize() - idx[2],
                   dxmult, dymult, dzmult;

            int n = 0;
            for (int di = 0; di < 2; di++)
            {
                for (int dj = 0; dj < 2; dj++)
                {
                    for (int dk = 0; dk < 2; dk++)
                    {
                        // 0th element:
                        dxmult = (-1 + 2*di )/m.StepSize();
                        dymult = dj == 0 ? (1 - dy) : dy;
                        dzmult = dk == 0 ? (1 - dz) : dz;
                        gradientOperator[0][n].Index = (idx[0] + di) + (idx[1] + dj) * m.NodesPerSide(0) + (idx[2] + dk) * m.NodesPerSide(0) * m.NodesPerSide(1);
                        gradientOperator[0][n].Coefficient = dxmult * dymult * dzmult;
                    
                        // 1st element:
                        dxmult = di == 0 ? (1 - dx) : dx;
                        dymult = (-1 + 2*dj )/m.StepSize();
                        dzmult = dk == 0 ? (1 - dz) : dz;
                        gradientOperator[1][n].Index = (idx[0] + di) + (idx[1] + dj) * m.NodesPerSide(0) + (idx[2] + dk) * m.NodesPerSide(0) * m.NodesPerSide(1);
                        gradientOperator[1][n].Coefficient = dxmult * dymult * dzmult;
                        
                        // 2nd element:
                        dxmult = di == 0 ? (1 - dx) : dx;
                        dymult = dj == 0 ? (1 - dy) : dy;
                        dzmult = (-1 + 2*dk )/m.StepSize();
                        gradientOperator[2][n].Index = (idx[0] + di) + (idx[1] + dj) * m.NodesPerSide(0) + (idx[2] + dk) * m.NodesPerSide(0) * m.NodesPerSide(1);
                        gradientOperator[2][n].Coefficient = dxmult * dymult * dzmult;
                        
                        n++;
                    }
                }
            }

            return gradientOperator;
        }

        protected override LocalMatrix[][] laplacianMatrix()
        {
            for (int i = 0; i < m.ArraySize; i++)
            {
                laplacianOperator[i] = new LocalMatrix[7];
            }

            int n = 0;

            int idxplus, idxminus;
            double coeff0, coeff;
            int N01 = m.NodesPerSide(0) * m.NodesPerSide(1);

            for (int k = 0; k < m.NodesPerSide(2); k++)
            {
                for (int j = 0; j < m.NodesPerSide(1); j++)
                {
                    for (int i = 0; i < m.NodesPerSide(0); i++)
                    {
                        // Laplacian index n corresponds to grid indices (i,j,k)

                        laplacianOperator[n][0].Coefficient = 0;
                        laplacianOperator[n][0].Index = i + j * m.NodesPerSide(0) + k * N01;


                        coeff = 1.0 / (m.StepSize() * m.StepSize());
                        coeff0 = -2.0 * coeff;

                        if (i == 0)
                        {
                            idxplus = (i + 1) + j * m.NodesPerSide(0) + k * N01;
                            idxminus = toroidal ? (m.NodesPerSide(0) - 2) + j * m.NodesPerSide(0) + k * N01 : idxplus;
                        }
                        else if (i == m.NodesPerSide(0) - 1)
                        {
                            idxminus = (i - 1) + j * m.NodesPerSide(0) + k * N01;
                            idxplus = toroidal ? 1 + j * m.NodesPerSide(0) + k * N01 : idxminus;
                        }
                        else
                        {
                            idxplus = (i + 1) + j * m.NodesPerSide(0) + k * N01;
                            idxminus = (i - 1) + j * m.NodesPerSide(0) + k * N01;
                        }

                        // (i+1), j, k
                        laplacianOperator[n][1].Coefficient = coeff;
                        laplacianOperator[n][1].Index = idxplus;

                        // (i-1), j, k
                        laplacianOperator[n][2].Coefficient = coeff;
                        laplacianOperator[n][2].Index = idxminus;

                        // i,j,k
                        laplacianOperator[n][0].Coefficient = laplacianOperator[n][0].Coefficient + coeff0;

                        if (j == 0)
                        {
                            idxplus = i + (j + 1) * m.NodesPerSide(0) + k * N01;
                            idxminus = toroidal ? i + (m.NodesPerSide(1) - 2) * m.NodesPerSide(0) + k * N01 : idxplus;
                        }
                        else if (j == m.NodesPerSide(1) - 1)
                        {
                            idxminus = i + (j - 1) * m.NodesPerSide(0) + k * N01;
                            idxplus = toroidal ? i + 1 * m.NodesPerSide(0) + k * N01 : idxminus;
                        }
                        else
                        {
                            idxplus = i + (j + 1) * m.NodesPerSide(0) + k * N01;
                            idxminus = i + (j - 1) * m.NodesPerSide(0) + k * N01;
                        }

                        // i, (j+1), k
                        laplacianOperator[n][3].Coefficient = coeff;
                        laplacianOperator[n][3].Index = idxplus;

                        // i, (j-1), k
                        laplacianOperator[n][4].Coefficient = coeff;
                        laplacianOperator[n][4].Index = idxminus;

                        // i,j,k
                        laplacianOperator[n][0].Coefficient = laplacianOperator[n][0].Coefficient + coeff0;

                        if (k == 0)
                        {
                            idxplus = i + k * m.NodesPerSide(0) + (k + 1) * N01;
                            idxminus = toroidal ? i + j * m.NodesPerSide(0) + (m.NodesPerSide(1) - 2) * N01 : idxplus;
                        }
                        else if (k == m.NodesPerSide(2) - 1)
                        {
                            idxminus = i + j * m.NodesPerSide(0) + (k - 1) * N01;
                            idxplus = toroidal ? i + j * m.NodesPerSide(0) + 1 * N01 : idxminus;
                        }
                        else
                        {
                            idxplus = i + j * m.NodesPerSide(0) + (k + 1) * N01;
                            idxminus = i + j * m.NodesPerSide(0) + (k - 1) * N01;
                        }

                        // i, j, (k+1)
                        laplacianOperator[n][5].Coefficient = coeff;
                        laplacianOperator[n][5].Index = idxplus;

                        // i, j, (k-1)
                        laplacianOperator[n][6].Coefficient = coeff;
                        laplacianOperator[n][6].Index = idxminus;

                        // i,j,k
                        laplacianOperator[n][0].Coefficient = laplacianOperator[n][0].Coefficient + coeff0;

                        n++;
                    }
                }
            }
            return laplacianOperator;
        }

        public override double Integration(ScalarField sf)
        {
            return 0.0;
        }
    }


    /// <summary>
    /// Trilinear 2D interpolation
    /// </summary>
    public class Trilinear2D : NodeInterpolator
    {

        public Trilinear2D()
            : base()
        {
            interpolationOperator = new LocalMatrix[4];
        }

        public override void  Init(InterpolatedNodes m, bool _toroidal)
        {
 	         base.Init(m, _toroidal);
            // laplacianOperator = laplacianMatrix();
            for (int i = 0; i < m.Dim; i++)
            {
                gradientOperator[i] = new LocalMatrix[2];
            }
        }

        // Don't need to account for toroidal BCs with this low-order scheme. 
        protected override LocalMatrix[] interpolationMatrix(double[] x)
        {
            int[] idx = m.localToIndexArray(x);

            if (idx[0] == m.NodesPerSide(0) - 1)
            {
                idx[0]--;
            }
            if (idx[1] == m.NodesPerSide(1) - 1)
            {
                idx[1]--;
            }

            double dx = x[0] / m.StepSize() - idx[0],
                   dy = x[1] / m.StepSize() - idx[1],
                   dxmult, dymult;
            int n = 0;

            for (int di = 0; di < 2; di++)
            {
                for (int dj = 0; dj < 2; dj++)
                {
                    dxmult = di == 0 ? (1 - dx) : dx;
                    dymult = dj == 0 ? (1 - dy) : dy;
                    interpolationOperator[n].Index = (idx[0] + di) + (idx[1] + dj) * m.NodesPerSide(0);
                    interpolationOperator[n].Coefficient = dxmult * dymult;
                    n++;
                }
            }
            return interpolationOperator;
        }

        // Don't need to account for toroidal BCs with this low-order scheme. 
        protected override LocalMatrix[][] gradientMatrix(double[] x)
        {
            int[] idx = m.localToIndexArray(x);

            if (idx[0] == m.NodesPerSide(0) - 1)
            {
                idx[0]--;
            }
            if (idx[1] == m.NodesPerSide(1) - 1)
            {
                idx[1]--;
            }

            double dx = x[0] / m.StepSize() - idx[0],
                   dy = x[1] / m.StepSize() - idx[1],
                   dxmult, dymult;
            int n = 0;
            int[] di = new int[2], dj = new int[2];

            for (int i = 0; i < 2; i++)
            {
                for (int d = 0; d < 2; d++)
                {
                    // x-direction
                    if (i == 0)
                    {
                        // interpolation multipliers
                        dxmult = 1;
                        dymult = d == 0 ? (1 - dy) : dy;
                        // index differences
                        di[0] = 1;
                        di[1] = 0;
                        dj[0] = d;
                        dj[1] = d;
                    }
                    else if (i == 1) // y-direction
                    {
                        // interpolation multipliers
                        dxmult = d == 0 ? (1 - dx) : dx;
                        dymult = 1;
                        // index differences
                        di[0] = d;
                        di[1] = d;
                        dj[0] = 1;
                        dj[1] = 0;
                    }
                    else // z-direction
                    {
                        // interpolation multipliers
                        dxmult = d == 0 ? (1 - dx) : dx;
                        dymult = d == 0 ? (1 - dy) : dy;
                        // index differences
                        di[0] = d;
                        di[1] = d;
                        dj[0] = d;
                        dj[1] = d;
                    }

                    gradientOperator[i][n].Index = (idx[0] + di[0]) + (idx[1] + dj[0]) * m.NodesPerSide(0);
                    gradientOperator[i][n].Coefficient = dxmult * dymult / m.StepSize();
                    n++;

                    gradientOperator[i][n].Index = (idx[0] + di[1]) + (idx[1] + dj[1]) * m.NodesPerSide(0);
                    gradientOperator[i][n].Coefficient = -dxmult * dymult / m.StepSize();
                    n++;

                }
            }
            return gradientOperator;
        }

        protected override LocalMatrix[][] laplacianMatrix()
        {
            for (int i = 0; i < m.ArraySize; i++)
            {
                laplacianOperator[i] = new LocalMatrix[5];
            }

            int n = 0;

            int idxplus, idxminus;
            double coeff0, coeff;

            for (int j = 0; j < m.NodesPerSide(1); j++)
            {
                for (int i = 0; i < m.NodesPerSide(0); i++)
                {
                    // Laplacian index n corresponds to grid indices (i,j,k)

                    laplacianOperator[n][0].Coefficient = 0;
                    laplacianOperator[n][0].Index = i + j * m.NodesPerSide(0);


                    coeff = 1.0 / (m.StepSize() * m.StepSize());
                    coeff0 = -2.0 * coeff;

                    if (i == 0)
                    {
                        idxplus = (i + 1) + j * m.NodesPerSide(0);
                        idxminus = toroidal ? m.NodesPerSide(0) - 2 : idxplus;
                    }
                    else if (i == m.NodesPerSide(0) - 1)
                    {
                        idxminus = (i - 1) + j * m.NodesPerSide(0);
                        idxplus = toroidal ? 1 : idxminus;
                    }
                    else
                    {
                        idxplus = (i + 1) + j * m.NodesPerSide(0);
                        idxminus = (i - 1) + j * m.NodesPerSide(0);
                    }

                    // (i+1), j
                    laplacianOperator[n][1].Coefficient = coeff;
                    laplacianOperator[n][1].Index = idxplus;

                    // (i-1), j
                    laplacianOperator[n][2].Coefficient = coeff;
                    laplacianOperator[n][2].Index = idxminus;

                    // i,j
                    laplacianOperator[n][0].Coefficient = laplacianOperator[n][0].Coefficient + coeff0;

                    if (j == 0)
                    {
                        idxplus = i + (j + 1) * m.NodesPerSide(0);
                        idxminus = toroidal ? i + (m.NodesPerSide(1) - 2) * m.NodesPerSide(0) : idxplus;
                    }
                    else if (j == m.NodesPerSide(1) - 1)
                    {
                        idxminus = i + (j - 1) * m.NodesPerSide(0);
                        idxplus = toroidal ? i + 1 * m.NodesPerSide(0) : idxminus;
                        idxplus = idxminus;
                    }
                    else
                    {
                        idxplus = i + (j + 1) * m.NodesPerSide(0);
                        idxminus = i + (j - 1) * m.NodesPerSide(0);
                    }

                    // i, (j+1)
                    laplacianOperator[n][3].Coefficient = coeff;
                    laplacianOperator[n][3].Index = idxplus;

                    // i, (j-1)
                    laplacianOperator[n][4].Coefficient = coeff;
                    laplacianOperator[n][4].Index = idxminus;

                    // i,j
                    laplacianOperator[n][0].Coefficient = laplacianOperator[n][0].Coefficient + coeff0;

                    n++;
                }
            }
            return laplacianOperator;
        }

        public override double Integration(ScalarField sf)
        {
            throw new NotImplementedException();
        }
    }


    /// <summary>
    /// Tricubic 3D interpolation.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="m"></param>
    /// <returns></returns>
    public class Tricubic3D : NodeInterpolator
    {
        public Tricubic3D()
            : base()
        {
            throw new NotImplementedException();
            // interpolator = new LocalMatrix[27];
        }

        public override void Init(InterpolatedNodes m, bool _toroidal)
        {
            base.Init(m, _toroidal);
        }

        public override double Integration(ScalarField sf)
        {
            throw new NotImplementedException();
        }

        protected override LocalMatrix[] interpolationMatrix(double[] x)
        {
            throw new NotImplementedException();
            //return interpolator;
        }

        protected override LocalMatrix[][] gradientMatrix(double[] x)
        {
            throw new NotImplementedException();
            //return gradient;
        }

        protected override LocalMatrix[][] laplacianMatrix()
        {
            throw new NotImplementedException();
            // return laplacian;
        }
    }
    
    /// <summary>
    /// Tricubic 2D interpolation.
    /// </summary>
    /// <param name="x"></param>
    /// <param name="m"></param>
    /// <returns></returns>
    public class Tricubic2D : NodeInterpolator
    {
        public Tricubic2D()
            : base()
        {
            throw new NotImplementedException();
            // interpolator = new LocalMatrix[27];
        }

        public override void Init(InterpolatedNodes m, bool _toroidal)
        {
            base.Init(m, _toroidal);
        }

        public override double Integration(ScalarField sf)
        {
            throw new NotImplementedException();
        }

        protected override LocalMatrix[] interpolationMatrix(double[] x)
        {
            throw new NotImplementedException();
            //return interpolator;
        }

        protected override LocalMatrix[][] gradientMatrix(double[] x)
        {
            throw new NotImplementedException();
            //return gradient;
        }

        protected override LocalMatrix[][] laplacianMatrix()
        {
            throw new NotImplementedException();
            // return laplacian;
        }
    }
}
