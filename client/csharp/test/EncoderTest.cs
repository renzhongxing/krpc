using System;
using System.Collections.Generic;
using System.Linq;
using Moq;
using NUnit.Framework;

namespace KRPC.Client.Test
{
    [TestFixture]
    public class EncoderTest
    {
        [Test]
        public void EncodeMessage ()
        {
            var call = new KRPC.Schema.KRPC.ProcedureCall ();
            call.Service = "ServiceName";
            call.Procedure = "ProcedureName";
            var data = Encoder.Encode (call, typeof(KRPC.Schema.KRPC.ProcedureCall));
            const string expected = "0a0b536572766963654e616d65120d50726f6365647572654e616d65";
            Assert.AreEqual (expected, data.ToHexString ());
        }

        [Test]
        public void EncodeValue ()
        {
            var data = Encoder.Encode (300u, typeof(uint));
            Assert.AreEqual ("ac02", data.ToHexString ());
        }

        [Test]
        public void EncodeUnicodeString ()
        {
            var data = Encoder.Encode ("\u2122", typeof(string));
            Assert.AreEqual ("03e284a2", data.ToHexString ());
        }

        [Test]
        public void EncodeRemoteObject ()
        {
            var mockClient = new Mock<IConnection> ();
            var obj = new Services.SpaceCenter.Vessel (mockClient.Object, 300);
            Assert.AreEqual (300, obj.id);
            Assert.AreSame (mockClient.Object, obj.connection);
            var data = Encoder.Encode (obj, typeof(Services.SpaceCenter.Vessel));
            Assert.AreEqual ("ac02", data.ToHexString ());
        }

        [Test]
        public void EncodeNullRemoteObject ()
        {
            var data = Encoder.Encode (null, typeof(Services.SpaceCenter.Vessel));
            Assert.AreEqual ("00", data.ToHexString ());
        }

        [Test]
        public void DecodeMessage ()
        {
            var message = "0a0b536572766963654e616d65120d50726f6365647572654e616d65".ToByteString ();
            var call = (KRPC.Schema.KRPC.ProcedureCall)Encoder.Decode (message, typeof(KRPC.Schema.KRPC.ProcedureCall), null);
            Assert.AreEqual ("ServiceName", call.Service);
            Assert.AreEqual ("ProcedureName", call.Procedure);
        }

        [Test]
        public void DecodeValue ()
        {
            var value = (uint)Encoder.Decode ("ac02".ToByteString (), typeof(uint), null);
            Assert.AreEqual (300, value);
        }

        [Test]
        public void DecodeUnicodeString ()
        {
            var value = (string)Encoder.Decode ("03e284a2".ToByteString (), typeof(string), null);
            Assert.AreEqual ("\u2122", value);
        }

        [Test]
        public void DecodeRemoteObject ()
        {
            var mockClient = new Mock<IConnection> ();
            var value = (Services.SpaceCenter.Vessel)Encoder.Decode ("ac02".ToByteString (), typeof(Services.SpaceCenter.Vessel), mockClient.Object);
            Assert.AreEqual (300, value.id);
            Assert.AreSame (mockClient.Object, value.connection);
        }

        [Test]
        public void DecodeNullRemoteObject ()
        {
            var mockClient = new Mock<IConnection> ();
            var value = (string)Encoder.Decode ("00".ToByteString (), typeof(Services.SpaceCenter.Vessel), mockClient.Object);
            Assert.IsNull (value);
        }

        [TestCase (3.14159265359f, "db0f4940")]
        [TestCase (-1.0f, "000080bf")]
        [TestCase (0.0f, "00000000")]
        [TestCase (float.PositiveInfinity, "0000807f")]
        [TestCase (float.NegativeInfinity, "000080ff")]
        [TestCase (float.NaN, "0000c0ff")]
        public void SingleValue (float value, string data)
        {
            var encodeResult = Encoder.Encode (value, typeof(float));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (float)Encoder.Decode (data.ToByteString (), typeof(float), null);
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase (0.0, "0000000000000000")]
        [TestCase (-1.0, "000000000000f0bf")]
        [TestCase (3.14159265359, "ea2e4454fb210940")]
        [TestCase (double.PositiveInfinity, "000000000000f07f")]
        [TestCase (double.NegativeInfinity, "000000000000f0ff")]
        [TestCase (double.NaN, "000000000000f8ff")]
        public void DoubleValue (double value, string data)
        {
            var encodeResult = Encoder.Encode (value, typeof(double));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (double)Encoder.Decode (data.ToByteString (), typeof(double), null);
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase (0, "00")]
        [TestCase (1, "02")]
        [TestCase (42, "54")]
        [TestCase (300, "d804")]
        [TestCase (-33, "41")]
        [TestCase (2147483647, "feffffff0f")]
        [TestCase (-2147483648, "ffffffff0f")]
        public void Int32Value (int value, string data)
        {
            var encodeResult = Encoder.Encode (value, typeof(int));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (int)Encoder.Decode (data.ToByteString (), typeof(int), null);
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase (0, "00")]
        [TestCase (1, "02")]
        [TestCase (42, "54")]
        [TestCase (300, "d804")]
        [TestCase (1234567890000L, "a091d89fee47")]
        [TestCase (-33, "41")]
        public void Int64Value (long value, string data)
        {
            var encodeResult = Encoder.Encode (value, typeof(long));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (long)Encoder.Decode (data.ToByteString (), typeof(long), null);
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase (0u, "00")]
        [TestCase (1u, "01")]
        [TestCase (42u, "2a")]
        [TestCase (300u, "ac02")]
        [TestCase (uint.MaxValue, "ffffffff0f")]
        public void UInt32Value (uint value, string data)
        {
            var encodeResult = Encoder.Encode (value, typeof(uint));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (uint)Encoder.Decode (data.ToByteString (), typeof(uint), null);
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase (-1)]
        [TestCase (-849)]
        public void InvalidUInt32Value (int value)
        {
            Assert.Throws<ArgumentException> (() => Encoder.Encode (value, typeof(uint)));
        }

        [TestCase (0u, "00")]
        [TestCase (1u, "01")]
        [TestCase (42u, "2a")]
        [TestCase (300u, "ac02")]
        [TestCase (1234567890000ul, "d088ec8ff723")]
        [TestCase (ulong.MaxValue, "ffffffffffffffffff01")]
        public void UInt64Value (ulong value, string data)
        {
            var encodeResult = Encoder.Encode (value, typeof(ulong));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (ulong)Encoder.Decode (data.ToByteString (), typeof(ulong), null);
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase (-1)]
        [TestCase (-849)]
        public void InvalidUInt64Value (int value)
        {
            Assert.Throws<ArgumentException> (() => Encoder.Encode (value, typeof(ulong)));
        }

        [TestCase (true, "01")]
        [TestCase (false, "00")]
        public void BooleanValue (bool value, string data)
        {
            var encodeResult = Encoder.Encode (value, typeof(bool));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (bool)Encoder.Decode (data.ToByteString (), typeof(bool), null);
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase ("", "00")]
        [TestCase ("testing", "0774657374696e67")]
        [TestCase ("One small step for Kerbal-kind!", "1f4f6e6520736d616c6c207374657020666f72204b657262616c2d6b696e6421")]
        [TestCase ("\u2122", "03e284a2")]
        [TestCase ("Mystery Goo\u2122 Containment Unit", "1f4d79737465727920476f6fe284a220436f6e7461696e6d656e7420556e6974")]
        public void StringValue (string value, string data)
        {
            var encodeResult = Encoder.Encode (value, typeof(string));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (string)Encoder.Decode (data.ToByteString (), typeof(string), null);
            Assert.AreEqual (value, decodeResult);
        }

        [TestCase ("", "00")]
        [TestCase ("bada55", "03bada55")]
        [TestCase ("deadbeef", "04deadbeef")]
        public void BytesValue (string value, string data)
        {
            var encodeResult = Encoder.Encode (value.ToByteString ().ToByteArray (), typeof(byte[]));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (byte[])Encoder.Decode (data.ToByteString (), typeof(byte[]), null);
            Assert.AreEqual (value.ToByteString (), decodeResult);
        }

        [TestCase (new uint[] { }, "")]
        [TestCase (new uint[] { 1 }, "0a0101")]
        [TestCase (new uint[] { 1, 2, 3, 4 }, "0a01010a01020a01030a0104")]
        public void ListCollection (IList<uint> values, string data)
        {
            IList<uint> value = new List<uint> (values);
            var encodeResult = Encoder.Encode (value, typeof(IList<uint>));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (IList<uint>)Encoder.Decode (data.ToByteString (), typeof(IList<uint>), null);
            CollectionAssert.AreEqual (value, decodeResult);
        }

        [TestCase (new string[] { }, new uint[]{ }, "")]
        [TestCase (new [] { "" }, new uint[]{ 0 }, "0a060a0100120100")]
        [TestCase (new [] { "foo", "bar", "baz" }, new uint[]{ 42, 365, 3 }, "0a090a0403666f6f12012a0a0a0a04036261721202ed020a090a040362617a120103")]
        public void DictionaryCollection (IList<string> keys, IList<uint> values, string data)
        {
            IDictionary<string,uint> value = new Dictionary<string,uint> ();
            for (int i = 0; i < keys.Count; i++)
                value [keys [i]] = values [i];
            var encodeResult = Encoder.Encode (value, typeof(IDictionary<string,uint>));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (IDictionary<string,uint>)Encoder.Decode (data.ToByteString (), typeof(IDictionary<string,uint>), null);
            CollectionAssert.AreEqual (value, decodeResult);
        }

        [TestCase (new uint[] { }, "")]
        [TestCase (new uint[] { 1 }, "0a0101")]
        [TestCase (new uint[] { 1, 2, 3, 4 }, "0a01010a01020a01030a0104")]
        public void SetCollection (IList<uint> values, string data)
        {
            ISet<uint> value = new HashSet<uint> (values);
            var encodeResult = Encoder.Encode (value, typeof(ISet<uint>));
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (ISet<uint>)Encoder.Decode (data.ToByteString (), typeof(ISet<uint>), null);
            CollectionAssert.AreEqual (value, decodeResult);
        }

        [Test]
        public void TupleCollection1 ()
        {
            var value = new Tuple<uint> (1);
            const string data = "0a0101";
            var encodeResult = Encoder.Encode (value, value.GetType ());
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (Tuple<uint>)Encoder.Decode (data.ToByteString (), value.GetType (), null);
            Assert.AreEqual (value, decodeResult);
        }

        [Test]
        public void TupleCollection2 ()
        {
            var value = new Tuple<uint,string,bool> (1, "jeb", false);
            const string data = "0a01010a04036a65620a0100";
            var encodeResult = Encoder.Encode (value, value.GetType ());
            Assert.AreEqual (data, encodeResult.ToHexString ());
            var decodeResult = (Tuple<uint,string,bool>)Encoder.Decode (data.ToByteString (), value.GetType (), null);
            Assert.AreEqual (value, decodeResult);
        }
    }
}
