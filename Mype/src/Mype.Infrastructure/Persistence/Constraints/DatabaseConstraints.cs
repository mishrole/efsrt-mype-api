namespace Mype.Infrastructure.Persistence.Constraints
{
    public static class DatabaseConstraints
    {
        public static class Users
        {
            public const string NormalizedEmail = "ux_users_normalized_email";
        }

        public static class Currencies
        {
            public const string Code = "ux_currencies_code";
        }

        public static class BusinessRoles
        {
            public const string Code = "ux_business_roles_code";
        }

        public static class Businesses
        {
            public const string Currency = "fk_businesses_currencies_currency_id";
            public const string CreatedByUser = "fk_businesses_users_created_by_user_id";
            public const string UpdatedByUser = "fk_businesses_users_updated_by_user_id";
            public const string Ruc = "ux_businesses_ruc";
        }

        public static class BusinessMemberships
        {
            public const string BusinessUser = "ux_business_memberships_business_user";
            public const string Business = "fk_business_memberships_businesses_business_id";
            public const string User = "fk_business_memberships_users_user_id";
            public const string Role = "fk_business_memberships_business_roles_role_id";
            public const string CreatedByUser = "fk_business_memberships_users_created_by_user_id";
            public const string UpdatedByUser = "fk_business_memberships_users_updated_by_user_id";
        }

        public static class Categories
        {
            public const string BusinessTypeName = "ux_categories_business_type_name";
            public const string IdBusiness = "ux_categories_id_business";
            public const string Business = "fk_categories_businesses_business_id";
            public const string CreatedByUser = "fk_categories_users_created_by_user_id";
            public const string UpdatedByUser = "fk_categories_users_updated_by_user_id";
        }

        public static class Products
        {
            public const string BusinessName = "ux_products_business_name";

            public const string BusinessActiveName = "ix_products_business_active_name";

            public const string BusinessCategoryActive = "ix_products_business_category_active";

            public const string Business = "fk_products_businesses_business_id";

            public const string CategoryBusiness = "fk_products_categories_category_id_business_id";

            public const string CreatedByUser = "fk_products_users_created_by_user_id";

            public const string UpdatedByUser = "fk_products_users_updated_by_user_id";
        }

        public static class FinancialMovements
        {
            public const string IdBusiness = "ux_financial_movements_id_business";
            public const string BusinessStatusUpdatedAt =
                "ix_financial_movements_business_status_updated_at";
            public const string BusinessTypeDate = "ix_financial_movements_business_type_date";
            public const string Business = "fk_financial_movements_businesses_business_id";
            public const string CreatedByUser = "fk_financial_movements_users_created_by_user_id";
            public const string UpdatedByUser = "fk_financial_movements_users_updated_by_user_id";
            public const string ConfirmedByUser =
                "fk_financial_movements_users_confirmed_by_user_id";
            public const string CancelledByUser =
                "fk_financial_movements_users_cancelled_by_user_id";
        }

        public static class Permissions
        {
            public const string Code = "ux_permissions_code";
        }

        public static class BusinessRolePermissions
        {
            public const string BusinessRole =
                "fk_business_role_permissions_business_roles_business_role_id";

            public const string Permission =
                "fk_business_role_permissions_permissions_permission_id";

            public const string PermissionIndex = "ix_business_role_permissions_permission_id";
        }
    }
}
