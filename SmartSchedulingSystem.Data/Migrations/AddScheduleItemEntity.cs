using Microsoft.EntityFrameworkCore.Migrations;
using System.Data.SqlClient;

public partial class AddScheduleItemEntity : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // 使用 IF NOT EXISTS 检查并创建表
        migrationBuilder.Sql(@"
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ScheduleItem')
            BEGIN
                CREATE TABLE [ScheduleItem] (
                    [ScheduleItemId] int NOT NULL IDENTITY,
                    [ScheduleResultId] int NOT NULL,
                    [CourseSectionId] int NOT NULL,
                    [TeacherId] int NOT NULL,
                    [ClassroomId] int NOT NULL,
                    [TimeSlotId] int NOT NULL,
                    CONSTRAINT [PK_ScheduleItem] PRIMARY KEY ([ScheduleItemId]),
                    CONSTRAINT [FK_ScheduleItem_ScheduleResults_ScheduleResultId] 
                        FOREIGN KEY ([ScheduleResultId]) 
                        REFERENCES [ScheduleResults]([ScheduleId]) 
                        ON DELETE NO ACTION,
                    CONSTRAINT [FK_ScheduleItem_CourseSections_CourseSectionId] 
                        FOREIGN KEY ([CourseSectionId]) 
                        REFERENCES [CourseSections]([CourseSectionId]) 
                        ON DELETE NO ACTION,
                    CONSTRAINT [FK_ScheduleItem_Teachers_TeacherId] 
                        FOREIGN KEY ([TeacherId]) 
                        REFERENCES [Teachers]([TeacherId]) 
                        ON DELETE NO ACTION,
                    CONSTRAINT [FK_ScheduleItem_Classrooms_ClassroomId] 
                        FOREIGN KEY ([ClassroomId]) 
                        REFERENCES [Classrooms]([ClassroomId]) 
                        ON DELETE NO ACTION,
                    CONSTRAINT [FK_ScheduleItem_TimeSlots_TimeSlotId] 
                        FOREIGN KEY ([TimeSlotId]) 
                        REFERENCES [TimeSlots]([TimeSlotId]) 
                        ON DELETE NO ACTION
                );

                CREATE INDEX [IX_ScheduleItem_ScheduleResultId] 
                ON [ScheduleItem]([ScheduleResultId]);

                CREATE INDEX [IX_ScheduleItem_CourseSectionId] 
                ON [ScheduleItem]([CourseSectionId]);

                CREATE INDEX [IX_ScheduleItem_TeacherId] 
                ON [ScheduleItem]([TeacherId]);

                CREATE INDEX [IX_ScheduleItem_ClassroomId] 
                ON [ScheduleItem]([ClassroomId]);

                CREATE INDEX [IX_ScheduleItem_TimeSlotId] 
                ON [ScheduleItem]([TimeSlotId]);
            END

            -- 检查并创建 AISchedulingSuggestions 表（如果不存在）
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AISchedulingSuggestions')
            BEGIN
                CREATE TABLE [AISchedulingSuggestions] (
                    [SuggestionId] int NOT NULL IDENTITY,
                    [ScheduleRequestId] int NOT NULL,
                    [SuggestionData] nvarchar(max) NOT NULL,
                    [Score] float NOT NULL,
                    [CreatedAt] datetime2 NOT NULL DEFAULT (GETDATE()),
                    CONSTRAINT [PK_AISchedulingSuggestions] PRIMARY KEY ([SuggestionId])
                );
            END

            -- 处理可能存在的其他表
            IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Classrooms')
            BEGIN
                CREATE TABLE [Classrooms] (
                    [ClassroomId] int NOT NULL IDENTITY,
                    [Name] nvarchar(max) NOT NULL,
                    [Building] nvarchar(max) NOT NULL,
                    [Capacity] int NOT NULL,
                    [HasComputers] bit NOT NULL,
                    [HasProjector] bit NOT NULL,
                    CONSTRAINT [PK_Classrooms] PRIMARY KEY ([ClassroomId])
                );
            END
        ");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(@"
            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'ScheduleItem')
                DROP TABLE [ScheduleItem];
            
            IF EXISTS (SELECT * FROM sys.tables WHERE name = 'AISchedulingSuggestions')
                DROP TABLE [AISchedulingSuggestions];
        ");
    }
}