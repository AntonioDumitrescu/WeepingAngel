using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using Transport;

namespace PacketReaderWriterTests
{
    public class Tests
    {
        private enum TestEnum
        {
            One,
            Two
        }
     
        [Test]
        public void TestRoundTrip()
        {
            using var writer = new PacketWriter();
        
            writer.WriteByte(byte.MaxValue);
            writer.WriteByte(byte.MinValue);
            writer.WriteByte(0);
            
            writer.WriteBool(true);
            writer.WriteBool(false);

            writer.WriteShort(short.MaxValue);
            writer.WriteShort(short.MinValue);
            writer.WriteShort(0);
            writer.WriteUShort(0);
            writer.WriteUShort(ushort.MaxValue);

            writer.WriteInt(int.MaxValue);
            writer.WriteInt(int.MinValue);
            writer.WriteInt(0);
            writer.WriteUInt(0);
            writer.WriteUInt(uint.MaxValue);

            writer.WriteLong(long.MaxValue);
            writer.WriteLong(long.MinValue);
            writer.WriteLong(0);
            writer.WriteULong(0);
            writer.WriteULong(ulong.MaxValue);

            var str = Encoding.UTF8.GetString(RandomNumberGenerator.GetBytes(1024));
            writer.WriteString(str);

            var bytes = RandomNumberGenerator.GetBytes(512);
            writer.WriteBytes(bytes);

            for (var i = -1024 * 1024; i < 1024 * 1024; i++)
            {
                writer.WritePackedInt(i);
            }

            writer.WriteNullableBytes(null);
            writer.WriteNullableBytes(new byte[]{0, 1, 2});
            writer.WriteNullableString(null);
            writer.WriteNullableString("str");

            writer.WriteEnum(TestEnum.Two);
            writer.WriteEnum(TestEnum.One);

            var reader = new PacketReader(writer.Compile(), writer.Length);
            
            Assert.IsTrue(reader.ReadByte() == byte.MaxValue);
            Assert.IsTrue(reader.ReadByte() == byte.MinValue);
            Assert.IsTrue(reader.ReadByte() == 0);

            Assert.IsTrue(reader.ReadBool());
            Assert.IsFalse(reader.ReadBool());

            Assert.IsTrue(reader.ReadShort() == short.MaxValue);
            Assert.IsTrue(reader.ReadShort() == short.MinValue);
            Assert.IsTrue(reader.ReadShort() == 0);
            Assert.IsTrue(reader.ReadUShort() == 0);
            Assert.IsTrue(reader.ReadUShort() == ushort.MaxValue);

            Assert.IsTrue(reader.ReadInt() == int.MaxValue);
            Assert.IsTrue(reader.ReadInt() == int.MinValue);
            Assert.IsTrue(reader.ReadInt() == 0);
            Assert.IsTrue(reader.ReadUInt() == 0);
            Assert.IsTrue(reader.ReadUInt() == uint.MaxValue);

            Assert.IsTrue(reader.ReadLong() == long.MaxValue);
            Assert.IsTrue(reader.ReadLong() == long.MinValue);
            Assert.IsTrue(reader.ReadLong() == 0);
            Assert.IsTrue(reader.ReadULong() == 0);
            Assert.IsTrue(reader.ReadULong() == ulong.MaxValue);

            Assert.IsTrue(reader.ReadString().Equals(str));
            Assert.IsTrue(reader.ReadBytes().SequenceEqual(bytes));

            for (var i = -1024 * 1024; i < 1024 * 1024; i++)
            {
                Assert.IsTrue(reader.ReadPackedInt() == i);
            }

            Assert.IsTrue(reader.ReadNullableBytes() == null);
            Assert.IsTrue((reader.ReadNullableBytes() ?? throw new NullReferenceException()).SequenceEqual(new byte[]{0, 1, 2}));
            Assert.IsTrue(reader.ReadNullableString() == null);
            Assert.IsTrue((reader.ReadNullableString() ?? throw new NullReferenceException()).Equals("str"));
            Assert.IsTrue(reader.ReadEnum<TestEnum>() == TestEnum.Two);
            Assert.IsTrue(reader.ReadEnum<TestEnum>() == TestEnum.One);
            Assert.IsTrue(reader.Remaining == 0);
        }
    }
}