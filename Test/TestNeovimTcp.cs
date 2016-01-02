using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeovimClient;
using System.Diagnostics;

namespace Test {
    [TestClass]
    public class TestNeovimTcp {

        private static NeovimClient<NeovimTcp> nvim;
        private static Process proc;

        [ClassInitialize]
        public static void ClassInit( TestContext context ) {
            var info = new ProcessStartInfo( "neovim/nvim.exe", "--embed --headless" );
            info.CreateNoWindow  = true;
            info.UseShellExecute = false;
            info.EnvironmentVariables.Add( "NVIM_LISTEN_ADDRESS", "127.0.0.1:20000" );

            proc = new Process();
            proc.StartInfo = info;
            proc.Start();

            nvim = new NeovimClient<NeovimTcp>( new NeovimTcp( "127.0.0.1", 20000 ) );
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

        [TestMethod]
        public void TestVimSetCurrentLine() {
            nvim.Action<string>( "vim_set_current_line", "aaa" );
            var ret = nvim.Func<string>( "vim_get_current_line" );
            Assert.AreEqual( ret, "aaa" );
        }

        [TestMethod]
        public void TestVimDelCurrentLine() {
            nvim.Action<string>( "vim_set_current_line", "aaa" );
            nvim.Action( "vim_del_current_line" );
            var ret = nvim.Func<string>( "vim_get_current_line" );
            Assert.AreEqual( ret, "" );
        }
    }
}
