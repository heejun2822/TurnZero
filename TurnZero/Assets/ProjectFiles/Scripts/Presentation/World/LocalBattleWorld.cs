using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

namespace TurnZero.Game
{
    public sealed class LocalBattleWorld : MonoBehaviour
    {
        [SerializeField] private LocalFieldMap map;
        private LocalTileView[] tiles;
        [SerializeField] private LocalActorView[] actors, ghosts;
        [SerializeField] private LineRenderer pathLine;
        [SerializeField] private GameObject itemPrefab, structurePrefab, impactPrefab, projectilePrefab;
        [SerializeField] private Transform dynamicRoot;
        private BattleSession session;
        private Camera viewCamera;
        private readonly Dictionary<string, GameObject> markers = new Dictionary<string, GameObject>();
        private readonly List<GameObject> effects = new List<GameObject>();
        public float Spacing => map.Spacing;
        public Vector3 Position(GridCell cell) => map.Position(cell);
        public void Bind(BattleSession value, Camera camera)
        {
            session = value; viewCamera = camera;
            var error = map.LayoutError();
            if (error != null) throw new System.InvalidOperationException(error);
            tiles = map.Tiles;
            foreach (var actor in actors.Concat(ghosts)) actor.Bind(camera);
            session.Events.Changed += Refresh; session.Events.CueRequested += Cue;
        }
        public bool TryCell(Camera camera, Vector2 screen, out GridCell cell)
        {
            var ray = camera.ScreenPointToRay(screen);
            if (Physics.Raycast(ray,out var hit,300))
            {
                var actor = hit.collider.GetComponentInParent<LocalActorView>();
                if (actor != null) { cell = actor.Cell; return true; }
                var tile = hit.collider.GetComponentInParent<LocalTileView>();
                if (tile != null) { cell = tile.Cell; return true; }
            }
            var plane = new Plane(Vector3.up,Vector3.zero);
            if (plane.Raycast(ray,out float distance))
            {
                var point = ray.GetPoint(distance);
                cell = map.WorldToCell(point);
                return session.Rules.Contains(cell);
            }
            cell = default; return false;
        }
        public bool TryActor(Camera camera, Vector2 screen, out int id)
        {
            id = 0;
            if (!Physics.Raycast(camera.ScreenPointToRay(screen),out var hit,300)) return false;
            var actor = hit.collider.GetComponentInParent<LocalActorView>();
            if (actor == null || actor.EntityId >= 101) return false;
            id = actor.EntityId; return true;
        }
        private void Refresh()
        {
            if (session == null) return;
            var view = session.View; var rules = session.Rules;
            if (view.Phase == BattlePhase.Setup)
                foreach (var actor in actors.Concat(ghosts)) actor.EnsureModel(actor.EntityId >= 101 ? session.Configuration.BaseModel : session.Configuration.Characters[(actor.EntityId-1)%5].Model);
            var selected = view.Units.Find(u => u.Id == session.SelectedId);
            bool planning = view.Phase == BattlePhase.Planning && !view.Ready && selected != null && selected.Owner == view.Player;
            var spec = selected == null ? null : session.Action == BattleAction.Move ? rules.Characters[selected.Slot].Move : session.Action == BattleAction.Attack ? rules.Characters[selected.Slot].Attack :
                session.Action == BattleAction.Skill ? rules.Characters[selected.Slot].Skill : session.Action == BattleAction.Equipment && selected.Equipment >= 0 ? rules.Equipment[selected.Equipment].Action : null;
            bool moving = selected != null && (session.Action == BattleAction.Move || session.Action == BattleAction.Skill && rules.Characters[selected.Slot].SkillEffect == SkillEffect.StealthMove);
            var reachable = planning && session.Armed && moving ? session.Match.Reachable(selected.Id,session.Action == BattleAction.Skill) : null;
            foreach (var tile in tiles)
            {
                var cell = tile.Cell; Color? highlight = null;
                bool deploy = rules.Home(view.Player,cell) && (!view.OwnBase.Placed || cell != view.OwnBase.Cell) &&
                    (view.Phase == BattlePhase.BasePlacement || view.Phase == BattlePhase.UnitPlacement && view.OwnBase.Cell.Distance(cell) <= rules.BaseVision);
                if (deploy) highlight = new Color(0.08f,0.48f,0.45f);
                if (planning && session.Armed)
                {
                    bool range = spec != null && selected.Cell.Distance(cell) <= spec.Range;
                    if (moving) range = reachable.Contains(cell);
                    if (session.Action == BattleAction.Revive) range = rules.Home(view.Player,cell) && view.OwnBase.Cell.Distance(cell) <= rules.BaseVision && cell != view.OwnBase.Cell;
                    if (session.PortalEntrance.HasValue) range = session.PortalEntrance.Value.Distance(cell) <= rules.Equipment[selected.Equipment].ExitRange && view.Explored.Contains(cell);
                    if (session.Action == BattleAction.Equipment && selected.Equipment >= 0 && rules.Equipment[selected.Equipment].Effect == EquipmentEffect.Swap)
                        range = view.Units.Any(u => u.Owner == view.Player && u.Alive && u.Id != selected.Id && u.Cell == cell);
                    else if (session.Action == BattleAction.Equipment && (cell == view.OwnBase.Cell || view.EnemyBase != null && cell == view.EnemyBase.Cell)) range = false;
                    if (range) highlight = session.Action == BattleAction.Move || session.Action == BattleAction.Revive ? new Color(0.06f,0.45f,0.45f) : new Color(0.54f,0.31f,0.12f);
                    int previewRadius = spec == null ? 0 : Mathf.Max(0,spec.Area-1);
                    if (session.Action == BattleAction.Equipment && selected.Equipment >= 0 && rules.Equipment[selected.Equipment].Effect == EquipmentEffect.Beacon) previewRadius = spec.Area;
                    if (session.HoverCell.HasValue && spec != null && session.Action != BattleAction.Move && cell.Distance(session.HoverCell.Value) <= previewRadius && selected.Cell.Distance(session.HoverCell.Value) <= spec.Range)
                        highlight = new Color(0.95f,0.47f,0.19f);
                }
                if (view.Orders.Any(o => o.Target == cell)) highlight = new Color(0.12f,0.65f,0.73f);
                if (session.PathPreview.Contains(cell)) highlight = new Color(0.15f,0.9f,0.8f);
                if (selected != null && selected.Alive && selected.Cell == cell) highlight = new Color(0.3f,0.95f,0.84f);
                int owner = cell.Y < rules.HomeHeight ? 0 : cell.Y >= rules.HomeHeight + rules.NeutralHeight ? 1 : -1;
                var field = session.Configuration.Fields[session.Fields[owner < 0 ? 0 : owner]];
                var ground = owner < 0 ? new Color(0.28f,0.25f,0.20f) : field.TileColor(cell.X,owner == 1 ? rules.Height - 1 - cell.Y : cell.Y);
                tile.Render(ground,view.Sight(cell),highlight);
            }
            foreach (var actor in actors)
            {
                if (actor.EntityId >= 101)
                {
                    int owner = actor.EntityId - 101; var home = owner == view.Player ? view.OwnBase : view.EnemyBase;
                    bool visible = home != null && home.Placed && home.Health > 0;
                    actor.Render(visible,home?.Cell ?? default,visible ? Position(home.Cell) : Vector3.zero,home == null ? "" : $"P{owner+1} BASE\n{home.Health} / {rules.BaseHealth}",Team(owner),false,owner != view.Player && !view.EnemyBaseVisible);
                    continue;
                }
                var unit = view.Units.Find(u => u.Id == actor.EntityId && u.Alive);
                Vector3 offset = Vector3.zero;
                int stackCount = 1; bool showLabel = true;
                if (unit != null)
                {
                    var stack = view.Units.Where(u => u.Alive && u.Cell == unit.Cell).OrderBy(u => u.Id).ToList(); int index = stack.FindIndex(u => u.Id == unit.Id);
                    stackCount = stack.Count;
                    if (stackCount > 1)
                    {
                        offset = new Vector3(Mathf.Cos(index*Mathf.PI*2/stackCount),0,Mathf.Sin(index*Mathf.PI*2/stackCount))*Spacing*0.31f;
                        showLabel = unit.Id == session.SelectedId || !stack.Any(u => u.Id == session.SelectedId) && index == 0;
                    }
                }
                string text = unit == null ? "" : $"{unit.Slot+1}  {unit.Health}/{rules.Characters[unit.Slot].Health}" + (unit.Rooted(view.Turn) ? " ROOT" : unit.HiddenThrough >= view.Turn && unit.HiddenThrough > 0 ? " HIDDEN" : "");
                if (stackCount > 1) text += $"\nSTACK {stackCount}";
                actor.Render(unit != null,unit?.Cell ?? default,unit == null ? Vector3.zero : Position(unit.Cell)+offset,text,Team(unit?.Owner ?? 0),unit?.Id == session.SelectedId,false,showLabel,stackCount > 1 ? 0.62f : 1);
            }
            foreach (var ghost in ghosts)
            {
                var seen = view.LastSeen.Find(o => o.Unit.Id == ghost.EntityId);
                ghost.Render(seen != null,seen?.Unit.Cell ?? default,seen == null ? Vector3.zero : Position(seen.Unit.Cell),seen == null ? "" :
                    $"{rules.Characters[seen.Unit.Slot].Name}\n{view.Turn-seen.Turn} T AGO / {seen.Unit.Health} HP",Color.gray,false,true);
            }
            foreach (var marker in markers.Values) marker.SetActive(false);
            foreach (var item in view.Items.Where(i => !i.Collected[view.Player])) Marker($"item/{item.Id}",itemPrefab,item.Cell,$"CRAFT",new Color(0.96f,0.72f,0.25f));
            foreach (var structure in view.Structures) Marker($"structure/{structure.Id}",structurePrefab,structure.Cell,
                structure.Kind == StructureKind.Beacon ? $"VISION\n{structure.Health} HP" : $"{(structure.Kind == StructureKind.PortalEntrance ? "IN" : "OUT")}\n{(view.Turn >= structure.ReadyTurn ? "READY" : $"{structure.ReadyTurn-view.Turn} T")} / {structure.Health} HP",Team(structure.Owner));
            foreach (var fire in view.Fires)
                foreach (var cell in BattleVisibility.Area(rules,fire.Cell,Mathf.Max(0,fire.Area-1)).Where(c => fire.Owner == view.Player || view.Visible.Contains(c)))
                    Marker($"fire/{fire.Id}/{cell}",impactPrefab,cell,"",new Color(0.72f,0.19f,0.05f));
            RenderPath(view,selected);
            if (view.Phase == BattlePhase.Setup)
            {
                foreach (var marker in markers.Values) Destroy(marker); markers.Clear();
                foreach (var effect in effects) if (effect != null) Destroy(effect); effects.Clear();
            }
        }
        private void RenderPath(BattleView view, BattleUnit selected)
        {
            var points = new List<GridCell>();
            if (selected != null && selected.Alive && selected.Owner == view.Player)
            {
                points.Add(selected.Cell);
                var order = view.Orders.Find(o => o.UnitId == selected.Id);
                if (session.PathPreview.Count > 0) points.AddRange(session.PathPreview);
                else if (session.Armed && session.HoverCell.HasValue && (session.Action == BattleAction.Move || session.Action == BattleAction.Skill && session.Rules.Characters[selected.Slot].SkillEffect == SkillEffect.StealthMove))
                    points.AddRange(session.Match.Path(selected.Id,session.HoverCell.Value,session.Action == BattleAction.Skill));
                else if (order != null) { if (order.Path.Count > 0) points.AddRange(order.Path); else points.Add(order.Target); }
            }
            pathLine.gameObject.SetActive(points.Count > 1);
            if (points.Count > 1) { pathLine.positionCount = points.Count; pathLine.SetPositions(points.Select(c => Position(c)+Vector3.up*0.18f).ToArray()); }
        }
        private void Marker(string key, GameObject prefab, GridCell cell, string text, Color color)
        {
            if (!markers.TryGetValue(key,out var marker)) { marker = Instantiate(prefab,dynamicRoot); marker.name = key; markers.Add(key,marker); }
            marker.SetActive(true); marker.transform.position = Position(cell) + Vector3.up * 0.07f;
            var label = marker.GetComponentInChildren<TMP_Text>(true);
            if (label != null) { label.text = text; label.transform.rotation = viewCamera.transform.rotation; }
            var block = new MaterialPropertyBlock(); block.SetColor("_BaseColor",color);
            foreach (var renderer in marker.GetComponentsInChildren<MeshRenderer>()) renderer.SetPropertyBlock(block);
        }
        private void Cue(BattleCue cue)
        {
            if (cue.Kind == CueKind.Move || cue.Kind == CueKind.Portal)
            {
                var actor = actors.FirstOrDefault(a => a.EntityId == cue.ActorId);
                if (actor != null && actor.gameObject.activeSelf) actor.MoveTo(Position(cue.Target),session.Configuration.MoveSeconds);
                return;
            }
            StartCoroutine(Effect(cue));
        }
        private IEnumerator Effect(BattleCue cue)
        {
            GameObject projectile = null;
            if (cue.SourceVisible && cue.Source != cue.Target)
            {
                projectile = Instantiate(projectilePrefab,dynamicRoot); effects.Add(projectile);
                float elapsed = 0, duration = Mathf.Max(0.05f,session.Configuration.ActionSeconds * 0.5f);
                while (elapsed < duration)
                {
                    elapsed += Time.unscaledDeltaTime; projectile.transform.position = Vector3.Lerp(Position(cue.Source)+Vector3.up,Position(cue.Target)+Vector3.up*0.2f,elapsed/duration); yield return null;
                }
                effects.Remove(projectile); Destroy(projectile);
            }
            var area = new List<GameObject>();
            var view = session.View;
            foreach (var cell in BattleVisibility.Area(session.Rules,cue.Target,Mathf.Max(0,cue.Area-1)).Where(c => cue.Owner == session.Player || view.Visible.Contains(c)))
            {
                var effect = Instantiate(impactPrefab,dynamicRoot); effect.transform.position = Position(cell)+Vector3.up*0.12f; effects.Add(effect); area.Add(effect);
            }
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f,session.Configuration.ActionSeconds * 0.5f));
            foreach (var effect in area) { effects.Remove(effect); if (effect != null) Destroy(effect); }
        }
        private void LateUpdate()
        {
            if (viewCamera == null) return;
            foreach (var marker in markers.Values.Where(m => m.activeSelf))
            { var label = marker.GetComponentInChildren<TMP_Text>(); if (label != null) label.transform.rotation = viewCamera.transform.rotation; }
        }
        public static Color Team(int player) => player == 0 ? new Color(0.12f,0.8f,0.76f) : new Color(1,0.42f,0.32f);
        private void OnDestroy() { if (session != null) { session.Events.Changed -= Refresh; session.Events.CueRequested -= Cue; } }
    }
}
