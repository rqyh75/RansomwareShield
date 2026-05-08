namespace CanaryAgent.DataGen
{
    public class FilePersona
    {
        public string? PersonaId { get; set; }
        public string? LogicalName { get; set; }
        public string? FileNamePattern { get; set; }
        public string? FileExtension { get; set; }
        public PersonaType Type { get; set; }
        public Func<IContentGenerator, string>? GenerateContent { get; set; }

        public int MinRows { get; set; } = 5;
        public int MaxRows { get; set; } = 20;
        public bool SupportsAppending { get; set; } = true;
        public int AppendMinRows { get; set; } = 1;
        public int AppendMaxRows { get; set; } = 3;
    }

    public enum PersonaType
    {
        HR_Payroll,
        Finance_Accounting,
        IT_Credentials,
        Operations_Inventory,
        Sales_CRM,
        Legal_Contracts,
        Executive_Reports,
        System_Logs
    }

    public static class PersonaRegistry
    {
        // Random.Shared is thread-safe (.NET 6+) and avoids the static-Random race condition.
        private static Random Rng => Random.Shared;

        private static string Pick(params string[] options) =>
            options[Rng.Next(options.Length)];

        public static List<FilePersona> GetAllPersonas()
        {
            return new List<FilePersona>
            {
                // HR / Payroll
                new FilePersona
                {
                    PersonaId = "hr_employee_roster",
                    LogicalName = "Employee Roster",
                    FileNamePattern = Rng.Next(4) switch {
                        0 => "employee_data_{variant}.csv",
                        1 => "staff_roster_{dept}_{year}.csv",
                        2 => "headcount_{location}_{date}.csv",
                        _ => "workforce_directory_{variant}.csv"
                    },
                    FileExtension = ".csv",
                    Type = PersonaType.HR_Payroll,
                    MinRows = 8, MaxRows = 25,
                    GenerateContent = gen => gen.GenerateEmployeeRoster()
                },
                new FilePersona
                {
                    PersonaId = "hr_payroll",
                    LogicalName = "Payroll Records",
                    FileNamePattern = Rng.Next(4) switch {
                        0 => "payroll_{period}.csv",
                        1 => "salary_run_{month}_{year}.csv",
                        2 => "compensation_register_{quarter}.csv",
                        _ => "pay_records_{dept}_{period}.csv"
                    },
                    FileExtension = ".csv",
                    Type = PersonaType.HR_Payroll,
                    MinRows = 10, MaxRows = 30,
                    GenerateContent = gen => gen.GeneratePayrollData()
                },
                new FilePersona
                {
                    PersonaId = "hr_benefits",
                    LogicalName = "Benefits Enrollment",
                    FileNamePattern = Rng.Next(4) switch {
                        0 => "benefits_{year}.csv",
                        1 => "enrollment_data_{year}_{variant}.csv",
                        2 => "benefits_elections_{open_enrollment}.csv",
                        _ => "hr_benefits_summary_{year}.csv"
                    },
                    FileExtension = ".csv",
                    Type = PersonaType.HR_Payroll,
                    MinRows = 6, MaxRows = 20,
                    GenerateContent = gen => gen.GenerateBenefitsData()
                },

                // Finance
                new FilePersona
                {
                    PersonaId = "finance_transactions",
                    LogicalName = "Transaction Ledger",
                    FileNamePattern = Rng.Next(4) switch {
                        0 => "transactions_{month}.csv",
                        1 => "gl_entries_{month}_{year}.csv",
                        2 => "ledger_export_{date}.csv",
                        _ => "txn_log_{account}_{period}.csv"
                    },
                    FileExtension = ".csv",
                    Type = PersonaType.Finance_Accounting,
                    MinRows = 15, MaxRows = 50,
                    GenerateContent = gen => gen.GenerateTransactionLedger()
                },
                new FilePersona
                {
                    PersonaId = "finance_invoices",
                    LogicalName = "Invoice Register",
                    FileNamePattern = Rng.Next(4) switch {
                        0 => "invoices_Q{quarter}.csv",
                        1 => "invoice_register_{month}_{year}.csv",
                        2 => "ap_ar_invoices_{quarter}.csv",
                        _ => "billing_records_Q{quarter}_{year}.csv"
                    },
                    FileExtension = ".csv",
                    Type = PersonaType.Finance_Accounting,
                    MinRows = 10, MaxRows = 35,
                    GenerateContent = gen => gen.GenerateInvoiceRegister()
                },
                new FilePersona
                {
                    PersonaId = "finance_expenses",
                    LogicalName = "Expense Report",
                    FileNamePattern = Rng.Next(4) switch {
                        0 => "expenses_{dept}_{month}.csv",
                        1 => "expense_report_{employee}_{month}.csv",
                        2 => "reimbursements_{dept}_{quarter}.csv",
                        _ => "cost_center_{dept}_{period}.csv"
                    },
                    FileExtension = ".csv",
                    Type = PersonaType.Finance_Accounting,
                    MinRows = 8, MaxRows = 25,
                    GenerateContent = gen => gen.GenerateExpenseReport()
                },

                // IT / Credentials
                new FilePersona
                {
                    PersonaId = "it_credentials",
                    LogicalName = "System Credentials",
                    FileNamePattern = Rng.Next(4) switch {
                        0 => "credentials_{system}.txt",
                        1 => "svc_accounts_{env}.txt",
                        2 => "auth_config_{system}_{env}.txt",
                        _ => "logins_{dept}_{system}.txt"
                    },
                    FileExtension = ".txt",
                    Type = PersonaType.IT_Credentials,
                    MinRows = 3, MaxRows = 8,
                    SupportsAppending = true,
                    GenerateContent = gen => gen.GenerateCredentialVault()
                },
                new FilePersona
                {
                    PersonaId = "it_api_keys",
                    LogicalName = "API Keys",
                    FileNamePattern = Rng.Next(4) switch {
                        0 => "api_keys_{env}.txt",
                        1 => "tokens_{service}_{env}.txt",
                        2 => "secrets_{project}.txt",
                        _ => "integration_keys_{env}_{date}.txt"
                    },
                    FileExtension = ".txt",
                    Type = PersonaType.IT_Credentials,
                    MinRows = 4, MaxRows = 12,
                    GenerateContent = gen => gen.GenerateAPIKeys()
                },
                new FilePersona
                {
                    PersonaId = "it_server_inventory",
                    LogicalName = "Server Inventory",
                    FileNamePattern = Rng.Next(4) switch {
                        0 => "servers_{location}.csv",
                        1 => "infrastructure_inventory_{date}.csv",
                        2 => "asset_register_{location}_{year}.csv",
                        _ => "host_list_{env}_{location}.csv"
                    },
                    FileExtension = ".csv",
                    Type = PersonaType.IT_Credentials,
                    MinRows = 5, MaxRows = 15,
                    GenerateContent = gen => gen.GenerateServerInventory()
                },

                // Sales / CRM
                new FilePersona
                {
                    PersonaId = "sales_leads",
                    LogicalName = "Sales Leads",
                    FileNamePattern = Rng.Next(4) switch {
                        0 => "leads_{quarter}.csv",
                        1 => "prospects_{region}_{quarter}.csv",
                        2 => "pipeline_{owner}_{month}.csv",
                        _ => "crm_leads_export_{date}.csv"
                    },
                    FileExtension = ".csv",
                    Type = PersonaType.Sales_CRM,
                    MinRows = 10, MaxRows = 40,
                    GenerateContent = gen => gen.GenerateSalesLeads()
                },
                new FilePersona
                {
                    PersonaId = "sales_customers",
                    LogicalName = "Customer Database",
                    FileNamePattern = Rng.Next(4) switch {
                        0 => "customers_{region}.csv",
                        1 => "client_list_{region}_{year}.csv",
                        2 => "accounts_{tier}_{region}.csv",
                        _ => "crm_export_{segment}_{date}.csv"
                    },
                    FileExtension = ".csv",
                    Type = PersonaType.Sales_CRM,
                    MinRows = 12, MaxRows = 35,
                    GenerateContent = gen => gen.GenerateCustomerDatabase()
                },

                // Operations
                new FilePersona
                {
                    PersonaId = "ops_inventory",
                    LogicalName = "Inventory Report",
                    FileNamePattern = Rng.Next(4) switch {
                        0 => "inventory_{warehouse}.csv",
                        1 => "stock_levels_{warehouse}_{date}.csv",
                        2 => "sku_report_{category}_{month}.csv",
                        _ => "wms_export_{warehouse}_{variant}.csv"
                    },
                    FileExtension = ".csv",
                    Type = PersonaType.Operations_Inventory,
                    MinRows = 15, MaxRows = 50,
                    GenerateContent = gen => gen.GenerateInventoryReport()
                },

                // Legal
                new FilePersona
                {
                    PersonaId = "legal_contracts",
                    LogicalName = "Contract Register",
                    FileNamePattern = Rng.Next(4) switch {
                        0 => "contracts_{year}.txt",
                        1 => "agreement_log_{type}_{year}.txt",
                        2 => "vendor_contracts_{quarter}.txt",
                        _ => "clm_register_{dept}_{year}.txt"
                    },
                    FileExtension = ".txt",
                    Type = PersonaType.Legal_Contracts,
                    MinRows = 3, MaxRows = 8,
                    GenerateContent = gen => gen.GenerateContractSummary()
                },

                // Executive
                new FilePersona
                {
                    PersonaId = "exec_financials",
                    LogicalName = "Financial Summary",
                    FileNamePattern = Rng.Next(4) switch {
                        0 => "financial_summary_Q{quarter}.txt",
                        1 => "exec_report_Q{quarter}_{year}.txt",
                        2 => "board_pack_{month}_{year}.txt",
                        _ => "kpi_summary_Q{quarter}.txt"
                    },
                    FileExtension = ".txt",
                    Type = PersonaType.Executive_Reports,
                    MinRows = 1, MaxRows = 1,
                    SupportsAppending = true,
                    GenerateContent = gen => gen.GenerateFinancialSummary()
                },

                // System Logs
                new FilePersona
                {
                    PersonaId = "system_access_log",
                    LogicalName = "Access Log",
                    FileNamePattern = Rng.Next(4) switch {
                        0 => "access_log_{date}.txt",
                        1 => "auth_events_{system}_{date}.txt",
                        2 => "audit_trail_{date}_{variant}.txt",
                        _ => "siem_export_{location}_{date}.txt"
                    },
                    FileExtension = ".txt",
                    Type = PersonaType.System_Logs,
                    MinRows = 20, MaxRows = 100,
                    GenerateContent = gen => gen.GenerateAccessLog()
                }
            };
        }

        public static string ApplyFileNamePattern(string pattern)
        {
            var now = DateTime.Now;
            string result = pattern;

            var replacements = new Dictionary<string, Func<string>>
            {
                { "{variant}",   () => Pick("master", "backup", "current", "latest") },
                { "{period}",    () => $"{now:yyyy_MM}" },
                { "{year}",      () => now.Year.ToString() },
                { "{month}",     () => now.ToString("MMMM") },
                { "{quarter}",   () => $"{(now.Month - 1) / 3 + 1}" },
                { "{dept}",      () => Pick("IT", "Sales", "HR", "Finance", "Operations") },
                { "{system}",    () => Pick("prod", "staging", "backup", "erp", "crm") },
                { "{env}",       () => Pick("production", "development", "testing", "staging") },
                { "{location}",  () => Pick("datacenter1", "cloud", "office", "dr-site") },
                { "{region}",    () => Pick("north", "south", "east", "west", "central") },
                { "{warehouse}", () => Pick("A", "B", "C", "main", "secondary") },
                { "{date}",      () => now.ToString("yyyy_MM_dd") },
                { "{open_enrollment}", () => $"{now.Year}_OE" },
                { "{account}",   () => Pick("1001", "1002", "1003") },
                { "{employee}",  () => Pick("jsmith", "abrown", "kliu") },
                { "{owner}",     () => Pick("east_team", "west_team", "central_team") },
                { "{category}",  () => Pick("electronics", "office", "tools") },
                { "{tier}",      () => Pick("enterprise", "smb", "startup") },
                { "{segment}",   () => Pick("active", "churned", "trial") },
                { "{type}",      () => Pick("vendor", "client", "partner") },
                { "{service}",   () => Pick("stripe", "sendgrid", "twilio") },
                { "{project}",   () => Pick("alpha", "beta", "gamma") },
            };

            foreach (var kvp in replacements)
                if (result.Contains(kvp.Key))
                    result = result.Replace(kvp.Key, kvp.Value());

            return result;
        }
    }
}
