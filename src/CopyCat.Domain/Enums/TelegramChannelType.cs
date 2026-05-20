namespace CopyCat.Domain.Enums;

/// <summary>
/// Represents the type of a Telegram chat or channel.
/// </summary>
public enum TelegramChannelType
{
    /// <summary>A broadcast-only channel.</summary>
    BroadcastChannel,

    /// <summary>A supergroup.</summary>
    MegaGroup,

    /// <summary>A regular group chat.</summary>
    Group,

    /// <summary>A private chat.</summary>
    PrivateChat,

    /// <summary>A direct conversation with a user.</summary>
    User,
}
