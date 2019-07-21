using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Server
{
    struct Message
    {
        public string from, contents;
        public DateTime time;
    }

    class Account
    {
        public string username;
        public string passcode;
        public List<Message> messages;

        public Account(string username, string passcode)
        {
            this.username = username;
            this.passcode = passcode;
            messages = new List<Message>();
            if(!File.Exists(Environment.CurrentDirectory+"\\"+username+"_messages.dat"))
            {
                File.Create(Environment.CurrentDirectory + "\\" + username + "_messages.dat").Close();
            }
            else
            {
                string[] lines = File.ReadAllLines(Environment.CurrentDirectory + "\\" + username + "_messages.dat");
                foreach(string line in lines)
                {
                    Message msg = new Message();
                    string[] args = line.Split('&');
                    msg.from = args[0];
                    msg.contents = args[1];
                    msg.time = DateTime.Parse(args[2]);
                    messages.Add(msg);
                }
            }
        }

        public void Save()
        {
            string[] lines = new string[messages.Count];
            for (int i = 0; i < messages.Count; i++)
            {
                lines[i] = messages[i].from + "&" + messages[i].contents + "&" + messages[i].time.ToString();
            }
            File.WriteAllLines(Environment.CurrentDirectory + "\\" + username + "_messages.dat", lines);
        }
    }

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

        static List<Account> accounts;

        static void LoadAccounts()
        {
            string[] lines = File.ReadAllLines(Environment.CurrentDirectory + "\\accounts.dat");
            accounts.Clear();
            foreach(string line in lines)
            {
                string[] args = line.Split('&');
                accounts.Add(new Account(args[0], args[1]));
            }
        }

        static void SaveAccounts()
        {
            string[] lines = new string[accounts.Count];
            for (int i = 0; i < accounts.Count; i++)
            {
                lines[i] = accounts[i].username +"&" +accounts[i].passcode;
                accounts[i].Save();
            }
            File.WriteAllLines(Environment.CurrentDirectory + "\\accounts.dat",lines);
        }

        static bool PushMessage(string username, string msg, string from)
        {
            foreach(Account account in accounts)
            {
                if(account.username == username)
                {
                    Message message = new Message();
                    message.from = from;
                    message.contents = msg;
                    message.time = DateTime.Now;
                    account.messages.Add(message);
                    return true;
                }
            }
            return false;
        }

        static bool Exists(string username)
        {
            foreach (var account in accounts)
            {
                if(account.username == username)
                {
                    return true;
                }
            }
            return false;
        }
        
        static Message[] GetMessages(string username, string password)
        {
            foreach (Account account in accounts)
            {
                if (account.username == username && password == account.passcode)
                {
                    return account.messages.ToArray();
                }
            }
            return null;
        }

        static bool Authenticate(string username, string password)
        {
            foreach (Account account in accounts)
            {
                if(account.username == username && account.passcode == password)
                {
                    return true;
                }
            }
            return false;
        }

        static int GetAccountIndex(string username, string password)
        {
            for (int i = 0; i < accounts.Count; i++)
            {
                if (accounts[i].username == username && accounts[i].passcode == password) 
                {
                    return i;
                }
            }
            return -1;
        }

        static void HandleTcpClient(TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            while(client.Available == 0)
            {
               
            }
            byte[] buffer = new byte[client.Available];
            stream.Read(buffer, 0, client.Available);
            string recdata = ASCIIEncoding.ASCII.GetString(buffer);
            string[] args = recdata.Split('&');
            string response = "Invalid command or argument length.";
            if(args.Length == 4)
            {
                if(args[0] == "MAIL")
                { 
                    if(PushMessage(args[1], args[2], args[3]))
                    {
                        Console.WriteLine("[Server]: Succesfully sent message to " + args[1] + ".");
                        response = "Message succesfully sent";
                        SaveAccounts();
                    }
                    else
                    {
                        Console.WriteLine("[Server]: Failed to sent message to " + args[1] + ".");
                        response = "Reciever not found";
                    }
                }
            }
            else if(args.Length == 3)
            {
                if (args[0] == "FETCH")
                {
                    Message[] messages = GetMessages(args[1], args[2]);
                    if (messages == null)
                    {
                        Console.WriteLine("[Server]: " + args[1] + " tried to fetch messages but failed.");
                        response = "Server Error&Incorrect username and password&"+DateTime.Now;
                    }
                    else if(messages.Length != 0)
                    {
                        response = messages[0].from + "&" + messages[0].contents + "&" + messages[0].time.ToString();
                        for (int i = 1; i < messages.Length; i++)
                        {
                            response += "\\" +messages[i].from + "&" + messages[i].contents + "&" + messages[i].time.ToString();
                        }
                        Console.WriteLine("[Server]: " + args[1] + " fetched "+messages.Length + " messages.");
                    }
                    else
                    {
                        response = "Server Error&No Messages Recieved&"+DateTime.Now;
                        Console.WriteLine("[Server]: " + args[1] + " fetched " + messages.Length + " messages.");
                    }
                }
                else if(args[0] == "REG")
                {
                    if (Exists(args[1]))
                    {
                        response = "Username already taken.";
                    }
                    else
                    {
                        accounts.Add(new Account(args[1], args[2]));
                        response = "Account succesfully registered.";
                        Console.WriteLine("[Server]: Account " + args[1] + " created.");
                        SaveAccounts();
                    }
                    SaveAccounts();
                }
                else if(args[0] == "UNREG")
                {
                    int index = GetAccountIndex(args[1], args[2]);
                    if(index != -1)
                    {
                        accounts.RemoveAt(index);
                        response = "Account succesfully deleted.";
                        Console.WriteLine("[Server]: Account "+args[1]+" deleted.");
                        SaveAccounts();
                    }
                    else
                    {
                        response = "Incorrect username or password.";
                        Console.WriteLine("[Server]: Account " + args[1] + " tried to delete their account.");
                    }
                }
                else if(args[0] == "CLEAR")
                {
                    int index = GetAccountIndex(args[1], args[2]);
                    if(index != -1)
                    {
                        accounts[index].messages.Clear();
                        response = "Messages have been cleared";
                        SaveAccounts();
                        Console.WriteLine("[Server]: Cleared " + args[1] + " messages.");
                    }
                    else
                    {
                        response = "Incorrect username or password.";
                    }
                }
                else if(args[0] == "AUTH")
                {
                    if(Authenticate(args[1],args[2]))
                    {
                        response = "True";
                        Console.WriteLine("[Server]: " + args[1] + " authenticated and succeded.");
                    }
                    else
                    {
                        response = "False";
                        Console.WriteLine("[Server]: " + args[1] + " authenticated and failed.");
                    }
                }
            }
            else if(args.Length == 1)
            {
                if(args[0] == "PING")
                {
                    response = "TRUE";
                }
            }
            buffer = ASCIIEncoding.ASCII.GetBytes(response);
            stream.Write(buffer, 0, buffer.Length);
            Thread.Sleep(100);
            stream.Close();
            client.Close();
        }

        static TcpListener listener;

        static void Main(string[] args)
        {
            Console.Title = "EMessenger Server";
            Console.WriteLine("[Server]: Initializing Tcp Listener...");
            listener = new TcpListener(LocalIP,port);
            listener.Start();
            Console.WriteLine("[Server]: Loading account names...");
            accounts = new List<Account>();
            if(!File.Exists(Environment.CurrentDirectory+"\\accounts.dat"))
            {
                Console.WriteLine("[Server]: Creating accounts file...");
                File.Create(Environment.CurrentDirectory + "\\accounts.dat").Close();
            }
            else
            {
                LoadAccounts();
            }
            Console.Clear();
            Console.WriteLine("EMessenger Server");
            Console.WriteLine("Accounts: " + accounts.Count+"\nIpAddress: "+LocalIP+"\nHostname: "+Dns.GetHostName());
            Thread serverthread = new Thread(new ThreadStart(DoServerThread));
            serverthread.Start();
            while(true)
            {
                string command = Console.ReadLine().ToUpper();
                if(command == "LIST")
                {
                    Console.WriteLine("Username:".PadRight(12)+"Password:".PadRight(12)+"Message Count:".PadRight(16)+"Time of Last Message:");
                    foreach (Account account in accounts)
                    {
                        if(account.messages.Count == 0)
                        {
                            Console.WriteLine(account.username.PadRight(12) + account.passcode.PadRight(12) + account.messages.Count.ToString().PadRight(16) + "N/A");
                        }
                        else
                        {
                            Console.WriteLine(account.username.PadRight(12) + account.passcode.PadRight(12) + account.messages.Count.ToString().PadRight(16) + account.messages[account.messages.Count - 1].time);
                        }
                    }
                }
                else if(command == "BACKUP")
                {
                    Console.WriteLine("[Server]: Backing up server files...");
                    Console.WriteLine("[Server]: Saving files...");
                    SaveAccounts();
                    Console.WriteLine("[Server]: Creating backup directory...");
                    string dirname = "backup_" + DateTime.Now;
                    dirname = dirname.Replace("/", "-");
                    dirname = dirname.Replace(":", "-");
                    Directory.CreateDirectory(Environment.CurrentDirectory+"\\"+dirname);
                    Console.WriteLine("[Server]: Copying account data file...");
                    File.Copy(Environment.CurrentDirectory + "\\accounts.dat", Environment.CurrentDirectory + "\\" + dirname + "\\accounts.dat");
                    foreach(Account account in accounts)
                    {
                        Console.WriteLine("[Server]: Copying " + account.username + "'s messages.");
                        File.Copy(Environment.CurrentDirectory + "\\" + account.username + "_messages.dat", Environment.CurrentDirectory + "\\" + dirname + "\\" + account.username + "_messages.dat");
                    }
                }
                else if(command == "BACKUPLIST")
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(Environment.CurrentDirectory);
                    foreach(DirectoryInfo directory in directoryInfo.GetDirectories())
                    {
                        if(directory.Name.StartsWith("backup_"))
                        {
                            Console.WriteLine(directory.Name.TrimStart("backup_".ToCharArray()));
                        }
                    }
                }
                else if(command == "CLEARBACKUPS")
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(Environment.CurrentDirectory);
                    foreach (DirectoryInfo directory in directoryInfo.GetDirectories())
                    {
                        if (directory.Name.StartsWith("backup_"))
                        {
                            Console.WriteLine("[Server]: Deleting " + directory.Name + "...");
                            foreach(FileInfo file in directory.GetFiles())
                            {
                                file.Delete();
                            }
                            directory.Delete();
                        }
                    }
                }
                else if (command == "RESTORE")
                {
                    Console.WriteLine("Backup Restoration Wizard");
                    Console.Write("DateTime>");
                    string dirname = Environment.CurrentDirectory+"\\backup_" + Console.ReadLine();
                    if(Directory.Exists(dirname))
                    {
                        foreach(string file in Directory.GetFiles(dirname))
                        {
                            string mfile = new FileInfo(file).Name;
                            Console.WriteLine("[Server]: Moving " + mfile);
                            if(File.Exists(Environment.CurrentDirectory+"\\"+mfile))
                            {
                                File.Delete(Environment.CurrentDirectory + "\\" + mfile);
                            }
                            File.Copy(dirname+"\\"+mfile, Environment.CurrentDirectory + "\\" + mfile);
                        }
                        Console.WriteLine("[Server]: Reloading data.");
                        LoadAccounts();
                    }
                    else
                    {
                        Console.WriteLine("[Error]: No backup for that time can be loaded.");
                    }
                }
                else if(command == "STOP")
                {
                    SaveAccounts();
                    serverthread.Abort();
                    break;
                }
                else if(command == "SAVE")
                {
                    Console.WriteLine("[Server]: Saving accounts...");
                    SaveAccounts();
                }
                else if(command == "HELP")
                {
                    Console.WriteLine("LIST         Lists all account information.");
                    Console.WriteLine("BACKUP       Backups the server.");
                    Console.WriteLine("BACKUPLIST   Provides a list of backup directories.");
                    Console.WriteLine("CLEARBACKUPS Delets all backup directories.");
                    Console.WriteLine("RESTORE      Restores server to a backup");
                    Console.WriteLine("STOP         Stops the server.");
                    Console.WriteLine("SAVE         Saves the servers data.");
                }
                else
                {
                    Console.WriteLine("[Error]: Couldn't recognize the command '"+command+"'.");
                }
            }
        }

        static void DoServerThread()
        {
            while (true)
            {
                if (listener.Pending())
                {
                    HandleTcpClient(listener.AcceptTcpClient());
                }
            }
        }
    }
}