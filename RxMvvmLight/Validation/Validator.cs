using System.Collections;
using System.Diagnostics;
using R3;

namespace RxMvvmLight.Validation;

public class Validator : IDisposable
{
    private sealed class PropertyState
    {
        public IPropertyPipeline? Pipeline;

        public List<string>? Errors;

        public ValidationState State = ValidationState.NotValidated;

        public BehaviorSubject<ValidationState>? StateSubject;
    }

    private readonly Lock gate = new();
    private readonly Lock notifyGate = new();
    private readonly Dictionary<string, PropertyState> properties = new(StringComparer.Ordinal);
    private readonly BehaviorSubject<bool> validatingSubject = new(false);
    private readonly BehaviorSubject<bool> validSubject = new(true);
    private readonly Subject<string> stateChangedSubject = new();

    public Observable<string> StateChanged => stateChangedSubject;

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
        RecomputeGates();
        stateChangedSubject.OnNext(propertyName);
    }

    // 评估开始：State=Validating，直至结果落定
    internal void BeginEvaluation(string propertyName)
    {
        BehaviorSubject<ValidationState>? subject;
        lock (gate)
        {
            var state = properties[propertyName];
            if (state.State == ValidationState.Validating)
                return;
            state.State = ValidationState.Validating;
            subject = state.StateSubject;
        }

        if (subject is not null)
        {
            lock (notifyGate)
            {
                subject.OnNext(ValidationState.Validating);
            }
        }
        RecomputeGates();
        stateChangedSubject.OnNext(propertyName);
    }

    internal void SetErrors(string propertyName, IReadOnlyCollection<string> messages)
    {
        lock (gate)
        {
            var state = properties[propertyName];
            var newList = messages.Count == 0 ? null : messages.ToList();
            state.Errors = newList;
        }

        stateChangedSubject.OnNext(propertyName);
    }

    public IEnumerable GetErrors(string? propertyName)
    {
        lock (gate)
        {
            if (propertyName is not null && properties.TryGetValue(propertyName, out var state) && state.Errors is { Count: > 0 } list)
                return list.ToArray();
            return Array.Empty<string>();
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
        stateChangedSubject.Dispose();
    }

    private void RecomputeGates()
    {
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

}
