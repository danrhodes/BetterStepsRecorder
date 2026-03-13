using System;
using System.Drawing;
using System.Windows.Forms;

namespace BetterStepsRecorder.UI.Dialogs
{
    /// <summary>
    /// Dialog for choosing the click indicator style drawn on captured screenshots.
    /// </summary>
    public class ClickIndicatorStyleDialog : Form
    {
        private RadioButton rdoArrow;
        private RadioButton rdoCircle;
        private RadioButton rdoCursor;
        private Button btnOK;
        private Button btnCancel;

        public ClickIndicatorStyle SelectedStyle { get; private set; }

        public ClickIndicatorStyleDialog(ClickIndicatorStyle current)
        {
            SelectedStyle = current;
            BuildUI();
            LoadCurrent(current);
        }

        private void BuildUI()
        {
            Text = "Click Indicator Style";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;
            Font = new Font("Segoe UI", 9f);

            int y = 16;

            Label lblNote = new Label
            {
                Text = "Choose how clicks are highlighted on captured screenshots:",
                Location = new Point(14, y),
                Size = new Size(390, 32),
                ForeColor = SystemColors.GrayText
            };
            Controls.Add(lblNote);
            y += 40;

            rdoArrow = AddRadio("Arrow  —  long pointer arrow (original style)", ref y);
            rdoCircle = AddRadio("Circle  —  highlighted ring around the click point", ref y);
            rdoCursor = AddRadio("Cursor  —  mouse pointer shape at the click point", ref y);

            y += 12;
            btnOK = new Button { Text = "OK", DialogResult = DialogResult.OK, Location = new Point(222, y), Size = new Size(72, 26) };
            btnCancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Location = new Point(302, y), Size = new Size(72, 26) };
            Controls.Add(btnOK);
            Controls.Add(btnCancel);
            AcceptButton = btnOK;
            CancelButton = btnCancel;

            ClientSize = new Size(390, y + 50);
            btnOK.Click += (s, e) => SaveSelection();
        }

        private RadioButton AddRadio(string text, ref int y)
        {
            var rb = new RadioButton
            {
                Text = text,
                Location = new Point(14, y),
                AutoSize = true
            };
            Controls.Add(rb);
            y += 26;
            return rb;
        }

        private void LoadCurrent(ClickIndicatorStyle style)
        {
            rdoArrow.Checked  = style == ClickIndicatorStyle.Arrow;
            rdoCircle.Checked = style == ClickIndicatorStyle.Circle;
            rdoCursor.Checked = style == ClickIndicatorStyle.Cursor;
        }

        private void SaveSelection()
        {
            if (rdoCircle.Checked) SelectedStyle = ClickIndicatorStyle.Circle;
            else if (rdoCursor.Checked) SelectedStyle = ClickIndicatorStyle.Cursor;
            else SelectedStyle = ClickIndicatorStyle.Arrow;
        }
    }
}
