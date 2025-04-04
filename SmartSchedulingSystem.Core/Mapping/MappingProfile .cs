// SmartSchedulingSystem.Core/Mapping/MappingProfile.cs
using AutoMapper;
using SmartSchedulingSystem.Core.DTOs;
using SmartSchedulingSystem.Data.Entities;

namespace SmartSchedulingSystem.Core.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // 定义实体与DTO之间的映射
            CreateMap<Department, DepartmentDto>().ReverseMap();
            CreateMap<Teacher, TeacherDto>()
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department.Name))
                .ReverseMap();
            CreateMap<Course, CourseDto>()
                .ForMember(dest => dest.DepartmentName, opt => opt.MapFrom(src => src.Department.Name))
                .ReverseMap();
            CreateMap<CourseSection, CourseSectionDto>()
                .ForMember(dest => dest.CourseName, opt => opt.MapFrom(src => src.Course.Name))
                .ForMember(dest => dest.CourseCode, opt => opt.MapFrom(src => src.Course.Code))
                .ForMember(dest => dest.SemesterName, opt => opt.MapFrom(src => src.Semester.Name))
                .ReverseMap();
            CreateMap<Classroom, ClassroomDto>().ReverseMap();
            CreateMap<Semester, SemesterDto>().ReverseMap();
            CreateMap<TimeSlot, TimeSlotDto>()
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime.ToString(@"hh\:mm")))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime.ToString(@"hh\:mm")))
                .ReverseMap();
            CreateMap<TeacherAvailability, TeacherAvailabilityDto>()
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Teacher.Name))
                .ForMember(dest => dest.DayName, opt => opt.MapFrom(src => GetDayName(src.TimeSlot.DayOfWeek)))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.TimeSlot.StartTime.ToString(@"hh\:mm")))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.TimeSlot.EndTime.ToString(@"hh\:mm")))
                .ReverseMap();
            CreateMap<ClassroomAvailability, ClassroomAvailabilityDto>()
                .ForMember(dest => dest.ClassroomName, opt => opt.MapFrom(src => src.Classroom.Name))
                .ForMember(dest => dest.DayName, opt => opt.MapFrom(src => GetDayName(src.TimeSlot.DayOfWeek)))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.TimeSlot.StartTime.ToString(@"hh\:mm")))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.TimeSlot.EndTime.ToString(@"hh\:mm")))
                .ReverseMap();
            CreateMap<SchedulingConstraint, SchedulingConstraintDto>().ReverseMap();
            // 配置 ScheduleResult 到 ScheduleResultDto 的映射
            CreateMap<ScheduleResult, ScheduleResultDto>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            // 配置 ScheduleItem 到 ScheduleItemDto 的映射
            CreateMap<ScheduleItem, ScheduleItemDto>()
                .ForMember(dest => dest.CourseName, opt => opt.MapFrom(src => src.CourseSection.Course.Name))
                .ForMember(dest => dest.CourseCode, opt => opt.MapFrom(src => src.CourseSection.Course.Code))
                .ForMember(dest => dest.SectionCode, opt => opt.MapFrom(src => src.CourseSection.SectionCode))
                .ForMember(dest => dest.TeacherName, opt => opt.MapFrom(src => src.Teacher.Name))
                .ForMember(dest => dest.Building, opt => opt.MapFrom(src => src.Classroom.Building))
                .ForMember(dest => dest.ClassroomName, opt => opt.MapFrom(src => src.Classroom.Name))
                .ForMember(dest => dest.DayOfWeek, opt => opt.MapFrom(src => src.TimeSlot.DayOfWeek))
                .ForMember(dest => dest.DayName, opt => opt.MapFrom(src => GetDayName(src.TimeSlot.DayOfWeek)))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.TimeSlot.StartTime.ToString(@"hh\:mm")))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.TimeSlot.EndTime.ToString(@"hh\:mm")));
        }

        private string GetDayName(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                1 => "周一",
                2 => "周二",
                3 => "周三",
                4 => "周四",
                5 => "周五",
                6 => "周六",
                7 => "周日",
                _ => "未知"
            };
        }
    }
}