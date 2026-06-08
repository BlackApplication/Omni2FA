using System.Threading.Channels;
using Omni2FA.Core.Dtos;

namespace Omni2FA.AspNetCore.Email;

/// <summary>
/// Singleton in-process queue of pending OTP emails, drained by <see cref="EmailDispatchBackgroundService"/>.
/// Unbounded — OTP volume is low and bounded upstream by rate limits and resend cooldowns.
/// </summary>
public sealed class EmailDispatchChannel {
    private readonly Channel<EmailMessage> _channel = Channel.CreateUnbounded<EmailMessage>(new UnboundedChannelOptions {
        SingleReader = true,
    });

    public ChannelWriter<EmailMessage> Writer => _channel.Writer;

    public ChannelReader<EmailMessage> Reader => _channel.Reader;
}
