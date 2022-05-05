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
    public class WBIGraviticGenerator : WBIModuleResourceConverterFX
    {
        bool drainedResourceProduced = false;
        string resourcesDrainedHash = string.Empty;

        public void OnDestroy()
        {
            GameEvents.OnResourceConverterOutput.Remove(onResourceConverterOutput);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            GameEvents.OnResourceConverterOutput.Add(onResourceConverterOutput);

            int count = outputList.Count;
            for (int index = 0; index < count; index++)
            {
                resourcesDrainedHash += outputList[index].ResourceName;
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            //Drain the output resources
            if (!IsActivated)
            {
                int outputCount = outputList.Count;
                string resourceName;
                double ratio;
                for (int index = 0; index < outputCount; index++)
                {
                    resourceName = outputList[index].ResourceName;
                    if (resourceName == "ElectricCharge")
                        continue;
                    ratio = outputList[index].Ratio;
                    if (this.part.Resources.Contains(resourceName))
                    {
                        if (this.part.Resources[resourceName].amount > 0.0f)
                            this.part.RequestResource(resourceName, ratio * TimeWarp.fixedDeltaTime, ResourceFlowMode.NO_FLOW);
                    }
                }

                drainedResourceProduced = false;
            }
        }

        private void onResourceConverterOutput(PartModule converter, string resourceName, double amount)
        {
            if (resourcesDrainedHash.Contains(resourceName))
                drainedResourceProduced = true;
        }
    }
}
