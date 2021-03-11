using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using zapoctak_antattack.graphics;
using zapoctak_antattack.utils;

namespace zapoctak_antattack.entity
{
    class Cursor : Entity
    {
        public Cursor()
            :base()
        {
            MovementSpeed = 5f;
        }

        public override void Draw(GameTime gameTime, SpriteBatch sb)
        {
            var pos = LevelRenderer.WorldToScreen(this.Position);
            sb.DrawTile(GameWindow.tiles, 2, pos);
        }

        internal Matrix CreateCenterViewMat()
        {
            return Matrix.CreateTranslation(new Vector3(-LevelRenderer.WorldToScreen(this.Position), 0f));
        }
    }
}
