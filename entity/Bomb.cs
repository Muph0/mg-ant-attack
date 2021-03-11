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
    class Bomb : Entity
    {
        int flyDistance;
        bool exploded = false;
        public Bomb(int flyDistance)
        {
            this.flyDistance = flyDistance;
            MovementSpeed = 7f;
            IsSolid = false;
        }

        /// <summary>
        /// Enter the explosion animation, play the sound
        /// and hurt the entities
        /// </summary>
        /// <param name="gameTime"></param>
        public void Explode(GameTime gameTime)
        {
            AnimationState = EntityAnimationState.Explode;
            GameWindow.sounds["boom"].Play();
            exploded = true;
            MovementSpeed = 1f;
            level.AlertAnts(Position.ToVector2(), 50f);

            bool goodShot = false;

            var entities = level.Entities.Where(e => (e.Position - Position).Length() < 10).ToList();
            foreach (Entity e in entities)
            {
                var dist = (e.Position - Position).Length();

                if (e is Ant ant)
                {
                    ant.Paralyze(700);

                    if (dist < 2.5)
                    {
                        goodShot = true;
                        ant.Kill();
                    }
                }
                else if (e is Human h)
                {
                    if ((dist < 4))
                        h.Hurt(4 - (int)dist);
                }
            }

            if (goodShot)
                level.game.ShowMessage(gameTime, "GOOD SHOT!", 2.5, false, Color.Red, Color.Transparent);
        }

        public override void Decide(GameTime gameTime)
        {
            if (this.TilePosition.Z > 0 && !level.IsSolid(this.TilePosition - Vector3.UnitZ))
            {
                AnimationState = EntityAnimationState.Fall;
                base.StepIn(EntityDirection.NegativeZ);
            }
            else
            {
                var target = (Direction.ToVector3() + Position).Rounded();
                if (level.IsSolid(target))
                    flyDistance = 0;

                if (flyDistance > 0)
                {
                    flyDistance--;
                    StepIn(Direction);
                }
                else
                {
                    if (!exploded)
                        Explode(gameTime);
                    else
                        Despawn();
                }
            }
        }

        public override void Draw(GameTime gameTime, SpriteBatch sb)
        {
            var tileset = GameWindow.tiles;
            if (AnimationState != EntityAnimationState.Explode)
                sb.Draw(tileset.Texture, LevelRenderer.WorldToScreen(Position), tileset.GetTile(56), Color.White);
            else
                sb.Draw(tileset.Texture, LevelRenderer.WorldToScreen(Position), tileset.GetTile(57 + (int)(animProgress * 4)), Color.White);
        }
    }
}
