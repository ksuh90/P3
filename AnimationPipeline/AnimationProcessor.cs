using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

using XnaAux;

namespace AnimationPipeline
{
    /// <summary>
    /// This class will be instantiated by the XNA Framework Content Pipeline
    /// to apply custom processing to content data, converting an object of
    /// type TInput to TOutput. The input and output types may be the same if
    /// the processor wishes to alter data without changing its type.
    ///
    /// This should be part of a Content Pipeline Extension Library project.
    ///
    /// TODO: change the ContentProcessor attribute to specify the correct
    /// display name for this processor.
    /// </summary>
    [ContentProcessor(DisplayName = "Animation Processor")]
    public class AnimationProcessor : ModelProcessor
    {
        ModelContent model;

        private string customMaterialProcessor = "";
        private int section = 0;

        private bool skinned = false;

        /// <summary>
        /// Indicate if this model is skinned
        /// </summary>
        [Browsable(true)]
        [DisplayName("Skinned")]
        [DefaultValue(false)]
        public bool Skinned { get { return skinned; } set { skinned = value; } }

        /// <summary>
        /// An array of indices that map a bone in the skeleton
        /// to a bone in the whole model.
        /// </summary>
        private List<int> skelToBone = new List<int>();

        /// <summary>
        /// Convert a tree of nodes into a list of nodes in topological order.
        /// </summary>
        /// <param name="item">The root of the heirarchy</param>
        /// <returns></returns>
        private List<NodeContent> FlattenHeirarchy(NodeContent item)
        {
            List<NodeContent> nodes = new List<NodeContent>();
            nodes.Add(item);
            foreach (NodeContent child in item.Children)
            {
                FlattenHeirarchy(nodes, child);
            }

            return nodes;
        }


        private void FlattenHeirarchy(List<NodeContent> nodes, NodeContent item)
        {
            nodes.Add(item);
            foreach (NodeContent child in item.Children)
            {
                FlattenHeirarchy(nodes, child);
            }
        }



        /// <summary>
        /// A custom material processor to use instead of the default one
        /// </summary>
        [Browsable(true)]
        [DisplayName("Custom Material Processor")]
        public string CustomMaterialProcessor
        {
            get { return customMaterialProcessor; }
            set { customMaterialProcessor = value; }
        }

        /// <summary>
        /// The ship section
        /// </summary>
        [Browsable(true)]
        [DisplayName("Ship Section")]
        public int Section { get { return section; } set { section = value; } }

        private const float TinyLength = 1e-4f;

        private const float TinyCosAngle = 0.9999995f;

        /// <summary>
        /// Bones lookup table, converts bone names to indices.
        /// </summary>
        private Dictionary<string, int> bones = new Dictionary<string, int>();

        public override ModelContent Process(NodeContent input, ContentProcessorContext context)
        {
            if (skinned)
            {
                ProcessSkeleton(input);
            }

            model = base.Process(input, context);
            AnimationClips clips = ProcessAnimations(model, input, context);
            clips.SkelToBone = skelToBone;
            model.Tag = clips;

            return model;
        }

        /// <summary>
        /// Bakes unwanted transforms into the model geometry,
        /// so everything ends up in the same coordinate system.
        /// </summary>
        void FlattenTransforms(NodeContent node, BoneContent skeleton)
        {
            foreach (NodeContent child in node.Children)
            {
                // Don't process the skeleton, because that is special.
                if (child == skeleton)
                    continue;

                // Bake the local transform into the actual geometry.
                MeshHelper.TransformScene(child, child.Transform);

                // Having baked it, we can now set the local
                // coordinate system back to identity.
                child.Transform = Matrix.Identity;

                // Recurse.
                FlattenTransforms(child, skeleton);
            }
        }

        /// <summary>
        /// Use our custom material processor
        /// to convert selected materials in this model.
        /// </summary>
        protected override MaterialContent ConvertMaterial(MaterialContent material,
                                                         ContentProcessorContext context)
        {
            if (CustomMaterialProcessor == "")
                return base.ConvertMaterial(material, context);

            OpaqueDataDictionary processorParameters = new OpaqueDataDictionary();
            processorParameters.Add("Section", section);
            processorParameters.Add("Skinned", skinned);

            return context.Convert<MaterialContent, MaterialContent>(material,
                                                CustomMaterialProcessor,
                                                processorParameters);
        }

        /// <summary>
        /// Indicates whether or not the data is useless
        /// 
        /// If no meshes use the data, the data is useless
        /// </summary>
        /// <param name="boneId"></param>
        /// <returns></returns>
        private bool IsUselessAnimation(int boneId)
        {
            foreach (ModelMeshContent mesh in model.Meshes)
            {
                if (mesh.ParentBone.Index == boneId)
                {
                    // It is used; It's not useless
                    return false;
                }
            }

            // It isn't used; It's useless
            return true;
        }

        /// <summary>
        /// Process the animation content for the model.
        /// </summary>
        /// <param name="model">Model as computed by ModelProcessor</param>
        /// <param name="input">The input content loaded from FBX</param>
        /// <param name="context">Context object for error and warning messages</param>
        /// <returns></returns>
        private AnimationClips ProcessAnimations(ModelContent model,
                                        NodeContent input, ContentProcessorContext context)
        {
            // First build a lookup table so we can determine the 
            // index into the list of bones from a bone name.
            for (int i = 0; i < model.Bones.Count; i++)
            {
                bones[model.Bones[i].Name] = i;
            }

            AnimationClips animationClips = new AnimationClips();

            ProcessAnimationsRecursive(input, animationClips);

            // Ensure all animations have a first key frame for every bone
            foreach (AnimationClips.Clip clip in animationClips.Clips.Values)
            {
                for (int b = 0; b < bones.Count; b++)
                {
                    List<AnimationClips.Keyframe> keyframes = clip.Keyframes[b];
                    if (keyframes.Count == 0 || keyframes[0].Time > 0)
                    {
                        AnimationClips.Keyframe keyframe = new AnimationClips.Keyframe();
                        keyframe.Time = 0;

                        Matrix transform = model.Bones[b].Transform;
                        transform.Right = Vector3.Normalize(transform.Right);
                        transform.Up = Vector3.Normalize(transform.Up);
                        transform.Backward = Vector3.Normalize(transform.Backward);
                        keyframe.Rotation = Quaternion.CreateFromRotationMatrix(transform);
                        keyframe.Translation = transform.Translation;

                        keyframes.Insert(0, keyframe);
                    }
                }
            }

            return animationClips;
        }

        private void LinearKeyframeReduction(LinkedList<AnimationClips.Keyframe> keyframes)
        {
            if�(keyframes.Count <�3)
                return;

            for�(LinkedListNode<AnimationClips.Keyframe>�node�=�keyframes.First.Next;�;�)
            {
                LinkedListNode<AnimationClips.Keyframe>�next�=�node.Next;
                if�(next�==�null)
                    break;
              
                //�Determine�nodes�before�and�after�the�current�node.
                AnimationClips.Keyframe a�=�node.Previous.Value;
                AnimationClips.Keyframe b�=�node.Value;
                AnimationClips.Keyframe c = next.Value;

                float�t�=�(float)((node.Value.Time - node.Previous.Value.Time)�/ 
                    (next.Value.Time - node.Previous.Value.Time));

                Vector3 translation�=�Vector3.Lerp( a.Translation, c.Translation,�t);
                Quaternion�rotation�=�Quaternion.Slerp(a.Rotation,�c.Rotation,�t);
                
                if�((translation - b.Translation).LengthSquared()�<�TinyLength &&
                    Quaternion.Dot(rotation,�b.Rotation)�>�TinyCosAngle)
                {
                    keyframes.Remove(node);
                }

                node = next;
            }
        }
        /// <summary>
        /// Recursive function that processes the entire scene graph, collecting up
        /// all of the animation data.
        /// </summary>
        /// <param name="input">The input scene graph node</param>
        /// <param name="animationClips">The animation clips object we put animation in</param>
        private void ProcessAnimationsRecursive(NodeContent input, AnimationClips animationClips)
        {
            foreach (KeyValuePair<string, AnimationContent> animation in input.Animations)
            {
                // Do we have this animation before?
                AnimationClips.Clip clip;
                if (!animationClips.Clips.TryGetValue(animation.Key, out clip))
                {
                    // Never before seen clip
                    // System.Diagnostics.Trace.WriteLine("New clip: " + animation.Key);

                    clip = new AnimationClips.Clip();
                    clip.Name = animation.Key;
                    clip.Duration = animation.Value.Duration.TotalSeconds;
                    clip.Keyframes = new List<AnimationClips.Keyframe>[bones.Count];
                    for (int b = 0; b < bones.Count; b++)
                        clip.Keyframes[b] = new List<AnimationClips.Keyframe>();

                    animationClips.Clips[animation.Key] = clip;
                }
                else if (animation.Value.Duration.TotalSeconds > clip.Duration)
                {
                    clip.Duration = animation.Value.Duration.TotalSeconds;
                }

                //
                // For each channel, determine the bone and then process all of the 
                // keyframes for that bone.
                //

                foreach (KeyValuePair<string, AnimationChannel> channel in animation.Value.Channels)
                {
                    // What is the bone index?
                    int boneIndex;

                    // Get rid of unnamed bones
                    if (!bones.TryGetValue(channel.Key, out boneIndex))
                    {
                        continue;           // Ignore if not a named bone
                    }

                    // Get rid of useless animations
                    if (!skinned && IsUselessAnimation(boneIndex))
                    {
                        // System.Diagnostics.Debug.WriteLine("Removed " + boneIndex.ToString());
                        continue;           // Ignore if useless
                    }


                    foreach (AnimationKeyframe keyframe in channel.Value)
                    {
                        Matrix transform = keyframe.Transform;      // Keyframe transformation

                        AnimationClips.Keyframe newKeyframe = new AnimationClips.Keyframe();
                        newKeyframe.Time = keyframe.Time.TotalSeconds;

                        transform.Right = Vector3.Normalize(transform.Right);
                        transform.Up = Vector3.Normalize(transform.Up);
                        transform.Backward = Vector3.Normalize(transform.Backward);
                        newKeyframe.Rotation = Quaternion.CreateFromRotationMatrix(transform);
                        newKeyframe.Translation = transform.Translation;

                        clip.Keyframes[boneIndex].Add(newKeyframe);
                    }

                    // Linear keyframe reduction (remove from clip.Keyframes)
                    System.Diagnostics.Debug.WriteLine(clip.Keyframes[boneIndex].Count);
                    LinkedList<AnimationClips.Keyframe> keyframes = new LinkedList<AnimationClips.Keyframe>();
                    foreach (AnimationClips.Keyframe keyframe in clip.Keyframes[boneIndex])
                    {
                        keyframes.AddLast(keyframe);
                    }
                    LinearKeyframeReduction(keyframes);
                    clip.Keyframes[boneIndex] = keyframes.ToList<AnimationClips.Keyframe>();
                    System.Diagnostics.Debug.WriteLine(clip.Keyframes[boneIndex].Count);

                }
            }

           // System.Diagnostics.Trace.WriteLine(input.Name);


            foreach (NodeContent child in input.Children)
            {
                ProcessAnimationsRecursive(child, animationClips);
            }

        }

        /// <summary>
        /// Process the skeleton in support of skeletal animation...
        /// </summary>
        /// <param name="input"></param>
        private BoneContent ProcessSkeleton(NodeContent input)
        {
            // Find the skeleton.
            BoneContent skeleton = MeshHelper.FindSkeleton(input);

            if (skeleton == null)
                throw new InvalidContentException("Input skeleton not found.");

            // We don't want to have to worry about different parts of the model being
            // in different local coordinate systems, so let's just bake everything.
            FlattenTransforms(input, skeleton);

            //
            // 3D Studio Max includes helper bones that end with "Nub"
            // These are not part of the skinning system and can be 
            // discarded.  TrimSkeleton removes them from the geometry.
            //
            TrimSkeleton(skeleton);
            
            // Convert the heirarchy of nodes and bones into a list
            List<NodeContent> nodeBones = FlattenHeirarchy(input);
            IList<BoneContent> skelBones = MeshHelper.FlattenSkeleton(skeleton);

            // Create a dictionary to convert a node to an index into the array of node bones
            Dictionary<NodeContent, int> nodeToIndex = new Dictionary<NodeContent, int>();
            for (int i = 0; i < nodeBones.Count; i++)
            {
                nodeToIndex[nodeBones[i]] = i;
            }

            // Now create the array that maps the bones to the nodes
            foreach (BoneContent skelBone in skelBones)
            {
                skelToBone.Add(nodeToIndex[skelBone]);
            }

            return skeleton;
        }

        /// <summary>
        /// 3D Studio Max includes an extra help bone at the end of each
        /// IK chain that doesn't effect the skinning system and is 
        /// redundant as far as the game is concerned.  This function
        /// looks for children who's name ends with "Nub" and removes
        /// them from the heirarchy.
        /// </summary>
        /// <param name="skeleton">Root of the skeleton tree</param>
        void TrimSkeleton(NodeContent skeleton)
        {
            List<NodeContent> todelete = new List<NodeContent>();

            foreach (NodeContent child in skeleton.Children)
            {
                if (child.Name.EndsWith("Nub") || child.Name.EndsWith("Footsteps"))
                    todelete.Add(child);
                else
                    TrimSkeleton(child);
            }

            foreach (NodeContent child in todelete)
            {
                skeleton.Children.Remove(child);
            }
        }
    }
}