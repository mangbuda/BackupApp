using BackupAppService;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BackupApp
{
    public partial class Main : Form
    {
        SqliteReaderService DataSvc = new SqliteReaderService();
        public Main()
        {
            InitializeComponent();
            DataTable dt = DataSvc.ReadAsDataTable(@"select * from SETTING;");
            dataGridView1.Columns.Clear();
            dataGridView1.DataSource = dt;

        }
    }
}
