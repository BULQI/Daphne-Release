#ifdef COMPILE_FLAG
#define DllExport __declspec(dllexport)
#else 
#define DllExport __declspec(dllimport)
#endif


namespace NativeDaphneLibrary
{
	class DllExport NtInterpolatedRectangularPrism
	{

		int nodePerSide0;
		int nodePerSide1;
		int nodePerSide2;
		double stepSize;

		//data for laplacian
		double coef1;
		double coef2;
		int *lpindex;
		int *sfindex;
		int indexInc[8];

		double *_tmparr;

		//for laplacianv0 only
		int *lpindex_inbound;
		int *sfindex_inbound;
		int inbound_length;

		//data for restrict
		int num_restrict_node;
		double *NodeIndex;	//for pos/sepsize in x, y z.
		double *Delta;		//for dx dy dz
		double *Omdelta;	//for 1-dx, 1-dy, 1-dz
		double *D1Array;	//constant 1.0

	public:

		NtInterpolatedRectangularPrism();

		NtInterpolatedRectangularPrism(int* index_operator, int* side_lens, double step_size, double _coef1, double _coef2);

		~NtInterpolatedRectangularPrism();

		int Laplacian(double *sfarray, double *retval, int n);

		//for testing access
		int TestAddition(int a, int b);

	};
}





