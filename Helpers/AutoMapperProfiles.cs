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
            CreateMap<Photo, PhotoDto>();
            CreateMap<RegisterDto, AppUser>();
            CreateMap<string, DateOnly>().ConvertUsing(s => DateOnly.Parse(s));
            CreateMap<Message, MessageDto>()
                        .ForMember(dest => dest.SenderPhotoUrl, opt => opt.MapFrom(src =>
                            src.Sender.Photos.FirstOrDefault(x => x.IsMain)!.Url))
                        .ForMember(dest => dest.RecipientPhotoUrl, opt => opt.MapFrom(src =>
                            src.Recipient.Photos.FirstOrDefault(x => x.IsMain)!.Url));
            // nem utc-t ad vissza alapbol a sqlite 
            CreateMap<DateTime, DateTime>().ConvertUsing(d => DateTime.SpecifyKind(d, DateTimeKind.Utc));
            CreateMap<DateTime?, DateTime?>().ConvertUsing(d => d.HasValue ? DateTime.SpecifyKind(d.Value, DateTimeKind.Utc) : null);
        }
    }
}