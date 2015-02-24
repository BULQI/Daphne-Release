// stdafx.cpp : source file that includes just the standard includes
// NativeDaphne.pch will be the pre-compiled header
// stdafx.obj will contain the pre-compiled type information

#include "stdafx.h"

#ifdef _DEBUG

class MemoryCheckInitializer
{
public:
   MemoryCheckInitializer()
   {
      // Guaranty call to _CrtDumpMemoryLeaks()
      _CrtSetDbgFlag(_CRTDBG_ALLOC_MEM_DF | _CRTDBG_LEAK_CHECK_DF);
      // Set output back to debug window
      _CrtSetReportMode(_CRT_ERROR, _CRTDBG_MODE_DEBUG);
   }
};

// Just call initializations
static MemoryCheckInitializer mci_;

#endif