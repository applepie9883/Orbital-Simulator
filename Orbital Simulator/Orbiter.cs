using GM.ECSLibrary;
using GM.ECSLibrary.Components;
using Microsoft.Xna.Framework.Graphics;

namespace Orbital_Simulator
{
    public class Orbiter : Entity
    {
        public Orbiter()
        {
            AddComponent(new PositionComponent());
            AddComponent(new VelocityComponent());
            AddComponent(new SpriteComponent());
        }
    }
}
