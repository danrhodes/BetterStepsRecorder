using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using Debug = System.Diagnostics.Debug;
using static BetterStepsRecorder.WindowHelper;

namespace BetterStepsRecorder
{
    internal static partial class Program
    {
        /// <summary>
        /// Captures a screenshot of a specific region of the screen and returns it as a Base64 string
        /// </summary>
        /// <param name="x">X coordinate of the top-left corner</param>
        /// <param name="y">Y coordinate of the top-left corner</param>
        /// <param name="width">Width of the region to capture</param>
        /// <param name="height">Height of the region to capture</param>
        /// <param name="eventId">ID of the associated record event</param>
        /// <returns>Base64 string representation of the screenshot, or null if capture failed</returns>
        public static string? SaveScreenRegionScreenshot(int x, int y, int width, int height, Guid eventId, POINT cursorPos)
        {
            try
            {
                // Create a bitmap of the specified size
                using (Bitmap bmp = new Bitmap(width, height, PixelFormat.Format32bppArgb))
                {
                    // Create graphics object from the bitmap
                    using (Graphics gfx = Graphics.FromImage(bmp))
                    {
                        // Copy the specified screen area to the bitmap
                        gfx.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);

                        // Draw an arrow pointing at the click position (use snapshotted coords, not live cursor)
                        DrawArrowAtCursor(gfx, width, height, x, y, cursorPos);
                    }

                    // Convert the bitmap to a memory stream
                    using (MemoryStream ms = new MemoryStream())
                    {
                        bmp.Save(ms, ImageFormat.Png);

                        // Convert byte array to Base64 string
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to capture screenshot: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Draws an arrow pointing to the current cursor position on the given graphics object
        /// </summary>
        /// <param name="gfx">Graphics object to draw on</param>
        /// <param name="width">Width of the bitmap</param>
        /// <param name="height">Height of the bitmap</param>
        /// <param name="offsetX">X offset of the bitmap</param>
        /// <param name="offsetY">Y offset of the bitmap</param>
        public static Color ArrowColor { get; set; } = Color.Magenta;

        private static void DrawArrowAtCursor(Graphics gfx, int width, int height, int offsetX, int offsetY, POINT cursorPos)
        {
            // Define the length of the arrow
            int arrowLength = 200;

            // Convert the screen coordinates to bitmap coordinates
            int cursorX = cursorPos.X - offsetX;
            int cursorY = cursorPos.Y - offsetY;

            // Determine arrow direction: down if in top half, up if in bottom half
            int endX, endY;
            if (cursorY < height / 2)
            {
                // Cursor is in the top half, arrow points down
                endX = cursorX;
                endY = cursorY + arrowLength;
            }
            else
            {
                // Cursor is in the bottom half, arrow points up
                endX = cursorX;
                endY = cursorY - arrowLength;
            }

            // Draw the arrow — both Pen and AdjustableArrowCap are IDisposable GDI objects
            using (var arrowCap = new System.Drawing.Drawing2D.AdjustableArrowCap(5, 5))
            using (var arrowPen = new Pen(ArrowColor, 5))
            {
                arrowPen.EndCap = System.Drawing.Drawing2D.LineCap.Custom;
                arrowPen.CustomEndCap = arrowCap;
                gfx.DrawLine(arrowPen, endX, endY, cursorX, cursorY);
            }
        }

        /// <summary>
        /// Converts a Base64 string to an Image
        /// </summary>
        /// <param name="base64String">Base64 string representation of the image</param>
        /// <returns>Image object</returns>
        public static Image Base64ToImage(string base64String)
        {
            byte[] imageBytes = Convert.FromBase64String(base64String);
            using (var ms = new MemoryStream(imageBytes))
            {
                // Return a Bitmap (stream-independent copy) so the stream can be safely disposed
                return new Bitmap(ms);
            }
        }

        /// <summary>
        /// Converts an Image to a Base64 string
        /// </summary>
        /// <param name="image">Image to convert</param>
        /// <param name="format">Image format to use</param>
        /// <returns>Base64 string representation of the image</returns>
        public static string ImageToBase64(Image image, ImageFormat format)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                // Convert Image to byte[]
                image.Save(ms, format);
                byte[] imageBytes = ms.ToArray();

                // Convert byte[] to Base64 String
                return Convert.ToBase64String(imageBytes);
            }
        }
    }
}