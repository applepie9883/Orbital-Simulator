﻿using GM.ECSLibrary;
using GM.ECSLibrary.Components;
using GM.ECSLibrary.Systems;
using GM.SpriteLibrary;
using GM.SpriteLibrary.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;

namespace Orbital_Simulator
{
    public enum GameScreen
    {
        MainMenu,
        Options,
        Playing,
        Paused
    };

    /// <summary>
    /// This is the main type for your game.
    /// </summary>
    public class OrbitalSimulator : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        SpriteFont aFont;

        GameScreen currentScreen;

        Panel menuPanel;
        Panel optionsPanel;

        OptionsData gameOptions;

        List<double> previousFramesPerSecond;

        List<Entity> orbiters;

        SystemsManager manager;

        Thread[] threads;

        ManualResetEvent[] resEvents;

        MouseState currentMouseState;
        MouseState oldMouseState;
        KeyboardState currentKeyboardState;
        KeyboardState oldKeyboardState;

        public OrbitalSimulator()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            Window.Title = "Orbital Simulator";
            IsFixedTimeStep = false;
            IsMouseVisible = true;
            Window.IsBorderless = true;
            graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            /*
            threads = new Thread[8];

            for (int i = 0; i < threads.Length; i++)
            {
                threads[i] = new Thread(UpdatePlayingThreaded);
            }

            resEvents = new ManualResetEvent[8];

            for (int i = 0; i < resEvents.Length; i++)
            {
                resEvents[i] = new ManualResetEvent(false);
            }
            */

            gameOptions = OptionsData.LoadDataFromFile("options.xml");

            if (gameOptions == null)
            {
                gameOptions = new OptionsData(8000, 2, 2, false, Color.Black, false);
            }

            gameOptions.WriteDataToFile("options.xml");

            currentScreen = GameScreen.MainMenu;

            previousFramesPerSecond = new List<double>();


            base.Initialize();


            manager = new SystemsManager(GraphicsDevice, spriteBatch);
            manager.AddSystem(new OrbiterSystem());
            manager.AddSystem(new VelocitySystem());
            manager.AddSystem(new DrawingSystem());
        }

        private void InitializeGame()
        {
            Random rndGen = new Random();

            orbiters = new List<Entity>();

            Texture2D orbiterTexture = new Texture2D(GraphicsDevice, gameOptions.OrbiterWidth, gameOptions.OrbiterHeight, false, SurfaceFormat.Color);
            orbiterTexture.SetData(Enumerable.Range(0, gameOptions.OrbiterWidth * gameOptions.OrbiterHeight).Select(i => gameOptions.OrbiterColor).ToArray());

            for (int i = 0; i < gameOptions.OrbiterCount; i++)
            {
                Orbiter newOrbiter = new Orbiter();

                newOrbiter.GetComponent<PositionComponent>().PositionOffset = new Vector2(gameOptions.OrbiterWidth / 2, gameOptions.OrbiterHeight / 2);
                newOrbiter.GetComponent<PositionComponent>().Position = new Vector2(rndGen.Next(0, GraphicsDevice.Viewport.Width), rndGen.Next(0, GraphicsDevice.Viewport.Height));

                newOrbiter.GetComponent<SpriteComponent>().Texture = orbiterTexture;

                orbiters.Add(newOrbiter);
            }
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);

            aFont = Content.Load<SpriteFont>("Assets/Fonts/Varela Round");

            InitializeMainMenuPanel();
            InitializeOptionsPanel();
        }

        private void InitializeMainMenuPanel()
        {
            menuPanel = new Panel(GraphicsDevice, new Vector2(10, 200))
            {
                DefaultControlFont = aFont,
                DefaultControlColor = Color.Black
            };

            // Play button
            Button playButton = new Button("Play", Color.White);
            playButton.GenerateTexture(GraphicsDevice, 80, 30, Color.Black);
            playButton.SetPosition(new Vector2(10, 10));
            playButton.OnLeftClickInsideEnd += (object sender, EventArgs e) => { StartGame(); };

            // Options button
            Button optionsButton = new Button("Options", Color.White);
            optionsButton.GenerateTexture(GraphicsDevice, 80, 30, Color.Black);
            optionsButton.SetPosition(playButton.GetPosition() + new Vector2(0, playButton.Height + 5));
            optionsButton.OnLeftClickInsideEnd += (object sender, EventArgs e) => { currentScreen = GameScreen.Options; };

            // Tests
            TextBox testBox = new TextBox(false, Color.White)
            {
                Width = 800,
                Height = 30
            };
            testBox.SetText("357.89");

            menuPanel.Add(playButton, "playButton");
            menuPanel.Add(optionsButton, "optionsButton");

            // Tests
            //menuPanel.Add(testBox, "testBox");
        }

        private void InitializeOptionsPanel()
        {
            optionsPanel = new Panel(GraphicsDevice, new Vector2(10, 200))
            {
                DefaultControlFont = aFont
            };

            // Main manu button
            Button mainMenuButton = new Button("Main Menu", Color.White);
            mainMenuButton.GenerateTexture(GraphicsDevice, 110, 30, Color.Black);
            mainMenuButton.SetPosition(new Vector2(10, 10));
            mainMenuButton.OnLeftClickInsideEnd += OptionsPanel_MainMenuButton_OnLeftClickInsideEnd;

            // Orbiter label and box
            TextBox orbiterCountLabelBox = new TextBox(true, Color.Black);
            orbiterCountLabelBox.GenerateTexture(GraphicsDevice, 110, 30, Color.Transparent);
            orbiterCountLabelBox.SetPosition(mainMenuButton.GetPosition() + new Vector2(0, mainMenuButton.Height + 5));
            orbiterCountLabelBox.SetText("Orbiter Count:");

            TextBox orbiterCountBox = new TextBox(false, Color.White);
            orbiterCountBox.GenerateTexture(GraphicsDevice, 80, 30, Color.Black);
            orbiterCountBox.SetPosition(orbiterCountLabelBox.GetPosition() + new Vector2(orbiterCountLabelBox.Width + 5, 0));
            orbiterCountBox.AllowedCharacters = "0123456789";
            orbiterCountBox.SetText(gameOptions.OrbiterCount.ToString());

            // Height label and box
            TextBox orbiterHeightLabelBox = new TextBox(true, Color.Black);
            orbiterHeightLabelBox.GenerateTexture(GraphicsDevice, 110, 30, Color.Transparent);
            orbiterHeightLabelBox.SetPosition(orbiterCountLabelBox.GetPosition() + new Vector2(0, orbiterCountLabelBox.Height + 5));
            orbiterHeightLabelBox.SetText("Box height:");

            TextBox orbiterHeightBox = new TextBox(false, Color.White);
            orbiterHeightBox.GenerateTexture(GraphicsDevice, 80, 30, Color.Black);
            orbiterHeightBox.SetPosition(orbiterHeightLabelBox.GetPosition() + new Vector2(orbiterHeightLabelBox.Width + 5, 0));
            orbiterHeightBox.AllowedCharacters = "0123456789";
            orbiterHeightBox.SetText(gameOptions.OrbiterHeight.ToString());

            // Width label and box
            TextBox orbiterWidthLabelBox = new TextBox(true, Color.Black);
            orbiterWidthLabelBox.GenerateTexture(GraphicsDevice, 110, 30, Color.Transparent);
            orbiterWidthLabelBox.SetPosition(orbiterHeightLabelBox.GetPosition() + new Vector2(0, orbiterHeightLabelBox.Height + 5));
            orbiterWidthLabelBox.SetText("Box width:");

            TextBox orbiterWidthBox = new TextBox(false, Color.White);
            orbiterWidthBox.GenerateTexture(GraphicsDevice, 80, 30, Color.Black);
            orbiterWidthBox.SetPosition(orbiterWidthLabelBox.GetPosition() + new Vector2(orbiterWidthLabelBox.Width + 5, 0));
            orbiterWidthBox.AllowedCharacters = "0123456789";
            orbiterWidthBox.SetText(gameOptions.OrbiterWidth.ToString());

            // Buddy system label and check box
            TextBox buddySystemLabelBox = new TextBox(true, Color.Black);
            buddySystemLabelBox.GenerateTexture(GraphicsDevice, 110, 30, Color.Transparent);
            buddySystemLabelBox.SetPosition(orbiterWidthLabelBox.GetPosition() + new Vector2(0, orbiterWidthLabelBox.Height + 5));
            buddySystemLabelBox.SetText("Buddy system:");

            CheckBox buddySystemCheckBox = new CheckBox(gameOptions.BuddySystem);
            buddySystemCheckBox.GenerateTexture(GraphicsDevice, 30, 30, Color.White);
            buddySystemCheckBox.SetPosition(buddySystemLabelBox.GetPosition() + new Vector2(buddySystemLabelBox.Width + 5, 0));
            buddySystemCheckBox.SetChecked(gameOptions.BuddySystem);

            // Add them all to the panel
            optionsPanel.Add(mainMenuButton, "mainMenuButton");
            optionsPanel.Add(orbiterCountLabelBox, "orbiterCountLabelBox");
            optionsPanel.Add(orbiterCountBox, "orbiterCountBox");
            optionsPanel.Add(orbiterWidthLabelBox, "orbiterWidthLabelBox");
            optionsPanel.Add(orbiterWidthBox, "orbiterWidthBox");
            optionsPanel.Add(orbiterHeightLabelBox, "orbiterHeightLabelBox");
            optionsPanel.Add(orbiterHeightBox, "orbiterHeightBox");
            optionsPanel.Add(buddySystemLabelBox, "buddySystemLabelBox");
            optionsPanel.Add(buddySystemCheckBox, "buddySystemCheckBox");
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// game-specific content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            currentMouseState = Mouse.GetState();
            currentKeyboardState = Keyboard.GetState();

            if (oldMouseState == null)
            {
                oldMouseState = currentMouseState;
            }

            if (oldKeyboardState == null)
            {
                oldKeyboardState = currentKeyboardState;
            }

            if (currentScreen == GameScreen.MainMenu)
            {
                UpdateMainMenu(gameTime);
            }
            else if (currentScreen == GameScreen.Options)
            {
                UpdateOptions(gameTime);
            }
            else if (currentScreen == GameScreen.Playing)
            {
                UpdatePlaying(gameTime);
            }
            else if (currentScreen == GameScreen.Paused)
            {
                UpdatePaused();
            }

            oldMouseState = currentMouseState;
            oldKeyboardState = currentKeyboardState;

            base.Update(gameTime);
        }

        private void UpdateMainMenu(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || (currentKeyboardState.IsKeyDown(Keys.Escape) && oldKeyboardState.IsKeyUp(Keys.Escape)))
                Exit();

            menuPanel.Update(currentMouseState, oldMouseState, currentKeyboardState, oldKeyboardState, gameTime);
        }

        private void UpdateOptions(GameTime gameTime)
        {
            optionsPanel.Update(currentMouseState, oldMouseState, currentKeyboardState, oldKeyboardState, gameTime);
        }

        private void OptionsPanel_MainMenuButton_OnLeftClickInsideEnd(object sender, EventArgs e)
        {
            gameOptions.OrbiterCount = int.Parse(((TextBox)optionsPanel.GetControl("orbiterCountBox")).GetText());
            gameOptions.OrbiterHeight = int.Parse(((TextBox)optionsPanel.GetControl("orbiterHeightBox")).GetText());
            gameOptions.OrbiterWidth = int.Parse(((TextBox)optionsPanel.GetControl("orbiterWidthBox")).GetText());
            gameOptions.BuddySystem = ((CheckBox)optionsPanel.GetControl("buddySystemCheckBox")).GetChecked();

            gameOptions.WriteDataToFile("options.xml");

            currentScreen = GameScreen.MainMenu;
        }

        private void UpdatePlaying(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || (currentKeyboardState.IsKeyDown(Keys.Escape) && oldKeyboardState.IsKeyUp(Keys.Escape)))
            {
                StopGame();
                return;
            }

            if (currentKeyboardState.IsKeyDown(Keys.Space) && oldKeyboardState.IsKeyUp(Keys.Space))
            {
                currentScreen = GameScreen.Paused;
                return;
            }

            manager.Update(orbiters);

            /*
            int usedIndexes = 0;
            int orbitersPerThread = gameOptions.OrbiterCount / threads.Length;
            for (int i = 0; i < threads.Length; i++)
            {
                if (i == threads.Length - 1)
                {
                    //threads[i].Start(new ThreadItems(gameTime, usedIndexes, gameOptions.OrbiterCount - usedIndexes - 1));
                    ThreadPool.QueueUserWorkItem(UpdatePlayingThreaded, new ThreadItems(gameTime, usedIndexes, gameOptions.OrbiterCount - usedIndexes - 1));
                }

                usedIndexes += orbitersPerThread;
                //threads[i].Start(new ThreadItems(gameTime, i * orbitersPerThread, i * orbitersPerThread + orbitersPerThread - 1));
                ThreadPool.QueueUserWorkItem(UpdatePlayingThreaded, new ThreadItems(gameTime, i * orbitersPerThread, i * orbitersPerThread + orbitersPerThread - 1));
            }

            for (int i = 0; i < threads.Length; i++)
            {
                //threads[i].Join();
            }

            foreach (var handle in resEvents)
                handle.WaitOne();
            */
        }

        /*
        private void UpdatePlayingThreaded(object stuff)
        {
            Random rndGen1 = new Random();

            ThreadItems items = (ThreadItems)stuff;
            GameTime gameTime = items.TheGameTime;
            int startIndex = items.StartIndex;
            int endIndex = items.EndIndex;

            int orbitersPerThread = gameOptions.OrbiterCount / threads.Length;

            int resEventIndex = startIndex / orbitersPerThread;

            resEvents[resEventIndex].Reset();

            // If the game window is the current active window
            if (IsActive)
            {
                if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || (currentKeyboardState.IsKeyDown(Keys.Escape) && oldKeyboardState.IsKeyUp(Keys.Escape)))
                {
                    StopGame();
                    resEvents[resEventIndex].Set();
                    return;
                }

                if (currentKeyboardState.IsKeyDown(Keys.Space) && oldKeyboardState.IsKeyUp(Keys.Space))
                {
                    currentScreen = GameScreen.Paused;
                    resEvents[resEventIndex].Set();
                    return;
                }

                //DoBuddySystem();

                for (int i = startIndex; i <= endIndex; i++)
                {
                    MoveableSprite orbiter = orbiters[i];

                    if (currentMouseState.LeftButton == ButtonState.Pressed)
                    {
                        if (Vector2.Distance(orbiter.GetPosition(), currentMouseState.Position.ToVector2()) < 20)
                        {
                            orbiter.SetVelocity(Vector2.Zero);
                        }
                        else
                        {
                            orbiter.SetSpeedAndDirection(Vector2.Distance(orbiter.GetPosition(), currentMouseState.Position.ToVector2()) / 3, currentMouseState.Position.ToVector2());
                        }
                    }
                    else if (currentMouseState.RightButton == ButtonState.Pressed)
                    {
                        orbiter.Accelerate(rndGen1.NextDouble() * 3, rndGen1.NextDouble() * 360);
                    }
                    else
                    {
                        // Slow the orbiter down (by 2%??) if the mouse hasn't moved since the last update
                        if (oldMouseState.Position.Equals(currentMouseState.Position))
                        {
                            orbiter.Accelerate(-(orbiter.GetVelocity().Length() / 50), orbiter.GetDirectionAngle());
                        }

                        // Massive lag box here
                        if (gameOptions.OrbiterOrbiter)
                        {
                            foreach (var j in orbiters)
                            {
                                if (orbiter == j)
                                {
                                    continue;
                                }

                                orbiter.Accelerate(0.05, j.GetPosition());
                            }
                        }

                        orbiter.Accelerate(0.5, currentMouseState.Position.ToVector2());
                        //orbiter.Accelerate(3 / Vector2.Distance(orbiter.GetPosition(), currentMouseState.Position.ToVector2()), currentMouseState.Position.ToVector2());
                        //orbiter.Accelerate(Vector2.Distance(orbiter.GetPosition(), currentMouseState.Position.ToVector2()) / 100, currentMouseState.Position.ToVector2());
                    }

                    orbiter.MoveWithVelocity();

                    orbiter.KeepWithin(new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));
                }
            }

            resEvents[resEventIndex].Set();
        }

        private void DoBuddySystem()
        {
            if (gameOptions.BuddySystem)
            {
                for (int i = orbiters.Count - 1; i >= 0; i--)
                {
                    bool isTouching = false;

                    MoveableSprite targeter = orbiters[i];

                    for (int j = orbiters.Count - 1; j >= 0; j--)
                    {
                        MoveableSprite target = orbiters[j];

                        if (targeter == target)
                        {
                            continue;
                        }

                        if (targeter.GetBoundingRectangle().Intersects(target.GetBoundingRectangle()))
                        {
                            // TODO: Optimize this so that the target doesn't have to be checked, because we know it is touching the targeter
                            isTouching = true;
                            break;
                        }
                    }

                    if (!isTouching)
                    {
                        orbiters.Remove(targeter);
                    }
                }
            }
        }
        */

        private void UpdatePaused()
        {
            if (currentKeyboardState.IsKeyDown(Keys.Space) && oldKeyboardState.IsKeyUp(Keys.Space))
            {
                currentScreen = GameScreen.Playing;
            }
        }

        private void StartGame()
        {
            InitializeGame();
            currentScreen = GameScreen.Playing;
        }

        private void StopGame()
        {
            currentScreen = GameScreen.MainMenu;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            if (currentScreen == GameScreen.Playing || currentScreen == GameScreen.Paused)
            {
                manager.Draw(orbiters);
            }

            spriteBatch.Begin();

            if (currentScreen == GameScreen.MainMenu)
            {
                menuPanel.Draw(spriteBatch);
            }
            else if (currentScreen == GameScreen.Options)
            {
                optionsPanel.Draw(spriteBatch);
            }

            if (previousFramesPerSecond.Count >= 10)
            {
                previousFramesPerSecond.RemoveAt(0);
            }

            // For some reason this stops at 0.5 and doesn't go higher...
            previousFramesPerSecond.Add(1 / gameTime.ElapsedGameTime.TotalSeconds);

            int averageFPS = (int)previousFramesPerSecond.Average();

            spriteBatch.DrawString(aFont, averageFPS.ToString(), Vector2.Zero, Color.LightGray);

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}

public class ThreadItems
{
    public GameTime TheGameTime { get; set; }

    public int StartIndex { get; set; }

    public int EndIndex { get; set; }

    public ThreadItems(GameTime theTime, int start, int end)
    {
        TheGameTime = theTime;
        StartIndex = start;
        EndIndex = end;
    }
}