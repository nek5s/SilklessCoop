using System;

namespace SilklessCoop
{
    internal class Packet
    {
        private static float parseFloat(string s)
        {
            s = s.Replace(",", ".");
            if (s.IndexOf('.') < 0) return float.Parse(s);

            string before = s.Split(".")[0];
            string after = s.Split(".")[1];

            float fbefore = float.Parse(before);
            float fafter = float.Parse(after) / MathF.Pow(10, after.Length);

            return fbefore + fafter;
        }

        public bool Valid = false;

        public string playerId;
        public int playerCount;
        public string sceneName;
        public float posX;
        public float posY;
        public float posZ;
        public int scaleX;
        public int spriteId;
        public float vX;
        public float vY;

        public static Packet FromString(string line)
        {
            try
            {
                Packet p = new Packet();

                string[] parts = line.Split("::");
                string id = parts[0];

                string metadata = parts[1];
                string[] metadataParts = metadata.Split(':');

                string content = parts[2];
                string[] contentParts = content.Split(':');

                // id
                p.playerId = id;
                // metadata
                p.playerCount = int.Parse(metadataParts[0]);
                // content
                p.sceneName = contentParts[0];
                p.posX = parseFloat(contentParts[1]);
                p.posY = parseFloat(contentParts[2]);
                p.posZ = parseFloat(contentParts[3]);
                p.scaleX = int.Parse(contentParts[4]);
                p.spriteId = int.Parse(contentParts[5]);
                p.vX = parseFloat(contentParts[6]);
                p.vY = parseFloat(contentParts[7]);

                p.Valid = true;

                return p;
            }
            catch (Exception e) {
                return null;
            }
        }

        public static string ToString(Packet p)
        {
            return $"{p.sceneName}:{p.posX}:{p.posY}:{p.posZ}:{p.scaleX}:{p.spriteId}:{p.vX}:{p.vY}\n";
        }
    }
}
