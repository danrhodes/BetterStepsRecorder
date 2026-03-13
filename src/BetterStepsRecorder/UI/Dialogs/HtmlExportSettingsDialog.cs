using System;
using System.Drawing;
using System.Windows.Forms;

namespace BetterStepsRecorder.UI.Dialogs
{
    /// <summary>
    /// Dialog for configuring which metadata sections are included in the HTML export.
    /// </summary>
    public class HtmlExportSettingsDialog : Form
    {
        private HtmlExportSettings _settings;

        // Controls
        private CheckBox chkSummary;
        private CheckBox chkGeneratedDate;
        private CheckBox chkStepTimestamps;
        private CheckBox chkAction;
        private CheckBox chkApplication;
        private CheckBox chkWindow;
        private CheckBox chkElement;
        private CheckBox chkElementType;
        private CheckBox chkMousePosition;
        private Button btnOK;
        private Button btnCancel;

        public HtmlExportSettingsDialog(HtmlExportSettings settings)
        {
            _settings = settings;
            BuildUI();
            LoadFromSettings();
        }

        private void BuildUI()
        {
            Text = "HTML Export Options";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(380, 390);
            Font = new Font("Segoe UI", 9f);

            int y = 12;
            int labelX = 12;

            Label lblNote = new Label
            {
                Text = "Choose which metadata to include in the HTML export.\nThe step description and screenshot are always included.",
                Location = new Point(labelX, y),
                Size = new Size(356, 46),
                ForeColor = SystemColors.GrayText
            };
            Controls.Add(lblNote);
            y += 54;

            AddSectionLabel("Header", ref y);
            chkSummary       = AddCheckbox("Summary bar (Steps / Start / End / Duration)", ref y);
            chkGeneratedDate = AddCheckbox("Generated date", ref y);
            y += 6;

            AddSectionLabel("Per-Step", ref y);
            chkStepTimestamps = AddCheckbox("Step timestamps", ref y);
            y += 6;

            AddSectionLabel("Detail Strip", ref y);
            chkAction      = AddCheckbox("Action", ref y);
            chkApplication = AddCheckbox("Application", ref y);
            chkWindow      = AddCheckbox("Window", ref y);
            chkElement     = AddCheckbox("Element", ref y);
            chkElementType = AddCheckbox("Element Type", ref y);
            chkMousePosition = AddCheckbox("Mouse Position", ref y);
            y += 6;

            y += 16;
            btnOK = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(212, y), Size = new Size(72, 26) };
            btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(292, y), Size = new Size(72, 26) };
            Controls.Add(btnOK);
            Controls.Add(btnCancel);
            AcceptButton = btnOK;
            CancelButton = btnCancel;

            ClientSize = new Size(380, y + 50);

            btnOK.Click += (s, e) => SaveToSettings();
        }

        private void AddSectionLabel(string text, ref int y)
        {
            Label lbl = new Label
            {
                Text = text,
                Location = new Point(12, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 8.5f, System.Drawing.FontStyle.Bold)
            };
            Controls.Add(lbl);
            y += 20;
        }

        private CheckBox AddCheckbox(string text, ref int y)
        {
            var cb = new CheckBox
            {
                Text = text,
                Location = new Point(20, y),
                AutoSize = true
            };
            Controls.Add(cb);
            y += 24;
            return cb;
        }

        private void LoadFromSettings()
        {
            chkSummary.Checked        = _settings.ShowSummary;
            chkGeneratedDate.Checked  = _settings.ShowGeneratedDate;
            chkStepTimestamps.Checked = _settings.ShowStepTimestamps;
            chkAction.Checked         = _settings.ShowAction;
            chkApplication.Checked    = _settings.ShowApplication;
            chkWindow.Checked         = _settings.ShowWindow;
            chkElement.Checked        = _settings.ShowElement;
            chkElementType.Checked    = _settings.ShowElementType;
            chkMousePosition.Checked  = _settings.ShowMousePosition;
        }

        private void SaveToSettings()
        {
            _settings.ShowSummary        = chkSummary.Checked;
            _settings.ShowGeneratedDate  = chkGeneratedDate.Checked;
            _settings.ShowStepTimestamps = chkStepTimestamps.Checked;
            _settings.ShowAction         = chkAction.Checked;
            _settings.ShowApplication    = chkApplication.Checked;
            _settings.ShowWindow         = chkWindow.Checked;
            _settings.ShowElement        = chkElement.Checked;
            _settings.ShowElementType    = chkElementType.Checked;
            _settings.ShowMousePosition  = chkMousePosition.Checked;
            _settings.Save();
        }
    }
}
