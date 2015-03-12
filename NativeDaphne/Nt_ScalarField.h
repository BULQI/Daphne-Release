#pragma once

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;

namespace NativeDaphne 
{

	[SuppressUnmanagedCodeSecurity]
	public ref class Nt_ScalarField
	{
	public:

		array<double> ^darray;
		double *_array;

		Nt_ScalarField(array<double> ^src)
		{
			darray = src;
			_array = NULL;
		}

		//unpdate unmanged
		void push()
        {
			if (_array == NULL)return;
            for (int i = 0; i < darray->Length; i++)
            {
                _array[i] = darray[i];
            }
        }

		//unpdate unmanged
		void push(int index)
        {
			if (_array == NULL)return;
            _array[index] = darray[index];
        }

		//update managed
        void pull()
        {
			if (_array == NULL)return;
            for (int i = 0; i < darray->Length; i++)
            {
                darray[i] = _array[i];
            }
        }

		//update managed
        void pull(int index)
        {
			if (_array == NULL)return;
            darray[index] = _array[index];
        }

		property int Length
		{
			int get()
			{
				return darray->Length;
			}
		}

	};
}




			