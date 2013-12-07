using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PrisonStep
{
    public class Spit : DrawableGameComponent
    {

        #region Fields

        private PrisonGame game;

        private Matrix myTransform = Matrix.Identity;
        public Matrix MyTransform { get { return myTransform; } set { myTransform = value; } }

        private const float velocity = 100f;
        private Vector3 directionShooting = Vector3.Zero;
        public Vector3 DirectionShooting { get { return directionShooting; } set { directionShooting = value; directionShooting.Normalize(); } }

        #endregion

        #region Initialization Constructor Restart

        public Spit(PrisonGame game):base(game)
        {
            this.game = game;
            this.DrawOrder = 1000;
        }

        #endregion

        #region Functions


        /// <summary>
        /// This function is called to update this component of our game
        /// to the current game time.
        /// </summary>
        /// <param name="gameTime"></param>
        public bool Update(double delta)
        {
                myTransform.Translation += directionShooting * velocity * (float)delta;

           

                string region = game.CollisionDetector.TestRegion(myTransform.Translation);
                //If region doesn't exist, then you've hit a wall
                if (region.Equals(String.Empty))
                {
                    return false;
                    //currPieState = PieState.OnWall;
                }


                #region AsteroidXwingCollisions
                Matrix[] transforms = new Matrix[game.PieModel.Bones.Count];
                game.PieModel.CopyAbsoluteBoneTransformsTo(transforms);
                Matrix xwingTransform = myTransform;
                foreach (ModelMesh mesh in game.PieModel.Meshes)
                {
                    BoundingSphere bs = mesh.BoundingSphere;
                    bs = bs.Transform(transforms[mesh.ParentBone.Index] * xwingTransform);
                    if (game.Player.TestSphereForCollision(bs))
                    {
                        game.StartSliming();
                        game.Score -= 100;
                        return false;
                    }
                }
                #endregion





                return true;
           
        }

        /// <summary>
        /// This function is called to draw this game component.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="gameTime"></param>
        
        public void Draw(GraphicsDeviceManager graphics, GameTime gameTime, Matrix transform)
        {
            graphics.GraphicsDevice.BlendState = BlendState.AlphaBlend;
            DrawModel(graphics, game.SpitModel, transform);
            graphics.GraphicsDevice.BlendState = BlendState.Opaque;
        }

        private void DrawModel(GraphicsDeviceManager graphics, Model model, Matrix world)
        {
            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting();
                    effect.World = Matrix.CreateRotationX((float)Math.PI / 2) * transforms[mesh.ParentBone.Index] * world;
                    effect.View = game.Camera.View;
                    effect.Projection = game.Camera.Projection;
                }
                mesh.Draw();
            }
        }

        #endregion
    }
}
