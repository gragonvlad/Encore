using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Reflection;
using Trinity.Core;
using Trinity.Core.IO;
using Trinity.Core.Reflection;

namespace Trinity.Encore.Game.IO
{
    [ContractClass(typeof(ClientDbReaderContracts<>))]
    public abstract class ClientDbReader<T> : BinaryFileReader
        where T : class, IClientDbRecord, new()
    {
        protected ClientDbReader(string fileName)
            : base(fileName, System.Text.Encoding.UTF8)
        {
            Contract.Requires(!string.IsNullOrEmpty(fileName));

            StringTable = new Dictionary<int, string>();
            Entries = new Dictionary<int, T>();

            ReadFile();
        }

        [ContractInvariantMethod]
        private void Invariant()
        {
            Contract.Invariant(StringTable != null);
            Contract.Invariant(Entries != null);
        }

        public virtual StringReadMode StringReadMode
        {
            get { return StringReadMode.StringTable1; }
        }

        public virtual int? HeaderMagic
        {
            get { return null; }
        }

        public int RecordCount { get; protected set; }

        public int StringTableSize { get; protected set; }

        public Dictionary<int, string> StringTable { get; private set; }

        public Dictionary<int, T> Entries { get; private set; }

        protected override void Read(BinaryReader reader)
        {
            if (HeaderMagic != null)
                if (reader.ReadInt32() != HeaderMagic)
                    throw new ClientDbException("Invalid client DB header magic number.");

            var data = ReadData(reader);

            if (StringReadMode != StringReadMode.Direct)
                ReadStringTable(reader);

            MapRecords(data);
        }

        protected abstract byte[] ReadData(BinaryReader reader);

        private void MapRecords(byte[] data)
        {
            Contract.Requires(data != null);

            using (var reader = new BinaryReader(new MemoryStream(data), Encoding))
            {
                for (var i = 0; i < RecordCount; i++)
                {
                    var obj = new T();
                    ReadValuesToClass(obj, reader);
                    Entries.Add(obj.Id, obj);
                }
            }
        }

        private void ReadValuesToClass(object obj, BinaryReader reader)
        {
            Contract.Requires(obj != null);
            Contract.Requires(reader != null);

            foreach (var prop in obj.GetType().GetProperties(BindingFlags.Instance))
            {
                Contract.Assume(prop != null);

                if (prop.GetCustomAttribute<SkipPropertyAttribute>() != null)
                    continue; // Skip this property.

                var value = ReadValueToProperty(prop, reader);
                prop.SetValue(obj, value, null);
            }
        }

        private object ReadValueToProperty(PropertyInfo prop, BinaryReader reader)
        {
            Contract.Requires(prop != null);
            Contract.Requires(reader != null);
            Contract.Ensures(Contract.Result<object>() != null);

            var type = prop.PropertyType;

            if (type.IsEnum)
            {
                object value;
                var enumUnderlying = type.GetEnumUnderlyingType();

                switch (Type.GetTypeCode(enumUnderlying))
                {
                    case TypeCode.SByte:
                        value = Enum.ToObject(type, reader.ReadSByte());
                        break;
                    case TypeCode.Byte:
                        value = Enum.ToObject(type, reader.ReadByte());
                        break;
                    case TypeCode.Int16:
                        value = Enum.ToObject(type, reader.ReadInt16());
                        break;
                    case TypeCode.UInt16:
                        value = Enum.ToObject(type, reader.ReadUInt16());
                        break;
                    case TypeCode.Int32:
                        value = Enum.ToObject(type, reader.ReadInt32());
                        break;
                    case TypeCode.UInt32:
                        value = Enum.ToObject(type, reader.ReadUInt32());
                        break;
                    case TypeCode.Int64:
                        value = Enum.ToObject(type, reader.ReadInt64());
                        break;
                    case TypeCode.UInt64:
                        value = Enum.ToObject(type, reader.ReadUInt64());
                        break;
                    default:
                        throw new ClientDbException("Bad underlying enum type {0} encountered.".Interpolate(enumUnderlying));
                }

                Contract.Assume(value != null);
                return value;
            }

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Object:
                    var value = Activator.CreateInstance(type);
                    ReadValuesToClass(value, reader);
                    return value;
                case TypeCode.Boolean:
                    return reader.ReadBoolean();
                case TypeCode.SByte:
                    return reader.ReadSByte();
                case TypeCode.Byte:
                    return reader.ReadByte();
                case TypeCode.Int16:
                    return reader.ReadInt16();
                case TypeCode.UInt16:
                    return reader.ReadUInt16();
                case TypeCode.Int32:
                    return reader.ReadInt32();
                case TypeCode.UInt32:
                    return reader.ReadUInt32();
                case TypeCode.Int64:
                    return reader.ReadInt64();
                case TypeCode.UInt64:
                    return reader.ReadUInt64();
                case TypeCode.Single:
                    return reader.ReadSingle();
                case TypeCode.String:
                    var str = StringReadMode == StringReadMode.Direct ?
                        reader.ReadCString() : StringTable[reader.ReadInt32()];
                    Contract.Assume(str != null);
                    return str;
            }

            throw new ClientDbException("Unsupported field type {0} encountered.".Interpolate(type));
        }

        protected void ReadStringTable(BinaryReader reader)
        {
            Contract.Requires(reader != null);

            var stream = reader.BaseStream;
            var stringTableStart = stream.Position;
            var stringTableEnd = stringTableStart + StringTableSize;

            while (stream.Position != stringTableEnd)
            {
                var stringIndex = (int)(stream.Position - stringTableStart);

                if (StringReadMode == StringReadMode.StringTable2)
                    reader.ReadInt16(); // String length.

                StringTable[stringIndex] = reader.ReadCString();
            }
        }
    }

    [ContractClassFor(typeof(ClientDbReader<>))]
    public abstract class ClientDbReaderContracts<T> : ClientDbReader<T>
        where T : class, IClientDbRecord, new()
    {
        protected ClientDbReaderContracts(string fileName)
            : base(fileName)
        {
        }

        protected override byte[] ReadData(BinaryReader reader)
        {
            Contract.Requires(reader != null);
            Contract.Ensures(Contract.Result<byte[]>() != null);

            return null;
        }
    }
}