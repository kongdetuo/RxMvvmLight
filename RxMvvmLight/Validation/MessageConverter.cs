using System.Text.RegularExpressions;

namespace RxMvvmLight.Validation;

public static class MessageConverter
{
    class ConverterRegistry<T>
    {
        public static Func<T, string> Converter = null!;
    }

    static MessageConverter()
    {
        ConverterRegistry<string>.Converter = message => message;
        ConverterRegistry<Func<string>>.Converter = messageFunc => messageFunc();
        ConverterRegistry<BuiltInMessage>.Converter = builtInMessage => builtInMessage switch
        {
            BuiltInMessage.Required => "此字段为必填项",
            BuiltInMessage.TextNotBlank => "此字段不能为空",
            BuiltInMessage.TextMinLength => "长度不能少于{0}个字符",
            BuiltInMessage.TextMaxLength => "长度不能超过{0}个字符",
            BuiltInMessage.TextLength => "长度必须在{0}到{1}个字符之间",
            BuiltInMessage.InvalidFormat => "格式不正确",
            BuiltInMessage.InvalidEmailFormat => "邮箱格式不正确",
            BuiltInMessage.InvalidUrlFormat => "URL格式不正确",
            _ => throw new ArgumentOutOfRangeException("Invalid built-in message")
        };
    }

    public static void Register<T>(Func<T, string> converter)
    {
        ConverterRegistry<T>.Converter = converter;
    }

    public static string Convert<T>(T value)
    {
        if(ConverterRegistry<T>.Converter == null)
        {
            throw new InvalidOperationException($"No message converter registered for type '{nameof(T)}'.");
        }
        return ConverterRegistry<T>.Converter(value);
    }
}