using Signum.Engine.Mailing;
using Signum.Entities.Files;
using Signum.Entities.Isolation;
using System.IO;

namespace Signum.Engine.Files;

public static class FileTypeLogic
{
    public static Dictionary<FileTypeSymbol, IFileTypeAlgorithm> FileTypes = new Dictionary<FileTypeSymbol, IFileTypeAlgorithm>();

    public static void Register(FileTypeSymbol fileTypeSymbol, IFileTypeAlgorithm algorithm)
    {
        if (fileTypeSymbol == null)
            throw AutoInitAttribute.ArgumentNullException(typeof(FileTypeSymbol), nameof(fileTypeSymbol));

        if (algorithm == null)
            throw new ArgumentNullException(nameof(algorithm));

        FileTypes.Add(fileTypeSymbol, algorithm);
    }

    public static void Start(SchemaBuilder sb)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            SymbolLogic<FileTypeSymbol>.Start(sb,() => FileTypes.Keys.ToHashSet());
            sb.Include<FileTypeSymbol>()
                .WithQuery(() => f => new
                {
                    Entity = f,
                    f.Key
                });
        }
    }

    public static IFileTypeAlgorithm GetAlgorithm(this FileTypeSymbol fileType)
    {
        return FileTypes.GetOrThrow(fileType);
    }
}

public interface IFileTypeAlgorithm
{
    bool OnlyImages { get; set; }
    long? MaxSizeInBytes { get; set; }
    void SaveFile(IFilePath fp);
    Task SaveFileAsync(IFilePath fp, CancellationToken token = default);
    void ValidateFile(IFilePath fp);
    void DeleteFiles(IEnumerable<IFilePath> files);
    void DeleteFilesIfExist(IEnumerable<IFilePath> files);
    byte[] ReadAllBytes(IFilePath fp);
    Stream OpenRead(IFilePath fp);
    void MoveFile(IFilePath ofp, IFilePath nfp);
    PrefixPair GetPrefixPair(IFilePath efp);
}

public static class SuffixGenerators
{
    //No GUID, use only for icons or public domain files
    public static class UNSAFE
    {
        public static readonly Func<IFilePath, string> FileName = (IFilePath fp) => Path.GetFileName(fp.FileName)!;
        public static readonly Func<IFilePath, string> CalculatedDirectory_FileName = (IFilePath fp) => Path.Combine(fp.CalculatedDirectory!, Path.GetFileName(fp.FileName)!);

        public static readonly Func<IFilePath, string> Year_FileName = (IFilePath fp) => Path.Combine(Clock.Now.Year.ToString(), Path.GetFileName(fp.FileName)!);
        public static readonly Func<IFilePath, string> Year_Month_FileName = (IFilePath fp) => Path.Combine(Clock.Now.Year.ToString(), Clock.Now.Month.ToString(), Path.GetFileName(fp.FileName)!);

    }

    //Thanks to the GUID, the file name can not be guessed
    public static class Safe
    {
        public static readonly Func<IFilePath, string> Year_GuidExtension = (IFilePath fp) => Path.Combine(Clock.Now.Year.ToString(), Guid.NewGuid().ToString() + Path.GetExtension(fp.FileName));
        public static readonly Func<IFilePath, string> Year_Month_GuidExtension = (IFilePath fp) => Path.Combine(Clock.Now.Year.ToString(), Clock.Now.Month.ToString(), Guid.NewGuid() + Path.GetExtension(fp.FileName));

        public static readonly Func<IFilePath, string> YearMonth_Guid_Filename = (IFilePath fp) => Path.Combine(Clock.Now.ToString("yyyy-MM"), Guid.NewGuid().ToString(), Path.GetFileName(fp.FileName)!);
        public static readonly Func<IFilePath, string> Isolated_YearMonth_Guid_Filename = (IFilePath fp) => Path.Combine(IsolationEntity.Current?.IdOrNull.ToString() ?? "None", Clock.Now.ToString("yyyy-MM"), Guid.NewGuid().ToString(), Path.GetFileName(fp.FileName)!);
    }
}

public abstract class FileTypeAlgorithmBase
{
    public Action<IFilePath>? OnValidateFile { get; set; }
    public long? MaxSizeInBytes { get; set; }
    public virtual bool OnlyImages { get; set; }

    public void ValidateFile(IFilePath fp)
    {
        if (OnlyImages)
        {
            var mime = MimeMapping.GetMimeType(fp.FileName);
            if (mime == null || !mime.StartsWith("image/"))
                throw new ApplicationException(FileMessage.TheFile0IsNotA1.NiceToString(fp.FileName, "image/*"));
        }

        if (MaxSizeInBytes != null)
        {
            if (fp.BinaryFile!.Length > MaxSizeInBytes)
                throw new ApplicationException(FileMessage.File0IsTooBigTheMaximumSizeIs1.NiceToString(fp.FileName, MaxSizeInBytes.Value.ToComputerSize()));
        }

        OnValidateFile?.Invoke(fp);
    }
}

