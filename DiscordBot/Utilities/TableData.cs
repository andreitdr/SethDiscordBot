using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using DiscordBotCore.Others;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace DiscordBot.Utilities
{
    public class TableData
    {
        public List<string> Columns;
        public List<OneOf<string, IRenderable>[]> Rows;

        public TableData()
        {
            Columns = new List<string>();
            Rows = new List<OneOf<string, IRenderable>[]>();
        }

        public TableData(List<string> columns)
        {
            Columns = columns;
            Rows = new List<OneOf<string, IRenderable>[]>();
        }

        public bool IsEmpty => Rows.Count == 0;
        public bool HasRoundBorders { get; set; } = true;
        public bool DisplayLinesBetweenRows { get; set; } = false;

        public void AddRow(OneOf<string, IRenderable>[] row)
        {
            Rows.Add(row);
        }

        public void PrintTable()
        {
            var table = new Table();
            table.Border(this.HasRoundBorders ? TableBorder.Rounded : TableBorder.Square);
            table.AddColumns(this.Columns.ToArray());
            table.ShowRowSeparators = DisplayLinesBetweenRows;
            foreach (var row in this.Rows)
            {
                table.AddRow(row.Select(element => element.Match(
                    (data) => new Markup(data),
                    (data) => data
                )));
            }

            AnsiConsole.Write(table);
        }
    }
}
