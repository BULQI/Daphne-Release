using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Abt.Controls.SciChart.Visuals;

namespace DaphneGui.CellLineage
{
    public class CellLineageChartSurface : CellPopChartSurface
    {
        public void SaveToFile(string filename)
        {
            if (filename.EndsWith("png"))
            {
                ExportToFile(filename, ExportType.Png);
            }
            else if (filename.EndsWith("bmp"))
            {
                ExportToFile(filename, ExportType.Bmp);
            }
            else if (filename.EndsWith("jpg"))
            {
                ExportToFile(filename, ExportType.Jpeg);
            }
            else if (filename.EndsWith("pdf"))
            {
                OutputToPDF(filename);
            }
            else if (filename.EndsWith("tif"))
            {
                ExportToTiff(filename);
            }
        }
    }
}
