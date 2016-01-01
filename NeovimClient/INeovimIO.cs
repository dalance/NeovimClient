using MsgPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NeovimClient {

    enum NeovimMsgType {
        Request      = 0,
        Response     = 1,
        Notification = 2,
    }

    public delegate void NeovimNotificationEventHandler( object sender, NeovimNotificationEventArgs e );

    public class NeovimNotificationEventArgs : EventArgs {
        public string            Name { get; set; }
        public MessagePackObject Args { get; set; }
    }

    public interface INeovimIO : IDisposable {
        event NeovimNotificationEventHandler NotificationReceived;
        MessagePackObject[] Request( string name, object[] parameters, bool hasResult = true );
        Task<MessagePackObject[]> RequestAsync( string name, object[] parameters, bool hasResult = true );
    }
}
