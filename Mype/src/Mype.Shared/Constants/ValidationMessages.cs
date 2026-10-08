namespace Mype.Shared.Constants
{
    public static class ValidationMessages
    {
        public const string Required = "{PropertyName} es obligatorio.";

        public const string MaximumLength = "{PropertyName} no debe exceder {MaxLength} caracteres.";

        public const string MinimumLength = "{PropertyName} debe contener al menos {MinLength} caracteres.";

        public const string Invalid = "{PropertyName} no es válido.";

        public const string MustContainLetter = "{PropertyName} debe contener al menos una letra.";

        public const string MustContainNumber = "{PropertyName} debe contener al menos un número.";

        public const string MustMatch = "{PropertyName} debe coincidir con {ComparisonProperty}.";

        public const string PasswordsDoNotMatch = "La confirmación de contraseña no coincide con la contraseña.";

    }
}