using System;
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

            static public TestObject Generater()
            {
                TestObject result = new TestObject()
                {
                    String = "String",
                    Int = int.MaxValue,
                    Long = long.MaxValue,
                    Bytes = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0 }
                };

                return result;
            }

            static public bool IsEquals(TestObject a, TestObject b)
            {
                return a.String.Equals(b.String) && a.Int == b.Int && a.Long == b.Long && Enumerable.SequenceEqual(a.Bytes, b.Bytes);
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
        public void StreamTest()
        {
            string input = "Hello World";
            byte[] value = input.GetBytes();

            MemoryStream sr = new MemoryStream();
            StreamHelper stream = new StreamHelper(sr);

            for (int i = 0; i < 10; i++) {
                stream.Write(input.GetBytes());
            }

            sr.Flush();
            sr.Position = 0;

            try {
                stream.Read((output) => {
                    Assert.IsTrue(Enumerable.SequenceEqual(output, value));
                });
            } catch (Exception ex) {
                Assert.Fail(ex.Message);
            }
        }
    }
}