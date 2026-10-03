using UnityEngine;

namespace PrefabExporter.Samples
{
    /// <summary>Provides manual playback controls for prefab export samples.</summary>
    [DisallowMultipleComponent]
    public sealed class PrefabExportTest : MonoBehaviour
    {
        private const string PlayMenu = "Test/Play";
        private const string StopMenu = "Test/Stop And Clear";

        [SerializeField] private ParticleSystem effect;

        [ContextMenu(PlayMenu)]
        public void Play()
        {
            if (effect == null)
            {
                return;
            }

            effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            effect.Play(true);
        }

        [ContextMenu(StopMenu)]
        public void StopAndClear()
        {
            if (effect != null)
            {
                effect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }

        private void Reset()
        {
            effect = GetComponentInChildren<ParticleSystem>(true);
        }
    }
}
