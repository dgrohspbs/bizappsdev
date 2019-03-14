using System;
using System.Linq;

namespace ClientFacingBot.Common
{
    public class Dialogs
    {
        public static string[] introAuthenticatedDialog = { "How can I help you?", "How can I assist you today?" };
        public static string[] introAnonymousDialog = { "Hello, in order for us to best serve you, can you provide your email address?", "Please provide your email address so we can serve you better." };
        public static string[] categoryDialog = { "Please select a Category regarding your concern.", "Can you identify below the Category of your concern?" };
        public static string[] printerDialog = { "Thank you! Can you provide the Printer model?", "Got it. Can you also give the product code?" };
        public static string[] foundArticleDialog = { "I have found a Knowledge Article that might help you. Kindly copy this link to your browser to see the steps.", "I have found this FAQ for your reference. Please copy this link to your browser." };
        public static string[] articleDialog = { "Did this solve your problem?", "Did this help you?" };
        public static string[] articleUsefulDialog = { "That’s great! Glad to help!", "Thanks for your feedback. Happy to assist!" };
        public static string[] articleNotUsefulDialog = { "Would you like to talk to a Customer Service Representative?", "Do you want to talk to a Live Agent?" };
        public static string[] escalateDialog = { "Please wait for a moment. Our Customer Service Representative will be with you shortly.", "Thank you, I will no forward your concern to a Live Agent." };
        public static string[] dontEscalateDialog = { "Please feel free to reach out anytime if you want to get in touch with us.", "I hope I can be of help to you in the future. You can reach out anytime." };
    }
}