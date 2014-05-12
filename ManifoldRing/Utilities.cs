using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using MathNet.Numerics.LinearAlgebra;

namespace ManifoldRing
{
    /// <summary>
    /// LocalMatrix is a struct to facilitate local matrix algebra on a lattice by providing an efficient
    /// representation of a sparse matrix. 
    /// </summary>
    public struct LocalMatrix
    {
        public int Index;
        public double Coefficient;
    }

    /// <summary>
    /// describes the position and orientation of an object;
    /// the rotation matrix denotes a coordinate frame (x, y, z); always 3d
    /// </summary>
    public class Transform
    {
        private Vector pos;
        private Matrix rot;
        /// <summary>
        /// true when rotation is present
        /// </summary>
        public bool HasRot { get; private set; }
        public static int Dim = 3;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="hasRot">true when the transform has rotation</param>
        public Transform(bool hasRot = true)
        {
            pos = new Vector(Dim);
            this.HasRot = hasRot;

            if (HasRot == true)
            {
                // set up the rotation to be aligned with the canonical world coordinates
                Vector tmp = new double[] { 1, 0, 0 };

                rot = new Matrix(Dim, Dim);

                // 1 0 0
                rot.SetColumnVector(tmp, 0);
                tmp[0] = 0;
                tmp[1] = 1;
                // 0 1 0
                rot.SetColumnVector(tmp, 1);
                tmp[1] = 0;
                tmp[2] = 1;
                // 0 0 1
                rot.SetColumnVector(tmp, 2);
            }
        }

        /// <summary>
        /// retrieve the translation component
        /// </summary>
        public Vector Translation
        {
            get { return pos; }
            set
            {
                if (pos.Length != value.Length)
                {
                    throw new Exception("Dimension mismatch.");
                }

                pos[0] = value[0]; pos[1] = value[1]; pos[2] = value[2];
            }
        }

        /// <summary>
        /// make the tranlation point to an external position vector; will stay in synch
        /// </summary>
        /// <param name="x">the position vector to synch with</param>
        public void setTranslationByReference(Vector x)
        {
            if (pos.Length != x.Length)
            {
                throw new Exception("Dimension mismatch.");
            }

            pos = x;
        }

        /// <summary>
        /// retrieve the rotation component
        /// </summary>
        public Matrix Rotation
        {
            get { return rot; }
            set
            {
                if (HasRot == true)
                {
                    if (rot.ColumnCount != value.ColumnCount || rot.RowCount != value.RowCount)
                    {
                        throw new Exception("Dimension mismatch.");
                    }

                    rot[0, 0] = value[0, 0]; rot[1, 0] = value[1, 0]; rot[2, 0] = value[2, 0];
                    rot[0, 1] = value[0, 1]; rot[1, 1] = value[1, 1]; rot[2, 1] = value[2, 1];
                    rot[0, 2] = value[0, 2]; rot[1, 2] = value[1, 2]; rot[2, 2] = value[2, 2];
                }
            }
        }

        /// <summary>
        /// make the rotation matrix point to an external rotation matrix; will stay in synch
        /// </summary>
        /// <param name="m">the rotation matrix to synch with</param>
        public void setRotationByReference(Matrix m)
        {
            if (HasRot == true)
            {
                if (rot.ColumnCount != m.ColumnCount || rot.RowCount != m.RowCount)
                {
                    throw new Exception("Dimension mismatch.");
                }

                rot = m;
            }
        }

        /// <summary>
        /// translate by x
        /// </summary>
        /// <param name="x">delta x</param>
        public void translate(Vector x)
        {
            if (pos.Length != x.Length)
            {
                throw new Exception("Dimension mismatch.");
            }

            pos += x;
        }

        /// <summary>
        /// rotate by rad about axis
        /// </summary>
        /// <param name="axis">rotation axis</param>
        /// <param name="rad">rotation angle in radians</param>
        public void rotate(Vector axis, double rad)
        {
            if (HasRot == true)
            {
                if (axis.Length != Dim)
                {
                    throw new Exception("Dimension mismatch.");
                }

                Matrix tmp = new Matrix(Dim, Dim);

                // make sure the axis is normalized
                axis = axis.Normalize();
                // column 0
                tmp[0, 0] = Math.Cos(rad) + axis[0] * axis[0] * (1 - Math.Cos(rad));
                tmp[1, 0] = axis[1] * axis[0] * (1 - Math.Cos(rad)) + axis[2] * Math.Sin(rad);
                tmp[2, 0] = axis[2] * axis[0] * (1 - Math.Cos(rad)) - axis[1] * Math.Sin(rad);

                // column 1
                tmp[0, 1] = axis[0] * axis[1] * (1 - Math.Cos(rad)) - axis[2] * Math.Sin(rad);
                tmp[1, 1] = Math.Cos(rad) + axis[1] * axis[1] * (1 - Math.Cos(rad));
                tmp[2, 1] = axis[2] * axis[1] * (1 - Math.Cos(rad)) + axis[0] * Math.Sin(rad);

                // column 2
                tmp[0, 2] = axis[0] * axis[2] * (1 - Math.Cos(rad)) + axis[1] * Math.Sin(rad);
                tmp[1, 2] = axis[1] * axis[2] * (1 - Math.Cos(rad)) - axis[0] * Math.Sin(rad);
                tmp[2, 2] = Math.Cos(rad) + axis[2] * axis[2] * (1 - Math.Cos(rad));

                rot = rot.Multiply(tmp);
            }
        }

        /// <summary>
        /// convert the argument into the parent frame
        /// </summary>
        /// <param name="x">vector to be converted</param>
        /// <returns>the resulting vector in the parent frame</returns>
        public Vector toContaining(Vector x)
        {
            if (x.Length != Dim)
            {
                throw new Exception("Dimension mismatch.");
            }

            if (HasRot == true)
            {
                Matrix tmp = new Matrix(Dim, 1);

                tmp[0, 0] = x[0];
                tmp[1, 0] = x[1];
                tmp[2, 0] = x[2];

                tmp = rot.Multiply(tmp);

                tmp[0, 0] += pos[0];
                tmp[1, 0] += pos[1];
                tmp[2, 0] += pos[2];

                return tmp.GetColumnVector(0);
            }
            else
            {
                return x + pos;
            }
        }

        /// <summary>
        /// convert the argument into the local frame
        /// </summary>
        /// <param name="x">vector to be converted</param>
        /// <returns>the resulting vector in the local frame</returns>
        public Vector toLocal(Vector x)
        {
            if (HasRot == true)
            {
                if (x.Length != Dim)
                {
                    throw new Exception("Dimension mismatch.");
                }

                Matrix tmp = new Matrix(Dim, 1);

                tmp[0, 0] = x[0];
                tmp[1, 0] = x[1];
                tmp[2, 0] = x[2];

                tmp[0, 0] -= pos[0];
                tmp[1, 0] -= pos[1];
                tmp[2, 0] -= pos[2];

                tmp = rot.Inverse().Multiply(tmp);

                return tmp.GetColumnVector(0);
            }
            else
            {
                return x - pos;
            }
        }

        // Specify type of boundary condition at boundary manifold
        // Neumann: specify flux at boundary
        // Dirichlet: specify value at boundary
        // Zero flux is the default boundary condition when Neumann=false and Dirichlet=false
        public bool Neumann = false;
        public bool Dirichlet = false;

    }
}
