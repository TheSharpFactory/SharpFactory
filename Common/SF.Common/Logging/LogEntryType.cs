namespace SF.Common.Logging;

/// <summary>
/// Specifies the event type of an log entry
/// </summary>
public enum LogEntryType
{
    /// <summary>
    /// An error event. This indicates a significant problem the user should know about. Usually a loss of functionality or data.
    /// </summary>
    Error = 1,

    /// <summary>
    /// A warning event. This indicates a problem that is not immediately significant,
    ///     but that may signify conditions that could cause future problems.
    /// </summary>
    Warning = 2,

    /// <summary>
    ///  An information event. This indicates a significant, successful operation.
    /// </summary>
    Information = 4,

    /// <summary>
    ///  A success audit event. This indicates a security event that occurs when an audited
    ///     access attempt is successful; for example, logging on successfully.
    /// </summary>
    SuccessAudit = 8,

    /// <summary>
    ///  A failure audit event. This indicates a security event that occurs when an audited
    ///     access attempt fails; for example, a failed attempt to open a file.
    /// </summary>
    FailureAudit = 16
}
