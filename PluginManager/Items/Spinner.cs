using System;
using System.Threading.Tasks;

namespace PluginManager.Items
{
    public class Spinner
    {
        public bool isSpinning;

        public Spinner()
        {
            isSpinning = false;
        }

        public async void Start()
        {
            isSpinning = true;
            int cnt = 0;

            while (isSpinning)
            {
                cnt++;
                switch (cnt % 4)
                {
                    case 0: Console.Write("/"); break;
                    case 1: Console.Write("-"); break;
                    case 2: Console.Write("\\"); break;
                    case 3: Console.Write("|"); break;
                }
                Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                await Task.Delay(500);
            }
        }

        public void Stop()
        {
            isSpinning = false;
        }
    }
}