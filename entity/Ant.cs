using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using zapoctak_antattack.graphics;
using zapoctak_antattack.utils;

namespace zapoctak_antattack.entity
{
    /// <summary>
    /// Represents an ant entity
    /// </summary>
    class Ant : Entity
    {
        float distanceToTarget => target.HasValue ? (target.Value - Position.ToVector2()).Length() : float.PositiveInfinity;

        Vector2? target = null;
        Random rnd = new Random();

        public bool Paralyzed => paralyzed != 0;
        int paralyzed = 0;

        public Ant()
        {
            MovementSpeed = 4f;
        }

        public override void Update(GameTime gameTime)
        {
            if (paralyzed > 0)
                paralyzed--;

            base.Update(gameTime);
        }

        /// <summary>
        /// Play the ant death sound and despawn the ant immediately.
        /// </summary>
        internal void Kill()
        {
            GameWindow.sounds["wasted"].Play(0.4f, -1f, 0f);
            this.Despawn();
        }

        public override void Decide(GameTime gameTime)
        {
            if (Paralyzed) return;

            var humansInReach = EntitiesInReach().Where(e => e is Human human && human.Alive).ToList();
            if (humansInReach.Count() > 0)
            {
                var victim = rnd.Select(humansInReach) as Human;
                Bite(victim);
            }

            if (target != null)
            {
                var dir = FollowDirection((Vector2)target);

                if (distanceToTarget < 5f && rnd.Next(5) == 0 ||
                    level.IsSolid(Position + dir.ToVector3()))
                    dir = (EntityDirection)rnd.Next((int)EntityDirection.PositiveX, (int)EntityDirection.NegativeY + 1);

                if (!level.IsSolid(Position + dir.ToVector3()))
                {
                    StepIn(dir);
                    this.AnimationState = EntityAnimationState.Dash;
                }
            }
        }

        /// <summary>
        /// Enter the bite animation and hurt the human.
        /// </summary>
        /// <param name="victim"></param>
        public void Bite(Human victim)
        {
            this.AnimationState = EntityAnimationState.Bite;
            this.Direction = FollowDirection(victim);
            victim.Hurt(1);
        }

        public override void Draw(GameTime gameTime, SpriteBatch sb)
        {
            var tileset = GameWindow.tiles;
            int inStep = 4;
            if (animProgress > 0.5f)
            {
                inStep = 0;
            }
            sb.Draw(tileset.Texture, LevelRenderer.WorldToScreen(Position), tileset.GetTile(48 + (int)Direction + inStep), Color.White);
        }

        /// <summary>
        /// Set the new target of the ant
        /// </summary>
        /// <param name="pos">The new target</param>
        public void Alert(Vector2 pos)
        {
            target = pos;
        }

        /// <summary>
        /// Paralyze the ant, but only if it isn't paralysed even more.
        /// </summary>
        /// <param name="amount"></param>
        internal void Paralyze(int amount)
        {
            if (paralyzed >= 0 && amount > paralyzed)
                paralyzed = amount;
        }
    }
}
