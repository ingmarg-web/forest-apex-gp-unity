using System.Collections.Generic;
using UnityEngine;

namespace ForestApex
{
    public sealed class TrackRuntime : MonoBehaviour
    {
        public readonly List<Vector3> Points = new List<Vector3>();
        public TrackId Id { get; private set; }

        public void Build(TrackId id)
        {
            Id = id;
            GeneratePath(id);
            CreateRoad(id);
            CreateEnvironment(id);
        }

        private void GeneratePath(TrackId id)
        {
            Points.Clear();
            int count = id == TrackId.NeonDowntown ? 88 : 96;

            for (int i = 0; i < count; i++)
            {
                float t = i / (float)count * Mathf.PI * 2f;
                float x;
                float z;
                float y;

                if (id == TrackId.PineRidge)
                {
                    float radius = 105f + Mathf.Sin(t * 3f) * 16f + Mathf.Sin(t * 7f) * 6f;
                    x = Mathf.Sin(t) * radius * 0.9f;
                    z = Mathf.Cos(t) * radius;
                    y = Mathf.Sin(t * 2f) * 5.5f + Mathf.Sin(t * 5f) * 1.4f;
                }
                else if (id == TrackId.SunsetGulch)
                {
                    float radius = 1f + Mathf.Sin(t * 4f) * 0.08f;
                    x = Mathf.Sin(t) * 155f * radius;
                    z = Mathf.Cos(t) * 95f * radius;
                    y = Mathf.Sin(t * 2f) * 2.2f;
                }
                else
                {
                    x = Mathf.Sin(t) * 118f + Mathf.Sin(t * 3f) * 20f;
                    z = Mathf.Cos(t) * 84f;

                    float normalized = i / (float)count;
                    bool bridge = normalized > 0.12f && normalized < 0.34f;
                    if (bridge)
                    {
                        float u = Mathf.InverseLerp(0.12f, 0.34f, normalized);
                        y = Mathf.Sin(u * Mathf.PI) * 14f + 3f;
                    }
                    else
                    {
                        y = 1.4f;
                    }
                }

                Points.Add(new Vector3(x, y, z));
            }
        }

        private void CreateRoad(TrackId id)
        {
            Color roadColor = id == TrackId.SunsetGulch
                ? new Color(0.19f, 0.16f, 0.14f)
                : new Color(0.055f, 0.065f, 0.075f);

            Material roadMaterial = MaterialUtil.Lit(roadColor, 0.28f, 0.05f);
            Material markingMaterial = MaterialUtil.Lit(new Color(0.9f, 0.92f, 0.86f), 0.15f, 0f);
            float width = id == TrackId.PineRidge ? 11.5f : id == TrackId.SunsetGulch ? 16f : 15f;

            for (int i = 0; i < Points.Count; i++)
            {
                Vector3 a = Points[i];
                Vector3 b = Points[(i + 1) % Points.Count];
                Vector3 center = (a + b) * 0.5f;
                Vector3 direction = b - a;
                float length = direction.magnitude;

                var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
                road.name = "Road";
                road.transform.SetParent(transform, false);
                road.transform.position = center - Vector3.up * 0.25f;
                road.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
                road.transform.localScale = new Vector3(width, 0.5f, length + 0.8f);
                road.GetComponent<Renderer>().material = roadMaterial;

                if (i % 2 == 0)
                {
                    var mark = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.Destroy(mark.GetComponent<Collider>());
                    mark.name = "CenterMark";
                    mark.transform.SetParent(transform, false);
                    mark.transform.position = center + Vector3.up * 0.03f;
                    mark.transform.rotation = road.transform.rotation;
                    mark.transform.localScale = new Vector3(0.16f, 0.03f, Mathf.Min(4f, length * 0.55f));
                    mark.GetComponent<Renderer>().material = markingMaterial;
                }

                if (id == TrackId.NeonDowntown && a.y > 5f)
                {
                    Vector3 right = Vector3.Cross(Vector3.up, direction.normalized);
                    CreateBarrier(center + right * (width * 0.52f) + Vector3.up * 0.8f, road.transform.rotation, length);
                    CreateBarrier(center - right * (width * 0.52f) + Vector3.up * 0.8f, road.transform.rotation, length);
                }
            }
        }

        private void CreateBarrier(Vector3 position, Quaternion rotation, float length)
        {
            var barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.name = "BridgeBarrier";
            barrier.transform.SetParent(transform, false);
            barrier.transform.SetPositionAndRotation(position, rotation);
            barrier.transform.localScale = new Vector3(0.28f, 1.5f, length + 0.5f);
            barrier.GetComponent<Renderer>().material = MaterialUtil.Lit(new Color(0.12f, 0.18f, 0.25f), 0.8f, 0.1f);
        }

        private void CreateEnvironment(TrackId id)
        {
            if (id == TrackId.PineRidge)
                ForestEnvironment();
            else if (id == TrackId.SunsetGulch)
                DesertEnvironment();
            else
                CityEnvironment();
        }

        private void ForestEnvironment()
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.17f, 0.24f, 0.25f);
            RenderSettings.fogDensity = 0.0035f;
            Camera.main.backgroundColor = new Color(0.22f, 0.33f, 0.38f);

            for (int i = 0; i < Points.Count; i += 2)
            {
                Vector3 point = Points[i];
                Vector3 direction = (Points[(i + 1) % Points.Count] - point).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, direction);

                for (int side = -1; side <= 1; side += 2)
                {
                    float distance = 15f + (i % 7) * 2.2f;
                    CreateTree(point + right * side * distance - Vector3.up * 0.2f, 5.5f + (i % 5));
                }
            }
        }

        private void CreateTree(Vector3 position, float height)
        {
            var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(trunk.GetComponent<Collider>());
            trunk.name = "TreeTrunk";
            trunk.transform.SetParent(transform, false);
            trunk.transform.position = position + Vector3.up * height * 0.22f;
            trunk.transform.localScale = new Vector3(0.32f, height * 0.22f, 0.32f);
            trunk.GetComponent<Renderer>().material = MaterialUtil.Lit(new Color(0.22f, 0.12f, 0.06f), 0.2f, 0);

            var crown = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Object.Destroy(crown.GetComponent<Collider>());
            crown.name = "TreeCrown";
            crown.transform.SetParent(transform, false);
            crown.transform.position = position + Vector3.up * height * 0.62f;
            crown.transform.localScale = new Vector3(2.2f, height * 0.38f, 2.2f);
            crown.GetComponent<Renderer>().material = MaterialUtil.Lit(new Color(0.035f, 0.22f, 0.11f), 0.12f, 0);
        }

        private void DesertEnvironment()
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.56f, 0.3f, 0.18f);
            RenderSettings.fogDensity = 0.0017f;
            Camera.main.backgroundColor = new Color(0.74f, 0.34f, 0.18f);

            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "DesertGround";
            ground.transform.SetParent(transform, false);
            ground.transform.position = Vector3.down * 0.6f;
            ground.transform.localScale = new Vector3(45, 1, 34);
            ground.GetComponent<Renderer>().material = MaterialUtil.Lit(new Color(0.42f, 0.23f, 0.12f), 0.08f, 0);

            for (int i = 0; i < 34; i++)
            {
                float angle = i / 34f * Mathf.PI * 2f;
                Vector3 position = new Vector3(
                    Mathf.Sin(angle) * (190 + (i % 5) * 12),
                    8 + (i % 4) * 4,
                    Mathf.Cos(angle) * (130 + (i % 4) * 9));

                var rock = GameObject.CreatePrimitive(PrimitiveType.Cube);
                Object.Destroy(rock.GetComponent<Collider>());
                rock.name = "Mesa";
                rock.transform.SetParent(transform, false);
                rock.transform.position = position;
                rock.transform.rotation = Quaternion.Euler(0, i * 37f, 0);
                rock.transform.localScale = new Vector3(14 + i % 9, 18 + i % 6 * 4, 11 + i % 7);
                rock.GetComponent<Renderer>().material = MaterialUtil.Lit(new Color(0.42f, 0.18f, 0.08f), 0.15f, 0);
            }
        }

        private void CityEnvironment()
        {
            RenderSettings.fog = true;
            RenderSettings.fogColor = new Color(0.015f, 0.025f, 0.065f);
            RenderSettings.fogDensity = 0.0025f;
            Camera.main.backgroundColor = new Color(0.008f, 0.012f, 0.04f);

            var water = GameObject.CreatePrimitive(PrimitiveType.Plane);
            water.name = "HarbourWater";
            water.transform.SetParent(transform, false);
            water.transform.position = new Vector3(45, -0.5f, 45);
            water.transform.localScale = new Vector3(20, 1, 17);
            water.GetComponent<Renderer>().material = MaterialUtil.Lit(new Color(0.01f, 0.05f, 0.11f), 0.85f, 0.45f);

            for (int i = 0; i < Points.Count; i += 3)
            {
                Vector3 point = Points[i];
                Vector3 direction = (Points[(i + 1) % Points.Count] - point).normalized;
                Vector3 right = Vector3.Cross(Vector3.up, direction);

                for (int side = -1; side <= 1; side += 2)
                {
                    float height = 12 + (i * 7 % 32);

                    var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    Object.Destroy(building.GetComponent<Collider>());
                    building.name = "Building";
                    building.transform.SetParent(transform, false);
                    building.transform.position = point + right * side * (24 + i % 5 * 3) + Vector3.up * (height * 0.5f - 0.2f);
                    building.transform.localScale = new Vector3(11 + i % 6, height, 10 + (i * 3) % 8);

                    Color color = i % 4 == 0 ? new Color(0.08f, 0.18f, 0.3f) : new Color(0.045f, 0.06f, 0.1f);
                    building.GetComponent<Renderer>().material = MaterialUtil.Lit(color, 0.65f, 0.18f);

                    if (i % 6 == 0)
                    {
                        var neon = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Object.Destroy(neon.GetComponent<Collider>());
                        neon.name = "Neon";
                        neon.transform.SetParent(building.transform, false);
                        neon.transform.localPosition = new Vector3(0, 0.1f, -0.51f);
                        neon.transform.localScale = new Vector3(0.7f, 0.08f, 0.02f);

                        Color neonColor = i % 12 == 0 ? new Color(0.1f, 0.8f, 1f) : new Color(1f, 0.08f, 0.65f);
                        neon.GetComponent<Renderer>().material = MaterialUtil.Emissive(neonColor, 4f);
                    }
                }
            }
        }

        public int NearestPoint(Vector3 position)
        {
            int best = 0;
            float bestSquaredDistance = float.MaxValue;

            for (int i = 0; i < Points.Count; i++)
            {
                float distance = (Points[i] - position).sqrMagnitude;
                if (distance < bestSquaredDistance)
                {
                    bestSquaredDistance = distance;
                    best = i;
                }
            }

            return best;
        }
    }
}
