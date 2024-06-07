namespace App.Domain.Enum
{
    public class ServerSettingEnum
    {
        public const string AppSmtpenabled = "app.smtpenabled";
        public const string AppSmtpUsername = "app.smtpusername";
        public const string AppSmtppassword = "app.smtppassword";
        public const string AppSmtpServer = "app.smtpserver";
        public const string AppAllowEmailClient = "app.allowemailclient";
        public const string AppAllowEmailShop = "app.allowemailshop";
        public const string AppStripeKey = "app.stripekey";
        public const string AppStripeWebHookKey = "app.stripe.webhookkey";
        public const string AppPublicKey = "app.publickey";

        public const string AppPrintName = "app.printer.name";
        public const string AppPrinterReceiptWidth = "app.printer.receiptwidth";
        public const string AppPrinterXflow = "app.printer.xflow";
        public const string AppPrinterYflow = "app.printer.yflow";
        public const string AppPrinterFontName = "app.printer.fontname";
        public const string AppPrinterLargeFontSize = "app.printer.largefontsize";
        public const string AppPrinterMediumFontSize = "app.printer.mediumfontsize";
        public const string AppPrinterSmallFontSize = "app.printer.smallfontsize";
        public const string AppPrinterPrintLanguage = "app.printer.PrintLanguage";

        public const string AppPrinterCashTillCommand = "app.printer.cashtillcommand";
        public const string AppUserTranslateInternal = "app.usertranslateinternal";


        public const string AppSmsAccount = "app.smsaccount";
        public const string AppSmsToken = "app.smstoken";
        public const string AppSmsFromNumber = "app.smsfromnumber";
        public const string AppEmailApiKey = "app.smtpapikey";
    }
}