using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Jupyter.Core
{
    public interface ITable
    {
        string[] Headers { get; }
        string[][] Cells { get; }
    }

    public class Table<TRow> : ITable
    {
        public List<(string, Func<TRow, string>)> Columns;
        public List<TRow> Rows;

        public string[] Headers =>
            Columns
                .Select(header => header.Item1)
                .ToArray();

        public string[][] Cells =>
            Rows
                .Select(
                    row => Columns
                        .Select(column => column.Item2(row))
                        .ToArray()
                )
                .ToArray();
    }

    public class TableToHtmlDisplayEncoder : IResultEncoder
    {
        public string MimeType => MimeTypes.Html;
        public EncodedData? Encode(object displayable)
        {
            if (displayable is ITable table)
            {
                var headers = table.Headers;
                var cells = table.Cells;

                return new EncodedData
                {
                    Data =
                        "<table>" +
                            "<thead>" +
                                "<tr>" +
                                    String.Join("",
                                        headers.Select(
                                            header => $"<th>{header}</th>"
                                        )
                                    ) +
                                "</tr>" +
                            "</thead>" +
                            "<tbody>" +
                                String.Join("",
                                    cells.Select(row =>
                                        "<tr>" +
                                        String.Join("",
                                            row.Select(
                                                cell => $"<td>{cell}</td>"
                                            )
                                        ) +
                                        "</tr>"
                                    )
                                ) +
                            "</tbody>" +
                        "</table>"
                };
            } else return null;
        }
    }

    public class TableToTextDisplayEncoder : IResultEncoder
    {
        public string MimeType => MimeTypes.PlainText;
        public EncodedData? Encode(object displayable)
        {
            if (displayable is ITable table)
            {
                var headers = table.Headers;
                var cells = table.Cells;

                // For the text, we need to find how wide each column is.
                var widths = headers.Select(column => 0).ToArray();
                var nCols = widths.Length;
                foreach (var row in cells)
                {
                    foreach (var idx in Enumerable.Range(0, nCols))
                    {
                        if (row[idx].Length > widths[idx])
                        {
                            widths[idx] = row[idx].Length;
                        }
                    }
                }

                var text = new StringBuilder();
                text.Append(String.Join(" ",
                    headers.Select((header, idxCol) =>
                        header.PadRight(widths[idxCol])
                    )
                ));
                text.Append("\n");
                text.Append(String.Join(" ",
                    widths.Select(width => new String('-', width))
                ));
                text.Append("\n");
                foreach (var row in cells)
                {
                    text.Append(String.Join(" ",
                        row.Select(
                            (cell, idxCol) => cell.PadRight(widths[idxCol])
                        )
                    ));
                    text.Append("\n");
                }


                return text.ToString().ToEncodedData();
            } else return null;
        }
    }
}