using Mono.Unix;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NeovimClient {
    public class NeovimSocket : NeovimIO {

        // - field -----------------------------------------------------------------------

        private string        neovimSocketPath;
        private NetworkStream neovimStream;

        // - property --------------------------------------------------------------------

        // - public methods --------------------------------------------------------------

        public NeovimSocket( string socketPath ) {
            neovimSocketPath = socketPath;
        }

        public override void Init() {
            InitSocket( neovimSocketPath );
            base.Init();
        }

        public override void Dispose() {
            try {
                neovimStream.Dispose();
            } finally {
                base.Dispose();
            }
        }

        // - event handler ---------------------------------------------------------------

        // - private methods -------------------------------------------------------------

        private void InitSocket( string socketPath ) {
            var endpoint = new UnixEndPoint( socketPath );
            var socket = new Socket( AddressFamily.Unix, SocketType.Stream, ProtocolType.Unspecified );
            socket.Connect( endpoint );
            neovimStream = new NetworkStream( socket, true );

            neovimIn  = new StreamWriter( neovimStream );
            neovimOut = new StreamReader( neovimStream );
        }

    }
}
