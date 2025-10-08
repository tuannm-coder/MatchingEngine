namespace Enum;

public enum CancelReason : byte
{
    UserRequested = 1,
    NoLiquidity, //MarketOrderNoLiquidity
    ImmediateOrCancel,
    FillOrKill,
    BookOrCancel,
    ValidityExpired,
    LessThanStepSize, //MarketOrderCannotMatchLessThanStepSize
    InvalidOrder,
    SelfMatch,
}