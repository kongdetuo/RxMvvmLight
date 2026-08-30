using System.Collections;
using R3;
using RxMvvmLight.Validation.Helpers;

namespace RxMvvmLight.Validation;

public class ReactiveValidator : IDisposable
{
    private sealed class PropertyState
    {
        public IPropertyPipeline? Pipeline;

        public IReadOnlyList<string> Errors = [];

        public ValidationState State = ValidationState.NotValidated;

        public BehaviorSubject<ValidationState>? StateSubject;

        public IReadOnlyList<Func<string>> ErrorProviders { get; internal set; } = [];
    }

    private static readonly WeakReferenceList<ReactiveValidator> allValidator = new();

    public ReactiveValidator()
    {
        allValidator.Add(this);
    }

    private readonly Lock gate = new();
    private readonly Lock notifyGate = new();
    private readonly Dictionary<string, PropertyState> properties = new(StringComparer.Ordinal);
    private readonly BehaviorSubject<bool> validatingSubject = new(false);
    private readonly BehaviorSubject<bool> validSubject = new(true);
    private readonly Subject<PropertyErrors> errorsChangedSubject = new();

    public Observable<PropertyErrors> ErrorsChanged => errorsChangedSubject;

    public Observable<bool> IsValidating => validatingSubject;

    public Observable<bool> IsValid => validSubject;

    public bool HasErrors
    {
        get
        {
            lock (gate)
            {
                return properties.Values.Any(p => p.Errors is { Count: > 0 });
            }
        }
    }

    public Observable<ValidationState> GetState(string propertyName)
    {
        lock (gate)
        {
            if (!properties.TryGetValue(propertyName, out var state))
                throw new InvalidOperationException(
                    $"Property '{propertyName}' is not registered.");
            state.StateSubject ??= new BehaviorSubject<ValidationState>(state.State);
            return state.StateSubject;
        }
    }

    public Observable<bool> Validating(string propertyName) =>
        GetState(propertyName).Select(s => s == ValidationState.Validating);

    public Observable<bool> Valid(string propertyName) =>
        GetState(propertyName).Select(s => s == ValidationState.Valid);

    public IReadOnlyList<string> GetInvalidProperties()
    {
        lock (gate)
        {
            return properties
                .Where(p => p.Value.State == ValidationState.Invalid)
                .Select(p => p.Key)
                .ToList();
        }
    }

    public bool ContainsProperty(string propertyName)
    {
        return properties.ContainsKey(propertyName);
    }

    internal void Register<T>(string propertyName, Observable<T> source, List<IRule<T>> rules, CascadeMode cascadeMode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        lock (gate)
        {
            if (properties.ContainsKey(propertyName))
                throw new InvalidOperationException(
                    $"Property '{propertyName}' is already registered. Use When() for conditional rules.");
            var state = new PropertyState();
            properties[propertyName] = state;
            state.Pipeline = new PropertyPipeline<T>(this, propertyName, source, rules, cascadeMode);
        }
    }

    internal void SetState(string propertyName, ValidationState newState)
    {
        BehaviorSubject<ValidationState>? subject;
        lock (gate)
        {
            var state = properties[propertyName];
            state.State = newState;
            subject = state.StateSubject;
        }

        if (subject is not null)
        {
            lock (notifyGate)
            {
                subject.OnNext(newState);
            }
        }

        bool validating, valid;
        lock (gate)
        {
            validating = properties.Values.Any(p => p.State == ValidationState.Validating);
            valid = properties.Values.All(p => p.State == ValidationState.Valid);
        }

        lock (notifyGate)
        {
            validatingSubject.OnNext(validating);
            validSubject.OnNext(valid);
        }
    }

    internal void SetErrors(string propertyName, IReadOnlyList<Func<string>> errors)
    {
        var newErrors = errors.Select(p=>p()).ToList();

        lock (gate)
        {
            var state = properties[propertyName];
            if (newErrors.SequenceEqual(state.Errors))
                return;
            state.ErrorProviders = errors;
            state.Errors = newErrors;
        }

        errorsChangedSubject.OnNext(new(propertyName, newErrors));
    }

    public IEnumerable GetErrors(string? propertyName)
    {
        lock (gate)
        {
            if (propertyName is not null && properties.TryGetValue(propertyName, out var state))
                return state.Errors;
            return Array.Empty<string>();
        }
    }

    public void RefreshMessage()
    {
        List<PropertyErrors> propertyErrors = [];
        lock (gate)
        {
            foreach (var item in properties.Values)
            {
                if(item.Errors.Count > 0)
                {
                    var errors = item.ErrorProviders.Select(p => p()).ToArray();
                    if (!errors.SequenceEqual(item.Errors))
                    {
                        item.Errors = errors;
                        propertyErrors.Add(new PropertyErrors(item.Pipeline.PropertyName, item.Errors));
                    }
                }
            }
        }
        foreach (var item in propertyErrors)
        {
            errorsChangedSubject.OnNext(item);
        }
    }

    public static void RefreshAllMessage()
    {
        foreach (var item in allValidator.GetLiveItems())
        {
            item.RefreshMessage();
        }
    }

    public void Dispose()
    {
        List<IDisposable> pipelinesToDispose;
        List<BehaviorSubject<ValidationState>> subjects;
        lock (gate)
        {
            pipelinesToDispose = properties.Values
                .Where(p => p.Pipeline is not null)
                .Select(p => (IDisposable)p.Pipeline!)
                .ToList();
            subjects = properties.Values
                .Where(p => p.StateSubject is not null)
                .Select(p => p.StateSubject!)
                .ToList();
            properties.Clear();
        }

        foreach (var pipeline in pipelinesToDispose)
            pipeline.Dispose();

        lock (notifyGate)
        {
            validatingSubject.OnCompleted();
            validSubject.OnCompleted();
            foreach (var subject in subjects)
                subject.OnCompleted();
        }
        validatingSubject.Dispose();
        validSubject.Dispose();
        foreach (var subject in subjects)
            subject.Dispose();
        errorsChangedSubject.Dispose();
    }


}

public record struct PropertyErrors(string PropertyName, IReadOnlyList<string> Errors);
