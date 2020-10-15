using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KerbalActuators;
using KSP.Localization;

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
    public class WBIGraviticLift : WBIModuleResourceConverterFX, IHoverController
    {
        #region Fields
        [KSPField]
        public float maxAcceleration;

        [KSPField]
        public float specificImpulse = 1.0f;

        [KSPField(guiActive = true, guiName = "Throttle Controlled")]
        [UI_Toggle(enabledText = "Enabled", disabledText = "Disabled")]
        public bool throttleControlled;
        #endregion

        #region Housekeeping
        public float verticalSpeed = 0f;
        public bool isLiftingOff = false;
        float liftAcceleration = 0f;
        bool isMissingResources = false;
        #endregion

        #region Overrides
        public override void FixedUpdate()
        {
            //Calculate efficiency
            UpdateEfficiency();

            base.FixedUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (!this.IsActivated)
                return;

            UpdateHoverState();
        }


        protected override void PostProcess(ConverterResults result, double deltaTime)
        {
            base.PostProcess(result, deltaTime);

            if (!IsActivated)
                return;

            isMissingResources = result.TimeFactor > 0f ? false : true;
        }
        #endregion

        #region IHoverController
        public bool GetHoverState()
        {
            return this.IsActivated;
        }

        public float GetVerticalSpeed()
        {
            return this.verticalSpeed;
        }

        public bool IsActive()
        {
            return this.IsActivated;
        }

        public bool IsEngineActive()
        {
            return this.IsActivated;
        }

        public void KillVerticalSpeed()
        {
            this.verticalSpeed = 0.0f;
            this.part.vessel.verticalSpeed = 0.0f;
            this.part.vessel.SetWorldVelocity(Vector3d.zero);
        }

        public void SetHoverMode(bool isActive)
        {
            if (this.part.vessel.situation == Vessel.Situations.ESCAPING ||
                this.part.vessel.situation == Vessel.Situations.DOCKED ||
                this.part.vessel.situation == Vessel.Situations.ORBITING ||
                this.part.vessel.situation == Vessel.Situations.SUB_ORBITAL)
            {
                this.StopResourceConverter();
                return;
            }

            //Set the mode
            if (isActive)
                ActivateHover();
            else
                DeactivateHover();

            //Update symmetry parts
            if (this.part.symmetryCounterparts.Count > 0)
            {
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    WBIGraviticLift graviticLift = symmetryPart.GetComponent<WBIGraviticLift>();
                    if (graviticLift != null)
                    {
                        if (this.IsActivated)
                            graviticLift.ActivateHover();
                        else
                            graviticLift.DeactivateHover();
                    }
                }
            }
        }

        public void SetVerticalSpeed(float verticalSpeed)
        {
            if (verticalSpeed > 0f)
                isLiftingOff = true;

            this.verticalSpeed = verticalSpeed;
        }

        public void StartEngine()
        {
            this.StartResourceConverter();
        }

        public void StopEngine()
        {
            this.StopResourceConverter();
        }
        #endregion

        #region Helpers
        public void UpdateEfficiency()
        {
            if (!this.IsActivated)
                return;

            //Get total mass
            float totalMass = 0.0f;
            if (HighLogic.LoadedSceneIsFlight)
                totalMass = vessel.GetTotalMass();
            else if (HighLogic.LoadedSceneIsEditor)
                totalMass = EditorLogic.fetch.ship.GetTotalMass();

            //Calculate lift acceleration
            liftAcceleration = (float)this.part.vessel.graviticAcceleration.magnitude;
            if (verticalSpeed > 0 && vessel.verticalSpeed < verticalSpeed)
                liftAcceleration += verticalSpeed;
            else if (verticalSpeed < 0 && vessel.verticalSpeed > verticalSpeed)
                liftAcceleration += verticalSpeed;
            if (liftAcceleration > maxAcceleration)
                liftAcceleration = maxAcceleration;

            //Calculate lift force
            float liftForce = liftAcceleration * totalMass;
            if (throttleControlled)
                liftForce = (maxAcceleration * totalMass) * FlightInputHandler.state.mainThrottle;

            //Calculate max flow.
            this.EfficiencyBonus = liftForce / (specificImpulse * 9.81f);
        }

        public void UpdateHoverState()
        {
            if (!this.IsActivated)
                return;
            if (isMissingResources)
                return;

            //If we just landed then kill vertical speed and exit
            if ((this.part.vessel.situation == Vessel.Situations.LANDED ||
                this.part.vessel.situation == Vessel.Situations.SPLASHED ||
                this.part.vessel.situation == Vessel.Situations.PRELAUNCH) && !isLiftingOff)
            {
                verticalSpeed = 0f;
                return;
            }

            //Once we're flying again, remove the flag.
            else if (VesselIsAirborne())
            {
                isLiftingOff = false;
            }

            //Get lift vector
            Vector3d accelerationVector = (this.part.vessel.CoM - this.vessel.mainBody.position).normalized * liftAcceleration;

            //Add acceleration. We do this manually instead of letting ModuleEnginesFX do it so that the craft can have any orientation desired.
            ApplyAccelerationVector(accelerationVector);
        }

        public void ActivateHover()
        {
            verticalSpeed = 0.0f;

            if (!IsActivated)
                this.StartResourceConverter();
        }

        public void DeactivateHover()
        {
            if (vessel.LandedOrSplashed)
            {
                vessel.ctrlState.mainThrottle = 0f;
                FlightInputHandler.state.mainThrottle = 0f;
            }

            if (IsActivated)
                this.StopResourceConverter();
        }

        public void ApplyAccelerationVector(Vector3d accelerationVector)
        {
            int partCount = vessel.parts.Count;
            Part vesselPart;
            for (int index = 0; index < partCount; index++)
            {
                vesselPart = vessel.parts[index];
                if (vesselPart.rb != null)
                {
                    vesselPart.rb.AddForce(accelerationVector, ForceMode.Acceleration);
                }
            }
        }

        public bool VesselIsAirborne()
        {
            if (this.part.vessel.situation == Vessel.Situations.FLYING || this.part.vessel.situation == Vessel.Situations.SUB_ORBITAL)
                return true;

            return false;
        }
        #endregion
    }
}
