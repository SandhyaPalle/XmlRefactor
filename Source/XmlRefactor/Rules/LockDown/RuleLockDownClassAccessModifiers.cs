using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace XmlRefactor
{
    class RuleLockDownClassAccessModifiers : Rule
    {
        HashSet<string> extendedClassHashSet = new HashSet<string>();
        string privateAttribute = "private ";
        string publicAttribute = "public ";
        string internalAttribute = "internal ";
        string internalFinalAttribute = "internal final ";
        string protectedAttribute = "protected ";
        string classAttribute = "class ";
        int signatureLineLength = 0;

        public override string RuleName()
        {
            return "Classes - Lock down access modifiers at class level";
        }
        public RuleLockDownClassAccessModifiers()
        {
            this.initializeClassHashSet();
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

        private bool isClassExtended(string className)
        {
            return extendedClassHashSet.Contains(className);
        }

        private void initializeClassHashSet()
        {
            var extendedClasses = File.ReadAllLines(@"../../RulesInput/ExtendedClasses.txt");
            extendedClassHashSet.Clear();
            foreach (var className in extendedClasses)
            {
                if (!extendedClassHashSet.Contains(className))
                {
                    extendedClassHashSet.Add(className);
                }
            }
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
                string classPath = MetaData.AOTPath("");
                string className = classPath.Substring(classPath.LastIndexOf("\\") + 1);

                if (this.isClassExtended(className))
                {
                    _input = this.replaceAccessModifierForClasses(_input, stringToUpdate, className);
                    Hits++;
                }
                else
                {
                    _input = this.replaceAccessModifierForExtendedClasses(_input, stringToUpdate, className);
                    Hits++;
                }
            }

            return _input;
        }

        private int signatureLineStartPos(string source, string className)
        {
            int startPos = 0;
            string potentialLine = string.Empty;
            int pos2 = 0;
            signatureLineLength = 0;

            do
            {
                signatureLineLength = source.IndexOf(Environment.NewLine + "{", startPos);
                pos2 = source.IndexOf(Environment.NewLine, startPos) + 1;
                potentialLine = source.Substring(pos2, signatureLineLength - pos2).TrimStart();
                startPos = signatureLineLength + 1;
            }
            while (potentialLine.StartsWith(" "));

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

        private string replaceAccessModifierForExtendedClasses(string _input, string source, string _className)
        {
            if (source.Contains(internalFinalAttribute))
            {
                return _input;
            }

            int signatureLinePosInSource = this.signatureLineStartPos(source, _className) + 1;
            string theline = source.Substring(signatureLinePosInSource, signatureLineLength - signatureLinePosInSource).TrimStart();
            string newline = "";

            if (theline.ToLowerInvariant().Contains(internalFinalAttribute))
            {
                return _input;
            }
            else if (theline.ToLowerInvariant().Contains(internalAttribute))
            {
                newline = theline.Replace(internalAttribute, internalFinalAttribute);
                return _input = _input.Replace(theline, newline);
            }
            else if (theline.ToLowerInvariant().Contains(publicAttribute))
            {
                newline = theline.Replace(publicAttribute, internalFinalAttribute);
                return _input = _input.Replace(theline, newline);
            }
            else if (theline.ToLowerInvariant().Contains(protectedAttribute))
            {
                newline = theline.Replace(protectedAttribute, internalFinalAttribute);
                return _input = _input.Replace(theline, newline);
            }
            else if (theline.ToLowerInvariant().Contains(privateAttribute))
            {
                newline = theline.Replace(privateAttribute, internalFinalAttribute);
                return _input = _input.Replace(theline, newline);
            }
            else
            {
                int spaces = theline.Length - theline.TrimStart().Length;
                int pos = _input.IndexOf(source) + signatureLinePosInSource;
                newline = internalFinalAttribute + theline.TrimStart(' ');
                return _input = _input.Replace(theline, newline);
            }
        }

        private string replaceAccessModifierForClasses(string _input, string source, string _className)
        {
            if (source.Contains(internalAttribute))
            {
                return _input;
            }

            int signatureLinePosInSource = this.signatureLineStartPos(source, _className) + 1;
            string theline = source.Substring(signatureLinePosInSource, signatureLineLength - signatureLinePosInSource).TrimStart();
            string newline = "";

            if (theline.ToLowerInvariant().Contains(internalAttribute))
            {
                return _input;
            }
            else if (theline.ToLowerInvariant().Contains(publicAttribute))
            {
                newline = theline.Replace(publicAttribute, internalAttribute);
                return _input = _input.Replace(theline, newline);
            }
            else if (theline.ToLowerInvariant().Contains(protectedAttribute))
            {
                newline = theline.Replace(protectedAttribute, internalAttribute);
                return _input = _input.Replace(theline, newline);
            }
            else if (theline.ToLowerInvariant().Contains(privateAttribute))
            {
                newline = theline.Replace(privateAttribute, internalAttribute);
                return _input = _input.Replace(theline, newline);
            }
            else
            {
                int spaces = theline.Length - theline.TrimStart().Length;
                int pos = _input.IndexOf(source) + signatureLinePosInSource;
                newline = internalAttribute + theline.TrimStart(' ');
                return _input = _input.Replace(theline, newline);
            }
        }
    }
}

