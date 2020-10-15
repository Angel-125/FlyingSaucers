using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KerbalActuators;

/*
Source code copyright 2018-2020, by Michael Billard (Angel-125)
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

        protected Transform plumeFXTransform;
        protected bool isRCSEnabled;
        protected WBIGraviticEngine engine = null;
        Quaternion originalRotation;
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
            }
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (plumeFXTransform == null)
                return;

            //Get engine
            if (engine == null)
            {
                engine = this.part.FindModuleImplementing<WBIGraviticEngine>();
                if (engine == null)
                    return;
            }

            //Get RCS state
            isRCSEnabled = FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.RCS];
            FlightCtrlState state = FlightInputHandler.state;

            //Update thrust plume direction
            updateThrustPlumeDirection();

            //Update thrust power
            //Priority goes to engine thrust, then RCS state.
            float fxPower = 0f;
            if ((engine.engineMode != WBIThrustModes.Off && engine.currentThrottle > 0))
                fxPower = engine.currentThrottle;
            if (isRCSEnabled || engine.hoverIsActive || (engine.warpVector != Vector3.zero && engine.currentThrottle > 0))
                fxPower = 1.0f;

            //Update stationary fx
            if (!string.IsNullOrEmpty(stationaryFXName))
                this.part.Effect(stationaryFXName, fxPower, -1);

            //Update plume fx
            //Disable if we're throttled down hover mode isn't active
            isRCSEnabled = state.X != 0 || state.Y != 0 || state.Z != 0;
            if (engine.currentThrottle <= 0 && !engine.hoverIsActive && engine.warpVector == Vector3.zero && !isRCSEnabled)
                fxPower = 0;
            this.part.Effect(plumeFXName, fxPower, -1);

            //Rotate the plume if needed
            if (plumeFXTransform.localRotation != targetRotation)
            {
                plumeFXTransform.localRotation = Quaternion.Lerp(plumeFXTransform.localRotation, targetRotation, 0.1f);
            }
        }

        protected void updateThrustPlumeDirection()
        {
            //Hover: point down if hover is activated.
            if (engine.hoverIsActive)
            {
                //While hovering, if crazy mode is activated, then check translation and create long plumes
                if (engine.crazyModeEnabled)
                    updateCrazyPlumeDirection();

                //If we're flying and thrust mode is forward or reverse, then the hover plume is diagonally forward or back.
                else if (engine.VesselIsAirborne() && (engine.engineMode == WBIThrustModes.Forward || engine.engineMode == WBIThrustModes.Reverse))
                    updateHoverFlyingPlumeDirection();

                //While hovering, if RCS is activated, then check translation and move downward plume slightly
                else if (isRCSEnabled)
                    updateHoverRCSPlumeDirection();

                //Point the plume towards the ground
                else
                    updateHoverPlumeDirection();
            }

            //No hover & not landed or splashed: long plume in opposite of thrust direction
            else if (engine.VesselIsAirborne())
            {
                //If RCS is activated then check translation and move plume slightly.
                updateFlyingDirection(isRCSEnabled);
            }

            //Orbiting or escaping: long plume in opposite thrust direction if RCS is activated.
            else if (engine.VesselIsOrbital())
            {
                updatePlumeDirection();
            }
        }

        protected void updateFlyingDirection(bool rcsIsActive)
        {
            Quaternion currentRotation = plumeFXTransform.localRotation;
            plumeFXTransform.localRotation = originalRotation;

            if (engine.currentThrottle > 0 && engine.engineMode == WBIThrustModes.Reverse)
                plumeFXTransform.Rotate(0, 180, 0);

            if (rcsIsActive && (engine.translateLtRt != 0 || engine.translateUpDn != 0))
            {
                if (engine.translateLtRt != 0)
                {
                    if (engine.translateLtRt > 0)
                        plumeFXTransform.Rotate(engine.engineMode == WBIThrustModes.Forward ? -10 : 10, 0, 0);
                    else
                        plumeFXTransform.Rotate(engine.engineMode == WBIThrustModes.Forward ? 10 : -10, 0, 0);
                }
                if (engine.translateUpDn != 0)
                {
                    if (engine.translateUpDn > 0)
                        plumeFXTransform.Rotate(0, engine.engineMode == WBIThrustModes.Forward ? 10 : -10, 0);
                    else
                        plumeFXTransform.Rotate(0, engine.engineMode == WBIThrustModes.Forward ? -10 : 10, 0);
                }
            }

            targetRotation = plumeFXTransform.localRotation;
            plumeFXTransform.localRotation = currentRotation;
        }

        protected void updateHoverFlyingPlumeDirection()
        {
            Quaternion currentRotation = plumeFXTransform.localRotation;
            plumeFXTransform.localRotation = originalRotation;
            plumeFXTransform.LookAt(this.part.vessel.mainBody.bodyTransform);

            if (engine.currentThrottle > 0)
            {
                if (engine.engineMode == WBIThrustModes.Forward)
                    plumeFXTransform.Rotate(0, -45 * engine.currentThrottle, 0);
                else
                    plumeFXTransform.Rotate(0, 45 * engine.currentThrottle, 0);
            }

            targetRotation = plumeFXTransform.localRotation;
            plumeFXTransform.localRotation = currentRotation;
        }

        protected void updateHoverRCSPlumeDirection()
        {
            //Update the plume based on translation input.
            if (engine.translateFwBk != 0 || engine.translateLtRt != 0 || engine.translateUpDn != 0)
            {
                Quaternion currentRotation = plumeFXTransform.localRotation;
                plumeFXTransform.localRotation = originalRotation;
                plumeFXTransform.LookAt(this.part.vessel.mainBody.bodyTransform);

                if (engine.translateFwBk != 0)
                {
                    if (engine.translateFwBk > 0)
                        plumeFXTransform.Rotate(0, -30, 0);
                    else
                        plumeFXTransform.Rotate(0, 30, 0);
                }
                if (engine.translateLtRt != 0)
                {
                    //Account for forward/back
                    if (engine.translateLtRt > 0)
                        plumeFXTransform.Rotate(engine.translateFwBk == 0 ? -30 : -15, 0, 0);
                    else
                        plumeFXTransform.Rotate(engine.translateFwBk == 0 ? 30 : 15, 0, 0);
                }
                if (engine.translateUpDn != 0)
                {
                    //Account for forward/back & left/right
                    if (engine.translateUpDn > 0)
                        plumeFXTransform.Rotate(engine.translateFwBk == 0 ? -30 : -15, engine.translateLtRt == 0 ? -30 : -15, 0);
                    else
                        plumeFXTransform.Rotate(engine.translateFwBk == 0 ? 30 : 15, engine.translateLtRt == 0 ? 30 : 15, 0);
                }

                targetRotation = plumeFXTransform.localRotation;
                plumeFXTransform.localRotation = currentRotation;
            }
            else
            {
                updateHoverPlumeDirection();
            }
        }

        protected void updateHoverPlumeDirection()
        {
            Quaternion currentRotation = plumeFXTransform.localRotation;
            plumeFXTransform.localRotation = originalRotation;

            plumeFXTransform.LookAt(this.part.vessel.mainBody.bodyTransform);

            targetRotation = plumeFXTransform.localRotation;
            plumeFXTransform.localRotation = currentRotation;
        }

        protected void updatePlumeDirection()
        {
            if (engine.translateFwBk != 0 || engine.translateLtRt != 0 || engine.translateUpDn != 0)
            {
                Quaternion currentRotation = plumeFXTransform.localRotation;
                plumeFXTransform.localRotation = originalRotation;

                if (engine.translateFwBk != 0)
                {
                    if (engine.translateFwBk > 0)
                        targetRotation = originalRotation;
                    else
                        plumeFXTransform.Rotate(0, 180, 0);
                }
                if (engine.translateLtRt != 0)
                {
                    //Account for forward/back
                    if (engine.translateLtRt > 0)
                        plumeFXTransform.Rotate(0, engine.translateFwBk == 0 ? -90 : -45, 0);
                    else
                        plumeFXTransform.Rotate(0, engine.translateFwBk == 0 ? 90 : 45, 0);
                }
                if (engine.translateUpDn != 0)
                {
                    //Account for forward/back & left/right
                    if (engine.translateUpDn > 0)
                        plumeFXTransform.Rotate(engine.translateFwBk == 0 ? -90 : -45, engine.translateLtRt == 0 ? -90 : -45, 0);
                    else
                        plumeFXTransform.Rotate(engine.translateFwBk == 0 ? 90 : 45, engine.translateLtRt == 0 ? 90 : 45, 0);
                }

                targetRotation = plumeFXTransform.localRotation;
                plumeFXTransform.localRotation = currentRotation;
            }
        }

        protected void updateCrazyPlumeDirection()
        {
            //If current throttle is zero, then just update in hover and exit.
            if (engine.currentThrottle <= 0)
            {
                updateHoverPlumeDirection();
                return;
            }

            //Update the plume based on translation input.
            if (engine.translateFwBk != 0 || engine.translateLtRt != 0 || engine.translateUpDn != 0)
            {
                Quaternion currentRotation = plumeFXTransform.localRotation;
                plumeFXTransform.localRotation = originalRotation;

                if (engine.translateFwBk != 0)
                {
                    if (engine.translateFwBk > 0)
                        targetRotation = originalRotation;
                    else
                        plumeFXTransform.Rotate(0, 180, 0);
                }
                if (engine.translateLtRt != 0)
                {
                    //Account for forward/back
                    if (engine.translateLtRt > 0)
                        plumeFXTransform.Rotate(0, engine.translateFwBk == 0 ? -90 : -45, 0);
                    else
                        plumeFXTransform.Rotate(0, engine.translateFwBk == 0 ? 90 : 45, 0);
                }
                if (engine.translateUpDn != 0)
                {
                    //Account for forward/back & left/right
                    if (engine.translateUpDn > 0)
                        plumeFXTransform.Rotate(engine.translateFwBk == 0 ? -90 : -45, engine.translateLtRt == 0 ? -90 : -45, 0);
                    else
                        plumeFXTransform.Rotate(engine.translateFwBk == 0 ? 90 : 45, engine.translateLtRt == 0 ? 90 : 45, 0);
                }

                targetRotation = plumeFXTransform.localRotation;
                plumeFXTransform.localRotation = currentRotation;
            }

            //If not in cruise control then just update to hover mode.
            else if (!engine.crazyCruiseControlEnabled)
            {
                updateHoverPlumeDirection();
            }
        }
        #endregion
    }
}
