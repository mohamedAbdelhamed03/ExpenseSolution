using System;

namespace Expense.Core.Common.Exceptions
{
    public class NotFoundException : DomainException
    {
        public NotFoundException(string errorCode) : base(errorCode)
        {
        }
    }
}