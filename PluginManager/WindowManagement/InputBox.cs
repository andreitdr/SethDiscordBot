using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PluginManager.WindowManagement.Controls;
using PluginManager.Others;

namespace PluginManager.WindowManagement
{
    public class InputBox
    {
        public string Title { get; set; }
        public string Message { get; set; }

        private List<TextBox> options = new List<TextBox>();
        private List<Label> labels = new List<Label>();

        private string InputStr = "=> ";

        public List<string> Show()
        {
            List<string> result = new List<string>();
            Console.Clear();
            Console.WriteLine(Title);
            Console.WriteLine(Message);

            foreach (var label in labels)
                label.Show();
            foreach (var option in options)
            {   
                option.Label.Show();
                while (true)
                {
                    Console.Write(InputStr);
                    if(option.SetText(Console.ReadLine()))
                    {
                        result.Add(option.Text);
                        break;
                    }
                }
                

            }

            return result;
        }

        public void AddOption(string text, Func<string, bool> isValid)
        {
            options.Add(new TextBox() { Label = new Label() {Text = text}, IsValid = isValid });
        }

        public void AddLabel(string text, TextType type)
        {
            labels.Add(new Label() {Text = text, Type  = type});
        }


    }
    
}