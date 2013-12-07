using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using XnaAux;
using Microsoft.Xna.Framework.Input;

namespace PrisonStep
{
    public class Alien : AnimatedModel
    {

        public Alien(PrisonGame game, string asset):base(game, asset)
        {   
        }

        override protected void DrawModel(GraphicsDeviceManager graphics, Model model, Matrix world)
        {
            base.DrawModel(graphics, model, world);
        }
    }
}
