using System.Text.RegularExpressions;

namespace RxMvvmLight.Validation;

public static class ValidationOperators
{
    extension<T>(PropertyValidationBuilder<T> builder)
    {
        public PropertyValidationBuilder<T> Must(Predicate<T> predicate, string message)
        {
            return builder.RegisterRule(value => predicate(value) ? Array.Empty<string>() : new[] { message });
        }

        public PropertyValidationBuilder<T> MustAsync(Func<T, Task<bool>> predicate, string message)
        {
            return builder.RegisterAsyncRule(async value => await predicate(value) ? Array.Empty<string>() : new[] { message });
        }

        public PropertyValidationBuilder<T> MustAsync(Func<T, CancellationToken, Task<bool>> predicate, string message)
        {
            return builder.RegisterAsyncRule(async (value, ct) => await predicate(value, ct) ? Array.Empty<string>() : new[] { message });
        }

        public PropertyValidationBuilder<T> Required(string message = "此字段为必填项")
        {
            return builder.Must(v => v is not null, message);
        }

        public PropertyValidationBuilder<T> Not(Predicate<T> predicate, string message)
        {
            return builder.Must(v => !predicate(v), message);
        }

        public PropertyValidationBuilder<T> In(IEnumerable<T> values, string message)
        {
            var set = values.ToHashSet();
            return builder.Must(v => v is not null && set.Contains(v), message);
        }

        public PropertyValidationBuilder<T> In(IEqualityComparer<T> comparer, IEnumerable<T> values, string message)
        {
            var set = new HashSet<T>(values, comparer);
            return builder.Must(v => v is not null && set.Contains(v), message);
        }
    }

    extension<T>(PropertyValidationBuilder<T> builder) where T : IComparable<T>
    {
        public PropertyValidationBuilder<T> GreaterThan(T min, string message)
        {
            return builder.Must(v => v is not null && v.CompareTo(min) > 0, message);
        }

        public PropertyValidationBuilder<T> GreaterThanOrEqual(T min, string message)
        {
            return builder.Must(v => v is not null && v.CompareTo(min) >= 0, message);
        }

        public PropertyValidationBuilder<T> LessThan(T max, string message)
        {
            return builder.Must(v => v is not null && v.CompareTo(max) < 0, message);
        }

        public PropertyValidationBuilder<T> LessThanOrEqual(T max, string message)
        {
            return builder.Must(v => v is not null && v.CompareTo(max) <= 0, message);
        }

        public PropertyValidationBuilder<T> Between(T min, T max, string message)
        {
            return builder.Must(v => v is not null && v.CompareTo(min) >= 0 && v.CompareTo(max) <= 0, message);
        }
    }

    extension(PropertyValidationBuilder<string> builder)
    {
        public PropertyValidationBuilder<string> Required(string message = "此字段为必填项")
        {
            return builder.Must(v => !string.IsNullOrEmpty(v), message);
        }

        public PropertyValidationBuilder<string> NotBlank(string message = "此字段不能为空")
        {
            return builder.Must(v => !string.IsNullOrWhiteSpace(v), message);
        }

        public PropertyValidationBuilder<string> MinLength(int length, string message = "长度不能少于{0}个字符")
        {
            return builder.Must(v => v?.Length >= length, string.Format(message, length));
        }

        public PropertyValidationBuilder<string> MaxLength(int length, string message = "长度不能超过{0}个字符")
        {
            return builder.Must(v => v?.Length <= length, string.Format(message, length));
        }

        public PropertyValidationBuilder<string> Length(int minLength, int maxLength, string message = "长度必须在{0}到{1}个字符之间")
        {
            return builder.Must(v => v?.Length >= minLength && v?.Length <= maxLength, string.Format(message, minLength, maxLength));
        }

        public PropertyValidationBuilder<string> Matches(string pattern, string message = "格式不正确")
        {
            return builder.Must(v => v is not null && Regex.IsMatch(v, pattern), message);
        }

        public PropertyValidationBuilder<string> Matches(Regex regex, string message = "格式不正确")
        {
            return builder.Must(v => v is not null && regex.IsMatch(v), message);
        }

        public PropertyValidationBuilder<string> Email(string message = "邮箱格式不正确")
        {
            return builder.Matches(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", message);
        }

        public PropertyValidationBuilder<string> Url(string message = "URL格式不正确")
        {
            return builder.Must(v => v is not null && Uri.TryCreate(v, UriKind.Absolute, out _), message);
        }
    }
}
