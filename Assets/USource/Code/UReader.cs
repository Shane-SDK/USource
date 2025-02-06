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
        public void ReadSourceObjectArray<T>(ref T[] array, long position, int version = 0) where T : struct, ISourceObject
        {
            InputStream.Position = position;
            ReadSourceObjectArray(ref array, version);
        }
        public void ReadSourceObjectArray<T>(ref T[] array, int version = 0) where T : struct, ISourceObject
        {
            for (int i = 0;  i < array.Length; i++)
            {
                array[i].ReadToObject(this, version);
            }
        }
        public T ReadSourceObject<T>(int version = 0) where T : ISourceObject, new()
        {
            T instance = new();
            instance.ReadToObject(this, version);
            return instance;
        }
        public void ReadType<T>(ref T type, long? position = null) where T : struct, ISourceObject
        {
            if (position.HasValue)
                BaseStream.Position = position.Value;
            type = ReadSourceObject<T>();
        }
        public void ReadArray<T>(ref T[] array, long? position = null) where T : struct, ISourceObject
        {
            if (position.HasValue)
                BaseStream.Position = position.Value;
            ReadSourceObjectArray<T>(ref array);
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
        public Vector3 ReadVector2()
        {
            Vector2 Vector2D = new Vector2(ReadSingle(), ReadSingle());

            return Vector2D;
        }
        public Vector3 ReadVector3()
        {
            Vector3 Vector3D = new Vector3(ReadSingle(), ReadSingle(), ReadSingle());

            return Vector3D;
        }
        public Vector4 ReadVector4()
        {
            return new Vector4(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        }
        public Quaternion ReadQuaternion()
        {
            return new Quaternion(ReadSingle(), ReadSingle(), ReadSingle(), ReadSingle());
        }
        public void ReadVector3Array(Vector3[] array)
        {
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = ReadVector3();
            }
        }
        public void Skip(int count)
        {
            BaseStream.Position += count;
        }
    }
}
