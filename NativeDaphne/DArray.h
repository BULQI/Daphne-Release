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
		//indicate pointer is replaced with external pointer
		bool is_pointer_owner;
		double *_array;
	public:

		Darray()
		{
			length = 0;
			is_pointer_owner = true;
			_array = NULL;
		}

		Darray(int len)
		{
			_array = (double *)malloc(len *sizeof(double));
			memset(_array, 0, len *sizeof(double));
			length = len;
			is_pointer_owner = true;
		}

		~Darray()
		{
			this->!Darray();
		}

		!Darray()
		{
			if (is_pointer_owner == false && _array != NULL)
			{
				free(_array);
			}
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

		property double *NativePointer
		{
			double *get()
			{
				return _array;
			}
			void set(double *dptr)
			{
				if (is_pointer_owner == true && _array != NULL)
				{
					free(_array);
				}
				_array = dptr;
				is_pointer_owner = false;
			}
		}

	};
}





