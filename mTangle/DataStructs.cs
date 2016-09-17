using System.Collections.Generic;

namespace mTangle {
    class BlockOfCode {
        public string Name;
        public bool Used;
        public List<LinesOfCode> lines = new List<LinesOfCode>();
    }

    class LinesOfCode {
        public string Text;
        public string File;
        public int Line;
    }
}
