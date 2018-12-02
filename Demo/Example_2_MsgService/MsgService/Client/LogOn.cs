using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Client
{
    public partial class LogOn : Form
    {
        public bool OK { get; set; }

        public string UserName { get; set; }

        public string PassWord { get; set; }


        public LogOn()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.OK = true;
            this.UserName = this.textBox1.Text;
            this.PassWord = this.textBox2.Text;
            this.Close();
        }
    }
}
