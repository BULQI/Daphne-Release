#pragma once

using namespace System;
using namespace System::Runtime::InteropServices;


namespace NativeDaphne 
{

	public ref class Nt_InterpolatedRectangularPrism
	{
	
	public:
		Nt_InterpolatedRectangularPrism(void);

		~Nt_InterpolatedRectangularPrism()
		{
			this->!Nt_InterpolatedRectangularPrism();
		}

		!Nt_InterpolatedRectangularPrism()
		{
			delete irprism;
		}
		//testing
		int add(int a, int b);

	private:
		
		NativeDaphneLibrary::NtInterpolatedRectangularPrism *irprism;

	};
}
