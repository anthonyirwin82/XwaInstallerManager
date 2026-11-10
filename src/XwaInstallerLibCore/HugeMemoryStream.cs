namespace XwaInstallerLibCore;

public sealed class HugeMemoryStream : Stream
{
    private const int PageSize = 1024000;
    private const int AllocStep = 1024;

    private byte[]?[]? _streamBuffers;
    private int _pageCount;
    private long _allocatedBytes;
    private long _position;
    private long _length;

    public override bool CanRead => true;

    public override bool CanSeek => true;

    public override bool CanWrite => true;

    public override long Length => _length;

    public override long Position
    {
        get => _position;
        set
        {
            if (value > _length)
            {
                throw new InvalidOperationException("Position > Length");
            }

            if (value < 0)
            {
                throw new InvalidOperationException("Position < 0");
            }

            _position = value;
        }
    }

    private static int GetPageCount(long length)
    {
        int num = (int)(length / PageSize) + 1;

        if (length % PageSize == 0)
        {
            num--;
        }

        return num;
    }

    private void ExtendPages()
    {
        if (_streamBuffers is null)
        {
            _streamBuffers = new byte[AllocStep][];
        }
        else
        {
            var array = new byte[_streamBuffers.Length + AllocStep][];
            Array.Copy(_streamBuffers, array, _streamBuffers.Length);
            _streamBuffers = array;
        }

        _pageCount = _streamBuffers.Length;
    }

    private void AllocSpaceIfNeeded(long value)
    {
        if (value < 0)
        {
            throw new InvalidOperationException("AllocSpaceIfNeeded < 0");
        }

        if (value == 0L)
        {
            return;
        }

        int pageCount = GetPageCount(_allocatedBytes);
        int pageCount2 = GetPageCount(value);

        while (pageCount < pageCount2)
        {
            if (pageCount == _pageCount)
            {
                ExtendPages();
            }

            _streamBuffers![pageCount++] = new byte[PageSize];
        }

        _allocatedBytes = (long)pageCount * PageSize;
        value = Math.Max(value, _length);

        if (_position > (_length = value))
        {
            _position = _length;
        }
    }

    public override void Flush()
    {
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int num = (int)(_position / PageSize);
        int num2 = (int)(_position % PageSize);
        int num3 = PageSize - num2;
        long position = _position;

        if (position + count > _length)
        {
            count = (int)(_length - position);
        }

        while (count != 0 && _position < _length)
        {
            if (num3 > count)
            {
                num3 = count;
            }

            Array.Copy(_streamBuffers![num++]!, num2, buffer, offset, num3);
            offset += num3;
            _position += num3;
            count -= num3;
            num2 = 0;
            num3 = PageSize;
        }

        return (int)(_position - position);
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        switch (origin)
        {
            case SeekOrigin.Current:
                offset += _position;
                break;
            case SeekOrigin.End:
                offset = _length + offset;
                break;
            case SeekOrigin.Begin:
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(origin));
        }

        return Position = offset;
    }

    public override void SetLength(long value)
    {
        if (value < 0)
        {
            throw new InvalidOperationException("SetLength < 0");
        }

        if (value == 0L)
        {
            _streamBuffers = null;
            _allocatedBytes = _position = _length = 0L;
            _pageCount = 0;
            return;
        }

        int num = GetPageCount(_allocatedBytes);
        int pageCount = GetPageCount(value);

        while (num > pageCount)
        {
            _streamBuffers![--num] = null;
        }

        AllocSpaceIfNeeded(value);

        if (_position > (_length = value))
        {
            _position = _length;
        }
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        int num = (int)(_position / PageSize);
        int num2 = (int)(_position % PageSize);
        int num3 = PageSize - num2;
        long position = _position;
        AllocSpaceIfNeeded(position + count);

        while (count != 0)
        {
            if (num3 > count)
            {
                num3 = count;
            }

            Array.Copy(buffer, offset, _streamBuffers![num++]!, num2, num3);
            offset += num3;
            _position += num3;
            count -= num3;
            num2 = 0;
            num3 = PageSize;
        }
    }
}
