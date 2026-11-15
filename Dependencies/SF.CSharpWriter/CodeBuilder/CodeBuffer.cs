#region Usings
using System;
using System.Text;
#endregion

namespace SF.CodeGeneration;

public static partial class CSharpFile
{
    /// <summary>
    /// <para>The text of the file being written: one char array, doubled when it is full and never split.</para>
    /// <para>CSharpFile reuses it for every file it writes. Starting a new file only sets the length to zero, and
    /// Replace copies the text into a second array kept for it and writes the result back, so neither allocates once
    /// the arrays are big enough. (A StringBuilder grows in chunks, and clearing it after it has split allocates a
    /// new array of its whole capacity, often on the large object heap.)</para>
    /// <para>Append of null writes nothing and Replace replaces every match from left to right, as StringBuilder
    /// does. AppendLine always ends the line with CRLF, on every operating system.</para>
    /// </summary>
    private sealed class CodeBuffer
    {
        private const string _newLine = "\r\n";

        private char[] _chars;
        private char[] _spare;
        private int _length;

        public CodeBuffer(int capacity)
        {
            _chars = new char[capacity];
            _spare = Array.Empty<char>();
        }

        /// <summary>
        /// The number of characters written; it can only be made shorter (Length = 0 starts over).
        /// </summary>
        public int Length
        {
            get
            {
                return _length;
            }
            set
            {
                if(value < 0 || value > _length)
                    throw new ArgumentOutOfRangeException(nameof(value), "The length can only be made shorter.");

                _length = value;
            }
        }

        public CodeBuffer Append(string value)
        {
            if(value != null)
                Append(value.AsSpan());

            return this;
        }

        public CodeBuffer Append(ReadOnlySpan<char> value)
        {
            EnsureCapacity(_length + value.Length);
            value.CopyTo(_chars.AsSpan(_length));
            _length += value.Length;
            return this;
        }

        public CodeBuffer Append(char value)
        {
            EnsureCapacity(_length + 1);
            _chars[_length] = value;
            _length++;
            return this;
        }

        public CodeBuffer Append(char value, int repeatCount)
        {
            if(repeatCount <= 0)
                return this;

            EnsureCapacity(_length + repeatCount);
            _chars.AsSpan(_length, repeatCount).Fill(value);
            _length += repeatCount;
            return this;
        }

        /// <summary>
        /// Copies the text of a StringBuilder (an interpolated argument) without making a string of it.
        /// </summary>
        public CodeBuffer Append(StringBuilder value)
        {
            var count = value.Length;
            if(count == 0)
                return this;

            EnsureCapacity(_length + count);
            value.CopyTo(0, _chars.AsSpan(_length, count), count);
            _length += count;
            return this;
        }

        /// <summary>
        /// Ends the line with CRLF, on every operating system.
        /// </summary>
        public CodeBuffer AppendLine()
        {
            return Append(_newLine);
        }

        public CodeBuffer AppendLine(string value)
        {
            Append(value);
            return Append(_newLine);
        }

        /// <summary>
        /// Replaces every match of <paramref name="oldValue"/>, from left to right, like StringBuilder.Replace.
        /// </summary>
        public CodeBuffer Replace(string oldValue, string newValue)
        {
            if(oldValue == null)
                throw new ArgumentNullException(nameof(oldValue));

            if(oldValue.Length == 0)
                throw new ArgumentException("The text to replace cannot be empty.", nameof(oldValue));

            newValue ??= string.Empty;
            var text = _chars.AsSpan(0, _length);
            var first = text.IndexOf(oldValue.AsSpan(), StringComparison.Ordinal);
            if(first < 0)
                return this;

            //count the matches the way the replacement will find them, to know the new length
            var count = 0;
            var at = first;
            while(at >= 0)
            {
                count++;
                var next = text.Slice(at + oldValue.Length).IndexOf(oldValue.AsSpan(), StringComparison.Ordinal);
                at = next < 0 ? -1 : at + oldValue.Length + next;
            }

            var newLength = _length + (count * (newValue.Length - oldValue.Length));

            //the current text goes to the spare array; the replaced text is written back from it
            if(_spare.Length < _length)
                _spare = new char[Math.Max(_length, _chars.Length)];

            text.CopyTo(_spare);
            var source = _spare.AsSpan(0, _length);
            EnsureCapacity(newLength);

            var read = 0;
            var write = 0;
            at = first;
            while(at >= 0)
            {
                source.Slice(read, at - read).CopyTo(_chars.AsSpan(write));
                write += at - read;
                newValue.AsSpan().CopyTo(_chars.AsSpan(write));
                write += newValue.Length;
                read = at + oldValue.Length;

                var next = source.Slice(read).IndexOf(oldValue.AsSpan(), StringComparison.Ordinal);
                at = next < 0 ? -1 : read + next;
            }

            source.Slice(read).CopyTo(_chars.AsSpan(write));
            _length = newLength;
            return this;
        }

        public override string ToString()
        {
            return new string(_chars, 0, _length);
        }

        /// <summary>
        /// The text written so far, without copying it.
        /// </summary>
        public ReadOnlySpan<char> Text
        {
            get
            {
                return _chars.AsSpan(0, _length);
            }
        }

        /// <summary>
        /// Writes the text to a writer straight from the array: no string is made.
        /// </summary>
        public void WriteTo(System.IO.TextWriter writer)
        {
            writer.Write(_chars, 0, _length);
        }

        /// <summary>
        /// Doubles the array (as many times as needed) when <paramref name="needed"/> characters do not fit.
        /// </summary>
        private void EnsureCapacity(int needed)
        {
            if(needed <= _chars.Length)
                return;

            var size = Math.Max(_chars.Length, 16);
            while(size < needed)
                size = size > int.MaxValue / 2 ? needed : size * 2;

            var bigger = new char[size];
            _chars.AsSpan(0, _length).CopyTo(bigger);
            _chars = bigger;
        }
    }
}
