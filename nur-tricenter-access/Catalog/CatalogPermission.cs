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