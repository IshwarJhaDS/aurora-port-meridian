// Third-person on-foot controller, an arcade car, and an orbit camera that follows either.
using UnityEngine;

namespace PhewaValley
{
    /// <summary>
    /// On-foot controller with a body that has weight:
    ///  · momentum: speeding up, slowing down and turning take time (slower turns at a run);
    ///  · Space jumps, or vaults/climbs onto a ledge 0.45-2.2 m high in front of you (climb clip);
    ///  · Ctrl while moving: a forward dive-roll with a real lunge (dive clip);
    ///  · traffic that hits you knocks you down; you get up and complain at the driver (fall → get up → complain);
    ///  · a vehicle whizzing past makes you flinch, running into people staggers you, long falls land hard;
    ///  · steps and steep slopes switch to the stair run.
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        public CharacterController body; public Figure figure; public bool inputEnabled = true; public CarController car;
        FootstepAudio footsteps; float gravityDelta;
        float vertical; public float Speed { get; private set; }
        /// <summary>Smoke tests: walk straight ahead at this speed without input.</summary>
        public float autoMove;
        /// <summary>Smoke tests: jump (or climb) / dive on the next grounded frame.</summary>
        public bool jumpNow, diveNow; float airTimer;

        enum State { Normal, Climb, Dive, Knocked }
        State state; float stateTime, stateLength, fallSpeed, flinchCooldown, bumpCooldown, lastY;
        Vector3 planar, climbFrom, climbTo, push;
        float coyote, jumpBuffer;
        bool groundedBeforeMove; float knockImmunity;
        readonly RaycastHit[] groundHits = new RaycastHit[24];
        const float Gravity = 15, JumpSpeed = 5.2f;
        public bool Busy => state != State.Normal;

        // All relocation paths must discard momentum and action state, including disabled climb capsules.
        public void Teleport(Vector3 position)
        {
            body.enabled = false; transform.position = position; body.enabled = true;
            vertical = fallSpeed = airTimer = coyote = jumpBuffer = gravityDelta = 0;
            planar = push = Vector3.zero; Speed = 0; state = State.Normal;
            knockImmunity = .6f;
            jumpNow = diveNow = false; lastFlags = CollisionFlags.None; lastY = position.y;
            figure.ResetPose(); footsteps?.ResetStride();
        }

        public bool CanStand(Vector3 feet)
        {
            foreach (var c in Physics.OverlapCapsule(feet + Vector3.up * (body.radius + .04f),
                feet + Vector3.up * (body.height - body.radius), body.radius - .02f,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                if (!c.transform.IsChildOf(transform)) return false;
            return true;
        }

        ModelLibrary models;
        public void Init(ModelLibrary models, string model = "player", Color? outfit = null, float height = 1.75f)
        {
            this.models = models;
            body = gameObject.AddComponent<CharacterController>(); body.height = 1.8f; body.radius = .32f; body.center = new Vector3(0, .9f, 0); body.stepOffset = .32f; body.slopeLimit = 50; body.skinWidth = .035f; body.minMoveDistance = 0; body.enableOverlapRecovery = true;
            SetFigure(model, outfit ?? new Color(.17f, .23f, .26f), height);
            lastY = transform.position.y;
            footsteps = gameObject.AddComponent<FootstepAudio>();
        }
        /// <summary>Swaps the visible body (used when switching protagonists).</summary>
        public void SetFigure(string model, Color outfit, float height)
        {
            if (figure) { figure.gameObject.SetActive(false); Destroy(figure.gameObject); }
            body.height = Mathf.Max(1.2f, height + .05f); body.center = Vector3.up * (body.height * .5f);
            body.radius = Mathf.Min(.32f, height * .185f); body.stepOffset = Mathf.Min(.32f, body.height * .2f);
            figure = Figure.Create(transform, model, outfit, new Color(.55f, .35f, .22f), new Color(.02f, .02f, .02f), null, models, height);
        }

        public bool InCar => car != null && car.driver == this;

        void Update()
        {
            if (InCar) { Speed = 0; planar = Vector3.zero; state = State.Normal; return; }
            float dt = Time.deltaTime; if (dt <= 0 || !body.enabled && state != State.Climb) return;
            var G = Game.Instance; var cam = Camera.main ? Camera.main.transform : transform;
            if (G && (!G.Ready || G.Paused || G.heroes != null && G.heroes.Switching)) return;
            groundedBeforeMove = body.enabled && body.isGrounded && vertical <= 0;
            coyote = groundedBeforeMove ? .1f : Mathf.Max(0, coyote - dt);
            gravityDelta = 0;
            if (groundedBeforeMove) vertical = -2;
            else { float previous = vertical; vertical = Mathf.Max(-40, vertical - Gravity * dt); gravityDelta = previous - vertical; }
            fallSpeed = Mathf.Min(fallSpeed, vertical);
            flinchCooldown -= dt; bumpCooldown -= dt; knockImmunity = Mathf.Max(0, knockImmunity - dt);
            switch (state)
            {
                case State.Climb: UpdateClimb(dt); return;
                case State.Dive: UpdateDive(dt); break;
                case State.Knocked: UpdateKnocked(dt); break;
                default: UpdateNormal(dt, cam); break;
            }
            if (state == State.Climb) return;
            // Vertical: gravity, and how hard we land.
            bool grounded = body.isGrounded;
            if (grounded)
            {
                if (!groundedBeforeMove) footsteps?.Land(-fallSpeed);
                if (!groundedBeforeMove && fallSpeed < -11 && state == State.Normal) HardLanding();
                fallSpeed = 0; if (vertical < 0) vertical = -1;
            }
            else fallSpeed = Mathf.Min(fallSpeed, vertical);
            if ((lastFlags & CollisionFlags.Above) != 0 && vertical > 0) vertical = 0;
            airTimer = grounded ? 0 : airTimer + dt; figure.Airborne = vertical > 0 || airTimer > .12f;
            // Up steps or a steep slope: the stair run.
            float climbRate = (transform.position.y - lastY) / dt; lastY = transform.position.y;
            figure.Climbing = grounded && Speed > 1 && climbRate > .45f;
            // Traffic.
            if (G && G.streetLife && state == State.Normal && knockImmunity <= 0)
            {
                if (G.streetLife.Hits(transform.position, out var hitVel)) Knock(hitVel);
                else if (flinchCooldown <= 0 && G.streetLife.NearMiss(transform.position)) { flinchCooldown = 5; figure.Action("hit_to_side", 0, 1.25f, 1.3f, Act.WhileMoving); }
            }
            // Running into people.
            if (G && state == State.Normal && Speed > 3.5f && bumpCooldown <= 0 && G.citizens)
                foreach (var a in G.citizens.agents)
                    if (a && (a.transform.position - transform.position).sqrMagnitude < .8f * .8f)
                    { bumpCooldown = 3; planar *= .25f; figure.Action("hit_to_body_02", .15f, 1.5f, 1.4f, Act.WhileMoving | Act.PinXZ); break; }
            if (transform.position.y < -20) Teleport(new Vector3(34, 1, 29));
        }

        CollisionFlags lastFlags;
        void Move(float dt)
        {
            var before = transform.position;
            lastFlags = body.Move((planar + Vector3.up * (vertical + gravityDelta * .5f)) * dt);
            // Keep contact on small descending steps; never snap an upward jump or a real fall.
            if (state == State.Normal && vertical <= 0 && !body.isGrounded && (groundedBeforeMove || coyote > 0))
            {
                int count = Physics.RaycastNonAlloc(transform.position + Vector3.up * .1f, Vector3.down, groundHits, .42f,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                float nearest = float.MaxValue;
                for (int i = 0; i < count; i++)
                    if (!groundHits[i].transform.IsChildOf(transform) && groundHits[i].normal.y >= Mathf.Cos(body.slopeLimit * Mathf.Deg2Rad)) nearest = Mathf.Min(nearest, groundHits[i].distance);
                if (nearest < float.MaxValue) lastFlags |= body.Move(Vector3.down * (nearest + body.skinWidth));
            }
            var actual = Vector3.ProjectOnPlane(transform.position - before, Vector3.up) / dt;
            // Animate the distance actually travelled, so pushing a wall does not run in place.
            Speed = actual.magnitude;
            figure.Speed = state == State.Normal ? Mathf.MoveTowards(figure.Speed, Speed, 18 * dt) : 0;
            footsteps?.Travel(actual.magnitude * dt, Speed, body.isGrounded && vertical <= 0 && state == State.Normal);
        }

        void UpdateNormal(float dt, Transform cam)
        {
            Vector2 input = inputEnabled ? new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")) : Vector2.zero;
            var forward = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < .01f) forward = transform.forward; var right = Vector3.Cross(Vector3.up, forward);
            var move = Vector3.ClampMagnitude(forward * input.y + right * input.x, 1);
            // Walk by default, Shift to run, C to stroll. Speeds sit inside the walk/run clips' natural
            // range (Tripo walk ~1.2 m/s, run ~4.2 m/s) so the feet match the ground.
            float speed = (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) ? 5.8f : Input.GetKey(KeyCode.C) ? 1.2f : 1.8f;
            if (autoMove > 0) { move = transform.forward; speed = autoMove; } // smoke tests only
            if (figure.Busy && !figure.Airborne) move *= .3f; // staggering or flinching: you cannot just walk on
            // Momentum: a body takes time to get going and to stop; running turns are wider.
            var want = move * speed;
            float rate = want.sqrMagnitude > planar.sqrMagnitude ? 9.5f : 14f; if (!body.isGrounded) rate *= .35f;
            planar = !inputEnabled && autoMove <= 0 ? Vector3.zero : Vector3.MoveTowards(planar, want, rate * dt);
            Speed = planar.magnitude;
            if (move.sqrMagnitude > .01f) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(move), 1 - Mathf.Exp(-dt * Mathf.Lerp(16, 9, Mathf.Clamp01((Speed - 2) / 4))));
            bool jumpKey = inputEnabled && Input.GetKeyDown(KeyCode.Space) || jumpNow; jumpNow = false;
            bool diveKey = inputEnabled && (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl)) || diveNow; diveNow = false;
            jumpBuffer = jumpKey ? .12f : Mathf.Max(0, jumpBuffer - dt);
            if (!inputEnabled && !jumpKey) jumpBuffer = 0;
            if (coyote > 0 && jumpBuffer > 0)
            {
                jumpBuffer = coyote = 0;
                if (groundedBeforeMove && FindLedge(out var top)) { StartClimb(top); return; }
                vertical = JumpSpeed - Gravity * dt; gravityDelta = Gravity * dt; figure.Jump(2 * JumpSpeed / Gravity);
            }
            // Releasing Space early makes a short hop; holding it keeps the full arc.
            if (inputEnabled && vertical > 0 && Input.GetKeyUp(KeyCode.Space)) vertical *= .55f;
            if (groundedBeforeMove && vertical <= 0 && diveKey && (Speed > 1.5f || autoMove > 0)) StartDive();
            Move(dt);
        }

        // ───────── Climbing / vaulting ─────────
        void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (Mathf.Abs(hit.normal.y) > .2f) return;
            float into = Vector3.Dot(planar, hit.normal);
            if (into < 0) planar -= hit.normal * into;
        }

        bool FindLedge(out Vector3 top)
        {
            top = default; var fwd = transform.forward; var feet = transform.position;
            if (!Physics.Raycast(feet + Vector3.up * .55f, fwd, out var wall, 1.1f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
            if (Mathf.Abs(wall.normal.y) > .5f || wall.collider == body) return false;
            var probe = wall.point + fwd * .4f; probe.y = feet.y + 2.5f;
            if (!Physics.Raycast(probe, Vector3.down, out var hit, 2.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) return false;
            float h = hit.point.y - feet.y; if (h < .45f || h > 2.2f || hit.normal.y < .7f) return false;
            top = hit.point + Vector3.up * .04f;
            return CanStand(top) && CanStand(new Vector3(feet.x, top.y, feet.z)); // validate the actual destination, without adding an unchecked forward offset
        }

        void StartClimb(Vector3 top)
        {
            float h = top.y - transform.position.y; Debug.Log($"[player] climbing a {h:0.00} m ledge");
            state = State.Climb; stateTime = 0; stateLength = Mathf.Lerp(.55f, 1.05f, Mathf.InverseLerp(.45f, 2.2f, h));
            climbFrom = transform.position; climbTo = top; body.enabled = false; planar = Vector3.zero; Speed = 0;
            // The climb clip's spring and knee tuck, timed to the climb; its own lift and travel are removed.
            figure.Action("climb", 1.3f, 2.25f, .95f / stateLength, Act.PinXZ | Act.CancelLift | Act.WhileMoving);
        }

        void UpdateClimb(float dt)
        {
            stateTime += dt; float k = Mathf.Clamp01(stateTime / stateLength);
            // Up first, then over the edge.
            float up = Mathf.SmoothStep(0, 1, Mathf.Clamp01(k / .65f)), over = Mathf.SmoothStep(0, 1, Mathf.Clamp01((k - .45f) / .55f));
            var p = Vector3.Lerp(climbFrom, new Vector3(climbTo.x, climbFrom.y, climbTo.z), over); p.y = Mathf.Lerp(climbFrom.y, climbTo.y + .02f, up);
            transform.position = p; figure.Speed = 0; figure.Airborne = true;
            if (k >= 1)
            {
                // Streaming or a moving prop can change the destination during the animation.
                Teleport(CanStand(climbTo) ? climbTo : climbFrom);
            }
        }

        // ───────── Dive-roll ─────────
        void StartDive()
        {
            state = State.Dive; stateTime = 0; stateLength = 2.0f;
            var dir = planar.sqrMagnitude > .1f ? planar.normalized : transform.forward; transform.rotation = Quaternion.LookRotation(dir);
            push = dir * Mathf.Max(6.2f, planar.magnitude + 1.5f);
            figure.Action("dive", .25f, 2.71f, 1.25f, Act.PinXZ | Act.WhileMoving | Act.NoPlant);
        }

        void UpdateDive(float dt)
        {
            stateTime += dt;
            // The lunge carries you ~4 m, the roll bleeds the speed away, then you are back on your feet.
            float k = stateTime < .75f ? 1 : Mathf.Clamp01(1 - (stateTime - .75f) / .8f);
            planar = push * k; Speed = planar.magnitude; figure.Speed = 0;
            Move(dt);
            if (stateTime >= stateLength) state = State.Normal;
        }

        // ───────── Knock-downs and hard landings ─────────
        /// <summary>Hit by a vehicle: thrown along its path, down on the road, up again, and an earful for the driver.</summary>
        public void Knock(Vector3 hitVelocity)
        {
            if (InCar || state == State.Knocked || knockImmunity > 0) return;
            if (state == State.Climb) Teleport(climbFrom);
            body.enabled = true;
            state = State.Knocked; stateTime = 0; stateLength = (2.6f - .3f) / 1.4f + (2.71f - 1.75f) / .9f;
            var away = hitVelocity; away.y = 0; push = away.normalized * Mathf.Clamp(hitVelocity.magnitude * .6f, 3, 8); vertical = 2.5f;
            if (away.sqrMagnitude > .01f) transform.rotation = Quaternion.LookRotation(-away.normalized);
            figure.Action("fall", .3f, 2.6f, 1.4f, Act.PinXZ | Act.WhileMoving | Act.NoPlant);
            figure.Then("dive", 1.75f, 2.71f, .9f, Act.PinXZ | Act.WhileMoving | Act.NoPlant);       // the end of the roll: getting back up
            figure.Then("complain_02", .4f, 3.2f, 1.1f, Act.None);                                  // shaking a fist at the driver
            Game.Instance?.Notify("Oi! Herera chalau, dai! (Watch where you drive!)", false);
        }

        void UpdateKnocked(float dt)
        {
            stateTime += dt; push = Vector3.MoveTowards(push, Vector3.zero, 7 * dt); planar = push; Speed = 0; figure.Speed = 0;
            Move(dt);
            if (stateTime >= stateLength && body.isGrounded) { state = State.Normal; knockImmunity = 1.25f; } // up on the feet; the complaint plays on until you move
        }

        void HardLanding()
        {
            if (fallSpeed < -16) { Knock(transform.forward * 2); return; }               // a real fall: down you go
            planar *= .2f; figure.Action("hit_to_body_02", .5f, 1.5f, 1.6f, Act.WhileMoving | Act.PinXZ); // knees buckle
        }
    }

    /// <summary>
    /// The Maruti. Handling built the way GTA V's developers describe vehicles: weight you can feel.
    ///  · Four wheel raycasts put the body on the real road, tilted to slopes, kerbs and the ghat road.
    ///  · A spring-damper suspension: the nose dives under braking, squats under power, the body rolls in corners.
    ///  · Tyres: velocity is separate from heading, so the car carries momentum and slides when grip runs out;
    ///    the handbrake locks the rear and swings it into a drift; monsoon rain cuts grip.
    ///  · Bicycle-model steering (tight when slow, calm when fast), an engine torque curve with gear-change dips,
    ///    hills that slow you down going up, and impacts that bounce the car back and jolt the camera.
    /// </summary>
    public class CarController : MonoBehaviour
    {
        public PlayerController driver; public float speed; CharacterController body; float vertical; Transform visual; Transform[] wheels;
        public bool Braking { get; private set; }
        /// <summary>Camera jolt after an impact (0..1, decays).</summary>
        public float Shake { get; private set; }
        /// <summary>Sideways slide in m/s (drifting).</summary>
        public float Slip { get; private set; }
        public int Gear { get; private set; } = 1;
        /// <summary>Smoke tests: drive with fixed throttle and steering.</summary>
        public bool autoDrive; public float autoThrottle, autoSteer;

        Vector3 velocity; float yawRate, steerAngle, lastForward, gearDip, slopeSin, spin;
        float pitch, pitchVel, roll, rollVel, visY, visVY; bool snapVisual = true;
        readonly Vector3[] wheelLocal = new Vector3[4]; float halfLength = 1.8f;
        static readonly RaycastHit[] hits = new RaycastHit[8];
        const float Wheelbase = 2.35f, TopSpeed = 24f;

        public void Init(ModelLibrary models)
        {
            // Capsule bottom at the car's origin, so the origin sits on the road (the old 1.4 m capsule was
            // stretched to 1.9 m by Unity and left the car floating about 23 cm up).
            body = gameObject.AddComponent<CharacterController>(); body.radius = .9f; body.height = 1.9f; body.center = new Vector3(0, .95f, 0); body.stepOffset = .35f; body.skinWidth = .04f; body.slopeLimit = 40;
            var model = models?.Instantiate("car", transform, transform.position, 0, 1.5f);
            if (model && model.GetComponentsInChildren<Renderer>().Length > 0)
            {
                model.transform.localPosition = Vector3.zero; model.transform.localRotation = Quaternion.identity; visual = model.transform;
                // Wheel contact points from the model's footprint.
                var rs = model.GetComponentsInChildren<Renderer>(); var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
                var ext = transform.InverseTransformVector(b.extents); float ex = Mathf.Abs(ext.x), ez = Mathf.Abs(ext.z);
                if (ex > ez) (ex, ez) = (ez, ex);
                SetWheels(ex * .8f, ez * .68f); halfLength = ez;
                return;
            }
            // Built-in white Maruti-style hatchback.
            var g = new MeshBuilder(); var white = new Color(.82f, .82f, .8f); var glass = new Color(.05f, .09f, .11f); var dark = new Color(.03f, .03f, .03f);
            g.Box(0, .62f, 0, .78f, .3f, 1.75f, white, new Surface(.3f, .2f)); g.Box(0, 1.12f, -.15f, .72f, .26f, 1.05f, white, new Surface(.3f, .2f));
            g.Box(0, 1.12f, .92f, .66f, .2f, .02f, glass, new Surface(.1f, .5f)); g.Box(0, 1.12f, -1.22f, .66f, .2f, .02f, glass, new Surface(.1f, .5f));
            g.Box(.73f, 1.12f, -.15f, .01f, .2f, .9f, glass, new Surface(.1f, .5f)); g.Box(-.73f, 1.12f, -.15f, .01f, .2f, .9f, glass, new Surface(.1f, .5f));
            g.Box(0, .55f, 1.76f, .3f, .08f, .01f, new Color(.7f, .05f, .03f)); // red Nepali number plate
            foreach (var s in new[] { -.5f, .5f }) { g.Box(s, .72f, 1.76f, .14f, .07f, .01f, new Color(1, .95f, .8f), new Surface(.3f, 0, -2.5f)); g.Box(s, .72f, -1.76f, .14f, .07f, .01f, new Color(.7f, .05f, .03f), new Surface(.3f, 0, -1.5f)); }
            var body3d = new GameObject("Body"); body3d.transform.SetParent(transform, false);
            body3d.AddComponent<MeshFilter>().sharedMesh = g.Build("Car"); body3d.AddComponent<MeshRenderer>().sharedMaterial = Materials.VertexLit; visual = body3d.transform;
            SetWheels(.72f, 1.1f); halfLength = 1.78f;
            wheels = new Transform[4];
            for (int i = 0; i < 4; i++)
            {
                var w = new MeshBuilder(); w.Cylinder(0, 0, 0, .3f, .1f, dark, new Surface(.9f), 10);
                var go = new GameObject("Wheel"); go.transform.SetParent(visual, false); go.transform.localPosition = wheelLocal[i] + Vector3.up * .3f;
                go.AddComponent<MeshFilter>().sharedMesh = w.Build("Wheel"); go.AddComponent<MeshRenderer>().sharedMaterial = Materials.VertexLit; wheels[i] = go.transform;
            }
        }

        // Front-left, front-right, rear-left, rear-right.
        void SetWheels(float x, float z) { wheelLocal[0] = new Vector3(-x, 0, z); wheelLocal[1] = new Vector3(x, 0, z); wheelLocal[2] = new Vector3(-x, 0, -z); wheelLocal[3] = new Vector3(x, 0, -z); }

        /// <summary>Sets the car moving forward (used by tests and scripted starts).</summary>
        public void Launch(float forwardSpeed) { velocity = transform.forward * forwardSpeed; speed = forwardSpeed; lastForward = forwardSpeed; }

        bool Ignore(Collider c) => c.transform.IsChildOf(transform) || driver != null && c.transform.IsChildOf(driver.transform) || c.isTrigger;

        void Update()
        {
            float dt = Mathf.Min(Time.deltaTime, .05f); if (dt <= 0) return;
            if (Game.Instance && (!Game.Instance.Ready || Game.Instance.Paused)) return;
            float throttle = 0, steer = 0; bool handbrake = false, brake = false;
            if (driver != null && driver.inputEnabled) { throttle = Input.GetAxisRaw("Vertical"); steer = Input.GetAxisRaw("Horizontal"); handbrake = Input.GetKey(KeyCode.Space); brake = Input.GetKey(KeyCode.B); }
            if (autoDrive) { throttle = autoThrottle; steer = autoSteer; } // smoke tests only
            float wet = Game.Instance && Game.Instance.dayNight ? Game.Instance.dayNight.Wetness : 0, grip = 1 - wet * .4f;

            // Steering: a bicycle model. Full lock at walking pace, only a few degrees at speed.
            float f0 = Vector3.Dot(velocity, transform.forward);
            float maxSteer = Mathf.Lerp(34, 7, Mathf.Clamp01(Mathf.Abs(f0) / TopSpeed));
            steerAngle = Mathf.MoveTowards(steerAngle, steer * maxSteer, 150 * dt);
            float wantYaw = f0 / Wheelbase * Mathf.Tan(steerAngle * Mathf.Deg2Rad);
            if (handbrake && Mathf.Abs(f0) > 4) wantYaw *= 1.7f;                                    // the rear steps out
            float maxYaw = 8.5f * grip / Mathf.Max(2.5f, Mathf.Abs(f0)) * (handbrake ? 2 : 1);       // tyres can only give ~0.9 g sideways
            yawRate = Mathf.Lerp(yawRate, Mathf.Clamp(wantYaw, -maxYaw, maxYaw), 1 - Mathf.Exp(-dt * 7));
            transform.Rotate(0, yawRate * Mathf.Rad2Deg * dt, 0);

            // The old velocity, seen from the new heading: what is now sideways is a slide the tyres must catch.
            var fwd = transform.forward; var right = transform.right;
            float f = Vector3.Dot(velocity, fwd), l = Vector3.Dot(velocity, right);

            // Engine: strong at low revs, fading toward top speed, with a short torque cut at each upshift.
            if (throttle > 0 && f > -.5f)
            {
                int gear = f < 5 ? 1 : f < 10 ? 2 : f < 15 ? 3 : f < 20 ? 4 : 5;
                if (gear > Gear) gearDip = .2f; Gear = gear;
                float torque = 7.4f * Mathf.Pow(Mathf.Clamp01(1 - f / TopSpeed), .55f) * throttle * (gearDip > 0 ? .15f : 1);
                f += torque * dt;
            }
            else if (throttle > 0) f = Mathf.MoveTowards(f, 0, 11 * grip * dt);                    // braking out of reverse
            else if (throttle < 0) f = f > .5f ? Mathf.MoveTowards(f, 0, 11 * grip * dt) : Mathf.MoveTowards(f, -6, 4 * dt);
            else f = Mathf.MoveTowards(f, 0, (1.1f + Mathf.Abs(f) * .035f) * dt);                    // engine braking, rolling and air drag
            if (throttle <= 0) Gear = Mathf.Max(1, Mathf.Min(Gear, f < 5 ? 1 : f < 10 ? 2 : f < 15 ? 3 : f < 20 ? 4 : 5));
            gearDip -= dt;
            if (brake) f = Mathf.MoveTowards(f, 0, 12 * grip * dt);
            if (handbrake) f = Mathf.MoveTowards(f, 0, 5 * grip * dt);
            f -= 9.81f * slopeSin * .85f * dt;                                                       // hills
            // Tyres pull the slide back in line; a locked rear (handbrake) or a wet road lets it go.
            l = Mathf.MoveTowards(l, 0, (handbrake ? 2.2f : 15) * grip * dt);
            velocity = fwd * f + right * l; speed = f; Slip = Mathf.Abs(l);

            // The capsule is shorter than the car: look ahead from the bumper so the nose does not sink into walls.
            var travel = velocity * dt; float reach = halfLength - body.radius + travel.magnitude;
            if (travel.sqrMagnitude > 1e-6f)
            {
                int n = Physics.SphereCastNonAlloc(transform.position + Vector3.up * .8f, .55f, travel.normalized, hits, reach, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < n; i++) if (!Ignore(hits[i].collider) && hits[i].distance > 0 && hits[i].normal.y < .6f) { Impact(); travel = Vector3.zero; break; } // walls, not road surface
            }
            if (body.isGrounded) vertical = -1; vertical -= 15 * dt;
            var flags = body.Move(travel + Vector3.up * vertical * dt);
            if ((flags & CollisionFlags.Sides) != 0) Impact();
            Shake = Mathf.MoveTowards(Shake, 0, dt * 2.5f);
            Braking = brake || handbrake || throttle < 0 && f > .5f;
            if (driver != null) { driver.transform.position = transform.position; driver.transform.rotation = transform.rotation; }
        }

        void Impact()
        {
            float f = Vector3.Dot(velocity, transform.forward);
            if (Mathf.Abs(f) > 3) { Shake = Mathf.Clamp01(Mathf.Abs(f) / 14); pitchVel += -Mathf.Sign(f) * Mathf.Abs(f) * 6; velocity = -velocity * .22f; }
            else velocity *= .3f;
        }

        /// <summary>Puts the body on the real road and runs the suspension (after the car has moved).</summary>
        void LateUpdate()
        {
            if (!visual) return; float dt = Mathf.Min(Time.deltaTime, .025f); if (dt <= 0) return;
            var up = Vector3.up; var c = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                var wp = transform.TransformPoint(wheelLocal[i]); c[i] = new Vector3(wp.x, transform.position.y, wp.z);
                int n = Physics.RaycastNonAlloc(wp + up * 1.2f, Vector3.down, hits, 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
                float best = float.MaxValue;
                for (int k = 0; k < n; k++) if (!Ignore(hits[k].collider) && hits[k].distance < best) { best = hits[k].distance; c[i].y = hits[k].point.y; }
            }
            var gf = ((c[0] + c[1]) - (c[2] + c[3])) * .5f; var gr = ((c[1] + c[3]) - (c[0] + c[2])) * .5f;
            var normal = Vector3.Cross(gf, gr).normalized; if (normal.y < .5f) normal = up;
            slopeSin = gf.normalized.y;
            float groundY = (c[0].y + c[1].y + c[2].y + c[3].y) * .25f;

            // Weight transfer: longitudinal and lateral acceleration drive a sprung body pitch and roll.
            float f = speed, longAcc = (f - lastForward) / dt; lastForward = f; float latAcc = f * yawRate;
            float pitchTarget = Mathf.Clamp(-longAcc * .45f, -4, 4), rollTarget = Mathf.Clamp(latAcc * .5f, -5, 5);
            pitchVel += ((pitchTarget - pitch) * 70 - pitchVel * 10) * dt; pitch += pitchVel * dt; pitch = Mathf.Clamp(pitch, -7, 7);
            rollVel += ((rollTarget - roll) * 60 - rollVel * 9) * dt; roll += rollVel * dt; roll = Mathf.Clamp(roll, -7, 7);
            // Heave: the body follows bumps through a spring rather than snapping to them.
            if (snapVisual) { visY = groundY; visVY = 0; snapVisual = false; }
            visVY += ((groundY - visY) * 160 - visVY * 18) * dt; visY += visVY * dt;
            if (Mathf.Abs(visY - groundY) > .5f) visY = groundY;

            var forward = Vector3.ProjectOnPlane(transform.forward, normal).normalized;
            visual.SetPositionAndRotation(new Vector3(transform.position.x, visY - (Mathf.Abs(pitch) + Mathf.Abs(roll)) * .004f, transform.position.z),
                Quaternion.LookRotation(forward, normal) * Quaternion.Euler(pitch, 0, roll));

            if (wheels != null)
            {
                spin += speed * Time.deltaTime / .3f * Mathf.Rad2Deg;
                for (int i = 0; i < 4; i++) wheels[i].localRotation = Quaternion.Euler(0, i < 2 ? steerAngle : 0, 0) * Quaternion.Euler(spin, 0, 0) * Quaternion.Euler(0, 0, 90);
            }
        }

        public bool TryExit(PlayerController player, out Vector3 exit)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                var at = transform.position + transform.right * (side * 2.1f);
                if (!Physics.Raycast(at + Vector3.up * 2, Vector3.down, out var ground, 4,
                    Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore) || ground.normal.y < .65f) continue;
                if (Mathf.Abs(ground.point.y - transform.position.y) > .65f) continue;
                at.y = ground.point.y + .05f;
                if (player.CanStand(at)) { exit = at; return true; }
            }
            exit = default; return false;
        }

        public void Recover(Vector3 near)
        {
            float x = Mathf.Clamp(Mathf.Round(near.x / 80) * 80 + 3.1f, -476, 403), z = Mathf.Clamp(near.z + 8, -460, 540);
            body.enabled = false; transform.SetPositionAndRotation(new Vector3(x, World.TerrainHeight(x, z) + .1f, z), Quaternion.identity); body.enabled = true;
            speed = vertical = Slip = steerAngle = gearDip = lastForward = 0; Gear = 1; Braking = false; velocity = Vector3.zero; yawRate = 0; pitch = roll = pitchVel = rollVel = 0; snapVisual = true;
        }
    }

    /// <summary>
    /// Third-person camera in the style of modern open-world games: several distances
    /// on foot (over-the-shoulder, medium, far, first person) and in vehicles (chase,
    /// far chase, bonnet), free mouse look, auto-recentre behind the vehicle,
    /// speed-based field of view and smoothed collision with walls.
    /// </summary>
    public class CameraRig : MonoBehaviour
    {
        public Transform target; public float yaw = 200, pitch = 12, distance = 6; public bool photo;
        public int footMode = 0, carMode = 0; float lookIdle, smoothDistance = 4, fov = 60; Camera cam; Vector3 velocity;
        public bool InVehicle => Game.Instance && Game.Instance.player && Game.Instance.player.InCar;
        public bool FirstPerson => !photo && (InVehicle ? carMode == 2 : footMode == 3);
        public string ModeName => InVehicle ? new[] { "Chase cam", "Far chase", "Bonnet cam" }[carMode] : new[] { "Over the shoulder", "Medium", "Far", "First person", "Kurosawa · telephoto" }[footMode];
        // (distance, shoulder offset, height, field of view)
        static readonly (float d, float side, float h, float fov)[] Foot = { (3.1f, .6f, 1.55f, 54), (5.2f, .35f, 1.6f, 58), (8.5f, 0, 1.8f, 62), (0, 0, 1.62f, 72), (20f, 0, 1.3f, 18) };
        const int KurosawaMode = 4;
        public bool Kurosawa => !photo && !InVehicle && footMode == KurosawaMode;
        bool inTwoShot;
        readonly RaycastHit[] cameraHits = new RaycastHit[64];
        bool Obstruction(Vector3 origin, float radius, Vector3 direction, float reach, out float distance)
        {
            distance = reach; bool found = false;
            int n = Physics.SphereCastNonAlloc(origin, radius, direction.normalized, cameraHits, reach,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            for (int i = 0; i < n; i++)
            {
                var h = cameraHits[i];
                if (h.transform.IsChildOf(target) || Game.Instance && Game.Instance.player && h.transform.IsChildOf(Game.Instance.player.transform)) continue;
                if (h.distance < distance) { distance = h.distance; found = true; }
            }
            return found;
        }
        static readonly (float d, float side, float h, float fov)[] Car = { (6.5f, 0, 1.9f, 60), (11f, 0, 2.8f, 62), (0, 0, 1.25f, 70) };

        void Awake()
        {
            cam = gameObject.AddComponent<Camera>(); cam.nearClipPlane = .08f; cam.farClipPlane = 2600; cam.fieldOfView = 60; gameObject.tag = "MainCamera";
            gameObject.AddComponent<AudioListener>();
        }

        public void SetTarget(Transform next)
        {
            SetBodyVisible(true); target = next; bodyHidden = false; velocity = Vector3.zero;
            smoothDistance = 0;
        }

        public void CycleMode() { if (InVehicle) carMode = (carMode + 1) % Car.Length; else footMode = (footMode + 1) % Foot.Length; }

        void LateUpdate()
        {
            if (!target) return;
            var G = Game.Instance; bool blocked = G && G.ui && G.ui.BlocksCamera;
            // Free mouse look while playing; the cursor is released for menus, dialogue and minigames.
            Cursor.lockState = blocked || !Application.isFocused ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = blocked || !Application.isFocused;
            float dt = Time.deltaTime;
            if (G && G.InDialogue && G.dialogue.Current.agent && !InVehicle && !photo) { TwoShot(G.player.transform.position, G.dialogue.Current.agent.transform.position, dt); return; }
            inTwoShot = false;
            float mx = blocked ? 0 : Input.GetAxis("Mouse X"), my = blocked ? 0 : Input.GetAxis("Mouse Y");
            yaw += mx * 2.6f; pitch = Mathf.Clamp(pitch - my * 2.1f, FirstPerson ? -70 : -25, 75);
            if (Kurosawa) { KurosawaShot(mx, my, dt); return; }
            if (photo) distance = Mathf.Clamp(distance * Mathf.Exp(-Input.mouseScrollDelta.y * .1f), 2.5f, 60);
            var spec = photo ? (distance, 0f, 1.6f, 55f) : InVehicle ? Car[carMode] : Foot[footMode];
            // In a vehicle the camera drifts back behind the bonnet when you stop steering it.
            float speed = 0;
            if (InVehicle)
            {
                var car = G.car; speed = Mathf.Abs(car.speed);
                lookIdle = Mathf.Abs(mx) + Mathf.Abs(my) > .02f ? 0 : lookIdle + dt;
                if (lookIdle > 1.2f && speed > 2) { yaw = Mathf.LerpAngle(yaw, car.transform.eulerAngles.y + (car.speed < -1 ? 180 : 0), dt * 2.2f); pitch = Mathf.Lerp(pitch, 10, dt); }
            }
            else if (G && G.player) speed = G.player.Speed;
            // Faster means wider: sprinting and driving open the field of view a little.
            float targetFov = spec.Item4 + Mathf.Clamp01(speed / 22) * (InVehicle ? 14 : 6);
            fov = Mathf.Lerp(fov, targetFov, dt * 3); cam.fieldOfView = fov;
            var rot = Quaternion.Euler(pitch, yaw, 0);
            var focus = target.position + Vector3.up * spec.Item3;
            if (FirstPerson)
            {
                var head = InVehicle ? target.TransformPoint(new Vector3(-.3f, spec.Item3, .15f)) : target.position + Vector3.up * spec.Item3 + Quaternion.Euler(0, yaw, 0) * Vector3.forward * .18f;
                transform.SetPositionAndRotation(head, rot); SetBodyVisible(false); return;
            }
            SetBodyVisible(true);
            var shoulder = rot * Vector3.right * spec.Item2; var origin = focus + shoulder;
            float want = spec.Item1;
            if (Obstruction(focus, .22f, origin - rot * Vector3.forward * want - focus, (origin - rot * Vector3.forward * want - focus).magnitude, out float obstruction))
                want = Mathf.Max(.15f, obstruction - .25f);
            smoothDistance = want < smoothDistance ? want : Mathf.Lerp(smoothDistance, want, dt * 4); // snap in, ease out
            var desired = origin - rot * Vector3.forward * smoothDistance;
            desired.y = Mathf.Max(desired.y, World.TerrainHeight(desired.x, desired.z) + .3f);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, InVehicle ? .05f : .02f);
            transform.rotation = Quaternion.LookRotation(focus + shoulder * .6f - transform.position);
            if (InVehicle && G.car.Shake > 0) transform.position += Random.insideUnitSphere * G.car.Shake * .18f; // impact jolt
        }

        // ───────────── Kurosawa framing ─────────────
        // Akira Kurosawa shot people from far away on long lenses: compressed depth that stacks the
        // street, the temple and the mountains behind the figure; the camera low and level or looking
        // slightly up; the subject on a third of the frame; weighty, deliberate pans instead of a camera
        // glued to the back. Dialogue is a side-on telephoto two-shot that keeps to one side of the line.

        /// <summary>Distance-from-wall aware telephoto: if a wall forces the camera in, widen the lens so the figure keeps its size.</summary>
        float FitLens(Vector3 look, Vector3 dir, float distance, float frameHeight, out float fit)
        {
            fit = distance;
            if (Obstruction(look, .3f, dir, distance, out float obstruction))
                fit = Mathf.Max(.15f, obstruction - .3f);
            return Mathf.Clamp(2 * Mathf.Atan(frameHeight * .5f / fit) * Mathf.Rad2Deg, 14, 65);
        }

        void KurosawaShot(float mx, float my, float dt)
        {
            SetBodyVisible(true);
            var G = Game.Instance; float speed = G && G.player ? G.player.Speed : 0;
            // When you are not steering the camera it pans slowly to follow the walk, like a camera on rails.
            lookIdle = Mathf.Abs(mx) + Mathf.Abs(my) > .02f ? 0 : lookIdle + dt;
            if (lookIdle > 1.5f && speed > .5f) yaw = Mathf.LerpAngle(yaw, target.eulerAngles.y + 25, dt * .35f);
            if (lookIdle > 1.5f) pitch = Mathf.Lerp(pitch, -3, dt * .8f); // just below eye level, looking slightly up
            pitch = Mathf.Clamp(pitch, -8, 14);
            var rot = Quaternion.Euler(pitch, yaw, 0); var right = rot * Vector3.right;
            // A long lens at 22 m: the full figure stands in the lower half, the street and hills stack up behind.
            const float frameHeight = 5.2f, lookHeight = 2.0f;
            float fov = FitLens(target.position + Vector3.up * lookHeight, -(rot * Vector3.forward), 22, frameHeight, out float d);
            // Rule of thirds: shift the whole camera sideways so the figure sits on the left third.
            float halfWidth = Mathf.Tan(fov * .5f * Mathf.Deg2Rad) * d * cam.aspect, third = halfWidth / 3;
            var look = target.position + Vector3.up * lookHeight + right * third;
            var desired = look - rot * Vector3.forward * d;
            desired.y = Mathf.Max(desired.y, World.TerrainHeight(desired.x, desired.z) + .5f);
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, .35f); // weighty dolly
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look - transform.position), 1 - Mathf.Exp(-dt * 6));
            this.fov = Mathf.Lerp(this.fov, fov, dt * 3); cam.fieldOfView = this.fov;
        }

        void TwoShot(Vector3 a, Vector3 b, float dt)
        {
            SetBodyVisible(true);
            var mid = (a + b) * .5f; var line = b - a; line.y = 0; float sep = line.magnitude; line = sep > .01f ? line / sep : transform.right;
            var side = Vector3.Cross(Vector3.up, line);
            // 180-degree rule: stay on whichever side of the line the camera is already on.
            if (Vector3.Dot(transform.position - mid, side) < 0) side = -side;
            const float lens = 22; float aspect = cam.aspect;
            float d = Mathf.Max(5.5f, (sep + 1.8f) / (2 * Mathf.Tan(lens * .5f * Mathf.Deg2Rad) * aspect));
            var look = mid + Vector3.up * 1.4f;
            var dir = (side * d + Vector3.up * -.25f).normalized; // camera a little below eye level
            float frameHeight = 2 * d * Mathf.Tan(lens * .5f * Mathf.Deg2Rad);
            float fov = FitLens(look, dir, d, frameHeight, out float fit);
            var desired = look + dir * fit; desired.y = Mathf.Max(desired.y, World.TerrainHeight(desired.x, desired.z) + .5f);
            if (!inTwoShot) { transform.position = desired; this.fov = fov; inTwoShot = true; } // a cut, hidden by the wipe
            else transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, .6f);
            transform.rotation = Quaternion.LookRotation(look - transform.position);
            this.fov = Mathf.Lerp(this.fov, fov, dt * 3); cam.fieldOfView = this.fov;
        }

        bool bodyHidden;
        void SetBodyVisible(bool visible)
        {
            if (!target) return; bodyHidden = !visible;
            foreach (var r in target.GetComponentsInChildren<Renderer>()) if (r.enabled != visible) r.enabled = visible;
        }
    }
}
