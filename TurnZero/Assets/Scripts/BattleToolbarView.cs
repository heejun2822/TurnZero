using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using static TurnZero.BattleUiStyle;
namespace TurnZero
{
    public sealed class BattleToolbarView : MonoBehaviour
    {
        [SerializeField] private Button switchButton, rotateButton, zoomInButton, zoomOutButton, resolveButton, exportButton, restartButton;
        [SerializeField] private Text switchText;
        public void Bind(Action switchPlayer, BattleCameraRig camera, Action resolve, Action save, Action restart)
        {
            switchButton.onClick.AddListener(() => switchPlayer());
            rotateButton.onClick.AddListener(camera.Rotate);
            zoomInButton.onClick.AddListener(() => camera.Zoom(-0.1f));
            zoomOutButton.onClick.AddListener(() => camera.Zoom(0.1f));
            resolveButton.onClick.AddListener(() => resolve());
            exportButton.onClick.AddListener(() => save());
            restartButton.onClick.AddListener(() => restart());
        }
        public void Render(PlayerView view)
        {
            switchText.text = $"VIEW P{view.Player + 1} / SWITCH";
            resolveButton.interactable = view.Phase == MatchPhase.Planning;
        }
    }
}
