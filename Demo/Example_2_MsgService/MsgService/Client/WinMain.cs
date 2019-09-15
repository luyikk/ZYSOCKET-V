using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO.Compression;
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

        private  void WinMain_Load(object sender, EventArgs e)
        {
            client = new SocketClient();
            client.BinaryInput += Client_BinaryInput;
            client.Disconnect += Client_Disconnect;
             Connect("127.0.0.1",3000);
        }

        private void Client_Disconnect(ISocketClient client, ZYSocket.ISockAsyncEventAsClient socketAsync, string msg)
        {
            this.BeginInvoke(new EventHandler((a, b) =>
                {
                    MessageBox.Show(msg);
                    this.Close();
                }));

        }

        private async void Client_BinaryInput(ISocketClient client, ZYSocket.ISockAsyncEventAsClient socketAsync)
        {
            var res = await socketAsync.GetFiberRwSSL(null, "localhost");  //我们在这地方使用SSL加密


            if (res.IsError)
            {
                MessageBox.Show(res.ErrMsg);
                client.ShutdownBoth();
                return;
            }

            client.SetConnected();

            for(; ; )
            {
                try
                {
                    await ReadCommand(res.FiberRw);
                }
                catch(Exception er)
                {
                    MessageBox.Show(er.ToString());
                    break;
                }
            }

            client.ShutdownBoth();
        }

               
        async Task ReadCommand(IFiberRw fiberRw)
        {
            var cmd = await fiberRw.ReadInt32();

            switch(cmd)
            {
                case 1001:
                    {
                        var isSuccess = await fiberRw.ReadBoolean();

                        if(isSuccess)
                        {
                            await fiberRw.ReadString();

                            await await fiberRw.Sync.Ask(() =>
                            {
                                fiberRw.Write(2000);
                                return fiberRw.Flush();
                            });

                        }
                        else
                        {
                            string msg = await fiberRw.ReadString();
                           
                            this.BeginInvoke(new EventHandler((a, b) => {
                                MessageBox.Show(msg);
                                LogOn(); }));                           
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



        private  void Connect(string host,int port)
        {
           var result= client.Connect(host,port,6000);

            if(!result.IsSuccess)
            {
                MessageBox.Show(result.Msg);
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

                await await fiberRw.Sync.Ask(() =>
                {
                    fiberRw.Write(1000);
                    fiberRw.Write(logOnWin.UserName);
                    fiberRw.Write(logOnWin.PassWord);
                    return fiberRw.Flush();
                });

            }
            else
                this.Close();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            var fiberRw = await client.GetFiberRw();

            await await fiberRw.Sync.Ask(() =>
            {
                fiberRw.Write(3000);
                fiberRw.Write(this.comboBox1.Text);
                fiberRw.Write(this.textBox1.Text);
                return fiberRw.Flush();
            });

            this.richTextBox1.AppendText($"->{this.comboBox1.Text}:{this.textBox1.Text}\r\n");

            this.textBox1.Text = "";
        }
    }
}
