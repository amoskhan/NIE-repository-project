using System.Reflection;
using System.Runtime.Serialization;

namespace BuildingBlocks.Extensions;

public static class EnumExtensions
{
    public static string GetEnumMemberValue(this global::System.Enum enumValue)
    {
        var type = enumValue.GetType();
        var memberInfo = type.GetMember(enumValue.ToString()).FirstOrDefault();
        if (memberInfo != null)
        {
            var attribute = memberInfo.GetCustomAttribute<EnumMemberAttribute>();
            if (attribute?.Value != null)
                return attribute.Value;
        }

        return enumValue.ToString();
    }
}
