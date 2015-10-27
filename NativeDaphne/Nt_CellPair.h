#pragma once

#include <errno.h>
#include "Nt_DArray.h"
#include "Nt_Cell.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_CellPair
	{
	private:
		array<double>^ tmp_arr;

	public:
		static array<double>^ gridSize;
		static bool isECSToroidal;
		static double gridStep;
		int MaxSeperation;

		Nt_CellPair(Nt_Cell^ a, Nt_Cell^ b)
		{
			this->a = a;
			this->b = b;
			dist = 0;
			b_ij = 0;

			//Math.ceiling
			double tmp = (a->Radius + b->Radius)/gridStep;
			MaxSeperation = (int)tmp;
			if (tmp - MaxSeperation > 0)MaxSeperation++;

			tmp_arr = gcnew array<double>(3);
		}

		property Nt_Cell^ Cell[int]
		{
			Nt_Cell^ get(int index)
			{
				if (index == 0) return a;
				return b;
			}
		}

		//to be removed.
		void distance()
        {
            //Vector tmp = new DenseVector(a.SpatialState.X);
            //tmp = (DenseVector)tmpArr.Subtract(new DenseVector(b.SpatialState.X));
            Nt_Darray^ a_X = a->SpatialState->X;
            Nt_Darray^ b_X = b->SpatialState->X;
            double x = a_X[0] - b_X[0];
            double y = a_X[1] - b_X[1];
            double z = a_X[2] - b_X[2];
            // correction for periodic boundary conditions
            if (isECSToroidal)
            {
                double dx = Math::Abs(x),
                       dy = Math::Abs(y),
                       dz = Math::Abs(z);

                if (dx > 0.5 * gridSize[0])
                {
                    x = gridSize[0] - dx;
                }
                if (dy > 0.5 * gridSize[1])
                {
                    y = gridSize[1] - dy;
                }
                if (dz > 0.5 * gridSize[2])
                {
                    z = gridSize[2] - dz;
                }
            }
            dist = Math::Sqrt(x * x + y * y + z * z);
        }

        property int GridIndex_dx
        {
            int get()
            {
                int dx;
                return ((dx = a->GridIndex[0] - b->GridIndex[0]) >= 0 ? dx : -dx);
            }
        }

        property int GridIndex_dy
        {
            int get()
            {
                int dy;
                return ((dy = a->GridIndex[1] - b->GridIndex[1]) >= 0 ? dy : -dy);
            }
        }

        property int GridIndex_dz
        {
            int get()
            {
                int dz;
                return ((dz = a->GridIndex[2] - b->GridIndex[2]) >= 0 ? dz : -dz);
            }
        }


        /// <summary>
        /// tells if this pair is critical, i.e. if it is interacting
        /// </summary>
        /// <returns>true for critical, false otherwise</returns>
        bool isCriticalPair()
        {
            return b_ij == 1;
        }

	protected:

		void bond()
		{
            if (b_ij == 0 && dist <= a->Radius + b->Radius)
            {
                b_ij = 1;
            }
            else if (b_ij == 1 && dist > a->Radius + b->Radius)
            {
                b_ij = 0;
            }
        }

	public:
		/// <summary>
        /// choose the normal to point from a to b: a feels a negative force, b positive
        /// </summary>
        /// <param name="dt">time step for this integration step</param>
        void pairInteract(double dt)
        {
            bond();

            if (b_ij != 0)
            {
                double force = 0.0;

                if (dist > 0)
                {
                    force = Phi1 * (1.0 / dist - 1.0 / (a->Radius + b->Radius));
                }

                if (force != 0.0)
                {
                    double* b_X = b->SpatialState->X->NativePointer;
                    double* a_X = a->SpatialState->X->NativePointer;
                    double dx = b_X[0] - a_X[0];
                    double dy = b_X[1] - a_X[1];
                    double dz = b_X[2] - a_X[2];

                    double tmplen = Math::Sqrt(dx * dx + dy * dy + dz * dz);

                    dx = dx * force / tmplen;
                    dy = dy * force / tmplen;
                    dz = dz * force / tmplen;
                    tmp_arr[0] = -dx;
                    tmp_arr[1] = -dy;
                    tmp_arr[2] = -dz;
                    a->addForce(tmp_arr);
                    tmp_arr[0] = dx;
                    tmp_arr[1] = dy;
                    tmp_arr[2] = dz;
                    b->addForce(tmp_arr);

                }
            }
        }

        Nt_Cell ^a;
		Nt_Cell ^b;
        double dist;
        int b_ij;
        static double Phi1, Phi2;
    };
}
	
