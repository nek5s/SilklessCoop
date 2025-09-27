using System;
using System.IO;
using System.Reflection;
using System.Text;

namespace SilklessLib;

internal static class SilklessSerialization
{
    public static void Serialize(BinaryWriter bw, object o)
    {
        try
        {
            if (o == null) return;

            Type t = o.GetType();

            if (t == typeof(int))
            {
                bw.Write(BitConverter.GetBytes((int)o));
                return;
            }

            if (t == typeof(float))
            {
                bw.Write(BitConverter.GetBytes((float)o));
                return;
            }

            if (t == typeof(bool))
            {
                bw.Write((byte)((bool)o ? 1 : 0));
                return;
            }

            if (t == typeof(string))
            {
                bw.Write(BitConverter.GetBytes(((string)o).Length));
                bw.Write(Encoding.UTF8.GetBytes((string)o));
                return;
            }

            if (t.IsArray)
            {
                bw.Write(BitConverter.GetBytes(((Array)o).Length));
                foreach (var a in (Array)o) Serialize(bw, a);
                return;
            }

            foreach (FieldInfo field in t.GetFields()) Serialize(bw, field.GetValue(o));
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
        }
    }
    
    public static T Deserialize<T>(BinaryReader br) => (T)Deserialize(br, typeof(T));
    
    private static object Deserialize(BinaryReader br, Type t) {
        try
        {
            if (t == typeof(int)) return br.ReadInt32();
            if (t == typeof(float)) return br.ReadSingle();
            if (t == typeof(bool)) return br.ReadByte() != 0;
            if (t == typeof(string))
            {
                int length = br.ReadInt32();
                byte[] bytes = br.ReadBytes(length);
                return Encoding.UTF8.GetString(bytes);
            }

            if (t.IsArray)
            {
                int length = br.ReadInt32();
                Type t2 = t.GetElementType();
                // ReSharper disable once AssignNullToNotNullAttribute
                Array array = Array.CreateInstance(t2, length);
                for (int i = 0; i < length; i++) array.SetValue(Deserialize(br, t2), i);
                return array;
            }

            object obj = Activator.CreateInstance(t);
            foreach (FieldInfo field in t.GetFields())
            {
                object fieldValue = Deserialize(br, field.FieldType);
                field.SetValue(obj, fieldValue);
            }

            return obj;
        }
        catch (Exception e)
        {
            LogUtil.LogError(e.ToString());
            return null;
        }
    }
}