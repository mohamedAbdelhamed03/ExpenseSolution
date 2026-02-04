namespace Expense.Core.Common.Exceptions
{
    public class GroupAccessDeniedException : AccessDeniedException
    {
        public GroupAccessDeniedException() : base("Group.AccessDenied")
        {
        }
    }
}