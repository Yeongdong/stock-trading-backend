using System.Text.Json;
using System.Text.Json.Serialization;
using StockTrading.Application.DTOs.External.KoreaInvestment.Responses;

namespace StockTrading.Application.DTOs.External.KoreaInvestment.Converters;

/// <summary>
/// KIS API의 output 필드가 배열 또는 단일 객체로 올 수 있는 경우를 처리하는 컨버터
/// </summary>
public class FlexibleOutputJsonConverter : JsonConverter<List<KisOverseasOrderExecutionData>>
{
    public override List<KisOverseasOrderExecutionData> Read(ref Utf8JsonReader reader, Type typeToConvert,
        JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.StartArray:
                // 배열인 경우 - 실제 데이터가 있을 때
                return JsonSerializer.Deserialize<List<KisOverseasOrderExecutionData>>(ref reader, options) ?? [];
            case JsonTokenType.StartObject:
            {
                // 단일 객체인 경우 - 데이터가 없을 때의 빈 객체
                var singleItem = JsonSerializer.Deserialize<KisOverseasOrderExecutionData>(ref reader, options);

                // 빈 객체가 아닌 실제 데이터가 있는 경우에만 리스트에 추가
                if (singleItem != null &&
                    !string.IsNullOrEmpty(singleItem.StockCode) &&
                    !string.IsNullOrEmpty(singleItem.OrderNumber))
                {
                    return [singleItem];
                }

                return []; // 빈 객체인 경우 빈 리스트 반환
            }
            case JsonTokenType.None:
            case JsonTokenType.EndObject:
            case JsonTokenType.EndArray:
            case JsonTokenType.PropertyName:
            case JsonTokenType.Comment:
            case JsonTokenType.String:
            case JsonTokenType.Number:
            case JsonTokenType.True:
            case JsonTokenType.False:
            case JsonTokenType.Null:
            default:
                return [];
        }
    }

    public override void Write(Utf8JsonWriter writer, List<KisOverseasOrderExecutionData> value,
        JsonSerializerOptions options)
    {
        JsonSerializer.Serialize(writer, value, options);
    }
}