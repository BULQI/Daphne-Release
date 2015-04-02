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
}
