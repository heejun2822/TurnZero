using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace TurnZero
{
    public sealed class LocalBattleEffectsView : MonoBehaviour
    {
        [SerializeField]
        private LocalFieldMap map;
        [SerializeField]
        private GameObject impactPrefab;
        [SerializeField]
        private GameObject projectilePrefab;

        private BattleSession session;
        private readonly List<GameObject> effects = new List<GameObject>();

        public void Bind(BattleSession value)
        {
            session = value;
        }

        public void Play(BattleCue cue)
        {
            StartCoroutine(Effect(cue));
        }

        public void Clear()
        {
            StopAllCoroutines();

            foreach (GameObject effect in effects)
            {
                if (effect != null)
                {
                    Destroy(effect);
                }
            }

            effects.Clear();
        }

        private IEnumerator Effect(BattleCue cue)
        {
            GameObject projectile = null;

            if (cue.SourceVisible && cue.Source != cue.Target)
            {
                projectile = Instantiate(projectilePrefab, transform);
                effects.Add(projectile);

                float elapsed = 0;
                float duration = Mathf.Max(0.05f, session.Configuration.ActionSeconds * 0.5f);

                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    projectile.transform.position = Vector3.Lerp(map.Position(cue.Source) + Vector3.up, map.Position(cue.Target) + Vector3.up * 0.2f, elapsed / duration);
                    yield return null;
                }

                effects.Remove(projectile);
                Destroy(projectile);
            }

            List<GameObject> area = new List<GameObject>();

            foreach (GridCell cell in (cue.VisibleCells ?? BattleVisibility.Area(session.Rules, cue.Target, cue.Area).ToList()))
            {
                GameObject effect = Instantiate(impactPrefab, transform);

                effect.transform.position = map.Position(cell) + Vector3.up * 0.12f;
                effects.Add(effect);
                area.Add(effect);
            }

            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, session.Configuration.ActionSeconds * 0.5f));

            foreach (GameObject effect in area)
            {
                effects.Remove(effect);

                if (effect != null)
                {
                    Destroy(effect);
                }
            }
        }
    }
}
