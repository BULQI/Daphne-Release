/*
Copyright (C) 2019 Kepler Laboratory of Quantitative Immunology

Permission is hereby granted, free of charge, to any person obtaining a copy of this software and associated documentation 
files (the "Software"), to deal in the Software without restriction, including without limitation the rights to use, copy, 
modify, merge, publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom the Software 
is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES 
OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS BE LIABLE FOR ANY 
CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH 
THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
﻿using System;
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
