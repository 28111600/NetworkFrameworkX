using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.UnitTestProject
{
    [TestClass]
    public class ShareCoreTest
    {
        [CollectionDataContract]
        private class TestSubObject : Dictionary<string, string>
        {
        }

        [DataContract]
        private class TestObject
        {
            [DataMember]
            public string String { get; set; }

            [DataMember]
            public int Int { get; set; }

            [DataMember]
            public long Long { get; set; }

            [DataMember]
            public byte[] Bytes { get; set; }

            [DataMember]
            public bool Bool { get; set; }

            [DataMember]
            public TestSubObject SubObject { get; set; }

            static public TestObject Generater()
            {
                TestObject result = new TestObject()
                {
                    String = "String",
                    Int = int.MaxValue,
                    Long = long.MaxValue,
                    Bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 },
                    Bool = true,
                    SubObject = new TestSubObject()
                };
                result.SubObject.Add("Key", "Value");

                return result;
            }

            static public bool IsEquals(TestObject a, TestObject b)
            {
                return a.String.Equals(b.String) && a.Int == b.Int && a.Long == b.Long && a.Bytes.SequenceEqual(b.Bytes) && a.Bool == b.Bool;
            }
        }

        [TestMethod]
        public void JsonSerialzationTest()
        {
            var serialzer = new JsonSerialzation();
            TestObject input = TestObject.Generater();
            TestObject output = serialzer.Deserialize<TestObject>(serialzer.Serialize(input));
            bool value = TestObject.IsEquals(input, output);

            Assert.IsTrue(value);
        }

        [TestMethod]
        public void DataContractJsonSerializerTest()
        {
            var serialzer = new DataContractJsonSerializer(typeof(TestObject));
            TestObject input = TestObject.Generater();

            using (MemoryStream msr = new MemoryStream()) {
                serialzer.WriteObject(msr, input);
                msr.Flush();
                msr.Seek(0, SeekOrigin.Begin);
                StreamReader sr = new StreamReader(msr);
                string s = sr.ReadToEnd();

                using (MemoryStream msw = new MemoryStream()) {
                    StreamWriter sw = new StreamWriter(msw);
                    sw.Write(s);
                    sw.Flush();
                    msw.Position = 0;
                    TestObject output = (TestObject)serialzer.ReadObject(msw);
                    bool value = TestObject.IsEquals(input, output);

                    Assert.IsTrue(value);
                }
            }
        }

        [TestMethod]
        public void MD5EncryptTest()
        {
            string input = "Hello World";
            string output = BitConverter.ToString(MD5.Encrypt(input.GetBytes()));
            string value = "b1-0a-8d-b1-64-e0-75-41-05-b7-a9-9b-e7-2e-3f-e5";

            Assert.AreEqual(output, value, true);
        }

        [TestMethod]
        public void SHA256EncryptTest()
        {
            string input = "Hello World";
            string output = BitConverter.ToString(SHA256.Encrypt(input.GetBytes()));
            string value = "a5-91-a6-d4-0b-f4-20-40-4a-01-17-33-cf-b7-b1-90-d6-2c-65-bf-0b-cd-a3-2b-57-b2-77-d9-ad-9f-14-6e";

            Assert.AreEqual(output, value, true);
        }

        [TestMethod]
        public void StreamTest()
        {
            string input = "Hello World";
            byte[] value = input.GetBytes();
            MemoryStream sr = new MemoryStream();
            StreamHelper stream = new StreamHelper(sr);

            const int SIZE_OF_BUFFER = 256;
            const int SIZE_OF_HEAD = 4;

            int times_write = 200;
            int times_read = times_write * (value.Length + SIZE_OF_HEAD) / SIZE_OF_BUFFER + 1;

            for (int i = 0; i < times_write; i++) {
                stream.Write(input.GetBytes());
            }

            sr.Flush();
            sr.Position = 0;
            int t = 0;
            stream.EndRead += (x, y) => { Assert.IsTrue(y.Data.SequenceEqual(value)); Trace.WriteLine(y.Data.GetString(), t++.ToString()); };

            try {
                for (int i = 0; i < times_read; i++) {
                    stream.Read();
                }
            } catch (Exception ex) {
                Assert.Fail(ex.Message);
            }
        }
    }
}