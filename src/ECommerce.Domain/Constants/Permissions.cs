using System.Reflection;

namespace ECommerce.Domain.Constants;

public static class Permissions
{
    public static class Product
    {
        public const string View = "Product.View";
        public const string Manage = "Product.Manage";
    }

    public static class Category
    {
        public const string Manage = "Category.Manage";
    }

    public static class Order
    {
        public const string Manage = "Order.Manage";
    }

    public static class User
    {
        public const string Manage = "User.Manage";
    }

    public static class Role
    {
        public const string Manage = "Role.Manage";
    }

    public static class Statistics
    {
        public const string View = "Statistics.View";
    }

    public static class Trash
    {
        public const string Manage = "Trash.Manage";
    }

    public static class Coupon
    {
        public const string Manage = "Coupon.Manage";
    }

    /// <summary>
    /// Returns all declared permission string constants via reflection.
    /// Useful for admin UI permission lists and role configuration.
    /// </summary>
    public static IReadOnlyList<string> GetAllPermissions()
    {
        var permissions = new List<string>();
        var nestedTypes = typeof(Permissions).GetNestedTypes(BindingFlags.Public | BindingFlags.Static);

        foreach (var type in nestedTypes)
        {
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

            foreach (var field in fields)
            {
                var value = field.GetValue(null)?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    permissions.Add(value);
                }
            }
        }

        return permissions.Distinct().ToList().AsReadOnly();
    }

    /// <summary>
    /// Returns all declared permission string constants grouped by module name.
    /// </summary>
    public static Dictionary<string, List<string>> GetPermissionsByModule()
    {
        var result = new Dictionary<string, List<string>>();
        var nestedTypes = typeof(Permissions).GetNestedTypes(BindingFlags.Public | BindingFlags.Static);

        foreach (var type in nestedTypes)
        {
            var moduleName = type.Name;
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
                .Where(f => f.IsLiteral && !f.IsInitOnly && f.FieldType == typeof(string));

            var permissions = new List<string>();
            foreach (var field in fields)
            {
                var val = field.GetValue(null)?.ToString();
                if (!string.IsNullOrWhiteSpace(val))
                {
                    permissions.Add(val);
                }
            }

            if (permissions.Count > 0)
            {
                result[moduleName] = permissions;
            }
        }

        return result;
    }
}
