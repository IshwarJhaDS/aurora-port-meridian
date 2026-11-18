// Conversation system (port of src/game/dialogue.js).
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace PhewaValley
{
    public class DialogueOption { public string id, text; public bool disabled; }
    public class Conversation { public Citizen agent; public string text; public List<DialogueOption> options = new List<DialogueOption>(); }

    public class Dialogue
    {
        readonly WorldData data; readonly Progress progress; readonly Missions missions; readonly Func<float> rain; readonly Action<string> navigate;
        public Conversation Current { get; private set; }
        public event Action<Conversation> Changed;

        public Dialogue(WorldData data, Progress progress, Missions missions, Func<float> rain, Action<string> navigate)
        { this.data = data; this.progress = progress; this.missions = missions; this.rain = rain; this.navigate = navigate; }

        LineData L => data.dialogue.lines;

        public void Open(Citizen agent)
        {
            if (agent == null) return;
            Close(); agent.talking = true;
            int count = progress.Conversations(agent.Id); progress.Talked(agent.Id);
            var g = data.dialogue.greetings; float m = progress.minutes;
            var greeting = count > 0 ? g.again : m < 720 ? g.morning : m < 1080 ? g.afternoon : g.evening;
            var line = !string.IsNullOrEmpty(agent.line) ? agent.line : agent.fleeing ? L.fleeing : L.idle;
            Current = new Conversation { agent = agent, text = $"{greeting} {line}" };
            missions.Event("talk", agent.Id, agent.place); Menu(); Changed?.Invoke(Current);
        }

        void Menu()
        {
            if (Current == null) return; var a = Current.agent; var options = new List<DialogueOption>();
            foreach (var id in data.JobsFor(a.Id))
            {
                var def = missions.Definition(id); var state = progress.Mission(id); if (def == null) continue;
                if (state?.status == "completed" && !def.repeatable) continue;
                options.Add(new DialogueOption { id = "mission:" + id, text = state?.status == "active" ? $"About “{def.title}”…" : $"{(def.category == "WORK" ? "Take a job" : "Help out")} · {def.title}", disabled = !missions.Unlocked(id) });
            }
            options.Add(new DialogueOption { id = "local", text = "Ke chha? What is happening around here?" });
            options.Add(new DialogueOption { id = "directions", text = "Point me toward somewhere useful." });
            options.Add(new DialogueOption { id = "goodbye", text = "Pheri bhetaula · see you around." });
            Current.options = options.GetRange(0, Math.Min(6, options.Count));
        }

        public void Choose(string id)
        {
            var c = Current; if (c == null) return; var option = c.options.Find(o => o.id == id); if (option == null || option.disabled) return;
            if (id == "goodbye") { Close(); return; }
            if (id.StartsWith("mission:"))
            {
                var mission = id.Substring(8); bool accepted = missions.Accept(mission);
                if (accepted) { var steps = missions.Steps(mission); var st = progress.Mission(mission); c.text = $"{missions.Definition(mission).description} {(st != null && st.step < steps.Length ? steps[st.step].text : L.thanks)}"; missions.Event("talk", c.agent.Id, c.agent.place); }
                else c.text = L.later;
            }
            else if (id == "local")
            {
                c.text = data.VoiceLine(c.agent.Id) ?? (rain() > .3f ? L.rain : $"It is {World.Clock(progress.minutes)}. {(progress.minutes > 1080 ? L.evening : L.day)}");
                missions.Event("talk", c.agent.Id, c.agent.place);
            }
            else if (id == "directions")
            {
                var p = data.Poi(string.IsNullOrEmpty(c.agent.place) ? "plaza" : c.agent.place) ?? data.Poi("plaza");
                navigate(p.id); c.text = $"{p.name} is marked on your map. {p.description}";
            }
            Menu(); Changed?.Invoke(c);
        }

        public void Say(string text)
        {
            if (Current == null || string.IsNullOrWhiteSpace(text)) return;
            var input = text.Trim().ToLowerInvariant(); var a = Current.agent;
            bool Has(string pattern) => Regex.IsMatch(input, $@"\b({pattern})\b");
            if (Has("jobs?|work|money|shifts?|missions?|help"))
            {
                string found = null; foreach (var id in data.JobsFor(a.Id)) if (missions.Unlocked(id)) { found = id; break; }
                Current.text = found != null ? $"I can help you with “{missions.Definition(found).title}”. Choose the job below and I will explain the next step." : L.jobsFallback;
            }
            else if (Has("weather|rain|storm|sun")) Current.text = rain() > .3f ? L.weatherRain : L.weatherClear;
            else if (Has("shipment|manifest|light|harbor|ghat|lake")) { Current.text = data.VoiceLine(a.Id) ?? L.askOthers; missions.Event("talk", a.Id, a.place); }
            else if (Has("where|direction|map|cafe|chiya|tea|clinic|stupa|temple")) { Choose("directions"); return; }
            else if (Has("bye|goodbye|bhetaula")) { Close(); return; }
            else if (Has("hi|hello|hey|namaste|namaskar|thanks|thank|dhanyabad|dhanyavad")) Current.text = L.welcome;
            else Current.text = $"{(string.IsNullOrEmpty(a.line) ? L.unknown : a.line)} {L.topics}";
            Changed?.Invoke(Current);
        }

        public void Close() { if (Current != null) Current.agent.talking = false; Current = null; Changed?.Invoke(null); }
    }
}
