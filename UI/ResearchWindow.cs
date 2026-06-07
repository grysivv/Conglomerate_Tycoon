using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;
using TycoonGame.Core;

namespace TycoonGame.UI
{
    public class ResearchWindow : Form
    {
        private readonly GameEngine engine;
        private Label lblActiveHeader;
        private Label lblResearchSpeed;
        private FlowLayoutPanel flowNodes;
        private Timer refreshTimer;

        public ResearchWindow(GameEngine gameEngine)
        {
            engine = gameEngine;
            InitializeComponent();
            RefreshTechTree();
            
            // Set up a window timer to refresh progress in real-time if open during ticking
            refreshTimer = new Timer { Interval = 1000 };
            refreshTimer.Tick += (s, e) => UpdateActiveProgress();
            refreshTimer.Start();

            FormClosed += (s, e) => refreshTimer.Stop();
        }

        private void InitializeComponent()
        {
            Text = "Research & Development Center";
            Size = new Size(820, 600);
            MinimumSize = new Size(820, 600);
            BackColor = Color.FromArgb(24, 28, 36);
            ForeColor = Color.White;
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;

            // Title
            Label lblTitle = new Label
            {
                Text = "Corporate Technology Tree",
                Font = new Font("Segoe UI", 16F, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 140, 200),
                Location = new Point(20, 15),
                Size = new Size(400, 35),
                AutoSize = true
            };
            Controls.Add(lblTitle);

            // Active Research Header Panel
            Panel pnlHeader = new Panel
            {
                Location = new Point(20, 60),
                Size = new Size(765, 80),
                BackColor = Color.FromArgb(32, 38, 48)
            };

            lblActiveHeader = new Label
            {
                Text = "Active Project: Idle",
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                ForeColor = Color.White,
                Location = new Point(15, 15),
                Size = new Size(400, 20),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblActiveHeader);

            lblResearchSpeed = new Label
            {
                Text = "Scientist Output: 0 RP/hr (0 active scientists)",
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                ForeColor = Color.FromArgb(170, 175, 190),
                Location = new Point(15, 45),
                Size = new Size(400, 15),
                AutoSize = true
            };
            pnlHeader.Controls.Add(lblResearchSpeed);

            Controls.Add(pnlHeader);

            // Scrollable Flow Panel for Technology Cards
            flowNodes = new FlowLayoutPanel
            {
                Location = new Point(20, 160),
                Size = new Size(765, 330),
                AutoScroll = true,
                BackColor = Color.FromArgb(20, 24, 30),
                Padding = new Padding(10)
            };
            Controls.Add(flowNodes);

            // Close Button
            Button btnClose = new Button
            {
                Text = "Close R&D Panel",
                Location = new Point(535, 510),
                Size = new Size(250, 32),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(48, 56, 70),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnClose.FlatAppearance.BorderSize = 0;
            btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(64, 100, 150);
            btnClose.Click += (s, e) => Close();
            Controls.Add(btnClose);
        }

        private void RefreshTechTree()
        {
            flowNodes.Controls.Clear();

            // Calculate scientist speed
            double speed = 0;
            int scientists = 0;
            foreach (var emp in engine.Employees)
            {
                if (emp.Role == EmployeeRole.Scientist && emp.AssignedX != -1)
                {
                    Tile tile = engine.Grid[emp.AssignedX, emp.AssignedY];
                    if (tile.Type == TileType.Office)
                    {
                        double powerFactor = tile.IsPowered ? 1.0 : 0.2;
                        speed += 3.0 * emp.GetPerformanceMultiplier() * powerFactor;
                        scientists++;
                    }
                }
            }

            lblResearchSpeed.Text = $"Scientist Output: {speed:F1} RP/hr ({scientists} active scientists assigned to offices)";

            if (engine.ActiveResearch != null)
            {
                lblActiveHeader.Text = $"Active Project: {engine.ActiveResearch.Name} ({(engine.ActiveResearch.GetProgressPercentage() * 100):F0}%)";
            }
            else
            {
                lblActiveHeader.Text = "Active Project: Idle (Select a project card below)";
            }

            foreach (var node in engine.TechTree)
            {
                Panel card = CreateNodeCard(node);
                flowNodes.Controls.Add(card);
            }
        }

        private Panel CreateNodeCard(ResearchNode node)
        {
            Panel p = new Panel
            {
                Size = new Size(350, 140),
                BackColor = Color.FromArgb(38, 42, 54),
                Margin = new Padding(10)
            };

            // Title
            Label lblName = new Label
            {
                Text = node.Name,
                Font = new Font("Segoe UI", 11F, FontStyle.Bold),
                Location = new Point(15, 12),
                Size = new Size(220, 20),
                AutoSize = true
            };

            // Color code state
            bool parentCompleted = string.IsNullOrEmpty(node.ParentNodeId) || 
                                   engine.TechTree.First(t => t.Id == node.ParentNodeId).IsCompleted;

            if (node.IsCompleted)
            {
                lblName.ForeColor = Color.FromArgb(80, 200, 120); // Green
            }
            else if (!parentCompleted)
            {
                lblName.ForeColor = Color.FromArgb(120, 120, 125); // Dark grey (locked)
            }
            else if (engine.ActiveResearch == node)
            {
                lblName.ForeColor = Color.FromArgb(230, 140, 80); // Orange (active)
            }
            else
            {
                lblName.ForeColor = Color.FromArgb(80, 140, 200); // Blue (unlocked)
            }
            p.Controls.Add(lblName);

            // Description
            Label lblDesc = new Label
            {
                Text = node.Description,
                Font = new Font("Segoe UI", 8.25F, FontStyle.Regular),
                ForeColor = Color.FromArgb(170, 175, 190),
                Location = new Point(15, 38),
                Size = new Size(320, 40),
                FlatStyle = FlatStyle.Flat
            };
            p.Controls.Add(lblDesc);

            // Progress bar panel
            Panel pnlProgress = new Panel
            {
                Location = new Point(15, 85),
                Size = new Size(200, 8),
                BackColor = Color.FromArgb(48, 52, 64)
            };

            int fillWidth = (int)(pnlProgress.Width * node.GetProgressPercentage());
            if (fillWidth > 0)
            {
                Panel pnlFill = new Panel
                {
                    Location = new Point(0, 0),
                    Size = new Size(fillWidth, 8),
                    BackColor = node.IsCompleted ? Color.FromArgb(80, 200, 120) : Color.FromArgb(230, 140, 80)
                };
                pnlProgress.Controls.Add(pnlFill);
            }
            p.Controls.Add(pnlProgress);

            // Points indicator label
            Label lblPoints = new Label
            {
                Text = node.IsCompleted ? "Completed" : $"{node.PointsInvested:F0} / {node.ResearchPointCost:F0} RP",
                Font = new Font("Segoe UI", 8F, FontStyle.Regular),
                ForeColor = Color.FromArgb(140, 145, 160),
                Location = new Point(15, 98),
                Size = new Size(150, 15),
                AutoSize = true
            };
            
            if (!parentCompleted)
            {
                var parent = engine.TechTree.First(t => t.Id == node.ParentNodeId);
                lblPoints.Text = $"Locked: Requires {parent.Name}";
                lblPoints.ForeColor = Color.FromArgb(180, 90, 90);
            }
            p.Controls.Add(lblPoints);

            // Research Action Button
            Button btnAction = new Button
            {
                Text = node.IsCompleted ? "Finished" : (engine.ActiveResearch == node ? "Active" : "Research"),
                Location = new Point(230, 85),
                Size = new Size(100, 28),
                FlatStyle = FlatStyle.Flat,
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            
            btnAction.FlatAppearance.BorderSize = 0;

            if (node.IsCompleted || !parentCompleted)
            {
                btnAction.Enabled = false;
                btnAction.BackColor = Color.FromArgb(48, 52, 60);
                btnAction.ForeColor = Color.FromArgb(110, 110, 115);
            }
            else if (engine.ActiveResearch == node)
            {
                btnAction.BackColor = Color.FromArgb(140, 70, 20);
                btnAction.Click += (s, e) =>
                {
                    engine.ActiveResearch = null; // Pause research
                    RefreshTechTree();
                };
            }
            else
            {
                btnAction.BackColor = Color.FromArgb(80, 140, 200);
                btnAction.FlatAppearance.MouseOverBackColor = Color.FromArgb(100, 165, 230);
                btnAction.Click += (s, e) =>
                {
                    engine.ActiveResearch = node;
                    RefreshTechTree();
                };
            }
            p.Controls.Add(btnAction);

            return p;
        }

        private void UpdateActiveProgress()
        {
            if (engine.ActiveResearch != null)
            {
                // Refresh progress displays on screen
                double pct = engine.ActiveResearch.GetProgressPercentage();
                lblActiveHeader.Text = $"Active Project: {engine.ActiveResearch.Name} ({(pct * 100):F0}%)";

                // Fast refresh: redraw the panel controls if we've reached 100% or just to show progress
                // Let's do a simple full refresh of the cards to avoid UI misalignments
                RefreshTechTree();
            }
        }
    }
}
