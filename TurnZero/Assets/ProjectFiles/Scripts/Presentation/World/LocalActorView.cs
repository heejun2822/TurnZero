using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TurnZero
{
    public sealed class LocalActorView : MonoBehaviour
    {
        [SerializeField]
        private int entityId;
        [SerializeField]
        private TMP_Text label;
        [SerializeField]
        private Renderer selection;
        [SerializeField]
        private Renderer[] teamParts;
        [SerializeField]
        private Renderer[] modelParts;
        [SerializeField]
        private GameObject modelRoot;
        [SerializeField]
        private GameObject modelPrefab;
        [SerializeField]
        private bool ghostModel;
        private Camera viewCamera;
        private Coroutine movement;
        private MaterialPropertyBlock block;
        private Vector3 modelScale = Vector3.one;

        public int EntityId => entityId;
        public GridCell Cell { get; private set; }

        public void Bind(Camera camera)
        {
            viewCamera = camera;

            if (modelRoot != null)
            {
                modelScale = modelRoot.transform.localScale;
            }
        }

        public void EnsureModel(GameObject prefab)
        {
            if (prefab == null || prefab == modelPrefab)
            {
                return;
            }

            if (modelRoot != null)
            {
                Destroy(modelRoot);
            }

            modelRoot = Instantiate(prefab, transform);
            modelRoot.name = "Model";
            modelPrefab = prefab;

            modelScale = modelRoot.transform.localScale;
            modelParts = modelRoot.GetComponentsInChildren<MeshRenderer>();
            teamParts = modelParts.Where(r => r.name.Contains("Scarf") || r.name.Contains("Banner")).ToArray();

            if (teamParts.Length == 0 && modelParts.Length > 0)
            {
                teamParts = new[] { modelParts[0] };
            }

            if (ghostModel)
            {
                foreach (Collider collider in modelRoot.GetComponentsInChildren<Collider>())
                {
                    collider.enabled = false;
                }
            }
        }

        public void Render(
            bool visible,
            GridCell cell,
            Vector3 position,
            string text,
            Color team,
            bool selected,
            bool ghost = false,
            bool showLabel = true,
            float modelSize = 1)
        {
            if (movement != null)
            {
                StopCoroutine(movement);
                movement = null;
            }

            gameObject.SetActive(visible);

            if (!visible)
            {
                return;
            }

            Cell = cell;
            transform.position = position;
            label.text = text;
            label.color = ghost ? new Color(0.55f, 0.62f, 0.67f) : Color.white;

            label.gameObject.SetActive(showLabel);

            if (modelRoot != null)
            {
                modelRoot.transform.localScale = modelScale * modelSize;
            }

            if (block == null)
            {
                block = new MaterialPropertyBlock();
            }

            foreach (Renderer renderer in modelParts)
            {
                Color original = renderer.sharedMaterial.HasProperty("_BaseColor")
                    ? renderer.sharedMaterial.GetColor("_BaseColor")
                    : Color.white;

                block.SetColor("_BaseColor", ghost ? new Color(0.12f, 0.16f, 0.19f) : original);
                renderer.SetPropertyBlock(block);
            }

            foreach (Renderer renderer in teamParts)
            {
                block.SetColor("_BaseColor", ghost ? new Color(0.2f, 0.25f, 0.29f) : team);
                renderer.SetPropertyBlock(block);
            }

            selection.gameObject.SetActive(selected);

            block.SetColor("_BaseColor", team);
            selection.SetPropertyBlock(block);
        }

        public void MoveTo(Vector3 target, float seconds)
        {
            if (movement != null)
            {
                StopCoroutine(movement);
            }

            movement = StartCoroutine(Move(target, seconds));
        }

        private IEnumerator Move(Vector3 target, float seconds)
        {
            Vector3 start = transform.position;
            float elapsed = 0;

            while (elapsed < seconds)
            {
                elapsed += Time.unscaledDeltaTime;
                transform.position = Vector3.Lerp(start, target, Mathf.Clamp01(elapsed / Mathf.Max(0.001f, seconds)));
                yield return null;
            }

            transform.position = target;
            movement = null;
        }

        private void LateUpdate()
        {
            if (viewCamera != null)
            {
                label.transform.rotation = viewCamera.transform.rotation;
            }
        }
    }
}
