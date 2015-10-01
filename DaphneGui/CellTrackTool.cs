using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Daphne;

namespace DaphneGui
{
    /// <summary>
    /// handles creation and maintenance of cell tracks
    /// </summary>
    class CellTrackTool
    {
        /// <summary>
        /// constructor
        /// </summary>
        public CellTrackTool()
        {
        }

        /// <summary>
        /// indicates whether the cell track controller with the given key exists
        /// </summary>
        /// <param name="key">the key is the cell id</param>
        /// <returns>true if it exists</returns>
        public bool IsInitialized(int key)
        {
            return ((VTKFullGraphicsController)MainWindow.GC).CellTrackControllers.ContainsKey(key) == true;
        }

        /// <summary>
        /// remove degenerate / identical points
        /// </summary>
        /// <param name="data">data object to filter</param>
        public void FilterData(CellTrackData data)
        {
            List<int> remove = new List<int>();
            double[] start = null,
                     delta = null;
            double eps = 1e-6;

            // find what to remove
            for(int i = 0; i < data.Positions.Count; i++)
            {
                // find the starting point
                if (i == 0)
                {
                    start = data.Positions[i];
                }
                // compare start against the current point
                else
                {
                    if (delta == null)
                    {
                        delta = new double[start.Length];
                    }

                    // find the deltas in each component
                    for(int j = 0; j < start.Length; j++)
                    {
                        delta[j] = start[j] - data.Positions[i][j];
                    }
                    // if either delta is outside of 'small' update the starting point
                    if (delta[0] < -eps || delta[0] > eps || delta[1] < -eps || delta[1] > eps || delta[2] < -eps || delta[2] > eps)
                    {
                        start = data.Positions[i];
                    }
                    // if the points match remove the current point
                    else
                    {
                        remove.Add(i);
                    }
                }
            }
            // remove
            for (int i = remove.Count - 1; i >= 0; i--)
            {
                data.Times.RemoveAt(remove[i]);
                data.Positions.RemoveAt(remove[i]);
            }
        }

        /// <summary>
        /// based on source data, create all underlying track data
        /// </summary>
        /// <param name="data">source data coming from file, database,...</param>
        /// <param name="key">the cell id, used as track key</param>
        public void InitializeCellTrack(CellTrackData data, int key)
        {
            VTKCellTrackData trackData = new VTKCellTrackData();
            VTKCellTrackController trackController = ((VTKFullGraphicsController)MainWindow.GC).CreateVTKCellTrackController();

            trackData.GenerateActualPathPolyData(data);
            trackController.GenerateActualPathProp(trackData);
            // insert CellTrackData
            SimulationBase.dataBasket.TrackData.Add(key, data);
            // insert VTKCellTrackData
            ((VTKFullDataBasket)MainWindow.VTKBasket).CellTracks.Add(key, trackData);
            // insert VTKCellTrackController
            ((VTKFullGraphicsController)MainWindow.GC).CellTrackControllers.Add(key, trackController);
        }

        /// <summary>
        /// toggle track visibility
        /// </summary>
        /// <param name="key">the cell id serves as track key</param>
        public void ToggleCellTrack(int key)
        {
            if (((VTKFullGraphicsController)MainWindow.GC).CellTrackControllers.ContainsKey(key) == true)
            {
                GraphicsProp track = ((VTKFullGraphicsController)MainWindow.GC).CellTrackControllers[key].ActualTrack;

                // toggle
                track.addToScene(!track.InScene);
                // zoom
                if (track.InScene == true)
                {
                   // ((VTKFullGraphicsController)MainWindow.GC).zoomToPath(track.Prop);
                }
                // initiate redisplay
                ((VTKFullGraphicsController)MainWindow.GC).RWC.Invalidate();
            }
        }

        public void HideCellTracks()
        {
            foreach (var item in ((VTKFullGraphicsController)MainWindow.GC).CellTrackControllers)
            {
                GraphicsProp track = item.Value.ActualTrack;
                track.addToScene(false);
            }
            // initiate redisplay
            ((VTKFullGraphicsController)MainWindow.GC).RWC.Invalidate();
        }
    }
}
