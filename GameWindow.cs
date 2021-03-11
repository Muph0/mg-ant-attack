using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.ExceptionServices;
using zapoctak_antattack.entity;
using zapoctak_antattack.graphics;
using zapoctak_antattack.utils;

namespace zapoctak_antattack
{
    public enum InterfaceState
    {
        Ingame, Title, RoundWin, IngameMessage, ShowScore, Freeze, Over
    }

    /// <summary>
    /// This represents the core of the game
    /// </summary>
    public class GameWindow : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch sb;

        public static Tileset tiles;
        public static Dictionary<string, SoundEffect> sounds;
        List<SoundEffectInstance> playingSounds = new List<SoundEffectInstance>();
        RenderTarget2D buffer;
        Level level;
        LevelRenderer levelRenderer;

        Matrix viewMat;

        // Interface state
        InterfaceState ifstate = InterfaceState.Title;
        SortedList<double, InterfaceState> ifstateQueue = new SortedList<double, InterfaceState>();

        Human playerEntity => level.Player;
        Human hostageEntity => level.Hostage;
        UserInput input;

        public static SpriteFont font;
        public static Tileset fontTileset;

        // in-game state:
        bool debug = false;
        bool playerIsBoy;
        bool winFlag = false;
        int score;
        int rescued = 0;
        int ammo;
        float throwForce = 0;
        float RoundTime;

        // Messages
        Color messageForeground, messageBackground;
        bool messageFreeze = true;
        string ingameMessage;

        public GameWindow()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            float scale = 5f;
            this.Window.AllowUserResizing = false;
            graphics.PreferredBackBufferWidth = (int)(256 * scale);
            graphics.PreferredBackBufferHeight = (int)(192 * scale);

            input = new UserInput();
        }

        /// <summary>
        /// Initialize. Called once
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            input.Update();
        }

        /// <summary>
        /// Load game files. Called once
        /// </summary>
        protected override void LoadContent()
        {
            sb = new SpriteBatch(GraphicsDevice);

            tiles = Tileset.Load(Content, "ants-tiles", 16, 16);
            buffer = new RenderTarget2D(GraphicsDevice, 256, 192);

            level = Level.Load(this, "level.txt");
            levelRenderer = new LevelRenderer(level, sb);

            // Load sounds
            {
                sounds = new Dictionary<string, SoundEffect>();
                foreach (var name in new[] { "fanfara", "ouch", "step", "jump", "blip",
                    "rescue", "wasted", "over", "boom", "ant" })
                    sounds.Add(name, Content.Load<SoundEffect>(name));
            }

            // Load font
            {
                var glyphBounds = new List<Rectangle>();
                var cropping = new List<Rectangle>();
                var characters = new List<char>();
                var kernings = new List<Vector3>();

                fontTileset = new Tileset(tiles.Texture, 8, 8);

                // the font in the tileset begins at tile no. 256 with space (' ')
                // so we start creating the spritefont from there
                for (char c = ' '; c <= '~'; c++)
                {
                    glyphBounds.Add(fontTileset.GetTile(c - ' ' + 256));
                    cropping.Add(new Rectangle(0, 0, 8, 8));
                    characters.Add(c);
                    kernings.Add(new Vector3(0, 8, 0));
                }

                // !!! following line needs MonoGame >=3.7, because in older versions SpriteFont constructor is private
                font = new SpriteFont(fontTileset.Texture, glyphBounds, cropping, characters, fontTileset.TileHeight, 0f, kernings, '?');
            }

            level.SpawnPlayer(false);
        }

        /// <summary>
        /// Unload all non-ContentManager files
        /// </summary>
        protected override void UnloadContent()
        {
            // Manually dispose graphics memory
            buffer.Dispose();
        }

        /// <summary>
        /// Plays a sound effect, and registers it to the playing sounds, so it can be later stopped by StopAllSounds().
        /// </summary>
        /// <param name="sound">Sound to play</param>
        public void PlaySound(SoundEffectInstance sound)
        {
            sound.Play();
            playingSounds.Add(sound);
        }

        /// <summary>
        /// Stops all sounds started by PlaySound(...).
        /// </summary>
        public void StopAllSounds()
        {
            foreach (var sound in playingSounds)
                sound.Stop();
            playingSounds.Clear();
        }

        /// <summary>
        /// Shows a message over the gameplay.
        /// </summary>
        /// <param name="gameTime">GameTime object of the current frame.</param>
        /// <param name="message">Message to show.</param>
        /// <param name="duration">How long to show the message for.</param>
        /// <param name="freeze">If true, game will freeze while the message is on display.</param>
        /// <param name="exitState">Interface state to return to after duration time runs out.</param>
        public void ShowMessage(GameTime gameTime, string message, double duration, bool freeze, InterfaceState exitState)
        {
            ShowMessage(gameTime, message, duration, freeze, null, null, exitState);
        }

        /// <summary>
        /// Shows a message over the gameplay.
        /// </summary>
        /// <param name="gameTime">GameTime object of the current frame.</param>
        /// <param name="message">Message to show.</param>
        /// <param name="duration">How long to show the message for.</param>
        /// <param name="freeze">If true, game will freeze while the message is on display.</param>
        /// <param name="foreground">Color of the message text</param>
        /// <param name="background">Backgound color of the message</param>
        /// <param name="exitState">Interface state to return to after <i>duration</i> time runs out.</param>
        public void ShowMessage(GameTime gameTime, string message, double duration, bool freeze = true,
            Color? foreground = null, Color? background = null, InterfaceState exitState = InterfaceState.Ingame)
        {
            if (foreground == null) foreground = Color.Black;
            if (background == null) background = Color.Yellow;

            messageForeground = (Color)foreground;
            messageBackground = (Color)background;
            messageFreeze = freeze;
            ingameMessage = message;
            ifstate = InterfaceState.IngameMessage;
            DelayState(gameTime, duration, exitState);
        }

        /// <summary>
        /// Puts a state in the queue and switches to it after <i>delay</i> seconds.
        /// </summary>
        /// <param name="gameTime">GameTime object of the current frame.</param>
        /// <param name="delay">Delay time in seconds.</param>
        /// <param name="state">The state to switch to.</param>
        void DelayState(GameTime gameTime, double delay, InterfaceState state)
        {
            ifstateQueue.Add(gameTime.TotalGameTime.TotalSeconds + delay, state);
        }

        /// <summary>
        /// Main update method. Is called in regular intervals.
        /// </summary>
        /// <param name="gameTime">GameTime object of the current frame.</param>
        protected override void Update(GameTime gameTime)
        {
            input.Update();

            if (input.KeyPressed(Keys.F3))
            {
                debug = !debug;
            }

            while (ifstateQueue.Count > 0 && ifstateQueue.Min(kv => kv.Key) < gameTime.TotalGameTime.TotalSeconds)
            {
                var key = ifstateQueue.Min(kv => kv.Key);
                ifstate = ifstateQueue.First(kv => kv.Key == key).Value;
                ifstateQueue.Remove(key);
            }

            playingSounds = playingSounds.Where(s => s.State == SoundState.Playing).ToList();

            switch (ifstate)
            {
                case InterfaceState.Title:
                    UpdateTitle(gameTime);
                    break;
                case InterfaceState.Ingame:
                    UpdateIngame(gameTime);
                    break;
                case InterfaceState.IngameMessage:
                    if (!messageFreeze)
                        UpdateIngame(gameTime);
                    break;
                case InterfaceState.ShowScore:
                    UpdateScore(gameTime);
                    break;
                case InterfaceState.Over:
                    UpdateOver(gameTime);
                    break;
            }

            base.Update(gameTime);
        }

        // Following methods are just there to split the body
        // of the main Update method to multiple chunks.
        void UpdateScore(GameTime gameTime)
        {
            if (input.KeyPressed(Keys.Enter))
            {
                StopAllSounds();
                sounds["blip"].Play();
                RestartRound(level.Round + 1);
                ifstate = InterfaceState.Freeze;
                DelayState(gameTime, 1, InterfaceState.Ingame);
            }
        }
        void UpdateOver(GameTime gameTime)
        {
            if (input.KeyPressed(Keys.Enter))
            {
                StopAllSounds();
                sounds["blip"].Play();
                ifstate = InterfaceState.Title;
            }
        }
        void UpdateTitle(GameTime gameTime)
        {
            if (input.KeyPressed(Keys.B) || input.KeyPressed(Keys.G))
            {
                sounds["blip"].Play();
                playerIsBoy = input.KeyPressed(Keys.B);
                RestartGame();
                ifstate = InterfaceState.Ingame;
            }
        }
        void UpdateIngame(GameTime gameTime)
        {
            level.Update(gameTime);

            RoundTime -= (float)gameTime.ElapsedGameTime.TotalSeconds * 5;
            if (playerEntity.DecisionFrame)
            {
                var reach = playerEntity.EntitiesInReach();
                var bounds = new Rectangle(1, 1, level.SizeX - 2, level.SizeY - 2);
                if (!bounds.Contains(playerEntity.Position.ToVector2()) && reach.Contains(hostageEntity))
                {
                    WinRound(gameTime);
                }

                if (hostageEntity.TiedUp && reach.Contains(hostageEntity))
                {
                    ShowMessage(gameTime, "\"MY HERO! TAKE ME\nAWAY FROM ALL THIS!\"", 2.5);
                    sounds["rescue"].Play();
                    hostageEntity.Rescue();
                }

                if (level.GetEntityAt(playerEntity.Position - Vector3.UnitZ) is Ant ant && !ant.Paralyzed)
                {
                    ShowMessage(gameTime, "PARALYZED AN ANT!", 2.5, false, Color.Blue, Color.Transparent);
                    sounds["ant"].Play();
                    ant.Paralyze(350);
                }


                if (input.KeyPressed(ControlKeys.Bomb))
                {
                    throwForce = 0;
                }
                if (ammo > 0 && input.KeyDown(ControlKeys.Bomb) && throwForce < 1f)
                {
                    throwForce += (float)gameTime.ElapsedGameTime.TotalSeconds;
                }
                else
                {
                    if (input.KeyDown(ControlKeys.Right) && input.KeyDown(ControlKeys.Down))
                        playerEntity.StepIn(EntityDirection.PositiveX);
                    if (input.KeyDown(ControlKeys.Left) && input.KeyDown(ControlKeys.Up))
                        playerEntity.StepIn(EntityDirection.NegativeX);
                    if (input.KeyDown(ControlKeys.Left) && input.KeyDown(ControlKeys.Down))
                        playerEntity.StepIn(EntityDirection.PositiveY);
                    if (input.KeyDown(ControlKeys.Right) && input.KeyDown(ControlKeys.Up))
                        playerEntity.StepIn(EntityDirection.NegativeY);

                    if ((input.KeyDown(ControlKeys.Bomb) || input.KeyReleased(ControlKeys.Bomb)) && !float.IsPositiveInfinity(throwForce))
                    {
                        if (ammo > 0)
                        {
                            ammo--;
                            var bomb = new Bomb((int)(4 + throwForce * 8));
                            bomb.Direction = playerEntity.Direction;
                            bomb.Spawn(level, playerEntity.Position);
                        }

                        throwForce = float.PositiveInfinity;
                    }
                }
            }

            if (ifstate == InterfaceState.Ingame)
            {
                if (!(hostageEntity.Alive && playerEntity.Alive))
                {
                    ShowMessage(gameTime, "\"They got me!\"", 3.5, false, InterfaceState.Over);
                    PlaySound(sounds["over"].CreateInstance());
                }
            }
        }

        void RestartGame()
        {
            score = 0;
            rescued = 0;
            winFlag = false;
            RestartRound(0);
        }
        void RestartRound(int round)
        {
            level.RestartRound(playerIsBoy, round);
            RoundTime = 1000;
            ammo = 20;
        }
        void WinRound(GameTime gameTime)
        {
            // tell the player that they won
            ShowMessage(gameTime, "YOU ARE A TRUE HERO!", 1.0, true, InterfaceState.IngameMessage);
            PlaySound(sounds["fanfara"].CreateInstance());
            
            // calculate score
            score += (int)((playerEntity.Hitpoints + hostageEntity.Hitpoints) / 20f * RoundTime);
            rescued++;

            // decide the next state
            if (level.Round < level.RoundCount - 1)
            {
                DelayState(gameTime, 3.0, InterfaceState.ShowScore);
            }
            else
            {
                DelayState(gameTime, 3.0, InterfaceState.Over);
                winFlag = true;
            }
        }

        /// <summary>
        /// Draw a filled rectangle using SpriteBatch. Needs to be called between SpriteBatch.Begin and SpriteBatch.End.
        /// </summary>
        /// <param name="rect">Rectangle in pixels.</param>
        /// <param name="color">Color of the rectangle.</param>
        void fillRect(Rectangle rect, Color color) { sb.Draw(tiles.Texture, rect, fontTileset.GetTile(256 + 64), color); }
        /// <summary>
        /// Print a text using SpriteBatch. Needs to be called between SpriteBatch.Begin and SpriteBatch.End.
        /// </summary>
        /// <param name="str">Text to print.</param>
        /// <param name="pos">Position in pixels.</param>
        /// <param name="color">Color of the text.</param>
        void print(string str, Vector2 pos, Color color) { sb.DrawString(font, str, pos, color); }

        // Custom palette
        Color gray = Color.LightGray;
        Color red = Color.FromNonPremultiplied(189, 0, 0, 255);
        Color magenta = Color.FromNonPremultiplied(189, 0, 189, 255);
        Color yellow = Color.FromNonPremultiplied(200, 200, 0, 255);
        Color blue = Color.FromNonPremultiplied(0, 0, 189, 255);

        /// <summary>
        /// Main drawing method. Called approx 60 times per second.
        /// </summary>
        /// <param name="gameTime">GameTime object of the current frame.</param>
        protected override void Draw(GameTime gameTime)
        {
            switch (ifstate)
            {
                case InterfaceState.Title:
                    DrawTitle(gameTime);
                    break;
                case InterfaceState.Ingame:
                    DrawIngame(gameTime);
                    break;
                case InterfaceState.IngameMessage:
                    DrawIngame(gameTime, false);
                    DrawIngameMessage(gameTime);
                    break;
                case InterfaceState.ShowScore:
                    DrawScore(gameTime);
                    break;
                case InterfaceState.Over:
                    DrawGameOver(gameTime);
                    break;
                case InterfaceState.Freeze:
                    DrawToScreen();
                    break;
            }

            base.Draw(gameTime);
        }

        /// <summary>
        /// Draws the <i>buffer</i> to the output screen.
        /// </summary>
        void DrawToScreen()
        {
            GraphicsDevice.SetRenderTarget(null);
            GraphicsDevice.Clear(Color.Black);
            sb.Begin(samplerState: SamplerState.PointClamp);
            {
                sb.Draw(buffer, GraphicsDevice.Viewport.Bounds, Color.White);
            }
            sb.End();//*/
        }

        // the following Draw methods are there only to split the main Draw method to smaller chunks

        /// <summary>
        /// Draws the title screen to the screen.
        /// </summary>
        /// <param name="gameTime">GameTime object of the current frame.</param>
        void DrawTitle(GameTime gameTime)
        {
            ////////////////////////////////////////////////////
            // Draw to buffer
            GraphicsDevice.SetRenderTarget(buffer);
            GraphicsDevice.Clear(magenta);

            // draw title screen
            sb.Begin(samplerState: SamplerState.PointClamp);
            {
                fillRect(new Rectangle(8, 8, 256 - 16, 192 - 16), yellow);

                sb.Draw(tiles.Texture,
                    new Rectangle(8, 40, 240, 32),
                    new Rectangle(0, 88, 60, 8), Color.Black);

                print(
                    "WELCOME TO ...\n\n\n\n\n\n\n\n\n" +
                    "You find yourself in front\n" +
                    "of gates of a great city.\n" +
                    "You hear a call in distress\n" +
                    "calling for a hero like you\n\n\n" +
                    "Press a key to play as\n" +
                    "(b)oy or (g)irl.",
                    new Vector2(16, 16), Color.Black);
            }
            sb.End();

            DrawToScreen();
        }

        /// <summary>
        /// Draws the game level to the buffer or screen.
        /// </summary>
        /// <param name="gameTime">GameTime object of the current frame.</param>
        /// <param name="toScreen">If true, will call DrawToScreen() at the end.</param>
        void DrawIngame(GameTime gameTime, bool toScreen = true)
        {
            viewMat = Matrix.CreateTranslation(buffer.Width / 2f, (buffer.Height - 64 + 12) / 2f, 0f);
            viewMat *= Matrix.CreateTranslation(new Vector3(LevelRenderer.WorldToScreen(-playerEntity.Position), 0f));

            ////////////////////////////////////////////////////
            // Draw to buffer
            GraphicsDevice.SetRenderTarget(buffer);

            GraphicsDevice.Clear(Color.LightGray);
            // draw level
            sb.Begin(sortMode: SpriteSortMode.Deferred, transformMatrix: viewMat);
            {
                levelRenderer.Draw(gameTime, tiles, playerEntity.Position);
            }
            sb.End();

            // draw UI
            sb.Begin(samplerState: SamplerState.PointClamp);
            {
                Human boy;
                Human girl;

                if (playerIsBoy)
                {
                    boy = level.Player;
                    girl = level.Hostage;
                }
                else
                {
                    boy = level.Hostage;
                    girl = level.Player;
                }

                fillRect(new Rectangle(0, 0, 256, 12), Color.Black);
                fillRect(new Rectangle(0, 12, 8, 116), Color.Black);
                fillRect(new Rectangle(256 - 8, 12, 8, 116), Color.Black);
                fillRect(new Rectangle(0, 192 - 64, 256, 64), Color.Black);

                fillRect(new Rectangle(8, 136, 240, 8), magenta);
                fillRect(new Rectangle(8, 168, 240, 8), magenta);

                fillRect(new Rectangle(16, 144, 176, 24), blue);
                fillRect(new Rectangle(16, 152, 176, 8), gray);

                for (int i = 1; i <= 3; i++)
                    fillRect(new Rectangle(48 * i, 144, 16, 24), Color.Black);

                print($"SCORE :{score}", new Vector2(16, 0), gray);
                print($"Rescued :{rescued}", new Vector2(144, 0), gray);

                print($"{ammo,4}  {girl.Hitpoints,4}  {boy.Hitpoints,4}  {(int)RoundTime,4}", new Vector2(16, 152), Color.Black);
                print("AMMO  GIRL  BOY   TIME  ", new Vector2(16, 168), gray);

                if (debug)
                {
                    print($"player: {playerEntity.TilePosition}", new Vector2(8, 12), Color.Yellow);
                }
            }
            sb.End();

            ////////////////////////////////////////////////////
            // Draw to screen
            if (toScreen) DrawToScreen();
        }

        /// <summary>
        /// Draws a message to the buffer and draws the buffer to the screen.
        /// </summary>
        /// <param name="gameTime">GameTime object of the current frame.</param>
        void DrawIngameMessage(GameTime gameTime)
        {
            ////////////////////////////////////////////////////
            // Draw to buffer
            GraphicsDevice.SetRenderTarget(buffer);
            sb.Begin();
            {
                int top = 32;
                int width = 0;
                int i = 0;
                foreach (char c in ingameMessage)
                {
                    i++;
                    if (c != '\n') width += 8;

                    if (c == '\n' || ingameMessage.Length == i)
                    {
                        fillRect(new Rectangle(32, top, width, 8), messageBackground);
                        top += 8;
                        width = 0;
                    }
                }

                sb.DrawString(font, ingameMessage, new Vector2(32, 32), messageForeground);
            }
            sb.End();

            DrawToScreen();
        }

        /// <summary>
        /// Draws the scoreboard
        /// </summary>
        /// <param name="gameTime">GameTime object of the current frame.</param>
        void DrawScore(GameTime gameTime)
        {
            ////////////////////////////////////////////////////
            // Draw to buffer
            GraphicsDevice.SetRenderTarget(buffer);
            GraphicsDevice.Clear(magenta);

            // draw title screen
            sb.Begin(samplerState: SamplerState.PointClamp);
            {
                fillRect(new Rectangle(8, 8, 256 - 16, 192 - 16), yellow);

                const int rightCol = 184, livesSavedLine = 7, timeLeftLine = 10, scoreLine = 14;


                fillRect(new Rectangle(48, 16, 160, 16), Color.Black);
                print("**** ANT ATTACK ****\n**** SCORE CARD ****", new Vector2(48, 16), gray);

                bool blink = (int)(gameTime.TotalGameTime.TotalSeconds * 1.5) % 2 == 1;
                if (blink)
                {
                    fillRect(new Rectangle(rightCol / 8 - 1, scoreLine - 1, score.ToString().Length + 2, 3).Scaled(8), Color.Cyan);
                    fillRect(new Rectangle(rightCol / 8, scoreLine, score.ToString().Length, 1).Scaled(8), yellow);
                }

                print("LIVES SAVED :", new Vector2(16, 8 * livesSavedLine), blue);
                print(rescued.ToString(), new Vector2(rightCol, 8 * livesSavedLine), blue);
                print("TIME LEFT   :", new Vector2(16, 8 * timeLeftLine), blue);
                print(RoundTime.ToString("F0"), new Vector2(rightCol, 8 * timeLeftLine), blue);
                print("TOTAL SCORE :", new Vector2(16, 8 * scoreLine), Color.Black);
                print(score.ToString(), new Vector2(rightCol, 8 * scoreLine), Color.Black);

                print("Press ENTER to try again.", new Vector2(16, buffer.Height - 8 * 4), Color.Black);
            }
            sb.End();

            DrawToScreen();
        }

        /// <summary>
        /// Draws the game over screen.
        /// </summary>
        /// <param name="gameTime">GameTime object of the current frame.</param>
        void DrawGameOver(GameTime gameTime)
        {
            ////////////////////////////////////////////////////
            // Draw to buffer
            GraphicsDevice.SetRenderTarget(buffer);
            GraphicsDevice.Clear(magenta);

            const int rightCol = 184, livesSavedLine = 14, scoreLine = livesSavedLine + 2;

            // draw title screen
            sb.Begin(samplerState: SamplerState.PointClamp);
            {
                fillRect(new Rectangle(8, 8, 256 - 16, 192 - 16), yellow);

                if (!winFlag)
                {
                    sb.Draw(tiles.Texture,
                        new Rectangle(8, 24, 240, 32),
                        new Rectangle(0, 96, 60, 8), Color.Red);

                    print(ingameMessage, new Vector2(16, 64), Color.Black);
                }
                else
                {
                    fillRect(new Rectangle(12, 12, 232, 40), Color.Black);
                    sb.Draw(tiles.Texture,
                        new Rectangle(8, 16, 240, 32),
                        new Rectangle(0, 104, 60, 8), gray);

                    print("CONGRATULATIONS! You have\nrescued everyone in the city.",
                        new Vector2(12, 64), Color.Black);
                }

                print("LIVES SAVED :", new Vector2(16, 8 * livesSavedLine), Color.Black);
                print(rescued.ToString(), new Vector2(rightCol, 8 * livesSavedLine), Color.Black);
                print("TOTAL SCORE :", new Vector2(16, 8 * scoreLine), Color.Black);
                print(score.ToString(), new Vector2(rightCol, 8 * scoreLine), Color.Black);

                print("Press ENTER to go back to\ntitle screen.", new Vector2(16, buffer.Height - 8 * 5), Color.Black);
            }
            sb.End();

            DrawToScreen();
        }
    }
}
