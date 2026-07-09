using AutoMapper;
using STL.Endpoints.ProductEndpoints;
using STL.Entities.CatalogModule;

namespace STL.MapperProfiles
{
    public class ProductProfile : Profile
    {
        public ProductProfile()
        {
            CreateMap<CreateProductRequest, Product>()
                .ForMember(dest => dest.Id, 
                    opt => opt.MapFrom(_ => Guid.NewGuid().ToString()))
                .ForMember(dest => dest.Deleted, 
                    opt => opt.MapFrom(_ => false));
            CreateMap<Product, GetProductResponse>();
            CreateMap<UpdateProductRequest, Product>()
                .ForMember(dest => dest.Id,
                    opt => opt.Ignore());
        }
    }
}
