namespace Nur.Tricenter.Access.Catalog;

/// <summary>
/// The scopes the catalog service recognises.
/// </summary>
/// <remarks>
/// Declared here, in the kernel, rather than in the catalog's own contracts project. The catalog
/// owns the MEANING of these scopes, but it does not own the only copy: identity has to be able to
/// grant them into a token, and the catalog is the one service that must never be a dependency of
/// the platform's security layer.
/// </remarks>
public static class CatalogPermission
{
    public const string FoodRead = "catalog.food:read";
    public const string FoodCreate = "catalog.food:create";
    public const string FoodEdit = "catalog.food:edit";
    public const string FoodManage = "catalog.food:manage";

    public const string RecipeRead = "catalog.recipe:read";
    public const string RecipeCreate = "catalog.recipe:create";
    public const string RecipeEdit = "catalog.recipe:edit";
    public const string RecipeManage = "catalog.recipe:manage";

    public const string MealPlanRead = "catalog.mealPlan:read";
    public const string MealPlanCreate = "catalog.mealPlan:create";
    public const string MealPlanEdit = "catalog.mealPlan:edit";
    public const string MealPlanManage = "catalog.mealPlan:manage";

    /// <summary>
    /// Every scope this service defines. Registered by the catalog to build one authorization
    /// policy per entry, and read by identity when granting a platform administrator the full set.
    /// </summary>
    public static IReadOnlyList<string> PermissionsList { get; } = new[]
    {
        FoodRead, FoodCreate, FoodEdit, FoodManage,
        RecipeRead, RecipeCreate, RecipeEdit, RecipeManage,
        MealPlanRead, MealPlanCreate, MealPlanEdit, MealPlanManage
    };
}

/// <summary>
/// The catalog's routes and the scope each one requires.
/// </summary>
/// <remarks>
/// <para>
/// Declared beside <see cref="CatalogPermission"/> on purpose: a permission and the route it
/// guards are one decision, and splitting them means a new endpoint can be added with a scope that
/// nothing declares. This is also where the reference architecture puts them, which is what
/// <c>Routes.RouteList</c> in the aggregator collects for the gateway.
/// </para>
/// <para>
/// <b>Today nothing reads this.</b> Our gateway discovers routes from Consul and synthesises the
/// <c>/api/{serviceName}</c> prefix itself, and authorization is enforced by <c>[Authorize]</c>
/// inside each controller rather than at the edge. So this is currently documentation: it says
/// which scope an endpoint needs, for whoever reads it.
/// </para>
/// <para>
/// That is not a permanent state. If the gateway ever consumes <c>Routes.RouteList</c>, an endpoint
/// whose author forgot <c>[Authorize]</c> is still rejected at the border — a second layer that does
/// not depend on every service remembering.
/// </para>
/// </remarks>
public static class CatalogRoutes
{
    private const string Base = "/api/catalog";

    /// <summary>
    /// Every route the catalog exposes, with the scope a caller must present.
    /// </summary>
    public static IReadOnlyList<ScopeAccessRule> Routes { get; } = new[]
    {
        // Foods
        new ScopeAccessRule { RouteTemplate = $"{Base}/foods", HttpMethod = "GET", RequiredScope = CatalogPermission.FoodRead },
        new ScopeAccessRule { RouteTemplate = $"{Base}/foods", HttpMethod = "POST", RequiredScope = CatalogPermission.FoodCreate },
        new ScopeAccessRule { RouteTemplate = $"{Base}/foods/{{id}}", HttpMethod = "GET", RequiredScope = CatalogPermission.FoodRead },
        new ScopeAccessRule { RouteTemplate = $"{Base}/foods/{{id}}", HttpMethod = "PUT", RequiredScope = CatalogPermission.FoodEdit },

        // Recipes
        new ScopeAccessRule { RouteTemplate = $"{Base}/recipes", HttpMethod = "GET", RequiredScope = CatalogPermission.RecipeRead },
        new ScopeAccessRule { RouteTemplate = $"{Base}/recipes", HttpMethod = "POST", RequiredScope = CatalogPermission.RecipeCreate },
        new ScopeAccessRule { RouteTemplate = $"{Base}/recipes/{{id}}", HttpMethod = "GET", RequiredScope = CatalogPermission.RecipeRead },
        new ScopeAccessRule { RouteTemplate = $"{Base}/recipes/ingredients", HttpMethod = "POST", RequiredScope = CatalogPermission.RecipeEdit },
        new ScopeAccessRule { RouteTemplate = $"{Base}/recipes/{{recipeId}}/ingredients/{{foodId}}", HttpMethod = "DELETE", RequiredScope = CatalogPermission.RecipeEdit },

        // Meal plans
        new ScopeAccessRule { RouteTemplate = $"{Base}/meal-plans", HttpMethod = "GET", RequiredScope = CatalogPermission.MealPlanRead },
        new ScopeAccessRule { RouteTemplate = $"{Base}/meal-plans", HttpMethod = "POST", RequiredScope = CatalogPermission.MealPlanCreate },
        new ScopeAccessRule { RouteTemplate = $"{Base}/meal-plans/{{id}}", HttpMethod = "GET", RequiredScope = CatalogPermission.MealPlanRead },
        new ScopeAccessRule { RouteTemplate = $"{Base}/meal-plans/meal-times", HttpMethod = "POST", RequiredScope = CatalogPermission.MealPlanEdit },
        new ScopeAccessRule { RouteTemplate = $"{Base}/meal-plans/assign-recipes", HttpMethod = "POST", RequiredScope = CatalogPermission.MealPlanEdit },
        new ScopeAccessRule { RouteTemplate = $"{Base}/meal-plans/{{planId}}/meal-times/{{mealTimeId}}/recipes/{{recipeId}}", HttpMethod = "DELETE", RequiredScope = CatalogPermission.MealPlanEdit }
    };
}