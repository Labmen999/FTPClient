using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FTP
{

    
    class Program
    {
        static bool state;
        static string name;
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Form2 form2 = new Form2();
            Application.Run(form2);
            
            
        }
    }
}

