using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KerbalActuators;
using KSP.Localization;
using ModularFI;

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
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    class WBIMFIGravityOverride : MonoBehaviour
    {
        public void Awake()
        {
            ModularVesselPrecalculate.RegisterCalculateGravityOverride(calculateGravity);
        }

        protected void calculateGravity()
        {
            Vessel activeVessel = FlightGlobals.ActiveVessel;
            CelestialBody mainBody = FlightGlobals.getMainBody(activeVessel.CoMD);
            double originalGMag = mainBody.gMagnitudeAtCenter;
            mainBody.gMagnitudeAtCenter = 0.0f;
            if (activeVessel.precalc is ModularVesselPrecalculate)
            {
                ModularVesselPrecalculate modularVesselPrecalc = (ModularVesselPrecalculate)activeVessel.precalc;
                modularVesselPrecalc.BaseCalculateGravity();
            }
            mainBody.gMagnitudeAtCenter = originalGMag;
        }
    }
}
