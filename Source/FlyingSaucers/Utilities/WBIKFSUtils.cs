using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

/*
Source code copyright 2018, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/
namespace WildBlueIndustries
{
    public enum WBIEngineStates
    {
        Shutdown,
        Starting,
        ShuttingDown,
        Running,
        Flameout
    }

    public enum WBIWarpDirections
    {
        Stop,
        Forward,
        Back,
        Left,
        Right,
        Up,
        Down
    }

    public class WBIKFSUtils
    {
        #region Cached Strings
        public static string kMaxAcceleration = "<color=white><b>Max Acceleration: </b>{0:n2}m/sec^2</color>";
        public static string kFlameout = "<color=white><b>Flameout under: </b>{0:n2}%</color>";
        public static string kPropellants = "\n<b><color=#99ff00ff>Propellants:</color></b>";
        public static string kFuelFlowVaries = "<b><color=orange>Fuel flow varies with vessel mass</color></b>";
        public static string kRCSProducedFrom = "<b>Propellants generated from: </b>";
        public static string kCrazyMode = "<color=orange><b>--- Crazy Mode ---</b></color>";
        public static string kCrazyModeVelocity = "<color=white><b>Velocity: </b>{0:n2}m/sec</color>";
        public static string kCrazyModeResource = "<color=white>Consumes {0:n2} units of {1} per second.</color>";
        #endregion

        public static void Log(string message)
        {
            Debug.Log(message);
        }
    }
}
