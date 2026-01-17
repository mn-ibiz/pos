namespace HospitalityPOS.Core.Enums;

/// <summary>
/// M-Pesa transaction type.
/// </summary>
public enum MpesaTransactionType
{
    CustomerPayBillOnline = 0,    // PayBill
    CustomerBuyGoodsOnline = 1,   // Till/Buy Goods
    StkPush = 2                   // Lipa Na M-Pesa Online
}

/// <summary>
/// M-Pesa STK push request status.
/// </summary>
public enum MpesaStkStatus
{
    Pending = 0,
    Processing = 1,
    Success = 2,
    Cancelled = 3,
    Failed = 4,
    Timeout = 5
}

/// <summary>
/// M-Pesa transaction status.
/// </summary>
public enum MpesaTransactionStatus
{
    Pending = 0,
    Completed = 1,
    Failed = 2,
    Reversed = 3,
    Cancelled = 4
}

/// <summary>
/// M-Pesa environment type.
/// </summary>
public enum MpesaEnvironment
{
    Sandbox = 0,
    Production = 1
}
