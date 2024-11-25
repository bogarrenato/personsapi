using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using AutoMapper;

namespace API.Helpers
{
    public class AutoMapperProfiles : Profile
    {
        public AutoMapperProfiles()
        {
            //From AppUser Go To MemberDto
            CreateMap<AppUser, MemberDto>()
                .ForMember(dest => dest.Age, o => o.MapFrom(src => src.DateOfBirth.CalculateAge()))
                .ForMember(dest => dest.PhotoUrl, opt => opt.MapFrom(src => src.Photos.FirstOrDefault(x => x.IsMain)!.Url));
            CreateMap<Photo, PhotoDtos>();
        }
    }
}