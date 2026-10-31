using Nur.Tricenter.Integration;

namespace Nur.Tricenter.Integration.Messages.Catalog;

/// <summary>
/// Announces the full composition of a meal plan after it changed. Published to
/// <c>meal-plan-composition-changed</c>.
/// </summary>
/// <remarks>
/// Published by MS2 (plans and recipes catalog) whenever a meal time is added to a plan or a recipe
/// is assigned to / removed from a meal time. It is a full snapshot, not a delta: MS4 consumes it
/// from <c>ms4.meal-plan-composition-changed</c> and replaces its projection of the plan's day
/// menus, ignoring a snapshot older than the one it already holds (by <c>CreatedAt</c>).
/// </remarks>
public record MealPlanCompositionChanged : IntegrationMessageContract
{
    /// <summary>Required by the Newtonsoft deserializer used by the RabbitMQ library.</summary>
    public MealPlanCompositionChanged()
    {
        Days = [];
    }

    /// <summary>Creates the contract and forwards trace metadata to the shared envelope.</summary>
    public MealPlanCompositionChanged(string? correlationId, string? source)
        : base(correlationId, source)
    {
        Days = [];
    }

    /// <summary>Stable discriminator on the wire, in the <c>&lt;Area&gt;.&lt;Fact&gt;</c> form.</summary>
    public string EventType => "MealPlan.CompositionChanged";

    public Guid PlanId { get; init; }

    public IReadOnlyList<MealPlanDay> Days { get; init; }
}

/// <summary>One day of a meal plan, identified by its number (1..N) inside the plan.</summary>
public record MealPlanDay
{
    public MealPlanDay()
    {
        MealTimes = [];
    }

    public int DayNumber { get; init; }

    public IReadOnlyList<MealPlanMealTime> MealTimes { get; init; }
}

/// <summary>A meal time of a plan day (breakfast, lunch, ...) with its recipes.</summary>
public record MealPlanMealTime
{
    public MealPlanMealTime()
    {
        Name = string.Empty;
        Recipes = [];
    }

    public string Name { get; init; }

    /// <summary>Position of the meal time inside the day.</summary>
    public int Order { get; init; }

    public IReadOnlyList<MealPlanRecipe> Recipes { get; init; }
}

/// <summary>A recipe assigned to a meal time, with the rations to prepare per patient.</summary>
public record MealPlanRecipe
{
    public MealPlanRecipe()
    {
        RecipeName = string.Empty;
    }

    public Guid RecipeId { get; init; }

    public string RecipeName { get; init; }

    public int Rations { get; init; }
}
