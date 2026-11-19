using System.Collections.Generic;

namespace PhewaValley
{
    // Match the source names exactly. Several Tripo names are truncated descriptions.
    public static class DhaneAnimations
    {
        public class Entry
        {
            public readonly string key, label, group, source;
            public Entry(string key, string label, string group, string source)
            { this.key = key; this.label = label; this.group = group; this.source = source; }
        }
        public static readonly Entry[] All =
        {
            new Entry("idle", "Idle", "Locomotion", "idle"),
            new Entry("heavy_cut", "Heavy cut", "Blade", "From a compact guard, load the hips and deliver a heavy horizon"),
            new Entry("land_soft", "Land soft", "Reactions", "land_soft: Dhane begins at ground contact after a small jump. F"),
            new Entry("blade_ready", "Blade ready", "Blade", "Stand in a compact right-handed guard. Left foot slightly forwa"),
            new Entry("fall", "Fall", "Reactions", "fall"),
            new Entry("crouch_idle", "Crouch idle", "Locomotion", "crouch_idle: Dhane holds a low scouting stance with bent knees,"),
            new Entry("wave", "Wave", "Social", "Nepali namaste"),
            new Entry("defensive_cut", "Defensive cut", "Blade", "ut\tMake a compact defensive cut while stepping backward. Move t"),
            new Entry("air_cut", "Air cut", "Blade", "Starting near the apex of a jump, make one compact right-handed"),
            new Entry("stealth_grab", "Stealth grab", "Stealth", "stealth_grab: From a low stance behind an imagined opponent, Dh"),
            new Entry("hit_light", "Hit light", "Reactions", "ight\tReact to a light hit from the front. Chest recoils, should"),
            new Entry("land_heavy", "Land heavy", "Reactions", "land_heavy: Dhane absorbs a higher drop. Feet contact first, hi"),
            new Entry("bow_draw", "Bow draw", "Archery", "bow_draw: Dhane stands side-on in an archer\u2019s stance. His lef"),
            new Entry("forward_roll", "Forward roll", "Traversal", "forward_roll: Dhane lowers from a forward run, tucks the head, "),
            new Entry("receive_weapon_bow", "Receive weapon bow", "Archery", "receive_weapon_bow: Dhane stands respectfully before an imagine"),
            new Entry("hit_left", "Hit left", "Reactions", "hit_left: Dhane reacts to a hit from his left side. Turn the ri"),
            new Entry("inspect_clue", "Inspect clue", "Social", "inspect_clue: Dhane notices a detail on the ground, lowers onto"),
            new Entry("thrust_recovery", "Thrust recovery", "Blade", "p\tStart with the right arm extended after a forward thrust. Dra"),
            new Entry("listen_and_agree", "Listen and agree", "Social", "listen_and_agree: Dhane stands listening to someone imagined ju"),
            new Entry("run_upstairs", "Run upstairs", "Locomotion", "run_upstairs"),
            new Entry("run", "Run", "Locomotion", "run"),
            new Entry("bow_release", "Bow release", "Archery", "bow_release: Dhane begins at full draw in a side-on archer stan"),
            new Entry("diagonal_cut", "Diagonal cut", "Blade", "Deliver a quick diagonal right-handed cut from high right to lo"),
            new Entry("jump_takeoff", "Jump takeoff", "Traversal", "jump_takeoff: From standing, Dhane dips briefly, swings the arm"),
            new Entry("jump_air", "Jump air", "Traversal", "jump_air: Dhane holds a balanced airborne pose near the apex of"),
            new Entry("hit_to_stomach", "Hit to stomach", "Reactions", "hit_to_stomach"),
            new Entry("hit_to_head", "Hit to head", "Reactions", "hit_to_head"),
            new Entry("hit_to_body_02", "Hit to body 02", "Reactions", "hit_to_body_02"),
            new Entry("bow_hold", "Bow hold", "Archery", "bow_hold: Dhane holds full draw in a side-on archer stance. Lef"),
            new Entry("close_elbow", "Close elbow", "Blade", "w\tAt close range, keep the right hand gripping the invisible ha"),
            new Entry("crouch_walk", "Crouch walk", "Locomotion", "crouch_walk: Dhane moves quietly in a low scouting stance. Take"),
            new Entry("ledge_climb", "Ledge climb", "Traversal", "ledge_climb: Dhane climbs an imagined waist-to-chest-high ledge"),
            new Entry("defensive_cut_alt", "Defensive cut alt", "Blade", "ut\tMake a compact defensive cut while stepping backward. Mo.001"),
            new Entry("guard_impact", "Guard impact", "Blade", "Begin in raised guard. An incoming impact drives the right wris"),
            new Entry("fold_arms", "Fold arms", "Social", "fold_arms"),
            new Entry("elbow_followup", "Elbow followup", "Blade", "r\tFollow a close elbow with a short step forward and a downward"),
            new Entry("drag_body", "Drag body", "Stealth", "drag_body: Dhane faces forward while gripping an imagined weigh"),
            new Entry("standing_relax", "Standing relax", "Locomotion", "standing_relax"),
            new Entry("guard_hold", "Guard hold", "Blade", "Maintain the raised guard with elbows close, weight centered, a"),
            new Entry("low_strike", "Low strike", "Blade", "From a low stance, draw the right elbow back, drive a short str"),
            new Entry("turn_180_left", "Turn 180 left", "Traversal", "turn_180_left: From standing, Dhane looks left toward the turn,"),
            new Entry("turn_180_right", "Turn 180 right", "Traversal", "turn_180_right: From standing, Dhane looks right toward the tur"),
            new Entry("low_vault", "Low vault", "Traversal", "low_vault: Dhane approaches an imagined waist-high obstacle, pl"),
            new Entry("stagger_open", "Stagger open", "Reactions", "stagger_open: A strong body hit folds Dhane\u2019s torso and lower"),
            new Entry("throw_stone", "Throw stone", "Social", "throw_stone: Dhane holds a small imagined stone in his right ha"),
            new Entry("dive", "Dive", "Traversal", "dive"),
            new Entry("guard_raise", "Guard raise", "Blade", "From ready, bring the right forearm into a compact front guard "),
            new Entry("namaste", "Namaste", "Social", "namaste: From relaxed standing, Dhane brings both palms togethe"),
            new Entry("fall_prepare", "Fall prepare", "Traversal", "fall_prepare: Dhane transitions from the apex of a jump into de"),
            new Entry("get_up_back", "Get up back", "Reactions", "get_up_back: Dhane starts lying flat on his back, head facing u"),
            new Entry("walk", "Walk", "Locomotion", "walk"),
            new Entry("forward_thrust", "Forward thrust", "Blade", "st\tStep forward from blade-ready and extend one committed chest"),
            new Entry("ledge_hang", "Ledge hang", "Traversal", "ledge_hang: Dhane hangs from an imagined horizontal ledge with "),
            new Entry("cut_recovery", "Cut recovery", "Blade", "Begin with the right hand low across the body after a diagonal "),
            new Entry("knockdown_back", "Knockdown back", "Reactions", "knockdown_back: Dhane loses balance from a frontal impact, step"),
            new Entry("hit_right", "Hit right", "Reactions", "hit_right: Dhane reacts to a hit from his right side. Turn the "),
        };
        public static string Source(string key)
        {
            foreach (var entry in All) if (ClipAnimator.Key(entry.key) == ClipAnimator.Key(key)) return entry.source;
            return key;
        }
        public static Entry ForSource(string name)
        {
            foreach (var entry in All) if (entry.source == name) return entry;
            return null;
        }
    }
}
