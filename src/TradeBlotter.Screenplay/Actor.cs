namespace TradeBlotter.Screenplay;

public interface IAbility { }
public interface ITask { void PerformAs(Actor actor); }
public interface IQuestion<out T> { T AnsweredBy(Actor actor); }

/// <summary>Immutable ability registration; execution is sequential and fail-fast.</summary>
public sealed class Actor
{
    private readonly IReadOnlyDictionary<Type, IAbility> abilities;
    private readonly bool canPerformTasks;
    public string Name { get; }

    public Actor(string name, params IAbility[] abilities) : this(name, true, abilities) { }

    internal Actor(string name, bool canPerformTasks, params IAbility[] abilities)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(abilities);
        Name = name;
        this.canPerformTasks = canPerformTasks;
        var registry = new Dictionary<Type, IAbility>();
        foreach (var ability in abilities)
        {
            ArgumentNullException.ThrowIfNull(ability);
            if (!registry.TryAdd(ability.GetType(), ability))
                throw new ArgumentException($"Duplicate ability {ability.GetType().Name} for {name}.", nameof(abilities));
        }
        this.abilities = registry;
    }

    public T AbilityTo<T>() where T : class, IAbility =>
        abilities.TryGetValue(typeof(T), out var ability) ? (T)ability
            : throw new InvalidOperationException($"{Name} does not have ability {typeof(T).Name}.");

    public Actor AttemptsTo(params ITask[] tasks)
    {
        ArgumentNullException.ThrowIfNull(tasks);
        if (tasks.Any(task => task is null)) throw new ArgumentException("Tasks cannot contain null.", nameof(tasks));
        if (!canPerformTasks) throw new InvalidOperationException($"{Name} is an inspection-only actor.");
        foreach (var task in tasks) task.PerformAs(this);
        return this;
    }

    public T AsksFor<T>(IQuestion<T> question)
    {
        ArgumentNullException.ThrowIfNull(question);
        return question.AnsweredBy(this);
    }
}
