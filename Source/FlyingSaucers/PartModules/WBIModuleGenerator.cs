using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KerbalActuators;
using KSP.Localization;

/*
Source code copyright 2019-2020, by Michael Billard (Angel-125)
License: GPLV3

Wild Blue Industries is trademarked by Michael Billard and may be used for non-commercial purposes. All other rights reserved.
Note that Wild Blue Industries is a ficticious entity 
created for entertainment purposes. It is in no way meant to represent a real entity.
Any similarity to a real entity is purely coincidental.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

namespace WildBlueIndustries
{
    [KSPModule("Resource Converter")]
    public class WBIModuleGenerator: ModuleResourceConverter
    {
        [KSPField]
        public bool guiVisible = true;

        [KSPField]
        public string startEffect = string.Empty;

        [KSPField]
        public string stopEffect = string.Empty;

        [KSPField]
        public string runningEffect = string.Empty;

        [KSPField(guiActiveEditor = true, guiName = "Particle Effects", isPersistant = true)]
        [UI_Toggle(enabledText = "On", disabledText = "Off")]
        public bool showParticleEffects = true;

        [KSPField]
        public bool affectsLights = true;

        public List<ModuleResource> drainedResources = new List<ModuleResource>();

        private Light[] lights;
        private KSPParticleEmitter[] emitters;

        /// <summary>
        /// Clears resource references when the module is destroyed.
        /// </summary>
        public void OnDestroy()
        {
            if (drainedResources != null)
                drainedResources.Clear();

            lights = null;
            emitters = null;
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            Events["StartResourceConverter"].guiActive = guiVisible;
            Events["StopResourceConverter"].guiActive = guiVisible;

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            lights = part.gameObject.GetComponentsInChildren<Light>();
            emitters = part.GetComponentsInChildren<KSPParticleEmitter>();
            setupLightsAndEmitters();

            if (IsActivated)
            {
                part.InitializeEffects();
                if (!string.IsNullOrEmpty(runningEffect))
                    part.Effect(runningEffect, 1.0f);
            }

            Fields["showParticleEffects"].guiName = ConverterName + " effects";
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            if (drainedResources == null)
                drainedResources = new List<ModuleResource>();
            else
                drainedResources.Clear();

            ConfigNode[] nodes = null;
            ModuleResource resource;
            if (node.HasNode("DRAINED_RESOURCE"))
            {
                nodes = node.GetNodes("DRAINED_RESOURCE");
                for (int index = 0; index < nodes.Length; index++)
                {
                    resource = loadDrainedResource(nodes[index]);
                    if (resource != null)
                        drainedResources.Add(resource);
                }
            }
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (HighLogic.LoadedSceneIsFlight && IsActivated == false)
                drainResources();
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight || !IsActivated)
                return;

            if (!string.IsNullOrEmpty(runningEffect))
                part.Effect(runningEffect, 1.0f);

            if (emitters == null)
                return;

            for (int index = 0; index < emitters.Length; index++)
            {
                emitters[index].emit = showParticleEffects;
                emitters[index].enabled = showParticleEffects;
            }
        }

        public override void OnInactive()
        {
            base.OnInactive();
            StopResourceConverter();
        }

        public override void StartResourceConverter()
        {
            base.StartResourceConverter();
            setupLightsAndEmitters();

            if (!string.IsNullOrEmpty(startEffect))
                part.Effect(startEffect, 1.0f);
            if (!string.IsNullOrEmpty(runningEffect))
                part.Effect(runningEffect, 1.0f);
        }

        public override void StopResourceConverter()
        {
            base.StopResourceConverter();
            setupLightsAndEmitters();

            if (!string.IsNullOrEmpty(runningEffect))
                part.Effect(runningEffect, 0.0f);
            if (!string.IsNullOrEmpty(stopEffect))
                part.Effect(stopEffect, 1.0f);
        }

        private void loadShutOffPercent(ModuleResource resource, ConfigNode node)
        {
            if (!node.HasValue("shutOffPercent"))
                return;

            float shutOffPercent = 0;
            if (float.TryParse(node.GetValue("shutOffPercent"), out shutOffPercent))
                resource.shutOffPercent = shutOffPercent;
        }

        private void drainResources()
        {
            if (drainedResources == null || drainedResources.Count <= 0 || part == null)
                return;

            int count = drainedResources.Count;
            ModuleResource resource;

            for (int index = 0; index < count; index++)
            {
                resource = drainedResources[index];
                if (resource == null || string.IsNullOrEmpty(resource.name) || !part.Resources.Contains(resource.name))
                    continue;

                if (part.Resources[resource.name].amount <= 0)
                    return;

                this.part.RequestResource(resource.name, resource.rate, resource.flowMode);
            }
        }

        private void setupLightsAndEmitters()
        {
            if (lights != null && affectsLights)
            {
                for (int index = 0; index < lights.Length; index++)
                    lights[index].intensity = IsActivated ? 1.0f : 0.0f;
            }

            if (emitters == null)
                return;

            for (int index = 0; index < emitters.Length; index++)
            {
                bool isEmitting = IsActivated && showParticleEffects;
                emitters[index].emit = isEmitting;
                emitters[index].enabled = isEmitting;
            }
        }

        /// <summary>
        /// Loads a drained resource definition while accepting both ModuleResource-style
        /// fields (name/rate/resourceFlowMode) and older converter-style fields
        /// (ResourceName/Ratio/FlowMode) used by existing FlyingSaucers configs.
        /// </summary>
        /// <param name="node">The DRAINED_RESOURCE config node to load.</param>
        /// <returns>A configured ModuleResource, or null when the node is invalid.</returns>
        private ModuleResource loadDrainedResource(ConfigNode node)
        {
            if (node == null)
                return null;

            string resourceName = node.GetValue("name");
            if (string.IsNullOrEmpty(resourceName))
                resourceName = node.GetValue("ResourceName");

            if (string.IsNullOrEmpty(resourceName))
            {
                Debug.LogWarning("[WBIModuleGenerator] - Skipping DRAINED_RESOURCE with no name/ResourceName.");
                return null;
            }

            string rate = node.GetValue("rate");
            if (string.IsNullOrEmpty(rate))
                rate = node.GetValue("Ratio");

            string flowMode = node.GetValue("resourceFlowMode");
            if (string.IsNullOrEmpty(flowMode))
                flowMode = node.GetValue("FlowMode");

            ConfigNode moduleResourceNode = new ConfigNode(node.name);
            moduleResourceNode.AddValue("name", resourceName);
            if (!string.IsNullOrEmpty(rate))
                moduleResourceNode.AddValue("rate", rate);
            if (!string.IsNullOrEmpty(flowMode))
                moduleResourceNode.AddValue("resourceFlowMode", flowMode);
            if (node.HasValue("amount"))
                moduleResourceNode.AddValue("amount", node.GetValue("amount"));
            if (node.HasValue("varyTime"))
                moduleResourceNode.AddValue("varyTime", node.GetValue("varyTime"));
            if (node.HasValue("useSI"))
                moduleResourceNode.AddValue("useSI", node.GetValue("useSI"));

            ModuleResource resource = new ModuleResource();
            try
            {
                resource.Load(moduleResourceNode);
                loadShutOffPercent(resource, node);
                return resource;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[WBIModuleGenerator] - Unable to load DRAINED_RESOURCE " + resourceName + ": " + ex.Message);
                return null;
            }
        }
    }
}
