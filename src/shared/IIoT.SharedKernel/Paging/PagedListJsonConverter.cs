using System.Text.Json;
using System.Text.Json.Serialization;

namespace IIoT.SharedKernel.Paging;

/// <summary>
/// PagedList&lt;T&gt; 的 JSON 转换器工厂。
/// 解决继承 List&lt;T&gt; 的类被默认序列化成数组、MetaData 属性丢失的坑。
/// 序列化输出格式:{ "items": [...], "metaData": { ... } }
/// 反序列化同构,支持前端 NSwag/Refit 生成的强类型客户端直接使用。
/// </summary>
public sealed class PagedListJsonConverterFactory : JsonConverterFactory
{
    public override bool CanConvert(Type typeToConvert)
    {
        if (!typeToConvert.IsGenericType) return false;
        return typeToConvert.GetGenericTypeDefinition() == typeof(PagedList<>);
    }

    public override JsonConverter CreateConverter(Type typeToConvert, JsonSerializerOptions options)
    {
        var itemType = typeToConvert.GetGenericArguments()[0];
        var converterType = typeof(PagedListJsonConverter<>).MakeGenericType(itemType);
        return (JsonConverter)Activator.CreateInstance(converterType)!;
    }
}

internal sealed class PagedListJsonConverter<T> : JsonConverter<PagedList<T>>
{
    public override PagedList<T>? Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject)
            throw new JsonException("期望 PagedList 起始 '{'");

        List<T>? items = null;
        PagedMetaData? metaData = null;

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                items ??= [];
                metaData ??= new PagedMetaData();

                var pagination = new Pagination
                {
                    PageNumber = metaData.CurrentPage,
                    PageSize = metaData.PageSize
                };
                var result = new PagedList<T>(items, (int)metaData.TotalCount, pagination);
                result.MetaData = metaData;
                return result;
            }

            if (reader.TokenType != JsonTokenType.PropertyName) continue;

            var propertyName = reader.GetString();
            reader.Read();

            if (string.Equals(propertyName, "items", StringComparison.OrdinalIgnoreCase))
            {
                items = JsonSerializer.Deserialize<List<T>>(ref reader, options);
            }
            else if (string.Equals(propertyName, "metaData", StringComparison.OrdinalIgnoreCase))
            {
                metaData = JsonSerializer.Deserialize<PagedMetaData>(ref reader, options);
            }
            else
            {
                reader.Skip();
            }
        }

        throw new JsonException("PagedList JSON 未正常闭合");
    }

    public override void Write(
        Utf8JsonWriter writer,
        PagedList<T> value,
        JsonSerializerOptions options)
    {
        writer.WriteStartObject();

        writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("Items") ?? "items");
        JsonSerializer.Serialize(writer, (List<T>)value, options);

        writer.WritePropertyName(options.PropertyNamingPolicy?.ConvertName("MetaData") ?? "metaData");
        JsonSerializer.Serialize(writer, value.MetaData, options);

        writer.WriteEndObject();
    }
}
