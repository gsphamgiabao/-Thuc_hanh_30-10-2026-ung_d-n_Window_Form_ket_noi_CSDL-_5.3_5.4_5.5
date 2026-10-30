using System.Drawing;

namespace InventoryManagement
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // To customize application configuration such as set high DPI settings or default font,
            // see https://aka.ms/applicationconfiguration.
            ApplicationConfiguration.Initialize();
            Application.SetDefaultFont(new Font("Segoe UI", 10F));
            Application.Run(new Form1());
        }
    }
}