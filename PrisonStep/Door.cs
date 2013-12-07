using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace PrisonStep
{
    public class Door
    {
        public enum DoorState { DoorOpen, DoorClosed, DoorOpening, DoorClosing };
        public int boneNum;
        public DoorState doorState;
        public string boneName;
        public double currentTime = 0;
        public Vector3 location = Vector3.Zero;

        public bool MatchesDoorName(string doorName)
        {
            doorName = doorName.Substring(6, doorName.Length - 6);
            string doorBoneName1 = "DoorInner" + doorName;
            string doorBoneName2 = "DoorOuter" + doorName;
            return (boneName == doorBoneName1 || boneName == doorBoneName2);
        }

        public int GetSectionNumber()
        {
            int sectionOfDoor;
            bool good = int.TryParse(boneName.Substring(9, boneName.Length - 9), out sectionOfDoor);
            if (!good) sectionOfDoor = -1;
            return sectionOfDoor;
        }
    }
}
