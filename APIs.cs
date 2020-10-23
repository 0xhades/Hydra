using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace hydra {
    public class Profile {
        public string username;
        public string biography;
        public string full_name;
        public string phone_number;
        public string email;
        public string gender;
        public string external_url;
    }
    public class Utils {
        static byte[] Decompress(byte[] gzip)
        {         
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }
        public IEnumerable<string> ReadLines(StreamReader stream)
        {
            StringBuilder sb = new StringBuilder();

            int symbol = stream.Peek();
            while (symbol != -1)
            {
                symbol = stream.Read();
                if (symbol == 13 && stream.Peek() == 10)
                {
                    stream.Read();

                    string line = sb.ToString();
                    sb.Clear();

                    yield return line;
                }
                else
                    sb.Append((char)symbol);
            }

            yield return sb.ToString();
        }
        byte[] Combine(byte[] first, byte[] second)
        {
            byte[] bytes = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, bytes, 0, first.Length);
            Buffer.BlockCopy(second, 0, bytes, first.Length, second.Length);
            return bytes;
        }
        void CopyTo(Stream src, Stream dest) {
            byte[] bytes = new byte[4096];
            int cnt;
            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0) {
                dest.Write(bytes, 0, cnt);
            }
        }
        byte[] Zip(string str) {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream()) {
                using (var gs = new GZipStream(mso, CompressionMode.Compress)) {
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }
        public dynamic parsePacket(string path, string method, Dictionary<string, string> headers, Dictionary<string, string> data = null, string rawData = "", bool compress = true, Dictionary<string, string> cookies = null, string connection = "keep-alive") {
            
            byte[] gzipped_body = null;
            string Method = method.ToUpper();
            string rawRequest = $"{method} {path} HTTP/1.1\r\n";

            if (method == "GET" && (data != null || rawData != "")) {
                string rawQuery = "";
                if (rawData != "") {
                    rawQuery = rawData;
                }
                else {
                    int i = 0;
                    foreach (KeyValuePair<string, string> entry in data) {
                        if (i == data.Count - 1) {
                            rawQuery += $"{entry.Key}={entry.Value}";
                        }
                        else {
                            rawQuery += $"{entry.Key}={entry.Value}&";
                        }
                        i++;
                    }
                }
                string query = $"{path}?{rawQuery}";
                rawRequest = $"{method} {query} HTTP/1.1\r\n";
            }

            string rawHeaders = "";
            foreach (KeyValuePair<string, string> entry in headers)
                rawHeaders += $"{entry.Key}: {entry.Value}\r\n";

            if (!rawHeaders.ToLower().Contains("host"))
                rawRequest += "Host: i.instagram.com\r\n";

            if (cookies != null) {
                string cookie = "";
                foreach (KeyValuePair<string, string> entry in cookies)
                    cookie += $"{entry.Key}={entry.Value};";
                rawHeaders += $"Cookie: {cookie}\r\n";
            }

            string _data = "";
            if (method == "POST" && (data != null || rawData != "")) {

                if (rawData != "")
                    _data = rawData;
                else {
                    int i = 0;
                    foreach (KeyValuePair<string, string> entry in data) {
                        if (i == data.Count - 1)
                            _data += $"{entry.Key}={entry.Value}";
                        else
                            _data += $"{entry.Key}={entry.Value}&";
                        i++;
                    }
                }

                if (compress)
                    gzipped_body = Zip(_data);
                if (!rawHeaders.ToLower().Contains("content-length")){
                    if (_data == "NULL?")
                        rawHeaders += "Content-Length: #CLEN\r\n";
                    else
                        if (compress) {
                            rawHeaders += $"content-Encoding: gzip\r\n";
                            rawHeaders += $"content-Length: {gzipped_body.Length}\r\n";
                        } else {
                            rawHeaders += $"content-Length: {Encoding.UTF8.GetBytes(_data).Length}\r\n";
                        }
                }
                if (!rawHeaders.ToLower().Contains("connection:")) {
                    rawHeaders += $"connection: {connection}\r\n";
                }
            }

            dynamic encodedRequest = null;
            rawRequest += $"{rawHeaders}\r\n";
            if (_data != "" && _data != "NULL?") {
                if (compress)
                    encodedRequest = Combine(Encoding.UTF8.GetBytes(rawRequest), gzipped_body);
                else
                    encodedRequest = rawRequest + _data; //Encoding.UTF8.GetBytes(rawRequest + _data);
            }
            else
                encodedRequest = rawRequest; //Encoding.UTF8.GetBytes(rawRequest);

            return encodedRequest;

        }
    }
   class APIs {
       private Random random = new Random();
       public Profile GetProfile(string SessionID) {
            Profile Profile = new Profile();
            HttpWebRequest req = WebRequest.CreateHttp("https://i.instagram.com/api/v1/accounts/current_user/?edit=true");
            req.UserAgent = $"Instagram {random.Next(5, 50)}.{random.Next(6, 10)}.{random.Next(0, 10)} Android (18/2.1; 160dpi; 720x900; ZTE; LAVA-9L7EZ; pdfz; hq3143; en_US)";
            req.Headers.Add("Cookie", $"sessionid={SessionID}");
            req.KeepAlive = false;
            req.ProtocolVersion = HttpVersion.Version10;
            req.Proxy = null;
            req.ServicePoint.UseNagleAlgorithm = false;
            req.ServicePoint.Expect100Continue = false;

            try {
                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                string Content = new StreamReader(res.GetResponseStream()).ReadToEnd();
                if (Content.ToLower().Contains("ok")) {
                    Profile.username = Regex.Match(Content, "\"username\": \"(.*?)\"").Groups[1].Value.Replace(" ", "");
                    Profile.email = HttpUtility.UrlEncode(Regex.Match(Content, "\"email\": \"(.*?)\"").Groups[1].Value.Replace(" ", ""));
                    Profile.biography = Regex.Match(Content, "\"biography\": \"(.*?)\"").Groups[1].Value.Replace(" ", "");
                    Profile.full_name = Regex.Match(Content, "\"full_name\": \"(.*?)\"").Groups[1].Value.Replace(" ", "");
                    Profile.phone_number = HttpUtility.UrlEncode(Regex.Match(Content, "\"phone_number\": \"(.*?)\"").Groups[1].Value.Replace(" ", ""));
                    Profile.gender = Regex.Match(Content, "\"gender\": (.*?),").Groups[1].Value.Replace(" ", "");
                    Profile.external_url = Regex.Match(Content, "\"external_url\": \"(.*?)\"").Groups[1].Value.Replace(" ", "");
                    return Profile;
                }
            } catch (WebException ex) {
                // HttpWebResponse HttpResponse = (HttpWebResponse)ex.Response;
                // StreamReader Reader = new StreamReader(HttpResponse.GetResponseStream());
            }

            return null; 
        }
        public string Login(string username, string password) {
            WebHeaderCollection headers = new WebHeaderCollection();
            headers.Add("User-Agent", $"Instagram {random.Next(5, 50)}.{random.Next(6, 10)}.{random.Next(0, 10)} Android (18/2.1; 160dpi; 720x900; ZTE; LAVA-9L7EZ; pdfz; hq3143; en_US)");
            headers.Add("Cookie2", "$Version=1");
            headers.Add("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            headers.Add("X-IG-Connection-Type", "WIFI");
            headers.Add("Accept-Language", "en-US");
            headers.Add("X-FB-HTTP-Engine", "Liger");
            headers.Add("X-IG-Capabilities", "3brTBw==");
            headers.Add("Connection", "Close");

            Dictionary<string, string> data = new Dictionary<string, string>();
            data.Add("jazoest", "22713");
            data.Add("phone_id", Guid.NewGuid().ToString());
            data.Add("enc_password", $"#PWD_INSTAGRAM_BROWSER:0:{(Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalMilliseconds}:{password}");
            data.Add("_csrftoken", "missing");
            data.Add("username", username);
            data.Add("adid", Guid.NewGuid().ToString());
            data.Add("device_id", $"android-{RandomString(16)}");
            data.Add("guid", Guid.NewGuid().ToString());
            data.Add("google_tokens", "[]");
            data.Add("login_attempt_count", "0");

            string JSON = DictToJSON(data);
		    string body = $"signed_body=SIGNATURE.{JSON}";

            CookieContainer CookieJar = new CookieContainer();
            HttpWebRequest request = WebRequest.CreateHttp("https://i.instagram.com/api/v1/accounts/login/");
            request.Headers = headers;
            request.Method = "POST";
            request.CookieContainer = CookieJar;
            request.Accept = "*/*";
            request.KeepAlive = false;
            request.ProtocolVersion = HttpVersion.Version10;
            request.ServicePoint.UseNagleAlgorithm = false;
            request.ServicePoint.Expect100Continue = false;
            byte[] bytes = Encoding.ASCII.GetBytes(body);
            request.ContentLength = (long)bytes.Length;
            Stream StreamWriter = request.GetRequestStream();
            StreamWriter.Write(bytes, 0, bytes.Length);
            StreamWriter.Flush();
            StreamWriter.Close();
            StreamWriter.Dispose();
            string Content = null;

            try {
                HttpWebResponse HttpResponse = (HttpWebResponse)request.GetResponse();
                StreamReader Reader = new StreamReader(HttpResponse.GetResponseStream());
                Content = Reader.ReadToEnd();
                foreach (Cookie Cookie in CookieJar.GetCookies(new Uri("https://i.instagram.com/api/v1/accounts/login/"))) {
                    if (Cookie.Name == "sessionid")
                        return Cookie.Value;
                }
            }
            catch (WebException ex) {
                // if (!ex.Message.Contains("timeout")) {
                //     HttpWebResponse HttpResponse = (HttpWebResponse)ex.Response;
                //     StreamReader Reader = new StreamReader(HttpResponse.GetResponseStream());
                //     Content = Reader.ReadToEnd();
                // }
            }

            return null;
        }
        public string DictToJSON(Dictionary<string, string> dict) {
            int i = 0;
            string json = "{";
            foreach (KeyValuePair<string, string> entry in dict) {
                json = ((i != dict.Count - 1) ? (json + "\"" + entry.Key + "\":\"" + entry.Value + "\",") : (json + "\"" + entry.Key + "\":\"" + entry.Value + "\""));
                i++;
            }
            return json + "}";
        }
        public string RandomString(int length) {
		    return new string(Enumerable.Repeat("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789".ToLower(), length).Select((Func<string, char>)((string s) => s[random.Next(s.Length)])).ToArray());
	    }
    }
}
