using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;

using HDF5DotNet;

namespace Daphne
{
    /// <summary>
    /// encapsulate the file and what it needs to know
    /// </summary>
    public abstract class HDF5FileBase
    {
        private string fullPath, fileName, filePath;
        private H5FileId fileId;
        // groups can be nested, so maintain a stack of open ones
        // note that this will only work for exclusive reads or writes, not mixed mode
        private List<H5GroupId> groupStack;
        // to retrieve a list of subgroup names
        private List<string> subGroups;

        public HDF5FileBase()
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
        /// access the file path
        /// </summary>
        public string FilePath
        {
            get
            {
                return filePath;
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
                // path only
                filePath = System.IO.Path.GetDirectoryName(fullPath) + @"\";
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
                H5T.close(typeId);
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
                H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_INT);
                H5DataSetId dset = H5D.open(groupStack.Last(), name);
                long size = H5D.getStorageSize(dset) / sizeof(int);

                if (data == null || data.Length != size)
                {
                    data = new int[size];
                }
                H5D.read(dset, typeId, new H5Array<int>(data));
                H5D.close(dset);
                H5T.close(typeId);
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
                H5T.close(typeId);
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
                H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_LLONG);
                H5DataSetId dset = H5D.open(groupStack.Last(), name);
                long size = H5D.getStorageSize(dset) / sizeof(long);

                if (data == null || data.Length != size)
                {
                    data = new long[size];
                }
                H5D.read(dset, typeId, new H5Array<long>(data));
                H5D.close(dset);
                H5T.close(typeId);
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
                H5T.close(typeId);
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
                H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_DOUBLE);
                H5DataSetId dset = H5D.open(groupStack.Last(), name);
                long size = H5D.getStorageSize(dset) / sizeof(double);

                if (data == null || data.Length != size)
                {
                    data = new double[size];
                }
                H5D.read(dset, typeId, new H5Array<double>(data));
                H5D.close(dset);
                H5T.close(typeId);
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
                H5T.close(typeId);
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
                H5DataTypeId typeId = H5T.copy(H5T.H5Type.NATIVE_SHORT);
                H5DataSetId dset = H5D.open(groupStack.Last(), name);
                long size = H5D.getStorageSize(dset) / sizeof(char);

                char[] tmp = new char[size];

                H5D.read(dset, typeId, new H5Array<char>(tmp));
                data = new string(tmp);
                H5D.close(dset);
                H5T.close(typeId);
            }
        }

        /// <summary>
        /// convenience, write a single int
        /// </summary>
        /// <param name="val">the integer value</param>
        /// <param name="name">the name in the hdf5 file</param>
        public void writeInt(int val, string name)
        {
            long[] dim = new long[] { 1 };
            int[] data = new int[] { val };

            writeDSInt(name, dim, new H5Array<int>(data));
        }

        /// <summary>
        /// convenience, read a single int
        /// </summary>
        /// <param name="name">the name in the hdf5 file</param>
        /// <returns>the value</returns>
        public int readInt(string name)
        {
            int[] data = null;

            readDSInt(name, ref data);
            return data[0];
        }

        /// <summary>
        /// convenience, write a single double
        /// </summary>
        /// <param name="val">the double value</param>
        /// <param name="name">the name in the hdf5 file</param>
        public void writeDouble(double val, string name)
        {
            long[] dim = new long[] { 1 };
            double[] data = new double[] { val };

            writeDSDouble(name, dim, new H5Array<double>(data));
        }

        /// <summary>
        /// convenience, read a single double
        /// </summary>
        /// <param name="name">the name in the hdf5 file</param>
        /// <returns>the value</returns>
        public double readDouble(string name)
        {
            double[] data = null;

            readDSDouble(name, ref data);
            return data[0];
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

        public void ReadReporterFileNamesFromClosedFile(string file)
        {
            // if the file is open we'll have to close it
            if (isOpen() == true)
            {
                // close the file and all open groups
                close(true);
            }
            // the file got regenerated, reopen
            initialize(file);
            if (openRead() == false)
            {
                MessageBox.Show("The HDF5 file could not be opened or does not exist.", "HDF5 error", MessageBoxButton.OK);
                return;
            }
            // open the experiment parent group
            openGroup("/Experiment");
            // read the reporter file names
            ReadReporterFileNames();
            // close the file and all groups
            close(true);
        }

        public abstract void StartHDF5File(SimulationBase sim, string protocolString, bool trunc);
        public abstract void WriteReporterFileNames();
        public abstract void ReadReporterFileNames();
    }

    public class VatReactionComplexHDF5File : HDF5FileBase
    {
        private VatReactionComplex hSim;

        public VatReactionComplexHDF5File(VatReactionComplex sim)
        {
            hSim = sim;
        }

        public override void StartHDF5File(SimulationBase sim, string protocolString, bool trunc)
        {
            if (assembleFullPath(hSim.Reporter.UniquePath, hSim.Reporter.FileNameBase, "rep", ".hdf5", true) == false)
            {
                MessageBox.Show("Error setting HDF5 filename. File might be currently open.", "HDF5 error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            openWrite(trunc);
            // group for this experiment
            createGroup("/Experiment");

            // the protocol as string; needed to reload arbitrary past experiments
            writeString("Protocol", protocolString);
        }

        public override void WriteReporterFileNames()
        {
            hSim.Reporter.WriteReporterFileNamesToHDF5(this);
        }

        public override void ReadReporterFileNames()
        {
            hSim.Reporter.ReadReporterFileNamesFromHDF5(this);
        }
    }

    public class TissueSimulationHDF5File : HDF5FileBase
    {
        private TissueSimulation hSim;

        public TissueSimulationHDF5File(TissueSimulation sim)
        {
            hSim = sim;
        }

        public override void StartHDF5File(SimulationBase sim, string protocolString, bool trunc)
        {
            if (assembleFullPath(hSim.Reporter.UniquePath, hSim.Reporter.FileNameBase, "vcr", ".hdf5", true) == false)
            {
                MessageBox.Show("Error setting HDF5 filename. File might be currently open.", "HDF5 error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            openWrite(trunc);
            // group for this experiment
            createGroup("/Experiment");

            // the protocol as string; needed to reload arbitrary past experiments
            writeString("Protocol", protocolString);
            // frames group
            createGroup("VCR_Frames");
        }

        public override void WriteReporterFileNames()
        {
            // closes the VCR_Frames group
            closeGroup();
            hSim.Reporter.WriteReporterFileNamesToHDF5(this);
        }

        public override void ReadReporterFileNames()
        {
            hSim.Reporter.ReadReporterFileNamesFromHDF5(this);
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
                    // state index
                    S_POS = 0,
                    S_VEL = 1,
                    S_FORCE = 2,
                    // linear numbering for molecules, m_count states number of compartments
                    M_CYTOSOL = 0,
                    M_MEMBRANE = 1,
                    M_COUNT = 2;
        private int cellCount;
        private int[] cellIds, cellGens, cellPopIds, cellBehaviors;
        private double[] cellStateSpatial;
        private double[][] ecsMolpops, cellStateGenes, cellStateMolecules;
        private TissueSimulationHDF5File hdf5file;

        /// <summary>
        /// constructor
        /// </summary>
        /// <param name="ts_hdf5file">handle to the hdf5 file; stored locally for conveninece</param>
        public TissueSimulationFrameData(TissueSimulationHDF5File ts_hdf5file)
        {
            hdf5file = ts_hdf5file;
        }

        /// <summary>
        /// access cell count, number of cells in this frame
        /// </summary>
        public int CellCount
        {
            get
            {
                return cellCount;
            }
        }

        /// <summary>
        /// access array of cell ids in this frame
        /// </summary>
        public int[] CellIDs
        {
            get
            {
                return cellIds;
            }
        }

        /// <summary>
        /// access array of cell generations in this frame
        /// </summary>
        public int[] CellGens
        {
            get
            {
                return cellGens;
            }
        }

        /// <summary>
        /// access array of cell population ids in this frame
        /// </summary>
        public int[] CellPopIDs
        {
            get
            {
                return cellPopIds;
            }
        }

        /// <summary>
        /// access array of cell behaviors in this frame
        /// </summary>
        public int[] CellBehaviors
        {
            get
            {
                return cellBehaviors;
            }
        }

        /// <summary>
        /// access array of spatal cell states in this frame
        /// </summary>
        public double[] CellStateSpatial
        {
            get
            {
                return cellStateSpatial;
            }
        }

        /// <summary>
        /// access array of cell genes in this frame
        /// </summary>
        public double[][] CellStateGenes
        {
            get
            {
                return cellStateGenes;
            }
        }

        /// <summary>
        /// access array of cell molecules in this frame
        /// </summary>
        public double[][] CellStateMolecules
        {
            get
            {
                return cellStateMolecules;
            }
        }

        /// <summary>
        /// access array of ecs molecular populations in this frame
        /// </summary>
        public double[][] ECSMolPops
        {
            get
            {
                return ecsMolpops;
            }
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
        /// <param name="inner">true for complete creation including the inner arrays</param>
        private void createGenesData(bool inner)
        {
            if (cellStateGenes == null || cellStateGenes.Length != cellCount)
            {
                cellStateGenes = new double[cellCount][];
            }

            if (inner == true)
            {
                int i = 0;

                foreach (Cell c in SimulationBase.dataBasket.Cells.Values)
                {
                    CellPopulation cellPop = ((TissueScenario)SimulationBase.ProtocolHandle.scenario).GetCellPopulation(c.Population_id);
                    // make sure the arrays are non-zero length
                    int nonZeroLength = cellPop.Cell.genes.Count;

                    if (nonZeroLength == 0)
                    {
                        nonZeroLength = 1;
                    }

                    if (cellStateGenes[i] == null || cellStateGenes[i].Length != nonZeroLength)
                    {
                        cellStateGenes[i] = new double[nonZeroLength];
                    }
                    i++;
                }
            }
        }

        /// <summary>
        /// create the molecules data array
        /// </summary>
        /// <param name="inner">true for complete creation including the inner arrays</param>
        private void createMoleculesData(bool inner)
        {
            if (cellStateMolecules == null || cellStateMolecules.Length != cellCount * M_COUNT)
            {
                cellStateMolecules = new double[cellCount * M_COUNT][];
            }

            if (inner == true)
            {
                int i = 0;

                foreach (Cell c in SimulationBase.dataBasket.Cells.Values)
                {
                    CellPopulation cellPop = ((TissueScenario)SimulationBase.ProtocolHandle.scenario).GetCellPopulation(c.Population_id);
                    // make sure the arrays are non-zero length
                    int nonZeroLengthCytosol = cellPop.Cell.cytosol.molpops.Count,
                        nonZeroLengthMembrane = cellPop.Cell.membrane.molpops.Count,
                        index;

                    // cytosol
                    if (nonZeroLengthCytosol == 0)
                    {
                        nonZeroLengthCytosol = 1;
                    }
                    else
                    {
                        nonZeroLengthCytosol *= c.Cytosol.Interior.ArraySize;
                    }

                    // membrane
                    if (nonZeroLengthMembrane == 0)
                    {
                        nonZeroLengthMembrane = 1;
                    }
                    else
                    {
                        nonZeroLengthMembrane *= c.PlasmaMembrane.Interior.ArraySize;
                    }

                    // create inner array for cytosol molecules
                    index = i * M_COUNT + M_CYTOSOL;
                    if (cellStateMolecules[index] == null || cellStateMolecules[index].Length != nonZeroLengthCytosol)
                    {
                        cellStateMolecules[index] = new double[nonZeroLengthCytosol];
                    }
                    // create inner array for membrane molecules
                    index = i * M_COUNT + M_MEMBRANE;
                    if (cellStateMolecules[index] == null || cellStateMolecules[index].Length != nonZeroLengthMembrane)
                    {
                        cellStateMolecules[index] = new double[nonZeroLengthMembrane];
                    }

                    i++;
                }
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
            hdf5file.writeInt(cellCount, "CellCount");

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
                createGenesData(true);
                // likewise for molecules
                createMoleculesData(true);

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

                    // we ensure non-zero length arrays; write -1 in that case, which is an implausible value for a gene
                    if (cellPop.Cell.genes.Count == 0)
                    {
                        cellStateGenes[i][0] = -1;
                    }
                    else
                    {
                        j = 0;
                        foreach (ConfigGene gene in cellPop.Cell.genes)
                        {
                            cellStateGenes[i][j] = c.Genes[gene.entity_guid].ActivationLevel;
                            j++;
                        }
                    }

                    // cytosol molecules
                    // we ensure non-zero length arrays; write -1 in that case
                    if (cellPop.Cell.cytosol.molpops.Count == 0)
                    {
                        cellStateMolecules[i * M_COUNT + M_CYTOSOL][0] = -1;
                    }
                    else
                    {
                        j = 0;
                        foreach (ConfigMolecularPopulation molpop in cellPop.Cell.cytosol.molpops)
                        {
                            c.Cytosol.Populations[molpop.molecule.entity_guid].Conc.CopyArray(cellStateMolecules[i * M_COUNT + M_CYTOSOL], j * c.Cytosol.Interior.ArraySize);
                            j++;
                        }
                    }

                    // membrane molecules
                    // we ensure non-zero length arrays; write -1 in that case
                    if (cellPop.Cell.membrane.molpops.Count == 0)
                    {
                        cellStateMolecules[i * M_COUNT + M_MEMBRANE][0] = -1;
                    }
                    else
                    {
                        j = 0;
                        foreach (ConfigMolecularPopulation molpop in cellPop.Cell.membrane.molpops)
                        {
                            c.PlasmaMembrane.Populations[molpop.molecule.entity_guid].Conc.CopyArray(cellStateMolecules[i * M_COUNT + M_MEMBRANE], j * c.PlasmaMembrane.Interior.ArraySize);
                            j++;
                        }
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
                i++;
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
            CellPopulation cellPop = ((TissueScenario)SimulationBase.ProtocolHandle.scenario).GetCellPopulation(cellPopIds[idx]);

            state.cgState.geneDict.Clear();
            i = 0;
            // cellStateGenes[idx] will always have non-zero length, but this will skip artificial entries when there are no genes present,
            // i.e. when length is one with the element being the result of padding
            foreach (ConfigGene gene in cellPop.Cell.genes)
            {
                state.setGeneState(gene.entity_guid, cellStateGenes[idx][i]);
                i++;
            }

            double[] vals;
            int blockSize = 0;

            state.cmState.molPopDict.Clear();
            // cytosol molecules
            i = 0;
            foreach (ConfigMolecularPopulation molpop in cellPop.Cell.cytosol.molpops)
            {
                // create value array
                if (blockSize == 0)
                {
                    blockSize = cellStateMolecules[idx * M_COUNT + M_CYTOSOL].Length / cellPop.Cell.cytosol.molpops.Count;
                }
                vals = new double[blockSize];
                // fill value array
                for (int j = 0; j < blockSize; j++)
                {
                    vals[j] = cellStateMolecules[idx * M_COUNT + M_CYTOSOL][i * blockSize + j];
                }
                // transfer to state
                state.addMolPopulation(molpop.molecule.entity_guid, vals);
                i++;
            }

            // membrane molecules
            blockSize = 0;
            i = 0;
            foreach (ConfigMolecularPopulation molpop in cellPop.Cell.membrane.molpops)
            {
                // create value array
                if (blockSize == 0)
                {
                    blockSize = cellStateMolecules[idx * M_COUNT + M_MEMBRANE].Length / cellPop.Cell.membrane.molpops.Count;
                }
                vals = new double[blockSize];
                // fill value array
                for (int j = 0; j < blockSize; j++)
                {
                    vals[j] = cellStateMolecules[idx * M_COUNT + M_MEMBRANE][i * blockSize + j];
                }
                // transfer to state
                state.addMolPopulation(molpop.molecule.entity_guid, vals);
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
            long[] dims = null;

            hdf5file.createGroup(groupName);

            prepareData();
            if (cellCount > 0)
            {
                // write the cell ids
                dims = new long[] { cellCount };

                // ids
                hdf5file.writeDSInt("CellIDs", dims, new H5Array<int>(cellIds));
                // generations
                hdf5file.writeDSInt("CellGens", dims, new H5Array<int>(cellGens));
                // population ids
                hdf5file.writeDSInt("CellPopIDs", dims, new H5Array<int>(cellPopIds));

                // gene activations
                for (int i = 0; i < cellStateGenes.Length; i++)
                {
                    dims[0] = cellStateGenes[i].Length;
                    hdf5file.writeDSDouble("GeneState" + i, dims, new H5Array<double>(cellStateGenes[i]));
                }

                // molecules
                for (int i = 0; i < cellStateMolecules.Length; i++)
                {
                    dims[0] = cellStateMolecules[i].Length;
                    hdf5file.writeDSDouble("MoleculeState" + i, dims, new H5Array<double>(cellStateMolecules[i]));
                }

                // behaviors
                dims = new long[] { cellCount, B_COUNT };
                hdf5file.writeDSInt("CellBehaviors", dims, new H5Array<int>(cellBehaviors));

                // write the cell spatial states
                dims[0] = cellCount;
                dims[1] = CellSpatialState.Dim;
                hdf5file.writeDSDouble("SpatialState", dims, new H5Array<double>(cellStateSpatial));
            }

            // ecs
            for (int i = 0; i < ecsMolpops.Length; i++)
            {
                if (i == 0)
                {
                    // index 0 is guaranteed to exist because of the loop over i
                    dims = new long[] { ecsMolpops[0].Length };
                }
                hdf5file.writeDSDouble("ECS" + i, dims, new H5Array<double>(ecsMolpops[i]));
            }

            // close the group
            hdf5file.closeGroup();
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
            hdf5file.openGroup(groupName);

            cellCount = hdf5file.readInt("CellCount");
            if (cellCount > 0)
            {
                // ids
                hdf5file.readDSInt("CellIDs", ref cellIds);
                // generations
                hdf5file.readDSInt("CellGens", ref cellGens);
                // population ids
                hdf5file.readDSInt("CellPopIDs", ref cellPopIds);

                // gene activations
                createGenesData(false);
                for (int i = 0; i < cellStateGenes.Length; i++)
                {
                    hdf5file.readDSDouble("GeneState" + i, ref cellStateGenes[i]);
                }

                // molecules
                createMoleculesData(false);
                for (int i = 0; i < cellStateMolecules.Length; i++)
                {
                    hdf5file.readDSDouble("MoleculeState" + i, ref cellStateMolecules[i]);
                }

                // behaviors
                hdf5file.readDSInt("CellBehaviors", ref cellBehaviors);

                // read the cell spatial states
                hdf5file.readDSDouble("SpatialState", ref cellStateSpatial);
            }

            // ecs
            createECSData();
            for (int i = 0; i < ecsMolpops.Length; i++)
            {
                hdf5file.readDSDouble("ECS" + i, ref ecsMolpops[i]);
            }

            // close the group
            hdf5file.closeGroup();
        }
    }
}
