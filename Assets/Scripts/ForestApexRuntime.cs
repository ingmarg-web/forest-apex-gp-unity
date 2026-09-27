using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ForestApex
{
    public enum TrackId { PineRidge, SunsetGulch, NeonDowntown }
    public enum CarId { ApexR, VeloceGT, RidgeRS }

    public sealed class ForestApexRuntime : MonoBehaviour
    {
        private Canvas ui;
        private GameObject menuRoot;
        private GameObject worldRoot;
        private Font font;
        private int selectedCar;
        private int selectedTrack;
        private Text carTitle;
        private Text trackTitle;
        private Text trackDescription;

        private readonly string[] carNames = { "APEX R", "VELOCE GT", "RIDGE RS" };
        private readonly string[] trackNames = { "PINE RIDGE", "SUNSET GULCH", "NEON DOWNTOWN" };
        private readonly string[] trackDescriptions =
        {
            "MOUNTAIN TECHNICAL  •  PRECISION / RHYTHM",
            "DESERT SPEED  •  DRAFTING / HIGH SPEED",
            "URBAN NIGHT  •  BRIDGE / OVERTAKING"
        };

        private void Awake()
        {
            Application.targetFrameRate = 60;
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EnsureEventSystem();
            CreateCameraAndLight();
            BuildUI();
            ShowGarage();
        }

        private void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }

        private void CreateCameraAndLight()
        {
            if (Camera.main == null)
            {
                var cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cam.tag = "MainCamera";
                cam.transform.position = new Vector3(0, 8, -14);
                cam.GetComponent<Camera>().fieldOfView = 62;
                cam.GetComponent<Camera>().farClipPlane = 900;
            }

            var sun = new GameObject("Sun", typeof(Light));
            sun.transform.rotation = Quaternion.Euler(35, -35, 0);
            var light = sun.GetComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
        }

        private void BuildUI()
        {
            var canvasGo = new GameObject("UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            ui = canvasGo.GetComponent<Canvas>();
            ui.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = canvasGo.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            menuRoot = Panel(ui.transform, "Menu", new Color(0.025f, 0.035f, 0.05f, 0.97f));

            var header = MakeText(menuRoot.transform, "FOREST APEX GP", 72, TextAnchor.MiddleLeft, FontStyle.Bold);
            SetRect(header.rectTransform, new Vector2(70, -55), new Vector2(900, 110), new Vector2(0, 1), new Vector2(0, 1));

            var sub = MakeText(menuRoot.transform, "PREMIUM MOBILE RACING", 26, TextAnchor.MiddleLeft, FontStyle.Normal);
            sub.color = new Color(0.35f, 0.8f, 1f);
            SetRect(sub.rectTransform, new Vector2(76, -140), new Vector2(700, 60), new Vector2(0, 1), new Vector2(0, 1));

            BuildGaragePanel();
            BuildTrackPanel();

            var race = MakeButton(menuRoot.transform, "START RACE", new Vector2(0, 45), new Vector2(1020, 95), StartRace,
                new Vector2(0.5f, 0), new Vector2(0.5f, 0));
            race.GetComponent<Image>().color = new Color(0.1f, 0.62f, 0.95f, 1f);
        }

        private void BuildGaragePanel()
        {
            var panel = Panel(menuRoot.transform, "CarPanel", new Color(0.07f, 0.09f, 0.13f, 0.92f));
            SetRect(panel.GetComponent<RectTransform>(), new Vector2(70, 120), new Vector2(760, 650), Vector2.zero, Vector2.zero);

            var label = MakeText(panel.transform, "GARAGE", 28, TextAnchor.MiddleLeft, FontStyle.Bold);
            SetRect(label.rectTransform, new Vector2(30, -25), new Vector2(300, 60), new Vector2(0, 1), new Vector2(0, 1));

            carTitle = MakeText(panel.transform, carNames[0], 60, TextAnchor.MiddleCenter, FontStyle.Bold);
            SetRect(carTitle.rectTransform, new Vector2(40, -150), new Vector2(680, 100), new Vector2(0, 1), new Vector2(0, 1));

            var silhouette = new GameObject("CarSilhouette", typeof(RectTransform), typeof(Image));
            silhouette.transform.SetParent(panel.transform, false);
            silhouette.GetComponent<Image>().color = new Color(0.15f, 0.65f, 1f);
            SetRect(silhouette.GetComponent<RectTransform>(), new Vector2(150, 250), new Vector2(460, 150), Vector2.zero, Vector2.zero);

            MakeButton(panel.transform, "<", new Vector2(55, 80), new Vector2(120, 80), () => ChangeCar(-1));
            MakeButton(panel.transform, ">", new Vector2(585, 80), new Vector2(120, 80), () => ChangeCar(1));

            var stats = MakeText(panel.transform,
                "ACCELERATION     ████████░░\nTOP SPEED         █████████░\nHANDLING          ████████░░",
                25, TextAnchor.UpperLeft, FontStyle.Normal);
            SetRect(stats.rectTransform, new Vector2(90, 80), new Vector2(520, 170), Vector2.zero, Vector2.zero);
        }

        private void BuildTrackPanel()
        {
            var panel = Panel(menuRoot.transform, "TrackPanel", new Color(0.07f, 0.09f, 0.13f, 0.92f));
            SetRect(panel.GetComponent<RectTransform>(), new Vector2(-70, 120), new Vector2(780, 650), new Vector2(1, 0), new Vector2(1, 0));

            var label = MakeText(panel.transform, "EVENT SELECT", 28, TextAnchor.MiddleLeft, FontStyle.Bold);
            SetRect(label.rectTransform, new Vector2(30, -25), new Vector2(400, 60), new Vector2(0, 1), new Vector2(0, 1));

            trackTitle = MakeText(panel.transform, trackNames[0], 52, TextAnchor.MiddleCenter, FontStyle.Bold);
            SetRect(trackTitle.rectTransform, new Vector2(40, -130), new Vector2(700, 90), new Vector2(0, 1), new Vector2(0, 1));

            trackDescription = MakeText(panel.transform, trackDescriptions[0], 22, TextAnchor.MiddleCenter, FontStyle.Normal);
            trackDescription.color = new Color(0.6f, 0.78f, 0.9f);
            SetRect(trackDescription.rectTransform, new Vector2(40, -205), new Vector2(700, 70), new Vector2(0, 1), new Vector2(0, 1));

            MakeButton(panel.transform, "<", new Vector2(60, 220), new Vector2(120, 80), () => ChangeTrack(-1));
            MakeButton(panel.transform, ">", new Vector2(600, 220), new Vector2(120, 80), () => ChangeTrack(1));
        }

        private void ChangeCar(int delta)
        {
            selectedCar = (selectedCar + delta + carNames.Length) % carNames.Length;
            carTitle.text = carNames[selectedCar];
        }

        private void ChangeTrack(int delta)
        {
            selectedTrack = (selectedTrack + delta + trackNames.Length) % trackNames.Length;
            trackTitle.text = trackNames[selectedTrack];
            trackDescription.text = trackDescriptions[selectedTrack];
        }

        private void ShowGarage()
        {
            if (worldRoot != null) Destroy(worldRoot);
            VirtualJoystick.Steering = 0;
            HoldPedal.Gas = 0;
            HoldPedal.Brake = 0;

            menuRoot.SetActive(true);
            var cam = Camera.main;
            cam.transform.position = new Vector3(0, 8, -14);
            cam.transform.rotation = Quaternion.Euler(16, 0, 0);
            cam.backgroundColor = new Color(0.015f, 0.02f, 0.035f);

            var chase = cam.GetComponent<ChaseCamera>();
            if (chase != null) chase.Target = null;
        }

        private void StartRace()
        {
            menuRoot.SetActive(false);
            worldRoot = new GameObject("RaceWorld");

            var track = new GameObject("Track", typeof(TrackRuntime)).GetComponent<TrackRuntime>();
            track.transform.SetParent(worldRoot.transform);
            track.Build((TrackId)selectedTrack);

            var spawnForward = track.Points[1] - track.Points[0];
            var player = CarFactory.Create((CarId)selectedCar, "PLAYER",
                track.Points[0] + Vector3.up * 1.2f, Quaternion.LookRotation(spawnForward), true);
            player.transform.SetParent(worldRoot.transform);

            var participants = new List<CarController> { player };

            for (int i = 0; i < 5; i++)
            {
                int idx = (track.Points.Count - 4 - i * 2 + track.Points.Count) % track.Points.Count;
                var forward = track.Points[(idx + 1) % track.Points.Count] - track.Points[idx];
                var ai = CarFactory.Create((CarId)((selectedCar + i + 1) % 3), "AI " + (i + 1),
                    track.Points[idx] + Vector3.up * 1.2f, Quaternion.LookRotation(forward), false);
                ai.transform.SetParent(worldRoot.transform);

                var driver = ai.gameObject.AddComponent<AIDriver>();
                driver.Configure(track, -3.2f + (i % 3) * 3.2f, 0.92f + i * 0.018f);
                participants.Add(ai);
            }

            var camera = Camera.main;
            var chase = camera.GetComponent<ChaseCamera>();
            if (chase == null) chase = camera.gameObject.AddComponent<ChaseCamera>();
            chase.Target = player.transform;

            var race = worldRoot.AddComponent<RaceManager>();
            race.Setup(track, participants, player, ui.transform, font, ShowGarage);
            BuildDrivingControls(race);
        }

        private void BuildDrivingControls(RaceManager race)
        {
            var controls = new GameObject("DrivingUI", typeof(RectTransform));
            controls.transform.SetParent(ui.transform, false);
            var rt = controls.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var joyBg = SimpleImage(controls.transform, "Steering", new Color(1, 1, 1, 0.12f), new Vector2(220, 190), new Vector2(250, 250));
            var joystick = joyBg.gameObject.AddComponent<VirtualJoystick>();
            var knob = SimpleImage(joyBg.transform, "Knob", new Color(1, 1, 1, 0.38f), Vector2.zero, new Vector2(95, 95));
            joystick.Knob = knob.rectTransform;

            var gas = MakeButton(controls.transform, "GAS", new Vector2(-245, 110), new Vector2(190, 190), null,
                new Vector2(1, 0), new Vector2(1, 0));
            gas.GetComponent<Image>().color = new Color(0.05f, 0.85f, 0.35f, 0.45f);
            gas.gameObject.AddComponent<HoldPedal>().Type = HoldPedal.PedalType.Gas;

            var brake = MakeButton(controls.transform, "BRAKE", new Vector2(-470, 80), new Vector2(165, 165), null,
                new Vector2(1, 0), new Vector2(1, 0));
            brake.GetComponent<Image>().color = new Color(0.95f, 0.15f, 0.12f, 0.45f);
            brake.gameObject.AddComponent<HoldPedal>().Type = HoldPedal.PedalType.Brake;

            race.DrivingControls = controls;
        }

        private GameObject Panel(Transform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            go.GetComponent<Image>().color = color;
            return go;
        }

        private Text MakeText(Transform parent, string value, int size, TextAnchor anchor, FontStyle style)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<Text>();
            t.font = font;
            t.text = value;
            t.fontSize = size;
            t.alignment = anchor;
            t.fontStyle = style;
            t.color = Color.white;
            t.horizontalOverflow = HorizontalWrapMode.Overflow;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            return t;
        }

        private Button MakeButton(Transform parent, string label, Vector2 pos, Vector2 size,
            UnityEngine.Events.UnityAction action, Vector2? anchorMin = null, Vector2? anchorMax = null)
        {
            var go = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = new Color(0.14f, 0.18f, 0.24f, 0.98f);

            var amin = anchorMin ?? Vector2.zero;
            var amax = anchorMax ?? Vector2.zero;
            SetRect(go.GetComponent<RectTransform>(), pos, size, amin, amax);

            var text = MakeText(go.transform, label, 27, TextAnchor.MiddleCenter, FontStyle.Bold);
            text.rectTransform.anchorMin = Vector2.zero;
            text.rectTransform.anchorMax = Vector2.one;
            text.rectTransform.offsetMin = text.rectTransform.offsetMax = Vector2.zero;

            var button = go.GetComponent<Button>();
            if (action != null) button.onClick.AddListener(action);
            return button;
        }

        private Image SimpleImage(Transform parent, string name, Color color, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var image = go.GetComponent<Image>();
            image.color = color;
            SetRect(go.GetComponent<RectTransform>(), pos, size, Vector2.zero, Vector2.zero);
            return image;
        }

        private static void SetRect(RectTransform rt, Vector2 pos, Vector2 size, Vector2 anchorMin, Vector2 anchorMax)
        {
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(
                anchorMin.x == 1 ? 1 : anchorMin.x == 0.5f ? 0.5f : 0,
                anchorMin.y == 1 ? 1 : anchorMin.y == 0.5f ? 0.5f : 0);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
        }
    }
}
