using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Container_Socket_Client
{   
    class Container
    {
        /// <summary>
        /// 通知form委托
        /// </summary>
        public Action<string> MessageAction=null;

        /// <summary>
        /// 定时重连
        /// </summary>
        private System.Threading.Timer _Timer;

        public Container()
        {
            _Timer = new System.Threading.Timer(AutoLink,0,TimeSpan.FromSeconds(5),TimeSpan.FromSeconds(5));
        }

        private void AutoLink(object state)
        {
            AsyncConect2server("127.0.0.1",12011);
        }

        /// <summary>
        /// 异步链接服务器
        /// </summary>
        /// <param name="Ip"></param>
        /// <param name="Port"></param>
        public void AsyncConect2server(string Ip,int Port)
        {
            IPEndPoint IPE = new IPEndPoint(IPAddress.Parse(Ip), Port);
            Socket Client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IAsyncResult ar= Client.BeginConnect(IPE, new AsyncCallback(ConnectCallBack), Client);
            ar.AsyncWaitHandle.WaitOne();
            MessageAction?.Invoke("start link to server");
            AsyncReceive(Client);
        }

        /// <summary>
        /// 链接回调
        /// </summary>
        /// <param name="ar"></param>
        private void ConnectCallBack(IAsyncResult ar)
        {
            try
            {
                Socket Client = (Socket)ar.AsyncState;
                Client.EndConnect(ar);
                MessageAction?.Invoke(string.Format("Connect server：{0} \n",Client.RemoteEndPoint.ToString()));
                _Timer.Change(-1, -1);//停止定时器
            }
            catch (SocketException ex)
            {
                MessageAction?.Invoke(string.Format("An error occurred when attempting to access the socket：{0}\n", ex.ToString()));
            }
            catch(ObjectDisposedException ex)
            {
                MessageAction?.Invoke(string.Format("The Socket has been closed：{0}\n", ex.ToString()));
            }            
        }

        /// <summary>
        /// 异步接收数据
        /// </summary>
        /// <param name="Client"></param>
        public void AsyncReceive(Socket Client)
        {
            try
            {
                Client.BeginReceive(DATA.buffer,0 ,DATA.SIZE,0,new AsyncCallback(ReceiveCallBack),Client);
            }
            catch (Exception ex)
            {
                MessageAction?.Invoke(string.Format("link error：\n", ex.ToString()));
            }
        }

        /// <summary>
        /// 异步接收回调
        /// </summary>
        /// <param name="ar"></param>
        private void ReceiveCallBack(IAsyncResult ar)
        {
            try
            {
                Socket Client = (Socket)ar.AsyncState;
                int DataSize = Client.EndReceive(ar);
                if(DataSize>0)
                {
                    Client.BeginReceive(DATA.buffer, 0, DATA.SIZE, 0, new AsyncCallback(ReceiveCallBack), Client);
                    string str = System.Text.Encoding.ASCII.GetString(DATA.buffer, 0, DataSize).Trim();
                    if (str.StartsWith("[C"))//判断最总结果
                    {                        
                        MessageAction?.Invoke(string.Format("Get Date：{0}", DATA.SplitData(str).Trim()));
                    }
                }
                else
                {
                    MessageAction?.Invoke("link of close \n");
                    _Timer.Change(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(5));
                }
            }
            catch (Exception ex)
            {
                MessageAction?.Invoke(ex.ToString());
            }
        }
    }
    class DATA
    {
        public static int SIZE = 4096;
        public static byte[] buffer = new byte[SIZE];

        /// <summary>
        /// 分解数据
        /// </summary>
        /// <param name="str"></param>
        public static string SplitData(string str)
        {
            string[] tmpString = str.Split('|');
            tmpString[tmpString.Length - 1]=tmpString[tmpString.Length - 1].Split(']')[0];
            Dictionary<string, string> dict = new Dictionary<string, string>();
            dict["TriggerTime"] = tmpString[1];
            dict["LaneNum"] = tmpString[2];
            dict["ContainerType"]= tmpString[3];
            dict["ContainerNum1"] = tmpString[4];
            dict["CheckNum1"] = tmpString[5];
            if(tmpString.Length==7)//单箱
            {
                dict["ISO1"] = tmpString[6];
            }
            else//双箱==9
            {
                dict["ContainerNum2"] = tmpString[6];
                dict["CheckNum2"] = tmpString[7];
                dict["ISO1"] = tmpString[8];
                dict["ISO2"] = tmpString[9];
            }
            string jsonStr = JsonConvert.SerializeObject(dict);
            return jsonStr;
        }

    }
}
