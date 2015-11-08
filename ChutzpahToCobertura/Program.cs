using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;

namespace ChutzpahToCobertura
{
    class Program
    {
        #region Fields
        private const string ElementNameRoot = "coverage";
        private const string ElementNameSources = "sources";
        private const string ElementNameSource = "source";
        private const string ElementNamePackages = "packages";
        private const string ElementNamePackage = "package";
        private const string ElementNameClasses = "classes";
        private const string ElementNameClass = "class";
        private const string ElementNameMethods = "methods";
        private const string ElementNameLines = "lines";
        private const string ElementNameLine = "line";

        private const string AttributeNameLineRate = "line-rate";
        private const string AttributeNameBranchRate = "branch-rate";
        private const string AttributeNameVersion = "version";
        private const string AttributeNameTimestamp = "timestamp";
        private const string AttributeNameName = "name";
        private const string AttributeNameFilename = "filename";
        private const string AttributeNameComplexity = "complexity";
        private const string AttributeNameNumber = "number";
        private const string AttributeNameHits = "hits";

        private const string AttributeValueVersion = "3.0";
        private const string AttributeValueOne = "1.0";
        private const string AttributeValueZero = "0.0";
        private const string AttributeValueDTD = "http://cobertura.sourceforge.net/xml/coverage-03.dtd";

        private const string JsonMemberNameCoveragePercentage = "CoveragePercentage";
        private const string JsonMemberNameFilePath = "FilePath";
        private const string JsonMemberNameLineExecutionCounts = "LineExecutionCounts";
        #endregion

        #region Program
        /// <summary>
        /// The program start method.
        /// </summary>
        public static void Main(string[] args)
        {
            if (args.Length == 2)
            {
                if (!File.Exists(args[0]))
                {
                    Console.WriteLine("Input file do not exists.");
                }

                var chutzpahReport = ReadJson(args[0]);
                CreateCoberturaReport(chutzpahReport, Environment.CurrentDirectory, args[1]);
            }
            else
            {
                Console.WriteLine("ChutzpahToCobertura <chuthpah_report_file> <cobertura_export_file>");
            }
        }
        #endregion

        #region Inner Methods
        /// <summary>
        /// Reads the chutzpah input report file and convert it to collection of models.
        /// </summary>
        private static IEnumerable<ChutzpahData> ReadJson(string file)
        {
            using (var stream = new StreamReader(file))
            {
                using (var reader = new JsonTextReader(stream))
                {
                    var jsonReport = JObject.Load(reader);
                    foreach(var jsonMember in jsonReport)
                    {
                        yield return new ChutzpahData()
                        {
                            Name = jsonMember.Value.Value<string>(JsonMemberNameFilePath),
                            Coverage = jsonMember.Value.Value<double>(JsonMemberNameCoveragePercentage),
                            Lines = jsonMember.Value.Value<JArray>(JsonMemberNameLineExecutionCounts).Values<ushort?>()
                        };
                    }
                }
            }
        }

        /// <summary>
        /// Creates the cobertura report.
        /// </summary>
        /// <param name="report">The chutzpah report.</param>
        /// <param name="currentDirectoryPath">The current directory path.</param>
        /// <param name="outputFilePath">The output file path.</param>
        private static void CreateCoberturaReport(IEnumerable<ChutzpahData> report, string currentDirectoryPath, string outputFilePath)
        {
            var codeCovarageTotal = report.Aggregate(1d, (seed, it) => seed * it.Coverage).ToString();

            using (var outputFileStream = new StreamWriter(outputFilePath))
            {
                using (XmlTextWriter writer = new XmlTextWriter(outputFileStream))
                {
                    writer.WriteStartDocument();
                    writer.WriteDocType(ElementNameRoot, string.Empty, AttributeValueDTD, string.Empty);
                    writer.WriteStartElement(ElementNameRoot);
                    writer.WriteAttributeString(AttributeNameLineRate, codeCovarageTotal);
                    writer.WriteAttributeString(AttributeNameBranchRate, AttributeValueOne);
                    writer.WriteAttributeString(AttributeNameVersion, AttributeValueVersion);
                    writer.WriteAttributeString(AttributeNameTimestamp, DateTime.Now.Ticks.ToString());

                    // sources
                    writer.WriteStartElement(ElementNameSources);
                    writer.WriteElementString(ElementNameSource, currentDirectoryPath);
                    writer.WriteEndElement();

                    // packages
                    writer.WriteStartElement(ElementNamePackages);
                    writer.WriteStartElement(ElementNamePackage);
                    writer.WriteAttributeString(AttributeNameName, string.Empty);
                    writer.WriteAttributeString(AttributeNameLineRate, codeCovarageTotal);
                    writer.WriteAttributeString(AttributeNameBranchRate, AttributeValueOne);
                    writer.WriteAttributeString(AttributeNameComplexity, AttributeValueZero);

                    // classes
                    writer.WriteStartElement(ElementNameClasses);
                    foreach (var element in report)
                    {
                        writer.WriteStartElement(ElementNameClass);
                        writer.WriteAttributeString(AttributeNameName, Path.GetFileName(element.Name));
                        writer.WriteAttributeString(AttributeNameFilename, element.Name.Substring(currentDirectoryPath.Length));
                        writer.WriteAttributeString(AttributeNameLineRate, element.Coverage.ToString());
                        writer.WriteAttributeString(AttributeNameBranchRate, AttributeValueOne);
                        writer.WriteAttributeString(AttributeNameComplexity, AttributeValueZero);

                        // methods
                        writer.WriteElementString(ElementNameMethods, string.Empty);

                        // lines
                        writer.WriteStartElement(ElementNameLines);
                        int lineNumber = 0;
                        foreach (var line in element.Lines)
                        {
                            lineNumber++;
                            if (line.HasValue)
                            {
                                writer.WriteStartElement(ElementNameLine);
                                writer.WriteAttributeString(AttributeNameNumber, lineNumber.ToString());
                                writer.WriteAttributeString(AttributeNameHits, line.Value.ToString());
                                writer.WriteEndElement();
                            }
                        }

                        writer.WriteEndElement();
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
            }
        }
        #endregion

        #region Inner Methods
        /// <summary>
        /// Chutzpah report data model.
        /// </summary>
        private sealed class ChutzpahData
        {
            public string Name { get; set; }
            public IEnumerable<ushort?> Lines { get; set; }
            public double Coverage { get; set; }
        }
        #endregion
    }
}