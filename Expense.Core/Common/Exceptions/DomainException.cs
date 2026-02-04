using System;

namespace Expense.Core.Common.Exceptions
{
    public abstract class DomainException : Exception
    {
        public string ErrorCode { get; }

        protected DomainException(string errorCode) : base($"Domain Exception: {errorCode}")
        {
            ErrorCode = errorCode;
        }

        protected DomainException(string errorCode, Exception innerException) : base($"Domain Exception: {errorCode}", innerException)
        {
            ErrorCode = errorCode;
        }
    }
}