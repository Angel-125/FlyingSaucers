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
    internal struct ConditionalMeshToggle
    {
        public string[] meshNames;
        public string techRequired;
    }

    /// <summary>
    /// This part module will show or hide meshes based on conditions set. Initially, the condition is the state of researched/not researched tech.
    /// </summary>
    public class WBIModuleConditionalMeshToggle: PartModule
    {
        #region Constants
        const string kMeshToggleNode = "MESH_TOGGLE";
        const string kMeshName = "meshName";
        const string kTechNode = "techRequired";
        #endregion

        #region Fields
        [KSPField]
        public bool debugMode;

        /// <summary>
        /// ID of the module. Used to find the proper config node.
        /// </summary>
        [KSPField]
        public string moduleID = string.Empty;
        #endregion

        #region Housekeeping
        List<ConditionalMeshToggle> conditionalMeshToggles;
        #endregion

        #region Overrides
        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight || HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX)
                return;

            loadMeshToggles();

            // Check the desired tech node. If researched then show the mesh. If not, hide the mesh.
            foreach (ConditionalMeshToggle meshToggle in conditionalMeshToggles)
            {
                ProtoTechNode techNode = AssetBase.RnDTechTree.FindTech(meshToggle.techRequired);
                if (techNode != null && techNode.state == RDTech.State.Available)
                {
                    setMeshesVisible(meshToggle.meshNames, true);
                }
                else
                {
                    setMeshesVisible(meshToggle.meshNames, false);
                }
            }
        }
        #endregion

        #region Helpers
        void setMeshesVisible(string[] meshNames, bool isVisible)
        {
            foreach (string meshName in meshNames)
            {
                //Get the targets
                Transform[] targets = part.FindModelTransforms(meshName);
                if (targets == null)
                {
                    if (debugMode)
                    {
                        Debug.Log("[WBIModuleConditionalMeshToggle] - Could not locate a mesh named " + meshName);
                    }
                    continue;
                }

                foreach (Transform target in targets)
                {
                    target.gameObject.SetActive(isVisible);
                    Collider collider = target.gameObject.GetComponent<Collider>();
                    if (collider != null)
                        collider.enabled = isVisible;

                    if (debugMode)
                    {
                        Debug.Log("[WBIModuleConditionalMeshToggle] - Set " + meshName + " visibility to " + isVisible);
                    }
                }
            }
        }

        void loadMeshToggles()
        {
            conditionalMeshToggles = new List<ConditionalMeshToggle>();
            ConfigNode node = getPartConfigNode();
            if (node == null || node.HasNode(kMeshToggleNode) == false)
                return;

            ConfigNode[] meshToggleNodes = node.GetNodes(kMeshToggleNode);
            ConditionalMeshToggle conditionalMeshToggle;
            foreach (ConfigNode meshToggleNode in meshToggleNodes)
            {
                if (meshToggleNode.HasValue(kMeshName) == false || meshToggleNode.HasValue(kTechNode) == false)
                    continue;

                conditionalMeshToggle = new ConditionalMeshToggle();
                conditionalMeshToggle.techRequired = meshToggleNode.GetValue(kTechNode);
                conditionalMeshToggle.meshNames = meshToggleNode.GetValues(kMeshName);

                if (debugMode)
                {
                    Debug.Log("[WBIModuleConditionalMeshToggle] - loaded Mesh Toggle");
                    Debug.Log("[WBIModuleConditionalMeshToggle] - techRequired: " + conditionalMeshToggle.techRequired);
                    Debug.Log("[WBIModuleConditionalMeshToggle] - Mesh Names:");
                    foreach(string meshName in conditionalMeshToggle.meshNames)
                    {
                        Debug.Log("[WBIModuleConditionalMeshToggle] - " + meshName);
                    }
                }

                conditionalMeshToggles.Add(conditionalMeshToggle);
            }
        }

        /// <summary>
        /// Retrieves the module's config node from the part config.
        /// </summary>
        /// <param name="className">Optional. The name of the part module to search for.</param>
        /// <returns>A ConfigNode for the part module.</returns>
        public ConfigNode getPartConfigNode(string className = "")
        {
            if (!HighLogic.LoadedSceneIsEditor && !HighLogic.LoadedSceneIsFlight)
                return null;
            if (this.part.partInfo.partConfig == null)
                return null;
            ConfigNode[] nodes = this.part.partInfo.partConfig.GetNodes("MODULE");
            ConfigNode partConfigNode = null;
            ConfigNode node = null;
            string moduleName;
            string nodeModuleID;

            //Get the config node.
            for (int index = 0; index < nodes.Length; index++)
            {
                node = nodes[index];
                if (node.HasValue("name"))
                {
                    moduleName = node.GetValue("name");
                    if (moduleName == this.ClassName || moduleName == className)
                    {
                        if (!string.IsNullOrEmpty(moduleID) && node.HasValue("moduleID"))
                        {
                            nodeModuleID = node.GetValue("moduleID");
                            if (moduleID == nodeModuleID)
                            {
                                partConfigNode = node;
                                break;
                            }
                        }
                        else
                        {
                            partConfigNode = node;
                            break;
                        }
                    }
                }
            }

            return partConfigNode;
        }
        #endregion
    }
}
