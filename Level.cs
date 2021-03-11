using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using zapoctak_antattack.entity;
using zapoctak_antattack.utils;

namespace zapoctak_antattack
{
    class Level
    {
        public int SizeX { get; private set; }
        public int SizeY { get; private set; }
        public int SizeZ => 8;

        Rectangle castleBounds;
        Vector3 spawnPoint;
        List<Vector3> hostagePoints;
        public int Round { get; private set; }
        public int RoundCount => hostagePoints.Count;

        byte[] rawData;
        public Entity[] TileEntities;
        public ListSet<Entity> Entities;

        public Human Player { get; private set; }
        public Human Hostage { get; private set; }

        public GameWindow game { get; private set; }
        public Level(GameWindow game, int sizeX, int sizeY)
        {
            this.game = game;
            this.SizeX = sizeX;
            this.SizeY = sizeY;
            rawData = new byte[sizeX * sizeY];
            TileEntities = new Entity[sizeX * sizeY * SizeZ];
            Entities = new ListSet<Entity>();
        }

        public static Level Load(GameWindow game, string filename)
        {
            int lineNumber = 0;
            Action<bool, Exception> assert = (b, inner) =>
            {
                if (!b) throw new FormatException($"Unsupported level file format, file {filename}:{lineNumber}.", inner);
            };

            Vector3? start = null;
            Rectangle? castle = null;
            Level level = null;
            var hostagePoints = new List<Vector3>();

            using (var sr = new StreamReader(filename))
            {
                bool mapMode = false;

                while (!sr.EndOfStream)
                {
                    if (!mapMode)
                    {
                        string[] line = sr.ReadLine().Split(" \t".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
                        lineNumber++;

                        if (line.Length > 0)
                            switch (line[0])
                            {
                                case "#":
                                    break;
                                case "size:":
                                    assert(line.Length == 3, null);
                                    level = new Level(game, int.Parse(line[1]), int.Parse(line[2]));
                                    break;
                                case "start:":
                                    assert(line.Length == 4, null);
                                    start = new Vector3(int.Parse(line[1]), int.Parse(line[2]), int.Parse(line[3]));
                                    break;
                                case "castle:":
                                    assert(line.Length == 5, null);
                                    castle = new Rectangle(int.Parse(line[1]), int.Parse(line[2]), int.Parse(line[3]), int.Parse(line[4]));
                                    break;
                                case "hostage:":
                                    assert(line.Length == 4, null);
                                    hostagePoints.Add(new Vector3(int.Parse(line[1]), int.Parse(line[2]), int.Parse(line[3])));
                                    break;
                                case "map:":
                                    assert(line.Length == 1, null);
                                    assert(level != null, new Exception("Level size must be specified before its contents."));
                                    mapMode = true;
                                    break;
                                default:
                                    throw new Exception($"Unknown key '{line[0]}' in map file '{filename}'.");
                            }
                    }
                    else
                    {
                        for (int y = 0; y < level.SizeY; y++)
                        {
                            string line = sr.ReadLine();
                            lineNumber++;

                            assert(line.Length == level.SizeX * 2, new Exception("Map line lenght must be twice the level X size."));
                            for (int x = 0; x < level.SizeX; x++)
                            {
                                string tile = line.Substring(2 * x, 2).ToLower();

                                if (((tile[0] >= 'a' && tile[0] <= 'f') ||
                                    (tile[0] >= '0' && tile[0] <= '9')) &&
                                    ((tile[1] >= 'a' && tile[1] <= 'f') ||
                                    (tile[1] >= '0' && tile[1] <= '9'))
                                    )
                                {
                                    int mask = Convert.ToInt32(tile, 16);
                                    level.rawData[x + y * level.SizeX] = (byte)mask;
                                }
                                else
                                {
                                    int height = int.Parse(tile.Substring(1));
                                    byte data = (byte)(~(0xffff << height));
                                    level.rawData[x + y * level.SizeX] = data;

                                    if (tile[0] == ' ')
                                    {

                                    }
                                    else
                                    {
                                        throw new Exception("Unsupported tile format.");
                                    }
                                }
                            }
                        }

                        mapMode = false;
                    }

                }
            }

            assert(start != null, new Exception("Spawn point not specified."));
            level.spawnPoint = (Vector3)start;

            assert(hostagePoints.Count > 0, new Exception("No hostage points specified"));
            level.hostagePoints = hostagePoints;

            assert(castle != null, new Exception("Castle bounds not specified."));
            level.castleBounds = (Rectangle)castle;

            return level;
        }

        internal Entity GetEntityAt(Vector3 pos)
        {
            return TileEntities[IndexTile(pos)];
        }

        /// <summary>
        /// Checks if the given position lies within the level.
        /// </summary>
        /// <param name="pos">Vector to check</param>
        /// <returns>True if the check is succesfull.</returns>
        public bool CheckRange(Vector3 pos)
        {
            return CheckRange((int)pos.X, (int)pos.Y, (int)pos.Z);
        }
        /// <summary>
        /// Checks if the given position lies within the level.
        /// </summary>
        /// <param name="x">X component of the position.</param>
        /// <param name="y">Y component of the position.</param>
        /// <param name="z">Z component of the position.</param>
        /// <returns>True if the check is succesfull.</returns>
        public bool CheckRange(int x, int y, int z)
        {
            return !(x < 0 || x >= SizeX || y < 0 || y >= SizeY || z < 0 || z >= SizeZ);

        }

        /// <summary>
        /// Access the undelying block bitmap with a vector.
        /// </summary>
        /// <param name="pos">Position of the block.</param>
        /// <returns>True if there is a block at the given position.</returns>
        public bool this[Vector3 pos]
        {
            get
            {
                return this[(int)pos.X, (int)pos.Y, (int)pos.Z];
            }
            set
            {
                this[(int)pos.X, (int)pos.Y, (int)pos.Z] = value;
            }
        }

        /// <summary>
        /// Constructs a Vector3 from a tile index.
        /// </summary>
        /// <param name="idx">The tile index.</param>
        /// <returns>The constructed vector.</returns>
        internal Vector3 DeindexTile(int idx)
        {
            return new Vector3(idx % SizeX, (idx / SizeX) % SizeY, idx / SizeX / SizeY);
        }

        /// <summary>
        /// Access the undelying block bitmap with a set of coordinates.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns>True if there is a block at the given position.</returns>
        /// <returns></returns>
        public bool this[int x, int y, int z]
        {
            get
            {
                if (!CheckRange(x, y, z)) throw new ArgumentOutOfRangeException();
                return ((rawData[x + SizeX * y] >> z) & 1) == 1;
            }
            set
            {
                if (!CheckRange(x, y, z)) throw new ArgumentOutOfRangeException();
                if (value)
                    rawData[x + SizeX * y] |= (byte)(1 << z);
                else
                    rawData[x + SizeX * y] &= (byte)((~1) << z);
            }
        }

        /// <summary>
        /// Checks if the position contains a solid obstacle.
        /// </summary>
        /// <param name="pos">The position to check.</param>
        /// <returns>True if there is a block or a solid entity.</returns>
        public bool IsSolid(Vector3 pos)
        {
            return IsSolid((int)pos.X, (int)pos.Y, (int)pos.Z);
        }
        /// <summary>
        /// Checks if the position contains a solid obstacle.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="z"></param>
        /// <returns>True if there is a block or a solid entity.</returns>
        public bool IsSolid(int x, int y, int z)
        {
            var entity = TileEntities[IndexTile(x, y, z)];
            return this[x, y, z] || (entity != null && entity.IsSolid);
        }

        /// <summary>
        /// Calculate tile index from a given Vector3
        /// </summary>
        /// <param name="pos">The vector</param>
        /// <returns>Tile index</returns>
        public int IndexTile(Vector3 pos)
        {
            var (x, y, z) = pos.ToTuple();
            return IndexTile(x, y, z);
        }
        /// <summary>
        /// Calculates tile index from a given set of coordinates
        /// </summary>
        /// <param name="posX"></param>
        /// <param name="posY"></param>
        /// <param name="posZ"></param>
        /// <returns></returns>
        public int IndexTile(int posX, int posY, int posZ)
        {
            posX = posX < 0 ? 0 : (posX >= SizeX ? SizeX - 1 : posX);
            posY = posY < 0 ? 0 : (posY >= SizeY ? SizeY - 1 : posY);
            posZ = posZ < 0 ? 0 : (posZ >= SizeZ ? SizeZ - 1 : posZ);

            return posX + SizeX * (posY + SizeY * (posZ));
        }

        /// <summary>
        /// Clears entities, and respawns new ones for the given round.
        /// </summary>
        /// <param name="boy">Give true if player is boy.</param>
        /// <param name="round">The round.</param>
        public void RestartRound(bool boy, int round)
        {
            Entities.Clear();
            for (int i = 0; i < TileEntities.Length; i++)
                TileEntities[i] = null;

            this.Round = round;
            Player = SpawnPlayer(boy);
            Hostage = SpawnHostage(Round);

            var rnd = new Random(123 + round);
            var antCount = rnd.Next(round + 1, (round + 1) * 2);
            for (int i = 0; i < antCount; i++)
                SpawnAnt(rnd);
        }

        /// <summary>
        /// Creates and spawns a new player Human.
        /// </summary>
        /// <param name="boy">Give true if player is boy</param>
        /// <returns>The spawned player</returns>
        public Human SpawnPlayer(bool boy)
        {
            var p = new Human(boy);
            p.Spawn(this, spawnPoint);

            return p;
        }

        /// <summary>
        /// Creates and spawns a new hostage Human
        /// </summary>
        /// <param name="roundNumber">The according round.</param>
        /// <returns>The spawned hostage.</returns>
        public Human SpawnHostage(int roundNumber)
        {
            var h = new Human(!Player.Boy);
            h.Hostage = true;
            h.Spawn(this, hostagePoints[roundNumber]);

            return h;
        }

        /// <summary>
        /// Creates and spawns an ant at a random location.
        /// </summary>
        /// <param name="rnd">Random sequence to pick from.</param>
        /// <returns>The spawned ant.</returns>
        public Ant SpawnAnt(Random rnd)
        {
            var ant = new Ant();
            int posX = rnd.Next(castleBounds.X, castleBounds.X + castleBounds.Width);
            int posY = rnd.Next(castleBounds.Y, castleBounds.Y + castleBounds.Height);

            ant.Spawn(this, new Vector3(posX, posY, 0f));

            return ant;
        }

        /// <summary>
        /// Alerts all ants around a given point in given radius.
        /// </summary>
        /// <param name="pos">New ants' target.</param>
        /// <param name="radius">Radius from the target</param>
        public void AlertAnts(Vector2 pos, float radius)
        {
            if (castleBounds.Contains(pos))
                foreach (Entity e in Entities)
                {
                    if (e is Ant ant)
                    {
                        var delta = ant.TilePosition.ToVector2() - pos;

                        if (delta.Length() <= radius)
                            ant.Alert(pos);
                    }
                }
        }

        /// <summary>
        /// Main Update method. Updates all entities.
        /// </summary>
        /// <param name="gameTime"></param>
        internal void Update(GameTime gameTime)
        {
            // Copy the entities first, so they can despawn in their Update()
            var entities = Entities.Select(e => e).ToList();
            foreach (Entity e in entities)
            {
                e.Update(gameTime);
            }
        }
    }
}
