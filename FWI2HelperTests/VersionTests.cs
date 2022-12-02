using System;
using FWI2HelperTests.ArtikelData;

namespace FWI2HelperTests
{
    public class VersionTests
    {
        [Fact]
        public void TestVersions()
        {
            var fwiAsm = typeof(Utils).Assembly;

            Assert.Equal(fwiAsm.GetAssemblyVersion()?.ToString(), "1.0.1.0");
            Assert.Equal(fwiAsm.GetFileVersion(), "1.0.1");
            Assert.Equal(fwiAsm.GetInformalVersion(), "1.0.1");
        }
    }
}