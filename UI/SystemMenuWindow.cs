using System;
using System.Drawing;
using System.IO;
using System.Text.Json;
using System.Windows.Forms;
using TycoonGame.Core;

namespace TycoonGame.UI
{
    public class SystemMenuWindow : Form
    {
        private readonly GameEngine engine;
        private TableLayoutPanel mainLayout;
        private TableLayoutPanel slotLayout;
        public bool ShouldExitToMainMenu { get; private set; } = false;

        public SystemMenuWindow(GameEngine engine)
        {
            this.engine = engine;
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            Text = "SYSTEM CONFIGURATION & SAVING";
            Size = new Size(550, 700);
            MinimumSize = new Size(550, 700);
            BackColor = Color.FromArgb(15, 20, 35);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterParent;

            mainLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 5,
                Padding = new Padding(20),
                BackColor = Color.FromArgb(15, 20, 35)
            };
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F)); // Title
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F)); // Quicksave Button
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // Scrollable slots grid
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Exit button
            mainLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50F)); // Resume button
            Controls.Add(mainLayout);

            Label lblTitle = new Label
            {
                Text = "SYSTEM REGISTRY & DATA STORAGE",
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                ForeColor = Color.FromArgb(0, 240, 255),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };
            mainLayout.Controls.Add(lblTitle, 0, 0);

            Button btnQuicksave = CreateSystemButton("QUICKSAVE SYSTEM STATE (autosave.sav)", BtnQuicksave_Click);
            btnQuicksave.ForeColor = Color.FromArgb(0, 255, 102);
            btnQuicksave.FlatAppearance.BorderColor = Color.FromArgb(0, 255, 102);
            mainLayout.Controls.Add(btnQuicksave, 0, 1);

            // Scrollable Panel for slots
            Panel pnlSlotsContainer = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Margin = new Padding(0, 10, 0, 10)
            };
            mainLayout.Controls.Add(pnlSlotsContainer, 0, 2);

            slotLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Top,
                ColumnCount = 1,
                RowCount = 10,
                Height = 460,
                Padding = new Padding(0, 0, 15, 0) // Leave room for scrollbar
            };
            for (int i = 0; i < 10; i++)
            {
                slotLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 45F));
            }
            pnlSlotsContainer.Controls.Add(slotLayout);

            PopulateSlots();

            Button btnExit = CreateSystemButton("DISCONNECT & EXIT TO MAIN MENU", BtnExit_Click);
            btnExit.ForeColor = Color.FromArgb(255, 80, 80);
            btnExit.FlatAppearance.BorderColor = Color.FromArgb(255, 80, 80);
            mainLayout.Controls.Add(btnExit, 0, 3);

            Button btnResume = CreateSystemButton("RESUME SIMULATION", (s, e) => Close());
            mainLayout.Controls.Add(btnResume, 0, 4);
        }

        private void PopulateSlots()
        {
            slotLayout.Controls.Clear();
            for (int i = 1; i <= 10; i++)
            {
                int slotIndex = i;
                string metadata = GetSlotMetadataText(slotIndex);
                bool hasSave = !string.IsNullOrEmpty(metadata);

                Button btnSlot = new Button
                {
                    Dock = DockStyle.Fill,
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                    Cursor = Cursors.Hand,
                    Margin = new Padding(0, 3, 0, 3)
                };

                if (hasSave)
                {
                    btnSlot.Text = $"OVERWRITE SLOT {slotIndex}: {metadata}";
                    btnSlot.BackColor = Color.FromArgb(26, 35, 48);
                    btnSlot.ForeColor = Color.FromArgb(255, 153, 0); // Amber warning
                    btnSlot.FlatAppearance.BorderColor = Color.FromArgb(255, 153, 0);
                }
                else
                {
                    btnSlot.Text = $"[ INITIALIZE SLOT {slotIndex} - EMPTY ]";
                    btnSlot.BackColor = Color.FromArgb(20, 24, 33);
                    btnSlot.ForeColor = Color.FromArgb(113, 128, 150);
                    btnSlot.FlatAppearance.BorderColor = Color.FromArgb(45, 55, 72);
                }

                btnSlot.Click += (s, e) => SaveToSlot(slotIndex);
                slotLayout.Controls.Add(btnSlot, 0, i - 1);
            }
        }

        private void SaveToSlot(int slotIndex)
        {
            try
            {
                string path = $"save_slot_{slotIndex}.json";
                engine.SaveToFile(path);
                MessageBox.Show($"Simulation state saved successfully to Slot {slotIndex}!", "Save Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
                PopulateSlots();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save state to slot: {ex.Message}", "Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnQuicksave_Click(object? sender, EventArgs e)
        {
            try
            {
                engine.SaveToFile("autosave.sav");
                MessageBox.Show("Simulation state quicksaved successfully to autosave.sav!", "Quicksave Complete", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to quicksave: {ex.Message}", "Quicksave Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnExit_Click(object? sender, EventArgs e)
        {
            var confirm = MessageBox.Show(
                "Are you sure you want to disconnect? Unsaved changes since your last save will be lost (an autosave will be generated).",
                "Exit Simulation",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (confirm == DialogResult.Yes)
            {
                try
                {
                    engine.SaveToFile("autosave.sav"); // Auto quicksave on exit
                }
                catch { }

                ShouldExitToMainMenu = true;
                Close();
            }
        }

        private string GetSlotMetadataText(int slotIndex)
        {
            string fileName = $"save_slot_{slotIndex}.json";
            if (File.Exists(fileName))
            {
                try
                {
                    string json = File.ReadAllText(fileName);
                    var data = JsonSerializer.Deserialize<SaveData>(json);
                    if (data != null)
                    {
                        string formattedDate = "Unknown Date";
                        if (DateTime.TryParse(data.CurrentDate, out DateTime parsedDate))
                        {
                            formattedDate = parsedDate.ToString("dd MMM yyyy HH:mm");
                        }
                        else if (!string.IsNullOrEmpty(data.CurrentDate))
                        {
                            formattedDate = data.CurrentDate;
                        }
                        return $"{data.CompanyName} ({formattedDate})";
                    }
                }
                catch
                {
                    return "[ CORRUPT SAVE ]";
                }
            }
            return "";
        }

        private Button CreateSystemButton(string text, EventHandler onClick)
        {
            Button btn = new Button
            {
                Text = text,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(26, 32, 44),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 5, 0, 5)
            };
            btn.FlatAppearance.BorderSize = 1;
            btn.FlatAppearance.BorderColor = Color.FromArgb(45, 55, 72);
            btn.Click += onClick;
            return btn;
        }
    }
}
