using System.Collections.Generic;

namespace Mype.Shared.Models
{
    public class ValidationResult
    {
        public List<ValidationFailure> Errors { get; set; } = new();

        public bool IsValid => Errors.Count == 0;

        public ValidationResult()
        {
            Errors = [];
        }

        public ValidationResult(IDictionary<string, string[]> errors)
        {
            Errors = [];

            if (errors != null)
            {
                foreach (var error in errors)
                {
                    foreach (var message in error.Value)
                    {
                        Errors.Add(new ValidationFailure(error.Key, message));
                    }
                }
            }
        }

        public void AddError(string propertyName, string errorMessage)
        {
            Errors.Add(new ValidationFailure(propertyName, errorMessage));
        }
    }
}
