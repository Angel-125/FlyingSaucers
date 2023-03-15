using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;

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
    public class WBIModuleGraviticRCS : ModuleRCSFX
    {
        #region Fields
        [KSPField]
        public string clusterTransformName = string.Empty;

        [KSPField]
        public string convertResource = string.Empty;

        [KSPField]
        public double convertPerSec = 1.0f;

        [KSPField]
        public float maxAcceleration = 1.0f;

        [KSPField]
        public float finalAcceleration = 1.0f;

        [KSPField]
        public string rcsSoundEffectName = string.Empty;

        [KSPField(guiActive = true, guiName = "state")]
        public string flightState = string.Empty;
        #endregion

        #region Housekeeping
        Transform clusterTransform;
        #endregion

        #region overrides
        public override void OnAwake()
        {
            base.OnAwake();
            if (!string.IsNullOrEmpty(rcsSoundEffectName))
                this.part.Effect(rcsSoundEffectName, 0f, -1);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            if (!string.IsNullOrEmpty(clusterTransformName))
                clusterTransform = this.part.FindModelTransform(clusterTransformName);

            Fields["realISP"].guiActive = false;
            Fields["realISP"].guiActiveEditor = false;
        }

        public override void DeactivateFX()
        {
            base.DeactivateFX();

            if (!string.IsNullOrEmpty(rcsSoundEffectName))
                this.part.Effect(rcsSoundEffectName, 0f, -1);
        }

        public override void DeactivatePowerFX()
        {
            base.DeactivatePowerFX();

            if (!string.IsNullOrEmpty(rcsSoundEffectName))
                this.part.Effect(rcsSoundEffectName, 0f, -1);
        }

        protected override void UpdatePowerFX(bool running, int idx, float power)
        {
            // Don't play the effects if we aren't the active vessel.
            if (FlightGlobals.ActiveVessel != part.vessel)
            {
                this.part.Effect(rcsSoundEffectName, 0f, -1);
                return;
            }

            //Play RCS sound. Why it only works in one direction, I don't know.
            if (!string.IsNullOrEmpty(rcsSoundEffectName) && running)
                this.part.Effect(rcsSoundEffectName, power, -1);

            if (HighLogic.LoadedSceneIsFlight)
            {
                //Update thrust and mass flow
                UpdateThrust();

                //Update thruster cluster to vessel CoM
                if (clusterTransform != null)
                    clusterTransform.position = this.part.vessel.CoM;

                //Generate some output resource from the convertResource
                GeneratePropellant();
            }

            base.UpdatePowerFX(running, idx, power);
        }

        public override string GetInfo()
        {
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
            PartResourceDefinition definition;
            StringBuilder info = new StringBuilder();

            //Max acceleration
            info.AppendLine(string.Format(WBIKFSUtils.kMaxAcceleration, maxAcceleration));

            //Propellants
            info.AppendLine(WBIKFSUtils.kPropellants);
            for (int index = 0; index < this.propellants.Count; index++)
            {
                info.AppendLine("-" + this.propellants[index].displayName);
                info.Append(propellants[index].GetFlowModeDescription());
            }

            if (!string.IsNullOrEmpty(convertResource))
            {
                definition = definitions[convertResource];
                info.AppendLine(WBIKFSUtils.kRCSProducedFrom + definition.displayName);
            }
            info.AppendLine(WBIKFSUtils.kFuelFlowVaries);
            return info.ToString();
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Generates propellant for use in the RCS thruster.
        /// </summary>
        public virtual void GeneratePropellant()
        {
            if (string.IsNullOrEmpty(convertResource) || !this.part.Resources.Contains(resourceName))
                return;

            PartResource resource = this.part.Resources[resourceName];
            if (FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.RCS])
            {
                double amountObtained = this.part.RequestResource(convertResource, convertPerSec * TimeWarp.fixedDeltaTime);
                if (resource.amount + amountObtained * 5 <= resource.maxAmount)
                    resource.amount += amountObtained * 5;
                else
                    resource.amount = resource.maxAmount;
            }
            else
            {
                resource.amount = 0;
            }
        }

        /// <summary>
        /// Updates thrust based on vessel mass and maximum possible acceleration.
        /// </summary>
        public virtual void UpdateThrust()
        {
            //Get total mass
            float totalMass = 0.0f;
            if (HighLogic.LoadedSceneIsFlight)
                totalMass = vessel.GetTotalMass();
            else if (HighLogic.LoadedSceneIsEditor)
                totalMass = EditorLogic.fetch.ship.GetTotalMass();

            //Calculate max thrust.
            this.thrusterPower = maxAcceleration * totalMass;

            //Calculate max flow.
            this.realISP = this.atmosphereCurve.Evaluate(0f);
            maxFuelFlow = this.thrusterPower / (this.realISP * this.G);

            //Determine current acceleration
            finalAcceleration = this.thrusterPower / totalMass;
        }
        #endregion
    }
}
