using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Internals;
using Microsoft.Bot.Builder.Internals.Fibers;
using Microsoft.Bot.Builder.Scorables.Internals;
using Microsoft.Bot.Connector;
using System.Threading;
using System.Threading.Tasks;

namespace ClientFacingBot.Dialogs
{
    public class EscalateScorable : ScorableBase<IActivity, string, double>
    {
        private readonly IDialogTask task;

        public EscalateScorable(IDialogTask task)
        {
            SetField.NotNull(out this.task, nameof(task), task);
        }

        protected override async Task<string> PrepareAsync(IActivity activity, CancellationToken token)
        {
            var message = activity as IMessageActivity;
            if (message != null && !string.IsNullOrWhiteSpace(message.Text))
            {
                string reply = message.Text.ToLower();

                if (reply.ToLower().Contains("speak") || reply.ToLower().Contains("escalate") || reply.ToLower().Contains("transfer") || reply.ToLower().Contains("consult") &&
                    reply.ToLower().Contains("live agent") || reply.ToLower().Contains("csr") || reply.ToLower().Contains("customer service representative") || reply.ToLower().Contains("agent")
                    || reply.ToLower().Contains("operator") || reply.ToLower().Contains("representative"))
                {
                    return message.Text;
                }
            }

            return null;
        }

        protected override bool HasScore(IActivity item, string state)
        {
            return state != null;
        }

        protected override double GetScore(IActivity item, string state)
        {
            return 1.0;
        }

        protected override async Task PostAsync(IActivity item, string state, CancellationToken token)
        {
            var message = item as IMessageActivity;

            if (message != null)
            {
                this.task.Reset();
                var escalateDialog = new EscalateDialog();

                var interruption = escalateDialog.Void<object, IMessageActivity>();
                this.task.Call(interruption, null);

                await this.task.PollAsync(token);
            }

        }
        protected override Task DoneAsync(IActivity item, string state, CancellationToken token)
        {
            return Task.CompletedTask;
        }
    }
}