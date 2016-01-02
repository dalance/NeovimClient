using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeovimClient;
using System.Diagnostics;

namespace Test {
    [TestClass]
    public class TestNeovimSocket {

        // This test only works with Mono on UNIX environment
        /*

        private static NeovimClient<NeovimSocket> nvim;
        private static Process proc;

        [ClassInitialize]
        public static void ClassInit( TestContext context ) {
            var info = new ProcessStartInfo( "nvim", "--embed --headless" );
            info.CreateNoWindow  = true;
            info.UseShellExecute = false;
            info.EnvironmentVariables.Add( "NVIM_LISTEN_ADDRESS", "/tmp/nvim" );

            proc = new Process();
            proc.StartInfo = info;
            proc.Start();

            nvim = new NeovimClient<NeovimSocket>( new NeovimSocket( "/tmp/nvim" ) );
            nvim.Init();
        }

        [ClassCleanup]
        public static void ClassCleanup() {
            nvim.Dispose();
            proc.Dispose();
        }

        [TestMethod]
        public void TestVimStrwidth() {
            var ret = nvim.Func<string, long>( "vim_strwidth", "aaa" );
            Assert.AreEqual( ret, 3 );
        }
        */
    }
}
