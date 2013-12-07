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
    public class Victoria : AnimatedModel
    {

        public Victoria(PrisonGame game, string asset):base(game, asset)
        {
            
        }

        override protected void DrawModel(GraphicsDeviceManager graphics, Model model, Matrix world)
        {
            #region RotateSpine
            int spineBone = model.Bones["Bip01 Spine1"].Index;
            boneTransforms[spineBone] = Matrix.CreateRotationZ(spineElevation) * Matrix.CreateRotationX(spineAzimuth) * boneTransforms[spineBone];
            model.CopyBoneTransformsFrom(boneTransforms);
            model.CopyAbsoluteBoneTransformsTo(absoTransforms);
            #endregion

            base.DrawModel(graphics, model, world);
        }
    }
}
