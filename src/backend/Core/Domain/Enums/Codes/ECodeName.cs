namespace Domain.Enums;

//NOTE: All code names should be present here and never be hardcoded anywhere
public enum ECodeName
{
    //Type - ECodeType.TITLE
    MR,
    MRS,

    //Type - ECodeType.USER_TYPE
    ADMIN,
    USER,

    //Type - ECodeType.VENDOR_CATEGORY
    IT_SERVICES,
    OFFICE_SUPPLIES,
    MAINTENANCE,
    CONSULTING,
    LOGISTICS,

    //Type - ECodeType.CATALOG_CATEGORY
    HARDWARE,
    SOFTWARE,
    FURNITURE,
    STATIONERY,
    CLEANING,

    //Type - ECodeType.UNIT_OF_MEASURE
    EACH,
    BOX,
    PACK,
    SET,
    HOUR,

    //Type - ECodeType.DELIVERY_LOCATION
    MAIN_OFFICE,
    WAREHOUSE,
    BRANCH_OFFICE,

    //Type - ECodeType.CURRENCY
    SGD,
    USD,
}
