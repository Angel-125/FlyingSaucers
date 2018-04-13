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
    [KSPModule("Resource Discharger")]
    public class WBIResourceDischarger : PartModule
    {
        const string statusMissing = "Missing: ";
        const string statusIdle = "Ready";
        const string statusOk = "Discharging";

        [KSPField]
        public string dischargedResources = string.Empty;

        [KSPField]
        public float landedDischargeRate = 0.01f;

        [KSPField]
        public string landedResourcesRequired = string.Empty;

        [KSPField]
        public float splashedDischargeRate = 0.01f;

        [KSPField]
        public string spashedResourcesRequired = string.Empty;

        [KSPField]
        public float atmosphericDischargeRate = 0.001f;

        [KSPField]
        public string atmosphereResourcesRequired = string.Empty;

        [KSPField]
        public float vacuumDischargeRate = 0.001f;

        [KSPField]
        public string vacuumResourcesRequired = string.Empty;

        [KSPField(guiActive = true, guiName = "Status")]
        public string status;

        [KSPField(guiActive = true, guiName = "Discharge Rate", guiFormat = "f3", guiUnits = "u/sec")]
        public float dischargeRate;

        [KSPField(isPersistant = true)]
        public double lastUpdateTime = 0f;

        public string[] resourcesToDump;
        public ResourceRatio[] landedResourceInputs;
        public ResourceRatio[] splashedResourceInputs;
        public ResourceRatio[] atmoResourceInputs;
        public ResourceRatio[] vaccResourceInputs;
        ModuleAnimateGeneric animation;

        public override string GetInfo()
        {
            getInputs();
            StringBuilder info = new StringBuilder();

            //List of resources to dump
            if (resourcesToDump.Length > 1)
            {
                info.Append("Discharges: ");
                for (int index = 0; index < resourcesToDump.Length - 1; index++)
                    info.Append(resourcesToDump[index] + ", ");
                info.AppendLine(" and " + resourcesToDump[resourcesToDump.Length - 1]);
            }
            else
            {
                info.AppendLine("Discharges " + dischargedResources);
            }

            //Discharge rates
            info.AppendLine(" ");
            info.AppendLine("<b>Discharge Rates</b>");

            //Landed
            info.AppendLine("<b>Landed: </b>" + formatRate(landedDischargeRate));
            if (landedResourceInputs != null)
                info.AppendLine(formatConsumedResources(landedResourceInputs));
            info.AppendLine(" ");

            //Splashed
            info.AppendLine("<b>Splashed: </b>" + formatRate(splashedDischargeRate));
            if (splashedResourceInputs != null)
                info.AppendLine(formatConsumedResources(splashedResourceInputs));
            info.AppendLine(" ");

            //Atmo
            info.AppendLine("<b>Atmosphere: </b>" + formatRate(atmosphericDischargeRate));
            if (atmoResourceInputs != null)
                info.AppendLine(formatConsumedResources(atmoResourceInputs));
            info.AppendLine(" ");

            //Vacuum
            info.AppendLine("<b>Vacuum: </b>" + formatRate(vacuumDischargeRate));
            if (vaccResourceInputs != null)
                info.AppendLine(formatConsumedResources(vaccResourceInputs));

            return info.ToString();
        }

        public void FixedUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            if (animation != null)
            {
                if (animation.Events["Toggle"].guiName == animation.startEventGUIName)
                {
                    dischargeRate = 0f;
                    status = statusIdle;
                    return;
                }
            }

            //Timekeeping
            double currentTime = Planetarium.GetUniversalTime();
            double elapsedTime = currentTime - lastUpdateTime;
            lastUpdateTime = currentTime;

            //Do we have resources to discharge?
            if (!hasResourcesToDump())
            {
                dischargeRate = 0f;
                status = statusIdle;
                return;
            }

            //Consume inputs based upon vehicle situation
            switch (this.part.vessel.situation)
            {
                //Use atmospheric rate and resource consumption if in atmosphere
                //otherwise use vacuum rate and consumption
                default:
                    if (this.part.vessel.dynamicPressurekPa > 0f)
                    {
                        dischargeRate = atmosphericDischargeRate;
                        if (atmoResourceInputs != null)
                        {
                            if (!consumeInputs(atmoResourceInputs, elapsedTime))
                                return;
                        }
                    }

                    else //Vacuum
                    {
                        dischargeRate = vacuumDischargeRate;

                        if (vaccResourceInputs != null)
                        {
                            dischargeRate = vacuumDischargeRate;
                            if (!consumeInputs(vaccResourceInputs, elapsedTime))
                                return;
                        }
                    }
                    break;

                //Use landed rate and consumption
                case Vessel.Situations.LANDED:
                case Vessel.Situations.PRELAUNCH:
                    dischargeRate = landedDischargeRate;
                    if (landedResourceInputs != null)
                    {
                        if (!consumeInputs(landedResourceInputs, elapsedTime))
                            return;
                    }
                    break;

                //Use splashed rate and consumption
                case Vessel.Situations.SPLASHED:
                    dischargeRate = splashedDischargeRate;
                    if (splashedResourceInputs != null)
                    {
                        if (!consumeInputs(splashedResourceInputs, elapsedTime))
                            return;
                    }
                    break;
            }

            //If we're still good, then discharge the desired resources
            status = statusOk;
            float dischargeRatePerFrame = dischargeRate * TimeWarp.fixedDeltaTime;

            //Account for catchup time
            if (elapsedTime >= 1.0f)
                dischargeRatePerFrame = dischargeRate * (float)elapsedTime;

            for (int index = 0; index < resourcesToDump.Length; index++)
            {
                this.part.RequestResource(resourcesToDump[index], dischargeRatePerFrame);
            }
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return;

            //Animation
            animation = this.part.FindModuleImplementing<ModuleAnimateGeneric>();
            if (animation != null)
            {
                animation.Fields["status"].guiName = "Emitter State";
            }

            //Get the inputs
            getInputs();

            //Timekeeping
            if (lastUpdateTime == 0f)
                lastUpdateTime = Planetarium.GetUniversalTime();
        }

        string formatConsumedResources(ResourceRatio[] inputs)
        {
            StringBuilder info = new StringBuilder();

            info.Append("Consumes ");
            for (int index = 0; index < inputs.Length; index++)
                info.Append(formatResource(inputs[index].ResourceName, inputs[index].Ratio));
            info.AppendLine(" ");

            string consumedResources = info.ToString().TrimEnd(new char[] { ',' });
            return consumedResources;
        }

        string formatResource(string resourceName, double ratio)
        {
            if (ratio < 0.0001)
                return resourceName + string.Format(": {0:f2}/day", ratio * (double)KSPUtil.dateTimeFormatter.Day);
            else if (ratio < 0.01)
                return resourceName + string.Format(": {0:f2}/hr", ratio * (double)KSPUtil.dateTimeFormatter.Hour);
            else
                return resourceName + string.Format(": {0:f2}/sec", ratio);
        }

        string formatRate(double rate)
        {
            if (rate < 0.0001)
                return string.Format("{0:f2}/day", rate * (double)KSPUtil.dateTimeFormatter.Day);
            else if (rate < 0.01)
                return string.Format("{0:f2}/hr", rate * (double)KSPUtil.dateTimeFormatter.Hour);
            else
                return string.Format("{0:f2}/sec", rate);
        }

        void getInputs()
        {
            char[] semicolonSeparator = new char[] { ';' };

            //Get the outputs
            if (string.IsNullOrEmpty(dischargedResources))
                return;
            resourcesToDump = dischargedResources.Split(semicolonSeparator);

            //Get inputs
            if (!string.IsNullOrEmpty(landedResourcesRequired))
            {
                WBIKFSUtils.Log("[" + this.ClassName + "] - Adding required resources for landed state");
                landedResourceInputs = getInputs(landedResourcesRequired);
            }
            if (!string.IsNullOrEmpty(spashedResourcesRequired))
            {
                WBIKFSUtils.Log("[" + this.ClassName + "] - Adding required resources for splashed state");
                splashedResourceInputs = getInputs(spashedResourcesRequired);
            }
            if (!string.IsNullOrEmpty(atmosphereResourcesRequired))
            {
                WBIKFSUtils.Log("[" + this.ClassName + "] - Adding required resources for atmosphere state");
                atmoResourceInputs = getInputs(atmosphereResourcesRequired);
            }
            if (!string.IsNullOrEmpty(vacuumResourcesRequired))
            {
                WBIKFSUtils.Log("[" + this.ClassName + "] - Adding required resources for vacuum state");
                vaccResourceInputs = getInputs(vacuumResourcesRequired);
            }
        }

        bool hasResourcesToDump()
        {
            string resourceName;
            double amount, maxAmount;
            PartResourceDefinition resourceDef = null;
            PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;

            for (int index = 0; index < resourcesToDump.Length; index++)
            {
                resourceName = resourcesToDump[index];

                //Get resource definition
                if (definitions.Contains(resourceName))
                    resourceDef = definitions[resourceName];

                //Check resource amount
                this.part.GetConnectedResourceTotals(resourceDef.id, out amount, out maxAmount);
                if (amount >= 0.0001f)
                    return true;
            }

            return false;
        }

        bool consumeInputs(ResourceRatio[] inputs, double elapsedTime)
        {
            double amountObtained = 0f;
            double demand;
            for (int index = 0; index < inputs.Length; index++)
            {
                demand = inputs[index].Ratio;
                if (elapsedTime >= 1.0f)
                    demand *= elapsedTime;

                amountObtained = this.part.RequestResource(inputs[index].ResourceName, demand);
                if (amountObtained / demand < 0.9999f)
                {
                    PartResourceDefinition resourceDef = null;
                    PartResourceDefinitionList definitions = PartResourceLibrary.Instance.resourceDefinitions;
                    resourceDef = definitions[inputs[index].ResourceName];

                    status = statusMissing + resourceDef.displayName;
                    dischargeRate = 0f;
                    return false;
                }
            }
            return true;
        }

        ResourceRatio[] getInputs(string inputResources)
        {
            char[] semicolonSeparator = new char[] { ';' };
            char[] commaSeparator = new char[] { ',' };
            string[] inputs;
            string[] parameters;
            List<ResourceRatio> inputResourceRatios = new List<ResourceRatio>();
            ResourceRatio resourceRatio;
            float amount;

            inputs = inputResources.Split(semicolonSeparator);
            for (int index = 0; index < inputs.Length; index++)
            {
                parameters = inputs[index].Split(commaSeparator);
                if (parameters.Length != 2)
                    continue;

                //index 0: resource name
                resourceRatio = new ResourceRatio();
                resourceRatio.ResourceName = parameters[0];

                //index 1: amount
                if (!float.TryParse(parameters[1], out amount))
                    continue;
                resourceRatio.Ratio = amount;

                inputResourceRatios.Add(resourceRatio);
                WBIKFSUtils.Log("[" + this.ClassName + "] - added input resource: " + resourceRatio.ResourceName + " amount: " + resourceRatio.Ratio);
            }

            return inputResourceRatios.ToArray();
        }
    }
}
