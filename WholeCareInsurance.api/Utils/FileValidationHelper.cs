namespace WholeCareInsurance.api.Utils
{
    public static class FileValidationHelper
    {
        public const long MaxFileSizeBytes = 5 * 1024 * 1024;

        private static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png"];

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
                case ".png":
                    return await MatchesSignatureAsync(content, [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]);
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
    }
}
