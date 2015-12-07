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

            // assume first row has column names

            var fileNames = Directory.GetFiles(dir);

            var fields = new Dictionary<int, Field>(); // <position, Field>
            
            fileNames.ToList().ForEach(fileName =>
            {
                using (var reader = File.OpenText(fileName))
                {
                    using (var csv = new CsvReader(reader))
                    {
                        csv.Configuration.HasHeaderRecord = true;
                        var fieldsLoaded = false;
                        while (csv.Read())
                        {
                            // field names are not available until the first Read() call
                            if (!fieldsLoaded && fields.Count == 0)
                            {
                                for (int i = 0; i < csv.FieldHeaders.Length; i++)
                                    fields.Add(i, new Field(i, csv.FieldHeaders[i]));
                                fieldsLoaded = true;
                            }

                            for (int i = 0; i < csv.FieldHeaders.Length; i++)
                                fields[i].AnalyzeNewValue(csv.GetField(i));
                        }


                    }
                }
            });

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

        static readonly Regex NumericRegEx = new Regex(@"^[0-9\.,]+$");

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
    }

    public enum FieldType
    {
        String,
        Numeric,
    }
}
