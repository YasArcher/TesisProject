using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tesisproject.shared.Wrappers
{
    public class ApiResult
    {
        public bool Succeeded { get; set; }
        public string? Error { get; set; }

        public static ApiResult Success() => new() { Succeeded = true };
        public static ApiResult Fail(string error) => new() { Succeeded = false, Error = error };
    }

    public class ApiResult<T>
    {
        public bool Succeeded { get; set; }
        public T? Value { get; set; }
        public string? Error { get; set; }

        public static ApiResult<T> Success(T value) => new()
        {
            Succeeded = true,
            Value = value
        };

        public static ApiResult<T> Fail(string error) => new()
        {
            Succeeded = false,
            Error = error
        };
    }
}