using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VirtoCommerce.Domain.Payment.Model;
using Uol.PagSeguro;
using Uol.PagSeguro.Constants;
using Uol.PagSeguro.Domain;
using Uol.PagSeguro.Exception;
using Uol.PagSeguro.Resources;
using System.Web;
using VirtoCommerce.Domain.Shipping.Model;
using VirtoCommerce.Domain.Customer.Services;

namespace VirtoCommerce.CoreModule.Data.Payment
{
    public class PagSeguroPaymentMethod : Domain.Payment.Model.PaymentMethod
    {
        public PagSeguroPaymentMethod() : base("PagSeguroPaymentMethod")
        {
        }


        public override Domain.Payment.Model.PaymentMethodType PaymentMethodType
        {
            get { return Domain.Payment.Model.PaymentMethodType.Redirection; }
        }

        public override PaymentMethodGroupType PaymentMethodGroupType
        {
            get { return Domain.Payment.Model.PaymentMethodGroupType.BankCard; }
        }

        public override ProcessPaymentResult ProcessPayment(ProcessPaymentEvaluationContext context)
        {
            return processPagSeguro(context);
        }

        public override PostProcessPaymentResult PostProcessPayment(PostProcessPaymentEvaluationContext context)
        {
            //TODO: insert PagSeguro API logic
            throw new NotImplementedException();
        }

        public override VoidProcessPaymentResult VoidProcessPayment(VoidProcessPaymentEvaluationContext context)
        {
            //TODO: insert PagSeguro API logic
            context.Payment.IsApproved = false;
            context.Payment.PaymentStatus = PaymentStatus.Voided;
            context.Payment.VoidedDate = DateTime.UtcNow;
            context.Payment.IsCancelled = true;
            context.Payment.CancelledDate = DateTime.UtcNow;
            return new VoidProcessPaymentResult { IsSuccess = true, NewPaymentStatus = PaymentStatus.Voided };
        }

        public override CaptureProcessPaymentResult CaptureProcessPayment(CaptureProcessPaymentEvaluationContext context)
        {
            //TODO: insert PagSeguro API logic
            context.Payment.IsApproved = true;
            context.Payment.PaymentStatus = PaymentStatus.Paid;
            context.Payment.VoidedDate = DateTime.UtcNow;
            return new CaptureProcessPaymentResult { IsSuccess = true, NewPaymentStatus = PaymentStatus.Paid };
        }

        public override RefundProcessPaymentResult RefundProcessPayment(RefundProcessPaymentEvaluationContext context)
        {
            //TODO: insert PagSeguro API logic
            throw new NotImplementedException();
        }

        public override ValidatePostProcessRequestResult ValidatePostProcessRequest(System.Collections.Specialized.NameValueCollection queryString)
        {
            return new ValidatePostProcessRequestResult { IsSuccess = false };
        }

        //http://pt.stackoverflow.com/questions/35093/comunica%C3%A7%C3%A3o-com-o-pagseguro
        public ProcessPaymentResult processPagSeguro(ProcessPaymentEvaluationContext context)
        {

            //try {

            //Use global configuration
            PagSeguroConfiguration.UrlXmlConfiguration = "D:/home/site/repository/platform/Configuration/PagSeguroConfig.xml";

            bool isSandbox = false;
            EnvironmentConfiguration.ChangeEnvironment(isSandbox);

            // Instantiate a new payment request
            PaymentRequest payment = new PaymentRequest();

            // Sets the currency
            payment.Currency = Currency.Brl;

            int countItem = 0;
            decimal productValues = 0;
            payment.AddParameter("customerId", context.Order.CustomerId);
            payment.AddParameter("customerName", context.Order.CustomerName);
            foreach (Domain.Order.Model.LineItem item in context.Order.Items)
            {
                countItem++;

                // Add an item for this payment request
                payment.Items.Add(new Item(countItem.ToString(), item.Name, item.Quantity, item.Price, 300));

                productValues += item.Price;

                payment.AddIndexedParameter("itemId", item.Id, 3);
                payment.AddIndexedParameter("itemDescription", item.Name, 3);
                payment.AddIndexedParameter("itemQuantity", item.Quantity.ToString(), 3);
                payment.AddIndexedParameter("itemPrice", item.Price.ToString(), 3);

            }


            // Sets a reference code for this payment request, it is useful to identify this payment in future notifications.
            payment.Reference = context.Order.Id;

            //TODO: shipping information for this payment request
            payment.Shipping = new Uol.PagSeguro.Domain.Shipping();
            payment.Shipping.ShippingType = ShippingType.NotSpecified;




            payment.Shipping.Cost = context.Order.Shipments.First().ShippingMethod.CalculateRates(new ShippingEvaluationContext(new Domain.Cart.Model.ShoppingCart() { Currency = context.Payment.Currency })).First().Rate;

            /*if (context.Order.Shipments.First().ShippingMethod.Name == "PagSeguro")
                payment.Shipping.Cost = context.Order.Shipments.First().ShippingMethod.CalculateRates(new ShippingEvaluationContext(new Domain.Cart.Model.ShoppingCart() { Currency = context.Payment.Currency})).First().Rate;
            else if (context.Order.Shipments.First().ShippingMethod.Name == "Shippify")
                payment.Shipping.Cost = 15.0m;*/

            Domain.Commerce.Model.Address shipAddress = context.Order.Addresses.First();

            //TODO:
            //payment.Shipping.Address = new Address(
            //    shipAddress.CountryCode,
            //    shipAddress.RegionId,
            //    shipAddress.RegionName,
            //    "Bairro",
            //    shipAddress.Zip,
            //    shipAddress.Line1,
            //    "Número",
            //    shipAddress.Line2
            //);

            payment.Shipping.Address = new Address(
                "BRA",
                "MG",
                "Minas Gerais",
                "Santa Mônica",
                "31530450",
                "Rua Professor Leopoldo Miranda",
                "160",
                "Casa 02"
            );

            //TODO: Sets your customer information? only works with katia email
            /*
            IMemberService _memberService = new ??;
            var member = _memberService.GetByIds(new[] { context.Order.CustomerId }).FirstOrDefault();
            if (member != null)
            {
                var email = member.Emails.FirstOrDefault();
                if (!string.IsNullOrEmpty(email))
                {
                    notification.Recipient = email;
                }
            }*/
            payment.Sender = new Sender(
                "Kátia Barroso",
                "katitanunesb@hotmail.com",
                new Phone("31", "994959478")
            );
            /*payment.Sender = new Sender(
                context.Payment.CustomerName,
                context.Payment.CustomerId,
                new Phone("31", "994959478")
            );*/

            //TODO: Sender CPF
            SenderDocument senderCPF = new SenderDocument(Documents.GetDocumentByType("CPF"), "01234567890");
            payment.Sender.Documents.Add(senderCPF);

            // Sets the url used by PagSeguro for redirect user after ends checkout process
            payment.RedirectUri = new Uri("http://www.minirouparia.com");

            //TODO: deifinir numeros de prestações e juros
            // Add installment without addition per payment method
            //payment.AddPaymentMethodConfig(PaymentMethodConfigKeys.MaxInstallmentsNoInterest, 1, PaymentMethodGroup.CreditCard);

            // Add installment limit per payment method
            //payment.AddPaymentMethodConfig(PaymentMethodConfigKeys.MaxInstallmentsLimit, 1, PaymentMethodGroup.CreditCard);

            // Add and remove groups and payment methods
            /*List<string> acceptDebito = new List<string>();
            acceptDebito.Add(AcceptedPaymentNames.DebitoItau);
            acceptDebito.Add(AcceptedPaymentNames.DebitoHSBC);
            acceptDebito.Add(AcceptedPaymentNames.DebitoBancoDoBrasil);
            acceptDebito.Add(AcceptedPaymentNames.DebitoBradesco);
            payment.AcceptPaymentMethodConfig(AcceptedPaymentGroups.Boleto, acceptDebito);

            List<string> acceptCredito = new List<string>();
            acceptCredito.Add(AcceptedPaymentNames.Elo);
            acceptCredito.Add(AcceptedPaymentNames.Mastercard);
            acceptCredito.Add(AcceptedPaymentNames.Visa);
            payment.AcceptPaymentMethodConfig(AcceptedPaymentGroups.CreditCard, acceptCredito);*/


            /*
            // Another way to set checkout parameters
            // Add discount per payment method
            payment.AddPaymentMethodConfig(PaymentMethodConfigKeys.DiscountPercent, 50.00, PaymentMethodGroup.CreditCard);

            // Add installment without addition per payment method
            payment.AddPaymentMethodConfig(PaymentMethodConfigKeys.MaxInstallmentsNoInterest, 6, PaymentMethodGroup.CreditCard);

            // Add installment limit per payment method
            payment.AddPaymentMethodConfig(PaymentMethodConfigKeys.MaxInstallmentsLimit, 8, PaymentMethodGroup.CreditCard);
            

            // Add and remove groups and payment methods
            List<string> accept = new List<string>();
            accept.Add(AcceptedPaymentNames.DebitoItau);
            accept.Add(AcceptedPaymentNames.DebitoHSBC);
            payment.AcceptPaymentMethodConfig(AcceptedPaymentGroups.CreditCard, accept);

            List<string> exclude = new List<string>();
            exclude.Add(AcceptedPaymentNames.Boleto);
            payment.ExcludePaymentMethodConfig(AcceptedPaymentGroups.Boleto, exclude);
            */


            /*int countItem = 0;
            foreach (Domain.Order.Model.LineItem item in context.Order.Items)
            {
                countItem++;

                // Add an item for this payment request
                payment.Items.Add(new Item(countItem.ToString(), item.Name, item.Quantity, item.Weight ?? 0));

            }

            // Sets a reference code for this payment request, it is useful to identify this payment in future notifications.
            payment.Reference = context.Order.Id;

            //TODO: shipping information for this payment request
            payment.Shipping = new Uol.PagSeguro.Domain.Shipping();
            payment.Shipping.ShippingType = ShippingType.Sedex;

            //TODO: Passando valor para ShippingCost
            payment.Shipping.Cost = 10.00m;

            Domain.Commerce.Model.Address shipAddress = context.Order.Addresses.First();

            //TODO:
            //payment.Shipping.Address = new Address(
            //    shipAddress.CountryCode,
            //    shipAddress.RegionId,
            //    shipAddress.RegionName,
            //    "Bairro",
            //    shipAddress.Zip,
            //    shipAddress.Line1,
            //    "Número",
            //    shipAddress.Line2
            //);

            payment.Shipping.Address = new Address(
                "BRA",
                "SP",
                "Sao Paulo",
                "Jardim Paulistano",
                "01452002",
                "Av. Brig. Faria Lima",
                "1384",
                "5o andar"
            );

            //TODO: Sets your customer information.
            payment.Sender = new Sender(
                context.Order.CustomerName,
                context.Order.CustomerId,
                new Phone("31", "994959478")
            );

            //TODO: Sender CPF
            SenderDocument senderCPF = new SenderDocument(Documents.GetDocumentByType("CPF"), "01234567890");
            payment.Sender.Documents.Add(senderCPF);

            // Sets the url used by PagSeguro for redirect user after ends checkout process
            payment.RedirectUri = new Uri("http://www.minirouparia.azurewebsites.net");

            //TODO: Add checkout metadata information
            //payment.AddMetaData(MetaDataItemKeys.GetItemKeyByDescription("CPF do passageiro"), "123.456.789-09", 1);
            //payment.AddMetaData("PASSENGER_PASSPORT", "23456", 1);

            // Add discount per payment method
            payment.AddPaymentMethodConfig(PaymentMethodConfigKeys.DiscountPercent, 0.00, PaymentMethodGroup.CreditCard);

            // Add installment without addition per payment method
            payment.AddPaymentMethodConfig(PaymentMethodConfigKeys.MaxInstallmentsNoInterest, 1, PaymentMethodGroup.CreditCard);

            // Add installment limit per payment method
            payment.AddPaymentMethodConfig(PaymentMethodConfigKeys.MaxInstallmentsLimit, 1, PaymentMethodGroup.CreditCard);

            // Add and remove groups and payment methods
            List<string> acceptDebito = new List<string>();
            acceptDebito.Add(AcceptedPaymentNames.DebitoItau);
            acceptDebito.Add(AcceptedPaymentNames.DebitoHSBC);
            acceptDebito.Add(AcceptedPaymentNames.DebitoBancoDoBrasil);
            acceptDebito.Add(AcceptedPaymentNames.DebitoBradesco);
            payment.AcceptPaymentMethodConfig(AcceptedPaymentGroups.Boleto, acceptDebito);

            List<string> acceptCredito = new List<string>();
            acceptCredito.Add(AcceptedPaymentNames.Elo);
            acceptCredito.Add(AcceptedPaymentNames.Mastercard);
            acceptCredito.Add(AcceptedPaymentNames.Visa);
            payment.AcceptPaymentMethodConfig(AcceptedPaymentGroups.CreditCard, acceptCredito);
            */


            //try
            //{
            AccountCredentials credentials = PagSeguroConfiguration.Credentials(isSandbox);
            //throw new NullReferenceException("CREDENTIALS: "+credentials.Email+" "+credentials.Token+" "+credentials.Attributes.ToString());
            Uri paymentRedirectUri = payment.Register(credentials);

            context.Payment.PaymentStatus = PaymentStatus.Pending;
            context.Payment.OuterId = context.Payment.Number;
            context.Payment.CapturedDate = DateTime.UtcNow;

            //throw new Exception("URI pagseguro: " + paymentRedirectUri.ToString());

            return new ProcessPaymentResult { IsSuccess = true, NewPaymentStatus = PaymentStatus.Pending, RedirectUrl = paymentRedirectUri.ToString() };

            //Console.WriteLine("URL do pagamento : " + paymentRedirectUri);
            //Console.ReadKey();
            //}
            //catch (PagSeguroServiceException exception)
            //{
            //}
            //}
            //catch (Exception exception)
            //{
            //}

            //FAILED:
            /*context.Payment.PaymentStatus = PaymentStatus.Voided;
            context.Payment.OuterId = context.Payment.Number;
            context.Payment.CapturedDate = DateTime.UtcNow;
            return new ProcessPaymentResult { IsSuccess = true, NewPaymentStatus = PaymentStatus.Voided};*/

        }
    }
}
