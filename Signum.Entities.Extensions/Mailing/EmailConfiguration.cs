using Signum.Entities.Basics;

namespace Signum.Entities.Mailing;

public class EmailConfigurationEmbedded : EmbeddedEntity
{
    
    public CultureInfoEntity DefaultCulture { get; set; }

    public string UrlLeft { get; set; }

    public bool SendEmails { get; set; }

    public bool ReciveEmails { get; set; }

    [StringLengthValidator(Min = 3, Max = 100), EMailValidator]
    public string? OverrideEmailAddress { get; set; }

    [Unit("hrs")]
    public double? AvoidSendingEmailsOlderThan { get; set; }

    public int ChunkSizeSendingEmails { get; set; } = 100;

    public int MaxEmailSendRetries { get; set; } = 3;

    [Unit("sec")]
    public int AsyncSenderPeriod { get; set; } = 5 * 60; //5 minutes
}
