// Run a development player with -phewa-verify. Isolated scene; never reads or writes a visit save.
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace PhewaValley
{
    public class ControllerChecks : MonoBehaviour
    {
        int failed, passed;
        readonly List<string> results = new List<string>();
        void Check(bool ok, string label)
        {
            if (ok) passed++; else failed++;
            string line = $"[verify] {(ok ? "PASS" : "FAIL")} {label}";
            results.Add(line); Debug.Log(line);
        }
        GameObject Box(string name, Vector3 at, Vector3 size)
        {
            var g = GameObject.CreatePrimitive(PrimitiveType.Cube); g.name = name;
            g.transform.position = at; g.transform.localScale = size; return g;
        }
        IEnumerator Start()
        {
            Application.targetFrameRate = 60; QualitySettings.vSyncCount = 0;
            var camera = new GameObject("Check camera").AddComponent<Camera>(); camera.tag = "MainCamera";
            camera.transform.position = new Vector3(0, 5, -10); camera.transform.rotation = Quaternion.identity;
            Box("Floor", new Vector3(0, -.5f, 0), new Vector3(120, 1, 120));
            var p = new GameObject("Test Kiran").AddComponent<PlayerController>(); p.Init(null);
            p.inputEnabled = false;
            float lowestJump = float.MaxValue, highestJump = 0;
            foreach (int fps in new[] { 15, 30, 60, 120, 144 })
            {
                Application.targetFrameRate = fps; p.Teleport(new Vector3(0, .05f, 0));
                yield return new WaitForSeconds(.3f);
                Check(p.body.isGrounded, $"{fps} fps settles on ground");
                float start = p.transform.position.y, apex = start;
                p.jumpNow = true;
                for (float t = 0; t < 1.2f; t += Time.deltaTime) { yield return null; apex = Mathf.Max(apex, p.transform.position.y); }
                Check(apex - start > .65f && apex - start < 1.1f, $"{fps} fps jump height {apex - start:0.000} m");
                lowestJump = Mathf.Min(lowestJump, apex - start); highestJump = Mathf.Max(highestJump, apex - start);
                Check(p.body.isGrounded && !p.figure.Airborne, $"{fps} fps lands and clears airborne");
            }
            Check(highestJump - lowestJump < .035f, "jump height varies by less than 3.5 cm across frame rates");
            Application.targetFrameRate = 60;
            var ceiling = Box("Low ceiling", new Vector3(0, 2.3f, 0), new Vector3(4, .3f, 4));
            p.Teleport(new Vector3(0, .05f, 0)); yield return new WaitForSeconds(.2f); p.jumpNow = true;
            yield return new WaitForSeconds(.7f);
            Check(p.body.isGrounded && p.transform.position.y < .15f, "head contact cancels ascent and returns to floor");
            Destroy(ceiling); yield return null;
            p.Teleport(new Vector3(0, 8, 0)); yield return new WaitForSeconds(.5f);
            p.Teleport(new Vector3(0, .05f, 0)); yield return new WaitForSeconds(.2f);
            Check(!p.Busy && p.body.isGrounded && p.Speed < .05f, "teleport clears fall momentum and knockdown");
            p.autoMove = 5; yield return new WaitForSeconds(.8f); p.diveNow = true;
            yield return new WaitForSeconds(.2f); Check(p.Busy, "dive enters action state");
            p.autoMove = 0; p.Teleport(new Vector3(0, .05f, 0)); yield return new WaitForSeconds(.2f);
            Check(!p.Busy && p.Speed < .05f, "teleport cancels dive lunge");
            var ledge = Box("Climb ledge", new Vector3(0, .5f, 1), new Vector3(3, 1, 1));
            p.Teleport(new Vector3(0, .05f, 0)); p.transform.rotation = Quaternion.identity;
            yield return new WaitForSeconds(.2f); p.jumpNow = true; yield return new WaitForSeconds(.15f);
            Check(p.Busy && !p.body.enabled, "ledge starts climb with capsule suspended");
            yield return new WaitForSeconds(1.2f);
            Check(!p.Busy && p.body.enabled && p.transform.position.y > .95f, "climb restores capsule on ledge");
            Destroy(ledge); yield return null;
            p.Teleport(new Vector3(8, .05f, 0)); p.transform.rotation = Quaternion.identity;
            var wall = Box("Solid wall", new Vector3(8, 1.5f, 2), new Vector3(4, 3, .3f));
            p.autoMove = 5; yield return new WaitForSeconds(1.2f);
            Check(p.transform.position.z < 1.65f && p.figure.Speed < .1f, "wall collision stops movement and locomotion animation");
            p.autoMove = 0; p.inputEnabled = true; var stopped = p.transform.position; Destroy(wall); yield return new WaitForSeconds(.12f);
            Check(Vector3.Distance(stopped, p.transform.position) < .04f && p.Speed < .1f, "blocked momentum does not launch Kiran when a wall clears");
            p.inputEnabled = false;
            p.SetFigure("resident_f", Color.green, 1.6f);
            Check(Mathf.Abs(p.body.height - 1.65f) < .01f && Mathf.Abs(p.body.center.y - .825f) < .01f, "protagonist capsule follows character height");
            p.enabled = false; p.Teleport(new Vector3(8, .05f, 0));
            p.SetCrouching(true);
            var crouchCeiling = Box("Crouch clearance", new Vector3(8, 1.45f, 0), new Vector3(4, .2f, 4));
            Physics.SyncTransforms();
            p.SetCrouching(false);
            Check(p.Crouching && p.body.height < 1.2f, "low ceiling prevents standing out of crouch");
            Destroy(crouchCeiling); yield return null; Physics.SyncTransforms();
            p.SetCrouching(false);
            Check(!p.Crouching && Mathf.Abs(p.body.height - 1.65f) < .01f, "clear headroom restores the standing capsule");
            p.enabled = true;
            var visibilityRoot = new GameObject("Camera visibility checks"); visibilityRoot.SetActive(false);
            var visibility = visibilityRoot.AddComponent<CameraRig>(); visibility.target = p.transform;
            var setVisible = typeof(CameraRig).GetMethod("SetBodyVisible", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            setVisible.Invoke(visibility, new object[] { false }); p.SetFigure("player", Color.gray, 1.75f); setVisible.Invoke(visibility, new object[] { false });
            bool hidden = true; foreach (var renderer in p.GetComponentsInChildren<Renderer>()) hidden &= !renderer.enabled;
            Check(hidden, "first-person camera also hides a newly swapped character body");
            setVisible.Invoke(visibility, new object[] { true }); Destroy(visibilityRoot);
            var stepDown = Box("Descending kerb", new Vector3(-10, .125f, 0), new Vector3(4, .25f, 2));
            p.Teleport(new Vector3(-10, .3f, 0)); p.transform.rotation = Quaternion.identity; yield return new WaitForSeconds(.3f);
            p.autoMove = 2; float airborneFor = 0, longestAir = 0;
            for (float t = 0; t < 1.5f; t += Time.deltaTime)
            { yield return null; airborneFor = p.body.isGrounded ? 0 : airborneFor + Time.deltaTime; longestAir = Mathf.Max(longestAir, airborneFor); }
            Check(longestAir < .08f && p.transform.position.y < .12f, $"descending a kerb keeps ground contact (air {longestAir:0.000} s)");
            p.autoMove = 0; Destroy(stepDown); p.Teleport(new Vector3(0, .05f, 0)); yield return new WaitForSeconds(.7f);
            p.Knock(Vector3.forward * 5); yield return new WaitForSeconds(.4f); p.Knock(Vector3.forward * 5);
            Check(p.Busy && p.body.enabled, "knockdown retains its capsule and resists a duplicate hit");
            yield return new WaitForSeconds(2.5f);
            Check(!p.Busy && p.body.isGrounded, "knockdown releases movement after the complete get-up sequence");
            p.Knock(Vector3.forward * 5);
            Check(!p.Busy, "brief recovery immunity prevents immediate repeated knockdowns");
            p.Teleport(new Vector3(20, .05f, 10));
            var car = new GameObject("Check car").AddComponent<CarController>();
            car.transform.position = new Vector3(20, .1f, 0); car.Init(null); yield return new WaitForSeconds(.3f);
            Check(car.TryExit(p, out _), "car finds supported clear exit");
            var left = Box("Blocked left door", new Vector3(17.9f, 1, 0), new Vector3(1, 2, 3));
            yield return new WaitForFixedUpdate();
            bool clearDoor = car.TryExit(p, out var exit);
            Check(clearDoor && exit.x > 20, $"blocked driver door uses passenger door (car {car.transform.position}, exit {exit})");
            var right = Box("Blocked right door", new Vector3(22.1f, 1, 0), new Vector3(1, 2, 3));
            yield return new WaitForFixedUpdate();
            bool bothDoors = car.TryExit(p, out var blockedExit);
            Check(!bothDoors, $"both obstructed doors refuse exit (exit {blockedExit})");
            Destroy(left); Destroy(right);
            car.Launch(20); car.Recover(new Vector3(-1000, 0, 0));
            Check(Mathf.Abs(car.speed) < .01f && car.transform.position.x >= -476, "recovery clears velocity inside valley boundaries");
            float shortestStop = float.MaxValue, longestStop = 0;
            foreach (int fps in new[] { 15, 60, 144 })
            {
                Application.targetFrameRate = fps;
                car.GetComponent<CharacterController>().enabled = false;
                car.transform.SetPositionAndRotation(new Vector3(20, .1f, -35), Quaternion.identity);
                car.GetComponent<CharacterController>().enabled = true;
                car.autoDrive = true; car.autoThrottle = -1; car.autoSteer = 0;
                car.Launch(10);
                float startZ = car.transform.position.z;
                for (float t = 0; t < 2 && car.speed > .2f; t += Time.deltaTime) yield return null;
                float distance = car.transform.position.z - startZ;
                Check(Mathf.Abs(car.speed) < 1 && distance > 3.5f && distance < 5.5f,
                    $"{fps} fps brakes from 10 m/s in {distance:0.00} m");
                shortestStop = Mathf.Min(shortestStop, distance); longestStop = Mathf.Max(longestStop, distance);
                car.autoDrive = false; car.Launch(0);
            }
            Check(longestStop - shortestStop < .2f, "braking distance varies by less than 20 cm across frame rates");
            Application.targetFrameRate = 60;
            var weather = new GameObject("Check weather").AddComponent<DayNight>(); weather.Init();
            Check(weather.GetComponentInChildren<ParticleSystem>() == null, "rain particle effects are removed");
            Check(weather.Rain == 0 && weather.Wetness == 0 && Shader.GetGlobalFloat("_PhewaWet") == 0, "rain and wet-road effects stay disabled");
            if (System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-phewa-skip-shader-check") >= 0)
            { results.Add("[verify] SKIP lake shader: existing-player validation cannot import new shaders"); Debug.Log(results[results.Count - 1]); }
            else Check(Shader.Find("PhewaValley/Lake") && Shader.Find("PhewaValley/Lake").isSupported, "lake shader included and supported");
            var schedule = DayNight.Schedule(2, 19 * 60);
            Check(schedule.on && schedule.until == "20:00" && !DayNight.Schedule(2, 20 * 60).on, "load shedding interval boundaries");
            var progress = JsonUtility.FromJson<Progress>("{\"version\":1,\"hero\":999,\"missions\":null,\"heroPositions\":null,\"graphics\":-10}");
            typeof(Progress).GetMethod("Sanitized", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(progress, null);
            Check(progress.hero == Protagonists.Roster.Count - 1 && progress.graphics == 0 && progress.missions != null && progress.heroPositions != null, "partial saves sanitize indices and missing lists");
            var data = WorldData.Load();
            var introVisit = new Progress { sessionOnly = true };
            var opening = new Missions(data, introVisit, (text, success) => { });
            var guide = new FirstVisitGuide(introVisit);
            Check(guide.Stage(false) == FirstStep.Move, "new visit starts with movement guidance");
            guide.Travel(1000, false); guide.Travel(float.NaN, false);
            Check(introVisit.introWalkDistance == 0, "teleports and invalid distances cannot advance guidance");
            guide.Travel(4, false); Check(guide.Stage(false) == FirstStep.MeetMaya, "walking advances to first conversation");
            opening.Event("talk", "mira"); Check(guide.Stage(false) == FirstStep.FindTeaShop, "Maya conversation starts the guided route");
            opening.Event("reach", "cafe"); Check(guide.Stage(false) == FirstStep.MeetLaxman, "tea-shop arrival teaches interaction");
            opening.Event("talk", "leo"); Check(guide.Stage(false) == FirstStep.ReturnToMaya, "Laxman conversation points back to Maya");
            opening.Event("talk", "mira"); Check(guide.Stage(false) == FirstStep.FindCar, "completed opening offers optional driving");
            Check(guide.Stage(true) == FirstStep.Drive, "boarding teaches vehicle controls");
            for (int i = 0; i < 4; i++) guide.Travel(10, true);
            Check(guide.Stage(true) == FirstStep.Park, "real driving distance teaches parking");
            var resumedIntro = JsonUtility.FromJson<Progress>(JsonUtility.ToJson(introVisit));
            Check(new FirstVisitGuide(resumedIntro).Stage(true) == FirstStep.Park, "guidance resumes its driving lesson after save reload");
            float beforeParking = introVisit.cash; guide.Parked(); guide.Parked();
            Check(guide.Stage(false) == FirstStep.Done && introVisit.cash == beforeParking, "parking completes guidance without duplicate story rewards");
            var veteran = new Progress { sessionOnly = true }; veteran.missions.Add(new MissionState { id = "arrival", status = "completed" });
            Check(new FirstVisitGuide(veteran).Stage(false) == FirstStep.Done, "existing completed visits skip the introduction");
            Check(FootstepAudio.SurfaceAt(Vector3.zero) == GroundSound.Road && FootstepAudio.SurfaceAt(new Vector3(30, 0, 30)) == GroundSound.Stone && FootstepAudio.SurfaceAt(new Vector3(35, 0, 350)) == GroundSound.Grass, "footsteps select road, paving and grass surfaces");
            foreach (GroundSound surface in System.Enum.GetValues(typeof(GroundSound)))
            {
                var clip = FootstepAudio.MakeClip(surface); var samples = new float[clip.samples]; clip.GetData(samples, 0);
                float peak = 0; bool valid = true;
                foreach (float sample in samples) { valid &= !float.IsNaN(sample) && !float.IsInfinity(sample) && Mathf.Abs(sample) <= 1; peak = Mathf.Max(peak, Mathf.Abs(sample)); }
                Check(valid && peak > .01f && Mathf.Abs(samples[samples.Length - 1]) < .01f, $"{surface} footstep audio is finite, bounded and fades out"); Destroy(clip);
            }
            var backdrop = new MeshBuilder(); ValleyBackdrop.Build(backdrop); var mountains = backdrop.Build("Backdrop check");
            bool geometryValid = true;
            foreach (var v in mountains.vertices) geometryValid &= !float.IsNaN(v.y) && v.y >= -3.01f && v.y < 1000 && new Vector2(v.x, v.z).magnitude > 800;
            Check(geometryValid && mountains.vertexCount < 25000 && mountains.triangles.Length > 20000, "layered mountain mesh is finite, outside gameplay and within geometry budget");
            Check(Mathf.Abs(ValleyBackdrop.Height(0, .5f, 1) - ValleyBackdrop.Height(Mathf.PI * 2, .5f, 1)) < .01f, "mountain ridges join around the angular seam"); Destroy(mountains);
            foreach (var def in data.missions)
            {
                var visit = new Progress { sessionOnly = true, reputation = 10, minutes = 600 };
                var missions = new Missions(data, visit, (text, success) => { });
                Check(missions.Accept(def.id), $"{def.id} accepts eligible visit");
                var steps = missions.Steps(def.id);
                foreach (var step in steps)
                {
                    if (step.kind == "voices") foreach (string who in step.targets) missions.Event("talk", who);
                    else missions.Event(step.kind == "place" ? "interact" : step.kind, step.target, step.kind == "work" ? "cafe" : "");
                }
                Check(visit.Mission(def.id).status == "completed", $"{def.id} completes every authored objective");
                float cash = visit.cash;
                foreach (var step in steps) missions.Event(step.kind, step.target);
                Check(visit.cash == cash, $"{def.id} duplicate events cannot pay twice");
                Check(missions.Accept(def.id) == def.repeatable, $"{def.id} replay policy");
            }
            var shiftVisit = new Progress { sessionOnly = true, minutes = 600 };
            var shift = new Missions(data, shiftVisit, (text, success) => { }); shift.Accept("cafe");
            shift.Update(151, Vector3.zero, true);
            Check(shiftVisit.Mission("cafe") == null, "expired tea-shop shift resets for retry");
            if ((data.audio?.music?.Length ?? 0) > 0)
            {
                var audioObject = new GameObject("Generated Aurora audio checks");
                var director = audioObject.AddComponent<AudioDirector>(); director.Init(data);
                for (float t = 0; t < 20; t += .1f)
                {
                    if (director.LoadedScores == data.audio.music.Length && director.LoadedEffects == data.audio.effects.Length &&
                        director.LoadedAmbience == data.audio.ambience.Length) break;
                    yield return new WaitForSeconds(.1f);
                }
                Check(director.LoadedScores == data.audio.music.Length && director.LoadedAmbience == data.audio.ambience.Length && director.LoadedEffects == data.audio.effects.Length,
                    "Aurora loads every generated score, ambience loop and effect");
                var audioClips = new HashSet<AudioClip>();
                foreach (var source in audioObject.GetComponents<AudioSource>()) if (source.clip) audioClips.Add(source.clip);
                foreach (var entry in data.audio.effects) { var effect = director.EffectClip(entry.id); if (effect) audioClips.Add(effect); }
                foreach (var clip in audioClips)
                {
                    var samples = new float[clip.samples * clip.channels];
                    bool valid = clip.GetData(samples, 0); float peak = 0;
                    foreach (float sample in samples)
                    {
                        valid &= !float.IsNaN(sample) && !float.IsInfinity(sample);
                        peak = Mathf.Max(peak, Mathf.Abs(sample));
                    }
                    Check(valid && peak > .001f && peak <= 1.001f && clip.length > .3f,
                        $"generated audio {clip.name} decodes to finite, audible samples");
                }
                Destroy(audioObject); yield return null;
            }
            yield return CharacterAnimationChecks.Run(transform, Check);
            Debug.Log($"[verify] RESULT passed={passed} failed={failed}");
            System.IO.File.WriteAllLines(System.IO.Path.Combine(Application.dataPath, "..", "controller-checks.txt"), results);
            Application.Quit(failed == 0 ? 0 : 1);
        }
    }
}
