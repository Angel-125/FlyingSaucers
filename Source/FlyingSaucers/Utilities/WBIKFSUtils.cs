using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KerbalActuators;

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
    /// <summary>
    /// This enumerator describes various engine states.
    /// </summary>
    public enum WBIEngineStates
    {
        Shutdown,
        Starting,
        ShuttingDown,
        Running,
        Flameout
    }

    /// <summary>
    /// This enumerator is used to describe which direction the engine is warping in.
    /// </summary>
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

    public interface IWarpController : IGenericController
    {
        /// <summary>
        /// Returns the current warp direction.
        /// </summary>
        /// <returns>A WBIWarpDirections enumerator describing the current warp direction.</returns>
        WBIWarpDirections GetWarpDirection();

        /// <summary>
        /// Sets the desired warp direction, but only if crazyModeUnlocked = true.
        /// </summary>
        /// <param name="direction">A WBIWarpDirections enumerator specifying the desired direction.</param>
        void SetWarpDirection(WBIWarpDirections direction);
    }

    public class WBIKFSUtils
    {
        public static void Log(string message)
        {
            Debug.Log(message);
        }
    }
}
