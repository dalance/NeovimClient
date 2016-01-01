using MsgPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NeovimClient {

    public class NeovimTcp : INeovimIO {

        // - field -----------------------------------------------------------------------

        private TcpClient client = new TcpClient();

        private StreamWriter stdIn ;
        private StreamReader stdOut;

        private int msgId = 0;
        private Dictionary<int, TaskCompletionSource<MessagePackObject[]>> msgResults = new Dictionary<int, TaskCompletionSource<MessagePackObject[]>>();

        private byte[] bufOut = new byte[65536];
        private byte[] bufErr = new byte[65536];

        // - property --------------------------------------------------------------------

        // - public methods --------------------------------------------------------------

        public NeovimTcp( string address, int port ) {
            InitTcp( address, port );
        }

        public void Dispose() {
            client.Close();
        }

        public MessagePackObject[] Request( string name, object[] parameters, bool hasResult = true ) {
            var ret = RequestAsync( name, parameters, hasResult );
            return ret.Result;
        }

        public async Task<MessagePackObject[]> RequestAsync( string name, object[] parameters, bool hasResult = true ) {
            var curId  = msgId++;
            var stream = new MemoryStream();
            var packer = Packer.Create( stream );
            packer.PackArrayHeader( 4 );
            packer.Pack( (int)NeovimMsgType.Request );
            packer.Pack( curId );
            packer.PackString( name );
            packer.PackArrayHeader( parameters.Length );
            foreach( var p in parameters ) {
                packer.Pack( p );
            }

            if( hasResult ) {
                msgResults.Add( curId, new TaskCompletionSource<MessagePackObject[]>() );
            }

            stream.Position = 0;
            using ( var reader = new BinaryReader( stream ) ) {
                var bytes = reader.ReadBytes( (int)reader.BaseStream.Length );
                stdIn.BaseStream.Write( bytes, 0, bytes.Length );
                stdIn.BaseStream.Flush();
            }

            if( hasResult ) {
                var result = await msgResults[curId].Task.ConfigureAwait( false );
                msgResults.Remove( curId );
                return result;
            } else {
                return null;
            }
        }

        // - event handler ---------------------------------------------------------------

        public event NeovimNotificationEventHandler NotificationReceived;

        protected virtual void OnNotificationReceived( NeovimNotificationEventArgs e ) {
            NeovimNotificationEventHandler handler = NotificationReceived;
            if ( handler != null ) {
                handler( this, e );
            }
        }

        private void OnProcExited( object sender, EventArgs e ) {
            throw new NotImplementedException();
        }

        private void OnStdOutRead( Task<int> count ) {
            int pos = 0;
            while( count.Result > pos ) {
                var dat = Unpacking.UnpackObject( bufOut, pos );
                pos += dat.ReadCount;

                var datList = dat.Value.AsList();
                var type = (NeovimMsgType)datList[0].AsInt64();

                switch( type ) {
                    case NeovimMsgType.Response:
                        var curId = datList[1].AsInt32();
                        var err   = datList[2];
                        var res   = datList[3];
                        msgResults[curId].SetResult( new[] { err, res } );
                        break;
                    case NeovimMsgType.Notification:
                        var name = datList[1].AsString();
                        var args = datList[2];
                        OnNotificationReceived( new NeovimNotificationEventArgs() { Name = name, Args = args } );
                        break;
                    default:
                        break;
                }
            }
            stdOut.BaseStream.ReadAsync( bufOut, 0, bufOut.Length ).ContinueWith( c => OnStdOutRead( c ) );
        }

        // - private methods -------------------------------------------------------------

        private void InitTcp( string address, int port ) {
            client.Connect( address, port );

            stdIn  = new StreamWriter( client.GetStream() );
            stdOut = new StreamReader( client.GetStream() );

            stdOut.BaseStream.ReadAsync( bufOut, 0, bufOut.Length ).ContinueWith( c => OnStdOutRead( c ) );
        }

    }
}
