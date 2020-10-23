using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using SocketProxy.Proxy;

namespace hydra
{
    class Program
    {
        static public APIs API = null;
        static public int MOD = 0;
        public static List<string> Proxies = new List<string>();
        public static List<string> SessionID = new List<string>();
        public static List<string> Accounts = new List<string>();
        public static List<Account> Sessions = new List<Account>();
        static public string Username = null;
        static public string Password = null;
        static public string Target = null;
        static public string last = null;
        static public int Workers = 5;
        static public int sleep = 5;
        static public int Speed = 0;
        static public int UnRead = 0;
        static public int RequestSent = 0;
        static public int RequestHandled = 0;
        static public int BadSessionID = 0;
        static public int Wait = 0;
        static public bool Success = false;
        static public string Payload = null;
        static public Random random = new Random();
        static public string banner = @"

██╗  ██╗██╗   ██╗██████╗ ██████╗  █████╗ 
██║  ██║╚██╗ ██╔╝██╔══██╗██╔══██╗██╔══██╗
███████║ ╚████╔╝ ██║  ██║██████╔╝███████║
██╔══██║  ╚██╔╝  ██║  ██║██╔══██╗██╔══██║
██║  ██║   ██║   ██████╔╝██║  ██║██║  ██║
╚═╝  ╚═╝   ╚═╝   ╚═════╝ ╚═╝  ╚═╝╚═╝  ╚═╝
                                         
        ";
        static void Main(string[] args)
        {
            API = new APIs();

            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine(banner);
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine("By Hades, @0xhades");
            Console.WriteLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Enter the proxy list [no? = 0]: ");
            Console.ForegroundColor = ConsoleColor.White;
            string outin = Console.ReadLine();
            if (outin != "0") {
                Proxies = new List<string>(File.ReadAllLines(outin));
                Shuffle(Proxies);
            } 

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("pool [1] / random [2]: ");
            Console.ForegroundColor = ConsoleColor.White;
            MOD = int.Parse(Console.ReadLine());
            
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Accounts List [Y/N]: ");
            Console.ForegroundColor = ConsoleColor.White;
            
            if (!Console.ReadLine().Equals("n", StringComparison.CurrentCultureIgnoreCase)) {
                
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("combo (B), user and pass list (A): ");
                Console.ForegroundColor = ConsoleColor.White;

                List<string> usernames;
                List<string> password;

                if (Console.ReadLine().ToLower() == "a") { 
                    
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("Usernames List");
                    Console.ForegroundColor = ConsoleColor.White;
                    usernames = new List<string>(File.ReadAllLines(Console.ReadLine()));

                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("Passwords List");
                    Console.ForegroundColor = ConsoleColor.White;
                    password = new List<string>(File.ReadAllLines(Console.ReadLine()));

                    if (usernames.Count != password.Count) {
                        throw new Exception("usernames list is not equals to the passwords's list");
                    }

                    int i = 0;
                    foreach (string user in usernames) {
                        Accounts.Add($"{user}:{password[i]}");
                        i++;
                    }

                } else {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.Write("Combo List: ");
                    Console.ForegroundColor = ConsoleColor.White;
                    Accounts = new List<string>(File.ReadAllLines(Console.ReadLine()));
                }
            
                List<Thread> Threads = new List<Thread>();
                foreach (string combo in Accounts) {

                    Thread t = new Thread((i) => {
                        
                        string userpass = (string)i;
                        string user = userpass.Split(':')[0];
                        string pass = userpass.Split(':')[1];

                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"Trying to log into {user}...");

                        var result = API.Login(user, pass);
                        if (result == null) {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.WriteLine($"{user} didn't logged in for some reason");
                            return;
                        }

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{user} logged in successfully");

                        var profile = API.GetProfile(result);
                        if (profile == null) {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.WriteLine($"{user} didn't get profile for some reason");
                            return;
                        }

                        var account = new Account(result, profile);
                        Sessions.Add(account);
                        StreamWriter writer = File.AppendText("savedSessions.txt");
                        writer.WriteLine($"{account.SessionID}");
                        writer.Close();

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"{user} Added successfully");

                    });
                    Threads.Add(t);
                    t.Priority = ThreadPriority.Highest;
                    t.Start(combo);

                }

                foreach (Thread t in Threads) t.Join();

            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("SessionID List [Y/N]: ");
            Console.ForegroundColor = ConsoleColor.White;

            List<Thread> _threads = new List<Thread>();
            if (Console.ReadLine().ToLower() == "y") {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.Write("SessionID's List: ");
                Console.ForegroundColor = ConsoleColor.White;
                
                foreach (var session in File.ReadAllLines(Console.ReadLine())) {

                    Thread t = new Thread((i) => {
                        
                        string sess = (string)i;
                        var profile = API.GetProfile(sess);
                        if (profile == null) {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.WriteLine($"Session {i} isn't a valid session ID");
                            return;
                        }
                        var account = new Account(sess, profile);
                        Sessions.Add(account);
                        StreamWriter writer = File.AppendText("savedSessions.txt");
                        writer.WriteLine($"{account.SessionID}");
                        writer.Close();
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"New valid session added successfully");

                    });
                    _threads.Add(t);
                    t.Priority = ThreadPriority.Highest;
                    t.Start(session);

                }

            }
                
            foreach (Thread t in _threads) t.Join();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Target: ");
            Console.ForegroundColor = ConsoleColor.White;
		    Target = Console.ReadLine();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Prepare the payload...");

            Dictionary<string, string> APIHeaders = new Dictionary<string, string>();
            APIHeaders.Add("User-Agent", $"Instagram {random.Next(5, 50)}.{random.Next(6, 10)}.{random.Next(0, 10)} Android (18/2.1; 160dpi; 720x900; ZTE; LAVA-9L7EZ; pdfz; hq3143; en_US)");
            APIHeaders.Add("Accept", "*/*");
            APIHeaders.Add("Cookie2", "$Version=1");
            APIHeaders.Add("Content-Type", "application/x-www-form-urlencoded; charset=UTF-8");
            APIHeaders.Add("X-IG-Connection-Type", "WIFI");
            APIHeaders.Add("Accept-Language", "en-US");
            APIHeaders.Add("X-FB-HTTP-Engine", "Liger");
            APIHeaders.Add("X-IG-Capabilities", "3brTBw==");
            APIHeaders.Add("Connection", "Keep-Alive");
            APIHeaders.Add("Cookie", "sessionid={SessionID};");
        
            Utils utils = new Utils();
            Payload = utils.parsePacket("/api/v1/accounts/edit_profile/", "POST", APIHeaders, null, "NULL?", false);

            Console.WriteLine("the payload are ready");

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Threads: ");
            Console.ForegroundColor = ConsoleColor.White;
		    Workers = int.Parse(Console.ReadLine());

            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("sleep: ");
            Console.ForegroundColor = ConsoleColor.White;
		    sleep = int.Parse(Console.ReadLine());
            
            List<Thread> threads = new List<Thread>();

            void opreation() {

                SslStream stream = null;

                if (Proxies.Count > 0) {

                    string proxy = Proxies[random.Next(Proxies.Count)];

                    while (true) {
				
                        string username = null;
                        string password = null;
                        
                        if (proxy.Contains('@')) {
                            string userpass = proxy.Split('@')[0];
                            username = userpass.Split(':')[0];
                            password = userpass.Split(':')[1];
                        }

                        string hostname = proxy.Split(':')[0];
                        int port = int.Parse(proxy.Split(':')[1]);

                        stream = CreateStream(hostname, port, username, password);

                        if (stream != null) break;
                        else proxy = Proxies[random.Next(Proxies.Count)];

                    }
                    
                } else {
                    stream = CreateStream();
                }

                Thread t = new Thread(() => {
                    RequestHandler(stream);
                });
                t.Priority = ThreadPriority.Normal;
                t.Start();

                if (MOD == 1) {

                    while (true) {
                        
                        foreach (Account session in Sessions) {
                            
                            Dictionary<string, string> data = new Dictionary<string, string>();
                            data.Add("username", Target);               
                            if (session.profile.phone_number != null) data.Add("phone_number", session.profile.phone_number);
                            if (session.profile.email != null) data.Add("email", session.profile.email);
                            if (session.profile.gender != null) data.Add("gender", session.profile.gender);
                            data.Add("external_url", "instagram.com/0xhades");
                            data.Add("biography", HttpUtility.UrlEncode("Hydra 🇦🇱 by 0xhades, @0xhades"));
                            data.Add("_uuid", Guid.NewGuid().ToString());
                            data.Add("device_id", $"android-{API.RandomString(16)}");

                            string JSON = API.DictToJSON(data);
                            string body = $"signed_body=SIGNATURE.{JSON}";

                            byte[] request = Encoding.UTF8.GetBytes(Payload.Replace("{SessionID}", session.SessionID).Replace("#CLEN", $"{Encoding.UTF8.GetBytes(body).Length}") + body);

                            stream.Write(request, 0, request.Length);
                            RequestSent++;
                            Thread.Sleep(sleep);

                        }

                    }

                } else {
                    
                    while (true) {

                        Account session = Sessions[random.Next(Sessions.Count)];

                        Dictionary<string, string> data = new Dictionary<string, string>();
                        data.Add("username", Target);               
                        if (session.profile.phone_number != null) data.Add("phone_number", session.profile.phone_number);
                        if (session.profile.email != null) data.Add("email", session.profile.email);
                        if (session.profile.gender != null) data.Add("gender", session.profile.gender);
                        data.Add("external_url", "instagram.com/0xhades");
                        data.Add("biography", HttpUtility.UrlEncode("Hydra 🇦🇱 by 0xhades, @0xhades"));
                        data.Add("_uuid", Guid.NewGuid().ToString());
                        data.Add("device_id", $"android-{API.RandomString(16)}");

                        string JSON = API.DictToJSON(data);
                        string body = $"signed_body=SIGNATURE.{JSON}";

                        byte[] request = Encoding.UTF8.GetBytes(Payload.Replace("{SessionID}", session.SessionID).Replace("#CLEN", $"{Encoding.UTF8.GetBytes(body).Length}") + body);

                        stream.Write(request, 0, request.Length);
                        RequestSent++;
                        Thread.Sleep(sleep);

                    }
                }

            }

            for (int i = 0; i <= Workers; i++) {

                Thread t1 = new Thread(() => {
                    opreation();
                });
                threads.Add(t1);
                t1.Priority = ThreadPriority.Highest;
                t1.Start();
                
            }

            Thread t2 = new Thread(() => {
                SuperVisior();
            });
            t2.Priority = ThreadPriority.Normal;
            t2.Start();


            Thread t3 = new Thread(() => {
                Rate();
            });
            t3.Priority = ThreadPriority.Normal;
            t3.Start();

            foreach (Thread t in threads) t.Join();

        }
        public static void RequestHandler(SslStream stream) {
            
            int ReceiveTimeout = 8000;
            int contentLength = 0;
            int statusCode = 0;
            Utils utils = new Utils();
            stream.ReadTimeout = ReceiveTimeout;

            try {

                StreamReader sr = new StreamReader(stream);
                foreach (string line in utils.ReadLines(sr)) {

                    if (line.Contains("HTTP/1.1")) {
                        statusCode = Int32.Parse(line.Split(' ')[1]);
                    }

                    if (line.Contains("Content-Length"))  {
                        contentLength = Int32.Parse(line.Substring(16, line.Length - 16));
                        var totalBytesRead = 0;
                        int bytesRead;
                        var buffer = new byte[contentLength];
                        while (totalBytesRead < contentLength - 1) {
                            bytesRead = stream.Read(buffer, totalBytesRead, contentLength - totalBytesRead);
                            totalBytesRead += bytesRead;
                        }
                        string Response = Encoding.ASCII.GetString(buffer);
                        if ((Response.Contains("user", StringComparison.CurrentCultureIgnoreCase) || Response.Contains("status\": true", StringComparison.CurrentCultureIgnoreCase)) && statusCode == 200) {
                            Success = true;
                            Claimed();
                            Environment.Exit(0);
                            break;
                        } else if (Response.Contains("wait", StringComparison.CurrentCultureIgnoreCase) || Response.Contains("spam", StringComparison.CurrentCultureIgnoreCase)) {
                            BadSessionID++;
                        } 

                        RequestHandled++;
                    
                    }
                }
            }

            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }

        }
        static public SslStream CreateStream(string Proxy = null, int Port = 0, string username = null, string password = null) {
            try {
                string hostname = "i.instagram.com";
                HttpProxyClient HttpProxyClient = null;
                TcpClient client = null;
                if (username != null && password != null) {
                    HttpProxyClient = new HttpProxyClient(Proxy, Port, username, password);
                    HttpProxyClient.TimeoutInSeconds = 60;
                    client = HttpProxyClient.CreateConnection(hostname, 443);
                } else if (Proxy != null && Port != 0) {
                    HttpProxyClient = new HttpProxyClient(Proxy, Port);
                    HttpProxyClient.TimeoutInSeconds = 60;
                    client = HttpProxyClient.CreateConnection(hostname, 443);
                } else {
                    client = new TcpClient();
                    client.Connect(hostname, 443);
                }
                client.NoDelay = true;
                SslStream stream = new SslStream(client.GetStream());
                stream.AuthenticateAsClient(hostname);
                return stream;
            } catch (Exception) {
                return null;
            }
        }
        static public void Claimed() {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(banner);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"You Got it !: {Target}");
        }
        public static void Rate() {
            while (true) {
                int i = RequestHandled;
                Thread.Sleep(1000);
                int f = RequestHandled;
                Speed = f - i;
            }
        }
        public static void SuperVisior() {
            Console.Clear();
            while (true) {
                if (!Success) 
                    Console.Clear();
                    Console.WriteLine();
                    Console.ForegroundColor = ConsoleColor.DarkRed;
                    Console.WriteLine(banner);
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"Requests Sent: {RequestSent}");
                    Console.WriteLine($"Response Handled: {RequestHandled}");
                    Console.WriteLine($"Sessions: {Sessions.Count}");
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Blocked Sessions: {BadSessionID}");
                    Console.WriteLine($"UnReaded Response: {UnRead}");
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"R/S: {Speed}");
                    Thread.Sleep(250);
            }
        }
        public static void Shuffle<T>(IList<T> list) {  
            int n = list.Count;  
            while (n > 1) {  
                n--;  
                int k = random.Next(n + 1);  
                T value = list[k];  
                list[k] = list[n];  
                list[n] = value;  
            }  
	    }
    }
    class Account {
        public Account(string SessionID, Profile profile) {
            this.profile = profile;
            this.SessionID = SessionID;
        }
        public Profile profile;
        public string SessionID;
    }
}
