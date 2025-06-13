using System;
using System.Windows.Forms;

static class Program {
    [STAThread]
    static void Main() {  //exceptions for preventing sudden errors
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        //!
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (sender, e) => {
            //!
            if (!(e.Exception is IndexOutOfRangeException)) {
                MessageBox.Show(e.Exception.Message, "Unexpected Error");
            }
        };
        Application.Run(new ChessDisplay());
    }
}
