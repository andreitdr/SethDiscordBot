using System;
using System.Collections.Generic;
using System.Linq;
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

        public Table AsTable()
        {

           var table = new Table();
            table.Border(this.HasRoundBorders ? TableBorder.Rounded : TableBorder.Square);
            table.AddColumns(this.Columns.ToArray());
            table.ShowRowSeparators = DisplayLinesBetweenRows;
            foreach (var row in this.Rows)
            {
                table.AddRow(row.Select(element => element.Match(
                    (string data) => new Markup(data),
                    (IRenderable data) => data
                )));
            }

            table.Alignment(Justify.Center);

            return table;
        }

        public void PrintTable()
        {
            if (IsEmpty) return;
            AnsiConsole.Write(this.AsTable());
        }
    }
}
