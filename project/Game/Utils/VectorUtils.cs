using Godot;

namespace FPS.Game.Utils
{
    public static class VectorUtils
    {


        public static Vector3 ProjectOnPlane(this Vector3 vector, Vector3 planeNormal)
        {
            float sqrMag = planeNormal.Dot(planeNormal);
            if (sqrMag < Mathf.Epsilon)
                return vector;
            else
            {
                var dot = vector.Dot(planeNormal);
                return new Vector3(vector.x - planeNormal.x * dot / sqrMag,
                    vector.y - planeNormal.y * dot / sqrMag,
                    vector.z - planeNormal.z * dot / sqrMag);
            }
        }

        public static float VerticalComponent(this Vector3 v)
        {
            return v.Dot(Vector3.Up);
        }

        public static Vector3 ToHorizontal(this Vector3 v)
        {
            return v.ProjectOnPlane(Vector3.Down);
        }
    }
}