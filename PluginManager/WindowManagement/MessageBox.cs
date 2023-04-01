using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PluginManager.WindowManagement.Controls;
namespace PluginManager.WindowManagement
{
    public class MessageBox
    {
        public string Title { get; set; }
        public string Message { get; set; }

        private List<ConsoleOption> options = new List<ConsoleOption>();

        public int OptionsCount {get => options.Count;}

        public MessageBox(string title, string message)
        {
            Title = title;
            Message = message;
        }

        public void AddOption(string text, Action action)
        {
            options.Add(new ConsoleOption() { Text = text, Index = (byte)(options.Count+1), Action = action});
        }

        public void AddRangeOptions(List<string> texts, List<Action> actions)
        {
            for (int i = 0; i < texts.Count; i++)
            {
                options.Add(new ConsoleOption() { Text = texts[i], Index = (byte)(options.Count + 1), Action = actions[i] });
            }
        }

        public int Show()
        {
            Console.Clear();
            Console.WriteLine(Title);
            Console.WriteLine(Message);

            foreach (var option in options)
            {
                Console.WriteLine($"{option.Index}. {option.Text}");
            }

            if(options.Count == 0)
            {
                Console.WriteLine("Press any key to continue...");
                Console.ReadKey();
                return 0;
            }

            if(int.TryParse(Console.ReadLine(), out int result))
            {
                if(result > 0 && result <= options.Count)
                {
                    if(options[result - 1].Action != null)
                            options[result - 1].Action();
                    return result;
                }
            }

            return -1;


        }

    }
}