using ClientFacingBot.Common;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using Newtonsoft.Json.Linq;
using System;
using System.Threading.Tasks;
using System.Web.Http;

namespace ClientFacingBot.Dialogs
{
    [Serializable]
    [BotAuthentication]
    [Authorize]
    public class EscalateDialog : IDialog<object>
    {
        public async Task StartAsync(IDialogContext context)
        {
            await context.PostAsync(Utilities.RandomPhrase(Common.Dialogs.escalateDialog));

            IMessageActivity transferMsg = context.MakeMessage();
            JObject transferChannelData = JObject.Parse(@"{'type':'transfer','skill':'BotEscalation'}");
            transferMsg.ChannelData = transferChannelData;
            transferMsg.Text = string.Empty;
            transferMsg.Type = ActivityTypes.Message;
            await context.PostAsync(transferMsg);
            context.Wait(MessageRecievedAsync);
        }

        public async Task MessageRecievedAsync(IDialogContext context, IAwaitable<object> result)
        {
        }
        private async Task AfterChildDialogIsDone(IDialogContext context, IAwaitable<IMessageActivity> result)
        {
            context.Done<object>(new object());
        }
    }
}