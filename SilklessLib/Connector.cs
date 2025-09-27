using System;

namespace SilklessLib
{
    internal abstract class Connector
    {
        public bool Connected;
        public bool Active;

        public abstract string GetConnectorName();
        public abstract string GetId();
        public abstract string GetUsername();

        public abstract bool Init();
        public abstract bool Connect();
        public abstract bool Disconnect();

        public abstract bool SendBytes(byte[] data);

        public virtual void Update(float dt) { }

        public Action<byte[]> OnData;
    }
}
