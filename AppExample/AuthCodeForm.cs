using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace VkCheatApi
{
    public partial class AuthCodeForm : Form
    {
        public int Code { get; private set; }

        public AuthCodeForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            int code;
            bool isSuccess = int.TryParse(textBox1.Text, out code);
            if (!isSuccess)
            {
                MessageBox.Show("Ошибка", "Введено не число.");
                return;
            }

            Code = code;
            this.Close();
        }
    }
}
