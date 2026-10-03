using Refit;
using SoftwareWorker.BYO.Integrations.Helpers;
using SoftwareWorker.BYO.Integrations.Stripe.Model;

namespace SoftwareWorker.BYO.Integrations.Stripe
{
    public class StripeConnector
    {
        private readonly IStripeAPI _api;
        private readonly bool _isVerbose;

        public StripeConnector(string secretKey, bool isVerbose = false)
        {
            _isVerbose = isVerbose;

            var httpClient = new HttpClient
            {
                BaseAddress = new Uri("https://api.stripe.com")
            };
            httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {secretKey}");

            _api = RestService.For<IStripeAPI>(httpClient);
        }

        // Customers
        public async Task<List<StripeCustomer>> ListCustomersAsync(int? maxResults = null)
        {
            var customers = new List<StripeCustomer>();
            string? startingAfter = null;
            int limit = 100;

            while (true)
            {
                var response = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListCustomers(limit, startingAfter));

                if (response == null || response.Data == null || response.Data.Count == 0)
                    break;

                customers.AddRange(response.Data);

                if (maxResults.HasValue && customers.Count >= maxResults.Value)
                {
                    return customers.Take(maxResults.Value).ToList();
                }

                if (!response.HasMore)
                    break;

                startingAfter = response.Data.Last().Id;
            }

            return customers;
        }

        public async Task<StripeCustomer?> GetCustomerAsync(string customerId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetCustomer(customerId));
        }

        public async Task<StripeCustomer?> CreateCustomerAsync(string email, string? name = null, Dictionary<string, object>? metadata = null)
        {
            var data = new Dictionary<string, object> { ["email"] = email };
            if (name != null) data["name"] = name;
            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    data[$"metadata[{kvp.Key}]"] = kvp.Value;
                }
            }

            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateCustomer(data));
        }

        public async Task<StripeCustomer?> UpdateCustomerAsync(string customerId, Dictionary<string, object> updates)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateCustomer(customerId, updates));
        }

        public async Task<bool> DeleteCustomerAsync(string customerId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.DeleteCustomer(customerId));
            return result?.Deleted ?? false;
        }

        // Payment Intents
        public async Task<List<StripePaymentIntent>> ListPaymentIntentsAsync(int? maxResults = null)
        {
            var paymentIntents = new List<StripePaymentIntent>();
            string? startingAfter = null;
            int limit = 100;

            while (true)
            {
                var response = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListPaymentIntents(limit, startingAfter));

                if (response == null || response.Data == null || response.Data.Count == 0)
                    break;

                paymentIntents.AddRange(response.Data);

                if (maxResults.HasValue && paymentIntents.Count >= maxResults.Value)
                {
                    return paymentIntents.Take(maxResults.Value).ToList();
                }

                if (!response.HasMore)
                    break;

                startingAfter = response.Data.Last().Id;
            }

            return paymentIntents;
        }

        public async Task<StripePaymentIntent?> GetPaymentIntentAsync(string paymentIntentId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetPaymentIntent(paymentIntentId));
        }

        public async Task<StripePaymentIntent?> CreatePaymentIntentAsync(long amount, string currency, string? customerId = null, Dictionary<string, object>? metadata = null)
        {
            var data = new Dictionary<string, object>
            {
                ["amount"] = amount,
                ["currency"] = currency
            };

            if (customerId != null) data["customer"] = customerId;
            if (metadata != null)
            {
                foreach (var kvp in metadata)
                {
                    data[$"metadata[{kvp.Key}]"] = kvp.Value;
                }
            }

            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreatePaymentIntent(data));
        }

        public async Task<StripePaymentIntent?> UpdatePaymentIntentAsync(string paymentIntentId, Dictionary<string, object> updates)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdatePaymentIntent(paymentIntentId, updates));
        }

        public async Task<StripePaymentIntent?> CancelPaymentIntentAsync(string paymentIntentId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CancelPaymentIntent(paymentIntentId));
        }

        // Charges
        public async Task<List<StripeCharge>> ListChargesAsync(int? maxResults = null)
        {
            var charges = new List<StripeCharge>();
            string? startingAfter = null;
            int limit = 100;

            while (true)
            {
                var response = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListCharges(limit, startingAfter));

                if (response == null || response.Data == null || response.Data.Count == 0)
                    break;

                charges.AddRange(response.Data);

                if (maxResults.HasValue && charges.Count >= maxResults.Value)
                {
                    return charges.Take(maxResults.Value).ToList();
                }

                if (!response.HasMore)
                    break;

                startingAfter = response.Data.Last().Id;
            }

            return charges;
        }

        public async Task<StripeCharge?> GetChargeAsync(string chargeId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetCharge(chargeId));
        }

        // Refunds
        public async Task<List<StripeRefund>> ListRefundsAsync(int? maxResults = null)
        {
            var refunds = new List<StripeRefund>();
            string? startingAfter = null;
            int limit = 100;

            while (true)
            {
                var response = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListRefunds(limit, startingAfter));

                if (response == null || response.Data == null || response.Data.Count == 0)
                    break;

                refunds.AddRange(response.Data);

                if (maxResults.HasValue && refunds.Count >= maxResults.Value)
                {
                    return refunds.Take(maxResults.Value).ToList();
                }

                if (!response.HasMore)
                    break;

                startingAfter = response.Data.Last().Id;
            }

            return refunds;
        }

        public async Task<StripeRefund?> GetRefundAsync(string refundId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetRefund(refundId));
        }

        public async Task<StripeRefund?> CreateRefundAsync(string chargeOrPaymentIntentId, long? amount = null, string? reason = null)
        {
            var data = new Dictionary<string, object>();

            if (chargeOrPaymentIntentId.StartsWith("ch_"))
                data["charge"] = chargeOrPaymentIntentId;
            else if (chargeOrPaymentIntentId.StartsWith("pi_"))
                data["payment_intent"] = chargeOrPaymentIntentId;

            if (amount.HasValue) data["amount"] = amount.Value;
            if (reason != null) data["reason"] = reason;

            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateRefund(data));
        }

        // Payment Methods
        public async Task<StripePaymentMethod?> GetPaymentMethodAsync(string paymentMethodId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetPaymentMethod(paymentMethodId));
        }

        public async Task<StripePaymentMethod?> CreatePaymentMethodAsync(string type, Dictionary<string, object> paymentMethodData)
        {
            var data = new Dictionary<string, object> { ["type"] = type };
            foreach (var kvp in paymentMethodData)
            {
                data[kvp.Key] = kvp.Value;
            }

            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreatePaymentMethod(data));
        }

        public async Task<StripePaymentMethod?> AttachPaymentMethodAsync(string paymentMethodId, string customerId)
        {
            var data = new Dictionary<string, object> { ["customer"] = customerId };
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.AttachPaymentMethod(paymentMethodId, data));
        }

        // Subscriptions
        public async Task<List<StripeSubscription>> ListSubscriptionsAsync(int? maxResults = null)
        {
            var subscriptions = new List<StripeSubscription>();
            string? startingAfter = null;
            int limit = 100;

            while (true)
            {
                var response = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListSubscriptions(limit, startingAfter));

                if (response == null || response.Data == null || response.Data.Count == 0)
                    break;

                subscriptions.AddRange(response.Data);

                if (maxResults.HasValue && subscriptions.Count >= maxResults.Value)
                {
                    return subscriptions.Take(maxResults.Value).ToList();
                }

                if (!response.HasMore)
                    break;

                startingAfter = response.Data.Last().Id;
            }

            return subscriptions;
        }

        public async Task<StripeSubscription?> GetSubscriptionAsync(string subscriptionId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetSubscription(subscriptionId));
        }

        public async Task<StripeSubscription?> CreateSubscriptionAsync(string customerId, string priceId, Dictionary<string, object>? additionalData = null)
        {
            var data = new Dictionary<string, object>
            {
                ["customer"] = customerId,
                ["items[0][price]"] = priceId
            };

            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    data[kvp.Key] = kvp.Value;
                }
            }

            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateSubscription(data));
        }

        public async Task<StripeSubscription?> UpdateSubscriptionAsync(string subscriptionId, Dictionary<string, object> updates)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateSubscription(subscriptionId, updates));
        }

        public async Task<StripeSubscription?> CancelSubscriptionAsync(string subscriptionId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CancelSubscription(subscriptionId));
        }

        // Products
        public async Task<List<StripeProduct>> ListProductsAsync(int? maxResults = null)
        {
            var products = new List<StripeProduct>();
            string? startingAfter = null;
            int limit = 100;

            while (true)
            {
                var response = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListProducts(limit, startingAfter));

                if (response == null || response.Data == null || response.Data.Count == 0)
                    break;

                products.AddRange(response.Data);

                if (maxResults.HasValue && products.Count >= maxResults.Value)
                {
                    return products.Take(maxResults.Value).ToList();
                }

                if (!response.HasMore)
                    break;

                startingAfter = response.Data.Last().Id;
            }

            return products;
        }

        public async Task<StripeProduct?> GetProductAsync(string productId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetProduct(productId));
        }

        public async Task<StripeProduct?> CreateProductAsync(string name, Dictionary<string, object>? additionalData = null)
        {
            var data = new Dictionary<string, object> { ["name"] = name };
            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    data[kvp.Key] = kvp.Value;
                }
            }

            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateProduct(data));
        }

        public async Task<StripeProduct?> UpdateProductAsync(string productId, Dictionary<string, object> updates)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.UpdateProduct(productId, updates));
        }

        public async Task<bool> DeleteProductAsync(string productId)
        {
            var result = await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.DeleteProduct(productId));
            return result?.Deleted ?? false;
        }

        // Prices
        public async Task<List<StripePrice>> ListPricesAsync(int? maxResults = null)
        {
            var prices = new List<StripePrice>();
            string? startingAfter = null;
            int limit = 100;

            while (true)
            {
                var response = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListPrices(limit, startingAfter));

                if (response == null || response.Data == null || response.Data.Count == 0)
                    break;

                prices.AddRange(response.Data);

                if (maxResults.HasValue && prices.Count >= maxResults.Value)
                {
                    return prices.Take(maxResults.Value).ToList();
                }

                if (!response.HasMore)
                    break;

                startingAfter = response.Data.Last().Id;
            }

            return prices;
        }

        public async Task<StripePrice?> GetPriceAsync(string priceId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetPrice(priceId));
        }

        public async Task<StripePrice?> CreatePriceAsync(string productId, long unitAmount, string currency, Dictionary<string, object>? additionalData = null)
        {
            var data = new Dictionary<string, object>
            {
                ["product"] = productId,
                ["unit_amount"] = unitAmount,
                ["currency"] = currency
            };

            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    data[kvp.Key] = kvp.Value;
                }
            }

            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreatePrice(data));
        }

        // Invoices
        public async Task<List<StripeInvoice>> ListInvoicesAsync(int? maxResults = null)
        {
            var invoices = new List<StripeInvoice>();
            string? startingAfter = null;
            int limit = 100;

            while (true)
            {
                var response = await ResilienceHelper.ExecuteWithResilienceAsync(
                    async () => await _api.ListInvoices(limit, startingAfter));

                if (response == null || response.Data == null || response.Data.Count == 0)
                    break;

                invoices.AddRange(response.Data);

                if (maxResults.HasValue && invoices.Count >= maxResults.Value)
                {
                    return invoices.Take(maxResults.Value).ToList();
                }

                if (!response.HasMore)
                    break;

                startingAfter = response.Data.Last().Id;
            }

            return invoices;
        }

        public async Task<StripeInvoice?> GetInvoiceAsync(string invoiceId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.GetInvoice(invoiceId));
        }

        public async Task<StripeInvoice?> CreateInvoiceAsync(string customerId, Dictionary<string, object>? additionalData = null)
        {
            var data = new Dictionary<string, object> { ["customer"] = customerId };
            if (additionalData != null)
            {
                foreach (var kvp in additionalData)
                {
                    data[kvp.Key] = kvp.Value;
                }
            }

            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.CreateInvoice(data));
        }

        public async Task<StripeInvoice?> PayInvoiceAsync(string invoiceId)
        {
            return await ResilienceHelper.ExecuteWithResilienceAsync(
                async () => await _api.PayInvoice(invoiceId));
        }
    }
}
