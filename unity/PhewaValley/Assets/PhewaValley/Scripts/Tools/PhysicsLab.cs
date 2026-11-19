using UnityEngine;

namespace PhewaValley
{
    // Launch with -aurora-physics-lab. This scene has no visit save or city systems.
    public class PhysicsLab : MonoBehaviour
    {
        PlayerController player;
        CarController car;
        Camera view;
        bool driving, mouseLook = true;
        int mouseReadyFrame;
        float yaw, pitch = 18;
        Vector3 brakingStart;
        float brakingDistance;
        bool measuringBrake;

        void Start()
        {
            Application.targetFrameRate = 120;
            QualitySettings.vSyncCount = 0;
            var sun = new GameObject("Lab sun").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.transform.rotation = Quaternion.Euler(50, -30, 0);
            RenderSettings.ambientLight = new Color(.55f, .58f, .65f);
            Box("Braking lane", new Vector3(0, -.5f, 30), new Vector3(90, 1, 180), Color.gray);
            for (int i = 0; i < 10; i++)
                Box("Distance line", new Vector3(15, .005f, i * 10), new Vector3(12, .01f, .12f), Color.white);
            for (int i = 0; i < 6; i++)
                Box("Stair", new Vector3(-8, (i + 1) * .12f, 8 + i * .6f),
                    new Vector3(4, (i + 1) * .24f, .6f), new Color(.4f, .5f, .6f));
            foreach (int angle in new[] { 20, 40, 60 })
            {
                var ramp = Box(angle + " degree ramp", new Vector3(-22 + (angle - 20) * .3f, Mathf.Sin(angle * Mathf.Deg2Rad) * 4, 25),
                    new Vector3(4, .3f, 8), new Color(.5f, .6f, .4f));
                ramp.transform.rotation = Quaternion.Euler(-angle, 0, 0);
            }
            Box("Wall corner A", new Vector3(0, 2, 18), new Vector3(5, 4, .4f), Color.white);
            Box("Wall corner B", new Vector3(2.3f, 2, 20), new Vector3(.4f, 4, 4), Color.white);
            Box("Vault obstacle", new Vector3(-4, .5f, 5), new Vector3(3, 1, 1), Color.gray);
            player = new GameObject("Lab character").AddComponent<PlayerController>();
            player.Init(null); player.Teleport(new Vector3(0, .05f, 0));
            car = new GameObject("Lab car").AddComponent<CarController>();
            car.Init(null); car.transform.position = new Vector3(15, .1f, 0);
            view = new GameObject("Lab camera").AddComponent<Camera>();
            view.tag = "MainCamera"; view.farClipPlane = 300;
        }

        GameObject Box(string label, Vector3 position, Vector3 size, Color color)
        {
            var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obj.name = label; obj.transform.position = position; obj.transform.localScale = size;
            obj.GetComponent<Renderer>().material.color = color;
            return obj;
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) mouseLook = !mouseLook;
            player.inputEnabled = mouseLook && Application.isFocused;
            if (!player.inputEnabled) return;
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (driving)
                {
                    if (Mathf.Abs(car.speed) > 2 || !car.TryExit(player, out var exit)) return;
                    car.driver = null; player.car = null; player.Teleport(exit);
                    player.figure.gameObject.SetActive(true);
                }
                else
                {
                    player.Teleport(player.transform.position);
                    car.driver = player; player.car = car;
                    player.body.enabled = false; player.figure.gameObject.SetActive(false);
                }
                driving = !driving;
            }
            if (Input.GetKeyDown(KeyCode.R))
            {
                car.Launch(0); car.GetComponent<CharacterController>().enabled = false;
                car.transform.SetPositionAndRotation(new Vector3(15, .1f, 0), Quaternion.identity);
                car.GetComponent<CharacterController>().enabled = true;
                if (!driving) player.Teleport(new Vector3(0, .05f, 0));
                measuringBrake = false; yaw = 0; pitch = 18;
            }
            if (driving && Input.GetKeyDown(KeyCode.B))
            {
                brakingStart = car.transform.position; measuringBrake = true;
            }
            if (measuringBrake)
            {
                brakingDistance = Vector3.Distance(brakingStart, car.transform.position);
                if (Mathf.Abs(car.speed) < .1f) measuringBrake = false;
            }
            if (Input.GetKeyDown(KeyCode.F1)) Application.targetFrameRate = 15;
            if (Input.GetKeyDown(KeyCode.F2)) Application.targetFrameRate = 60;
            if (Input.GetKeyDown(KeyCode.F3)) Application.targetFrameRate = 144;
        }

        void LateUpdate()
        {
            bool capture = mouseLook && Application.isFocused;
            if (capture && Cursor.lockState != CursorLockMode.Locked) mouseReadyFrame = Time.frameCount + 2;
            Cursor.lockState = capture ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !capture;
            if (capture && Time.frameCount > mouseReadyFrame)
            {
                yaw += Input.GetAxis("Mouse X") * 2.6f;
                pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 2.1f, -10, 70);
            }
            var target = driving ? car.transform.position : player.transform.position;
            view.transform.rotation = Quaternion.Euler(pitch, yaw, 0);
            view.transform.position = target + Vector3.up * 1.6f - view.transform.forward * (driving ? 8 : 5);
        }

        void OnGUI()
        {
            GUI.Box(new Rect(12, 12, Mathf.Min(Screen.width - 24, 1000), 150),
                "AURORA PHYSICS LAB\nWASD move | Shift run | Space jump / handbrake | Ctrl dive\n" +
                "E switch car / character | B brake | R reset | F1 15 FPS | F2 60 FPS | F3 144 FPS | Esc release mouse\n" +
                $"Speed {(driving ? Mathf.Abs(car.speed) : player.Speed):0.00} m/s | Brake distance {brakingDistance:0.00} m\n" +
                "Stairs and vault ahead | Slopes beyond the stairs | Braking lane on the right",
                new GUIStyle(GUI.skin.box) { fontSize = 18, alignment = TextAnchor.UpperLeft, wordWrap = true });
        }
    }
}
