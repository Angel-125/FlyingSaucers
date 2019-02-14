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

            //make sure the vessel has an active gravitic engine that's in hover mode, and the vessel is flying.

            //Get the celestial body's original gravity at its center
            CelestialBody mainBody = FlightGlobals.getMainBody(activeVessel.CoMD);
            double originalGMag = mainBody.gMagnitudeAtCenter;

            //Hack gravity! :)
            mainBody.gMagnitudeAtCenter = 0.0f;

            //Get the ModularVesselPrecalculate
            if (activeVessel.precalc is ModularVesselPrecalculate)
            {
                //Now let the base class do it's thing...
                ModularVesselPrecalculate modularVesselPrecalc = (ModularVesselPrecalculate)activeVessel.precalc;
                modularVesselPrecalc.BaseCalculateGravity();
                
                //Calculate the lift vector.
                Vector3d liftVector = (activeVessel.CoM - activeVessel.mainBody.position).normalized;

                //Add acceleration. integrationAccel appears to be related to gravity. Note that negative values make you fall towards the planet, positive falls away.
                //Lift acceleration should be: liftVector * (1.0 + verticalSpeed)
                modularVesselPrecalc.integrationAccel = liftVector * 1.0f;
            }

            //Restore gravity.
            mainBody.gMagnitudeAtCenter = originalGMag;
        }
    }
}
