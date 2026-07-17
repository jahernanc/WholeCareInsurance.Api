namespace WholeCareInsurance.api.Utils
{
    public static class AgentFieldValidation
    {
        // Espeja la lógica condicional del formulario de Agentes.jsx (Licensed -> LicenseNumber,
        // HasCompanyContract -> ContractNumber/CompanyName) para que un POST/PUT directo a la API
        // (sin pasar por la UI) no pueda dejar un agente "licenciado" sin número de licencia.
        public static string? Validate(bool licensed, string? licenseNumber, bool hasCompanyContract, string? contractNumber, string? companyName)
        {
            if (licensed && string.IsNullOrWhiteSpace(licenseNumber))
                return "Hay que indicar el número de licencia si el agente está licenciado.";

            if (hasCompanyContract && (string.IsNullOrWhiteSpace(contractNumber) || string.IsNullOrWhiteSpace(companyName)))
                return "Hay que indicar el número de contrato y el nombre de la compañía si el agente tiene contrato.";

            return null;
        }
    }
}
