//+---------------------------------------------------------------------+
//   DESCRIPTION: File System Info for Web
//       CREATED: Kehhf on 2020/07/20
// LAST MODIFIED: Kehhf on 2020/07/20
//+---------------------------------------------------------------------+

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace Kehhf.Net.WebClient
{
    public enum WebFileSystemInfoType { Directory, File }

    public class WebFileSystemInfo
    {
        public WebFileSystemInfo(Uri uri, WebFileSystemInfoType type, DateTime lastWriteTime = default(DateTime), long length = 0) {
            Uri = uri;
            Type = type;
            LastWriteTime = lastWriteTime;
            Length = length;
        }

        public DateTime LastWriteTime { get; private set; }

        public long Length { get; private set; }

        public WebFileSystemInfoType Type { get; private set; }

        public Uri Uri { get; private set; }

        public string FullName {
            get { return Uri.AbsoluteUri; }
        }

        public string Name {
            get { return Path.GetFileName(FullName); }
        }
    }
}
