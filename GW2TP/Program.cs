using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using GW2Miner.Domain;

namespace GW2TP
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            GW2TP mainForm = new GW2TP();
            try
            {
                Application.Run(mainForm);
            }
            catch (Exception e)
            {
                //string errorMsg = e.Message + e.StackTrace;
                //errorMsg = errorMsg.Replace('"', '\'').Replace("\r\n", @"\n");
                MessageBox.Show(ExceptionHelper.FlattenException(e));

                string errorMsg = e.ToString();
                Logger fileLogger = Logger.Instance;
                fileLogger.CreateEntry(errorMsg);
            }
            finally
            {
                mainForm.CleanUp();
            }
        }
    }
}
