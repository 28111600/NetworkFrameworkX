using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.UnitTestProject
{
    [TestClass]
    public class ServerTest
    {
        [TestMethod]
        public void MessageSerializeTest()
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            JsonSerialzation jsonSerialzation = new JsonSerialzation();
            AESKey key = AESKey.Generate();

            CallBody call = new CallBody() { Call = "Test", Args = new Arguments() };

            call.Args.Put("arg1", 1);
            call.Args.Put("arg2", true);
            call.Args.Put("arg3", Guid.NewGuid().ToString());
            call.Args.Put("arg4", true);

            long start = sw.ElapsedTicks;

            const int COUNT = 4;
            const int TIMES = 100;

            MessageBody[] input = new MessageBody[COUNT];
            byte[][] data = new byte[COUNT][];
            MessageBody[] output = new MessageBody[COUNT];
            string[] name = new string[COUNT];

            long[,] tick = new long[COUNT, 2];
            for (int i = 0; i < TIMES; i++) {
                // AES
                name[0] = "AES";
                input[0] = new MessageBody()
                {
                    Flag = MessageFlag.Message,
                    Guid = Guid.NewGuid().ToString(),
                    Content = AESHelper.Encrypt(jsonSerialzation.Serialize(call), key)
                };

                data[0] = jsonSerialzation.Serialize(input[0]).GetBytes();
                tick[0, 0] += sw.ElapsedTicks - start; start = sw.ElapsedTicks;

                output[0] = jsonSerialzation.Deserialize<MessageBody>(data[0].GetString());

                Assert.IsTrue(AESHelper.Decrypt(output[0].Content, key).SequenceEqual(jsonSerialzation.Serialize(call).GetBytes()));
                tick[0, 1] += sw.ElapsedTicks - start; start = sw.ElapsedTicks;

                // GZIP + AES
                name[1] = "GZIP + AES";
                input[1] = new MessageBody()
                {
                    Flag = MessageFlag.Message,
                    Guid = Guid.NewGuid().ToString(),
                    Content = AESHelper.Encrypt(GZip.Compress(jsonSerialzation.Serialize(call).GetBytes()), key)
                };

                data[1] = jsonSerialzation.Serialize(input[1]).GetBytes();
                tick[1, 0] += sw.ElapsedTicks - start; start = sw.ElapsedTicks;

                output[1] = jsonSerialzation.Deserialize<MessageBody>(data[1].GetString());

                Assert.IsTrue(GZip.Decompress(AESHelper.Decrypt(output[1].Content, key)).SequenceEqual(jsonSerialzation.Serialize(call).GetBytes()));
                tick[1, 1] += sw.ElapsedTicks - start; start = sw.ElapsedTicks;

                // GZIP + AES + GZIP
                name[2] = "GZIP + AES + GZIP";
                input[2] = new MessageBody()
                {
                    Flag = MessageFlag.Message,
                    Guid = Guid.NewGuid().ToString(),
                    Content = AESHelper.Encrypt(GZip.Compress(jsonSerialzation.Serialize(call).GetBytes()), key)
                };

                data[2] = GZip.Compress(jsonSerialzation.Serialize(input[2]).GetBytes());
                tick[2, 0] += sw.ElapsedTicks - start; start = sw.ElapsedTicks;

                output[2] = jsonSerialzation.Deserialize<MessageBody>(GZip.Decompress(data[2]).GetString());

                Assert.IsTrue(GZip.Decompress(AESHelper.Decrypt(output[2].Content, key)).SequenceEqual(jsonSerialzation.Serialize(call).GetBytes()));
                tick[2, 1] += sw.ElapsedTicks - start; start = sw.ElapsedTicks;

                // AES + GZIP
                name[3] = "AES + GZIP";
                input[3] = new MessageBody()
                {
                    Flag = MessageFlag.Message,
                    Guid = Guid.NewGuid().ToString(),
                    Content = AESHelper.Encrypt(jsonSerialzation.Serialize(call), key)
                };

                data[3] = GZip.Compress(jsonSerialzation.Serialize(input[3]).GetBytes());
                tick[3, 0] += sw.ElapsedTicks - start; start = sw.ElapsedTicks;

                output[3] = jsonSerialzation.Deserialize<MessageBody>(GZip.Decompress(data[3]).GetString());

                Assert.IsTrue(AESHelper.Decrypt(output[3].Content, key).SequenceEqual(jsonSerialzation.Serialize(call).GetBytes()));
                tick[3, 1] += sw.ElapsedTicks - start; start = sw.ElapsedTicks;
            }

            for (int i = 0; i < COUNT; i++) {
                Trace.WriteLine($"{name[i]}  : E {tick[i, 0] / TIMES / 1000.0:0.00}0ms, D {tick[i, 1] / TIMES / 1000.0:0.00}ms, Size: { data[i].Length}");
            }
        }
    }
}