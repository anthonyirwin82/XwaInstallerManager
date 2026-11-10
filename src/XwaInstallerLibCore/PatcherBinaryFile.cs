namespace XwaInstallerLibCore;

internal class PatcherBinaryFile
{
    private readonly string _filename;
    private readonly byte[] _bytes;

    public PatcherBinaryFile(string filename)
    {
        _filename = filename;
        _bytes = File.ReadAllBytes(filename);
    }

    public void Close()
    {
        File.WriteAllBytes(_filename, _bytes);
    }

    public void ApplyPatch(byte[]? patchData)
    {
        if (patchData is null || patchData.Length == 0)
        {
            return;
        }

        ZtFile ztFile;

        using (var stream = new MemoryStream(patchData, false))
        {
            ztFile = ZtFile.From(stream);
        }

        ztFile.Apply(_bytes);
    }
}
