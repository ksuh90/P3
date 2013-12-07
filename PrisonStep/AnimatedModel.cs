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
    public class AnimatedModel
    {
        /// <summary>
        /// Reference to the game that uses this class
        /// </summary>
        protected PrisonGame game;

        /// <summary>
        /// The XNA model we will be animating
        /// </summary>
        protected Model model;

        Quaternion orientation = Quaternion.Identity;

        /// <summary>
        /// The number of skinning matrices in SkinnedEffect.fx. This must
        /// match the number in SkinnedEffect.fx.
        /// </summary>
        public const int NumSkinBones = 57;

        protected float spineElevation = 0;
        public float SpineElevation { get { return spineElevation; } set { spineElevation = value; } }
        protected float spineAzimuth = 0;
        public float SpineAzimuth { get { return spineAzimuth; } set { spineAzimuth = value; } }

        protected List<int> skelToBone = null;
        protected Matrix[] inverseBindTransforms = null;

        protected Matrix[] bindTransforms;
        protected Matrix[] boneTransforms;
        protected Matrix[] absoTransforms;
        protected Matrix[] skinTransforms = null;

        protected Matrix rootMatrixRaw = Matrix.Identity;
        protected Matrix deltaMatrix = Matrix.Identity;

        public Matrix DeltaMatrix { get { return deltaMatrix; } }
        public Vector3 DeltaPosition;
        public Matrix RootMatrix { get { return inverseBindTransforms[skelToBone[0]] * rootMatrixRaw; } }

        /// <summary>
        /// Access the current animation player
        /// </summary>
        public AnimationPlayer Player { get { return player; } }

        /// <summary>
        /// This class describes a single animation clip we load from
        /// an asset.
        /// </summary>
        protected class AssetClip
        {
            public AssetClip(string name, string asset)
            {
                Name = name;
                Asset = asset;
                TheClip = null;
            }

            public string Name { get; set; }
            public string Asset { get; set; }
            public AnimationClips.Clip TheClip { get; set; }
        }


        public Matrix GetHandTransform()
        {
            int handBone = model.Bones["Bip01 L Hand"].Index;

            return absoTransforms[handBone];
        }




        /// <summary>
        /// Tests a laser point to see if it is in the bounding sphere of any of 
        /// our asteroids.  If so, it deletes asteroid and
        /// returns true.
        /// </summary>
        /// <param name="position">Tip of the laser</param>
        /// <returns></returns>
        public bool TestSphereForCollision(BoundingSphere sphere, Vector3 location)
        {

            // Obtain a bounding sphere for the asteroid.  I can get away
            // with this here because I know the model has exactly one mesh
            // and exactly one bone.
            BoundingSphere bs = model.Meshes[0].BoundingSphere;
            bs = bs.Transform(model.Bones[0].Transform);

            // Move this to world coordinates.  Note how easy it is to 
            // transform a bounding sphere
            // bs.Radius *= asteroid.size;
            bs.Center += location;

            if (sphere.Intersects(bs))
            {
                return true;
            }


            return false;
        }

        /// <summary>
        /// Add an asset clip to the dictionary.
        /// </summary>
        /// <param name="name">Name we will use for the clip</param>
        /// <param name="asset">The FBX asset to load</param>
        public void AddAssetClip(string name, string asset)
        {
            assetClips[name] = new AssetClip(name, asset);
        }

        /// <summary>
        /// A dictionary that allows us to look up animation clips
        /// by name. 
        /// </summary>
        protected Dictionary<string, AssetClip> assetClips = new Dictionary<string, AssetClip>();

        /// <summary>
        /// Name of the asset we are going to load
        /// </summary>
        protected string asset;


        public AnimatedModel(PrisonGame game, string asset)
        {
            this.game = game;
            this.asset = asset;

            skinTransforms = new Matrix[57];
            for (int i = 0; i < skinTransforms.Length; i++)
            {
                skinTransforms[i] = Matrix.Identity;
            }
        }


        /// <summary>
        /// This function is called to load content into this component
        /// of our game.
        /// </summary>
        /// <param name="content">The content manager to load from.</param>
        public void LoadContent(ContentManager content)
        {
            model = content.Load<Model>(asset);

            int boneCnt = model.Bones.Count;
            bindTransforms = new Matrix[boneCnt];
            boneTransforms = new Matrix[boneCnt];
            absoTransforms = new Matrix[boneCnt];

            model.CopyBoneTransformsTo(bindTransforms);
            model.CopyBoneTransformsTo(boneTransforms);
            model.CopyAbsoluteBoneTransformsTo(absoTransforms);

            foreach (AssetClip clip in assetClips.Values)
            {
                Model clipmodel = content.Load<Model>(clip.Asset);
                AnimationClips modelclips = clipmodel.Tag as AnimationClips;
                clip.TheClip = modelclips.Clips["Take 001"];
            }

            AnimationClips clips = model.Tag as AnimationClips;
            if (clips != null && clips.SkelToBone.Count > 0)
            {
                skelToBone = clips.SkelToBone;

                inverseBindTransforms = new Matrix[boneCnt];
                skinTransforms = new Matrix[NumSkinBones];

                model.CopyAbsoluteBoneTransformsTo(inverseBindTransforms);

                for (int b = 0; b < inverseBindTransforms.Length; b++)
                    inverseBindTransforms[b] = Matrix.Invert(inverseBindTransforms[b]);

                for (int i = 0; i < skinTransforms.Length; i++)
                    skinTransforms[i] = Matrix.Identity;
            }
        }

        /// <summary>
        /// This function is called to update this component of our game
        /// to the current game time.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(double delta)
        {
            if (player != null)
            {
                // Update the clip
                player.Update(delta);

                for (int b = 0; b < player.BoneCount; b++)
                {
                    AnimationPlayer.IBone bone = player.GetBone(b);
                    if (!bone.Valid)
                        continue;

                    Vector3 scale = new Vector3(bindTransforms[b].Right.Length(),
                        bindTransforms[b].Up.Length(),
                        bindTransforms[b].Backward.Length());

                    boneTransforms[b] = Matrix.CreateScale(scale) *
                        Matrix.CreateFromQuaternion(bone.Rotation) *
                        Matrix.CreateTranslation(bone.Translation);
                }

                if (skelToBone != null)
                {
                    int rootBone = skelToBone[0];
                    
                    deltaMatrix = Matrix.Invert(rootMatrixRaw) * boneTransforms[rootBone];
                    DeltaPosition = boneTransforms[rootBone].Translation - rootMatrixRaw.Translation;

                    rootMatrixRaw = boneTransforms[rootBone];
                    boneTransforms[rootBone] = bindTransforms[rootBone];
                }

                model.CopyBoneTransformsFrom(boneTransforms);
            }

            model.CopyBoneTransformsFrom(boneTransforms);
            model.CopyAbsoluteBoneTransformsTo(absoTransforms);
        }

        protected AnimationPlayer player = null;

        /// <summary>
        /// Play an animation clip on this model.
        /// </summary>
        /// <param name="name"></param>
        public AnimationPlayer PlayClip(string name)
        {
            if (name != "Take 001")
            {
                player = new AnimationPlayer(this, assetClips[name].TheClip);
                Update(0);
                return player;
            }

            player = null;

            AnimationClips clips = model.Tag as AnimationClips;
            if (clips != null)
            {
                player = new AnimationPlayer(this, clips.Clips[name]);
                Update(0);
            }

            return player;
        }

        /// <summary>
        /// This function is called to draw this game component.
        /// </summary>
        /// <param name="graphics">Device to draw the model on.</param>
        /// <param name="gameTime">Current game time.</param>
        /// <param name="transform">Transform that puts the model where we want it.</param>
        public void Draw(GraphicsDeviceManager graphics, GameTime gameTime, Matrix transform)
        {
            DrawModel(graphics, model, transform);
        }




        private MouseState lastMouseState = Mouse.GetState();

        virtual protected void DrawModel(GraphicsDeviceManager graphics, Model model, Matrix world)
        {
            #region RayClickDetection
            MouseState currentMouseState = Mouse.GetState();
            if (lastMouseState.LeftButton == ButtonState.Released && currentMouseState.LeftButton == ButtonState.Pressed)
            {
                float mouseY = currentMouseState.Y;
                float mouseX = currentMouseState.X;
                
                // Only continue if you clicked in a valid region
                if (mouseY > 0 && 
                    mouseY < graphics.GraphicsDevice.Viewport.Height &&
                    mouseX > 0 &&
                    mouseX < graphics.GraphicsDevice.Viewport.Width)
                {
                    // Determine point on near clipping plane
                    Vector3 nearsource = new Vector3(mouseX, mouseY, 0);
                    Vector3 nearPoint = graphics.GraphicsDevice.Viewport.Unproject(nearsource, game.Camera.Projection, game.Camera.View, Matrix.Identity);

                    // Determine point on far clipping plane
                    Vector3 farsource = new Vector3(mouseX, mouseY, 1);
                    Vector3 farPoint = graphics.GraphicsDevice.Viewport.Unproject(farsource, game.Camera.Projection, game.Camera.View, Matrix.Identity);

                    // The direction of the click
                    Vector3 direction = farPoint - nearPoint;
                    direction.Normalize();

                    // Origin is where you clicked; headed towards the far point
                    Ray pickRay = new Ray(nearPoint, direction);

                    foreach (ModelMesh mesh in model.Meshes)
                    {
                        BoundingSphere boundingSphere = mesh.BoundingSphere;
                        boundingSphere = boundingSphere.Transform(world);
                        float? distance = pickRay.Intersects(boundingSphere);
                        if (distance != null)
                        {
                            Console.Out.WriteLine(distance);
                        }
                    }
                }
            }
            lastMouseState = currentMouseState;
            #endregion RayClickDetection

            if (skelToBone != null)
            {
                for (int b = 0; b < skelToBone.Count; b++)
                {
                    int n = skelToBone[b];
                    skinTransforms[b] = inverseBindTransforms[n] * absoTransforms[n];
                }
            }

            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (Effect effect in mesh.Effects)
                {
                    effect.Parameters["World"].SetValue(absoTransforms[mesh.ParentBone.Index] * world);
                    effect.Parameters["View"].SetValue(game.Camera.View);
                    effect.Parameters["Projection"].SetValue(game.Camera.Projection);
                    effect.Parameters["Bones"].SetValue(skinTransforms);
                }
                mesh.Draw();
            }

        }
    }
}
