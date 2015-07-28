using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Daphne
{
    public interface IDynamic
    {
        void Step(double dt);
    }

    public interface IVTKDataBasket
    {
        void SetupVTKData(Protocol protocol);
        void UpdateData();
        void Cleanup();
    }

    public interface IVTKGraphicsController
    {
        void Cleanup();
        void CreatePipelines();
        void DrawFrame(int progress);
        void DisableComponents(bool complete);
        void EnableComponents(bool finished);
        void ResetGraphics();
    }

    public interface IFrameData
    {
        void applyStateByIndex(int idx, ref CellState state);
        void writeData(int i);
        void writeData(string groupName);
        void readData(int i);
        void readData(string groupName);
    }

    public interface ProbDistribution3D
    {
        /// <summary>
        /// Return x,y,z coordinates for the next cell using the appropriate probability density distribution.
        /// </summary>
        /// <returns>double[3] {x,y,z}</returns>
        double[] nextPosition();
        /// <summary>
        /// Update extents of ECS and other distribution-specific tasks.
        /// </summary>
        void Resize(double[] newExtents);
    }

    public interface ReporterData
    {
        /// <summary>
        /// implements a sort by which the data arrays/lists/dictionaries get arranged (most likely sorted by time)
        /// </summary>
        /// <returns></returns>
        void Sort();
    }
}
