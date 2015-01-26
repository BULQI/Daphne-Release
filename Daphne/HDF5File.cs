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
        private string fullPath, fileName;
        private H5FileId fileId;
        // groups can be nested, so maintain a stack of open ones
        // note that this will only work for exclusive reads or writes, not mixed mode
        private List<H5GroupId> groupStack;
        // to retrieve a list of subgroup names
        private List<string> subGroups;

        public HDF5File()
        {
            fullPath = "";
            fileName = "";
            groupStack = new List<H5GroupId>();
            subGroups = new List<string>();
        }

        /// <summary>
        /// access the file name
        /// </summary>
        public string FileName
        {
            get
            {
                return fileName;
            }
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
                fullPath = fn;
                // extract the last part, file name alone
                fileName = System.IO.Path.GetFileName(fullPath);
                return true;
            }
            return false;
        }

        /// <summary>
        /// assemble the full path given it's parts; unique upon demand
        /// </summary>
        /// <param name="path"></param>
        /// <param name="name"></param>
        /// <param name="userDef"></param>
        /// <param name="extension"></param>
        /// <returns></returns>
        public bool assembleFullPath(string path, string name, string userDef, string extension, bool unique)
        {
            string buildPath = path + name + "_" + userDef + extension;
            int version = 1;

            // unique name
            while(unique == true && File.Exists(buildPath) == true)
            {
                buildPath = path + name + "_" + userDef + "(" + version + ")" + extension;
                version++;
            }
            return initialize(buildPath);
        }

        /// <summary>
        /// open it for writing
        /// <param name="trunc">true to force truncation (deletion)</param>
        /// </summary>
        public void openWrite(bool trunc)
        {
            if (fullPath != "" && fileId == null)
            {
                if (trunc == false && File.Exists(fullPath) == true)
                {
                    try
                    {
                        fileId = H5F.open(fullPath, H5F.OpenMode.ACC_RDWR);
                    }
                    catch
                    {
                    }
                }
                else
                {
                    try
                    {
                        fileId = H5F.create(fullPath, H5F.CreateMode.ACC_TRUNC);
                    }
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>
        /// open it for reading
        /// </summary>
        public bool openRead()
        {
            if (fullPath != "" && File.Exists(fullPath) == true && fileId == null)
            {
                try
                {
                    fileId = H5F.open(fullPath, H5F.OpenMode.ACC_RDONLY);
                }
                catch
                {
                }
                return true;
            }
            return false;
        }

        /// <summary>
        ///  close it
        /// </summary>
        public void close(bool closeGroups)
        {
            if (fileId != null)
            {
                if (closeGroups == true)
                {
                    closeAllGroups();
                }
                H5F.close(fileId);
                fileId = null;
            }
        }

        /// <summary>
        /// indicates if file is currently open
        /// </summary>
        /// <returns>true for open</returns>
        public bool isOpen()
        {
            return fileId != null;
        }

        /// <summary>
        /// clear all entries in the file
        /// </summary>
        public void clearFile()
        {
            close(true);
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
        /// close all open groups
        /// </summary>
        public void closeAllGroups()
        {
            while (groupStack.Count > 0)
            {
                closeGroup();
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

                if (data == null || data.Length != size)
                {
                    data = new int[size];
                }
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

                if (data == null || data.Length != size)
                {
                    data = new long[size];
                }
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

                if (data == null || data.Length != size)
                {
                    data = new double[size];
                }
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
            if (groupStack.Count > 0)
            {
                long[] dims = new long[] { data.Length };

                H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_SHORT);
                H5DataSpaceId spaceId = H5S.create_simple(dims.Length, dims);
                H5DataSetId dset = H5D.create(groupStack.Last(), name, typeId, spaceId);

                H5D.write(dset, typeId, new H5Array<char>(data.ToArray()));
                H5D.close(dset);
                H5S.close(spaceId);
            }
        }

        /// <summary>
        /// read a string dataset
        /// </summary>
        /// <param name="name">dataset name</param>
        /// <param name="data">data array</param>
        public void readString(string name, ref string data)
        {
            if (groupStack.Count > 0)
            {
                H5DataSetId dset = H5D.open(groupStack.Last(), name);
                H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_SHORT);
                long size = H5D.getStorageSize(dset) / sizeof(char);

                char[] tmp = new char[size];

                H5D.read(dset, typeId, new H5Array<char>(tmp));
                data = new string(tmp);
                H5D.close(dset);
            }
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
        // linear numbering for behavior bookkeeping, b_count must indicate the number of behaviors
        private int B_DEATH = 0,
                    B_DIV = 1,
                    B_DIFF = 2,
                    B_COUNT = 3,
                    S_POS = 0,
                    S_VEL = 1,
                    S_FORCE = 2;
        private int cellCount;
        private int[] cellIds, cellGens, cellPopIds, cellBehaviors;
        private double[] cellStateSpatial;
        private double[][] ecsMolpops, cellStateGenes;

        public int CellCount
        {
            get
            {
                return cellCount;
            }
        }

        public int[] CellIDs
        {
            get
            {
                return cellIds;
            }
        }

        public int[] CellGens
        {
            get
            {
                return cellGens;
            }
        }

        public int[] CellPopIDs
        {
            get
            {
                return cellPopIds;
            }
        }

        public int[] CellBehaviors
        {
            get
            {
                return cellBehaviors;
            }
        }

        public double[] CellStateSpatial
        {
            get
            {
                return cellStateSpatial;
            }
        }

        public double[][] CellStateGenes
        {
            get
            {
                return cellStateGenes;
            }
        }

        public double[][] ECSMolPops
        {
            get
            {
                return ecsMolpops;
            }
        }

        public TissueSimulationFrameData()
        {
        }

        /// <summary>
        /// create ecs data array
        /// </summary>
        private void createECSData()
        {
            // ECS, create the data space, size equal to number of molpops
            int mpSize = 0,
                length = SimulationBase.ProtocolHandle.scenario.environment.comp.molpops.Count;

            // if needed, find the molpop size; each molpop same size, pick the first one
            if (length > 0)
            {
                ConfigMolecularPopulation first = SimulationBase.ProtocolHandle.scenario.environment.comp.molpops.First();

                mpSize = SimulationBase.dataBasket.Environment.Comp.Populations[first.molecule.entity_guid].Conc.M.ArraySize;
            }

            // create the outer array
            if (ecsMolpops == null || ecsMolpops.Length != length)
            {
                ecsMolpops = new double[length][];
            }
            // create the inner arrays, one per molpop
            for (int i = 0; i < length; i++)
            {
                if (ecsMolpops[i] == null || ecsMolpops[i].Length != mpSize)
                {
                    ecsMolpops[i] = new double[mpSize];
                }
            }
        }

        /// <summary>
        /// create the genes data array
        /// </summary>
        private void createGenesData()
        {
            if (cellStateGenes == null || cellStateGenes.Length != cellCount)
            {
                cellStateGenes = new double[cellCount][];
            }

            int i = 0;

            foreach (Cell c in SimulationBase.dataBasket.Cells.Values)
            {
                CellPopulation cellPop = ((TissueScenario)SimulationBase.ProtocolHandle.scenario).GetCellPopulation(c.Population_id);

                if (cellStateGenes[i] == null || cellStateGenes[i].Length != cellPop.Cell.genes.Count)
                {
                    cellStateGenes[i] = new double[cellPop.Cell.genes.Count];
                }
                i++;
            }
        }

        /// <summary>
        /// prepare the data from the cells (transfer to the frame) for writing
        /// </summary>
        private void prepareData()
        {
            int i;

            cellCount = SimulationBase.dataBasket.Cells.Count;
            // write the cell count at any rate
            writeInt(cellCount, "CellCount");

            if (cellCount > 0)
            {
                // create the data arrays if needed
                if (cellStateSpatial == null || cellStateSpatial.Length != cellCount * CellSpatialState.Dim)
                {
                    cellStateSpatial = new double[cellCount * CellSpatialState.Dim];
                    cellGens = new int[cellCount];
                    cellPopIds = new int[cellCount];
                    cellBehaviors = new int[cellCount * B_COUNT];
                }
                // need to do this regardless whether the cell number changed; it could be the same if the same number of cells died and got born, but
                // depending on cell type we could have different gene numbers
                createGenesData();

                // fill the data arrays
                cellIds = SimulationBase.dataBasket.Cells.Keys.ToArray();

                i = 0;
                foreach (Cell c in SimulationBase.dataBasket.Cells.Values)
                {
                    int j;

                    // spatial state
                    for (j = 0; j < CellSpatialState.SingleDim; j++)
                    {
                        cellStateSpatial[i * CellSpatialState.Dim + CellSpatialState.SingleDim * S_POS + j] = c.SpatialState.X[j];
                        cellStateSpatial[i * CellSpatialState.Dim + CellSpatialState.SingleDim * S_VEL + j] = c.SpatialState.V[j];
                        cellStateSpatial[i * CellSpatialState.Dim + CellSpatialState.SingleDim * S_FORCE + j] = c.SpatialState.F[j];
                    }
                    // generation
                    cellGens[i] = c.generation;
                    // population
                    cellPopIds[i] = c.Population_id;
                    // death
                    cellBehaviors[i * B_COUNT + B_DEATH] = c.Alive == true ? 0 : 1;
                    // division
                    cellBehaviors[i * B_COUNT + B_DIV] = c.DividerState;
                    // differentiation
                    cellBehaviors[i * B_COUNT + B_DIFF] = c.DifferentiationState;

                    // genes
                    CellPopulation cellPop = ((TissueScenario)SimulationBase.ProtocolHandle.scenario).GetCellPopulation(c.Population_id);

                    j = 0;
                    foreach (ConfigGene gene in cellPop.Cell.genes)
                    {
                        cellStateGenes[i][j] = c.Genes[gene.entity_guid].ActivationLevel;
                        j++;
                    }

                    i++;
                }
            }

            // ECS, create space
            createECSData();

            // ECS, fill the data space
            i = 0;
            foreach (ConfigMolecularPopulation cmp in SimulationBase.ProtocolHandle.scenario.environment.comp.molpops)
            {
                MolecularPopulation cur_mp = SimulationBase.dataBasket.Environment.Comp.Populations[cmp.molecule.entity_guid];

                cur_mp.Conc.CopyArray(ecsMolpops[i]);
            }
        }

        /// <summary>
        /// get the state for a cell by index and write it into the CellState parameter
        /// </summary>
        /// <param name="idx">cell index</param>
        /// <param name="state"></param>
        public void applyStateByIndex(int idx, ref CellState state)
        {
            int i;

            // set the spatial state
            for (i = 0; i < CellSpatialState.SingleDim; i++)
            {
                state.spState.X[i] = cellStateSpatial[idx * CellSpatialState.Dim + CellSpatialState.SingleDim * S_POS + i];
                state.spState.V[i] = cellStateSpatial[idx * CellSpatialState.Dim + CellSpatialState.SingleDim * S_VEL + i];
                state.spState.F[i] = cellStateSpatial[idx * CellSpatialState.Dim + CellSpatialState.SingleDim * S_FORCE + i];
            }
            // set the generation
            state.setCellGeneration(cellGens[idx]);
            // death
            state.setDeathDriverState(cellBehaviors[idx * B_COUNT + B_DEATH]);
            // division
            state.setDivisonDriverState(cellBehaviors[idx * B_COUNT + B_DIV]);
            // differentiation
            state.setDifferentiationDriverState(cellBehaviors[idx * B_COUNT + B_DIFF]);

            // genes
            CellPopulation cellPop = ((TissueScenario)SimulationBase.ProtocolHandle.scenario).GetCellPopulation(CellPopIDs[idx]);

            state.cgState.geneDict.Clear();
            i = 0;
            foreach (ConfigGene gene in cellPop.Cell.genes)
            {
                state.setGeneState(gene.entity_guid, cellStateGenes[idx][i]);
                i++;
            }
        }

        private void writeInt(int val, string name)
        {
            long[] dim = new long[] { 1 };
            int[] data = new int[] { val };

            DataBasket.hdf5file.writeDSInt(name, dim, new H5Array<int>(data));
        }

        private int readInt(string name)
        {
            int[] data = null;

            DataBasket.hdf5file.readDSInt(name, ref data);
            return data[0];
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
            long[] dims = null;

            DataBasket.hdf5file.createGroup(groupName);

            prepareData();
            if (cellCount > 0)
            {
                // write the cell ids
                dims = new long[] { cellCount };

                // ids
                DataBasket.hdf5file.writeDSInt("CellIDs", dims, new H5Array<int>(cellIds));
                // generations
                DataBasket.hdf5file.writeDSInt("CellGens", dims, new H5Array<int>(cellGens));
                // population ids
                DataBasket.hdf5file.writeDSInt("CellPopIDs", dims, new H5Array<int>(cellPopIds));

                // gene activations
                for (int i = 0; i < cellStateGenes.Length; i++)
                {
                    if (cellStateGenes[i].Length > 0)
                    {
                        dims[0] = cellStateGenes[i].Length;
                        DataBasket.hdf5file.writeDSDouble("GeneState" + i, dims, new H5Array<double>(cellStateGenes[i]));
                    }
                }

                // behaviors
                dims = new long[] { cellCount, B_COUNT };
                DataBasket.hdf5file.writeDSInt("CellBehaviors", dims, new H5Array<int>(cellBehaviors));

                // write the cell spatial states
                dims[0] = cellCount;
                dims[1] = CellSpatialState.Dim;
                DataBasket.hdf5file.writeDSDouble("SpatialState", dims, new H5Array<double>(cellStateSpatial));
            }

            // ecs
            for (int i = 0; i < ecsMolpops.Length; i++)
            {
                if (i == 0)
                {
                    // index 0 is guaranteed to exist because of the loop over i
                    dims = new long[] { ecsMolpops[0].Length };
                }
                DataBasket.hdf5file.writeDSDouble("ECS" + i, dims, new H5Array<double>(ecsMolpops[i]));
            }

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

            cellCount = readInt("CellCount");
            if (cellCount > 0)
            {
                // ids
                DataBasket.hdf5file.readDSInt("CellIDs", ref cellIds);
                // generations
                DataBasket.hdf5file.readDSInt("CellGens", ref cellGens);
                // population ids
                DataBasket.hdf5file.readDSInt("CellPopIDs", ref cellPopIds);

                // gene activations
                createGenesData();
                for (int i = 0; i < cellStateGenes.Length; i++)
                {
                    if (cellStateGenes[i].Length > 0)
                    {
                        DataBasket.hdf5file.readDSDouble("GeneState" + i, ref cellStateGenes[i]);
                    }
                }

                // behaviors
                DataBasket.hdf5file.readDSInt("CellBehaviors", ref cellBehaviors);

                // read the cell spatial states
                DataBasket.hdf5file.readDSDouble("SpatialState", ref cellStateSpatial);
            }

            // ecs
            createECSData();
            for (int i = 0; i < ecsMolpops.Length; i++)
            {
                DataBasket.hdf5file.readDSDouble("ECS" + i, ref ecsMolpops[i]);
            }

            // close the group
            DataBasket.hdf5file.closeGroup();
        }
    }
}
