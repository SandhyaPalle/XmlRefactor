using System.IO;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleCollectClassDataForForms : Rule
    {
        public override string RuleName()
        {
            return "Collect classes data for forms";
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
            //forms
            xpoMatch.AddXMLStart("Source", false);
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Source");
        }

        public override string Run(string input)
        {
            return this.Run(input, 0);
        }

        public bool include(string source)
        {
            if (source.Contains("class") || source.Contains("interface"))
                return true;

            return false;
        }

        public string Run(string _input, int startAt = 0)
        {
            Match match = xpoMatch.Match(_input, startAt);

            if (match.Success)
            {
                string xml = _input;

                string final = "";
                string abstractInterface = "";
                string accessModifier = "";
                string extends_string = "";
                string implements_string = "";
                string extensionOf_string = "";
                string extensionOf = "";
                string extends = "";
                string implements = "";

                if (this.include(match.Value.ToLowerInvariant()))
                {
                    var stringToUpdate = match.Value;

                    if (stringToUpdate.Contains("final"))
                    {
                        final = "final";
                    }
                    if (stringToUpdate.Contains("interface"))
                    {
                        abstractInterface = "interface";
                    }
                    if (stringToUpdate.Contains("abstract"))
                    {
                        abstractInterface = "abstract";
                    }
                    if (stringToUpdate.Contains("internal"))
                    {
                        accessModifier = "internal";
                    }
                    if (stringToUpdate.Contains("public"))
                    {
                        accessModifier = "public";
                    }

                    if (stringToUpdate.Contains("extends ") &&
                        (stringToUpdate.LastIndexOf("extends") > (stringToUpdate.Contains("internal") ? stringToUpdate.LastIndexOf("internal") : stringToUpdate.LastIndexOf("public"))))
                    {
                        int position1 = stringToUpdate.LastIndexOf("extends");
                        int position2 = stringToUpdate.Contains("implements ") ? stringToUpdate.LastIndexOf("implements") - 1 : stringToUpdate.IndexOf("{") - 1;
                        extends_string = stringToUpdate.Substring(position1 + 8, position2 - position1 - 8).Trim().Replace("\r\n", "");
                        extends = "extends";
                    }

                    if (stringToUpdate.Contains("implements ") &&
                        (stringToUpdate.LastIndexOf("implements") > (stringToUpdate.Contains("internal") ? stringToUpdate.LastIndexOf("internal") : stringToUpdate.LastIndexOf("public"))))
                    {
                        int position1 = stringToUpdate.LastIndexOf("implements");
                        int position2 = stringToUpdate.IndexOf("{") - 1;
                        implements_string = stringToUpdate.Substring(position1 + 11, position2 - position1 - 11).Trim().Replace("\r\n", "");
                        implements = "implements";
                    }

                    if (stringToUpdate.Contains("ExtensionOf("))
                    {
                        int position1 = stringToUpdate.IndexOf("ExtensionOf(");
                        int position2 = stringToUpdate.IndexOf(")]");
                        extensionOf_string = stringToUpdate.Substring(position1 + 12, position2 - position1 - 12).Trim().Replace("\r\n", "");
                        extensionOf = "extensionOf";
                    }

                    string AOTPath = MetaData.AOTPath("") + "\\" + final + "\\" + abstractInterface + "\\" + accessModifier + "\\" + extends + "\\" + extends_string + "\\" + implements + "\\" + implements_string + "\\" + extensionOf + "\\" + extensionOf_string;
                    AOTPath.Replace("\r\n", "");

                    //Forms require if condition	
                    if (AOTPath.Contains("extends") && AOTPath.Contains("FormRun"))
                    {
                        using (StreamWriter sw = File.AppendText(@"c:\temp\Classes-all.txt"))
                        {
                            sw.WriteLine(AOTPath);
                        }
                    }
                }

                _input = this.Run(_input, match.Index + 1);
            }

            return _input;
        }
    }
}
