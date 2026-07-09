using Sieve.Services;
using STL.Entities.CatalogModule;

namespace STL.Sieve
{
    public class ProductSieveConfiguration : ISieveConfiguration
    {
        public void Configure(SievePropertyMapper mapper)
        {
            mapper.Property<Product>(x => x.Name)
                .CanFilter()
                .CanSort();
            mapper.Property<Product>(x => x.Id)
                .CanFilter();
        }
    }
}
