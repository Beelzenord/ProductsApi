namespace ProductsApi.Application.ErrorHandling
{
    public class GatewayException : Exception
    {
        public GatewayException(string message, Exception inner)
          : base(message, inner) { }
    }
    public class ServiceUnavailableException : Exception
    {
        public ServiceUnavailableException(string message, Exception inner)
          : base(message, inner) { }
    }

    public class DomainException : Exception
    {
        public DomainException(string message, Exception inner)
          : base(message, inner) { }
    }

    public class ServiceException : Exception
    {
        public ServiceException(string message, Exception inner)
            : base(message, inner) { }
    }

    public class ResolverConfigurationException : Exception
    {
        public ResolverConfigurationException(string message, Exception inner)
            : base(message, inner) { }
    }

    public class ProductResolutionException : Exception
    {
        public ProductResolutionException(string message, Exception inner)
            : base(message, inner) { }
    }

    public class CategoryPathBuilderException : Exception
    {
        public CategoryPathBuilderException(string message, Exception inner)
            : base(message, inner) { }
    }

    public class AttributeLookupException : Exception
    {
        public AttributeLookupException(string message)
        : base(message) { }
        public AttributeLookupException(string message, Exception inner)
            : base(message, inner) { }
    }
}
