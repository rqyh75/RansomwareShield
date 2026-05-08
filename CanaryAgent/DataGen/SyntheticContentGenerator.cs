using Bogus;
using System.Text;

namespace CanaryAgent.DataGen
{
    /// <summary>
    /// Synthetic data generator using Bogus for realistic, varied content.
    /// The seed is based on the current tick count so each process launch
    /// produces a different sequence of file content.
    /// </summary>
    public class SyntheticContentGenerator : IContentGenerator
    {
        private readonly Faker _faker;
        private readonly Random _random;

        public SyntheticContentGenerator()
        {
            // Use a time-based seed so content differs across runs on the same machine.
            // Previously the seed was derived from MachineName+UserName, which meant
            // every run produced identical pseudo-random sequences and identical file hashes.
            int seed = Environment.TickCount ^ (int)DateTime.UtcNow.Ticks;
            _random = new Random(seed);
            _faker  = new Faker { Random = new Randomizer(seed) };
        }

        #region HR / Payroll

        public string GenerateEmployeeRoster()
        {
            var sb = new StringBuilder();
            var headers = ChooseHeaders(new[]
            {
                "EmployeeID,FirstName,LastName,Department,Title,Email,Phone,HireDate,Status",
                "EmpNo,FullName,Dept,Position,ContactEmail,Mobile,StartDate,Active",
                "ID,Name,Division,Role,EmailAddress,PhoneNumber,JoinDate,EmploymentStatus"
            });
            sb.AppendLine(headers);

            int rowCount = _random.Next(8, 25);
            for (int i = 0; i < rowCount; i++)
            {
                var person = _faker.Person;
                sb.AppendLine(
                    $"EMP{_random.Next(1000, 9999)},{person.FirstName},{person.LastName}," +
                    $"{_faker.Commerce.Department()},{_faker.Name.JobTitle()},{person.Email}," +
                    $"{_faker.Phone.PhoneNumber()}," +
                    $"{_faker.Date.Between(DateTime.Now.AddYears(-10), DateTime.Now.AddMonths(-1)):yyyy-MM-dd}," +
                    $"{_faker.PickRandom("Active", "Active", "Active", "On Leave", "Remote")}");
            }
            return sb.ToString();
        }

        public string GeneratePayrollData()
        {
            var sb = new StringBuilder();
            sb.AppendLine("PayPeriod,EmployeeID,Name,BaseSalary,Bonus,Deductions,NetPay,PaymentDate");

            string payPeriod = _faker.Date.Recent(30).ToString("yyyy-MM");
            int rowCount = _random.Next(10, 30);

            for (int i = 0; i < rowCount; i++)
            {
                decimal baseSalary = _random.Next(45, 150) * 1000;
                decimal bonus      = _random.Next(0, 10) > 7 ? _random.Next(1, 10) * 1000m : 0;
                decimal deductions = baseSalary * 0.28m + _random.Next(-500, 500);
                decimal netPay     = baseSalary + bonus - deductions;

                sb.AppendLine(
                    $"{payPeriod},EMP{_random.Next(1000, 9999)},{_faker.Name.FullName()}," +
                    $"{baseSalary},{bonus},{deductions:F2},{netPay:F2}," +
                    $"{_faker.Date.Recent(10):yyyy-MM-dd}");
            }
            return sb.ToString();
        }

        public string GenerateBenefitsData()
        {
            var sb = new StringBuilder();
            sb.AppendLine("EmployeeID,Name,HealthPlan,DentalPlan,Vision,401k_Contribution,FSA_Amount,Effective_Date");

            string[] plans  = { "Premium PPO", "Basic HMO", "Standard PPO", "High Deductible" };
            string[] dental = { "Full Coverage", "Basic", "None" };
            int rowCount = _random.Next(6, 20);

            for (int i = 0; i < rowCount; i++)
                sb.AppendLine(
                    $"EMP{_random.Next(1000, 9999)},{_faker.Name.FullName()}," +
                    $"{_faker.PickRandom(plans)},{_faker.PickRandom(dental)}," +
                    $"{_faker.PickRandom("Yes", "No")},{_random.Next(3, 10)}%," +
                    $"{_random.Next(0, 3000)},{_faker.Date.Past(1):yyyy-MM-dd}");

            return sb.ToString();
        }

        #endregion

        #region Finance

        public string GenerateTransactionLedger()
        {
            var sb = new StringBuilder();
            sb.AppendLine("TransactionID,Date,Account,Description,Debit,Credit,Balance,Category");

            decimal runningBalance = _random.Next(10000, 100000);
            int rowCount = _random.Next(15, 50);

            for (int i = 0; i < rowCount; i++)
            {
                bool    isDebit = _random.Next(2) == 0;
                decimal amount  = _random.Next(100, 10000);
                runningBalance  = isDebit ? runningBalance - amount : runningBalance + amount;

                sb.AppendLine(
                    $"TXN{_random.Next(100000, 999999)},{_faker.Date.Recent(90):yyyy-MM-dd}," +
                    $"{_random.Next(1000, 9999)},{_faker.Commerce.ProductName()}," +
                    $"{(isDebit ? amount : 0)},{(isDebit ? 0 : amount)}," +
                    $"{runningBalance:F2},{_faker.Commerce.Department()}");
            }
            return sb.ToString();
        }

        public string GenerateInvoiceRegister()
        {
            var sb = new StringBuilder();
            sb.AppendLine("InvoiceNo,CustomerID,CustomerName,InvoiceDate,DueDate,Amount,Status,PaymentMethod");

            string[] statuses = { "Paid", "Paid", "Paid", "Pending", "Overdue", "Partial" };
            int rowCount = _random.Next(10, 35);

            for (int i = 0; i < rowCount; i++)
            {
                var invoiceDate = _faker.Date.Recent(60);
                sb.AppendLine(
                    $"INV{_random.Next(10000, 99999)},CUST{_random.Next(1000, 9999)}," +
                    $"{_faker.Company.CompanyName()},{invoiceDate:yyyy-MM-dd}," +
                    $"{invoiceDate.AddDays(30):yyyy-MM-dd},{_random.Next(500, 50000)}," +
                    $"{_faker.PickRandom(statuses)},{_faker.PickRandom("Wire", "Check", "ACH", "Credit Card")}");
            }
            return sb.ToString();
        }

        public string GenerateExpenseReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("ExpenseID,EmployeeID,Date,Category,Vendor,Amount,Receipt,Status");

            string[] categories = { "Travel", "Meals", "Software", "Hardware", "Training", "Marketing", "Office Supplies" };
            int rowCount = _random.Next(8, 25);

            for (int i = 0; i < rowCount; i++)
                sb.AppendLine(
                    $"EXP{_random.Next(10000, 99999)},EMP{_random.Next(1000, 9999)}," +
                    $"{_faker.Date.Recent(30):yyyy-MM-dd},{_faker.PickRandom(categories)}," +
                    $"{_faker.Company.CompanyName()},{_random.Next(20, 2000)}," +
                    $"{_faker.PickRandom("Yes", "No", "Pending")},{_faker.PickRandom("Approved", "Pending", "Submitted")}");

            return sb.ToString();
        }

        #endregion

        #region IT / Credentials

        public string GenerateCredentialVault()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== SYSTEM CREDENTIALS ===");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"Machine: {Environment.MachineName}");
            sb.AppendLine("========================================\n");

            string[] systems = {
                "AWS Console", "Azure Portal", "GitHub Enterprise", "Database Admin",
                "VPN Gateway", "Jenkins CI", "Docker Registry", "Kubernetes Dashboard"
            };

            foreach (var system in systems.Take(_random.Next(3, 8)))
            {
                sb.AppendLine($"System: {system}");
                sb.AppendLine($"Username: {_faker.Internet.UserName()}");
                sb.AppendLine($"Password: {GenerateRealisticPassword()}");
                sb.AppendLine($"Last Updated: {_faker.Date.Recent(90):yyyy-MM-dd}");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public string GenerateAPIKeys()
        {
            var sb = new StringBuilder();
            sb.AppendLine("=== API KEYS & SECRETS ===");
            sb.AppendLine($"Environment: {_faker.PickRandom("Production", "Staging", "Development")}");
            sb.AppendLine("========================================\n");

            string[] services = {
                "Stripe", "AWS S3", "SendGrid", "Twilio", "Google Maps",
                "OpenAI", "Slack", "GitHub", "DataDog"
            };

            foreach (var service in services.Take(_random.Next(4, 9)))
            {
                sb.AppendLine($"Service: {service}");
                sb.AppendLine($"API Key: {GenerateAPIKey()}");
                sb.AppendLine($"Secret: {GenerateAPISecret()}");
                sb.AppendLine($"Created: {_faker.Date.Past(2):yyyy-MM-dd}");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        public string GenerateServerInventory()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Hostname,IPAddress,OS,CPU_Cores,RAM_GB,Disk_TB,Role,Status,LastPatched");

            string[] oses  = { "Ubuntu 22.04", "CentOS 8", "Windows Server 2022", "RHEL 8" };
            string[] roles = { "Web Server", "Database", "Load Balancer", "Application", "Backup" };
            int rowCount = _random.Next(5, 15);

            for (int i = 0; i < rowCount; i++)
                sb.AppendLine(
                    $"srv-{_faker.Hacker.Noun()}-{_random.Next(10, 99)}," +
                    $"{_faker.Internet.IpAddress()},{_faker.PickRandom(oses)}," +
                    $"{_random.Next(4, 64)},{_random.Next(8, 256)},{_random.Next(1, 10)}," +
                    $"{_faker.PickRandom(roles)},{_faker.PickRandom("Running", "Running", "Maintenance", "Offline")}," +
                    $"{_faker.Date.Recent(180):yyyy-MM-dd}");

            return sb.ToString();
        }

        #endregion

        #region Sales / CRM

        public string GenerateSalesLeads()
        {
            var sb = new StringBuilder();
            sb.AppendLine("LeadID,CompanyName,ContactName,Email,Phone,Industry,LeadSource,Status,EstimatedValue,CreatedDate");

            string[] sources  = { "Website", "Referral", "Trade Show", "Cold Call", "LinkedIn", "Partner" };
            string[] statuses = { "New", "Contacted", "Qualified", "Proposal", "Negotiation", "Closed Won", "Closed Lost" };
            int rowCount = _random.Next(10, 40);

            for (int i = 0; i < rowCount; i++)
            {
                var person = _faker.Person;
                sb.AppendLine(
                    $"LEAD{_random.Next(10000, 99999)},{_faker.Company.CompanyName()},{person.FullName}," +
                    $"{person.Email},{_faker.Phone.PhoneNumber()},{_faker.Commerce.Department()}," +
                    $"{_faker.PickRandom(sources)},{_faker.PickRandom(statuses)}," +
                    $"{_random.Next(10, 500)}K,{_faker.Date.Recent(180):yyyy-MM-dd}");
            }
            return sb.ToString();
        }

        public string GenerateCustomerDatabase()
        {
            var sb = new StringBuilder();
            sb.AppendLine("CustomerID,CompanyName,Industry,ContactName,Email,Phone,Address,City,State,ZIP,Revenue,AccountManager,Status");

            int rowCount = _random.Next(12, 35);
            for (int i = 0; i < rowCount; i++)
            {
                var person  = _faker.Person;
                var address = _faker.Address;
                sb.AppendLine(
                    $"CUST{_random.Next(1000, 9999)},{_faker.Company.CompanyName()}," +
                    $"{_faker.Commerce.Department()},{person.FullName},{person.Email}," +
                    $"{_faker.Phone.PhoneNumber()},{address.StreetAddress()},{address.City()}," +
                    $"{address.StateAbbr()},{address.ZipCode()},{_random.Next(50, 5000)}K," +
                    $"{_faker.Name.FullName()},{_faker.PickRandom("Active", "Active", "Inactive", "On Hold")}");
            }
            return sb.ToString();
        }

        #endregion

        #region Operations

        public string GenerateInventoryReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine("SKU,ProductName,Category,Quantity,UnitPrice,TotalValue,Location,Supplier,LastRestocked");

            int rowCount = _random.Next(15, 50);
            for (int i = 0; i < rowCount; i++)
            {
                int qty   = _random.Next(0, 500);
                int price = _random.Next(10, 1000);
                sb.AppendLine(
                    $"SKU{_random.Next(10000, 99999)},{_faker.Commerce.ProductName()}," +
                    $"{_faker.Commerce.Department()},{qty},{price},{qty * price}," +
                    $"Warehouse-{_faker.PickRandom("A", "B", "C")}," +
                    $"{_faker.Company.CompanyName()},{_faker.Date.Recent(90):yyyy-MM-dd}");
            }
            return sb.ToString();
        }

        #endregion

        #region Legal

        public string GenerateContractSummary()
        {
            var sb = new StringBuilder();
            sb.AppendLine("CONTRACT REGISTER");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd}");
            sb.AppendLine("========================================\n");

            int count = _random.Next(3, 8);
            for (int i = 0; i < count; i++)
            {
                var startDate = _faker.Date.Past(2);
                int term      = _random.Next(12, 36);
                sb.AppendLine($"Contract ID: CTR-{_random.Next(10000, 99999)}");
                sb.AppendLine($"Vendor: {_faker.Company.CompanyName()}");
                sb.AppendLine($"Type: {_faker.PickRandom("Software License", "Service Agreement", "Consulting", "Support")}");
                sb.AppendLine($"Start Date: {startDate:yyyy-MM-dd}");
                sb.AppendLine($"Term: {term} months");
                sb.AppendLine($"Annual Value: ${_random.Next(10, 500)}K");
                sb.AppendLine($"Status: {_faker.PickRandom("Active", "Renewal Pending", "Expired")}");
                sb.AppendLine();
            }
            return sb.ToString();
        }

        #endregion

        #region Executive

        public string GenerateFinancialSummary()
        {
            var sb = new StringBuilder();
            int quarter = (DateTime.Now.Month - 1) / 3 + 1;
            sb.AppendLine($"EXECUTIVE FINANCIAL SUMMARY - Q{quarter} {DateTime.Now.Year}");
            sb.AppendLine($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}");
            sb.AppendLine("========================================\n");

            int revenue  = _random.Next(800, 5000);
            int expenses = (int)(revenue * 0.65 + _random.Next(-200, 200));
            int profit   = revenue - expenses;

            sb.AppendLine("REVENUE:");
            sb.AppendLine($"  Product Sales:        ${_random.Next(300, 2000)}K");
            sb.AppendLine($"  Services:             ${_random.Next(200, 1500)}K");
            sb.AppendLine($"  Subscriptions:        ${_random.Next(100, 800)}K");
            sb.AppendLine($"  Other:                ${_random.Next(50, 300)}K");
            sb.AppendLine($"  TOTAL REVENUE:        ${revenue}K\n");

            sb.AppendLine("EXPENSES:");
            sb.AppendLine($"  Payroll & Benefits:   ${_random.Next(200, 1500)}K");
            sb.AppendLine($"  Infrastructure:       ${_random.Next(50, 400)}K");
            sb.AppendLine($"  Marketing & Sales:    ${_random.Next(100, 600)}K");
            sb.AppendLine($"  R&D:                  ${_random.Next(50, 500)}K");
            sb.AppendLine($"  Operations:           ${_random.Next(50, 300)}K");
            sb.AppendLine($"  TOTAL EXPENSES:       ${expenses}K\n");

            sb.AppendLine($"NET PROFIT:             ${profit}K");
            sb.AppendLine($"Profit Margin:          {((double)profit / revenue * 100):F1}%");
            return sb.ToString();
        }

        #endregion

        #region System

        public string GenerateAccessLog()
        {
            var sb = new StringBuilder();
            sb.AppendLine("Timestamp,UserID,Username,IPAddress,Action,Resource,Status,ResponseTime");

            string[] actions   = { "LOGIN", "LOGOUT", "READ", "WRITE", "DELETE", "UPDATE", "DOWNLOAD", "UPLOAD" };
            string[] resources = { "/api/users", "/api/data", "/dashboard", "/admin", "/files", "/reports" };
            string[] statuses  = { "200 OK", "200 OK", "200 OK", "401 Unauthorized", "403 Forbidden", "404 Not Found", "500 Error" };
            int rowCount = _random.Next(20, 100);

            for (int i = 0; i < rowCount; i++)
                sb.AppendLine(
                    $"{_faker.Date.Recent(7):yyyy-MM-dd HH:mm:ss},USR{_random.Next(1000, 9999)}," +
                    $"{_faker.Internet.UserName()},{_faker.Internet.IpAddress()}," +
                    $"{_faker.PickRandom(actions)},{_faker.PickRandom(resources)}," +
                    $"{_faker.PickRandom(statuses)},{_random.Next(10, 3000)}ms");

            return sb.ToString();
        }

        #endregion

        #region Mutation / Appending

        public string AppendToContent(string existingContent, PersonaType personaType) =>
            personaType switch
            {
                PersonaType.HR_Payroll           => AppendEmployeeRow(existingContent),
                PersonaType.Finance_Accounting   => AppendFinanceRow(existingContent),
                PersonaType.IT_Credentials       => AppendCredential(existingContent),
                PersonaType.Sales_CRM            => AppendSalesRow(existingContent),
                PersonaType.Operations_Inventory => AppendInventoryRow(existingContent),
                PersonaType.System_Logs          => AppendLogEntries(existingContent),
                _                                => existingContent + $"\n[Updated: {DateTime.Now:yyyy-MM-dd}]\n"
            };

        private string AppendEmployeeRow(string content)
        {
            var person = _faker.Person;
            return content +
                $"EMP{_random.Next(1000, 9999)},{person.FirstName},{person.LastName}," +
                $"{_faker.Commerce.Department()},{_faker.Name.JobTitle()},{person.Email}," +
                $"{_faker.Phone.PhoneNumber()},{_faker.Date.Recent(30):yyyy-MM-dd},Active\n";
        }

        private string AppendFinanceRow(string content) =>
            content +
            $"TXN{_random.Next(100000, 999999)},{DateTime.Now:yyyy-MM-dd},{_random.Next(1000, 9999)}," +
            $"{_faker.Commerce.ProductName()},{_random.Next(100, 5000)},0,{_random.Next(10000, 100000):F2}," +
            $"{_faker.Commerce.Department()}\n";

        private string AppendCredential(string content)
        {
            var sb = new StringBuilder(content);
            sb.AppendLine($"\nSystem: {_faker.Hacker.Verb()} {_faker.Hacker.Noun()}");
            sb.AppendLine($"Username: {_faker.Internet.UserName()}");
            sb.AppendLine($"Password: {GenerateRealisticPassword()}");
            sb.AppendLine($"Added: {DateTime.Now:yyyy-MM-dd}");
            return sb.ToString();
        }

        private string AppendSalesRow(string content)
        {
            var person = _faker.Person;
            return content +
                $"LEAD{_random.Next(10000, 99999)},{_faker.Company.CompanyName()},{person.FullName}," +
                $"{person.Email},{_faker.Phone.PhoneNumber()},{_faker.Commerce.Department()}," +
                $"Website,New,{_random.Next(10, 500)}K,{DateTime.Now:yyyy-MM-dd}\n";
        }

        private string AppendInventoryRow(string content)
        {
            int qty   = _random.Next(0, 500);
            int price = _random.Next(10, 1000);
            return content +
                $"SKU{_random.Next(10000, 99999)},{_faker.Commerce.ProductName()}," +
                $"{_faker.Commerce.Department()},{qty},{price},{qty * price}," +
                $"Warehouse-A,{_faker.Company.CompanyName()},{DateTime.Now:yyyy-MM-dd}\n";
        }

        private string AppendLogEntries(string content)
        {
            var sb      = new StringBuilder(content);
            int entries = _random.Next(3, 10);
            string[] actions   = { "LOGIN", "READ", "WRITE", "DOWNLOAD" };
            string[] resources = { "/api/users", "/api/data", "/dashboard" };

            for (int i = 0; i < entries; i++)
                sb.AppendLine(
                    $"{DateTime.Now.AddMinutes(-_random.Next(0, 60)):yyyy-MM-dd HH:mm:ss}," +
                    $"USR{_random.Next(1000, 9999)},{_faker.Internet.UserName()}," +
                    $"{_faker.Internet.IpAddress()},{_faker.PickRandom(actions)}," +
                    $"{_faker.PickRandom(resources)},200 OK,{_random.Next(10, 500)}ms");

            return sb.ToString();
        }

        #endregion

        #region Helpers

        private string ChooseHeaders(string[] options) => options[_random.Next(options.Length)];

        private string GenerateRealisticPassword()
        {
            string adj  = _faker.Hacker.Adjective();
            string part = char.ToUpper(adj[0]) + adj[1..];
            return $"{part}{_faker.Random.Number(10, 999)}{_faker.PickRandom("!", "@", "#", "$", "%", "&", "*")}";
        }

        private string GenerateAPIKey() =>
            $"{_faker.Random.AlphaNumeric(8)}-{_faker.Random.AlphaNumeric(16)}-{_faker.Random.AlphaNumeric(8)}";

        private string GenerateAPISecret() => _faker.Random.AlphaNumeric(40);

        #endregion
    }
}
