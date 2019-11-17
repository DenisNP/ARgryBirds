using System;

namespace Assets.Scripts
{
    [Serializable]
    public class State
    {
        public Hit last_hit;
        public Pose last_pose;

        public bool HasPose(int i)
        {
            if (last_pose == null) return false;
            
            var ct = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            return ct - last_pose.time_end < 1000
                   && last_pose.pose_id == i;
            // && last_pose.time_end - last_pose.time_start > millis;
        }

        public bool Hit(long lastHit)
        {
            return last_hit != null && last_hit.time != lastHit;
        }
    }

    [Serializable]
    public class Hit
    {
        public long time;
        public int x;
        public int y;
        public float strength;
        public string id;
    }

    [Serializable]
    public class Pose
    {
        public long time_start;
        public long time_end;
        public int pose_id;
    }
}