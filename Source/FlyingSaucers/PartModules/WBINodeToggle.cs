using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KerbalActuators;
using KSP.Localization;

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
    public class WBINodeToggle: PartModule
    {
        [KSPField]
        public string primaryNodes = "Node1;Node2";

        [KSPField]
        public string primaryNodesString = "Outer";

        [KSPField]
        public string primaryMeshName = string.Empty;

        [KSPField]
        public string secondaryNodes = "Node3;Node4";

        [KSPField]
        public string secondaryNodesString = "Inner";

        [KSPField]
        public string secondaryMeshName = string.Empty;

        [KSPField(isPersistant = true)]
        public bool usePrimaryNodes = true;

        float originalRadius = 0.0f;

        [KSPEvent(guiActiveEditor = true)]
        public void ToggleNodes()
        {
            usePrimaryNodes = !usePrimaryNodes;
            if (usePrimaryNodes)
                Events["ToggleNodes"].guiName = primaryNodesString;
            else
                Events["ToggleNodes"].guiName = secondaryNodesString;

            updateNodeStates();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            if (usePrimaryNodes)
                Events["ToggleNodes"].guiName = primaryNodesString;
            else
                Events["ToggleNodes"].guiName = secondaryNodesString;

            char[] delimiters = new char[] { ';' };
            string[] nodeNames = primaryNodes.Split(delimiters);
            AttachNode node = this.part.FindAttachNode(nodeNames[0]);
            if (node != null)
                originalRadius = node.radius;

            updateNodeStates();
        }

        protected void updateNodeStates()
        {
            char[] delimiters = new char[] { ';' };
            string[] nodeNames;
            if (usePrimaryNodes)
            {
                nodeNames = primaryNodes.Split(delimiters);
                updateNodeStates(nodeNames, true);
                nodeNames = secondaryNodes.Split(delimiters);
                updateNodeStates(nodeNames, false);
                setMeshVisible(primaryMeshName, true);
                setMeshVisible(secondaryMeshName, false);
            }
            else
            {
                nodeNames = primaryNodes.Split(delimiters);
                updateNodeStates(nodeNames, false);
                nodeNames = secondaryNodes.Split(delimiters);
                updateNodeStates(nodeNames, true);
                setMeshVisible(primaryMeshName, false);
                setMeshVisible(secondaryMeshName, true);
            }
        }

        protected void setMeshVisible(string meshName, bool isVisible)
        {
            if (string.IsNullOrEmpty(meshName))
                return;
            string[] nameTransforms = meshName.Split(';');
            Transform[] targets;

            foreach (string transform in nameTransforms)
            {
                //Get the targets
                targets = part.FindModelTransforms(transform);
                if (targets == null)
                {
                    Debug.Log("No targets found for " + transform);
                    return;
                }

                foreach (Transform target in targets)
                {
                    target.gameObject.SetActive(isVisible);
                    Collider collider = target.gameObject.GetComponent<Collider>();
                    if (collider != null)
                        collider.enabled = isVisible;
                }
            }
        }

        protected void updateNodeStates(string[] nodeNames, bool isVisible)
        {
            AttachNode node;
            foreach (string nodeName in nodeNames)
            {
                node = this.part.FindAttachNode(nodeName);
                if (node != null)
                {
                    if (isVisible)
                    {
                        node.nodeType = AttachNode.NodeType.Stack;
                        node.radius = originalRadius;
                    }
                    else
                    {
                        node.nodeType = AttachNode.NodeType.Dock;
                        node.radius = 0.00001f;
                    }
                }
            }
        }
    }
}
