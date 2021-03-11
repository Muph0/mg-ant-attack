using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using zapoctak_antattack.utils;

namespace zapoctak_antattack.entity
{
    /// <summary>
    /// Represents a direction which an entity is facing
    /// </summary>
    enum EntityDirection
    {
        PositiveX = 0, NegativeX, PositiveY, NegativeY, PositiveZ, NegativeZ
    }
    static class EntityDirectionExtension
    {
        /// <summary>
        /// Convert the direction to unit vector in that direction.
        /// </summary>
        /// <param name="dir">The direction.</param>
        /// <returns>The unit vector.</returns>
        public static Vector3 ToVector3(this EntityDirection dir)
        {
            switch (dir)
            {
                case EntityDirection.PositiveX: return Vector3.UnitX;
                case EntityDirection.NegativeX: return -Vector3.UnitX;
                case EntityDirection.PositiveY: return Vector3.UnitY;
                case EntityDirection.NegativeY: return -Vector3.UnitY;
                case EntityDirection.PositiveZ: return Vector3.UnitZ;
                case EntityDirection.NegativeZ: return -Vector3.UnitZ;
            }

            throw new ArgumentException();
        }
    }

    /// <summary>
    /// Represents a current entity animation state
    /// </summary>
    enum EntityAnimationState
    {
        Idle, Walk, Fall, Climb, Lay, Dash, Bite, Explode
    }

    /// <summary>
    /// Represents a non-tile object in the level.
    /// </summary>
    abstract class Entity
    {
        public Vector3 Position { get; set; }
        public EntityDirection Direction;

        /// <summary>
        /// In tiles per second.
        /// </summary>
        public float MovementSpeed { get; set; }
        /// <summary>
        /// Used in Level.IsSolid(...)
        /// </summary>
        public bool IsSolid { get; protected set; }

        public EntityAnimationState AnimationState { get; protected set; }
        protected float animProgress;

        protected Level level;
        int tileEntityIndex;
        public Vector3 TilePosition => level.DeindexTile(tileEntityIndex);
        public Vector3 TileInFront => (Direction.ToVector3() + Position).Rounded();
        public bool DecisionFrame => AnimationState == EntityAnimationState.Idle;

        public Entity()
        {
            MovementSpeed = 1f;
            IsSolid = true;
        }

        /// <summary>
        /// Make this entity appear in the level at given position.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="pos">Position to spawn at.</param>
        public void Spawn(Level level, Vector3 pos)
        {
            Spawn(level, (int)pos.X, (int)pos.Y, (int)pos.Z);
        }

        /// <summary>
        /// Make this entity appear in the level at given position.
        /// </summary>
        /// <param name="level">The level.</param>
        /// <param name="posX">X component of the position.</param>
        /// <param name="posY">Y component of the position.</param>
        /// <param name="posZ">Z component of the position.</param>
        public void Spawn(Level level, int posX, int posY, int posZ)
        {
            this.level = level;

            this.Position = new Vector3(posX, posY, posZ);
            tileEntityIndex = level.IndexTile(posX, posY, posZ);
            if (level.TileEntities[tileEntityIndex] == null)
                level.TileEntities[tileEntityIndex] = this;
            level.Entities.Add(this);
        }
        /// <summary>
        /// Remove this entity from the level.
        /// </summary>
        public void Despawn()
        {
            this.level.Entities.Remove(this);
            level.TileEntities[tileEntityIndex] = null;
        }

        /// <summary>
        /// Calculates the direction to head in to follow the given entity.
        /// </summary>
        /// <param name="entity">The entity to follow.</param>
        /// <returns></returns>
        public virtual EntityDirection FollowDirection(Entity entity)
        {
            return FollowDirection(((entity.Position + entity.TilePosition) / 2f).ToVector2());
        }
        public virtual EntityDirection FollowDirection(Vector2 pos)
        {
            float minDist = float.MaxValue;
            var minDirection = EntityDirection.PositiveX;
            for (EntityDirection d = EntityDirection.PositiveX; d <= EntityDirection.NegativeY; d++)
            {
                var delta = pos - (this.Position + d.ToVector3()).ToVector2();
                if (delta.LengthSquared() < minDist)
                {
                    minDist = delta.LengthSquared();
                    minDirection = d;
                }
            }

            return minDirection;
        }

        public virtual List<Entity> EntitiesInReach()
        {
            var inReach = new List<Entity>();
            for (EntityDirection dir = EntityDirection.PositiveX; dir <= EntityDirection.NegativeY; dir++)
            {
                var e = level.GetEntityAt((this.TilePosition + dir.ToVector3()).Rounded());
                if (e != null)
                    inReach.Add(e);
            }

            return inReach;
        }

        /// <summary>
        /// Starts a walking animation in the given direction and moves the entity through the level.
        /// </summary>
        /// <param name="direction">The direction to walk in.</param>
        public virtual void StepIn(EntityDirection direction)
        {
            if (this.AnimationState == EntityAnimationState.Idle)
                this.AnimationState = EntityAnimationState.Walk;

            if (direction >= EntityDirection.PositiveX && direction <= EntityDirection.NegativeY)
                this.Direction = direction;

            if (level.TileEntities[tileEntityIndex] == this)
                level.TileEntities[tileEntityIndex] = null;

            if (AnimationState == EntityAnimationState.Climb)
                tileEntityIndex = level.IndexTile((this.Position + this.Direction.ToVector3() + Vector3.UnitZ).Rounded());
            else if (AnimationState == EntityAnimationState.Fall)
                tileEntityIndex = level.IndexTile((this.Position - Vector3.UnitZ).Rounded());
            else
                tileEntityIndex = level.IndexTile((this.Position + this.Direction.ToVector3()).Rounded());

            if (level.TileEntities[tileEntityIndex] == null)
                level.TileEntities[tileEntityIndex] = this;
        }

        /// <summary>
        /// Entity decision method. Is called when the entity is idle.
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void Decide(GameTime gameTime)
        {

        }

        /// <summary>
        /// Main update method. Calculates entity position during an animation.
        /// </summary>
        /// <param name="gameTime"></param>
        public virtual void Update(GameTime gameTime)
        {
            Vector3 delta = TilePosition - Position;
            if (delta.Length() > 0.01f)
            {
                // movement animations
                delta.Normalize();
                switch (AnimationState)
                {
                    case EntityAnimationState.Lay:
                    case EntityAnimationState.Idle:
                        break;
                    case EntityAnimationState.Walk:
                    case EntityAnimationState.Fall:
                        Position += delta * (float)gameTime.ElapsedGameTime.TotalSeconds * MovementSpeed;
                        animProgress += (float)(gameTime.ElapsedGameTime.TotalSeconds * MovementSpeed);
                        break;
                    case EntityAnimationState.Climb:
                        if (animProgress > 0.25)
                            Position += delta * 1.41421f * (float)(MovementSpeed / 2 * gameTime.ElapsedGameTime.TotalSeconds * 4f / 3f);
                        animProgress += (float)(gameTime.ElapsedGameTime.TotalSeconds * MovementSpeed / 2);
                        break;
                    case EntityAnimationState.Dash:
                        if (animProgress < 0.5f)
                            Position += delta * (float)gameTime.ElapsedGameTime.TotalSeconds * MovementSpeed * 2;
                        animProgress += (float)(gameTime.ElapsedGameTime.TotalSeconds * MovementSpeed);
                        break;
                    default:
                        animProgress += (float)(gameTime.ElapsedGameTime.TotalSeconds * MovementSpeed);
                        break;
                }
            }
            else
            {
                // other animations
                switch (AnimationState)
                {
                    case EntityAnimationState.Lay:
                    case EntityAnimationState.Idle:
                        break;
                    default:
                        animProgress += (float)(gameTime.ElapsedGameTime.TotalSeconds * MovementSpeed);
                        break;
                }
            }

            if (animProgress >= 1f)
            {
                this.AnimationState = EntityAnimationState.Idle;
                this.Position = TilePosition;
                this.animProgress = 0f;
            }

            if (this.AnimationState == EntityAnimationState.Idle)
            {
                if (tileEntityIndex >= 0 && level.TileEntities[tileEntityIndex] == null)
                    level.TileEntities[tileEntityIndex] = this;
                Decide(gameTime);
            }
        }

        public abstract void Draw(GameTime gameTime, SpriteBatch sb);
    }
}
