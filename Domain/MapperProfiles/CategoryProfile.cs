using AutoMapper;
using STL.Endpoints.CategoryEndpoints;
using STL.Entities.CatalogModule;

namespace STL.MapperProfiles
{
    public class CategoryProfile : Profile
    {
        public CategoryProfile() 
        {
            CreateMap<CreateCategoryRequest, Category>()
                .ForMember(dest => dest.Id,
                    opt => opt.MapFrom(_ => Guid.NewGuid().ToString()))
                .ForMember(dest => dest.Name,
                    opt => opt.MapFrom(opt => opt.Name));
            CreateMap<Category, GetListCategoryResponse>()
                .ForMember(dest => dest.CategoryId,
                    opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.CategoryName,
                    opt => opt.MapFrom(src => src.Name));
            CreateMap<Category, GetCategoryResponse>();
            CreateMap<UpdateCategoryRequest, Category>()
                .ForMember(dest => dest.Name,
                    opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Id,
                    opt => opt.Ignore());
        }
    }
}
