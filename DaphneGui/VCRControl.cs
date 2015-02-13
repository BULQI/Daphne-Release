﻿//#define USE_DATACACHE
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;

using Kitware.VTK;

using Daphne;


namespace DaphneGui
{
    /// <summary>
    /// entity encapsulating a vcr-like controller to playback a simulation
    /// </summary>
    public class VCRControl : INotifyPropertyChanged
    {
        private List<string> frameNames;
        private List<int> frames;
        private int frame;
#if USE_DATACACHE
        private Dictionary<int, List<DBRow>> dataCache;
#endif
        private byte vcrFlags, savedFlags;
        private long lastFramePlayed;
        public bool LastFrame { get; set; }
        // reference frame time in milliseconds, 30fps
        private const double E_DT = 1000 / 30;
        private double speedFactor, speedFactorExp;
        /// <summary>
        /// property changed event to handle the updating of our vcr control
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        // thread safety
        private Object playLock = new Object(),
                       frameLock = new Object();

        public static byte VCR_OPEN   = (1 << 0),
                           VCR_ACTIVE = (1 << 1),
                           VCR_EXPORT = (1 << 2);

        /// <summary>
        /// constructor
        /// </summary>
        public VCRControl()
        {
            frame = -1;
            SetInactive();
            frameNames = new List<string>();
            LastFrame = true;
            vcrFlags = savedFlags = 0;
        }

        /// <summary>
        /// create a data reader object
        /// </summary>
        /// <returns>false for failure or empty simulation</returns>
        public bool OpenVCR()
        {
#if USE_DATACACHE
            dataCache = new Dictionary<int, List<DBRow>>();
#endif

            if (DataBasket.hdf5file != null
#if USE_DATACACHE
                && dataCache != null
#endif
)
            {
                if (DataBasket.hdf5file.openRead() == false)
                {
                    return false;
                }
                SetFlag(VCR_OPEN);
                frameNames.Clear();
                // find the frame names and with them the number of frames
                frameNames = DataBasket.hdf5file.subGroupNames(String.Format("/Experiment_VCR/VCR_Frames"));

                if (frameNames.Count == 0)
                {
                    return false;
                }

                // open the parent group for this experiment
                DataBasket.hdf5file.openGroup(String.Format("/Experiment_VCR"));

                // open the group that holds the frames for this experiment
                DataBasket.hdf5file.openGroup("VCR_Frames");

                // build the list of frames
                frames = new List<int>();

                for (int i = 0; i < frameNames.Count; i++)
                {
                    frames.Add(i);
                }

                if (LastFrame == true)
                {
                    CurrentFrame = frames.Count - 1;
                }
                else
                {
                    CurrentFrame = 0;
                }
                SetInactive();
                speedFactor = 1.0;
                speedFactorExp = 0.0;
                return true;
            }
            return false;
        }

        /// <summary>
        /// clears a flag
        /// </summary>
        /// <param name="flag">flag to clear</param>
        private void clearFlag(byte flag)
        {
            vcrFlags &= (byte)~flag;
        }

        /// <summary>
        /// checks if a flag is set
        /// </summary>
        /// <param name="flag">flag to check for</param>
        /// <returns>true if it's present</returns>
        public bool CheckFlag(byte flag)
        {
            return (vcrFlags & flag) != 0;
        }

        /// <summary>
        /// set a flag
        /// </summary>
        /// <param name="flag">flag to set</param>
        public void SetFlag(byte flag)
        {
            vcrFlags |= flag;
        }

        /// <summary>
        /// set the speed factor for playback; will set to normal speed if value is not greater than zero
        /// </summary>
        public double SpeedFactor
        {
            set
            {
                if (value <= 0.0)
                {
                    speedFactor = 1.0;
                }
                else
                {
                    speedFactor = value;
                }
            }
            get { return speedFactor; }
        }

        /// <summary>
        /// set the speed factor exponent and speed factor implicitly; normal speed for exponent = 0
        /// </summary>
        public double SpeedFactorExponent
        {
            set
            {
                speedFactorExp = value;
                speedFactor = Math.Pow(2.0, speedFactorExp);
            }
            get { return speedFactorExp; }
        }

        /// <summary>
        /// helper function to quickly set the player's state to inactive
        /// </summary>
        public void SetInactive()
        {
            clearFlag(VCR_ACTIVE);
            lock (playLock)
            lastFramePlayed = 0;
        }

        /// <summary>
        /// save the current flags
        /// </summary>
        public void SaveFlags()
        {
            savedFlags = vcrFlags;
        }

        /// <summary>
        /// restore the saved flags
        /// </summary>
        public void RestoreFlags()
        {
            vcrFlags = savedFlags;
        }

        /// <summary>
        /// close and release the data reader object
        /// </summary>
        public void ReleaseVCR()
        {
            if (DataBasket.hdf5file != null)
            {
                DataBasket.hdf5file.close(true);
            }
#if USE_DATACACHE
            if (dataCache != null)
            {
                dataCache.Clear();
                dataCache = null;
            }
#endif
            if (frames != null)
            {
                frames.Clear();
                frames = null;
            }
            SetInactive();
            clearFlag(VCR_OPEN);
        }

        /// <summary>
        /// retrieve the current frame
        /// </summary>
        public int CurrentFrame
        {
            get { return frame; }
            set
            {
                lock (frameLock)
                {
                    frame = value;
                }
                NotifyPropertyChanged("CurrentFrame");
                NotifyPropertyChanged("CurrentTime");
                // synch vtk to the current frame
                if (SimulationBase.ProtocolHandle.CheckScenarioType(Protocol.ScenarioType.TISSUE_SCENARIO) == true)
                {
                    TissueSimulationFrameData fdata = (TissueSimulationFrameData)CurrentFrameData();

                    SimulationBase.dataBasket.UpdateCells(fdata);
                    SimulationBase.dataBasket.UpdateECSMolpops(fdata);
                    MainWindow.VTKBasket.UpdateData();
                    MainWindow.GC.DrawFrame(GetPlaybackPercent());
                }
            }
        }

        /// <summary>
        /// retrieve the current time
        /// </summary>
        public double CurrentTime
        {
            get { return frame < TotalFrames() - 1 ? frames[frame] * MainWindow.SOP.Protocol.scenario.time_config.rendering_interval : MainWindow.SOP.Protocol.scenario.time_config.duration; }
            set
            {
                int tmp = (int)((double)value / MainWindow.SOP.Protocol.scenario.time_config.rendering_interval);

                // find the frame that qualifies as first one to include the time passed in
                for (int i = 0; i < frames.Count; i++)
                {
                    if (frames[i] >= tmp)
                    {
                        CurrentFrame = i;
                        break;
                    }
                }
                NotifyPropertyChanged("CurrentTime");
            }
        }

        /// <summary>
        /// returns the total number of frames
        /// </summary>
        /// <returns>total number of frames</returns>
        public int TotalFrames()
        {
            if (frames == null)
            {
                return -1;
            }
            return frames.Count;
        }

        /// <summary>
        /// read the current frame
        /// <returns>frame read</returns>
        /// </summary>
        private IFrameData CurrentFrameData()
        {
#if USE_DATACACHE
            if (dataCache == null)
            {
                return null;
            }
#endif

            lock (frameLock)
            {
                if (frame >= 0 && frame < frames.Count)
                {
#if USE_DATACACHE
                    if (dataCache.ContainsKey(frame) == false)
                    {
                        // add the data to the cache
                        dataCache.Add(frame, reader.FetchByTime(frames[frame]));
                    }
                    return dataCache[frame];
#else
                    MainWindow.Sim.FrameData.readData(frames[frame]);
                    return MainWindow.Sim.FrameData;
#endif
                }
                return null;
            }
        }

        /// <summary>
        /// advance the frame, can be by a negative number (rewind) by a number of frames; does not go past the first or last frame
        /// </summary>
        /// <param name="delta">frames to advance</param>
        /// <param name="single">true to force a single step; will cause playback to change into pause</param>
        public void Advance(int delta, bool single = true)
        {
            if (single == true)
            {
                SetInactive();
            }

            int tmp = frame + delta;

            if (0 <= tmp && tmp < TotalFrames())
            {
                CurrentFrame = tmp;
            }
            else if (tmp < 0)
            {
                CurrentFrame = 0;
            }
            else if (tmp >= TotalFrames())
            {
                CurrentFrame = TotalFrames() - 1;
            }
        }

        /// <summary>
        /// move to a specific frame
        /// </summary>
        /// <param name="toFrame">the frame to move to</param>
        /// <param name="single">true to force a single step; will cause playback into pause</param>
        public void MoveToFrame(int toFrame, bool single = true)
        {
            if (single == true)
            {
                SetInactive();
            }

            if (0 <= toFrame && toFrame < TotalFrames())
            {
                CurrentFrame = toFrame;
            }
            else if (toFrame < 0)
            {
                CurrentFrame = 0;
            }
            else if (toFrame >= TotalFrames())
            {
                CurrentFrame = TotalFrames() - 1;
            }
        }

        /// <summary>
        /// helper function to insert a delay for smooth playback
        /// </summary>
        private void Delay()
        {
            long currentTime = DateTime.UtcNow.Ticks,
                 msDelta = (currentTime - lastFramePlayed) / TimeSpan.TicksPerMillisecond,
                 target = (long)(1.0 / speedFactor * E_DT);

            if (msDelta < target)
            {
                int delay = (int)(target - msDelta);

                if (delay > 0)
                {
                    Thread.Sleep(delay);
                }
            }
            lastFramePlayed = DateTime.UtcNow.Ticks;
        }

        /// <summary>
        /// regular play forward
        /// </summary>
        public void Play()
        {
            lock (playLock)
            {
                if (CheckFlag(VCR_ACTIVE) == true)
                {
                    // insert a delay when needed to keep the fps steady
                    Delay();

                    // end of simulation? roll around to the beginning and start playing
                    if (frame >= TotalFrames() - 1)
                    {
                        MoveToFrame(0, false);
                    }
                    else
                    {
                        Advance(1, false);
                        // if playing reached the last frame then pause the player
                        if (frame == TotalFrames() - 1)
                        {
                            SetInactive();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// calculate and return the state of playback
        /// </summary>
        /// <returns>integer indicating the percent state of playback</returns>
        public int GetPlaybackPercent()
        {
            return (int)(Math.Min(100, TotalFrames() <= 1 ? 0 : 100 * frame / (TotalFrames() - 1)));
        }

        /// <summary>
        /// Supports notification of change for dependency properties
        /// </summary>
        private void NotifyPropertyChanged(String info)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(info));
            }
        }

    }

}
