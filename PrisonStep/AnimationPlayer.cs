﻿#region

using System.Collections.Generic;
using Microsoft.Xna.Framework;
using XnaAux;

#endregion

namespace PrisonStep
{
    /// <summary>
    ///     The AnimationPlayer class contains the code to play an animation on
    ///     an animated model.
    /// </summary>
    public class AnimationPlayer
    {
        /// <summary>
        ///     The clip we are playing
        /// </summary>
        private readonly AnimationClips.Clip clip;

        private int boneCnt;
        private BoneInfo[] boneInfos;

        private bool looping;

        /// <summary>
        ///     The model we are playing onto
        /// </summary>
        private AnimatedModel model;

        private double speed = 1.0f;

        private double time;

        /// <summary>
        ///     Construct an AnimationPlayer to play the clip
        /// </summary>
        /// <param name="model"></param>
        /// <param name="clip"></param>
        public AnimationPlayer(AnimatedModel model, AnimationClips.Clip clip)
        {
            this.model = model;
            this.clip = clip;

            Initialize();
        }

        public int BoneCount
        {
            get { return boneCnt; }
        }

        /// <summary>
        ///     Get the clip being played by this player
        /// </summary>
        public AnimationClips.Clip Clip
        {
            get { return clip; }
        }

        #region Properties

        public double Time
        {
            get { return time; }
            set { time = value; }
        }

        /// <summary>
        ///     Indicates if the playback should "loop" or not.
        /// </summary>
        public bool Looping
        {
            get { return looping; }
            set { looping = value; }
        }

        /// <summary>
        ///     Playback speed
        /// </summary>
        public double Speed
        {
            get { return speed; }
            set { speed = value; }
        }

        /// <summary>
        ///     Get a bone
        /// </summary>
        /// <param name="b"></param>
        /// <returns></returns>
        public IBone GetBone(int b)
        {
            return boneInfos[b];
        }

        #endregion

        /// <summary>
        ///     Initialize for use
        /// </summary>
        private void Initialize()
        {
            boneCnt = clip.Keyframes.Length;
            boneInfos = new BoneInfo[boneCnt];

            time = 0;
            for (int b = 0; b < boneCnt; b++)
            {
                boneInfos[b].CurrentKeyframe = -1;
                boneInfos[b].Valid = false;
            }
        }


        /// <summary>
        ///     Update the clip position
        /// </summary>
        /// <param name="delta">The amount of time that has passed.</param>
        public void Update(double delta)
        {
            time += speed*delta;
            if (looping && time >= clip.Duration)
            {
                time -= clip.Duration;
                for (int b = 0; b < boneCnt; b++)
                {
                    boneInfos[b].CurrentKeyframe = -1;
                    boneInfos[b].Valid = false;
                }
            }

            for (int b = 0; b < boneInfos.Length; b++)
            {
                List<AnimationClips.Keyframe> keyframes = clip.Keyframes[b];
                if (keyframes.Count == 0)
                    continue;

                // The time needs to be greater than or equal to the
                // current keyframe time and less than the next keyframe 
                // time.
                while (boneInfos[b].CurrentKeyframe < 0 ||
                       (boneInfos[b].CurrentKeyframe < keyframes.Count - 1 &&
                        keyframes[boneInfos[b].CurrentKeyframe + 1].Time <= time))
                {
                    // Advance to the next keyframe
                    boneInfos[b].CurrentKeyframe++;

                    int c = boneInfos[b].CurrentKeyframe;

                    boneInfos[b].Keyframe1 = keyframes[c];
                    boneInfos[b].Time1 = keyframes[c].Time;
                    if (c == keyframes.Count - 1)
                    {
                        if (looping)
                        {
                            boneInfos[b].Keyframe2 = keyframes[0];
                            boneInfos[b].Time2 = clip.Duration;
                        }
                        else
                        {
                            // If not looping, there is no next keyframe.
                            boneInfos[b].Keyframe2 = null;
                            boneInfos[b].Time2 = clip.Duration;
                        }
                    }
                    else
                    {
                        boneInfos[b].Keyframe2 = keyframes[c + 1];
                        boneInfos[b].Time2 = keyframes[c + 1].Time;
                    }
                }

                //
                // Update the bone
                //

                if (boneInfos[b].Keyframe1 != null)
                {
                    if (boneInfos[b].Keyframe2 != null)
                    {
                        AnimationClips.Keyframe keyframe1 = boneInfos[b].Keyframe1;
                        AnimationClips.Keyframe keyframe2 = boneInfos[b].Keyframe2;

                        var t = (float) ((time - boneInfos[b].Time1)/(boneInfos[b].Time2 - boneInfos[b].Time1));

                        boneInfos[b].Rotation = Quaternion.Slerp(keyframe1.Rotation, keyframe2.Rotation, t);
                        boneInfos[b].Translation = Vector3.Lerp(keyframe1.Translation, keyframe2.Translation, t);
                        boneInfos[b].Valid = true;
                    }
                    else
                    {
                        AnimationClips.Keyframe keyframe1 = boneInfos[b].Keyframe1;
                        boneInfos[b].Rotation = keyframe1.Rotation;
                        boneInfos[b].Translation = keyframe1.Translation;
                    }

                    boneInfos[b].Valid = true;
                }
            }
        }

        private struct BoneInfo : IBone
        {
            public AnimationClips.Keyframe Keyframe1;

            public AnimationClips.Keyframe Keyframe2; // Second keyframe value
            public double Time1; // First time
            public double Time2; // Second time

            public int CurrentKeyframe { get; set; }
            public bool Valid { get; set; }
            public Quaternion Rotation { get; set; }
            public Vector3 Translation { get; set; }
        }

        public interface IBone
        {
            bool Valid { get; }
            Quaternion Rotation { get; }
            Vector3 Translation { get; }
        }
    }
}