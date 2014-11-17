using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using HDF5DotNet;

namespace TestHDF
{
    /// <summary>
    /// encapsulate the file and what it needs to know
    /// </summary>
    class HDF5File
    {
        private string filename;
        private H5FileId fileId;
        // groups can be nested, so maintain a stack of open ones
        // note that this will only work for exclusive reads or writes, not mixed mode
        private List<H5GroupId> groupStack;

        public HDF5File(string fn)
        {
            filename = fn;
            groupStack = new List<H5GroupId>();
        }

        /// <summary>
        /// open it for writing
        /// </summary>
        public void openWrite()
        {
            fileId = H5F.create(filename, H5F.CreateMode.ACC_TRUNC);
        }

        /// <summary>
        /// open it for reading
        /// </summary>
        public void openRead()
        {
            fileId = H5F.open(filename, H5F.OpenMode.ACC_RDONLY);
        }

        /// <summary>
        ///  close it
        /// </summary>
        public void close()
        {
            H5F.close(fileId);
        }

        /// <summary>
        ///  create a new group
        /// </summary>
        /// <param name="groupName">group's name</param>
        public void createGroup(string groupName)
        {
            groupStack.Add(H5G.create(fileId, groupName));
        }

        /// <summary>
        /// open an existing group
        /// </summary>
        /// <param name="groupName"></param>
        public void openGroup(string groupName)
        {
            groupStack.Add(H5G.open(fileId, groupName));
        }

        /// <summary>
        /// close the currently open group
        /// </summary>
        public void closeGroup()
        {
            H5G.close(groupStack.Last());
            groupStack.RemoveAt(groupStack.Count - 1);
        }

        /// <summary>
        /// write integer dataset
        /// </summary>
        /// <param name="name">dataset name</param>
        /// <param name="dims">dimensions</param>
        /// <param name="data">data array</param>
        public void writeDSInt(string name, long[] dims, H5Array<int> data)
        {
            H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_INT);
            H5DataSpaceId spaceId = H5S.create_simple(dims.Length, dims);
            H5DataSetId dset = H5D.create(groupStack.Last(), name, typeId, spaceId);

            H5D.write(dset, typeId, data);
            H5D.close(dset);
            H5S.close(spaceId);
        }

        /// <summary>
        /// read an integer dataset
        /// </summary>
        /// <param name="name">dataset name</param>
        /// <param name="data">data array</param>
        public void readDSInt(string name, ref int[] data)
        {
            H5DataSetId dset = H5D.open(groupStack.Last(), name);
            H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_INT);
            long size = H5D.getStorageSize(dset) / sizeof(int);

            data = new int[size];
            H5D.read(dset, typeId, new H5Array<int>(data));
            H5D.close(dset);
        }

        /// <summary>
        /// write long dataset
        /// </summary>
        /// <param name="name">dataset name</param>
        /// <param name="dims">dimensions</param>
        /// <param name="data">data array</param>
        public void writeDSLong(string name, long[] dims, H5Array<long> data)
        {
            H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_LLONG);
            H5DataSpaceId spaceId = H5S.create_simple(dims.Length, dims);
            H5DataSetId dset = H5D.create(groupStack.Last(), name, typeId, spaceId);

            H5D.write(dset, typeId, data);
            H5D.close(dset);
            H5S.close(spaceId);
        }

        /// <summary>
        /// read a long dataset
        /// </summary>
        /// <param name="name">dataset name</param>
        /// <param name="data">data array</param>
        public void readDSLong(string name, ref long[] data)
        {
            H5DataSetId dset = H5D.open(groupStack.Last(), name);
            H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_LLONG);
            long size = H5D.getStorageSize(dset) / sizeof(long);

            data = new long[size];
            H5D.read(dset, typeId, new H5Array<long>(data));
            H5D.close(dset);
        }

        /// <summary>
        /// write double dataset
        /// </summary>
        /// <param name="name">dataset name</param>
        /// <param name="dims">dimensions</param>
        /// <param name="data">data array</param>
        public void writeDSDouble(string name, long[] dims, H5Array<double> data)
        {
            H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_DOUBLE);
            H5DataSpaceId spaceId = H5S.create_simple(dims.Length, dims);
            H5DataSetId dset = H5D.create(groupStack.Last(), name, typeId, spaceId);

            H5D.write(dset, typeId, data);
            H5D.close(dset);
            H5S.close(spaceId);
        }

        /// <summary>
        /// read a double dataset
        /// </summary>
        /// <param name="name">dataset name</param>
        /// <param name="data">data array</param>
        public void readDSDouble(string name, ref double[] data)
        {
            H5DataSetId dset = H5D.open(groupStack.Last(), name);
            H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_DOUBLE);
            long size = H5D.getStorageSize(dset) / sizeof(double);

            data = new double[size];
            H5D.read(dset, typeId, new H5Array<double>(data));
            H5D.close(dset);
        }
    }
}
