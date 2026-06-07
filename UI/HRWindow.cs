using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TycoonGame.Core;

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
        
        private GroupBox grpActions;
        private Button btnHireWorker;
        private Button btnHireManager;
        private Button btnHireScientist;
        private Button btnFireSelected;
        
        private GroupBox grpSalary;
        private Label lblSalaryVal;
        private TrackBar tkSalary;
        private Button btnApplySalary;
        
        private GroupBox grpTraining;
        private Label lblTrainingVal;
        private TrackBar tkTraining;

        public HRWindow(GameEngine gameEngine)
        {
            engine = gameEngine;
            InitializeComponent();
            RefreshData();
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
                Text = "Corporate Personnel Directory",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 140, 200),
                Location = new Point(20, 15),
                Size = new Size(400, 35),
                AutoSize = true
            };
            Controls.Add(lblTitle);

            // Employee ListView Grid
            employeeListView = new ListView
            {
                Location = new Point(20, 60),
                Size = new Size(520, 380),
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

                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, employeeListView.Font, e.Bounds, itemTextColor, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };

            employeeListView.Columns.Add("ID", 70);
            employeeListView.Columns.Add("Name", 120);
            employeeListView.Columns.Add("Role", 80);
            employeeListView.Columns.Add("Morale", 70);
            employeeListView.Columns.Add("Skill Level", 80);
            employeeListView.Columns.Add("Hourly Wage", 90);
            
            employeeListView.SelectedIndexChanged += EmployeeListView_SelectedIndexChanged;
            Controls.Add(employeeListView);

            // Group: Stats Overview
            GroupBox grpStats = new GroupBox
            {
                Text = "Department Summary",
                Location = new Point(20, 455),
                Size = new Size(520, 75),
                ForeColor = Color.FromArgb(170, 175, 190),
                FlatStyle = FlatStyle.Flat
            };

            lblTotalStaff = new Label { Location = new Point(15, 25), Size = new Size(110, 20), ForeColor = Color.White, Text = "Total Employees: 0" };
            lblAvgMorale = new Label { Location = new Point(140, 25), Size = new Size(110, 20), ForeColor = Color.White, Text = "Avg Morale: 0%" };
            lblAvgSkill = new Label { Location = new Point(265, 25), Size = new Size(110, 20), ForeColor = Color.White, Text = "Avg Skill: 0%" };
            lblAvgWage = new Label { Location = new Point(390, 25), Size = new Size(120, 20), ForeColor = Color.White, Text = "Avg Hourly Wage: $0" };

            grpStats.Controls.Add(lblTotalStaff);
            grpStats.Controls.Add(lblAvgMorale);
            grpStats.Controls.Add(lblAvgSkill);
            grpStats.Controls.Add(lblAvgWage);
            Controls.Add(grpStats);

            // Group: Hiring & Actions
            grpActions = new GroupBox
            {
                Text = "Recruitment & Termination",
                Location = new Point(560, 52),
                Size = new Size(250, 170),
                ForeColor = Color.FromArgb(170, 175, 190),
                FlatStyle = FlatStyle.Flat
            };

            btnHireWorker = CreateStyledButton("Recruit Worker ($500)", new Point(15, 25), new Size(220, 30));
            btnHireWorker.Click += (s, e) => RecruitStaff(EmployeeRole.Worker);

            btnHireManager = CreateStyledButton("Recruit Manager ($1500)", new Point(15, 60), new Size(220, 30));
            btnHireManager.Click += (s, e) => RecruitStaff(EmployeeRole.Manager);

            btnHireScientist = CreateStyledButton("Recruit Scientist ($2000)", new Point(15, 95), new Size(220, 30));
            btnHireScientist.Click += (s, e) => RecruitStaff(EmployeeRole.Scientist);

            btnFireSelected = CreateStyledButton("Terminate Selected", new Point(15, 130), new Size(220, 30));
            btnFireSelected.BackColor = Color.FromArgb(140, 40, 40);
            btnFireSelected.Click += BtnFireSelected_Click;

            grpActions.Controls.Add(btnHireWorker);
            grpActions.Controls.Add(btnHireManager);
            grpActions.Controls.Add(btnHireScientist);
            grpActions.Controls.Add(btnFireSelected);
            Controls.Add(grpActions);

            // Group: Individual Salary Adjustment
            grpSalary = new GroupBox
            {
                Text = "Salary Administration",
                Location = new Point(560, 230),
                Size = new Size(250, 140),
                ForeColor = Color.FromArgb(170, 175, 190),
                Enabled = false,
                FlatStyle = FlatStyle.Flat
            };

            Label lblSalaryText = new Label { Text = "Target Hourly Wage:", Location = new Point(15, 25), Size = new Size(120, 20), ForeColor = Color.White };
            lblSalaryVal = new Label { Text = "$0.00 / hr", Location = new Point(140, 25), Size = new Size(90, 20), ForeColor = Color.FromArgb(80, 140, 200), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
            
            tkSalary = new TrackBar
            {
                Location = new Point(15, 50),
                Size = new Size(220, 45),
                Minimum = 10,
                Maximum = 150,
                Value = 15,
                TickFrequency = 10,
                BackColor = Color.FromArgb(24, 28, 36)
            };
            tkSalary.Scroll += TkSalary_Scroll;

            btnApplySalary = CreateStyledButton("Approve Wage Rate", new Point(15, 95), new Size(220, 30));
            btnApplySalary.Click += BtnApplySalary_Click;

            grpSalary.Controls.Add(lblSalaryText);
            grpSalary.Controls.Add(lblSalaryVal);
            grpSalary.Controls.Add(tkSalary);
            grpSalary.Controls.Add(btnApplySalary);
            Controls.Add(grpSalary);

            // Group: Global Training Budget
            grpTraining = new GroupBox
            {
                Text = "Skills Development Training",
                Location = new Point(560, 380),
                Size = new Size(250, 105),
                ForeColor = Color.FromArgb(170, 175, 190),
                FlatStyle = FlatStyle.Flat
            };

            Label lblTrainingText = new Label { Text = "Hourly Training Budget:", Location = new Point(15, 25), Size = new Size(140, 20), ForeColor = Color.White };
            lblTrainingVal = new Label { Text = $"${engine.TrainingBudgetPerHourPerEmployee:F2} / hr", Location = new Point(150, 25), Size = new Size(90, 20), ForeColor = Color.FromArgb(80, 200, 120), Font = new Font("Segoe UI", 9F, FontStyle.Bold) };

            tkTraining = new TrackBar
            {
                Location = new Point(15, 50),
                Size = new Size(220, 45),
                Minimum = 0,
                Maximum = 50,
                Value = (int)engine.TrainingBudgetPerHourPerEmployee,
                TickFrequency = 5,
                BackColor = Color.FromArgb(24, 28, 36)
            };
            tkTraining.Scroll += TkTraining_Scroll;

            grpTraining.Controls.Add(lblTrainingText);
            grpTraining.Controls.Add(lblTrainingVal);
            grpTraining.Controls.Add(tkTraining);
            Controls.Add(grpTraining);

            // Close button
            Button btnClose = CreateStyledButton("Close Window", new Point(560, 498), new Size(250, 32));
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
            // Populate employee listView
            employeeListView.Items.Clear();
            foreach (var emp in engine.Employees)
            {
                ListViewItem item = new ListViewItem(emp.Id);
                item.SubItems.Add(emp.Name);
                item.SubItems.Add(emp.Role.ToString());
                item.SubItems.Add($"{(emp.Morale * 100):F0}%");
                item.SubItems.Add($"{(emp.SkillLevel * 100):F0}%");
                item.SubItems.Add($"${emp.HourlyWage:F2}");
                item.Tag = emp;
                employeeListView.Items.Add(item);
            }

            // Set aggregate statistics Labels
            int count = engine.Employees.Count;
            lblTotalStaff.Text = $"Total Employees: {count}";

            if (count > 0)
            {
                double avgMorale = engine.Employees.Average(e => e.Morale);
                double avgSkill = engine.Employees.Average(e => e.SkillLevel);
                double avgWage = engine.Employees.Average(e => e.HourlyWage);

                lblAvgMorale.Text = $"Avg Morale: {(avgMorale * 100):F0}%";
                lblAvgSkill.Text = $"Avg Skill: {(avgSkill * 100):F0}%";
                lblAvgWage.Text = $"Avg Wage: ${avgWage:F2}/hr";
            }
            else
            {
                lblAvgMorale.Text = "Avg Morale: 0%";
                lblAvgSkill.Text = "Avg Skill: 0%";
                lblAvgWage.Text = "Avg Wage: $0/hr";
            }
        }

        private void EmployeeListView_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (employeeListView.SelectedItems.Count > 0)
            {
                var emp = (Employee)employeeListView.SelectedItems[0].Tag;
                grpSalary.Enabled = true;
                tkSalary.Value = (int)Math.Clamp(emp.HourlyWage, tkSalary.Minimum, tkSalary.Maximum);
                lblSalaryVal.Text = $"${emp.HourlyWage:F2} / hr";
            }
            else
            {
                grpSalary.Enabled = false;
                lblSalaryVal.Text = "$0.00 / hr";
            }
        }

        private void RecruitStaff(EmployeeRole role)
        {
            var emp = engine.HireEmployee(role);
            if (emp != null)
            {
                RefreshData();
            }
            else
            {
                MessageBox.Show("Insufficient cash reserves to fund the recruitment signing bonus!", "HR Department", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnFireSelected_Click(object? sender, EventArgs e)
        {
            if (employeeListView.SelectedItems.Count == 0) return;
            var emp = (Employee)employeeListView.SelectedItems[0].Tag;

            var result = MessageBox.Show($"Are you sure you want to terminate the employment contract for {emp.Name} ({emp.Role})?", "HR Department", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                engine.FireEmployee(emp.Id);
                grpSalary.Enabled = false;
                RefreshData();
            }
        }

        private void TkSalary_Scroll(object? sender, EventArgs e)
        {
            lblSalaryVal.Text = $"${tkSalary.Value}.00 / hr";
        }

        private void BtnApplySalary_Click(object? sender, EventArgs e)
        {
            if (employeeListView.SelectedItems.Count == 0) return;
            var emp = (Employee)employeeListView.SelectedItems[0].Tag;

            emp.HourlyWage = tkSalary.Value;
            RefreshData();
        }

        private void TkTraining_Scroll(object? sender, EventArgs e)
        {
            engine.TrainingBudgetPerHourPerEmployee = tkTraining.Value;
            lblTrainingVal.Text = $"${tkTraining.Value}.00 / hr";
        }
    }
}
