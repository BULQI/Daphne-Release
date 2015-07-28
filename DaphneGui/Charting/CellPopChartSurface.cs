using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Abt.Controls.SciChart.Visuals;
using iTextSharp.text;
using iTextSharp.text.pdf;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows;
using System.IO;
using System.Windows.Controls;
using System.Printing;
using System.Windows.Xps;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;
using System.Windows.Media;
using Newtonsoft.Json;
using System.Windows.Media.Imaging;

//------------------------------------------------------------
using Abt.Controls.SciChart.Visuals.RenderableSeries;
using Daphne;
using System.Windows.Forms.DataVisualization.Charting;

namespace DaphneGui
{

    /// <summary>
    /// This class derives from SciChartSurface and serves as a base class for Lineage and Cell Pop Dynamics chart surface classes.
    /// Common methods are located here.  This is not an abstract class.
    /// </summary>
    public class CellPopChartSurface : SciChartSurface
    {
        /// <summary>
        /// This method saves the chart to file - types png, bmp, jpg, pdf, tif
        /// </summary>
        /// <param name="filename"></param>
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
            else if (filename.EndsWith("xps"))
            {
                ExportToXps(filename);
            }
        }

        /// <summary>
        /// This method outputs a PDF file without first outputting a .bmp file.
        /// </summary>
        /// <param name="filename"></param>
        public void OutputToPDF(string filename)
        {
            //Export this graph to BitmapSource
            var source = this.ExportToBitmapSource();

            //Then retrieve from BitmapSource into a Bitmap object
            Bitmap bmp1 = new Bitmap(source.PixelWidth, source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            BitmapData data = bmp1.LockBits(new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp1.Size),
            ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bmp1.UnlockBits(data);

            //-------------------------------------------------
            System.Drawing.Image image = bmp1;
            iTextSharp.text.Image pdfImage = iTextSharp.text.Image.GetInstance(image, System.Drawing.Imaging.ImageFormat.Bmp);

            Document doc = new Document(PageSize.A4);
            PdfWriter.GetInstance(doc, new FileStream(filename, FileMode.Create));
            doc.Open();
            
            doc.Add(pdfImage);
            //A good thing is always to add meta information to files, this does it easier to index the file in a proper way. 
            //You can easilly add meta information by using these methods. (NOTE: This is optional, you don't have to do it, just keep in mind that it's good to do it!)
            // Add meta information to the document
            doc.AddAuthor("Sanjeev Gupta");
            doc.AddCreator("Daphne PDF output");
            doc.AddKeywords("PDF export daphne");
            doc.AddSubject("Document subject - Save the SciChart graph to a PDF document");
            doc.AddTitle("The document title - Daphne graph in PDF format");
            doc.Close();
            //------------------------------------------------------------------------------------------

            //Another possible way
            Document doc2 = new Document(PageSize.A4);


            doc2.Close();
        }

        /// <summary>
        /// This method outputs a tiff file without outputting a bmp file.
        /// </summary>
        /// <param name="outFile"></param>
        public void ExportToTiff(string outFile)
        {
            var source = this.ExportToBitmapSource();
            Bitmap bmp3 = new Bitmap(source.PixelWidth, source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            BitmapData data = bmp3.LockBits(new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp3.Size),
                ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bmp3.UnlockBits(data);
            bmp3.Save(outFile, ImageFormat.Tiff);
        }

        public void ExportToXps(string filename)
        {
            var dialog = new PrintDialog();

            if (dialog.ShowDialog() == true)
            {
                var size = new System.Windows.Size(dialog.PrintableAreaWidth, dialog.PrintableAreaWidth * 3 / 4);

                //var scs = CreateSciChartSurfaceWithoutShowingIt(size);
                //And print. This works particularly well to XPS! 
                Action printAction = () => dialog.PrintVisual(this, "XPS file");
                Dispatcher.BeginInvoke(printAction);
            }

            //OutputXpsDoc(filename);
        }

        //Trying to output "real" PDF file instead of copying bitmap and outputting it
        public void OutputToPDF2(string filename)
        {            
            //Export this graph to BitmapSource
            this.Width = 1000;
            this.Height = 1000;
            var source = this.ExportToBitmapSource();

            //Then retrieve from BitmapSource into a Bitmap object
            Bitmap bmp1 = new Bitmap(source.PixelWidth, source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            BitmapData data = bmp1.LockBits(new System.Drawing.Rectangle(System.Drawing.Point.Empty, bmp1.Size),
            ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
            source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);
            bmp1.UnlockBits(data);

            //-------------------------------------------------
            System.Drawing.Image image = bmp1;
            iTextSharp.text.Image pdfImage = iTextSharp.text.Image.GetInstance(image, System.Drawing.Imaging.ImageFormat.Bmp);
            //iTextSharp.text.Image pdfImage = iTextSharp.text.Image.GetInstance(

            Document doc = new Document(PageSize.LETTER, 10, 10, 20, 20);
            PdfWriter.GetInstance(doc, new FileStream(filename, FileMode.Create));
            doc.Open();

            doc.Add(pdfImage);
            //A good thing is always to add meta information to files, this does it easier to index the file in a proper way. 
            //You can easilly add meta information by using these methods. (NOTE: This is optional, you don't have to do it, just keep in mind that it's good to do it!)
            // Add meta information to the document
            doc.AddAuthor("Sanjeev Gupta");
            doc.AddCreator("Daphne PDF output");
            doc.AddKeywords("PDF export daphne");
            doc.AddSubject("Document subject - Save the SciChart graph to a PDF document");
            doc.AddTitle("The document title - Daphne graph in PDF format");
            doc.Close();
            //------------------------------------------------------------------------------------------

            //FixedDocument fixedDoc = new FixedDocument();
            //PageContent pageContent = new PageContent();
            //FixedPage fixedPage = new FixedPage();

            ////Create first page of document
            //var dialog = new PrintDialog();
            //var size = new System.Windows.Size(dialog.PrintableAreaWidth, dialog.PrintableAreaWidth * 3 / 4);

            //var scs = CreateSciChartSurfaceWithoutShowingIt(size);

            //fixedPage.Children.Add((SciChartSurface)scs);
            //((System.Windows.Markup.IAddChild)pageContent).AddChild(fixedPage);
            //fixedDoc.Pages.Add(pageContent);

            //XpsDocument xpsd = new XpsDocument(filename, FileAccess.ReadWrite);

            //System.Windows.Xps.XpsDocumentWriter xw = XpsDocument.CreateXpsDocumentWriter(xpsd);
            //xw.Write(fixedDoc);
            //xpsd.Close();

            //------------------------------------------------------------------------------------------
            //Another possible way

            //Document doc2 = new Document(PageSize.A4);
            //doc2.Close();
        }

        //Not working!
        public void ExportToXps2(string filename)
        {

            ////FixedDocument doc = (FixedDocument)documentViewer1.Document;
            ////XpsDocument xpsd = new XpsDocument(filename, FileAccess.ReadWrite);
            ////System.Windows.Xps.XpsDocumentWriter xw = XpsDocument.CreateXpsDocumentWriter(xpsd);
            ////xw.Write(doc);
            ////xpsd.Close();


            FixedDocument fixedDoc = new FixedDocument();
            PageContent pageContent = new PageContent();
            FixedPage fixedPage = new FixedPage();

            //Create first page of document
            var dialog = new PrintDialog();
            var size = new System.Windows.Size(dialog.PrintableAreaWidth, dialog.PrintableAreaWidth * 3 / 4);

            var scs = CreateSciChartSurfaceWithoutShowingIt(size);

            fixedPage.Children.Add((SciChartSurface)scs);
            ((System.Windows.Markup.IAddChild)pageContent).AddChild(fixedPage);
            fixedDoc.Pages.Add(pageContent);

            XpsDocument xpsd = new XpsDocument(filename, FileAccess.ReadWrite);

            System.Windows.Xps.XpsDocumentWriter xw = XpsDocument.CreateXpsDocumentWriter(xpsd);
            xw.Write(fixedDoc);
            xpsd.Close();
        }

        private Visual CreateSciChartSurfaceWithoutShowingIt(System.Windows.Size size)
        {
            SciChartSurface surf = new SciChartSurface();
            // We must set a width and height. If you are rendering off screen without showing
            // we have to tell the control what size to render
            surf.Width = size.Width;
            surf.Height = size.Height;

            // Doing an export sets up the chart for rendering off screen, including handling all the horrible layout issues
            // that occur when a WPF element is created and rendered without showing it.
            //
            // You must call this before printing, even if you don't intend to use the bitmap.
            //BitmapSource source = this.ExportToBitmapSource();

            surf.ExportToBitmapSource();

            return surf;
        }

        private Visual CreateSciChartSurfaceWithoutShowingIt2(System.Windows.Size size)
        {
            // Create a fresh ChartView, this contains the ViewModel (declared in XAML) and data
            var control = new SciChartSurface();
            var scs = control;

            // We must set a width and height. If you are rendering off screen without showing 
            // we have to tell the control what size to render
            scs.Width = size.Width;
            scs.Height = size.Height;

            // Doing an export sets up the chart for rendering off screen, including handling all the horrible layout issues
            // that occur when a WPF element is created and rendered without showing it. 
            // 
            // You must call this before printing, even if you don't intend to use the bitmap. 
            scs.ExportToBitmapSource();

            return scs;
        }

    }
}
