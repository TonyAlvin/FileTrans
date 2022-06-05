using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace fileTran
{
    //////////////////////////Socket连接类/////////////////////////////////////////////////////
    class SocketConnection
    {
        public IPAddress ips;
        public IPEndPoint ipnode;
        public int socketport;
        public bool isclient { get; }
        private Socket socket_commu;
        private Socket socket_ser_listener;

        public SocketConnection()
        {
            socketport = 80;
            ConsoleKey key_input;
            string ipinput = null;
            while (true)
            {
                try
                {
                    while (true)
                    {
                        Console.Write("press \"1\" for server, \"2\" for client:");
                        key_input = Console.ReadKey(false).Key;
                        Console.Write("\n");
                        if (key_input == ConsoleKey.D1)
                        {
                            isclient = false;
                            break;
                        }
                        else if (key_input == ConsoleKey.D2)
                        {
                            isclient = true;
                            break;
                        }
                    }
                    Console.Write("input IP Address:");
                    ipinput = Console.ReadLine();
                    ips = IPAddress.Parse(ipinput);
                    ipnode = new IPEndPoint(ips, socketport);
                    if (isclient)
                        socket_commu = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    else
                        socket_ser_listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    break;
                }
                catch
                {
                    Console.WriteLine("Error input. again:");
                }
            }
        }
        public void connect(int clientnum)
        {
            if (isclient)
            {
                socket_commu.Connect(ipnode);
            }
            else
            {
                Console.WriteLine("bingin connection.");
                socket_ser_listener.Bind(ipnode);   // 开始监听
                socket_ser_listener.Listen(clientnum);
                socket_commu = socket_ser_listener.Accept();
            }
        }
        public void disconnect()
        {
            try
            {
                socket_commu.Disconnect(true);
                socket_ser_listener.Disconnect(true);
            }
            catch
            {

            }
        }
        public void send(byte[] data)
        {
            socket_commu.Send(data);
        }
        public int recv(byte[] buffer)
        {
            try
            {
                return socket_commu.Receive(buffer);
            }
            catch
            {
                return -1;
            }
        }
    }//////////////////////////End of Socket连接类/////////////////////////////////////////////////////.
    struct DataRecv
    {
        public byte[] buffer;
        public int length;
    };

    class Program
    {

        static SocketConnection con1;
        static FileStream f;
        static BinaryWriter bf;

        ///////////////////////////////////////blocks///////////////////////////////////////////////////////////
        private static ActionBlock<DataRecv> fileWriteBlock = new ActionBlock<DataRecv>((input) =>
        {   // 写文件
            if (input.length >= 0)
            {
                byte[] writeTemp = new byte[input.length];
                writeTemp = input.buffer;
                Console.WriteLine("writing file.");
                bf.Write(writeTemp);
            }
            else
            {
                bf.Close();
                f.Close();
            }
        });

        private static TransformBlock<byte[], byte[]> fileReadBlock = new TransformBlock<byte[], byte[]>(p => p);
        private static TransformBlock<byte[], DataRecv> socketRecBlock = new TransformBlock<byte[], DataRecv>(buffer =>
        {   // 收数据
            DataRecv rec = new DataRecv { };
            rec.length = con1.recv(buffer);
            rec.buffer = buffer;
            return rec;
        });

        private static ActionBlock<byte[]> socketTransBlock = new ActionBlock<byte[]>((input) =>
        {   // 发数据
            con1.send(input);
        });///////////////////////////////////////end of blocks///////////////////////////////////////////////////////////
        void Main(string[] args)
        {
            fileReadBlock.LinkTo(socketTransBlock); // 读文件->发送
            socketRecBlock.LinkTo(fileWriteBlock);  // 接受->写文件

            con1 = new SocketConnection();  // 声明Socket类的实例
            con1.connect(1);                // 连接
        }
    }


}