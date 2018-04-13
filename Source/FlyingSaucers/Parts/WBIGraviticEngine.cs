using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.IO;
using KerbalActuators;

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
    [KSPModule("Gravitic Engine")]
    public class WBIGraviticEngine : ModuleEnginesFX, IHoverController, IThrustVectorController, ICustomController
    {
        protected const int kDefaultAnimationLayer = 2;

        [KSPField]
        public bool debugEnabled = false;

        #region Animation
        [KSPField]
        public int animationLayer = kDefaultAnimationLayer;

        [KSPField()]
        public string animationName;

        [KSPField]
        public float startupTime = 2.0f;

        [KSPField]
        public float shutdownTime = 1.0f;

        [KSPField]
        public string gravRingTransformName = string.Empty;

        [KSPField]
        public string gravRingSpinAxis = "0,0,1";

        [KSPField]
        public float spinRateRPMMin = 3.0f;

        [KSPField]
        public float spinRateRPMMax = 12.0f;

        [KSPField]
        public float runningPowerMin = 0.05f;
        #endregion

        #region Gravitic Engine Stats
        [KSPField]
        public float maxAcceleration = 20.0f;

        [KSPField(guiActive = true, guiName = "Acceleration", guiUnits = "m/sec", guiFormat = "f2")]
        public float finalAcceleration;

        [KSPField(isPersistant = true)]
        public bool managedHover = false;

        [KSPField]
        public string reverseTransformName = string.Empty;

        [KSPField]
        public string vtolThrustTransformName = string.Empty;

        [KSPField]
        public string vtolFXTransformName = string.Empty;

        [KSPField]
        public string fwdThrustEffect = string.Empty;

        [KSPField]
        public string revThrustEffect = string.Empty;

        [KSPField]
        public string vtolThrustEffect = string.Empty;
        #endregion

        #region Housekeeping
        [KSPField(guiName = "Singularity Projector", guiActive = true, isPersistant = true)]
        public WBIEngineStates engineState;

        [KSPField(guiName = "Current Mode", guiActive = true, isPersistant = true)]
        public WBIThrustModes engineMode;

        public Animation animation = null;
        public float verticalSpeed = 0f;

        protected float rotationPerFrame = 0;
        protected float rotationPerFrameMin = 0;
        protected float rotationPerFrameMax = 0;
        protected float currentStartStopLerp = 0.0f;
        protected AnimationState animationState;
        protected Transform gravRingTransform = null;
        protected Transform thrustTransform = null;
        protected Transform reverseThrustTransform = null;
        protected Transform vtolThrustTransform = null;
        protected Transform vtolFXTransform = null;
        protected Vector3 gravSpinAxis = Vector3.zero;
        protected LineRenderer lineRenderer;
        protected GameObject thrustLine;
        protected bool isLiftingOff = false;
        protected string thrustEffect = string.Empty;
        #endregion

        #region IHoverController
        [KSPEvent(guiActive = true, guiName = "Toggle Engine Mode")]
        public virtual void ToggleEngineMode()
        {
            switch (engineMode)
            {
                default:
                case WBIThrustModes.Forward:
                    engineMode = WBIThrustModes.Reverse;
                    break;

                case WBIThrustModes.Reverse:
                    engineMode = WBIThrustModes.VTOL;
                    break;

                case WBIThrustModes.VTOL:
                    engineMode = WBIThrustModes.Forward;
                    break;
            }

            SetupEngineMode();
        }

        [KSPAction("Toggle Engine Mode")]
        public virtual void ToggleEngineModeAction()
        {
            ToggleEngineMode();
        }

        public virtual void SetupEngineMode()
        {
            thrustTransforms.Clear();
            switch (engineMode)
            {
                default:
                case WBIThrustModes.Forward:
                    thrustTransforms.Add(thrustTransform);
                    thrustEffect = fwdThrustEffect;
                    if (!string.IsNullOrEmpty(revThrustEffect))
                        this.part.Effect(revThrustEffect, 0f, -1);
                    if (!string.IsNullOrEmpty(vtolThrustEffect))
                        this.part.Effect(vtolThrustEffect, 0f, -1);
                    break;

                case WBIThrustModes.Reverse:
                    thrustTransforms.Add(reverseThrustTransform);
                    thrustEffect = revThrustEffect;
                    if (!string.IsNullOrEmpty(vtolThrustEffect))
                        this.part.Effect(vtolThrustEffect, 0f, -1);
                    if (!string.IsNullOrEmpty(fwdThrustEffect))
                        this.part.Effect(fwdThrustEffect, 0f, -1);
                    break;

                case WBIThrustModes.VTOL:
                    thrustEffect = vtolThrustEffect;
                    if (!managedHover)
                        thrustTransforms.Add(vtolThrustTransform);
                    if (!string.IsNullOrEmpty(revThrustEffect))
                        this.part.Effect(revThrustEffect, 0f, -1);
                    if (!string.IsNullOrEmpty(fwdThrustEffect))
                        this.part.Effect(fwdThrustEffect, 0f, -1);
                    break;
            }

            if (HighLogic.LoadedSceneIsFlight)
                UpdateCenterOfThrust();
        }

        public bool IsEngineActive()
        {
            return isOperational;
        }

        public void StartEngine()
        {
            Activate();
        }

        public void StopEngine()
        {
            Shutdown();
        }

        public void UpdateHoverState(float throttleValue)
        {
            //If we just landed then kill vertical speed and exit
            if ((this.part.vessel.situation == Vessel.Situations.LANDED ||
                this.part.vessel.situation == Vessel.Situations.SPLASHED ||
                this.part.vessel.situation == Vessel.Situations.PRELAUNCH) && !isLiftingOff)
            {
                verticalSpeed = 0f;
                return;
            }

            //Once we're flying again, remove the flag.
            else if (this.part.vessel.situation == Vessel.Situations.FLYING)
            {
                isLiftingOff = false;
            }

            //Get force of gravity
            float forceOfGravity = (float)FlightGlobals.ActiveVessel.gravityForPos.magnitude;

            //Get lift vector
            Vector3d liftVector = (this.part.transform.position - this.vessel.mainBody.position).normalized;
            
            //Calculate base lift force
            float totalMass = vessel.GetTotalMass();
            float liftForce = totalMass * forceOfGravity;

            //Account for desired vertical acceleration
            liftForce += verticalSpeed;

            //Add lift force to part. We do this manually instead of letting ModuleEnginesFX do it so that the craft can have any orientation desired.
            this.part.AddForceAtPosition(liftVector * (float)liftForce, this.part.vessel.CoM);
        }

        public void SetHoverMode(bool isActive)
        {
            if (this.part.vessel.situation == Vessel.Situations.ESCAPING ||
                this.part.vessel.situation == Vessel.Situations.DOCKED ||
                this.part.vessel.situation == Vessel.Situations.ORBITING ||
                this.part.vessel.situation == Vessel.Situations.SUB_ORBITAL)
            {
                managedHover = false;
                return;
            }
            if (flameout)
            {
                managedHover = false;
                return;
            }
            managedHover = isActive;

            //Set the mode
            if (isActive)
                ActivateHover();
            else
                DeactivateHover();

            //Update symmetry parts
            if (this.part.symmetryCounterparts.Count > 0)
            {
                foreach (Part symmetryPart in this.part.symmetryCounterparts)
                {
                    WBIGraviticEngine graviticEngine = symmetryPart.GetComponent<WBIGraviticEngine>();
                    if (graviticEngine != null)
                    {
                        if (isActive)
                            graviticEngine.ActivateHover();
                        else
                            graviticEngine.DeactivateHover();
                    }
                }
            }

        }

        public void SetVerticalSpeed(float verticalSpeed)
        {
            this.verticalSpeed = verticalSpeed;
            if (verticalSpeed > 0f)
                isLiftingOff = true;
        }

        public void KillVerticalSpeed()
        {
            verticalSpeed = 0.0f;
            this.part.vessel.verticalSpeed = 0.0f;
            this.part.vessel.SetWorldVelocity(Vector3d.zero);
        }

        public void ActivateHover()
        {
            verticalSpeed = 0.0f;

            //Switch to vtol transform
            engineMode = WBIThrustModes.VTOL;
            SetupEngineMode();
        }

        public void DeactivateHover()
        {
            //Switch to normal transform
            vessel.ctrlState.mainThrottle = 0f;
            FlightInputHandler.state.mainThrottle = 0f;
            engineMode = WBIThrustModes.Forward;
            SetupEngineMode();
        }
        #endregion

        #region IThrustVectorController
        public WBIThrustModes GetThrustMode()
        {
            return engineMode;
        }

        public void SetForwardThrust(WBIVTOLManager vtolManager)
        {
            engineMode = WBIThrustModes.Forward;
            vtolManager.thrustMode = engineMode;
            SetupEngineMode();
        }

        public void SetReverseThrust(WBIVTOLManager vtolManager)
        {
            engineMode = WBIThrustModes.Reverse;
            vtolManager.thrustMode = engineMode;
            SetupEngineMode();
        }

        public void SetVTOLThrust(WBIVTOLManager vtolManager)
        {
            engineMode = WBIThrustModes.VTOL;
            vtolManager.thrustMode = engineMode;
            SetupEngineMode();
        }

        #endregion

        #region Crazy Mode and ICustomController
        /// <summary>
        /// Flag to indicate that Crazy Mode is available for use
        /// </summary>
        [KSPField]
        public bool crazyModeUnlocked;

        /// <summary>
        /// How fast to warp the craft in crazy mode. Measured in meters per second.
        /// </summary>
        [KSPField]
        public float crazyModeVelocity = 10000.0f;

        /// <summary>
        /// Resource to consume during Crazy Mode
        /// </summary>
        [KSPField]
        public string crazyModeResource = string.Empty;

        /// <summary>
        /// How many units per second to consume the required resource.
        /// </summary>
        [KSPField]
        public double crazyModeResourcePerSec = 50.0f;

        /// <summary>
        /// Current direction for crazy mode travel
        /// </summary>
        protected WBIWarpDirections warpDirection;

        GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.Height(32), GUILayout.Width(32) };

        /// <summary>
        /// Determines if the custom controller UI is visible. Only the first gravitic engine in the vessel will draw its controls
        /// and only if the engine has its crazy mode enabled.
        /// </summary>
        /// <returns></returns>
        public bool IsVisible()
        {
            //Check to make sure crazy mode is unlocked.
            if (!crazyModeUnlocked)
                return false;

            List<WBIGraviticEngine> graviticEngines = this.part.vessel.FindPartModulesImplementing<WBIGraviticEngine>();
            if (graviticEngines[0] == this)
                return true;
            else
                return false;
        }

        /// <summary>
        /// Draws the Crazy Mode controls
        /// </summary>
        public void DrawCustomController()
        {
            //Available only during flight.
            if (this.part.vessel.situation != Vessel.Situations.FLYING)
                return;

            GUILayout.BeginVertical();

            //Label
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.Label(WBIKFSUtils.kCrazyMode);
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            //Forward, Stop, Backward
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("FWD", buttonOptions))
                warpDirection = WBIWarpDirections.Forward;
            if (GUILayout.Button("STP", buttonOptions))
                warpDirection = WBIWarpDirections.Stop;
            if (GUILayout.Button("REV", buttonOptions))
                warpDirection = WBIWarpDirections.Back;

            //Left, Up, Right, Down
            if (GUILayout.Button("LT", buttonOptions))
                warpDirection = WBIWarpDirections.Left;
            if (GUILayout.Button("UP", buttonOptions))
                warpDirection = WBIWarpDirections.Up;
            if (GUILayout.Button("RT", buttonOptions))
                warpDirection = WBIWarpDirections.Right;
            if (GUILayout.Button("DN", buttonOptions))
                warpDirection = WBIWarpDirections.Down;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
        #endregion

        #region overrides
        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (!crazyModeUnlocked)
                return;

            //Setup the warp direction
            Vector3 direction = Vector3.zero;
            switch (warpDirection)
            {
                default:
                case WBIWarpDirections.Stop:
                    return;

                case WBIWarpDirections.Forward:
                    direction = this.transform.up;
                    break;

                case WBIWarpDirections.Back:
                    direction = this.transform.up * -1;
                    break;

                case WBIWarpDirections.Left:
                    direction = this.transform.right * -1;
                    break;

                case WBIWarpDirections.Right:
                    direction = this.part.vessel.transform.up;
                    break;

                case WBIWarpDirections.Up:
                    direction = this.part.vessel.transform.forward * -1;
                    break;

                case WBIWarpDirections.Down:
                    direction = this.transform.forward;
                    break;
            }

            //Consume the resource
            if (!string.IsNullOrEmpty(crazyModeResource))
            {
                double amountRequested = crazyModeResourcePerSec * TimeWarp.fixedDeltaTime;
                double amountObtained = this.part.RequestResource(crazyModeResource, amountRequested, ResourceFlowMode.ALL_VESSEL);

                //Make sure we got enough of the requested resource.
                if ((amountObtained / amountRequested) < 0.25f)
                {
                    warpDirection = WBIWarpDirections.Stop;
                    return;
                }
            }

            //Now reposition the craft
            Vector3d offsetPosition = this.part.vessel.transform.position + (direction * crazyModeVelocity * TimeWarp.fixedDeltaTime);
            FloatingOrigin.SetOutOfFrameOffset(offsetPosition);
        }

        public override string GetInfo()
        {
            StringBuilder info = new StringBuilder();

            //Max acceleration
            info.AppendLine(string.Format(WBIKFSUtils.kMaxAcceleration, maxAcceleration));

            //Propellants
            info.AppendLine(WBIKFSUtils.kPropellants);
            for (int index = 0; index < this.propellants.Count; index++)
            {
                info.AppendLine("-" + this.propellants[index].displayName);
                info.Append(propellants[index].GetFlowModeDescription());
            }
            info.AppendLine(WBIKFSUtils.kFuelFlowVaries);

            //Flameout
            info.AppendLine(string.Format(WBIKFSUtils.kFlameout, (this.ignitionThreshold * 100.0f).ToString("0.#")));

            //Crazy Mode
            if (crazyModeUnlocked)
            {
                info.AppendLine("\n" + WBIKFSUtils.kCrazyMode);
                info.AppendLine(string.Format(WBIKFSUtils.kCrazyModeVelocity, crazyModeVelocity));
                if (!string.IsNullOrEmpty(crazyModeResource))
                    info.AppendLine(string.Format(WBIKFSUtils.kCrazyModeResource, crazyModeResourcePerSec, crazyModeResource));
            }

            return info.ToString();
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            SetupAnimations();

            //Get the gravity ring transform
            gravRingTransform = this.part.FindModelTransform(gravRingTransformName);
            if (gravRingTransform == null)
                return;

            //Thrust transforms
            if (!string.IsNullOrEmpty(thrustVectorTransformName))
                thrustTransform = this.part.FindModelTransform(thrustVectorTransformName);
            if (!string.IsNullOrEmpty(reverseTransformName))
                reverseThrustTransform = this.part.FindModelTransform(reverseTransformName);
            if (!string.IsNullOrEmpty(vtolFXTransformName))
                vtolFXTransform = this.part.FindModelTransform(vtolFXTransformName);
            if (!string.IsNullOrEmpty(vtolThrustTransformName))
                vtolThrustTransform = this.part.FindModelTransform(vtolThrustTransformName);

            //Get the rotation axis
            if (string.IsNullOrEmpty(gravRingSpinAxis) == false)
            {
                string[] axisValues = gravRingSpinAxis.Split(',');
                float value;
                if (axisValues.Length == 3)
                {
                    if (float.TryParse(axisValues[0], out value))
                        gravSpinAxis.x = value;
                    if (float.TryParse(axisValues[1], out value))
                        gravSpinAxis.y = value;
                    if (float.TryParse(axisValues[2], out value))
                        gravSpinAxis.z = value;
                }
            }

            //Rotations per frame
            rotationPerFrameMax = ((spinRateRPMMax * 60.0f) * TimeWarp.fixedDeltaTime);
            rotationPerFrameMin = ((spinRateRPMMin * 60.0f) * TimeWarp.fixedDeltaTime);

            //Calculate max thrust and fuel flow
            UpdateThrust();

            //Thrust mode setup
            SetupEngineMode();
            if (engineState == WBIEngineStates.Running)
                currentStartStopLerp = 1f;

            //GUI setup
            Fields["realIsp"].guiActive = false;
            Fields["finalThrust"].guiActive = false;

            //Check to make sure crazy mode is unlocked for sandbox.
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (!crazyModeUnlocked && HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX)
                    crazyModeUnlocked = true;
            }
        }

        public override void Activate()
        {
            UnFlameout();

            base.Activate();

            PlayAnimation(false);

            engineState = WBIEngineStates.Starting;

            currentStartStopLerp = 0.0f;
            if (vessel.ctrlState.mainThrottle > 0)
                rotationPerFrame = ((spinRateRPMMax * 60.0f) * TimeWarp.fixedDeltaTime) * FlightInputHandler.state.mainThrottle;
            else
                rotationPerFrame = ((spinRateRPMMin * 60.0f) * TimeWarp.fixedDeltaTime);
        }

        public override void Shutdown()
        {
            base.Shutdown();

            PlayAnimation(true);

            engineState = WBIEngineStates.ShuttingDown;

            currentStartStopLerp = 1.0f;
        }

        public override void UpdateThrottle()
        {
            base.UpdateThrottle();

            //Kill the throttle if we haven't warmed up or we are shut down or shutting down.
            if (engineState == WBIEngineStates.Running)
                return;

            currentThrottle = 0.0f;
        }

        public override void OnStartFinished(StartState state)
        {
            base.OnStartFinished(state);
            if (!string.IsNullOrEmpty(fwdThrustEffect))
                this.part.Effect(fwdThrustEffect, 0f, -1);
            if (!string.IsNullOrEmpty(revThrustEffect))
                this.part.Effect(revThrustEffect, 0f, -1);
            if (!string.IsNullOrEmpty(vtolThrustEffect))
                this.part.Effect(vtolThrustEffect, 0f, -1);
        }

        public override void FXUpdate()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            //Adjust thrust & mass flow rate based on current gravity
            UpdateCenterOfThrust();
            UpdateThrust();

            //Point VTOL FX at main body
            if (vtolFXTransform != null)
                vtolFXTransform.LookAt(this.part.vessel.mainBody.position);

            //Account for flameout
            if (flameout && engineState != WBIEngineStates.ShuttingDown && engineState != WBIEngineStates.Shutdown)
            {
                currentStartStopLerp = 1f;
                Shutdown();
            }

            //Now drive the FX
            switch (engineState)
            {
                default:
                case WBIEngineStates.Shutdown:
                    break;

                case WBIEngineStates.Running:
                    //Running
                    if (!string.IsNullOrEmpty(runningEffectName))
                        this.part.Effect(runningEffectName);

                    //Power
                    float powerLevel = vessel.ctrlState.mainThrottle;
                    if (powerLevel < runningPowerMin)
                        powerLevel = runningPowerMin;
                    if (!managedHover)
                    {
                        this.part.Effect(powerEffectName, powerLevel);
                        this.part.Effect(thrustEffect, powerLevel);
                    }

                    //VTOL effect should be run along with thrust effects.
                    else
                    {
                        this.part.Effect(powerEffectName, 1.0f);
                        this.part.Effect(thrustEffect, powerLevel);
                        this.part.Effect(vtolThrustEffect, 1.0f);
                    }

                    //Spin the grav ring
                    if (gravRingTransform != null)
                    {
                        if (managedHover)
                            powerLevel = 0.5f;
                        rotationPerFrame = ((spinRateRPMMax * 60.0f) * TimeWarp.fixedDeltaTime) * powerLevel;
                        if (rotationPerFrame < rotationPerFrameMin)
                            rotationPerFrame = rotationPerFrameMin;
                        gravRingTransform.Rotate(gravSpinAxis * rotationPerFrame);
                    }
                    break;

                case WBIEngineStates.Starting:
                    currentStartStopLerp = Mathf.Lerp(currentStartStopLerp, 1.0f, TimeWarp.fixedDeltaTime / startupTime);
                    if (!string.IsNullOrEmpty(runningEffectName))
                        this.part.Effect(runningEffectName, currentStartStopLerp);
                    if (!string.IsNullOrEmpty(powerEffectName))
                        this.part.Effect(powerEffectName, 0f);
                    if (!string.IsNullOrEmpty(thrustEffect))
                        this.part.Effect(thrustEffect, 0f);

                    if (gravRingTransform != null)
                        gravRingTransform.Rotate(gravSpinAxis * rotationPerFrame * currentStartStopLerp);

                    if (currentStartStopLerp >= 0.99f)
                        engineState = WBIEngineStates.Running;
                    break;

                case WBIEngineStates.ShuttingDown:
                    currentStartStopLerp = Mathf.Lerp(currentStartStopLerp, 0.0f, TimeWarp.fixedDeltaTime / shutdownTime);
                    this.part.Effect(runningEffectName, currentStartStopLerp);
                    if (!string.IsNullOrEmpty(thrustEffect))
                        this.part.Effect(thrustEffect, 0f);

                    if (gravRingTransform != null)
                        gravRingTransform.Rotate(gravSpinAxis * rotationPerFrame * currentStartStopLerp);

                    if (currentStartStopLerp <= 0.01f)
                        engineState = WBIEngineStates.Shutdown;
                    break;
            }
        }

        public override void OnCenterOfThrustQuery(CenterOfThrustQuery qry)
        {
            base.OnCenterOfThrustQuery(qry);

            if (HighLogic.LoadedSceneIsEditor)
                qry.pos = EditorMarker_CoM.findCenterOfMass(EditorLogic.RootPart);
        }

        #endregion

        #region Helpers
        protected Vector3 getVesselPosition()
        {
            PQS pqs = this.part.vessel.mainBody.pqsController;
            double alt = pqs.GetSurfaceHeight(vessel.mainBody.GetRelSurfaceNVector(this.part.vessel.latitude, this.part.vessel.longitude)) - vessel.mainBody.Radius;
            alt = Math.Max(alt, 0); // Underwater!
            return this.part.vessel.mainBody.GetRelSurfacePosition(this.part.vessel.latitude, this.part.vessel.longitude, alt);
        }

        /// <summary>
        /// Updates thrust based on vessel mass and maximum possible acceleration.
        /// This is for non-vtol forward and reverse thrust.
        /// </summary>
        public virtual void UpdateThrust()
        {
            //Get total mass
            float totalMass = 0.0f;
            if (HighLogic.LoadedSceneIsFlight)
                totalMass = vessel.GetTotalMass();
            else if (HighLogic.LoadedSceneIsEditor)
                totalMass = EditorLogic.fetch.ship.GetTotalMass();

            //Calculate max thrust.
            maxThrust = maxAcceleration * totalMass;

            //Calculate max flow.
            this.realIsp = this.atmosphereCurve.Evaluate(0.0f);
            maxFuelFlow = maxThrust / (this.realIsp * this.g);

            //Determine current acceleration
            finalAcceleration = this.finalThrust / totalMass;

            //Update VTOL thrust transform
            if (vtolThrustTransform != null && HighLogic.LoadedSceneIsFlight)
                vtolThrustTransform.LookAt(this.part.vessel.mainBody.position);
        }

        public virtual void UpdateCenterOfThrust()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            //Move the thrust transform to the ship's center of mass
            int transformCount = thrustTransforms.Count;
            for (int index = 0; index < transformCount; index++)
                thrustTransforms[index].transform.position = vessel.CurrentCoM;
            if (debugEnabled)
                DrawThrustTransform();
        }

        public virtual void Destroy()
        {
            if (lineRenderer != null)
            {
                lineRenderer.SetPosition(0, Vector3.zero);
                lineRenderer.SetPosition(1, Vector3.zero);
                GameObject.DestroyImmediate(lineRenderer);
            }
        }

        public virtual void DrawThrustTransform()
        {
            if (lineRenderer == null)
            {
                Material mat = new Material(Shader.Find("Particles/Additive"));
                Color lineColor = XKCDColors.Purple_Blue;

                thrustLine = new GameObject();
                thrustLine.name = "thrustLine";
                lineRenderer = thrustLine.AddComponent<LineRenderer>();
                lineRenderer.useWorldSpace = false;
                lineRenderer.material = mat;
                lineRenderer.SetColors(lineColor, lineColor);
                lineRenderer.SetWidth(0.25f, 0.05f);
                lineRenderer.SetVertexCount(2);
                lineRenderer.SetPosition(0, Vector3.zero);
                lineRenderer.SetPosition(1, Vector3.zero);
            }

            Vector3 startPoint = thrustTransforms[0].transform.position;
            Vector3 endPoint = startPoint * 10.0f;
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint);
        }

        public virtual void SetupAnimations()
        {
            Log("SetupAnimations called.");

            Animation[] animations = this.part.FindModelAnimators(animationName);
            if (animations == null)
            {
                Log("No animations found.");
                return;
            }
            if (animations.Length == 0)
            {
                Log("No animations found.");
                return;
            }

            animation = animations[0];
            if (animation == null)
                return;

            //Set layer
            animationState = animation[animationName];
            animation[animationName].layer = animationLayer;

            if (EngineIgnited)
            {
                animation[animationName].normalizedTime = 1.0f;
                animation[animationName].speed = 10000f;
            }
            else
            {
                animation[animationName].normalizedTime = 0f;
                animation[animationName].speed = -10000f;
            }
            animation.Play(animationName);
        }

        public virtual void PlayAnimation(bool playInReverse = false)
        {
            if (string.IsNullOrEmpty(animationName))
                return;

            float animationSpeed = playInReverse == false ? 1.0f : -1.0f;
            Animation anim = this.part.FindModelAnimators(animationName)[0];

            if (playInReverse)
            {
                anim[animationName].time = anim[animationName].length;
                if (HighLogic.LoadedSceneIsFlight)
                    anim[animationName].speed = animationSpeed;
                else
                    anim[animationName].speed = animationSpeed * 100;
                anim.Play(animationName);
            }

            else
            {
                if (HighLogic.LoadedSceneIsFlight)
                    anim[animationName].speed = animationSpeed;
                else
                    anim[animationName].speed = animationSpeed * 100;
                anim.Play(animationName);
            }
        }

        public virtual void Log(object message)
        {
            if (HighLogic.LoadedScene == GameScenes.LOADING || HighLogic.LoadedScene == GameScenes.LOADINGBUFFER ||
                HighLogic.LoadedScene == GameScenes.PSYSTEM || HighLogic.LoadedScene == GameScenes.SETTINGS)
                return;

            if (!WBIMainSettings.EnableDebugLogging)
                return;

            Debug.Log(this.ClassName + " [" + this.GetInstanceID().ToString("X")
                + "][" + Time.time.ToString("0.0000") + "]: " + message);
        }
        #endregion

    }
}
