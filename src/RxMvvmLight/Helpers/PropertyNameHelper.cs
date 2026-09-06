using System;
using System.Collections.Generic;
using System.Text;

namespace RxMvvmLight.Helpers;

public static class PropertyNameHelper
{
    public static string[] ExtractNames(string propertySelector)
    {
        if(string.IsNullOrWhiteSpace(propertySelector))
            throw new ArgumentException("Property selector cannot be null or whitespace.", nameof(propertySelector));
        if(propertySelector.Contains('{') || propertySelector.Contains('['))
            throw new ArgumentException("only support simple property name or simple tuple", nameof(propertySelector));

        var lambdaIndex = propertySelector.IndexOf("=>");
        var span = propertySelector.AsSpan(lambdaIndex + 2).Trim();
        span = span.TrimStart('(');
        span = span.TrimEnd(')');

        if(span.Count('(') > 0)
        {
            throw new ArgumentException("only support simple property name or simple tuple", nameof(propertySelector));
        }

        var ranges = span.Split(',');

        var lambdaParam = propertySelector.AsSpan(0, lambdaIndex).Trim();
        lambdaParam = lambdaParam.TrimStart('(');
        lambdaParam = lambdaParam.TrimEnd(')');
        lambdaParam = lambdaParam.Trim();

        // x => x.Name  // simple property expression
        // x => Name    // simple property with closure capture
        // x => (x.Name, x.Email) // simple tuple expression
        List<string> result = new List<string>();

        foreach (var range in span.Split(','))
        {
            var name = span[range].Trim();
            if (name.StartsWith(lambdaParam.ToString()) && name.Length > lambdaParam.Length + 1
                && (name[lambdaParam.Length] == '.' || char.IsWhiteSpace(name[lambdaParam.Length])))
                name = name[(lambdaParam.Length + 1)..];

            name = name.TrimStart('.');

            // if is a chain of properties, take the first one, like x => x.Foo.Bar.Baz, we only need Foo
            var dotIndex = name.IndexOf('.');
            if (dotIndex > 0)
                name = name[..dotIndex].Trim();

            // make sure to remove any trailing characters that are not part of the property name, like ?, !, or whitespace
            name = name.TrimEnd('?');
            name = name.TrimEnd('!');
            name = name.Trim();

            result.Add(name.ToString());
        }

        return result.ToArray();
    }
}
