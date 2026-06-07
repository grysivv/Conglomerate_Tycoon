using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TycoonGame.Core;
using Color = Microsoft.Xna.Framework.Color;

namespace TycoonGame.Rendering
{
    public static class SpriteGenerator
    {
        private static readonly Random Random = new Random(42); // Seeded for deterministic patterns

        public static Texture2D CreateGrassTexture(GraphicsDevice device, int width, int height)
        {
            Texture2D texture = new Texture2D(device, width, height);
            Color[] data = new Color[width * height];

            double cx = width / 2.0;
            double cy = height / 2.0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;

                    if (IsInsideDiamond(x, y, width, height))
                    {
                        // Generate rich grassy green with organic noise
                        int baseGreen = 120 + Random.Next(-15, 15);
                        int baseRed = 40 + Random.Next(-5, 5);
                        int baseBlue = 30 + Random.Next(-5, 5);

                        // Subtle border shading for grid structure
                        double distToEdge = GetDistanceToDiamondEdge(x, y, width, height);
                        if (distToEdge < 0.08)
                        {
                            // Darken edges slightly to frame the tiles
                            double shadowFactor = 0.7 + (distToEdge / 0.08) * 0.3;
                            baseRed = (int)(baseRed * shadowFactor);
                            baseGreen = (int)(baseGreen * shadowFactor);
                            baseBlue = (int)(baseBlue * shadowFactor);
                        }

                        data[index] = new Color(baseRed, baseGreen, baseBlue, 255);
                    }
                    else
                    {
                        data[index] = Color.Transparent;
                    }
                }
            }

            texture.SetData(data);
            return texture;
        }

        public static Texture2D CreateRoadTexture(GraphicsDevice device, int width, int height)
        {
            Texture2D texture = new Texture2D(device, width, height);
            Color[] data = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;

                    if (IsInsideDiamond(x, y, width, height))
                    {
                        // Asphalt dark grey color with slight noise
                        int grey = 45 + Random.Next(-3, 3);
                        Color pixelColor = new Color(grey, grey, grey + 2, 255);

                        double cx = width / 2.0;
                        double cy = height / 2.0;
                        
                        // Distance to the center lines (for road markings)
                        // Diagonal 1: (col - row) axis
                        double d1 = Math.Abs((y - cy) - 0.5 * (x - cx));
                        // Diagonal 2: (col + row) axis
                        double d2 = Math.Abs((y - cy) + 0.5 * (x - cx));

                        // Draw yellow dashed center lines
                        bool onD1Line = d1 < 1.5;
                        bool onD2Line = d2 < 1.5;

                        // Create dashed segments
                        bool d1Dash = (x % 16 < 8);
                        bool d2Dash = (y % 8 < 4);

                        if ((onD1Line && d1Dash) || (onD2Line && d2Dash))
                        {
                            pixelColor = new Color(220, 180, 20); // Golden yellow road line
                        }

                        // Curbs (borders of the road diamond)
                        double distToEdge = GetDistanceToDiamondEdge(x, y, width, height);
                        if (distToEdge < 0.05)
                        {
                            pixelColor = new Color(130, 130, 135); // Concrete grey curb
                        }

                        data[index] = pixelColor;
                    }
                    else
                    {
                        data[index] = Color.Transparent;
                    }
                }
            }

            texture.SetData(data);
            return texture;
        }

        public static Texture2D CreateBuildingTexture(GraphicsDevice device, int width, int height, TileType type)
        {
            // Buildings are drawn inside 128x128 textures
            Texture2D texture = new Texture2D(device, width, height);
            Color[] data = new Color[width * height];

            int hHalf = height / 2; // Wall junction point (y=64)

            Color roofColor = Color.Red;
            Color leftWallColor = Color.LightGray;
            Color rightWallColor = Color.Gray;

            switch (type)
            {
                case TileType.Office:
                    roofColor = new Color(30, 50, 80); // Dark steel blue
                    leftWallColor = new Color(80, 140, 200); // Glass blue
                    rightWallColor = new Color(50, 100, 150); // Shaded glass blue
                    break;
                case TileType.Factory:
                    roofColor = new Color(110, 50, 40); // Rusted iron
                    leftWallColor = new Color(140, 140, 140); // Concrete grey
                    rightWallColor = new Color(100, 100, 100); // Shaded concrete
                    break;
                case TileType.Retail:
                    roofColor = new Color(50, 120, 70); // Green awning
                    leftWallColor = new Color(180, 110, 90); // Red brick
                    rightWallColor = new Color(140, 80, 65); // Shaded brick
                    break;
                case TileType.PowerPlant:
                    roofColor = new Color(40, 40, 45); // Dark carbon sheet
                    leftWallColor = new Color(85, 85, 90); // Dark factory plates
                    rightWallColor = new Color(60, 60, 65); // Shaded plates
                    break;
                case TileType.Apartment:
                    roofColor = new Color(75, 75, 80); // Flat gravel roof
                    leftWallColor = new Color(200, 160, 120); // Warm brick beige
                    rightWallColor = new Color(160, 125, 95); // Shaded beige
                    break;
                case TileType.University:
                    roofColor = new Color(40, 60, 110); // Classical slate blue
                    leftWallColor = new Color(170, 70, 60); // Classic red brick
                    rightWallColor = new Color(130, 50, 40); // Shaded red brick
                    break;
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    
                    // 1. Check Roof (Diamond in upper half)
                    // The roof diamond spans y=0 to y=64, centered at x=64, y=32
                    if (IsInsideDiamond(x, y, width, hHalf))
                    {
                        Color c = roofColor;
                        // Draw metal seams or brick tile textures on roof
                        if (type == TileType.PowerPlant)
                        {
                            if (x % 16 == 0 || y % 8 == 0) c = Color.Black; // Cooling vents
                        }
                        else if (type == TileType.Factory)
                        {
                            if (x % 8 == 0) c = new Color(90, 40, 30); // Corrugated lines
                        }
                        else if (type == TileType.Office)
                        {
                            if (x % 32 == 0 || y % 16 == 0) c = new Color(15, 30, 50); // Glass frame
                        }
                        data[index] = c;
                    }
                    // 2. Check Left Wall
                    // Bounded by: 0 <= x < 64 and (0.5x + 32) <= y < (0.5x + 96)
                    else if (x < width / 2 && y >= (0.5 * x + hHalf / 2) && y < (0.5 * x + hHalf + hHalf / 2))
                    {
                        Color c = leftWallColor;
                        int localX = x;
                        int localY = y - (int)(0.5 * x);

                        // Draw detail features (windows, doors)
                        if (type == TileType.Office)
                        {
                            // Window grid
                            if (localX % 16 > 4 && localX % 16 < 12 && localY % 16 > 4 && localY % 16 < 12)
                            {
                                // Draw glowing yellow offices or deep blue sky reflection
                                c = (Random.Next(10) > 7) ? new Color(250, 240, 150) : new Color(180, 220, 255);
                            }
                        }
                        else if (type == TileType.Factory)
                        {
                            // Loading bay door
                            if (localX > 16 && localX < 48 && localY > 64)
                            {
                                c = new Color(60, 60, 65); // Large iron roll door
                            }
                            else if (localX % 20 > 8 && localX % 20 < 16 && localY % 24 > 10 && localY % 24 < 18)
                            {
                                c = new Color(40, 40, 40); // Dark high vent window
                            }
                        }
                        else if (type == TileType.Retail)
                        {
                            // Big shop window display
                            if (localX > 12 && localX < 52 && localY > 50 && localY < 85)
                            {
                                c = new Color(120, 200, 230); // Glass show-panel
                                if (localY > 70 && localX % 8 > 2) c = new Color(180, 80, 50); // Display items
                            }
                        }
                        else if (type == TileType.PowerPlant)
                        {
                            // Thick hazard warning stripes
                            if ((localX + localY) % 16 < 8 && localY > 70)
                            {
                                c = new Color(210, 170, 10); // Bright hazard yellow
                            }
                        }
                        else if (type == TileType.Apartment)
                        {
                            // Balconies and windows
                            if (localY % 24 > 4 && localY % 24 < 14)
                            {
                                if (localY % 24 >= 11 && localX % 16 >= 2 && localX % 16 <= 14)
                                {
                                    c = new Color(50, 50, 50); // Dark iron balcony railing
                                }
                                else if ((localX % 16 > 3 && localX % 16 < 7) || (localX % 16 > 9 && localX % 16 < 13))
                                {
                                    c = (Random.Next(10) > 6) ? new Color(250, 225, 120) : new Color(100, 150, 190);
                                }
                            }
                        }
                        else if (type == TileType.University)
                        {
                            // Arched columns and windows (classic campus look)
                            if (localX % 20 >= 4 && localX % 20 <= 16 && localY % 24 > 4 && localY % 24 < 18)
                            {
                                if (localY % 24 < 8)
                                {
                                    c = new Color(230, 225, 210); // Limestone arch trim
                                }
                                else
                                {
                                    c = new Color(80, 150, 200); // Blue glass window pane
                                }
                            }
                            else if (localX % 20 < 4 || localX % 20 > 16)
                            {
                                c = new Color(210, 205, 190); // White limestone pillars
                            }
                        }

                        data[index] = c;
                    }
                    // 3. Check Right Wall
                    // Bounded by: 64 <= x < 128 and (-0.5x + 96) <= y < (-0.5x + 160)
                    else if (x >= width / 2 && y >= (-0.5 * x + hHalf + hHalf / 2) && y < (-0.5 * x + height + hHalf / 2))
                    {
                        Color c = rightWallColor;
                        int localX = x - width / 2;
                        int localY = y - (int)(-0.5 * x + height);

                        // Draw details
                        if (type == TileType.Office)
                        {
                            // Windows in shadow
                            if (localX % 16 > 4 && localX % 16 < 12 && localY % 16 > 4 && localY % 16 < 12)
                            {
                                c = (Random.Next(10) > 8) ? new Color(220, 210, 120) : new Color(120, 160, 200);
                            }
                        }
                        else if (type == TileType.Factory)
                        {
                            // Large ventilation fans
                            if (localX > 20 && localX < 44 && localY > 45 && localY < 69)
                            {
                                double rx = localX - 32;
                                double ry = localY - 57;
                                if (rx * rx + ry * ry < 100)
                                {
                                    c = Color.Black; // Vent hole
                                }
                            }
                        }
                        else if (type == TileType.Retail)
                        {
                            // Shop entrance doors
                            if (localX > 16 && localX < 48 && localY > 45)
                            {
                                c = new Color(70, 70, 75); // Door frame
                                if (localX > 20 && localX < 44 && localY > 50 && localY < 90)
                                {
                                    c = new Color(200, 230, 250); // Glass door pane
                                }
                            }
                        }
                        else if (type == TileType.PowerPlant)
                        {
                            // Large ventilation pipes
                            if (localX > 12 && localX < 24 && localY > 40 && localY < 90)
                            {
                                c = new Color(40, 40, 45); // Metal pipe running up the wall
                            }
                        }
                        else if (type == TileType.Apartment)
                        {
                            // Shaded balconies and windows
                            if (localY % 24 > 4 && localY % 24 < 14)
                            {
                                if (localY % 24 >= 11 && localX % 16 >= 2 && localX % 16 <= 14)
                                {
                                    c = new Color(30, 30, 30); // Balcony in shadow
                                }
                                else if ((localX % 16 > 3 && localX % 16 < 7) || (localX % 16 > 9 && localX % 16 < 13))
                                {
                                    c = (Random.Next(10) > 7) ? new Color(180, 160, 90) : new Color(60, 100, 130);
                                }
                            }
                        }
                        else if (type == TileType.University)
                        {
                            // Shaded arched windows and columns
                            if (localX % 20 >= 4 && localX % 20 <= 16 && localY % 24 > 4 && localY % 24 < 18)
                            {
                                if (localY % 24 < 8)
                                {
                                    c = new Color(170, 165, 155); // Shaded arch trim
                                }
                                else
                                {
                                    c = new Color(50, 100, 140); // Shaded window pane
                                }
                            }
                            else if (localX % 20 < 4 || localX % 20 > 16)
                            {
                                c = new Color(160, 155, 140); // Shaded pillars
                            }
                        }

                        data[index] = c;
                    }
                    else
                    {
                        data[index] = Color.Transparent;
                    }
                }
            }

            texture.SetData(data);
            return texture;
        }

        public static Texture2D CreateSelectorTexture(GraphicsDevice device, int width, int height)
        {
            Texture2D texture = new Texture2D(device, width, height);
            Color[] data = new Color[width * height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int index = y * width + x;
                    double distToEdge = GetDistanceToDiamondEdge(x, y, width, height);

                    // Make a glowing border ring
                    if (IsInsideDiamond(x, y, width, height) && distToEdge < 0.08)
                    {
                        // Semi-transparent glowing cyan border
                        data[index] = new Color(0, 255, 255, 180);
                    }
                    else
                    {
                        data[index] = Color.Transparent;
                    }
                }
            }

            texture.SetData(data);
            return texture;
        }

        private static bool IsInsideDiamond(int x, int y, int w, int h)
        {
            double cx = w / 2.0;
            double cy = h / 2.0;
            double dx = Math.Abs(x - cx) / cx;
            double dy = Math.Abs(y - cy) / cy;
            return (dx + dy) <= 1.0;
        }

        private static double GetDistanceToDiamondEdge(int x, int y, int w, int h)
        {
            double cx = w / 2.0;
            double cy = h / 2.0;
            double dx = Math.Abs(x - cx) / cx;
            double dy = Math.Abs(y - cy) / cy;
            return 1.0 - (dx + dy);
        }
    }
}
