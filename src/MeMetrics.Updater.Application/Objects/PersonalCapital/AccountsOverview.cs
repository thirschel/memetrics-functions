using Newtonsoft.Json;

namespace MeMetrics.Updater.Application.Objects.PersonalCapital
{
    public class AccountsOverview
    {
        [JsonProperty("spHeader")]
        public SpHeader SpHeader { get; set; }

        [JsonProperty("spData")]
        public AccountsData SpData { get; set; }
    }

    public class AccountsData
    {
        [JsonProperty("creditCardAccountsTotal")]
        public double CreditCardAccountsTotal { get; set; }

        [JsonProperty("assets")]
        public double Assets { get; set; }

        [JsonProperty("otherLiabilitiesAccountsTotal")]
        public long OtherLiabilitiesAccountsTotal { get; set; }

        [JsonProperty("cashAccountsTotal")]
        public double CashAccountsTotal { get; set; }

        [JsonProperty("liabilities")]
        public double Liabilities { get; set; }

        [JsonProperty("networth")]
        public double Networth { get; set; }

        [JsonProperty("investmentAccountsTotal")]
        public double InvestmentAccountsTotal { get; set; }

        [JsonProperty("mortgageAccountsTotal")]
        public long MortgageAccountsTotal { get; set; }

        [JsonProperty("loanAccountsTotal")]
        public long LoanAccountsTotal { get; set; }

        [JsonProperty("accounts")]
        public Account[] Accounts { get; set; }

        [JsonProperty("otherAssetAccountsTotal")]
        public long OtherAssetAccountsTotal { get; set; }
    }

    public partial class Account
    {
        [JsonProperty("availableCash", NullValueHandling = NullValueHandling.Ignore)]
        public string AvailableCash { get; set; }

        [JsonProperty("isOnUs")]
        public bool IsOnUs { get; set; }

        [JsonProperty("userProductId")]
        public long UserProductId { get; set; }

        [JsonProperty("contactInfo", NullValueHandling = NullValueHandling.Ignore)]
        public ContactInfo ContactInfo { get; set; }

        [JsonProperty("isHome")]
        public bool IsHome { get; set; }

        [JsonProperty("nextAction")]
        public NextAction NextAction { get; set; }

        [JsonProperty("isIAVEligible")]
        public bool IsIavEligible { get; set; }

        [JsonProperty("loginFields")]
        public LoginField[] LoginFields { get; set; }

        [JsonProperty("enrollmentConciergeRequested")]
        public long EnrollmentConciergeRequested { get; set; }

        [JsonProperty("isCrypto")]
        public bool IsCrypto { get; set; }

        [JsonProperty("isPartner")]
        public bool IsPartner { get; set; }

        [JsonProperty("priorBalance", NullValueHandling = NullValueHandling.Ignore)]
        public double? PriorBalance { get; set; }

        [JsonProperty("isCustomManual")]
        public bool IsCustomManual { get; set; }

        [JsonProperty("originalName")]
        public string OriginalName { get; set; }

        [JsonProperty("isIAVAccountNumberValid")]
        public bool IsIavAccountNumberValid { get; set; }

        [JsonProperty("isExcludeFromHousehold")]
        public bool IsExcludeFromHousehold { get; set; }

        [JsonProperty("isAsset")]
        public bool IsAsset { get; set; }

        [JsonProperty("aggregating")]
        public bool Aggregating { get; set; }

        [JsonProperty("balance")]
        public double Balance { get; set; }

        [JsonProperty("isStatementDownloadEligible")]
        public bool IsStatementDownloadEligible { get; set; }

        [JsonProperty("is401KEligible")]
        public bool Is401KEligible { get; set; }

        [JsonProperty("isAccountUsedInFunding")]
        public bool IsAccountUsedInFunding { get; set; }

        [JsonProperty("isOnUs401K")]
        public bool IsOnUs401K { get; set; }

        [JsonProperty("advisoryFeePercentage")]
        public long AdvisoryFeePercentage { get; set; }

        [JsonProperty("lastRefreshed")]
        public long LastRefreshed { get; set; }

        [JsonProperty("productId")]
        public long ProductId { get; set; }

        [JsonProperty("userSiteId")]
        public long UserSiteId { get; set; }

        [JsonProperty("isManual")]
        public bool IsManual { get; set; }

        [JsonProperty("logoPath", NullValueHandling = NullValueHandling.Ignore)]
        public string LogoPath { get; set; }

        [JsonProperty("currentBalance")]
        public double CurrentBalance { get; set; }

        [JsonProperty("accountType")]
        public string AccountType { get; set; }

        [JsonProperty("paymentFromStatus")]
        public bool PaymentFromStatus { get; set; }

        [JsonProperty("isRefetchTransactionEligible")]
        public bool IsRefetchTransactionEligible { get; set; }

        [JsonProperty("accountId")]
        public string AccountId { get; set; }

        [JsonProperty("homeUrl")]
        public string HomeUrl { get; set; }

        [JsonProperty("isManualPortfolio")]
        public bool IsManualPortfolio { get; set; }

        [JsonProperty("excludeFromProposal", NullValueHandling = NullValueHandling.Ignore)]
        public bool? ExcludeFromProposal { get; set; }

        [JsonProperty("userAccountId")]
        public string UserAccountId { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("firmName")]
        public string FirmName { get; set; }

        [JsonProperty("accountTypeGroup")]
        public string AccountTypeGroup { get; set; }

        [JsonProperty("paymentToStatus")]
        public bool PaymentToStatus { get; set; }

        [JsonProperty("isSelectedForTransfer")]
        public bool IsSelectedForTransfer { get; set; }

        [JsonProperty("isPaymentToCapable")]
        public bool IsPaymentToCapable { get; set; }

        [JsonProperty("closedComment", NullValueHandling = NullValueHandling.Ignore)]
        public string ClosedComment { get; set; }

        [JsonProperty("loginUrl")]
        public string LoginUrl { get; set; }

        [JsonProperty("isTaxDeferredOrNonTaxable")]
        public bool IsTaxDeferredOrNonTaxable { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("fundFees", NullValueHandling = NullValueHandling.Ignore)]
        public double? FundFees { get; set; }

        [JsonProperty("productType")]
        public string ProductType { get; set; }

        [JsonProperty("isAccountNumberValidated")]
        public bool IsAccountNumberValidated { get; set; }

        [JsonProperty("accountTypeNew", NullValueHandling = NullValueHandling.Ignore)]
        public string AccountTypeNew { get; set; }

        [JsonProperty("isLiability")]
        public bool IsLiability { get; set; }

        [JsonProperty("defaultAdvisoryFee", NullValueHandling = NullValueHandling.Ignore)]
        public double? DefaultAdvisoryFee { get; set; }

        [JsonProperty("isAdvised", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsAdvised { get; set; }

        [JsonProperty("feesPerYear", NullValueHandling = NullValueHandling.Ignore)]
        public string FeesPerYear { get; set; }

        [JsonProperty("isEsog")]
        public bool IsEsog { get; set; }

        [JsonProperty("createdDate")]
        public long CreatedDate { get; set; }

        [JsonProperty("closedDate")]
        public string ClosedDate { get; set; }

        [JsonProperty("totalFee", NullValueHandling = NullValueHandling.Ignore)]
        public double? TotalFee { get; set; }

        [JsonProperty("isPaymentFromCapable")]
        public bool IsPaymentFromCapable { get; set; }

        [JsonProperty("siteId")]
        public long SiteId { get; set; }

        [JsonProperty("originalFirmName")]
        public string OriginalFirmName { get; set; }

        [JsonProperty("dueDate", NullValueHandling = NullValueHandling.Ignore)]
        public long? DueDate { get; set; }

        [JsonProperty("memo", NullValueHandling = NullValueHandling.Ignore)]
        public string Memo { get; set; }

        [JsonProperty("type", NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }

        [JsonProperty("accountHolder", NullValueHandling = NullValueHandling.Ignore)]
        public string AccountHolder { get; set; }

        [JsonProperty("apr", NullValueHandling = NullValueHandling.Ignore)]
        public double? Apr { get; set; }

        [JsonProperty("availableCredit", NullValueHandling = NullValueHandling.Ignore)]
        public double? AvailableCredit { get; set; }

        [JsonProperty("accountName", NullValueHandling = NullValueHandling.Ignore)]
        public string AccountName { get; set; }

        [JsonProperty("link", NullValueHandling = NullValueHandling.Ignore)]
        public string Link { get; set; }

        [JsonProperty("lastPaymentDate", NullValueHandling = NullValueHandling.Ignore)]
        public long? LastPaymentDate { get; set; }

        [JsonProperty("lastPaymentAmount", NullValueHandling = NullValueHandling.Ignore)]
        public string LastPaymentAmount { get; set; }

        [JsonProperty("minPaymentDue", NullValueHandling = NullValueHandling.Ignore)]
        public string MinPaymentDue { get; set; }

        [JsonProperty("creditUtilization", NullValueHandling = NullValueHandling.Ignore)]
        public double? CreditUtilization { get; set; }

        [JsonProperty("amountDue", NullValueHandling = NullValueHandling.Ignore)]
        public string AmountDue { get; set; }

        [JsonProperty("runningBalance", NullValueHandling = NullValueHandling.Ignore)]
        public double? RunningBalance { get; set; }

        [JsonProperty("accountProperties", NullValueHandling = NullValueHandling.Ignore)]
        public long[] AccountProperties { get; set; }

        [JsonProperty("iavLastRefreshedDate", NullValueHandling = NullValueHandling.Ignore)]
        public string IavLastRefreshedDate { get; set; }

        [JsonProperty("iavStatus", NullValueHandling = NullValueHandling.Ignore)]
        public string IavStatus { get; set; }

        [JsonProperty("treatedAsInvestment", NullValueHandling = NullValueHandling.Ignore)]
        public bool? TreatedAsInvestment { get; set; }

        [JsonProperty("availableBalance", NullValueHandling = NullValueHandling.Ignore)]
        public double? AvailableBalance { get; set; }

        [JsonProperty("mfaType", NullValueHandling = NullValueHandling.Ignore)]
        public string MfaType { get; set; }

        [JsonProperty("routingNumberSource", NullValueHandling = NullValueHandling.Ignore)]
        public string RoutingNumberSource { get; set; }

        [JsonProperty("isRoutingNumberValidated", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsRoutingNumberValidated { get; set; }

        [JsonProperty("routingNumber", NullValueHandling = NullValueHandling.Ignore)]
        public string RoutingNumber { get; set; }

        [JsonProperty("creditLimit", NullValueHandling = NullValueHandling.Ignore)]
        public long? CreditLimit { get; set; }

        [JsonProperty("aggregationError", NullValueHandling = NullValueHandling.Ignore)]
        public AggregationError AggregationError { get; set; }

        [JsonProperty("disbursementType", NullValueHandling = NullValueHandling.Ignore)]
        public string DisbursementType { get; set; }

        [JsonProperty("accountTypeSubtype", NullValueHandling = NullValueHandling.Ignore)]
        public string AccountTypeSubtype { get; set; }
    }

    public partial class AggregationError
    {
        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("tags")]
        public string[] Tags { get; set; }
    }

    public partial class ContactInfo
    {
        [JsonProperty("url")]
        public string Url { get; set; }
    }

    public partial class LoginField
    {
        [JsonProperty("isUsername", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsUsername { get; set; }

        [JsonProperty("isRequired")]
        public bool IsRequired { get; set; }

        [JsonProperty("hint")]
        public string Hint { get; set; }

        [JsonProperty("parts")]
        public LoginFieldPart[] Parts { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("isPassword", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsPassword { get; set; }
    }

    public partial class LoginFieldPart
    {
        [JsonProperty("size", NullValueHandling = NullValueHandling.Ignore)]
        public long? Size { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("maxLength")]
        public long MaxLength { get; set; }

        [JsonProperty("mask")]
        public string Mask { get; set; }

        [JsonProperty("sizge", NullValueHandling = NullValueHandling.Ignore)]
        public long? Sizge { get; set; }
    }

    public partial class NextAction
    {
        [JsonProperty("nextActionMessage")]
        public string NextActionMessage { get; set; }

        [JsonProperty("iconType")]
        public string IconType { get; set; }

        [JsonProperty("action")]
        public string Action { get; set; }

        [JsonProperty("reportAction")]
        public string ReportAction { get; set; }

        [JsonProperty("statusMessage")]
        public string StatusMessage { get; set; }

        [JsonProperty("prompts")]
        public Prompt[] Prompts { get; set; }

        [JsonProperty("aggregationErrorType")]
        public string AggregationErrorType { get; set; }

        [JsonProperty("instructions", NullValueHandling = NullValueHandling.Ignore)]
        public string Instructions { get; set; }

        [JsonProperty("supportedAccountTypes", NullValueHandling = NullValueHandling.Ignore)]
        public string[] SupportedAccountTypes { get; set; }
    }

    public partial class Prompt
    {
        [JsonProperty("isRequired")]
        public bool IsRequired { get; set; }

        [JsonProperty("isUsername", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsUsername { get; set; }

        [JsonProperty("semantic")]
        public string Semantic { get; set; }

        [JsonProperty("parts")]
        public PromptPart[] Parts { get; set; }

        [JsonProperty("label")]
        public string Label { get; set; }

        [JsonProperty("isPassword", NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsPassword { get; set; }
    }

    public partial class PromptPart
    {
        [JsonProperty("characterSet")]
        public string CharacterSet { get; set; }

        [JsonProperty("placeholderValue")]
        public string PlaceholderValue { get; set; }

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; }

        [JsonProperty("maxLength")]
        public long MaxLength { get; set; }
    }

}
