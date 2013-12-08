using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace PrisonStep
{
    public class Pie
    {

        #region Fields

        private PrisonGame game;

        public enum PieType { Pie1, Pie2, Pie3 };
        private PieType currPieType = PieType.Pie1;
        public PieType CurrPieType { get { return currPieType; } set { currPieType = value; } }


        public enum PieState { InBazooka, Shooting, OnWall, OnAlien };
        private PieState currPieState = PieState.InBazooka;
        public PieState CurrPieState { get { return currPieState; } set { currPieState = value; } }


        private Matrix myTransform = Matrix.Identity;
        public Matrix MyTransform { get { return myTransform; } set { myTransform = value; } }

        private const float velocity = 100f;
        private Vector3 directionShooting = Vector3.Zero;
        public Vector3 DirectionShooting { get { return directionShooting; } set { directionShooting = value; directionShooting.Normalize(); } }

        #endregion

        #region Initialization Constructor Restart

        public Pie(PrisonGame game)
        {
            this.game = game;
        }

        #endregion

        #region Functions


        /// <summary>
        /// This function is called to update this component of our game
        /// to the current game time.
        /// </summary>
        /// <param name="gameTime"></param>
        public bool Update(double delta, Matrix world)
        {
            #region RayClickDetection
            MouseState currentMouseState = Mouse.GetState();
            if (lastMouseState.LeftButton == ButtonState.Released && currentMouseState.LeftButton == ButtonState.Pressed)
            {
                float mouseY = currentMouseState.Y;
                float mouseX = currentMouseState.X;

                // Only continue if you clicked in a valid region
                if (mouseY > 0 &&
                    mouseY < game.Graphics.GraphicsDevice.Viewport.Height &&
                    mouseX > 0 &&
                    mouseX < game.Graphics.GraphicsDevice.Viewport.Width)
                {
                    // Determine point on near clipping plane
                    Vector3 nearsource = new Vector3(mouseX, mouseY, 0);
                    Vector3 nearPoint = game.Graphics.GraphicsDevice.Viewport.Unproject(nearsource, game.Camera.Projection, game.Camera.View, Matrix.Identity);

                    // Determine point on far clipping plane
                    Vector3 farsource = new Vector3(mouseX, mouseY, 1);
                    Vector3 farPoint = game.Graphics.GraphicsDevice.Viewport.Unproject(farsource, game.Camera.Projection, game.Camera.View, Matrix.Identity);

                    // The direction of the click
                    Vector3 direction = farPoint - nearPoint;
                    direction.Normalize();

                    // Origin is where you clicked; headed towards the far point
                    Ray pickRay = new Ray(nearPoint, direction);

                    foreach (ModelMesh mesh in game.PieModel.Meshes)
                    {
                        BoundingSphere boundingSphere = mesh.BoundingSphere;
                        boundingSphere = boundingSphere.Transform(world);
                        float? distance = pickRay.Intersects(boundingSphere);
                        if (distance != null)
                        {
                            game.RemovePie(this);
                            //Console.Out.WriteLine(distance);
                        }
                    }
                }
            }
            lastMouseState = currentMouseState;
            #endregion RayClickDetection

            if (currPieState.Equals(PieState.Shooting))
            {
                myTransform.Translation += directionShooting * velocity * (float)delta;

                string region = game.CollisionDetector.TestRegion(myTransform.Translation);
                //If region doesn't exist, then you've hit a wall
                if (region.Equals(String.Empty))
                {
                    currPieState = PieState.OnWall;
                    game.Score -= 1;
                }


                #region AsteroidXwingCollisions
                Matrix[] transforms = new Matrix[game.PieModel.Bones.Count];
                game.PieModel.CopyAbsoluteBoneTransformsTo(transforms);
                Matrix xwingTransform = myTransform;
                foreach (ModelMesh mesh in game.PieModel.Meshes)
                {
                    BoundingSphere bs = mesh.BoundingSphere;
                    bs = bs.Transform(transforms[mesh.ParentBone.Index] * xwingTransform);
                    if (game.Alien2.TestSphereForCollision(bs))
                    {
                        currPieState = PieState.OnAlien;
                        game.Alien2.AddPie(this);
                        game.Score += 10;
                        return false;
                    }
                    if (game.Alien1.TestSphereForCollision(bs))
                    {
                        currPieState = PieState.OnAlien;
                        game.Alien1.Stun();
                        game.Score += 50;
                        return false;
                    }
                }
                #endregion

            }
            return true;


        }

        /// <summary>
        /// This function is called to draw this game component.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="gameTime"></param>
        
        public void Draw(GraphicsDeviceManager graphics, GameTime gameTime, Matrix transform)
        {
            DrawModel(graphics, game.PieModel, transform);
        }

        private MouseState lastMouseState = Mouse.GetState();

        private void DrawModel(GraphicsDeviceManager graphics, Model model, Matrix world)
        {


            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);
            Matrix offsetTransform = Matrix.Identity;

            foreach (ModelMesh mesh in model.Meshes)
            {
                if (currPieType == PieType.Pie1 && !mesh.Name.Contains("Pie1"))
                {
                    continue;
                }
                if (currPieType == PieType.Pie2 && !mesh.Name.Contains("Pie2"))
                {
                    offsetTransform = Matrix.CreateTranslation(0, -10, 0);
                    continue;
                }
                if (currPieType == PieType.Pie3 && !mesh.Name.Contains("Pie3"))
                {
                    offsetTransform = Matrix.CreateTranslation(0, -20, 0);
                    continue;
                }
                foreach (BasicEffect effect in mesh.Effects)
                {
                    effect.EnableDefaultLighting();
                    effect.World = offsetTransform * Matrix.CreateRotationX((float)Math.PI / 2) * transforms[mesh.ParentBone.Index] * world;
                    effect.View = game.Camera.View;
                    effect.Projection = game.Camera.Projection;
                }
                mesh.Draw();
            }
        }

        #endregion
    }
}
