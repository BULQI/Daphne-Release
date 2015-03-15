#pragma once

using namespace System;
using namespace System::Collections::Generic;
using namespace System::Security;
using namespace  Newtonsoft::Json;

namespace NativeDaphne 
{
	//forward declaration
	ref class Nt_Darray;

	public ref class DoubleArraySerializer : public JsonConverter
	{
	public:
		virtual void WriteJson(JsonWriter^ writer, Object^ value, JsonSerializer ^serializer) override; 

		virtual Object^ ReadJson(JsonReader^ reader, Type^ objectType, Object^ existingValue, JsonSerializer^ serializer) override; 

		virtual bool CanConvert(Type^ objectType) override 
		{
			Type^ t = Nt_Darray::typeid;
			return t->IsAssignableFrom(objectType);
		}	
	};

	[SuppressUnmanagedCodeSecurity]
	[JsonConverter(DoubleArraySerializer::typeid)]
	public ref class Nt_Darray
	{
	private:
		int length;
		//indicate pointer is replaced with external pointer
		bool is_pointer_owner;
		double *_array;
	public:

		Nt_Darray()
		{
			length = 0;
			is_pointer_owner = true;
			_array = NULL;
		}

		Nt_Darray(int len)
		{
			_array = (double *)malloc(len *sizeof(double));
			memset(_array, 0, len *sizeof(double));
			length = len;
			is_pointer_owner = true;
		}

		~Nt_Darray()
		{
			this->!Nt_Darray();
		}

		!Nt_Darray()
		{
			if (is_pointer_owner == false && _array != NULL)
			{
				free(_array);
			}
		}

		property array<double>^ ArrayCopy
		{
			array<double>^ get() 
			{
				array<double> ^tmp = gcnew array<double>(length);
				for (int i=0; i<length; i++)
				{
					tmp[i] = _array[i];
				}
				return tmp;
			}
		}


			

		[JsonIgnore]
		property double default[int]
		{
			double get(int index)
			{
				if (index <0 || index >= length)
				{
					throw gcnew Exception("Error: index out of range");
				}
				return _array[index];
			}
			void set(int index, double value)
			{
				if (index <0 || index >= length)
				{
					throw gcnew Exception("Error: index out of range");
				} 
				_array[index] = value;
			}
		}

		property int Length
		{
			int get()
			{
				return length;
			}
		}

	public private:
		[JsonIgnore]
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





