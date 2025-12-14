namespace TicketingASP.Models;

/// <summary>
/// View model for displaying error information
/// </summary>
public class ErrorViewModel
{
    public string? RequestId { get; set; }

    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}
