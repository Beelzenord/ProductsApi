namespace ProductsApi.Application.ErrorHandling
{
    public class GatewayException : Exception
    {
        public GatewayException(string message)
          : base(message) { }
        public GatewayException(string message, Exception inner)
          : base(message, inner) { }
    }
    public class ServiceUnavailableException : Exception
    {
        public ServiceUnavailableException(string message)
          : base(message) { }
        public ServiceUnavailableException(string message, Exception inner)
          : base(message, inner) { }
    }

    public class DomainException : Exception
    {
        public DomainException(string message)
          : base(message) { }
        public DomainException(string message, Exception inner)
          : base(message, inner) { }
    }

    public class ServiceException : Exception
    {
        public ServiceException(string message)
          : base(message) { }
        public ServiceException(string message, Exception inner)
            : base(message, inner) { }
    }

    public class ResolverConfigurationException : Exception
    {
        public ResolverConfigurationException(string message)
          : base(message) { }
        public ResolverConfigurationException(string message, Exception inner)
            : base(message, inner) { }
    }

    public class ProductResolutionException : Exception
    {
        public ProductResolutionException(string message)
            : base(message) { }
        public ProductResolutionException(string message, Exception inner)
            : base(message, inner) { }
    }

    public class CategoryPathBuilderException : Exception
    {
        public CategoryPathBuilderException(string message)
            : base(message) { }
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

    public class StrategyImplementationException : Exception
    {
        public StrategyImplementationException(string message)
            : base(message) { }

        public StrategyImplementationException(string message, Exception inner)
            : base(message, inner) { }
    }

    public class AttributeMappingException : Exception
    {
        public AttributeMappingException(string message)
            : base(message) { }

        public AttributeMappingException(string message, Exception inner)
            : base(message, inner) { }
    }

    public class CategoryMappingException : Exception
    {
        public CategoryMappingException(string message)
            : base(message) { }

        public CategoryMappingException(string message, Exception inner)
            : base(message, inner) { }
    }
}
