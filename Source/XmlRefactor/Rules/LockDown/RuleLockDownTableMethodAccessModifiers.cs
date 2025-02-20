﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Text;

namespace XmlRefactor
{
    class RuleLockDownTableMethodAccessModifiers : Rule
    {
        HashSet<string> overRiddenTableMethodHashSet = new HashSet<string>();
        HashSet<string> internalTableMethodHashSet = new HashSet<string>();

        string hookableAttribute = "Hookable(false)";
        string privateAttribute = "private ";
        string publicAttribute = "public ";
        string internalAttribute = "internal ";
        string protectedAttribute = "protected ";
        public RuleLockDownTableMethodAccessModifiers()
        {
            this.initializeTableMethodHashSet();
        }

        public override string RuleName()
        {
            return "Tables - Lock down access modifiers at method level";
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
            xpoMatch.AddXMLStart("Method", false);
            xpoMatch.AddWhiteSpace();
            xpoMatch.AddXMLStart("Name", false);
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Name");
            xpoMatch.AddCaptureAnything();
            xpoMatch.AddXMLEnd("Method");
        }
        
        public override string Run(string _input)
        {
            return this.Run(_input, 0);
        }

        private bool isOverRiddenMethod(string methodName)
        {
            return overRiddenTableMethodHashSet.Contains(methodName);
        }

        private bool isInternalMethod(string className, string methodName)
        {
            var tableMethodPath = string.Concat(className, "/", methodName);
            return internalTableMethodHashSet.Contains(tableMethodPath);
        }

        public string Run(string _input, int _startAt = 0)
        {
            Match match = xpoMatch.Match(_input, _startAt);

            if (match.Success &&
                !_input.ToLowerInvariant().Contains(" implements "))
            {
                string methodName = match.Groups[1].Value.Trim();
                var stringToUpdate = match.Value;
                string classPath = MetaData.AOTPath("");
                string className = classPath.Substring(classPath.LastIndexOf("\\") + 1);

                if (methodName != "classDeclaration")
                {
                    if (this.isOverRiddenMethod(methodName) || match.Value.Contains("super("))
                    {
                        if (!stringToUpdate.Contains("Replaceable") &&
                            !stringToUpdate.Contains("QueryRangeFunction") &&
                            !stringToUpdate.Contains("Hookable") &&
                            !stringToUpdate.Contains("Wrappable") &&
                            !stringToUpdate.Contains("SysObsolete"))
                        {
                            _input = this.appendAttributeHookableFalse(_input, stringToUpdate, methodName);
                            _input = this.replaceAccessModifierForOverriddenMethods(_input, stringToUpdate, methodName);
                            Hits++;
                        }
                    }
                    else if (match.Value.Contains("display "))
                    {
                        _input = removeAttributeHookableFalse(_input, stringToUpdate, methodName);
                        if (!this.isInternalMethod(className, methodName))
                        {
                            _input = this.replaceAccessModifierForDisplayMethods(_input, stringToUpdate, methodName);
                            Hits++;
                        }
                        else
                        {
                            _input = this.replaceAccessModifierForInternalMethods(_input, stringToUpdate, methodName);
                            Hits++;
                        }
                    }
                    else if (methodName.Contains(" parm"))
                    {
                        _input = removeAttributeHookableFalse(_input, stringToUpdate, methodName);
                        _input = this.replaceAccessModifierForParmMethods(_input, stringToUpdate, methodName);
                        Hits++;
                    }
                    else if (!match.Value.Contains(" extends "))
                    {
                        _input = removeAttributeHookableFalse(_input, stringToUpdate, methodName);
                        if (this.isInternalMethod(className, methodName))
                        {
                            _input = this.replaceAccessModifierForInternalMethods(_input, stringToUpdate, methodName);
                            Hits++;
                        }
                        else
                        {
                            _input = this.replaceAccessModifierForPrivateMethods(_input, stringToUpdate, methodName);
                            Hits++;
                        }
                    }
                }
                
                _input = this.Run(_input, match.Index + 1);
            }

            return _input;
        }

        private void initializeTableMethodHashSet()
        {
            var overRiddenTableMethods = File.ReadAllLines(@"../../RulesInput/OverRiddenTableMethods.txt");
            overRiddenTableMethodHashSet.Clear();
            foreach (var method in overRiddenTableMethods)
            {
                if (!overRiddenTableMethodHashSet.Contains(method))
                {
                    overRiddenTableMethodHashSet.Add(method);
                }
            }

            var internalTableMethods = File.ReadAllLines(@"../../RulesInput/InternalTableMethods.txt");
            internalTableMethodHashSet.Clear();
            foreach (var method in internalTableMethods)
            {
                if (!internalTableMethodHashSet.Contains(method))
                {
                    internalTableMethodHashSet.Add(method);
                }
            }
        }

        private int signatureLineStartPos(string source, string methodName)
        {
            int startPos = 0;
            string potentialLine = string.Empty;
            int pos2 = 0;
            do
            {
                int pos = source.IndexOf(" " + methodName, startPos);
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

        private string appendAttributeHookableFalse(string _input, string source, string _methodName)
        {
            if (source.Contains(hookableAttribute))
            {
                return _input;
            }

            int signatureLinePosInSource = this.signatureLineStartPos(source, _methodName) + 1;
            string theline = source.Substring(signatureLinePosInSource);
            
            int attributeEndPosInSource = this.attributeEndPos(source, signatureLinePosInSource);

            if (attributeEndPosInSource > 0)
            {
                int pos = _input.IndexOf(source) + attributeEndPosInSource;
                _input = _input.Insert(pos, ", " + hookableAttribute);
            }
            else
            {
                int spaces = theline.Length - theline.TrimStart().Length;
                int pos = _input.IndexOf(source) + signatureLinePosInSource;
                _input = _input.Insert(pos, " ".PadLeft(spaces) + "[" + hookableAttribute + "]" + Environment.NewLine);
            }
            return _input;
        }

        private string replaceAccessModifierForOverriddenMethods(string _input, string source, string _methodName)
        {
            if (source.Contains(publicAttribute))
            {
                return _input;
            }

            int signatureLinePosInSource = this.signatureLineStartPos(source, _methodName) + 1;
            string theline = source.Substring(signatureLinePosInSource);
            string newline = "";

            if (theline.ToLowerInvariant().Contains(publicAttribute))
            {
                return _input;
            }
            else if (theline.ToLowerInvariant().Contains(internalAttribute))
            {
                newline = theline.Replace(internalAttribute, publicAttribute);
                return _input = _input.Replace(theline, newline);
            }
            else if (theline.ToLowerInvariant().Contains(privateAttribute))
            {
                newline = theline.Replace(privateAttribute, publicAttribute);
                return _input = _input.Replace(theline, newline);
            }
            else if (theline.ToLowerInvariant().Contains(protectedAttribute))
            {
                newline = theline.Replace(protectedAttribute, publicAttribute);
                return _input = _input.Replace(theline, newline);
            }
            else
            {
                int spaces = theline.Length - theline.TrimStart().Length;
                int pos = _input.IndexOf(source) + signatureLinePosInSource;
                newline = " ".PadLeft(spaces) + publicAttribute + theline.TrimStart(' ');
                return _input = _input.Replace(theline, newline);
            }
        }

        private string removeAttributeHookableFalse(string _input, string source, string _methodName)
        {
            if (!source.Contains(hookableAttribute))
            {
                return _input;
            }

            string newline = "";
            if (source.Contains("[" + hookableAttribute + "]"))
            {
                newline = source.Replace("[" + hookableAttribute + "]", "");
                _input = _input.Replace(source, newline);
            }
            else
            {
                newline = source.Replace(", " + hookableAttribute, "");
                _input = _input.Replace(source, newline);
            }
            return _input;
        }

        private string replaceAccessModifierForDisplayMethods(string _input, string source, string _methodName)
        {
            if (source.Contains(internalAttribute))
            {
                return _input;
            }

            int signatureLinePosInSource = this.signatureLineStartPos(source, _methodName) + 1;
            string theline = source.Substring(signatureLinePosInSource);
            string newline = "";

            if (theline.ToLowerInvariant().Contains("display "))
            {
                if (theline.ToLowerInvariant().Contains(internalAttribute))
                {
                    return _input;
                }
                else if (theline.ToLowerInvariant().Contains(publicAttribute))
                {
                    newline = theline.Replace(publicAttribute, internalAttribute);
                    return _input = _input.Replace(theline, newline);
                }
                else if (theline.ToLowerInvariant().Contains(privateAttribute))
                {
                    newline = theline.Replace(privateAttribute, internalAttribute);
                    return _input = _input.Replace(theline, newline);
                }
                else if (theline.ToLowerInvariant().Contains(protectedAttribute))
                {
                    newline = theline.Replace(protectedAttribute, internalAttribute);
                    return _input = _input.Replace(theline, newline);
                }
                else
                {
                    int spaces = theline.Length - theline.TrimStart().Length;
                    int pos = _input.IndexOf(source) + signatureLinePosInSource;
                    newline = " ".PadLeft(spaces) + internalAttribute + theline.TrimStart(' ');
                    return _input = _input.Replace(theline, newline);
                }
            }

            return _input;
        }

        private string replaceAccessModifierForParmMethods(string _input, string source, string _methodName)
        {
            if (source.Contains(internalAttribute))
            {
                return _input;
            }

            int signatureLinePosInSource = this.signatureLineStartPos(source, _methodName) + 1;
            string theline = source.Substring(signatureLinePosInSource);
            string newline = "";

            if (theline.ToLowerInvariant().Contains(" parm"))
            {
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
                    newline = " ".PadLeft(spaces) + internalAttribute + theline.TrimStart(' ');
                    return _input = _input.Replace(theline, newline);
                }
            }

            return _input;
        }

        private string replaceAccessModifierForPrivateMethods(string _input, string source, string _methodName)
        {
            if (source.Contains(privateAttribute))
            {
                return _input;
            }

            int signatureLinePosInSource = this.signatureLineStartPos(source, _methodName) + 1;
            string theline = source.Substring(signatureLinePosInSource);
            string newline = "";

            if (theline.ToLowerInvariant().Contains(privateAttribute))
            {
                return _input;
            }
            else if (theline.ToLowerInvariant().Contains(publicAttribute))
            {
                newline = theline.Replace(publicAttribute, privateAttribute);
                return _input = _input.Replace(theline, newline);
            }
            else if (theline.ToLowerInvariant().Contains(protectedAttribute))
            {
                newline = theline.Replace(protectedAttribute, privateAttribute);
                return _input = _input.Replace(theline, newline);
            }
            else if (theline.ToLowerInvariant().Contains(internalAttribute))
            {
                newline = theline.Replace(internalAttribute, privateAttribute);
                return _input = _input.Replace(theline, newline);
            }
            else
            {
                int spaces = theline.Length - theline.TrimStart().Length;
                int pos = _input.IndexOf(source) + signatureLinePosInSource;
                newline = " ".PadLeft(spaces) + privateAttribute + theline.TrimStart(' ');
                return _input = _input.Replace(theline, newline);
            }
        }

        private string replaceAccessModifierForInternalMethods(string _input, string source, string _methodName)
        {
            if (source.Contains(internalAttribute))
            {
                return _input;
            }

            int signatureLinePosInSource = this.signatureLineStartPos(source, _methodName) + 1;
            string theline = source.Substring(signatureLinePosInSource);
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
                newline = " ".PadLeft(spaces) + internalAttribute + theline.TrimStart(' ');
                return _input = _input.Replace(theline, newline);
            }
        }
        
    }
}