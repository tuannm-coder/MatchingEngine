namespace Enum;

public enum OrderStatus : byte
{
    Undefined = 0,
    Prepared,
    Listed,
    Matched,
    Filled,
    Cancelled,
    Rejected,
    Reduced,
    Triggered,
    Expired,
}