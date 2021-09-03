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
        #region constants
        public const string ICON_PATH = "WildBlueIndustries/FlyingSaucers/Icons/";
        protected const int kDefaultAnimationLayer = 2;
        protected float kMessageDuration = 3f;
        #endregion

        #region Fields
        [KSPField]
        public bool debugEnabled = false;

        //Control axis groups
        //KSPAxisGroup.TranslateX: L/R KSPAxisGroup.TranslateY: U/D KSPAxisGroup.TranslateZ: F/B
        [KSPAxisField(axisGroup = KSPAxisGroup.TranslateZ, axisMode = KSPAxisMode.Absolute, guiActive = false, guiActiveEditor = false, guiName = "Crazy Mode: F/B", ignoreIncrementByZero = true, incrementalSpeed = 10f, isPersistant = true, maxValue = 1f, minValue = -1f)]
        [UI_FloatRange(affectSymCounterparts = UI_Scene.All, maxValue = 1f, minValue = -1f, stepIncrement = 10f)]
        public float translateFwBk;

        [KSPAxisField(axisGroup = KSPAxisGroup.TranslateX, axisMode = KSPAxisMode.Absolute, guiActive = false, guiActiveEditor = false, guiName = "Crazy Mode: L/R", ignoreIncrementByZero = true, incrementalSpeed = 10f, isPersistant = true, maxValue = 1f, minValue = -1f)]
        [UI_FloatRange(affectSymCounterparts = UI_Scene.All, maxValue = 1f, minValue = -1f, stepIncrement = 10f)]
        public float translateLtRt;

        [KSPAxisField(axisGroup = KSPAxisGroup.TranslateY, axisMode = KSPAxisMode.Absolute, guiActive = false, guiActiveEditor = false, guiName = "Crazy Mode: U/D", ignoreIncrementByZero = true, incrementalSpeed = 10f, isPersistant = true, maxValue = 1f, minValue = -1f)]
        [UI_FloatRange(affectSymCounterparts = UI_Scene.All, maxValue = 1f, minValue = -1f, stepIncrement = 10f)]
        public float translateUpDn;
        #endregion

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
        public bool hoverIsActive = false;

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

        [KSPField(guiName = "Acceleration Mode", guiActive = true, isPersistant = true)]
        public WBIThrustModes engineMode;

        public Animation animation = null;
        public float verticalSpeed = 0f;
        public Vector3 warpVector = Vector3.zero;
        public bool prevCrazyModeEnabled;
        public bool prevCruiseModeEnabled;

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
        protected bool translationKeysActive = false;
        float totalMaxAcceleration = 0;
        #endregion

        #region IHoverController
        public bool GetHoverState()
        {
            return hoverIsActive;
        }

        public bool IsEngineActive()
        {
            return isOperational && EngineIgnited;
        }

        public void StartEngine()
        {
            Activate();
        }

        public void StopEngine()
        {
            Shutdown();
        }

        public void SetHoverMode(bool isActive)
        {
            if (this.part.vessel.situation == Vessel.Situations.ESCAPING ||
                this.part.vessel.situation == Vessel.Situations.DOCKED ||
                this.part.vessel.situation == Vessel.Situations.ORBITING ||
                this.part.vessel.situation == Vessel.Situations.SUB_ORBITAL)
            {
                hoverIsActive = false;
                return;
            }
            hoverIsActive = isActive;

            //Set the mode
            if (hoverIsActive)
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
                        if (hoverIsActive)
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
            updateHoverEventGUI();
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
            updateHoverEventGUI();
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
        /// Flag to indicate if Crazy Mode is enabled.
        /// </summary>
        [KSPField(guiName = "Crazy Mode", isPersistant = true, guiActiveEditor = false, guiActive = true)]
        [UI_Toggle(enabledText = "Enabled", disabledText = "Disabled")]
        public bool crazyModeEnabled;

        /// <summary>
        /// Flag to indicate if Crazy Mode cruise control is enabled.
        /// </summary>
        [KSPField(guiName = "Crazy Cruise Control", isPersistant = true, guiActiveEditor = false, guiActive = true)]
        [UI_Toggle(enabledText = "Enabled", disabledText = "Disabled")]
        public bool crazyCruiseControlEnabled;

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

                //Make sure we're in hover mode
                if (!hoverIsActive)
                    SetHoverMode(true);

                //Kill any vertical speed that we have.
                KillVerticalSpeed();
            }

            //Update other engines
            List<WBIGraviticEngine> graviticEngines = this.part.vessel.FindPartModulesImplementing<WBIGraviticEngine>();
            int count = graviticEngines.Count;
            for (int index = 0; index < count; index++)
            {
                if (graviticEngines[index] != this)
                {
                    graviticEngines[index].warpDirection = direction;
                    if (!graviticEngines[index].hoverIsActive)
                        graviticEngines[index].SetHoverMode(true);
                    graviticEngines[index].KillVerticalSpeed();
                }
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

            if (EngineIgnited && isOperational)
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
            if (!VesselIsAirborne())
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
            {
                setCrazyCruiseMode(true);
                StopWarp();
            }

            Texture buttonIcon = fwdIconSel;
            if (warpDirection == WBIWarpDirections.Forward)
                buttonIcon = fwdIconSel;
            else
                buttonIcon = fwdIcon;
            if (GUILayout.Button(buttonIcon, buttonOptions))
            {
                setCrazyCruiseMode(true);
                SetWarpDirection(WBIWarpDirections.Forward);
                updateWarpVector();
            }

            if (warpDirection == WBIWarpDirections.Back)
                buttonIcon = revIconSel;
            else
                buttonIcon = revIcon;
            if (GUILayout.Button(buttonIcon, buttonOptions))
            {
                setCrazyCruiseMode(true);
                SetWarpDirection(WBIWarpDirections.Back);
                updateWarpVector();
            }

            //Left, Up, Right, Down
            if (warpDirection == WBIWarpDirections.Left)
                buttonIcon = leftIconSel;
            else
                buttonIcon = leftIcon;
            if (GUILayout.Button(buttonIcon, buttonOptions))
            {
                setCrazyCruiseMode(true);
                SetWarpDirection(WBIWarpDirections.Left);
                updateWarpVector();
            }

            if (warpDirection == WBIWarpDirections.Right)
                buttonIcon = rightIconSel;
            else
                buttonIcon = rightIcon;
            if (GUILayout.Button(buttonIcon, buttonOptions))
            {
                setCrazyCruiseMode(true);
                SetWarpDirection(WBIWarpDirections.Right);
                updateWarpVector();
            }

            if (warpDirection == WBIWarpDirections.Up)
                buttonIcon = upIconSel;
            else
                buttonIcon = upIcon;
            if (GUILayout.Button(buttonIcon, buttonOptions))
            {
                setCrazyCruiseMode(true);
                SetWarpDirection(WBIWarpDirections.Up);
                updateWarpVector();
            }

            if (warpDirection == WBIWarpDirections.Down)
                buttonIcon = dnIconSel;
            else
                buttonIcon = dnIcon;
            if (GUILayout.Button(buttonIcon, buttonOptions))
            {
                setCrazyCruiseMode(true);
                SetWarpDirection(WBIWarpDirections.Down);
                updateWarpVector();
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
        #endregion

        #region Events
        [KSPEvent(guiActive = true, guiName = "Toggle Hover Mode")]
        public virtual void ToggleHoverMode()
        {
            hoverIsActive = !hoverIsActive;
            if (!hoverIsActive)
            {
                crazyModeEnabled = false;
                engineMode = WBIThrustModes.Forward;
                SetupEngineMode();
            }
            SetHoverMode(hoverIsActive);

            List<WBIGraviticEngine> engines = this.part.vessel.FindPartModulesImplementing<WBIGraviticEngine>();
            WBIGraviticEngine engine;
            int count = engines.Count;
            for (int index = 0; index < count; index++)
            {
                engine = engines[index];
                if (!engine.IsEngineActive())
                    continue;
            
                if (engine != this)
                {
                    if (!hoverIsActive)
                    {
                        engine.crazyModeEnabled = false;
                        engine.engineMode = WBIThrustModes.Forward;
                        engine.SetupEngineMode();
                    }
                    engine.SetHoverMode(hoverIsActive);
                }
            }
        }

        [KSPEvent(guiActive = true, guiName = "Toggle Acceleration Mode")]
        public virtual void ToggleThrustMode()
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

            List<WBIGraviticEngine> engines = this.part.vessel.FindPartModulesImplementing<WBIGraviticEngine>();
            int count = engines.Count;
            for (int index = 0; index < count; index++)
            {
                if (!engines[index].IsEngineActive())
                    continue;

                engines[index].engineMode = this.engineMode;
            }
        }
        #endregion

        #region Actions
        [KSPAction("Set Forward Acceleration")]
        public void SetFwdThrustAction(KSPActionParam param)
        {
            SetForwardThrust(WBIVTOLManager.Instance);
            ScreenMessages.PostScreenMessage("Gravitic Acceleration: Forward", kMessageDuration, ScreenMessageStyle.UPPER_LEFT);

            List<WBIGraviticEngine> engines = this.part.vessel.FindPartModulesImplementing<WBIGraviticEngine>();
            int count = engines.Count;
            for (int index = 0; index < count; index++)
            {
                if (!engines[index].IsEngineActive())
                    continue;

                engines[index].SetForwardThrust(WBIVTOLManager.Instance);
            }
        }

        [KSPAction("Set Reverse Acceleration")]
        public void SetReverseThrustAction(KSPActionParam param)
        {
            SetReverseThrust(WBIVTOLManager.Instance);
            ScreenMessages.PostScreenMessage("Gravitic Acceleration: Reverse", kMessageDuration, ScreenMessageStyle.UPPER_LEFT);

            List<WBIGraviticEngine> engines = this.part.vessel.FindPartModulesImplementing<WBIGraviticEngine>();
            int count = engines.Count;
            for (int index = 0; index < count; index++)
            {
                if (!engines[index].IsEngineActive())
                    continue;

                engines[index].SetReverseThrust(WBIVTOLManager.Instance);
            }
        }

        [KSPAction("Set VTOL Acceleration")]
        public void SetVTOLThrustAction(KSPActionParam param)
        {
            SetVTOLThrust(WBIVTOLManager.Instance);
            ScreenMessages.PostScreenMessage("Gravitic Acceleration: VTOL", kMessageDuration, ScreenMessageStyle.UPPER_LEFT);

            List<WBIGraviticEngine> engines = this.part.vessel.FindPartModulesImplementing<WBIGraviticEngine>();
            int count = engines.Count;
            for (int index = 0; index < count; index++)
            {
                if (!engines[index].IsEngineActive())
                    continue;

                engines[index].SetVTOLThrust(WBIVTOLManager.Instance);
            }
        }

        [KSPAction("Toggle Hover Mode")]
        public virtual void ToggleHoverModeAction()
        {
            ToggleHoverMode();
            switch (engineMode)
            {
                default:
                case WBIThrustModes.Forward:
                    ScreenMessages.PostScreenMessage("Hover Mode deactivated", kMessageDuration, ScreenMessageStyle.UPPER_LEFT);
                    break;

                case WBIThrustModes.VTOL:
                    ScreenMessages.PostScreenMessage("Hover Mode activated", kMessageDuration, ScreenMessageStyle.UPPER_LEFT);
                    break;
            }
        }

        [KSPAction("Halt Crazy Moves", actionGroup = KSPActionGroup.Brakes)]
        public void StopCrazyModeAction(KSPActionParam param)
        {
            if (!EngineIgnited || !isOperational)
                return;

            if (hoverIsActive)
                KillVerticalSpeed();

            if (crazyModeUnlocked && crazyModeEnabled)
            {
                warpDirection = WBIWarpDirections.Stop;
                warpVector = Vector3.zero;
            }

            List<WBIGraviticEngine> engines = this.part.vessel.FindPartModulesImplementing<WBIGraviticEngine>();
            int count = engines.Count;
            for (int index = 0; index < count; index++)
            {
                if (!engines[index].IsEngineActive())
                    continue;

                engines[index].warpDirection = WBIWarpDirections.Stop;
                engines[index].warpVector = Vector3.zero;
            }
        }
        #endregion

        #region overrides
        public override void OnUpdate()
        {
            base.OnUpdate();
            updatePAWGUI();
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            //Update the hover state
            UpdateHoverState();

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

            //Thrust transforms
            if (!string.IsNullOrEmpty(thrustVectorTransformName))
                thrustTransform = this.part.FindModelTransform(thrustVectorTransformName);
            if (!string.IsNullOrEmpty(reverseTransformName))
                reverseThrustTransform = this.part.FindModelTransform(reverseTransformName);
            if (!string.IsNullOrEmpty(vtolFXTransformName))
                vtolFXTransform = this.part.FindModelTransform(vtolFXTransformName);
            if (!string.IsNullOrEmpty(vtolThrustTransformName))
                vtolThrustTransform = this.part.FindModelTransform(vtolThrustTransformName);

            //Get the gravity ring transform
            gravRingTransform = this.part.FindModelTransform(gravRingTransformName);

            //Get the rotation axis
            if (gravRingTransform != null)
            {
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
            }

            //Calculate max thrust and fuel flow
            UpdateThrust();

            //Thrust mode setup
            SetupEngineMode();
            if (engineState == WBIEngineStates.Running)
                currentStartStopLerp = 1f;

            //GUI setup
            Fields["realIsp"].guiActive = debugEnabled;
            Fields["finalThrust"].guiActive = debugEnabled;
            Fields["translateFwBk"].guiActive = debugEnabled;
            Fields["translateLtRt"].guiActive = debugEnabled;
            Fields["translateUpDn"].guiActive = debugEnabled;
            updateHoverEventGUI();

            //Check to make sure crazy mode is unlocked for sandbox.
            if (HighLogic.LoadedSceneIsFlight)
            {
                if (!crazyModeUnlocked && HighLogic.CurrentGame.Mode == Game.Modes.SANDBOX)
                    crazyModeUnlocked = true;
            }

            prevCrazyModeEnabled = crazyModeEnabled;
            prevCruiseModeEnabled = crazyCruiseControlEnabled;
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
                    this.part.Effect(powerEffectName, 0f, -1);
                    this.part.Effect(thrustEffect, 0f, -1);
                    this.part.Effect(vtolThrustEffect, 0f, -1);
                    break;

                case WBIEngineStates.Running:
                    //Running
                    if (!string.IsNullOrEmpty(runningEffectName))
                        this.part.Effect(runningEffectName);

                    //Power
                    float powerLevel = vessel.ctrlState.mainThrottle;
                    if (powerLevel < runningPowerMin)
                        powerLevel = runningPowerMin;
                    if (!hoverIsActive)
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
                        if (hoverIsActive)
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

        public void UpdateHoverState()
        {
            if (!hoverIsActive)
                return;

            //Translation axis (only applies when crazy mode is deactivated)
            if (translateUpDn != 0 && !crazyModeEnabled && !translationKeysActive)
            {
                translationKeysActive = true;
                if (translateUpDn > 0)
                {
                    verticalSpeed += 1f;
                    if (verticalSpeed > 0f)
                        isLiftingOff = true;
                }
                else
                    verticalSpeed -= 1f;
            }
            else
            {
                translationKeysActive = false;
            }

            //If we just landed then kill vertical speed and exit
            if ((this.part.vessel.situation == Vessel.Situations.LANDED ||
                this.part.vessel.situation == Vessel.Situations.SPLASHED ||
                this.part.vessel.situation == Vessel.Situations.PRELAUNCH) && !isLiftingOff)
            {
                verticalSpeed = 0f;
                return;
            }

            //Once we're flying again, remove the flag.
            else if (VesselIsAirborne())
            {
                isLiftingOff = false;
            }

            //Calculate lift acceleration
            bool applyAcceleration = shouldApplyHoverAcceleration();
            float liftAcceleration = (float)this.part.vessel.graviticAcceleration.magnitude;
            if (verticalSpeed > 0 && vessel.verticalSpeed < verticalSpeed)
                liftAcceleration += verticalSpeed;
            else if (verticalSpeed < 0 && vessel.verticalSpeed > verticalSpeed)
                liftAcceleration += verticalSpeed;

            //First account for max acceleration that this engine can provide
            if (liftAcceleration > maxAcceleration)
                liftAcceleration = maxAcceleration;
            currentThrottle = maxAcceleration - liftAcceleration;

            //Now account for max total acceleration
            if (liftAcceleration > totalMaxAcceleration)
                liftAcceleration = totalMaxAcceleration;

            //Consume resources
            float accelerationRatio = (liftAcceleration / totalMaxAcceleration) - FlightInputHandler.state.mainThrottle;
            if (!CheatOptions.InfinitePropellant && VesselIsAirborne() && accelerationRatio > 0)
            {
                double fuelMass = RequiredPropellantMass(accelerationRatio);
                double propellantReceived = RequestPropellant(fuelMass);

                this.propellantReqMet = (float)(propellantReceived * 100);
                this.fuelFlowGui = (float)(fuelMass * propellantReceived * this.mixtureDensityRecip * this.ratioSum) * (1 / TimeWarp.fixedDeltaTime);
                UpdatePropellantStatus();

                if (propellantReceived < 0.9999f)
                {
                    Flameout(Localizer.Format("#autoLOC_220370"), false, true);
                    return;
                }
            }

            //Only one engine should apply lift acceleration. Check and see if we're the chosen one.
            if (applyAcceleration)
            {
                //Get lift vector
                Vector3d accelerationVector = (this.part.vessel.CoM - this.vessel.mainBody.position).normalized * liftAcceleration;

                //Add acceleration. We do this manually instead of letting ModuleEnginesFX do it so that the craft can have any orientation desired.
                ApplyAccelerationVector(accelerationVector);
            }
        }

        /// <summary>
        /// Updates Crazy Mode propulsion, consuming resources and repositioning the craft as needed.
        /// </summary>
        public virtual void UpdateCrazyMode()
        {
            //If crazy mode isn't enabled then we're done.
            if (!crazyModeUnlocked || !crazyModeEnabled)
                return;

            //Make sure we're flying. If not, then turn off crazy mode.
            if (!VesselIsAirborne())
            {
                StopWarp();
                return;
            }

            //Handle translation axis
            Transform refTransform = this.part.vessel.transform;
            if (translateFwBk != 0 || translateLtRt != 0 || translateUpDn != 0)
            {
                warpVector = Vector3.zero;
                if (translateFwBk != 0)
                {
                   if (translateFwBk > 0)
                        warpVector += refTransform.up;
                    else
                        warpVector += refTransform.up * -1;
                }
                if (translateLtRt != 0)
                {
                    if (translateLtRt > 0)
                        warpVector += refTransform.right;
                    else
                        warpVector += refTransform.right * -1;
                }
                if (translateUpDn != 0)
                {
                    if (translateUpDn > 0)
                        warpVector += refTransform.forward * -1;
                    else
                        warpVector += refTransform.forward;
                }
            }

            // No direction? No translation.
            if (warpVector == Vector3.zero)
                return;

            //Get throttle setting
            float throttleSetting = FlightInputHandler.state.mainThrottle * (thrustPercentage / 100.0f);
            if (throttleSetting <= 0f)
                return;

            //Adjust by engine thrust
            throttleSetting *= (thrustPercentage / 100f);

            //Calculate offset position
            Vector3d offsetPosition = refTransform.position + (warpVector * crazyModeVelocity * throttleSetting * TimeWarp.fixedDeltaTime);

            //Make sure we won't collide with the terrain
            if (Physics.Raycast(refTransform.position, warpVector, out terrainHit, (float)offsetPosition.magnitude, layerMask))
            {
                Part prt = terrainHit.collider.gameObject.GetComponent<Part>();

                //See if we found the ground. 15 = Local Scenery, 28 = TerrainColliders
                if (terrainHit.collider.gameObject.layer == 15 || terrainHit.collider.gameObject.layer == 28)
                {
                    //If we would warp into the ground then stop Crazy Mode.
                    if (terrainHit.distance <= Math.Abs(offsetPosition.magnitude))
                    {
                        ScreenMessages.PostScreenMessage(WBIKFSUtils.kTerrainWarning, 3.0f, ScreenMessageStyle.UPPER_CENTER);
                        StopWarp();
                        return;
                    }
                }
            }

            //Consume the resource
            if (!string.IsNullOrEmpty(crazyModeResource) && !CheatOptions.InfinitePropellant)
            {
                //Don't drop below reserve amount of the resource. This is to avoide flameouts.
                PartResourceDefinition def = PartResourceLibrary.Instance.resourceDefinitions[crazyModeResource];
                double amount;
                double maxAmount;
                this.part.GetConnectedResourceTotals(def.id, out amount, out maxAmount, true);
                if (amount / maxAmount < crazyModeResourceReserve)
                {
                    FlightInputHandler.state.mainThrottle = 0.0f;
                    StopWarp();
                    return;
                }

                double amountRequested = crazyModeResourcePerSec * throttleSetting * TimeWarp.fixedDeltaTime;
                double amountObtained = this.part.RequestResource(crazyModeResource, amountRequested, ResourceFlowMode.ALL_VESSEL);

                //Make sure we got enough of the requested resource.
                if ((amountObtained / amountRequested) < 0.25f)
                {
                    StopWarp();
                    return;
                }
            }

            //A-OK! Warp the ship.
            if (FlightGlobals.VesselsLoaded.Count > 1)
                this.part.vessel.SetPosition(offsetPosition);
            else
                FloatingOrigin.SetOutOfFrameOffset(offsetPosition); //Use this for warp drive?

            //Clear the warp vector if cruise control is disabled.
            if (!crazyCruiseControlEnabled)
            {
                warpDirection = WBIWarpDirections.Stop;
                warpVector = Vector3.zero;
            }
        }

        /// <summary>
        /// Updates thrust based on vessel mass and maximum possible acceleration.
        /// </summary>
        public virtual void UpdateThrust()
        {
            if (!isOperational && !EngineIgnited)
                return;
            if (this.part.vessel == null)
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
            if (engineState != WBIEngineStates.Running || vessel.ctrlState.mainThrottle <= 0)
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

            else if (engineMode == WBIThrustModes.VTOL && !hoverIsActive)
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

        public bool VesselIsAirborne()
        {
            if (this.part.vessel.situation == Vessel.Situations.FLYING || this.part.vessel.situation == Vessel.Situations.SUB_ORBITAL)
                return true;

            return false;
        }

        public bool VesselIsOrbital()
        {
            if (this.part.vessel.situation == Vessel.Situations.ORBITING || this.part.vessel.situation == Vessel.Situations.ESCAPING)
                return true;

            return false;
        }

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
                    else if (propellant.actualTotalAvailable <= 0.0f && (engineMode == WBIThrustModes.VTOL || hoverIsActive))
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

        protected bool shouldApplyHoverAcceleration()
        {
            //Only one active gravitic engine should apply the hover acceleration. It should be the first active engine in the list.
            List<WBIGraviticEngine> engines = this.part.vessel.FindPartModulesImplementing<WBIGraviticEngine>();
            WBIGraviticEngine engine, prevEngine;
            int count = engines.Count;
            bool applyHoverAcceleration = false;

            totalMaxAcceleration = 0;
            for (int index = 0; index < count; index++)
            {
                engine = engines[index];
                if (engine.IsEngineActive() && engine.hoverIsActive)
                {
                    totalMaxAcceleration += engine.maxAcceleration;

                    //If the index is at the top of the list and we're the topmost engine then we should apply acceleartion.
                    if (engine == this && index == 0)
                    {
                        applyHoverAcceleration = true;
                    }

                    //Check previous engine. If it is active and hovering then it will be applying acceleration, so we should not apply acceleration.
                    else if (engine == this)
                    {
                        prevEngine = engines[index - 1];
                        if (prevEngine.IsEngineActive() && prevEngine.hoverIsActive)
                            applyHoverAcceleration = false;
                        else
                            applyHoverAcceleration = true;
                    }
                }
            }

            return applyHoverAcceleration;
        }

        protected void updatePAWGUI()
        {
            //Hover mode disabled when engine isn't on.
            bool isEngineActive = IsEngineActive();
            Events["ToggleHoverMode"].active = isEngineActive && engineState == WBIEngineStates.Running;

            //Thrust vector toggle is disabled when engine isn't on.
            Events["ToggleThrustMode"].active = isEngineActive && engineState == WBIEngineStates.Running;

            //Crazy mode is only available when the vessel is airborne.
            bool isAirborne = VesselIsAirborne();
            Fields["crazyModeEnabled"].guiActive = isEngineActive && isAirborne;

            //If vessel isn't airborne then make sure crazy mode is disabled.
            if (!isAirborne && crazyModeEnabled)
                crazyModeEnabled = false;

            //Crazy cruise is only enabled when crazy mode is.
            Fields["crazyCruiseControlEnabled"].guiActive = crazyModeEnabled;

            //Make sure hover mode is on if crazy mode is on
            if (crazyModeEnabled && !hoverIsActive)
                hoverIsActive = true;

            //Update crazy mode for all active engines.
            if (prevCrazyModeEnabled != crazyModeEnabled)
            {
                prevCrazyModeEnabled = crazyModeEnabled;

                List<WBIGraviticEngine> engines = this.part.vessel.FindPartModulesImplementing<WBIGraviticEngine>();
                int count = engines.Count;
                for (int index = 0; index < count; index++)
                {
                    engines[index].crazyModeEnabled = this.crazyModeEnabled;
                    engines[index].prevCrazyModeEnabled = this.prevCrazyModeEnabled;
                }
            }
            if (prevCruiseModeEnabled != crazyCruiseControlEnabled)
            {
                prevCruiseModeEnabled = crazyCruiseControlEnabled;

                List<WBIGraviticEngine> engines = this.part.vessel.FindPartModulesImplementing<WBIGraviticEngine>();
                int count = engines.Count;
                for (int index = 0; index < count; index++)
                {
                    engines[index].crazyCruiseControlEnabled = this.crazyCruiseControlEnabled;
                    engines[index].prevCruiseModeEnabled = this.prevCruiseModeEnabled;
                }
            }
        }

        protected void updateHoverEventGUI()
        {
            Events["ToggleHoverMode"].guiName = hoverIsActive ? "Disable Hover Mode" : "Enable Hover Mode";
        }

        protected void setCrazyCruiseMode(bool enabled)
        {
            crazyModeEnabled = enabled;
            crazyCruiseControlEnabled = enabled;
        }

        protected void updateWarpVector()
        {
            //Setup the warp direction based on UI buttons
            switch (warpDirection)
            {
                default:
                case WBIWarpDirections.Stop:
                    warpVector = Vector3.zero;
                    return;

                case WBIWarpDirections.Forward:
                    warpVector = this.transform.up;
                    break;

                case WBIWarpDirections.Back:
                    warpVector = this.transform.up * -1;
                    break;

                case WBIWarpDirections.Left:
                    warpVector = this.transform.right * -1;
                    break;

                case WBIWarpDirections.Right:
                    warpVector = this.transform.right;
                    break;

                case WBIWarpDirections.Up:
                    warpVector = this.transform.forward * -1;
                    break;

                case WBIWarpDirections.Down:
                    warpVector = this.transform.forward;
                    break;
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
            Light[] lights = gravRingTransform.gameObject.GetComponentsInChildren<Light>();

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

            if (lights != null)
            {
                foreach (Light light in lights)
                    light.intensity = playInReverse ? 0 : 1;
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

        protected void StopWarp()
        {
            warpDirection = WBIWarpDirections.Stop;
            warpVector = Vector3.zero;
        }

        #endregion

    }
}
