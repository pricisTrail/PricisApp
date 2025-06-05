using System;
using System.Windows.Forms;
using System.Threading;
using System.Globalization;

namespace PricisApp;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        try
        {
            // Set culture to invariant for consistent number formatting
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            // Enable visual styles
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            // Set default font
            Application.SetDefaultFont(new System.Drawing.Font("Segoe UI", 9F));

            // Run the application
            Application.Run(new Form1());
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                $"An unexpected error occurred: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}",
                "Error",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);
        }
    }
}
