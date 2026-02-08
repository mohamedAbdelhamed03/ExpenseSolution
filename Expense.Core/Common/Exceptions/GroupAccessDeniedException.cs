namespace Expense.Core.Common.Exceptions
{
    public class GroupAccessDeniedException : AccessDeniedException
    {
        public GroupAccessDeniedException(string errorCode = "Group.AccessDenied") : base(errorCode)
        {
        }
    }
}