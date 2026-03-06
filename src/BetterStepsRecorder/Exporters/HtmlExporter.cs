using System;
using System.IO;
using System.Text;

namespace BetterStepsRecorder.Exporters
{
    /// <summary>
    /// Exporter for HTML files
    /// </summary>
    public class HtmlExporter : ExporterBase
    {
        private static string HtmlEncode(string value) =>
            value.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;").Replace("\"", "&quot;");

        /// <summary>
        /// Exports the current steps recording to HTML format
        /// </summary>
        /// <param name="filePath">The full path where the HTML file should be saved</param>
        /// <returns>True if export was successful, false otherwise</returns>
        public override bool Export(string filePath)
        {
            try
            {
                EnsureDirectoryExists(filePath);
                
                // Create images folder
                string folderPath = Path.GetDirectoryName(filePath);
                string imagesFolder = Path.Combine(folderPath, "images");
                if (!Directory.Exists(imagesFolder))
                {
                    Directory.CreateDirectory(imagesFolder);
                }
                
                // Get the filename without extension to use as title
                string title = Path.GetFileNameWithoutExtension(filePath);
                
                int totalSteps = Program._recordEvents.Count;
                string generated = DateTime.Now.ToString("dd MMM yyyy, HH:mm");

                // Start building the HTML content
                StringBuilder html = new StringBuilder();
                html.AppendLine("<!DOCTYPE html>");
                html.AppendLine("<html lang=\"en\">");
                html.AppendLine("<head>");
                html.AppendLine("    <meta charset=\"UTF-8\">");
                html.AppendLine("    <meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
                html.AppendLine($"    <title>{title}</title>");
                html.AppendLine("    <style>");
                html.AppendLine("        *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }");
                html.AppendLine("        body { font-family: 'Segoe UI', system-ui, Arial, sans-serif; background: #f0f2f5; color: #1a1a2e; min-height: 100vh; }");
                html.AppendLine("        .page-header { background: linear-gradient(135deg, #1a1a2e 0%, #16213e 60%, #0f3460 100%); color: #fff; padding: 48px 40px 40px; }");
                html.AppendLine("        .page-header h1 { font-size: 2rem; font-weight: 700; letter-spacing: -0.5px; margin-bottom: 8px; }");
                html.AppendLine("        .page-header .meta { font-size: 0.85rem; opacity: 0.65; }");
                html.AppendLine("        .progress-bar-wrap { background: rgba(255,255,255,0.15); border-radius: 4px; height: 4px; margin-top: 24px; }");
                html.AppendLine("        .progress-bar-fill { background: #e94560; border-radius: 4px; height: 4px; width: 100%; }");
                html.AppendLine("        .container { max-width: 960px; margin: 0 auto; padding: 40px 20px 60px; }");
                html.AppendLine("        .step-card { background: #fff; border-radius: 12px; box-shadow: 0 2px 8px rgba(0,0,0,0.07), 0 1px 2px rgba(0,0,0,0.04); margin-bottom: 28px; overflow: hidden; transition: box-shadow 0.2s; }");
                html.AppendLine("        .step-card:hover { box-shadow: 0 8px 24px rgba(0,0,0,0.11), 0 2px 6px rgba(0,0,0,0.06); }");
                html.AppendLine("        .step-header { display: flex; align-items: center; gap: 16px; padding: 20px 24px; border-bottom: 1px solid #f0f0f0; }");
                html.AppendLine("        .step-badge { background: #e94560; color: #fff; font-size: 0.72rem; font-weight: 700; letter-spacing: 0.5px; text-transform: uppercase; border-radius: 20px; padding: 4px 12px; white-space: nowrap; flex-shrink: 0; }");
                html.AppendLine("        .step-title { font-size: 0.97rem; font-weight: 500; color: #1a1a2e; line-height: 1.45; }");
                html.AppendLine("        .step-body { padding: 20px 24px; }");
                html.AppendLine("        .step-body img { width: 100%; height: auto; border-radius: 8px; border: 1px solid #e8e8e8; display: block; cursor: zoom-in; }");
                html.AppendLine("        .no-screenshot { color: #aaa; font-size: 0.85rem; font-style: italic; padding: 8px 0; }");
                html.AppendLine("        .footer { text-align: center; color: #aaa; font-size: 0.78rem; padding-bottom: 20px; }");
                html.AppendLine("        .footer a { color: #0f3460; text-decoration: none; }");
                html.AppendLine("        .footer a:hover { text-decoration: underline; }");
                // Lightbox overlay
                html.AppendLine("        #lb-overlay { display:none; position:fixed; inset:0; background:rgba(0,0,0,0.85); z-index:9999; align-items:center; justify-content:center; cursor:zoom-out; }");
                html.AppendLine("        #lb-overlay.active { display:flex; }");
                html.AppendLine("        #lb-img { max-width:92vw; max-height:92vh; border-radius:6px; box-shadow:0 8px 40px rgba(0,0,0,0.6); }");
                html.AppendLine("    </style>");
                html.AppendLine("</head>");
                html.AppendLine("<body>");

                // Page header
                html.AppendLine("    <div class=\"page-header\">");
                html.AppendLine($"        <h1>{HtmlEncode(title)}</h1>");
                html.AppendLine($"        <div class=\"meta\">{totalSteps} step{(totalSteps == 1 ? "" : "s")} &nbsp;·&nbsp; Generated {generated}</div>");
                html.AppendLine("        <div class=\"progress-bar-wrap\"><div class=\"progress-bar-fill\"></div></div>");
                html.AppendLine("    </div>");

                html.AppendLine("    <div class=\"container\">");

                // Add each step
                foreach (var recordEvent in Program._recordEvents)
                {
                    string stepText = HtmlEncode(recordEvent._StepText ?? string.Empty);
                    html.AppendLine("        <div class=\"step-card\">");
                    html.AppendLine("            <div class=\"step-header\">");
                    html.AppendLine($"                <span class=\"step-badge\">Step {recordEvent.Step}</span>");
                    html.AppendLine($"                <span class=\"step-title\">{stepText}</span>");
                    html.AppendLine("            </div>");
                    html.AppendLine("            <div class=\"step-body\">");

                    if (!string.IsNullOrEmpty(recordEvent.Screenshotb64))
                    {
                        string imageFileName = $"step_{recordEvent.Step}_{recordEvent.ShortId}.png";
                        string imageFilePath = Path.Combine(imagesFolder, imageFileName);

                        if (SaveImageFromBase64(recordEvent.Screenshotb64, imageFilePath))
                        {
                            html.AppendLine($"                <img src=\"images/{imageFileName}\" alt=\"Screenshot for Step {recordEvent.Step}\" onclick=\"openLb(this)\">");
                        }
                    }
                    else
                    {
                        html.AppendLine("                <span class=\"no-screenshot\">No screenshot captured for this step.</span>");
                    }

                    html.AppendLine("            </div>");
                    html.AppendLine("        </div>");
                }

                html.AppendLine("    </div>"); // .container

                // Lightbox markup
                html.AppendLine("    <div id=\"lb-overlay\" onclick=\"closeLb()\"><img id=\"lb-img\" src=\"\" alt=\"\"></div>");

                // Footer
                html.AppendLine("    <div class=\"footer\">");
                html.AppendLine("        Generated with <a href=\"https://github.com/Mentaleak/BetterStepsRecorder\" target=\"_blank\">Better Steps Recorder</a>");
                html.AppendLine("    </div>");

                // Lightweight lightbox script — no dependencies
                html.AppendLine("    <script>");
                html.AppendLine("        function openLb(img) { document.getElementById('lb-img').src = img.src; document.getElementById('lb-overlay').classList.add('active'); }");
                html.AppendLine("        function closeLb() { document.getElementById('lb-overlay').classList.remove('active'); }");
                html.AppendLine("        document.addEventListener('keydown', function(e) { if (e.key === 'Escape') closeLb(); });");
                html.AppendLine("    </script>");

                // Close the HTML document
                html.AppendLine("</body>");
                html.AppendLine("</html>");
                
                // Write the HTML file directly from the StringBuilder to avoid a full string copy
                using (var writer = new StreamWriter(filePath, append: false, encoding: Encoding.UTF8))
                {
                    writer.Write(html);
                }
                
                ShowExportSuccess(filePath);
                return true;
            }
            catch (Exception ex)
            {
                ShowExportError("Error exporting to HTML", ex);
                return false;
            }
        }
    }
}