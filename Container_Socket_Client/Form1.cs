using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Container_Socket_Client
{
    public partial class Form1 : Form
    {
        private Container _Container = new Container();
        private delegate void MessageDelegate(string message);

        public Form1()
        {
            InitializeComponent();
            _Container.MessageAction += MessageForm;
        }

        private void MessageForm(string obj)
        {
            if(textBox1.InvokeRequired)
            {
                textBox1.Invoke(new MessageDelegate(MessageForm), new object[] { obj });
            }
            else
            {
                textBox1.AppendText(obj);
                textBox1.AppendText("\r\n");
            }
        }
    }
}
