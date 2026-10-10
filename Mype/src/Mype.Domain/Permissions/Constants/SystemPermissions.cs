using System;
using System.Collections.ObjectModel;

namespace Mype.Domain.Permissions.Constants
{
    public static class SystemPermissions
    {
        public static readonly SystemPermissionDefinition BusinessRead = Create(
            1,
            "BUSINESS_READ",
            "Consultar negocio",
            "Permite consultar la información del negocio."
        );

        public static readonly SystemPermissionDefinition BusinessUpdate = Create(
            2,
            "BUSINESS_UPDATE",
            "Actualizar negocio",
            "Permite actualizar la información y la moneda del negocio."
        );

        public static readonly SystemPermissionDefinition BusinessDeactivate = Create(
            3,
            "BUSINESS_DEACTIVATE",
            "Desactivar negocio",
            "Permite desactivar el negocio."
        );

        public static readonly SystemPermissionDefinition BusinessReactivate = Create(
            4,
            "BUSINESS_REACTIVATE",
            "Reactivar negocio",
            "Permite reactivar el negocio."
        );

        public static readonly SystemPermissionDefinition CategoryRead = Create(
            5,
            "CATEGORY_READ",
            "Consultar categorías",
            "Permite consultar las categorías del negocio."
        );

        public static readonly SystemPermissionDefinition CategoryCreate = Create(
            6,
            "CATEGORY_CREATE",
            "Crear categorías",
            "Permite crear categorías para el negocio."
        );

        public static readonly SystemPermissionDefinition CategoryUpdate = Create(
            7,
            "CATEGORY_UPDATE",
            "Actualizar categorías",
            "Permite actualizar las categorías del negocio."
        );

        public static readonly SystemPermissionDefinition CategoryDeactivate = Create(
            8,
            "CATEGORY_DEACTIVATE",
            "Desactivar categorías",
            "Permite desactivar categorías del negocio."
        );

        public static readonly SystemPermissionDefinition CategoryReactivate = Create(
            9,
            "CATEGORY_REACTIVATE",
            "Reactivar categorías",
            "Permite reactivar categorías del negocio."
        );

        public static readonly SystemPermissionDefinition ProductRead = Create(
            10,
            "PRODUCT_READ",
            "Consultar productos",
            "Permite consultar los productos del negocio."
        );

        public static readonly SystemPermissionDefinition ProductCreate = Create(
            11,
            "PRODUCT_CREATE",
            "Crear productos",
            "Permite crear productos para el negocio."
        );

        public static readonly SystemPermissionDefinition ProductUpdate = Create(
            12,
            "PRODUCT_UPDATE",
            "Actualizar productos",
            "Permite actualizar los productos del negocio."
        );

        public static readonly SystemPermissionDefinition ProductDeactivate = Create(
            13,
            "PRODUCT_DEACTIVATE",
            "Desactivar productos",
            "Permite desactivar productos del negocio."
        );

        public static readonly SystemPermissionDefinition ProductReactivate = Create(
            14,
            "PRODUCT_REACTIVATE",
            "Reactivar productos",
            "Permite reactivar productos del negocio."
        );

        public static readonly SystemPermissionDefinition MovementRead = Create(
            15,
            "MOVEMENT_READ",
            "Consultar movimientos operativos",
            "Permite consultar movimientos operativos y borradores del negocio."
        );

        public static readonly SystemPermissionDefinition MovementCreate = Create(
            16,
            "MOVEMENT_CREATE",
            "Crear movimientos",
            "Permite crear movimientos financieros en borrador."
        );

        public static readonly SystemPermissionDefinition MovementUpdate = Create(
            17,
            "MOVEMENT_UPDATE",
            "Actualizar movimientos",
            "Permite actualizar borradores, ítems y evidencias de movimientos."
        );

        public static readonly SystemPermissionDefinition MovementConfirm = Create(
            18,
            "MOVEMENT_CONFIRM",
            "Confirmar movimientos",
            "Permite confirmar movimientos financieros en borrador."
        );

        public static readonly SystemPermissionDefinition MovementDiscard = Create(
            19,
            "MOVEMENT_DISCARD",
            "Descartar movimientos",
            "Permite descartar movimientos financieros en borrador."
        );

        public static readonly SystemPermissionDefinition MovementCancel = Create(
            20,
            "MOVEMENT_CANCEL",
            "Anular movimientos",
            "Permite anular movimientos financieros confirmados."
        );

        public static readonly SystemPermissionDefinition HistoryRead = Create(
            21,
            "HISTORY_READ",
            "Consultar historial financiero",
            "Permite consultar y filtrar el historial financiero del negocio."
        );

        public static readonly SystemPermissionDefinition DashboardRead = Create(
            22,
            "DASHBOARD_READ",
            "Consultar dashboard financiero",
            "Permite consultar los indicadores financieros del negocio."
        );

        public static readonly ReadOnlyCollection<SystemPermissionDefinition> All =
            Array.AsReadOnly(
                new[]
                {
                    BusinessRead,
                    BusinessUpdate,
                    BusinessDeactivate,
                    BusinessReactivate,
                    CategoryRead,
                    CategoryCreate,
                    CategoryUpdate,
                    CategoryDeactivate,
                    CategoryReactivate,
                    ProductRead,
                    ProductCreate,
                    ProductUpdate,
                    ProductDeactivate,
                    ProductReactivate,
                    MovementRead,
                    MovementCreate,
                    MovementUpdate,
                    MovementConfirm,
                    MovementDiscard,
                    MovementCancel,
                    HistoryRead,
                    DashboardRead,
                }
            );

        public static readonly ReadOnlyCollection<SystemPermissionDefinition> Owner =
            Array.AsReadOnly(
                new[]
                {
                    BusinessRead,
                    BusinessUpdate,
                    BusinessDeactivate,
                    BusinessReactivate,
                    CategoryRead,
                    CategoryCreate,
                    CategoryUpdate,
                    CategoryDeactivate,
                    CategoryReactivate,
                    ProductRead,
                    ProductCreate,
                    ProductUpdate,
                    ProductDeactivate,
                    ProductReactivate,
                    MovementRead,
                    MovementCreate,
                    MovementUpdate,
                    MovementConfirm,
                    MovementDiscard,
                    MovementCancel,
                    HistoryRead,
                    DashboardRead,
                }
            );

        public static readonly ReadOnlyCollection<SystemPermissionDefinition> Collaborator =
            Array.AsReadOnly(Array.Empty<SystemPermissionDefinition>());

        private static SystemPermissionDefinition Create(
            int sequence,
            string code,
            string name,
            string description
        )
        {
            return new SystemPermissionDefinition(
                Guid.Parse($"30000000-0000-0000-0000-{sequence:D12}"),
                code,
                name,
                description
            );
        }
    }
}
