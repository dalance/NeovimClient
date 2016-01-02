using System;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace NeovimClient {

    public class NeovimHost : NeovimIO {

        // - field -----------------------------------------------------------------------

        private string neovimPath;

        private Process      proc;
        private StreamReader neovimErr;

        private byte[] bufErr = new byte[65536];

        // - property --------------------------------------------------------------------

        // - public methods --------------------------------------------------------------

        public NeovimHost( string path ) {
            neovimPath = path;
        }

        public override void Init() {
            InitProcess( neovimPath );
            base.Init();
        }

        public override void Dispose() {
            try {
                neovimErr.Dispose();
            	proc.Dispose();
            } finally {
                base.Dispose();
            }
        }

        // - event handler ---------------------------------------------------------------

        private void OnProcExited( object sender, EventArgs e ) {
        }

        private void OnNeovimErrRead( Task<int> count ) {
            Console.WriteLine( Encoding.Default.GetString( bufErr ) );
            neovimErr.BaseStream.ReadAsync( bufErr, 0, bufErr.Length ).ContinueWith( c => OnNeovimErrRead( c ) );
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

            neovimIn  = proc.StandardInput;
            neovimOut = proc.StandardOutput;
            neovimErr = proc.StandardError;

            neovimErr.BaseStream.ReadAsync( bufErr, 0, bufErr.Length ).ContinueWith( c => OnNeovimErrRead( c ) );
        }

    }
}
