using System;

namespace Expense.Core.Common.Exceptions
{
    public class AccessDeniedException : DomainException
    {
        public AccessDeniedException(string errorCode) : base(errorCode)
        {
        }
    }
}