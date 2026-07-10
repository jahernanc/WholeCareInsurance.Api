using System.IO.Compression;

namespace WholeCareInsurance.api.Utils
{
    public static class FileValidationHelper
    {
        public const long MaxFileSizeBytes = 5 * 1024 * 1024;

        private static readonly string[] AllowedExtensions = [".pdf", ".docx", ".jpg", ".jpeg"];

        public static bool HasAllowedExtension(string fileName)
            => AllowedExtensions.Contains(Path.GetExtension(fileName).ToLowerInvariant());

        public static async Task<bool> MatchesContentAsync(Stream content, string extension)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".pdf":
                    return await MatchesSignatureAsync(content, [0x25, 0x50, 0x44, 0x46]); // %PDF
                case ".jpg":
                case ".jpeg":
                    return await MatchesSignatureAsync(content, [0xFF, 0xD8, 0xFF]);
                case ".docx":
                    return await IsValidDocxAsync(content);
                default:
                    return false;
            }
        }

        private static async Task<bool> MatchesSignatureAsync(Stream content, byte[] signature)
        {
            content.Position = 0;
            var buffer = new byte[signature.Length];
            var read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length));
            return read == signature.Length && buffer.SequenceEqual(signature);
        }

        private static async Task<bool> IsValidDocxAsync(Stream content)
        {
            // .docx is a ZIP (OOXML) container: check the ZIP signature first,
            // then confirm the OOXML entries are actually present — a plain
            // .zip renamed to .docx has the right signature but not these entries.
            if (!await MatchesSignatureAsync(content, [0x50, 0x4B, 0x03, 0x04]))
                return false;

            content.Position = 0;
            try
            {
                using var archive = new ZipArchive(content, ZipArchiveMode.Read, leaveOpen: true);
                return archive.GetEntry("[Content_Types].xml") != null
                    && archive.GetEntry("word/document.xml") != null;
            }
            catch (InvalidDataException)
            {
                return false;
            }
            finally
            {
                content.Position = 0;
            }
        }
    }
}
