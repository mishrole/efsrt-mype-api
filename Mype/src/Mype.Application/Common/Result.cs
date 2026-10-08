namespace Mype.Application.Common
{
    #nullable enable
    public class Result<T>
    {
        public bool IsSuccess { get; set; }
        public T? Value { get; set; }
        public ApplicationError? Error { get; set; }

        private Result(T? value)
        {
            IsSuccess = true;
            Value = value;
        }

        private Result(ApplicationError error)
        {
            IsSuccess = false;
            Error = error;
        }

        public static Result<T> Success(T value) => new(value);

        public static Result<T> Failure(ApplicationError error) => new(error);
    }
}
