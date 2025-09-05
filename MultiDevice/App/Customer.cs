using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MultiDevice
{

    class Customer
    {
        public Socket socket;
        public List<indices_enu> indices;
        public double[] TimeElapsed = Enumerable.Repeat(double.MaxValue, 333).ToArray();
        //一个特殊的数字，防止有其他数字重合
        public DateTime[] test_fetch = new DateTime[333];

        public string SocketIP { get { return theIP; } }
        string theIP = "";
        public Customer(Socket s, List<indices_enu> x)
        {
            socket = s;
            indices = x;
            theIP= (s.RemoteEndPoint as IPEndPoint).Address.ToString();
        }

        public bool SocketIsAlive()
        {
            try
            {
                return !(socket.Poll(500, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException)
            {
                return false;
            }
        }
    }
}
