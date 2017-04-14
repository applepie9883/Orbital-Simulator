using System;
using GM.ECSLibrary;
using GM.ECSLibrary.Systems;
using GM.ECSLibrary.Components;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;

namespace Orbital_Simulator
{
    public class OrbiterSystem : SystemBase
    {
        public override UpdateStage SystemUpdateStage => UpdateStage.Update;

        private Random rndGen = new Random();

        public OrbiterSystem()
        {
            _RequiredComponents.AddRange(new[] { typeof(PositionComponent), typeof(VelocityComponent) });
        }

        protected override void OnUpdate(Entity updatingEntity)
        {
            MouseState currentMouseState = ManagerCatalog.CurrentMouseState;
            MouseState oldMouseState = ManagerCatalog.OldMouseState;
            KeyboardState currentKeyboardState = ManagerCatalog.CurrentKeyboardState;

            PositionComponent orbiterPosition = updatingEntity.GetComponent<PositionComponent>();
            VelocityComponent orbiterVelocity = updatingEntity.GetComponent<VelocityComponent>();

            Vector2 mousePosition = currentMouseState.Position.ToVector2();

            float radians = (float)Math.Atan2(-(mousePosition.Y - orbiterPosition.Position.Y), (mousePosition.X - orbiterPosition.Position.X));
            double degrees = MathHelper.ToDegrees(radians);

            while (degrees > 360)
                degrees -= 360;

            while (degrees < 0)
                degrees += 360;

            if (currentMouseState.LeftButton == ButtonState.Pressed)
            {
                float distanceToMouse = Vector2.Distance(orbiterPosition.Position, mousePosition);

                if (distanceToMouse < 20)
                {
                    orbiterVelocity.Velocity = Vector2.Zero;
                }
                else
                {
                    orbiterVelocity.SetSpeedAndDirection(distanceToMouse / 3, degrees);
                }
            }
            else if (currentMouseState.RightButton == ButtonState.Pressed)
            {
                orbiterVelocity.Accelerate(rndGen.NextDouble() * 3, rndGen.NextDouble() * 360);
            }
            else
            {
                // Slow the orbiter down (by 2%??) if the mouse hasn't moved since the last update
                if (oldMouseState.Position.Equals(currentMouseState.Position))
                {
                    orbiterVelocity.Accelerate(-orbiterVelocity.Speed / 50, orbiterVelocity.DirectionDegrees);
                }

                // TODO: Get this working (or not)
                // Massive lag box here
                /*
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
                */

                orbiterVelocity.Accelerate(0.5, degrees);
            }

            //orbiterPosition.Position += orbiterVelocity.Velocity;

            // TODO: Possible get this code working (if not here then somewhere else)
            //orbiter.KeepWithin(new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));




            // TODO: This code needs to be moved into the main update method in the game class
            /*
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
            */
        }
    }
}