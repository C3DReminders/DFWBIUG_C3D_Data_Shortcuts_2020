using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.GraphicsInterface;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

// Code sample taken from here: https://forums.autodesk.com/t5/net/transient-image-always-draw-behind-db-resident-entities/td-p/7957887

namespace DFWBIUG_C3D_Data_Shortcuts_2020.TransientImage
{
    public class DFWBIUG_ImageTransient : Transient
    {
        private Point3d _insertionPoint;
        private Bitmap _bmp;
        private double _geogWidth;
        private double _geogHeight;

        private ImageBGRA32 _img;
        private IntPtr unmanagedPointer;

        public DFWBIUG_ImageTransient()
        {
            var imagePath = @"C:\Users\chris\source\repos\DFWBIUG_C3D_Data_Shortcuts_2020\DFWBIUG_TransientImage\20221009_162445.jpg";
            var img = Image.FromFile(imagePath);
            _bmp = DFWBIUG_ImageTransientCommand.ResizeImage(img, 500, 500);
            _insertionPoint = new Point3d(100, 100, 50);

            _geogWidth = img.Width;
            _geogHeight = img.Height;
        }

        /// <summary>
        /// Updates the image which should be drawn.
        /// </summary>
        /// <param name="mapTileDrawInfo"></param>
        public void Update()
        {
            // [...] // some business logic here, update ImageBGRA32 from .NET-Bitmap
        }

        protected override int SubSetAttributes(DrawableTraits traits)
        {
            return (int)DrawableAttributes.None;
        }

        protected override void SubViewportDraw(ViewportDraw vd)
        {
            // no need to implement, see
            // https://knowledge.autodesk.com/search-result/caas/CloudHelp/cloudhelp/2017/ENU/OARXMAC-DevGuide/files/GUID-DAF74804-2C45-4899-AFAD-EB762DE9FD5E-htm.html
            return;
        }

        protected override bool SubWorldDraw(WorldDraw wd)
        {
            BitmapData raw = _bmp.LockBits(new Rectangle(0, 0, (int)_bmp.Width, (int)_bmp.Height), ImageLockMode.ReadOnly, _bmp.PixelFormat);

            int size = raw.Height * raw.Stride;

            byte[] rawImageRGB24 = new Byte[size];

            System.Runtime.InteropServices.Marshal.Copy(raw.Scan0, rawImageRGB24, 0, size);

            byte[] rawImageBGRA32 = new Byte[_bmp.Width * _bmp.Height * 4];

            for (int row = 0; row < _bmp.Height; row++)
            {
                for (int col = 0; col < _bmp.Width; col++)
                {
                    var rgbIndex = col * 3 + row * raw.Stride;
                    var rgbaIndex = col * 4 + row * _bmp.Width * 4;

                    rawImageBGRA32[rgbaIndex] = rawImageRGB24[rgbIndex];            // Blue
                    rawImageBGRA32[rgbaIndex + 1] = rawImageRGB24[rgbIndex + 1];    // Green
                    rawImageBGRA32[rgbaIndex + 2] = rawImageRGB24[rgbIndex + 2];    // Red
                    rawImageBGRA32[rgbaIndex + 3] = 255;                            // Alpha
                }
            }

            if (raw != null)
            {
                _bmp.UnlockBits(raw);
            }

            unmanagedPointer = Marshal.AllocHGlobal(rawImageBGRA32.Length);

            Marshal.Copy(rawImageBGRA32, 0, unmanagedPointer, rawImageBGRA32.Length);

            _img = new ImageBGRA32((uint)_bmp.Width, (uint)_bmp.Height, unmanagedPointer);

            Marshal.FreeHGlobal(unmanagedPointer);

            if (_img == null || _insertionPoint == null)
                return false;

            // wd.Geometry.Image(_img, _insertionPoint, Vector3d.XAxis * (_bmp.Width / _bmp.Height), new Vector3d(0.5, 0.75, 1.0));
            // return true;
            return wd.Geometry.Image(_img,
                _insertionPoint,
                Vector3d.XAxis * _geogWidth,
                new Vector3d(0.5, 0.75, 1.0) * _geogHeight);
            // return wd.Geometry.Image(_img, _insertionPoint, Vector3d.XAxis * (_bmp.Width / _bmp.Height), new Vector3d(0.5, 0.75, 1.0)); 
        }

    }
}
