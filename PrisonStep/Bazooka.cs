using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PrisonStep
{
    public class Bazooka
    {

        #region Fields

        private PrisonGame game;

        private Model model;
        private Queue<Pie> pies=new Queue<Pie>();

        #endregion

        #region Initialization Constructor Restart

        public Bazooka(PrisonGame game)
        {
            this.game = game;
            Reload();
        }

        public void Reload()
        {
            while (pies.Count < 10)
            {
                Pie addThisPie = new Pie(game);
                if (pies.Count % 3 == 0) { addThisPie.CurrPieType = Pie.PieType.Pie1; }
                if (pies.Count % 3 == 1) { addThisPie.CurrPieType = Pie.PieType.Pie2; }
                if (pies.Count % 3 == 2) { addThisPie.CurrPieType = Pie.PieType.Pie3; }
                pies.Enqueue(addThisPie);
            }
        }

        #endregion

        #region Functions


        /// <summary>
        /// This function is called to load content into this component
        /// of our game.
        /// </summary>
        /// <param name="content">The content manager to load from.</param>
        public void LoadContent(ContentManager content)
        {
            model = content.Load<Model>("PieBazooka");
        }

        public void Shoot(Matrix transform)
        {
            if(pies.Count>0)
            {
                Pie pie = pies.Dequeue();
                pie.CurrPieState = Pie.PieState.Shooting;
                pie.MyTransform = transform;
                pie.DirectionShooting = transform.Backward;
                game.ShootPie(pie);
            }
            //switch(pie.CurrPieType)
            //{
            //    case Pie.PieType.Pie1:
            //        pie = Pie.PieType.Pie2;
            //        break;

            //    case Pie.PieType.Pie2:
            //        pie = Pie.PieType.Pie3;
            //        break;

            //    case Pie.PieType.Pie3:
            //        pie = Pie.PieType.Pie1;
            //        break;
            //}
        }

        /// <summary>
        /// This function is called to update this component of our game
        /// to the current game time.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(double delta)
        {
        }

        /// <summary>
        /// This function is called to draw this game component.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="gameTime"></param>
        public void Draw(GraphicsDeviceManager graphics, GameTime gameTime, Matrix transform)
        {
            DrawModel(graphics, model, transform);
            foreach (Pie pie in pies)
            {
                pie.Draw(graphics, gameTime, transform);
            }
        }

        public void DrawCrosshair(GraphicsDeviceManager graphics, GameTime gameTime, Matrix transform)
        {
            game.LineDraw.Crosshair(transform.Translation, 20, Color.White);
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
                    effect.World = transforms[mesh.ParentBone.Index] * world;
                    effect.View = game.Camera.View;
                    effect.Projection = game.Camera.Projection;
                }
                mesh.Draw();
            }
        }

        #endregion
    }
}
