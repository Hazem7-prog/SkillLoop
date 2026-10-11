using System.Text.Json.Serialization;

namespace SkillLoop.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CourseLevel
{
    Beginner = 0,
    Intermediate = 1,
    Advanced = 2
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CourseStatus
{
    Active = 0,
    Archived = 1
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BookingType
{
    Online = 0,
    Offline = 1
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum BookingStatus
{
    Upcoming = 0,
    Completed = 1,
    Cancelled = 2
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransactionType
{
    SessionBooking,
    SessionEarning,
    Purchase,
    PromoCode,
    Refund
}