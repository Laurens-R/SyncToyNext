using System.Collections.Generic;

namespace DiffPlex.Model
{
    public class ModificationData
    {
        public int[] HashedPieces { get; set; } = System.Array.Empty<int>();

        public string RawData { get; }

        public bool[] Modifications { get; set; } = System.Array.Empty<bool>();

        public IReadOnlyList<string>? Pieces { get; set; } 

        public ModificationData(string str)
        {
            RawData = str;
        }
    }
}