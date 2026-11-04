using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json;
using System.Xml;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;

namespace FreshRescue.API.Extensions;

/// <summary>
/// Formatter XML dự phòng (Ch.03): <c>XmlSerializerOutputFormatter</c> không viết được
/// <c>IReadOnlyList&lt;T&gt;</c> và record không có constructor rỗng — tức mọi <c>PagedResponse</c>,
/// ProblemDetails và phần lớn DTO — nên trước đây <c>Accept: application/xml</c> âm thầm nhận JSON.
/// Formatter này đứng sau XmlSerializer: kiểu nào XmlSerializer viết được thì vẫn dùng XmlSerializer,
/// còn lại chuyển chính JSON của API (cùng tên trường camelCase, enum là chuỗi) sang XML.
/// </summary>
public sealed class JsonXmlOutputFormatter : TextOutputFormatter
{
    public JsonXmlOutputFormatter()
    {
        SupportedMediaTypes.Add("application/xml");
        SupportedMediaTypes.Add("text/xml");
        SupportedEncodings.Add(Encoding.UTF8);
    }

    protected override bool CanWriteType(Type? type)
        => type is not null && !typeof(Stream).IsAssignableFrom(type) && type != typeof(byte[]);

    public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
    {
        var jsonOptions = context.HttpContext.RequestServices.GetRequiredService<IOptions<JsonOptions>>().Value.JsonSerializerOptions;
        var json = JsonSerializer.SerializeToUtf8Bytes(context.Object, context.ObjectType ?? typeof(object), jsonOptions);

        XDocument document;
        using (var reader = JsonReaderWriterFactory.CreateJsonReader(json, XmlDictionaryReaderQuotas.Max))
        {
            document = XDocument.Load(reader);
        }

        var root = document.Root!;
        root.Name = RootName(context.ObjectType ?? context.Object?.GetType());
        // Thuộc tính type="..." là chi tiết của bộ chuyển JSON→XML, client không cần.
        foreach (var element in root.DescendantsAndSelf())
        {
            element.Attributes("type").Remove();
        }

        var settings = new XmlWriterSettings { Encoding = selectedEncoding, Async = true, Indent = false };
        await using var writer = XmlWriter.Create(context.HttpContext.Response.Body, settings);
        await document.WriteToAsync(writer, context.HttpContext.RequestAborted);
        await writer.FlushAsync();
    }

    // PagedResponse`1[OfferDto] → PagedResponseOfOfferDto, giống cách XmlSerializer đặt tên kiểu generic.
    private static string RootName(Type? type)
    {
        if (type is null)
        {
            return "response";
        }

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var name = type.Name[..type.Name.IndexOf('`')];
        return name + "Of" + string.Join("And", type.GetGenericArguments().Select(RootName));
    }
}
