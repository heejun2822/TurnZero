using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace TurnZero
{
    public sealed class LocalWorldMarkersView : MonoBehaviour
    {
        [SerializeField]
        private LocalFieldMap map;
        [SerializeField]
        private GameObject itemPrefab;
        [SerializeField]
        private GameObject structurePrefab;
        [SerializeField]
        private GameObject impactPrefab;

        private BattleSession session;
        private Camera viewCamera;
        private MaterialPropertyBlock block;
        private readonly Dictionary<string, GameObject> markers = new Dictionary<string, GameObject>();

        public void Bind(BattleSession value, Camera camera)
        {
            session = value;
            viewCamera = camera;
            block = new MaterialPropertyBlock();
        }

        public void Render(BattleView view)
        {
            GameRules rules = session.Rules;

            foreach (GameObject marker in markers.Values)
            {
                marker.SetActive(false);
            }

            foreach (GroundItem item in view.Items)
            {
                Marker($"item/{item.Id}", itemPrefab, item.Cell, $"CRAFT", new Color(0.96f, 0.72f, 0.25f));
            }

            foreach (BattleStructure structure in view.Structures)
            {
                Marker(
                    $"structure/{structure.Id}",
                    structurePrefab,
                    structure.Cell,
                    structure.Kind == StructureKind.Beacon
                    ? $"VISION\n{structure.Health} HP"
                    : $"{(structure.Kind == StructureKind.PortalEntrance ? "IN" : "OUT")}\n{(structure.Completed ? "READY" : $"{structure.ReadyTurn - view.Turn} T")} / {structure.Health} HP",
                    LocalBattleWorld.Team(structure.Owner));
            }

            foreach (FireZone fire in view.Fires)
            {
                foreach (GridCell cell in BattleVisibility.Area(rules, fire.Cell, fire.Area))
                {
                    if (fire.Owner != view.Player && !view.Visible.Contains(cell))
                    {
                        continue;
                    }

                    Marker($"fire/{fire.Id}/{cell}", impactPrefab, cell, "", new Color(0.72f, 0.19f, 0.05f));
                }
            }

            if (view.Phase == BattlePhase.Setup)
            {
                Clear();
            }
        }

        private void Marker(string key, GameObject prefab, GridCell cell, string text, Color color)
        {
            if (!markers.TryGetValue(key, out GameObject marker))
            {
                marker = Instantiate(prefab, transform);
                marker.name = key;
                markers.Add(key, marker);
            }

            marker.SetActive(true);
            marker.transform.position = map.Position(cell) + Vector3.up * 0.07f;

            TMP_Text label = marker.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
            {
                label.text = text;
                label.transform.rotation = viewCamera.transform.rotation;
            }

            block.SetColor("_BaseColor", color);

            foreach (MeshRenderer renderer in marker.GetComponentsInChildren<MeshRenderer>())
            {
                renderer.SetPropertyBlock(block);
            }
        }

        private void LateUpdate()
        {
            if (viewCamera == null)
            {
                return;
            }

            foreach (GameObject marker in markers.Values)
            {
                if (!marker.activeSelf)
                {
                    continue;
                }

                TMP_Text label = marker.GetComponentInChildren<TMP_Text>();

                if (label != null)
                {
                    label.transform.rotation = viewCamera.transform.rotation;
                }
            }
        }

        private void Clear()
        {
            foreach (GameObject marker in markers.Values)
            {
                Destroy(marker);
            }

            markers.Clear();
        }
    }
}
