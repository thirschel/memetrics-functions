using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MeMetrics.Updater.Application.Objects.PersonalCapital
{
    public class UserTransactions
    {
        [JsonProperty("spHeader")]
        public SpHeader SpHeader { get; set; }

        [JsonProperty("spData")]
        public TransactionData SpData { get; set; }
    }

    public class TransactionData
    {
        [JsonProperty("intervalType")]
        public string IntervalType { get; set; }

        [JsonProperty("endDate")]
        public DateTimeOffset EndDate { get; set; }

        [JsonProperty("moneyIn")]
        public double MoneyIn { get; set; }

        [JsonProperty("transactions")]
        public List<Transaction> Transactions { get; set; }

        [JsonProperty("netCashflow")]
        public double NetCashflow { get; set; }

        [JsonProperty("averageOut")]
        public double AverageOut { get; set; }

        [JsonProperty("moneyOut")]
        public double MoneyOut { get; set; }

        [JsonProperty("startDate")]
        public DateTimeOffset StartDate { get; set; }

        [JsonProperty("averageIn")]
        public double AverageIn { get; set; }
    }

    public partial class Transaction
    {
        [JsonProperty("symbol")]
        public string Symbol { get; set; }

        [JsonProperty("isInterest")]
        public bool IsInterest { get; set; }

        [JsonProperty("netCost")]
        public long NetCost { get; set; }

        [JsonProperty("cusipNumber")]
        public string CusipNumber { get; set; }

        [JsonProperty("accountName")]
        public string AccountName { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("memo")]
        public string Memo { get; set; }

        [JsonProperty("isCredit")]
        public bool IsCredit { get; set; }

        [JsonProperty("isEditable")]
        public bool IsEditable { get; set; }

        [JsonProperty("isCashOut")]
        public bool IsCashOut { get; set; }

        [JsonProperty("merchantId")]
        public string MerchantId { get; set; }

        [JsonProperty("price")]
        public long Price { get; set; }

        [JsonProperty("holdingType")]
        public string HoldingType { get; set; }

        [JsonProperty("lotHandling")]
        public string LotHandling { get; set; }

        [JsonProperty("customReason")]
        public string CustomReason { get; set; }

        [JsonProperty("userTransactionId")]
        public string UserTransactionId { get; set; }

        [JsonProperty("currency")]
        public string Currency { get; set; }

        [JsonProperty("isDuplicate")]
        public bool IsDuplicate { get; set; }

        [JsonProperty("resultType")]
        public string ResultType { get; set; }

        [JsonProperty("originalDescription")]
        public string OriginalDescription { get; set; }

        [JsonProperty("isSpending")]
        public bool IsSpending { get; set; }

        [JsonProperty("amount")]
        public double Amount { get; set; }

        [JsonProperty("checkNumber")]
        public string CheckNumber { get; set; }

        [JsonProperty("transactionTypeId")]
        public long TransactionTypeId { get; set; }

        [JsonProperty("isIncome")]
        public bool IsIncome { get; set; }

        [JsonProperty("includeInCashManager")]
        public bool IncludeInCashManager { get; set; }

        [JsonProperty("merchant")]
        public string Merchant { get; set; }

        [JsonProperty("isNew")]
        public bool IsNew { get; set; }

        [JsonProperty("isCashIn")]
        public bool IsCashIn { get; set; }

        [JsonProperty("transactionDate")]
        public DateTimeOffset TransactionDate { get; set; }

        [JsonProperty("transactionType")]
        public string TransactionType { get; set; }

        [JsonProperty("accountId")]
        public string AccountId { get; set; }

        [JsonProperty("originalAmount")]
        public double OriginalAmount { get; set; }

        [JsonProperty("isCost")]
        public bool IsCost { get; set; }

        [JsonProperty("userAccountId")]
        public long UserAccountId { get; set; }

        [JsonProperty("simpleDescription")]
        public string SimpleDescription { get; set; }

        [JsonProperty("investmentType", NullValueHandling = NullValueHandling.Ignore)]
        public string InvestmentType { get; set; }

        [JsonProperty("runningBalance")]
        public long RunningBalance { get; set; }

        [JsonProperty("hasViewed")]
        public bool HasViewed { get; set; }

        [JsonProperty("categoryId")]
        public int CategoryId { get; set; }

        [JsonProperty("status")]
        public string Status { get; set; }

        [JsonProperty("originalCategoryId", NullValueHandling = NullValueHandling.Ignore)]
        public long? OriginalCategoryId { get; set; }

        [JsonProperty("catKeyword", NullValueHandling = NullValueHandling.Ignore)]
        public string CatKeyword { get; set; }
    }



}
