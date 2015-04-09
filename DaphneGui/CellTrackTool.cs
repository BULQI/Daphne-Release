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
        /// based on source data, create all underlying track data
        /// </summary>
        /// <param name="data">source data coming from file, database, ...</param>
        /// <param name="key">the cell id, used as track key</param>
        public void InitializeCellTrack(CellTrackData data, int key)
        {
            if (data != null)
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
                    ((VTKFullGraphicsController)MainWindow.GC).zoomToPath(track.Prop);
                }
                // initiate redisplay
                ((VTKFullGraphicsController)MainWindow.GC).RWC.Invalidate();
            }
        }
    }
}
