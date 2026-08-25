namespace DragonCommon.Domain;

public static class ValidationMessages
{
    public const string IS_REQUIRED = "is required";
    public const string MUST_BE_A_POSITIVE_NUMBER = "must be a positive number";
    public const string MUST_BE_A_NON_NEGATIVE_NUMBER = "must be a non-negative number";
    public const string MUST_BE_MIDNIGHT_UTC = "must be midnight UTC";
    public const string START_DATE_BEFORE_END_DATE = "start date must preceed end date";
    public const string MOST_BE_IN_DRAFT_STATUS_TO_EDIT = "Must be in Draft status to be edited";
}
