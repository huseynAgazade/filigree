using System.Text;
using System.Text.Json;

namespace Filigree.Core;

/// <summary>
/// One compact JSON object per line. Rotates when the file exceeds maxBytes,
/// keeping the most recent maxFiles archives.
/// </summary>
public sealed class JsonlSink : IEventSink
{
    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = false };

    private readonly string _path;
    private readonly long _maxBytes;
    private readonly int _maxFiles;
    private readonly Lock _gate = new();
    private StreamWriter _writer;
    private long _written;

    public JsonlSink(string path, long maxBytes = 64L * 1024 * 1024, int maxFiles = 5)
    {
        _path = Path.GetFullPath(path);
        _maxBytes = maxBytes;
        _maxFiles = maxFiles;

        var dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        _written = File.Exists(_path) ? new FileInfo(_path).Length : 0;
        _writer = Open();
    }

    public void Write(FiligreeEvent evt)
    {
        var line = JsonSerializer.Serialize(evt, Opts);
        lock (_gate)
        {
            _writer.WriteLine(line);
            _written += Encoding.UTF8.GetByteCount(line) + Environment.NewLine.Length;
            if (_written >= _maxBytes) Rotate();
        }
    }

    private StreamWriter Open()
        => new(new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.Read), Encoding.UTF8)
           { AutoFlush = true };

    private void Rotate()
    {
        _writer.Dispose();

        var oldest = $"{_path}.{_maxFiles}";
        if (File.Exists(oldest)) File.Delete(oldest);

        for (var i = _maxFiles - 1; i >= 1; i--)
        {
            var from = $"{_path}.{i}";
            if (File.Exists(from)) File.Move(from, $"{_path}.{i + 1}", overwrite: true);
        }

        if (File.Exists(_path)) File.Move(_path, $"{_path}.1", overwrite: true);

        _written = 0;
        _writer = Open();
    }

    public void Dispose()
    {
        lock (_gate) _writer.Dispose();
    }
}
