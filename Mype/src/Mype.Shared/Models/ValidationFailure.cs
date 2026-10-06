namespace Mype.Shared.Models
{
    public class ValidationFailure
    {
        public string PropertyName { get; set; }
        public string ErrorMessage { get; set; }

        public ValidationFailure(string propertyName, string errorMessage)
        {
            PropertyName = propertyName;
            ErrorMessage = errorMessage;
        }

        public ValidationFailure()
        {
            PropertyName = string.Empty;
            ErrorMessage = string.Empty;
        }
    }
}
