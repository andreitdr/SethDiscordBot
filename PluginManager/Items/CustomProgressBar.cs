using System;
namespace PluginManager.Items
{
    public class CustomProgressBar
    {
        private const char _block = '#';
        private const char _emptyBlock = ' ';
        private const char _leftMargin = '[';
        private const char _rightMargin = ']';

        const string _back = "\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b";
        public static void WriteProgressBar(int percent)
        {
            Console.Write(_back);
            Console.Write(_leftMargin);
            var p = (int)((percent / 10f) + .5f);
            for (var i = 0; i < 10; ++i)
            {
                if (i >= p)
                    Console.Write(_emptyBlock);
                else
                    Console.Write(_block);
            }
            Console.Write($"{_rightMargin} " + percent + " %");
        }
    }
}
