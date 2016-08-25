using System.Collections.Specialized;

namespace HttpWebReFormer
{

    public struct HttpWebResponseForm
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Method { get; set; }
        public string Action { get; set; }
        public NameValueCollection Fields { get; set; }
    }

}
