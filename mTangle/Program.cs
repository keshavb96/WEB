using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace mTangle {

    class Program {
        public Program(string Path) {
            Dir = "";
            path = Path;
            List<char> tmp = new List<char>();
            for (int i = path.Length - 1; i > 0; i--) {
                if (path[i] == '\\')
                    break;
                tmp.Add(path[i]);
            }
            tmp.Reverse();
            foreach (var v in tmp) {
                Dir += v;
            }
            Code.Add("<<Program>>");
        }

        #region--------------------------------------------------------------------------------STEP ONE
        /// STEP ONE :
        /// This function goes through the entire .m file and populates the Dictionary mBLocks with blocks of CS Code
        /// It only populates the dictionary with blocks which end with >>= or >>+= 
        /// When a particular block ends with >>= it is added along with its contents into the dictionary
        /// When a particular block ends with >>+=, the code in the current block is added to the code of the block already in the dictionary whose name corresponds to the current block
        public void Parser() {
            StreamReader sr = new StreamReader(path);
            string txt;
            string NameOfBlock = "";
            while ((txt = sr.ReadLine()) != null) {
                if (txt.Trim().StartsWith("<<"))
                    txt = txt.Trim();

                LinesOfCode CurrentLine = new LinesOfCode { File = path, Line = LineNumber++, Text = txt };

                if (txt.StartsWith("<<") && txt.EndsWith(">>"))
                    NewBlocks.Add(txt, LineNumber);

                if (txt.StartsWith("<<") && txt.EndsWith(">>=")) {
                    //A new block (not existing in mBLocks) is encountered
                    NameOfBlock = txt;
                    NewCodeMode = true; // For the next few iterations of the loop till a new line starts with @ (when NewCodeMode will become false) code will be added to mBlocks
                }

                if (txt.StartsWith("<<") && txt.EndsWith("+=")) {
                    //A block whose contents are to be added on to an already existing block in mBlocks is encountered.
                    AddCodeMode = true; // For the next few itearations of the loop till a new line starts with @ (when AddCodeMode will become false) code will be added to an already existing block in mBlocks.
                    NameOfBlock = txt.Substring(0, txt.Length - 2);
                    NameOfBlock += "=";
                }

                if (txt.StartsWith("@")) {
                    NewCodeMode = false;
                    AddCodeMode = false;
                    if (CodeGathered) {
                        //This condition is satisfied if and only if a line starting with @ is encountered after the end of a code block.
                        //At this point the List<string> tmp contains all the lines of code that was present in the block that was just extracted.
                        BlockOfCode bc = new BlockOfCode(); // A BlockOfCode object corresponding to the block was just extracted.
                        bc.Name = NameOfBlock;
                        for (int i = 0; i < tmp.Count; i++)
                            bc.lines.Add(tmp[i]);
                        mBlocks.Add(NameOfBlock, bc);
                        tmp.Clear();
                        CodeGathered = false;
                    }
                }

                if (NewCodeMode == false && AddCodeMode == false) {
                    //A new code block that is independant (a block not part of the definition of another block) is encountered.
                    if (txt.StartsWith("<<") && txt.EndsWith(">>"))
                        Code.Add(txt);
                    //At the end of parsing the .m file the List<string> Code will contain the name of all blocks that are independant.
                }

                if (NewCodeMode) {
                    if (!(txt.EndsWith(">>="))) {
                        //The code of a new block along with the name of the block is being added to mBlocks.
                        tmp.Add(CurrentLine);
                        CodeGathered = true;
                    }
                }

                if (AddCodeMode) {
                    if (!(txt.EndsWith(">>+="))) {
                        //Code is to be added to a block already existing in mBlocks
                        mBlocks[NameOfBlock].lines.Add(CurrentLine);
                    }
                }
            }
            if (tmp.Count != 0) {
                BlockOfCode bc = new BlockOfCode() { Name = NameOfBlock };
                foreach (var v in tmp) {
                    bc.lines.Add(v);
                }
                mBlocks.Add(NameOfBlock, bc);
                tmp.Clear();
            }
        }
        #endregion

        #region--------------------------------------------------------------------------------STEP TWO
        /// STEP TWO :
        /// In this step we search for Independant blocks in the .m file (now in the Code list) and replace in each position in the
        /// Code list which has such a block, an First order expansion of that block
        /// These First order expanded blocks may contain other blocks within them which may be undefined so far
        public void BlockFinder() {
            for (int i = 0; i < Code.Count; i++) {
                if (Code[i].StartsWith("<<") && Code[i].EndsWith(">>"))
                    BlockExpander(Code[i], i);
            }
        }

        public void BlockExpander(string blockname, int pos) {
            string tmp = blockname + "=";
            Code[pos] = GatherCode(tmp);
        }
        #endregion


        #region-------------------------------------------------------------------------------STEP THREE
        public void CodeGatherer() {
            for (int i = 0; i < Code.Count; i++) {
                string[] A = Code[i].Split('\n');
                if (BlockDefCheck(A))
                    CodeExpander(A, i);
            }
        }

        public void CodeExpander(string[] lines, int position) {
            string tmp = "";
            for (int i = 0; i < lines.Length; i++) {
                if (lines[i].StartsWith("<<") && lines[i].EndsWith(">>")) {
                    tmp = lines[i] + "=";
                    lines[i] = GatherCode(tmp);
                    Update(lines, position);
                }
            }
        }

        public void Update(string[] lines, int index) {
            string tmp = "";
            for (int i = 0; i < lines.Length; i++) {
                tmp += lines[i];
                tmp += '\n';
            }
            Code[index] = tmp;
            CodeGatherer();
        }

        public bool BlockDefCheck(string[] lines) {
            for (int i = 0; i < lines.Length; i++) {
                if (lines[i].StartsWith("<<") && lines[i].EndsWith(">>"))
                    return true;
            }
            return false;
        }
        #endregion

        #region----------------------------------- Private Methods
        string GatherCode(string blockdef) {
            //The string lines is a multi line temporary string that will be instantiated each time GatherCode is called
            string lines = "";
            string tmpr = blockdef.Substring(0, blockdef.Length - 1);
            try {
                for (int i = 0; i < mBlocks[blockdef].lines.Count; i++) {
                    lines += "#line" + " " + mBlocks[blockdef].lines[i].Line.ToString(); //line directives are added before each line of code
                    lines += " " + '\"' + Dir + '\"';
                    lines += '\n';
                    lines += mBlocks[blockdef].lines[i].Text;
                    lines += '\n';
                }
                mBlocks[blockdef].Used = true;
                return lines; // At the end of each function call lines will contain an expansion for the code block whose name is blockdef
            }
            catch (KeyNotFoundException) {
                UndefinedBlocks.Add(tmpr); // All undefined blocks i.e blocks that are used but not defined (<<Sample>> is present but <<Sample>>= is not)
                return null;
            }
        }

        void DanglingBlockAccumalator() // This function gathers all 'Dangling Blocks' (blocks like <<Sample>>= for which <<Sample>> does not exist) into a hashset
        {
            foreach (var v in mBlocks) {
                if (v.Value.Used == false) // If a block in mBlocks is not used then its Used field would be false
                    DanglingBlocks.Add(v.Key);
            }
        }

        void WriteToFileAndCompile() {
            string ActPath = path.Substring(0, path.Length - 1);
            ActPath += "cs";
            int nExitCode;
            StreamWriter sw = new StreamWriter(ActPath);
            foreach (var v in Code)
                sw.WriteLine(v);
            sw.Close();
            string prog = @"c:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe";
            string[] output = CompileApp(prog, ActPath, out nExitCode);
            if (nExitCode != 0)
                foreach (string s in output)
                    Console.WriteLine(s);
        }

        string[] CompileApp(string exe, string arg, out int ExitCode) {
            List<string> output = new List<string>();
            ProcessStartInfo startinfo = new ProcessStartInfo() { CreateNoWindow = true, UseShellExecute = false, WorkingDirectory = Directory.GetCurrentDirectory(), Arguments = arg, FileName = exe, RedirectStandardError = true, RedirectStandardOutput = true };
            Process proc = Process.Start(startinfo);
            if (proc == null) throw new IOException(string.Format("Cannot execute {0}", exe));
            proc.OutputDataReceived += (s, e) => { if (e.Data != null) output.Add(e.Data); };
            proc.ErrorDataReceived += (s, e) => { if (e.Data != null) output.Add(e.Data); };
            proc.BeginOutputReadLine(); proc.BeginErrorReadLine();
            proc.WaitForExit(); ExitCode = proc.ExitCode;
            proc.Close();
            return output.ToArray();
        }
        #endregion

        #region-------------------Private Members
        List<string> Code = new List<string>();
        List<LinesOfCode> tmp = new List<LinesOfCode>();
        Dictionary<string, int> NewBlocks = new Dictionary<string, int>();
        Dictionary<string, BlockOfCode> mBlocks = new Dictionary<string, BlockOfCode>();
        HashSet<string> DanglingBlocks = new HashSet<string>();
        HashSet<string> UndefinedBlocks = new HashSet<string>();
        bool CodeGathered;
        int LineNumber;
        bool NewCodeMode;
        bool AddCodeMode;
        string path;
        string Dir;
        #endregion

        static void Main(string[] args) {
            try {
                StreamReader sr = new StreamReader(args[0]);
                Program ob = new Program(args[0]);
                ob.Parser();
                ob.BlockFinder();
                ob.CodeGatherer();
                ob.DanglingBlockAccumalator();
                if ((ob.DanglingBlocks.Count == 0) && (ob.UndefinedBlocks.Count == 0))
                    ob.WriteToFileAndCompile();
                else {
                    if ((ob.DanglingBlocks.Count == 0) && (ob.UndefinedBlocks.Count != 0)) {
                        foreach (var v in ob.UndefinedBlocks) {
                            int ErNo = 3001;
                            int n = ob.NewBlocks[v];
                            string FirstPl = string.Format("(" + "{0}, 1)", n.ToString());
                            string error = string.Format(ob.Dir + "{0} : error {1}: Undefined Block" + " " + v + " found", FirstPl, ErNo.ToString());
                            Console.WriteLine(error);
                        }
                    }

                    else if ((ob.DanglingBlocks.Count != 0) && (ob.UndefinedBlocks.Count == 0)) {
                        foreach (var v in ob.DanglingBlocks) {
                            int ErNo = 5001;
                            int n = ob.mBlocks[v].lines[0].Line;
                            string FirstPl = string.Format("(" + "{0}, 1)", n.ToString());
                            string error = string.Format(ob.Dir + "{0} : error {1}: Dangling Block" + " " + v + " found", FirstPl, ErNo.ToString());
                            Console.WriteLine(error);
                        }
                    }

                    else {
                        foreach (var v in ob.UndefinedBlocks) {
                            int ErNo = 3001;
                            int n = ob.NewBlocks[v];
                            string FirstPl = string.Format("(" + "{0}, 1)", n.ToString());
                            string error = string.Format(ob.Dir + "{0} : error {1}: Undefined Block" + " " + v + " found", FirstPl, ErNo.ToString());
                            Console.WriteLine(error);
                        }

                        foreach (var v in ob.DanglingBlocks) {
                            int ErNo = 5001;
                            int n = ob.mBlocks[v].lines[0].Line;
                            string FirstPl = string.Format("(" + "{0}, 1)", n.ToString());
                            string error = string.Format(ob.Dir + "{0} : error {1}: Dangling Block" + " " + v + " found", FirstPl, ErNo.ToString());
                            Console.WriteLine(error);
                        }
                    }
                }
            }

            catch (FileNotFoundException) {
                Console.WriteLine("File Not Found");
            }

            catch (IndexOutOfRangeException) {
                Console.WriteLine("Please enter path name of your Literate Programming Document");
            }

            catch (DirectoryNotFoundException) {
                Console.WriteLine("Enter Valid Directory");
            }
        }
    }
}
