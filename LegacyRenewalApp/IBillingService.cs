namespace LegacyRenewalApp;

public interface IBillingService {
    void saveInvoce(RenewalInvoice voice);
    void sendEmail(string mail, string name, string plan);
}