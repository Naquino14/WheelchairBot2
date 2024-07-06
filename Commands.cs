using Discord.Commands;

namespace WheelchairBot;

public static class Commands
{
    public static async Task Say(CommandContext context, string[] args)
    {
        await context.Channel.SendMessageAsync(string.Join(" ", args.Skip(1)));
    }

    public static readonly Func<CommandContext, string[], Task> Say2 = async (context, args) =>
    {
        await context.Channel.SendMessageAsync(string.Join(" ", args.Skip(1)));
    };
}
