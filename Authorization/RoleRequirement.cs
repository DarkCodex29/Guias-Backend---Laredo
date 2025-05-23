using Microsoft.AspNetCore.Authorization;

namespace GuiasBackend.Authorization
{
    public class RoleRequirement : IAuthorizationRequirement
    {
        public string Role { get; }
        
        public RoleRequirement(string role)
        {
            Role = role;
        }
    }

    public class RoleHandler : AuthorizationHandler<RoleRequirement>
    {
        protected override Task HandleRequirementAsync(
            AuthorizationHandlerContext context,
            RoleRequirement requirement)
        {
            if (context.User.IsInRole(requirement.Role))
            {
                context.Succeed(requirement);
            }
            
            return Task.CompletedTask;
        }
    }
}
