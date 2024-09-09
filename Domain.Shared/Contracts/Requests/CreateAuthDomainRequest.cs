namespace SETiAuth.Domain.Shared.Contracts.Requests;

public class CreateAuthDomainRequest {
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string> Roles { get; set; } = [];
}