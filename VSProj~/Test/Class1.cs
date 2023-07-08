
using com.bbbirder;
using System.Xml.Linq;

namespace com.bbb
{
    public partial class EMail
    {
        public int Id { get; set; }

        public string Title { get; set; } = "";

        [global::System.ComponentModel.DefaultValue("")]
        public string Message { get; set; } = "";

        public ulong ExpireTimestamp { get; set; }

        public IEnumerator<int> Attachmen3tIds { get; set; }
        public List<int> Attachmen2tIds { get; set; }
        public int[] AttachmentIds { get; set; }

        public AcceptState Accept { get; set; }
    }
    public interface IExt
    {
        
    }
    public enum AcceptState
    {
        AcceptStateNOTSET = 0,
        AcceptStateUnavailable = 1,
        AcceptStateAcceptable = 2,
        AcceptStateAccepted = 3,
    }
}