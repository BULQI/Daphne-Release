using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

using HDF5DotNet;

namespace Daphne
{
    /// <summary>
    /// encapsulate the file and what it needs to know
    /// </summary>
    public class HDF5File
    {
        private string filename;
        private H5FileId fileId;
        // groups can be nested, so maintain a stack of open ones
        // note that this will only work for exclusive reads or writes, not mixed mode
        private List<H5GroupId> groupStack;
        // to retrieve a list of subgroup names
        private List<string> subGroups;

        public HDF5File()
        {
            filename = "";
            groupStack = new List<H5GroupId>();
            subGroups = new List<string>();
        }

        /// <summary>
        /// (re)create the file
        /// </summary>
        /// <param name="fn">file name</param>
        /// <returns>true for success</returns>
        public bool initialize(string fn)
        {
            if (fileId == null)
            {
                filename = fn;
                return true;
            }
            return false;
        }

        /// <summary>
        /// open it for writing
        /// <param name="trunc">true to force truncation (deletion)</param>
        /// </summary>
        public void openWrite(bool trunc)
        {
            if (filename != "" && fileId == null)
            {
                if (trunc == false && File.Exists(filename) == true)
                {
                    fileId = H5F.open(filename, H5F.OpenMode.ACC_RDWR);
                }
                else
                {
                    fileId = H5F.create(filename, H5F.CreateMode.ACC_TRUNC);
                }
            }
        }

        /// <summary>
        /// open it for reading
        /// </summary>
        public void openRead()
        {
            if (filename != "" && fileId == null)
            {
                fileId = H5F.open(filename, H5F.OpenMode.ACC_RDONLY);
            }
        }

        /// <summary>
        ///  close it
        /// </summary>
        public void close()
        {
            if (fileId != null)
            {
                H5F.close(fileId);
                fileId = null;
            }
        }

        /// <summary>
        /// clear all entries in the file
        /// </summary>
        public void clearFile()
        {
            close();
            openWrite(true);
        }

        /// <summary>
        /// a utility to find the location for creating or opening a group; groups are hierarchical, i.e. if one is open then the one
        /// to be opened or created will be with respect to the one that's open; otherwise, open / create it with respect to the file root
        /// </summary>
        /// <returns>location id</returns>
        private H5LocId findLocation()
        {
            H5LocId loc;

            // if a group is open, make this a subgroup, otherwise a standalone group
            if (groupStack.Count > 0)
            {
                loc = groupStack.Last();
            }
            else
            {
                loc = fileId;
            }
            return loc;
        }

        /// <summary>
        ///  create a new group
        /// </summary>
        /// <param name="groupName">group's name</param>
        public void createGroup(string groupName)
        {
            H5LocId loc = findLocation();

            if (loc != null)
            {
                groupStack.Add(H5G.create(loc, groupName));
            }
        }

        /// <summary>
        /// open an existing group
        /// </summary>
        /// <param name="groupName"></param>
        public void openGroup(string groupName)
        {
            try
            {
                H5LocId loc = findLocation();

                if (loc != null)
                {
                    groupStack.Add(H5G.open(loc, groupName));
                }
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        ///  try to open a group, if it doesn't exist create it
        /// </summary>
        /// <param name="groupName"></param>
        public void openCreateGroup(string groupName)
        {
            try
            {
                openGroup(groupName);
            }
            catch
            {
                createGroup(groupName);
            }
        }

        /// <summary>
        /// close the currently open group
        /// </summary>
        public void closeGroup()
        {
            if (groupStack.Count > 0)
            {
                H5G.close(groupStack.Last());
                groupStack.RemoveAt(groupStack.Count - 1);
            }
        }

        /// <summary>
        /// write integer dataset
        /// </summary>
        /// <param name="name">dataset name</param>
        /// <param name="dims">dimensions</param>
        /// <param name="data">data array</param>
        public void writeDSInt(string name, long[] dims, H5Array<int> data)
        {
            if (groupStack.Count > 0)
            {
                H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_INT);
                H5DataSpaceId spaceId = H5S.create_simple(dims.Length, dims);
                H5DataSetId dset = H5D.create(groupStack.Last(), name, typeId, spaceId);

                H5D.write(dset, typeId, data);
                H5D.close(dset);
                H5S.close(spaceId);
            }
        }

        /// <summary>
        /// read an integer dataset
        /// </summary>
        /// <param name="name">dataset name</param>
        /// <param name="data">data array</param>
        public void readDSInt(string name, ref int[] data)
        {
            if (groupStack.Count > 0)
            {
                H5DataSetId dset = H5D.open(groupStack.Last(), name);
                H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_INT);
                long size = H5D.getStorageSize(dset) / sizeof(int);

                data = new int[size];
                H5D.read(dset, typeId, new H5Array<int>(data));
                H5D.close(dset);
            }
        }

        /// <summary>
        /// write long dataset
        /// </summary>
        /// <param name="name">dataset name</param>
        /// <param name="dims">dimensions</param>
        /// <param name="data">data array</param>
        public void writeDSLong(string name, long[] dims, H5Array<long> data)
        {
            if (groupStack.Count > 0)
            {
                H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_LLONG);
                H5DataSpaceId spaceId = H5S.create_simple(dims.Length, dims);
                H5DataSetId dset = H5D.create(groupStack.Last(), name, typeId, spaceId);

                H5D.write(dset, typeId, data);
                H5D.close(dset);
                H5S.close(spaceId);
            }
        }

        /// <summary>
        /// read a long dataset
        /// </summary>
        /// <param name="name">dataset name</param>
        /// <param name="data">data array</param>
        public void readDSLong(string name, ref long[] data)
        {
            if (groupStack.Count > 0)
            {
                H5DataSetId dset = H5D.open(groupStack.Last(), name);
                H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_LLONG);
                long size = H5D.getStorageSize(dset) / sizeof(long);

                data = new long[size];
                H5D.read(dset, typeId, new H5Array<long>(data));
                H5D.close(dset);
            }
        }

        /// <summary>
        /// write double dataset
        /// </summary>
        /// <param name="name">dataset name</param>
        /// <param name="dims">dimensions</param>
        /// <param name="data">data array</param>
        public void writeDSDouble(string name, long[] dims, H5Array<double> data)
        {
            if (groupStack.Count > 0)
            {
                H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_DOUBLE);
                H5DataSpaceId spaceId = H5S.create_simple(dims.Length, dims);
                H5DataSetId dset = H5D.create(groupStack.Last(), name, typeId, spaceId);

                H5D.write(dset, typeId, data);
                H5D.close(dset);
                H5S.close(spaceId);
            }
        }

        /// <summary>
        /// read a double dataset
        /// </summary>
        /// <param name="name">dataset name</param>
        /// <param name="data">data array</param>
        public void readDSDouble(string name, ref double[] data)
        {
            if (groupStack.Count > 0)
            {
                H5DataSetId dset = H5D.open(groupStack.Last(), name);
                H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_DOUBLE);
                long size = H5D.getStorageSize(dset) / sizeof(double);

                data = new double[size];
                H5D.read(dset, typeId, new H5Array<double>(data));
                H5D.close(dset);
            }
        }

        /// <summary>
        /// write a string dataset
        /// </summary>
        /// <param name="name">dataset name</param>
        /// <param name="data">the string to be written</param>
        public void writeString(string name, string data)
        {
            long[] dims = new long[] { data.Length };

            H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_SHORT);
            H5DataSpaceId spaceId = H5S.create_simple(dims.Length, dims);
            H5DataSetId dset = H5D.create(groupStack.Last(), name, typeId, spaceId);

            H5D.write(dset, typeId, new H5Array<char>(data.ToArray()));
            H5D.close(dset);
            H5S.close(spaceId);
        }

        /// <summary>
        /// read a string dataset
        /// </summary>
        /// <param name="name">dataset name</param>
        /// <param name="data">data array</param>
        public void readString(string name, ref string data)
        {
            H5DataSetId dset = H5D.open(groupStack.Last(), name);
            H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_SHORT);
            long size = H5D.getStorageSize(dset) / sizeof(char);

            char[] tmp = new char[size];

            H5D.read(dset, typeId, new H5Array<char>(tmp));
            data = new string(tmp);
            H5D.close(dset);
        }

        /// <summary>
        /// just a helper to be able to use H5G.iterate
        /// </summary>
        /// <param name="id"></param>
        /// <param name="objectName"></param>
        /// <param name="param"></param>
        /// <returns></returns>
        private int groupCallback(H5GroupId id, string objectName, Object param)
        {
            subGroups.Add(objectName);
            return 0;
        }

        /// <summary>
        /// retrieve a list of subgroup names
        /// </summary>
        /// <param name="path">path to the parent group</param>
        /// <returns>the list</returns>
        public List<string> subGroupNames(string path)
        {
            List<string> local = new List<string>();

            if (fileId != null)
            {
                int x = 0;

                subGroups.Clear();
                H5G.iterate(fileId, path, groupCallback, null, ref x);
                foreach (string s in subGroups)
                {
                    local.Add(s);
                }
            }
            return local;
        }

    }

    // define the frame data classes here because they require knowledge of hdf5 data structures

    /// <summary>
    /// one simulation frame for the tissue simulation
    /// </summary>
    public class TissueSimulationFrameData : IFrameData
    {
        private int[] cellIds;
        private double[] cellPos;

        public int[] CellIDs
        {
            get
            {
                return cellIds;
            }
        }

        public double[] CellPos
        {
            get
            {
                return cellPos;
            }
        }

        public TissueSimulationFrameData()
        {
        }

        /// <summary>
        /// prepare the data from the cells (transfer to the frame) for writing
        /// </summary>
        public void prepareData()
        {
            if (cellPos == null || cellPos.GetLength(0) != SimulationBase.dataBasket.Cells.Count)
            {
                cellPos = new double[SimulationBase.dataBasket.Cells.Count * CellSpatialState.SingleDim];
            }

            cellIds = SimulationBase.dataBasket.Cells.Keys.ToArray();

            int i = 0;

            foreach (Cell c in SimulationBase.dataBasket.Cells.Values)
            {
                for (int j = 0; j < CellSpatialState.SingleDim; j++)
                {
                    cellPos[i * CellSpatialState.SingleDim + j] = c.SpatialState.X[j];
                }
                i++;
            }
        }

        /// <summary>
        /// write a simulation frame to file by index
        /// </summary>
        /// <param name="i">frame index</param>
        public void writeData(int i)
        {
            writeData(String.Format("Frame_{0}", i));
        }

        /// <summary>
        /// write a simulation frame to file by name
        /// </summary>
        /// <param name="groupName">group name in the HDF5 file</param>
        public void writeData(string groupName)
        {
            prepareData();
            DataBasket.hdf5file.createGroup(groupName);

            // write the cell ids
            long[] dims = new long[] { SimulationBase.dataBasket.Cells.Count };

            DataBasket.hdf5file.writeDSInt("CellIDs", dims, new H5Array<int>(cellIds));

            // write the cell positions
            dims = new long[] { SimulationBase.dataBasket.Cells.Count, CellSpatialState.SingleDim };
            DataBasket.hdf5file.writeDSDouble("Position", dims, new H5Array<double>(cellPos));

            // close the group
            DataBasket.hdf5file.closeGroup();
        }

        /// <summary>
        /// read a simulation frame
        /// </summary>
        /// <param name="i">frame index</param>
        public void readData(int i)
        {
            readData(String.Format("Frame_{0}", i));
        }

        /// <summary>
        /// read a simulation frame
        /// </summary>
        /// <param name="groupName">group name in the HDF5 file</param>
        public void readData(string groupName)
        {
            DataBasket.hdf5file.openGroup(groupName);

            // read the cell ids
            long[] dims = new long[] { SimulationBase.dataBasket.Cells.Count };

            DataBasket.hdf5file.readDSInt("CellIDs", ref cellIds);

            // read the cell positions
            dims = new long[] { SimulationBase.dataBasket.Cells.Count, CellSpatialState.SingleDim };
            DataBasket.hdf5file.readDSDouble("Position", ref cellPos);

            // close the group
            DataBasket.hdf5file.closeGroup();
        }
    }
}
