using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PluginManager.Others;

namespace PluginManager.WindowManagement.Controls
{
    public class Label
    {
        public string Text { get; set; }
        public TextType Type { get; set; } = TextType.NORMAL;

        public void Show()
        {
            Logger.SetConsoleColor(Type);
            Console.WriteLine(Text);
            Logger.ResetConsoleColor();
        }

        public void Show(int x, int y)
        {
            Logger.SetConsoleColor(Type);
            Console.SetCursorPosition(x, y);
            Console.WriteLine(Text);

            Logger.ResetConsoleColor();
        }

        public void Show(int x, int y, ConsoleColor color)
        {
            Console.SetCursorPosition(x, y);
            Console.ForegroundColor = color;
            Console.WriteLine(Text);
            Console.ResetColor();
        }

        public void Show(int x, int y, ConsoleColor color, ConsoleColor background)
        {
            Console.SetCursorPosition(x, y);
            Console.ForegroundColor = color;
            Console.BackgroundColor = background;
            Console.WriteLine(Text);
            Console.ResetColor();
        }
    }
}