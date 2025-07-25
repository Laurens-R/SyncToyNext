using System;
using System.Collections.Generic;

namespace DiffPlex.DiffBuilder.Model
{
    public enum ChangeType
    {
        Unchanged,
        Deleted,
        Inserted,
        Imaginary,
        Modified
    }

    public class DiffPiece : IEquatable<DiffPiece>
    {
        public ChangeType Type { get; set; }
        public int? Position { get; set; }
        public string? Text { get; set; }
        public List<DiffPiece> SubPieces { get; set; } = new List<DiffPiece>();

        public DiffPiece(string? text, ChangeType type, int? position = null)
        {
            Text = text;
            Position = position;
            Type = type;
        }

        public DiffPiece()
            : this(null, ChangeType.Imaginary)
        {
        }

        public override bool Equals(object? obj)
        {

            return obj is DiffPiece other && Equals(other);
        }

        public bool Equals(DiffPiece? other)
        {
            return other != null
                && Type == other.Type
                && EqualityComparer<int?>.Default.Equals(Position, other.Position)
                && Text == other.Text
                && SubPiecesEqual(other);
        }

        public override int GetHashCode()
        {
            var hashCode = 1688038063;
            hashCode = hashCode * -1521134295 + Type.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<int>.Default.GetHashCode(Position??0);
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(Text??String.Empty);
            hashCode = hashCode * -1521134295 + EqualityComparer<int?>.Default.GetHashCode(SubPieces?.Count??0);
            return hashCode;
        }

        private bool SubPiecesEqual(DiffPiece other)
        {
            if (SubPieces is null)
                return other.SubPieces is null;
            else if (other.SubPieces is null)
                return false;

            if (SubPieces.Count != other.SubPieces.Count)
                return false;

            for (int i = 0; i < SubPieces.Count; i++)
            {
                if (!Equals(SubPieces[i], other.SubPieces[i]))
                    return false;
            }

            return true;
        }
    }
}