using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CsvHelper;
using System.IO;
using System.Text.RegularExpressions;

namespace CsvAnalyzer
{
    class Program
    {
        static void Main(string[] args)
        {
            var dir = @"nih\awards";

            if (args.Length > 0)
                dir = args[0];

            // assume first row has column names

            var fileNames = Directory.GetFiles(dir);

            var fields = new Dictionary<string, Field>(); // <name, Field>
            
            fileNames.ToList().ForEach(fileName =>
            {
                using (var reader = File.OpenText(fileName))
                {
                    Console.WriteLine($"Reading {Path.GetFileName(fileName)}");
                    var count = 0;
                    using (var csv = new CsvReader(reader))
                    {
                        csv.Configuration.HasHeaderRecord = true;
                        var fieldsLoaded = false;
                        while (csv.Read())
                        {
                            // field names are not available until the first Read() call
                            if (!fieldsLoaded)
                            {
                                for (int i = 0; i < csv.FieldHeaders.Length; i++)
                                {
                                    var header = csv.FieldHeaders[i];
                                    if (!fields.ContainsKey(header))
                                        fields.Add(header, new Field(i, csv.FieldHeaders[i]));
                                }
                                fieldsLoaded = true;
                            }

                            for (int i = 0; i < csv.FieldHeaders.Length; i++)
                            {
                                var header = csv.FieldHeaders[i];
                                fields[header].AnalyzeNewValue(csv.GetField(header));
                            }

                            count++;
                        }

                        Console.WriteLine($"Read {count} records");
                        Console.WriteLine();
                    }
                }
            });


            var sb = new StringBuilder();
            sb.AppendLine(Field.GetHeaders());
            fields.Select(o => o.Value).ToList().ForEach(field =>
            {
                sb.AppendLine(field.ToString());
            });
            File.WriteAllText("result.txt", sb.ToString());
        }
    }



    public class Field
    {
        public Field(int position, string name)
        {
            Position = position;
            Name = name;
        }

        public int Position { get; set; }
        public string Name { get; set; }

        public FieldType Type { get; set; } = FieldType.String;

        public int MaxStringLength { get; set; }
        public int MaxDecimalPlaces { get; set; }

        static readonly Regex NumericRegEx = new Regex(@"^[0-9\.,]+$", RegexOptions.Compiled | RegexOptions.Multiline);

        public void AnalyzeNewValue(string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            var length = value.Length;
            MaxStringLength = MaxStringLength > length ? MaxStringLength : length;

            if (NumericRegEx.IsMatch(value))
            {
                Type = FieldType.Numeric;
                decimal dValue;
                if (decimal.TryParse(value, out dValue))
                {
                    var decimalPlaces = BitConverter.GetBytes(decimal.GetBits(dValue)[3])[2];
                    MaxDecimalPlaces = MaxDecimalPlaces > decimalPlaces ? MaxDecimalPlaces : decimalPlaces;
                }
            }
        }

        public override string ToString()
        {
            return $"{Position}\t{Name}\t{Type.ToString()}\t{MaxStringLength}\t{MaxDecimalPlaces}";
        }
        public static string GetHeaders()
        {
            return $"{nameof(Position)}\t{nameof(Name)}\t{nameof(Type)}\t{nameof(MaxStringLength)}\t{nameof(MaxDecimalPlaces)}";
        }
    }

    public enum FieldType
    {
        String,
        Numeric,
    }
}
