using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Client
{
    class Program
    {
        public static IPAddress GetIpFromHostname(string name, AddressFamily addressFamily = AddressFamily.InterNetwork)
        {
            IPHostEntry entry = Dns.GetHostEntry(name);
            foreach (IPAddress address in entry.AddressList)
            {
                if (address.AddressFamily == addressFamily)
                {
                    return address;
                }
            }
            return null;
        }

        public static IPAddress LocalIP
        {
            get
            {
                return GetIpFromHostname(Dns.GetHostName());
            }
        }

        static int port = 300;

        static string RegisterAccount(IPAddress address, string username, string password)
        {
            TcpClient client = new TcpClient();
            client.Connect(address, port);
            NetworkStream stream = client.GetStream();
            stream.Write(ASCIIEncoding.ASCII.GetBytes("REG&" + username + "&" + password), 0, ASCIIEncoding.ASCII.GetBytes("REG&" + username + "&" + password).Length);
            while (client.Available == 0)
            {

            }
            byte[] buffer = new byte[client.Available];
            stream.Read(buffer, 0, buffer.Length);
            string response = ASCIIEncoding.ASCII.GetString(buffer);
            return response;
        }

        static string UnRegisterAccount(IPAddress address, string username, string password)
        {
            TcpClient client = new TcpClient();
            client.Connect(address, port);
            NetworkStream stream = client.GetStream();
            stream.Write(ASCIIEncoding.ASCII.GetBytes("UNREG&" + username + "&" + password), 0, ASCIIEncoding.ASCII.GetBytes("UNREG&" + username + "&" + password).Length);
            while (client.Available == 0)
            {

            }
            byte[] buffer = new byte[client.Available];
            stream.Read(buffer, 0, buffer.Length);
            string response = ASCIIEncoding.ASCII.GetString(buffer);
            return response;
        }

        static bool Authenticate(IPAddress address, string username, string password)
        {
            TcpClient client = new TcpClient();
            client.Connect(address, port);
            NetworkStream stream = client.GetStream();
            stream.Write(ASCIIEncoding.ASCII.GetBytes("AUTH&" + username + "&" + password), 0, ASCIIEncoding.ASCII.GetBytes("AUTH&" + username + "&" + password).Length);
            while(client.Available == 0)
            {

            }
            byte[] buffer = new byte[client.Available];
            stream.Read(buffer, 0, buffer.Length);
            string response = ASCIIEncoding.ASCII.GetString(buffer);
            return bool.Parse(response);
        }

        static string[] FetchMessages(IPAddress address, string username, string password)
        {
            TcpClient client = new TcpClient();
            client.Connect(address, port);
            NetworkStream stream = client.GetStream();
            stream.Write(ASCIIEncoding.ASCII.GetBytes("FETCH&" + username + "&" + password), 0, ASCIIEncoding.ASCII.GetBytes("FETCH&" + username + "&" + password).Length);
            while (client.Available == 0)
            {

            }
            byte[] buffer = new byte[client.Available];
            stream.Read(buffer, 0, buffer.Length);
            string response = ASCIIEncoding.ASCII.GetString(buffer);
            string[] messages = response.Split('\\');
            return messages;
        }

        static string Send(IPAddress address, string yourname, string msg, string to)
        {
            TcpClient client = new TcpClient();
            client.Connect(address, port);
            NetworkStream stream = client.GetStream();
            stream.Write(ASCIIEncoding.ASCII.GetBytes("MAIL&" + to + "&" + msg + "&" + yourname), 0, ASCIIEncoding.ASCII.GetBytes("MAIL&" + to + "&" + msg + "&" + yourname).Length);
            while (client.Available == 0)
            {

            }
            byte[] buffer = new byte[client.Available];
            stream.Read(buffer, 0, buffer.Length);
            string response = ASCIIEncoding.ASCII.GetString(buffer);
            return response;
        }

        static IPAddress GetAddress(string address)
        {
            try
            {
                return IPAddress.Parse(address);
            }
            catch
            {
                try
                {
                    return GetIpFromHostname(address);
                }
                catch
                {
                    return IPAddress.Any;
                }
            }
        }

        static string clear(IPAddress address, string username, string passcode)
        {
            TcpClient client = new TcpClient();
            client.Connect(address, port);
            NetworkStream stream = client.GetStream();
            stream.Write(ASCIIEncoding.ASCII.GetBytes("CLEAR&"+username+"&"+passcode), 0, ASCIIEncoding.ASCII.GetBytes("CLEAR&" + username + "&" + passcode).Length);
            while (client.Available == 0)
            {

            }
            byte[] buffer = new byte[client.Available];
            stream.Read(buffer, 0, buffer.Length);
            string response = ASCIIEncoding.ASCII.GetString(buffer);
            return response;
        }

        static bool ServerOnline(IPAddress address)
        {
            try
            {
                TcpClient client = new TcpClient();
                client.Connect(address, port);
                NetworkStream stream = client.GetStream();
                stream.Write(ASCIIEncoding.ASCII.GetBytes("PING"), 0, ASCIIEncoding.ASCII.GetBytes("PING").Length);
                while (client.Available == 0)
                {

                }
                byte[] buffer = new byte[client.Available];
                stream.Read(buffer, 0, buffer.Length);
                string response = ASCIIEncoding.ASCII.GetString(buffer);
                if(response == "TRUE")
                {
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        static void Main(string[] args)
        {
            Console.Title = "EMessenger";
            Console.WriteLine("EMessenger\nLocalIP: "+LocalIP);
            while(true)
            {
                Console.Write("[Client]: Server Name>");
                IPAddress address = GetAddress(Console.ReadLine());
                string username = "";
                string password = "";
                bool loggedin = false;
                Console.WriteLine("[Options]: 1. Disconnect");
                Console.WriteLine("[Options]: 2. Login. (logged out only)");
                Console.WriteLine("[Options]: 3. Log Out. (logged in only)");
                Console.WriteLine("[Options]: 4. Send a message");
                Console.WriteLine("[Options]: 5. Register account.(logged out only)");
                Console.WriteLine("[Options]: 6. Fetch Messages. (logged in only)");
                Console.WriteLine("[Options]: 7. Delete Account. (logged in only)");
                Console.WriteLine("[Options]: 8. Clear Messages. (logged in only)");
                while (true)
                {
                    if(!ServerOnline(address))
                    {
                        Console.WriteLine("[Server]: The server is no longer online, isn't a EMessenger server, doesn't exist.");
                        break;
                    }
                    string option = Console.ReadLine();
                    if(option == "1")
                    {
                        break;
                    }
                    else if(option == "2")
                    {
                        if(loggedin)
                        {
                            Console.WriteLine("[Client]: You are logged in. Log out first, then sign in with a different account.");
                        }
                        else
                        {
                            Console.Write("[Input]: Username>");
                            username = Console.ReadLine();
                            Console.Write("[Input]: Password>");
                            password = Console.ReadLine();
                            if (Authenticate(address, username, password))
                            {
                                loggedin = true;
                                Console.WriteLine("[Client]: Succesfully logged in.");
                            }
                            else
                            {
                                Console.WriteLine("[Client]: Failed to log in.");
                                loggedin = false;
                                username = "";
                                password = "";
                            }
                        }
                    }
                    else if(option == "3")
                    {
                        if(!loggedin)
                        {
                            Console.WriteLine("[Client]: You must log in to log out.");
                        }
                        else
                        {
                            loggedin = false;
                            username = "";
                            password = "";
                        }
                    }
                    else if(option == "4")
                    {
                        string name;
                        if(loggedin)
                        {
                            name = username + "@"+address.ToString();
                        }
                        else
                        {
                            name = Environment.UserName + "@" + Environment.MachineName;
                        }
                        Console.Write("[Input]: Send to>");
                        string to = Console.ReadLine();
                        Console.Write("[Input]: Message>");
                        string message = Console.ReadLine();
                        string response = Send(address, name, message, to);
                        Console.WriteLine("[Server]: "+response);
                    }
                    else if(option == "5")
                    {
                        if(loggedin)
                        {
                            Console.WriteLine("[Client]: You must log out to make a new account.");
                        }
                        else
                        {
                            Console.Write("[Input]: Username>");
                            username = Console.ReadLine();
                            Console.Write("[Input]: Password>");
                            password = Console.ReadLine();
                            string response = RegisterAccount(address, username, password);
                            Console.WriteLine("[Server]: " + response);
                            loggedin = true;
                        }
                    }
                    else if(option == "6")
                    {
                        string[] messages = FetchMessages(address, username, password);
                        Console.WriteLine("[Server]: You have " + messages.Length);
                        foreach(string message in messages)
                        {
                            string[] msgargs = message.Split('&');
                            Console.WriteLine("\nFrom: " + msgargs[0] + "   Time Recieved: " + msgargs[2]);
                            Console.WriteLine(msgargs[1]);
                        }
                    }
                    else if(option == "7")
                    {
                        if(!loggedin)
                        {
                            Console.WriteLine("[Client]: You must log in to unregister an account.");
                        }
                        else
                        {
                            string response = UnRegisterAccount(address, username, password);
                            Console.WriteLine("[Server]: " + response);
                            loggedin = false;
                        }
                    }
                    else if(option == "8")
                    {
                        if(!loggedin)
                        {
                            Console.WriteLine("[Client]: You must log in to clear your messages.");
                        }
                        else
                        {
                            Console.WriteLine("[Server]: " + clear(address, username, password));
                        }
                    }
                }
            }
        }
    }
}
