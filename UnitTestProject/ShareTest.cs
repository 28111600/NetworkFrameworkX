using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NetworkFrameworkX.Share;

namespace NetworkFrameworkX.UnitTestProject
{
    [TestClass]
    public class ShareTest
    {
        [TestMethod]
        public void PowTest()
        {
            Random random = new Random();
            int inputX = random.Next(-10, 10);
            int inputY = random.Next(-10, 10);
            double output = inputX.Pow(inputY);
            double value = Math.Pow(inputX, inputY);

            Assert.AreEqual(output, value);
        }

        [TestMethod]
        public void GetSizeStringTest()
        {
            long input = 32768;
            string output = Utility.GetSizeString(input);
            string value = "32.00 KB";

            Assert.AreEqual(output, value);

            input = 14193422336;
            output = Utility.GetSizeString(input);
            value = "13.22 GB";
            
            Assert.AreEqual(output, value);
        }
    }
}