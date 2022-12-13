using System;

namespace FWI2HelperTests
{
    public class VersionTests
    {
        [Fact]
        public void TestVersions()
        {
            var fwiAsm = typeof(Utils).Assembly;

            Assert.Equal("1.0.2.0", fwiAsm.GetAssemblyVersion()?.ToString());
            Assert.Equal("1.0.2", fwiAsm.GetFileVersion());
            Assert.Equal("1.0.2", fwiAsm.GetInformalVersion());
        }
    }
}