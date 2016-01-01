using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NeovimClient;

namespace Test {
    [TestClass]
    public class TestNeovimHost {

        private static NeovimClient<NeovimHost> nvim;

        [ClassInitialize]
        public static void ClassInit( TestContext context ) {
            nvim = new NeovimClient<NeovimHost>( new NeovimHost( "neovim/nvim.exe" ) );
        }

        [ClassCleanup]
        public static void ClassCleanup() {
            nvim.Dispose();
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
