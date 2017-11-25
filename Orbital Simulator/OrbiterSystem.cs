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

        private Random RndGen { get; set; }

        public OrbiterSystem()
        {
            _RequiredComponents.AddRange(new[] { typeof(PositionComponent), typeof(VelocityComponent) });

            RndGen = new Random();
        }

        protected override void OnUpdate(Entity updatingEntity)
        {
            MouseState currentMouseState = ManagerCatalog.CurrentMouseState;
            MouseState oldMouseState = ManagerCatalog.OldMouseState;

            PositionComponent orbiterPosition = updatingEntity.GetComponent<PositionComponent>();
            VelocityComponent orbiterVelocity = updatingEntity.GetComponent<VelocityComponent>();

            Vector2 mousePosition = currentMouseState.Position.ToVector2();

            double accelerationSpeed = 0;
            double accelerationRadians = 0;

            double radians = Math.Atan2(-(mousePosition.Y - orbiterPosition.Position.Y), (mousePosition.X - orbiterPosition.Position.X));

            // 2π is equivalent to 360 degrees
            while (radians >= 2 * Math.PI)
                radians -= 2 * Math.PI;

            while (radians < 0)
                radians += 2 * Math.PI;

            if (currentMouseState.LeftButton == ButtonState.Pressed)
            {
                float distanceToMouse = Vector2.Distance(orbiterPosition.Position, mousePosition);

                if (distanceToMouse < 20)
                {
                    orbiterVelocity.Velocity = Vector2.Zero;
                }
                else
                {
                    orbiterVelocity.SetSpeedAndDirection(distanceToMouse / 3, radians);
                }
            }
            else if (currentMouseState.RightButton == ButtonState.Pressed)
            {
                lock (RndGen)
                {
                    orbiterVelocity.Accelerate(ManagerCatalog.CurrentGameTime, RndGen.NextDouble() * 3, RndGen.NextDouble() * 360);
                }
            }
            else
            {
                // Slow the orbiter down (by 2%??) if the mouse hasn't moved since the last update
                if (oldMouseState.Position.Equals(currentMouseState.Position))
                {
                    orbiterVelocity.Accelerate(ManagerCatalog.CurrentGameTime, -orbiterVelocity.Speed / 50, orbiterVelocity.DirectionRadians);
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

                orbiterVelocity.Accelerate(ManagerCatalog.CurrentGameTime, 0.5, radians);
            }

            double elapsedMilliseconds = ManagerCatalog.CurrentGameTime.ElapsedGameTime.TotalMilliseconds;

            if (elapsedMilliseconds < orbiterVelocity.MovementMilliseconds || orbiterVelocity.OverCompinsate)
            {
                accelerationSpeed *= (float)(elapsedMilliseconds / orbiterVelocity.MovementMilliseconds);
            }

            orbiterVelocity.Accelerate(accelerationSpeed, accelerationRadians);

            // TODO: Possible get this code working (if not here then somewhere else)
            //orbiter.KeepWithin(new Rectangle(0, 0, GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height));
        }
    }
}