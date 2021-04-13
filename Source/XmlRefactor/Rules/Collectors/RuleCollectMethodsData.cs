using System;
using System.IO;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleCollectMethodsData : Rule
    {
        public override string RuleName()
        {
            return "Collect method data for classes, tables and forms";
        }
        override public string Grouping()
        {
            return "Collectors";
        }
        public override bool Enabled()
        {
            return false;
        }
        protected override void buildXpoMatch()
        {
            xpoMatch.AddXMLStart("Method", false);
            xpoMatch.AddWhiteSpace();
            xpoMatch.AddXMLStart("Name", false);
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Name");
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Method");
        }

        public override string Run(string input)
        {
            return this.Run(input, 0);
        }

        public bool include(string _input)
        {
            int pos = _input.IndexOf("CData");

            string source = _input.Substring(pos + 6);
            string[] lines = source.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);
            string firstLine = "";
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line == String.Empty ||
                    lines[i].Trim().StartsWith("//"))
                {
                    continue;
                }

                if (!line.Contains("("))
                {
                    continue;
                }

                if (line.Contains("["))
                {
                    continue;
                }

                firstLine = line;
                break;
            }

            if (firstLine.Contains("public ") ||
                   firstLine.Contains("internal ") ||
                   firstLine.Contains("protected ") ||
                   !firstLine.Contains("private "))
            {
                return true;
            }

            return false;
        }

        public string Run(string _input, int startAt = 0)
        {
            Match match = xpoMatch.Match(_input, startAt);

            if (match.Success)
            {
                string xml = _input;
                string hookableWrappable = "";
                string accessModifier = "";

                if (this.include(match.Value))
                {
                    var stringToUpdate = match.Value;

                    if (stringToUpdate.Contains("Hookable(false)"))
                    {
                        if (stringToUpdate.Contains("Hookable(false)"))
                            hookableWrappable = "Hookable(false)";
                        else if (stringToUpdate.Contains("Hookable(true)"))
                            hookableWrappable = "Hookable(true)";
                    }
                    if (stringToUpdate.Contains("Wrappable(false)"))
                    {
                        if (stringToUpdate.Contains("Wrappable(false)"))
                            hookableWrappable = "Wrappable(false)";
                        else if (stringToUpdate.Contains("Wrappable(true)"))
                            hookableWrappable = "Wrappable(true)";
                    }

                    if (stringToUpdate.Contains("public"))
                    {
                        accessModifier = "Public";
                    }
                    else if (stringToUpdate.Contains("internal") && stringToUpdate.Contains("protected"))
                    {
                        accessModifier = "Protected Internal";
                    }
                    else if (stringToUpdate.Contains("protected"))
                    {
                        accessModifier = "Protected";
                    }
                    else if (stringToUpdate.Contains("internal"))
                    {
                        accessModifier = "Internal";
                    }
                    else if (!stringToUpdate.Contains("private"))
                    {
                        accessModifier = "No Access Modifier";
                    }

                    string name = match.Groups[1].Value.Trim();

                    if (name.ToLowerInvariant() != "classdeclaration")
                    {
                        string AOTPath = MetaData.AOTPath(name);
                        using (StreamWriter sw = File.AppendText(this.logFileName()))
                        {
                            sw.WriteLine(AOTPath + "\\" + accessModifier + "\\" + hookableWrappable);
                        }
                    }
                }

                _input = this.Run(_input, match.Index + 1);
            }

            return _input;
        }
    }
}