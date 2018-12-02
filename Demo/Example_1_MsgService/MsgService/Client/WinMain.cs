using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using ZYSocket.Client;
using ZYSocket.FiberStream;

namespace Client
{
    public partial class WinMain : Form
    {
        SocketClient client;
        public WinMain()
        {
            InitializeComponent();
        }

        private async void WinMain_Load(object sender, EventArgs e)
        {
            client = new SocketClient();
            client.BinaryInput += Client_BinaryInput;
            client.Disconnect += Client_Disconnect;
            await Connect("127.0.0.1",3000);
        }

        private void Client_Disconnect(ISocketClient client, ZYSocket.ISockAsyncEventAsClient socketAsync, string msg)
        {
            MessageBox.Show(msg);

            this.Close();
        }

        private async void Client_BinaryInput(ISocketClient client, ZYSocket.ISockAsyncEventAsClient socketAsync)
        {
            var fiberRw = await socketAsync.GetFiberRwSSL(null,"");

            if(fiberRw==null)
            {
                client.ShutdownBoth();
            }


            for(; ; )
            {
                try
                {
                    await ReadCommand(fiberRw);
                }
                catch(Exception er)
                {
                    MessageBox.Show(er.ToString());
                    break;
                }
            }

            fiberRw.Disconnect();
        }

               
        async Task ReadCommand(IFiberRw fiberRw)
        {
            var cmd = await fiberRw.ReadInt32();

            switch(cmd)
            {
                case 1001:
                    {
                        var isSuccess = await fiberRw.ReadBoolean();

                        if(isSuccess.GetValueOrDefault())
                        {
                            await fiberRw.ReadString();

                            fiberRw.Write(2000);
                            await fiberRw.Flush();

                        }
                        else
                        {
                            MessageBox.Show(await fiberRw.ReadString());
                            this.BeginInvoke(new EventHandler((a, b) => LogOn()));                           
                        }
                    }
                    break;
                case 2001:
                    {
                        var list = await fiberRw.ReadObject<List<string>>();

                        this.BeginInvoke(new EventHandler((a, b) =>
                        {
                            this.listView1.Items.Clear();
                            this.comboBox1.Items.Clear();
                            foreach (var item in list)
                            {
                                this.listView1.Items.Add(new ListViewItem(item));
                            }
                            this.comboBox1.Items.Add("ALL");
                            this.comboBox1.Items.AddRange(list.ToArray());
                        }));
                     
                    }
                    break;
                case 2002://通知新用户登入
                    {
                        var user = await fiberRw.ReadString();

                        this.BeginInvoke(new EventHandler((a, b) =>
                        {
                            this.listView1.Items.Add(user,user,0);
                            this.comboBox1.Items.Add(user);
                        }));
                    }
                    break;
                case 3001:
                    {
                        string username = await fiberRw.ReadString();
                        string msg = await fiberRw.ReadString();
                        this.BeginInvoke(new EventHandler((a, b) =>
                        {
                            this.richTextBox1.AppendText($"{username}:{msg}\r\n");
                        }));
                    }
                    break;
                case 3002:
                    {
                        string username = await fiberRw.ReadString();
                        string msg = await fiberRw.ReadString();

                        this.BeginInvoke(new EventHandler((a, b) =>
                        {
                            this.richTextBox1.AppendText($"{username}>>{msg}\r\n");
                        }));
                    }
                    break;
                case 4000:
                    {
                        string username = await fiberRw.ReadString();


                        this.BeginInvoke(new EventHandler((a, b) =>
                        {
                            this.listView1.Items.RemoveByKey(username);
                            this.comboBox1.Items.Remove(username);
                        }));

                    }
                    break;

            }
        }



        private async Task Connect(string host,int port)
        {
           var (isOK,Msg)=await client.ConnectAsync(host,port);

            if(!isOK)
            {
                MessageBox.Show(Msg);
                this.Close();
            }
            else
            {
                LogOn();
            }
        }

        private async void LogOn()
        {
            LogOn logOnWin = new LogOn();
            logOnWin.ShowDialog();

            if (logOnWin.OK)
            {

                var fiberRw = await client.GetFiberRw();
                fiberRw.Write(1000);
                fiberRw.Write(logOnWin.UserName);
                fiberRw.Write(logOnWin.PassWord);
                await fiberRw.Flush();

            }
            else
                this.Close();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var fiberRw = await client.GetFiberRw();

            fiberRw.Write(3000);
            fiberRw.Write(this.comboBox1.Text);
            fiberRw.Write(this.textBox1.Text);
            await fiberRw.Flush();

            this.richTextBox1.AppendText($"->{this.comboBox1.Text}:{this.textBox1.Text}\r\n");

            this.textBox1.Text = "";
        }
    }
}
