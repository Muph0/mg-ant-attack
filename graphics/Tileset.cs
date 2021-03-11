using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace zapoctak_antattack.graphics
{
    public class Tileset
    {
        public int TileWidth { get; private set; }
        public int TileHeight { get; private set; }
        public Texture2D Texture { get; private set; }

        public Tileset(Texture2D texture, int tileWidth, int tileHeight)
        {
            this.Texture = texture;
            this.TileWidth = tileWidth;
            this.TileHeight = tileHeight;
        }
        public static Tileset Load(ContentManager Content, string assetName, int tileWidth, int tileHeight)
        {
            Texture2D texture = Content.Load<Texture2D>(assetName);
            return new Tileset(texture, tileWidth, tileHeight);
        }

        public Rectangle GetTile(int id)
        {
            int W = Texture.Width / TileWidth;
            int H = Texture.Height / TileHeight;

            return new Rectangle((id % W) * TileWidth, (id / W) * TileHeight, TileWidth, TileHeight);
        }
    }

    static class SpriteBatchTilesetExtension
    {
        public static void DrawTile(this SpriteBatch sb, Tileset tileset, int tileId, Vector2 position, Color? color = null)
        {
            sb.Draw(tileset.Texture, position, tileset.GetTile(tileId), color ?? Color.White);
        }
        public static void DrawTile(this SpriteBatch sb, Tileset tileset, int tileId, Vector3 position, Level level, Color? color = null)
        {
            float depth = position.X / level.SizeX + position.Y / level.SizeY + position.Z / level.SizeZ;
            sb.Draw(tileset.Texture, LevelRenderer.WorldToScreen(position), tileset.GetTile(tileId), color ?? Color.White, 0f, Vector2.Zero, 1f, SpriteEffects.None, depth);
        }
    }
}
