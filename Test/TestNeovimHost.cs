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
            nvim.Init();
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

        [TestMethod]
        public void TestUiAttach() {
            nvim.NotificationReceived += NvimNotificationReceived;
            nvim.Action<long, long, bool>( "ui_attach", 100, 100, true );
        }

        private void NvimNotificationReceived( object sender, NeovimNotificationEventArgs e ) {
            Assert.AreEqual( e.Name, "redraw" );
        }

        [TestMethod]
        public void TestGetFuncs() {
            var ret = nvim.GetFuncs();
            Assert.IsTrue ( ret.Contains( "vim_get_api_info" ) );
            Assert.IsFalse( ret.Contains( "aaa" ) );
        }

        [TestMethod]
        public void TestGetFuncInfo() {
            var ret0 = nvim.GetFuncInfo( "vim_command" );
            Assert.AreEqual( ret0.Name          , "vim_command" );
            Assert.AreEqual( ret0.ParamTypeCs[0], typeof( string ) );
            Assert.AreEqual( ret0.ReturnTypeCs  , typeof( void ) );
            Assert.AreEqual( ret0.Description   , "void Action<String>( String name, String str )" );

            var ret1 = nvim.GetFuncInfo( "vim_replace_termcodes" );
            Assert.AreEqual( ret1.Name       , "vim_replace_termcodes" );
            Assert.AreEqual( ret1.Description, "String Func<String, Boolean, Boolean, Boolean>( String name, String str, Boolean from_part, Boolean do_lt, Boolean special )" );
        }

    }
}
