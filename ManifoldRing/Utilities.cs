using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//using MathNet.Numerics.LinearAlgebra;
using MathNet.Numerics.LinearAlgebra.Double;
using NativeDaphne;
namespace ManifoldRing
{
    /// <summary>
    /// container for factories to keep them central
    /// not every instance of a class should need its own factory
    /// </summary>
    public class FactoryContainer
    {
        public static IFieldInitializerFactory fieldInitFactory;

        public FactoryContainer(IFieldInitializerFactory fif)
        {
            fieldInitFactory = fif;
        }
    }


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
        private Nt_Darray darray;
        //private Vector pos;
        private Matrix rot;
        private double[] position; //storage for pos data
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
            position = new double[Dim];
            darray = new Nt_Darray(Dim);
            //pos = new DenseVector(Dim);
            this.HasRot = hasRot;

            if (HasRot == true)
            {
                // set up the rotation to be aligned with the canonical world coordinates
                rot = new DenseMatrix(Dim, Dim, new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1 });
            }
        }


        //allow outside access to array data without using translation.toArray() - axin
        public double[] Position
        {
            get
            {
                for (int i = 0; i < Dim; i++)
                {
                    position[i] = darray[i];
                }
                return position;
            }
        }


        /// <summary>
        /// retrieve the translation component
        /// </summary>
        public Nt_Darray Translation
        {
            get { return darray; }
            set
            {
                if (darray.Length != value.Length)
                {
                    throw new Exception("Dimension mismatch.");
                }

                darray[0] = value[0]; darray[1] = value[1]; darray[2] = value[2];
            }
        }

        public void setTranslationByReference(Nt_Darray x)
        {
            if (darray.Length != x.Length)
            {
                throw new Exception("Dimension mismatch.");
            }
            darray = x;
        }

        /// <summary>
        /// make the tranlation point to an external position vector; will stay in synch
        /// </summary>
        /// <param name="x">the position vector to synch with</param>
        //public void setTranslationByReference(Vector x)
        //{
        //    if (pos.Count != x.Count)
        //    {
        //        throw new Exception("Dimension mismatch.");
        //    }
        //    pos = x;
        //}

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
            if (darray.Length != x.Count)
            {
                throw new Exception("Dimension mismatch.");
            }
            for (int i = 0; i < darray.Length; i++)
            {
                darray[i] += x[i];
            }
            //pos = (DenseVector)(pos + x);
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
                if (axis.Count != Dim)
                {
                    throw new Exception("Dimension mismatch.");
                }

                Matrix tmp = new DenseMatrix(Dim, Dim);

                // make sure the axis is normalized
                axis = (DenseVector)axis.Normalize(2.0);
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

                rot = (Matrix)rot.Multiply(tmp);
            }
        }

        /// <summary>
        /// convert the argument into the parent frame
        /// </summary>
        /// <param name="x">vector to be converted</param>
        /// <returns>the resulting vector in the parent frame</returns>
        public Vector toContaining(Vector x)
        {
            if (x.Count != Dim)
            {
                throw new Exception("Dimension mismatch.");
            }

            if (HasRot == true)
            {
                Matrix tmp = new DenseMatrix(Dim, 1);

                tmp[0, 0] = x[0];
                tmp[1, 0] = x[1];
                tmp[2, 0] = x[2];

                tmp = (DenseMatrix)rot.Multiply(tmp);

                tmp[0, 0] += darray[0];
                tmp[1, 0] += darray[1];
                tmp[2, 0] += darray[2];

                return (Vector)tmp.Column(0);
            }
            else
            {
                Vector tmp = new DenseVector(Dim);
                for (int i = 0; i < tmp.Count; i++)
                {
                    tmp[i] = x[i] + darray[i];
                }
                return tmp;
                //return (Vector)(x + pos);
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
                if (x.Count != Dim)
                {
                    throw new Exception("Dimension mismatch.");
                }

                Matrix tmp = new DenseMatrix(Dim, 1);

                tmp[0, 0] = x[0];
                tmp[1, 0] = x[1];
                tmp[2, 0] = x[2];

                tmp[0, 0] -= darray[0];
                tmp[1, 0] -= darray[1];
                tmp[2, 0] -= darray[2];

                tmp = (Matrix)rot.Inverse().Multiply(tmp);

                return (Vector)tmp.Column(0);
            }
            else
            {
                Vector tmp = new DenseVector(Dim);
                for (int i = 0; i < tmp.Count; i++)
                {
                    tmp[i] = x[i] - darray[i];
                }
                return tmp;

                //return (Vector)(x - pos);
            }
        }
    }
}
