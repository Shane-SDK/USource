using UnityEngine;
using System.Runtime.InteropServices;
using System.IO;
using System;
using System.Text;
using USource.Formats;

namespace USource
{
    public class UReader : BinaryReader
    {
        public Stream InputStream;

        public UReader(Stream InputStream) 
            : base(InputStream)
        {
            this.InputStream = InputStream;

            if (!InputStream.CanRead)
                throw new InvalidDataException("Stream unreadable!");
        }

        public byte[] GetBytes(int Count, long Offset)
        {
            if (!Offset.Equals(0) && !Offset.Equals(InputStream.Position))
                InputStream.Seek(Offset, SeekOrigin.Begin);

            byte[] Buffer = new byte[Count];
            InputStream.Read(Buffer, 0, Buffer.Length);

            return Buffer;
        }
        public void ReadType<T>(ref T Variable, long? Offset = null)
        {
            if (Offset.HasValue)
                InputStream.Seek(Offset.Value, SeekOrigin.Begin);

            Byte[] Buffer = new byte[Marshal.SizeOf(typeof(T))];
            InputStream.Read(Buffer, 0, Buffer.Length);

            GCHandle Handle = GCHandle.Alloc(Buffer, GCHandleType.Pinned);
            try
            {
                Variable = (T)Marshal.PtrToStructure(Handle.AddrOfPinnedObject(), typeof(T));
            }
            finally
            {
                Handle.Free();
            }
        }
        public void ReadArray<T>(ref T[] Array, long? Offset = null)
        {
            if (Offset.HasValue)
                InputStream.Seek(Offset.Value, SeekOrigin.Begin);

            for (Int32 i = 0; i < Array.Length; i++)
                ReadType(ref Array[i]);
        }
        public void ReadArray<T>(ref T[] array, int version = 0) where T : struct, ISourceObject
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i].ReadToObject(this, version);
            }
        }
        public void ReadSourceObjectArray<T>(ref T[] array, long position, int version = 0) where T : struct, ISourceObject
        {
            InputStream.Position = position;
            ReadArray(ref array, version);
        }
        public void ReadSourceObjectArray<T>(ref T[] array, int version = 0) where T : struct, ISourceObject
        {
            ReadArray(ref array, version);
        }
        public T ReadSourceObject<T>(int version = 0) where T : ISourceObject, new()
        {
            T instance = new();
            instance.ReadToObject(this, version);
            return instance;
        }
        [ThreadStatic]
        private static StringBuilder _sBuilder;
        public String ReadNullTerminatedString(long? Offset = null)
        {
            if (Offset.HasValue && !Offset.Value.Equals(InputStream.Position))
                InputStream.Seek(Offset.Value, SeekOrigin.Begin);

            if (_sBuilder == null) 
                _sBuilder = new StringBuilder();
            else 
                _sBuilder.Remove(0, _sBuilder.Length);

            while (true)
            {
                Char c = (char)InputStream.ReadByte();
                if (c == 0) 
                    return _sBuilder.ToString();

                _sBuilder.Append(c);
            }
        }
        public Vector3 ReadVector3D(bool SwapZY = true)
        {
            Vector3 Vector3D = new Vector3(ReadSingle(), ReadSingle(), ReadSingle());

            if (SwapZY)
            {
                float x = Vector3D.x;
                float y = Vector3D.y;
                float z = Vector3D.z;

                Vector3D.x = -y;
                Vector3D.y = z;
                Vector3D.z = x;
            }

            return Vector3D;
        }
        public Vector3 ReadVector2D()
        {
            Vector2 Vector2D = new Vector2(ReadSingle(), ReadSingle());

            return Vector2D;
        }
        public Vector4 ReadVector4()
        {
            return new Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        }
    }
}
