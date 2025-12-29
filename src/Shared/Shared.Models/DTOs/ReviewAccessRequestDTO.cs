namespace Shared.Models.DTOs;

/// <summary>
/// DTO for reviewing an access request (approve or deny)
/// </summary>
public class ReviewAccessRequestDTO
{
    public Guid RequestId { get; set; }
    public bool Approved { get; set; }
    public string ReviewComments { get; set; } = string.Empty;
    public Guid ReviewedBy { get; set; }
}

