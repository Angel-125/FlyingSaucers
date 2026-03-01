using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KerbalActuators;
using KSP.Localization;
using WBIResources;

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
    public class WBIModuleGenerator: WBIModuleResourceConverterFX
    {
        [KSPField]
        public bool guiVisible = true;

        public List<ModuleResource> drainedResources;

        public void OnDestroy()
        {
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            ConfigNode[] nodes = null;
            ModuleResource resource;
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

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();

            if (HighLogic.LoadedSceneIsFlight && IsActivated == false)
                drainResources();
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
            int count = drainedResources.Count;
            ModuleResource resource;

            for (int index = 0; index < count; index++)
            {
                resource = drainedResources[index];
                if (part.Resources[resource.name].amount <= 0)
                    return;

                this.part.RequestResource(resource.name, resource.rate, resource.flowMode);
            }
        }
    }
}
