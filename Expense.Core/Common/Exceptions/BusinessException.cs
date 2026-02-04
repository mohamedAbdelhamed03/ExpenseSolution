using System;

namespace Expense.Core.Common.Exceptions
{
    public class BusinessException : DomainException
    {
        public BusinessException(string errorCode) : base(errorCode)
        {
        }

        public BusinessException(string errorCode, Exception innerException) : base(errorCode, innerException)
        {
        }
    }
}
