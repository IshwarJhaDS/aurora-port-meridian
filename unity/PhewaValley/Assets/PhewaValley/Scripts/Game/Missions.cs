// Mission state machine (port of src/game/missions.js). Steps come from world.json.
using System;
using System.Linq;
using UnityEngine;

namespace PhewaValley
{
    public class Missions
    {
        readonly WorldData data; readonly Progress progress; readonly Action<string, bool> notify; public event Action Changed;
        public Missions(WorldData data, Progress progress, Action<string, bool> notify)
        {
            this.data = data; this.progress = progress; this.notify = notify;
            if (progress.Mission("arrival") == null) progress.missions.Add(new MissionState { id = "arrival" });
        }

        public MissionData Definition(string id) => data.Mission(id);
        public bool Unlocked(string id) => id != "shipment" || progress.reputation >= 4;
        public StepData[] Steps(string id)
        {
            var def = Definition(id); if (def == null) return Array.Empty<StepData>();
            int variant = Mathf.Clamp(progress.Mission(id)?.variant ?? 0, 0, def.variants.Length - 1);
            return def.variants[variant].steps;
        }

        public bool Accept(string id)
        {
            var def = Definition(id); var old = progress.Mission(id);
            if (def == null || !Unlocked(id)) { notify("Build more reputation in the tole first.", false); return false; }
            if (id == "cafe" && (progress.minutes < 420 || progress.minutes >= 1320)) { notify("Tea shop shifts run from 07:00 to 22:00.", false); return false; }
            if (old?.status == "active") { progress.tracked = id; Changed?.Invoke(); return true; }
            if (old?.status == "completed" && !def.repeatable) { notify("You have already completed this story.", false); return false; }
            progress.missions.Remove(old);
            progress.missions.Add(new MissionState { id = id, variant = id == "courier" ? progress.courierCycle % 3 : 0 });
            progress.tracked = id; notify($"Started · {def.title}", false); progress.Save(); Changed?.Invoke(); return true;
        }

        public (MissionState state, MissionData def, StepData step)? Tracked()
        {
            var state = progress.Mission(progress.tracked);
            if (state?.status != "active") state = progress.missions.FirstOrDefault(m => m.status == "active");
            if (state == null) return null;
            var def = Definition(state.id); if (def == null) return null;
            var steps = Steps(state.id);
            return (state, def, state.step < steps.Length ? steps[state.step] : null);
        }

        public Vector3? ObjectivePosition(Citizens citizens)
        {
            var t = Tracked(); if (t?.step == null) return null; var step = t.Value.step;
            if (step.kind == "talk" && citizens.Get(step.target) != null) return citizens.Get(step.target).transform.position;
            if (step.kind == "voices") { var next = step.targets.FirstOrDefault(x => !t.Value.state.flags.Contains(x)); var c = next == null ? null : citizens.Get(next); if (c) return c.transform.position; }
            if (step.hasPosition) return new Vector3(step.x, World.TerrainHeight(step.x, step.z), step.z);
            return data.PoiPosition(step.place);
        }

        public bool Event(string kind, string target, string place = "")
        {
            bool progressed = false;
            foreach (var state in progress.missions.ToList())
            {
                if (state.status != "active") continue;
                var steps = Steps(state.id); if (state.step >= steps.Length) continue; var step = steps[state.step];
                bool match = step.kind == kind && step.target == target;
                if (step.kind == "place") match = kind == "interact" && target == step.target || kind == "talk" && place == step.target;
                if (step.kind == "work") match = kind == "work" && target == step.target && place == "cafe";
                if (step.kind == "voices" && kind == "talk" && step.targets.Contains(target))
                {
                    if (!state.flags.Contains(target)) { state.flags.Add(target); notify($"Voices of the tole · {state.flags.Count}/3", false); progressed = true; }
                    match = step.targets.All(x => state.flags.Contains(x));
                }
                if (!match) continue;
                if (step.kind == "repair" && !progress.repaired.Contains(target)) progress.repaired.Add(target);
                state.step++; progressed = true;
                if (state.step >= steps.Length) Complete(state);
                else notify($"Objective updated · {steps[state.step].text}", false);
            }
            if (progressed) { progress.Save(); Changed?.Invoke(); }
            return progressed;
        }

        void Complete(MissionState state)
        {
            var def = Definition(state.id); state.status = "completed";
            int cycle = def.repeatable ? (state.id == "courier" ? progress.courierCycle++ : progress.jobCount++) : 0;
            float before = progress.cash; progress.Reward($"mission:{state.id}:{cycle}", def.reward, def.reputation);
            notify($"Completed · {def.title}   +Rs {Mathf.RoundToInt(progress.cash - before)}   +{def.reputation} reputation", true);
        }

        public void Update(float dt, Vector3 player, bool canAct)
        {
            foreach (var state in progress.missions.ToList())
            {
                if (state.status != "active") continue;
                var steps = Steps(state.id); if (state.step >= steps.Length) continue; var step = steps[state.step];
                if (step.kind == "reach" && World.DistanceXZ(player, data.PoiPosition(step.target)) < 8) Event("reach", step.target);
                if (state.id == "cafe" && canAct)
                {
                    state.remaining -= dt;
                    if (state.remaining <= 0) { progress.missions.Remove(state); notify("The tea shop shift ended. Talk to Laxman dai to try another shift.", false); progress.Save(); Changed?.Invoke(); }
                }
            }
        }

        public StepData RepairNear(Vector3 position)
        {
            var state = progress.Mission("repair"); if (state?.status != "active") return null;
            var steps = Steps("repair"); if (state.step >= steps.Length) return null; var step = steps[state.step];
            return step.kind == "repair" && World.DistanceXZ(position, step.x, step.z) < 2.8f ? step : null;
        }
    }
}
