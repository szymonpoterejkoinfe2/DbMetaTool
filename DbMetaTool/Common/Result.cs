namespace DbMetaTool.Common
{
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public string Error { get; }

        protected Result(bool success, string error)
        {
            IsSuccess = success;
            Error = error;
        }

        public static Result Success() => new(true, string.Empty);
        public static Result Fail(string error) => new(false, error);
    }

    public class Result<T> : Result
    {
        public T Value { get; }

        private Result(T value) : base(true, string.Empty)
        {
            Value = value;
        }

        private Result(string error) : base(false, error)
        {
            Value = default!;
        }

        public static Result<T> Success(T value) => new(value);
        public static Result<T> Fail(string error) => new(error);
    }

}
