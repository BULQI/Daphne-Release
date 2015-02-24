#include "stdafx.h"
#include "NtInterpolatedRectangularPrism.h"
#include <stdexcept>

using namespace std;

namespace NativeDaphneLibrary
{

	NtInterpolatedRectangularPrism::NtInterpolatedRectangularPrism()
	{}


	NtInterpolatedRectangularPrism::NtInterpolatedRectangularPrism(int *, int *, double, double, double){}



	NtInterpolatedRectangularPrism::~NtInterpolatedRectangularPrism(){}


	int NtInterpolatedRectangularPrism::Laplacian(double *, double *, int){ return 0;}

	int NtInterpolatedRectangularPrism::TestAddition(int a, int b)
	{
		return a+b;
	}

}



