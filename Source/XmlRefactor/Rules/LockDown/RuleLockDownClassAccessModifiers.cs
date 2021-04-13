using System;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleLockDownClassAccessModifiers : Rule
    {
        string privateAttribute = "private ";
        string publicAttribute = "public ";
        string internalAttribute = "internal ";
        string protectedAttribute = "protected ";
        string classAttribute = "class ";
        public override string RuleName()
        {
            return "Classes - Lock down access modifiers at class level";
        }

        public override bool Enabled()
        {
            return false;
        }
        override public string Grouping()
        {
            return "Lock down";
        }
        protected override void buildXpoMatch()
        {
            xpoMatch.AddXMLStart("Declaration", false);
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Declaration");
        }

        public override string Run(string _input)
        {
            return this.Run(_input, 0);
        }

        public string Run(string _input, int _startAt = 0)
        {
            Match match = xpoMatch.Match(_input, _startAt);
            if (match.Success && match.Value.Contains(classAttribute))
            {
                string classInput = match.Groups[1].Value.Trim();
                var stringToUpdate = match.Value;

                _input = this.replaceAccessModifierClassLevel(_input, stringToUpdate);
                Hits++;
            }

            return _input;
        }

        private int signatureLineStartPos(string source)
        {
            int startPos = 0;
            string potentialLine = string.Empty;
            int pos2 = 0;
            do
            {
                int pos = source.IndexOf(classAttribute, startPos);
                pos2 = source.LastIndexOf(Environment.NewLine, pos) + 1;
                potentialLine = source.Substring(pos2, pos - pos2).TrimStart();
                startPos = pos + 1;
            }
            while (potentialLine.StartsWith("//"));

            return pos2;
        }

        private string getLineAtPos(string source, int pos)
        {
            int pos2 = source.LastIndexOf(Environment.NewLine, pos) + 1;
            return source.Substring(pos2, pos - pos2);
        }

        private int attributeEndPos(string source, int posOfSignatureLine)
        {
            int startPos = posOfSignatureLine;
            string potentialLine = string.Empty;
            int pos = 0;
            do
            {
                pos = source.LastIndexOf("]", startPos);
                if (pos > 0)
                {
                    potentialLine = this.getLineAtPos(source, pos).TrimStart();
                    startPos = pos - 1;
                }
            }
            while (pos > 0 && potentialLine.StartsWith("//"));

            return pos;
        }

        private string replaceAccessModifierClassLevel(string _input, string source)
        {
            if (source.Contains(internalAttribute))
            {
                return _input;
            }

            string originalString = source;
            int signatureLinePosInSource = this.signatureLineStartPos(source) + 1;
            string theline = source.Substring(signatureLinePosInSource);
            string newline = "";

            if (theline.ToLowerInvariant().Contains(internalAttribute))
            {
                return _input;
            }
            else if (theline.ToLowerInvariant().Contains(publicAttribute))
            {
                newline = theline.Replace(publicAttribute, internalAttribute);
                source = source.Replace(theline, newline);
            }
            else if (theline.ToLowerInvariant().Contains(protectedAttribute))
            {
                newline = theline.Replace(protectedAttribute, internalAttribute);
                source = source.Replace(theline, newline);
            }
            else if (theline.ToLowerInvariant().Contains(privateAttribute))
            {
                newline = theline.Replace(privateAttribute, internalAttribute);
                source = source.Replace(theline, newline);
            }
            else
            {
                int spaces = theline.Length - theline.TrimStart().Length;
                int pos = _input.IndexOf(source) + signatureLinePosInSource;
                newline = internalAttribute + theline.TrimStart(' ');
                source = source.Replace(theline, newline);
            }

            return _input.Replace(originalString, source);
        }
    }
}

