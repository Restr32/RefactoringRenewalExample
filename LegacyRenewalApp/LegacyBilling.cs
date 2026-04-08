namespace LegacyRenewalApp;

public class LegacyBilling : IBillingService {
    public void saveInvoce(RenewalInvoice voice) {
        LegacyBillingGateway.SaveInvoice(voice);
    }

    public void sendEmail(string mail, string name, string plan) {
        var subject = "Generated information!";
        var body = $"Morning {name}, We are happy to announce that ur plan: {plan} is ready!";
        LegacyBillingGateway.SendEmail(mail, subject, body);
    }
}