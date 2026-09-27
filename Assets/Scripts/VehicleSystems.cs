using UnityEngine;
using UnityEngine.EventSystems;

namespace ForestApex
{
    public static class CarFactory
    {
        public static CarController Create(CarId id, string name, Vector3 pos, Quaternion rot, bool player)
        {
            var root = new GameObject(name, typeof(Rigidbody), typeof(BoxCollider), typeof(CarController));
            root.transform.SetPositionAndRotation(pos, rot);

            var rb = root.GetComponent<Rigidbody>();
            rb.mass = 1280;
            rb.centerOfMass = new Vector3(0, -0.42f, 0.12f);
            rb.linearDamping = 0.04f;
            rb.angularDamping = 2.4f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;

            var collider = root.GetComponent<BoxCollider>();
            collider.size = new Vector3(1.85f, 0.8f, 4.25f);
            collider.center = new Vector3(0, 0.45f, 0);

            var controller = root.GetComponent<CarController>();
            controller.IsPlayer = player;
            controller.Configure(id);

            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(body.GetComponent<Collider>());
            body.name = "Body";
            body.transform.SetParent(root.transform, false);
            body.transform.localPosition = new Vector3(0, 0.45f, 0);
            body.transform.localScale = new Vector3(1.85f, 0.62f, 4.15f);

            var cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(cabin.GetComponent<Collider>());
            cabin.name = "Cabin";
            cabin.transform.SetParent(root.transform, false);
            cabin.transform.localPosition = new Vector3(0, 0.93f, -0.15f);
            cabin.transform.localScale = new Vector3(1.45f, 0.5f, 1.8f);

            var colors = new[]
            {
                new Color(0.08f, 0.65f, 1f),
                new Color(0.95f, 0.18f, 0.12f),
                new Color(0.95f, 0.72f, 0.08f)
            };

            body.GetComponent<Renderer>().material = MaterialUtil.Lit(colors[(int)id], 0.72f, 0.15f);
            cabin.GetComponent<Renderer>().material = MaterialUtil.Lit(new Color(0.035f, 0.055f, 0.08f), 0.92f, 0.08f);

            for (int side = -1; side <= 1; side += 2)
            {
                for (int axle = -1; axle <= 1; axle += 2)
                {
                    var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    Object.Destroy(wheel.GetComponent<Collider>());
                    wheel.name = "Wheel";
                    wheel.transform.SetParent(root.transform, false);
                    wheel.transform.localPosition = new Vector3(side * 0.95f, 0.15f, axle * 1.38f);
                    wheel.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    wheel.transform.localScale = new Vector3(0.38f, 0.18f, 0.38f);
                    wheel.GetComponent<Renderer>().material = MaterialUtil.Lit(new Color(0.015f, 0.015f, 0.018f), 0.1f, 0f);
                }
            }

            return controller;
        }
    }

    public sealed class CarController : MonoBehaviour
    {
        public bool IsPlayer;
        public float SpeedKph { get; private set; }
        public float SteerInput { get; set; }
        public float GasInput { get; set; }
        public float BrakeInput { get; set; }
        public bool RaceEnabled { get; set; }

        private Rigidbody rb;
        private float acceleration;
        private float brakeForce;
        private float topSpeed;
        private float steering;
        private float grip;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
        }

        public void Configure(CarId id)
        {
            switch (id)
            {
                case CarId.ApexR:
                    acceleration = 19.5f;
                    brakeForce = 30f;
                    topSpeed = 70f;
                    steering = 1.0f;
                    grip = 8.5f;
                    break;
                case CarId.VeloceGT:
                    acceleration = 18.2f;
                    brakeForce = 29f;
                    topSpeed = 75f;
                    steering = 0.91f;
                    grip = 7.8f;
                    break;
                default:
                    acceleration = 20.8f;
                    brakeForce = 31f;
                    topSpeed = 67f;
                    steering = 1.08f;
                    grip = 9.2f;
                    break;
            }
        }

        private void FixedUpdate()
        {
            if (IsPlayer)
            {
                float keySteer = Input.GetAxisRaw("Horizontal");
                float keyGas = Mathf.Max(0, Input.GetAxisRaw("Vertical"));
                float keyBrake = Mathf.Max(0, -Input.GetAxisRaw("Vertical"));

                SteerInput = Mathf.Abs(VirtualJoystick.Steering) > 0.02f ? VirtualJoystick.Steering : keySteer;
                GasInput = Mathf.Max(HoldPedal.Gas, keyGas);
                BrakeInput = Mathf.Max(HoldPedal.Brake, keyBrake);
            }

            SpeedKph = rb.linearVelocity.magnitude * 3.6f;
            if (!RaceEnabled) return;

            Vector3 local = transform.InverseTransformDirection(rb.linearVelocity);
            float forwardSpeed = local.z;

            if (GasInput > 0.01f && forwardSpeed < topSpeed)
                rb.AddForce(transform.forward * (GasInput * acceleration), ForceMode.Acceleration);

            if (BrakeInput > 0.01f)
            {
                Vector3 direction = rb.linearVelocity.sqrMagnitude > 0.2f ? -rb.linearVelocity.normalized : -transform.forward;
                rb.AddForce(direction * (BrakeInput * brakeForce), ForceMode.Acceleration);
            }

            float speedFactor = Mathf.Clamp01(Mathf.Abs(forwardSpeed) / 7f);
            float steerRate = Mathf.Lerp(0.35f, 1f, speedFactor) * steering * 48f;
            float directionSign = Mathf.Sign(Mathf.Abs(forwardSpeed) < 0.01f ? 1 : forwardSpeed);
            Quaternion rotation = Quaternion.Euler(0, SteerInput * steerRate * Time.fixedDeltaTime * directionSign, 0);
            rb.MoveRotation(rb.rotation * rotation);

            rb.AddForce(-transform.right * local.x * grip, ForceMode.Acceleration);
            rb.AddForce(-transform.up * rb.linearVelocity.magnitude * 0.36f, ForceMode.Acceleration);

            if (rb.linearVelocity.magnitude > topSpeed * 1.06f)
                rb.linearVelocity = rb.linearVelocity.normalized * topSpeed * 1.06f;
        }
    }

    public sealed class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public static float Steering;
        public RectTransform Knob;

        private RectTransform area;
        private float radius;

        private void Awake()
        {
            area = GetComponent<RectTransform>();
            radius = area.sizeDelta.x * 0.34f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(area, eventData.position, eventData.pressEventCamera, out Vector2 point);
            point = Vector2.ClampMagnitude(point, radius);
            if (Knob != null) Knob.anchoredPosition = point;
            Steering = Mathf.Clamp(point.x / radius, -1, 1);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Steering = 0;
            if (Knob != null) Knob.anchoredPosition = Vector2.zero;
        }

        private void OnDisable()
        {
            Steering = 0;
            if (Knob != null) Knob.anchoredPosition = Vector2.zero;
        }
    }

    public sealed class HoldPedal : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public enum PedalType { Gas, Brake }

        public PedalType Type;
        public static float Gas;
        public static float Brake;

        public void OnPointerDown(PointerEventData eventData)
        {
            SetValue(1);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SetValue(0);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetValue(0);
        }

        private void SetValue(float value)
        {
            if (Type == PedalType.Gas) Gas = value;
            else Brake = value;
        }

        private void OnDisable()
        {
            if (Type == PedalType.Gas) Gas = 0;
            else Brake = 0;
        }
    }

    public sealed class ChaseCamera : MonoBehaviour
    {
        public Transform Target;
        private Vector3 velocity;

        private void LateUpdate()
        {
            if (Target == null) return;

            var car = Target.GetComponent<CarController>();
            float speed = car != null ? car.SpeedKph : 0;
            float distance = Mathf.Lerp(8.5f, 12.5f, Mathf.InverseLerp(40, 230, speed));

            Vector3 desired = Target.position - Target.forward * distance + Vector3.up * 4.1f;
            transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, 0.12f);

            Vector3 look = Target.position + Target.forward * Mathf.Lerp(7, 15, speed / 250f) + Vector3.up * 1.1f;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look - transform.position), 7f * Time.deltaTime);

            Camera cam = GetComponent<Camera>();
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, Mathf.Lerp(62, 78, Mathf.InverseLerp(40, 230, speed)), 3.5f * Time.deltaTime);
        }
    }

    public sealed class AIDriver : MonoBehaviour
    {
        private TrackRuntime track;
        private CarController car;
        private float lane;
        private float pace;

        public void Configure(TrackRuntime trackRuntime, float laneOffset, float paceMultiplier)
        {
            track = trackRuntime;
            lane = laneOffset;
            pace = paceMultiplier;
            car = GetComponent<CarController>();
        }

        private void FixedUpdate()
        {
            if (track == null || car == null) return;

            int nearest = track.NearestPoint(transform.position);
            int targetIndex = (nearest + 4 + Mathf.RoundToInt(car.SpeedKph / 70f)) % track.Points.Count;

            Vector3 target = track.Points[targetIndex];
            Vector3 next = track.Points[(targetIndex + 1) % track.Points.Count];
            Vector3 roadForward = (next - target).normalized;
            Vector3 right = Vector3.Cross(Vector3.up, roadForward);

            float dynamicLane = lane + Mathf.Sin(Time.time * 0.42f + lane) * 0.8f;
            target += right * dynamicLane;

            Vector3 local = transform.InverseTransformPoint(target);
            car.SteerInput = Mathf.Clamp(local.x / Mathf.Max(3f, local.magnitude * 0.35f), -1, 1);

            float corner = Mathf.Abs(car.SteerInput);
            car.GasInput = corner > 0.78f ? 0.48f : pace;
            car.BrakeInput = corner > 0.9f && car.SpeedKph > 135 ? 0.28f : 0;
        }
    }

    public static class MaterialUtil
    {
        public static Material Lit(Color color, float smoothness, float metallic)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            var material = new Material(shader);
            material.color = color;

            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            return material;
        }

        public static Material Emissive(Color color, float intensity)
        {
            Material material = Lit(color, 0.65f, 0.1f);
            material.EnableKeyword("_EMISSION");
            if (material.HasProperty("_EmissionColor"))
                material.SetColor("_EmissionColor", color * intensity);
            return material;
        }
    }
}
