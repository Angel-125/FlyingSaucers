﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KerbalActuators;

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
    public class WBIModuleEngineCOMThrust : ModuleEnginesFX
    {
        public override void FXUpdate()
        {
            base.FXUpdate();
            if (HighLogic.LoadedSceneIsFlight == false)
                return;
            int transformCount = thrustTransforms.Count;
            for (int index = 0; index < transformCount; index++)
                thrustTransforms[index].transform.position = vessel.CurrentCoM;
        }

        public override void OnCenterOfThrustQuery(CenterOfThrustQuery qry)
        {
            base.OnCenterOfThrustQuery(qry);

            if (HighLogic.LoadedSceneIsEditor)
                qry.pos = EditorMarker_CoM.findCenterOfMass(EditorLogic.RootPart);
        }

    }
}
