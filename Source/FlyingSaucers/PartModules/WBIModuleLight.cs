using UnityEngine;

/*
Source code copyright 2018-2026, by Michael Billard (Angel-125)
License: GPLV3
*/

namespace WildBlueIndustries
{
    /// <summary>
    /// FlyingSaucers-owned animated light with color, intensity, and EC consumption.
    /// </summary>
    [KSPModule("Light")]
    public class WBIModuleLight : PartModule
    {
        [KSPField]
        public string animationName = string.Empty;

        [KSPField]
        public string startEventGUIName = "Lights On";

        [KSPField]
        public string endEventGUIName = "Lights Off";

        [KSPField]
        public int animationLayer = 3;

        [KSPField(isPersistant = true)]
        public bool isDeployed;

        [KSPField(isPersistant = true)]
        public double ecRequired;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "red")]
        [UI_FloatRange(stepIncrement = 0.05f, maxValue = 1f, minValue = 0f)]
        public float red = 1f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "green")]
        [UI_FloatRange(stepIncrement = 0.05f, maxValue = 1f, minValue = 0f)]
        public float green = 1f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "blue")]
        [UI_FloatRange(stepIncrement = 0.05f, maxValue = 1f, minValue = 0f)]
        public float blue = 1f;

        [KSPField(isPersistant = true, guiActive = true, guiActiveEditor = true, guiName = "level")]
        [UI_FloatRange(stepIncrement = 0.05f, maxValue = 1f, minValue = 0f)]
        public float level = 1f;

        [KSPField]
        public float intensity = 1f;

        private Animation animation;
        private Light[] lights;
        private float previousRed;
        private float previousGreen;
        private float previousBlue;
        private float previousLevel;

        [KSPEvent(guiActive = true, guiActiveEditor = true, guiName = "Lights On")]
        public void ToggleLights()
        {
            setLightState(!isDeployed, true);
        }

        [KSPAction("Toggle Lights", KSPActionGroup.Light)]
        public void ToggleLightsAction(KSPActionParam param)
        {
            setLightState(!isDeployed, false);
        }

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            Animation[] animators = part.FindModelAnimators(animationName);
            if (animators != null && animators.Length > 0)
            {
                animation = animators[0];
                if (animation[animationName] != null)
                    animation[animationName].layer = animationLayer;
            }

            lights = part.gameObject.GetComponentsInChildren<Light>();
            applyState(true);
        }

        public override void OnFixedUpdate()
        {
            base.OnFixedUpdate();
            if (!HighLogic.LoadedSceneIsFlight || !isDeployed || ecRequired <= 0.0)
                return;

            double requested = ecRequired * TimeWarp.fixedDeltaTime;
            double received = part.RequestResource("ElectricCharge", requested, ResourceFlowMode.ALL_VESSEL);
            if (requested > 0.0 && received / requested < 0.999)
                setLightState(false, false);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (previousRed != red || previousGreen != green || previousBlue != blue || previousLevel != level)
                setupLights();
        }

        private void setLightState(bool state, bool updateSymmetry)
        {
            isDeployed = state;
            applyState(false);

            if (!updateSymmetry || !HighLogic.LoadedSceneIsEditor)
                return;

            for (int index = 0; index < part.symmetryCounterparts.Count; index++)
            {
                WBIModuleLight counterpart = part.symmetryCounterparts[index].FindModuleImplementing<WBIModuleLight>();
                if (counterpart != null)
                    counterpart.setLightState(state, false);
            }
        }

        private void applyState(bool instant)
        {
            Events["ToggleLights"].guiName = isDeployed ? endEventGUIName : startEventGUIName;

            if (animation != null && animation[animationName] != null)
            {
                AnimationState animationState = animation[animationName];
                if (instant)
                {
                    animationState.normalizedTime = isDeployed ? 1f : 0f;
                    animationState.speed = 0f;
                }
                else
                {
                    animationState.normalizedTime = isDeployed ? 0f : 1f;
                    animationState.speed = isDeployed ? 1f : -1f;
                }
                animation.Play(animationName);
            }

            setupLights();
        }

        private void setupLights()
        {
            if (lights == null)
                return;

            Color color = new Color(red, green, blue, 1f);
            for (int index = 0; index < lights.Length; index++)
            {
                lights[index].color = color;
                lights[index].intensity = isDeployed ? intensity * level : 0f;
            }

            previousRed = red;
            previousGreen = green;
            previousBlue = blue;
            previousLevel = level;
        }
    }
}
