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
    public class WBIGraviticGenerator : WBIModuleResourceConverterFX
    {
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
                    ratio = outputList[index].Ratio;
                    if (this.part.Resources.Contains(resourceName))
                    {
                        if (this.part.Resources[resourceName].amount > 0.0f)
                            this.part.RequestResource(resourceName, ratio * TimeWarp.fixedDeltaTime, ResourceFlowMode.NO_FLOW);
                    }
                }
            }
        }

        /*
        protected override void PostProcess(ConverterResults result, double deltaTime)
        {
            base.PostProcess(result, deltaTime);

            //If we're missing resources then stop the converter.
            if (!string.IsNullOrEmpty(result.Status))
            {
                if (result.Status.Contains(Localizer.Format("#autoLOC_261263")))
                    StopResourceConverter();
            }
        }
        */
    }
}
