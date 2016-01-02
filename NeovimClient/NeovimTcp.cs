using System.IO;
using System.Net.Sockets;

namespace NeovimClient {

    public class NeovimTcp : NeovimIO {

        // - field -----------------------------------------------------------------------

        private string neovimAddress;
        private int    neovimPort;
        private TcpClient client = new TcpClient();

        // - property --------------------------------------------------------------------

        // - public methods --------------------------------------------------------------

        public NeovimTcp( string address, int port ) {
            neovimAddress = address;
            neovimPort = port;
        }

        public override void Init() {
            InitTcp( neovimAddress, neovimPort );
            base.Init();
        }

        public override void Dispose() {
            try {
                client.Close();
            } finally {
                base.Dispose();
            }
        }

        // - event handler ---------------------------------------------------------------

        // - private methods -------------------------------------------------------------

        private void InitTcp( string address, int port ) {
            client.Connect( address, port );

            neovimIn  = new StreamWriter( client.GetStream() );
            neovimOut = new StreamReader( client.GetStream() );
        }

    }
}
