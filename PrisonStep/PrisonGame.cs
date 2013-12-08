using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace PrisonStep
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class PrisonGame : Microsoft.Xna.Framework.Game
    {
        #region Fields
        private SpriteFont scoreFont;

        private int score;

        public int Score
        {
            get { return score; }
            set { score = value; }
        }
        private SpriteBatch spriteBatch;

        public void DrawSprites(GameTime gameTime, SpriteBatch spriteBatch)
        {
            string scoreString = String.Format("{0:0000}", Score);
            spriteBatch.DrawString(scoreFont, scoreString, new Vector2(10, 10), Color.White);
        }

        private PSLineDraw lineDraw;
        public PSLineDraw LineDraw { get { return lineDraw; } }

        private CollisionDetection collisionDetector = new CollisionDetection();
        public CollisionDetection CollisionDetector { get { return collisionDetector; } }

        /// <summary>
        /// This graphics device we are drawing on in this assignment
        /// </summary>
        GraphicsDeviceManager graphics;
        public GraphicsDeviceManager Graphics { get { return graphics; } }

        /// <summary>
        /// The camera we use
        /// </summary>
        private Camera camera;

        /// <summary>
        /// The player in your game is modeled with this class
        /// </summary>
        private Player player;
        public Player Player{ get { return player; } }

        private Dalek alien1;
        public Dalek Alien1 { get { return alien1; } }
        private AlienParent alien2;
        public AlienParent Alien2 { get { return alien2; } }

        private Model pieModel;
        public Model PieModel { get { return pieModel; } }

        private Model spitModel;
        public Model SpitModel { get { return spitModel; } }

        /// <summary>
        /// This is the actual model we are using for the prison
        /// </summary>
        private List<PrisonModel> phibesModel = new List<PrisonModel>();

        #endregion

        #region Properties

        /// <summary>
        /// The game camera
        /// </summary>
        public Camera Camera { get { return camera; } }

        #endregion

        private List<Pie> shootingPies = new List<Pie>();
        private List<Spit> shootingSpits = new List<Spit>();

        public void ShootPie(Pie pie)
        {
            shootingPies.Add(pie);
        }

        public void RemovePie(Pie pie)
        {
            shootingPiesToRemove.Add(pie);
        }

        public void RemoveSpit(Spit spit)
        {
            shootingSpits.Remove(spit);
        }

        public void ShootSpit(Spit spit)
        {
            shootingSpits.Add(spit);
        }


        public void StartSliming()
        {
            m_sliming = SlimingState.Sliming;
        }

        public void StopSliming()
        {
            m_sliming = SlimingState.Unsliming;
        }

        public enum SlimingState { Unslimed, Sliming, Slimed, Unsliming };

        private double m_slimeTime = 1;
        public double SlimeTime { get { return m_slimeTime; } set { m_slimeTime = value; } }

        private SlimingState m_sliming = SlimingState.Unslimed;
        public SlimingState Sliming { get { return m_sliming; } set { m_sliming = value; } }
        /// <summary>
        /// Constructor
        /// </summary>
        public PrisonGame()
        {
            // XNA startup
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // Create objects for the parts of the ship
            for(int i=1;  i<=6;  i++)
            {
                phibesModel.Add(new PrisonModel(this, i));
            }

            // Create a player object
            player = new Player(this);
            alien1 = new Dalek(this, player);
            alien2 = new AlienParent(this);

            // Some basic setup for the display window
            this.IsMouseVisible = true;
			this.Window.AllowUserResizing = true;
			this.graphics.PreferredBackBufferWidth = 1024;
			this.graphics.PreferredBackBufferHeight = 728;

            // Basic camera settings
            camera = new Camera(graphics);
            camera.Eye = new Vector3(800, 180, 1053);
            camera.DesiredEye = camera.Eye;
            camera.Center = new Vector3(275, 90, 1053);
            camera.FieldOfView = MathHelper.ToRadians(42);

            lineDraw = new PSLineDraw(this, Camera);
            this.Components.Add(lineDraw);

        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            camera.Initialize();

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            scoreFont = Content.Load<SpriteFont>("scorefont");
            player.LoadContent(Content);
            alien1.LoadContent(Content);
            alien2.LoadContent(Content);
            collisionDetector.LoadContent(Content);
            pieModel = Content.Load<Model>("pies");
            spitModel = Content.Load<Model>("Spit");

            spriteBatch = new SpriteBatch(GraphicsDevice);
            foreach (PrisonModel model in phibesModel)
            {
                model.LoadContent(Content);
            }
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }


        List<Pie> shootingPiesToRemove = new List<Pie>();

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            double delta = gameTime.ElapsedGameTime.TotalSeconds;
            lineDraw.Clear();
            //lineDraw.Crosshair(new Vector3(0, 100, 0), 20, Color.White);
            //lineDraw.Begin();
            //lineDraw.Vertex(new Vector3(0, 150, 0), Color.White);
            //lineDraw.Vertex(new Vector3(50, 100, 0), Color.Red);
            //lineDraw.End();

                foreach (Pie pie in shootingPies)
                {
                    if (!pie.Update(delta, pie.MyTransform)) { shootingPiesToRemove.Add(pie); }
                }
                foreach (Pie pie in shootingPiesToRemove)
                {
                    shootingPies.Remove(pie);
                }
                shootingPiesToRemove.Clear();

            List<Spit> spitsToRemove = new List<Spit>();
            foreach (Spit spit in shootingSpits)
            {
                if (!spit.Update(delta)) { spitsToRemove.Add(spit); };
            }
            foreach (Spit spit in spitsToRemove)
            {
                shootingSpits.Remove(spit);
            }
            spitsToRemove.Clear();

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                Keyboard.GetState().IsKeyDown(Keys.Escape))
                this.Exit();

            //
            // Update game components
            //

            if (m_sliming == SlimingState.Sliming)
            {
                m_slimeTime -= delta;
                if (m_slimeTime <= -1)
                {
                    m_sliming = SlimingState.Slimed;
                    m_slimeTime = -1;
                }
            }
            if (m_sliming == SlimingState.Unsliming)
            {
                m_slimeTime += delta;
                if (m_slimeTime >= 1)
                {
                    m_sliming = SlimingState.Unslimed;
                    m_slimeTime = 1;
                }
            }


            player.Update(gameTime);
            alien1.Update(gameTime);
            alien2.Update(gameTime);

            foreach (PrisonModel model in phibesModel)
            {
                model.Update(gameTime);
            }

            camera.Update(gameTime);

            base.Update(gameTime);
        }

        public float MostSignificant(Vector3 inn)
        {
            if (Math.Abs(inn.X) > Math.Abs(inn.Z)) { return inn.X; }
            else { return inn.Z; }
        }

        public bool PlayerIsFacing(Player player, string door)
        {
            Vector3 doorLocation = Vector3.Zero;
            foreach (PrisonModel model in phibesModel)
            {
                Vector3 coords = model.GetDoorLocation(door);
                if (coords != Vector3.Zero)
                {
                    doorLocation = coords;
                }
            }

            Vector3 playerLocation = player.getLocation();

            Vector3 playerFacing = player.getFacing();
            playerFacing.Normalize();

            Vector3 doorDirectionFromPlayer = doorLocation - playerLocation;
            doorDirectionFromPlayer.Normalize();

            int playerFacingSign = (int)Math.Round(MostSignificant(playerFacing));
            int appopriateFacingSign = (int)Math.Round(MostSignificant(doorDirectionFromPlayer));

            return playerFacingSign == appopriateFacingSign;
        }

        public void OpenDoor(string door)
        {
            foreach (PrisonModel model in phibesModel)
            {
                model.OpenDoor(door);
            }
        }

        public void CloseDoor(string door)
        {
            foreach (PrisonModel model in phibesModel)
            {
                model.CloseDoor(door);
            }
        }

        public bool IsDoorOpen(string door)
        {
            foreach (PrisonModel model in phibesModel)
            {
                if(model.IsDoorOpen(door)) return true;
            }
            return false;
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.Black);

            foreach (PrisonModel model in phibesModel)
            {
                model.Draw(graphics, gameTime);
            }

                foreach (Pie pie in shootingPies)
                {
                    pie.Draw(graphics, gameTime, pie.MyTransform);
                }
         

            player.Draw(graphics, gameTime);
            alien1.Draw(graphics, gameTime);
            alien2.Draw(graphics, gameTime);

            #region DrawScore
            spriteBatch.Begin();
            DrawSprites(gameTime, spriteBatch);
            spriteBatch.End();
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            #endregion

            #region DrawSpit
            foreach (Spit spit in shootingSpits)
            {
                spit.Draw(graphics, gameTime, spit.MyTransform);
            }
            #endregion

            base.Draw(gameTime);
        }
    }
}
