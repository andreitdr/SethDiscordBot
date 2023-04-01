using PluginManager.WindowManagement;

InputBox inputBox = new InputBox();
inputBox.Title = "Test";
inputBox.Message = "This is a test";
inputBox.AddOption("Enter a number", (input) => int.TryParse(input, out int result));
inputBox.AddOption("Enter a string", (input) => !string.IsNullOrEmpty(input));
inputBox.AddOption("Enter a number", (input) => int.TryParse(input, out int result));

var result = inputBox.Show();

foreach (var item in result)
{
    Console.WriteLine(item);
}

