using AnalaizerClassLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GraphInterface
{
    static class Program
    {
        // Імпорт функцій WinAPI з kernel32.dll для роботи з консоллю
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool AllocConsole();
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool FreeConsole();

        // Під'єднання до консолі батьківського процесу (cmd, з якого запущено програму)
        [DllImport("kernel32.dll")]
        static extern bool AttachConsole(int dwProcessId);
        private const int ATTACH_PARENT_PROCESS = -1;

        /// <summary>
        /// Точка входу. Якщо передано аргумент, програма працює в консольному режимі
        /// (обчислює вираз і виводить результат), інакше відкриває графічне вікно.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            int argCount = args == null ? 0 : args.Length;

            // Консольний режим: GraphInterface.exe "2+3*4"
            if (argCount > 0)
            {
                // WinForms-програма не має власної консолі, тому під'єднуємось до cmd.
                // Виклик має бути до першого Console.WriteLine()
                AttachConsole(ATTACH_PARENT_PROCESS);

                AnalaizerClass.expression = args[0];

                // Затираємо prompt cmd, який встиг вивестись у поточний рядок
                int length = Console.CursorLeft;
                Console.SetCursorPosition(0, Console.CursorTop);
                Console.WriteLine(new string(' ', length));
                Console.SetCursorPosition(0, Console.CursorTop);

                Console.WriteLine("Expression:" + AnalaizerClass.expression);
                string result = AnalaizerClass.Estimate();

                // Успішний результат виводиться зеленим, помилка червоним.
                // Ознака помилки: символ '&' на початку рядка
                ConsoleColor color = ConsoleColor.Green;

                if (result.StartsWith("&"))
                {
                    result = result.TrimStart('&');
                    color = ConsoleColor.Red;
                }
                else
                    result = result + Environment.NewLine + "Error: 0";

                // Виводимо результат потрібним кольором і повертаємо попередній колір
                ConsoleColor current = Console.ForegroundColor;
                Console.ForegroundColor = color;
                Console.OutputEncoding = Encoding.UTF8;   // коректне відображення кирилиці в повідомленнях
                Console.WriteLine("Result: " + result);
                Console.ForegroundColor = current;

                return;
            }

            // Графічний режим: запуск головного вікна калькулятора
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}