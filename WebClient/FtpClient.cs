//+---------------------------------------------------------------------+
//   DESCRIPTION: FTP Client
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
    public class FtpClient
    {
        public FtpClient(ICredentials credentials) {
            Credentials = credentials;
        }

        public ICredentials Credentials { get; private set; }

        public void CreateDirectory(Uri directory) {
            CheckUri(directory);

            using (FtpWebResponse response = (FtpWebResponse)(CreateRequest(directory, WebRequestMethods.Ftp.MakeDirectory).GetResponse())) {; }
        }

        public void DeleteDirectory(Uri directory) {
            CheckUri(directory);

            using (FtpWebResponse response = (FtpWebResponse)(CreateRequest(directory, WebRequestMethods.Ftp.RemoveDirectory).GetResponse())) {; }
        }

        public void DeleteFile(Uri file) {
            CheckUri(file);

            using (FtpWebResponse response = (FtpWebResponse)(CreateRequest(file, WebRequestMethods.Ftp.DeleteFile).GetResponse())) {; }
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

            FtpWebRequest request = CreateRequest(sourceFile, WebRequestMethods.Ftp.Rename);
            request.RenameTo = renameTo;

            using (request.GetResponse()) {; }
        }

        public Stream OpenRead(Uri file) {
            MemoryStream memoryStream = new MemoryStream();
            byte[] buff = new byte[2048];
            int count;

            using (FtpWebResponse response = (FtpWebResponse)(CreateRequest(file, WebRequestMethods.Ftp.DownloadFile).GetResponse())) {
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
            FtpWebRequest request = CreateRequest(file, WebRequestMethods.Ftp.UploadFile);

            request.ContentLength = stream.Length;

            using (Stream requestStream = request.GetRequestStream()) { stream.CopyTo(requestStream); }
            using (request.GetResponse()) {; }
        }

        private void CheckUri(Uri uri) {
            if (uri == null) throw new ArgumentNullException("uri");
            if (uri.Scheme != Uri.UriSchemeFtp) throw new ArgumentException("The URI isn't a valid FTP URI", "uri");
        }

        private FtpWebRequest CreateRequest(Uri requestUri, string requestMethod) {
            FtpWebRequest request = (FtpWebRequest)WebRequest.Create(requestUri);

            request.ConnectionGroupName = "Kehhf.FTP";
            request.Credentials = Credentials;
            request.KeepAlive = true;
            request.Method = requestMethod;
            request.ServicePoint.ConnectionLimit = 8;

            return request;
        }

        private IEnumerable<WebFileSystemInfo> GetFileSystemInfos(Uri directory, WebFileSystemInfoType type) {
            List<WebFileSystemInfo> infos = new List<WebFileSystemInfo>();
            Regex regex = new Regex(@"^(\d+-\d+-\d+\s+\d+:\d+(?:AM|PM))\s+(<DIR>|\d+)\s+(.+)$", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace);
            Match match;

            using (FtpWebResponse response = (FtpWebResponse)(CreateRequest(directory, WebRequestMethods.Ftp.ListDirectoryDetails).GetResponse())) {
                using (Stream stream = response.GetResponseStream()) {
                    using (StreamReader reader = new StreamReader(stream)) {
                        while (!reader.EndOfStream) {
                            match = regex.Match(reader.ReadLine());

                            if (type == WebFileSystemInfoType.Directory && match.Groups[2].Value == "<DIR>") {
                                infos.Add(new WebFileSystemInfo(new Uri(directory, match.Groups[3].Value), type));
                            } else if (type == WebFileSystemInfoType.File && match.Groups[2].Value != "<DIR>") {
                                infos.Add(new WebFileSystemInfo(new Uri(directory, match.Groups[3].Value), type, DateTime.ParseExact(match.Groups[1].Value, "MM-dd-yy  hh:mmtt", DateTimeFormatInfo.InvariantInfo), long.Parse(match.Groups[2].Value)));
                            }
                        }
                    }
                }
            }

            return infos;
        }
    }
}
