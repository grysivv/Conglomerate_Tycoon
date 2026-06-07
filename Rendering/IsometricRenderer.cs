using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TycoonGame.Core;
using Color = Microsoft.Xna.Framework.Color;
using Rectangle = Microsoft.Xna.Framework.Rectangle;

namespace TycoonGame.Rendering
{
    public class IsometricRenderer
    {
        private Dictionary<TileType, Texture2D> buildingTextures;
        private Texture2D grassTexture;
        private Texture2D roadTexture;
        private Texture2D selectorTexture;
        private Texture2D warningPowerTexture;
        private Texture2D warningRoadTexture;

        public float CameraX { get; set; }
        public float CameraY { get; set; }
        public float Zoom { get; set; }

        private Texture2D pixelTexture;

        public IsometricRenderer()
        {
            buildingTextures = new Dictionary<TileType, Texture2D>();
            CameraX = 0f; // Center camera on the new map initially
            CameraY = GameEngine.MapSize * 32f;
            Zoom = 1.0f;
        }

        public void LoadContent(GraphicsDevice device)
        {
            // Generate standard ground textures
            grassTexture = SpriteGenerator.CreateGrassTexture(device, 128, 64);
            roadTexture = SpriteGenerator.CreateRoadTexture(device, 128, 64);
            selectorTexture = SpriteGenerator.CreateSelectorTexture(device, 128, 64);

            // Generate building textures (128x128)
            buildingTextures[TileType.Office] = SpriteGenerator.CreateBuildingTexture(device, 128, 128, TileType.Office);
            buildingTextures[TileType.Factory] = SpriteGenerator.CreateBuildingTexture(device, 128, 128, TileType.Factory);
            buildingTextures[TileType.Retail] = SpriteGenerator.CreateBuildingTexture(device, 128, 128, TileType.Retail);
            buildingTextures[TileType.PowerPlant] = SpriteGenerator.CreateBuildingTexture(device, 128, 128, TileType.PowerPlant);
            buildingTextures[TileType.Apartment] = SpriteGenerator.CreateBuildingTexture(device, 128, 128, TileType.Apartment);
            buildingTextures[TileType.University] = SpriteGenerator.CreateBuildingTexture(device, 128, 128, TileType.University);

            // Generate warning badges (16x16 icons)
            warningPowerTexture = CreateWarningBadge(device, Color.Red); // Power outage badge
            warningRoadTexture = CreateWarningBadge(device, Color.Orange); // Road disconnected badge

            // Create solid 1x1 color pixel texture for mini-map rendering
            pixelTexture = new Texture2D(device, 1, 1);
            pixelTexture.SetData(new Color[] { Color.White });
        }

        private Texture2D CreateWarningBadge(GraphicsDevice device, Color color)
        {
            Texture2D texture = new Texture2D(device, 16, 16);
            Color[] data = new Color[16 * 16];

            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int index = y * 16 + x;
                    
                    // Draw a simple exclamation mark inside a circular badge
                    double rx = x - 8.0;
                    double ry = y - 8.0;
                    double radiusSq = rx * rx + ry * ry;

                    if (radiusSq < 64.0)
                    {
                        if (radiusSq < 50.0 && ((x == 8 && y >= 3 && y <= 9) || (x == 8 && y == 12)))
                        {
                            data[index] = Color.White; // Exclamation mark
                        }
                        else
                        {
                            data[index] = color; // Badge body
                        }
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

        public Tuple<int, int> ScreenToTile(int mouseX, int mouseY, int viewportWidth, int viewportHeight)
        {
            // Convert screen coordinates back to world space
            double worldX = (mouseX - viewportWidth / 2.0) / Zoom + CameraX;
            double worldY = (mouseY - viewportHeight / 2.0) / Zoom + CameraY;

            // Invert the isometric transform
            // worldX = (col - row) * 64.0
            // worldY = (col + row) * 32.0
            double col = ((worldX / 64.0) + (worldY / 32.0)) / 2.0;
            double row = ((worldY / 32.0) - (worldX / 64.0)) / 2.0;

            int cellX = (int)Math.Floor(col + 0.5);
            int cellY = (int)Math.Floor(row + 0.5);

            return new Tuple<int, int>(cellX, cellY);
        }

        public Vector2 TileToScreen(int col, int row, int viewportWidth, int viewportHeight)
        {
            double worldX = (col - row) * 64.0;
            double worldY = (col + row) * 32.0;

            float screenX = (float)((worldX - CameraX) * Zoom + viewportWidth / 2.0);
            float screenY = (float)((worldY - CameraY) * Zoom + viewportHeight / 2.0);

            return new Vector2(screenX, screenY);
        }

        public bool IsTileVisible(int col, int row, int viewportWidth, int viewportHeight)
        {
            double worldX = (col - row) * 64.0;
            double worldY = (col + row) * 32.0;

            // Size bounds for largest building (128x128)
            double minX = (worldX - 64 - CameraX) * Zoom + viewportWidth / 2.0;
            double maxX = (worldX + 64 - CameraX) * Zoom + viewportWidth / 2.0;
            double minY = (worldY - 96 - CameraY) * Zoom + viewportHeight / 2.0;
            double maxY = (worldY + 32 - CameraY) * Zoom + viewportHeight / 2.0;

            return !(maxX < 0 || minX > viewportWidth || maxY < 0 || minY > viewportHeight);
        }

        public void Draw(SpriteBatch spriteBatch, GameEngine engine, int viewportWidth, int viewportHeight, Tuple<int, int>? hoveredTile)
        {
            // Depth-sorted nested loops (painter's algorithm from back to front)
            for (int y = 0; y < GameEngine.MapSize; y++)
            {
                for (int x = 0; x < GameEngine.MapSize; x++)
                {
                    if (!IsTileVisible(x, y, viewportWidth, viewportHeight))
                    {
                        continue; // Cull off-screen tiles
                    }

                    Tile tile = engine.Grid[x, y];
                    Vector2 screenPos = TileToScreen(x, y, viewportWidth, viewportHeight);

                    // 1. Draw base grass tile first
                    if (tile.Type == TileType.Grass || tile.Type == TileType.Road)
                    {
                        Texture2D tex = (tile.Type == TileType.Road) ? roadTexture : grassTexture;
                        spriteBatch.Draw(
                            tex, 
                            screenPos, 
                            null, 
                            Color.White, 
                            0f, 
                            new Vector2(64, 32), // Draw origin set to center of diamond
                            Zoom, 
                            SpriteEffects.None, 
                            0f);
                    }
                    else
                    {
                        // Draw base grass under any buildings so transparent edges show grass
                        spriteBatch.Draw(
                            grassTexture, 
                            screenPos, 
                            null, 
                            Color.White, 
                            0f, 
                            new Vector2(64, 32), 
                            Zoom, 
                            SpriteEffects.None, 
                            0f);

                        // Draw building sprite (128x128)
                        if (buildingTextures.TryGetValue(tile.Type, out Texture2D? bTex))
                        {
                            spriteBatch.Draw(
                                bTex, 
                                screenPos, 
                                null, 
                                Color.White, 
                                0f, 
                                new Vector2(64, 96), // Aligns bottom half of building with ground
                                Zoom, 
                                SpriteEffects.None, 
                                0f);
                        }
                    }

                    // 2. Draw active building warnings (unpowered, no roads)
                    if (tile.Type != TileType.Grass && tile.Type != TileType.Road)
                    {
                        float warningOffset = -48f * Zoom; // Shift badge up onto building facade
                        
                        if (!tile.IsPowered)
                        {
                            Vector2 badgePos = new Vector2(screenPos.X - 18 * Zoom, screenPos.Y + warningOffset);
                            spriteBatch.Draw(
                                warningPowerTexture, 
                                badgePos, 
                                null, 
                                Color.White, 
                                0f, 
                                Vector2.Zero, 
                                Zoom, 
                                SpriteEffects.None, 
                                0f);
                        }

                        if (!tile.HasRoadAccess)
                        {
                            Vector2 badgePos = new Vector2(screenPos.X + 2 * Zoom, screenPos.Y + warningOffset);
                            spriteBatch.Draw(
                                warningRoadTexture, 
                                badgePos, 
                                null, 
                                Color.White, 
                                0f, 
                                Vector2.Zero, 
                                Zoom, 
                                SpriteEffects.None, 
                                0f);
                        }
                    }
                }
            }

            // 3. Draw Hover Selector Diamond
            if (hoveredTile != null)
            {
                int hx = hoveredTile.Item1;
                int hy = hoveredTile.Item2;

                if (hx >= 0 && hx < GameEngine.MapSize && hy >= 0 && hy < GameEngine.MapSize)
                {
                    Vector2 hoverPos = TileToScreen(hx, hy, viewportWidth, viewportHeight);
                    spriteBatch.Draw(
                        selectorTexture, 
                        hoverPos, 
                        null, 
                        Color.White, 
                        0f, 
                        new Vector2(64, 32), 
                        Zoom, 
                        SpriteEffects.None, 
                        0f);
                }
            }

            // 4. Draw Mini Map in the bottom right corner
            DrawMiniMap(spriteBatch, engine, viewportWidth, viewportHeight);
        }

        public void DrawMiniMap(SpriteBatch spriteBatch, GameEngine engine, int viewportWidth, int viewportHeight)
        {
            int mapSizePx = GameEngine.MapSize; // 120 pixels for MapSize 120
            int margin = 10;
            int mapX = viewportWidth - mapSizePx - margin;
            int mapY = viewportHeight - mapSizePx - margin;

            // Draw border (2px thickness)
            spriteBatch.Draw(pixelTexture, new Rectangle(mapX - 2, mapY - 2, mapSizePx + 4, mapSizePx + 4), new Color(48, 56, 70));
            // Draw background
            spriteBatch.Draw(pixelTexture, new Rectangle(mapX, mapY, mapSizePx, mapSizePx), new Color(20, 24, 30));

            // Draw top-down grid representation
            for (int y = 0; y < GameEngine.MapSize; y++)
            {
                for (int x = 0; x < GameEngine.MapSize; x++)
                {
                    Tile tile = engine.Grid[x, y];
                    Color c = tile.Type switch
                    {
                        TileType.Grass => new Color(34, 139, 34),
                        TileType.Road => new Color(105, 105, 105),
                        TileType.Office => new Color(0, 191, 255),
                        TileType.Factory => new Color(210, 105, 30),
                        TileType.Retail => new Color(220, 20, 60),
                        TileType.PowerPlant => new Color(255, 215, 0),
                        TileType.Apartment => new Color(138, 43, 226),
                        TileType.University => new Color(255, 20, 147),
                        _ => new Color(34, 139, 34)
                    };

                    spriteBatch.Draw(pixelTexture, new Rectangle(mapX + x, mapY + y, 1, 1), c);
                }
            }

            // Draw camera visible area rectangle on mini-map
            Tuple<int, int> centerTile = ScreenToTile(viewportWidth / 2, viewportHeight / 2, viewportWidth, viewportHeight);
            int cx = Math.Clamp(centerTile.Item1, 0, GameEngine.MapSize - 1);
            int cy = Math.Clamp(centerTile.Item2, 0, GameEngine.MapSize - 1);

            int viewSize = (int)(16 / Zoom); // width of visible tiles region (scales with zoom)
            if (viewSize < 4) viewSize = 4;
            int rx = cx - viewSize / 2;
            int ry = cy - viewSize / 2;

            Rectangle viewRect = new Rectangle(mapX + rx, mapY + ry, viewSize, viewSize);
            DrawHollowRect(spriteBatch, viewRect, Color.White);
        }

        private void DrawHollowRect(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, rect.Width, 1), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y + rect.Height - 1, rect.Width, 1), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X, rect.Y, 1, rect.Height), color);
            spriteBatch.Draw(pixelTexture, new Rectangle(rect.X + rect.Width - 1, rect.Y, 1, rect.Height), color);
        }
    }
}
