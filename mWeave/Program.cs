using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace mWeave {

    class HtmlCSSGen {

        public HtmlCSSGen(string path) {
            DirOfHTMLFile = "";
            Path = path;
            List<char> tmp = new List<char>();
            for (int i = path.Length - 1; i >= 0; i--) {
                if (path[i] == '\\')
                    break;
                tmp.Add(path[i]);
            }
            tmp.Reverse();
            foreach (var v in tmp) {
                DirOfHTMLFile += v;
            }


            DirOfCSSFile = DirOfHTMLFile.Substring(0, DirOfHTMLFile.Length - 2) + ".css";
            DirOfHTMLFile = path.Substring(0, path.Length - DirOfHTMLFile.Length) + DirOfHTMLFile.Substring(0, DirOfHTMLFile.Length - 2) + ".html";
        }


        public string CssGenerator() {
            string css = "body {" + '\n';
            css += "    font-family: Cambria;" + '\n';
            css += "    max-width: 800px;" + '\n';
            css += "}";
            css += "" + '\n';
            css += "p.code {" + '\n';
            css += "    font-family: Consolas;" + '\n';
            css += "    background-color:#E0E0E0;" + '\n';
            css += "}";
            return css;
        }

        public void HtmlGenerator() {
            HTMLText = "";
            HTMLText += "<html lang=\"en\">" + '\n';
            HTMLText += "<head>" + '\n';
            HTMLText += "<meta charset=\"utf-8\"/>" + '\n';
            HTMLText += string.Format("<link rel=\"stylesheet\" type=\"text/css\" href=\"{0}\" />" + '\n', DirOfCSSFile);
            HTMLText += "</head>" + '\n';
            HTMLText += "<body>" + '\n';
            DirOfCSSFile = Path.Substring(0, Path.Length - 2) + ".css";
            string txt = "";
            StreamReader sr = new StreamReader(Path);
            while ((txt = sr.ReadLine()) != null) {
                if (txt.StartsWith("@")) {
                    CodeMode = false;
                    mBlocks.Add(TempBlock);
                    TempBlock = "";
                }
                else if (txt.Trim().StartsWith("<<")) {
                    if (TempPara.Length != 0)
                        mParagraphs.Add(TempPara);
                    if (!CodeMode) {
                        BlockIndicators.Add(mParagraphs.Count);
                    }
                    CodeMode = true;
                }

                if (!CodeMode) {
                    if (isEndOfParagraph(txt) && TempPara.Length != 0) {
                        mParagraphs.Add(TempPara);
                        TempPara = "";
                    }

                    if (txt.StartsWith("@")) {
                        string tmp = "";
                        tmp = ExtractTextWithoutAt(txt);
                        tmp = tmp.Trim();
                        TempPara += tmp + '\n';
                    }

                    else {
                        if (txt.Trim().Length != 0)
                            TempPara += txt + '\n';
                    }
                }



                else {
                    if (txt.Trim().StartsWith("<<") && txt.Trim().EndsWith(">>"))
                        TempBlock += Spaces(txt) + OpenBracket + "<i>" + TextInBetween(txt) + "</i>" + CloseBracket + "<br />" + '\n';
                    else if (txt.Trim().StartsWith("<<") && txt.Trim().EndsWith(">>="))
                        TempBlock += Spaces(txt) + OpenBracket + "<i>" + TextInBetween(txt) + "</i>" + CloseBracket + EqualSign + "<br />" + '\n';
                    else if (txt.Trim().StartsWith("<<") && txt.Trim().EndsWith(">>+="))
                        TempBlock += Spaces(txt) + OpenBracket + "<i>" + TextInBetween(txt) + "</i>" + CloseBracket + "+" + EqualSign + "<br />" + '\n';
                    else {
                        if (txt.Trim().Length != 0)
                            TempBlock += Spaces(txt) + txt.Trim() + "<br />" + '\n';
                    }
                }
            }

            if (TempBlock != "")
                mBlocks.Add(TempBlock);
        }


        #region--------Private Methods
        string TextInBetween(string line) {
            line = line.Trim();
            if (line.EndsWith(">>"))
                return line.Substring(2, line.Length - 4);
            else if (line.EndsWith(">>+="))
                return line.Substring(2, line.Length - 6);
            else
                return line.Substring(2, line.Length - 5);
        }

        string Spaces(string line) {
            string tmp = "";
            int i = 0;
            if (line.StartsWith(" ")) {
                while (line[i] == ' ') {
                    tmp += SpaceUnicode;
                    i++;
                }
            }
            return tmp;
        }


        bool isEndOfParagraph(string line) {
            if (line.Trim().Length == 0)
                return true;
            return false;
        }

        void AddLastLine() {
            mParagraphs.Add(TempPara);
        }

        string ExtractTextWithoutAt(string line) {
            string tmp = "";
            for (int i = 0; i < line.Length; i++) {
                if (line[i] != '@') {
                    tmp += line[i];
                }
            }
            tmp = tmp.Trim();
            return tmp;
        }

        void ModifyParagraphsAndBlocks() {
            mParagraphs = mParagraphs.Select(a => a.Trim())
                                     .Select(a => (a.AddToFront(StartDoc) + EndDoc)).ToList();


            mBlocks.RemoveAt(0);
            mBlocks = mBlocks.Select(a => a.AddToFront("<p class=\"code\">"))
                             .Select(a => a.Substring(0, a.Length - 7))
                             .Select(a => (a + EndDoc)).ToList();
        }

        void AccumalateFragments() {
            int j = 0;
            int k = 0;
            for (int i = 0; i < BlockIndicators.Count; i++) {
                while (j != (BlockIndicators[i])) {
                    HTMLText += mParagraphs[0] + '\n' + '\n';
                    j++;
                    mParagraphs.RemoveAt(0);
                }
                HTMLText += mBlocks[k] + '\n' + '\n';
                k++;
            }
            foreach (var v in mParagraphs)
                HTMLText += v + '\n';
            HTMLText += "</body>";
            HTMLText += "</html>";

        }

        void GenerateHTMLFile() {
            StreamWriter sw = new StreamWriter(DirOfHTMLFile);
            sw.Write(HTMLText);
            sw.Close();
        }

        void GenerateCSSFile() {
            StreamWriter sw = new StreamWriter(DirOfCSSFile);
            sw.Write(CssGenerator());
            sw.Close();
        }
        #endregion




        #region---Private Members
        string Path;
        string DirOfHTMLFile;
        string DirOfCSSFile;
        bool CodeMode;
        string HTMLText;
        string TempPara = "";
        string TempBlock = "";
        const string StartDoc = "<p>";
        const string EndDoc = "</p>";
        const string OpenBracket = "&#x2329;&#x2329;";
        const string CloseBracket = "&#x232A;&#x232A;";
        const string EqualSign = "&#x2261;";
        const string SpaceUnicode = "&nbsp;";
        List<string> mParagraphs = new List<string>();
        List<string> mBlocks = new List<string>();
        List<int> BlockIndicators = new List<int>();
        #endregion



        static void Main(string[] args) {
            try {
                HtmlCSSGen ob = new HtmlCSSGen(args[0]);
                ob.HtmlGenerator();
                ob.AddLastLine();
                ob.ModifyParagraphsAndBlocks();
                ob.AccumalateFragments();
                ob.GenerateCSSFile();
                ob.GenerateHTMLFile();
            }

            catch (FileNotFoundException) {
                Console.WriteLine("FILE NOT FOUND");
            }

            catch (DirectoryNotFoundException) {
                Console.WriteLine("DIRECTORY NOT FOUND");
            }
        }
    }
}
