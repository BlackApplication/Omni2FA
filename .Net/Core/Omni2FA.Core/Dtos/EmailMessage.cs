namespace Omni2FA.Core.Dtos;

/// <summary>A composed email ready for transport. Built by <c>IEmailMessageBuilder</c>, sent by <c>IEmailSender</c>.</summary>
public class EmailMessage {
    /// <summary>Destination address.</summary>
    public required string To { get; init; }

    /// <summary>Subject line.</summary>
    public required string Subject { get; init; }

    /// <summary>HTML body.</summary>
    public required string HtmlBody { get; init; }

    /// <summary>Plain-text body, for clients that don't render HTML.</summary>
    public required string TextBody { get; init; }
}
