using System.ComponentModel;
using System.Reflection;

namespace StockTrading.Domain.Extensions;

public static class EnumExtensions
{
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }

    public static T GetEnumFromDescription<T>(string description) where T : Enum
    {
        var fields = typeof(T).GetFields();
        
        foreach (var field in fields)
        {
            var attribute = field.GetCustomAttribute<DescriptionAttribute>();
            if (attribute?.Description == description)
                return (T)field.GetValue(null)!;
        }
        
        return (T)Enum.Parse(typeof(T), description, true);
    }
}