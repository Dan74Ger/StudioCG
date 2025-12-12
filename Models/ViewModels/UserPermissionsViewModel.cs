namespace StudioCG.Web.Models.ViewModels
{
    public class UserPermissionsViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string NomeCompleto { get; set; } = string.Empty;
        public List<PermissionAssignmentViewModel> Permissions { get; set; } = new();
    }

    public class PermissionAssignmentViewModel
    {
        public int PermissionId { get; set; }
        public string PageName { get; set; } = string.Empty;
        public string PageUrl { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public bool CanView { get; set; }
        public bool CanEdit { get; set; }
        public bool CanCreate { get; set; }
        public bool CanDelete { get; set; }
    }
}

