namespace WholeCareInsurance.api.Services
{
    public interface IPolicyDocumentStorage
    {
        Task<string> SaveAsync(int policyId, Stream content, string extension);
        string GetPhysicalPath(int policyId, string storedFileName);
        void Delete(int policyId, string storedFileName);
        void DeletePolicyFolder(int policyId);
    }
}
