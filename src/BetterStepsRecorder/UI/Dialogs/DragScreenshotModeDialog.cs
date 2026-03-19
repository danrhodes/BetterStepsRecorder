using System.Drawing;
using System.Windows.Forms;

namespace BetterStepsRecorder.UI.Dialogs
{
    public class DragScreenshotModeDialog : Form
    {
        private RadioButton rdoCropped;
        private RadioButton rdoActiveScreen;
        private RadioButton rdoAllScreens;
        private Button btnOK;
        private Button btnCancel;

        public DragScreenshotMode SelectedMode { get; private set; }

        public DragScreenshotModeDialog(DragScreenshotMode current)
        {
            SelectedMode = current;
            BuildUI();
            rdoCropped.Checked      = current == DragScreenshotMode.Cropped;
            rdoActiveScreen.Checked = current == DragScreenshotMode.ActiveScreen;
            rdoAllScreens.Checked   = current == DragScreenshotMode.AllScreens;
        }

        private void BuildUI()
        {
            Text = "Drag Screenshot Mode";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9f);

            int y = 16;

            var lblNote = new Label
            {
                Text = "Choose how screenshots are captured for drag actions:",
                Location = new Point(14, y),
                Size = new Size(360, 32),
                ForeColor = SystemColors.GrayText
            };
            Controls.Add(lblNote);
            y += 40;

            rdoCropped      = AddRadio("Cropped  —  tight crop around the drag path", ref y);
            rdoActiveScreen = AddRadio("Active screen  —  screen containing the drag", ref y);
            rdoAllScreens   = AddRadio("All screens  —  entire virtual desktop captured", ref y);

            y += 12;
            btnOK     = new Button { Text = "OK",     DialogResult = DialogResult.OK,     Location = new Point(192, y), Size = new Size(72, 26) };
            btnCancel = new Button { Text = "Cancel",  DialogResult = DialogResult.Cancel, Location = new Point(272, y), Size = new Size(72, 26) };
            Controls.Add(btnOK);
            Controls.Add(btnCancel);
            AcceptButton = btnOK;
            CancelButton = btnCancel;

            ClientSize = new Size(360, y + 50);
            btnOK.Click += (s, e) =>
            {
                if (rdoAllScreens.Checked)
                    SelectedMode = DragScreenshotMode.AllScreens;
                else if (rdoActiveScreen.Checked)
                    SelectedMode = DragScreenshotMode.ActiveScreen;
                else
                    SelectedMode = DragScreenshotMode.Cropped;
            };
        }

        private RadioButton AddRadio(string text, ref int y)
        {
            var rb = new RadioButton { Text = text, Location = new Point(14, y), AutoSize = true };
            Controls.Add(rb);
            y += 26;
            return rb;
        }
    }
}
