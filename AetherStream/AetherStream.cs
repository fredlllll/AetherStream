using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AetherStream
{
    public class AetherStream : IDisposable
    {
        private Stream stream;
        private BinaryReader reader;
        private BinaryWriter writer;

        public Stream Stream { get { return stream; } }

        public BinaryReader Reader { get { return reader; } }

        public BinaryWriter Writer { get { return writer; } }

        public AetherStream(Stream stream)
        {
            this.stream = stream;
            this.reader = new BinaryReader(stream, Encoding.UTF8, true);
            this.writer = new BinaryWriter(stream, Encoding.UTF8, true);
        }

        #region Write

        public void WriteDTYPE(AetherDataType dtype)
        {
            writer.Write((byte)dtype);
        }

        public void WriteSize(long size)
        {
            //taken from net7s Write7BitEncodedInt64

            ulong uValue = (ulong)size;

            while (uValue > 0x7Fu)
            {
                writer.Write((byte)((uint)uValue | ~0x7Fu));
                uValue >>= 7;
            }

            writer.Write((byte)uValue);
        }

        public void WriteSByte(sbyte value)
        {
            WriteDTYPE(AetherDataType.INT8);
            writer.Write(value);
        }

        public void WriteShort(short value)
        {
            WriteDTYPE(AetherDataType.INT16);
            writer.Write(value);
        }

        public void WriteInt(int value)
        {
            WriteDTYPE(AetherDataType.INT32);
            writer.Write(value);
        }

        public void WriteLong(long value)
        {
            WriteDTYPE(AetherDataType.INT64);
            writer.Write(value);
        }

        public void WriteByte(byte value)
        {
            WriteDTYPE(AetherDataType.UINT8);
            writer.Write(value);
        }

        public void WriteUShort(ushort value)
        {
            WriteDTYPE(AetherDataType.UINT16);
            writer.Write(value);
        }

        public void WriteUInt(uint value)
        {
            WriteDTYPE(AetherDataType.UINT32);
            writer.Write(value);
        }

        public void WriteULong(ulong value)
        {
            WriteDTYPE(AetherDataType.UINT64);
            writer.Write(value);
        }

        public void WriteFloat(float value)
        {
            WriteDTYPE(AetherDataType.FLOAT32);
            writer.Write(value);
        }

        public void WriteDouble(double value)
        {
            WriteDTYPE(AetherDataType.FLOAT64);
            writer.Write(value);
        }

        public void WriteBool(bool value)
        {
            WriteDTYPE(value ? AetherDataType.TRUE : AetherDataType.FALSE);
        }

        public void WriteString(string value)
        {
            WriteDTYPE(AetherDataType.STRING);
            var bytes = Encoding.UTF8.GetBytes(value);
            WriteSize(bytes.Length);
            writer.Write(bytes);
        }

        public void WriteBytes(byte[] value)
        {
            WriteDTYPE(AetherDataType.BYTES);
            WriteSize(value.Length);
            writer.Write(value);
        }

        public void WriteList<T>(List<T> value)
        {
            WriteDTYPE(AetherDataType.LIST);
            WriteSize(value.Count);
            foreach (var item in value)
            {
                Write(item);
            }
        }

        public void WriteList(System.Collections.IList value)
        {
            WriteDTYPE(AetherDataType.LIST);
            WriteSize(value.Count);
            foreach (var item in value)
            {
                Write(item);
            }
        }

        public void WriteDict<T, U>(Dictionary<T, U> value) where T : notnull
        {
            WriteDTYPE(AetherDataType.DICTIONARY);
            WriteSize(value.Count);
            foreach (var kv in value)
            {
                Write(kv.Key);
                Write(kv.Value);
            }
        }

        public void WriteDict(System.Collections.IDictionary value)
        {
            WriteDTYPE(AetherDataType.DICTIONARY);
            WriteSize(value.Count);
            foreach (var key in value.Keys)
            {
                Write(key);
                Write(value[key]);
            }
        }

        public void Write(object? value)
        {
            if (value == null)
            {
                WriteDTYPE(AetherDataType.NULL);
            }
            else if (value is byte b)
            {
                WriteByte(b);
            }
            else if (value is short s)
            {
                WriteShort(s);
            }
            else if (value is int i)
            {
                WriteInt(i);
            }
            else if (value is long l)
            {
                WriteLong(l);
            }
            else if (value is sbyte sb)
            {
                WriteSByte(sb);
            }
            else if (value is ushort us)
            {
                WriteUShort(us);
            }
            else if (value is uint ui)
            {
                WriteUInt(ui);
            }
            else if (value is ulong ul)
            {
                WriteULong(ul);
            }
            else if (value is float f)
            {
                WriteFloat(f);
            }
            else if (value is double d)
            {
                WriteDouble(d);
            }
            else if (value is bool bo)
            {
                WriteBool(bo);
            }
            else if (value is string str)
            {
                WriteString(str);
            }
            else if (value is byte[] bytes)
            {
                WriteBytes(bytes);
            }
            else if (value is System.Collections.IDictionary dict)
            {
                WriteDict(dict);
            }
            else if (value is System.Collections.IList list)
            {
                WriteList(list);
            }
            else
            {
                throw new NotImplementedException("unsupported type " + value.GetType());
            }
        }

        #endregion
        #region Read
        public AetherDataType ReadDTYPE()
        {
            return (AetherDataType)reader.ReadByte();
        }

        public long ReadSize()
        {
            //taken from net7s Read7BitEncodedInt64

            ulong result = 0;
            byte byteReadJustNow;

            // Read the integer 7 bits at a time. The high bit
            // of the byte when on means to continue reading more bytes.
            //
            // There are two failure cases: we've read more than 10 bytes,
            // or the tenth byte is about to cause integer overflow.
            // This means that we can read the first 9 bytes without
            // worrying about integer overflow.

            const int MaxBytesWithoutOverflow = 9;
            for (int shift = 0; shift < MaxBytesWithoutOverflow * 7; shift += 7)
            {
                // ReadByte handles end of stream cases for us.
                byteReadJustNow = reader.ReadByte();
                result |= (byteReadJustNow & 0x7Ful) << shift;

                if (byteReadJustNow <= 0x7Fu)
                {
                    return (long)result; // early exit
                }
            }

            // Read the 10th byte. Since we already read 63 bits,
            // the value of this byte must fit within 1 bit (64 - 63),
            // and it must not have the high bit set.

            byteReadJustNow = reader.ReadByte();
            if (byteReadJustNow > 0b_1u)
            {
                throw new FormatException("SR.Format_Bad7BitInt");
            }

            result |= (ulong)byteReadJustNow << (MaxBytesWithoutOverflow * 7);
            return (long)result;
        }

        public T ReadNext<T>()
        {
#pragma warning disable CS8603 // Possible null reference return.
            return (T)ReadNext();
#pragma warning restore CS8603 // Possible null reference return.

            /*var dtype = ReadDTYPE();
            if (dtype == DTYPE.NULL)
            {
                return (T)(object)null; //TODO: check if it can actually be set to null
            }
            if (typeof(T).Equals(typeof(bool)))
            {
                if (dtype == DTYPE.TRUE)
                {
                    return (T)(object)true;
                }
                else if (dtype == DTYPE.FALSE)
                {
                    return (T)(object)false;
                }
                else
                {
                    throw new InvalidDataException("expecting bool but got " + dtype);
                }
            }
            if (typeToDtype.TryGetValue(typeof(T), out var expectedType))
            {
                if (expectedType != dtype)
                {
                    throw new InvalidDataException("Expected " + expectedType + " but got " + dtype);
                }
            }
            return (T)ReadByDtype(dtype);*/
        }

        public object? ReadByDtype(AetherDataType dtype)
        {
            switch (dtype)
            {
                case AetherDataType.NULL:
                    return null;
                case AetherDataType.INT8:
                    return reader.ReadSByte();
                case AetherDataType.INT16:
                    return reader.ReadInt16();
                case AetherDataType.INT32:
                    return reader.ReadInt32();
                case AetherDataType.INT64:
                    return reader.ReadInt64();
                case AetherDataType.UINT8:
                    return reader.ReadByte();
                case AetherDataType.UINT16:
                    return reader.ReadUInt16();
                case AetherDataType.UINT32:
                    return reader.ReadUInt32();
                case AetherDataType.UINT64:
                    return reader.ReadUInt64();
                case AetherDataType.FLOAT32:
                    return reader.ReadSingle();
                case AetherDataType.FLOAT64:
                    return reader.ReadDouble();
                case AetherDataType.TRUE:
                    return true;
                case AetherDataType.FALSE:
                    return false;
                case AetherDataType.STRING:
                    var strlen = ReadSize();
                    var strBytes = reader.ReadBytes((int)strlen);
                    return Encoding.UTF8.GetString(strBytes);
                case AetherDataType.BYTES:
                    var byteslen = ReadSize();
                    var bytes = reader.ReadBytes((int)byteslen);
                    return bytes;
                case AetherDataType.DICTIONARY:
                    throw new NotImplementedException("use seperate function for dictionary");
                case AetherDataType.LIST:
                    throw new NotImplementedException("use seperate function for list");
                default:
                    throw new NotImplementedException("unknown dtype: " + dtype);
            }
            throw new Exception("how did you get here?");
        }

        public Dictionary<T, U> ReadNextDictionary<T, U>()
        {
            var dtype = ReadDTYPE();
            if (dtype != AetherDataType.DICTIONARY)
            {
                throw new Exception($"expected {AetherDataType.DICTIONARY} but got {dtype}");
            }

            var dict = new Dictionary<T, U>();
            long size = ReadSize();
            for (long i = 0; i < size; i++)
            {
                var key = ReadNext<T>();
                var value = ReadNext<U>();
                dict.Add(key, value);
            }

            return dict;
        }

        public List<T> ReadNextList<T>()
        {
            var dtype = ReadDTYPE();
            if (dtype != AetherDataType.LIST)
            {
                throw new Exception($"expected {AetherDataType.LIST} but got {dtype}");
            }

            var list = new List<T>();
            long size = ReadSize();
            for (long i = 0; i < size; i++)
            {
                list.Add(ReadNext<T>());
            }
            return list;
        }

        public object? ReadNext()
        {
            var dtype = ReadDTYPE();
            return ReadByDtype(dtype);
        }

        #endregion
        #region Dispose
        ~AetherStream()
        {
            Dispose(disposing: false);
        }
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        private bool disposed = false;
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    //leave stream open
                    //stream?.Dispose();
                    reader.Dispose();
                    writer.Dispose();
                }
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
                stream = null;
                reader = null;
                writer = null;
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
                disposed = true;
            }
        }
        #endregion
    }
}
