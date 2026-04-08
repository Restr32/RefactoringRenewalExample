namespace LegacyRenewalApp;

public class LegacyBilling : IBillingService {
    public void saveInvoce(RenewalInvoice voice) {
        LegacyBillingGateway.SaveInvoice(voice);
    }

    public void sendEmail(string mail, string name, string body) {
        LegacyBillingGateway.SendEmail(mail, name, body);
    }
}