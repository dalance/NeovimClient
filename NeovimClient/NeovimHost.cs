using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;
using MsgPack;

namespace NeovimClient {

    public class NeovimHost : INeovimIO, IDisposable {

        // - field -----------------------------------------------------------------------

        private string neovimPath;

        private Process      proc;
        private StreamWriter stdIn;
        private StreamReader stdOut;
        private StreamReader stdErr;

        private int msgId = 0;
        private Dictionary<int, TaskCompletionSource<MessagePackObject[]>> msgResults = new Dictionary<int, TaskCompletionSource<MessagePackObject[]>>();

        private byte[] bufOut = new byte[65536];
        private byte[] bufErr = new byte[65536];

        // - property --------------------------------------------------------------------

        // - public methods --------------------------------------------------------------

        public NeovimHost( string path ) {
            neovimPath = path;
        }

        public void Init() {
            InitProcess( neovimPath );
        }

        public void Dispose() {
            proc.Dispose();
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

        private void OnStdErrRead( Task<int> count ) {
            Console.WriteLine( Encoding.Default.GetString( bufOut ) );
            stdErr.BaseStream.ReadAsync( bufErr, 0, bufErr.Length ).ContinueWith( c => OnStdErrRead( c ) );
        }

        // - private methods -------------------------------------------------------------

        private void InitProcess( string path ) {
            var info = new ProcessStartInfo( path, "--embed --headless" );
            info.CreateNoWindow         = true;
            info.RedirectStandardError  = true;
            info.RedirectStandardInput  = true;
            info.RedirectStandardOutput = true;
            info.UseShellExecute        = false;

            proc = new Process();
            proc.StartInfo = info;
            proc.EnableRaisingEvents = true;
            proc.Exited += OnProcExited;
            proc.Start();

            stdIn  = proc.StandardInput;
            stdOut = proc.StandardOutput;
            stdErr = proc.StandardError;

            stdOut.BaseStream.ReadAsync( bufOut, 0, bufOut.Length ).ContinueWith( c => OnStdOutRead( c ) );
            stdErr.BaseStream.ReadAsync( bufErr, 0, bufErr.Length ).ContinueWith( c => OnStdErrRead( c ) );
        }

    }
}
