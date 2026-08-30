using System.Text.RegularExpressions;

namespace RxMvvmLight.Validation;

public enum BuiltInMessage
{
    Required = 0,
    TextNotBlank = 100,
    TextMinLength = 101,
    TextMaxLength = 102,
    TextLength = 103,
    InvalidFormat = 201,
    InvalidEmailFormat = 202,
    InvalidUrlFormat = 203,
}

public static class ValidationOperators
{
    extension<T>(PropertyValidationBuilder<T> builder)
    {
        public PropertyValidationBuilder<T> Must<TMessage>(Predicate<T> predicate, TMessage message)
        {
            return builder.RegisterRule(value => predicate(value), message);
        }

        public PropertyValidationBuilder<T> MustAsync<TMessage>(Func<T, Task<bool>> predicate, TMessage message)
        {
            return builder.RegisterAsyncRule(async value => await predicate(value), message);
        }

        public PropertyValidationBuilder<T> MustAsync<TMessage>(Func<T, CancellationToken, Task<bool>> predicate, TMessage message)
        {
            return builder.RegisterAsyncRule(async (value, ct) => await predicate(value, ct), message);
        }

        public PropertyValidationBuilder<T> Required() => builder.Required(BuiltInMessage.TextNotBlank);
        public PropertyValidationBuilder<T> Required<TMessage>(TMessage message)
        {
            return builder.Must(v => v is not null, message);
        }

        public PropertyValidationBuilder<T> Not<TMessage>(Predicate<T> predicate, TMessage message)
        {
            return builder.Must(v => !predicate(v), message);
        }

        public PropertyValidationBuilder<T> In<TMessage>(IEnumerable<T> values, TMessage message)
        {
            var set = values.ToHashSet();
            return builder.Must(v => v is not null && set.Contains(v), message);
        }

        public PropertyValidationBuilder<T> In<TMessage>(IEqualityComparer<T> comparer, IEnumerable<T> values, TMessage message)
        {
            var set = new HashSet<T>(values, comparer);
            return builder.Must(v => v is not null && set.Contains(v), message);
        }
    }

    extension<T>(PropertyValidationBuilder<T> builder) where T : IComparable<T>
    {
        public PropertyValidationBuilder<T> GreaterThan<TMessage>(T min, string message)
        {
            return builder.Must(v => v is not null && v.CompareTo(min) > 0, message);
        }

        public PropertyValidationBuilder<T> GreaterThanOrEqual<TMessage>(T min, string message)
        {
            return builder.Must(v => v is not null && v.CompareTo(min) >= 0, message);
        }

        public PropertyValidationBuilder<T> LessThan<TMessage>(T max, string message)
        {
            return builder.Must(v => v is not null && v.CompareTo(max) < 0, message);
        }

        public PropertyValidationBuilder<T> LessThanOrEqual<TMessage>(T max, string message)
        {
            return builder.Must(v => v is not null && v.CompareTo(max) <= 0, message);
        }

        public PropertyValidationBuilder<T> Between<TMessage>(T min, T max, string message)
        {
            return builder.Must(v => v is not null && v.CompareTo(min) >= 0 && v.CompareTo(max) <= 0, message);
        }
    }

    extension(PropertyValidationBuilder<string> builder)
    {

        public PropertyValidationBuilder<string> Required() => builder.Required(BuiltInMessage.TextNotBlank);
        public PropertyValidationBuilder<string> Required<TMessage>(TMessage message)
        {
            return builder.Must(v => !string.IsNullOrEmpty(v), message);
        }

        public PropertyValidationBuilder<string> NotBlank() => builder.Required(BuiltInMessage.TextNotBlank);
        public PropertyValidationBuilder<string> NotBlank<TMessage>(TMessage message)
        {
            return builder.RegisterRule(v => !string.IsNullOrWhiteSpace(v), message);
        }

        public PropertyValidationBuilder<string> MinLength(int length) => builder.MinLength(length, BuiltInMessage.TextMinLength);
        public PropertyValidationBuilder<string> MinLength<TMessage>(int length, TMessage message)
        {
            return builder.RegisterRule(v => v?.Length >= length, () => string.Format(MessageConverter.Convert(message), length));
        }

        public PropertyValidationBuilder<string> MaxLength(int length) => builder.MaxLength(length, BuiltInMessage.TextMaxLength);
        public PropertyValidationBuilder<string> MaxLength<TMessage>(int length, TMessage message)
        {
            return builder.RegisterRule(v => v?.Length <= length, () => string.Format(MessageConverter.Convert(message), length));
        }

        public PropertyValidationBuilder<string> Length(int minLength, int maxLength) => builder.Length(minLength, maxLength, BuiltInMessage.TextLength);
        public PropertyValidationBuilder<string> Length<TMessage>(int minLength, int maxLength, TMessage message)
        {
            return builder.RegisterRule(v => v?.Length >= minLength && v?.Length <= maxLength, 
                () => string.Format(MessageConverter.Convert(message), minLength, maxLength));
        }

        public PropertyValidationBuilder<string> Matches(string pattern) => builder.Matches(pattern, BuiltInMessage.InvalidFormat);
        public PropertyValidationBuilder<string> Matches<TMessage>(string pattern, TMessage message)
        {
            return builder.RegisterRule(v => v is not null && Regex.IsMatch(v, pattern), message);
        }

        public PropertyValidationBuilder<string> Matches(Regex regex) => builder.Matches(regex, BuiltInMessage.InvalidFormat);
        public PropertyValidationBuilder<string> Matches<TMessage>(Regex regex, TMessage message)
        {
            return builder.RegisterRule(v => v is not null && regex.IsMatch(v), message);
        }

        public PropertyValidationBuilder<string> Email() => builder.Email(BuiltInMessage.InvalidEmailFormat);
        public PropertyValidationBuilder<string> Email<TMessage>(TMessage message)
        {
            return builder.Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", message);
        }

        public PropertyValidationBuilder<string> Url() => builder.Url(BuiltInMessage.InvalidUrlFormat);
        public PropertyValidationBuilder<string> Url<TMessage>(TMessage message)
        {
            return builder.RegisterRule(v => v is not null && Uri.TryCreate(v, UriKind.Absolute, out _), message);
        }
    }
}
