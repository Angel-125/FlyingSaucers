using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KerbalActuators;
using KSP.Localization;

namespace WildBlueIndustries
{
    #region ResourceMode
    internal class ResourceMode
    {
        public string name;
        public string displayName;
        public List<ModuleResource> inputResources;
        public List<ModuleResource> outputResources;
        public List<ModuleResource> drainedResources;

        public void Load(ConfigNode node)
        {
            if (!node.HasValue("name") || !node.HasValue("displayName"))
                return;

            name = node.GetValue("name");
            displayName = Localizer.Format(node.GetValue("displayName"));

            inputResources = new List<ModuleResource>();
            outputResources = new List<ModuleResource>();
            drainedResources = new List<ModuleResource>();

            ConfigNode[] nodes = null;
            ModuleResource resource;
            if (node.HasNode("RESOURCE"))
            {
                nodes = node.GetNodes("RESOURCE");
                for (int index = 0; index < nodes.Length; index++)
                {
                    resource = new ModuleResource();
                    resource.Load(nodes[index]);
                    loadShutOffPercent(resource, nodes[index]);
                    inputResources.Add(resource);
                }
            }

            if (node.HasNode("INPUT_RESOURCE"))
            {
                nodes = node.GetNodes("INPUT_RESOURCE");
                for (int index = 0; index < nodes.Length; index++)
                {
                    resource = new ModuleResource();
                    resource.Load(nodes[index]);
                    loadShutOffPercent(resource, nodes[index]);
                    inputResources.Add(resource);
                }
            }

            if (node.HasNode("OUTPUT_RESOURCE"))
            {
                nodes = node.GetNodes("OUTPUT_RESOURCE");
                for (int index = 0; index < nodes.Length; index++)
                {
                    resource = new ModuleResource();
                    resource.Load(nodes[index]);
                    loadShutOffPercent(resource, nodes[index]);
                    outputResources.Add(resource);
                }
            }

            if (node.HasNode("DRAINED_RESOURCE"))
            {
                nodes = node.GetNodes("DRAINED_RESOURCE");
                for (int index = 0; index < nodes.Length; index++)
                {
                    resource = new ModuleResource();
                    resource.Load(nodes[index]);
                    loadShutOffPercent(resource, nodes[index]);
                    drainedResources.Add(resource);
                }
            }
        }

        private void loadShutOffPercent(ModuleResource resource, ConfigNode node)
        {
            if (!node.HasValue("shutOffPercent"))
                return;

            float shutOffPercent = 0;
            if (float.TryParse(node.GetValue("shutOffPercent"), out shutOffPercent))
                resource.shutOffPercent = shutOffPercent;
        }
    }
    #endregion

    /// <summary>
    /// This class adds the ability to generate and consume resources in addition to the normal gravitic engine functions.
    /// It is intended for self-contained gravitic engines.
    /// </summary>
    public class WBIGraviticEngineGenerator : WBIGraviticEngine
    {
        #region Fields
        [KSPField(isPersistant = true)]
        public int selectedModeIndex = -1;

        [KSPField(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KFS_currentResourceMode")]
        public string currentModeDisplay = string.Empty;

        [KSPField(guiActive = true, guiName = "#LOC_KFS_generatorStatus")]
        public string generatorStatus = string.Empty;

        [KSPField]
        public string defaultMode = string.Empty;
        #endregion

        #region Housekeeping
        List<ResourceMode> resourceModes = null;
        ResourceMode currentMode = null;
        bool allowUnflameout = true;
        #endregion

        #region Events
        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "#LOC_KFS_nextResourceMode")]
        public void NextMode()
        {
            selectedModeIndex = (selectedModeIndex + 1) % resourceModes.Count;
            updateResourceMode();
        }
        #endregion

        #region Overrides
        public override void OnFixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (EngineIgnited && !flameout)
                {
                    runGenerator();
                }
                else
                {
                    drainResources();
                    checkFullResources();
                }
            }

            base.OnFixedUpdate();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            loadResourceModes();
            if (resourceModes.Count > 0)
            {
                // Update default mode
                if (selectedModeIndex < 0)
                {
                    if (!string.IsNullOrEmpty(defaultMode))
                        selectedModeIndex = findDefaultResourceMode();
                    if (selectedModeIndex < 0)
                        selectedModeIndex = 0;
                }
                updateResourceMode();
            }

            // If there is only one resource mode then disable the resource mode selector and display.
            if (resourceModes.Count == 1)
            {
                Fields["currentModeDisplay"].guiActive = false;
                Fields["currentModeDisplay"].guiActiveEditor = false;
                Events["NextMode"].active = false;
            }
        }

        public override void UnFlameout(bool showFX = true)
        {
            if (allowUnflameout)
                base.UnFlameout(showFX);
        }
        #endregion

        #region Helpers
        private int findDefaultResourceMode()
        {
            int count = resourceModes.Count;

            ResourceMode resourceMode;
            for (int index = 0; index < count; index++)
            {
                resourceMode = resourceModes[index];
                if (resourceMode.name == defaultMode)
                    return index;
            }

            return -1;
        }

        private void checkFullResources()
        {
            if (allowUnflameout)
                return;

            int count = resHandler.outputResources.Count;
            ModuleResource outputResource;
            double amount;
            double maxAmount;

            // We need to handle the generator's equivalent of DumpExcess = false manually.
            for (int index = 0; index < count; index++)
            {
                outputResource = resHandler.outputResources[index];
                if (outputResource.shutOffPercent <= 0)
                    continue;

                part.GetConnectedResourceTotals(outputResource.id, out amount, out maxAmount);
                if (amount >= maxAmount)
                {
                    generatorStatus = outputResource.resourceDef.displayName + " " + Localizer.Format("#LOC_KFS_resourceFull");
                    return;
                }
            }

            allowUnflameout = true;
        }

        private void runGenerator()
        {
            resHandler.UpdateModuleResourceInputs(ref generatorStatus, 1, 0.1, true, true);

            int count = resHandler.inputResources.Count;
            for (int index = 0; index < count; index++)
            {
                if (!resHandler.inputResources[index].available)
                    return;
            }

            resHandler.UpdateModuleResourceOutputs();
            count = resHandler.outputResources.Count;
            ModuleResource outputResource;
            double amount;
            double maxAmount;
            for (int index = 0; index < count; index++)
            {
                outputResource = resHandler.outputResources[index];
                if (outputResource.shutOffPercent <= 0)
                    continue;

                part.GetConnectedResourceTotals(outputResource.id, out amount, out maxAmount);
                if ((amount / maxAmount) >= (outputResource.shutOffPercent / 100))
                {
                    string message = outputResource.resourceDef.displayName + " " + Localizer.Format("#LOC_KFS_resourceFull");
                    allowUnflameout = false;
                    Flameout(message);
                    generatorStatus = message;
                    Shutdown();
                    return;
                }
            }

            generatorStatus = Localizer.Format("#LOC_KFS_generatorStatusGood");
        }

        private void drainResources()
        {
            if (currentMode == null || currentMode.drainedResources.Count == 0)
                return;

            int count = currentMode.drainedResources.Count;
            ModuleResource resource;

            for (int index = 0; index < count; index++)
            {
                resource = currentMode.drainedResources[index];

                this.part.RequestResource(resource.name, resource.rate, resource.flowMode);
            }

            generatorStatus = Localizer.Format("#LOC_KFS_generatorStatusOff");
        }

        private void updateResourceMode()
        {
            currentMode = resourceModes[selectedModeIndex];
            currentModeDisplay = currentMode.displayName;

            resHandler.inputResources.Clear();
            resHandler.inputResources.AddRange(currentMode.inputResources);

            resHandler.outputResources.Clear();
            resHandler.outputResources.AddRange(currentMode.outputResources);
        }

        private void loadResourceModes()
        {
            resourceModes = new List<ResourceMode>();
            ConfigNode node = getPartConfigNode();
            if (node == null || !node.HasNode("RESOURCE_MODE"))
                return;

            ConfigNode[] nodes = node.GetNodes("RESOURCE_MODE");
            ResourceMode resourceMode;
            ConfigNode resourceNode;
            for (int index = 0; index < nodes.Length; index++)
            {
                resourceNode = nodes[index];

                resourceMode = new ResourceMode();
                resourceMode.Load(resourceNode);
                resourceModes.Add(resourceMode);
            }
        }

        private ConfigNode getPartConfigNode()
        {
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return null;
            if (this.part.partInfo.partConfig == null)
                return null;
            ConfigNode[] nodes = this.part.partInfo.partConfig.GetNodes("MODULE");
            ConfigNode partConfigNode = null;
            ConfigNode node = null;
            string moduleName;

            //Get the switcher config node.
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (node.HasValue("name"))
                {
                    moduleName = node.GetValue("name");
                    if (moduleName == this.ClassName)
                    {
                        partConfigNode = node;
                        break;
                    }
                }
            }

            return partConfigNode;
        }
        #endregion
    }
}
