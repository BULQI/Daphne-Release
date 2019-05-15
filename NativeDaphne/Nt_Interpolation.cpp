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
#include "stdafx.h"
#include "Nt_Interpolation.h"
#include "Nt_Manifolds.h"
#include "Nt_Scalarfield.h"
#include "Nt_ManifoldUtilities.h"




namespace Nt_ManifoldRing
{

	//********************************************
	// implementation of NodeInterpolator
	//********************************************


	 void NodeInterpolator::Init(InterpolatedNodes^ m, bool _toroidal)
	{
		this->m = m;
		toroidal = _toroidal;
		laplacian = gcnew ScalarField(m);
		laplacianOperator = gcnew array<array<LocalMatrix>^>(m->ArraySize);
		laplacianOperator = laplacianMatrix();
		gradientOperator = gcnew array<array<LocalMatrix>^>(m->Dim);
		gradient = gcnew array<double>(m->Dim);
	}

	double NodeInterpolator::Interpolate(array<double>^ x, ScalarField^ sf)
	{
		array<LocalMatrix>^ lm = interpolationMatrix(x);
		double value = 0;

		if (lm != nullptr)
		{
			for (int i = 0; i < lm->Length; i++)
			{
				value += lm[i].Coefficient * sf->darray[lm[i].Index];
			}
		}
		return value;
	}

	array<double>^ NodeInterpolator::Gradient(array<double>^ x, ScalarField^ sf)
	{
		array<array<LocalMatrix>^>^ lm = gradientMatrix(x);

		if (lm != nullptr)
		{
			auto sfarray = sf->darray;
			for (int i = 0; i < m->Dim; i++)
			{
				double value = 0;
				array<LocalMatrix>^ lmi = lm[i];
				for (int j = 0; j < lmi->Length; j++)
				{
					value += lmi[j].Coefficient * sfarray[lmi[j].Index];
				}
				gradient[i] = value;
			}
		}
		return gradient;
	}

	ScalarField^ NodeInterpolator::Laplacian(ScalarField^ sf)
	{
		for (int i = 0; i < sf->darray->Length; i++)
		{
			laplacian->darray[i] = 0.0;

			for (int j = 0; j < laplacianOperator[i]->Length; j++)
			{
				laplacian->darray[i] += laplacianOperator[i][j].Coefficient * sf->darray[laplacianOperator[i][j].Index];
			}
		}
		return laplacian;
	}

	/// <summary>
	/// Return a scalar field (in the volume) representing the applied flux at a surface.
	/// Uses interpolation to distribute the flux source to surrounding nodes.
	/// NOTES: 
	///     This may not be well suited for flux from interpolated-node fields, like natural boundaries 
	///     Doesn't make use of rotation information in Transform, 
	///     so interpolated-node surface normals are assumed to be coincident with volume axes.
	/// </summary>
	/// <param name="flux">Flux from the surface element of the volume</param>
	/// <param name="t">Transform information about embedding of surface in volume </param>
	/// <returns></returns>
	ScalarField^ NodeInterpolator::DiffusionFlux(ScalarField^ flux, Transform^ t, ScalarField^ dst, double dt)
	{
		//ScalarField^ temp = gcnew ScalarField(m);
		array<int>^ indices = gcnew array<int>(3);
		int n;
		double volFactor;

		// This loop is intended to accomodate flux from surfaces using interpolated-node scalar fields
		// For interpolated-node scalar fields:
		//      Each node on the surface would be a principle point
		// For moment-expansion scalar fields: 
		//      There is only one principle point (0,0,0), so the flux gradient is not used.
		for (int i = 0; i < flux->M->PrincipalPoints->Length; i++)
		{
			// The concentration source term
			double concAdd = flux->M->Area() * flux->darray[i] * (-dt)/ m->VoxelVolume();

			// Indices of nodes surround source and proportions for dividing the concentration source among them
			array<LocalMatrix>^ lm = interpolationMatrix(t->toContaining(flux->M->PrincipalPoints[i])->ToArray());

			// Find the node in this manifold that is closest to the principal point
			n = m->indexArrayToLinearIndex(m->localToIndexArray(t->toContaining(flux->M->PrincipalPoints[i])));
			int nps0 = m->NodesPerSide(0)-1;
			int nps1 = m->NodesPerSide(1)-1;
			int nps2 = m->NodesPerSide(2)-1;
			for (int k = 0; k < lm->Length; k++)
			{
				// Boundary nodes don't have the full voxel volume. Correct accordingly.
				volFactor = 1;
				indices = m->linearIndexToIndexArray(lm[k].Index);
				//if ((indices[0] == 0) || (indices[0] == m->NodesPerSide(0) - 1))
				if ((indices[0] == 0) || (indices[0] == nps0))
				{
					volFactor *= 2;
				}
				if ((indices[1] == 0) || (indices[1] == nps1))
				{
					volFactor *= 2;
				} if ((indices[2] == 0) || (indices[2] == nps2))
				{
					volFactor *= 2;
				}
				//temp.darray[lm[k]->Index] += volFactor * lm[k]->Coefficient * concAdd;
				dst->darray[lm[k].Index] += (volFactor * lm[k].Coefficient * concAdd);
			}
		}

		return dst;
	}

	/// <summary>
	/// Return a scalar field (in the volume) representing the applied flux at a surface.
	/// Assigns the entire flux source to the closest node.
	/// NOTES: 
	///     Doesn't make use of rotation information in Transform, 
	///     so interpolated-node surface normals are assumed to be coincident with volume axes.
	/// </summary>
	/// <param name="flux">Flux from the surface element of the volume</param>
	/// <param name="t">Transform information about the location and orientation of surface in volume </param>
	/// <returns></returns>       
	ScalarField^ NodeInterpolator::DiffusionFlux_1Pt(ScalarField^ flux, Transform^ t)
	{
		ScalarField^ temp = gcnew ScalarField(m); 
		int n;
		array<int>^ indices = gcnew array<int>(3);
		double volFactor;

		// This loop is intended to accomodate flux from surfaces using interpolated-node scalar fields
		// For interpolated-node scalar fields:
		//      Each node on the surface would be a principle point
		// For moment-expansion scalar fields: 
		//      There is only one principle point (0,0,0), so the flux gradient is not used.
		for (int i = 0; i < flux->M->PrincipalPoints->Length; i++)
		{
			// Find the node in this manifold that is closest to the principal point
			n = m->indexArrayToLinearIndex(m->localToIndexArray(t->toContaining(flux->M->PrincipalPoints[i])));

			if (n >= 0 && n < temp->darray->Length)
			{
				// Boundary nodes don't have the full voxel volume. Correct accordingly.
				volFactor = 1;
				indices = m->linearIndexToIndexArray(n);
				if ( (indices[0] == 0 ) || (indices[0] == m->NodesPerSide(0) - 1 ) )
				{
					volFactor *= 2;
				}
				if ((indices[1] == 0) || (indices[1] == m->NodesPerSide(1) - 1))
				{
					volFactor *= 2;
				} if ((indices[2] == 0) || (indices[2] == m->NodesPerSide(2) - 1))
				{
					volFactor *= 2;
				}

				temp->darray[n] += volFactor * flux->M->Area() * flux->darray[i] / m->VoxelVolume();
			}
			else
			{
				throw gcnew Exception("Could not apply flux-> Could not find valid lattice point.");
			}
		}

		return temp;
	}

	/// <summary>
	/// Impose Dirichlet boundary conditions
	/// NOTE: This algorithm is best when there is a one-to-one correspondance between 
	/// boundary and interior manifold principal points (nodes). May not be as accurate
	/// when there is not a one-to-one correspondance.
	/// </summary>
	/// <param name="from">Field specified on the boundary manifold</param>
	/// <param name="t">Transform that specifies the geometric relationship between 
	/// the boundary and interior manifolds </param>
	/// <param name="to">Field specified on the interior manifold</param>
	/// <returns>The field after imposing Dirichlet boundary conditions</returns>
	ScalarField^ NodeInterpolator::DirichletBC(ScalarField^ from, Transform^ t, ScalarField^ sf)
	{
		int n;
		for (int i = 0; i < from->M->PrincipalPoints->Length; i++)
		{
			// Find the node in this manifold that is closest to the principal point
			n = m->indexArrayToLinearIndex(m->localToIndexArray(t->toContaining(from->M->PrincipalPoints[i])));
			if (n >= 0 && n < sf->darray->Length)
			{
				//sf->darray[n] = from.Value(from->M->PrincipalPoints[i]);
				sf->darray[n] = from->Value(from->M->PrincipalPoints[i]->ToArray() );
			}
		}
		return sf;
	}


	//********************************************
	// implementation of Trilinear3D
	//********************************************


	void Trilinear3D::Init(InterpolatedNodes^ m, bool _toroidal) 
	{
		NodeInterpolator::Init(m, _toroidal);
		gradientOperator16 = gcnew array<array<LocalMatrix>^>(m->Dim);
		gradientOperator20 = gcnew array<array<LocalMatrix>^>(m->Dim);
		gradientOperatorJagged = gcnew array<array<LocalMatrix>^>(m->Dim);

		for (int i = 0; i < m->Dim; i++)
		{
			gradientOperator[i] = gcnew array<LocalMatrix>(24);
			gradientOperator16[i] = gcnew array<LocalMatrix>(16);
			gradientOperator20[i] = gcnew array<LocalMatrix>(20);
		}

		NodePerSide0 = m->NodesPerSide(0);
		NodePerSide1 = m->NodesPerSide(1);
		NodePerSide2 = m->NodesPerSide(2);
		idxarr = gcnew array<int>(3);

		int *tmp = (int *)malloc(3 *sizeof(int));
		tmp[0] = NodePerSide0;
		tmp[1] = NodePerSide1;
		tmp[2] = NodePerSide2;
		NtInstance = new NtTrilinear3D(tmp, m->StepSize(), _toroidal);
		free(tmp);
	}


	// Don't need to account for toroidal BCs with this low-order scheme. 
	array<LocalMatrix>^ Trilinear3D::interpolationMatrix(array<double>^ x) 
	{

		//array<int>^ idx = m->localToIndexArray(new DenseVector(x));
		double StepSize = m->StepSize();
		idxarr[0] = (int)(x[0] / StepSize);
		idxarr[1] = (int)(x[1] / StepSize);
		idxarr[2] = (int)(x[2] / StepSize);
		int nps01 = NodePerSide0 * NodePerSide1;


		if (idxarr[0] == NodePerSide0 - 1)
		{
			idxarr[0]--;
		}
		if (idxarr[1] == NodePerSide1 - 1)
		{
			idxarr[1]--;
		}
		if (idxarr[2] == NodePerSide2 - 1)
		{
			idxarr[2]--;
		}

		double dx = x[0] / m->StepSize() - idxarr[0],
			dy = x[1] / m->StepSize() - idxarr[1],
			dz = x[2] / m->StepSize() - idxarr[2],
			dxmult, dxymult;

		int n = 0;
		int base_index = idxarr[0] + idxarr[1] * NodePerSide1 + idxarr[2] * nps01;
		int node_index;
		for (int di = 0; di < 2; di++)
		{
			dxmult = di == 0 ? (1 - dx) : dx;
			for (int dj = 0; dj < 2; dj++)
			{
				if (dj == 0)
				{
					dxymult = dxmult * (1 - dy);
					node_index = base_index + di;
				}
				else
				{
					dxymult = dxmult * dy;
					node_index = base_index + di + NodePerSide0;
				}
				for (int dk = 0; dk < 2; dk++)
				{
					if (dk == 0)
					{
						interpolationOperator[n].Index = node_index;
						interpolationOperator[n].Coefficient = dxymult * (1 - dz);
					}
					else
					{
						interpolationOperator[n].Index = node_index + nps01;
						interpolationOperator[n].Coefficient = dxymult * dz;
					}
					n++;
				}
			}
		}

		return interpolationOperator;
	}


	array<LocalMatrix>^ Trilinear3D::interpolationMatrix_original(array<double>^ x)
	{
		//array<int>^ idx = m->localToIndexArray(x);
		array<int>^ idx = m->localToIndexArray(gcnew DenseVector(x));

		if (idx[0] == m->NodesPerSide(0) - 1)
		{
			idx[0]--;
		}
		if (idx[1] == m->NodesPerSide(1) - 1)
		{
			idx[1]--;
		}
		if (idx[2] == m->NodesPerSide(2) - 1)
		{
			idx[2]--;
		}

		double dx = x[0] / m->StepSize() - idx[0],
			dy = x[1] / m->StepSize() - idx[1],
			dz = x[2] / m->StepSize() - idx[2],
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
					interpolationOperator[n].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
					interpolationOperator[n].Coefficient = dxmult * dymult * dzmult;
					n++;
				}
			}
		}
		return interpolationOperator;
	}

	// NOTES:
	// Gradient operators indices could be created and stored once when the NodeInterpolator is instantiated, similar to Laplacian. 
	array<array<LocalMatrix>^>^ Trilinear3D::gradientMatrix(array<double>^ x)  
	{

		//array<int>^ idx = m->localToIndexArray(new DenseVector(x));
		double StepSize = m->StepSize();
		idxarr[0] = (int)(x[0] / StepSize);
		idxarr[1] = (int)(x[1] / StepSize);
		idxarr[2] = (int)(x[2] / StepSize);

		int nps0_m1 = NodePerSide0 - 1;
		int nps1_m1 = NodePerSide1 - 1;
		int nps2_m1 = NodePerSide2 - 1;
		int nps01 = NodePerSide0 * NodePerSide1;

		if (idxarr[0] == nps0_m1)
		{
			idxarr[0]--;
		}
		if (idxarr[1] == nps1_m1)
		{
			idxarr[1]--;
		}
		if (idxarr[2] == nps2_m1)
		{
			idxarr[2]--;
		}

		double dx = x[0] / StepSize - idxarr[0],
			dy = x[1] / StepSize - idxarr[1],
			dz = x[2] / StepSize - idxarr[2],
			dxmult, dymult, coeff;
		int n = 0;
		int n1 = 0;
		int n2 = 0;

		//precomputed values
		int base_index = idxarr[0] + idxarr[1] * NodePerSide1 + idxarr[2] * nps01;
		int dyindex, node_index;
		double StepSize2 = StepSize * 2;

		//0 - not in bound; 1 - left is bound; 2 - right is bound
		int xbound = idxarr[0] == 0 ? 1 : (idxarr[0] + 1 == nps0_m1 ? 2 : 0);
		int ybound = idxarr[1] == 0 ? 1 : (idxarr[1] + 1 == nps1_m1 ? 2 : 0);
		int zbound = idxarr[2] == 0 ? 1 : (idxarr[2] + 1 == nps2_m1 ? 2 : 0);
		array<LocalMatrix>^ gradientMatrix = (xbound == 0 || toroidal == true) ? gradientOperator16[0] :  gradientOperator20[0];
		array<LocalMatrix>^ gradientMatrix1 = (ybound == 0 || toroidal == true) ? gradientOperator16[1] : gradientOperator20[1];
		array<LocalMatrix>^ gradientMatrix2 = (zbound == 0 || toroidal == true) ? gradientOperator16[2] : gradientOperator20[2];
		gradientOperatorJagged[0] = gradientMatrix;
		gradientOperatorJagged[1] = gradientMatrix1;
		gradientOperatorJagged[2] = gradientMatrix2;

		for (int di = 0; di < 2; di++)
		{
			if (di == 0)
			{
				dxmult = (1 - dx) / StepSize2;
			}
			else
			{
				node_index = base_index + 1;
				dxmult = dx / StepSize2;
			}
			for (int dj = 0; dj < 2; dj++)
			{
				if (dj == 0)
				{
					dymult = 1 - dy;
					dyindex = di;
				}
				else
				{
					dymult = dy;
					dyindex = di + NodePerSide0;
				}
				dymult *= dxmult;
				for (int dk = 0; dk < 2; dk++)
				{
					if (dk == 0)
					{
						coeff = dymult * (1 - dz);
						node_index = base_index + dyindex;
					}
					else
					{
						coeff = dymult * dz;
						node_index = base_index + dyindex + nps01;
					}

					// 0th element:
					if (xbound == 0 || xbound + di == 2) //point not in bound
					{
						gradientMatrix[n].Index = node_index + 1;
						gradientMatrix[n++].Coefficient = coeff;


						gradientMatrix[n].Index = node_index - 1;
						gradientMatrix[n++].Coefficient = -coeff;
						//gradientMatrix[n].Index = 0;
						//gradientMatrix[n++].Coefficient = 0.0;
					}
					else if (di == 1) //right bound
					{
						if (toroidal)
						{
							gradientMatrix[n].Index = node_index - nps0_m1 + 1;
							gradientMatrix[n++].Coefficient = coeff;
							gradientMatrix[n].Index = node_index-1;
							gradientMatrix[n++].Coefficient = -coeff;
							//gradientMatrix[n].Index = 0;
							//gradientMatrix[n++].Coefficient = 0.0;
						}
						else
						{
							gradientMatrix[n].Index = node_index;
							gradientMatrix[n++].Coefficient = 3 * coeff;
							gradientMatrix[n].Index = node_index - 1; 
							gradientMatrix[n++].Coefficient = -4 * coeff;
							gradientMatrix[n].Index = node_index - 2;
							gradientMatrix[n++].Coefficient = coeff;
						}
					}
					else //left bound
					{
						if (toroidal)
						{
							gradientMatrix[n].Index = node_index + 1;
							gradientMatrix[n++].Coefficient = coeff;
							gradientMatrix[n].Index = node_index + NodePerSide0 - 2;
							gradientMatrix[n++].Coefficient = -coeff;
							//gradientMatrix[n].Index = 0;
							//gradientMatrix[n++].Coefficient = 0.0;
						}
						else
						{
							gradientMatrix[n].Index = node_index;
							gradientMatrix[n++].Coefficient = -3 * coeff;
							gradientMatrix[n].Index = node_index + 1;
							gradientMatrix[n++].Coefficient = 4 * coeff;
							gradientMatrix[n].Index = node_index + 2;
							gradientMatrix[n++].Coefficient = -coeff;
						}
					}


					// 1st element:
					if (ybound == 0 || ybound + dj == 2)
					{
						gradientMatrix1[n1].Index = node_index + NodePerSide0;
						gradientMatrix1[n1++].Coefficient = coeff;
						gradientMatrix1[n1].Index = node_index - NodePerSide0;
						gradientMatrix1[n1++].Coefficient = -coeff;
					}
					else if (dj == 1)
					{
						if (toroidal)
						{
							gradientMatrix1[n1].Index = node_index - (NodePerSide1 - 2) * NodePerSide0;
							gradientMatrix1[n1++].Coefficient = coeff;
							gradientMatrix1[n1].Index = node_index - NodePerSide0;
							gradientMatrix1[n1++].Coefficient = -coeff;
						}
						else
						{
							gradientMatrix1[n1].Index = node_index;
							gradientMatrix1[n1++].Coefficient = 3 * coeff;
							gradientMatrix1[n1].Index = node_index - NodePerSide0; 
							gradientMatrix1[n1++].Coefficient = -4 * coeff;
							gradientMatrix1[n1].Index = node_index - NodePerSide0 - NodePerSide0;
							gradientMatrix1[n1++].Coefficient = coeff;
						}
					}
					else
					{
						if (toroidal)
						{
							gradientMatrix1[n1].Index = node_index + NodePerSide0;
							gradientMatrix1[n1++].Coefficient = coeff;
							gradientMatrix1[n1].Index = node_index + (NodePerSide1 - 2) * NodePerSide0 ;
							gradientMatrix1[n1++].Coefficient = -coeff;
						}
						else
						{
							gradientMatrix1[n1].Index = node_index;
							gradientMatrix1[n1++].Coefficient = -3 * coeff;
							gradientMatrix1[n1].Index = node_index + NodePerSide0;
							gradientMatrix1[n1++].Coefficient = 4 * coeff;
							gradientMatrix1[n1].Index = node_index + NodePerSide0 + NodePerSide0;
							gradientMatrix1[n1++].Coefficient = -coeff;
						}
					}                   

					// 2nd element:
					if (zbound == 0 || zbound + dk == 2)
					{
						gradientMatrix2[n2].Index = node_index + nps01;
						gradientMatrix2[n2++].Coefficient = coeff;
						gradientMatrix2[n2].Index = node_index - nps01;
						gradientMatrix2[n2++].Coefficient = -coeff;
					}
					else if (dk == 1)
					{
						if (toroidal)
						{
							gradientMatrix2[n2].Index = node_index - (NodePerSide2 - 2) * nps01;
							gradientMatrix2[n2++].Coefficient = coeff;
							gradientMatrix2[n2].Index = node_index - nps01;
							gradientMatrix2[n2++].Coefficient = -coeff;
						}
						else
						{
							gradientMatrix2[n2].Index = node_index;
							gradientMatrix2[n2++].Coefficient = 3 * coeff;
							gradientMatrix2[n2].Index = node_index - nps01;
							gradientMatrix2[n2++].Coefficient = -4 * coeff;
							gradientMatrix2[n2].Index = node_index - nps01 - nps01;
							gradientMatrix2[n2++].Coefficient = coeff;
						}
					}
					else
					{
						if (toroidal)
						{
							gradientMatrix2[n2].Index = node_index + nps01;
							gradientMatrix2[n2++].Coefficient = coeff;
							gradientMatrix2[n2].Index = node_index + (NodePerSide2 - 2) * nps01;
							gradientMatrix2[n2++].Coefficient = -coeff;
						}
						else
						{
							gradientMatrix2[n2].Index = node_index;
							gradientMatrix2[n2++].Coefficient = -3 * coeff;
							gradientMatrix2[n2].Index = node_index + nps01;
							gradientMatrix2[n2++].Coefficient = 4 * coeff;
							gradientMatrix2[n2].Index = node_index + nps01 + nps01;
							gradientMatrix2[n2++].Coefficient = -coeff;
						}
					}                    
				}
			}
		}

		return gradientOperatorJagged;
	}


	array<array<LocalMatrix>^>^ Trilinear3D::gradientMatrix_original(array<double>^ x)
	{
		// array<int>^ idx = m->localToIndexArray(x);
		array<int>^ idx = m->localToIndexArray(gcnew DenseVector(x));

		if (idx[0] == m->NodesPerSide(0) - 1)
		{
			idx[0]--;
		}
		if (idx[1] == m->NodesPerSide(1) - 1)
		{
			idx[1]--;
		}
		if (idx[2] == m->NodesPerSide(2) - 1)
		{
			idx[2]--;
		}

		double dx = x[0] / m->StepSize() - idx[0],
			dy = x[1] / m->StepSize() - idx[1],
			dz = x[2] / m->StepSize() - idx[2],
			dxmult, dymult, dzmult,
			coeff;

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

					coeff = dxmult * dymult * dzmult / (2 * m->StepSize());

					// 0th element:
					if (idx[0] + di == m->NodesPerSide(0) - 1)
					{
						if (toroidal)
						{
							gradientOperator[0][n].Index = (1) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[0][n].Coefficient = coeff;
							gradientOperator[0][n + 1].Index = (idx[0] + di - 1) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[0][n + 1].Coefficient = -coeff;
							gradientOperator[0][n + 2].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[0][n + 2].Coefficient = 0.0;
						}
						else
						{
							gradientOperator[0][n].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[0][n].Coefficient = 3 * coeff;
							gradientOperator[0][n + 1].Index = (idx[0] + di - 1) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[0][n + 1].Coefficient = -4 * coeff;
							gradientOperator[0][n + 2].Index = (idx[0] + di - 2) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[0][n + 2].Coefficient = coeff;
						}
					}
					else if (idx[0] + di == 0)
					{
						if (toroidal)
						{
							gradientOperator[0][n].Index = (idx[0] + di + 1) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[0][n].Coefficient = coeff;
							gradientOperator[0][n + 1].Index = (m->NodesPerSide(0) - 2) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[0][n + 1].Coefficient = -coeff;
							gradientOperator[0][n + 2].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[0][n + 2].Coefficient = 0.0;
						}
						else
						{
							gradientOperator[0][n].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[0][n].Coefficient = -3 * coeff;
							gradientOperator[0][n + 1].Index = (idx[0] + di + 1) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[0][n + 1].Coefficient = 4 * coeff;
							gradientOperator[0][n + 2].Index = (idx[0] + di + 2) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[0][n + 2].Coefficient = -coeff;
						}
					}
					else
					{
						gradientOperator[0][n].Index = (idx[0] + di + 1) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
						gradientOperator[0][n].Coefficient = coeff;
						gradientOperator[0][n + 1].Index = (idx[0] + di - 1) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
						gradientOperator[0][n + 1].Coefficient = -coeff;
						gradientOperator[0][n + 2].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
						gradientOperator[0][n + 2].Coefficient = 0.0;
					}

					// 1st element:
					if (idx[1] + dj == m->NodesPerSide(1) - 1)
					{
						if (toroidal)
						{
							gradientOperator[1][n].Index = (idx[0] + di) + (1) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[1][n].Coefficient = coeff;
							gradientOperator[1][n + 1].Index = (idx[0] + di) + (idx[1] + dj - 1) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[1][n + 1].Coefficient = -coeff;
							gradientOperator[1][n + 2].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[1][n + 2].Coefficient = 0.0;
						}
						else
						{
							gradientOperator[1][n].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[1][n].Coefficient = 3 * coeff;
							gradientOperator[1][n + 1].Index = (idx[0] + di) + (idx[1] + dj - 1) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[1][n + 1].Coefficient = -4 * coeff;
							gradientOperator[1][n + 2].Index = (idx[0] + di) + (idx[1] + dj - 2) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[1][n + 2].Coefficient = coeff;
						}
					}
					else if (idx[1] + dj == 0)
					{
						if (toroidal)
						{
							gradientOperator[1][n].Index = (idx[0] + di) + (idx[1] + dj + 1) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[1][n].Coefficient = coeff;
							gradientOperator[1][n + 1].Index = (idx[0] + di) + (m->NodesPerSide(1) - 2) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[1][n + 1].Coefficient = -coeff;
							gradientOperator[1][n + 2].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[1][n + 2].Coefficient = 0.0;
						}
						else
						{
							gradientOperator[1][n].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[1][n].Coefficient = -3 * coeff;
							gradientOperator[1][n + 1].Index = (idx[0] + di) + (idx[1] + dj + 1) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[1][n + 1].Coefficient = 4 * coeff;
							gradientOperator[1][n + 2].Index = (idx[0] + di) + (idx[1] + dj + 2) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[1][n + 2].Coefficient = -coeff;
						}
					}
					else
					{
						gradientOperator[1][n].Index = (idx[0] + di) + (idx[1] + dj + 1) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
						gradientOperator[1][n].Coefficient = coeff;
						gradientOperator[1][n + 1].Index = (idx[0] + di) + (idx[1] + dj - 1) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
						gradientOperator[1][n + 1].Coefficient = -coeff;
						gradientOperator[1][n + 2].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
						gradientOperator[1][n + 2].Coefficient = 0.0;
					}

					// 2nd element:
					if (idx[2] + dk == m->NodesPerSide(2) - 1)
					{
						if (toroidal)
						{
							gradientOperator[2][n].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (1) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[2][n].Coefficient = coeff;
							gradientOperator[2][n + 1].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk - 1) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[2][n + 1].Coefficient = -coeff;
							gradientOperator[2][n + 2].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[2][n + 2].Coefficient = 0.0;
						}
						else
						{
							gradientOperator[2][n].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[2][n].Coefficient = 3 * coeff;
							gradientOperator[2][n + 1].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk - 1) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[2][n + 1].Coefficient = -4 * coeff;
							gradientOperator[2][n + 2].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk - 2) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[2][n + 2].Coefficient = coeff;
						}
					}
					else if (idx[2] + dk == 0)
					{
						if (toroidal)
						{
							gradientOperator[2][n].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk + 1) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[2][n].Coefficient = coeff;
							gradientOperator[2][n + 1].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (m->NodesPerSide(2) - 2) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[2][n + 1].Coefficient = -coeff;
							gradientOperator[2][n + 2].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[2][n + 2].Coefficient = 0.0;
						}
						else
						{
							gradientOperator[2][n].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[2][n].Coefficient = -3 * coeff;
							gradientOperator[2][n + 1].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk + 1) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[2][n + 1].Coefficient = 4 * coeff;
							gradientOperator[2][n + 2].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk + 2) * m->NodesPerSide(0) * m->NodesPerSide(1);
							gradientOperator[2][n + 2].Coefficient = -coeff;
						}
					}
					else
					{
						gradientOperator[2][n].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk + 1) * m->NodesPerSide(0) * m->NodesPerSide(1);
						gradientOperator[2][n].Coefficient = coeff;
						gradientOperator[2][n + 1].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk - 1) * m->NodesPerSide(0) * m->NodesPerSide(1);
						gradientOperator[2][n + 1].Coefficient = -coeff;
						gradientOperator[2][n + 2].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0) + (idx[2] + dk) * m->NodesPerSide(0) * m->NodesPerSide(1);
						gradientOperator[2][n + 2].Coefficient = 0.0;
					}
					n += 3;
				}
			}
		}

		return gradientOperator;
	}


	array<array<LocalMatrix>^>^ Trilinear3D::laplacianMatrix() 
	{
		for (int i = 0; i < m->ArraySize; i++)
		{
			laplacianOperator[i] = gcnew array<LocalMatrix>(7);
		}

		int n = 0;
		int idxplus, idxminus;
		double  coeff = 1.0 / (m->StepSize() * m->StepSize()),
			coeff0 = -2.0 * coeff;
		int N01 = m->NodesPerSide(0) * m->NodesPerSide(1);

		for (int k = 0; k < m->NodesPerSide(2); k++)
		{
			for (int j = 0; j < m->NodesPerSide(1); j++)
			{
				for (int i = 0; i < m->NodesPerSide(0); i++)
				{
					// Laplacian index n corresponds to grid indices (i,j,k)

					laplacianOperator[n][0].Coefficient = 0;
					laplacianOperator[n][0].Index = i + j * m->NodesPerSide(0) + k * N01;

					if (i == 0)
					{
						idxplus = (i + 1) + j * m->NodesPerSide(0) + k * N01;
						idxminus = toroidal ? (m->NodesPerSide(0) - 2) + j * m->NodesPerSide(0) + k * N01 : idxplus;
					}
					else if (i == m->NodesPerSide(0) - 1)
					{
						idxminus = (i - 1) + j * m->NodesPerSide(0) + k * N01;
						idxplus = toroidal ? 1 + j * m->NodesPerSide(0) + k * N01 : idxminus;
					}
					else
					{
						idxplus = (i + 1) + j * m->NodesPerSide(0) + k * N01;
						idxminus = (i - 1) + j * m->NodesPerSide(0) + k * N01;
					}

					// (i+1), j, k
					laplacianOperator[n][1].Coefficient = coeff;
					laplacianOperator[n][1].Index = idxplus;

					// (i-1), j, k
					laplacianOperator[n][2].Coefficient = coeff;
					laplacianOperator[n][2].Index = idxminus;

					// i,j,k
					laplacianOperator[n][0].Coefficient += coeff0;

					if (j == 0)
					{
						idxplus = i + (j + 1) * m->NodesPerSide(0) + k * N01;
						idxminus = toroidal ? i + (m->NodesPerSide(1) - 2) * m->NodesPerSide(0) + k * N01 : idxplus;
					}
					else if (j == m->NodesPerSide(1) - 1)
					{
						idxminus = i + (j - 1) * m->NodesPerSide(0) + k * N01;
						idxplus = toroidal ? i + 1 * m->NodesPerSide(0) + k * N01 : idxminus;
					}
					else
					{
						idxplus = i + (j + 1) * m->NodesPerSide(0) + k * N01;
						idxminus = i + (j - 1) * m->NodesPerSide(0) + k * N01;
					}

					// i, (j+1), k
					laplacianOperator[n][3].Coefficient = coeff;
					laplacianOperator[n][3].Index = idxplus;

					// i, (j-1), k
					laplacianOperator[n][4].Coefficient = coeff;
					laplacianOperator[n][4].Index = idxminus;

					// i,j,k
					laplacianOperator[n][0].Coefficient += coeff0;

					if (k == 0)
					{
						idxplus = i + j * m->NodesPerSide(0) + (k + 1) * N01;
						idxminus = toroidal ? i + j * m->NodesPerSide(0) + (m->NodesPerSide(1) - 2) * N01 : idxplus;
					}
					else if (k == m->NodesPerSide(2) - 1)
					{
						idxminus = i + j * m->NodesPerSide(0) + (k - 1) * N01;
						idxplus = toroidal ? i + j * m->NodesPerSide(0) + 1 * N01 : idxminus;
					}
					else
					{
						idxplus = i + j * m->NodesPerSide(0) + (k + 1) * N01;
						idxminus = i + j * m->NodesPerSide(0) + (k - 1) * N01;
					}

					// i, j, (k+1)
					laplacianOperator[n][5].Coefficient = coeff;
					laplacianOperator[n][5].Index = idxplus;

					// i, j, (k-1)
					laplacianOperator[n][6].Coefficient = coeff;
					laplacianOperator[n][6].Index = idxminus;

					// i,j,k
					laplacianOperator[n][0].Coefficient += coeff0;

					n++;
				}
			}
		}
		return laplacianOperator;
	}

	double Trilinear3D::Integration(ScalarField^ sf) 
	{
		array<double>^ point = gcnew array<double>(3);
		double sum = 0;
		for (int k = 0; k < m->NodesPerSide(2) - 1; k++)
		{
			for (int j = 0; j < m->NodesPerSide(1) - 1; j++)
			{
				for (int i = 0; i < m->NodesPerSide(0) - 1; i++)
				{
					point[0] = (i + 0.5) * m->StepSize();
					point[1] = (j + 0.5) * m->StepSize();
					point[2] = (k + 0.5) * m->StepSize();

					// The value at the center of the voxel
					sum += sf->Value(point);
				}
			}
		}
		return sum * m->StepSize() * m->StepSize() * m->StepSize();
	}

	//for debug only - compare reuslts between managed and unmanged code
	//ScalarField^ Trilinear3D :: Laplacian(ScalarField^ sf)
	//{
	//	ScalarField^ sf1 = NodeInterpolator::Laplacian(sf);

	//	double *sfarray = sf->ArrayPointer;
	//	double *lparray = laplacian->ArrayPointer;
	//	int len = sf->darray->Length;
	//	double *test_array = (double *)malloc(len * sizeof(double));
	//	NtInstance->Laplacian(sfarray, test_array, sf->darray->Length);

	//	for (int i = 0; i< len; i++)
	//	{
	//		double a = test_array[i];
	//		double b = lparray[i];
	//		double diff = a  > b ? a-b : b-a;
	//		if (diff > 1.0e-16)
	//		{
	//			throw gcnew Exception("Debug Error: laplacian result differ");
	//		}
	//	}
	//	free(test_array);
	//	return laplacian;
	//}
	
	ScalarField^ Trilinear3D :: Laplacian(ScalarField^ sf)
	{
		if (NtInstance != NULL)
		{
			double *sfarray = sf->ArrayPointer;
			double *lparray = laplacian->ArrayPointer;
			NtInstance->Laplacian(sfarray, lparray, sf->darray->Length);
			return laplacian;
		}
		else 
		{
			//original method
			return NodeInterpolator::Laplacian(sf);
		}
	}


	//********************************************
	// implementation of Trilinear2D
	//********************************************


	void  Trilinear2D::Init(InterpolatedNodes^ m, bool _toroidal) 
	{
		NodeInterpolator::Init(m, _toroidal);
		for (int i = 0; i < m->Dim; i++)
		{
			gradientOperator[i] = gcnew array<LocalMatrix>(12);
		}
	}


	// Don't need to account for toroidal BCs with this low-order scheme. 
	array<LocalMatrix>^ Trilinear2D::interpolationMatrix(array<double>^ x) 
	{
		//array<int>^ idx = m->localToIndexArray(x);
		array<int>^ idx = m->localToIndexArray(gcnew DenseVector(x));

		if (idx[0] == m->NodesPerSide(0) - 1)
		{
			idx[0]--;
		}
		if (idx[1] == m->NodesPerSide(1) - 1)
		{
			idx[1]--;
		}

		double dx = x[0] / m->StepSize() - idx[0],
			dy = x[1] / m->StepSize() - idx[1],
			dxmult, dymult;
		int n = 0;

		for (int di = 0; di < 2; di++)
		{
			for (int dj = 0; dj < 2; dj++)
			{
				dxmult = di == 0 ? (1 - dx) : dx;
				dymult = dj == 0 ? (1 - dy) : dy;
				interpolationOperator[n].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0);
				interpolationOperator[n].Coefficient = dxmult * dymult;
				n++;
			}
		}
		return interpolationOperator;
	}


	// NOTES:
	// Gradient operators indices could be created and stored once when the NodeInterpolator is instantiated, similar to Laplacian. 
	array<array<LocalMatrix>^>^ Trilinear2D::gradientMatrix(array<double>^ x) 
	{
		array<int>^ idx = m->localToIndexArray(gcnew DenseVector(x));

		if (idx[0] == m->NodesPerSide(0) - 1)
		{
			idx[0]--;
		}
		if (idx[1] == m->NodesPerSide(1) - 1)
		{
			idx[1]--;
		}

		double dx = x[0] / m->StepSize() - idx[0],
			dy = x[1] / m->StepSize() - idx[1],
			dxmult, dymult,
			coeff;

		int n = 0;
		for (int di = 0; di < 2; di++)
		{
			for (int dj = 0; dj < 2; dj++)
			{
				dxmult = di == 0 ? (1 - dx) : dx;
				dymult = dj == 0 ? (1 - dy) : dy;
				coeff = dxmult * dymult / (2 * m->StepSize());

				// 0th element:
				if (idx[0] + di == m->NodesPerSide(0) - 1)
				{
					if (toroidal)
					{
						gradientOperator[0][n].Index = (1) + (idx[1] + dj) * m->NodesPerSide(0);
						gradientOperator[0][n].Coefficient = coeff;
						gradientOperator[0][n + 1].Index = (idx[0] + di - 1) + (idx[1] + dj) * m->NodesPerSide(0);
						gradientOperator[0][n + 1].Coefficient = -coeff;
						gradientOperator[0][n + 2].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0);
						gradientOperator[0][n + 2].Coefficient = 0.0;
					}
					else
					{
						gradientOperator[0][n].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0);
						gradientOperator[0][n].Coefficient = 3 * coeff;
						gradientOperator[0][n + 1].Index = (idx[0] + di - 1) + (idx[1] + dj) * m->NodesPerSide(0);
						gradientOperator[0][n + 1].Coefficient = -4 * coeff;
						gradientOperator[0][n + 2].Index = (idx[0] + di - 2) + (idx[1] + dj) * m->NodesPerSide(0);
						gradientOperator[0][n + 2].Coefficient = coeff;
					}
				}
				else if (idx[0] + di == 0)
				{
					if (toroidal)
					{
						gradientOperator[0][n].Index = (idx[0] + di + 1) + (idx[1] + dj) * m->NodesPerSide(0);
						gradientOperator[0][n].Coefficient = coeff;
						gradientOperator[0][n + 1].Index = (m->NodesPerSide(0) - 2) + (idx[1] + dj) * m->NodesPerSide(0);
						gradientOperator[0][n + 1].Coefficient = -coeff;
						gradientOperator[0][n + 2].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0);
						gradientOperator[0][n + 2].Coefficient = 0.0;
					}
					else
					{
						gradientOperator[0][n].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0);
						gradientOperator[0][n].Coefficient = -3 * coeff;
						gradientOperator[0][n + 1].Index = (idx[0] + di + 1) + (idx[1] + dj) * m->NodesPerSide(0);
						gradientOperator[0][n + 1].Coefficient = 4 * coeff;
						gradientOperator[0][n + 2].Index = (idx[0] + di + 2) + (idx[1] + dj) * m->NodesPerSide(0);
						gradientOperator[0][n + 2].Coefficient = -coeff;
					}
				}
				else
				{
					gradientOperator[0][n].Index = (idx[0] + di + 1) + (idx[1] + dj) * m->NodesPerSide(0);
					gradientOperator[0][n].Coefficient = coeff;
					gradientOperator[0][n + 1].Index = (idx[0] + di - 1) + (idx[1] + dj) * m->NodesPerSide(0);
					gradientOperator[0][n + 1].Coefficient = -coeff;
					gradientOperator[0][n + 2].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0);
					gradientOperator[0][n + 2].Coefficient = 0.0;
				}

				// 1st element:
				if (idx[1] + dj == m->NodesPerSide(1) - 1)
				{
					if (toroidal)
					{
						gradientOperator[1][n].Index = (idx[0] + di) + (1) * m->NodesPerSide(0);
						gradientOperator[1][n].Coefficient = coeff;
						gradientOperator[1][n + 1].Index = (idx[0] + di) + (idx[1] + dj - 1) * m->NodesPerSide(0);
						gradientOperator[1][n + 1].Coefficient = -coeff;
						gradientOperator[1][n + 2].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0);
						gradientOperator[1][n + 2].Coefficient = 0.0;
					}
					else
					{
						gradientOperator[1][n].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0);
						gradientOperator[1][n].Coefficient = 3 * coeff;
						gradientOperator[1][n + 1].Index = (idx[0] + di) + (idx[1] + dj - 1) * m->NodesPerSide(0);
						gradientOperator[1][n + 1].Coefficient = -4 * coeff;
						gradientOperator[1][n + 2].Index = (idx[0] + di) + (idx[1] + dj - 2) * m->NodesPerSide(0);
						gradientOperator[1][n + 2].Coefficient = coeff;
					}
				}
				else if (idx[1] + dj == 0)
				{
					if (toroidal)
					{
						gradientOperator[1][n].Index = (idx[0] + di) + (idx[1] + dj + 1) * m->NodesPerSide(0);
						gradientOperator[1][n].Coefficient = coeff;
						gradientOperator[1][n + 1].Index = (idx[0] + di) + (m->NodesPerSide(1) - 2) * m->NodesPerSide(0);
						gradientOperator[1][n + 1].Coefficient = -coeff;
						gradientOperator[1][n + 2].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0);
						gradientOperator[1][n + 2].Coefficient = 0.0;
					}
					else
					{
						gradientOperator[1][n].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0);
						gradientOperator[1][n].Coefficient = -3 * coeff;
						gradientOperator[1][n + 1].Index = (idx[0] + di) + (idx[1] + dj + 1) * m->NodesPerSide(0);
						gradientOperator[1][n + 1].Coefficient = 4 * coeff;
						gradientOperator[1][n + 2].Index = (idx[0] + di) + (idx[1] + dj + 2) * m->NodesPerSide(0);
						gradientOperator[1][n + 2].Coefficient = -coeff;
					}
				}
				else
				{
					gradientOperator[1][n].Index = (idx[0] + di) + (idx[1] + dj + 1) * m->NodesPerSide(0);
					gradientOperator[1][n].Coefficient = coeff;
					gradientOperator[1][n + 1].Index = (idx[0] + di) + (idx[1] + dj - 1) * m->NodesPerSide(0);
					gradientOperator[1][n + 1].Coefficient = -coeff;
					gradientOperator[1][n + 2].Index = (idx[0] + di) + (idx[1] + dj) * m->NodesPerSide(0);
					gradientOperator[1][n + 2].Coefficient = 0.0;
				}
				n += 3;

			}
		}

		return gradientOperator;
	}

	array<array<LocalMatrix>^>^ Trilinear2D::laplacianMatrix() 
	{
		for (int i = 0; i < m->ArraySize; i++)
		{
			laplacianOperator[i] = gcnew array<LocalMatrix>(5);
		}

		int n = 0;

		int idxplus, idxminus;
		double  coeff=1.0 / (m->StepSize() * m->StepSize()),
			coeff0=-2.0 * coeff;

		for (int j = 0; j < m->NodesPerSide(1); j++)
		{
			for (int i = 0; i < m->NodesPerSide(0); i++)
			{
				// Laplacian index n corresponds to grid indices (i,j,k)

				laplacianOperator[n][0].Coefficient = 0;
				laplacianOperator[n][0].Index = i + j * m->NodesPerSide(0);

				if (i == 0)
				{
					idxplus = (i + 1) + j * m->NodesPerSide(0);
					idxminus = toroidal ? m->NodesPerSide(0) - 2 : idxplus;
				}
				else if (i == m->NodesPerSide(0) - 1)
				{
					idxminus = (i - 1) + j * m->NodesPerSide(0);
					idxplus = toroidal ? 1 : idxminus;
				}
				else
				{
					idxplus = (i + 1) + j * m->NodesPerSide(0);
					idxminus = (i - 1) + j * m->NodesPerSide(0);
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
					idxplus = i + (j + 1) * m->NodesPerSide(0);
					idxminus = toroidal ? i + (m->NodesPerSide(1) - 2) * m->NodesPerSide(0) : idxplus;
				}
				else if (j == m->NodesPerSide(1) - 1)
				{
					idxminus = i + (j - 1) * m->NodesPerSide(0);
					idxplus = toroidal ? i + 1 * m->NodesPerSide(0) : idxminus;
					idxplus = idxminus;
				}
				else
				{
					idxplus = i + (j + 1) * m->NodesPerSide(0);
					idxminus = i + (j - 1) * m->NodesPerSide(0);
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


	double Trilinear2D::Integration(ScalarField^ sf) 
	{
		array<double>^ point = gcnew array<double>(3);
		double sum = 0;
		point[2] = 0;

		for (int j = 0; j < m->NodesPerSide(1) - 1; j++)
		{
			for (int i = 0; i < m->NodesPerSide(0) - 1; i++)
			{
				point[0] = (i + 0.5) * m->StepSize();
				point[1] = (j + 0.5) * m->StepSize();

				// The value at the center of the pixel
				sum += sf->Value(point);
			}
		}
		return sum * m->StepSize() * m->StepSize();
	}
}
