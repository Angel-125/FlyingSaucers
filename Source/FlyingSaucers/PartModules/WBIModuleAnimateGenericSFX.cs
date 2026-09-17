using System;
using UnityEngine;

/*
Source code copyright 2018-2026, by Michael Billard (Angel-125)
License: GPLV3
*/

namespace WildBlueIndustries
{
    /// <summary>
    /// FlyingSaucers-owned animated part module with start, loop, and stop sounds.
    /// </summary>
    public class WBIModuleAnimateGenericSFX : ModuleAnimateGeneric
    {
        [KSPField]
        public bool debugMode = false;

        [KSPField]
        public string startSoundURL = string.Empty;

        [KSPField]
        public float startSoundPitch = 1.0f;

        [KSPField]
        public float startSoundVolume = 0.5f;

        [KSPField]
        public string loopSoundURL = string.Empty;

        [KSPField]
        public float loopSoundPitch = 1.0f;

        [KSPField]
        public float loopSoundVolume = 0.5f;

        [KSPField]
        public string stopSoundURL = string.Empty;

        [KSPField]
        public float stopSoundPitch = 1.0f;

        [KSPField]
        public float stopSoundVolume = 0.5f;

        [KSPField]
        public string enabledModules = string.Empty;

        [KSPField(isPersistant = true)]
        public bool isDeployed = false;

        [KSPField(isPersistant = true)]
        public bool modulesEnabled = false;

        private AudioSource loopSound;
        private AudioSource startSound;
        private AudioSource stopSound;
        private bool isMoving;

        public override void OnStart(StartState state)
        {
            base.OnStart(state);

            Fields["isDeployed"].guiActive = debugMode;
            Fields["isDeployed"].guiActiveEditor = debugMode;
            Fields["modulesEnabled"].guiActive = debugMode;
            Fields["modulesEnabled"].guiActiveEditor = debugMode;

            if (!HighLogic.LoadedSceneIsFlight)
                return;

            startSound = createAudioSource(startSoundURL, startSoundPitch, startSoundVolume, false);
            loopSound = createAudioSource(loopSoundURL, loopSoundPitch, loopSoundVolume, true);
            stopSound = createAudioSource(stopSoundURL, stopSoundPitch, stopSoundVolume, false);
            setModulesActive(isDeployed);
        }

        public override void OnUpdate()
        {
            base.OnUpdate();
            if (!HighLogic.LoadedSceneIsFlight)
                return;

            if (isMoving)
                updateDeployStatus();

            if (isDeployed != moduleIsEnabled)
            {
                moduleIsEnabled = isDeployed;
                setModulesActive(isDeployed);
            }

            if (aniState == animationStates.MOVING && !isMoving)
            {
                isMoving = true;
                playStart();
            }
            else if ((aniState == animationStates.LOCKED || aniState == animationStates.CLAMPED) && isMoving)
            {
                isMoving = false;
                playEnd();
            }
        }

        private AudioSource createAudioSource(string soundURL, float pitch, float volume, bool loop)
        {
            if (string.IsNullOrEmpty(soundURL))
                return null;

            AudioSource audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.clip = GameDatabase.Instance.GetAudioClip(soundURL);
            audioSource.pitch = pitch;
            audioSource.volume = GameSettings.SHIP_VOLUME * volume;
            audioSource.loop = loop;
            return audioSource;
        }

        private void playStart()
        {
            if (startSound != null)
                startSound.Play();
            if (loopSound != null)
                loopSound.Play();
        }

        private void playEnd()
        {
            if (stopSound != null)
                stopSound.Play();
            if (loopSound != null)
                loopSound.Stop();
        }

        private void updateDeployStatus()
        {
            if (anim == null || anim[animationName] == null)
                return;

            if (anim[animationName].normalizedTime >= 0.999f)
                isDeployed = true;
            else if (anim[animationName].normalizedTime < 0.001f)
                isDeployed = false;
        }

        private void setModulesActive(bool isActive)
        {
            if (string.IsNullOrEmpty(enabledModules) || !HighLogic.LoadedSceneIsFlight)
                return;

            string[] moduleNames = enabledModules.Split(new char[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int index = 0; index < part.Modules.Count; index++)
            {
                for (int moduleIndex = 0; moduleIndex < moduleNames.Length; moduleIndex++)
                {
                    if (part.Modules[index].moduleName != moduleNames[moduleIndex].Trim())
                        continue;

                    part.Modules[index].enabled = isActive;
                    part.Modules[index].isEnabled = isActive;
                    part.Modules[index].moduleIsEnabled = isActive;
                    break;
                }
            }

            modulesEnabled = isActive;
        }
    }
}
