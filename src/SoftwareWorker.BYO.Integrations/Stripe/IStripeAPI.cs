using Refit;
using SoftwareWorker.BYO.Integrations.Stripe.Model;

namespace SoftwareWorker.BYO.Integrations.Stripe
{
    /// <summary>
    /// Stripe REST API v1 - https://stripe.com/docs/api
    /// </summary>
    public interface IStripeAPI
    {
        // Customers
        [Get("/v1/customers")]
        Task<StripeListResponse<StripeCustomer>> ListCustomers([Query] int? limit = null, [Query] string? starting_after = null);

        [Get("/v1/customers/{id}")]
        Task<StripeCustomer> GetCustomer([AliasAs("id")] string customerId);

        [Post("/v1/customers")]
        Task<StripeCustomer> CreateCustomer([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);

        [Post("/v1/customers/{id}")]
        Task<StripeCustomer> UpdateCustomer([AliasAs("id")] string customerId, [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);

        [Delete("/v1/customers/{id}")]
        Task<StripeDeleteResponse> DeleteCustomer([AliasAs("id")] string customerId);

        // Payment Intents
        [Get("/v1/payment_intents")]
        Task<StripeListResponse<StripePaymentIntent>> ListPaymentIntents([Query] int? limit = null, [Query] string? starting_after = null);

        [Get("/v1/payment_intents/{id}")]
        Task<StripePaymentIntent> GetPaymentIntent([AliasAs("id")] string paymentIntentId);

        [Post("/v1/payment_intents")]
        Task<StripePaymentIntent> CreatePaymentIntent([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);

        [Post("/v1/payment_intents/{id}")]
        Task<StripePaymentIntent> UpdatePaymentIntent([AliasAs("id")] string paymentIntentId, [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);

        [Post("/v1/payment_intents/{id}/cancel")]
        Task<StripePaymentIntent> CancelPaymentIntent([AliasAs("id")] string paymentIntentId);

        // Charges
        [Get("/v1/charges")]
        Task<StripeListResponse<StripeCharge>> ListCharges([Query] int? limit = null, [Query] string? starting_after = null);

        [Get("/v1/charges/{id}")]
        Task<StripeCharge> GetCharge([AliasAs("id")] string chargeId);

        // Refunds
        [Get("/v1/refunds")]
        Task<StripeListResponse<StripeRefund>> ListRefunds([Query] int? limit = null, [Query] string? starting_after = null);

        [Get("/v1/refunds/{id}")]
        Task<StripeRefund> GetRefund([AliasAs("id")] string refundId);

        [Post("/v1/refunds")]
        Task<StripeRefund> CreateRefund([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);

        // Payment Methods
        [Get("/v1/payment_methods/{id}")]
        Task<StripePaymentMethod> GetPaymentMethod([AliasAs("id")] string paymentMethodId);

        [Post("/v1/payment_methods")]
        Task<StripePaymentMethod> CreatePaymentMethod([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);

        [Post("/v1/payment_methods/{id}/attach")]
        Task<StripePaymentMethod> AttachPaymentMethod([AliasAs("id")] string paymentMethodId, [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);

        // Subscriptions
        [Get("/v1/subscriptions")]
        Task<StripeListResponse<StripeSubscription>> ListSubscriptions([Query] int? limit = null, [Query] string? starting_after = null);

        [Get("/v1/subscriptions/{id}")]
        Task<StripeSubscription> GetSubscription([AliasAs("id")] string subscriptionId);

        [Post("/v1/subscriptions")]
        Task<StripeSubscription> CreateSubscription([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);

        [Post("/v1/subscriptions/{id}")]
        Task<StripeSubscription> UpdateSubscription([AliasAs("id")] string subscriptionId, [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);

        [Delete("/v1/subscriptions/{id}")]
        Task<StripeSubscription> CancelSubscription([AliasAs("id")] string subscriptionId);

        // Products
        [Get("/v1/products")]
        Task<StripeListResponse<StripeProduct>> ListProducts([Query] int? limit = null, [Query] string? starting_after = null);

        [Get("/v1/products/{id}")]
        Task<StripeProduct> GetProduct([AliasAs("id")] string productId);

        [Post("/v1/products")]
        Task<StripeProduct> CreateProduct([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);

        [Post("/v1/products/{id}")]
        Task<StripeProduct> UpdateProduct([AliasAs("id")] string productId, [Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);

        [Delete("/v1/products/{id}")]
        Task<StripeDeleteResponse> DeleteProduct([AliasAs("id")] string productId);

        // Prices
        [Get("/v1/prices")]
        Task<StripeListResponse<StripePrice>> ListPrices([Query] int? limit = null, [Query] string? starting_after = null);

        [Get("/v1/prices/{id}")]
        Task<StripePrice> GetPrice([AliasAs("id")] string priceId);

        [Post("/v1/prices")]
        Task<StripePrice> CreatePrice([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);

        // Invoices
        [Get("/v1/invoices")]
        Task<StripeListResponse<StripeInvoice>> ListInvoices([Query] int? limit = null, [Query] string? starting_after = null);

        [Get("/v1/invoices/{id}")]
        Task<StripeInvoice> GetInvoice([AliasAs("id")] string invoiceId);

        [Post("/v1/invoices")]
        Task<StripeInvoice> CreateInvoice([Body(BodySerializationMethod.UrlEncoded)] Dictionary<string, object> data);

        [Post("/v1/invoices/{id}/pay")]
        Task<StripeInvoice> PayInvoice([AliasAs("id")] string invoiceId);
    }
}
