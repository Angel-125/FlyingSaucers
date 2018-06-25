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
    /// <summary>
    /// Derived from IWarpController, this interface is used to control Crazy Mode.
    /// </summary>
    public interface ICrazyModeController : IWarpController
    {
        /// <summary>
        /// Determines whether or not Crazy Mode has been unlocked.
        /// </summary>
        /// <returns></returns>
        bool IsCrazyModeUnlocked();
    }

    [KSPModule("Gravitic Engine")]
    public class WBIGraviticEngine : ModuleEnginesFX, IHoverController, IThrustVectorController, ICustomController, ICrazyModeController
    {
        public const string ICON_PATH = "WildBlueIndustries/FlyingSaucers/Icons/";
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

        [KSPField]
        public FloatCurve accelerationCurve;
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
        protected LineRenderer comRenderer;
        protected GameObject thrustLine;
        protected GameObject comLine;
        protected bool isLiftingOff = false;
        protected string thrustEffect = string.Empty;
        protected RaycastHit terrainHit;
        protected LayerMask layerMask = -1;
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
                    thrustEffect = fwdThrustEffect;
                    if (!string.IsNullOrEmpty(revThrustEffect))
                        this.part.Effect(revThrustEffect, 0f, -1);
                    if (!string.IsNullOrEmpty(vtolThrustEffect))
                        this.part.Effect(vtolThrustEffect, 0f, -1);
                    break;

                case WBIThrustModes.Reverse:
                    thrustEffect = revThrustEffect;
                    if (!string.IsNullOrEmpty(vtolThrustEffect))
                        this.part.Effect(vtolThrustEffect, 0f, -1);
                    if (!string.IsNullOrEmpty(fwdThrustEffect))
                        this.part.Effect(fwdThrustEffect, 0f, -1);
                    break;

                case WBIThrustModes.VTOL:
                    thrustEffect = vtolThrustEffect;
                    if (!string.IsNullOrEmpty(revThrustEffect))
                        this.part.Effect(revThrustEffect, 0f, -1);
                    if (!string.IsNullOrEmpty(fwdThrustEffect))
                        this.part.Effect(fwdThrustEffect, 0f, -1);
                    break;
            }

            if (HighLogic.LoadedSceneIsFlight)
                UpdateCenterOfThrust();
        }

        public bool GetHoverState()
        {
            return managedHover;
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
            float forceOfGravity = (float)this.part.vessel.gravityForPos.magnitude;

            //Calculate lift acceleration
            float liftAcceleration = forceOfGravity;
            if (verticalSpeed > 0 && vessel.verticalSpeed < verticalSpeed)
                liftAcceleration += verticalSpeed;
            else if (verticalSpeed < 0 && vessel.verticalSpeed > verticalSpeed)
                liftAcceleration += verticalSpeed;
            currentThrottle = maxAcceleration - liftAcceleration;

            //Get lift vector
            Vector3d accelerationVector = (this.part.vessel.CoM - this.vessel.mainBody.position).normalized * liftAcceleration;
            
            //Add acceleration. We do this manually instead of letting ModuleEnginesFX do it so that the craft can have any orientation desired.
            ApplyAccelerationVector(accelerationVector);
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
            managedHover = isActive;

            //Set the mode
            if (managedHover)
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
                        if (managedHover)
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

        public float GetVerticalSpeed()
        {
            return verticalSpeed;
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
            if (vessel.LandedOrSplashed)
            {
                vessel.ctrlState.mainThrottle = 0f;
                FlightInputHandler.state.mainThrottle = 0f;
            }

            //Switch to normal transform
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
        /// Stop crazy mode when the resource drops below the ratio (5% default)
        /// </summary>
        [KSPField]
        public double crazyModeResourceReserve = 0.05f;

        /// <summary>
        /// Current direction for crazy mode travel
        /// </summary>
        protected WBIWarpDirections warpDirection;

        GUILayoutOption[] buttonOptions = new GUILayoutOption[] { GUILayout.Height(32), GUILayout.Width(32) };
        public static Texture stopIcon = null;
        public static Texture fwdIcon = null;
        public static Texture revIcon = null;
        public static Texture upIcon = null;
        public static Texture dnIcon = null;
        public static Texture leftIcon = null;
        public static Texture rightIcon = null;
        public static Texture fwdIconSel = null;
        public static Texture revIconSel = null;
        public static Texture upIconSel = null;
        public static Texture dnIconSel = null;
        public static Texture leftIconSel = null;
        public static Texture rightIconSel = null;

        /// <summary>
        /// Determines whether or not Crazy Mode has been unlocked.
        /// </summary>
        /// <returns></returns>
        public bool IsCrazyModeUnlocked()
        {
            return crazyModeUnlocked;
        }

        /// <summary>
        /// Returns the current warp direction.
        /// </summary>
        /// <returns>A WBIWarpDirections enumerator describing the current warp direction.</returns>
        public WBIWarpDirections GetWarpDirection()
        {
            return warpDirection;
        }

        /// <summary>
        /// Sets the desired warp direction, but only if crazyModeUnlocked = true.
        /// </summary>
        /// <param name="direction">A WBIWarpDirections enumerator specifying the desired direction.</param>
        public void SetWarpDirection(WBIWarpDirections direction)
        {
            if (crazyModeUnlocked)
            {
                warpDirection = direction;
            }

            //Update other engines
            List<WBIGraviticEngine> graviticEngines = this.part.vessel.FindPartModulesImplementing<WBIGraviticEngine>();
            int count = graviticEngines.Count;
            for (int index = 0; index < count; index++)
            {
                if (graviticEngines[index] != this)
                    graviticEngines[index].warpDirection = direction;
            }
        }

        /// <summary>
        /// Determines whether or not the controller is active. For instance, you might only have the first controller on a vessel set to active while the rest are inactive.
        /// </summary>
        /// <returns>True if the controller is active, false if not.</returns>
        public bool IsActive()
        {
            //Check to make sure crazy mode is unlocked.
            if (!crazyModeUnlocked)
                return false;

            List<WBIGraviticEngine> graviticEngines = this.part.vessel.FindPartModulesImplementing<WBIGraviticEngine>();
            if (graviticEngines[0] == this)
            {
                if (EngineIgnited && isOperational)
                    return true;
                else
                    return false;
            }
            else
            {
                return false;
            }
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
            if (GUILayout.Button(stopIcon, buttonOptions))
                warpDirection = WBIWarpDirections.Stop;

            Texture buttonIcon = fwdIconSel;
            if (warpDirection == WBIWarpDirections.Forward)
                buttonIcon = fwdIconSel;
            else
                buttonIcon = fwdIcon;
            if (GUILayout.Button(buttonIcon, buttonOptions))
                warpDirection = WBIWarpDirections.Forward;

            if (warpDirection == WBIWarpDirections.Back)
                buttonIcon = revIconSel;
            else
                buttonIcon = revIcon;
            if (GUILayout.Button(buttonIcon, buttonOptions))
                warpDirection = WBIWarpDirections.Back;

            //Left, Up, Right, Down
            if (warpDirection == WBIWarpDirections.Left)
                buttonIcon = leftIconSel;
            else
                buttonIcon = leftIcon;
            if (GUILayout.Button(buttonIcon, buttonOptions))
                warpDirection = WBIWarpDirections.Left;

            if (warpDirection == WBIWarpDirections.Right)
                buttonIcon = rightIconSel;
            else
                buttonIcon = rightIcon;
            if (GUILayout.Button(buttonIcon, buttonOptions))
                warpDirection = WBIWarpDirections.Right;

            if (warpDirection == WBIWarpDirections.Up)
                buttonIcon = upIconSel;
            else
                buttonIcon = upIcon;
            if (GUILayout.Button(buttonIcon, buttonOptions))
                warpDirection = WBIWarpDirections.Up;

            if (warpDirection == WBIWarpDirections.Down)
                buttonIcon = dnIconSel;
            else
                buttonIcon = dnIcon;
            if (GUILayout.Button(buttonIcon, buttonOptions))
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

            //Check for flameout
            CheckFlameout();

            //Make sure center of thrust is at vessel CoM
            UpdateCenterOfThrust();

            //Adjust thrust & mass flow rate based on current gravity
            UpdateThrust();

            UpdateCrazyMode();
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

        public override void OnInactive()
        {
            base.OnInactive();
            engineState = WBIEngineStates.Shutdown;
            this.part.Effect(runningEffectName, 0f);
            this.part.Effect(powerEffectName, 0f);
            this.part.Effect(thrustEffect, 0f);
            this.part.Effect(vtolThrustEffect, 0f);
        }

        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);
            layerMask = 1 << LayerMask.NameToLayer("TerrainColliders") | 1 << LayerMask.NameToLayer("Local Scenery");
            setupIcons();
            SetupAnimations();
            loadAccelerationCurve();

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

        public override void Flameout(string message, bool statusOnly = false, bool showFX = true)
        {
            base.Flameout(message, statusOnly, showFX);

            if (engineState != WBIEngineStates.Shutdown && engineState != WBIEngineStates.ShuttingDown)
            {
                SetHoverMode(false);
                PlayAnimation(true);
                engineState = WBIEngineStates.ShuttingDown;
                currentStartStopLerp = 1.0f;
            }
        }

        public override void UnFlameout(bool showFX = true)
        {
            base.UnFlameout(showFX);
            if (!isOperational || !EngineIgnited)
                return;

            if (engineState != WBIEngineStates.Running && engineState != WBIEngineStates.Starting)
            {
                PlayAnimation(false);

                engineState = WBIEngineStates.Starting;

                currentStartStopLerp = 0.0f;
                if (vessel.ctrlState.mainThrottle > 0)
                    rotationPerFrame = ((spinRateRPMMax * 60.0f) * TimeWarp.fixedDeltaTime) * FlightInputHandler.state.mainThrottle;
                else
                    rotationPerFrame = ((spinRateRPMMin * 60.0f) * TimeWarp.fixedDeltaTime);
            }
        }

        public override void Activate()
        {
            UnFlameout();

            base.Activate();
            this.part.force_activate();

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

            SetHoverMode(false);
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
                    this.part.Effect(powerEffectName, 0f);

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

        #region Propulsion Systems
        /// <summary>
        /// Updates Crazy Mode propulsion, consuming resources and repositioning the craft as needed.
        /// </summary>
        public virtual void UpdateCrazyMode()
        {
            //If crazy mode isn't enabled then we're done.
            if (!crazyModeUnlocked)
                return;

            //Make sure we're flying. If not, then turn off crazy mode.
            if (this.part.vessel.situation != Vessel.Situations.FLYING)
            {
                warpDirection = WBIWarpDirections.Stop;
                return;
            }

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
                    direction = this.transform.right;
                    break;

                case WBIWarpDirections.Up:
                    direction = this.transform.forward * -1;
                    break;

                case WBIWarpDirections.Down:
                    direction = this.transform.forward;
                    break;
            }

            //Get throttle setting
            float throttleSetting = FlightInputHandler.state.mainThrottle * (thrustPercentage / 100.0f);
            if (throttleSetting <= 0f)
                return;

            //Calculate offset position
            Vector3d offsetPosition = this.part.vessel.transform.position + (direction * crazyModeVelocity * throttleSetting * TimeWarp.fixedDeltaTime);

            //Make sure we won't collide with the terrain
            if (Physics.Raycast(vessel.transform.position, direction, out terrainHit, (float)offsetPosition.magnitude, layerMask))
            {
                Part prt = terrainHit.collider.gameObject.GetComponent<Part>();

                //See if we found the ground. 15 = Local Scenery, 28 = TerrainColliders
                if (terrainHit.collider.gameObject.layer == 15 || terrainHit.collider.gameObject.layer == 28)
                {
                    //If we would warp into the ground then stop Crazy Mode.
                    if (terrainHit.distance <= Math.Abs(offsetPosition.magnitude))
                    {
                        ScreenMessages.PostScreenMessage(WBIKFSUtils.kTerrainWarning, 3.0f, ScreenMessageStyle.UPPER_CENTER);
                        warpDirection = WBIWarpDirections.Stop;
                        return;
                    }
                }
            }

            //Consume the resource
            if (!string.IsNullOrEmpty(crazyModeResource))
            {
                //Don't drop below reserve amount of the resource. This is to avoide flameouts.
                PartResourceDefinition def = PartResourceLibrary.Instance.resourceDefinitions[crazyModeResource];
                double amount;
                double maxAmount;
                this.part.GetConnectedResourceTotals(def.id, out amount, out maxAmount, true);
                if (amount / maxAmount < crazyModeResourceReserve)
                {
                    FlightInputHandler.state.mainThrottle = 0.0f;
                    warpDirection = WBIWarpDirections.Stop;
                    return;
                }

                double amountRequested = crazyModeResourcePerSec * throttleSetting * TimeWarp.fixedDeltaTime;
                double amountObtained = this.part.RequestResource(crazyModeResource, amountRequested, ResourceFlowMode.ALL_VESSEL);

                //Make sure we got enough of the requested resource.
                if ((amountObtained / amountRequested) < 0.25f)
                {
                    warpDirection = WBIWarpDirections.Stop;
                    return;
                }
            }

            //A-OK! Warp the ship.
            if (FlightGlobals.VesselsLoaded.Count > 1)
                this.part.vessel.SetPosition(offsetPosition);
            else
                FloatingOrigin.SetOutOfFrameOffset(offsetPosition); //Use this for warp drive?
        }

        /// <summary>
        /// Updates thrust based on vessel mass and maximum possible acceleration.
        /// </summary>
        public virtual void UpdateThrust()
        {
            if (!isOperational && !EngineIgnited)
                return;

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

            //Apply acceleration. We don't let ModuleEnginesFX do this because we want to simulate "falling" towards the artificial singularity.
            if (engineState != WBIEngineStates.Running)
                return;
            if (engineMode == WBIThrustModes.Forward || engineMode == WBIThrustModes.Reverse)
            {
                //Get the acceleration speed.
                float accelerationMagnitude = maxAcceleration * accelerationCurve.Evaluate(vessel.ctrlState.mainThrottle);

                //Calcualte the acceleration vector
                Vector3d accelerationVector = (this.part.vessel.GetReferenceTransformPart().transform.up).normalized * accelerationMagnitude;
                if (engineMode == WBIThrustModes.Reverse)
                    accelerationVector *= -1.0f;

                //Apply acceleration
                ApplyAccelerationVector(accelerationVector);
            }

            else if (engineMode == WBIThrustModes.VTOL && !managedHover)
            {
                //Get force of gravity
                float forceOfGravity = (float)this.part.vessel.gravityForPos.magnitude;

                //Calculate lift acceleration
                float liftAcceleration = maxAcceleration * vessel.ctrlState.mainThrottle;

                //Get lift vector
                Vector3d accelerationVector = (this.part.vessel.CoM - this.vessel.mainBody.position).normalized * liftAcceleration;

                //Apply acceleration
                ApplyAccelerationVector(accelerationVector);
            }
        }

        public virtual void UpdateCenterOfThrust()
        {
            if (!HighLogic.LoadedSceneIsFlight)
                return;
            //Move the thrust transform to the ship's center of mass
            int transformCount = thrustTransforms.Count;
            for (int index = 0; index < transformCount; index++)
                thrustTransforms[index].transform.position = vessel.CoM;
            if (debugEnabled)
                DrawThrustTransform();
        }
        #endregion

        #region Helpers

        public void ApplyAccelerationVector(Vector3d accelerationVector)
        {
            int partCount = vessel.parts.Count;
            Part vesselPart;
            for (int index = 0; index < partCount; index++)
            {
                vesselPart = vessel.parts[index];
                if (vesselPart.rb != null)
                {
                    vesselPart.rb.AddForce(accelerationVector, ForceMode.Acceleration);
                }
            }
        }

        public void CheckFlameout()
        {
            if (EngineIgnited)
            {
                int propellantCount = this.propellants.Count;
                Propellant propellant;
                for (int index = 0; index < propellantCount; index++)
                {
                    propellant = this.propellants[index];

                    //Check for un-flameout
                    if (propellant.actualTotalAvailable > 0.0f && !isOperational)
                    {
                        UnFlameout();
                        break;
                    }

                    //Check for flameout in hover mode
                    else if (propellant.actualTotalAvailable <= 0.0f && engineMode == WBIThrustModes.VTOL)
                    {
                        Debug.Log("Hover mode flameout");
                        SetHoverMode(false);
                        PlayAnimation(true);
                        engineState = WBIEngineStates.ShuttingDown;
                        currentStartStopLerp = 1.0f;
                        Flameout(Localizer.Format("#autoLOC_219016"));
                        break;
                    }

                }
            }
        }

        protected Vector3 getVesselPosition()
        {
            PQS pqs = this.part.vessel.mainBody.pqsController;
            double alt = pqs.GetSurfaceHeight(vessel.mainBody.GetRelSurfaceNVector(this.part.vessel.latitude, this.part.vessel.longitude)) - vessel.mainBody.Radius;
            alt = Math.Max(alt, 0); // Underwater!
            return this.part.vessel.mainBody.GetRelSurfacePosition(this.part.vessel.latitude, this.part.vessel.longitude, alt);
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
            /*
            if (lineRenderer == null)
            {
                Material mat = new Material(Shader.Find("Particles/Additive"));
                Color lineColor = XKCDColors.Purple_Blue;

                thrustLine = new GameObject();
                thrustLine.name = "thrustLine";
                lineRenderer = thrustLine.AddComponent<LineRenderer>();
                lineRenderer.useWorldSpace = false;
                lineRenderer.material = mat;
                lineRenderer.startColor = lineColor;
                lineRenderer.endColor = lineColor;
                lineRenderer.startWidth = 0.25f;
                lineRenderer.endWidth = 0.25f;
                lineRenderer.positionCount = 2;
                lineRenderer.SetPosition(0, Vector3.zero);
                lineRenderer.SetPosition(1, Vector3.zero);
            }

            if (comRenderer == null)
            {
                Material mat = new Material(Shader.Find("Particles/Additive"));
                comLine = new GameObject();
                comLine.name = "CoMLine";
                comRenderer = comLine.AddComponent<LineRenderer>();
                comRenderer.useWorldSpace = false;
                comRenderer.material = mat;
                comRenderer.startColor = XKCDColors.Yellow;
                comRenderer.endColor = XKCDColors.Yellow;
                comRenderer.startWidth = 0.25f;
                comRenderer.endWidth = 0.25f;
                comRenderer.positionCount = 2;
                comRenderer.SetPosition(0, Vector3.zero);
                comRenderer.SetPosition(1, Vector3.zero);
            }
            Vector3 startPoint = vessel.CoM;
            if (thrustTransforms.Count > 0)
                startPoint = thrustTransforms[0].transform.position;
            Vector3 endPoint = startPoint * 10.0f;
            lineRenderer.SetPosition(0, startPoint);
            lineRenderer.SetPosition(1, endPoint);

//            startPoint = (this.part.transform.position - this.vessel.mainBody.position).normalized;
            startPoint = (vessel.CoM - vessel.mainBody.position).normalized;
            endPoint = startPoint * 10.0f;
            comRenderer.SetPosition(0, startPoint);
            comRenderer.SetPosition(1, endPoint);
             */
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

        protected void setupIcons()
        {
            string baseIconURL = ICON_PATH;
            ConfigNode settingsNode = GameDatabase.Instance.GetConfigNode("FlyingSaucers");
            if (settingsNode != null)
                baseIconURL = settingsNode.GetValue("iconsFolder");
            fwdIcon = GameDatabase.Instance.GetTexture(baseIconURL + "UFOFwd", false);
            revIcon = GameDatabase.Instance.GetTexture(baseIconURL + "UFORev", false);
            upIcon = GameDatabase.Instance.GetTexture(baseIconURL + "UFOUp", false);
            dnIcon = GameDatabase.Instance.GetTexture(baseIconURL + "UFODn", false);
            leftIcon = GameDatabase.Instance.GetTexture(baseIconURL + "UFOLeft", false);
            rightIcon = GameDatabase.Instance.GetTexture(baseIconURL + "UFORight", false);

            fwdIconSel = GameDatabase.Instance.GetTexture(baseIconURL + "UFOFwdSel", false);
            revIconSel = GameDatabase.Instance.GetTexture(baseIconURL + "UFORevSel", false);
            upIconSel = GameDatabase.Instance.GetTexture(baseIconURL + "UFOUpSel", false);
            dnIconSel = GameDatabase.Instance.GetTexture(baseIconURL + "UFODnSel", false);
            leftIconSel = GameDatabase.Instance.GetTexture(baseIconURL + "UFOLeftSel", false);
            rightIconSel = GameDatabase.Instance.GetTexture(baseIconURL + "UFORightSel", false);

            baseIconURL = WBIServoManager.ICON_PATH;
            settingsNode = GameDatabase.Instance.GetConfigNode("KerbalActuators");
            if (settingsNode != null)
                baseIconURL = settingsNode.GetValue("iconsFolder");
            stopIcon = GameDatabase.Instance.GetTexture(baseIconURL + "Stop", false);
        }

        protected void loadAccelerationCurve()
        {
            if (accelerationCurve.Curve.length > 0)
                return;
            ConfigNode[] nodes = this.part.partInfo.partConfig.GetNodes("MODULE");
            ConfigNode engineNode = null;
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
                        engineNode = node;
                        break;
                    }
                }
            }
            if (engineNode == null)
                return;
            if (!engineNode.HasNode("accelerationCurve"))
                return;

            node = engineNode.GetNode("accelerationCurve");
            accelerationCurve.Load(node);
        }
        #endregion

    }
}
