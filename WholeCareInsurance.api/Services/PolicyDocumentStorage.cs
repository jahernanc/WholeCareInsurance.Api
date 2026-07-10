namespace WholeCareInsurance.api.Services
{
    public class PolicyDocumentStorage : IPolicyDocumentStorage
    {
        private readonly string _basePath;

        public PolicyDocumentStorage(IWebHostEnvironment env, IConfiguration configuration)
        {
            var relativePath = configuration["Storage:PolicyDocumentsPath"] ?? "App_Data/PolicyDocuments";
            _basePath = Path.Combine(env.ContentRootPath, relativePath);
        }

        public async Task<string> SaveAsync(int policyId, Stream content, string extension)
        {
            var folder = GetPolicyFolder(policyId);
            Directory.CreateDirectory(folder);

            var storedFileName = $"{Guid.NewGuid()}{extension.ToLowerInvariant()}";
            var fullPath = Path.Combine(folder, storedFileName);

            content.Position = 0;
            using (var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                await content.CopyToAsync(fileStream);
            }

            return storedFileName;
        }

        public string GetPhysicalPath(int policyId, string storedFileName)
            => Path.Combine(GetPolicyFolder(policyId), storedFileName);

        public void Delete(int policyId, string storedFileName)
        {
            var path = GetPhysicalPath(policyId, storedFileName);
            if (File.Exists(path))
                File.Delete(path);
        }

        public void DeletePolicyFolder(int policyId)
        {
            var folder = GetPolicyFolder(policyId);
            if (Directory.Exists(folder))
                Directory.Delete(folder, recursive: true);
        }

        private string GetPolicyFolder(int policyId)
            => Path.Combine(_basePath, policyId.ToString());
    }
}
