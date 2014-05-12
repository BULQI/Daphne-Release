﻿//#define USE_DATACACHE
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;


namespace DaphneGui
{
    /// <summary>
    /// enumeration that allows controlling the vcr state
    /// </summary>
    public enum VCRControlState
    {
        /// <summary>
        /// inactive
        /// </summary>
        VCR_INACTIVE,
        /// <summary>
        /// play
        /// </summary>
        VCR_PLAY
    };

    /// <summary>
    /// entity encapsulating a vcr-like controller to playback a simulation
    /// </summary>
    public class VCRControl : INotifyPropertyChanged
    {
        //private DataReader reader;
#if USE_DATACACHE
        private Dictionary<int, List<DBRow>> dataCache;
#endif
        private List<int> frames;
        private int frame;
        private VCRControlState playbackState, savedState;
        private long lastFramePlayed;
        // frame time in milliseconds; currently 30fps
        private const long E_DT = 1000 / 30;
        /// <summary>
        /// property changed event to handle the updating of our vcr control
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        // thread safety
        private Object playLock = new Object(),
                       frameLock = new Object();

        /// <summary>
        /// constructor
        /// </summary>
        public VCRControl()
        {
            frame = -1;
            SetInactive();
        }

        /// <summary>
        /// create a data reader object
        /// </summary>
        /// <param name="lastFrame">true for opening the control pointing to the last frame</param>
        /// <param name="expID">experiment id</param>
        /// <returns>false for failure or empty simulation</returns>
        public bool OpenVCR(bool lastFrame, int expID = -1)
        {
            //reader = new DataReader(expID < 0 ? MainWindow.SC.SimConfig.experiment_db_id : expID);

            //if (reader.TimeVals.Count == 0)
            //{
            //    return false;
            //}

#if USE_DATACACHE
            dataCache = new Dictionary<int, List<DBRow>>();
#endif

//            if (reader != null
//#if USE_DATACACHE
//                && dataCache != null
//#endif
//               )
//            {
//                // build the list of frames
//                double lastFrameChecked = reader.TimeVals[0];

//                frames = new List<int>();

//                for (int i = 0; i < reader.TimeVals.Count; i++)
//                {
//                    // we have to add the frame if it is past the render step after the last frame or it is the very first or last one in the list
//                    if (reader.TimeVals[i] - lastFrameChecked >= Math.Max(MainWindow.Sim.RenderingInterval, Simulation.RENDER_STEP) || i == 0 || i == reader.TimeVals.Count - 1)
//                    {
//                        frames.Add(i);
//                        lastFrameChecked = reader.TimeVals[i];
//                    }
//                }

//                if (lastFrame == true)
//                {
//                    CurrentFrame = frames.Count - 1;
//                }
//                else
//                {
//                    CurrentFrame = 0;
//                }
//            }
            SetInactive();
            return true;
        }

        /// <summary>
        /// set the player's state
        /// </summary>
        /// <param name="state">value indicating the state</param>
        public void SetPlaybackState(VCRControlState state)
        {
            playbackState = state;
        }

        /// <summary>
        /// helper function to quickly set the player's state to inactive
        /// </summary>
        public void SetInactive()
        {
            SetPlaybackState(VCRControlState.VCR_INACTIVE);
            lock (playLock)
            lastFramePlayed = 0;
        }

        /// <summary>
        /// save the current playback state
        /// </summary>
        public void SaveState()
        {
            savedState = playbackState;
        }

        /// <summary>
        /// retrieve the saved playback state
        /// </summary>
        public VCRControlState SavedState
        {
            get { return savedState; }
        }

        /// <summary>
        /// retrieve if the player is active (currently anything but inactive qualifies)
        /// </summary>
        /// <returns>true for active</returns>
        public bool IsActive()
        {
            return playbackState != VCRControlState.VCR_INACTIVE;
        }

        /// <summary>
        /// close and release the data reader object
        /// </summary>
        public void ReleaseVCR()
        {
//            if (reader != null)
//            {
//                reader.CloseConnection();
//                reader = null;
//            }
//#if USE_DATACACHE
//            if (dataCache != null)
//            {
//                dataCache.Clear();
//                dataCache = null;
//            }
//#endif
//            if (frames != null)
//            {
//                frames.Clear();
//                frames = null;
//            }
            SetInactive();
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
                //MainWindow.Basket.UpdateCells(CurrentFrameData(), GetPlaybackPercent());
            }
        }

        /// <summary>
        /// retrieve the current time
        /// </summary>
        public double CurrentTime
        {
            get { return 1.0; }

            //get { return frame < TotalFrames() - 1 ? frames[frame] * MainWindow.Sim.RenderingInterval : MainWindow.Sim.EndTime; }
            //set
            //{
            //    int tmp = (int)((double)value / MainWindow.Sim.RenderingInterval);

            //    // find the frame that qualifies as first one to include the time passed in
            //    for (int i = 0; i < frames.Count; i++)
            //    {
            //        if (frames[i] >= tmp)
            //        {
            //            CurrentFrame = i;
            //            break;
            //        }
            //    }
            //    NotifyPropertyChanged("CurrentTime");
            //}
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
                 msDelta = (currentTime - lastFramePlayed) / TimeSpan.TicksPerMillisecond;

            if (msDelta < E_DT)
            {
                int delay = (int)(E_DT - msDelta);

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
                if (playbackState == VCRControlState.VCR_PLAY)
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