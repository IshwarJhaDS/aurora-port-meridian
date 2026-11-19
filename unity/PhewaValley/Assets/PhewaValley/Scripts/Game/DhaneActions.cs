using System.Collections.Generic;
using UnityEngine;

namespace PhewaValley
{
    // Clip actions run on the visible model while the controller retains collision ownership.
    public class DhaneActions : MonoBehaviour
    {
        public bool Open { get; private set; }
        Vector2 scroll;
        string selectedGroup = "All", active = "";
        readonly string[] groups = { "All", "Locomotion", "Traversal", "Reactions", "Social", "Blade", "Archery", "Stealth" };
        Game G => Game.Instance;
        Figure Actor => G.player.figure;

        void Update()
        {
            if (!G || !G.Ready || !G.Started) return;
            if (Open && (G.heroes.Current.id != "dhane" || G.player.InCar || G.InDialogue))
                Close();
            if (Open && (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.F4))) { Close(); return; }
            if (!Open && Input.GetKeyDown(KeyCode.F4) && G.heroes.Current.id == "dhane" &&
                !G.Paused && !G.InDialogue && !G.ui.PhotoActive && !G.ui.ShowWork &&
                G.activities.Current == null && !G.player.InCar && !G.player.Busy &&
                G.player.body.isGrounded && !G.heroes.Switching)
            {
                Open = true; active = "";
            }
        }

        void Close()
        {
            Open = false;
            Actor.ResetPose();
        }

        void OnGUI()
        {
            if (!Open || !G || !G.Ready) return;
            var matrix = GUI.matrix;
            float scale = Screen.height / 900f;
            GUI.matrix = Matrix4x4.Scale(Vector3.one * scale);
            var panel = new Rect(20, 35, 420, 825);
            var color = GUI.color;
            GUI.color = new Color(.035f, .045f, .065f, .98f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = color;
            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, wordWrap = true, normal = { textColor = Color.white } };
            var buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 18, wordWrap = true };
            GUILayout.BeginArea(new Rect(panel.x + 16, panel.y + 12, panel.width - 32, panel.height - 24));
            GUILayout.Label("DHANE ACTIONS", new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold });
            GUILayout.Label("F4 / Esc to close. Select an animation to play it.", labelStyle);
            GUILayout.Label("Blade, archery and stealth clips play as demonstrations.", labelStyle);
            int group = System.Array.IndexOf(groups, selectedGroup);
            int next = GUILayout.SelectionGrid(group, groups, 2, buttonStyle);
            if (next != group) { selectedGroup = groups[next]; scroll = Vector2.zero; }
            GUILayout.Space(8);
            GUILayout.Label(Actor.Emoting ? "Playing " + active : "Ready", labelStyle);
            if (GUILayout.Button("Stop and stand", buttonStyle, GUILayout.Height(36))) { Actor.ResetPose(); active = ""; }
            scroll = GUILayout.BeginScrollView(scroll);
            foreach (var source in Actor.ClipNames ?? new List<string>())
            {
                var entry = DhaneAnimations.ForSource(source);
                if (selectedGroup != "All" && entry?.group != selectedGroup) continue;
                string label = entry?.label ?? source;
                if (GUILayout.Button(label, buttonStyle, GUILayout.Height(36)))
                {
                    // Preview in place. The actor's capsule continues to settle against the ground.
                    if (Actor.PlayClip(source, Act.PinXZ | Act.WhileMoving | Act.NoPlant))
                    {
                        active = label;
                        if (entry != null) G.audioDirector.ActionCue(entry.group, entry.key);
                    }
                }
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
            GUI.matrix = matrix;
        }
    }
}
