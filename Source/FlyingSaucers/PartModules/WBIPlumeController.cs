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
    public class WBIPlumeController: PartModule
    {
        #region Fields
        [KSPField]
        public string stationaryFXName = string.Empty;

        [KSPField]
        public string plumeFXName = string.Empty;

        [KSPField]
        public string plumeFXTransformName = string.Empty;
        #endregion

        #region Housekeeping
        public WBIThrustModes engineMode = WBIThrustModes.Off;

        protected WBIThrustModes prevEngineMode = WBIThrustModes.Off;
        protected WBIWarpDirections warpDirection;
        protected WBIWarpDirections prevWarpDirection;
        protected Transform plumeFXTransform;
        protected bool isRCSEnabled;
        protected List<WBIGraviticEngine> engineList;
        protected float engineThrottle;
        protected float prevEngineThrottle;
        protected bool hoverIsActive;
        int vesselPartCount = 0;
        Quaternion originalRotation;
        Quaternion reverseRotation;
        Quaternion upRotation;
        Quaternion downRotation;
        Quaternion leftRotation;
        Quaternion rightRotation;
        Quaternion targetRotation;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            engineMode = WBIThrustModes.Off;

            if (!string.IsNullOrEmpty(plumeFXTransformName))
            {
                plumeFXTransform = this.part.FindModelTransform(plumeFXTransformName);

                //Setup rotations
                originalRotation = plumeFXTransform.localRotation;
                targetRotation = originalRotation;

                plumeFXTransform.Rotate(Vector3.up, 180.0f);
                reverseRotation = plumeFXTransform.localRotation;
                plumeFXTransform.localRotation = originalRotation;

                plumeFXTransform.Rotate(Vector3.up, -90f);
                leftRotation = plumeFXTransform.localRotation;
                plumeFXTransform.localRotation = originalRotation;

                plumeFXTransform.Rotate(Vector3.up, 90f);
                rightRotation = plumeFXTransform.localRotation;
                plumeFXTransform.localRotation = originalRotation;

                plumeFXTransform.Rotate(Vector3.right, -90f);
                downRotation = plumeFXTransform.localRotation;
                plumeFXTransform.localRotation = originalRotation;

                plumeFXTransform.Rotate(Vector3.right, 90f);
                upRotation = plumeFXTransform.localRotation;
                plumeFXTransform.localRotation = originalRotation;
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (plumeFXTransform == null)
                return;

            //Get engine state
            updateEngineState();

            //Get RCS state
            isRCSEnabled = FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.RCS];
            FlightCtrlState state = FlightInputHandler.state;

            //Update thrust plume direction
            updateThrustPlumeDirection();

            //Update thrust power
            //Priority goes to engine thrust, then RCS state.
            float fxPower = 0f;
            if ((engineMode != WBIThrustModes.Off && engineThrottle > 0))
                fxPower = engineThrottle;
            if (isRCSEnabled || hoverIsActive || (warpDirection != WBIWarpDirections.Stop && engineThrottle > 0))
                fxPower = 1.0f;

            //Update stationary fx
            if (!string.IsNullOrEmpty(stationaryFXName))
                this.part.Effect(stationaryFXName, fxPower, -1);

            //Update plume fx
            //Disable if we're throttled down hover mode isn't active
            isRCSEnabled = state.X != 0 || state.Y != 0 || state.Z != 0;
            if (engineThrottle <= 0 && !hoverIsActive && warpDirection == WBIWarpDirections.Stop && engineMode == WBIThrustModes.Off && !isRCSEnabled)
                fxPower = 0;
            this.part.Effect(plumeFXName, fxPower, -1);

            //Rotate the plume if needed
            if (plumeFXTransform.localRotation != targetRotation)
            {
                plumeFXTransform.localRotation = Quaternion.Lerp(plumeFXTransform.localRotation, targetRotation, 0.1f);
            }
        }

        protected void updateEngineState()
        {
            //Get the engine list
            if (engineList == null || vesselPartCount != this.part.vessel.parts.Count || engineList.Count == 0)
            {
                engineThrottle = 0f;
                prevEngineThrottle = 0f;
                engineMode = WBIThrustModes.Off;
                prevWarpDirection = WBIWarpDirections.Stop;
                vesselPartCount = this.part.vessel.parts.Count;

                engineList = this.part.vessel.FindPartModulesImplementing<WBIGraviticEngine>();
                if (engineList.Count == 0)
                    return;
            }

            //If we have at least one engine that is active then use its state.
            int count = engineList.Count;
            WBIGraviticEngine engine;
            for (int index = 0; index < count; index++)
            {
                engine = engineList[index];
                if (engine.isOperational && engine.EngineIgnited)
                {
                    prevEngineMode = engineMode;
                    engineMode = engine.engineMode;
                    prevEngineThrottle = engineThrottle;
                    engineThrottle = engine.currentThrottle;
                    hoverIsActive = engine.hoverIsActive;

                    //Translate warp direction into thrust mode
                    prevWarpDirection = warpDirection;
                    warpDirection = engine.GetWarpDirection();
                    if (warpDirection != WBIWarpDirections.Stop)
                    {
                        switch (warpDirection)
                        {
                            case WBIWarpDirections.Left:
                                engineMode = WBIThrustModes.Right;
                                break;

                            case WBIWarpDirections.Right:
                                engineMode = WBIThrustModes.Left;
                                break;

                            case WBIWarpDirections.Forward:
                                engineMode = WBIThrustModes.Forward;
                                break;

                            case WBIWarpDirections.Back:
                                engineMode = WBIThrustModes.Reverse;
                                break;

                            case WBIWarpDirections.Up:
                                engineMode = WBIThrustModes.VTOL;
                                break;

                            case WBIWarpDirections.Down:
                                engineMode = WBIThrustModes.Down;
                                break;
                        }
                    }
                    return;
                }
                else
                {
                    engineMode = WBIThrustModes.Off;
                    warpDirection = WBIWarpDirections.Stop;
                }
            }

            //No engines active!
            engineThrottle = 0f;
            prevEngineThrottle = 0f;
            engineMode = WBIThrustModes.Off;
            prevWarpDirection = WBIWarpDirections.Stop;
        }

        protected void updateThrustPlumeDirection()
        {
            //No need to update if we haven't changed engine mode
            if (engineMode == prevEngineMode && warpDirection == prevWarpDirection && prevEngineThrottle == engineThrottle)
                return;

            //Account for crazy mode & hover state. If crazy mode is on, hover is active, and throttle is zeroed, then set vtol plume direction.
            if (warpDirection != WBIWarpDirections.Stop && hoverIsActive && engineThrottle <= 0)
            {
                targetRotation = downRotation;
                return;
            }

            //Update plume fx: start with thrust direction.
            switch (engineMode)
            {
                case WBIThrustModes.Reverse:
                    targetRotation = reverseRotation;
                    break;

                case WBIThrustModes.Left:
                    targetRotation = leftRotation;
                    break;

                case WBIThrustModes.Right:
                    targetRotation = rightRotation;
                    break;

                case WBIThrustModes.VTOL:
                    targetRotation = downRotation;
                    break;

                case WBIThrustModes.Down:
                    targetRotation = upRotation;
                    break;

                default:
                    targetRotation = originalRotation;
                    break;
            }
        }

        /*
        protected void updateRCSPlumeDirection(FlightCtrlState state)
        {
            //Left/right rotations
            float rotationLtRt = 0f;

            //Fwd Left, plume goes back and right
            //Fwd Right, plume goes back and left
            if ((state.X > 0f && state.Z < 0f) || (state.X < 0f && state.Z < 0f))
                rotationLtRt = -45f * state.X * state.Z;

            //Back Left, plume goes front and right
            //Back Right, plume goes front and left
            else if ((state.X > 0f && state.Z > 0f) || (state.X < 0f && state.Z > 0f))
                rotationLtRt = 135f * state.X * state.Z;

            //Left or right
            else if (state.X != 0f)
                rotationLtRt = 90 * state.X;

            //Up/down rotations
            float rotationUpDn = 0f;

            //Fwd up, plume goes back and down
            //Fwd down, plume goes back and up
            if ((state.Y > 0f && state.Z < 0f) || (state.Y < 0f && state.Z < 0f))
                rotationUpDn = 45f * state.Y * state.Z;

            //Back up, plume goes front and down
            //Back down, plume goes front and up
            else if ((state.Y > 0f && state.Z > 0f) || (state.Y < 0f && state.Z > 0f))
                rotationUpDn = -135f * state.Y * state.Z;

            //Up or down
            else if (state.Y != 0f)
                rotationUpDn = -90f * state.Y;

            //If the throttle is off and we have no RCS inputs then keep the previous plume rotation.
            if (state.X <= 0 && state.Y <= 0 && state.Z <= 0 && engineThrottle <= 0)
            {
                plumeFXTransform.localEulerAngles = previousRCSRotation;
                return;
            }

            //Rotate the plume fx transform
            if (rotationLtRt != 0f)
                plumeFXTransform.Rotate(Vector3.up, rotationLtRt);
            if (rotationUpDn != 0f)
                plumeFXTransform.Rotate(Vector3.right, rotationUpDn);
            previousRCSRotation = plumeFXTransform.localEulerAngles;
        }
        */
        #endregion
    }
}
