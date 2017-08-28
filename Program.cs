using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace swf2lm
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            //Application.EnableVisualStyles();
            //Application.SetCompatibleTextRenderingDefault(false);
            //Application.Run(new MainForm());

            var swf = new SWF(@"C:\Users\mOatles\Documents\lumen.swf");
            //var swf = new SWF(@"C:\Users\mOatles\Downloads\space_people.swf");
        }
    }
}
