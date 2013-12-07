#region

using Microsoft.Xna.Framework;

#endregion

namespace PrisonStep
{
    /// <summary>
    /// This is the main camera
    /// </summary>
    public class Camera
    {
        #region Fields

        private readonly GraphicsDeviceManager graphics;

        /// <summary>
        /// The location we are looking at in space.
        /// </summary>
        private Vector3 center = new Vector3(0, 0, 0);

        private Vector3 desiredEye = Vector3.Zero;

        /// <summary>
        /// The up direction
        /// </summary>
        private Vector3 desiredUp = new Vector3(0, 1, 0);

        /// <summary>
        /// The eye position in space
        /// </summary>
        private Vector3 eye = new Vector3(1000, 1000, 1000);

        private float eyeSpringDamping = 60;
        private float eyeSpringStiffness = 100;

        /// <summary>
        /// Field of view
        /// </summary>
        private float fov = MathHelper.ToRadians(35);

        private Matrix projection;

        /// <summary>
        /// The up direction
        /// </summary>
        private Vector3 up = new Vector3(0, 1, 0);

        private float upSpringDamping = 60;
        private float upSpringStiffness = 100;
        private Vector3 velocity = Vector3.Zero;
        private Matrix view;

        /// <summary>
        /// Far cut-off
        /// </summary>
        private float zfar = 10000;

        /// <summary>
        /// Near cut-off
        /// </summary>
        private float znear = 10;

        #endregion

        #region Constructor and Initialize

        /// <summary>
        /// Constructor. Initializes the graphics field from a passed parameter.
        /// </summary>
        /// <param name="graphics">The graphics device manager for our program</param>
        public Camera(GraphicsDeviceManager graphics)
        {
            this.graphics = graphics;
        }

        public void Initialize()
        {
            ComputeView();
            ComputeProjection();
            Center = new Vector3(0, 0, 1);
        }

        #endregion

        #region Functions

        public void Update(GameTime gameTime)
        {

            {
                // Calculate spring force
                Vector3 stretch = desiredEye - eye;
                Vector3 acceleration = stretch*eyeSpringStiffness - velocity*eyeSpringDamping;

                // Apply acceleration
                velocity += acceleration*(float) gameTime.ElapsedGameTime.TotalSeconds;

                // Apply velocity
                eye += velocity*(float) gameTime.ElapsedGameTime.TotalSeconds;
            }
            {
                // Calculate spring force
                Vector3 stretch = desiredUp - up;
                Vector3 acceleration = stretch*upSpringStiffness - velocity*upSpringDamping;

                // Apply acceleration
                velocity += acceleration*(float) gameTime.ElapsedGameTime.TotalSeconds;

                // Apply velocity
                up += velocity*(float) gameTime.ElapsedGameTime.TotalSeconds;
            }
        }

        private void ComputeView()
        {
            view = Matrix.CreateLookAt(eye, center, up);
        }

        private void ComputeProjection()
        {
            projection = Matrix.CreatePerspectiveFieldOfView(fov,
                graphics.GraphicsDevice.Viewport.AspectRatio, znear, zfar);
        }

        #endregion

        #region Properties

        public Matrix View
        {
            get { return view; }
        }

        public Matrix Projection
        {
            get { return projection; }
        }

        public Vector3 Center
        {
            get { return center; }
            set
            {
                center = value;
                ComputeView();
            }
        }

        public float FieldOfView
        {
            get { return fov; }
            set
            {
                fov = value;
                ComputeView();
            }
        }

        public Vector3 Eye
        {
            get { return eye; }
            set
            {
                eye = value;
                ComputeView();
            }
        }

        public Vector3 DesiredEye
        {
            get { return desiredEye; }
            set { desiredEye = value; }
        }

        public Vector3 DesiredUp
        {
            get { return desiredUp; }
            set { desiredUp = value; }
        }

        public float Stiffness
        {
            get { return eyeSpringStiffness; }
            set { eyeSpringStiffness = value; }
        }

        public float Damping
        {
            get { return eyeSpringDamping; }
            set { eyeSpringDamping = value; }
        }

        #endregion
    }
}