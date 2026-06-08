using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TycoonGame.Core;
using Timer = System.Windows.Forms.Timer;

namespace TycoonGame.UI
{
    public class HRWindow : Form
    {
        private readonly GameEngine engine;
        private ListView employeeListView;
        private Label lblTotalStaff;
        private Label lblAvgMorale;
        private Label lblAvgSkill;
        private Label lblAvgWage;
        private Timer refreshTimer;

        public HRWindow(GameEngine gameEngine)
        {
            engine = gameEngine;
            InitializeComponent();
            RefreshData();

            // Real-time refresh timer
            refreshTimer = new Timer { Interval = 1000 };
            refreshTimer.Tick += (s, e) => RefreshData();
            refreshTimer.Start();

            FormClosed += (s, e) => refreshTimer.Stop();

            // Enable Double Buffering to reduce repaint flickering
            EnableDoubleBuffered(this);
            EnableDoubleBuffered(employeeListView);
        }

        private void InitializeComponent()
        {
            // Set Form Settings
            Text = "Human Resources & Payroll Department";
            Size = new Size(850, 580);
            MinimumSize = new Size(850, 580);
            BackColor = Color.FromArgb(24, 28, 36);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            // Title Label
            Label lblTitle = new Label
            {
                Text = "Corporate Personnel & Departments Directory",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 140, 200),
                Location = new Point(20, 15),
                Size = new Size(500, 35),
                AutoSize = true
            };
            Controls.Add(lblTitle);

            // Employee/Department ListView Grid
            employeeListView = new ListView
            {
                Location = new Point(20, 60),
                Size = new Size(795, 380),
                View = View.Details,
                FullRowSelect = true,
                GridLines = false,
                BackColor = Color.FromArgb(32, 38, 48),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.None,
                HeaderStyle = ColumnHeaderStyle.Nonclickable
            };
            
            // Add custom draw events for listview to paint custom dark-mode headers and rows
            employeeListView.OwnerDraw = true;
            employeeListView.DrawColumnHeader += (s, e) =>
            {
                using Brush brush = new SolidBrush(Color.FromArgb(48, 56, 70));
                e.Graphics.FillRectangle(brush, e.Bounds);
                TextRenderer.DrawText(e.Graphics, e.Header.Text, employeeListView.Font, e.Bounds, Color.FromArgb(180, 200, 230), TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };
            employeeListView.DrawSubItem += (s, e) =>
            {
                bool isSelected = e.Item.Selected;
                using Brush bgBrush = new SolidBrush(isSelected ? Color.FromArgb(64, 100, 150) : Color.FromArgb(32, 38, 48));
                e.Graphics.FillRectangle(bgBrush, e.Bounds);

                Color itemTextColor = Color.White;
                if (e.ColumnIndex == 3) // Morale column
                {
                    double moraleVal = double.Parse(e.SubItem.Text.Replace("%", "")) / 100.0;
                    itemTextColor = moraleVal < 0.4 ? Color.FromArgb(240, 100, 100) : (moraleVal > 0.75 ? Color.FromArgb(100, 240, 140) : Color.White);
                }
                else if (e.ColumnIndex == 4) // Skill Level column
                {
                    double skillVal = double.Parse(e.SubItem.Text.Replace("%", "")) / 100.0;
                    itemTextColor = skillVal < 0.3 ? Color.FromArgb(240, 150, 100) : (skillVal > 0.75 ? Color.FromArgb(100, 240, 200) : Color.White);
                }

                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, employeeListView.Font, e.Bounds, itemTextColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };

            employeeListView.Columns.Add("Sector Address", 130);
            employeeListView.Columns.Add("Building Type", 150);
            employeeListView.Columns.Add("Hired Staff", 130);
            employeeListView.Columns.Add("Morale", 100);
            employeeListView.Columns.Add("Skill Level", 110);
            employeeListView.Columns.Add("Hourly Upkeep", 150);
            
            Controls.Add(employeeListView);

            // Group: Stats Overview
            GroupBox grpStats = new GroupBox
            {
                Text = "Corporate Workforce Summary",
                Location = new Point(20, 455),
                Size = new Size(520, 75),
                ForeColor = Color.FromArgb(170, 175, 190),
                FlatStyle = FlatStyle.Flat
            };

            lblTotalStaff = new Label { Location = new Point(15, 25), Size = new Size(130, 20), ForeColor = Color.White, Text = "Total Hired: 0" };
            lblAvgMorale = new Label { Location = new Point(150, 25), Size = new Size(110, 20), ForeColor = Color.White, Text = "Avg Morale: 0%" };
            lblAvgSkill = new Label { Location = new Point(270, 25), Size = new Size(110, 20), ForeColor = Color.White, Text = "Avg Skill: 0%" };
            lblAvgWage = new Label { Location = new Point(390, 25), Size = new Size(120, 20), ForeColor = Color.White, Text = "Avg Base Wage: $0/hr" };

            grpStats.Controls.Add(lblTotalStaff);
            grpStats.Controls.Add(lblAvgMorale);
            grpStats.Controls.Add(lblAvgSkill);
            grpStats.Controls.Add(lblAvgWage);
            Controls.Add(grpStats);

            // Close button
            Button btnClose = CreateStyledButton("Close HR Dashboard", new Point(565, 470), new Size(250, 45));
            btnClose.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);
        }

        private Button CreateStyledButton(string text, Point loc, Size sz)
        {
            Button btn = new Button
            {
                Text = text,
                Location = loc,
                Size = sz,
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(48, 56, 70),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 100, 150);
            return btn;
        }

        private void RefreshData()
        {
            // Prevent listview refresh scroll reset by preserving top item if possible
            int topIndex = employeeListView.TopItem?.Index ?? -1;
            
            // Selected item tracking
            string selectedAddress = "";
            if (employeeListView.SelectedItems.Count > 0)
            {
                selectedAddress = employeeListView.SelectedItems[0].Text;
            }

            employeeListView.BeginUpdate();
            employeeListView.Items.Clear();

            int totalStaff = 0;
            double moraleSum = 0;
            double skillSum = 0;
            double wageSum = 0;
            int activeDepts = 0;

            double wageScaleFactor = 1.0 + (0.06 - engine.Unemployment_Rate) * 1.5;

            for (int x = 0; x < GameEngine.MapSize; x++)
            {
                for (int y = 0; y < GameEngine.MapSize; y++)
                {
                    Tile tile = engine.Grid[x, y];
                    if (tile.Type != TileType.Grass && tile.Type != TileType.Road)
                    {
                        activeDepts++;
                        totalStaff += tile.EmployeeCount;
                        moraleSum += tile.Morale;
                        skillSum += tile.SkillLevel;

                        double baseWage = tile.Type switch
                        {
                            TileType.Office => 30.00,
                            _ => 17.50
                        };
                        double hourlyWage = baseWage * wageScaleFactor;
                        wageSum += hourlyWage;

                        ListViewItem item = new ListViewItem($"[{tile.X}, {tile.Y}]");
                        item.SubItems.Add(tile.Type.ToString());
                        item.SubItems.Add($"{tile.EmployeeCount} / {tile.MaxEmployees}");
                        item.SubItems.Add($"{(tile.Morale * 100):F0}%");
                        item.SubItems.Add($"{(tile.SkillLevel * 100):F0}%");

                        double upkeep = tile.MaintenanceCost + (hourlyWage * tile.EmployeeCount);
                        item.SubItems.Add($"${upkeep:F0}/hr");
                        item.Tag = tile;

                        if (item.Text == selectedAddress)
                        {
                            item.Selected = true;
                        }

                        employeeListView.Items.Add(item);
                    }
                }
            }

            employeeListView.EndUpdate();

            if (topIndex >= 0 && topIndex < employeeListView.Items.Count)
            {
                employeeListView.TopItem = employeeListView.Items[topIndex];
            }

            // Set aggregate statistics Labels
            lblTotalStaff.Text = $"Total Hired: {totalStaff}";

            if (activeDepts > 0)
            {
                lblAvgMorale.Text = $"Avg Morale: {((moraleSum / activeDepts) * 100.0):F0}%";
                lblAvgSkill.Text = $"Avg Skill: {((skillSum / activeDepts) * 100.0):F0}%";
                lblAvgWage.Text = $"Avg Base Wage: ${(wageSum / activeDepts):F2}/hr";
            }
            else
            {
                lblAvgMorale.Text = "Avg Morale: 0%";
                lblAvgSkill.Text = "Avg Skill: 0%";
                lblAvgWage.Text = "Avg Wage: $0/hr";
            }
        }

        private void EnableDoubleBuffered(Control control)
        {
            try
            {
                typeof(Control).GetProperty("DoubleBuffered", 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Instance)
                    ?.SetValue(control, true);
            }
            catch { }
        }
    }
}
