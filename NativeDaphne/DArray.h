#pragma once

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{
	[SuppressUnmanagedCodeSecurity]
	public ref class Darray
	{
	private:
		int length;
		double *_array;
	public:
		Darray(int len)
		{
			_array = (double *)malloc(len *sizeof(double));
			memset(_array, 0, len *sizeof(double));
			length = len;
		}

		property double default[int]
		{
			double get(int index)
			{
				return _array[index];
			}
			void set(int index, double value)
			{
				//testing the setting 
				_array[index] = value + 1;
			}
		}

		property int Length
		{
			int get()
			{
				return length;
			}
		}
	};
}





