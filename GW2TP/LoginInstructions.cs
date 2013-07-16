using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GW2TP
{
    public partial class LoginInstructions : Form
    {
        GW2TP mainForm;

        public LoginInstructions(GW2TP mainForm)
        {
            this.mainForm = mainForm;
            InitializeComponent();
        }

        private void OK_Click(object sender, EventArgs e)
        {
            this.Close();
            this.DialogResult = DialogResult.OK;
        }
    }
}
