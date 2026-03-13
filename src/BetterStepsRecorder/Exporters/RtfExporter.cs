using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;

namespace BetterStepsRecorder.Exporters
{
    /// <summary>
    /// Exporter for Rich Text Format (RTF) files
    /// </summary>
    public class RtfExporter : ExporterBase
    {
        /// <summary>
        /// Exports the current steps recording to RTF format
        /// </summary>
        /// <param name="filePath">The full path where the RTF file should be saved</param>
        /// <returns>True if export was successful, false otherwise</returns>
        public override bool Export(string filePath)
        {
            try
            {
                EnsureDirectoryExists(filePath);
                
                // Get the filename without extension to use as title
                string title = Path.GetFileNameWithoutExtension(filePath);
                
                using (RichTextBox rtfBox = new RichTextBox())
                using (var fontBody     = new Font("Segoe UI", 10))
                using (var fontTitle    = new Font("Segoe UI", 16, FontStyle.Bold))
                using (var fontStep     = new Font("Segoe UI", 12, FontStyle.Bold))
                using (var fontSep      = new Font("Segoe UI", 9))
                using (var fontFooter   = new Font("Segoe UI", 8))
                using (var fontLink     = new Font("Segoe UI", 8, FontStyle.Underline))
                {
                    // Set document properties
                    rtfBox.Font = fontBody;

                    // Add title using the filename
                    rtfBox.SelectionFont = fontTitle;
                    rtfBox.AppendText($"{title}\n\n");

                    // Add each step
                    foreach (var recordEvent in Program._recordEvents)
                    {
                        // Add step header
                        rtfBox.SelectionFont = fontStep;
                        rtfBox.AppendText($"Step {recordEvent.Step}: {recordEvent._StepText}\n");

                        /* Add element details if available
                        if (!string.IsNullOrEmpty(recordEvent.ElementName))
                        {
                            rtfBox.SelectionFont = new Font("Segoe UI", 9);
                            rtfBox.AppendText($"Element: {recordEvent.ElementName}\n");

                            // Get automation ID if available
                            string automationId = RecordEvent.GetAutomationId(recordEvent.UIElement);
                            if (!string.IsNullOrEmpty(automationId))
                            {
                                rtfBox.AppendText($"Automation ID: {automationId}\n");
                            }
                        }
                        */

                        // Add screenshot if available
                        if (recordEvent.HasScreenshot)
                        {
                            rtfBox.AppendText("\n");

                            using (Image img = GetRtfImage(recordEvent))
                            {
                                if (img != null)
                                {
                                    Clipboard.SetImage(img);
                                    rtfBox.Paste();
                                    rtfBox.AppendText("\n");
                                }
                            }
                        }

                        // Add separator between steps
                        rtfBox.SelectionFont = fontSep;
                        rtfBox.AppendText("\n----------------------------\n\n");
                    }

                    // Add footer with link to GitHub
                    rtfBox.SelectionAlignment = HorizontalAlignment.Center;
                    rtfBox.AppendText("\n");
                    rtfBox.SelectionFont = fontFooter;
                    rtfBox.AppendText("Generated with ");

                    // Add the hyperlink text
                    rtfBox.SelectionColor = Color.Blue;
                    rtfBox.SelectionFont = fontLink;
                    rtfBox.AppendText("Better Steps Recorder");

                    // Add the URL in parentheses
                    rtfBox.SelectionFont = fontFooter;
                    rtfBox.SelectionColor = rtfBox.ForeColor;
                    rtfBox.AppendText(" (https://github.com/Mentaleak/BetterStepsRecorder)");

                    // Save the RTF file
                    rtfBox.SaveFile(filePath);
                }
                
                ShowExportSuccess(filePath);
                return true;
            }
            catch (Exception ex)
            {
                ShowExportError("Error exporting to RTF", ex);
                return false;
            }
        }
        
        /// <summary>
        /// Loads and scales a screenshot from a RecordEvent for embedding in RTF.
        /// </summary>
        private Image GetRtfImage(RecordEvent recordEvent)
        {
            try
            {
                byte[]? imageBytes = Program.GetScreenshotBytes(recordEvent);
                if (imageBytes == null) return null;
                using (var ms = new MemoryStream(imageBytes))
                using (var original = new Bitmap(ms))
                {
                    const int maxWidth = 800;
                    int targetWidth = Math.Min(original.Width, maxWidth);
                    int targetHeight = (int)((double)original.Height / original.Width * targetWidth);
                    return new Bitmap(original, targetWidth, targetHeight);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"GetRtfImage failed: {ex.Message}");
                return null;
            }
        }
    }
}