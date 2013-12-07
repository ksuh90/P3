using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace PrisonStep
{
    public class Dalek
    {
        #region Fields

        private PrisonGame game;
        private Model model;
        private Random random = new Random();
        private Vector3 position = new Vector3(1313, 0, -1440);
        private Quaternion orientation = Quaternion.Identity;
        private CollisionDetection collisionDetector = new CollisionDetection();


        private int head,
            plungerArm,
            arm2,
            eye;

        private float headRotZ = 0;
        private float plungerArmRotZ = 0;
        private float plungerArmRotY = 0;
        private float eyeRotX = 0;


        
        private float signOfTurnRate=1;

        private Player player;

        private float stun = 0f;

        public void Stun()
        {
            stun=5f;
        }

        public bool TestSphereForCollision(BoundingSphere sphere)
        {

            // Obtain a bounding sphere for the asteroid.  I can get away
            // with this here because I know the model has exactly one mesh
            // and exactly one bone.
            BoundingSphere bs = model.Meshes[0].BoundingSphere;
            bs = bs.Transform(model.Bones[0].Transform);

            // Move this to world coordinates.  Note how easy it is to 
            // transform a bounding sphere
            // bs.Radius *= asteroid.size;
            bs.Center += position;
            bs.Radius = 80;

            sphere.Center.Y = 0;
            bs.Center.Y = 0;

            if (sphere.Intersects(bs))
            {
                return true;
            }


            return false;
        }


        //private float signOfPitchRate;
        private float turnRate;
        //private float pitchRate;
        //private const float MaxPitchRate = 1;
        private const float MaxTurnRate = (float)Math.PI;
        private const float Drag = 1f;
        private const float MaxThrust = 100;
        //private float wingAngle;
        //private bool wingsGoingDown;
        private float speed;
        private float thrust;
        private float timeSinceLastTurnCheck;
        private float timeSinceLastSpit;

        //// Walking Related
        //private float walkingStateTime = 0f;
        //private enum WalkingState { LegsMovingBack, LegsMovingForward };
        //private WalkingState walkingState = WalkingState.LegsMovingBack;
        //private enum LocustState { Walking, Flying };
        //private float locustStateTime = 0f;
        //private float legsDegree = 0;

        #endregion

        #region Constructor and Initialize

        /// <summary>
        /// Initializes a new instance of the <see cref="Locust" /> class.
        /// This function creates Bat, given the game supplied
        /// </summary>
        /// <param name="game"></param>
        public Dalek(PrisonGame p_game, Player p_player)
        {
            game = p_game;
            player = p_player;
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
            model = content.Load<Model>("Dalek");
            collisionDetector.LoadContent(content);
            head = model.Bones.IndexOf(model.Bones["Head"]);
            plungerArm = model.Bones.IndexOf(model.Bones["PlungerArm"]);
            arm2 = model.Bones.IndexOf(model.Bones["Arm2"]);
            eye = model.Bones.IndexOf(model.Bones["Eye"]);
        }

        /// <summary>
        /// This function is called to update this component of our game
        /// to the current game time.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            // Calculate time since last update
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (stun > 0)
            {
                stun -= delta;
                if (stun < 0) stun = 0;
                return;
            }
            AutoPilot(delta);

            FaceVictoria();
            timeSinceLastSpit += delta;
            if (timeSinceLastSpit>=2f&&IsCloseToVictoria())
            {
                SpitAtVictoria();
                timeSinceLastSpit = 0;
            }


        }

        public void SpitAtVictoria()
        {
            Spit dalekSpit = new Spit(game);

            Vector3 playerLoc = player.Location;
            Vector3 myLoc = position;
            Vector3 pointAtPlayerFromMyFront = (myLoc - playerLoc);
            pointAtPlayerFromMyFront.Normalize();
            float frontToPlayer = (float)Math.Atan2(pointAtPlayerFromMyFront.X, pointAtPlayerFromMyFront.Z);


            dalekSpit.MyTransform = Matrix.CreateRotationY((float)Math.PI+frontToPlayer) * Matrix.CreateTranslation(position + new Vector3(0, 120, 0));

            dalekSpit.DirectionShooting = dalekSpit.MyTransform.Backward;
            game.ShootSpit(dalekSpit);
        }

        public bool IsCloseToVictoria()
        {
            Vector3 playerLocation = player.Location;
            Vector3 myLocation = position;
            float distance = Vector3.Distance(playerLocation, myLocation);
            if (distance < 200)
            {
                return true;
            }
            return false;
        }

        public void FaceVictoria()
        {
            Vector3 playerLoc = player.Location;
            Vector3 myLoc = position;
            Vector3 pointAtPlayerFromMyFront = (myLoc - playerLoc);
            pointAtPlayerFromMyFront.Normalize();
            float frontToPlayer = (float)Math.Atan2(pointAtPlayerFromMyFront.X, pointAtPlayerFromMyFront.Z);

            Matrix myOrientation = Matrix.CreateFromQuaternion(orientation);
            Vector3 origFront = myOrientation.Backward;
            float frontToFixed = -(float)Math.Atan2(origFront.X, origFront.Z);

            headRotZ = (float)Math.PI +frontToPlayer +frontToFixed;
            plungerArmRotZ = (float)Math.PI + frontToPlayer + frontToFixed;
        }

        /// <summary>
        /// This function is called to draw this game component.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="gameTime"></param>
        public void Draw(GraphicsDeviceManager graphics, GameTime gameTime)
        {
            DrawModel(graphics, model, Transform);
        }

        /// <summary>
        /// Draws the specific model.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="model"></param>
        /// <param name="world"></param>
        private void DrawModel(GraphicsDeviceManager graphics, Model model, Matrix world)
        {
            Matrix[] transforms = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transforms);

            int count = model.Bones.Count;
            for (int i = 0; i < count; i++)
            {
                
                if (i == head)
                {
                    transforms[i] = Matrix.CreateRotationZ(headRotZ);
                }
                else if (i == plungerArm)
                {
                    transforms[i] = Matrix.CreateRotationY(plungerArmRotY);
                    transforms[i] = Matrix.CreateRotationZ(plungerArmRotZ);
                }
                //else if (i == arm2)
                //{
                //    transforms[i] = Matrix.CreateRotationY(arm2Rot);
                //}
                else if (i == eye)
                {
                    transforms[i] = Matrix.CreateRotationX(eyeRotX);
                }
                else
                {
                    transforms[i] = Matrix.Identity;
                }

                ModelBone bone = model.Bones[i];
                if (bone.Parent == null)
                {
                    transforms[i] *= bone.Transform;
                }
                else
                {
                    transforms[i] *= bone.Transform * transforms[bone.Parent.Index];
                }
            }

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    // Effects are the shadowings
                    effect.EnableDefaultLighting();
                    effect.World = transforms[mesh.ParentBone.Index] * world;
                    effect.View = game.Camera.View;
                    effect.Projection = game.Camera.Projection;
                }
                mesh.Draw();
            }
        }

        #endregion

        #region AutoPilot

        public void AutoPilot(float delta)
        {
            //    timeSinceLastTurnCheck += delta;

            #region RandomizeThurst

            //Get a random float
            float randomThrust = (float)random.NextDouble();

            // If the float is less than .1, make it .1 so it doesn't just stop
            if (randomThrust < .1f) randomThrust = .1f;

            //Assign the randomThrust to the thrust
            thrust = randomThrust;

            #endregion

            //    #region UpdateWings

            //    // The furthest out the wings can flap
            //    const float MaxAngle = 2f;

            //    // The closest in the wings can flap
            //    const float MinAngle = 0f;

            //    // The total distance (radians) that the wings are moving
            //    const float DeploymentAngle = MaxAngle - MinAngle;

            //    // The amount of time in which it should move the total distance
            //    const float DeploymentTime = .2f;

            //    if // The wings should be opening
            //        (wingsGoingDown && wingAngle < MaxAngle)
            //    {
            //        wingAngle += (float)(DeploymentAngle * delta / DeploymentTime);
            //    }
            //    else if // The wings should be closing
            //        (!wingsGoingDown && wingAngle > -MinAngle)
            //    {
            //        wingAngle -= (float)(DeploymentAngle * delta / DeploymentTime);
            //    }
            //    else // The wings have hit the end; they need to go the other way
            //    {
            //        wingsGoingDown = !wingsGoingDown;
            //    }

            //    #endregion

            #region RandomizeTurnRate

            // 10% chance every 1/2 second for the sign to be flipped (this may vary system to system)
            timeSinceLastTurnCheck += delta;
            if (timeSinceLastTurnCheck >= .5f)
            {
                timeSinceLastTurnCheck = 0;
                if (random.Next(5) == 1)
                {
                    signOfTurnRate = -signOfTurnRate;
                }
            }

            // Get a random float to be the turnRate
            float randomTurnRate = (float)random.NextDouble();

            // Make the turnRate the randomTurnRate but the same sign as it should be
            turnRate = randomTurnRate * signOfTurnRate;

            #endregion


            #region UpdateOrientation

            // Get the amout to which the locust should be turning
            float turnAngle = turnRate * MaxTurnRate * delta;

            // Create a new orientation vector based where the locust is facing
            orientation *= Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), turnAngle);// *
             //   Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), pitchRate * MaxPitchRate * delta) *
               // Quaternion.CreateFromAxisAngle(new Vector3(0, 0, 1), -turnAngle);

            // Normalize the orientation to prevent round-off error
            orientation.Normalize();

            #endregion

            #region UpdatePosition

            // Get the current acceleration of the locust
            float acceleration = thrust * MaxThrust - Drag * speed;

            // Update the speed of the locust
            speed += acceleration * delta;

            // Depending on where the locust is facing, update the transform
            Matrix transform = Matrix.CreateFromQuaternion(orientation);

            // Get the thrust to which the locust is facing
            Vector3 directedThrust = Vector3.TransformNormal(new Vector3(0, 0, 1), transform);

            // Update the position based on the thrust, speed, and time since last update
            Vector3 newPosition = position + directedThrust * speed * delta;






            #region UpdatePosition
            //Vector3 newPosition = position + delta * new Vector3(100, 0, 0);
            string region = collisionDetector.TestRegion(newPosition);
            //If region doesn't exist, don't allow you to go in it
            if (region.Equals(String.Empty))
            {

            }//NTOE IT GOES THRU DOORS
            else//Otherwise, it's a valid location
            {
                //location is in the bounds of the map
                position = newPosition;
            }
            //position.Y = 0;
            #endregion




            #endregion
        }

        //    if (locustStateTime > 15f)
        //    {
        //        locustState = LocustState.Walking;
        //        locustStateTime = 0;
        //        wingAngle = 0;
        //        position.Y = 0;
        //        orientation = Quaternion.Identity;
        //        turnRate = 0;
        //        pitchRate = 0;
        //    }

        //}

        //public void WalkBySelf(float delta)
        //{
        //    walkingStateTime += delta;

        //    position.Z += 30 * delta;

        //    switch (walkingState)
        //    {
        //        case WalkingState.LegsMovingBack:
        //            {
        //                legsDegree -= delta;
        //                if (walkingStateTime > .2f)
        //                {
        //                    walkingState = WalkingState.LegsMovingForward;
        //                    walkingStateTime = 0;
        //                    legsDegree = 0;
        //                }
        //            }
        //            break;
        //        case WalkingState.LegsMovingForward:
        //            {
        //                legsDegree += delta;
        //                if (walkingStateTime > .2f)
        //                {
        //                    walkingState = WalkingState.LegsMovingBack;
        //                    walkingStateTime = 0;
        //                    legsDegree = .2f;
        //                }
        //            }
        //            break;
        //    }
        //    if (locustStateTime > 5f)
        //    {
        //        locustState = LocustState.Flying;
        //        locustStateTime = 0;
        //    }
        //}

        #endregion

        #region Properties

        //public bool WingsDeployed { get { return wingsGoingDown; } set { wingsGoingDown = value; } }

        //public float Thrust { get { return thrust; } set { thrust = value; } }

        //public float TurnRate { get { return turnRate; } set { turnRate = value; } }

        //public float PitchRate { get { return pitchRate; } set { pitchRate = value; } }

        //public Vector3 Position { get { return position; } set { position = value; } }

        /// <summary>
        /// Gets the underlying model
        /// </summary>
        public Model Model { get { return model; } }

        /// <summary>
        /// Gets the current transformation
        /// </summary>
        public Matrix Transform
        {
            get
            {
                return Matrix.CreateFromQuaternion(orientation) *
                        Matrix.CreateTranslation(position);
            }
        }

        #endregion
    }
}
