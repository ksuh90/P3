using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Content;

namespace PrisonStep
{
    /// <summary>
    /// This class describes our player in the game. 
    /// </summary>
    public class Player
    {
        #region Fields

        private enum States { Start, StanceStartRaised, StanceRaised, WalkLoopStartRaised, WalkLoopRaised, StanceStartFiringPosition, StanceFiringPosition, StanceStartCrouch, StanceCrouch }
        private States state = States.Start;

        /// <summary>
        /// Our animated model
        /// </summary>
        private Victoria victoria;

        private Bazooka bazooka;

        /// <summary>
        /// Game that uses this player
        /// </summary>
        private PrisonGame game;

        KeyboardState lastKeyboardState = Keyboard.GetState();
        GamePadState lastGamePadState = GamePad.GetState(PlayerIndex.One);
        //
        // Player location information.  We keep a x/z location (y stays zero)
        // and an orientation (which way we are looking).
        //

        /// <summary>
        /// Player location in the prison. Only x/z are important. y still stay zero
        /// unless we add some flying or jumping behavior later on.
        /// </summary>
        private Vector3 location = new Vector3(275, 0, 1053);
        public Vector3 Location { get { return location; } set { location = value; } }
        /// <summary>
        /// The player orientation as a simple angle
        /// </summary>
        private float orientation = 1.6f;

        /// <summary>
        /// The player transformation matrix. Places the player where they need to be.
        /// </summary>
        private Matrix transform;

        /// <summary>
        /// The rotation rate in radians per second when player is rotating
        /// </summary>
        private const float panRate = 2;

        private const float moveCoefficient = 2f;

        private CollisionDetection collisionDetector = new CollisionDetection();

        #endregion

        private float GetDesiredSpeed(ref KeyboardState keyboardState, ref GamePadState gamePadState)
        {
            if (keyboardState.IsKeyDown(Keys.Up))
                return moveCoefficient*1;

            float speed = gamePadState.ThumbSticks.Left.Y;

            // I'm not allowing you to walk backwards
            if (speed < 0)
                speed = 0;

            return moveCoefficient*speed;
        }

        private float GetDesiredTurnRate(ref KeyboardState keyboardState, ref GamePadState gamePadState)
        {
            if (keyboardState.IsKeyDown(Keys.Left))
            {
                return panRate;
            }

            if (keyboardState.IsKeyDown(Keys.Right))
            {
                return -panRate;
            }

            return -gamePadState.ThumbSticks.Right.X * panRate;
        }

        private float GetDesiredShootingAngleY(ref KeyboardState keyboardState, ref GamePadState gamePadState)
        {
            return .5f*gamePadState.ThumbSticks.Right.Y;
        }
        
        public Player(PrisonGame game)
        {
            this.game = game;
            victoria = new Victoria(game, "Victoria");
            bazooka = new Bazooka(game);
            victoria.AddAssetClip("walk", "Victoria-walk");
            victoria.AddAssetClip("walkstart", "Victoria-walkstartbazooka");
            victoria.AddAssetClip("walkloop", "Victoria-walkloopbazooka");
            victoria.AddAssetClip("raisebazooka", "Victoria-raisebazooka");
            victoria.AddAssetClip("lowerbazooka", "Victoria-lowerbazooka");
            victoria.AddAssetClip("crouchbazooka", "Victoria-crouchbazooka");

            SetPlayerTransform();

            //game.Camera.Eye = location + new Vector3(-300 * (float)Math.Sin(orientation), 125, -300 * (float)Math.Cos(orientation));

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
            victoria.LoadContent(content);
            bazooka.LoadContent(content);
            collisionDetector.LoadContent(content);
            AnimationPlayer player = victoria.PlayClip("walk");
        }

        public Vector3 getLocation()
        {
            return location;
        }

        public Vector3 getFacing()
        {
            return transform.Backward;
        }

        public void Update(GameTime gameTime)
        {
            double deltaTotal = gameTime.ElapsedGameTime.TotalSeconds;

            KeyboardState keyboardState = Keyboard.GetState();
            GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);
            float speed = 0;
            float turn = 0;

            //if (keyboardState.IsKeyDown(Keys.Q) && !lastKeyboardState.IsKeyDown(Keys.Q))
            //{
            //    victoria.SpineElevation += .1f;
            //}
            //if (keyboardState.IsKeyDown(Keys.A) && !lastKeyboardState.IsKeyDown(Keys.A))
            //{
            //    victoria.SpineElevation -= .1f;
            //}
            //if (keyboardState.IsKeyDown(Keys.W) && !lastKeyboardState.IsKeyDown(Keys.W))
            //{
            //    victoria.SpineAzimuth += .1f;
            //}
            //if (keyboardState.IsKeyDown(Keys.S) && !lastKeyboardState.IsKeyDown(Keys.S))
            //{
            //    victoria.SpineAzimuth -= .1f;
            //}

            //if (keyboardState.IsKeyDown(Keys.Q) && !lastKeyboardState.IsKeyDown(Keys.Q))
            //{
            //    location = new Vector3(1313, 0, -1440);
            //}



            do
            {
                double delta = deltaTotal;

                //
                // State machine will go here
                //
                switch (state)
                {
                    case States.Start:
                        state = States.StanceStartRaised;
                        delta = 0;
                        break;

                    /* Switching you from anything to the stance state */
                    case States.StanceStartRaised:
                        victoria.PlayClip("raisebazooka").Speed = 0;
                        state = States.StanceRaised;
                        location.Y = 0;
                        break;

                    /* You'll be just standing. But if the speed is greater than zero, etner walkstart */
                    case States.StanceRaised:
                        speed = GetDesiredSpeed(ref keyboardState, ref gamePadState);
                        turn = GetDesiredTurnRate(ref keyboardState, ref gamePadState);
                        if (speed > 0)
                        {
                            // We need to leave the stance state and start walking
                            victoria.PlayClip("walkstart");
                            victoria.Player.Speed = speed;
                            state = States.WalkLoopRaised;
                        }


                        if (gamePadState.IsButtonDown(Buttons.RightTrigger) && !lastGamePadState.IsButtonDown(Buttons.RightTrigger) || 
                            keyboardState.IsKeyDown(Keys.A) && !lastKeyboardState.IsKeyDown(Keys.A))
                        {
                            state = States.StanceStartFiringPosition;
                        }

                        if (gamePadState.IsButtonDown(Buttons.B) && !lastGamePadState.IsButtonDown(Buttons.B) ||
                            keyboardState.IsKeyDown(Keys.Z) && !lastKeyboardState.IsKeyDown(Keys.Z))
                        {
                            state = States.StanceStartCrouch;
                        }

                        break;

                    /* Walk loop by editing the speed as necessary */
                    case States.WalkLoopRaised:
                        //if (state == States.WalkStart) { victoria.PlayClip("lowerbazooka"); }
                        location.Y = 0;
                        if (delta > victoria.Player.Clip.Duration - victoria.Player.Time)
                        {
                            delta = victoria.Player.Clip.Duration - victoria.Player.Time;

                            // The clip is done after this update
                            state = States.WalkLoopStartRaised;
                        }


                        speed = GetDesiredSpeed(ref keyboardState, ref gamePadState);
                        if (speed == 0)
                        {
                            delta = 0;
                            state = States.StanceStartRaised;
                        }
                        else
                        {
                            victoria.Player.Speed = speed;
                        }

                        break;

                    /* Start the walk loop by beginning the clip and entering walk loop */
                    case States.WalkLoopStartRaised:
                        victoria.PlayClip("walkloop").Speed = GetDesiredSpeed(ref keyboardState, ref gamePadState);
                        state = States.WalkLoopRaised;
                        break;










                    /* Switching you from anything to the stance state */
                    case States.StanceStartFiringPosition:
                        victoria.PlayClip("lowerbazooka").Speed = 0;
                        state = States.StanceFiringPosition;
                        location.Y = 0;
                        break;

                    /* You'll be just standing. But if the speed is greater than zero, etner walkstart */
                    case States.StanceFiringPosition:
                        speed = GetDesiredSpeed(ref keyboardState, ref gamePadState);
                        turn = GetDesiredTurnRate(ref keyboardState, ref gamePadState);
                        victoria.SpineElevation = GetDesiredShootingAngleY(ref keyboardState, ref gamePadState);
                        if (speed > 0)
                        {
                            //If you move, automatically put in non-firing position
                            state = States.StanceStartRaised;
                            victoria.SpineElevation = 0f;
                        }
                        if (gamePadState.IsButtonDown(Buttons.LeftTrigger) && !lastGamePadState.IsButtonDown(Buttons.LeftTrigger) ||
                            keyboardState.IsKeyDown(Keys.Q) && !lastKeyboardState.IsKeyDown(Keys.Q))
                        {
                            bazooka.Shoot(Matrix.CreateRotationX(MathHelper.ToRadians(109.4f))*
                Matrix.CreateRotationY(MathHelper.ToRadians(9.7f))*
                Matrix.CreateRotationZ(MathHelper.ToRadians(72.9f))*
                Matrix.CreateTranslation(-9.6f,11.85f,21.1f)*
                victoria.GetHandTransform()*
                transform);
                        }
                        break;



                    /* Switching you from anything to the stance state */
                    case States.StanceStartCrouch:
                        state = States.StanceCrouch;
                        victoria.PlayClip("crouchbazooka");
                        location.Y = 0;
                        break;

                    /* You'll be just standing. But if the speed is greater than zero, etner walkstart */
                    case States.StanceCrouch:
                        speed = GetDesiredSpeed(ref keyboardState, ref gamePadState);
                        turn = GetDesiredTurnRate(ref keyboardState, ref gamePadState);
   
                        if (delta > victoria.Player.Clip.Duration - victoria.Player.Time)
                        {
                            delta = victoria.Player.Clip.Duration - victoria.Player.Time;

                            // The clip is done after this update
                            state = States.StanceStartRaised;
                        }
                        break;



                }

                // 
                // State update
                //

                victoria.Update(delta);

                bazooka.Update(delta);

                #region ComputeNewOrientation
                // Enable turning while walking
                orientation += GetDesiredTurnRate(ref keyboardState, ref gamePadState) * (float)delta;

                Matrix deltaMatrix = victoria.DeltaMatrix;
                float deltaAngle = (float)Math.Atan2(deltaMatrix.Backward.X, deltaMatrix.Backward.Z);
                orientation += deltaAngle;
                #endregion ComputeNewOrientation

                #region ComputeNewLocation
                // We are likely rotated from the angle the model expects to be in
                // Determine that angle.
                Matrix rootMatrix = victoria.RootMatrix;
                float actualAngle = (float)Math.Atan2(rootMatrix.Backward.X, rootMatrix.Backward.Z);
                Vector3 newLocation = location + Vector3.TransformNormal(victoria.DeltaPosition,
                               Matrix.CreateRotationY(orientation - actualAngle));
                #endregion

                #region LocationCheck
                string region = collisionDetector.TestRegion(newLocation);
                //If region doesn't exist, don't allow you to go in it
                if (region.Equals(String.Empty))
                {
                }
                //If region is a closed door, don't allow you to go in it
                else if (region.StartsWith("R_Door") && !game.IsDoorOpen(region))
                {
                }
                //Otherwise, it's a valid location
                else
                {
                    //location is in the bounds of the map
                    location = newLocation;

                    // newCameraLocation may or may not be wihtin the bounds of the map
                    Vector3 idealCameraLocation = location + new Vector3(-300 * (float)Math.Sin(orientation), 125, -300 * (float)Math.Cos(orientation));

                    // Over the shoulder
                    Vector3 backupCameraLocation = location + new Vector3(-40 * (float)Math.Sin(orientation)+40, 160, -40 * (float)Math.Cos(orientation));

                    // checks if the camera is within the map
                    string cameraRegion = collisionDetector.TestRegion(idealCameraLocation);

                    // if camera is out of the map, zoom in a bit.
                    if (cameraRegion.Equals(String.Empty) || (cameraRegion.StartsWith("R_Door") && !game.IsDoorOpen(cameraRegion)))
                    {
                        game.Camera.DesiredEye = backupCameraLocation;
                    }
                    // if camera is in different region than victoria, use backup
                    else if (cameraRegion != region)
                    {
                        game.Camera.DesiredEye = backupCameraLocation;
                    }
                    else
                    {
                        game.Camera.DesiredEye = idealCameraLocation;
                    }
                }
                #endregion


                #region SlimeCheck
                //If player is in section1, stop sliming
                if (region.Equals("R_Section1"))
                {
                    game.StopSliming();
                    bazooka.Reload();
                }
                #endregion SlimeCheck

                #region DoorCheck
                //If player is facing the door and in the zone, open it
                if (region.Contains("R_Door") && game.PlayerIsFacing(this, region))
                {
                    game.OpenDoor(region);
                }

                //Close all doors if not in a doorway
                for (int i = 1; i <= 6; i++)
                {
                    if (!region.Contains("R_Door"))
                    {
                        game.CloseDoor("R_Door" + i.ToString());
                    }
                }
                #endregion DoorCheck

                SetPlayerTransform();

                deltaTotal -= delta;
            } while (deltaTotal > 0);

            //
            // Make the camera follow the player
            //
            //game.Camera.Eye = location + new Vector3(-300 * (float)Math.Sin(orientation), 125, -300 * (float)Math.Cos(orientation));
            game.Camera.Center = game.Camera.Eye + transform.Backward + new Vector3(0, -0.1f, 0);
           // game.Camera.ZNear = 250; // do this once

            lastKeyboardState = keyboardState;
            lastGamePadState = gamePadState;
        }

        public void Draw(GraphicsDeviceManager graphics, GameTime gameTime)
        {
            Matrix transform = Matrix.CreateRotationY(orientation);
            transform.Translation = location;

            victoria.Draw(graphics, gameTime, transform);

            Matrix bazMat =
                Matrix.CreateRotationX(MathHelper.ToRadians(109.4f))*
                Matrix.CreateRotationY(MathHelper.ToRadians(9.7f))*
                Matrix.CreateRotationZ(MathHelper.ToRadians(72.9f))*
                Matrix.CreateTranslation(-9.6f,11.85f,21.1f)*
                victoria.GetHandTransform()*
                transform;


            bazooka.Draw(graphics, gameTime, bazMat);

            if (state == States.StanceFiringPosition)
            {
                //Matrix crosshairTrans =
                //    Matrix.CreateRotationX(MathHelper.ToRadians(109.4f)) *
                //    Matrix.CreateRotationY(MathHelper.ToRadians(9.7f)) *
                //    Matrix.CreateRotationZ(MathHelper.ToRadians(72.9f)) *
                //    Matrix.CreateTranslation(-9.6f, 11.85f, 21.1f) *
                //    victoria.GetHandTransform() *
                //    transform;

                //bazooka.DrawCrosshair(graphics, gameTime, crosshairTrans);
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
            return victoria.TestSphereForCollision(sphere, location);
        }
    }
}
