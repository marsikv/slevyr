using System;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Slevyr.DataAccess.Services;

namespace UnitTest
{
    [TestClass]
    public class UnitTestHelper
    {
        [TestMethod]
        public void TestLsbMsbConversion()
        {
            short v = 256;
            byte lsb, msb;
            Helper.FromShort(v,out lsb,out msb);
            System.Console.WriteLine($"lsb={lsb} msb={msb}");
            short vv = Helper.ToShort(lsb, msb);
            System.Console.WriteLine($"value={vv}");
            Assert.AreEqual(v,vv);

            v = short.MaxValue;
            Helper.FromShort(v, out lsb, out msb);
            System.Console.WriteLine($"lsb={lsb} msb={msb}");
            vv = Helper.ToShort(lsb, msb);
            System.Console.WriteLine($"value={vv}");
            Assert.AreEqual(v, vv);


            v = short.MinValue;
            Helper.FromShort(v, out lsb, out msb);
            System.Console.WriteLine($"lsb={lsb} msb={msb}");
            vv = Helper.ToShort(lsb, msb);
            System.Console.WriteLine($"value={vv}");
            Assert.AreEqual(v, vv);
        }
    }
}
