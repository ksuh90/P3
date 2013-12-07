using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace PrisonStep
{
    /// <summary>
    /// This class describes our player in the game. 
    /// </summary>
    public class AlienParent
    {
        #region Fields
        private enum AlienStates { Start, StanceStart, Stance, WalkLoopStart, WalkLoop, StartEatPie, EatPie }
        private AlienStates state = AlienStates.Start;

        /// <summary>
        /// Our animated model
        /// </summary>
        private Alien alien;

        /// <summary>
        /// Game that uses this player
        /// </summary>
        private PrisonGame game;

        /// <summary>
        /// Player location in the prison. Only x/z are important. y still stay zero
        /// unless we add some flying or jumping behavior later on.
        /// </summary>
        private Vector3 location = new Vector3(1313, 0, -1540);
        public Vector3 Location { get { return location; } set { location = value; } }
        /// <summary>
        /// The player orientation as a simple angle
        /// </summary>
        private float orientation = -1.6f;

        /// <summary>
        /// The player transformation matrix. Places the player where they need to be.
        /// </summary>
        private Matrix transform;

        private CollisionDetection collisionDetector = new CollisionDetection();

        #endregion

        private float GetDesiredSpeed()
        {
            return 1;
        }

        private float GetDesiredTurnRate()
        {
            return 1;
        }
        public AlienParent(PrisonGame game)
        {
            this.game = game;
            alien = new Alien(game, "Alien");
            alien.AddAssetClip("catcheat", "Alien-catcheat");
            alien.AddAssetClip("ob", "Alien-ob");
            alien.AddAssetClip("stance", "Alien-stance");
            alien.AddAssetClip("trantrum", "Alien-trantrum");
            alien.AddAssetClip("walkloop", "Alien-walkloop");
            alien.AddAssetClip("walkstart", "Alien-walkstart");   

            SetPlayerTransform();
        }

        /// <summary>
        /// Set the value of transform to match the current location
        /// and orientation.
        /// </summary>
        private void SetPlayerTransform()
        {
            transform = Matrix.CreateRotationY(orientation);
            transform.Translation = location;
        }

        public void LoadContent(ContentManager content)
        {
            alien.LoadContent(content);
            collisionDetector.LoadContent(content);
            AnimationPlayer player = alien.PlayClip("walkstart");
        }

        public Vector3 GetLocation()
        {
            return location;
        }

        public Vector3 GetFacing()
        {
            return transform.Backward;
        }

        private Pie myPie = null;

        public void AddPie(Pie pie)
        {
            myPie = pie;
        }

        public void Update(GameTime gameTime)
        {
            double deltaTotal = gameTime.ElapsedGameTime.TotalSeconds;

            float speed = 0;
            float turn = 0;

            do
            {
                double delta = deltaTotal;

                if (myPie != null&&state!=AlienStates.EatPie) { state = AlienStates.StartEatPie; }

                //
                // State machine will go here
                //
                switch (state)
                {
                    case AlienStates.Start:
                        state = AlienStates.StanceStart;
                        delta = 0;
                        break;

                    /* Switching you from anything to the stance state */
                    case AlienStates.StanceStart:
                        alien.PlayClip("stance").Speed = 0;
                        state = AlienStates.Stance;
                        location.Y = 0;
                        break;

                    /* You'll be just standing. But if the speed is greater than zero, etner walkstart */
                    case AlienStates.Stance:
                        speed = GetDesiredSpeed(  );
                        turn = GetDesiredTurnRate(  );
                        if (speed > 0)
                        {
                            // We need to leave the stance state and start walking
                            alien.PlayClip("walkstart");
                            alien.Player.Speed = speed;
                            state = AlienStates.WalkLoop;
                        }
                        break;

                    /* Walk loop by editing the speed as necessary */
                    case AlienStates.WalkLoop:
                        //if (state == States.WalkStart) { victoria.PlayClip("lowerbazooka"); }
                        location.Y = 0;
                        if (delta > alien.Player.Clip.Duration - alien.Player.Time)
                        {
                            delta = alien.Player.Clip.Duration - alien.Player.Time;

                            // The clip is done after this update
                            state = AlienStates.WalkLoopStart;
                        }
                        speed = GetDesiredSpeed();
                        if (speed == 0)
                        {
                            delta = 0;
                            state = AlienStates.StanceStart;
                        }
                        else
                        {
                            alien.Player.Speed = speed;
                        }
                        break;

                    /* Start the walk loop by beginning the clip and entering walk loop */
                    case AlienStates.WalkLoopStart:
                        alien.PlayClip("walkloop").Speed = GetDesiredSpeed();
                        state = AlienStates.WalkLoop;
                        break;

                    case AlienStates.StartEatPie:
                        alien.PlayClip("catcheat");
                        state = AlienStates.EatPie;
                        location.Y = 0;
                        delta = 0;
                        break;

                    case AlienStates.EatPie:
                        if (alien.Player.Time > 2f)
                        {
                            myPie = null;
                        }
                        if (delta > alien.Player.Clip.Duration - alien.Player.Time)
                        {
                            delta = alien.Player.Clip.Duration - alien.Player.Time;

                            myPie = null;
                            // The clip is done after this update
                            state = AlienStates.WalkLoopStart;
                        }
                        
                        break;
                }

                // 
                // State update
                //

                alien.Update(delta);

                #region ComputeNewOrientation
                // Enable turning while walking
                float OrientationAdd1 = GetDesiredTurnRate() * (float)delta;

              //  Matrix deltaMatrix = alien.DeltaMatrix;
              //  float OrientationAdd2 = (float)Math.Atan2(deltaMatrix.Backward.X, deltaMatrix.Backward.Z);
                orientation += OrientationAdd1;// +OrientationAdd2;

                #endregion ComputeNewOrientation

                #region ComputeNewLocation
                // We are likely rotated from the angle the model expects to be in
                // Determine that angle.
                Matrix rootMatrix = alien.RootMatrix;
                float actualAngle = (float)Math.Atan2(rootMatrix.Backward.X, rootMatrix.Backward.Z);
                Vector3 newLocation = location + Vector3.TransformNormal(alien.DeltaPosition,
                               Matrix.CreateRotationY(orientation - actualAngle));
                #endregion

                #region LocationCheck
                string region = collisionDetector.TestRegion(newLocation);
                //If region doesn't exist, don't allow you to go in it
                if (region.Equals(String.Empty))
                {
                }
                //Otherwise, it's a valid location
                else
                {
                    //location is in the bounds of the map
                    location = newLocation;
                }
                #endregion


                SetPlayerTransform();

                deltaTotal -= delta;
            } while (deltaTotal > 0);
        }

        public void Draw(GraphicsDeviceManager graphics, GameTime gameTime)
        {
            Matrix transform = Matrix.CreateRotationY(orientation);
            transform.Translation = location;

            alien.Draw(graphics, gameTime, transform);

            if (myPie != null)
            {
                Matrix pieMat = alien.GetHandTransform() *
                    transform;
                myPie.Draw(graphics, gameTime, pieMat);
            }
        }
        /// <summary>
        /// Tests a laser point to see if it is in the bounding sphere of any of 
        /// our asteroids.  If so, it deletes asteroid and
        /// returns true.
        /// </summary>
        /// <param name="position">Tip of the laser</param>
        /// <returns></returns>
        public bool TestSphereForCollision(BoundingSphere sphere)
        {
            return alien.TestSphereForCollision(sphere, location);
        }
    }
}
