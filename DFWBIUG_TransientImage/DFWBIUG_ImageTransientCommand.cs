using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// This line is not mandatory, but improves loading performances
[assembly: CommandClass(typeof(DFWBIUG_C3D_Data_Shortcuts_2020.TransientImage.DFWBIUG_ImageTransientCommand))]

namespace DFWBIUG_C3D_Data_Shortcuts_2020.TransientImage
{
    public class DFWBIUG_ImageTransientCommand
    {
        private DFWBIUG_ImageTransient _transient; // initialization elsewhere

        [CommandMethod("TransientImageExample")]
        public void TransientImageCommand() // This method can have any name
        {
            // Put your command code here
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return;
            }

            var ed = doc.Editor;

            try
            {
                _transient = new DFWBIUG_ImageTransient();
                TransientManager.CurrentTransientManager.AddTransient(_transient, TransientDrawingMode.Main, 0, new IntegerCollection());
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("Error: " + ex.Message);
            }
        }

        [CommandMethod("RemoveTransientImageExample")]
        public void RemoveTransientImageCommand() // This method can have any name
        {
            // Put your command code here
            var doc = Application.DocumentManager.MdiActiveDocument;
            if (doc == null)
            {
                return;
            }

            var ed = doc.Editor;

            try
            {
                var intCol = new IntegerCollection();
                TransientManager.CurrentTransientManager.EraseTransient(_transient, intCol);
                if (!_transient.IsDisposed)
                {
                    _transient.Dispose();
                }

                _transient = null;
            }
            catch (System.Exception ex)
            {
                ed.WriteMessage("Error: " + ex.Message);
            }
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// Source: https://stackoverflow.com/questions/1922040/how-to-resize-an-image-c-sharp
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}
