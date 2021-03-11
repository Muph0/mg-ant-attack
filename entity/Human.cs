using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using zapoctak_antattack.graphics;
using zapoctak_antattack.utils;

namespace zapoctak_antattack.entity
{
    class Human : Entity
    {
        public bool Boy = true;
        public int Hitpoints = 20;

        public bool Alive => Hitpoints > 0;

        public bool TiedUp => Alive && AnimationState == EntityAnimationState.Lay;

        static byte[] walkCycle = { 1, 0, 2, 0 };
        int animStep = 0;

        bool hostage;
        bool rescued;

        public bool Hostage
        {
            get
            {
                return hostage;
            }
            set
            {
                hostage = true;
                AnimationState = EntityAnimationState.Lay;
            }
        }

        public Human(bool boy)
        {
            this.Boy = boy;
            this.MovementSpeed = 5;
        }

        public void Hurt(int amount)
        {
            Hitpoints -= amount;

            if (Hitpoints > 0)
                playPitchCorrected(GameWindow.sounds["ouch"]);
            else
            {
                Hitpoints = 0;
                playPitchCorrected(GameWindow.sounds["wasted"]);
            }
        }

        /// <summary>
        /// Alerts all ants around the player within 25 blocks
        /// </summary>
        /// <param name="pos"></param>
        private void alertAnts(Vector2 pos)
        {
            if (this.TilePosition.Z == 0)
                this.level.AlertAnts(pos, 25.0f);
        }

        /// <summary>
        /// Try to move the human in the given direction
        /// and roll the Walk/Climb animation if appropriate
        /// </summary>
        /// <param name="direction"></param>
        public override void StepIn(EntityDirection direction)
        {
            var target = (direction.ToVector3() + Position).Rounded();

            if (level.CheckRange(target) && (this.TilePosition.Z == 0 || level.IsSolid(this.TilePosition - Vector3.UnitZ)))
            {
                if (!this.level.IsSolid(target))
                {
                    base.StepIn(direction);
                    alertAnts(TilePosition.ToVector2());
                }

                else if (!level.IsSolid(target + Vector3.UnitZ))
                {
                    this.AnimationState = EntityAnimationState.Climb;
                    animStep = 0;
                    base.StepIn(direction);
                    alertAnts(TilePosition.ToVector2());
                }
            }
        }
        public override void Update(GameTime gameTime)
        {
            if ((hostage && !rescued) || !Alive)
                AnimationState = EntityAnimationState.Lay;

            if (this.AnimationState != EntityAnimationState.Idle && AnimationState != EntityAnimationState.Lay)
                animStep++;
            else
                animStep = 0;

            if (this.AnimationState == EntityAnimationState.Walk && animStep % 20 == 2)
            {
                GameWindow.sounds["step"].Play();
            }
            if (this.AnimationState == EntityAnimationState.Climb && animStep == 1)
            {
                playPitchCorrected(GameWindow.sounds["jump"]);
            }

            base.Update(gameTime);

            if (this.AnimationState == EntityAnimationState.Idle && this.TilePosition.Z > 0 && !level.IsSolid(this.TilePosition - Vector3.UnitZ))
            {
                AnimationState = EntityAnimationState.Fall;
                base.StepIn(EntityDirection.NegativeZ);
            }
        }

        /// <summary>
        /// Test if the player entity is on one of the neighboring blocks.
        /// </summary>
        /// <returns>True if the check is succesful.</returns>
        private bool PlayerInReach()
        {
            Vector2 hostage = new Vector2(Position.X, Position.Y);
            Vector2 player = new Vector2(level.Player.Position.X, level.Player.Position.Y);

            return (player - hostage).Length() <= 1.001f && Math.Abs(Position.Z - level.Player.Position.Z) <= 1.001f;
        }

        public override void Decide(GameTime gameTime)
        {
            if (hostage && rescued && !PlayerInReach())
            {
                var dir = FollowDirection(level.Player);
                StepIn(dir);
            }
        }

        /// <summary>
        /// Plays the given sound effect, shifted up if
        /// the character is female.
        /// </summary>
        /// <param name="sound"></param>
        void playPitchCorrected(SoundEffect sound)
        {
            if (Boy)
                sound.Play();
            else
                sound.Play(1f, 0.5f, 0f);
        }

        /// <summary>
        /// Decide which tile to show with regard to the current animation.
        /// </summary>
        /// <returns>The decided tile.</returns>
        int decideTile()
        {
            int tile = Boy ? 16 : 32;

            if (AnimationState != EntityAnimationState.Lay &&
                (this.Direction == EntityDirection.NegativeX || this.Direction == EntityDirection.PositiveY))
                tile += 5;

            switch (this.AnimationState)
            {
                case EntityAnimationState.Idle: return tile + 0;
                case EntityAnimationState.Walk: return tile + walkCycle[(animStep / 10) % walkCycle.Length];
                case EntityAnimationState.Climb: return tile + (animProgress < 0.25 ? 3 : 2);
                case EntityAnimationState.Fall: return tile + 4;
                case EntityAnimationState.Lay: return tile + 10 + (int)Direction;
                default: return 3;
            }
        }

        internal void Rescue()
        {
            rescued = true;
            this.AnimationState = EntityAnimationState.Idle;
        }

        public override void Draw(GameTime gameTime, SpriteBatch sb)
        {
            int tileid = decideTile();

            sb.DrawTile(GameWindow.tiles, tileid, LevelRenderer.WorldToScreen(this.Position));
        }
    }
}
