using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

namespace PrisonStep
{
    /// <summary>
    /// This class implements one section of our prison ship
    /// </summary>
    public class PrisonModel
    {
        #region Fields

        /// <summary>
        /// The section (6) of the ship
        /// </summary>
        private int section;

        /// <summary>
        /// The name of the asset (FBX file) for this section
        /// </summary>
        private string asset;

        /// <summary>
        /// The game we are associated with
        /// </summary>
        private PrisonGame game;

        /// <summary>
        /// The XNA model for this part of the ship
        /// </summary>
        private Model model;

        /// <summary>
        /// To make animation possible and easy, we save off the initial (bind) 
        /// transformation for all of the model bones. 
        /// </summary>
        private Matrix[] bindTransforms;

        /// <summary>
        /// The is the transformations for all model bones, potentially after we
        /// have made some change in the tranformation.
        /// </summary>
        private Matrix[] boneTransforms;

        /// <summary>
        /// A list of all of the door bones in the model.
        /// </summary>
        private List<Door> doors = new List<Door>();
        #endregion

        #region Construction and Loading

        /// <summary>
        /// Constructor. Creates an object for a section.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="section"></param>
        public PrisonModel(PrisonGame game, int section)
        {
            this.game = game;
            this.section = section;
            this.asset = "AntonPhibes" + section.ToString();
        }

        /// <summary>
        /// This function is called to load content into this component
        /// of our game.
        /// </summary>
        /// <param name="content">The content manager to load from.</param>
        public void LoadContent(ContentManager content)
        {
            // Load the second model
            model = content.Load<Model>(asset);

            // Save off all of hte bone information
            int boneCnt = model.Bones.Count;
            bindTransforms = new Matrix[boneCnt];
            boneTransforms = new Matrix[boneCnt];

            model.CopyBoneTransformsTo(bindTransforms);
            model.CopyBoneTransformsTo(boneTransforms);

            // Find all of the doors and save the index for the bone
            for (int b = 0; b < boneCnt; b++)
            {
                if (model.Bones[b].Name.StartsWith("DoorInner") || model.Bones[b].Name.StartsWith("DoorOuter"))
                {
                    //Create and initialize doors
                    Door thisDoor = new Door();
                    thisDoor.boneNum = b;
                    thisDoor.boneName = model.Bones[b].Name;
                    thisDoor.doorState = Door.DoorState.DoorClosed;
                    switch(thisDoor.boneName.Substring(9,thisDoor.boneName.Length-9))
                    {
                        case "1":
                            thisDoor.location=new Vector3(218,0,1023);
                            break;
                        case "2":
                            thisDoor.location=new Vector3(-11,0,-769);
                            break;
                        case "3":
                            thisDoor.location=new Vector3(587,0,-999);
                            break;
                        case "4":
                            thisDoor.location=new Vector3(787,0,-763);
                            break;
                        case "5":
                            thisDoor.location=new Vector3(1187,0,-1218);
                            break;
                    }
                    doors.Add(thisDoor);
                }
            }
        }

        public bool IsDoorOpen(string doorName)
        {
            foreach (Door door in doors)
            {
                if (door.MatchesDoorName(doorName) && door.doorState == Door.DoorState.DoorOpen)
                {
                    return true;
                }
            }
            return false;
        }

        public void OpenDoor(string doorName)
        {
            foreach (Door door in doors)
            {
                if (door.MatchesDoorName(doorName) && (door.doorState == Door.DoorState.DoorClosed||door.doorState == Door.DoorState.DoorClosing))
                {
                    door.doorState = Door.DoorState.DoorOpening;
                }
            }
        }

        public void CloseDoor(string doorName)
        {
            foreach (Door door in doors)
            {
                if (door.MatchesDoorName(doorName) && door.doorState == Door.DoorState.DoorOpen)
                {
                    door.doorState = Door.DoorState.DoorClosing;
                }
            }
        }

        public Vector3 GetDoorLocation(string doorName)
        {
            foreach (Door door in doors)
            {
                if (door.MatchesDoorName(doorName))
                {
                    return door.location;
                }
            }
            return Vector3.Zero;
        }

        #endregion

        #region Update and Draw

        /// <summary>
        /// This function is called to update this component of our game
        /// to the current game time.
        /// </summary>
        /// <param name="gameTime"></param>
        public void Update(GameTime gameTime)
        {
            float delta = (float)gameTime.ElapsedGameTime.TotalSeconds;

            foreach (Door door in doors)
            {
                if (door.doorState == Door.DoorState.DoorClosing)
                {
                    boneTransforms[door.boneNum] =  Matrix.CreateTranslation(0, (float)door.currentTime*200, 0) * bindTransforms[door.boneNum];
                    if(door.currentTime<=0)
                    {
                        door.currentTime=0;
                        door.doorState=Door.DoorState.DoorClosed;
                    }
                    door.currentTime -= delta/2;
                }
                if (door.doorState == Door.DoorState.DoorOpening)
                {
                    boneTransforms[door.boneNum] = Matrix.CreateTranslation(0, (float)door.currentTime*200, 0) * bindTransforms[door.boneNum];
                    if (door.currentTime >= 1)
                    {
                        door.currentTime = 1;
                        door.doorState = Door.DoorState.DoorOpen;
                    }
                    door.currentTime += delta/2;
                }
            }
        }

        /// <summary>
        /// This function is called to draw this game component.
        /// </summary>
        /// <param name="graphics"></param>
        /// <param name="gameTime"></param>
        public void Draw(GraphicsDeviceManager graphics, GameTime gameTime)
        {
            DrawModel(graphics, model, Matrix.Identity);
        }


        #region COPIED

        private double[] lightData =
        {
            1, 568, 246, 1036, 0.53, 0.53, 0.53, 821, 224,
            941, 14.2941, 45, 43.9412, 814, 224, 1275, 82.5, 0, 0, 568, 246, 1036,
            2, -5, 169, 428, 0.3964, 0.503, 0.4044, -5.4, 169,
            1020, 129.4902, 107.5686, 41.8039, -5.4, 169, -138, 37.8275, 91, 91,-5,      169,     428,
            3, 113, 217, -933, 0.5, 0, 0, -129, 185,
            -1085, 50, 0, 0, 501, 185, -1087, 48, 0, 0, 113,      217,    -933,  
            4, 781, 209, -998, 0.2, 0.1678, 0.1341, 1183, 209,
            -998, 50, 41.9608, 33.5294, 984, 113, -932, 0, 80, 0, 781,      209,    -998,  
            5, 782, 177, -463, 0.65, 0.5455, 0.4359, 563, 195,
            -197, 50, 0, 0, 1018, 181, -188, 80, 0, 0,    782,      177,    -463,  
            6, 1182, 177, -1577, 0.65, 0.5455, 0.4359, 971, 181,
            -1801, 0, 13.1765, 80, 1406, 181, -1801, 0, 13.1765, 80, 1182,      177,   -1577};

        private bool skinned = false;

        /// <summary>
        /// Set true if this model should use the SkinnedEffect.fx instead
        /// of the other Phibes model effects.
        /// </summary>
        public bool Skinned { get { return skinned; } set { skinned = value; } }


        /// <summary>
        /// Get light information for a section. This pulls data from the lightData
        /// array.
        /// </summary>
        /// <param name="section">Section number 1-6</param>
        /// <param name="item">Item 0 for light 1 location, 1 for light 1 color, etc.</param>
        /// <returns></returns>
        private Vector3 LightInfo(int section, int item)
        {
            int offset = (section - 1) * 22 + 1 + (item * 3);
            return new Vector3((float)lightData[offset], (float)lightData[offset + 1], (float)lightData[offset + 2]);
        }
        #endregion

        private Door GetOpenDoor(int excludeThisSection)
        {
            foreach (Door door in doors)
            {
                if (door.doorState != Door.DoorState.DoorClosed)
                {
                    if(door.GetSectionNumber()!=excludeThisSection)
                        return door;
                }
            }
            return null;
        }
        private float GetOpenDoorDegree(Door door)
        {
            return boneTransforms[door.boneNum].Translation.Y/200;
        }

        private void DrawModel(GraphicsDeviceManager graphics, Model model, Matrix world)
        {
            // Apply the bone transforms
            Matrix[] absoTransforms = new Matrix[model.Bones.Count];
            model.CopyBoneTransformsFrom(boneTransforms);
            model.CopyAbsoluteBoneTransformsTo(absoTransforms);
            
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (Effect effect in mesh.Effects)
                {
                    effect.Parameters["World"].SetValue(absoTransforms[mesh.ParentBone.Index] * world);
                    effect.Parameters["View"].SetValue(game.Camera.View);
                    effect.Parameters["Projection"].SetValue(game.Camera.Projection);
                    effect.Parameters["Time"].SetValue((float)game.SlimeTime);

                    // The section the open door is in. (-1 for no open door)
                    int sectionOfOpenDoor = -1;

                    // The door that is open (null for no open door)
                    Door openDoor = GetOpenDoor(section);

                    // If the door is open, get the sectionOfOpenDoor
                    if (openDoor != null)
                    {
                        sectionOfOpenDoor = openDoor.GetSectionNumber();
                    }

                    //If there is an open door, set the openness.
                    if (sectionOfOpenDoor != -1)
                    {
                        // How open the door is (0 for no open door)
                        float opennessOfDoor = GetOpenDoorDegree(openDoor);
                        if (opennessOfDoor < 0) opennessOfDoor = 0;
                        if (opennessOfDoor > 1) opennessOfDoor = 1;

                        //If there is a slightly open door, set the color (turn it on) and set the location.
                        effect.Parameters["Light4Location"].SetValue(LightInfo(sectionOfOpenDoor, 6));
                        effect.Parameters["OpennessOfDoor"].SetValue(opennessOfDoor);
                    }
                    else 
                    {
                        effect.Parameters["OpennessOfDoor"].SetValue(0);
                    }
                }

                mesh.Draw();
            }
        }

        #endregion
    }
}
