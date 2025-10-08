namespace Enum;

public enum MatchState : byte
{
    //Success Result
    OrderAccepted = 1,
    CancelAcepted = 2,
    OrderValid = 3,

    //Failure Result
    OrderNotExists = 11,
    OrderInvalid = 12, //Invalid_Price_Quantity_Stop_Price_Order_Amount_Or_Total_Quantity
    DuplicateOrder = 13,
    BOCCannotMOS = 14, //Book_Or_Cancel_Cannot_Be_Market_Or_Stop_Order
    IOCCannotMOS = 15, //Immediate_Or_Cancel_Cannot_Be_Market_Or_Stop_Order
    IcebergCannotMOSM = 16, //Iceberg_Order_Cannot_Be_Market_Or_Stop_Market_Order
    IcebergCannotFOKIOC = 17, //Iceberg_Order_Cannot_Be_FOK_or_IOC
    InvalidIcebergVolume = 18, //Invalid_Iceberg_Order_Total_Quantity
    FOKCannotStopOrder = 19, //Fill_Or_Kill_Cannot_Be_Stop_Order
    InvalidCancelOnForGTD = 20,
    GTDCannotMarketOrIOCFOK = 21, //GoodTillDate_Cannot_Be_Market_Or_IOC_or_FOK
    MOONotBothAmountOrVolume = 22, //Market_Order_Only_Supported_Order_Amount_Or_Quantity_No_Both
    MarketBuyAmountOnly = 23, //Order_Amount_Only_Supported_For_Market_Buy_Order
    NotMultipleOfStepSize = 24, //Quantity_And_Total_Quantity_Should_Be_Multiple_Of_Step_Size
    BOCCannotBook = 31, //BookOrCancelCannotBook
    FOKCannotFill = 32, //FillOrKillCannotFill
    IOCCannotFill = 33, //ImmediateOrCancelCannotFill
    MONoLiquidity = 34, //MarketOrderNoLiquidity

    SystemError = 99
}