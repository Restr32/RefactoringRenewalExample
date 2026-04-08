using System;

namespace LegacyRenewalApp
{
    public class SubscriptionRenewalService {
        private CustomerRepository customerRepository;
        private SubscriptionPlanRepository planRepository;
        private Discount dis;
        private IBillingService bill;
        public SubscriptionRenewalService() {
            customerRepository = new CustomerRepository();
            planRepository = new SubscriptionPlanRepository();
            dis = new Discount(); 
            bill = new LegacyBilling();
        }
        public RenewalInvoice CreateRenewalInvoice(
            int customerId,
            string planCode,
            int seatCount,
            string paymentMethod,
            bool includePremiumSupport,
            bool useLoyaltyPoints)
        {
            if (customerId <= 0)
            {
                throw new ArgumentException("Customer id must be positive");
            }

            if (string.IsNullOrWhiteSpace(planCode))
            {
                throw new ArgumentException("Plan code is required");
            }

            if (seatCount <= 0)
            {
                throw new ArgumentException("Seat count must be positive");
            }

            if (string.IsNullOrWhiteSpace(paymentMethod))
            {
                throw new ArgumentException("Payment method is required");
            }

            string normalizedPlanCode = planCode.Trim().ToUpperInvariant();
            string normalizedPaymentMethod = paymentMethod.Trim().ToUpperInvariant();
            
            var customer = customerRepository.GetById(customerId);
            var plan = planRepository.GetByCode(normalizedPlanCode);

            if (!customer.IsActive)
            {
                throw new InvalidOperationException("Inactive customers cannot renew subscriptions");
            }

            decimal baseAmount = (plan.MonthlyPricePerSeat * seatCount * 12m) + plan.SetupFee;
            
            decimal discountAmount = 0m;
            string notes = string.Empty;
            decimal subtotalAfterDiscount = 0m;
            (discountAmount, notes, subtotalAfterDiscount) = dis.calculate(customer, plan, baseAmount, seatCount, useLoyaltyPoints);
            
            
            decimal supportFee = includePremiumSupport ? GetFee(normalizedPlanCode) : 0m;
            if (includePremiumSupport)
                notes += "premium support included; ";
            
            notes += GetPaymentString(normalizedPaymentMethod);
            decimal paymentFee = (subtotalAfterDiscount + supportFee) * GetPayment(normalizedPaymentMethod);


            decimal taxRate = GetTax(customer.Country);

            decimal taxBase = subtotalAfterDiscount + supportFee + paymentFee;
            decimal taxAmount = taxBase * taxRate;
            decimal finalAmount = taxBase + taxAmount;

            if (finalAmount < 500m)
            {
                finalAmount = 500m;
                notes += "minimum invoice amount applied; ";
            }

            var invoice = new RenewalInvoice
            {
                InvoiceNumber = $"INV-{DateTime.UtcNow:yyyyMMdd}-{customerId}-{normalizedPlanCode}",
                CustomerName = customer.FullName,
                PlanCode = normalizedPlanCode,
                PaymentMethod = normalizedPaymentMethod,
                SeatCount = seatCount,
                BaseAmount = Math.Round(baseAmount, 2, MidpointRounding.AwayFromZero),
                DiscountAmount = Math.Round(discountAmount, 2, MidpointRounding.AwayFromZero),
                SupportFee = Math.Round(supportFee, 2, MidpointRounding.AwayFromZero),
                PaymentFee = Math.Round(paymentFee, 2, MidpointRounding.AwayFromZero),
                TaxAmount = Math.Round(taxAmount, 2, MidpointRounding.AwayFromZero),
                FinalAmount = Math.Round(finalAmount, 2, MidpointRounding.AwayFromZero),
                Notes = notes.Trim(),
                GeneratedAt = DateTime.UtcNow
            };

            
            bill.saveInvoce(invoice);

            if (!string.IsNullOrWhiteSpace(customer.Email))
            {
                string subject = "Subscription renewal invoice";
                string body =
                    $"Hello {customer.FullName}, your renewal for plan {normalizedPlanCode} " +
                    $"has been prepared. Final amount: {invoice.FinalAmount:F2}.";

                bill.sendEmail(customer.Email, subject, body);
            }

            return invoice;
        }
        private decimal GetFee(string plan) => plan switch {
            "START" => 250m,
            "PRO" => 400m,
            "ENTERPRISE" => 700m,
            _ => 0m
        };
        
        private decimal GetPayment(string method) => method switch {
            "CARD" => 0.02m,
            "BANK_TRANSFER" => 0.01m,
            "PAYPAL" => 0.035m,
            "INVOICE" => 0m,
            _ => throw new ArgumentException("Unsupported payment method")
        };
        private string GetPaymentString(string method) => method switch {
            "CARD" => "card payment fee; ",
            "BANK_TRANSFER" => "bank transfer fee; ",
            "PAYPAL" => "paypal fee; ",
            "INVOICE" => "invoice payment; ",
            _ => throw new ArgumentException("Unsupported payment method")
        };
        private decimal GetTax(string country) => country switch {
            "Poland" => 0.23m,
            "Germany" => 0.19m,
            "Czech Republic" => 0.21m,
            "Norway" => 0.25m,
            _ => 0.20m
        };
    }
}
