using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Better_Steps_Recorder
{
    internal class PictureBoxTools
    {
        private bool isDrawing;
        private Rectangle blurRectangle;
        private Point startPoint;

        private void pictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            isDrawing = true;
            startPoint = e.Location;
        }

        private void pictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                Point endPoint = e.Location;
                blurRectangle = new Rectangle(
                    Math.Min(startPoint.X, endPoint.X),
                    Math.Min(startPoint.Y, endPoint.Y),
                    Math.Abs(startPoint.X - endPoint.X),
                    Math.Abs(startPoint.Y - endPoint.Y));

                pictureBox.Invalidate(); // Refresh the PictureBox to draw the selection rectangle
            }
        }

        private void pictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            if (isDrawing)
            {
                isDrawing = false;
                ApplyBlur(blurRectangle);
            }
        }

        private void ApplyBlur(Rectangle rect)
        {
            if (pictureBox.Image == null)
                return;

            using (Bitmap originalBitmap = new Bitmap(pictureBox.Image))
            {
                Bitmap blurredBitmap = new Bitmap(originalBitmap);

                int bmpWidth  = originalBitmap.Width;
                int bmpHeight = originalBitmap.Height;

                // Lock both bitmaps for bulk pixel access — far faster than GetPixel/SetPixel
                BitmapData srcData = originalBitmap.LockBits(
                    new Rectangle(0, 0, bmpWidth, bmpHeight),
                    ImageLockMode.ReadOnly,
                    PixelFormat.Format32bppArgb);

                BitmapData dstData = blurredBitmap.LockBits(
                    new Rectangle(0, 0, bmpWidth, bmpHeight),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);

                int stride      = srcData.Stride;
                int byteCount   = stride * bmpHeight;
                byte[] srcPixels = new byte[byteCount];
                byte[] dstPixels = new byte[byteCount];

                Marshal.Copy(srcData.Scan0, srcPixels, 0, byteCount);
                Marshal.Copy(dstData.Scan0, dstPixels, 0, byteCount);

                originalBitmap.UnlockBits(srcData);
                blurredBitmap.UnlockBits(dstData);

                // Clamp the blur rect to bitmap bounds
                int blurSize = 10;
                int xMin = Math.Max(rect.X, 0);
                int yMin = Math.Max(rect.Y, 0);
                int xMax = Math.Min(rect.Right, bmpWidth);
                int yMax = Math.Min(rect.Bottom, bmpHeight);

                // Simple box blur using raw byte arrays (BGRA order for Format32bppArgb)
                for (int x = xMin; x < xMax; x += blurSize)
                {
                    for (int y = yMin; y < yMax; y += blurSize)
                    {
                        int sumB = 0, sumG = 0, sumR = 0, count = 0;

                        int blockXMax = Math.Min(x + blurSize, xMax);
                        int blockYMax = Math.Min(y + blurSize, yMax);

                        for (int xx = x; xx < blockXMax; xx++)
                        {
                            for (int yy = y; yy < blockYMax; yy++)
                            {
                                int idx = yy * stride + xx * 4;
                                sumB += srcPixels[idx];
                                sumG += srcPixels[idx + 1];
                                sumR += srcPixels[idx + 2];
                                count++;
                            }
                        }

                        if (count == 0) continue;

                        byte avgB = (byte)(sumB / count);
                        byte avgG = (byte)(sumG / count);
                        byte avgR = (byte)(sumR / count);

                        for (int xx = x; xx < blockXMax; xx++)
                        {
                            for (int yy = y; yy < blockYMax; yy++)
                            {
                                int idx = yy * stride + xx * 4;
                                dstPixels[idx]     = avgB;
                                dstPixels[idx + 1] = avgG;
                                dstPixels[idx + 2] = avgR;
                                // preserve alpha (idx + 3) unchanged from src
                                dstPixels[idx + 3] = srcPixels[idx + 3];
                            }
                        }
                    }
                }

                // Write the blurred pixels back into the destination bitmap
                BitmapData writeData = blurredBitmap.LockBits(
                    new Rectangle(0, 0, bmpWidth, bmpHeight),
                    ImageLockMode.WriteOnly,
                    PixelFormat.Format32bppArgb);
                Marshal.Copy(dstPixels, 0, writeData.Scan0, byteCount);
                blurredBitmap.UnlockBits(writeData);

                // Dispose the old image before replacing it, then assign the blurred result
                pictureBox.Image?.Dispose();
                pictureBox.Image = blurredBitmap;
            }
        }

    }
}
