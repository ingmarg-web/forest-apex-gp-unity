using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ForestApex
{
    public sealed class RaceManager : MonoBehaviour
    {
        private TrackRuntime track;
        private List<CarController> cars;
        private CarController player;
        private Font font;
        private Text countdown;
        private Text hud;
        private float startTime;
        private bool started;
        private bool finished;
        private readonly Dictionary<CarController, int> lap = new Dictionary<CarController, int>();
        private readonly Dictionary<CarController, int> lastIndex = new Dictionary<CarController, int>();
        private System.Action onGarage;

        public GameObject DrivingControls;

        public void Setup(TrackRuntime trackRuntime, List<CarController> participants, CarController playerCar,
            Transform uiRoot, Font uiFont, System.Action garage)
        {
            track = trackRuntime;
            cars = participants;
            player = playerCar;
            font = uiFont;
            onGarage = garage;

            foreach (CarController car in cars)
            {
                lap[car] = 0;
                lastIndex[car] = track.NearestPoint(car.transform.position);
                car.RaceEnabled = false;
            }

            countdown = MakeText(uiRoot, "3", 120, TextAnchor.MiddleCenter);
            countdown.rectTransform.anchorMin = new Vector2(0.35f, 0.35f);
            countdown.rectTransform.anchorMax = new Vector2(0.65f, 0.65f);
            countdown.rectTransform.offsetMin = countdown.rectTransform.offsetMax = Vector2.zero;

            hud = MakeText(uiRoot, string.Empty, 30, TextAnchor.UpperLeft);
            hud.rectTransform.anchorMin = new Vector2(0, 0.78f);
            hud.rectTransform.anchorMax = new Vector2(0.35f, 1f);
            hud.rectTransform.offsetMin = new Vector2(38, -20);
            hud.rectTransform.offsetMax = new Vector2(-20, -30);

            startTime = Time.time;
        }

        private void Update()
        {
            if (!started)
            {
                float elapsed = Time.time - startTime;

                if (elapsed < 1) countdown.text = "3";
                else if (elapsed < 2) countdown.text = "2";
                else if (elapsed < 3) countdown.text = "1";
                else if (elapsed < 3.8f) countdown.text = "GO!";
                else
                {
                    started = true;
                    countdown.gameObject.SetActive(false);
                    foreach (CarController car in cars)
                        car.RaceEnabled = true;
                }

                return;
            }

            if (finished) return;

            foreach (CarController car in cars)
            {
                int index = track.NearestPoint(car.transform.position);
                int previous = lastIndex[car];

                if (previous > track.Points.Count * 0.78f && index < track.Points.Count * 0.2f)
                    lap[car]++;

                lastIndex[car] = index;
            }

            int position = 1;
            float playerProgress = lap[player] * track.Points.Count + lastIndex[player];

            foreach (CarController car in cars)
            {
                if (car == player) continue;

                float progress = lap[car] * track.Points.Count + lastIndex[car];
                if (progress > playerProgress) position++;
            }

            hud.text =
                "POS  " + position + "/" + cars.Count +
                "\nLAP  " + Mathf.Clamp(lap[player] + 1, 1, 3) + "/3" +
                "\n" + Mathf.RoundToInt(player.SpeedKph) + " KM/H";

            if (lap[player] >= 3)
                Finish(position);
        }

        private void Finish(int position)
        {
            finished = true;

            foreach (CarController car in cars)
                car.RaceEnabled = false;

            if (DrivingControls != null)
                DrivingControls.SetActive(false);

            Transform root = hud.transform.parent;

            var panel = new GameObject("Results", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(root, false);

            RectTransform rt = panel.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.25f, 0.23f);
            rt.anchorMax = new Vector2(0.75f, 0.77f);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            panel.GetComponent<Image>().color = new Color(0.03f, 0.045f, 0.07f, 0.97f);

            Text result = MakeText(panel.transform, "RACE COMPLETE\nP" + position, 58, TextAnchor.MiddleCenter);
            result.rectTransform.anchorMin = new Vector2(0.08f, 0.42f);
            result.rectTransform.anchorMax = new Vector2(0.92f, 0.92f);
            result.rectTransform.offsetMin = result.rectTransform.offsetMax = Vector2.zero;

            var buttonObject = new GameObject("Garage", typeof(RectTransform), typeof(Image), typeof(Button));
            buttonObject.transform.SetParent(panel.transform, false);

            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            buttonRect.anchorMin = new Vector2(0.2f, 0.12f);
            buttonRect.anchorMax = new Vector2(0.8f, 0.34f);
            buttonRect.offsetMin = buttonRect.offsetMax = Vector2.zero;

            buttonObject.GetComponent<Image>().color = new Color(0.08f, 0.6f, 0.95f);

            Text label = MakeText(buttonObject.transform, "RETURN TO GARAGE", 28, TextAnchor.MiddleCenter);
            label.rectTransform.anchorMin = Vector2.zero;
            label.rectTransform.anchorMax = Vector2.one;
            label.rectTransform.offsetMin = label.rectTransform.offsetMax = Vector2.zero;

            buttonObject.GetComponent<Button>().onClick.AddListener(() =>
            {
                Destroy(panel);
                Destroy(hud.gameObject);
                Destroy(countdown.gameObject);
                if (DrivingControls != null) Destroy(DrivingControls);
                onGarage?.Invoke();
            });
        }

        private Text MakeText(Transform parent, string value, int size, TextAnchor anchor)
        {
            var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            Text text = go.GetComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.fontStyle = FontStyle.Bold;
            text.alignment = anchor;
            text.color = Color.white;
            return text;
        }
    }
}
