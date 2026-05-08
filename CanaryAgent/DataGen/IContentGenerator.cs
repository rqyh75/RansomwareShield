namespace CanaryAgent.DataGen
{
    /// <summary>
    /// Interface for content generators. Allows easy swapping/extending of generation strategies.
    /// </summary>
    public interface IContentGenerator
    {
        // HR / Payroll
        string GenerateEmployeeRoster();
        string GeneratePayrollData();
        string GenerateBenefitsData();

        // Finance
        string GenerateTransactionLedger();
        string GenerateInvoiceRegister();
        string GenerateExpenseReport();

        // IT / Credentials
        string GenerateCredentialVault();
        string GenerateAPIKeys();
        string GenerateServerInventory();

        // Sales / CRM
        string GenerateSalesLeads();
        string GenerateCustomerDatabase();

        // Operations
        string GenerateInventoryReport();

        // Legal
        string GenerateContractSummary();

        // Executive
        string GenerateFinancialSummary();

        // System
        string GenerateAccessLog();

        // Mutation
        string AppendToContent(string existingContent, PersonaType personaType);
    }
}