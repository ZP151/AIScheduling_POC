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
            // Define the mapping between entities and DTOs
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
            
            // Extended DTO mapping configuration

            CreateMap<Teacher, TeacherExtDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.TeacherId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.DepartmentId, opt => opt.MapFrom(src => src.DepartmentId))
                .ForMember(dest => dest.MaxDailyHours, opt => opt.MapFrom(src => 8)) // Default value
                .ForMember(dest => dest.MaxWeeklyHours, opt => opt.MapFrom(src => 40)) // Default value
                .ForMember(dest => dest.MaxConsecutiveHours, opt => opt.MapFrom(src => 4)) // Default value
                .ReverseMap();
                
            CreateMap<Classroom, ClassroomExtDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ClassroomId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Building, opt => opt.MapFrom(src => src.Building))
                .ForMember(dest => dest.Capacity, opt => opt.MapFrom(src => src.Capacity))
                .ForMember(dest => dest.Type, opt => opt.MapFrom(src => "General classroom")) // Default value
                .ForMember(dest => dest.CampusId, opt => opt.MapFrom(src => 1)) // Default value
                .ForMember(dest => dest.CampusName, opt => opt.MapFrom(src => "Main campus")) // Default value
                .ReverseMap();
                
            CreateMap<TimeSlot, TimeSlotExtDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.TimeSlotId))
                .ForMember(dest => dest.DayOfWeek, opt => opt.MapFrom(src => src.DayOfWeek))
                .ForMember(dest => dest.DayName, opt => opt.MapFrom(src => GetDayName(src.DayOfWeek)))
                .ForMember(dest => dest.StartTime, opt => opt.MapFrom(src => src.StartTime.ToString(@"hh\:mm")))
                .ForMember(dest => dest.EndTime, opt => opt.MapFrom(src => src.EndTime.ToString(@"hh\:mm")))
                .ReverseMap();
                
            CreateMap<CourseSection, CourseSectionExtDto>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.CourseSectionId))
                .ForMember(dest => dest.CourseId, opt => opt.MapFrom(src => src.CourseId))
                .ForMember(dest => dest.CourseCode, opt => opt.MapFrom(src => src.Course.Code))
                .ForMember(dest => dest.CourseName, opt => opt.MapFrom(src => src.Course.Name))
                .ForMember(dest => dest.SectionCode, opt => opt.MapFrom(src => src.SectionCode))
                .ForMember(dest => dest.Credits, opt => opt.MapFrom(src => 3)) // Default value
                .ForMember(dest => dest.WeeklyHours, opt => opt.MapFrom(src => 3.0)) // Default value
                .ForMember(dest => dest.SessionsPerWeek, opt => opt.MapFrom(src => 2)) // Default value
                .ForMember(dest => dest.HoursPerSession, opt => opt.MapFrom(src => 1.5)) // Default value
                .ForMember(dest => dest.Enrollment, opt => opt.MapFrom(src => src.ActualEnrollment))
                .ForMember(dest => dest.DepartmentId, opt => opt.MapFrom(src => src.Course.DepartmentId))
                .ReverseMap();
            
            // Configure mapping from ScheduleResult to ScheduleResultDto
            CreateMap<ScheduleResult, ScheduleResultDto>()
                .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items));

            // Configure mapping from ScheduleItem to ScheduleItemDto
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
                1 => "Monday",
                2 => "Tuesday",
                3 => "Wednesday",
                4 => "Thursday",
                5 => "Friday",
                6 => "Saturday",
                7 => "Sunday",
                _ => "Unknown"
            };
        }
    }
}