using System;
using System.Linq;
using System.Threading.Tasks;
using Expense.Core.Application.ActivityLogs;
using Expense.Core.Application.Groups;
using Expense.Core.Application.Persistence;
using Expense.Core.Application.Notifications;
using Expense.Core.Domain.Entities;
using Expense.Core.Domain.Enums;
using Expense.Core.DTOs.Groups;
using Expense.Core.DTOs.Notifications;
using FluentValidation;

using Expense.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Expense.Core.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace Expense.Infrastructure.Groups
{
    public class GroupService : IGroupService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IActivityLogService _activityLogService;
        private readonly IRealtimeNotifier _notifier;
        private readonly IValidator<UpdateGroupMemberRolePatchDto> _patchValidator;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<GroupService> _logger;
        
        public GroupService(
            IUnitOfWork unitOfWork, 
            IActivityLogService activityLogService,
            IRealtimeNotifier notifier,
            IValidator<UpdateGroupMemberRolePatchDto> patchValidator,
            UserManager<ApplicationUser> userManager,
            ILogger<GroupService> logger)
        {
            _unitOfWork = unitOfWork;
            _activityLogService = activityLogService;
            _notifier = notifier;
            _patchValidator = patchValidator;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<GroupDto> CreateGroupAsync(string creatorUserId, CreateGroupDto dto, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Create group requested. CreatorUserId: {CreatorUserId}", creatorUserId);
            var inviteCode = Guid.NewGuid().ToString("N")[..8];
            var group = new Group
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                LogoUrl = dto.LogoUrl,
                CreatedByUserId = creatorUserId,
                InviteCode = inviteCode
            };
            
            _unitOfWork.Groups.Add(group);
            
            var member = new GroupMember
            {
                Group = group,
                UserId = creatorUserId,
                Role = GroupRole.Admin
            };
            
            _unitOfWork.Groups.AddMember(member);

            await _activityLogService.LogActivityAsync(group.Id, creatorUserId, ActivityType.Created, EntityType.Group, group.Id.ToString(), $"Group '{group.Name}' created", cancellationToken);

            await _unitOfWork.SaveAsync(cancellationToken);
            _logger.LogInformation("Group created. GroupId: {GroupId}, CreatorUserId: {CreatorUserId}", group.Id, creatorUserId);

            // Map directly from entity since we have all data in memory
            return new GroupDto
            {
                Id = group.Id,
                Name = group.Name,
                LogoUrl = group.LogoUrl,
                InviteCode = group.InviteCode,
                Members = new[] 
                { 
                    new GroupMemberDto 
                    { 
                        UserId = member.UserId, 
                        Role = member.Role.ToString() 
                    } 
                }
            };
        }

        public async Task<GroupDto?> GetGroupAsync(Guid groupId, string requesterUserId, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Group details requested. GroupId: {GroupId}, RequesterUserId: {RequesterUserId}", groupId, requesterUserId);
            var isMember = await _unitOfWork.Groups.IsMemberAsync(groupId, requesterUserId, cancellationToken);
                
            if (!isMember) return null;

            var group = await _unitOfWork.Groups.GetWithMembersAsync(groupId, cancellationToken);

            if (group == null) return null;

            return new GroupDto
            {
                Id = group.Id,
                Name = group.Name,
                LogoUrl = group.LogoUrl,
                InviteCode = group.InviteCode,
                Members = group.Members.Select(m => new GroupMemberDto
                {
                    UserId = m.UserId,
                    Role = m.Role.ToString()
                })
            };
        }

        public async Task<bool> JoinGroupAsync(string userId, string inviteCode, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Join group requested. UserId: {UserId}, InviteCodeLength: {InviteCodeLength}", userId, inviteCode?.Length ?? 0);
            var group = await _unitOfWork.Groups.GetByInviteCodeAsync(inviteCode, cancellationToken);
                
            if (group == null) return false;

            var existingMember = await _unitOfWork.Groups.IsMemberAsync(group.Id, userId, cancellationToken);
                
            if (existingMember) return true;

            var member = new GroupMember
            {
                GroupId = group.Id,
                UserId = userId,
                Role = GroupRole.Member
            };
            
            _unitOfWork.Groups.AddMember(member);

            await _activityLogService.LogActivityAsync(group.Id, userId, ActivityType.Joined, EntityType.Group, group.Id.ToString(), null, cancellationToken);

            await _unitOfWork.SaveAsync(cancellationToken);
            _logger.LogInformation("User joined group. GroupId: {GroupId}, UserId: {UserId}", group.Id, userId);
            
            await NotifyGroupMembersAsync(group.Id, userId, "Member_Joined", new { UserId = userId }, cancellationToken);

            return true;
        }

        public async Task<bool> AddMemberByEmailAsync(Guid groupId, string requesterUserId, AddGroupMemberDto dto, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Add group member requested. GroupId: {GroupId}, RequesterUserId: {RequesterUserId}", groupId, requesterUserId);
            var isRequesterMember = await _unitOfWork.Groups.IsMemberAsync(groupId, requesterUserId, cancellationToken);
            if (!isRequesterMember) throw new GroupAccessDeniedException("Group_NotMember");

            var user = await _userManager.FindByEmailAsync(dto.Email);
            if (user == null)
            {
                throw new NotFoundException("User_NotFound"); 
            }

            var isTargetMember = await _unitOfWork.Groups.IsMemberAsync(groupId, user.Id.ToString(), cancellationToken);
            if (isTargetMember)
            {
                throw new BusinessException("Group_UserAlreadyMember");
            }

            var member = new GroupMember
            {
                GroupId = groupId,
                UserId = user.Id.ToString(),
                Role = GroupRole.Member
            };

            _unitOfWork.Groups.AddMember(member);
            
            await _activityLogService.LogActivityAsync(groupId, requesterUserId, ActivityType.Updated, EntityType.Group, groupId.ToString(), $"Added user {user.Email} to group", cancellationToken);
            
            await _unitOfWork.SaveAsync(cancellationToken);
            _logger.LogInformation("Group member added. GroupId: {GroupId}, AddedUserId: {AddedUserId}, RequestedBy: {RequesterUserId}", groupId, user.Id, requesterUserId);

            await NotifyGroupMembersAsync(groupId, requesterUserId, "Member_Added", new { UserId = user.Id, Email = user.Email }, cancellationToken);
            
            await _notifier.NotifyUserAsync(user.Id.ToString(), new NotificationMessage 
            { 
                Type = "Group_Added", 
                GroupId = groupId, 
                ActorUserId = requesterUserId,
                Payload = new { GroupId = groupId } 
            }, cancellationToken);

            return true;
        }

        private async Task NotifyGroupMembersAsync(Guid groupId, string actorUserId, string type, object payload, CancellationToken cancellationToken)
        {
            try
            {
                var members = await _unitOfWork.Repository<GroupMember>().GetAll(m => m.GroupId == groupId);
                var userIds = members.Select(m => m.UserId).Where(u => u != actorUserId).Distinct().ToList();

                if (userIds.Any())
                {
                    var message = new NotificationMessage
                    {
                        Type = type,
                        GroupId = groupId,
                        ActorUserId = actorUserId,
                        Payload = payload,
                        Timestamp = DateTime.UtcNow
                    };
                    await _notifier.NotifyUsersAsync(userIds, message, cancellationToken);
                }
            }
            catch
            {
                _logger.LogWarning("Group notification dispatch skipped due to notifier failure. GroupId: {GroupId}, ActorUserId: {ActorUserId}, Type: {NotificationType}",
                    groupId, actorUserId, type);
                // Fire and forget
            }
        }

        public async Task<IEnumerable<GroupDto>> GetUserGroupsAsync(string userId, CancellationToken cancellationToken)
        {
            _logger.LogDebug("User groups requested. UserId: {UserId}", userId);
            var groups = await _unitOfWork.Groups.GetGroupsForUserAsync(userId, cancellationToken);
            return groups.Select(g => new GroupDto
            {
                Id = g.Id,
                Name = g.Name,
                InviteCode = g.InviteCode,
                Members = g.Members.Select(m => new GroupMemberDto
                {
                    UserId = m.UserId,
                    Role = m.Role.ToString()
                })
            });
        }

        public async Task<bool> UpdateMemberRoleAsync(Guid groupId, string requesterUserId, string targetUserId, string newRole, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Update member role requested. GroupId: {GroupId}, RequesterUserId: {RequesterUserId}, TargetUserId: {TargetUserId}", groupId, requesterUserId, targetUserId);
            var requester = await _unitOfWork.Groups.GetMemberAsync(groupId, requesterUserId, cancellationToken);
            if (requester == null || requester.Role != GroupRole.Admin) return false;

            var target = await _unitOfWork.Groups.GetMemberAsync(groupId, targetUserId, cancellationToken);
            if (target == null) return false;

            if (Enum.TryParse<GroupRole>(newRole, true, out var roleEnum))
            {
                target.Role = roleEnum;
                _unitOfWork.Groups.UpdateMember(target);

                await _activityLogService.LogActivityAsync(groupId, targetUserId, ActivityType.Updated, EntityType.Member, targetUserId, $"Role updated to {newRole} by {requesterUserId}", cancellationToken);

                await _unitOfWork.SaveAsync(cancellationToken);
                _logger.LogInformation("Member role updated. GroupId: {GroupId}, TargetUserId: {TargetUserId}, NewRole: {NewRole}, RequestedBy: {RequesterUserId}",
                    groupId, targetUserId, newRole, requesterUserId);

                return true;
            }
            return false;
        }

        public async Task<bool> UpdateMemberRolePartialAsync(Guid groupId, string requesterUserId, string targetUserId, UpdateGroupMemberRolePatchDto dto, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Partial member role update requested. GroupId: {GroupId}, RequesterUserId: {RequesterUserId}, TargetUserId: {TargetUserId}",
                groupId, requesterUserId, targetUserId);
            await _patchValidator.ValidateAndThrowAsync(dto, cancellationToken);

            var requester = await _unitOfWork.Groups.GetMemberAsync(groupId, requesterUserId, cancellationToken);
            if (requester == null || requester.Role != GroupRole.Admin) return false;

            var target = await _unitOfWork.Groups.GetMemberAsync(groupId, targetUserId, cancellationToken);
            if (target == null) return false;

            if (dto.Role != null)
            {
                if (Enum.TryParse<GroupRole>(dto.Role, true, out var roleEnum))
                {
                    if (target.Role != roleEnum)
                    {
                        target.Role = roleEnum;
                        _unitOfWork.Groups.UpdateMember(target);

                        await _activityLogService.LogActivityAsync(groupId, targetUserId, ActivityType.Updated, EntityType.Member, targetUserId, $"Role updated to {dto.Role} by {requesterUserId}", cancellationToken);

                        await _unitOfWork.SaveAsync(cancellationToken);
                        _logger.LogInformation("Member role partially updated. GroupId: {GroupId}, TargetUserId: {TargetUserId}, NewRole: {NewRole}, RequestedBy: {RequesterUserId}",
                            groupId, targetUserId, dto.Role, requesterUserId);
                        return true;
                    }
                }
            }
            return false;
        }

        public async Task<bool> RemoveMemberAsync(Guid groupId, string requesterUserId, string targetUserId, CancellationToken cancellationToken)
        {
            _logger.LogInformation("Remove member requested. GroupId: {GroupId}, RequesterUserId: {RequesterUserId}, TargetUserId: {TargetUserId}",
                groupId, requesterUserId, targetUserId);
            var requester = await _unitOfWork.Groups.GetMemberAsync(groupId, requesterUserId, cancellationToken);
            if (requester == null) return false;

            var target = await _unitOfWork.Groups.GetMemberAsync(groupId, targetUserId, cancellationToken);
            if (target == null) return false;

            // Admin can remove anyone; User can remove themselves
            if (requester.Role != GroupRole.Admin && requesterUserId != targetUserId)
            {
                return false;
            }

            _unitOfWork.Groups.RemoveMember(target);

            var activityType = requesterUserId == targetUserId ? ActivityType.Left : ActivityType.Removed;
            var details = activityType == ActivityType.Removed ? $"Removed by admin {requesterUserId}" : null;

            await _activityLogService.LogActivityAsync(groupId, targetUserId, activityType, EntityType.Group, groupId.ToString(), details, cancellationToken);

            await _unitOfWork.SaveAsync(cancellationToken);
            _logger.LogInformation("Member removed from group. GroupId: {GroupId}, TargetUserId: {TargetUserId}, RequestedBy: {RequesterUserId}, ActivityType: {ActivityType}",
                groupId, targetUserId, requesterUserId, activityType);

            return true;
        }
    }
}
