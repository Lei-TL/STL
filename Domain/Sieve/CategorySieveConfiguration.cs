using Sieve.Services;
using STL.Entities.CatalogModule;

namespace STL.Sieve
{
    public class CategorySieveConfiguration : ISieveConfiguration
    {
        public void Configure(SievePropertyMapper mapper)
        {
            mapper.Property<Category>(x => x.Name)
                .CanFilter()
                .CanSort();

            mapper.Property<Category>(x => x.Id)
                .CanFilter();
        }
    }
}
