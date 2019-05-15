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
#pragma once

#include "Nt_Darray.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;
using namespace System::Linq;
using namespace System::Text;
using namespace MathNet::Numerics::LinearAlgebra::Double;
using namespace NativeDaphne;

namespace Nt_ManifoldRing
{
	//forward declear
	interface class IFieldInitializerFactory;

    /// <summary>
    /// container for factories to keep them central
    /// not every instance of a class should need its own factory
    /// </summary>
    public ref class FactoryContainer
    {
	public:
		static IFieldInitializerFactory^ fieldInitFactory;

		FactoryContainer(IFieldInitializerFactory^ fif)
        {
            fieldInitFactory = fif;
        }
    };


    /// <summary>
    /// LocalMatrix is a struct to facilitate local matrix algebra on a lattice by providing an efficient
    /// representation of a sparse matrix. 
    /// </summary>
    public value struct LocalMatrix
    {
	public:
		int Index;
        double Coefficient;
    };

    /// <summary>
    /// describes the position and orientation of an object;
    /// the rotation matrix denotes a coordinate frame (x, y, z); always 3d
    /// </summary>
    public ref class Transform
    {
	private:
		Nt_Darray^ darray;

		bool hasRot;
        //private Vector pos;
        Matrix^ rot;
		array<double>^ position; //storage for pos data

	public:
        /// <summary>
        /// true when rotation is present
        /// </summary>
        property bool HasRot
		{ 
			bool get()
			{
				return hasRot;
			}
		private:
			void set(bool value)
			{
				hasRot = value;
			}
		}


		static int Dim = 3;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="hasRot">true when the transform has rotation</param>
		Transform()
        {
            position = gcnew array<double>(Dim);
            darray = gcnew Nt_Darray(Dim);
            this->HasRot = true;
            // set up the rotation to be aligned with the canonical world coordinates
            rot = gcnew DenseMatrix(Dim, Dim, gcnew array<double>{ 1, 0, 0, 0, 1, 0, 0, 0, 1 });
        }

		Transform(bool hasRot)
        {
            position = gcnew array<double>(Dim);
            darray = gcnew Nt_Darray(Dim);
            //pos = gcnew DenseVector(Dim);
            this->HasRot = hasRot;

            if (HasRot == true)
            {
                // set up the rotation to be aligned with the canonical world coordinates
                rot = gcnew DenseMatrix(Dim, Dim, gcnew array<double>{ 1, 0, 0, 0, 1, 0, 0, 0, 1 });
            }
        }
        //allow outside access to array data without using translation.toArray() - axin
		property array<double>^ Position
        {
            array<double>^ get()
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
		property Nt_Darray^ Translation
        {
            Nt_Darray^ get()
			{ 
				return  darray; 
			}
            void set(Nt_Darray^ value)
            {
                if (darray->Length != value->Length)
                {
                    throw gcnew Exception("Dimension mismatch.");
                }

                darray[0] = value[0]; darray[1] = value[1]; darray[2] = value[2];
            }
        }

		void setTranslationByReference(Nt_Darray^ x)
        {
            if (darray->Length != x->Length)
            {
                throw gcnew Exception("Dimension mismatch.");
            }
            darray = x;
        }

        /// <summary>
        /// retrieve the rotation component
        /// </summary>
		property Matrix^ Rotation
        {
            Matrix^ get()
			{ 
				return rot; 
			}
            void set(Matrix^ value)
            {
                if (HasRot == true)
                {
                    if (rot->ColumnCount != value->ColumnCount || rot->RowCount != value->RowCount)
                    {
                        throw gcnew Exception("Dimension mismatch.");
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
        void setRotationByReference(Matrix^ m)
        {
            if (HasRot == true)
            {
                if (rot->ColumnCount != m->ColumnCount || rot->RowCount != m->RowCount)
                {
                    throw gcnew Exception("Dimension mismatch.");
                }

                rot = m;
            }
        }

        /// <summary>
        /// translate by x
        /// </summary>
        /// <param name="x">delta x</param>
		void translate(Vector^ x)
        {
            if (darray->Length != x->Count)
            {
                throw gcnew Exception("Dimension mismatch.");
            }
            for (int i = 0; i < darray->Length; i++)
            {
                darray[i] += x[i];
            }
        }

        /// <summary>
        /// rotate by rad about axis
        /// </summary>
        /// <param name="axis">rotation axis</param>
        /// <param name="rad">rotation angle in radians</param>
		void rotate(Vector^ axis, double rad)
        {
            if (HasRot == true)
            {
                if (axis->Count != Dim)
                {
                    throw gcnew Exception("Dimension mismatch.");
                }

                Matrix^ tmp = gcnew DenseMatrix(Dim, Dim);

                // make sure the axis is normalized
                axis = dynamic_cast<DenseVector^>(axis->Normalize(2.0)); //(DenseVector)axis->Normalize(2.0);
                // column 0
                tmp[0, 0] = Math::Cos(rad) + axis[0] * axis[0] * (1 - Math::Cos(rad));
                tmp[1, 0] = axis[1] * axis[0] * (1 - Math::Cos(rad)) + axis[2] * Math::Sin(rad);
                tmp[2, 0] = axis[2] * axis[0] * (1 - Math::Cos(rad)) - axis[1] * Math::Sin(rad);

                // column 1
                tmp[0, 1] = axis[0] * axis[1] * (1 - Math::Cos(rad)) - axis[2] * Math::Sin(rad);
                tmp[1, 1] = Math::Cos(rad) + axis[1] * axis[1] * (1 - Math::Cos(rad));
                tmp[2, 1] = axis[2] * axis[1] * (1 - Math::Cos(rad)) + axis[0] * Math::Sin(rad);

                // column 2
                tmp[0, 2] = axis[0] * axis[2] * (1 - Math::Cos(rad)) + axis[1] * Math::Sin(rad);
                tmp[1, 2] = axis[1] * axis[2] * (1 - Math::Cos(rad)) - axis[0] * Math::Sin(rad);
                tmp[2, 2] = Math::Cos(rad) + axis[2] * axis[2] * (1 - Math::Cos(rad));

                rot = (Matrix^)rot->Multiply(tmp);
            }
        }

        /// <summary>
        /// convert the argument into the parent frame
        /// </summary>
        /// <param name="x">vector to be converted</param>
        /// <returns>the resulting vector in the parent frame</returns>
		Vector^ toContaining(Vector^ x)
        {
            if (x->Count != Dim)
            {
                throw gcnew Exception("Dimension mismatch.");
            }

            if (HasRot == true)
            {
                Matrix^ tmp = gcnew DenseMatrix(Dim, 1);

                tmp[0, 0] = x[0];
                tmp[1, 0] = x[1];
                tmp[2, 0] = x[2];

                tmp = dynamic_cast<DenseMatrix^>(rot->Multiply(tmp));

                tmp[0, 0] += darray[0];
                tmp[1, 0] += darray[1];
                tmp[2, 0] += darray[2];

                return dynamic_cast<Vector^>(tmp->Column(0));
            }
            else
            {
                Vector^ tmp = gcnew DenseVector(Dim);
                for (int i = 0; i < tmp->Count; i++)
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
		Vector^ toLocal(Vector^ x)
        {
            if (HasRot == true)
            {
                if (x->Count != Dim)
                {
                    throw gcnew Exception("Dimension mismatch.");
                }

                Matrix^ tmp = gcnew DenseMatrix(Dim, 1);

                tmp[0, 0] = x[0];
                tmp[1, 0] = x[1];
                tmp[2, 0] = x[2];

                tmp[0, 0] -= darray[0];
                tmp[1, 0] -= darray[1];
                tmp[2, 0] -= darray[2];

                tmp = (Matrix^)rot->Inverse()->Multiply(tmp);

                return (Vector^)tmp->Column(0);
            }
            else
            {
                Vector^ tmp = gcnew DenseVector(Dim);
                for (int i = 0; i < tmp->Count; i++)
                {
                    tmp[i] = x[i] - darray[i];
                }
                return tmp;
            }
        }
    };
}
