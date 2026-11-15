#region Usings
using System;
using System.IO;
using System.Text;
#endregion

namespace SF.CodeGeneration;

//getting the file out without the string ToString makes: to a TextWriter, to a Stream, or straight to a file. For a
//stream or a file the text is encoded into a byte array kept for every file, so once it is big enough nothing is
//allocated for the text. The default encoding is UTF-8 without a byte order mark, as File.WriteAllText; an encoding
//with a preamble (new UTF8Encoding(true)) writes its byte order mark first
public static partial class CSharpFile
{
    private static readonly Encoding _utf8 = new UTF8Encoding(false);

    //the encoded file, reused for every file: grows to the largest one and stays
    private static byte[] _bytes = Array.Empty<byte>();

    /// <summary>
    /// Writes the file to <paramref name="writer"/> (a StreamWriter, a StringWriter...) without making a string.
    /// </summary>
    public static void WriteTo(TextWriter writer)
    {
        ArgumentNullException.ThrowIfNull(writer);
        _b.WriteTo(writer);
    }

    /// <summary>
    /// Writes the file to <paramref name="stream"/> as UTF-8 without a byte order mark, without making a string.
    /// </summary>
    public static void WriteTo(Stream stream)
    {
        WriteTo(stream, _utf8);
    }

    /// <summary>
    /// Writes the file to <paramref name="stream"/> in <paramref name="encoding"/>: its byte order mark first when it
    /// has one, then the text. No string is made.
    /// </summary>
    public static void WriteTo(Stream stream, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(encoding);
        var count = Encode(encoding);
        stream.Write(encoding.Preamble);
        stream.Write(_bytes, 0, count);
    }

    /// <summary>
    /// Saves the file to <paramref name="path"/> as UTF-8 without a byte order mark, replacing a file that is there.
    /// No string is made.
    /// </summary>
    public static void Save(string path)
    {
        Save(path, _utf8);
    }

    /// <summary>
    /// Saves the file to <paramref name="path"/> in <paramref name="encoding"/> (its byte order mark first when it has
    /// one), replacing a file that is there. The folder has to exist. No string is made.
    /// </summary>
    public static void Save(string path, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(encoding);
        var count = Encode(encoding);
        var preamble = encoding.Preamble;

        //the file is opened for writing only and written in place: no stream or buffer of its own
        using var handle = File.OpenHandle(path, FileMode.Create, FileAccess.Write);
        RandomAccess.Write(handle, preamble, 0);
        RandomAccess.Write(handle, new ReadOnlySpan<byte>(_bytes, 0, count), preamble.Length);
    }

    /// <summary>
    /// Encodes the text into the reused byte array (grown when it is too small) and returns how many bytes it took.
    /// </summary>
    private static int Encode(Encoding encoding)
    {
        var text = _b.Text;
        var count = encoding.GetByteCount(text);
        if(_bytes.Length < count)
            _bytes = new byte[Math.Max(count, _bytes.Length * 2)];

        return encoding.GetBytes(text, _bytes);
    }
}
