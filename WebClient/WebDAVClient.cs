//+---------------------------------------------------------------------+
//   DESCRIPTION: WebDAV Client
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
using System.Xml;
using System.Xml.XPath;

namespace Kehhf.Net.WebClient
{
    public class WebDAVClient
    {
        public WebDAVClient(ICredentials credentials) {
            Credentials = credentials;
        }

        public ICredentials Credentials { get; private set; }

        public void CreateDirectory(Uri directory) {
            CheckUri(directory);

            using (HttpWebResponse response = (HttpWebResponse)(CreateRequest(directory, WebRequestMethods.Http.MkCol, null).GetResponse())) {; }
        }

        public void DeleteDirectory(Uri directory) {
            CheckUri(directory);

            using (HttpWebResponse response = (HttpWebResponse)(CreateRequest(directory, WebDAVRequestMethods.Detele, null).GetResponse())) {; }
        }

        public void DeleteFile(Uri file) {
            CheckUri(file);

            using (HttpWebResponse response = (HttpWebResponse)(CreateRequest(file, WebDAVRequestMethods.Detele, null).GetResponse())) {; }
        }

        public bool DirectoryExists(Uri directory) {
            return ListDirectories(new Uri(directory, "./")).Any(x => x.Uri.AbsoluteUri == directory.AbsoluteUri);
        }

        public bool FileExists(Uri file) {
            return ListFiles(new Uri(file, "./")).Any(x => x.Uri.AbsoluteUri == file.AbsoluteUri);
        }

        public IEnumerable<WebFileSystemInfo> ListDirectories(Uri directory) {
            CheckUri(directory);

            return GetFileSystemInfos(directory, WebFileSystemInfoType.Directory);
        }

        public IEnumerable<WebFileSystemInfo> ListFiles(Uri directory) {
            CheckUri(directory);

            return GetFileSystemInfos(directory, WebFileSystemInfoType.File);
        }

        public void MoveFile(Uri sourceFile, string renameTo) {
            CheckUri(sourceFile);

            HttpWebRequest request = CreateRequest(sourceFile, WebDAVRequestMethods.Move, new Dictionary<string, string> { { "Destination", renameTo } });

            using (request.GetResponse()) {; }
        }

        public Stream OpenRead(Uri file) {
            MemoryStream memoryStream = new MemoryStream();
            byte[] buff = new byte[2048];
            int count;

            using (HttpWebResponse response = (HttpWebResponse)(CreateRequest(file, WebRequestMethods.Http.Get, null).GetResponse())) {
                using (Stream stream = response.GetResponseStream()) {
                    while ((count = stream.Read(buff, 0, buff.Length)) > 0) {
                        memoryStream.Write(buff, 0, count);
                    }
                }
            }

            memoryStream.Position = 0;

            return memoryStream;
        }

        public void OpenWrite(Uri file, Stream stream) {
            HttpWebRequest request = CreateRequest(file, WebRequestMethods.Http.Put, null);

            request.ContentLength = stream.Length;

            using (Stream requestStream = request.GetRequestStream()) { stream.CopyTo(requestStream); }
            using (request.GetResponse()) {; }
        }

        private void CheckUri(Uri uri) {
            if (uri == null) throw new ArgumentNullException("uri");
            if (uri.Scheme != Uri.UriSchemeHttp) throw new ArgumentException("The URI isn't a valid HTTP URI", "uri");
        }

        private HttpWebRequest CreateRequest(Uri requestUri, string requestMethod, IDictionary<string, string> headers) {
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(requestUri);

            request.ConnectionGroupName = "Kehhf.HTTP";
            request.Credentials = Credentials;
            request.KeepAlive = true;
            request.Method = requestMethod;

            request.ServicePoint.ConnectionLimit = 8;
            request.ServicePoint.Expect100Continue = false;

            if (headers != null) {
                foreach (string key in headers.Keys) {
                    request.Headers.Set(key, headers[key]);
                }
            }

            return request;
        }

        private IEnumerable<WebFileSystemInfo> GetFileSystemInfos(Uri directory, WebFileSystemInfoType type) {
            List<WebFileSystemInfo> infos = new List<WebFileSystemInfo>();
            byte[] pfbs = UTF8Encoding.Default.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><propfind xmlns=\"DAV:\"><prop><getlastmodified/><displayname/><getcontentlength/><iscollection/></prop></propfind>");

            HttpWebRequest request = CreateRequest(directory, WebDAVRequestMethods.PropFind, new Dictionary<string, string> { { "Depth", "1" } });
            request.ContentLength = pfbs.Length;
            request.ContentType = "text/plain;charset=utf-8";

            using (Stream requestStream = request.GetRequestStream()) {
                requestStream.Write(pfbs, 0, pfbs.Length);
            }
            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                using (Stream responseStream = response.GetResponseStream()) {
                    XmlDocument doc = new XmlDocument();
                    XmlNamespaceManager xnm = new XmlNamespaceManager(doc.NameTable);
                    xnm.AddNamespace("D", "DAV:");

                    doc.Load(responseStream);

                    foreach (XmlNode item in doc.SelectNodes("/D:multistatus/D:response", xnm)) {
                        XmlNode node = item.SelectSingleNode("D:href", xnm);
                        XmlNode node2 = item.SelectSingleNode("D:propstat/D:prop/D:iscollection", xnm);
                        XmlNode node3 = item.SelectSingleNode("D:propstat/D:prop/D:displayname", xnm);
                        XmlNode node4 = item.SelectSingleNode("D:propstat/D:prop/D:getlastmodified", xnm);
                        XmlNode node5 = item.SelectSingleNode("D:propstat/D:prop/D:getcontentlength", xnm);
                        if (type == WebFileSystemInfoType.Directory && node2.InnerText == "1") {
                            if (node3.InnerText == "/") { continue; }
                            infos.Add(new WebFileSystemInfo(new Uri(node.InnerText), type));
                        } else if (type == WebFileSystemInfoType.File && node2.InnerText == "0") {
                            infos.Add(new WebFileSystemInfo(new Uri(node.InnerText), type, DateTime.Parse(node4.InnerText), long.Parse(node5.InnerText)));
                        }
                    }
                }
            }

            return infos;
        }

        internal static class WebDAVRequestMethods
        {
            public const string Detele = "DELETE";
            public const string Move = "MOVE";
            public const string PropFind = "PROPFIND";
        }
    }
}
