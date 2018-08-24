using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.UnitTestProject
{
    [TestClass]
    public class ShareCoreTest
    {
        [TestMethod]
        public void JsonSerialzationTest()
        {
            var jsonSerialzation = new JsonSerialzation();
            byte[] input = Guid.NewGuid().ToByteArray();
            byte[] output = jsonSerialzation.Deserialize<byte[]>(jsonSerialzation.Serialize(input));
            bool value = Enumerable.SequenceEqual(input, output);

            Assert.IsTrue(value);
        }

        [TestMethod]
        public void MD5EncryptTest()
        {
            string input = "Hello World";
            string output = BitConverter.ToString(MD5.Encrypt(input.GetBytes()));
            string value = "b1-0a-8d-b1-64-e0-75-41-05-b7-a9-9b-e7-2e-3f-e5";

            Assert.AreEqual(output, value, true);
        }
    }
}