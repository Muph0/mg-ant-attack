using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using zapoctak_antattack.entity;
using zapoctak_antattack.utils;

namespace zapoctak_antattack.graphics
{
    class LevelRenderer
    {
        readonly Level level;
        readonly SpriteBatch sb;

        public LevelRenderer(Level level, SpriteBatch sb)
        {
            this.level = level;
            this.sb = sb;
        }

        private bool isTileVisible(int x, int y, int z)
        {
            if (x == level.SizeX - 1 || y == level.SizeY - 1 || z == level.SizeZ - 1)
                return true;

            bool visible = !level[x + 1, y + 1, z + 1];
            visible = visible && !(level[x + 1, y, z] && level[x, y + 1, z] && level[x, y, z + 1]);

            return visible;
        }

        public static Vector2 WorldToScreen(Vector3 vector)
        {
            return WorldToScreen(vector.X, vector.Y, vector.Z);
        }
        public static Vector2 WorldToScreen(float x, float y, float z)
        {
            return new Vector2(
                    8f * (x - y),
                    4f * (x + y) - 8f * z
                    ).Rounded();
        }

        public void Draw(GameTime gameTime, Tileset tileset, Vector3 camera)
        {
            const int u_range = 10;
            const int v_range = 31;

            for (int w = 0; w < level.SizeZ; w++)
                for (int v = -v_range; v < v_range; v++)
                    for (int u = -u_range; u < u_range; u++)
                    {
                        int x = (v + 0) / 2 + u + w + (int)camera.X;
                        int y = (v + 1) / 2 - u + w + (int)camera.Y;
                        int z = w;

                        if (level.CheckRange(x, y, z))
                        {
                            if (level[x, y, z] && isTileVisible(x, y, z))
                            {
                                Vector2 pos = LevelRenderer.WorldToScreen(x, y, z);
                                int light = (int)(255 + (z - 8) * 10f);

                                if (x == 0 && y == 0 && z == 1)
                                    sb.DrawTile(tileset, 0, pos, Color.FromNonPremultiplied(light, light / 2, light / 2, 255));
                                else
                                    sb.DrawTile(tileset, 0, pos, Color.FromNonPremultiplied(light, light, light, 255));
                            }

                            Entity tileEntity = level.TileEntities[level.IndexTile(x, y, z)];
                            if (tileEntity != null)
                                tileEntity.Draw(gameTime, sb);
                        }
                    }

        }
    }
}
