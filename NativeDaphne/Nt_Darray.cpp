#include "stdafx.h"
#include <stdlib.h>
#include <malloc.h>

#include "Nt_DArray.h"

using namespace System;
using namespace System::Collections::Generic;
using namespace Newtonsoft::Json::Linq;

namespace NativeDaphne
{

	void DoubleArraySerializer::WriteJson(JsonWriter^ writer, Object^ value, JsonSerializer ^serializer) 
	{
		//if (value->GetType()->IsGenericType == false)return;
		Nt_Darray^ darray = dynamic_cast<Nt_Darray^>(value);
		if (darray == nullptr)return;
		array<double> ^darr = darray->ArrayCopy;
		writer->WriteStartArray();
		for (int i=0; i< darr->Length; i++)
		{
			writer->WriteValue(darr[i]);
		}
		writer->WriteEndArray();
	}

	Object^ DoubleArraySerializer::ReadJson(JsonReader^ reader, Type^ objectType, Object^ existingValue, JsonSerializer^ serializer) 
	{
		//auto xx = JObject::Parse(reader);
        JArray^ jArray = JArray::Load(reader);
		int count = jArray->Count;
		if (count == 0)
		{
			return gcnew Nt_Darray();
		}
		Nt_Darray^ darray = gcnew Nt_Darray(count);
		for (int i=0; i<count; i++)
		{
			darray[i] = (double)jArray[i];
		}
        return darray;
	}
}